using System.ComponentModel.DataAnnotations;

namespace CustomerCareRavenDB.ViewModels;

public class ServiceFormViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ.")]
    [StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Giá phải từ 0 trở lên.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập loại dịch vụ.")]
    [StringLength(120)]
    public string Category { get; set; } = string.Empty;
}
