# 🍜 Luồng Nội dung game

> **Đọc khi:** mở 🧭 Leader Nội dung game hoặc kênh con của luồng này.
> **Cập nhật khi:** mở/xong kênh con, user chốt, PR merge. Chỉ Leader Nội dung game ghi.

## Bây giờ

- **Phạm vi:** các mục gameplay trong roadmap "Tiếp theo": Thời tiết, Shipper + nấu ăn, Nội thất lưới 8×8, Cốt truyện Chương 1; và sổ ý tưởng `docs/design/ideas.md` (ví dụ giá nâng cấp quán bằng Material, nhân vật bằng Coin).
- **Leader:** 🧭 Nội dung game #1 (`local_95aae363-67d6-40fa-8849-673dbe1f854c`). Giai đoạn này chỉ viết spec (không cần khoá Unity); xin CEO khoá trước khi mở 🔨.
- **Thứ tự spec (user chốt 2026-11-21):** Thời tiết → Cốt truyện Ch1 → Shipper + nấu ăn → Nội thất 8×8. Cắt đầu tiên nếu trễ: Nội thất (lưới thoi ngược bố cục quán đã chốt 01/11), rồi Shipper (vướng AFK + server).
- **Spec đang làm:** Thời tiết — 🎨 `docs/thoi-tiet-spec` (từ `feature/online-2`, vì PR #22 chưa merge).
- **Designer cũ:** 🎨 Game designer (`local_e53e2e08-d782-488d-bfd6-efd6c42a0b13`) vẫn giữ `docs/design/ideas.md`, `screen-flow.md`.

## Nhật ký

- 2026-11-16 — Lập luồng.
- 2026-11-21 — Mở Leader #1; user chốt thứ tự; mở 🎨 Spec Thời tiết.
- 2026-11-21 — Spec + kế hoạch Thời tiết xong, user duyệt (nhánh `docs/thoi-tiet-spec`, chờ 📋 commit): 4 task, 2 phiên 🔨, +39 EditMode. Trạng thái ở `PlayerSave.Weather`, không đổi server. 🔨 làm sau khi PR #22 merge, cần khoá Unity. Kế: spec Cốt truyện Ch1.
