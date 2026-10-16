# BÁO CÁO KIỂM THỬ TỰ ĐỘNG HẠ TẦNG RAVENDB (TEST REPORT)

## 1. Môi Trường Kiểm Thử Thực Tế
- **Hệ điều hành:** Windows 10/11 x64
- **.NET SDK:** 10.0.402
- **Test Framework:** xUnit v2.9.3 + Microsoft.NET.Test.Sdk v17.14.1
- **Database Engine:** RavenDB Server v7.2.6 (chạy trên cổng `127.0.0.1:8080`)
- **RavenDB Client:** RavenDB.Client v7.2.6
- **Chiến lược an toàn dữ liệu:** Toàn bộ integration test được thực thi trên database kiểm thử cô lập độc lập (`Test_Validation_*`, `Test_Integration_*`) và tự động dọn dẹp sau khi chạy. Database chính `CustomerCareDB` tuyệt đối không bị ảnh hưởng.

---

## 2. Kết Quả Thực Thi Thực Tế (Execution Summary)

Lệnh thực thi kiểm thử:
```powershell
dotnet test
```

### Bằng chứng chạy kiểm thử thực tế từ Console:
```text
  Determining projects to restore...
  All projects are up-to-date for restore.
  CustomerCareRavenDB -> C:\Users\ngocv\OneDrive\Desktop\NoSQL_Project\CustomerCareRavenDB\bin\Debug\net10.0\CustomerCareRavenDB.dll
  CustomerCareRavenDB.Tests -> C:\Users\ngocv\OneDrive\Desktop\NoSQL_Project\CustomerCareRavenDB.Tests\bin\Debug\net10.0\CustomerCareRavenDB.Tests.dll
Test run for C:\Users\ngocv\OneDrive\Desktop\NoSQL_Project\CustomerCareRavenDB.Tests\bin\Debug\net10.0\CustomerCareRavenDB.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18, Duration: 2 s - CustomerCareRavenDB.Tests.dll (net10.0)
```

| Chỉ số | Kết quả | Trạng thái |
|---|---|---|
| **Tổng số test cases** | **18** | Hoàn thành |
| **Thành công (Passed)** | **18** | 100% ĐẠT |
| **Thất bại (Failed)** | **0** | Không có lỗi |
| **Bỏ qua (Skipped)** | **0** | Đầy đủ |
| **Thời gian chạy** | **~2 giây** | Tốc độ cao |

---

## 3. Chi Tiết Các Bộ Ca Kiểm Thử (Test Suites Breakdown)

### 3.1. ModelTests (3 Test Cases)
Xác thực cấu trúc mô hình tài liệu theo đúng sơ đồ thiết kế CSDL và cơ chế tuần tự hóa JSON:
1. `Customer_WithEmbeddedAddress_SerializesCorrectly`: Kiểm tra đối tượng `CustomerAddress` được nhúng trực tiếp dưới dạng JSON object con bên trong `Customer`, không bị phân rã thành collection riêng.
2. `DocumentReferences_StoreStringIds_NotEmbeddedObjects`: Xác thực các liên kết `Appointments`, `ServiceHistories`, `Feedbacks` lưu chuỗi ID tham chiếu (`customers/001`, `appointments/001`), không nhúng nguyên object. Đảm bảo tên trường chuẩn `Note` và `ServiceDate`.
3. `Staff_And_Service_Properties_AreConsistent`: Kiểm tra các thuộc tính đơn giá `Price` (decimal) và chức danh của Staff.

### 3.2. RavenConnectionTests (3 Test Cases)
Xác thực cấu hình kết nối và cơ chế xử lý lỗi kết nối hạ tầng:
1. `RavenDbContext_Throws_WhenUrlsAreEmpty`: Ném ngoại lệ `ArgumentException` khi danh sách URL rỗng.
2. `RavenDbContext_Throws_WhenDatabaseNameIsEmpty`: Ném ngoại lệ `ArgumentException` khi tên database bị để trống.
3. `RavenDbContext_PingAsync_ReturnsTrue_WhenServerIsOnline`: Kiểm tra gọi hàm `PingAsync()` tới RavenDB Server trả về `true` khi server hoạt động.

### 3.3. ValidationTests (9 Test Cases)
Xác thực logic nghiệp vụ kiểm tra toàn vẹn tài liệu (`IDocumentValidator`) trước khi lưu:
1. `ValidateAppointment_Succeeds_WhenCustomerAndServiceExist`: Lịch hẹn hợp lệ khi cả khách hàng và dịch vụ đều tồn tại.
2. `ValidateAppointment_Fails_WhenCustomerDoesNotExist`: Từ chối tạo lịch hẹn khi `CustomerId` không tồn tại.
3. `ValidateAppointment_Fails_WhenServiceDoesNotExist`: Từ chối tạo lịch hẹn khi `ServiceId` không tồn tại.
4. `ValidateFeedback_Fails_WhenRatingIsLessThan1`: Bắt lỗi và từ chối đánh giá khi `Rating = 0` (yêu cầu 1 <= Rating <= 5).
5. `ValidateFeedback_Fails_WhenRatingIsGreaterThan5`: Bắt lỗi và từ chối đánh giá khi `Rating = 6`.
6. `ValidateFeedback_Fails_WhenCustomerIdDoesNotMatchAppointment`: Phát hiện và từ chối khi `Feedback.CustomerId` không khớp với khách hàng đã đặt lịch hẹn (`Appointment.CustomerId`).
7. `ValidateServiceHistory_Fails_WhenCustomerIdDoesNotMatchAppointment`: Phát hiện và từ chối khi `ServiceHistory.CustomerId` bị lệch với `Appointment.CustomerId`.
8. `ValidateServiceHistory_Fails_WhenServiceIdDoesNotMatchAppointment`: Phát hiện và từ chối khi `ServiceHistory.ServiceId` bị lệch với `Appointment.ServiceId`.
9. `ValidateServiceHistory_Succeeds_WhenAllReferencesAreConsistent`: Chấp thuận khi toàn bộ liên kết giữa Appointment, Customer, Service và Staff hoàn toàn nhất quán.

### 3.4. IntegrationTests (3 Test Cases)
Xác thực tích hợp cơ sở dữ liệu thực tế:
1. `Seeder_InitializesCompleteBusinessWorkflow_AndIsIdempotent`:
   - Nạp thành công toàn bộ chuỗi nghiệp vụ mẫu: Customer (`customers/001`) -> Staff (`staffs/001`) -> Service (`services/001`) -> Appointment (`appointments/001`) -> ServiceHistory (`servicehistories/001`) -> Feedback (`feedbacks/001`).
   - Kiểm tra tính Idempotent: Khi gọi hàm Seed lần 2, hệ thống tự động phát hiện dữ liệu đã tồn tại và trả về `false`, không ghi đè hay nhân đôi bản ghi.
2. `StaticIndex_ServiceHistories_ByCustomerAndDate_CanBeQueried`: Đăng ký thành công Static Index `ServiceHistories/ByCustomerAndDate` và thực thi truy vấn LINQ/RQL lọc theo `CustomerId` và sắp xếp theo `ServiceDate`.
3. `RqlQuery_WithLoad_RetrievesReferencedCustomer`: Kiểm tra truy vấn RQL với từ khóa `load` kết hợp dữ liệu giữa `Appointments` và `Customers`.

---

## 4. Kết Luận
Nền tảng hạ tầng RavenDB, Data Layer, hệ thống Model, chỉ mục và các quy tắc kiểm tra toàn vẹn tài liệu đã được kiểm thử tự động toàn diện và đạt tỷ lệ thành công 100%. Cơ sở dữ liệu và Data Layer đã sẵn sàng bàn giao cho các thành viên phát triển module nghiệp vụ.
