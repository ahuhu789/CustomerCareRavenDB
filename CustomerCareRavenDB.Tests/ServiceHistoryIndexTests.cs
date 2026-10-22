using CustomerCareRavenDB.Controllers;
using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Indexes;
using CustomerCareRavenDB.Models;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.Documents.Session;

namespace CustomerCareRavenDB.Tests;

/// <summary>
/// Trang /ServiceHistories lọc qua static index ServiceHistories/ByCustomerAndDate
/// và trả về đúng kết quả như truy vấn collection trước đây.
/// </summary>
public class ServiceHistoryIndexTests : TestDatabase
{
    private const string IndexName = "ServiceHistories/ByCustomerAndDate";

    public override async Task InitializeAsyncExtra()
    {
        // Thêm 1 lịch sử thứ hai (khách 002 / nhân viên 002 / dịch vụ 002, 10/03/2026) để các bộ lọc có ý nghĩa.
        using var session = Context.OpenAsyncSession();
        await session.StoreAsync(new ServiceHistory
        {
            Id = "servicehistories/002",
            AppointmentId = "appointments/002",
            CustomerId = "customers/002",
            ServiceId = "services/002",
            StaffId = "staffs/002",
            ServiceDate = new DateTime(2026, 3, 10, 23, 30, 0, DateTimeKind.Utc),
            Description = "Thay IC nguồn",
            CreatedAt = new DateTime(2026, 3, 10, 23, 45, 0, DateTimeKind.Utc)
        });
        await session.SaveChangesAsync();
    }

    public static TheoryData<string?, string?, string?, string?, string?, string[]> Cases => new()
    {
        { null, null, null, null, null, ["servicehistories/002", "servicehistories/001"] },
        { "customers/001", null, null, null, null, ["servicehistories/001"] },
        { "customers/002", null, null, null, null, ["servicehistories/002"] },
        { "customers/999", null, null, null, null, [] },
        { null, "staffs/002", null, null, null, ["servicehistories/002"] },
        { null, null, "services/001", null, null, ["servicehistories/001"] },
        { null, null, null, "2026-02-01", "2026-02-01", ["servicehistories/001"] },
        { null, null, null, "2026-03-10", "2026-03-10", ["servicehistories/002"] }, // 23:30 UTC vẫn thuộc ngày 10
        { null, null, null, "2026-02-02", null, ["servicehistories/002"] },
        { null, null, null, null, "2026-01-31", [] },
        { null, null, null, "2026-03-01", "2026-01-01", [] },
        { "customers/001", null, null, "2026-02-01", "2026-02-28", ["servicehistories/001"] },
        { "customers/002", null, null, "2026-02-01", "2026-02-28", [] },
        { "customers/001", "staffs/001", "services/001", "2026-02-01", "2026-02-01", ["servicehistories/001"] },
        { "customers/001", "staffs/002", null, null, null, [] },
    };

    private static DateTime? D(string? s) => s == null ? null : DateTime.Parse(s);

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Filter_UsesStaticIndex_AndMatchesExpectedRows(
        string? customerId, string? staffId, string? serviceId, string? from, string? to, string[] expected)
    {
        using var session = Context.OpenAsyncSession();
        var results = await ServiceHistoryQueries.Filter(session, customerId, staffId, serviceId, D(from), D(to))
            .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(10)))
            .Statistics(out QueryStatistics stats)
            .ToListAsync();

        Assert.Equal(IndexName, stats.IndexName);
        Assert.Equal(expected, results.Select(h => h.Id).ToArray());
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Filter_MatchesPreviousCollectionQuery(
        string? customerId, string? staffId, string? serviceId, string? from, string? to, string[] _)
    {
        using var session = Context.OpenAsyncSession();
        var viaIndex = await ServiceHistoryQueries.Filter(session, customerId, staffId, serviceId, D(from), D(to))
            .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(10)))
            .ToListAsync();

        // Cách lọc cũ: trên toàn bộ collection (tính ở bộ nhớ, cùng điều kiện).
        var all = await session.Query<ServiceHistory>()
            .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(10))).ToListAsync();
        var start = D(from)?.Date; var end = D(to)?.Date.AddDays(1);
        var expected = all
            .Where(h => customerId == null || h.CustomerId == customerId)
            .Where(h => staffId == null || h.StaffId == staffId)
            .Where(h => serviceId == null || h.ServiceId == serviceId)
            .Where(h => start == null || h.ServiceDate >= start)
            .Where(h => end == null || h.ServiceDate < end)
            .OrderByDescending(h => h.ServiceDate)
            .Select(h => h.Id);

        Assert.Equal(expected, viaIndex.Select(h => h.Id));
    }

    [Fact]
    public async Task IndexDefinition_ContainsFilterFields()
    {
        var definition = await Context.Store.Maintenance.SendAsync(new GetIndexOperation(IndexName));
        var map = Assert.Single(definition.Maps);
        foreach (var field in new[] { "CustomerId", "ServiceDate", "StaffId", "ServiceId" })
            Assert.Contains(field, map);
    }

    [Fact]
    public async Task IndexPage_ReturnsFilteredModel()
    {
        await IndexWaiter.WaitAsync(Context.Store, DbName);
        var controller = new ServiceHistoriesController(Context, Validator);

        var view = Assert.IsType<ViewResult>(await controller.Index(null, "staffs/001", null, null, null));
        Assert.Equal(["servicehistories/001"], Assert.IsAssignableFrom<IEnumerable<ServiceHistory>>(view.Model).Select(h => h.Id));

        view = Assert.IsType<ViewResult>(await controller.Index("customers/002", null, null, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)));
        Assert.Equal(["servicehistories/002"], Assert.IsAssignableFrom<IEnumerable<ServiceHistory>>(view.Model).Select(h => h.Id));
    }
}

internal static class IndexWaiter
{
    public static async Task WaitAsync(IDocumentStore store, string database)
    {
        for (var i = 0; i < 100; i++)
        {
            var stats = await store.Maintenance.ForDatabase(database).SendAsync(new GetIndexesStatisticsOperation());
            if (stats.All(s => !s.IsStale)) return;
            await Task.Delay(100);
        }
    }
}
