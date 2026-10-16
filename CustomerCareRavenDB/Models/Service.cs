namespace CustomerCareRavenDB.Models;

/// <summary>
/// Root Document Collection: Services.
/// Đại diện cho các dịch vụ chăm sóc, bảo dưỡng hoặc sửa chữa của trung tâm.
/// Document ID format: "services/{id}" (ví dụ: "services/001").
/// </summary>
public class Service
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian tạo bản ghi (lưu trữ UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
