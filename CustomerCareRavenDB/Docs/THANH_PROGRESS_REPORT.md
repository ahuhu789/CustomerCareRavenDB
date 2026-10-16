# BÁO CÁO TIẾN ĐỘ THỰC HIỆN DỰ ÁN (THANH - DATABASE & FOUNDATION)

**Người thực hiện:** Thanh  
**Vai trò:** Database Architect & Data Layer Engineer  
**Dự án:** Quản lý hồ sơ khách hàng và lịch sử chăm sóc khách hàng bằng RavenDB  
**Đối tượng nhận báo cáo:** Thầy/Cô hướng dẫn và 2 thành viên trong nhóm (Quốc, Đạt)

---

## Bảng Tiến Độ Tổng Quan (Progress Table)

| Hạng mục công việc | Người phụ trách | Trạng thái | Bằng chứng kiểm thử / Hoàn thành |
|---|---|---|---|
| **Kiểm tra môi trường & cài đặt SDK/RavenDB** | Thanh | **ĐÃ HOÀN THÀNH** | .NET 10.0.402, RavenDB Server 7.2.6 (Port 8080) |
| **Thiết kế & cài đặt 7 C# Document Models** | Thanh | **ĐÃ HOÀN THÀNH** | Customer (Embedded Address), Staff, Service, Appointment, ServiceHistory, Feedback |
| **Xây dựng Data Layer (RavenDbContext, Singleton)** | Thanh | **ĐÃ HOÀN THÀNH** | Singleton IDocumentStore, Async Sessions, Options Pattern |
| **Xây dựng DatabaseSeeder (Idempotent, Chuẩn quy trình)** | Thanh | **ĐÃ HOÀN THÀNH** | Nạp chuỗi 001: Customer -> Appointment -> Service -> ServiceHistory -> Staff -> Feedback |
| **Triển khai Static Index (ServiceHistories/ByCustomerAndDate)** | Thanh | **ĐÃ HOÀN THÀNH** | AbstractIndexCreationTask tự động deploy khi app chạy |
| **Xác thực 10 câu truy vấn RQL & Auto-Index** | Thanh | **ĐÃ HOÀN THÀNH** | Đã chạy và verify 10/10 truy vấn RQL thành công |
| **Bộ kiểm thử tự động (Unit & Integration Tests)** | Thanh | **ĐÃ HOÀN THÀNH** | **18/18 tests ĐẠT** (xUnit + isolated test database) |
| **Bộ tài liệu kỹ thuật & Hướng dẫn tích hợp** | Thanh | **ĐÃ HOÀN THÀNH** | 6 tài liệu chi tiết tại thư mục `Docs/` |
| **Module Khách hàng & Lịch hẹn** | Quốc | Chưa bắt đầu | Đã có sẵn Model, Data Layer, Validator và Code mẫu |
| **Module Dịch vụ, Lịch sử chăm sóc & Phản hồi** | Đạt | Chưa bắt đầu | Đã có sẵn Model, Data Layer, Validator và Code mẫu |

---

## 1. Tổng Quan Công Việc Được Phân Công
Thanh chịu trách nhiệm toàn bộ phần nền tảng kỹ thuật cốt lõi (Technical Foundation) của dự án:
- Khảo sát và thiết lập hạ tầng RavenDB 7.2.
- Xây dựng kiến trúc cơ sở dữ liệu `CustomerCareDB` với 6 primary collections chuẩn hóa.
- Cài đặt hệ thống Document Models C# đảm bảo đúng bản chất NoSQL (Embedded Document cho địa chỉ khách hàng, ID Reference cho các liên kết khác).
- Cài đặt Data Layer `RavenDbContext` theo mô hình Singleton và Unit of Work.
- Cài đặt cơ chế kiểm tra ràng buộc toàn vẹn dữ liệu `IDocumentValidator`.
- Cài đặt công cụ khởi tạo dữ liệu mẫu ban đầu `DatabaseSeeder`.
- Xây dựng Static Index `ServiceHistories_ByCustomerAndDate` và xác thực 10 câu truy vấn RQL.
- Viết bộ kiểm thử tự động (Automated Tests) và toàn bộ tài liệu hướng dẫn nhóm.

---

## 2. Các Đầu Việc Thanh Đã Hoàn Thành
1. Khởi tạo cấu trúc Solution chuẩn .NET bao gồm dự án web ASP.NET Core MVC `CustomerCareRavenDB` và dự án kiểm thử `CustomerCareRavenDB.Tests`.
2. Tạo 7 file Model C# chuẩn hóa với đầy đủ XML docstring.
3. Cấu hình kết nối linh hoạt trong `appsettings.json` thông qua Options Pattern (`RavenSettings`).
4. Hiện thực hóa `IRavenDbContext` / `RavenDbContext` quản lý duy nhất 1 `IDocumentStore` Singleton, cung cấp các phiên làm việc bất đồng bộ `OpenAsyncSession()`.
5. Hiện thực hóa `DocumentValidator` ngăn chặn việc tạo dữ liệu rác hoặc dữ liệu mâu thuẫn.
6. Hiện thực hóa `DatabaseSeeder` tạo database tự động và nạp dữ liệu kịch bản mẫu trọn vẹn.
7. Tạo chỉ mục tĩnh `ServiceHistories_ByCustomerAndDate` bằng C# class kế thừa `AbstractIndexCreationTask`.
8. Kiểm thử thành công 10 truy vấn RQL phức hợp có sử dụng phép chiếu dữ liệu và từ khóa liên kết `load`.
9. Viết 18 ca kiểm thử tự động bao quát toàn bộ logic và chạy thành công 100%.
10. Xây dựng giao diện Dashboard trực quan tại trang chủ (`HomeController`) và trang tương tác RQL (`/Home/RqlQueries`).

---

## 3. Cấu Trúc CSDL Đã Triển Khai
Database: **`CustomerCareDB`** trên cụm RavenDB Server cục bộ `http://127.0.0.1:8080`.
Bao gồm chính xác 6 primary collection:
1. `Customers`: Hồ sơ khách hàng. Địa chỉ `CustomerAddress` là **Embedded Document** nhúng trực tiếp trong document của khách hàng.
2. `Staffs`: Nhân viên kỹ thuật trung tâm.
3. `Services`: Bảng giá và danh mục gói dịch vụ.
4. `Appointments`: Lịch hẹn dịch vụ, liên kết tới `Customers.Id` và `Services.Id`.
5. `ServiceHistories`: Nhật ký bảo dưỡng/sửa chữa thực tế, liên kết tới `Appointments.Id`, `Customers.Id`, `Services.Id` và `Staffs.Id`. Sử dụng trường `ServiceDate` và `Note`.
6. `Feedbacks`: Phản hồi đánh giá mức độ hài lòng từ 1 đến 5 sao, liên kết trực tiếp tới `Appointments.Id` và `Customers.Id`.

---

## 4. Các File Đã Tạo Hoặc Cập Nhật
- **Solution & Projects:**
  - `CustomerCareRavenDB.sln`
  - `CustomerCareRavenDB/CustomerCareRavenDB.csproj`
  - `CustomerCareRavenDB.Tests/CustomerCareRavenDB.Tests.csproj`
- **Models:**
  - `CustomerCareRavenDB/Models/CustomerAddress.cs` (Embedded Document)
  - `CustomerCareRavenDB/Models/Customer.cs`
  - `CustomerCareRavenDB/Models/Staff.cs`
  - `CustomerCareRavenDB/Models/Service.cs`
  - `CustomerCareRavenDB/Models/Appointment.cs`
  - `CustomerCareRavenDB/Models/ServiceHistory.cs`
  - `CustomerCareRavenDB/Models/Feedback.cs`
  - `CustomerCareRavenDB/Models/DashboardViewModel.cs`
- **Data Layer & Indexes:**
  - `CustomerCareRavenDB/Data/RavenSettings.cs`
  - `CustomerCareRavenDB/Data/IRavenDbContext.cs`
  - `CustomerCareRavenDB/Data/RavenDbContext.cs`
  - `CustomerCareRavenDB/Data/Validation/IDocumentValidator.cs`
  - `CustomerCareRavenDB/Data/Validation/DocumentValidator.cs`
  - `CustomerCareRavenDB/Data/DatabaseSeeder.cs`
  - `CustomerCareRavenDB/Indexes/ServiceHistories_ByCustomerAndDate.cs`
- **Controllers & Views:**
  - `CustomerCareRavenDB/Controllers/HomeController.cs`
  - `CustomerCareRavenDB/Controllers/DiagnosticController.cs`
  - `CustomerCareRavenDB/Views/Home/Index.cshtml`
  - `CustomerCareRavenDB/Views/Home/RqlQueries.cshtml`
  - `CustomerCareRavenDB/Views/Shared/_Layout.cshtml`
  - `CustomerCareRavenDB/Program.cs`
  - `CustomerCareRavenDB/appsettings.json`
- **Kiểm thử tự động:**
  - `CustomerCareRavenDB.Tests/ModelTests.cs`
  - `CustomerCareRavenDB.Tests/RavenConnectionTests.cs`
  - `CustomerCareRavenDB.Tests/ValidationTests.cs`
  - `CustomerCareRavenDB.Tests/IntegrationTests.cs`
- **Tài liệu hướng dẫn:**
  - `Docs/DATABASE_DESIGN.md`
  - `Docs/RQL_QUERIES.md`
  - `Docs/RAVENDB_STUDIO_GUIDE.md`
  - `Docs/INTEGRATION_GUIDE.md`
  - `Docs/TEST_REPORT.md`
  - `Docs/THANH_PROGRESS_REPORT.md`
  - `README.md`
  - `.gitignore`
  - `.env.example`

---

## 5. Kết Quả Kiểm Thử
- **Lệnh thực thi:** `dotnet test`
- **Số lượng:** 18/18 tests PASSED.
- **Thời gian chạy:** ~2 giây.
- **Phạm vi kiểm thử:** Kiểm tra tính năng nhúng CustomerAddress, tuần tự hóa chuỗi ID liên kết, nạp cấu hình kết nối, xử lý lỗi ngoại lệ, các quy tắc kiểm tra Rating (1-5), kiểm tra tính nhất quán dữ liệu giữa Appointment - ServiceHistory - Feedback, tính Idempotent của Seeder, và kiểm tra truy vấn qua Static Index & RQL `load`.

---

## 6. Kết Quả Kết Nối RavenDB
- Server: RavenDB 7.2.6 chạy trên cổng `http://127.0.0.1:8080`.
- Trạng thái: **Healthy** (Đã kiểm chứng thông qua endpoint `/Diagnostic/Ping` và phương thức `PingAsync()`).
- Database `CustomerCareDB` đã được tạo và nạp thành công bộ dữ liệu mẫu chuẩn:
  - `customers/001`: Nguyễn Văn An
  - `staffs/001`: Trần Thị Mai
  - `services/001`: Bảo dưỡng và vệ sinh thiết bị toàn diện
  - `appointments/001`: Lịch hẹn ngày 01/02/2026
  - `servicehistories/001`: Biên bản kỹ thuật hoàn tất vệ sinh
  - `feedbacks/001`: Đánh giá 5 sao từ khách hàng

---

## 7. Những Gì Quốc Có Thể Bắt Đầu Làm (Module A)
Quốc có thể bắt đầu xây dựng ngay các chức năng nghiệp vụ sau:
1. Tạo `CustomersController.cs` và các View tương ứng (`Index`, `Details`, `Create`, `Edit`).
2. Tạo `AppointmentsController.cs`:
   - Màn hình đặt lịch hẹn mới cho khách hàng.
   - Khi lưu lịch hẹn, gọi `_validator.ValidateAppointmentAsync(session, appointment)` để đảm bảo `CustomerId` và `ServiceId` tồn tại.
   - Danh sách lịch hẹn theo trạng thái (Pending, Confirmed, Completed).
3. Đọc kỹ file hướng dẫn `Docs/INTEGRATION_GUIDE.md` phần mục 4 để copy code mẫu.

---

## 8. Những Gì Đạt Có Thể Bắt Đầu Làm (Module B)
Đạt có thể bắt đầu xây dựng ngay các chức năng nghiệp vụ sau:
1. Tạo `ServicesController.cs`: Quản lý danh mục gói dịch vụ bảo dưỡng và sửa chữa.
2. Tạo `ServiceHistoriesController.cs`:
   - Ghi nhận nhật ký khi kỹ thuật viên thực hiện dịch vụ cho khách.
   - Khi lưu, gọi `_validator.ValidateServiceHistoryAsync(session, history)` để đảm bảo `CustomerId` và `ServiceId` khớp với thông tin trên `Appointment`.
   - Sử dụng Static Index `ServiceHistories_ByCustomerAndDate` để tra cứu lịch sử nhanh chóng.
3. Tạo `FeedbacksController.cs`:
   - Màn hình tiếp nhận đánh giá từ khách hàng sau khi hoàn tất lịch hẹn.
   - Gọi `_validator.ValidateFeedbackAsync(session, feedback)` để kiểm tra điểm Rating (1..5) và tham chiếu `AppointmentId`.
4. Đọc kỹ file hướng dẫn `Docs/INTEGRATION_GUIDE.md` phần mục 5 để copy code mẫu.

---

## 9. Các Quy Ước Tích Hợp Bắt Buộc
- **Không dùng Entity Framework Core hay SQL:** Mọi thao tác truy xuất dữ liệu bắt buộc qua `IRavenDbContext.OpenAsyncSession()`.
- **Không tự ý đổi tên trường:** Giữ đúng `Note` (không dùng `Notes`), `ServiceDate` (không dùng `CompletedAt`), và `AppointmentId` trong Feedback (không dùng `ServiceHistoryId`).
- **Địa chỉ khách hàng là Embedded Document:** Truy cập qua `customer.Address.Street`, `customer.Address.City`, v.v...
- **Luôn kiểm tra Validation trước khi lưu:** Sử dụng `IDocumentValidator` đã được tiêm qua DI.
- **Lưu trữ ngày giờ theo chuẩn UTC:** Luôn gán `DateTime.UtcNow`.

---

## 10. Hạn Chế và Công Việc Còn Lại
- **Hạn chế hiện tại:** Chưa cài đặt xác thực người dùng / phân quyền (Authentication / Authorization) phức tạp (sẽ thực hiện nếu yêu cầu đề tài phát sinh thêm).
- **Công việc còn lại:**
  - Quốc hoàn thiện giao diện và Controller cho Customers & Appointments.
  - Đạt hoàn thiện giao diện và Controller cho Services, ServiceHistories & Feedbacks.
  - Ghép nối toàn bộ hệ thống và tiến hành quay video demo báo cáo môn học.

---

## 11. Hướng Dẫn Chạy Dự Án
```powershell
# 1. Khởi động RavenDB Server tại http://127.0.0.1:8080
# 2. Khôi phục thư viện và build:
dotnet restore
dotnet build

# 3. Chạy toàn bộ test tự động:
dotnet test

# 4. Chạy ứng dụng web:
dotnet run --project CustomerCareRavenDB
```
Mở trình duyệt truy cập: `http://localhost:5069` (hoặc cổng hiển thị trên console).

---

## 12. Commit Hash và Branch Bàn Giao
- **Branch bàn giao:** `feature/thanh-ravendb-foundation`
- **Commit hash nền tảng:** `7876592`
- **Lịch sử các commits:**
  1. `33d4ab5`: `feat(database): implement RavenDB foundation and document models`
  2. `66b2d7d`: `feat(database): add seeding and indexes`
  3. `eee7f1c`: `test(database): add RavenDB integration tests`
  4. `56dd992`: `docs: add setup guide and team progress report`
  5. `7876592`: `docs: record commit references and finalize handoff report`
- **Trạng thái:** Toàn bộ mã nguồn, cấu hình, dữ liệu mẫu, chỉ mục và tài liệu kỹ thuật đã được đóng gói hoàn chỉnh, vượt qua 18/18 ca kiểm thử và sẵn sàng bàn giao cho Quốc và Đạt.
