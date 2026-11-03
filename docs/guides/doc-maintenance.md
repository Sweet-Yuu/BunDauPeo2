# Bảo trì tài liệu

> **Đọc khi:** kết thúc task, không chắc phải cập nhật doc nào; hoặc định tạo file doc mới.

## Cập nhật gì, khi nào

| Sự kiện | Sửa file |
|---|---|
| Agent làm sai / user chỉnh lại | `lessons/lesson.md` (thêm mục, luôn làm) |
| Chốt hướng kỹ thuật, đổi package/setting | `decisions/decisions.md` (+ `project/tech-stack.md` nếu đổi sự thật) |
| Thêm thư mục/quy ước đặt tên | `project/structure.md` |
| Chốt quy ước code mới | `guides/code-style.md` |
| Bắt đầu / xong một task | `roadmap.md` |
| Xong một feature (trước khi hỏi PR) | thêm dòng "Đã xong, đây là lịch sử" vào đầu spec của nó; nâng các lesson của nhánh thành luật (xem `lessons/lesson.md`, mục Cách ghi) |
| Phát hiện lệnh Unity CLI mới/sai, skill Unity cần thêm vào bảng chọn | `guides/unity-workflow.md` |
| Thêm nguồn tham khảo | `references/README.md` |
| Thêm hoặc đổi tên ảnh trong `Mota/` | `references/ui-screens.md` |
| User đổi ý tưởng/phạm vi | `vision/vision.md` — **chỉ khi user yêu cầu** |
| User chốt hoặc đổi luật chơi | `design/game-design.md` |

## Quy tắc

1. Sửa **đúng một** file cho một sự thật; chỗ khác chỉ link.
2. Sửa nhỏ, theo mục (không viết lại cả file). Giữ dòng **Đọc khi / Cập nhật khi** chính xác.
3. Doc mới chỉ tạo khi không file nào hiện có phù hợp; phải thêm vào bảng điều hướng trong `docs/README.md`.
4. File nào dài hơn ~150 dòng → tách theo chủ đề và cập nhật README.
5. Không chắc → ghi `TODO(user)`, không đoán.
