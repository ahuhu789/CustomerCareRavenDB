# HƯỚNG DẪN TÍCH HỢP VÀ PHÁT TRIỂN MODULE DÀNH CHO QUỐC VÀ ĐẠT

Tài liệu này được biên soạn bởi **Thanh (Database Architect & Data Layer)** nhằm hướng dẫn chi tiết cách tích hợp, khai thác Data Layer và phát triển các module nghiệp vụ tiếp theo mà không gây xung đột mã nguồn.

---

## 1. Phân Công Trách Nhiệm Chi Tiết

| Thành viên | Trách nhiệm phát triển | Thư mục/File phụ trách chính |
|---|---|---|
| **Thanh** | Hạ tầng RavenDB, Cấu hình Store, Models, Seeder, Indexes, Validation, Unit/Integration Tests & Docs | `CustomerCareRavenDB/Data/*`, `CustomerCareRavenDB/Models/*`, `CustomerCareRavenDB/Indexes/*`, `Docs/*` |
| **Quốc** | **Module A:** Nghiệp vụ Quản lý Khách hàng (`Customers`) & Lịch hẹn (`Appointments`) | `CustomerCareRavenDB/Controllers/CustomersController.cs`, `CustomerCareRavenDB/Controllers/AppointmentsController.cs`, `Views/Customers/*`, `Views/Appointments/*` |
| **Đạt** | **Module B:** Nghiệp vụ Quản lý Gói dịch vụ (`Services`), Lịch sử chăm sóc (`ServiceHistories`) & Đánh giá (`Feedbacks`) | `CustomerCareRavenDB/Controllers/ServicesController.cs`, `CustomerCareRavenDB/Controllers/ServiceHistoriesController.cs`, `CustomerCareRavenDB/Controllers/FeedbacksController.cs`, `Views/Services/*`, `Views/ServiceHistories/*`, `Views/Feedbacks/*` |

---

## 2. Chuẩn Bị Môi Trường Phát Triển

### Phần mềm yêu cầu:
1. **.NET SDK:** Phiên bản .NET 10.0 (hoặc SDK tương thích).
2. **RavenDB Server:** Phiên bản 7.x (khuyến nghị chạy tại cổng mặc định `http://127.0.0.1:8080`).
3. **Git & Visual Studio 2022 / VS Code / JetBrains Rider**.

### Các bước khởi động:
1. **Clone repository:**
   ```bash
   git clone <REPO_URL>
   cd NoSQL_Project
   ```
2. **Khởi động RavenDB Server:**
   Chạy file `Raven.Server.exe` trên máy tính cá nhân. Mở trình duyệt kiểm tra `http://127.0.0.1:8080`.
3. **Kiểm tra cấu hình `appsettings.json`:**
   ```json
   "RavenSettings": {
     "Urls": [ "http://127.0.0.1:8080" ],
     "DatabaseName": "CustomerCareDB"
   }
   ```
4. **Khôi phục gói thư viện và Build dự án:**
   ```bash
   dotnet restore
   dotnet build
   ```
5. **Chạy ứng dụng Web:**
   ```bash
   dotnet run --project CustomerCareRavenDB
   ```
   Ứng dụng sẽ tự động kiểm tra database, tạo database `CustomerCareDB` nếu chưa có, đăng ký Static Index và nạp dữ liệu mẫu ban đầu.

---

## 3. Cách Sử Dụng Data Layer Trong Controller

Data Layer được quản lý thông qua interface `IRavenDbContext` (đăng ký dạng **Singleton** trong Dependency Injection).

### 3.1. Tiêm phụ thuộc (Dependency Injection)
```csharp
using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using Microsoft.AspNetCore.Mvc;

public class MyBusinessController : Controller
{
    private readonly IRavenDbContext _ravenContext;
    private readonly IDocumentValidator _validator;

    public MyBusinessController(IRavenDbContext ravenContext, IDocumentValidator validator)
    {
        _ravenContext = ravenContext;
        _validator = validator;
    }
}
```

### 3.2. Mở Asynchronous Session (Unit of Work)
Mỗi request chỉ nên mở **một Async Session riêng** và giải phóng bằng `using`:
```csharp
using var session = _ravenContext.OpenAsyncSession();
```

### 3.3. Đọc dữ liệu theo ID (Load by ID)
```csharp
// Nạp một khách hàng theo ID
var customer = await session.LoadAsync<Customer>("customers/001");
if (customer == null)
{
    return NotFound("Không tìm thấy khách hàng.");
}
```

### 3.4. Thêm mới hoặc Cập nhật tài liệu (Store & SaveChanges)
```csharp
var newCustomer = new Customer
{
    FullName = "Trần Văn Bình",
    Phone = "0912345678",
    Email = "binh@example.com",
    Address = new CustomerAddress
    {
        Street = "45 Lê Lợi",
        District = "Quận 1",
        City = "TP. Hồ Chí Minh"
    },
    CreatedAt = DateTime.UtcNow
};

// Store đưa đối tượng vào tracking session của RavenDB
await session.StoreAsync(newCustomer);

// SaveChanges lưu tất cả thay đổi trong session vào RavenDB theo 1 Transaction nguyên tử
await session.SaveChangesAsync();
```

### 3.5. Truy vấn danh sách tài liệu với LINQ
```csharp
// Lấy danh sách khách hàng kèm phân trang
var customers = await session.Query<Customer>()
    .Where(c => c.Status == "Active")
    .OrderBy(c => c.FullName)
    .Take(20)
    .ToListAsync();
```

### 3.6. Sử dụng IDocumentValidator để kiểm tra ràng buộc trước khi lưu
```csharp
// Kiểm tra lịch hẹn trước khi lưu
var validation = await _validator.ValidateAppointmentAsync(session, appointment);
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        ModelState.AddModelError(string.Empty, error);
    }
    return View(appointment);
}

await session.StoreAsync(appointment);
await session.SaveChangesAsync();
```

---

## 4. Code Mẫu Sẵn Sàng Copy-Paste Cho Quốc

### `CustomersController.cs` (Quản lý Khách hàng - Quốc)
```csharp
using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Models;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;

namespace CustomerCareRavenDB.Controllers;

public class CustomersController : Controller
{
    private readonly IRavenDbContext _context;

    public CustomersController(IRavenDbContext context)
    {
        _context = context;
    }

    // GET: /Customers
    public async Task<IActionResult> Index()
    {
        using var session = _context.OpenAsyncSession();
        var list = await session.Query<Customer>()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return View(list);
    }

    // GET: /Customers/Details/customers%2F001
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        using var session = _context.OpenAsyncSession();
        var customer = await session.LoadAsync<Customer>(id);
        if (customer == null) return NotFound();

        return View(customer);
    }

    // POST: /Customers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        if (!ModelState.IsValid) return View(customer);

        customer.CreatedAt = DateTime.UtcNow;
        using var session = _context.OpenAsyncSession();
        await session.StoreAsync(customer);
        await session.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
```

### `AppointmentsController.cs` (Quản lý Lịch hẹn - Quốc)
```csharp
using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Models;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;

namespace CustomerCareRavenDB.Controllers;

public class AppointmentsController : Controller
{
    private readonly IRavenDbContext _context;
    private readonly IDocumentValidator _validator;

    public AppointmentsController(IRavenDbContext context, IDocumentValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    // GET: /Appointments
    public async Task<IActionResult> Index()
    {
        using var session = _context.OpenAsyncSession();
        var list = await session.Query<Appointment>()
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
        return View(list);
    }

    // POST: /Appointments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Appointment appointment)
    {
        using var session = _context.OpenAsyncSession();
        
        // 1. Bắt buộc kiểm tra tính hợp lệ của CustomerId và ServiceId
        var validation = await _validator.ValidateAppointmentAsync(session, appointment);
        if (!validation.IsValid)
        {
            foreach (var err in validation.Errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            return View(appointment);
        }

        appointment.CreatedAt = DateTime.UtcNow;
        await session.StoreAsync(appointment);
        await session.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
```

---

## 5. Code Mẫu Sẵn Sàng Copy-Paste Cho Đạt

### `ServiceHistoriesController.cs` (Quản lý Lịch sử chăm sóc - Đạt)
```csharp
using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Indexes;
using CustomerCareRavenDB.Models;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;

namespace CustomerCareRavenDB.Controllers;

public class ServiceHistoriesController : Controller
{
    private readonly IRavenDbContext _context;
    private readonly IDocumentValidator _validator;

    public ServiceHistoriesController(IRavenDbContext context, IDocumentValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    // GET: /ServiceHistories
    // Sử dụng Static Index ServiceHistories_ByCustomerAndDate để truy vấn
    public async Task<IActionResult> Index(string? customerId)
    {
        using var session = _context.OpenAsyncSession();
        var query = session.Query<ServiceHistory, ServiceHistories_ByCustomerAndDate>();

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(h => h.CustomerId == customerId);
        }

        var list = await query.OrderByDescending(h => h.ServiceDate).ToListAsync();
        return View(list);
    }

    // POST: /ServiceHistories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceHistory history)
    {
        using var session = _context.OpenAsyncSession();

        // Kiểm tra ràng buộc nhất quán: CustomerId, ServiceId phải khớp Appointment
        var validation = await _validator.ValidateServiceHistoryAsync(session, history);
        if (!validation.IsValid)
        {
            foreach (var err in validation.Errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            return View(history);
        }

        history.CreatedAt = DateTime.UtcNow;
        await session.StoreAsync(history);
        await session.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
```

### `FeedbacksController.cs` (Quản lý Phản hồi & Đánh giá - Đạt)
```csharp
using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Data.Validation;
using CustomerCareRavenDB.Models;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;

namespace CustomerCareRavenDB.Controllers;

public class FeedbacksController : Controller
{
    private readonly IRavenDbContext _context;
    private readonly IDocumentValidator _validator;

    public FeedbacksController(IRavenDbContext context, IDocumentValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    // POST: /Feedbacks/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Feedback feedback)
    {
        using var session = _context.OpenAsyncSession();

        // Kiểm tra Rating (1..5 sao), AppointmentId và CustomerId
        var validation = await _validator.ValidateFeedbackAsync(session, feedback);
        if (!validation.IsValid)
        {
            foreach (var err in validation.Errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            return View(feedback);
        }

        feedback.CreatedAt = DateTime.UtcNow;
        await session.StoreAsync(feedback);
        await session.SaveChangesAsync();

        return RedirectToAction("Index", "Home");
    }
}
```

---

## 6. Quy Tắc Phối Hợp Git Để Tránh Xung Đột (Conflict Prevention)

1. **Không sửa đổi trực tiếp trên branch `main`:**
   - Mỗi thành viên tạo feature branch riêng:
     - Quốc: `feature/quoc-customers-appointments`
     - Đạt: `feature/dat-services-histories-feedbacks`
2. **Không sửa đổi cấu trúc trong thư mục `Data/` và `Models/` đã chốt:**
   - Các Model và Data Layer đã được nghiệm thu 100% bằng Unit & Integration Test.
   - Nếu cần bổ sung trường dữ liệu mới, hãy trao đổi trực tiếp với Thanh để cập nhật migration/seeder thống nhất.
3. **Kéo code mới nhất từ `main` trước khi bắt đầu code:**
   ```bash
   git checkout main
   git pull origin main
   git checkout -b <ten-branch-cua-ban>
   ```
4. **Tạo Pull Request trên GitHub để review trước khi merge vào `main`**.
