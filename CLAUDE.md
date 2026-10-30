# BunDauPeo

Game Unity 6 (2D, URP). Mọi thông tin chi tiết nằm trong `docs/` — file này chỉ là cổng vào.

## Cách làm việc với tài liệu

1. Bắt đầu từ [docs/README.md](docs/README.md) (bảng điều hướng: task nào → đọc file nào).
2. **Chỉ đọc file mà task cần.** Không đọc toàn bộ `docs/`.
3. Trước khi sửa code: tìm bài học liên quan, ví dụ `Grep "#physics" docs/lessons/lesson.md` (tag liệt kê ở đầu file).
4. Khi agent làm sai hoặc user sửa lại: ghi 1 mục vào [docs/lessons/lesson.md](docs/lessons/lesson.md) **ngay trong task đó**.
5. Khi task làm thay đổi sự thật đã ghi trong docs: sửa đúng file đó (xem [docs/guides/doc-maintenance.md](docs/guides/doc-maintenance.md)).
6. Khi gọi subagent: chọn model và effort theo [docs/guides/agent-routing.md](docs/guides/agent-routing.md).

## Luật cứng (luôn áp dụng)

- Không sửa/xoá tay file `.meta`; không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`.
- Không tự đổi hướng game so với [docs/vision/vision.md](docs/vision/vision.md); muốn đổi thì hỏi user trước.
- Tài liệu viết tiếng Việt; tên class/biến/file code viết tiếng Anh.
- Không commit/push khi user chưa yêu cầu.
