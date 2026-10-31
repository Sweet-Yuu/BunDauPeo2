# BunDauPeo

Game Unity 6 (2D, URP). Mọi thông tin chi tiết nằm trong `docs/` — file này chỉ là cổng vào.

## Cách làm việc với tài liệu

1. Bắt đầu từ [docs/README.md](docs/README.md) (bảng điều hướng: task nào → đọc file nào).
2. **Chỉ đọc file mà task cần.** Không đọc toàn bộ `docs/`.
3. Trước khi sửa code: tìm bài học liên quan, ví dụ `Grep "#physics" docs/lessons/lesson.md` (tag liệt kê ở đầu file).
4. Khi agent làm sai hoặc user sửa lại: ghi 1 mục vào [docs/lessons/lesson.md](docs/lessons/lesson.md) **ngay trong task đó**.
5. Khi task làm thay đổi sự thật đã ghi trong docs: sửa đúng file đó (xem [docs/guides/doc-maintenance.md](docs/guides/doc-maintenance.md)).
6. Khi gọi subagent: chọn model và effort theo [docs/guides/agent-routing.md](docs/guides/agent-routing.md).

## Công cụ (luôn áp dụng)

- Làm việc với Unity bằng **Unity CLI (`unity`)** là chính; chưa chắc lệnh thì đọc `unity -h` và `unity <lệnh> -h`, không đoán cờ. Bảng lệnh và quy tắc: [docs/guides/unity-workflow.md](docs/guides/unity-workflow.md).
- Việc có skill trong plugin Unity (Editor, UI, sprite, package…) thì nạp skill đó và làm theo; bảng chọn skill ở cùng file. Khi skill mâu thuẫn với docs dự án thì docs thắng, và báo user.

## Luật cứng (luôn áp dụng)

- Không sửa/xoá tay file `.meta`; không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`.
- Không tự đổi hướng game so với [docs/vision/vision.md](docs/vision/vision.md); muốn đổi thì hỏi user trước.
- Tài liệu viết tiếng Việt; tên class/biến/file code viết tiếng Anh.
- Nút Reset Save Data phải xoá **toàn bộ** dữ liệu đã lưu và luôn hỏi xác nhận. Dữ liệu lưu mới chỉ đi qua `PlayerSave`/`SaveService`; luật đầy đủ ở [D-005](docs/decisions/decisions.md).
- UI và hình tạm chỉ dùng hình có sẵn của Unity (vuông, tròn, tam giác…), không thêm file ảnh. Việc liên quan đến Canvas thì dùng component `Image`; chi tiết ở [docs/guides/unity-workflow.md](docs/guides/unity-workflow.md).
- Session code: commit và **push** sau mỗi nhiệm vụ nhỏ đã xong và sau mỗi task lớn. Không force push. Session điều phối và việc không phải code chỉ commit/push khi user yêu cầu. Chi tiết: mục Git trong [docs/guides/unity-workflow.md](docs/guides/unity-workflow.md).
