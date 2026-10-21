using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging;

namespace CustomerCareRavenDB.Data;

/// <summary>
/// Document lưu một khóa ASP.NET Core Data Protection (collection "DataProtectionKeys").
/// </summary>
public class DataProtectionKey
{
    public string? Id { get; set; }
    public string FriendlyName { get; set; } = string.Empty;
    public string Xml { get; set; } = string.Empty;
}

/// <summary>
/// Lưu khóa Data Protection (dùng cho anti-forgery token, TempData cookie) vào RavenDB,
/// để khóa không bị mất khi container khởi động lại (filesystem của Render Free là tạm thời).
/// Nếu RavenDB không truy cập được, ứng dụng vẫn chạy với khóa chỉ tồn tại trong tiến trình.
/// Lưu ý: khóa được lưu dạng XML không mã hóa — ai đọc được database thì đọc được khóa.
/// </summary>
public class RavenDbXmlRepository : IXmlRepository
{
    private readonly IRavenDbContext _context;
    private readonly ILogger<RavenDbXmlRepository>? _logger;

    public RavenDbXmlRepository(IRavenDbContext context, ILogger<RavenDbXmlRepository>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        try
        {
            using var session = _context.Store.OpenSession(_context.DatabaseName);
            return session.Query<DataProtectionKey>()
                .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(5)))
                .ToList()
                .Select(k => XElement.Parse(k.Xml))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Không đọc được khóa Data Protection từ RavenDB: {Message}", ex.Message);
            return [];
        }
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        try
        {
            using var session = _context.Store.OpenSession(_context.DatabaseName);
            session.Store(new DataProtectionKey
            {
                Id = $"DataProtectionKeys/{friendlyName}",
                FriendlyName = friendlyName,
                Xml = element.ToString(SaveOptions.DisableFormatting)
            });
            session.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex,
                "Không lưu được khóa Data Protection '{FriendlyName}' vào RavenDB; khóa chỉ tồn tại trong tiến trình hiện tại: {Message}",
                friendlyName, ex.Message);
        }
    }
}
