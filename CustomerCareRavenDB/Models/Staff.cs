namespace CustomerCareRavenDB.Models;

/// <summary>
/// Root Document Collection: Staffs.
/// Đại diện cho nhân viên kỹ thuật hoặc chuyên viên chăm sóc khách hàng.
/// Document ID format: "staffs/{id}" (ví dụ: "staffs/001").
/// </summary>
public class Staff
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian tạo bản ghi (lưu trữ UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
