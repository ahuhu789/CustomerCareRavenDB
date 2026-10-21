using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Models;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace CustomerCareRavenDB.Tests;

public class ValidationTests : IAsyncLifetime
{
    private readonly string _testDbName = $"Test_Validation_{Guid.NewGuid():N}";
    private IDocumentStore _store = null!;
    private RavenDbContext _context = null!;
    private DocumentValidator _validator = null!;

    public async Task InitializeAsync()
    {
        var settings = Options.Create(new RavenSettings
        {
            Urls = [TestSettings.RavenUrl],
            DatabaseName = _testDbName
        });

        _context = new RavenDbContext(settings);
        _store = _context.Store;
        _validator = new DocumentValidator();

        // Tạo database kiểm thử riêng biệt
        await _context.EnsureDatabaseExistsAsync(_testDbName);

        // Chuẩn bị dữ liệu mẫu chuẩn cho các ca kiểm thử validation
        using var session = _context.OpenAsyncSession();
        await session.StoreAsync(new Customer
        {
            Id = "customers/test-01",
            FullName = "Khách Hàng Test 1",
            Phone = "0900000001",
            Address = new CustomerAddress { Street = "10 Lê Duẩn", City = "TP. HCM" }
        });
        await session.StoreAsync(new Customer
        {
            Id = "customers/test-02",
            FullName = "Khách Hàng Test 2",
            Phone = "0900000002"
        });
        await session.StoreAsync(new Service
        {
            Id = "services/test-01",
            Name = "Dịch Vụ Test 1",
            Price = 100000m
        });
        await session.StoreAsync(new Staff
        {
            Id = "staffs/test-01",
            FullName = "Kỹ Thuật Viên Test 1"
        });
        await session.StoreAsync(new Appointment
        {
            Id = "appointments/test-01",
            CustomerId = "customers/test-01",
            ServiceId = "services/test-01",
            AppointmentDate = DateTime.UtcNow.AddDays(1),
            Status = "Confirmed",
            Note = "Lịch hẹn test"
        });
        await session.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            // Xóa database kiểm thử để dọn dẹp môi trường (KHÔNG xóa database CustomerCareDB thật)
            await _store.Maintenance.Server.SendAsync(new DeleteDatabasesOperation(_testDbName, hardDelete: true));
        }
        catch
        {
            // Bỏ qua lỗi xóa nếu database đã được giải phóng
        }
        finally
        {
            _context.Dispose();
        }
    }

    [Fact]
    public async Task ValidateAppointment_Succeeds_WhenCustomerAndServiceExist()
    {
        using var session = _context.OpenAsyncSession();
        var appointment = new Appointment
        {
            CustomerId = "customers/test-01",
            ServiceId = "services/test-01"
        };

        var result = await _validator.ValidateAppointmentAsync(session, appointment);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAppointment_Fails_WhenCustomerDoesNotExist()
    {
        using var session = _context.OpenAsyncSession();
        var appointment = new Appointment
        {
            CustomerId = "customers/non-existent-999",
            ServiceId = "services/test-01"
        };

        var result = await _validator.ValidateAppointmentAsync(session, appointment);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("customers/non-existent-999"));
    }

    [Fact]
    public async Task ValidateAppointment_Fails_WhenServiceDoesNotExist()
    {
        using var session = _context.OpenAsyncSession();
        var appointment = new Appointment
        {
            CustomerId = "customers/test-01",
            ServiceId = "services/non-existent-999"
        };

        var result = await _validator.ValidateAppointmentAsync(session, appointment);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("services/non-existent-999"));
    }

    [Fact]
    public async Task ValidateFeedback_Fails_WhenRatingIsLessThan1()
    {
        using var session = _context.OpenAsyncSession();
        var feedback = new Feedback
        {
            CustomerId = "customers/test-01",
            AppointmentId = "appointments/test-01",
            Rating = 0, // Điểm không hợp lệ (< 1)
            Comment = "Điểm quá thấp"
        };

        var result = await _validator.ValidateFeedbackAsync(session, feedback);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Rating"));
    }

    [Fact]
    public async Task ValidateFeedback_Fails_WhenRatingIsGreaterThan5()
    {
        using var session = _context.OpenAsyncSession();
        var feedback = new Feedback
        {
            CustomerId = "customers/test-01",
            AppointmentId = "appointments/test-01",
            Rating = 6, // Điểm không hợp lệ (> 5)
            Comment = "Điểm vượt trần"
        };

        var result = await _validator.ValidateFeedbackAsync(session, feedback);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Rating"));
    }

    [Fact]
    public async Task ValidateFeedback_Fails_WhenCustomerIdDoesNotMatchAppointment()
    {
        using var session = _context.OpenAsyncSession();
        var feedback = new Feedback
        {
            CustomerId = "customers/test-02", // Không khớp với customers/test-01 của appointment
            AppointmentId = "appointments/test-01",
            Rating = 5,
            Comment = "Đánh giá hộ người khác"
        };

        var result = await _validator.ValidateFeedbackAsync(session, feedback);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Không nhất quán"));
    }

    [Fact]
    public async Task ValidateServiceHistory_Fails_WhenCustomerIdDoesNotMatchAppointment()
    {
        using var session = _context.OpenAsyncSession();
        var history = new ServiceHistory
        {
            AppointmentId = "appointments/test-01",
            CustomerId = "customers/test-02", // Lệch với appointment.CustomerId (test-01)
            ServiceId = "services/test-01",
            StaffId = "staffs/test-01",
            ServiceDate = DateTime.UtcNow
        };

        var result = await _validator.ValidateServiceHistoryAsync(session, history);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("CustomerId"));
    }

    [Fact]
    public async Task ValidateServiceHistory_Fails_WhenServiceIdDoesNotMatchAppointment()
    {
        using var session = _context.OpenAsyncSession();
        var history = new ServiceHistory
        {
            AppointmentId = "appointments/test-01",
            CustomerId = "customers/test-01",
            ServiceId = "services/wrong-service", // Lệch với appointment.ServiceId (services/test-01)
            StaffId = "staffs/test-01",
            ServiceDate = DateTime.UtcNow
        };

        var result = await _validator.ValidateServiceHistoryAsync(session, history);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ServiceId"));
    }

    [Fact]
    public async Task ValidateServiceHistory_Succeeds_WhenAllReferencesAreConsistent()
    {
        using var session = _context.OpenAsyncSession();
        var history = new ServiceHistory
        {
            AppointmentId = "appointments/test-01",
            CustomerId = "customers/test-01",
            ServiceId = "services/test-01",
            StaffId = "staffs/test-01",
            ServiceDate = DateTime.UtcNow,
            Description = "Hoàn tất bảo dưỡng",
            Result = "Tốt"
        };

        var result = await _validator.ValidateServiceHistoryAsync(session, history);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
