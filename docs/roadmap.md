# Roadmap

> **Đọc khi:** bắt đầu phiên làm việc, chọn task tiếp theo.
> **Cập nhật khi:** hoàn thành / bắt đầu / thêm / bỏ một task. Giữ ngắn.

## Đang làm

- Công cụ thử tự động, phiên 2/2 (🔨, Task 6–9), nhánh `feature/playtest-tools`: xong Task 6 (`PlaytestTools` + menu, EditMode 715) Task 7 (`ScreenCapture1080`) và Task 8 (smoke test PlayMode trọn vòng, PlayMode 4/4); **đang dừng playtest sau Task 8**: chờ user xem ảnh `D:/Unity/bdp-shots/smoke-result.png` và thử menu `Tools/BunDauPeo/Playtest/*` trước Task 9 (docs). Kế hoạch: [playtest-tools](superpowers/plans/2026-11-03-playtest-tools.md).

## Tiếp theo

Mỗi mục có spec và kế hoạch riêng, kèm UI tối thiểu để thử được. Đích: bản Android chơi được ngày 31/12/2026.

Ngày 2026-11-03 user đối chiếu master plan (`C:\Users\Admin\Downloads\BunDauPeo_MasterPlan_GoogleSheets.md`, v0.43, viết ngày 21/10 cho bản dự án cũ). Chỉ lấy các phần user chọn bên dưới. Luật đã làm giữ nguyên: chọn chiêu kiểu FGO + AUTO (không đổi sang "hybrid"), quán theo dây chuyền ba vai, chất lượng, AFK. Không lấy: nội tại/phong độ nhân viên, điểm danh, nhiệm vụ ngày/tuần, cấp nhân vật bằng Gold.

- [ ] Công cụ thử tự động (user chốt 2026-11-03: làm ngay sau Combat, trước Song ngữ; **đã có spec và kế hoạch, user duyệt 2026-11-03**: [spec](superpowers/specs/2026-11-03-playtest-tools-design.md), [kế hoạch](superpowers/plans/2026-11-03-playtest-tools.md), 9 task chia 2 phiên 🔨 — Task 1–5, Task 6–9; nhánh code `feature/playtest-tools`): `PlaytestTools` trong assembly Editor (mở Boot, sao lưu/khôi phục save, bật chạy nền rồi hoàn tác ProjectSettings, chụp màn 1920×1080, bấm nút theo tên) và một PlayMode smoke test chạy trọn vòng Boot → bản đồ → đội → trận AUTO → thưởng → nạp lại save bằng save tạm. Mục đích: gom các bẫy ở [unity-workflow](guides/unity-workflow.md) thành code, giảm thời gian mỗi lần playtest.
- [ ] Build Android thử sớm (user chốt 2026-11-03: ngay sau Combat, cùng đợt với công cụ thử): method `BuildAndroid` chạy bằng `-executeMethod`, build lại sau mỗi feature để bắt lỗi riêng của nền tảng trước hạn 31/12. Không thay mục "Build Android, test trên máy thật, Closed Test" ở dưới.
- [ ] Song ngữ Việt–Anh (master plan, user chốt 2026-11-03): mọi chữ trong game có bản tiếng Anh. Làm ngay sau hai việc trên, vì mỗi màn mới làm thêm đều phải sửa lại chữ.
- [ ] Nâng sao nhân vật (sau Combat): dùng mảnh từ Gacha, tối đa 5★, chỉ số theo sao. User chốt vị trí này 2026-10-31; `TODO(user)` giá mảnh mỗi sao và ảnh hưởng chỉ số, chốt trong spec riêng. [Vision](vision/vision.md) đã cập nhật ngày 2026-10-31.
- [ ] UI hub: khung và thanh điều hướng dưới, nối các tính năng. Kèm nút "Reset dữ liệu" trong game, gọi `GameSession.ResetToDefaults()` sau hộp xác nhận ([D-005](decisions/decisions.md)).
- [ ] Thời tiết (master plan): ngẫu nhiên, chỉ giảm lượng khách, mỗi đợt tối đa 30 phút; xác suất tăng sau hơn 90 phút chơi liên tục. Nhỏ, ghép vào mô phỏng quán hiện có.
- [ ] Shipper + nấu ăn (master plan): mở lại game sau khi vắng thì có hàng shipper (tối đa 15), nấu và giao lần lượt với một đồng hồ chung; khoảng 3 món. `TODO(user)` trong spec: cách ghép với AFK hiện có (AFK đang tích cả khi app vào nền, master plan chỉ tính khi tắt hẳn).
- [ ] Nội thất lưới 8×8 (master plan): đặt, xoay nội thất trên lưới hình thoi, kéo camera; 1–2 nội thất thử. `TODO(user)` trong spec: lưới hình thoi ngược với bố cục màn quán user chốt 2026-11-01 (bỏ phòng isometric).
- [ ] Quảng cáo thưởng test (master plan, thuộc Kiếm tiền): ô đầu shop xem quảng cáo nhận Gem, có giới hạn mỗi ngày, ad unit test. Cần UI hub.
- [ ] Kiếm tiền, phần mua thật: gói Gem, thẻ tháng, gói tân thủ, gói nhân vật khởi đầu. User duyệt hướng 2026-11-02 ([D-007](decisions/decisions.md)); master plan để IAP thật **sau 31/12**. `TODO(user)`: nội dung, giá gói và luật Reset Save Data với quyền lợi đã mua, xem [game-design](design/game-design.md#kiếm-tiền).
- [ ] Cốt truyện Chương 1 (master plan, user đưa vào bản 31/12 ngày 2026-11-03, thay "sau MVP"): Péo, Hợp chất X, phe địch toàn tôm; hội thoại thật thay hai nút cốt truyện giữ chỗ của Combat. `TODO(user)`: kịch bản, loại tôm, tên boss. Quái mẫu của Combat (ruồi, chuột, gián) phải đổi sang tôm.
- [ ] Build Android, test trên máy thật, Closed Test: hạn bản chơi được 31/12/2026. Lịch dự kiến các việc còn lại ở sheet master plan (tab *Tiến độ*).
- [ ] Kiểm save trên Android thật (`File.Replace`, lưu khi vào nền): user tạm hoãn (2026-11-01), làm sau khi user nối điện thoại.

## Đã xong

- 2026-11-03 — Quy trình studio: các kênh 🎯 📋 📐 🔨 🔍, skill `/coordinator` `/secretary` `/worker`, [docs/studio](studio/README.md), luật chặn cứng trong `.claude/settings.json`, lesson nâng thành luật. PR #7.

- 2026-11-03 — Combat: lượt, 3 hành động, quái, wave, bản đồ 7 màn, chọn đội, thể lực (trừ khi vào trận), phần thưởng; class và đội hình 3 vị trí: [spec](superpowers/specs/2026-11-02-combat-design.md), [kế hoạch](superpowers/plans/2026-11-02-combat.md). 15 task, 4 session, 692 test EditMode xanh, đã qua review độc lập cuối nhánh (Opus 5.5 xhigh), đã merge vào `main` (PR #5). `TODO(user)`: xác nhận đã làm playtest 3 (trọn vòng bản đồ → đội → trận → thưởng → màn mở → thoát và mở lại vẫn giữ tiến độ và thể lực).

- 2026-11-02 — Restaurant: mô phỏng quán, xếp nhân viên, nâng cấp (UI bản thử), chất lượng, sự cố, AFK, popup AFK và chất lượng, màn quán `Restaurant.unity` bố cục theo ba ảnh tham khảo: [spec](superpowers/specs/2026-11-01-restaurant-design.md), [kế hoạch](superpowers/plans/2026-11-01-restaurant.md). 425 test EditMode xanh; nhánh `feature/restaurant`, đã qua review độc lập cuối nhánh (Opus 5.5 xhigh), user đã thử, đã merge vào `main` (PR #3). Còn `TODO(user)`: UI nâng cấp hoàn chỉnh, cân bằng Coin/Material (spec mục 9), kiểm save trên Android.
- 2026-10-31 — Gacha: banner, roll, đảm bảo (pity), nhân vật trùng thành mảnh, màn thử (`Gacha.unity`): [spec](superpowers/specs/2026-10-31-gacha-design.md), [kế hoạch](superpowers/plans/2026-10-31-gacha.md). 148 test EditMode xanh; nhánh `feature/gacha`, đã qua review độc lập cuối nhánh.
- 2026-11-01 — Gacha reveal: cutscene, từng nhân vật lóe sáng, SSR cuối không bỏ qua, trang tổng 5 + 5, popup quay tiếp, lịch sử quay, số sao khởi điểm: [spec](superpowers/specs/2026-10-31-gacha-reveal-design.md), [kế hoạch](superpowers/plans/2026-10-31-gacha-reveal.md). 185 test EditMode xanh.
- 2026-10-30 — Nền móng kiến trúc + SO nhân vật (Core, Wallet, SaveService, Roster, Boot, asset mẫu): [spec](superpowers/specs/2026-10-30-foundation-architecture-design.md), [kế hoạch](superpowers/plans/2026-10-30-foundation.md).
- 2026-10-30 — Chốt vision, luật chơi, kiến trúc và spec nền móng.
- 2026-10-30 — Dựng project Unity 6 (2D URP) + bộ docs.
