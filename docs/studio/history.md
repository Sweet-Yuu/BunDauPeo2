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
