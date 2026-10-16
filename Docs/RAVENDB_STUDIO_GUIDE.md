# HƯỚNG DẪN SỬ DỤNG RAVENDB STUDIO CHO DỰ ÁN CUSTOMERCAREDB

## 1. Giới Thiệu RavenDB Studio
RavenDB Studio là giao diện đồ họa quản trị web tích hợp sẵn đi kèm với RavenDB Server. Studio cho phép các thành viên trong nhóm trực quan hóa cấu trúc dữ liệu, theo dõi các Collection, thực thi truy vấn RQL, theo dõi chỉ mục (Indexes) và chẩn đoán hiệu năng hệ thống mà không cần cài đặt phần mềm bên thứ ba.

---

## 2. Truy Cập và Chọn Database

1. Đảm bảo tiến trình `Raven.Server` đang chạy trên máy cục bộ.
2. Mở trình duyệt web (Chrome, Edge, Firefox) và truy cập đường dẫn:
   ```text
   http://127.0.0.1:8080
   ```
   Hoặc truy cập trực tiếp vào database của dự án qua URL:
   ```text
   http://127.0.0.1:8080/studio/index.html#databases/documents?&database=CustomerCareDB
   ```
3. Tại góc trên bên trái màn hình Studio, nhấp vào danh sách database và chọn **CustomerCareDB**.

---

## 3. Khám Phá Collections và Documents

Tại menu thanh bên trái, chọn **Documents** -> **Documents**:
1. **Danh sách Collections:**
   Bạn sẽ thấy chính xác 6 primary collection:
   - `Customers` (Hồ sơ khách hàng)
   - `Staffs` (Nhân viên kỹ thuật)
   - `Services` (Gói dịch vụ)
   - `Appointments` (Lịch hẹn)
   - `ServiceHistories` (Lịch sử dịch vụ)
   - `Feedbacks` (Đánh giá)

2. **Kiểm tra Embedded Document (Tài liệu nhúng):**
   - Nhấp vào collection `Customers`, mở document `customers/001`.
   - Quan sát trường `Address`: Đối tượng này là một JSON Object con nhúng trực tiếp trong document, không nằm ở một bảng hay collection ngoài:
     ```json
     "Address": {
       "Street": "123 Nguyễn Huệ",
       "Ward": "Bến Nghé",
       "District": "Quận 1",
       "City": "TP. Hồ Chí Minh"
     }
     ```

3. **Kiểm tra Document References (Liên kết tài liệu):**
   - Mở document `appointments/001`: Trường `CustomerId` chứa giá trị chuỗi `"customers/001"`. RavenDB Studio tự động hiển thị liên kết clickable (có thể click chuột vào) để nhảy trực tiếp sang xem hồ sơ khách hàng.
   - Tương tự, mở `servicehistories/001` để thấy các tham chiếu `AppointmentId`, `CustomerId`, `ServiceId`, `StaffId`.

---

## 4. Chạy Truy Vấn RQL Trong Studio

1. Tại thanh menu bên trái, nhấp chọn **Indexes** -> **Query**.
2. Nhập câu truy vấn RQL vào khung soạn thảo. Ví dụ:
   ```rql
   from Customers where Phone = '0901234567'
   ```
3. Nhấp nút **Run** (hoặc tổ hợp phím `Ctrl + Enter`).
4. Kết quả sẽ hiển thị ở khu vực bên dưới với 2 chế độ hiển thị:
   - **Grid view:** Xem dưới dạng bảng cột trực quan.
   - **JSON view:** Xem trực tiếp cấu trúc JSON gốc và `@metadata`.

5. **Thử nghiệm phép nạp dữ liệu liên kết `load`:**
   Dán câu lệnh sau vào khung Query:
   ```rql
   from Appointments as a
   load a.CustomerId as c
   select {
       AppointmentId: id(a),
       CustomerName: c.FullName,
       Phone: c.Phone,
       Status: a.Status
   }
   ```
   Bấm **Run** để thấy thông tin từ 2 collection `Appointments` và `Customers` được kết hợp tự động ngay trên server.

---

## 5. Quản Lý Chỉ Mục (Indexes)

1. **Xem danh sách Indexes:**
   Vào **Indexes** -> **List of Indexes**.
2. **Static Index:**
   Tìm chỉ mục `ServiceHistories/ByCustomerAndDate`.
   - Nhấp vào tên chỉ mục để xem định nghĩa Map (C#/JavaScript).
   - Nhấp vào **Terms** để xem các giá trị đã được lập chỉ mục (`CustomerId`, `ServiceDate`).
   - Kiểm tra trạng thái indexing: `Normal` (xanh lá cây), không bị lỗi hay `Stale`.
3. **Auto-Indexes:**
   Quan sát các chỉ mục có tiền tố `Auto/...` do RavenDB tự động tạo khi chạy truy vấn lọc:
   - `Auto/Customers/ByPhone`
   - `Auto/Appointments/ByCustomerIdAndStatus`
   - `Auto/Feedbacks/ByRating`

---

## 6. Thao Tác Thêm Mới & Chỉnh Sửa Dữ Liệu Trong Studio

1. **Tạo tài liệu mới bằng tay:**
   - Trong màn hình **Documents**, bấm nút **New Document**.
   - Dán nội dung JSON vào trình soạn thảo.
   - Nhập Document ID (ví dụ: `customers/003`) hoặc để trống để RavenDB tự sinh ID ngẫu nhiên.
   - Bấm **Save** (`Ctrl + S`).
2. **Xóa tài liệu:**
   - Mở tài liệu cần xóa, bấm nút **Delete** ở thanh công cụ trên cùng và xác nhận.

---

## 7. Sao Lưu và Phục Hồi Dữ Liệu

- **Xuất dữ liệu (Export):** Vào **Tasks** -> **Export Database** -> Chọn Collections muốn xuất -> Bấm **Export Database** để tải về file `.ravendbdump`.
- **Nhập dữ liệu (Import):** Vào **Tasks** -> **Import Database** -> Chọn file dump cần nạp -> Bấm **Import Database**.
