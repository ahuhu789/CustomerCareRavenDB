using CustomerCareRavenDB.Indexes;
using CustomerCareRavenDB.Models;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace CustomerCareRavenDB.Data;

/// <summary>
/// Truy vấn lọc lịch sử chăm sóc cho trang /ServiceHistories, chạy trên static index
/// <see cref="ServiceHistories_ByCustomerAndDate"/> (CustomerId, ServiceDate, StaffId, ServiceId).
/// </summary>
public static class ServiceHistoryQueries
{
    /// <param name="from">Ngày bắt đầu (UTC, tính từ 00:00).</param>
    /// <param name="to">Ngày kết thúc (UTC, bao gồm cả ngày này).</param>
    public static IRavenQueryable<ServiceHistory> Filter(
        IAsyncDocumentSession session,
        string? customerId, string? staffId, string? serviceId,
        DateTime? from, DateTime? to)
    {
        var query = session.Query<ServiceHistory, ServiceHistories_ByCustomerAndDate>();

        if (!string.IsNullOrWhiteSpace(customerId)) query = query.Where(h => h.CustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(staffId)) query = query.Where(h => h.StaffId == staffId);
        if (!string.IsNullOrWhiteSpace(serviceId)) query = query.Where(h => h.ServiceId == serviceId);
        if (from.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);
            query = query.Where(h => h.ServiceDate >= startUtc);
        }
        if (to.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(h => h.ServiceDate < endUtc);
        }

        return query.OrderByDescending(h => h.ServiceDate);
    }
}
