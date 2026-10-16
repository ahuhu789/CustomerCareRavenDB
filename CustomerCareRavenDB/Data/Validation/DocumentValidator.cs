using CustomerCareRavenDB.Models;
using Raven.Client.Documents.Session;

namespace CustomerCareRavenDB.Data.Validation;

/// <summary>
/// Triển khai nghiệp vụ kiểm tra toàn vẹn tài liệu RavenDB theo thiết kế.
/// </summary>
public class DocumentValidator : IDocumentValidator
{
    public async Task<ValidationResult> ValidateAppointmentAsync(
        IAsyncDocumentSession session,
        Appointment appointment,
        CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(appointment.CustomerId))
        {
            result.AddError("CustomerId không được để trống.");
        }
        else
        {
            var customer = await session.LoadAsync<Customer>(appointment.CustomerId, cancellationToken);
            if (customer == null)
            {
                result.AddError($"Khách hàng với Id '{appointment.CustomerId}' không tồn tại trong hệ thống.");
            }
        }

        if (string.IsNullOrWhiteSpace(appointment.ServiceId))
        {
            result.AddError("ServiceId không được để trống.");
        }
        else
        {
            var service = await session.LoadAsync<Service>(appointment.ServiceId, cancellationToken);
            if (service == null)
            {
                result.AddError($"Dịch vụ với Id '{appointment.ServiceId}' không tồn tại trong hệ thống.");
            }
        }

        return result;
    }

    public async Task<ValidationResult> ValidateServiceHistoryAsync(
        IAsyncDocumentSession session,
        ServiceHistory history,
        CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(history.AppointmentId))
        {
            result.AddError("AppointmentId không được để trống.");
            return result;
        }

        var appointment = await session.LoadAsync<Appointment>(history.AppointmentId, cancellationToken);
        if (appointment == null)
        {
            result.AddError($"Lịch hẹn với Id '{history.AppointmentId}' không tồn tại trong hệ thống.");
            return result;
        }

        // Kiểm tra ràng buộc nhất quán CustomerId giữa ServiceHistory và Appointment
        if (string.IsNullOrWhiteSpace(history.CustomerId))
        {
            result.AddError("CustomerId trong ServiceHistory không được để trống.");
        }
        else if (history.CustomerId != appointment.CustomerId)
        {
            result.AddError($"Không nhất quán dữ liệu: CustomerId '{history.CustomerId}' trong ServiceHistory không khớp với CustomerId '{appointment.CustomerId}' trong Appointment.");
        }
        else
        {
            var customer = await session.LoadAsync<Customer>(history.CustomerId, cancellationToken);
            if (customer == null)
            {
                result.AddError($"Khách hàng với Id '{history.CustomerId}' không tồn tại.");
            }
        }

        // Kiểm tra ràng buộc nhất quán ServiceId giữa ServiceHistory và Appointment
        if (string.IsNullOrWhiteSpace(history.ServiceId))
        {
            result.AddError("ServiceId trong ServiceHistory không được để trống.");
        }
        else if (history.ServiceId != appointment.ServiceId)
        {
            result.AddError($"Không nhất quán dữ liệu: ServiceId '{history.ServiceId}' trong ServiceHistory không khớp với ServiceId '{appointment.ServiceId}' trong Appointment.");
        }
        else
        {
            var service = await session.LoadAsync<Service>(history.ServiceId, cancellationToken);
            if (service == null)
            {
                result.AddError($"Dịch vụ với Id '{history.ServiceId}' không tồn tại.");
            }
        }

        // Kiểm tra StaffId
        if (string.IsNullOrWhiteSpace(history.StaffId))
        {
            result.AddError("StaffId không được để trống.");
        }
        else
        {
            var staff = await session.LoadAsync<Staff>(history.StaffId, cancellationToken);
            if (staff == null)
            {
                result.AddError($"Nhân viên với Id '{history.StaffId}' không tồn tại trong hệ thống.");
            }
        }

        return result;
    }

    public async Task<ValidationResult> ValidateFeedbackAsync(
        IAsyncDocumentSession session,
        Feedback feedback,
        CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        // 1. Kiểm tra dải điểm Rating từ 1 đến 5 sao
        if (feedback.Rating < 1 || feedback.Rating > 5)
        {
            result.AddError($"Rating không hợp lệ ({feedback.Rating}). Điểm đánh giá phải nằm trong khoảng từ 1 đến 5 sao.");
        }

        // 2. Kiểm tra tham chiếu Appointment
        if (string.IsNullOrWhiteSpace(feedback.AppointmentId))
        {
            result.AddError("AppointmentId không được để trống. Phản hồi phải gắn liền với một lịch hẹn dịch vụ.");
            return result;
        }

        var appointment = await session.LoadAsync<Appointment>(feedback.AppointmentId, cancellationToken);
        if (appointment == null)
        {
            result.AddError($"Lịch hẹn với Id '{feedback.AppointmentId}' không tồn tại trong hệ thống.");
            return result;
        }

        // 3. Kiểm tra tính nhất quán CustomerId giữa Feedback và Appointment
        if (string.IsNullOrWhiteSpace(feedback.CustomerId))
        {
            result.AddError("CustomerId trong Feedback không được để trống.");
        }
        else if (feedback.CustomerId != appointment.CustomerId)
        {
            result.AddError($"Không nhất quán dữ liệu: CustomerId '{feedback.CustomerId}' trong Feedback không khớp với người đặt lịch '{appointment.CustomerId}' trong Appointment.");
        }
        else
        {
            var customer = await session.LoadAsync<Customer>(feedback.CustomerId, cancellationToken);
            if (customer == null)
            {
                result.AddError($"Khách hàng với Id '{feedback.CustomerId}' không tồn tại.");
            }
        }

        return result;
    }
}
