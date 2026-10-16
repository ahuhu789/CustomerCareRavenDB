# Quản Lý Hồ Sơ Khách Hàng và Lịch Sử Chăm Sóc Khách Hàng (RavenDB)

> **Dự án môn học Cơ sở dữ liệu NoSQL**  
> **Đề tài:** Quản lý hồ sơ khách hàng và lịch sử chăm sóc khách hàng của trung tâm dịch vụ bằng RavenDB Studio.

---

## A. Tổng Quan Dự Án (Project Overview)

- **Tên dự án:** CustomerCareRavenDB
- **Mục tiêu:** Xây dựng hệ thống phần mềm quản lý hồ sơ khách hàng, danh mục gói dịch vụ, phân công nhân viên kỹ thuật, theo dõi lịch hẹn, ghi nhận lịch sử chăm sóc/sửa chữa thực tế và tiếp nhận đánh giá phản hồi từ khách hàng.
- **Công nghệ cốt lõi:**
  - **Database Engine:** RavenDB Document Database (NoSQL) v7.2.6 & RavenDB Studio.
  - **Framework & Ngôn ngữ:** .NET 10.0 / C# (ASP.NET Core MVC).
  - **Giao diện:** Razor Views + Bootstrap 5 + Bootstrap Icons.
  - **Data Access:** `RavenDB.Client` v7.2.6 (Singleton `IDocumentStore`, Async Sessions, Unit of Work).
  - **Truy vấn:** Raven Query Language (RQL) & LINQ.
  - **Kiểm thử tự động:** xUnit v2.9.3.
  - **TUYỆT ĐỐI KHÔNG SỬ DỤNG:** Entity Framework Core, SQL Server, SQLite, MongoDB. RavenDB là cơ sở dữ liệu duy nhất của toàn bộ hệ thống.
- **Kiến trúc hệ thống:**
  Hệ thống áp dụng mô hình kiến trúc phân lớp hướng tài liệu (Document-Driven Architecture):
  - **Presentation Layer:** ASP.NET Core MVC Controllers & Bootstrap Views.
  - **Data Layer:** `RavenDbContext` quản lý kết nối, phiên làm việc bất đồng bộ, nạp cấu hình từ `appsettings.json`.
  - **Validation Layer:** `DocumentValidator` đảm bảo toàn vẹn tham chiếu giữa các Document trước khi ghi.
  - **Persistence & Indexing:** RavenDB Server quản lý 6 Primary Collections, Auto-Indexes và Static Index `ServiceHistories/ByCustomerAndDate`.

---

## B. Phân Công Trách Nhiệm Nhóm (Team Responsibilities)

| Thành viên | Phụ trách | Trạng thái hiện tại |
|---|---|---|
| **Thanh** | Database architecture, RavenDB Studio, Document Models, RavenDB integration, Data Layer, RQL, Indexes, integration testing and technical documentation | **ĐÃ HOÀN THÀNH 100% NỀN TẢNG** |
| **Quốc** | Customers and Appointments business modules (Controllers & Views) | **Sẵn sàng tiếp nhận để phát triển** |
| **Đạt** | Services, ServiceHistories and Feedbacks business modules (Controllers & Views) | **Sẵn sàng tiếp nhận để phát triển** |

---

## C. Tiến Độ Thực Hiện Hiện Tại (Current Implementation Progress)

### 1. Đã hoàn thành (Completed - Đã kiểm chứng thực tế)
- [x] Khảo sát môi trường và xác thực kết nối RavenDB Server 7.2.6 (`http://127.0.0.1:8080`).
- [x] Xây dựng cấu trúc dự án ASP.NET Core MVC và Solution chuẩn .NET 10.
- [x] Cài đặt 7 C# Document Models theo đúng lược đồ thiết kế CSDL (bao gồm Embedded Document `CustomerAddress`).
- [x] Triển khai Data Layer `IRavenDbContext` / `RavenDbContext` theo mô hình Singleton vòng đời và Unit of Work cho Async Sessions.
- [x] Triển khai `IDocumentValidator` / `DocumentValidator` kiểm tra ràng buộc toàn vẹn giữa Appointment, ServiceHistory và Feedback.
- [x] Triển khai `DatabaseSeeder` tự động kiểm tra, tạo database `CustomerCareDB` và nạp kịch bản mẫu trọn vẹn (Workflow từ `customers/001` đến `feedbacks/001`), hỗ trợ tính Idempotent.
- [x] Triển khai Static Index `ServiceHistories/ByCustomerAndDate` kế thừa `AbstractIndexCreationTask`.
- [x] Kiểm thử và xác thực 10/10 câu truy vấn Raven Query Language (RQL) kèm Auto-Index và cú pháp `load`.
- [x] Xây dựng bộ kiểm thử tự động xUnit với **18/18 test cases ĐẠT 100%**.
- [x] Xây dựng giao diện Dashboard trung tâm (`/Home/Index`) và trang kiểm thử RQL trực tiếp (`/Home/RqlQueries`).
- [x] Soạn thảo toàn bộ tài liệu kỹ thuật chi tiết trong thư mục `Docs/`.

### 2. Đang thực hiện (In progress)
- [*] Chuẩn bị phân phối mã nguồn và bàn giao kỹ thuật cho Quốc và Đạt qua GitHub.

### 3. Chưa bắt đầu (Not started)
- [ ] Xây dựng giao diện CRUD nghiệp vụ Customers & Appointments (Phụ trách: Quốc).
- [ ] Xây dựng giao diện CRUD nghiệp vụ Services, ServiceHistories & Feedbacks (Phụ trách: Đạt).

### 4. Tắc nghẽn (Blocked)
- Không có bất kỳ tắc nghẽn hạ tầng nào. Hạ tầng database và Data Layer vận hành ổn định.

---

## D. Kiến Trúc Cơ Sở Dữ Liệu (Database Architecture)

Cơ sở dữ liệu: **`CustomerCareDB`** gồm 6 Primary Collections:

1. **`Customers`**: Quản lý hồ sơ khách hàng.
   - Trường: `Id`, `FullName`, `Phone`, `Email`, `Address`, `Birthday`, `Gender`, `CreatedAt`, `Status`.
   - **`CustomerAddress` (Embedded Document):** Gồm `Street`, `Ward`, `District`, `City`. Địa chỉ được nhúng trực tiếp dưới dạng JSON con trong document của khách hàng để tối ưu hóa hiệu năng đọc và đảm bảo tính nguyên tử, **không tách thành collection riêng**.
2. **`Staffs`**: Quản lý hồ sơ nhân viên kỹ thuật.
   - Trường: `Id`, `FullName`, `Position`, `Phone`, `Email`, `CreatedAt`.
3. **`Services`**: Quản lý danh mục gói dịch vụ.
   - Trường: `Id`, `Name`, `Description`, `Price` (decimal), `Category`, `CreatedAt`.
4. **`Appointments`**: Quản lý lịch hẹn dịch vụ của khách.
   - Trường: `Id`, `CustomerId`, `ServiceId`, `AppointmentDate`, `Status`, `Note` (Dùng `Note`, không dùng `Notes`), `CreatedAt`.
   - Liên kết: `CustomerId` -> `Customers.Id`, `ServiceId` -> `Services.Id`.
5. **`ServiceHistories`**: Nhật ký thực hiện dịch vụ thực tế.
   - Trường: `Id`, `AppointmentId`, `CustomerId`, `ServiceId`, `StaffId`, `ServiceDate` (Dùng `ServiceDate`, không dùng `CompletedAt`), `Description`, `Result`, `Note`, `CreatedAt`.
   - Liên kết: `AppointmentId` -> `Appointments.Id`, `CustomerId` -> `Customers.Id`, `ServiceId` -> `Services.Id`, `StaffId` -> `Staffs.Id`.
6. **`Feedbacks`**: Đánh giá và phản hồi của khách hàng.
   - Trường: `Id`, `CustomerId`, `AppointmentId` (Liên kết trực tiếp tới `Appointments.Id`, không dùng `ServiceHistoryId`), `Rating` (1 <= Rating <= 5), `Comment`, `CreatedAt`.

---

## E. Cấu Trúc Dự Án Thực Tế (Project Structure)

```text
NoSQL_Project/
│
├── CustomerCareRavenDB.sln
│
├── CustomerCareRavenDB/
│   ├── Controllers/
│   │   ├── HomeController.cs              # Dashboard thống kê và kết nối RavenDB
│   │   └── DiagnosticController.cs        # Chẩn đoán kết nối và thực thi RQL trực tiếp
│   │
│   ├── Data/
│   │   ├── RavenSettings.cs               # Model cấu hình kết nối từ appsettings.json
│   │   ├── IRavenDbContext.cs             # Giao diện Data Layer
│   │   ├── RavenDbContext.cs              # Singleton DocumentStore và quản lý Session
│   │   ├── DatabaseSeeder.cs              # Khởi tạo database & nạp dữ liệu mẫu
│   │   └── Validation/
│   │       ├── IDocumentValidator.cs      # Giao diện kiểm tra toàn vẹn tài liệu
│   │       └── DocumentValidator.cs       # Logic kiểm tra quan hệ và ràng buộc
│   │
│   ├── Indexes/
│   │   └── ServiceHistories_ByCustomerAndDate.cs  # Static Index của RavenDB
│   │
│   ├── Models/
│   │   ├── CustomerAddress.cs             # Embedded Document
│   │   ├── Customer.cs
│   │   ├── Staff.cs
│   │   ├── Service.cs
│   │   ├── Appointment.cs
│   │   ├── ServiceHistory.cs
│   │   ├── Feedback.cs
│   │   └── DashboardViewModel.cs
│   │
│   ├── Views/
│   │   ├── Home/
│   │   │   ├── Index.cshtml               # Màn hình Dashboard trực quan
│   │   │   └── RqlQueries.cshtml          # Trình thử nghiệm 10 câu truy vấn RQL
│   │   └── Shared/
│   │       └── _Layout.cshtml             # Giao diện dùng chung Bootstrap 5
│   │
│   ├── Docs/                              # Tài liệu kỹ thuật dự án
│   │   ├── DATABASE_DESIGN.md
│   │   ├── RQL_QUERIES.md
│   │   ├── RAVENDB_STUDIO_GUIDE.md
│   │   ├── INTEGRATION_GUIDE.md
│   │   ├── TEST_REPORT.md
│   │   └── THANH_PROGRESS_REPORT.md
│   │
│   ├── appsettings.json                   # Cấu hình kết nối RavenDB
│   ├── Program.cs                         # Khởi tạo DI, tự động nạp seeder & index
│   └── CustomerCareRavenDB.csproj
│
├── CustomerCareRavenDB.Tests/             # Dự án kiểm thử tự động độc lập
│   ├── ModelTests.cs                      # Kiểm tra tính tuần tự hóa & embedded address
│   ├── RavenConnectionTests.cs            # Kiểm tra nạp cấu hình & ping server
│   ├── ValidationTests.cs                 # Kiểm tra logic nghiệp vụ & ràng buộc dữ liệu
│   ├── IntegrationTests.cs                # Kiểm tra tích hợp database, seeder & index
│   └── CustomerCareRavenDB.Tests.csproj
│
├── Docs/                                  # Bản sao tài liệu cấp gốc phục vụ tra cứu
├── README.md                              # Tài liệu trung tâm toàn nhóm
├── .gitignore                             # Loại trừ bin, obj, test results và bí mật
└── .env.example                           # Mẫu biến môi trường
```

---

## F. Hướng Dẫn Cài Đặt và Khởi Chạy (Installation Instructions)

Áp dụng cho máy tính Windows:

### 1. Clone mã nguồn
```powershell
git clone <URL_REPOSITORY>
cd NoSQL_Project
```

### 2. Khởi động RavenDB Server
Khởi động RavenDB Server cục bộ trên cổng `8080`. Mở trình duyệt kiểm tra tại: `http://127.0.0.1:8080`.

### 3. Khôi phục thư viện và Build
```powershell
dotnet restore
dotnet build
```

### 4. Chạy toàn bộ kiểm thử tự động
```powershell
dotnet test
```

### 5. Khởi chạy ứng dụng Web
```powershell
dotnet run --project CustomerCareRavenDB
```
Truy cập vào địa chỉ hiển thị trên màn hình console (ví dụ: `http://localhost:5069`) để trải nghiệm Dashboard và trình thử nghiệm truy vấn RQL.

---

## G. Quy Ước Tích Hợp Dành Cho Quốc và Đạt (Integration Contract)

1. **Sử dụng Data Layer:**
   - Luôn tiêm `IRavenDbContext` qua constructor của Controller.
   - Luôn mở Session bằng cú pháp `using var session = _context.OpenAsyncSession();`.
   - Luôn lưu dữ liệu bằng `await session.SaveChangesAsync();`.
2. **Kiểm tra ràng buộc tài liệu:**
   - Tiêm `IDocumentValidator` để kiểm tra trước khi Store bản ghi:
     - `ValidateAppointmentAsync` cho Appointment.
     - `ValidateServiceHistoryAsync` cho ServiceHistory.
     - `ValidateFeedbackAsync` cho Feedback.
3. **Tra cứu tài liệu hướng dẫn chi tiết:**
   - Đọc file [`Docs/INTEGRATION_GUIDE.md`](Docs/INTEGRATION_GUIDE.md) để lấy code mẫu Controller sẵn sàng chạy.

---

## H. Hạn Chế Đã Biết (Known Limitations)

1. Hệ thống hiện sử dụng xác thực kết nối nội bộ không bảo mật (Unsecured HTTP mode) cho môi trường Development trường học. Nếu triển khai Production qua Internet có bảo mật HTTPS, cần cấp file chứng chỉ `.pfx` vào thuộc tính `CertificatePath` trong `appsettings.json`.
2. Chưa triển khai cơ chế phân quyền tài khoản người dùng đăng nhập (sẽ bổ sung nếu có yêu cầu thêm từ giảng viên).

---

## I. Các Bước Tiếp Theo (Next Steps)

1. **Quốc:** Tiếp nhận nhánh `feature/thanh-ravendb-foundation`, tạo nhánh riêng và triển khai chức năng quản lý khách hàng và lịch hẹn (`CustomersController`, `AppointmentsController`).
2. **Đạt:** Tiếp nhận nhánh `feature/thanh-ravendb-foundation`, tạo nhánh riêng và triển khai chức năng quản lý dịch vụ, biên bản kỹ thuật và phản hồi (`ServicesController`, `ServiceHistoriesController`, `FeedbacksController`).
3. **Thanh:** Hỗ trợ giải đáp các thắc mắc về truy vấn RQL nâng cao, hỗ trợ kiểm thử tích hợp chéo giữa các module và tổng hợp báo cáo chung.

---

## J. Thông Tin Cập Nhật Lần Cuối (Last Update)

- **Ngày hoàn thành bàn giao:** 17/10/2026
- **Branch bàn giao:** `feature/thanh-ravendb-foundation`
- **Người bàn giao:** Thanh (Database Architect & Data Layer Engineer)
- **Commit hash nền tảng:** `7876592`
- **Lịch sử commit:**
  - `33d4ab5`: `feat(database): implement RavenDB foundation and document models`
  - `66b2d7d`: `feat(database): add seeding and indexes`
  - `eee7f1c`: `test(database): add RavenDB integration tests`
  - `56dd992`: `docs: add setup guide and team progress report`
  - `7876592`: `docs: record commit references and finalize handoff report`
