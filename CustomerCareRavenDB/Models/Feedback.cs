namespace CustomerCareRavenDB.Models;

/// <summary>
/// Root Document Collection: Feedbacks.
/// Đại diện cho đánh giá và phản hồi của khách hàng sau khi trải nghiệm dịch vụ.
/// Document ID format: "feedbacks/{id}" (ví dụ: "feedbacks/001").
/// 
/// LƯU Ý LIÊN KẾT VÀ TÍNH TOÀN VẸN:
/// - CustomerId    -> Customers.Id    ("customers/001")
/// - AppointmentId -> Appointments.Id ("appointments/001")
/// QUY TẮC BẮT BUỘC TỪ THIẾT KẾ CSDL:
/// - Feedback tham chiếu trực tiếp đến Appointment thông qua 'AppointmentId' (KHÔNG thay bằng ServiceHistoryId).
/// - Feedback.CustomerId PHẢI trùng khớp với Appointment.CustomerId.
/// - Rating PHẢI nằm trong khoảng từ 1 đến 5 (1 <= Rating <= 5).
/// </summary>
public class Feedback
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID tham chiếu tới Customer đánh giá.
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// ID tham chiếu tới Appointment được đánh giá (LƯU Ý: Không dùng ServiceHistoryId).
    /// </summary>
    public string AppointmentId { get; set; } = string.Empty;

    /// <summary>
    /// Điểm đánh giá mức độ hài lòng từ 1 đến 5 sao.
    /// </summary>
    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian gửi phản hồi (lưu trữ UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
