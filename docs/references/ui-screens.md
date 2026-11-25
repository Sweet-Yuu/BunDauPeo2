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
| Quán | `restaurant-room-isometric-en.jpg` | Phòng isometric có nhân vật đi lại. **Không còn là mẫu cần theo**, đã thay bằng ba ảnh trên. Trong game gốc đây là Guildhouse |
| Hub | `hub-missions.png` | **Màn Nhiệm vụ cần theo** (user gửi 2026-11-21, Princess Connect): tab Daily/Normal, mỗi dòng có nhãn loại, mô tả, thanh Status, ô thưởng, nút Challenge; dưới có Deadline (giờ còn lại), Completion Status 0/9, nút Receive All. Nhân vật lớn bên trái không lấy. Đã nhận: nút xám, không bấm được (user nói) |
| Hub | `hub-missions-daily.jpg` | Nhiệm vụ, tab Daily (user gửi 2026-11-21): dòng Quest 0/10 nút Challenge, dòng đã đủ nút Receive sáng; dưới có Deadline (giờ còn lại) và Completion Status 0/5, nút Receive All |
| Hub | `hub-missions-normal.jpg` | Nhiệm vụ, tab Normal (tân thủ): dòng đã đủ nút Receive sáng, dòng chưa đủ nút Challenge trắng; Completion Status 2/12 |
| Hub | `hub-missions-title.jpg` | Nhiệm vụ, tab thứ ba: nhận quà **thành tựu** (user giải thích 2026-11-21). **Chưa làm**, để sau ([ideas](../design/ideas.md#để-sau)) |
| Hub | `hub-mailbox-en.png` | **Hộp thư cần theo** (game khác): danh sách thư bên trái (biểu tượng phong thư, tiêu đề cắt, người gửi, dấu ghim/ngôi sao, chấm đỏ chưa đọc), nội dung thư bên phải (tiêu đề, người gửi, ngày, nội dung, Attachments, nút Claim), dưới trái Quantity 2/500, Claim All, Delete Read |
| Quán | `restaurant-layout-target-en.png` | **Quán user muốn vẽ lại** (2026-11-21): phòng nhà gỗ hai tầng, nền tile, nhân vật chibi. Đồng thời là ảnh nút **Hộp thư** ở Lobby (góc phải dưới, hình hòm thư, nhãn Claim All). Việc vẽ lại quán chưa có luồng ([ideas](../design/ideas.md#để-sau)) |
| Nhân vật | `character-upgrade-level.jpg` | Tab Lv nâng cấp nhân vật: Lv, thanh EXP, danh sách vật phẩm EXP (trống thì báo "không có"). **Chưa làm**, cho nhiệm vụ "Nâng lv" |
| Nhân vật | `character-upgrade-skill.jpg` | Tab nâng cấp kỹ năng: dòng kỹ năng, Lv, Mana tốn, nút nâng, khoá theo Rank |
| Nhân vật | `character-upgrade-memory-piece.jpg` | Tab "tài năng": thanh mảnh 0/30, Mana tốn, nút bật. Cùng loại với nâng sao bằng mảnh |
| Nhân vật | `character-upgrade-confirm.jpg` | Hộp xác nhận nâng nhanh: **lắp sẵn trang bị đã có để nâng cùng lúc** (user giải thích 2026-11-21); chưa làm |
| Cốt truyện | `story-dialogue-en.webp` | Hội thoại: nhân vật lớn, khung thoại, nút Skip. Cốt truyện sẽ làm sau MVP ([vision](../vision/vision.md)) |

User đã gửi đủ ảnh cho các màn hiện biết (2026-11-01). Các phần khác của quán (xếp nhân viên, nâng cấp, AFK) không có ảnh: agent đề xuất phương án tối ưu trong spec Restaurant, user duyệt.
