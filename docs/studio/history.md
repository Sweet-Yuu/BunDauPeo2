# Lịch sử điều phối

> **Đọc khi:** mở kênh 🎯 Điều phối hoặc 📋 Thư ký (`/coordinator`, `/secretary`). Đọc khối "Bắt đầu từ đây" và 2–3 mục mới nhất là đủ.
> **Cập nhật khi:** kênh điều phối mở hoặc đóng một kênh con, user chốt một quyết định, hoặc kết thúc kênh điều phối. Chỉ kênh 🎯 ghi file này. Trạng thái task vẫn nằm ở [roadmap](../roadmap.md); đừng chép lại ở đây.

## Bắt đầu từ đây

_Cập nhật 2026-11-03 bởi 🎯 Điều phối #1._

- **Nhánh `main`** có Foundation, Gacha, Gacha reveal, Restaurant, Combat (PR #5) và quy trình studio (PR #7, `abd34bd`). Nhánh mặc định trên GitHub là `main`. Không có kênh con nào đang chạy.
- **Chưa thử thật:** `/coordinator` trong một session mới, và luật chặn sửa `.meta` của `.claude/settings.json`.
- **Việc tiếp theo** (theo roadmap, user chốt 2026-11-03): (1) Công cụ thử tự động `PlaytestTools` + PlayMode smoke test, (2) Build Android thử sớm, (3) Song ngữ Việt–Anh. Cả ba chưa có spec, nên bắt đầu bằng kênh 📐.
- **Chờ user:** xác nhận playtest 3 của Combat đã làm chưa (Combat merge khi roadmap còn ghi "chờ playtest 3"); mở `Assets/_Recovery/0.unity` xem có việc chưa lưu không.

## Nhật ký (mới nhất trên cùng)

### #2 — 2026-11-03 — 🎯 Điều phối #2

- Bắt đầu từ: `main` ở `67d6c0c`, không có kênh con nào chạy; việc kế tiếp theo roadmap là `PlaytestTools` + PlayMode smoke test (chưa có spec, mở kênh 📐).
- User xác nhận. Mở [📐 Spec Công cụ thử tự động 1/1](#local_88a87be4-081d-4ebd-a39a-1af207417c4b) (Opus 5.5 high, nhánh `docs/playtest-tools-spec`, chỉ docs). Chờ spec và kế hoạch được user duyệt rồi mới mở 🔨.
- 📐 xong: user duyệt spec + kế hoạch (9 task, 2 phiên 🔨: Task 1–5, Task 6–9), commit cuối `30bc401` đã push lên `origin/docs/playtest-tools-spec`. Đã kiểm: 2 commit, file spec/plan có, roadmap trỏ tới. Checkout chung đang ở nhánh này; history.md chưa commit.
- User xác nhận. Mở [🔨 Công cụ thử 1/2 (Task 1–5)](#local_ab30ab5f-241e-4312-ac6f-49b56aeb6eb0) (Sonnet 5.5 high, nhánh `feature/playtest-tools` cắt từ nhánh docs spec, được subagent song song cho Task 2–4).
- 🔨 đang ở Task 1, chưa commit: lượt `unity test --mode PlayMode` đầu tiên treo ~15 phút ở scene Boot (nghi L-009 Editor không focus, hoặc `BootScenePlayMode` ép vào Boot). Kênh chờ timeout 900s rồi xử lý.
- 🔨 qua được Task 1, commit `4dace54` đã push. Đang làm đợt Task 2–5 bằng subagent (BackgroundPlay đã đọc xong, còn chờ SaveBackup), chưa chạy `unity test` cho cả đợt.
- 🔨 phiên 1 xong Task 1–5, commit cuối `520d1f2` trên `origin/feature/playtest-tools` (5 commit, mỗi task một). Đã kiểm: EditMode 712/712 khớp mốc kế hoạch, PlayMode 1/1, checkbox T1–5 đã tick, roadmap "Đang làm" đã cập nhật, `ProjectSettings` sạch. Treo PlayMode gỡ bằng guard `-runTests` ở `BootScenePlayMode` (L-023). Lệch nhỏ: bỏ bước "thấy đỏ" riêng từng task vì chạy cả đợt một lần.
- Ngoài kế hoạch: user cho kênh 🔨 phiên 1 làm thêm panel DEV Cài đặt (+1000 Gem, Reset dữ liệu có xác nhận), nhánh `feature/dev-settings` (= `feature/playtest-tools` + `4783ede`), 715 test, chưa PR. Chưa 🔍 review.
- 2026-11-04 — User xác nhận. Mở [🔨 Công cụ thử 2/2 (Task 6–9)](#local_4041cac9-309e-4944-bf05-e19fb1aade4a) (Sonnet 5.5 high) trên `feature/playtest-tools`, dừng playtest sau Task 8. `feature/dev-settings` để riêng.
- 2026-11-06 — 🔨 phiên 2 xong Task 6–9, commit cuối `6c97132` trên `origin/feature/playtest-tools`. Đã kiểm: 4 commit (mỗi task một), EditMode 715/715, PlayMode 4/4 khớp kế hoạch, roadmap "Đang làm" ghi chờ 🔍. Còn 2 checkbox chưa tick trong kế hoạch (chưa xem là bước nào). Chưa mở 🔍.
- User yêu cầu mới: cài MCP CoplayDev (`unity-mcp`), bỏ MCP Unity cũ, rồi sửa layout game theo `Mota/` bằng MCP mới. Phía Claude, `~/.claude.json` đã trỏ `unity-mcp` sang `mcpforunityserver==10.3.0` (bản CoplayDev); phía Unity chưa có package CoplayDev, `Packages/manifest.json` vẫn có `com.unity.ai.assistant` (MCP cũ). Việc này không có trong roadmap.
- User chốt thứ tự: (1) 🔍 review + PR playtest-tools trước, (2) 🔨 cài MCP CoplayDev trên nhánh mới từ `main`, (3) 📐 rà mọi màn đang có so với `Mota/`, lập danh sách chỗ lệch cho user chọn, rồi chia phiên 🔨. Mở [🔍 Review Công cụ thử 1/1](#local_0dd31cab-06fd-495a-a711-04f1067ccc70) (Opus 5.5 xhigh), review cả `feature/playtest-tools` và commit `4783ede` của `feature/dev-settings` (D-005).
- 🔍 xong, docs review ở `3ad7b3e` (L-023/L-024 nâng thành luật, thêm L-025). 715/715 + 4/4, save thật không đổi. Không lỗi chặn PR. Trung bình: A1 nhánh `Blocked` của `UiDriver` không có test; A2 smoke test chạy trong Editor (không `-runTests`) đụng save thật trước SetUp; B1 nút DEV "Cài đặt" che `MenuButton` của Battle và ô Gem ở StageSelect. Thấp: A3 `interactable` thay `IsInteractable()`, A5 Restore không báo giờ bản sao lưu, B2 luồng xác nhận Reset không có test, B3 Reset lỗi vẫn đóng panel. Chờ user: A6 (`playtest-backup/` có là ngoại lệ D-005 không). dev-settings tách từ `520d1f2`, merge sẽ xung đột `structure.md`/`roadmap.md`, chưa chạy test nhánh đó.
- User chốt: sửa trong một phiên 🔨, gộp một PR (`feature/dev-settings` sau khi merge `feature/playtest-tools` vào) → `main`; A6 giữ `playtest-backup/` khi Reset, ghi ngoại lệ vào D-005/D-009. Mở [🔨 Sửa review Công cụ thử 1/1](#local_cf1944c0-99b5-4d55-9939-9ab61d978653) (Sonnet 5.5 high): A1–A3, A5, A6, B1–B3; A4/A7 chỉ ghi chú.
- 🔨 sửa review xong và (theo luật cũ) tự commit, push, mở [PR #8](https://github.com/Sweet-Yuu/BunDauPeo2/pull/8) `feature/dev-settings` → `main`, đang OPEN. Còn: user playtest tay rồi merge. 🎯 chưa kiểm số test của phiên này.
- User đổi luật Git (2026-11-06): **chỉ 📋 Thư ký commit + push**. Worker xong thì tự nhắn 📋; 📋 commit toàn bộ, ghi thay đổi trong kênh 📋, hỏi user rồi nhắn 🎯 làm tiếp. 📋 dùng Sonnet 5.5 low. Đã sửa: `CLAUDE.md`, skill `coordinator`/`secretary`/`worker`, `docs/studio/README.md` (mục mới "Commit và push"), `unity-workflow.md`, `session-brief.md`, `agent-routing.md`, `doc-maintenance.md`. Chưa commit.
- 📋 commit `77fa6d0` (10 file luật mới) lên `feature/dev-settings`, đã push; user bảo 🎯 làm tiếp. Đã kiểm PR #8: OPEN, MERGEABLE, 22 commit; kết quả test cuối (01:48 ngày 06/11) EditMode 720/720, PlayMode 12/12. Chờ user playtest tay bằng `PlaytestTools` rồi merge PR #8; sau merge mới mở 🔨 cài MCP CoplayDev từ `main`.
- User playtest xong, bảo merge: PR #8 đã merge vào `main` (`6ec636e`). Roadmap "Đang làm" còn ghi chờ playtest, kênh 🔨 kế tiếp sửa. Có thể archive các kênh của Công cụ thử (📐, 🔨 1/2, 🔨 2/2, 🔍, 🔨 sửa review).
- User xác nhận. Mở [🔨 Cài MCP CoplayDev 1/1](#local_7ad0d09b-2ec6-463a-903d-b2ad8659264c) (Sonnet 5.5 medium), nhánh `chore/coplay-mcp` từ `main`: thêm package CoplayDev khớp server 10.3.0, gỡ `com.unity.ai.assistant` nếu không có gì phụ thuộc, sửa docs MCP và roadmap. Xong thì nhắn 📋.
- 🔨 MCP vướng: lệnh `unity command package add` với git URL CoplayDev `#v10.3.0` bị bộ phân loại quyền chặn ("Untrusted Code Integration"). Đã kiểm: tag `v10.3.0` có thật; `com.unity.ai.assistant` không bị package nào phụ thuộc (kể cả `com.unity.pipeline`), gỡ được. Chờ user: tự thêm package qua Package Manager, hoặc thêm luật cho phép trong `.claude/settings.json`.
- User chọn cho phép lệnh. 🎯 thêm đúng một luật allow vào `.claude/settings.json`: `unity command package add --identifier "<URL CoplayDev #v10.3.0>" *`, rồi nhắn 🔨 MCP chạy lại.
- 🔨 MCP xong (chưa commit, đã nhắn 📋): package CoplayDev `v10.3.0` vào, `com.unity.ai.assistant` gỡ, 720/720 + 12/12. Docs: `unity-workflow.md` (mục MCP mới), D-010, `tech-stack.md`, roadmap. Vướng: cầu nối MCP không tự chạy sau khi mở Editor hay nạp lại domain, phải bật bằng `unity command script eval '…StdioBridgeHost.StartAutoConnect()…'`; `run_tests` PlayMode qua MCP kẹt, PlayMode vẫn chạy bằng `unity test`. Chờ 📋 commit.
- 📋 commit `285be2c` lên `origin/chore/coplay-mcp` (8 file), user bảo làm tiếp. Đã kiểm: manifest thêm `com.coplaydev.unity-mcp` `#v10.3.0`, bỏ `com.unity.ai.assistant`; test chạy sau khi đổi package (02:20–02:21 ngày 06/11) EditMode 720/720, PlayMode 12/12. Chưa có PR.
- User bảo mở PR + merge: [PR #9](https://github.com/Sweet-Yuu/BunDauPeo2/pull/9) đã merge vào `main` (`b6eea3d`), không qua 🔍. Kế tiếp theo thứ tự đã chốt: 📐 rà mọi màn đang có so với `Mota/` bằng MCP mới.
- User xác nhận. Mở [📐 Rà layout theo Mota 1/1](#local_4f592dc8-58c0-4251-98d5-f0cee4a76487) (Opus 5.5 high), nhánh `docs/layout-mota-spec` từ `main`: chụp từng màn, bảng chỗ lệch cho user chọn, rồi spec + kế hoạch + mục roadmap. Kênh này đụng Unity (chụp màn), nên không mở 🔨 song song.
- 📐 đã chụp mọi màn đang có, lập bảng 13 chỗ lệch (không chỗ nào đổi luật chơi), save đã khôi phục. Chờ user chọn mục và thứ tự trong kênh 📐.
- 📐 xong, user duyệt; 📋 commit `e49f5ac` lên `origin/docs/layout-mota-spec` (spec, kế hoạch, roadmap, L-026), chưa PR. Đã kiểm: kế hoạch 12 task, 3 phiên 🔨 (A 1–4, B 5–8, C 9–12), dừng playtest cuối mỗi phiên, không đụng tiền/save; roadmap có mục "Sửa layout theo Mota" ở đầu "Tiếp theo". Lệch: "Đang làm" còn ghi cài MCP (PR #9 đã merge), giao 🔨 phiên A sửa. 📋 để lại `history.md` chưa commit.
- User xác nhận. Mở [🔨 Layout Mota 1/3 (Task 1–4)](#local_606f42a0-7173-4025-9b88-108ac3b3f0f7) (Sonnet 5.5 medium), nhánh `feature/layout-mota` từ `origin/docs/layout-mota-spec`; dừng playtest sau Task 4, sửa luôn dòng "Đang làm" của roadmap.
- 🔨 phiên A đang chờ user đóng Editor (lệnh đóng bị chặn); Task 1 đã viết code, EditMode 725/725, Task 2–4 chưa làm.
- 🔨 phiên A xong Task 1–4; 📋 commit gộp `7d98bbd` lên `origin/feature/layout-mota` (25 file, 3 `.meta` mới), chưa PR. Đã kiểm: EditMode 729/729 (khớp mốc cộng dồn trong kế hoạch), PlayMode 12/12; roadmap "Đang làm" đã sửa; thêm L-027 (L-025 lặp lần hai, chờ 🔍 nâng thành luật). Còn: Step 5 "Dừng playtest Phiên A" chưa tick, chờ user xem ảnh `pa-*.png` ở `D:\Unity\bdp-shots\` và nhận xét trong kênh 🔨 A, rồi mới mở phiên B.
- User duyệt ảnh phiên A. Mở [🔨 Layout Mota 2/3 (Task 5–8)](#local_779f7d05-bf5d-449c-86e2-aa871e3ba9ae) (Sonnet 5.5 medium) trên `feature/layout-mota`; tick luôn bước dừng playtest phiên A.
- 🔨 phiên B xong Task 5–8, user duyệt ảnh; 📋 commit `2cb4983` lên `origin/feature/layout-mota` (23 file). Đã kiểm: bước dừng playtest A và B đã tick, roadmap "Đang làm" ở phiên B, EditMode 737/737 + PlayMode 12/12 theo báo cáo. Còn phiên C (Task 9–12); định gộp vào phiên C việc đổi đường dẫn `bdp-*` sang `D:\bdp_BunDauPeo`.
- User xác nhận. Mở [🔨 Layout Mota 3/3 (Task 9–12)](#local_194a0748-9138-44a9-969e-3b36da033a17) (Sonnet 5.5 medium), kèm việc đổi đường dẫn file thử sang `D:\bdp_BunDauPeo` sau khi user duyệt ảnh phiên C.
- 🔨 phiên C xong Task 9–12 + đổi đường dẫn file thử; 📋 commit `eef6ade` lên `origin/feature/layout-mota`. Đã kiểm: `ScreenCapture1080` mặc định `D:\bdp_BunDauPeo\shots`, lệnh trong `unity-workflow.md` và kế hoạch dùng `../../bdp_BunDauPeo/`, `D:\bdp_BunDauPeo\tests.xml` 742/742; kế hoạch chỉ còn bước mở 🔍. Cả 12 task đã commit, chưa PR.
- 🎯 đã chuyển 56 file/thư mục `bdp-*` từ `D:\Unity\` sang `D:\bdp_BunDauPeo\` (`mv -n`, không ghi đè; 9 thư mục `bdp-save-backup*` còn đủ). Việc chờ dưới đây đã xong.
- User xác nhận. Mở [🔍 Review Layout Mota 1/1](#local_84756d26-e4da-4ffb-b91f-3334c8fdbeb9) (Opus 5.5 high) cho toàn nhánh `feature/layout-mota`; kèm nâng L-025/L-027 thành luật nếu đủ điều kiện.
- 🔍 xong: không lỗi chặn PR, không chạm save; 742/742 + 12/12 chạy lại xanh. Nâng L-025, L-027, L-028 thành luật (`code-style.md`, `unity-workflow.md`), chưa commit, đã nhắn 📋. 4 lỗi nhỏ để sau PR: thẻ popup tạm dừng dùng `unit.Hp` thay `ShownHp` (`BattleView.cs:158`); `RewardRow.HasAllReferences()` không được view gọi; `SmokeLoopTests.cs:181` thiếu `GetComponentInParent<Canvas>(true)` (L-013); Task 1 bỏ bước test đỏ. Chờ 📋 commit, rồi user playtest + PR.
- 📋 commit + push `5b49c01` (3 file docs sau 🔍), đã kiểm trên `origin/feature/layout-mota`: nhánh 5 commit so với main, chưa có PR. Chờ user playtest rồi hỏi PR.
- User playtest xong; 🎯 mở PR #10 (`feature/layout-mota` → `main`, chưa merge), đánh dấu spec layout xong, roadmap chuyển sang Đã xong; nhờ 📋 commit 3 file docs.
- User bảo merge: PR #10 đã merge vào `main` ở `810ff45`; checkout chuyển về `main`. Roadmap ghi hash merge (chưa commit, đi cùng commit kế của 📋). Kế tiếp: Build Android thử sớm; tuỳ chọn một phiên 🔨 nhỏ sửa 4 góp ý của 🔍.
- Mở 🔨 Sửa 4 góp ý review layout 1/1 (`local_1e002fa3-5d42-46ee-b3e2-3afdb328ad20`, Sonnet 5.5 medium), nhánh `fix/layout-review-nits` từ `main`: popup tạm dừng dùng `ShownHp`, view gọi `RewardRow.HasAllReferences()`, `GetComponentInParent<Canvas>(true)` ở SmokeLoopTests, kiểm test Task 1 bắt được lỗi.
- 🔨 Sửa 4 góp ý xong, 📋 commit + push `d0f423c` lên `origin/fix/layout-review-nits`; đã kiểm: 743/743 EditMode + 12/12 PlayMode (xml 2026-11-06 15:29Z), diff đúng 4 mục; phá thử Task 1 thì 6 test đỏ. Roadmap/history vẫn chưa commit. Chưa có PR.
- 📋 commit `e6d813b` (roadmap + history); 🎯 merge PR #11 vào `main` ở `4a830b6` (user đã bảo "Mở PR + merge"), checkout về `main`. Kế tiếp theo roadmap: Build Android thử sớm.
- Mở 📐 Build Android thử sớm (`local_f9e00385-05f3-4030-ae37-4b176a0a6842`, Opus 5.5 high), nhánh `docs/build-android-spec` từ `main`: spec + kế hoạch, kiểm module Android trên máy, Player Settings bản thử, APK ra `bdp_BunDauPeo`.
- 📐 xong, user duyệt spec; 📋 commit + push `0bb7a10` lên `origin/docs/build-android-spec` (spec, kế hoạch 4 task 1 phiên 🔨, roadmap, history). Đã kiểm: spec "đã duyệt", kế hoạch 747 EditMode + 12 PlayMode, máy đủ module Android. Chờ user mở 🔨.
- Mở 🔨 Build Android 1/1 (`local_002122b7-67b3-43e2-b10f-62adb4e74ad8`, Sonnet 5.5 medium), Task 1–4, nhánh `feature/build-android` từ `docs/build-android-spec`.
- 🔨 Build Android xong, 📋 commit + push `281dd41` lên `origin/feature/build-android`. Đã kiểm: APK 46 MB ở `D:/bdp_BunDauPeo/android/` (build 20:16→20:23), 747/747 + 12/12 (xml 2026-11-07 13:25Z), kế hoạch tick hết trừ bước user cài thử APK. Editor đang ở target Android. Chờ user mở 🔍.
- Mở 🔍 Review Build Android 1/1 (`local_b076a08f-8a15-464e-9c4d-67c9f004bc88`, Opus 5.5 high) trên `feature/build-android`.
- 🔍 Build Android xong: không lỗi chặn PR; đã kiểm review-android-*.xml 747/747 + 12/12, phá thử 2 chỗ thì 2 test đỏ; 📋 commit `0e2dbcc` (spec Đã xong). Nên sửa: `AndroidBuild.cs:121` log ghi cỡ APK 846 MB thay vì 46 MB. Góp ý: khối `m_BuildTargetBatching` Android trong ProjectSettings, build ghi lại 2 asset URP, `OutputPath` không chặn đường dẫn trong repo.
- User bảo "Sửa rồi mở PR": 🔨 Sửa log cỡ APK (`local_47080955-1cc2-49ec-a62f-961fdbf8afbe`) đổi sang `FileInfo.Length`, 747/747 (edit.xml 13:59Z); 📋 commit `d0ace5b`. 🎯 chuyển Build Android sang Đã xong trong roadmap, mở PR vào `main`.
- 📋 commit `4a7bd76`; user bảo merge: PR #12 đã merge vào `main` ở `c41cc59`, checkout về `main`. Roadmap ghi hash merge (chưa commit). Kế tiếp theo roadmap: Song ngữ Việt–Anh. APK chưa cài thử trên điện thoại.
- Mở 📐 Song ngữ Việt–Anh (`local_8787f0df-e38a-4be6-a717-8f610a723708`, Opus 5.5 high), nhánh `docs/bilingual-spec` từ `main`: rà chữ trong game, chọn package Localization hay bảng tự viết, lưu ngôn ngữ qua `PlayerSave`.
- 📐 Song ngữ xong, user duyệt spec + kế hoạch; 📋 commit + push `4398c1a` lên `origin/docs/bilingual-spec` (kèm roadmap/history của 🎯). Kế hoạch 11 task, 3 phiên 🔨: A 1–3 nền, B 4–7 chuyển chữ + dựng 4 scene, C 8–11 tên SO, nút đổi ngôn ngữ DEV, test chặn chữ viết cứng, dừng playtest ảnh tiếng Anh. Chờ user mở 🔨 A.
- 2026-11-08: mở 🔨 Song ngữ 1/3 (Task 1–3, Sonnet 5.5 high, nhánh `feature/bilingual` từ `docs/bilingual-spec`).
- 🔨 Song ngữ 1/3 xong: 📋 commit + push `89ddd50` lên `origin/feature/bilingual`. 🎯 kiểm: EditMode 764/764, PlayMode 12/12, 12 checkbox phiên A đã tick; thêm L-029 (EditMode không gọi `OnEnable`, gọi bằng reflection). Chờ user mở 🔨 B.
- 2026-11-08: mở 🔨 Song ngữ 2/3 (Task 4–7, Sonnet 5.5 medium, tiếp trên `feature/bilingual`; Task 4–6 được chạy 3 subagent song song).
- 🔨 Song ngữ 2/3 xong (user cho phép `unity run` dựng 4 scene): 📋 commit + push `562027d` lên `origin/feature/bilingual`. 🎯 kiểm: EditMode 777/777, PlayMode 12/12, 16 checkbox phiên B đã tick, 4 scene dựng lại. Ghi chú: subagent viết test cùng lúc với code nên Task 4–6 không quan sát bước đỏ. 5 bản dịch nháp chờ user duyệt ở Task 11. Chờ user mở 🔨 C.
- 2026-11-08: mở 🔨 Song ngữ 3/3 (Task 8–11, Sonnet 5.5 medium, tiếp trên `feature/bilingual`); dừng sau Task 11 cho user duyệt ảnh tiếng Anh và bảng dịch.
- 🔨 Song ngữ 3/3 xong; user duyệt 9 ảnh `bi-*.png` và 5 bản dịch nháp. 📋 commit + push `a0e3fa1` lên `origin/feature/bilingual` (62 file, D-012). 🎯 kiểm: EditMode 784/784, PlayMode 13/13; tick 4 checkbox Task 11 mà 🔨 bỏ sót, sửa dòng "Đang làm". Lưu ý cho 🔍: máy dev để tiếng Anh nên bước Reset trong test PlayMode không phân biệt được trường hợp kẹt English; Task 4–6 không quan sát bước đỏ. Chờ user mở 🔍 review.
- 2026-11-08: mở 🔍 review cuối nhánh `feature/bilingual` (Opus 5.5 xhigh vì đụng save); báo cáo gửi 🎯.
- 🔍 Review Song ngữ xong: EditMode 784/784, PlayMode 13/13; tạm phá 11 chỗ thì 18 test đỏ (Task 4–6 có test thật). **Chặn merge B1:** popup thông tin màn (`StageInfoPopup`) vẫn hiện "Đợt 1…4" khi chọn English, vì builder ghi sẵn chữ Việt và view không ghi lại (🎯 đã đọc code, đúng). Nên sửa: N1 chưa test nào phủ Reset → ngôn ngữ theo máy; N2 `GameLanguage.Current` giữ English sau khi thoát Play trong Editor; N3 `UiText.AllEntries` phụ thuộc thứ tự khởi tạo giữa các file partial. 6 góp ý nhỏ G1–G6. 🔍 không nhắn được 🎯 vì brief ghi sai id (L-030; id đúng của 🎯 #2 là `local_f37812f1-5dc0-4caa-b508-3f456dac0d42`). Chờ user mở 🔨 sửa.
- 2026-11-08: mở 🔨 Sửa review Song ngữ (B1, N1–N3; Sonnet 5.5 high, trên `feature/bilingual`); báo cáo gửi cả 📋 và 🎯.
- 🔨 Sửa review Song ngữ xong (chưa commit, chờ 📋): EditMode 788/788 (+4), PlayMode 13/13. B1 `StageInfoPopup.Open` tự ghi nhãn đợt; N1 `GameSession` nhận `Func<SystemLanguage>`, Reset áp ngôn ngữ máy trước `ResetCompleted` (xoá tạm thì đúng 1 test đỏ); N2 `Editor/LanguageReset.cs` trả Vi khi về Edit Mode; N3 `UiText.AllEntries` tính lười. 🎯 đã đọc diff, đạt. Chưa bấm thử tay trong Editor.
- 📋 commit + push `9bafa66` lên `origin/feature/bilingual` (🎯 kiểm trên origin). Nhánh có 5 commit so với `main`, chưa có PR. Chưa ai bấm thử tay nút đổi ngôn ngữ rồi thoát Play. G1–G6 để sau. Chờ user mở PR.
- 🎯 mở PR #13 (`feature/bilingual` → `main`, 5 commit). Chờ user merge.
- User merge PR #13 → `main` ở `8ec923c`. 🎯 đánh dấu spec Song ngữ đã xong, chuyển roadmap sang "Đã xong"; Đang làm: không có, kế tiếp Nâng sao nhân vật. Các sửa docs này chưa commit (trên `main`).
- 2026-11-08: mở 📐 Nâng sao nhân vật (Opus 5.5 high, nhánh `docs/star-up-spec` từ `main`); báo cáo gửi 📋 và 🎯.
- 📐 Nâng sao xong, user duyệt spec + kế hoạch (🎯 kiểm: spec "đã duyệt", kế hoạch 7 task, 2 phiên 🔨: A 1–3 save/`StarConfig`/`StarService`/nối Combat-Boot, B 4–7 ★ ở thẻ đội + Gacha, màn `Characters.unity` mở từ panel DEV, dừng playtest sau Task 7; dự kiến 831 EditMode + 14 PlayMode). Luật: giá 2/4/6/8 mảnh, +10% HP/Tấn công/Thủ mỗi sao đã nâng, mảnh dư ở 5★ để dành. vision.md đổi (theo 📐: user cho phép). Chưa commit, chờ 📋 commit `docs/star-up-spec`.
- **Việc chờ (user giao 2026-11-06, đã xong):** xong việc sửa layout thì dọn các file/thư mục `bdp-*` ở `D:\Unity\` (log, xml test, `bdp-shots`, `bdp-save-backup*`, script `.sh`) vào `D:\bdp_BunDauPeo` (user tạo, đang trống, 06/11). Kèm sửa đường dẫn mặc định: `ScreenCapture1080.cs` (`../bdp-shots`), lệnh `unity test --output ../bdp-*.xml` trong `unity-workflow.md` và kế hoạch layout (giao một kênh 🔨, 📋 commit). Kế hoạch cũ đã xong thì để nguyên.

### #1 — 2026-11-03 — 🎯 Điều phối #1 (quy trình studio)

- User hỏi agent phải đọc file nào trước khi làm và mình đã cấp quyền gì; trả lời từ `CLAUDE.md`.
- Đánh giá quy trình vibe coding (điểm 7/10) và đề xuất 11 cải tiến. User duyệt: tự làm việc an toàn, chỉ hỏi việc rủi ro.
- Đã làm, commit `5ef826a` trên `docs/workflow-tuning`:
  - `.claude/settings.json`: chặn cứng `.meta`, `Library/`, `Logs/`, `UserSettings/`, `Temp/`, `Không động tới/`, force push, `git add -A`.
  - `.gitignore` thêm `_Recovery`, cấu hình cầu nối CLI, thư mục riêng của user.
  - Nâng 19/22 lesson thành luật ở `code-style.md` và `unity-workflow.md`; thêm `session-brief.md` và sổ chi phí trong `agent-routing.md`.
  - Ghi "đã xong, lịch sử" ở đầu 4 spec cũ; thêm thứ tự ưu tiên chỉ dẫn vào `CLAUDE.md`.
- User quyết:
  - commit lên nhánh docs riêng;
  - chỉ đổi nhánh mặc định sang `main`, không xoá nhánh cũ (user đã tự đổi);
  - giữ nguyên plugin ponytail và i-have-adhd;
  - công cụ thử tự động và build Android làm **ngay sau Combat, trước Song ngữ**.
- Đã xoá `docs/finance/BunDauPeo_DuBao_2027_v2.xlsx` theo yêu cầu, chỉ giữ v3.
- Dựng mô hình studio: các kênh 🎯 📋 📐 🔨 🔍, skill `/coordinator`, `/secretary`, `/worker`, file này, hai mục sidebar. Subagent song song được phép trong kênh 🔨 theo luật ở [README studio](README.md).
- Mở kênh 📋 Thư ký đầu tiên: nạp được skill và báo cáo đúng khung.
- User duyệt, nên đã commit `b797d9c`, mở PR #7 và merge vào `main` (`abd34bd`). Checkout trên máy đã chuyển về `main`.

### #0 — 2026-10-30 → 2026-11-02 — trước khi có file này

Tóm lại từ roadmap, git và danh sách session (chưa có history.md lúc đó):

- Kênh điều phối cũ: "BunDauPeo game producer handoff" (đang ghim trong sidebar).
- Đã làm:
  - Foundation, 30/10;
  - Gacha, 31/10, 148 test;
  - Gacha reveal, 01/11, 185 test;
  - Restaurant, 02/11, PR #3, 425 test;
  - Combat, 02–03/11, 4 session code, PR #5, 692 test.
- Cách làm đã chạy tốt: spec (Opus) → kế hoạch → session code tuần tự (Sonnet) → review độc lập cuối nhánh (Opus xhigh) → user playtest → PR.
