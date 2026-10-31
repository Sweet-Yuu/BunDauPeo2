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
- Đội chiến đấu tối đa 5 người (user chốt 2026-10-31).
- **Vị trí:** đội hình có 3 vị trí là front, mid và back.
- **Class:** 5 class chính là đỡ đòn, đấu sĩ, hỗ trợ, xạ thủ, pháp sư. Mỗi nhân vật thuộc một class.
- `TODO(user)`: mỗi class đứng vị trí nào, mỗi vị trí có mấy ô trong 5 ô, và vị trí là cố định theo nhân vật hay người chơi tự xếp. Chốt trong spec Combat. `CharacterDefinition` và `Roster` hiện chưa có class hay vị trí; `Roster` đang giữ đội là một danh sách phẳng.
- Game mới được cấp sẵn một ít tiền và vài nhân vật, chỉ một lần.
- **Nhân vật trùng** (user chốt 2026-10-31): quay ra nhân vật đã có thì nhận **mảnh** của nhân vật đó, không đổi kho nhân vật. Số mảnh theo độ hiếm, *khởi điểm* R/SR/SSR đều **1**, đặt trong banner SO. Mảnh nằm trong save (qua `Roster`) và bị Reset Save Data xoá. Mảnh dùng để nâng sao (tối đa 5★, mảnh dư vào kho) ở spec riêng sau Combat; cho đến lúc đó mảnh chỉ được cộng; màn thử Gacha chỉ hiện số mảnh nhận được ở mỗi lượt, chưa có màn kho.
- Chưa thiết kế: cấp nhân vật, nâng sao (giá mảnh, ảnh hưởng chỉ số, số sao ban đầu).

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

- Banner có chi phí Gem cho 1 lượt và 10 lượt, tỉ lệ theo độ hiếm, ngưỡng đảm bảo. MVP có **1 banner**; bộ đếm đảm bảo lưu theo Id banner nên thêm banner sau không đổi save.
- Roll trên máy, vì game offline. Thiếu Gem thì không quay và không đổi gì.
- Số *khởi điểm* (user chốt 2026-10-31), đặt trong banner SO: tỉ lệ **R 70 / SR 25 / SSR 5**; **100 Gem** một lượt, **900 Gem** mười lượt. Trong cùng độ hiếm, mọi nhân vật của banner có xác suất như nhau.
- **Đảm bảo SSR:** lượt thứ **50** kể từ SSR gần nhất chắc chắn ra SSR. Đếm theo từng banner, quay lẻ hay quay 10 đều tính từng lượt, về 0 khi ra SSR (kể cả trúng tỉ lệ thường), lưu trong save. Không soft pity. Ngưỡng 0 là tắt.
- **Đảm bảo quay 10:** lượt thứ 10 mà chưa có SR trở lên thì ra SR hoặc SSR (theo tỉ lệ SR:SSR). Bật/tắt trong banner.
- **Quay không đổi trạng thái dở dang:** banner sai cấu hình (ví dụ chi phí 0, độ hiếm có tỉ lệ mà không có nhân vật) bị từ chối thay vì thành lượt quay miễn phí. Roll xong mới trừ Gem, rồi mới trao nhân vật hoặc mảnh và lưu một lần. Trong một lần quay 10, bản thứ hai của cùng nhân vật đã là bản trùng.
- Nhân vật trùng thành mảnh: xem mục [Nhân vật](#nhân-vật). Thiết kế đầy đủ: [spec Gacha](../superpowers/specs/2026-10-31-gacha-design.md).
