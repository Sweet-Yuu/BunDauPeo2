# Sổ ý tưởng

> **Đọc khi:** muốn biết ý tưởng nào đang bàn, để sau hay đã bỏ; trước khi đề xuất tính năng mới.
> **Cập nhật khi:** user thêm, sửa, hoãn hay bỏ một ý tưởng. Kênh 🎨 Game designer giữ file này. Ý đã chốt thành luật thì chép sang [game-design.md](game-design.md).

Mỗi ý ghi ngày và người đề xuất. Luồng màn ở [screen-flow.md](screen-flow.md).

Ý đã chốt và chuyển sang làm, nên rời sổ này: **phạm vi online**, **Nhiệm vụ** (đảo lại quyết định 2026-11-03 bỏ nhiệm vụ ngày/tuần), **Hộp quà**, **Hồ sơ online** (ID ở Online đợt 1, tài khoản Google ở đợt 3) và **dòng Thông báo "phần thưởng chưa nhận"** (đợt 4). User chốt 2026-11-13 trong [spec Online](../superpowers/specs/2026-11-13-online-design.md); luật ở [game-design](game-design.md), màn ở [screen-flow](screen-flow.md).

## Đang bàn

- **Mua thể lực trong Shop** (user, 2026-11-09). Ngược luật hiện tại ([D-008](../decisions/decisions.md), [Thể lực](game-design.md#thể-lực)). `TODO(user)`: mua bằng Gem hay tiền thật, giá, giới hạn mỗi ngày.
- **Mua Coin trong Shop** (user, 2026-11-09): nút + ở Coin. `TODO(user)`: giá, có làm lệch vòng lặp "quán kiếm Coin" không.
- **Nâng cấp nhân vật tốn Coin** (user, 2026-11-09): chọn nhân vật ở màn Nhân vật sẽ sang màn nâng cấp (`Mota/character-upgrade.png`), màn này hiện Coin. Hiện chỉ có nâng sao bằng mảnh. Cấp nhân vật đang "chưa thiết kế" ([game-design](game-design.md#nhân-vật)), và 2026-11-03 user đã không lấy "cấp nhân vật bằng Gold" từ master plan. `TODO(user)`: Coin dùng để nâng gì. Ngày 2026-11-11 user nói thêm: "nâng cấp nhân vật bằng Coin, quán bằng Material". Giá nâng cấp quán hiện là Coin + Material; đợt B UI hub chỉ đổi thanh tài nguyên (Nâng cấp quán hiện Material), **không đổi giá**. Đổi giá quán sang chỉ Material là việc cân bằng riêng, làm cùng câu "Coin dùng để nâng gì" ([spec đợt B mục 11](../superpowers/specs/2026-11-11-ui-hub-b-design.md)).

## Để sau

- **Thông báo tin từ server** (🎨, user đồng ý 2026-11-09). Bỏ khỏi bản 31/12 (user chốt 2026-11-13), vẫn để sau.
- **Hội (Clan)** (🎨, user đồng ý 2026-11-09). Sau 31/12 (user chốt 2026-11-13); nút tròn chưa dựng.
- **Xếp hạng, PvP** (user, 2026-11-13): sau 31/12.
- **Chế độ khác ngoài Chương chính** (🎨, 2026-11-09; user chốt tên ô 2026-11-12): 4 ô khoá "Sắp có" ở màn Chọn chế độ là **Khó, Sự kiện, Thử thách, Đấu trường**. Chưa thiết kế luật cho ô nào. Đấu trường giữ khoá tới khi làm xếp hạng/PvP (sau 31/12).
- **Nạp tiền và quảng cáo thưởng** (user chốt 2026-11-13): sau 31/12, bản 31/12 không có tiền thật ([game-design](game-design.md#kiếm-tiền)). Hai ý "mua thể lực / mua Coin trong Shop" ở trên đi cùng việc này.
- **Banner sự kiện và gói mua** ở bên trái Lobby (🎨, 2026-11-09). Chỗ banner đã giữ, banner đầu trỏ Gacha.
- **Nút góc màn Tiêu đề**: ngôn ngữ, xoá cache, hỗ trợ (🎨, user đồng ý 2026-11-09).
- **Bước đặt tên lúc mới vào game** (🎨, 2026-11-12): hiện chỉ sửa tên trong Hồ sơ (nhiệm vụ tân thủ "Đặt tên" ở Online đợt 3 nhắc người chơi). Bàn cùng luồng hướng dẫn lần đầu chơi.

## Bỏ

- **Guildhouse** (🎨, user đồng ý 2026-11-09): Quán đã là Lobby.
- **Cấp và EXP người chơi** trên thanh trên (🎨, user đồng ý 2026-11-09): game không có cấp người chơi.
- **Nhân vật lớn và bong bóng thoại ở Lobby** (🎨, 2026-11-09, suy ra từ việc user chọn Quán là nền Lobby): chỗ đó là quán.
