# Studio: các kênh chat và cách chúng làm việc với nhau

> **Đọc khi:** mở một kênh (session) mới, điều phối task, hoặc không biết kênh nào làm việc gì.
> **Cập nhật khi:** user đổi cách vận hành studio (vai, icon, luật chia phiên).

## Các vai

Desktop app không cho đặt ảnh logo riêng cho session, nên mỗi kênh mang **icon emoji ở đầu tên**. Nhìn sidebar là biết kênh làm gì.

| Icon | Vai | Mở bằng | Ai mở | Model gợi ý | Được ghi |
|---|---|---|---|---|---|
| 🎯 | **Điều phối**: giữ danh sách task, chia phiên, mở kênh con, kiểm kết quả | `/coordinator` | user | Sonnet 5.5 medium | chỉ [history.md](history.md) (📋 commit) |
| 📋 | **Thư ký**: commit + push khi worker báo xong; báo cáo đã làm gì, đang làm gì, làm gì tiếp, chỗ lệch | `/secretary` | user, điều phối | Sonnet 5.5 low | chỉ git (commit, push); không sửa file |
| 🎨 | **Game designer**: bàn ý tưởng với user, ghi và sửa sổ ý tưởng, vẽ luồng màn từ Lobby (user lập 2026-11-09) | `/designer` | user, điều phối | Opus 5.5 high | `docs/design/screen-flow.md`, `docs/design/ideas.md`, game-design và vision khi user chốt, `ui-screens.md` |
| 📐 | **Thiết kế**: brainstorm với user, viết spec và kế hoạch | `/worker` | điều phối | Opus 5.5 high | spec, plan, docs liên quan |
| 🔨 | **Code**: làm Task a–b của một kế hoạch, có thể chạy subagent song song | `/worker` | điều phối | Sonnet 5.5 medium/high | code, test, roadmap, lesson |
| 🔍 | **Review**: review độc lập cuối nhánh | `/worker` | điều phối | Opus 5.5 high/xhigh | lesson, ghi chú review |

Tên kênh con: `<icon> <Việc> <phần>/<tổng> (Task a–b)`, ví dụ `🔨 Song ngữ 2/3 (Task 5–8)`. Tên kênh chính: `🎯 Điều phối #N`, `📋 Thư ký`.

Sidebar có hai mục tự tạo: **🎯 BunDauPeo · Điều phối** (kênh điều phối và thư ký) và **🔨 BunDauPeo · Phiên con** (mọi kênh con). Kênh nào mở xong thì tự đặt tên và tự xếp vào mục của mình.

## Luồng một task

1. 🎯 nạp lịch sử ([history.md](history.md)), đọc [roadmap](../roadmap.md), chọn task kế tiếp theo thứ tự trong "Tiếp theo" rồi hỏi user một câu xác nhận.
2. Task chưa có spec thì mở 📐. User trả lời câu hỏi thiết kế **trong kênh 📐**, không qua điều phối.
3. Có kế hoạch rồi thì 🎯 chia kế hoạch thành các phiên 🔨 (luật ở dưới) và mở **lần lượt từng phiên**: phiên trước báo xong, 📋 commit, user bảo làm tiếp và 🎯 kiểm kết quả rồi mới mở phiên sau.
4. Hết task trong kế hoạch thì mở 🔍. Lỗi review tìm ra được sửa trong một phiên 🔨 mới.
5. Báo user playtest. User đồng ý thì hỏi mở PR vào `main`. Merge xong mới tách nhánh cho task sau.
6. Sau mỗi mốc (mở hoặc xong một kênh con, user chốt một quyết định), 🎯 ghi ngay một dòng vào [history.md](history.md). File nằm trên đĩa nên kênh mới vẫn đọc được dù chưa commit; 📋 commit nó cùng lượt kế tiếp.

## Commit và push

User chốt 2026-11-06: **chỉ 📋 Thư ký commit và push.**

1. Worker (📐 🔨 🔍) làm xong thì viết khối "Báo cáo cuối kênh" và tự nhắn nó cho phiên 📋 (`send_message`). Worker và subagent không commit.
2. 📋 kiểm báo cáo (test xanh, đúng nhánh), add theo đường dẫn, commit, push, rồi ghi trong kênh 📋 những gì đã đổi.
3. 📋 hỏi user "Bắt đầu 🎯 Điều phối làm tiếp?". User đồng ý thì 📋 nhắn 🎯, và 🎯 kiểm rồi mở bước kế.
4. Chưa có phiên 📋 thì 🎯 mở một phiên và chuyển báo cáo sang. Các bước chi tiết ở skill `/secretary`.

## Luật chia phiên

- Một phiên 🔨 làm tối đa **4–5 task** của kế hoạch, hoặc tới lần dừng playtest kế tiếp, tuỳ cái nào đến trước.
- Thấy phiên dài quá (đã bị tóm tắt ngữ cảnh, hoặc sai hai lần cùng một việc) thì dừng ở task xong gần nhất và báo xong (📋 commit) để 🎯 mở phiên tiếp.
- **Một kênh đụng Unity tại một thời điểm**: một project Unity chỉ mở được một Editor hoặc một lệnh batch. Hai phiên 🔨 không bao giờ chạy cùng lúc.
- Kênh con dùng chung thư mục project (`use_worktree: false`). Worktree riêng phải import lại `Library/`, rất chậm.
- Brief của kênh con theo [session-brief.md](../guides/session-brief.md). Kênh con không thấy cuộc trò chuyện của điều phối, nên brief phải đủ để làm.

## Subagent song song trong kênh 🔨

Được dùng `superpowers:dispatching-parallel-agents` khi kế hoạch có **2–3 task độc lập**: không task nào cần kết quả của task kia, không cùng sửa một file, không cùng sửa scene/prefab/asset.

- Subagent chỉ viết code và test của phần mình. Subagent **không chạy Unity** (`unity test`, `unity run`, `unity command`) và **không commit**.
- Kênh 🔨 gom kết quả, đọc diff, rồi tự chạy `unity test` **một lần** cho cả đợt, sửa cho xanh. Kênh 🔨 cũng không commit; 📋 commit khi phiên báo xong.
- Việc đụng scene, prefab, ProjectSettings hay builder scene thì kênh 🔨 tự làm tuần tự, không giao subagent.
- Model và effort của subagent theo [agent-routing.md](../guides/agent-routing.md). Phần tiền, save, gacha, AFK không giao cho Haiku.

## Trạng thái nằm ở đâu

| Cần biết | Xem |
|---|---|
| Task nào xong, đang làm, làm tiếp | [roadmap.md](../roadmap.md) |
| Điều phối đã quyết gì, mở kênh nào, bắt đầu lại từ đâu | [history.md](history.md) |
| Trong một kế hoạch đã xong task nào | checkbox trong `docs/superpowers/plans/` |
| Code thật đã tới đâu | `git log origin/main`, nhánh đang mở |
| Kênh con nào đang chạy | sidebar, mục 🔨 BunDauPeo · Phiên con |
