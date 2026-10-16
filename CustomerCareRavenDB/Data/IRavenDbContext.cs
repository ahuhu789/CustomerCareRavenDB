using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace CustomerCareRavenDB.Data;

/// <summary>
/// Giao diện Data Layer kết nối RavenDB.
/// Đảm bảo quản lý duy nhất một instance DocumentStore (Singleton)
/// và cung cấp cơ chế mở Async Session riêng biệt cho mỗi tác vụ/request (Unit of Work).
/// </summary>
public interface IRavenDbContext : IDisposable
{
    /// <summary>
    /// Instance DocumentStore gốc (Singleton) của toàn bộ ứng dụng.
    /// </summary>
    IDocumentStore Store { get; }

    /// <summary>
    /// Tên database được cấu hình mặc định (CustomerCareDB).
    /// </summary>
    string DatabaseName { get; }

    /// <summary>
    /// Mở một phiên làm việc bất đồng bộ (IAsyncDocumentSession) trên database mặc định.
    /// Mỗi request hoặc tác vụ cần mở một session riêng và dispose sau khi hoàn tất.
    /// </summary>
    IAsyncDocumentSession OpenAsyncSession();

    /// <summary>
    /// Mở một phiên làm việc bất đồng bộ trên một database chỉ định (ví dụ: database kiểm thử).
    /// </summary>
    IAsyncDocumentSession OpenAsyncSession(string database);

    /// <summary>
    /// Kiểm tra kết nối tới RavenDB server.
    /// </summary>
    Task<bool> PingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Đảm bảo database tồn tại trên server. Chỉ tạo mới nếu chưa có, KHÔNG ghi đè hay xóa dữ liệu cũ.
    /// </summary>
    Task EnsureDatabaseExistsAsync(string? databaseName = null, CancellationToken cancellationToken = default);
}
