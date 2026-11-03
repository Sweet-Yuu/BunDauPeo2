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
