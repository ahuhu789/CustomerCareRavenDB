namespace CustomerCareRavenDB.Models;

/// <summary>
/// Root Document Collection: Customers.
/// Đại diện cho hồ sơ khách hàng tại trung tâm dịch vụ.
/// Document ID format: "customers/{id}" (ví dụ: "customers/001" hoặc auto-id "customers/1-A").
/// </summary>
public class Customer
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Embedded Document: Địa chỉ được nhúng trực tiếp trong Customer document,
    /// không phân tách thành bảng/collection riêng.
    /// </summary>
    public CustomerAddress Address { get; set; } = new();

    public DateTime? Birthday { get; set; }
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian tạo bản ghi (lưu trữ theo định dạng UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Active";
}
