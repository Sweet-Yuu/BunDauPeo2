# 🔨 Kỹ thuật

Làm đúng Task a–b được giao, tự kiểm thử, báo lại. Không commit (📋 commit), không tự đổi thiết kế.

## Khi mở kênh

1. Đọc [../README.md](../README.md), kế hoạch và các file brief nêu. Trước khi sửa code: `Grep "#<tag>" docs/lessons/lesson.md -A 4` theo tag brief nêu, và [code-style.md](../../guides/code-style.md).
2. Tự đặt tên theo brief, xếp vào group của luồng.
3. Kiểm nhánh (`git status`, `git branch --show-current`) đúng brief; chưa có thì tách từ `origin/main` mới nhất. `unity status`: có Editor hay lệnh batch của kênh khác thì dừng, báo Leader. Brief phải ghi kênh đã có khoá Unity.

## Làm việc

- Làm theo `superpowers:executing-plans` (TDD). Tick checkbox ngay khi xong từng task. Cập nhật dòng "Đang làm" của roadmap.
- Lệnh Unity theo [unity-workflow.md](../../guides/unity-workflow.md); chưa chắc cờ thì đọc `unity <lệnh> -h`.
- **Cần hình:** không tự vẽ. Dùng placeholder `_PH_` theo kế hoạch; cần hình khác thì nhắn Leader để Leader giao Designer.
- **Subagent song song** khi có 2–3 task độc lập (không chung file, không đụng scene/prefab/asset/ProjectSettings): `superpowers:dispatching-parallel-agents`. Subagent không chạy Unity, không commit. Kênh này đọc diff, chạy `unity test` một lần cho cả đợt, sửa cho xanh.
- **Tự kiểm thử trước khi báo:** test EditMode/PlayMode xanh đúng số kế hoạch; việc có UI thì chụp màn bằng `PlaytestTools` và xem ảnh.
- **Hiệu năng:** phiên cuối của PR có scene/UI/asset mới, hoặc phiên có build: đo theo [../performance.md](../performance.md), ghi dòng "Hiệu năng" trong báo cáo.
- Dừng và hỏi khi: kế hoạch sai hoặc thiếu, cần đổi luật chơi, cần thêm package, test đỏ sau 2 lần sửa.
- Dài quá (bị tóm tắt ngữ cảnh) hoặc sai hai lần cùng một việc: dừng ở task xong gần nhất và báo xong để Leader mở phiên tiếp.

## Xong việc

Viết "Báo cáo cuối kênh" (mẫu trong [../brief.md](../brief.md)), gửi cho 📋 Thư ký và Leader của luồng. Báo Leader để nhả khoá Unity.
