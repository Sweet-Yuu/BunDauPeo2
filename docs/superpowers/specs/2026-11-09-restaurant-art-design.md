# Artwork và làm đẹp Restaurant — 09/11/2026

## Yêu cầu đã chốt

User chọn phương án 1 từ ảnh mẫu: quán Việt ấm áp, xanh lá–kem–gỗ, minh họa vẽ tay; áp dụng ba trang remake **Skill tree, Kitchen grid, Shop & daily** và **Restaurant/Lobby gốc**. User yêu cầu đọc README/quy trình Git, báo cáo thay đổi và lưu ngữ cảnh cho chat sau.

## Phương án

- Đã chọn: artwork riêng từng biểu tượng, khung uGUI và chữ thật, giữ chức năng. Dễ chỉnh, đọc rõ, không biến giao diện thành một ảnh phẳng.
- Đã cân nhắc: giao diện ít họa tiết (phương án 2), chỉ đổi màn gốc (phương án 3). User chọn phong cách nhiều minh họa và cả hai khu vực.
- Ảnh mẫu chỉ xác định phong cách. Số tiền, level, đường prerequisite và buff trong ảnh không phải luật chơi: luôn giữ số liệu thật của code hiện tại.

## Thiết kế

- Bộ icon: 9 skill, 7 thiết bị; coin, diamond, nguyên liệu, quà daily, các gói Gold và mâm bún đậu. PNG do ImageGen tạo, alpha thật; cắt sprite bằng TextureImporter của Unity.
- Nền quán Việt nhẹ, chi tiết ở rìa; panel kem che nền sau nội dung. Màu jade cho hành động, tím cho exclusive, vàng cho tiến độ/điểm, xám cho khóa.
- Remake: tiêu đề và tab có phân cấp, card skill có icon và chữ thật; inventory/thiết bị đặt có icon nhưng giữ hình ô chiếm; shop có minh họa riêng và CTA rõ.
- Lobby gốc: giữ phòng nghiêng, vị trí bàn/trạm/người, sáu ô thanh dưới và các trang hiện có; thay phần nhìn của nền, đồ vật, HUD và nút theo cùng phong cách. Không thay luồng màn hoặc thêm chức năng thuộc UI hub B/C.
- Mọi hình trang trí dùng Image, preserveAspect khi phù hợp, raycastTarget=false. Giữ hit surface, tên object/nút, reference và điều kiện bật/tắt.
- Chỉnh scene/prefab bằng Unity Editor API, không sửa tay YAML/meta; cập nhật đường dựng để lần rebuild sau không mất skin. Không thêm package/font.

## Phạm vi và quyết định

- Nhánh: Hoang, baseline 5e4327d, working tree sạch. User lần này cho phép chỉnh Restaurant/Lobby gốc ngoài giới hạn riêng `_Hoang`; chỉ mở rộng cho phần nhìn và tài liệu task này. Không sửa Packages/ProjectSettings/Build Settings.
- Artwork dùng chung đặt ở `_Project/UI/Resources/RestaurantArt`; UI remake tham chiếu helper của assembly UI. Không tạo hai bản ảnh giống nhau.
- Tài liệu chung theo README ở `docs/`, hướng dẫn mở hai UI và bàn giao tại `Assets/_Hoang/Docs/RestaurantArtHandoff.md`.
- Baseline xác minh trong phiên trước: 906 EditMode + 24 PlayMode, 0 failed. Cần chạy lại sau thay đổi.

## Tiêu chí hoàn thành

1. Cả ba trang remake và Lobby gốc dùng artwork thật và có ảnh chụp runtime để đối chiếu.
2. Giữ skill points/prerequisite, thao tác lưới, Daily/Shop/Ad, save/reset, incident và điều hướng hub.
3. Kiểm EditMode, PlayMode dùng save tạm, scene/prefab không missing reference; xem chữ, vùng bấm và trạng thái disabled/locked.
4. Review độc lập cuối diff; ghi lỗi còn lại và giới hạn thực tế. Không tự merge sang main.
