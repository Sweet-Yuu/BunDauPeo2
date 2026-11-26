# Bảng điều hành

> **Đọc khi:** mở kênh 👑 CEO, hoặc cần biết ai đang giữ khoá Unity.
> **Cập nhật khi:** mở/đóng luồng hay Leader, cấp/nhả khoá Unity, user chốt việc giữa các luồng. Chỉ 👑 CEO ghi. Giữ ngắn: chi tiết nằm trong file luồng.

_Cập nhật 2026-11-26 bởi 👑 CEO #1._

## Nền

`main` ở `54dd0be` (PR #22 Online đợt 2 merge 2026-11-21, user playtest đạt; trước đó PR #20 mô hình studio mới): Foundation, Gacha, Restaurant, Combat, công cụ thử, layout theo ảnh tham khảo, Build Android thử, Song ngữ, Nâng sao, UI hub 3 đợt, Online đợt 1 (PR #18), Release prep R1 (PR #19). Test: 944 EditMode + 41 PlayMode + `dotnet test` 3/3.

**Lịch** (user chốt 2026-11-16): itch.io giữa T12, đủ tính năng 31/12; CH Play thử kín ~12–13/12 (hạn cứng 15/12), công khai đầu T1/2027. Thứ tự cắt khi trễ: Hộp quà → Nhiệm vụ ngày → dời ngày công khai.

## Khoá Unity

Đang giữ: **🧭 Online · 🔨 O4-b** (cấp 2026-11-26). O4-a xong code Task 1–3 (EditMode 1295, dotnet 22); Task 3 Step 6 (deploy `development` + gọi thật) chờ user cho phép và tạo Custom data `gifts/list` trên Dashboard. PR #24 Cấp nhân vật merge `4cea38c` (user duyệt 2026-11-26). Nhánh `feature/online-4` CEO cắt từ `main` `4cea38c`, checkout chung đang ở nhánh này. Hàng chờ (user chốt 2026-11-25): O4-a → O4-b (spec `cc02734`/`69f6def` trên `claude/gallant-gagarin-27d560`) → Thời tiết → Nhiều chương → Cốt truyện → Vẽ lại quán (user chốt 2026-11-26; Vẽ lại quán cắt trước nếu trễ). Nhiều chương và Cốt truyện chờ spec 🎨; spec chưa xong thì lượt đó nhường cho việc kế.

## Luồng

| Luồng | File | Leader | Bước kế |
|---|---|---|---|
| 🌐 Online | [streams/online.md](streams/online.md) | 🧭 Online #1 `local_ced3c354-9202-4fa5-a75d-2a562249beef` | Đợt 2 merge (PR #22, `54dd0be`). Đợt 3 (Google + Nhiệm vụ): spec + kế hoạch 12 task user duyệt 2026-11-21 (nhánh `claude/fervent-brattain-b00297`); 🔨 O3-a chờ PR #22 merge + khoá Unity, đi trước Thời tiết. Còn: đo Android thật, deploy production |
| 🚀 Phát hành | [streams/release.md](streams/release.md) | chưa mở | R2 khi có khoá upload + icon |
| 🍜 Nội dung game | [streams/content.md](streams/content.md) | 🧭 Nội dung game #1 `local_95aae363-67d6-40fa-8849-673dbe1f854c` (mở 2026-11-21) | Ba spec user duyệt, 🔨 xếp hàng khoá Unity sau Cấp nhân vật, làm lần lượt vì chung PlayerSave/ProjectSetup/RestaurantSceneBuilder: (1) Thời tiết (`docs/thoi-tiet-spec`, 4 task, 2 phiên); (2) Cốt truyện: **tạm dừng** (user đổi cốt truyện 2026-11-26, tóm tắt `docs/design/story-core.md`; hệ thống hội thoại spec cũ mục 3–6 còn dùng; chờ user chia chương rồi 🎨 viết lại); (3) Vẽ lại quán tilemap + Nội thất 8×8 (`31f8ba4` trên `docs/tilemap-noi-that-spec`, 4 task tối thiểu + 2 đầy đủ, 3 phiên; cảnh cũ cất vào `Restaurant/Prefabs/_Old/`). Shipper: chưa spec; user chốt 2026-11-23 chờ O3 xong, CEO ước lượng tốc độ rồi mới quyết mở spec; cắt trước nếu trễ |
| 🧩 Nhân vật | [streams/characters.md](streams/characters.md) | 🧭 Nhân vật #1 `local_6be8b17d-59b0-47a3-bdcd-45aeda92edc8` (mở 2026-11-21) | Cấp nhân vật merge (PR #24, `4cea38c`, 2026-11-26). Chưa thử: tiếng Việt, Reset Save, tab Sao, thua trận, Android thật. Trang bị sau 31/12 |
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
- Online đợt 3 (`bc7b187`, chưa PR), user trả lời trong kênh 🧭 Online: cho phép deploy `development` (Task 5), plugin Google Play Games + Play Console/OAuth (Task 10, dính tài khoản Play Console trước 30/11), xem lại D-008, bật đăng nhập tên + mật khẩu trên Dashboard UGS.
- Đăng nhập Google (O3-c Task 10): user chốt 2026-11-24 dời sang sau, không vá gradle, không lên Unity 6.5 (plugin cần 6.5+). Cần trước CH Play công khai; itch.io 31/12 không cần.
- APK Development 54,3 MB, +18% so với 2026-11-07 (vượt ngưỡng 10%; gồm Online đợt 1–3, chưa tách nguyên nhân): kiểm ở 🚀 Phát hành trước thử kín.
- Thứ tự khoá Unity sau PR #22 (user chốt 2026-11-21): Online O3-a → O3-b → O3-c → Cấp nhân vật #1 → #2 → Thời tiết → Cốt truyện → Vẽ lại quán. Cấp nhân vật cắt nhánh từ nhánh O3 (hoặc `main` sau khi O3 merge) vì sửa cùng `CompleteStage`/`GameData`. Tab Thành tựu: luồng Online, sau 31/12 (user chốt 2026-11-21).
- Cốt truyện mới (2026-11-26): user chốt bản 31/12 giữ chương mới, Chương 0 chia nhiều chapter (khung 6), **bản 31/12 chỉ cần 2–3 chapter tối thiểu**, hệ thống nhiều chapter làm đủ, chapter sau thêm bằng dữ liệu; chờ user viết xong + chia chương rồi 🎨 viết lại spec, sau đó xếp lại vào hàng khoá. `vision.md` dòng Cốt truyện Ch1 (nhánh `docs/cot-truyen-spec`) còn tả cốt truyện cũ, 🎨 sửa. Đổi tên Zola/Y/TikTak xa thương hiệu thật.
- Giá nâng cấp (quán bằng Material, nhân vật bằng Coin) mới nằm ở `ideas.md`, chưa xếp roadmap.

## Lưu ý vận hành

- `unity run` (Rebuild/Setup) hay bị chặn quyền; user duyệt trong kênh 🔨.
- Turn do kênh khác bắt đầu thì chỉ đề xuất thẻ phiên, không tự mở kênh.
- Group `🗄️ BunDauPeo · Cũ (chờ archive)` chứa ~55 phiên cũ; archive dần khi user đồng ý.
- Worktree cũ `.claude/worktrees/jolly-tharp-ad3076` (nhánh `docs/release-prep`, đã merge) có thể dọn.
