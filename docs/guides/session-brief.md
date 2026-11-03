# Mẫu prompt mở session

> **Đọc khi:** session điều phối chuẩn bị mở hoặc viết prompt cho một session code.
> **Cập nhật khi:** một session code hỏi lại điều mà mẫu lẽ ra phải ghi sẵn.

Session không chia sẻ trí nhớ (xem [agent-routing](agent-routing.md)). Prompt phải đủ để session mới làm mà không cần đọc lại cuộc hội thoại điều phối. Chép khối dưới, điền chỗ `<…>`, xoá dòng không dùng.

```
Vai: session code            (hoặc: session điều phối / việc docs — quyết định luật commit và push ở CLAUDE.md)
Model gợi ý: <Sonnet 5.5 medium | high>   (theo bảng ở docs/guides/agent-routing.md)

Việc: <một câu, kết quả người chơi thấy được>
Nhánh: <feature/xxx>, tách từ <nhánh gốc>
Kế hoạch: docs/superpowers/plans/<file>.md — làm Task <a>–<b>, tick checkbox khi xong từng task
Đọc thêm: <chỉ các file task cần, vd. spec mục 4, game-design#chiến-đấu>
Lesson cần grep: <#script #ui …>

Xong khi:
- Test EditMode xanh, tổng <N> test (số theo kế hoạch)
- <hành vi kiểm được trong Play, nếu task có UI>
- Commit + push từng task; roadmap.md cập nhật dòng "Đang làm"

Dừng và hỏi user khi: kế hoạch sai hoặc thiếu, cần đổi luật chơi, cần thêm package, test đỏ sau 2 lần sửa.
Không làm: <ngoài phạm vi, vd. không đụng Restaurant>
```

Quy ước:
- Một session code đụng Unity tại một thời điểm.
- Đừng chép nội dung kế hoạch vào prompt; trỏ tới file và số task.
- Đổi model phiên chính bằng `/model` trước khi dán prompt nếu khác model đang chạy.
