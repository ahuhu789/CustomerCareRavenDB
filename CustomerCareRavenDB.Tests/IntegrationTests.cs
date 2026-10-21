using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Indexes;
using CustomerCareRavenDB.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.ServerWide.Operations;

namespace CustomerCareRavenDB.Tests;

public class IntegrationTests : IAsyncLifetime
{
    private readonly string _testDbName = $"Test_Integration_{Guid.NewGuid():N}";
    private IDocumentStore _store = null!;
    private RavenDbContext _context = null!;
    private DatabaseSeeder _seeder = null!;

    public async Task InitializeAsync()
    {
        var settings = Options.Create(new RavenSettings
        {
            Urls = [TestSettings.RavenUrl],
            DatabaseName = _testDbName
        });

        _context = new RavenDbContext(settings, NullLogger<RavenDbContext>.Instance);
        _store = _context.Store;

        var validator = new DocumentValidator();
        _seeder = new DatabaseSeeder(_context, validator, NullLogger<DatabaseSeeder>.Instance);

        // Đảm bảo database test được tạo
        await _context.EnsureDatabaseExistsAsync(_testDbName);

        // Đăng ký static index ServiceHistories_ByCustomerAndDate
        await IndexCreation.CreateIndexesAsync(
            typeof(ServiceHistories_ByCustomerAndDate).Assembly,
            _store);
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _store.Maintenance.Server.SendAsync(new DeleteDatabasesOperation(_testDbName, hardDelete: true));
        }
        catch
        {
            // Bỏ qua lỗi dọn dẹp
        }
        finally
        {
            _context.Dispose();
        }
    }

    [Fact]
    public async Task Seeder_InitializesCompleteBusinessWorkflow_AndIsIdempotent()
    {
        // Act 1: Lần nạp đầu tiên
        var firstRunResult = await _seeder.SeedAsync();
        Assert.True(firstRunResult, "Lần seed đầu tiên phải trả về true khi nạp thành công.");

        // Kiểm tra dữ liệu được nạp vào
        using (var session = _context.OpenAsyncSession())
        {
            var customer = await session.LoadAsync<Customer>("customers/001");
            Assert.NotNull(customer);
            Assert.Equal("Nguyễn Văn An", customer.FullName);
            Assert.NotNull(customer.Address);
            Assert.Equal("123 Nguyễn Huệ", customer.Address.Street);

            var staff = await session.LoadAsync<Staff>("staffs/001");
            Assert.NotNull(staff);
            Assert.Equal("Trần Thị Mai", staff.FullName);

            var service = await session.LoadAsync<Service>("services/001");
            Assert.NotNull(service);
            Assert.Equal(350000m, service.Price);

            var appointment = await session.LoadAsync<Appointment>("appointments/001");
            Assert.NotNull(appointment);
            Assert.Equal(customer.Id, appointment.CustomerId);
            Assert.Equal(service.Id, appointment.ServiceId);

            var history = await session.LoadAsync<ServiceHistory>("servicehistories/001");
            Assert.NotNull(history);
            Assert.Equal(appointment.Id, history.AppointmentId);
            Assert.Equal(customer.Id, history.CustomerId);
            Assert.Equal(service.Id, history.ServiceId);
            Assert.Equal(staff.Id, history.StaffId);

            var feedback = await session.LoadAsync<Feedback>("feedbacks/001");
            Assert.NotNull(feedback);
            Assert.Equal(customer.Id, feedback.CustomerId);
            Assert.Equal(appointment.Id, feedback.AppointmentId);
            Assert.Equal(5, feedback.Rating);
        }

        // Act 2: Lần nạp thứ hai (kiểm tra tính Idempotent)
        var secondRunResult = await _seeder.SeedAsync();
        Assert.False(secondRunResult, "Lần seed thứ hai phải trả về false và không được nạp đè dữ liệu.");
    }

    [Fact]
    public async Task StaticIndex_ServiceHistories_ByCustomerAndDate_CanBeQueried()
    {
        // Nạp dữ liệu mẫu
        await _seeder.SeedAsync();

        using var session = _context.OpenAsyncSession();

        // Chờ index xử lý xong (Non-stale)
        var histories = await session.Query<ServiceHistory, ServiceHistories_ByCustomerAndDate>()
            .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(10)))
            .Where(x => x.CustomerId == "customers/001")
            .OrderByDescending(x => x.ServiceDate)
            .ToListAsync();

        Assert.NotEmpty(histories);
        Assert.All(histories, h => Assert.Equal("customers/001", h.CustomerId));
    }

    [Fact]
    public async Task RqlQuery_WithLoad_RetrievesReferencedCustomer()
    {
        await _seeder.SeedAsync();

        using var session = _context.OpenAsyncSession();

        // RQL Query #10: sử dụng load để lấy thông tin Customer từ Appointment
        var rql = @"
            from Appointments as a
            load a.CustomerId as c
            select {
                AppointmentId: id(a),
                CustomerName: c.FullName,
                CustomerPhone: c.Phone,
                Status: a.Status
            }";

        var results = await session.Advanced.AsyncRawQuery<dynamic>(rql)
            .ToListAsync();

        Assert.NotEmpty(results);
    }
}
