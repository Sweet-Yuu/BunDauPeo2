# Mẫu prompt mở session

> **Đọc khi:** kênh 🎯 Điều phối chuẩn bị mở kênh con (`start_session`: khối dưới là `prompt`).
> **Cập nhật khi:** một kênh con hỏi lại điều mà mẫu lẽ ra phải ghi sẵn.

Session không chia sẻ trí nhớ (xem [studio](../studio/README.md)). Prompt phải đủ để kênh con làm mà không cần đọc lại cuộc hội thoại điều phối. Chép khối dưới, điền chỗ `<…>`, xoá dòng không dùng.

```
Nạp skill worker. Vai: <📐 Thiết kế | 🔨 Code | 🔍 Review>   (worker không commit; chỉ 📋 commit, xem CLAUDE.md)
Tên kênh: <icon> <Việc> <phần>/<tổng> (Task a–b)
Model: <theo bảng ở docs/guides/agent-routing.md>   (truyền vào start_session, không cần /model)
Subagent song song: <Task x và y độc lập, được chạy song song | không>  (luật ở docs/studio/README.md)

Việc: <một câu, kết quả người chơi thấy được>
Nhánh: <feature/xxx>, tách từ <nhánh gốc>
Kế hoạch: docs/superpowers/plans/<file>.md — làm Task <a>–<b>, tick checkbox khi xong từng task
Đọc thêm: <chỉ các file task cần, vd. spec mục 4, game-design#chiến-đấu>
Lesson cần grep: <#script #ui …>

Xong khi:
- Test EditMode xanh, tổng <N> test (số theo kế hoạch)
- <hành vi kiểm được trong Play, nếu task có UI>
- Không commit (📋 commit); roadmap.md cập nhật dòng "Đang làm"
- Kết thúc bằng khối "Báo cáo cuối kênh" của skill worker và nhắn nó cho 📋 Thư ký

Dừng và hỏi user khi: kế hoạch sai hoặc thiếu, cần đổi luật chơi, cần thêm package, test đỏ sau 2 lần sửa.
Không làm: <ngoài phạm vi, vd. không đụng Restaurant>
```

Quy ước:
- Một kênh đụng Unity tại một thời điểm.
- Đừng chép nội dung kế hoạch vào prompt; trỏ tới file và số task.
- Mở tay (không qua `start_session`) thì đổi model bằng `/model` trước khi dán prompt.
