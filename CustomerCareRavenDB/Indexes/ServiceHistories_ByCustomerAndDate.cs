using CustomerCareRavenDB.Models;
using Raven.Client.Documents.Indexes;

namespace CustomerCareRavenDB.Indexes;

/// <summary>
/// Static Index: ServiceHistories/ByCustomerAndDate
/// Tối ưu hóa truy vấn tra cứu lịch sử chăm sóc khách hàng theo CustomerId và sắp xếp/lọc theo ServiceDate.
/// 
/// RQL Map definition trong RavenDB Studio:
/// <code>
/// from history in docs.ServiceHistories
/// select new
/// {
///     history.CustomerId,
///     history.ServiceDate
/// }
/// </code>
/// </summary>
public class ServiceHistories_ByCustomerAndDate : AbstractIndexCreationTask<ServiceHistory>
{
    public ServiceHistories_ByCustomerAndDate()
    {
        Map = histories => from history in histories
                           select new
                           {
                               history.CustomerId,
                               history.ServiceDate
                           };
    }
}
