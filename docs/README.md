# Docs — bản đồ điều hướng

Tài liệu được chia nhỏ để agent chỉ nạp đúng thứ cần cho từng task. **Đừng đọc hết.**

## Task nào → đọc file nào

| Task | Đọc (theo thứ tự) |
|---|---|
| Hiểu dự án là gì, đi về đâu | [vision/vision.md](vision/vision.md) |
| Làm / sửa gameplay, tính năng mới | vision → [design/game-design.md](design/game-design.md) → [roadmap.md](roadmap.md) → [project/structure.md](project/structure.md) → [guides/code-style.md](guides/code-style.md) |
| Cân bằng số liệu, hỏi luật chơi | [design/game-design.md](design/game-design.md) |
| Triển khai theo spec / kế hoạch | `superpowers/specs/`, `superpowers/plans/` (xem link trong [roadmap.md](roadmap.md)) |
| Chọn model, effort, subagent hay native | [guides/agent-routing.md](guides/agent-routing.md) |
| Viết hoặc sửa script C# | [guides/code-style.md](guides/code-style.md) → [project/structure.md](project/structure.md) |
| Sửa scene / prefab / asset / ProjectSettings | [guides/unity-workflow.md](guides/unity-workflow.md) |
| Thêm package, đổi render pipeline, đổi input | [project/tech-stack.md](project/tech-stack.md) → [decisions/decisions.md](decisions/decisions.md) |
| Tra tài liệu / link bên ngoài (API, art, âm thanh…) | [references/README.md](references/README.md) |
| Chọn hướng làm, hỏi "sao lại làm vậy?" | [decisions/decisions.md](decisions/decisions.md) |
| Chuyện gì đang làm / làm tiếp gì | [roadmap.md](roadmap.md) |
| Agent vừa làm sai / user vừa sửa | **ghi vào** [lessons/lesson.md](lessons/lesson.md) |
| Không chắc phải cập nhật doc nào | [guides/doc-maintenance.md](guides/doc-maintenance.md) |

## Cấu trúc

```
CLAUDE.md                  cổng vào, luật cứng (ngắn)
docs/
  README.md                file này
  roadmap.md               việc đang làm / sắp làm / đã xong
  vision/vision.md         ý tưởng, mục tiêu, phạm vi dự án
  design/game-design.md    luật chơi: tiền tệ, nhân vật, quán, AFK, chiến đấu, gacha
  superpowers/             spec và kế hoạch triển khai theo từng đợt
  project/
    tech-stack.md          engine, package, pipeline (sự thật kỹ thuật)
    structure.md           cấu trúc thư mục Assets/, quy ước đặt tên
  guides/
    code-style.md          quy ước viết C#
    unity-workflow.md      cách thao tác an toàn với file Unity
    doc-maintenance.md     khi nào đọc / sửa doc nào
    agent-routing.md       chọn model, effort, subagent hay native
  decisions/decisions.md   quyết định kỹ thuật + lý do (ADR gọn)
  references/README.md     mục lục nguồn tham khảo ngoài
  lessons/lesson.md        sổ bài học: lỗi agent đã mắc, đừng lặp lại
```

## Nguyên tắc

- Mỗi file có dòng **Đọc khi / Cập nhật khi** ở đầu — nhìn là biết có cần mở không.
- Một sự thật chỉ ghi ở **một** nơi; chỗ khác chỉ link.
- Mục nào chưa biết ghi `TODO(user)` thay vì đoán.
