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
| **Quốc** | Customers and Appointments business modules (Controllers & Views) | **Đã merge vào `main` (PR #2)** |
| **Đạt** | Services, ServiceHistories and Feedbacks business modules (Controllers & Views) | **Đã merge vào `main` (PR #3)** |

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
- [x] Giao diện CRUD nghiệp vụ Customers & Appointments (Quốc — PR #2, đã merge vào `main`).
- [x] Giao diện CRUD nghiệp vụ Services, ServiceHistories & Feedbacks (Đạt — PR #3, đã merge vào `main`).

### 2. Đang thực hiện (In progress)
- [*] Sửa các lỗi phát hiện khi rà soát (nhánh `fix/bugs-from-review`).

### 3. Chưa bắt đầu (Not started)
- [ ] Phân quyền / đăng nhập người dùng (xem mục H).
- [ ] Triển khai (deploy) lên môi trường online.

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
CustomerCareRavenDB/                       # Thư mục gốc repository
│
├── CustomerCareRavenDB.sln                # Solution dùng để build/test (khuyến nghị)
├── CustomerCareRavenDB.slnx               # Bản solution định dạng mới (cùng 2 project, cho VS 2026)
│
├── CustomerCareRavenDB/
│   ├── Controllers/
│   │   ├── HomeController.cs              # Dashboard thống kê và kết nối RavenDB
│   │   ├── DiagnosticController.cs        # Chẩn đoán kết nối và thực thi RQL trực tiếp
│   │   ├── CustomersController.cs         # CRUD khách hàng (Quốc)
│   │   ├── AppointmentsController.cs      # CRUD lịch hẹn (Quốc)
│   │   ├── ServicesController.cs          # CRUD dịch vụ (Đạt)
│   │   ├── ServiceHistoriesController.cs  # Lịch sử chăm sóc (Đạt)
│   │   └── FeedbacksController.cs         # Phản hồi khách hàng (Đạt)
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

Yêu cầu: **.NET SDK 10.0** và một RavenDB Server (7.x) chạy cục bộ trên cổng `8080`. Các lệnh dưới đây dùng được trên Windows (PowerShell), macOS và Linux.

### 1. Clone mã nguồn
```powershell
git clone https://github.com/ahuhu789/CustomerCareRavenDB.git
cd CustomerCareRavenDB
```

### 2. Khởi động RavenDB Server
Khởi động RavenDB Server cục bộ trên cổng `8080`. Mở trình duyệt kiểm tra tại: `http://127.0.0.1:8080`.

Nếu có Docker, có thể chạy nhanh một server không bảo mật (chỉ dùng cho máy cá nhân):
```powershell
docker run -d --name ravendb -p 8080:8080 -e RAVEN_Setup_Mode=None -e RAVEN_License_Eula_Accepted=true -e RAVEN_Security_UnsecuredAccessAllowed=PrivateNetwork ravendb/ravendb:ubuntu-latest
```

Địa chỉ server / tên database / chứng chỉ có thể đổi trong `CustomerCareRavenDB/appsettings.json` (section `RavenSettings`) hoặc qua biến môi trường — xem `.env.example`.

### 3. Khôi phục thư viện và Build
Thư mục gốc có **hai** file solution (`.sln` và `.slnx`), nên phải chỉ rõ solution, nếu không `dotnet` báo lỗi `MSB1011`:
```powershell
dotnet restore CustomerCareRavenDB.sln
dotnet build CustomerCareRavenDB.sln
```

### 4. Chạy toàn bộ kiểm thử tự động
Các bài test tích hợp cần RavenDB Server đang chạy tại `http://127.0.0.1:8080` (đổi bằng biến môi trường `RAVEN_TEST_URL`).
```powershell
dotnet test CustomerCareRavenDB.sln
```

### 5. Khởi chạy ứng dụng Web
```powershell
dotnet run --project CustomerCareRavenDB
```
Truy cập vào địa chỉ hiển thị trên màn hình console (ví dụ: `http://localhost:5069`) để trải nghiệm Dashboard và trình thử nghiệm truy vấn RQL.

Ở môi trường Development, ứng dụng tự tạo database, triển khai index và nạp dữ liệu mẫu; ở môi trường khác thêm tham số `--seed` hoặc đặt `Seed__OnStartup=true` để nạp dữ liệu mẫu. Trình thử RQL (`/Diagnostic/ExecuteRql`) chỉ bật mặc định trong Development (cấu hình `Diagnostics:EnableRqlEndpoint`).

---

## G. Triển Khai (Deploy) lên Render Free

### 1. Kiến trúc
Ứng dụng được deploy thành **một web service Free duy nhất** trên Render (region Singapore), dùng `Dockerfile` ở thư mục gốc:

```
Internet ──HTTPS──> Render proxy ──HTTP :$PORT (10000)──> ASP.NET Core app ──http://127.0.0.1:8080──> RavenDB (cùng container)
```

- RavenDB Server chạy **trong cùng container**, không bảo mật nhưng **chỉ bind `127.0.0.1`** (HTTP 8080, TCP 38888) và không được EXPOSE, nên không thể truy cập từ Internet. Chỉ app trong container gọi được.
- Lý do không tách RavenDB thành service riêng: theo tài liệu Render, *web service Free không nhận được private network traffic*, và Free không có persistent disk.
- `docker/entrypoint.sh` khởi động RavenDB trước, chờ `GET /build/version` trả về 200, rồi mới chạy app. Nếu một trong hai tiến trình dừng, container thoát để Render khởi động lại.
- App tự tạo database `CustomerCareDB`, triển khai index và **nạp dữ liệu mẫu mỗi lần khởi động** (`Seed__OnStartup=true`, seeder idempotent).
- RavenDB Studio **không khả dụng** trên bản deploy (`RavenSettings__StudioEnabled=false`); giao diện hiển thị nút "Studio không khả dụng" thay cho liên kết.

### 2. Giới hạn cần biết (gói Free)
| Giới hạn | Ảnh hưởng |
|---|---|
| Không có persistent disk | **Mọi dữ liệu thêm/sửa sẽ mất** khi service restart, redeploy hoặc spin-down; sau đó dữ liệu mẫu được nạp lại. Chỉ phù hợp demo. |
| Spin-down sau 15 phút không có request | Lần truy cập tiếp theo phải chờ khởi động lại (với 0.1 CPU: ~1-2 phút, RavenDB khởi động trước rồi tới app). |
| 512 MB RAM, 0.1 CPU | Đo thực tế với `--memory=512m --cpus=0.1`: ~280-450 MiB (phần lớn là page cache của RavenDB, có thể thu hồi), không bị OOM khi tải 1500 request đồng thời. Trang đầu tiên sau khởi động chậm. |
| Render có thể restart service Free bất kỳ lúc nào | Giống mất dữ liệu ở trên. |
| Data Protection key lưu trong RavenDB | Mất khi restart → form đang mở trước khi restart cần tải lại trang (lỗi antiforgery). Key lưu không mã hóa (chỉ phù hợp demo). |

### 3. Các bước deploy trên Render
1. Đẩy code lên nhánh `main` của GitHub.
2. Render Dashboard → **New → Blueprint** → chọn repo này (Render đọc `render.yaml`). Hoặc tạo thủ công **New → Web Service** với:
   - Runtime/Language: **Docker**
   - Dockerfile Path: `./Dockerfile`
   - Docker Build Context Directory: `.` (thư mục gốc repo)
   - Instance Type: **Free**, Region: **Singapore**, Branch: `main`
   - Health Check Path: `/healthz`
3. Không cần khai báo bí mật nào. Các biến môi trường (đã có mặc định trong `Dockerfile`, `render.yaml` khai báo lại cho rõ):

| Biến | Giá trị | Ý nghĩa |
|---|---|---|
| `PORT` | Render tự đặt (10000) | Cổng app lắng nghe (`0.0.0.0:$PORT`) |
| `ASPNETCORE_ENVIRONMENT` | `Production` | |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` | Tin `X-Forwarded-Proto/For` từ proxy Render (HTTPS, HSTS đúng) |
| `RavenSettings__Urls__0` | `http://127.0.0.1:8080` | RavenDB nhúng |
| `RavenSettings__DatabaseName` | `CustomerCareDB` | |
| `RavenSettings__StudioEnabled` | `false` | Ẩn liên kết Studio |
| `Seed__OnStartup` | `true` | Nạp dữ liệu mẫu khi khởi động |
| `EMBEDDED_RAVENDB` | `true` | `false` = không chạy RavenDB nhúng, dùng RavenDB bên ngoài |

4. Kiểm tra: `https://<tên-service>.onrender.com/healthz` trả về `{"status":"Healthy",...}` (HTTP 200; 503 nếu RavenDB chưa sẵn sàng).

`render.yaml` đặt `autoDeployTrigger: checksPass`: Render chỉ deploy commit mới trên `main` khi workflow CI pass.

### 4. Chạy image cục bộ (giống Render)
```bash
docker build -t customercare-ravendb .
docker run --rm -p 10000:10000 --memory=512m --cpus=0.1 customercare-ravendb
# Mở http://localhost:10000 , health check: http://localhost:10000/healthz
```

### 5. (Tùy chọn) Dùng RavenDB bên ngoài có chứng chỉ
Nếu sau này có RavenDB bảo mật (RavenDB Cloud hoặc server riêng), đặt `EMBEDDED_RAVENDB=false`, `RavenSettings__Urls__0=https://...`, `RavenSettings__StudioEnabled=true` và chứng chỉ client dạng Base64 (ưu tiên hơn `CertificatePath`), khai báo là biến bí mật, không commit:
```bash
base64 -w0 client-certificate.pfx            # Linux
[Convert]::ToBase64String([IO.File]::ReadAllBytes("client-certificate.pfx"))   # PowerShell
```
→ `RavenSettings__CertificateBase64=<chuỗi base64>`, `RavenSettings__CertificatePassword=<mật khẩu nếu có>`. Chứng chỉ cần tối thiểu quyền **Admin trên database** để deploy index (ReadWrite không đủ). Nếu chứng chỉ không có quyền tạo database nhưng database đã tồn tại, app vẫn tiếp tục.

### 6. CI (GitHub Actions)
`.github/workflows/ci.yml` chạy khi push/PR vào `main`:
- **Build & test**: .NET 10 + RavenDB service container (`ravendb/ravendb:ubuntu-latest`, cổng 8080); test đọc URL từ `RAVEN_TEST_URL` (mặc định `http://127.0.0.1:8080`).
- **Docker smoke test**: build image, chạy với `--memory=512m` và kiểm tra `/healthz`, `/`, `/Customers`.

---

## H. Quy Ước Tích Hợp Dành Cho Quốc và Đạt (Integration Contract)

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

## I. Hạn Chế Đã Biết (Known Limitations)

1. Hệ thống hiện sử dụng xác thực kết nối nội bộ không bảo mật (Unsecured HTTP mode) cho môi trường Development trường học. Nếu triển khai Production qua Internet có bảo mật HTTPS, cần cấp file chứng chỉ `.pfx` vào thuộc tính `CertificatePath` trong `appsettings.json`.
2. Chưa triển khai cơ chế phân quyền tài khoản người dùng đăng nhập (sẽ bổ sung nếu có yêu cầu thêm từ giảng viên).

---

## J. Các Bước Tiếp Theo (Next Steps)

1. **Cả nhóm:** Kiểm thử chéo toàn bộ 6 phân hệ trên nhánh `main` (các module của Quốc và Đạt đã được merge qua PR #2 và PR #3).
2. **Quốc / Đạt:** Bổ sung kiểm tra tham chiếu khi xóa khách hàng (lịch hẹn/lịch sử/phản hồi đang tham chiếu) và phân trang cho các danh sách lớn.
3. **Thanh:** Hỗ trợ giải đáp các thắc mắc về truy vấn RQL nâng cao, hỗ trợ kiểm thử tích hợp chéo giữa các module và tổng hợp báo cáo chung.

---

## K. Thông Tin Cập Nhật Lần Cuối (Last Update)

- **Ngày hoàn thành bàn giao:** 17/10/2026
- **Branch bàn giao:** `feature/thanh-ravendb-foundation`
- **Người bàn giao:** Thanh (Database Architect & Data Layer Engineer)
- **Commit hash nền tảng:** `7ed75c7`
- **Lịch sử commit:**
  - `33d4ab5`: `feat(database): implement RavenDB foundation and document models`
  - `66b2d7d`: `feat(database): add seeding and indexes`
  - `eee7f1c`: `test(database): add RavenDB integration tests`
  - `56dd992`: `docs: add setup guide and team progress report`
  - `7ed75c7`: `docs: record commit references and finalize handoff report`
