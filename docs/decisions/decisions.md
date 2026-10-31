# Quyết định kỹ thuật

> **Đọc khi:** sắp đổi hướng kỹ thuật, hoặc cần biết "tại sao dự án làm thế này".
> **Cập nhật khi:** chốt một lựa chọn có đánh đổi (package, kiến trúc, setting). Thêm mục mới ở **đầu** danh sách; không xoá mục cũ — nếu bị thay thế thì đánh dấu `Superseded by`.

## Mẫu

```
### D-NNN — Tiêu đề (YYYY-MM-DD)
- Bối cảnh:
- Quyết định:
- Lý do / đánh đổi:
- Trạng thái: Accepted | Superseded by D-xxx
```

## Danh sách

### D-005 — Reset Save Data xoá toàn bộ dữ liệu đã lưu, luôn hỏi xác nhận (2026-10-31)
- Bối cảnh: user muốn đưa game về mặc định bằng một nút thay vì xoá file tay, và nút phải có hiệu lực với **mọi thứ đã lưu**, kể cả dữ liệu các tính năng thêm về sau.
- Quyết định: menu Editor `Tools/BunDauPeo/Reset Save Data...` (`ResetSaveData.Run`) và lõi `GameSession.ResetToDefaults()` cho nút trong game (làm ở UI hub). Chưa xác nhận thì không xoá gì. Reset xoá mọi file `save.json.*` (`.bak`, `.tmp`, `.corrupt`) rồi file chính, ghi đè cả `PlayerSave` bằng `new PlayerSave()`, cấp lại đồ game mới, lưu, rồi báo `Wallet.Changed` và `Roster.Changed`.
- Luật cho code về sau (**bắt buộc**):
  1. Dữ liệu cần lưu chỉ nằm trong `PlayerSave` và đi qua `SaveService`. Không dùng `PlayerPrefs` hay file riêng; nếu bắt buộc thì phải thêm vào đường reset và có test.
  2. Field mới của `PlayerSave` phải có giá trị khởi tạo đúng cho game mới, vì reset ghi đè bằng bản mặc định.
  3. Trạng thái chạy nằm ngoài `PlayerSave` (ví dụ simulation của quán, bộ đếm AFK trong bộ nhớ) phải tự reset khi `ResetToDefaults()` chạy; thêm cách nghe sự kiện reset khi các tính năng đó ra đời.
  4. Đang Play thì phải reset qua session đang chạy, không xoá file ngầm, nếu không `OnApplicationQuit` ghi lại dữ liệu cũ.
- Lý do / đánh đổi: phải xoá `.bak`, nếu không `Load` hồi sinh dữ liệu cũ; xoá luôn `.corrupt` nên mất bản cứu hộ cuối, hộp xác nhận nói rõ điều này. File chính xoá sau cùng để lỗi giữa chừng không để lại `.bak` mà mất file chính; xoá thất bại thì bộ nhớ giữ nguyên.
- Trạng thái: Accepted

### D-004 — Company `Sweet-Yuu`, product `BunDauPeo` (2026-10-30)
- Bối cảnh: template đặt `DefaultCompany` / `My project (1)`; `Application.persistentDataPath` (nơi lưu `save.json`) suy ra từ hai tên này.
- Quyết định: `companyName = Sweet-Yuu`, `productName = BunDauPeo`. Save nằm ở `%USERPROFILE%\AppData\LocalLow\Sweet-Yuu\BunDauPeo\` trên Windows. Không đổi Bundle Identifier.
- Lý do / đánh đổi: đổi sau khi có save thật thì save cũ bị bỏ lại ở thư mục cũ, nên chốt trước khi có người chơi. `Setup Project` đặt lại hai giá trị này mỗi lần chạy.
- Trạng thái: Accepted

### D-003 — Offline, mobile màn ngang, không nạp tiền thật (2026-10-30)
- Bối cảnh: game gacha thường cần server để chống gian lận; template đang cho phép xoay cả màn dọc.
- Quyết định: chơi offline, save JSON cục bộ, roll gacha trên máy, không IAP. Player Settings chỉ cho phép màn ngang.
- Lý do / đánh đổi: không cần server và backend. Người chơi có thể sửa save hoặc chỉnh đồng hồ để gian lận, chấp nhận được vì không có tiền thật. Nếu thêm IAP phải xét lại `Wallet`, gacha và save.
- Trạng thái: Accepted

### D-002 — Folder theo tính năng, asmdef riêng, không event bus (2026-10-30)
- Bối cảnh: game gồm nhiều hệ thống làm lần lượt, cần giữ chúng không dính nhau.
- Quyết định: mỗi tính năng một folder và một assembly, chỉ phụ thuộc `Core` và `Characters`. Dùng `event` C# trên service, `ServiceLocator` cho view, không DI container, không event bus chung.
- Lý do / đánh đổi: compiler chặn phụ thuộc chéo. Sau khi đặt `StageDefinition` vào Combat và `UpgradeDefinition` vào Restaurant, không tính năng nào cần nghe tính năng ngang hàng, nên event bus là thừa. Thêm event bus khi xuất hiện nhu cầu thật. Chi tiết: [spec](../superpowers/specs/2026-10-30-foundation-architecture-design.md).
- Trạng thái: Accepted

### D-001 — Dùng Unity 6 + URP 2D + Input System (2026-10-30)
- Bối cảnh: khởi tạo dự án từ template 2D (URP).
- Quyết định: giữ Unity 6000.3.15f1, URP Renderer2D, Input System mới.
- Lý do / đánh đổi: là mặc định của template; không có lý do đổi khi chưa có code.
- Trạng thái: Accepted
