# TỔNG HỢP VÀ HƯỚNG DẪN 10 TRUY VẤN RAVEN QUERY LANGUAGE (RQL)

Tài liệu này cung cấp danh sách đầy đủ 10 câu truy vấn chuẩn hóa bằng **Raven Query Language (RQL)** đã được kiểm thử và xác thực thực tế trên cơ sở dữ liệu `CustomerCareDB`.

---

## 1. Danh Sách 10 Truy Vấn RQL Nghiệp Vụ

### Truy Vấn 1: Liệt kê toàn bộ khách hàng
- **Mục đích:** Lấy toàn bộ danh sách khách hàng trong collection `Customers`, bao gồm cả đối tượng nhúng `CustomerAddress`.
- **Câu lệnh RQL:**
```rql
from Customers
```
- **Kết quả thực tế:** Trả về 2 bản ghi (`customers/001`, `customers/002`) với đầy đủ trường dữ liệu và thông tin địa chỉ con.

---

### Truy Vấn 2: Tìm kiếm khách hàng theo số điện thoại
- **Mục đích:** Tra cứu hồ sơ khách hàng nhanh chóng khi khách gọi đến hotline trung tâm chăm sóc.
- **Câu lệnh RQL:**
```rql
from Customers where Phone = '0901234567'
```
- **Cơ chế Index:** RavenDB tự động kích hoạt Auto-Index `Auto/Customers/ByPhone`.
- **Kết quả thực tế:** Trả về document `customers/001` (Khách hàng Nguyễn Văn An).

---

### Truy Vấn 3: Tìm lịch hẹn theo CustomerId
- **Mục đích:** Tra cứu tất cả lịch hẹn dịch vụ của một khách hàng cụ thể.
- **Câu lệnh RQL:**
```rql
from Appointments where CustomerId = 'customers/001'
```
- **Kết quả thực tế:** Trả về document `appointments/001`.

---

### Truy Vấn 4: Lọc lịch hẹn theo trạng thái
- **Mục đích:** Quản lý danh sách các lịch hẹn đã hoàn thành dịch vụ (`Completed`).
- **Câu lệnh RQL:**
```rql
from Appointments where Status = 'Completed'
```
- **Kết quả thực tế:** Trả về document `appointments/001`.

---

### Truy Vấn 5: Tìm lịch sử chăm sóc theo AppointmentId
- **Mục đích:** Tra cứu biên bản kỹ thuật thực tế gắn liền với lịch hẹn dịch vụ ban đầu.
- **Câu lệnh RQL:**
```rql
from ServiceHistories where AppointmentId = 'appointments/001'
```
- **Kết quả thực tế:** Trả về document `servicehistories/001` ghi nhận kết quả bảo dưỡng vệ sinh thiết bị.

---

### Truy Vấn 6: Tìm lịch sử chăm sóc theo CustomerId
- **Mục đích:** Tra cứu toàn bộ nhật ký chăm sóc và bảo hành của khách hàng trong suốt vòng đời sử dụng dịch vụ.
- **Câu lệnh RQL:**
```rql
from ServiceHistories where CustomerId = 'customers/001'
```
- **Kết quả thực tế:** Trả về document `servicehistories/001`.

---

### Truy Vấn 7: Tìm lịch sử dịch vụ theo StaffId
- **Mục đích:** Thống kê hiệu suất và nhật ký làm việc của nhân viên kỹ thuật.
- **Câu lệnh RQL:**
```rql
from ServiceHistories where StaffId = 'staffs/001'
```
- **Kết quả thực tế:** Trả về các dịch vụ do nhân viên kỹ thuật Trần Thị Mai (`staffs/001`) trực tiếp phụ trách.

---

### Truy Vấn 8: Tìm đánh giá phản hồi theo điểm Rating
- **Mục đích:** Lọc ra các đánh giá dịch vụ đạt chất lượng cao (Rating từ 4 đến 5 sao) để phục vụ báo cáo chất lượng trung tâm.
- **Câu lệnh RQL:**
```rql
from Feedbacks where Rating >= 4
```
- **Kết quả thực tế:** Trả về document `feedbacks/001` (Rating = 5).

---

### Truy Vấn 9: Sắp xếp lịch sử dịch vụ theo thời gian thực hiện
- **Mục đích:** Xem nhật ký chăm sóc khách hàng theo thứ tự thời gian mới nhất trước (`desc`).
- **Câu lệnh RQL:**
```rql
from ServiceHistories order by ServiceDate desc
```
- **Kết quả thực tế:** Trả về danh sách `ServiceHistories` được sắp xếp giảm dần theo trường `ServiceDate`.

---

### Truy Vấn 10: Sử dụng từ khóa `load` để nạp thông tin liên kết (NoSQL Join)
- **Mục đích:** Trong kiến trúc NoSQL Document Database, dữ liệu quan hệ được lưu trữ dưới dạng ID tham chiếu. RQL hỗ trợ cú pháp `load` cho phép server nạp tài liệu liên quan và chiếu (project) dữ liệu tổng hợp về client trong **một round-trip mạng duy nhất**, không phát sinh bài toán N+1 query.
- **Câu lệnh RQL:**
```rql
from Appointments as a
load a.CustomerId as c
select {
    AppointmentId: id(a),
    CustomerName: c.FullName,
    CustomerPhone: c.Phone,
    AppointmentDate: a.AppointmentDate,
    Status: a.Status,
    Note: a.Note
}
```
- **Kết quả thực tế (JSON):**
```json
[
  {
    "AppointmentId": "appointments/001",
    "CustomerName": "Nguyễn Văn An",
    "CustomerPhone": "0901234567",
    "AppointmentDate": "2026-02-01T09:30:00.0000000Z",
    "Status": "Completed",
    "Note": "Khách hàng yêu cầu kiểm tra kỹ quạt tản nhiệt do phát ra tiếng ồn khi chạy tải nặng"
  },
  {
    "AppointmentId": "appointments/002",
    "CustomerName": "Lê Thị Bích Ngọc",
    "CustomerPhone": "0987654321",
    "AppointmentDate": "2026-02-05T14:00:00.0000000Z",
    "Status": "Confirmed",
    "Note": "Khách báo máy tính khởi động không lên nguồn"
  }
]
```

---

## 2. Minh Họa Chỉ Mục (Index Demonstration)

### 2.1. Auto-Index
Khi thực hiện các truy vấn lọc động không qua Static Index, RavenDB tự động phân tích và tạo index ngầm định:
- `Auto/Customers/ByPhone`
- `Auto/Appointments/ByCustomerIdAndStatus`
- `Auto/Feedbacks/ByRating`
- `Auto/ServiceHistories/ByAppointmentIdAndCustomerIdAndServiceDateAndStaffId`

### 2.2. Static Index: `ServiceHistories/ByCustomerAndDate`
- **Mục tiêu:** Cố định hóa chỉ mục cho màn hình tra cứu lịch sử của khách hàng.
- **Map Definition (RQL/JavaScript):**
```javascript
from history in docs.ServiceHistories
select new
{
    history.CustomerId,
    history.ServiceDate
}
```
- **Truy vấn RQL sử dụng Static Index:**
```rql
from index 'ServiceHistories/ByCustomerAndDate' 
where CustomerId = 'customers/001'
order by ServiceDate desc
```

---

## 3. Hướng Dẫn Tái Hiện Trên RavenDB Studio

1. Mở trình duyệt và truy cập RavenDB Studio: `http://127.0.0.1:8080`.
2. Ở thanh chọn cơ sở dữ liệu phía trên, chọn **CustomerCareDB**.
3. Tại menu bên trái, nhấp chọn mục **Indexes** -> **Query**.
4. Dán câu lệnh RQL cần chạy vào khung soạn thảo truy vấn.
5. Nhấn phím tắt `Ctrl + Enter` hoặc click nút **Run** (biểu tượng Play màu xanh).
6. Kết quả sẽ được hiển thị ngay bên dưới dưới dạng bảng (Grid) hoặc mã JSON.
7. Để xem Static Index, vào mục **Indexes** -> **List of Indexes**, nhấp vào `ServiceHistories/ByCustomerAndDate` để kiểm tra Map definition và trạng thái indexing.
