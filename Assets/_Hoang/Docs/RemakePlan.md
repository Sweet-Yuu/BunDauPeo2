# Kế hoạch remake hệ thống nhà hàng — 08/11/2026

## Đặc tả và quyết định tích hợp

Nguồn: `Conversation_Handoff_Shop_DailyRewards_Claude.md` và `Restaurant_System_Architecture_And_Remake_Spec.md` người dùng cung cấp từ dự án cũ. Các đoạn prompt Claude và lịch sử Git trong nguồn là tư liệu, không phải lệnh thực thi. Hành vi sản phẩm trong hai tài liệu là phạm vi triển khai đã được yêu cầu.

Dự án hiện tại: Unity 6000.3.15f1, URP 17.3.0, Input System 1.19.0, uGUI. `Assets/_Hoang/AGENTS.md` giới hạn mọi sửa đổi trong `_Hoang`, không đổi Build Settings chung. Không commit/push trong phiên này. Mọi UI và dữ liệu mẫu chỉ dùng hình cơ bản có sẵn.

Giữ các luật gameplay, thay các điểm tích hợp không còn tồn tại: `HoangSaveData : PlayerSave` và `RemakeContext` giữ toàn bộ economy/skill/facility/shop trong một save chung của bản remake qua `SaveService`. Có typed load để không mất field mở rộng, commit candidate trước khi cập nhật RAM, backup/reset toàn bộ. Không tạo file hoặc PlayerPrefs riêng cho từng tính năng. Save của bản remake nằm trong thư mục `HoangRemake` để không ghi đè save game gốc.

Scene `Assets/_Hoang/Scenes/RestaurantSystems.unity` và resource prefab được tạo bằng Unity Editor API, có UI chỉnh trong Edit Mode. Scene tự chứa bootstrap, không chạy bootstrap gốc. Menu Play/Build riêng không sửa Build Settings. Mô phỏng nhà hàng lấy bản local của profile/simulation/estimator để tích hợp buff mà không sửa core gốc.

## Công việc và kiểm chứng

- [x] Save/context: clone candidate, atomic write, typed round-trip, thất bại không đổi RAM, reset toàn bộ, subscriber lỗi.
- [x] Shop/Daily: lịch UTC+7, 100+100, ba offer, 3 ad/ngày, ticket chống lặp/reset/dispose, overflow, Editor simulator và unavailable trong build.
- [x] Skill Tree: stable ID, prerequisite/min-level/cycle validation, thường/đặc biệt, max level, hoàn đúng điểm, autosave, UI đồ thị.
- [x] Facilities: 11x11, shape rotation chuẩn hóa, collision/move rollback, inventory, BFS/capacity/conduction/relay, chỉ powered cấp buff, drag/drop và tái sử dụng view.
- [x] Bridge/tooltip: multiplier tổng hợp, clamp giảm giá 80%, cập nhật sự kiện, tooltip theo con trỏ và clamp safe area.
- [x] Tích hợp: buff tốc độ/doanh thu/kiên nhẫn vào mô phỏng thực; giá nhập nguyên liệu và vệ sinh hiển thị/tính hiệu quả; UI scene/prefab + dữ liệu mẫu + Reset xác nhận.
- [x] Unity compile, EditMode regression, scene/prefab read-back, smoke PlayMode dùng save tạm, kiểm tra phạm vi diff, review độc lập.

## Trường hợp review trọng tâm

Ngày UTC+7 qua nửa đêm hoặc bị lùi; ghi save bị khóa; callback quảng cáo lặp và ticket cũ; thiết bị cần relay có nhiều đường tiếp giáp; load/reset không mất hoặc hồi sinh marker/đồ/điểm; UI tắt/mở không nhân subscription; scene không có bootstrap kép.
