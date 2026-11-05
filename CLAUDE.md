# BunDauPeo

Game Unity 6 (2D, URP). Mọi thông tin chi tiết nằm trong `docs/` — file này chỉ là cổng vào.

## Cách làm việc với tài liệu

1. Bắt đầu từ [docs/README.md](docs/README.md) (bảng điều hướng: task nào → đọc file nào).
2. **Chỉ đọc file mà task cần.** Không đọc toàn bộ `docs/`.
3. Trước khi sửa code: tìm bài học liên quan, ví dụ `Grep "#physics" docs/lessons/lesson.md -A 4` (tag liệt kê ở đầu file; thiếu `-A` chỉ ra dòng tiêu đề). Luật code đã chốt nằm ở [docs/guides/code-style.md](docs/guides/code-style.md).
4. Khi agent làm sai hoặc user sửa lại: ghi 1 mục vào [docs/lessons/lesson.md](docs/lessons/lesson.md) **ngay trong task đó**.
5. Khi task làm thay đổi sự thật đã ghi trong docs: sửa đúng file đó (xem [docs/guides/doc-maintenance.md](docs/guides/doc-maintenance.md)).
6. Khi gọi subagent: chọn model và effort theo [docs/guides/agent-routing.md](docs/guides/agent-routing.md).
7. Studio có các kênh 🎯 Điều phối, 📋 Thư ký, 📐 Thiết kế, 🔨 Code, 🔍 Review. Mở kênh bằng `/coordinator`, `/secretary`, `/worker`. Luật ở [docs/studio/README.md](docs/studio/README.md), lịch sử ở [docs/studio/history.md](docs/studio/history.md).

## Công cụ (luôn áp dụng)

- Làm việc với Unity bằng **Unity CLI (`unity`)** là chính; chưa chắc lệnh thì đọc `unity -h` và `unity <lệnh> -h`, không đoán cờ. Bảng lệnh và quy tắc: [docs/guides/unity-workflow.md](docs/guides/unity-workflow.md).
- Việc có skill trong plugin Unity (Editor, UI, sprite, package…) thì nạp skill đó và làm theo; bảng chọn skill ở cùng file. Khi skill mâu thuẫn với docs dự án thì docs thắng, và báo user.
- Thứ tự ưu tiên khi chỉ dẫn chồng nhau: user > docs dự án > skill `superpowers:*` (quy trình spec → kế hoạch → TDD → review) > plugin phong cách (ponytail…). Ví dụ: "làm ít nhất" không được bỏ bước test hay review mà kế hoạch yêu cầu.
- Làm màn hình hoặc layout UI: tra ảnh tham khảo qua [docs/references/ui-screens.md](docs/references/ui-screens.md). Thiếu ảnh thì nói rõ cần ảnh gì để user gửi; ảnh mới để trong `Mota/`, đặt tên theo quy tắc ở file đó.

## Luật cứng (luôn áp dụng)

- Không sửa/xoá tay file `.meta`; không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`, và thư mục riêng của user `Không động tới/`. Một phần luật này đã chặn cứng trong [.claude/settings.json](.claude/settings.json); bị chặn thì đừng tìm đường vòng.
- Không tự đổi hướng game so với [docs/vision/vision.md](docs/vision/vision.md); muốn đổi thì hỏi user trước.
- Tài liệu viết tiếng Việt; tên class/biến/file code viết tiếng Anh.
- Nút Reset Save Data phải xoá **toàn bộ** dữ liệu đã lưu và luôn hỏi xác nhận. Dữ liệu lưu mới chỉ đi qua `PlayerSave`/`SaveService`; luật đầy đủ ở [D-005](docs/decisions/decisions.md).
- UI và hình tạm chỉ dùng hình có sẵn của Unity (vuông, tròn, tam giác…), không tự thêm file ảnh (file user gửi và cho phép dùng thì được). Việc liên quan đến Canvas thì dùng component `Image`; chi tiết ở [docs/guides/unity-workflow.md](docs/guides/unity-workflow.md).
- Chỉ kênh 📋 Thư ký commit và **push** (user chốt 2026-11-06): kênh 📐 🔨 🔍 và subagent không commit; worker xong thì nhắn 📋, 📋 commit + push toàn bộ, tóm tắt thay đổi rồi hỏi user để 🎯 làm tiếp. Không force push. Session khác (🎯, không có vai) chỉ commit/push khi user yêu cầu. Vai của session ghi ở dòng đầu prompt (mẫu: [docs/guides/session-brief.md](docs/guides/session-brief.md)); không ghi thì coi là điều phối. Chi tiết: mục Git trong [docs/guides/unity-workflow.md](docs/guides/unity-workflow.md).
