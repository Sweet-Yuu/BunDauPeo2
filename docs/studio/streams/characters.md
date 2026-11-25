# 🧩 Luồng Nhân vật

> **Đọc khi:** mở 🧭 Leader Nhân vật hoặc kênh con của luồng này.
> **Cập nhật khi:** mở/xong kênh con, user chốt, PR merge. Chỉ Leader Nhân vật ghi.

## Bây giờ

- **Phạm vi:** **Cấp nhân vật** (vào bản 31/12, user chốt 2026-11-21; `vision.md` đã sửa trên nhánh `docs/cot-truyen-spec`). **Trang bị để sau 31/12**: chỉ ghi vào `docs/design/ideas.md`, không spec.
- **Leader:** 🧭 Nhân vật #1.
- **Bước kế:** 🎨 Designer brainstorm với user rồi viết spec + kế hoạch Cấp nhân vật. Câu hỏi then chốt: cấp tăng chỉ số nào; giá bằng tiền gì (Coin tính trên máy, hay Material do server giữ); giới hạn cấp so với sao (Nâng sao).
- **Đọc cho Designer:** `docs/design/game-design.md`, `docs/superpowers/specs/2026-11-08-star-up-design.md`, spec Online đợt 2 (server giữ gì), D-005 (lưu qua `PlayerSave`).
- **Nhánh gốc spec:** `feature/online-2` (PR #22 chưa merge; cấp nhân vật dính server Online). Merge xong thì rebase về `main`.
- **Khoá Unity:** 🔨 chỉ chạy sau khi có kế hoạch và CEO cấp khoá. Hàng chờ hiện tại: Online O3 → Thời tiết → Cốt truyện; Cấp nhân vật xếp sau khi có kế hoạch.

## Đã chốt

- 2026-11-21 — Cấp nhân vật trong 31/12; trang bị sau 31/12 (user).

- 2026-11-21 — Cấp nhân vật (user duyệt spec + kế hoạch): cấp người chơi (EXP = thể lực tiêu, Lv20) + cấp nhân vật (trần = cấp người chơi, +2%/cấp HP/Tấn công/Thủ); chi phí chính thuốc EXP + Coin phụ; thuốc rơi từ màn (qua server) và quán (phụ, có AFK); cấp lưu phía máy cho 31/12, chuyển server **bắt buộc** sau 31/12; số cân bằng tính lại cuối T12; trang bị, nâng skill để sau.
- Spec `docs/superpowers/specs/2026-11-21-character-level-design.md`, kế hoạch `docs/superpowers/plans/2026-11-21-character-level.md` (nhánh `docs/cap-nhan-vat-spec`). 8 task, 2 phiên 🔨 (#1 Task 1–5, #2 Task 6–8), +77 EditMode +1 .NET +3 PlayMode. Cần khoá Unity cả hai phiên.
- **Phối hợp:** server dựng lại + triển khai lại (`ExpPotion`, ProtocolVersion 3, Min giữ 2) xếp cùng Online O3 (🧭 Online); phiên #1 chạm CombatService/RestaurantService/AfkService/StageProgressService; ảnh mô phỏng UI (spec mục 8) phải được user duyệt trước Task 7, 🔨 dừng sau Task 6 nếu chưa; `ideas.md` chưa ghi trang bị và nâng skill.

- 2026-11-21 — Thứ tự khoá Unity (user chốt, qua CEO): O3-a → O3-b → O3-c → **Cấp nhân vật #1 → #2** → Thời tiết → Cốt truyện → Vẽ lại quán. 🔨 #1 cắt nhánh từ nhánh code O3 sau O3-c (hoặc `main` nếu O3 đã merge) vì sửa cùng CompleteStage/GameData. `ProtocolVersion` giữ 3 do O3 đặt; deploy development lại sau Task 6 khi user cho phép. CEO nhắn khi tới lượt khoá.

- 2026-11-21 — User duyệt ảnh mô phỏng UI (bản v2), đã ghi vào spec mục 8; không còn cổng chờ duyệt trước Task 7. Kế hoạch sửa theo lịch khoá: Task 2 chỉ còn test `Version` không đổi; test mới **86 EditMode** + 1 .NET + 3 PlayMode. UI: danh sách nhân vật → bấm thẻ mở panel chi tiết trong scene Nhân vật (tab Cấp, tab Sao, mũi tên chuyển nhân vật, Quay lại); nâng sao chuyển vào tab Sao; không scene mới.

- 2026-11-24 — CEO cấp khoá Unity cho luồng. Online đợt 3 xong code: `feature/online-3` HEAD `11ce1e4` (chưa PR). 🔨 #1 mở sau tin "checkout sạch" của CEO: cắt `feature/character-level` từ `feature/online-3` `11ce1e4`, merge `docs/cap-nhan-vat-spec` vào, `use_worktree: false`. ProtocolVersion giữ 3. Deploy development chỉ khi user cho phép trong kênh 🔨. Xong #1 thì báo CEO giữ khoá sang #2.

- 2026-11-24 — 🔨 #1 (Task 1–5) xong: `5cfb896` trên `feature/character-level` (đã push), EditMode 1231/1231, dotnet 19/19, `GameData.json` đã xuất lại (user cho phép). Chưa deploy server cho `ExpPotion` (🧭 Online làm). Kế: 🔨 #2 Task 6–8 (nối `LevelConfig` vào Boot, UI, PlayMode, docs).

- 2026-11-25 — `ExpPotion` đã deploy development (user bấm Deploy, commit `c58ae1d`); production chưa. 🧭 Online: `local_ced3c354-9202-4fa5-a75d-2a562249beef`. Kế: khi 🔨 #2 báo xong → review cuối nhánh bằng subagent Opus 5.5 xhigh (đụng server/save) → user playtest → xin user mở PR. Khoá Unity giữ tới khi review sửa xong.

- 2026-11-25 — Review cuối nhánh (Opus xhigh, `11ce1e4..c58ae1d`): không Critical; sửa ý 1, 2, 3, 5, 7, 8 → `dcc47cf` (EditMode 1261, PlayMode 60 xanh), đã nhả khoá Unity. **Hoãn:** ý 4 (thưởng lên cấp phát hai lần khi cân bằng lại số → field "cấp cao nhất đã phát thưởng", làm cùng đợt cân bằng cuối T12), ý 6 (Min protocol 2→3, CEO hỏi user), ý 9 (`Restaurant.unity` sinh lại toàn bộ: nhánh khác đụng scene này phải dựng lại bằng builder sau merge). Chi tiết: `side-session-notes/…/review-character-level.md`. Chưa có test PlayMode đi đường thật TeamSelect → Battle → Lobby cho popup lên cấp (chỉ EditMode mô phỏng). Kế: user playtest → mở PR vào `main` sau khi PR #23 merge.

- 2026-11-25 — 🧭 tự playtest theo lời user (Editor của user, `BackupSave`/`RestoreSave`, save đã trả): chọn đội → trận AUTO → popup kết quả có "Stamina used: 10 / Player EXP +10" và thưởng "10 EXP Potion"; EXP nhảy qua ngưỡng Lv2→Lv3 lúc vào trận, **popup lên cấp hiện đúng khi về Lobby** (ý 1 xác nhận bằng đường thật); thưởng +600 Coin ✓; panel nhân vật: trần cấp = cấp người chơi, bấm lên cấp trừ 300 Coin + 6 thuốc, HP 816→832, Attack 82→83, giá kế 8 thuốc + 400 Coin, dòng "Reached Lv 3" còn nguyên sau 12 giây ví đổi (ý 2 ✓). Chưa thử: ngôn ngữ tiếng Việt, Reset Save, tab Sao, thua trận, máy Android thật.

## Nhật ký

- 2026-11-21 — Lập luồng, mở Leader #1, tạo file luồng.
- 2026-11-21 — Mở 🎨 Nhân vật · Spec Cấp nhân vật (`local_c603d818-ad9e-4e41-b46d-72428b1bd8db`, nhánh `docs/cap-nhan-vat-spec` từ `feature/online-2`).
