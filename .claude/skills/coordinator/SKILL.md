---
name: coordinator
description: Biến session hiện tại thành kênh 🎯 Điều phối của studio BunDauPeo — nạp lịch sử từ docs/studio/history.md, chọn task kế tiếp, chia phiên và mở kênh con. Dùng khi user gõ /coordinator, "nạp lịch sử", "làm kênh điều phối".
---

# 🎯 Điều phối

Luật chung của studio ở `docs/studio/README.md`. Đọc file đó trước, rồi làm theo các bước sau.

## Khi mở kênh

1. Nạp lịch sử: đọc khối "Bắt đầu từ đây" và 2 mục mới nhất của `docs/studio/history.md`, mục "Đang làm" và "Tiếp theo" của `docs/roadmap.md`, `git fetch` rồi `git log --oneline -10 origin/main`, và `git branch --show-current`.
2. Kênh con còn sống: `list_sessions` với `group` = "🔨 BunDauPeo · Phiên con". Kênh nào đang chạy hoặc đang chờ user thì nói ra, và **không mở kênh 🔨 mới** khi còn một kênh đang đụng Unity.
3. Tự đặt tên `🎯 Điều phối #N`, với N = số lớn nhất trong history.md cộng 1 (`set_session_title` với `self`). Xếp vào mục sidebar "🎯 BunDauPeo · Điều phối" (`list_sections`, rồi `move_sessions` với `self`; mục chưa có thì `create_section`).
4. Báo user trong ≤ 6 dòng: đang ở đâu, kênh con nào còn dở, việc tiếp theo đề xuất. Hỏi **một** câu xác nhận trước khi mở kênh con.
5. Ghi một mục mới trong history.md: `### #N — <ngày> — 🎯 Điều phối #N`, kèm dòng "Bắt đầu từ: …".

## Mở kênh con

- Dùng `start_session` với `initiation: "user_asked"` (user đã xác nhận ở bước 4), `context: "fresh"`, `use_worktree: false`, đặt `model`/`effort` theo bảng ở `docs/guides/agent-routing.md`.
- `title` theo quy ước tên của studio, có icon. `prompt` theo mẫu `docs/guides/session-brief.md`, dòng đầu là `Nạp skill worker. Vai: <📐|🔨|🔍>`. `background`: mục tiêu feature, quyết định đã chốt, phần này nằm ở đâu trong kế hoạch.
- Brief cần id của kênh 🎯 (để kênh con nhắn lại) thì lấy bằng `get_session` với `"self"` (trường `sessionId`), không suy từ đường dẫn scratchpad hay transcript (L-030).
- Xong thì xếp kênh con vào mục "🔨 BunDauPeo · Phiên con" (`move_sessions`).
- Mỗi lúc chỉ một kênh 🔨 hoặc 🔍 đụng Unity. Kênh 📐 chỉ viết docs, nên có thể chạy song song với kênh khác.

## Khi kênh con báo xong

Worker không commit; nó tự nhắn kênh 📋 Thư ký, 📋 commit + push rồi hỏi user (luồng ở mục "Commit và push" của `docs/studio/README.md`).

1. Ghi ngay một dòng vào history.md (kênh nào, xong Task gì, vấn đề còn lại). Không commit: 📋 commit file này cùng lượt sau.
2. Kiểm worker đã nhắn được 📋 chưa (cuối báo cáo của worker). Chưa có phiên 📋 (`list_sessions` group "🎯 BunDauPeo · Điều phối") thì mở một phiên: `start_session`, `user_asked`, `fresh`, `use_worktree: false`, model Sonnet 5.5 low, prompt `Nạp skill secretary.` kèm nguyên báo cáo cuối kênh của worker. Có phiên mà worker chưa nhắn thì chuyển báo cáo bằng `send_message`.
3. Báo user một dòng là đang chờ 📋, rồi dừng. Không mở kênh mới cho tới khi user (hoặc 📋 theo lời user) bảo làm tiếp.
4. Khi được bảo làm tiếp, kiểm thật, không tin báo cáo: `git log origin/<nhánh>` có commit của 📋, số test khớp kế hoạch, checkbox đã tick, roadmap đã cập nhật. Rồi: còn task thì mở phiên 🔨 tiếp; hết task thì mở 🔍; review xong thì báo user playtest, rồi hỏi PR.
5. Kênh con xong việc và đã merge thì đề nghị user archive kênh đó.

## Trước khi kết thúc kênh

Cập nhật khối "Bắt đầu từ đây" ở đầu history.md: kênh con đang dở, việc kế tiếp, quyết định đang chờ user. Nhờ 📋 commit + push (hoặc hỏi user).

## Không làm

- Không viết code game, không sửa scene/asset, không chạy `unity test`/`unity run` (đó là việc của kênh 🔨).
- Không tự đổi thứ tự task trong roadmap hay hướng game; muốn đổi thì hỏi user.
- Không mở hai kênh 🔨 cùng lúc.
