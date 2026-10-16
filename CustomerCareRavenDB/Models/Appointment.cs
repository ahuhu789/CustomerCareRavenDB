namespace CustomerCareRavenDB.Models;

/// <summary>
/// Root Document Collection: Appointments.
/// Đại diện cho lịch hẹn dịch vụ của khách hàng.
/// Document ID format: "appointments/{id}" (ví dụ: "appointments/001").
/// 
/// LƯU Ý LIÊN KẾT (DOCUMENT REFERENCE):
/// Trong RavenDB, liên kết giữa các root collection được lưu dưới dạng chuỗi ID tham chiếu:
/// - CustomerId -> trỏ tới document Customers.Id (ví dụ: "customers/001")
/// - ServiceId  -> trỏ tới document Services.Id  (ví dụ: "services/001")
/// Khi cần truy vấn dữ liệu quan hệ, dùng hàm `load(doc.CustomerId)` trong RQL hoặc `.Include(x => x.CustomerId)` trong C# Linq.
/// </summary>
public class Appointment
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID tham chiếu tới document Customer (ví dụ: "customers/001").
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// ID tham chiếu tới document Service (ví dụ: "services/001").
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Ngày giờ hẹn diễn ra dịch vụ (lưu trữ UTC).
    /// </summary>
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Trạng thái lịch hẹn (Pending, Confirmed, Completed, Cancelled).
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Ghi chú lịch hẹn (LƯU Ý: Trường tên là 'Note', không dùng 'Notes').
    /// </summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian tạo lịch hẹn (lưu trữ UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
