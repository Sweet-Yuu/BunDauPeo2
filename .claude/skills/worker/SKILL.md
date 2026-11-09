---
name: worker
description: Biến session hiện tại thành kênh con của studio BunDauPeo (📐 Thiết kế, 🔨 Code hoặc 🔍 Review) theo brief của kênh điều phối. Dùng khi prompt bắt đầu bằng "Nạp skill worker" hoặc user gõ /worker.
---

# Kênh con: 📐 Thiết kế, 🔨 Code, 🔍 Review

Luật chung của studio ở `docs/studio/README.md`. Vai và phạm vi nằm ở dòng đầu brief. Brief không ghi vai thì hỏi user, đừng đoán.

## Khi mở kênh

1. Tự đặt tên theo `title` mà điều phối đã đặt; tên chưa có icon thì thêm icon của vai.
2. Đọc `CLAUDE.md` và `docs/README.md`, rồi chỉ đọc các file mà brief nêu.
3. Kiểm nhánh: `git status` và `git branch --show-current`. Nhánh phải đúng nhánh trong brief; chưa có thì tách từ `origin/main` mới nhất.
4. Với 🔨 và 🔍: chạy `unity status`. Có Editor hay lệnh batch của kênh khác đang chạy thì dừng và báo user.

## 🔨 Code

- Làm đúng Task a–b của kế hoạch theo `superpowers:executing-plans` (TDD). Tick checkbox ngay khi xong từng task.
- **Subagent song song** (luật ở mục "Subagent song song" của `docs/studio/README.md`): chỉ giao các task độc lập, không chung file, không đụng scene/asset. Subagent không chạy Unity và không commit. Kênh này đọc diff, chạy `unity test` một lần cho cả đợt, sửa cho xanh.
- **Không commit, không push** (chỉ 📋 Thư ký làm, user chốt 2026-11-06). Được tạo và chuyển nhánh. Cập nhật dòng "Đang làm" trong `docs/roadmap.md`.
- Dài quá hoặc sai hai lần cùng một việc thì dừng ở task xong gần nhất, rồi báo xong như mục cuối để 📋 commit và điều phối mở phiên tiếp.

## 📐 Thiết kế

- Dùng `superpowers:brainstorming` rồi `superpowers:writing-plans`. Câu hỏi thiết kế hỏi thẳng user trong kênh này.
- Kế hoạch phải ghi sẵn cách chia phiên: mỗi phiên 4–5 task hoặc tới lần dừng playtest, và đánh dấu task nào độc lập để chạy subagent song song được.
- Kế hoạch không có bước commit: mỗi task kết thúc bằng test xanh và tick checkbox; 📋 commit khi phiên báo xong.

## 🔍 Review

- Review toàn nhánh so với `origin/main` theo `superpowers:requesting-code-review`, ở mức trong `docs/guides/agent-routing.md`. Phần tiền, save, gacha, AFK review ở mức xhigh.
- Không tự sửa code. Ghi lỗi tìm được thành danh sách để điều phối mở phiên 🔨 sửa. Nâng lesson của nhánh thành luật theo `docs/guides/doc-maintenance.md`.

## Báo cáo cuối kênh (cả ba vai)

Kết thúc bằng khối này, để 📋 commit và điều phối kiểm được. `<session id>` lấy bằng `get_session` với `"self"` (trường `sessionId`), không suy từ đường dẫn scratchpad hay transcript (L-030, L-031).

```
Xong: Task a–b  ·  Nhánh: <nhánh> (chưa commit)  ·  Kênh: <tên kênh> (<session id>)
Test: <N>/<N> xanh (EditMode), <M>/<M> (PlayMode)  ·  Roadmap: đã cập nhật
File đã đổi: <nhóm và đường dẫn chính>
Còn lại / vướng: <…>
Cần user: <câu hỏi, hoặc "không">
```

Rồi gửi nguyên khối cho kênh 📋 Thư ký: `list_sessions` với group "🎯 BunDauPeo · Điều phối", tìm phiên có tên bắt đầu bằng "📋 Thư ký", `send_message` tới id đó. Không thấy phiên 📋 thì ghi "Chưa nhắn được 📋" ở cuối báo cáo để 🎯 mở một phiên.
