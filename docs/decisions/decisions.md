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
