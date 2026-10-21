using System.Security.Cryptography;
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

        // Nạp chứng chỉ client trước khi tạo DocumentStore để lỗi cấu hình chứng chỉ được báo rõ ràng.
        var certificate = LoadClientCertificate(config);

        try
        {
            var store = new DocumentStore
            {
                Urls = config.Urls,
                Database = config.DatabaseName
            };

            // Cấu hình chứng chỉ nếu có
            if (certificate != null)
            {
                store.Certificate = certificate;
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

    /// <summary>
    /// Cờ nạp khóa riêng của chứng chỉ PKCS#12, chọn theo hệ điều hành:
    /// - Linux/container: EphemeralKeySet giữ khóa trong bộ nhớ, không ghi ra đĩa
    ///   (user non-root trong container không cần thư mục key store); thêm Exportable để
    ///   RavenDB client có thể dùng lại chứng chỉ cho kết nối TCP.
    /// - Windows: SChannel không dùng được khóa ephemeral cho TLS client auth nên dùng UserKeySet.
    /// - macOS: không hỗ trợ EphemeralKeySet nên dùng mặc định.
    /// </summary>
    internal static X509KeyStorageFlags KeyStorageFlags
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable;
            if (OperatingSystem.IsMacOS())
                return X509KeyStorageFlags.Exportable;
            return X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable;
        }
    }

    /// <summary>
    /// Nạp chứng chỉ client từ cấu hình. Thứ tự ưu tiên: CertificateBase64, rồi CertificatePath.
    /// Trả về null nếu không cấu hình chứng chỉ (RavenDB chạy chế độ không bảo mật).
    /// </summary>
    public static X509Certificate2? LoadClientCertificate(RavenSettings config)
    {
        var password = string.IsNullOrEmpty(config.CertificatePassword) ? null : config.CertificatePassword;
        X509Certificate2 certificate;

        if (!string.IsNullOrWhiteSpace(config.CertificateBase64))
        {
            byte[] bytes;
            try
            {
                // Bỏ khoảng trắng/xuống dòng thường gặp khi dán Base64 vào biến môi trường.
                var base64 = string.Concat(config.CertificateBase64.Where(c => !char.IsWhiteSpace(c)));
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "RavenSettings:CertificateBase64 không phải chuỗi Base64 hợp lệ. " +
                    "Tạo bằng lệnh: base64 -w0 client.pfx (Linux) hoặc [Convert]::ToBase64String([IO.File]::ReadAllBytes('client.pfx')) (PowerShell).",
                    ex);
            }

            try
            {
                certificate = X509CertificateLoader.LoadPkcs12(bytes, password, KeyStorageFlags);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException(
                    "Không đọc được chứng chỉ từ RavenSettings:CertificateBase64 (sai mật khẩu RavenSettings:CertificatePassword hoặc không phải file .pfx).",
                    ex);
            }
        }
        else if (!string.IsNullOrWhiteSpace(config.CertificatePath))
        {
            // Nếu đã cấu hình CertificatePath thì file bắt buộc phải tồn tại,
            // tránh trường hợp lặng lẽ kết nối không có chứng chỉ rồi bị RavenDB từ chối.
            if (!File.Exists(config.CertificatePath))
            {
                throw new FileNotFoundException(
                    $"Không tìm thấy file chứng chỉ RavenDB tại '{config.CertificatePath}' (cấu hình RavenSettings:CertificatePath). " +
                    "Kiểm tra lại đường dẫn hoặc bỏ trống CertificatePath nếu RavenDB không yêu cầu chứng chỉ.",
                    config.CertificatePath);
            }

            // Chứng chỉ client cần khóa riêng nên luôn nạp dạng PKCS#12 (.pfx), kể cả khi không có mật khẩu.
            certificate = X509CertificateLoader.LoadPkcs12FromFile(config.CertificatePath, password, KeyStorageFlags);
        }
        else
        {
            return null;
        }

        if (!certificate.HasPrivateKey)
        {
            certificate.Dispose();
            throw new InvalidOperationException(
                "Chứng chỉ RavenDB đã cấu hình không chứa khóa riêng (private key); cần file .pfx của client certificate.");
        }

        return certificate;
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
            // Chứng chỉ quyền User (không phải Operator) không được liệt kê/tạo database ở cấp server.
            // Nếu database đã tồn tại và truy cập được thì vẫn tiếp tục.
            if (await DatabaseIsAccessibleAsync(targetDb, cancellationToken))
            {
                _logger?.LogWarning(
                    "Không kiểm tra/tạo được database '{Database}' ở cấp server ({Message}), " +
                    "nhưng database đã tồn tại và truy cập được — tiếp tục khởi động.",
                    targetDb, ex.Message);
                return;
            }

            _logger?.LogError(ex, "Không thể tạo database '{Database}': {Message}", targetDb, ex.Message);
            throw new InvalidOperationException($"Lỗi khi kiểm tra hoặc tạo database '{targetDb}': {ex.Message}", ex);
        }
    }

    private async Task<bool> DatabaseIsAccessibleAsync(string database, CancellationToken cancellationToken)
    {
        try
        {
            await _store.Maintenance.ForDatabase(database)
                .SendAsync(new GetStatisticsOperation(), cancellationToken);
            return true;
        }
        catch
        {
            return false;
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
