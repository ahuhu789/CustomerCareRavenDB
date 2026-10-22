namespace CustomerCareRavenDB.Models;

/// <summary>
/// Bộ giá trị chuẩn của <see cref="Customer.Gender"/>: lưu "Male" / "Female" / "Other",
/// hiển thị nhãn tiếng Việt "Nam" / "Nữ" / "Khác".
/// Dữ liệu cũ lưu bằng tiếng Việt ("Nam", "Nữ", "Khác") được ánh xạ về giá trị chuẩn khi đọc/sửa,
/// nên không bị mất khi chỉnh sửa khách hàng.
/// </summary>
public static class CustomerGender
{
    public const string Male = "Male";
    public const string Female = "Female";
    public const string Other = "Other";

    public const string NotSetLabel = "Chưa cập nhật";

    /// <summary>Danh sách (giá trị lưu, nhãn hiển thị) theo thứ tự hiển thị trên form.</summary>
    public static IReadOnlyList<(string Value, string Label)> Options { get; } =
    [
        (Male, "Nam"),
        (Female, "Nữ"),
        (Other, "Khác")
    ];

    /// <summary>
    /// Chuẩn hóa giá trị giới tính về Male/Female/Other (không phân biệt hoa thường, chấp nhận
    /// nhãn tiếng Việt có/không dấu). Rỗng -> rỗng. Giá trị không nhận diện được giữ nguyên.
    /// </summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var v = value.Trim();
        return v.ToLowerInvariant() switch
        {
            "male" or "nam" => Male,
            "female" or "nữ" or "nu" => Female,
            "other" or "khác" or "khac" => Other,
            _ => v
        };
    }

    /// <summary>Nhãn tiếng Việt để hiển thị; rỗng hoặc không nhận diện được -> "Chưa cập nhật".</summary>
    public static string ToDisplay(string? value) => Normalize(value) switch
    {
        Male => "Nam",
        Female => "Nữ",
        Other => "Khác",
        _ => NotSetLabel
    };
}
