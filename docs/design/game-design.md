# Thiết kế game

> **Đọc khi:** làm hoặc sửa gameplay (gacha, quán, combat, nâng cấp, AFK), cân bằng số liệu.
> **Cập nhật khi:** user chốt hoặc đổi một luật chơi. Ý tưởng tổng ở [vision](../vision/vision.md); kiến trúc code ở [structure](../project/structure.md).

Mục ghi *khởi điểm* là giá trị tạm để bắt đầu cân bằng, đặt trong ScriptableObject để chỉnh không cần sửa code.

## Tiền tệ

| Loại | Nguồn | Dùng để |
|---|---|---|
| Coin | Khách trả tiền ở quán, thu thập AFK | Mua nâng cấp quán |
| Material | Thưởng màn chơi | Mua nâng cấp quán |
| Gem | Thưởng màn chơi | Quay gacha |

Nâng cấp tốn cả Coin và Material, nên người chơi phải vừa để quán chạy vừa đi đánh.

## Nhân vật

- Có được từ gacha. Mỗi nhân vật có chỉ số chiến đấu (HP, tấn công, thủ, tốc độ) và chỉ số quán (nấu, phục vụ, thu ngân).
- Độ hiếm: R, SR, SSR.
- Một nhân vật chỉ ở một nơi: **trong đội chiến đấu thì không làm quán, dù đang đánh hay không.** Đưa vào đội thì tự rời quán, và ngược lại.
- Đội chiến đấu tối đa 5 người.
- Game mới được cấp sẵn một ít tiền và vài nhân vật, chỉ một lần.
- Chưa thiết kế: cấp nhân vật, xử lý nhân vật trùng.

## Quán

- Tự chạy: khách đến ngẫu nhiên, nhân vật làm việc theo chỉ số ở vị trí được xếp (nấu, phục vụ, thu ngân). Người chơi xếp nhân sự và mua nâng cấp.
- Có hình: khách và nhân vật đi lại trong quán. Logic tách khỏi hình.
- Vẫn chạy khi người chơi đang đánh trận.

## AFK

Quán gần như tự động, nên AFK phải **kém hơn chơi thật**, để người không vào game không có tài nguyên ngang người cày.

- **Khi nào tích:** từ lúc app vào nền hoặc tắt đến khi mở lại. Trong game thì quán chạy thật, không tích AFK.
- **Trần:** 12 giờ tích lũy. Đầy thì dừng, **phải thu thập mới tích tiếp**.
- **Chưa thu thập mà thoát lại:** phần cũ vẫn giữ, tích tiếp đến khi đủ 12 giờ tổng cộng.
- **Chỉ cho Coin.** Material và Gem chỉ đến từ chiến đấu.
- **Tỉ lệ:** thu nhập mỗi giây × hệ số AFK. Hệ số *khởi điểm* **0.3**, đặt trong `RestaurantConfig` cùng với số giờ tối đa.
- **Thu nhập mỗi giây lấy tại lúc thoát game**, dựa trên nhân viên và nâng cấp lúc đó. Như vậy không lách được bằng cách xếp nhân viên giỏi ngay trước khi thu thập.
- **Thu thập:** người chơi tự bấm, tiền vào ví và bộ đếm về 0.
- **Đồng hồ máy:** thời gian âm (đồng hồ bị lùi) tính là 0. Chỉnh đồng hồ tiến để gian lận thì chấp nhận, vì game offline và đã có trần 12 giờ.

Save cần lưu: thời điểm hoạt động cuối, thu nhập mỗi giây lúc thoát, số giây đã tích, số Coin đang chờ. Triển khai trong spec Restaurant.

## Chiến đấu

- Turn-based, xếp lượt theo tốc độ.
- Mỗi nhân vật có đúng 3 hành động: đánh thường, skill (có hồi chiêu), ultimate (cần năng lượng).
- Đội đánh nhiều quái theo wave trong từng màn. Thắng màn được Material và Gem, mở màn tiếp theo.

## Gacha

- Banner có chi phí Gem cho 1 lượt và 10 lượt, tỉ lệ theo độ hiếm, ngưỡng đảm bảo.
- Roll trên máy, vì game offline.
