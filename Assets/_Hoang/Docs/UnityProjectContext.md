# Ngữ cảnh dự án Unity — 08/11/2026

> Cập nhật mới nhất 10/11/2026: main đã merge vào Hoang (`5e4327d`); game gốc hiện dùng hub A với Boot/Restaurant/Battle. User duyệt artwork cho ba trang remake **và Lobby gốc**, ngoại lệ riêng task cho phép chỉnh phần nhìn `_Project` và dùng ảnh mới. Xem [RestaurantArtHandoff.md](RestaurantArtHandoff.md) trước khi tiếp tục; các ghi chú “chưa commit”, “scene gốc 5 màn” và “không có artwork” dưới đây là lịch sử triển khai 08/11.

- Đã xác nhận project root: `D:/MakeGame/MyGame/GameWithVu/BunDauPeo2`, nhánh `Hoang`, ban đầu working tree sạch.
- Unity 6000.3.15f1, URP 2D 17.3.0, Input System 1.19.0, uGUI 2.0.0, Test Framework 1.6.0.
- Code gốc trong `Assets/_Project`: Core/Characters/Restaurant/Combat/Gacha/Boot/UI, mỗi module có asmdef. GameSession giữ PlayerSave, Wallet và Roster; SaveService ghi temp/replace/backup. UI dùng uGUI Text.
- Scene build gốc: Boot, Gacha, Restaurant, StageSelect, Battle trong `_Project/Scenes`. `_Hoang/Scenes` ban đầu có bản sao năm scene, chưa có hệ thống từ hai tài liệu cũ.
- MCP CoplayDev có trong manifest nhưng không có tool Unity kết nối trong phiên; Unity CLI có sẵn, ban đầu `unity status` không có Editor trả lời. Dùng CLI batch tuần tự để sinh asset/test.
- Quy định: chỉ sửa `_Hoang`, để Unity tạo meta, không đổi Packages/ProjectSettings/Build Settings, không commit/push ngoài yêu cầu trực tiếp. Không dùng Input cũ, không dùng emoji trong UI.
- Bản remake: xem `RemakePlan.md`. Không dùng EconomySaveData/GameSaveCoordinator của dự án cũ vì không tồn tại ở đây.
- Nguồn kiểm tra: CLAUDE.md, `_Hoang/AGENTS.md`, manifest, ProjectVersion, EditorBuildSettings, Core PlayerSave/SaveService/Wallet, Boot GameSession/GameBootstrap, Restaurant Profile/Service/Simulation, docs/guides và docs/references/ui-screens.md.
- Commit nền: `a9027e8`. Bản remake hiện được thêm dưới `_Hoang`, chưa commit/push. Scene chạy: `RestaurantSystems.unity`; cấu hình khởi tạo là type riêng `RemakeNewGameConfig`, không thêm instance của NewGameConfig gốc.
- Giao diện hiện tại theo yêu cầu người dùng: ba tab Skill tree, Kitchen grid, Shop & daily; Skill tree mở mặc định. Đã bỏ tab/UI mô phỏng Restaurant và vòng tick demo tự động, không sinh khách/cộng Gold từ mô phỏng ngầm. Service tích hợp buff `RemakeRestaurant` được giữ để tính buff và kiểm chứng qua API/test.
- Kết quả trước lần bỏ tab Restaurant: 833 EditMode và 14 PlayMode passed. Sau khi bỏ tab: Unity recompile thành công, 2/2 test scene/prefab passed trong Editor đang mở; xem `Validation.md` và `RestaurantRemake.md`.
