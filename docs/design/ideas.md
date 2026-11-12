# Sổ ý tưởng

> **Đọc khi:** muốn biết ý tưởng nào đang bàn, để sau hay đã bỏ; trước khi đề xuất tính năng mới.
> **Cập nhật khi:** user thêm, sửa, hoãn hay bỏ một ý tưởng. Kênh 🎨 Game designer giữ file này. Ý đã chốt thành luật thì chép sang [game-design.md](game-design.md).

Mỗi ý ghi ngày và người đề xuất. Luồng màn ở [screen-flow.md](screen-flow.md).

## Đang bàn

- **Phạm vi online** (user, 2026-11-09). User đã chốt online xong trong bản 31/12, và [vision](../vision/vision.md) cùng roadmap đã sửa theo. Còn phải chốt phạm vi trong spec Online: đăng nhập, lưu trên server, Hộp quà, Thông báo, Nhiệm vụ, Hội, có xếp hạng hay PvP không.
- **Mua thể lực trong Shop** (user, 2026-11-09). Ngược luật hiện tại ([D-008](../decisions/decisions.md), [Thể lực](game-design.md#thể-lực)). `TODO(user)`: mua bằng Gem hay tiền thật, giá, giới hạn mỗi ngày.
- **Mua Coin trong Shop** (user, 2026-11-09): nút + ở Coin. `TODO(user)`: giá, có làm lệch vòng lặp "quán kiếm Coin" không.
- **Nâng cấp nhân vật tốn Coin** (user, 2026-11-09): chọn nhân vật ở màn Nhân vật sẽ sang màn nâng cấp (`Mota/character-upgrade.png`), màn này hiện Coin. Hiện chỉ có nâng sao bằng mảnh. Cấp nhân vật đang "chưa thiết kế" ([game-design](game-design.md#nhân-vật)), và 2026-11-03 user đã không lấy "cấp nhân vật bằng Gold" từ master plan. `TODO(user)`: Coin dùng để nâng gì. Ngày 2026-11-11 user nói thêm: "nâng cấp nhân vật bằng Coin, quán bằng Material". Giá nâng cấp quán hiện là Coin + Material; đợt B UI hub chỉ đổi thanh tài nguyên (Nâng cấp quán hiện Material), **không đổi giá**. Đổi giá quán sang chỉ Material là việc cân bằng riêng, làm cùng câu "Coin dùng để nâng gì" ([spec đợt B mục 11](../superpowers/specs/2026-11-11-ui-hub-b-design.md)).

## Để sau

- **Hộp quà** (🎨 đề xuất, user đồng ý 2026-11-09): quà từ server, quà đền bù. Cần online. Ô giữ chỗ ở hàng nút tròn của Lobby.
- **Thông báo tin từ server** (🎨, user đồng ý 2026-11-09). Cần online.
- **Nhiệm vụ** (🎨, user đồng ý 2026-11-09). Cần online. Lưu ý: 2026-11-03 user đã không lấy nhiệm vụ ngày/tuần từ master plan; bàn lại khi làm.
- **Hội (Clan)** (🎨, user đồng ý 2026-11-09). Cần online.
- **Chế độ khác ngoài Chương chính** (🎨, 2026-11-09; user chốt tên ô 2026-11-12): 4 ô khoá "Sắp có" ở màn Chọn chế độ của UI hub đợt C là **Khó, Sự kiện, Thử thách, Đấu trường**. Chưa thiết kế luật cho ô nào. Đấu trường dính tới câu "có xếp hạng hay PvP không" của phạm vi online.
- **Banner sự kiện và gói mua** ở bên trái Lobby (🎨, 2026-11-09). Chỗ banner đã giữ, banner đầu trỏ Gacha.
- **Nút góc màn Tiêu đề**: ngôn ngữ, xoá cache, hỗ trợ (🎨, user đồng ý 2026-11-09).
- **Hồ sơ online**: ID, tài khoản (🎨, user đồng ý 2026-11-09). Làm ở đợt Online; Hồ sơ đợt C chưa chừa chỗ (user chốt 2026-11-12).
- **Thông báo "phần thưởng chưa nhận"** từ Hộp quà, Nhiệm vụ (user, 2026-11-09). Làm ở đợt Online, không ở UI hub đợt C (user chốt 2026-11-12).
- **Bước đặt tên lúc mới vào game** (🎨, 2026-11-12): đợt C chỉ cho sửa tên trong Hồ sơ. Bàn cùng luồng hướng dẫn lần đầu chơi.

## Bỏ

- **Guildhouse** (🎨, user đồng ý 2026-11-09): Quán đã là Lobby.
- **Cấp và EXP người chơi** trên thanh trên (🎨, user đồng ý 2026-11-09): game không có cấp người chơi.
- **Nhân vật lớn và bong bóng thoại ở Lobby** (🎨, 2026-11-09, suy ra từ việc user chọn Quán là nền Lobby): chỗ đó là quán.
