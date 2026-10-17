using System.ComponentModel.DataAnnotations;

namespace CustomerCareRavenDB.ViewModels;

public class ServiceHistoryFormViewModel
{
    [Required(ErrorMessage = "Hãy chọn lịch hẹn.")]
    public string AppointmentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hãy chọn nhân viên.")]
    public string StaffId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hãy nhập ngày thực hiện.")]
    public DateTime ServiceDate { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "Hãy nhập nội dung thực hiện.")]
    [StringLength(3000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(3000)]
    public string Result { get; set; } = string.Empty;

    [StringLength(3000)]
    public string Note { get; set; } = string.Empty;
}
