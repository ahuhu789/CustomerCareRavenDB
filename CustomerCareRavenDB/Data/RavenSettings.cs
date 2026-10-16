namespace CustomerCareRavenDB.Data;

/// <summary>
/// Cấu hình kết nối tới RavenDB từ appsettings.json ("RavenSettings").
/// </summary>
public class RavenSettings
{
    public const string SectionName = "RavenSettings";

    /// <summary>
    /// Danh sách URL của cụm RavenDB Server (mặc định: http://127.0.0.1:8080).
    /// </summary>
    public string[] Urls { get; set; } = ["http://127.0.0.1:8080"];

    /// <summary>
    /// Tên cơ sở dữ liệu chính (mặc định: CustomerCareDB).
    /// </summary>
    public string DatabaseName { get; set; } = "CustomerCareDB";

    /// <summary>
    /// Đường dẫn chứng chỉ X.509 (.pfx) nếu RavenDB bật HTTPS/Authentication (tùy chọn).
    /// </summary>
    public string? CertificatePath { get; set; }

    /// <summary>
    /// Mật khẩu chứng chỉ (nếu có).
    /// </summary>
    public string? CertificatePassword { get; set; }
}
