# 🌐 Luồng Online

> **Đọc khi:** mở 🧭 Leader Online hoặc kênh con của luồng này.
> **Cập nhật khi:** mở/xong kênh con, user chốt, PR merge. Chỉ Leader Online ghi. Mục "Bây giờ" luôn đúng hiện tại; nhật ký mỗi mốc một dòng, mới nhất trên cùng.

## Bây giờ

- **Spec:** [online-design](../../superpowers/specs/2026-11-13-online-design.md) (user duyệt 2026-11-13). Đợt 2: spec con + kế hoạch 13 task đã merge (PR #21).
- **Bước kế:** user playtest (Task 13 Step 4) rồi merge PR #22 `feature/online-2` → `main`; sau đó báo CEO, quyết đợt 3 (Google + Nhiệm vụ). Cần ảnh `ReferenceImages/hub-missions.jpg`, `hub-presents-popup.jpg` trước spec đợt 3/4.
- **Đã xong:** PR #21 merge `f514fc9`. Code đợt 2 xong, review cuối xhigh xong: O2-a `b34453b`, O2-b `88859a3` (đã deploy `development`, kiểm gọi thật đạt), O2-c `c8f5d2c`, O2-d `724ffc6` (sửa review + GC). **PR #22 đang mở**, chưa merge. Sổ đo GC: phần code ≈ 0 B (nền Editor).
- **Ghi chú merge (đã xong):** nhánh đó sửa `docs/studio/history.md` (khối "Bắt đầu từ đây" và mục #3). `main` đã chuyển sang mô hình mới (history là lưu trữ): giữ nội dung của nhánh trong history.md, không chép lại vào chỗ khác. Nhánh còn ghi `Mota/`; sau merge đổi thành `ReferenceImages/` trong file nhánh mang vào.
- **Lưu ý:** bản đợt 1 (APK cũ, Editor `main` cũ) kẹt ở Tiêu đề sau deploy `development` (hàng rào bản cũ). `production`: chưa deploy, cần user cho phép riêng. Còn treo: playtest user, đo GC và dung lượng trên Android thật (🚀 Phát hành), kiểm Reset mất trả lời trên Cloud Save thật; hộp Mất kết nối khi ServerError lặp chỉ có Thử lại (đúng spec, user quyết sau).
- **Lịch** (user chốt): đợt 2 logic server ~12/12, đợt 3 Google + Nhiệm vụ ~22/12, đợt 4 Hộp quà ~28/12. Cắt khi trễ: Hộp quà → Nhiệm vụ ngày → dời công khai.
- **Đợt 3 (Designer xong 2026-11-21):** spec [online-3-design](../../superpowers/specs/2026-11-21-online-3-design.md) + kế hoạch 12 task [online-3](../../superpowers/plans/2026-11-21-online-3.md), user đã duyệt, nhánh docs `claude/fervent-brattain-b00297` (từ `feature/online-2`), 📋 đang commit. Chốt: liên kết Google (Play Games) + tên đăng nhập/mật khẩu, luôn vào ẩn danh trước; việc ngày thay hoàn toàn; quay miễn phí 1 lượt/ngày; Hộp thư chỉ có thư ghim nhắc liên kết. Thứ tự cắt đề xuất (chờ user duyệt): Hộp thư → quay miễn phí → tên + mật khẩu → Nhiệm vụ ngày → dời công khai; không cắt Google.
- **Cần user cho đợt 3:** merge PR #22 trước; cho phép deploy `development` (Task 5); cho phép thêm plugin Google Play Games + làm xong Play Console/OAuth (Task 10); xem lại D-008 (thể lực vượt trần); bật tên đăng nhập + mật khẩu trên Dashboard.
- **Việc sau 31/12 (user xếp 2026-11-21):** tab Thành tựu (dựa khung Nhiệm vụ đợt 3), cùng IAP và quảng cáo. Khoá Unity sau PR #22: O3-a đi trước Thời tiết.
- **Thứ tự khoá Unity (user chốt 2026-11-21):** O3-a → O3-b → O3-c → Cấp nhân vật #1, #2 → Thời tiết → Cốt truyện → Vẽ lại quán. Luồng Nhân vật cắt nhánh từ code O3 sau O3-c và thêm ExpPotion vào `RewardData`/`StageRewardPayload`/`CompleteStage`: O3 không đổi tên/chữ ký những chỗ đó quá mức cần. `ProtocolVersion` giữ 3 (O3 đặt). Deploy `production` sau cả hai luồng.
- **Chờ user:** playtest rồi duyệt merge PR #22.

## Đã chốt

UGS (Cloud Code .NET 8, assembly `Rules` dùng chung), phải có mạng mới chơi (D-014), server giữ Gem, gacha, thể lực, thưởng màn, nâng sao; quán và Coin tính trên máy; Reset giữ ID, có Xoá tài khoản; bỏ Thông báo tin; IAP và quảng cáo sau 31/12.

## Nhật ký

- 2026-11-21 — User playtest đạt, PR #22 merge `54dd0be`. Cắt `feature/online-3` từ `main`, merge nhánh spec đợt 3 (chưa commit, 📋 commit). CEO cấp khoá Unity cho O3-a.
- 2026-11-21 — Designer đợt 3 xong: spec + kế hoạch 12 task user duyệt. Chờ merge PR #22 rồi xin khoá Unity mở 🔨 O3-a.
- 2026-11-18 — O2-b/c/d xong, review xhigh sửa xong (`724ffc6`). Mở PR #22. Module đã lên `development`.
- 2026-11-16 — O2-a xong, commit `b34453b` (1016 EditMode, 13 dotnet). PR #21 merge `f514fc9`.
- 2026-11-16 — Chuyển sang luồng của mô hình mới. Đợt 1 đã merge (PR #18). Kế hoạch đợt 2 user duyệt, chờ PR.
