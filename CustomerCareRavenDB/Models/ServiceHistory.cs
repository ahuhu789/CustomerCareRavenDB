namespace CustomerCareRavenDB.Models;

/// <summary>
/// Root Document Collection: ServiceHistories.
/// Đại diện cho lịch sử thực hiện dịch vụ thực tế của trung tâm.
/// Document ID format: "servicehistories/{id}" (ví dụ: "servicehistories/001").
/// 
/// LƯU Ý LIÊN KẾT VÀ TÍNH TOÀN VẸN:
/// - AppointmentId -> Appointments.Id ("appointments/001")
/// - CustomerId    -> Customers.Id    ("customers/001")
/// - ServiceId     -> Services.Id     ("services/001")
/// - StaffId       -> Staffs.Id       ("staffs/001")
/// QUY TẮC RÀNG BUỘC (Consistency):
/// - ServiceHistory.CustomerId PHẢI trùng khớp với Appointment.CustomerId.
/// - ServiceHistory.ServiceId PHẢI trùng khớp với Appointment.ServiceId.
/// TÊN TRƯỜNG BẮT BUỘC:
/// - Dùng 'ServiceDate', KHÔNG dùng 'CompletedAt'.
/// - Dùng 'Note', KHÔNG dùng 'Notes'.
/// </summary>
public class ServiceHistory
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID tham chiếu tới Appointment gốc.
    /// </summary>
    public string AppointmentId { get; set; } = string.Empty;

    /// <summary>
    /// ID tham chiếu tới Customer nhận dịch vụ.
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// ID tham chiếu tới Service được thực hiện.
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// ID tham chiếu tới Staff phụ trách thực hiện dịch vụ.
    /// </summary>
    public string StaffId { get; set; } = string.Empty;

    /// <summary>
    /// Ngày thực hiện dịch vụ thực tế (lưu trữ UTC).
    /// BẮT BUỘC: Trường tên là 'ServiceDate', không dùng 'CompletedAt'.
    /// </summary>
    public DateTime ServiceDate { get; set; }

    public string Description { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Ghi chú khi thực hiện dịch vụ (LƯU Ý: Trường tên là 'Note', không dùng 'Notes').
    /// </summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian tạo bản ghi lịch sử (lưu trữ UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
