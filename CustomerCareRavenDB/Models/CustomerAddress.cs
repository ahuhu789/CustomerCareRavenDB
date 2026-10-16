namespace CustomerCareRavenDB.Models;

/// <summary>
/// Embedded Document: CustomerAddress.
/// LƯU Ý KIẾN TRÚC:
/// Đây là một Embedded Document (tài liệu nhúng), KHÔNG PHẢI một Collection riêng biệt trong RavenDB.
/// Đối tượng CustomerAddress được lưu trực tiếp bên trong document Customer dưới dạng một JSON object con.
/// Lợi ích trong mô hình NoSQL/RavenDB:
/// - Đảm bảo tính nguyên tử (Atomicity): Dữ liệu địa chỉ được cập nhật cùng lúc với Customer trong 1 transaction.
/// - Tối ưu hiệu năng đọc: Không cần phép JOIN hay round-trip mạng bổ sung để lấy địa chỉ của khách hàng.
/// - Không tốn tài nguyên quản lý Collection độc lập khi địa chỉ luôn thuộc về 1 khách hàng cụ thể.
/// </summary>
public class CustomerAddress
{
    public string Street { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public override string ToString()
    {
        var parts = new[] { Street, Ward, District, City }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }
}
