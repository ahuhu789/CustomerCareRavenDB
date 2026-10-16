using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Models;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;

namespace CustomerCareRavenDB.Data;

public interface IDatabaseSeeder
{
    Task<bool> SeedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Quản lý việc khởi tạo dữ liệu mẫu ban đầu (Seeding) cho CustomerCareDB.
/// Đảm bảo tính Idempotent (chạy nhiều lần không trùng lặp), không ghi đè dữ liệu cũ,
/// và minh họa trọn vẹn luồng nghiệp vụ:
/// Customer -> Appointment -> Service -> ServiceHistory -> Staff -> Feedback.
/// </summary>
public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly IRavenDbContext _context;
    private readonly IDocumentValidator _validator;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IRavenDbContext context,
        IDocumentValidator validator,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _validator = validator;
        _logger = logger;
    }

    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Đảm bảo Database tồn tại (KHÔNG xóa hay drop database hiện có)
            await _context.EnsureDatabaseExistsAsync(cancellationToken: cancellationToken);

            using var session = _context.OpenAsyncSession();

            // 2. Kiểm tra xem dữ liệu mẫu chính đã tồn tại chưa để tránh trùng lặp
            var existingCustomer = await session.LoadAsync<Customer>("customers/001", cancellationToken);
            if (existingCustomer != null)
            {
                _logger.LogInformation("Dữ liệu mẫu 'customers/001' đã tồn tại. Bỏ qua seeding để bảo toàn dữ liệu hiện tại.");
                return false;
            }

            _logger.LogInformation("Bắt đầu nạp kịch bản dữ liệu mẫu chuẩn nghiệp vụ vào RavenDB...");

            // --- 1. Customers ---
            var customer1 = new Customer
            {
                Id = "customers/001",
                FullName = "Nguyễn Văn An",
                Phone = "0901234567",
                Email = "nguyenvanan@example.com",
                Address = new CustomerAddress
                {
                    Street = "123 Nguyễn Huệ",
                    Ward = "Bến Nghé",
                    District = "Quận 1",
                    City = "TP. Hồ Chí Minh"
                },
                Birthday = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Nam",
                CreatedAt = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
                Status = "Active"
            };

            var customer2 = new Customer
            {
                Id = "customers/002",
                FullName = "Lê Thị Bích Ngọc",
                Phone = "0987654321",
                Email = "bichngoc.le@example.com",
                Address = new CustomerAddress
                {
                    Street = "45 Lê Lợi",
                    Ward = "Phường 4",
                    District = "Quận 3",
                    City = "TP. Hồ Chí Minh"
                },
                Birthday = new DateTime(1995, 11, 20, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Nữ",
                CreatedAt = new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc),
                Status = "Active"
            };

            await session.StoreAsync(customer1, cancellationToken);
            await session.StoreAsync(customer2, cancellationToken);

            // --- 2. Staffs ---
            var staff1 = new Staff
            {
                Id = "staffs/001",
                FullName = "Trần Thị Mai",
                Position = "Chuyên viên kỹ thuật cao cấp",
                Phone = "0912345678",
                Email = "tranmai@centercare.vn",
                CreatedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc)
            };

            var staff2 = new Staff
            {
                Id = "staffs/002",
                FullName = "Phạm Quốc Tuấn",
                Position = "Kỹ thuật viên hệ thống",
                Phone = "0933456789",
                Email = "tuantq@centercare.vn",
                CreatedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc)
            };

            await session.StoreAsync(staff1, cancellationToken);
            await session.StoreAsync(staff2, cancellationToken);

            // --- 3. Services ---
            var service1 = new Service
            {
                Id = "services/001",
                Name = "Bảo dưỡng và vệ sinh thiết bị toàn diện",
                Description = "Gói kiểm tra toàn diện, vệ sinh chi tiết linh kiện và tra keo tản nhiệt cao cấp",
                Price = 350000m,
                Category = "Bảo dưỡng định kỳ",
                CreatedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc)
            };

            var service2 = new Service
            {
                Id = "services/002",
                Name = "Chẩn đoán và xử lý sự cố bo mạch",
                Description = "Đo đạc nguồn, xử lý chạm chập IC và thay thế linh kiện điện tử",
                Price = 600000m,
                Category = "Sửa chữa phần cứng",
                CreatedAt = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc)
            };

            await session.StoreAsync(service1, cancellationToken);
            await session.StoreAsync(service2, cancellationToken);

            // --- 4. Appointments ---
            var appointment1 = new Appointment
            {
                Id = "appointments/001",
                CustomerId = "customers/001",
                ServiceId = "services/001",
                AppointmentDate = new DateTime(2026, 2, 1, 9, 30, 0, DateTimeKind.Utc),
                Status = "Completed",
                Note = "Khách hàng yêu cầu kiểm tra kỹ quạt tản nhiệt do phát ra tiếng ồn khi chạy tải nặng",
                CreatedAt = new DateTime(2026, 1, 28, 14, 20, 0, DateTimeKind.Utc)
            };

            var appointment2 = new Appointment
            {
                Id = "appointments/002",
                CustomerId = "customers/002",
                ServiceId = "services/002",
                AppointmentDate = new DateTime(2026, 2, 5, 14, 0, 0, DateTimeKind.Utc),
                Status = "Confirmed",
                Note = "Khách báo máy tính khởi động không lên nguồn",
                CreatedAt = new DateTime(2026, 2, 2, 10, 15, 0, DateTimeKind.Utc)
            };

            await session.StoreAsync(appointment1, cancellationToken);
            await session.StoreAsync(appointment2, cancellationToken);

            // --- 5. ServiceHistories ---
            var history1 = new ServiceHistory
            {
                Id = "servicehistories/001",
                AppointmentId = "appointments/001",
                CustomerId = "customers/001",
                ServiceId = "services/001",
                StaffId = "staffs/001",
                ServiceDate = new DateTime(2026, 2, 1, 10, 45, 0, DateTimeKind.Utc),
                Description = "Thực hiện vệ sinh quạt tản nhiệt, loại bỏ bụi bẩn, bôi trơn trục quạt và thay keo tản nhiệt gốm",
                Result = "Thiết bị hoạt động êm ái, nhiệt độ CPU giảm 15 độ C khi stress test, mọi chức năng ổn định",
                Note = "Đã bàn giao khách hàng test trực tiếp, khách hài lòng",
                CreatedAt = new DateTime(2026, 2, 1, 11, 0, 0, DateTimeKind.Utc)
            };

            await session.StoreAsync(history1, cancellationToken);

            // --- 6. Feedbacks ---
            var feedback1 = new Feedback
            {
                Id = "feedbacks/001",
                CustomerId = "customers/001",
                AppointmentId = "appointments/001",
                Rating = 5,
                Comment = "Dịch vụ rất chu đáo và chuyên nghiệp! Nhân viên Mai tư vấn nhiệt tình, máy chạy mát và êm hẳn.",
                CreatedAt = new DateTime(2026, 2, 1, 15, 30, 0, DateTimeKind.Utc)
            };

            await session.StoreAsync(feedback1, cancellationToken);

            // Lưu toàn bộ dữ liệu mẫu trong 1 transaction an toàn
            await session.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Hoàn tất nạp dữ liệu mẫu vào CustomerCareDB thành công!");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong quá trình Seeding dữ liệu: {Message}", ex.Message);
            throw;
        }
    }
}
