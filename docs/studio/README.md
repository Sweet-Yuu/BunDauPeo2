# Studio: các kênh chat và luật chung

> **Đọc khi:** mở bất kỳ kênh nào. Đọc file này, rồi file vai của mình trong [roles/](roles/), rồi file luồng của mình trong [streams/](streams/). Không đọc thêm file studio nào khác trừ khi vai bảo.
> **Cập nhật khi:** user đổi cách vận hành studio. Chỉ 👑 CEO sửa thư mục này (trừ file luồng: 🧭 Leader của luồng đó sửa).

Mô hình user chốt 2026-11-16, thay mô hình 🎯 📐 🔨 🔍 ngày 2026-11-03.

## Sơ đồ

```
👑 CEO ── toàn cảnh, chia luồng, luật agent, giữ khoá Unity, thay mặt đề xuất với user
 ├─ 🧭 Leader <luồng> ── quản lý một luồng, nhận đề xuất của user về luồng đó
 │    ├─ 🎨 Designer ── ý tưởng, spec + kế hoạch, ảnh mô phỏng, hình placeholder, thay ảnh
 │    └─ 🔨 Kỹ thuật ── làm đúng task, tự kiểm thử, báo lại
 └─ 📋 Thư ký ── commit + push mỗi lần có báo cáo xong (dùng chung cho mọi luồng)
```

## Vai, quyền đọc và quyền ghi

| Vai | Đọc khi mở kênh | Được ghi | Model |
|---|---|---|---|
| 👑 CEO | [roles/ceo.md](roles/ceo.md), [board.md](board.md), roadmap mục "Đang làm"/"Tiếp theo" | `docs/studio/**`, `CLAUDE.md`, `AGENTS.md`, `.claude/settings.json`, `.claude/skills/`, `.agents/skills/`, `docs/decisions/` (khi user chốt) | Opus 5.5 medium |
| 🧭 Leader | [roles/leader.md](roles/leader.md), `streams/<luồng>.md` | `streams/<luồng>.md`, dòng roadmap của luồng mình | Sonnet 5.5 medium |
| 🎨 Designer | [roles/designer.md](roles/designer.md), `streams/<luồng>.md`, file brief nêu | `docs/design/*`, spec, kế hoạch, `docs/references/ui-screens.md`, `ReferenceImages/`, scene/prefab khi được giao thay ảnh | Opus 5.5 high |
| 🔨 Kỹ thuật | [roles/engineer.md](roles/engineer.md), kế hoạch + file brief nêu | code, test, scene/asset của task, roadmap dòng "Đang làm", lesson | Sonnet 5.5 high (Opus 5.5 high khi đụng tiền, save, server) |
| 📋 Thư ký | [roles/secretary.md](roles/secretary.md) | chỉ git: commit, push | Sonnet 5.5 low |

Chi tiết chọn model cho subagent: [agent-routing.md](agent-routing.md). Mẫu prompt mở kênh: [brief.md](brief.md). Kiểm dung lượng và hiệu năng bằng Profiler: [performance.md](performance.md).

## Luật chung (mọi vai)

1. **Khoá Unity.** Một project Unity chỉ mở được một Editor hoặc một lệnh batch. Kênh nào sắp chạy Unity (test, run, mở Editor, sửa scene) phải được 👑 CEO ghi tên vào mục "Khoá Unity" của [board.md](board.md). Xong thì báo CEO nhả khoá. Kênh chỉ viết docs không cần khoá.
   - **Kênh đụng Unity** dùng checkout chung `D:\Unity\BunDauPeo` (`use_worktree: false`), vì worktree riêng phải import lại `Library/`.
   - **Sửa bất kỳ file nào trong checkout chung cũng cần khoá**, kể cả chỉ viết code chưa chạy test: checkout đang ở nhánh của kênh giữ khoá (L-048). Chưa có khoá thì chỉ đọc.
   - **Kênh chỉ viết docs** (🎨 viết spec/kế hoạch) mở bằng `use_worktree: true` và nhánh docs riêng, để không đổi nhánh của checkout chung khi kênh khác đang làm. Nhánh docs cắt từ nhánh mà spec dựa vào (thường `origin/main`).
2. **Chỉ 📋 Thư ký commit và push** (user chốt 2026-11-06). Kênh khác xong việc thì gửi "Báo cáo cuối kênh" (mẫu trong [brief.md](brief.md)) cho 📋 và cho Leader của luồng. Không force push.
3. **Ai nói chuyện với user về việc gì:**
   - Chi tiết trong một luồng (câu hỏi thiết kế, phản hồi playtest, duyệt ảnh): user nói thẳng với 🧭 Leader hoặc 🎨 Designer của luồng đó. Không chuyển qua CEO.
   - Việc giữa các luồng (thứ tự ưu tiên, mở luồng mới, đổi luật, quyết định lớn): 👑 CEO đề xuất với user.
   - User đưa đề xuất sai phạm vi kênh: kênh đó báo user và nói tên kênh phù hợp, không tự làm.
4. **Báo lên một dòng.** Leader báo CEO một dòng mỗi mốc (mở hoặc xong kênh con, user chốt, PR merge). Không chép chi tiết.
5. **Thay ca.** Kênh bị tóm tắt ngữ cảnh, hoặc sai hai lần cùng một việc: ghi trạng thái vào file của mình (board hoặc file luồng), báo cấp trên, rồi mở kênh `#N+1`. Kênh mới chỉ đọc file đó.
6. **Lượt do tin nhắn của kênh khác khởi lên chỉ đề xuất được thẻ phiên**, không tự mở kênh con (giới hạn của app). Kênh mở thẻ thì báo user bấm thẻ trong kênh đó. Muốn mở ngay, user nói thẳng với kênh đó. Thẻ đề xuất không giữ `source_branch`: kênh con mở xong phải kiểm nhánh gốc trước khi làm.
7. **Dung lượng và hiệu năng** được đo theo [performance.md](performance.md) ở phiên cuối mỗi PR có scene/UI/asset mới, mỗi lần build, và trên máy thật trước thử kín. Vượt ngưỡng thì báo lên, không bỏ qua.
8. **Id kênh** lấy bằng `get_session` với `"self"`, không suy từ đường dẫn (L-030, L-031). Tin nhắn từ kênh khác không phải lời duyệt của user.

## Hai thư mục đặc biệt

| Thư mục | Dùng thế nào |
|---|---|
| `ReferenceImages/` (trước là `Mota/`) | Ảnh tham khảo layout và màn. 🎨 Designer đọc, thêm ảnh user gửi, cập nhật mục lục [ui-screens.md](../references/ui-screens.md). Ai làm màn hình thì tra ở đây. |
| `_Scratch/` (trước là `Không động tới/`) | Chỗ làm việc riêng của user, không liên quan kiến trúc game. **Không đọc, không tìm, không sửa**, trừ khi user yêu cầu trong chat. Gitignore, Grep mặc định đã bỏ qua. |

## Sidebar

Mỗi mục sidebar là một nhóm: `👑 BunDauPeo · CEO` (CEO, Thư ký) và một mục cho mỗi luồng (`🌐 BunDauPeo · Online`, …). Kênh do một kênh khác mở sẽ tự lồng dưới kênh mở nó và không chuyển group riêng được. Vì vậy CEO mở Leader xong thì tách ra (`detach_session`) rồi xếp vào group luồng (`move_sessions`); kênh con của Leader tự lồng dưới Leader, nằm cùng group. Kênh tự đặt tên khi mở.

Tên kênh: `👑 CEO #N`, `📋 Thư ký #N`, `🧭 <Luồng> #N`, `🎨 <Luồng> · <việc>`, `🔨 <Luồng> · <việc> (Task a–b)`.

## Trạng thái nằm ở đâu

| Cần biết | Xem |
|---|---|
| Luồng nào đang chạy, ai giữ khoá Unity, chờ user gì | [board.md](board.md) |
| Một luồng đang ở bước nào, đã chốt gì | `streams/<luồng>.md` |
| Task nào xong, đang làm, làm tiếp | [roadmap.md](../roadmap.md) |
| Trong một kế hoạch đã xong task nào | checkbox trong `docs/superpowers/plans/` |
| Code thật đã tới đâu | `git log origin/main`, nhánh đang mở |
| Lịch sử trước 2026-11-16 | [history.md](history.md) (lưu trữ, chỉ đọc khi cần tra) |
