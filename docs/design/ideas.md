# Sổ ý tưởng

> **Đọc khi:** muốn biết ý tưởng nào đang bàn, để sau hay đã bỏ; trước khi đề xuất tính năng mới.
> **Cập nhật khi:** user thêm, sửa, hoãn hay bỏ một ý tưởng. Kênh 🎨 Game designer giữ file này. Ý đã chốt thành luật thì chép sang [game-design.md](game-design.md).

Mỗi ý ghi ngày và người đề xuất. Luồng màn ở [screen-flow.md](screen-flow.md).

Ý đã chốt và chuyển sang làm, nên rời sổ này: **phạm vi online**, **Nhiệm vụ** (đảo lại quyết định 2026-11-03 bỏ nhiệm vụ ngày/tuần), **Hộp quà**, **Hồ sơ online** (ID ở Online đợt 1, tài khoản Google ở đợt 3) **dòng Thông báo "phần thưởng chưa nhận"** (đợt 4) và **Nâng cấp nhân vật bằng Coin** (**Đã làm** 2026-11-21: thành cấp nhân vật, tốn thuốc EXP + Coin; xem [game-design](game-design.md#cấp-người-chơi-và-cấp-nhân-vật), [spec](../superpowers/specs/2026-11-21-character-level-design.md)). User chốt 2026-11-13 trong [spec Online](../superpowers/specs/2026-11-13-online-design.md); luật ở [game-design](game-design.md), màn ở [screen-flow](screen-flow.md).

## Đang bàn

- **Mua thể lực trong Shop** (user, 2026-11-09). Ngược luật hiện tại ([D-008](../decisions/decisions.md), [Thể lực](game-design.md#thể-lực)). `TODO(user)`: mua bằng Gem hay tiền thật, giá, giới hạn mỗi ngày.
- **Mua Coin trong Shop** (user, 2026-11-09): nút + ở Coin. `TODO(user)`: giá, có làm lệch vòng lặp "quán kiếm Coin" không.
- **Đổi giá nâng cấp quán sang chỉ Material** (user, 2026-11-11: "nâng cấp nhân vật bằng Coin, quán bằng Material"): việc cân bằng riêng, chưa làm. Phần "nâng cấp nhân vật bằng Coin" đã chuyển thành **Đã làm** (xem dòng đầu mục này): Coin là chi phí phụ của cấp nhân vật ([spec](../superpowers/specs/2026-11-21-character-level-design.md)). Giá quán hiện vẫn Coin + Material ([spec đợt B mục 11](../superpowers/specs/2026-11-11-ui-hub-b-design.md)).

## Để sau
- **Trang bị** (🎨 Nhân vật, 2026-11-21): sau 31/12. Ô trang bị trên panel nhân vật (có trong `character-upgrade.png`, đợt cấp nhân vật không lấy). Dùng quy tắc chi phí chung "chi phí chính + Coin phụ" ([spec Cấp nhân vật mục 2.4](../superpowers/specs/2026-11-21-character-level-design.md)). `TODO(user)`: nguồn trang bị, số ô, có cần server giữ không.
- **Nâng skill** (🎨 Nhân vật, 2026-11-21): sau 31/12. Chi phí chính là nguyên liệu skill, Coin là phụ (cùng quy tắc), trần có thể theo cấp nhân vật. `TODO(user)`: nguyên liệu lấy từ đâu, số bậc skill.
- **"Nâng tối đa" nhiều cấp một lần và chỗ tiêu mảnh dư ở 5★** (🎨 Nhân vật, 2026-11-21): ngoài phạm vi đợt cấp nhân vật.

- **Thành tựu** (user, 2026-11-21, ảnh `hub-missions-title.jpg`): tab thứ ba của Nhiệm vụ, nhận quà khi đạt thành tựu. Không thuộc Online đợt 3; làm sau.
- **Hệ thống cấp nhân vật và trang bị** (user, 2026-11-21, ảnh `character-upgrade-*.jpg`): nâng Lv bằng EXP, kỹ năng, nâng nhanh bằng trang bị lắp sẵn. Chưa có spec; Nhiệm vụ "Nâng lv" khoá "Sắp có" tới khi làm.
- **Google ID token riêng, Unity Player Accounts, Facebook** (🎨, user 2026-11-21): thêm cách đăng nhập khác; user chọn dùng chung một nút Google (qua Play Games) cho đợt 3. Google ID token cần plugin bên thứ ba. Sau 31/12.
- **Vẽ lại quán theo tilemap** (user, 2026-11-21, ảnh `restaurant-layout-target-en.png`): cất hình quán hiện tại vào `old`, vẽ lại theo cách tile map để dễ thay ảnh, hạn chế số hình. Ngoài luồng Online, chờ 👑 CEO chia luồng.
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
