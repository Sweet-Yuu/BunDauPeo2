# Bài học khi remake

Giới hạn `_Hoang/AGENTS.md` không cho sửa `docs/lessons` chung; ghi các bài học phát sinh trong khu vực này.

- Khi remake từ tài liệu bàn giao, không tự thêm màn chơi mẫu ngoài các hệ thống người dùng yêu cầu. Người dùng đã yêu cầu bỏ tab Restaurant được thêm để xem mô phỏng; bỏ cả UI lẫn tick demo chạy ngầm, kiểm chứng bằng service/test thay vì đưa thêm trang vào luồng chơi.
- Reset không được xóa bản hợp lệ cuối trước khi commit default. Phải giữ recovery qua cả lỗi IO lẫn process crash; candidate chưa commit không phải tiến trình được chấp nhận.
- Trước khi tạo thêm SO dùng type core, kiểm các đường discovery `FindAssets(...).Single()`. Cấu hình độc lập của remake cần type riêng thay vì làm sai giả định của hệ gốc.
- Field trạng thái của fixture NUnit phải khởi tạo lại trong SetUp; không giả định NUnit tạo instance test class mới cho từng method.
- Trạng thái Editor cần sống qua domain reload phải dùng SessionState hoặc asset, không giữ riêng bằng static field.
- Log CLI/Unity có thể chứa tham số xác thực khởi chạy. Để log kiểm chứng trong thư mục bỏ qua import/Git và làm sạch token trước khi chia sẻ; không đưa log thô vào Docs.
