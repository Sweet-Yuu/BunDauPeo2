# Roadmap

> **Đọc khi:** bắt đầu phiên làm việc, chọn task tiếp theo.
> **Cập nhật khi:** hoàn thành / bắt đầu / thêm / bỏ một task. Giữ ngắn.

## Đang làm

(trống)

## Tiếp theo

Mỗi mục có spec và kế hoạch riêng, kèm UI tối thiểu để thử được.

- [ ] Restaurant: simulation, khách và món, xếp nhân viên, nâng cấp, AFK.
- [ ] Combat: lượt, 3 hành động, quái, wave, màn chơi, phần thưởng; class và đội hình 3 vị trí (front, mid, back), xem [game-design](design/game-design.md#nhân-vật).
- [ ] Nâng sao nhân vật (sau Combat): dùng mảnh từ Gacha, tối đa 5★, chỉ số theo sao. User chốt vị trí này 2026-10-31; `TODO(user)` giá mảnh mỗi sao và ảnh hưởng chỉ số, chốt trong spec riêng. [Vision](vision/vision.md) đã cập nhật ngày 2026-10-31.
- [ ] UI hub: khung và thanh điều hướng dưới, nối các tính năng. Kèm nút "Reset dữ liệu" trong game, gọi `GameSession.ResetToDefaults()` sau hộp xác nhận ([D-005](decisions/decisions.md)).
- [ ] Kiểm save trên Android thật (`File.Replace`, lưu khi vào nền): user tạm hoãn (2026-11-01), làm sau khi user nối điện thoại.
- [ ] Cốt truyện và hội thoại (sau MVP, user chốt 2026-11-01). Chưa có spec; ảnh tham khảo ở [ui-screens.md](references/ui-screens.md).

## Đã xong

- 2026-10-31 — Gacha: banner, roll, đảm bảo (pity), nhân vật trùng thành mảnh, màn thử (`Gacha.unity`): [spec](superpowers/specs/2026-10-31-gacha-design.md), [kế hoạch](superpowers/plans/2026-10-31-gacha.md). 148 test EditMode xanh; nhánh `feature/gacha`, đã qua review độc lập cuối nhánh.
- 2026-11-01 — Gacha reveal: cutscene, từng nhân vật lóe sáng, SSR cuối không bỏ qua, trang tổng 5 + 5, popup quay tiếp, lịch sử quay, số sao khởi điểm: [spec](superpowers/specs/2026-10-31-gacha-reveal-design.md), [kế hoạch](superpowers/plans/2026-10-31-gacha-reveal.md). 185 test EditMode xanh.
- 2026-10-30 — Nền móng kiến trúc + SO nhân vật (Core, Wallet, SaveService, Roster, Boot, asset mẫu): [spec](superpowers/specs/2026-10-30-foundation-architecture-design.md), [kế hoạch](superpowers/plans/2026-10-30-foundation.md).
- 2026-10-30 — Chốt vision, luật chơi, kiến trúc và spec nền móng.
- 2026-10-30 — Dựng project Unity 6 (2D URP) + bộ docs.
