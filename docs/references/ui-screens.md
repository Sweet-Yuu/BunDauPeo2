# Ảnh tham khảo UI và màn chơi

> **Đọc khi:** thiết kế hoặc làm một màn hình, layout UI, hoặc viết spec có màn hình.
> **Cập nhật khi:** thêm, đổi tên hoặc xoá ảnh trong `ReferenceImages/`.

`ReferenceImages/` chứa ảnh chụp layout và các màn mà user muốn hướng đến, gồm Princess Connect Re:Dive, Epic Seven (`battle-result-*`, `story-*`) và Fate/Grand Order (`battle-skill-select-*`). Mở đúng ảnh của màn đang làm (`Glob "ReferenceImages/battle-*"`), không cần xem hết.

## Quy tắc

- **Thiếu ảnh thì hỏi user:** cần tham khảo một màn mà bảng dưới không có thì nói rõ cần ảnh gì. User sẽ gửi.
- **Đặt tên:** `<nhóm>-<màn>[-chi-tiết].<đuôi>`, chữ thường, tiếng Anh, nối bằng `-`. Thêm `-en` nếu chữ trong ảnh là tiếng Anh. Agent được đổi tên cho dễ tìm; đổi tên hoặc thêm ảnh thì cập nhật bảng dưới.
- **Chỉ tham khảo:** học bố cục và luồng màn hình. Không đưa ảnh vào `Assets/` hay build, không sao chép art; UI vẫn theo mục "UI và hình tạm" của [unity-workflow.md](../guides/unity-workflow.md).
- **Luật chơi không lấy từ ảnh:** ảnh có nhiều thứ game này chưa có hoặc không làm (hệ nguyên tố, trang bị, EXP người chơi, clan, nạp tiền). Phạm vi theo [vision](../vision/vision.md), luật theo [game-design](../design/game-design.md).

## Danh sách

| Nhóm | File (trong `ReferenceImages/`) | Xem gì |
|---|---|---|
| Vào game | `title-touch-to-start.jpg` | Màn tiêu đề: logo, "Touch To Start", nút menu góc trên |
| Hub | `hub-home-annotated-en.png` | **Xem trước:** màn chính có nhãn tiếng Anh cho từng vùng và 7 mục thanh điều hướng dưới |
| Hub | `hub-home.jpg` | Màn chính bản không nhãn, bố cục tương tự |
| Quest | `quest-mode-select.jpg` | Chọn chế độ: thẻ lớn "Main Quest", các chế độ phụ có khoá |
| Quest | `quest-stage-map.jpg` | Bản đồ màn: các điểm nối nhau, sao, khoá, NEXT, độ khó |
| Quest | `quest-stage-map-small-en.jpg` | Bản đồ màn, ảnh nhỏ, có chữ tiếng Anh |
| Quest | `quest-stage-info-popup.jpg` | Popup một màn: quái, vật phẩm, phần thưởng, nút vào trận |
| Quest | `quest-team-select-popup.jpg` | Popup chọn đội 5 người: lưới nhân vật, hàng đội hiện tại |
| Chiến đấu | `battle-skill-select-en.png` | **Cách chọn chiêu cần theo:** icon chiêu của từng nhân vật, nút Attack, tên và máu quái. Ảnh chỉ có 3 nhân vật, game này có 5 |
| Chiến đấu | `battle-wave.jpg` | **Bố trí 5 nhân vật cần theo:** wave, 5 chân dung có thanh máu và năng lượng, nút SET/AUTO/tốc độ. Nhân vật trong ảnh đứng đè nhau, game này thì không (xem [game-design](../design/game-design.md#chiến-đấu)) |
| Chiến đấu | `battle-wave-small.jpg` | Như trên, ảnh nhỏ, bối cảnh khác |
| Chiến đấu | `battle-boss.png` | Trận boss: thanh máu boss, tên và cấp, biểu tượng hiệu ứng |
| Chiến đấu | `battle-pause-popup.jpg` | Popup tạm dừng: tình trạng đội, cài đặt trận, tiếp tục, bỏ cuộc |
| Kết quả trận | `battle-result-clear-en.webp` | Thắng: hiệu ứng CLEAR, 3 sao, nhiệm vụ màn và phần thưởng |
| Kết quả trận | `battle-result-rewards-mvp-en.webp` | Trang thưởng: MVP, đội, vật phẩm nhận được, nút Confirm |
| Kết quả trận | `battle-result-stats-popup-en.webp` | Popup thống kê sát thương và hồi máu của từng nhân vật |
| Kết quả trận | `battle-result-failed-en.webp` | Thua: gợi ý cách mạnh hơn, nút Leave, Lobby, Try again |
| Gacha | `gacha-banner.jpg` | Màn banner: danh sách banner, nút quay 1 và 10 lượt kèm giá, số dư |
| Gacha | `gacha-reveal-character.webp` | Lúc mở từng nhân vật: artwork lớn, tên, số sao |
| Gacha | `gacha-result-10.jpg` | Kết quả 10 lượt: lưới 2 × 5, nút OK |
| Nhân vật | `character-list-en.png` | Danh sách nhân vật: thẻ artwork có sao, lọc Front/Middle/Back, sắp xếp |
| Nhân vật | `character-upgrade.png` | Nâng cấp nhân vật: nhân vật ở giữa, tab chức năng, chỉ số |
| Quán | `restaurant-layout-garden-en.webp` | **Bố cục màn quán cần theo** (user chốt 2026-11-01), cùng hai ảnh dưới: phòng chữ nhật nghiêng lấp gần hết màn, tường ở cạnh sau và cạnh trái, cảnh quanh phòng, thẻ tiền trên-trái, cột nút phải. Mô tả và phần không lấy: [spec Restaurant mục 5.1](../superpowers/specs/2026-11-01-restaurant-design.md#51-màn-quán-restaurantunity) |
| Quán | `restaurant-layout-farm-en.webp` | Như trên, bối cảnh nông trại: có quầy thu ngân và lối vào rõ hơn |
| Quán | `restaurant-layout-sea-en.webp` | Như trên, bối cảnh biển: dãy dài dọc tường sau, hai hàng đồ giữa phòng |
| Quán | `restaurant-room-isometric-en.jpg` | Phòng isometric có nhân vật đi lại. **Lại là mẫu cho hình thoi của sàn** (user chốt 2026-11-21, thay quyết định 2026-11-01 ở dòng này); vẫn lấy vườn, HUD, cột nút từ ba ảnh trên. Trong game gốc đây là Guildhouse |
| Quán · Nội thất | `restaurant-furniture-menu.jpg` | **Xem trước.** Phòng isometric có tường sau-trái và sau-phải; menu nổi lưới 3 cột (Cửa hàng, Kho, Hiệu ứng, Bộ cài sẵn, Cất tất cả, Xếp nhân vật…) mở từ nút MENU; nút chọn tầng bên trái. User chốt 2026-11-22: lấy cách mở menu nội thất và các popup dưới đây (user chốt 2026-11-21: sàn hình thoi). Không lấy tầng, BGM, Game table, quà, hiệu ứng sản xuất |
| Quán · Nội thất | `restaurant-furniture-shop.jpg` | Cửa hàng nội thất: tab loại, thẻ món (tên, hình, giá Coin), hai nút **Mua cất kho** / **Mua và đặt**; đếm "đã đặt / chỗ kho" ở góc dưới. Không lấy món giới hạn thời gian |
| Quán · Nội thất | `restaurant-furniture-storage.jpg` | Kho: thẻ món có số lượng ×N, nút **Bán** / **Đặt**, tab loại, sắp xếp, đếm "đã đặt / chỗ kho". Bán không có ở món đặc biệt |
| Quán · Nội thất | `restaurant-furniture-bulk-store.jpg` | Hộp xác nhận cất tất cả: số đã đặt, số trong kho, ô chọn loại, cảnh báo đỏ không hoàn tác |
| Quán · Nội thất | `restaurant-furniture-myset.jpg` | Bộ cài sẵn: nhiều ô lưu bố cục (xoá, đổi tên, lưu bố cục hiện tại, áp dụng). Có thể để sau |
| Quán · Nội thất | `restaurant-furniture-effects.jpg` | Danh sách nội thất có hiệu ứng (sản xuất vật phẩm theo giờ). Chỉ tham khảo bố cục danh sách; hiệu ứng của game này là điểm trang trí |
| Quán · Nội thất | `restaurant-character-placement.jpg` | Chọn nhân vật đứng trong phòng theo tầng. Game này không làm (nhân viên theo vai ở màn Nhân viên) |
| Cốt truyện | `story-dialogue-en.webp` | Hội thoại: nhân vật lớn, khung thoại, nút Skip. Cốt truyện sẽ làm sau MVP ([vision](../vision/vision.md)) |

User đã gửi đủ ảnh cho các màn hiện biết (2026-11-01). Các phần khác của quán (xếp nhân viên, nâng cấp, AFK) không có ảnh: agent đề xuất phương án tối ưu trong spec Restaurant, user duyệt.
