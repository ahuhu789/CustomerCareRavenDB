using CustomerCareRavenDB.Models;
using Raven.Client.Documents.Session;

namespace CustomerCareRavenDB.Data.Validation;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = [];

    public static ValidationResult Success() => new();

    public static ValidationResult Failed(params string[] errors)
    {
        var result = new ValidationResult();
        result.Errors.AddRange(errors);
        return result;
    }

    public void AddError(string error) => Errors.Add(error);
}

/// <summary>
/// Giao diện kiểm tra ràng buộc toàn vẹn dữ liệu tài liệu (Document Integrity & Business Rules).
/// Phục vụ cho các module của Quốc (Appointments) và Đạt (ServiceHistories, Feedbacks).
/// </summary>
public interface IDocumentValidator
{
    /// <summary>
    /// Kiểm tra tính hợp lệ của Appointment:
    /// - CustomerId phải tồn tại trong collection Customers.
    /// - ServiceId phải tồn tại trong collection Services.
    /// </summary>
    Task<ValidationResult> ValidateAppointmentAsync(
        IAsyncDocumentSession session,
        Appointment appointment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra tính hợp lệ của ServiceHistory:
    /// - AppointmentId phải tồn tại.
    /// - CustomerId phải khớp với Appointment.CustomerId.
    /// - ServiceId phải khớp với Appointment.ServiceId.
    /// - StaffId phải tồn tại trong collection Staffs.
    /// </summary>
    Task<ValidationResult> ValidateServiceHistoryAsync(
        IAsyncDocumentSession session,
        ServiceHistory history,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra tính hợp lệ của Feedback:
    /// - Rating phải từ 1 đến 5 (1 <= Rating <= 5).
    /// - AppointmentId phải tồn tại.
    /// - CustomerId phải khớp với Appointment.CustomerId.
    /// </summary>
    Task<ValidationResult> ValidateFeedbackAsync(
        IAsyncDocumentSession session,
        Feedback feedback,
        CancellationToken cancellationToken = default);
}
