# Roadmap

> **Đọc khi:** bắt đầu phiên làm việc, chọn task tiếp theo.
> **Cập nhật khi:** hoàn thành / bắt đầu / thêm / bỏ một task. Giữ ngắn.

## Đang làm

- [ ] Gacha: banner, roll, pity, nhân vật trùng thành mảnh, UI thử. Spec đã duyệt (2026-10-31): [spec](superpowers/specs/2026-10-31-gacha-design.md), [kế hoạch](superpowers/plans/2026-10-31-gacha.md) (9 task, chờ user chọn cách thực thi).

## Tiếp theo

Mỗi mục có spec và kế hoạch riêng, kèm UI tối thiểu để thử được.

- [ ] Restaurant: simulation, khách và món, xếp nhân viên, nâng cấp, AFK.
- [ ] Combat: lượt, 3 hành động, quái, wave, màn chơi, phần thưởng; class và đội hình 3 vị trí (front, mid, back), xem [game-design](design/game-design.md#nhân-vật).
- [ ] Nâng sao nhân vật (sau Combat): dùng mảnh từ Gacha, tối đa 5★, chỉ số theo sao. User chốt vị trí này 2026-10-31; `TODO(user)` giá mảnh mỗi sao và ảnh hưởng chỉ số, chốt trong spec riêng. User tự cập nhật [vision](vision/vision.md) cho khớp (xem mục 9 của spec Gacha).
- [ ] UI hub: khung và thanh điều hướng dưới, nối các tính năng. Kèm nút "Reset dữ liệu" trong game, gọi `GameSession.ResetToDefaults()` sau hộp xác nhận ([D-005](decisions/decisions.md)).
- [ ] Kiểm save trên Android thật (`File.Replace`, lưu khi vào nền): chờ user nối điện thoại, rồi hỏi lại.

## Đã xong

- 2026-10-30 — Nền móng kiến trúc + SO nhân vật (Core, Wallet, SaveService, Roster, Boot, asset mẫu): [spec](superpowers/specs/2026-10-30-foundation-architecture-design.md), [kế hoạch](superpowers/plans/2026-10-30-foundation.md).
- 2026-10-30 — Chốt vision, luật chơi, kiến trúc và spec nền móng.
- 2026-10-30 — Dựng project Unity 6 (2D URP) + bộ docs.
