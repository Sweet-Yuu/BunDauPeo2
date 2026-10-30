# Vision

> **Đọc khi:** bắt đầu tính năng mới, cần quyết định thiết kế, không chắc việc có đúng hướng không.
> **Cập nhật khi:** user đổi ý tưởng/phạm vi. Agent **không** tự sửa file này — chỉ đề xuất.

## Một câu mô tả

BunDauPeo là game RPG gacha 2D turn-based kết hợp quản lý quán bún đậu mắm tôm, chơi offline trên mobile màn ngang.

## Trải nghiệm cốt lõi

Người chơi điều hành một quán bún đậu gần như tự chạy: khách đến ngẫu nhiên, ăn và trả tiền. Nhân vật quay từ gacha vừa có thể làm nhân viên quán vừa có thể đi chiến đấu, nhưng không thể làm cả hai cùng lúc, nên người chơi luôn phải cân nhắc giữa đội mạnh và quán đông người.

Vòng lặp chính: quán kiếm Coin → chiến đấu kiếm Material và Gem → Coin và Material mua nâng cấp tăng thu nhập quán → Gem quay nhân vật mới → mạnh hơn cả ở quán lẫn ở trận.

Người vắng mặt vẫn có thu nhập AFK nhưng thấp hơn người chơi thật. Luật chi tiết ở [game-design.md](../design/game-design.md).

## Đối tượng & nền tảng

- Đối tượng: TODO(user)
- Nền tảng mục tiêu: Mobile, màn ngang, offline hoàn toàn, không nạp tiền thật.

## Phong cách

- Hình ảnh: 2D, UI hub và màn hình tham khảo Princess Connect Re:Dive, chiến đấu turn-based tham khảo Epic Seven (ảnh trong `Mota/`). Hiện dùng sprite có sẵn của Unity làm placeholder. TODO(user): phong cách art cuối cùng.
- Âm thanh / tone: TODO(user)

## Trong phạm vi (MVP)

- Kho nhân vật và gacha.
- Quán tự chạy có hình, nâng cấp quán, thu nhập AFK.
- Chiến đấu turn-based theo màn chơi: mỗi nhân vật có 1 đánh thường, 1 skill, 1 ultimate.
- UI hub có thanh điều hướng dưới.
- Lưu game cục bộ.

## Ngoài phạm vi (chưa làm)

- Nạp tiền thật, tính năng online, server.
- Hệ nguyên tố, trang bị.
- Cấp nhân vật và đổi nhân vật trùng thành tài nguyên (chưa thiết kế).

## Tiêu chí "xong" của MVP

Chơi trọn một vòng trên máy, sau khi tắt và mở lại vẫn giữ tiến độ:

1. Quay gacha và nhận nhân vật.
2. Xếp nhân viên, quán tự kiếm Coin.
3. Xếp đội, đánh thắng một màn, nhận Material và Gem.
4. Mua nâng cấp và thấy thu nhập tăng.
5. Tắt game, mở lại, thu thập tiền AFK.
