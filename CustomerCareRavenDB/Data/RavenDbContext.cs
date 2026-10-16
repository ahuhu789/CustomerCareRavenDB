using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace CustomerCareRavenDB.Data;

/// <summary>
/// Triển khai Data Layer RavenDbContext.
/// Quản lý vòng đời Singleton của IDocumentStore và xử lý các thao tác hạ tầng cơ sở dữ liệu.
/// </summary>
public class RavenDbContext : IRavenDbContext
{
    private readonly IDocumentStore _store;
    private readonly ILogger<RavenDbContext>? _logger;
    private readonly string _defaultDatabase;
    private bool _disposed;

    public IDocumentStore Store => _store;
    public string DatabaseName => _defaultDatabase;

    public RavenDbContext(IOptions<RavenSettings> settings, ILogger<RavenDbContext>? logger = null)
    {
        _logger = logger;
        var config = settings.Value;

        if (config.Urls == null || config.Urls.Length == 0)
        {
            throw new ArgumentException("RavenSettings:Urls phải chứa ít nhất một địa chỉ URL hợp lệ.", nameof(settings));
        }

        if (string.IsNullOrWhiteSpace(config.DatabaseName))
        {
            throw new ArgumentException("RavenSettings:DatabaseName không được để trống.", nameof(settings));
        }

        _defaultDatabase = config.DatabaseName;

        try
        {
            var store = new DocumentStore
            {
                Urls = config.Urls,
                Database = config.DatabaseName
            };

            // Cấu hình chứng chỉ nếu có
            if (!string.IsNullOrWhiteSpace(config.CertificatePath) && File.Exists(config.CertificatePath))
            {
                store.Certificate = string.IsNullOrWhiteSpace(config.CertificatePassword)
                    ? X509CertificateLoader.LoadCertificateFromFile(config.CertificatePath)
                    : X509CertificateLoader.LoadPkcs12FromFile(config.CertificatePath, config.CertificatePassword);
            }

            // Đặt các quy ước tên Collection chuẩn hóa
            store.Conventions.FindCollectionName = type =>
            {
                // Giữ nguyên chuẩn tên Collection theo yêu cầu đề bài:
                // Customers, Staffs, Services, Appointments, ServiceHistories, Feedbacks
                if (type.Name == "Staff") return "Staffs";
                if (type.Name == "Customer") return "Customers";
                if (type.Name == "Service") return "Services";
                if (type.Name == "Appointment") return "Appointments";
                if (type.Name == "ServiceHistory") return "ServiceHistories";
                if (type.Name == "Feedback") return "Feedbacks";
                return DocumentConventions.DefaultGetCollectionName(type);
            };

            store.Initialize();
            _store = store;

            _logger?.LogInformation(
                "RavenDB DocumentStore đã khởi tạo thành công. Urls: {Urls}, Database: {Database}",
                string.Join(", ", config.Urls), config.DatabaseName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi khi khởi tạo RavenDB DocumentStore với URLs: {Urls}", string.Join(", ", config.Urls));
            throw new InvalidOperationException($"Không thể kết nối hoặc khởi tạo RavenDB DocumentStore tại {string.Join(", ", config.Urls)}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Constructor hỗ trợ truyền trực tiếp DocumentStore (phục vụ unit test / test harness).
    /// </summary>
    public RavenDbContext(IDocumentStore store, string databaseName, ILogger<RavenDbContext>? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _defaultDatabase = databaseName;
        _logger = logger;
    }

    public IAsyncDocumentSession OpenAsyncSession()
    {
        ThrowIfDisposed();
        return _store.OpenAsyncSession(_defaultDatabase);
    }

    public IAsyncDocumentSession OpenAsyncSession(string database)
    {
        ThrowIfDisposed();
        return _store.OpenAsyncSession(database);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try
        {
            var buildNumber = await _store.Maintenance.Server.SendAsync(new GetBuildNumberOperation(), cancellationToken);
            return buildNumber != null && !string.IsNullOrEmpty(buildNumber.ProductVersion);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Ping RavenDB thất bại: {Message}", ex.Message);
            return false;
        }
    }

    public async Task EnsureDatabaseExistsAsync(string? databaseName = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var targetDb = string.IsNullOrWhiteSpace(databaseName) ? _defaultDatabase : databaseName;

        try
        {
            // Kiểm tra xem database đã tồn tại hay chưa
            var databaseNames = await _store.Maintenance.Server.SendAsync(
                new GetDatabaseNamesOperation(0, 1024), cancellationToken);

            if (databaseNames.Contains(targetDb, StringComparer.OrdinalIgnoreCase))
            {
                _logger?.LogInformation("Database '{Database}' đã tồn tại trên RavenDB Server.", targetDb);
                return;
            }

            _logger?.LogInformation("Database '{Database}' chưa tồn tại. Đang tiến hành tạo mới...", targetDb);
            var createOp = new CreateDatabaseOperation(new DatabaseRecord(targetDb));
            await _store.Maintenance.Server.SendAsync(createOp, cancellationToken);
            _logger?.LogInformation("Đã tạo mới thành công Database '{Database}'.", targetDb);
        }
        catch (ConcurrencyException)
        {
            // Trường hợp nhiều node/thread cùng tạo cùng lúc, bỏ qua nếu đã tồn tại
            _logger?.LogInformation("Database '{Database}' đã được tạo đồng thời bởi tiến trình khác.", targetDb);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Không thể tạo database '{Database}': {Message}", targetDb, ex.Message);
            throw new InvalidOperationException($"Lỗi khi kiểm tra hoặc tạo database '{targetDb}': {ex.Message}", ex);
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _store.Dispose();
        GC.SuppressFinalize(this);
    }
}
