# Khu vực làm việc của Hoang

- Chỉ làm việc trên nhánh Git `Hoang`; chỉ push `HEAD:Hoang`, không push hoặc merge sang nhánh khác.
- Chỉ thêm, sửa, xoá nội dung trong `Assets/_Hoang/`. `Assets/_Hoang.meta` là metadata của chính thư mục này, do Unity sinh và được đưa vào commit cùng thư mục.
- Các thư mục khác, bao gồm `Assets/_Project/`, `Packages/` và `ProjectSettings/`, chỉ được đọc hoặc tham chiếu; không sửa. Không chỉnh Build Settings chung.
- Scene làm việc nằm trong `Assets/_Hoang/Scenes/`, được nhân bản bằng Unity để có GUID riêng. Tham chiếu tới script và asset gốc chỉ được dùng để đọc; cần chỉnh thì tạo bản riêng trong `_Hoang` trước.
- Lưu ý: bản sao scene vẫn có thể gọi luồng chuyển scene hoặc bootstrap gốc. Nhân bản scene không tự cô lập toàn bộ runtime; kiểm tra và xử lý trong `_Hoang` trước khi làm cơ chế cần chạy riêng.
- Không sửa tay `.meta`; để Unity quản lý GUID và metadata.
- Khi hoàn thành một cơ chế: kiểm tra phù hợp, xem diff và xác nhận không có thay đổi ngoài phạm vi; sau đó tự commit các file thuộc `_Hoang` và push lên `origin/Hoang` theo yêu cầu người dùng. Không cần hỏi lại quyền commit/push.
- Không đưa skill cài riêng trên máy trong `.agents/skills/` vào commit.

