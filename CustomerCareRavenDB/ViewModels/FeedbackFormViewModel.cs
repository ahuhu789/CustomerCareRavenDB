using System.ComponentModel.DataAnnotations;

namespace CustomerCareRavenDB.ViewModels;

public class FeedbackFormViewModel
{
    [Required(ErrorMessage = "Hãy chọn lịch hẹn.")]
    public string AppointmentId { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao.")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Hãy nhập nhận xét.")]
    [StringLength(2000)]
    public string Comment { get; set; } = string.Empty;
}
