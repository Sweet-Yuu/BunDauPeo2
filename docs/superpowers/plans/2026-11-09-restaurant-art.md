# Kế hoạch artwork Restaurant

> Thực hiện trong phiên hiện tại; chia công việc code độc lập khi không chung file. Chỉ phiên chính chạy Unity. Theo dõi kết quả ngay trong file này và handoff.

**Goal:** Làm đẹp ba trang remake và Lobby gốc theo phong cách user chọn.

**Architecture:** Sprite atlas dùng chung qua helper runtime nhỏ; view giữ logic hiện có. Editor tool cập nhật scene/prefab hiện hữu; builder gốc áp dụng skin sau khi dựng để giữ artwork khi các test/setup rebuild.

**Tech Stack:** Unity 6000.3.15f1, uGUI Text/Image, ImageGen, Unity CLI, NUnit.

**Spec:** [Thiết kế](../specs/2026-11-09-restaurant-art-design.md).

## Ràng buộc và review focus

- Chỉ nhánh Hoang; không đổi gameplay/save/cost, không thêm package hay sửa Build Settings.
- Không sửa tay YAML/meta; icon trang trí không chặn pointer.
- Skin phải tồn tại sau runtime Refresh, tab change và builder rebuild.
- Giữ hình ô thật của thiết bị không chữ nhật, ON/OFF và hit surface kéo/thả.
- Giữ label localization gốc; không dùng chữ baked trong ảnh.
- Kiểm màn nhỏ, trạng thái locked/claimed/selected và modal phủ đúng thứ tự.

## Task 1 — Bộ hình và hợp đồng dùng chung

- [x] Tạo hai atlas icon alpha và nền quán Việt, lưu prompt/nguồn cùng manifest tên sprite.
- [x] Nhập bằng Unity TextureImporter, tạo helper `BunDauPeo.UI.RestaurantArt.Icon(string id)` và bảng màu.
- [x] Kiểm atlas đủ sprite và hình không bị cắt.

## Task 2 — Ba trang remake

- [x] Thêm icon, khung, phân cấp chữ và tab cho shell/skill/shop/kitchen; giữ binding.
- [x] Dùng trường icon sẵn có của SkillSO và FacilityItemSO; giữ ô chiếm/hit surface.
- [x] Áp dụng scene và prefab bằng Editor API, cập nhật đường Create cho nội dung runtime.

## Task 3 — Lobby gốc

- [x] Làm đẹp background, phòng, HUD, thanh dưới và trang của quán qua Editor skin helper.
- [x] Gọi helper từ RestaurantSceneBuilder để skin không mất khi dựng lại.
- [x] Không thay bố cục điều hướng hoặc vị trí actor/incident.

## Task 4 — Xác minh và bàn giao

- [x] Thêm check cần thiết cho icon/reference/raycast, chụp ba trang remake và Lobby gốc bằng save tạm.
- [x] Chạy toàn bộ EditMode và PlayMode; đọc XML, xem ảnh runtime và sửa chỗ lỗi.
- [x] Reviewer độc lập kiểm diff; xử lý lỗi chặn.
- [x] Cập nhật handoff, nguồn prompt, roadmap/history, báo cáo rõ file/phạm vi/giới hạn.

## Nhật ký

- 09/11: user duyệt hướng 1 và thêm Lobby gốc. Đã đọc README, CLAUDE.md, studio README, Git/Unity workflow, code-style và lesson UI liên quan. Chưa sửa code/asset ở thời điểm ghi kế hoạch.

- 10/11: đã hoàn tất 4 task. 910/910 EditMode, 24/24 PlayMode; sau sửa capture L-034 chạy lại 24/24 PlayMode. Đã xem ảnh, review độc lập không còn blocker; sửa cache reimport. Chưa APK/màn 4:3/thiết bị; chưa commit/push. File và lệnh tiếp tục trong RestaurantArtHandoff.md.
