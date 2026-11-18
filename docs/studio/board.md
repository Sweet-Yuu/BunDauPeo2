# Bảng điều hành

> **Đọc khi:** mở kênh 👑 CEO, hoặc cần biết ai đang giữ khoá Unity.
> **Cập nhật khi:** mở/đóng luồng hay Leader, cấp/nhả khoá Unity, user chốt việc giữa các luồng. Chỉ 👑 CEO ghi. Giữ ngắn: chi tiết nằm trong file luồng.

_Cập nhật 2026-11-16 bởi 👑 CEO #1._

## Nền

`main` ở `496da65` (PR #20: mô hình studio mới): Foundation, Gacha, Restaurant, Combat, công cụ thử, layout theo ảnh tham khảo, Build Android thử, Song ngữ, Nâng sao, UI hub 3 đợt, Online đợt 1 (PR #18), Release prep R1 (PR #19). Test: 944 EditMode + 41 PlayMode + `dotnet test` 3/3.

**Lịch** (user chốt 2026-11-16): itch.io giữa T12, đủ tính năng 31/12; CH Play thử kín ~12–13/12 (hạn cứng 15/12), công khai đầu T1/2027. Thứ tự cắt khi trễ: Hộp quà → Nhiệm vụ ngày → dời ngày công khai.

## Khoá Unity

Đang giữ: **không ai**. (O2-d xong `724ffc6`, Editor đã đóng, nhả 2026-11-18.)

## Luồng

| Luồng | File | Leader | Bước kế |
|---|---|---|---|
| 🌐 Online | [streams/online.md](streams/online.md) | 🧭 Online #1 `local_ced3c354-9202-4fa5-a75d-2a562249beef` | Review xhigh xong: 1 Major (`CommandGate` kẹt Busy) + Minor + GC; [PR #22](https://github.com/Sweet-Yuu/BunDauPeo2/pull/22) Online 2 → `main` chờ merge (user chọn mở trước playtest). Còn: playtest, đo Android thật, deploy production |
| 🚀 Phát hành | [streams/release.md](streams/release.md) | chưa mở | R2 khi có khoá upload + icon |
| 🍜 Nội dung game | [streams/content.md](streams/content.md) | chưa mở | sau Online đợt 2 |
| 💰 Kiếm tiền | chưa có | chưa mở | sau 31/12 |

## Kênh đứng lâu

- 👑 CEO #1: `local_f5d6363f-7bba-4fa7-8094-160219c34ff2`.
- 📋 Thư ký #2: mở 2026-11-16 dưới CEO (id trong group `👑 BunDauPeo · CEO`). Thư ký #1 (`local_44aa4230-…`) nghỉ.
- 🎨 Nội dung game · Ý tưởng (trước là 🎨 Game designer): `local_e53e2e08-d782-488d-bfd6-efd6c42a0b13`, sửa `docs/design/*` cho mọi luồng tới khi luồng có designer riêng. Nằm lồng dưới 🎯 Điều phối #2 trong group `🗄️ … Cũ`; agent không tách được, user tách tay (chuột phải → Detach) nếu muốn.
- 📐 Online đợt 2 (`local_c1f2182d-…`): giữ worktree `online-2-plan`, đừng archive trước khi nhánh đó merge.

## Chờ user (giữa các luồng)

- Xem ngưỡng dung lượng/hiệu năng CEO đề xuất ở [performance.md](performance.md); chưa đo lần nào. Đề xuất đo nền trong Editor ngay sau Online O2-a.
- Phát hành: U3 khoá upload, U5 deploy production, U7 icon PNG, U9 thử save trên điện thoại, D-015 → Accepted, tài khoản Play Console (trước 30/11), email hỗ trợ.
- Ảnh `ReferenceImages/hub-missions.jpg`, `ReferenceImages/hub-presents-popup.jpg` trước spec Online đợt 3/4.
- Giá nâng cấp (quán bằng Material, nhân vật bằng Coin) mới nằm ở `ideas.md`, chưa xếp roadmap.

## Lưu ý vận hành

- `unity run` (Rebuild/Setup) hay bị chặn quyền; user duyệt trong kênh 🔨.
- Turn do kênh khác bắt đầu thì chỉ đề xuất thẻ phiên, không tự mở kênh.
- Group `🗄️ BunDauPeo · Cũ (chờ archive)` chứa ~55 phiên cũ; archive dần khi user đồng ý.
- Worktree cũ `.claude/worktrees/jolly-tharp-ad3076` (nhánh `docs/release-prep`, đã merge) có thể dọn.
