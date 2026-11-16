# Mẫu prompt mở kênh và mẫu báo cáo cuối kênh

> **Đọc khi:** 👑 CEO mở Leader, hoặc 🧭 Leader mở Designer/Kỹ thuật (`start_session`: khối dưới là `prompt`); hoặc khi một kênh xong việc.
> **Cập nhật khi:** một kênh con hỏi lại điều mà mẫu lẽ ra phải ghi sẵn.

Session không chia sẻ trí nhớ. Prompt phải đủ để kênh con làm mà không đọc lại cuộc hội thoại của kênh mở nó. Đừng chép kế hoạch vào prompt; trỏ tới file và số task.

## Mẫu prompt

```
Vai: <🧭 Leader | 🎨 Designer | 🔨 Kỹ thuật> · Luồng: <Online | Phát hành | …>
Nạp skill <leader | designer | engineer>.   (luật: docs/studio/README.md; chỉ 📋 commit)
Tên kênh: <theo README mục Sidebar>   ·   Group sidebar: <icon> BunDauPeo · <Luồng>
Kênh mở tôi: <tên> (<session id lấy bằng get_session "self">)   ·   📋 Thư ký: <session id>
Khoá Unity: <đã cấp cho kênh này | không cần (chỉ docs)>
Subagent song song: <Task x và y độc lập | không>

Việc: <một câu, kết quả người chơi thấy được>
Nhánh: <feature/xxx>, tách từ <nhánh gốc>
Kế hoạch: docs/superpowers/plans/<file>.md — Task <a>–<b>, tick checkbox khi xong từng task
Đọc thêm: <chỉ các file task cần>
Lesson cần grep: <#script #ui …>

Xong khi:
- Test EditMode <N>, PlayMode <M> xanh (số theo kế hoạch)
- <hành vi kiểm được trong Play, nếu có UI>
- Gửi "Báo cáo cuối kênh" cho 📋 và kênh mở tôi

Dừng và hỏi user khi: kế hoạch sai hoặc thiếu, cần đổi luật chơi, cần thêm package, test đỏ sau 2 lần sửa.
Không làm: <ngoài phạm vi>
```

Prompt mở Leader chỉ cần 4 dòng đầu, dòng "Việc" (bước kế của luồng) và "Đọc thêm: docs/studio/streams/<luồng>.md".

Việc có UI thì nhắc: ảnh mô phỏng được user duyệt trước khi dựng; object chưa có hình đặt tên `<Tên>_PH_<rộng>x<cao>`.

## Báo cáo cuối kênh

```
Xong: <Task a–b | việc>  ·  Nhánh: <nhánh> (chưa commit)  ·  Kênh: <tên> (<session id>)
Test: <N>/<N> EditMode, <M>/<M> PlayMode  ·  Roadmap: đã cập nhật
Hiệu năng: <màn: ms/khung, GC/khung; dung lượng build | không áp dụng> (ngưỡng ở docs/studio/performance.md)
File đã đổi: <nhóm và đường dẫn chính>
Còn lại / vướng: <…>
Cần user: <câu hỏi, hoặc "không">
```

Gửi nguyên khối bằng `send_message` cho 📋 Thư ký và kênh mở mình (id trong brief). Không có id 📋 thì tìm trong group `👑 BunDauPeo · CEO`; không thấy thì ghi "Chưa nhắn được 📋" để Leader báo CEO.
