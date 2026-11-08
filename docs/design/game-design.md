# Thiết kế game

> **Đọc khi:** làm hoặc sửa gameplay (gacha, quán, combat, nâng cấp, AFK), cân bằng số liệu.
> **Cập nhật khi:** user chốt hoặc đổi một luật chơi. Ý tưởng tổng ở [vision](../vision/vision.md); kiến trúc code ở [structure](../project/structure.md).

Mục ghi *khởi điểm* là giá trị tạm để bắt đầu cân bằng, đặt trong ScriptableObject để chỉnh không cần sửa code.

## Tiền tệ

| Loại | Nguồn | Dùng để |
|---|---|---|
| Coin | Khách trả tiền ở quán, thu thập AFK | Mua nâng cấp quán |
| Material | Thưởng màn chơi | Mua nâng cấp quán |
| Gem | Thưởng màn chơi; mua trong game; quảng cáo thưởng ([Kiếm tiền](#kiếm-tiền)) | Quay gacha |

Nâng cấp tốn cả Coin và Material, nên người chơi phải vừa để quán chạy vừa đi đánh.

## Nhân vật

- Có được từ gacha. Mỗi nhân vật có chỉ số chiến đấu (HP, tấn công, thủ, tốc độ) và chỉ số quán (nấu, phục vụ, thu ngân).
- Độ hiếm: R, SR, SSR.
- Một nhân vật chỉ ở một nơi: **trong đội chiến đấu thì không làm quán, dù đang đánh hay không.** Đưa vào đội thì tự rời quán, và ngược lại.
- Đội chiến đấu tối đa 5 người (user chốt 2026-10-31). **Vào trận cần tối thiểu 1 người; dưới 5 người thì phải qua hộp xác nhận** để tránh bấm nhầm (user chốt 2026-11-01, thuộc spec Combat).
- **Vị trí:** đội hình có 3 vị trí, 5 ô: **front 2, mid 2, back 1** (user chốt 2026-11-02). Vị trí chỉ ảnh hưởng quái nhắm ai: trọng số *khởi điểm* front 3, mid 2, back 1.
- **Class:** 5 class là đỡ đòn, đấu sĩ, hỗ trợ, xạ thủ, pháp sư; mỗi nhân vật một class, đặt trong `CharacterDefinition`. **Class quyết định vị trí, hệ thống tự xếp** (user chốt 2026-11-02): đỡ đòn và đấu sĩ đứng front, hỗ trợ và xạ thủ đứng mid, pháp sư đứng back; hàng đầy thì người thừa sang hàng gần nhất. Người chơi chỉ chọn ai vào đội, chưa tự xếp ô (để dành, làm thì phải đổi save). Ngoài vị trí, class không có luật riêng; khác nhau ở chỉ số và skill.
- Game mới được cấp sẵn một ít tiền và vài nhân vật, chỉ một lần.
- **Nhân vật trùng** (user chốt 2026-10-31): quay ra nhân vật đã có thì nhận **mảnh** của nhân vật đó, không đổi kho nhân vật. Số mảnh theo độ hiếm, *khởi điểm* R/SR/SSR đều **1**, đặt trong banner SO. Mảnh nằm trong save (qua `Roster`) và bị Reset Save Data xoá. Màn Gacha chỉ hiện số mảnh nhận được ở mỗi lượt, chưa có màn kho.
- **Số sao khởi điểm** (user chốt 2026-10-31): mỗi nhân vật có số sao khởi điểm 1..5 đặt trong `CharacterDefinition`, mẫu R = 1, SR = 2, SSR = 3, chỉnh riêng từng nhân vật.
- **Nâng sao** (user chốt 2026-11-08, [spec](../superpowers/specs/2026-11-08-star-up-design.md)): tiêu mảnh của chính nhân vật đó, mỗi lần 1 sao, tối đa 5★. Giá theo sao đích, không theo độ hiếm, *khởi điểm* lên 2★/3★/4★/5★ tốn **2/4/6/8** mảnh, đặt trong `StarConfig`. Mỗi sao đã nâng (trên sao khởi điểm) cộng **10%** chỉ số gốc vào HP, Tấn công, Thủ (*khởi điểm*); Tốc độ, chỉ số quán và AFK không đổi. Đã 5★ thì mảnh vẫn được cộng và để dành, chưa có chỗ tiêu. Nâng ở màn Nhân vật. Reset Save Data xoá sao đã nâng cùng mảnh.
- Chưa thiết kế: cấp nhân vật; chỗ tiêu mảnh dư ở 5★.

## Quán

- Tự chạy: khách đến ngẫu nhiên, nhân vật làm việc theo chỉ số ở vị trí được xếp (nấu, phục vụ, thu ngân). Người chơi xếp nhân sự và mua nâng cấp.
- Có hình: khách và nhân vật đi lại trong quán. Logic tách khỏi hình.
- Vẫn chạy khi người chơi đang đánh trận.
- **Bố cục màn quán** (user chốt 2026-11-01, thay phòng isometric trước đó): theo ba ảnh `Mota/restaurant-layout-*`. Chỉ lấy bố cục; mô tả cụ thể và phần không lấy ở [spec Restaurant mục 5.1](../superpowers/specs/2026-11-01-restaurant-design.md#51-màn-quán-restaurantunity).
- **Dây chuyền** (user chốt 2026-11-01): khách vào, ngồi bàn, **nấu**, **phục vụ** bưng ra, ăn (không cần ai), **thu ngân** thanh toán, về. Thời gian một việc = thời gian gốc × 100 / (100 + chỉ số), chỉ số chỉ làm nhanh hơn, không đổi giá món. Mỗi vai khởi điểm 1 ô, bàn khởi điểm 3 (nâng cấp tăng).
- **Kiên nhẫn:** hai lần chờ có hạn (từ lúc đến tới khi có món; từ lúc ăn xong tới khi trả tiền xong). Quá hạn thì khách bỏ về **không trả tiền**.
- **Thiếu người ở một vai** thì trạm đó đứng yên, quán không thu được gì. Không có "chủ quán làm thay". Với 3 nhân vật khởi đầu, chạy quán thì đội chiến đấu trống: đúng cân nhắc "đội mạnh hay quán đông".
- **Nâng cấp** (user chốt 2026-11-01): sáu loại, mọi cấp tốn cả Coin và Material, không có điều kiện cần có trước: Bàn (5 cấp, 3 → 8), ba loại ô nhân viên (2 cấp mỗi loại, 1 → 3), Biển hiệu (5 cấp, khách đến nhanh hơn), Thực đơn (5 cấp, giá món cao hơn). Số khởi điểm ở [spec Restaurant](../superpowers/specs/2026-11-01-restaurant-design.md#34-sáu-nâng-cấp-mẫu). UI nâng cấp hoàn chỉnh do người khác làm, `TODO(user)` gửi mô tả; công thức nấu ăn và đồ trang trí là ý tưởng chưa thiết kế, `TODO(user)`.
- **Chất lượng quán** (user chốt 2026-11-01): thang 100 điểm, 20 điểm là 1 sao, chỉ ảnh hưởng AFK, không ảnh hưởng tiền khi chơi thật. Gồm hai phần, mỗi phần tối đa 50 điểm:
  - **Chuyên cần:** 50 × trung bình của **7 ngày trọn vẹn gần nhất** (hôm qua trở về trước) của min(1, thời gian ở màn quán trong ngày / 5 phút). Vì Lobby là màn quán và các màn khác là popup phủ lên quán ([luồng màn](screen-flow.md)), "ở màn quán" là **mọi lúc trong game trừ trong trận**; app vào nền thì không tính (user chốt 2026-11-09). **Hôm nay không được tính**, nên chơi hôm nay chỉ tăng chất lượng từ ngày mai. Vắng ngày thứ 2 mới bắt đầu giảm, tối đa sau 7 ngày. Kiểu quest tuần: quà AFK thấp là cái giá của việc không chăm.
  - **Xử lý sự cố:** 50 × trung bình điểm của 10 sự cố gần nhất (kịp 1, trễ 0,5, bỏ lỡ 0, chưa có 0).
- **Sự cố** (user chốt 2026-11-01): chỉ xuất hiện khi người chơi đang ở màn quán, tối đa 1 sự cố cùng lúc, cách nhau khoảng 60–120 giây. Biểu tượng "!" trên một trạm, bấm một lần để xử lý: trong 8 giây là kịp, trong 20 giây là trễ, quá là bỏ lỡ. Chưa xử lý thì trạm đó đứng yên. **Vai không có người vẫn sinh sự cố nhưng không xử lý được nếu vai đó không có nhân vật.** Mở popup toàn màn (Gacha, Nhân vật, bản đồ…) **vẫn sinh sự cố**, nhưng đồng hồ 8/20 giây chỉ chạy khi đang ở Lobby không có popup; trạm đó đứng yên tới khi xử lý; sự cố hiện trong nút Thông báo (user chốt 2026-11-09, thay luật "mở popup thì không sinh sự cố mới"). Vào trận là rời màn quán. Rời màn quán hoặc app vào nền khi sự cố đang chạy thì sự cố bị huỷ, không tính điểm, trạm mở lại. Loại sự cố có lựa chọn xử lý để dành làm sau.

## AFK

Quán gần như tự động, nên AFK phải **kém hơn chơi thật**, để người không vào game không có tài nguyên ngang người cày.

- **Khi nào tích:** từ lúc app vào nền hoặc tắt đến khi mở lại. Trong game thì quán chạy thật, không tích AFK.
- **Trần:** 12 giờ tích lũy. Đầy thì dừng, **phải thu thập mới tích tiếp**.
- **Chưa thu thập mà thoát lại:** phần cũ vẫn giữ, tích tiếp đến khi đủ 12 giờ tổng cộng.
- **Chỉ cho Coin.** Material và Gem chỉ đến từ chiến đấu.
- **Tỉ lệ** (user chốt 2026-11-01, thay hệ số 0,3 cố định cũ): AFK/giây = **thu nhập lý thuyết** × **hệ số theo chất lượng**. Hệ số chạy liên tục từ **0,1** (0 điểm) đến **0,5** (100 điểm), *khởi điểm*, đặt trong `RestaurantConfig` cùng với số giờ tối đa. Hệ số tối đa luôn nhỏ hơn tỉ lệ thu nhập thật so với lý thuyết, nên AFK luôn thấp hơn chơi thật.
- **Thu nhập lý thuyết** là cận trên của mô phỏng, tính bằng công thức nút cổ chai từ nhân viên và nâng cấp ([spec 4.2](../superpowers/specs/2026-11-01-restaurant-design.md#42-thu-nhập-lý-thuyết-incomecalculator-hàm-tĩnh-thuần)), không ngẫu nhiên. **Chốt lúc thoát game**, nên không lách được bằng cách xếp nhân viên giỏi ngay trước khi thu thập. Quán thiếu một vai thì bằng 0.
- **Hệ số chất lượng tính lúc thu thập**, không phải lúc thoát. Nhờ vậy vắng nhiều ngày thì quà giảm (xem Chuyên cần ở mục Quán).
- **Thu thập:** người chơi tự bấm, tiền vào ví và bộ đếm về 0. Mở lại màn quán sau khi vắng từ 5 phút thì hiện popup thu thập; nút "Thu AFK" còn trên màn quán khi có Coin chờ. Thu mà lưu không được thì Coin vẫn vào ví nhưng người chơi được báo "chưa lưu được dữ liệu" (ở popup lẫn nút), game thử lưu lại ở lần lưu sau. Bấm thẻ sao ở màn quán mở popup chất lượng (chuyên cần 7 ngày, hôm nay, sự cố, hệ số AFK).
- **Đồng hồ máy:** thời gian âm (đồng hồ bị lùi) tính là 0. Chỉnh đồng hồ tiến để gian lận thì chấp nhận, vì game offline và đã có trần 12 giờ.
- **Quán chưa kiếm tiền khi thoát** (thu nhập bằng 0) thì thời gian vắng không tính vào trần, để không bị kẹt ở trần mà không có gì để thu.

Save cần lưu: thời điểm hoạt động cuối, thu nhập lý thuyết mỗi giây lúc thoát, số giây đã tích, số Coin lý thuyết đang chờ (chưa nhân hệ số chất lượng), cùng thời gian có mặt từng ngày và điểm 10 sự cố gần nhất cho chất lượng. Triển khai trong [spec Restaurant](../superpowers/specs/2026-11-01-restaurant-design.md).

## Chiến đấu

Chi tiết và số khởi điểm: [spec Combat](../superpowers/specs/2026-11-02-combat-design.md) (user chốt 2026-11-02).

- **Lượt:** mỗi vòng, ai còn sống đánh đúng 1 lần theo tốc độ giảm dần; hoà thì phe ta trước, rồi theo ô. Sát thương không ngẫu nhiên, không chí mạng, không né; ngẫu nhiên duy nhất là quái chọn mục tiêu theo trọng số vị trí.
- Mỗi nhân vật có đúng 3 hành động: đánh thường, skill (hồi chiêu tính theo lượt của chính nhân vật), ultimate (cần năng lượng; năng lượng có từ đánh thường, skill và khi bị đánh, mỗi trận bắt đầu từ 0). Quái có đánh thường và tối đa một skill, không có ultimate.
- **Công thức:** sát thương = max(1, làm tròn(tấn công × hệ số × 100 / (100 + thủ))); hồi máu = tấn công × hệ số, không vượt máu tối đa, không hồi sinh.
- Đội đánh nhiều quái theo wave trong từng màn; máu, năng lượng, hồi chiêu giữ qua các wave. Quá số vòng tối đa (*khởi điểm* 60) thì thua. Trận đang đánh không được lưu: thoát giữa trận là bỏ trận, không có thưởng và thể lực đã trừ lúc vào không hoàn ([Thể lực](#thể-lực)).
- **Công cụ trong trận:** AUTO, tốc độ x1/x2, tạm dừng và bỏ cuộc (tính thua, không hoàn thể lực). Không có nút SET, thống kê sát thương, 3 sao, MVP.
- **Thưởng:** thắng lần đầu được Material và Gem (*khởi điểm* màn đầu 30 Material, 150 Gem, tăng dần); đánh lại được Material như cũ và khoảng 1/5 Gem, để cày Gem chậm hơn cày Material. Thắng mới mở màn tiếp theo.
- **Màn hình** (user chốt 2026-11-01): phe ta đứng bên trái. Nhân vật giữ khoảng cách, **không đứng chồng lên nhau**. Vì đội có 5 người nên bố trí theo kiểu Princess Connect; chọn chiêu theo kiểu Fate/Grand Order. Chỉ lấy giao diện chọn chiêu; luật giữ nguyên, từng nhân vật đánh theo tốc độ. Ảnh: [ui-screens.md](../references/ui-screens.md).

## Màn chơi

(user chốt 2026-11-02)

- MVP có **1 chương: 5 màn đánh và 2 màn cốt truyện**, 7 nút nối nhau trên bản đồ. Màn mở theo thứ tự; màn đã clear luôn mở.
- **Màn cốt truyện** hiện chỉ là nút giữ chỗ: bấm Tiếp tục là clear, không thưởng, không tốn thể lực. Hội thoại thật làm sau MVP ([vision](../vision/vision.md)).
- Vào một màn đánh cần: màn đã mở, đủ thể lực, đội có ít nhất 1 người (dưới 5 người thì hỏi xác nhận, xem [Nhân vật](#nhân-vật)). Thể lực bị trừ ngay khi vào, xem [Thể lực](#thể-lực).
- Chưa làm: nhiều chương, độ khó Hard, quét nhanh, tự tiến màn tiếp theo.

## Thể lực

(user chốt 2026-11-03, theo master plan mục 20.7, thay luật 2026-11-02; [D-008](../decisions/decisions.md). Luật mới so với [vision](../vision/vision.md), `TODO(user)` cập nhật vision)

- Tối đa **100**, hồi **1 điểm mỗi 5 phút theo giờ máy, kể cả khi tắt game** (từ 0 lên đầy mất 8 giờ 20). Đầy thì không tích thêm.
- Mỗi màn đánh tốn **10**, kể cả màn boss; màn cốt truyện tốn 0.
- **Trừ một lần ngay khi vào trận hợp lệ**, và lưu ngay. Thua, bỏ cuộc, tắt app giữa trận đều **không hoàn**. "Thử lại" sau khi thua là một lần vào mới, trừ lại. Chỉ trừ sau khi mọi kiểm tra (màn, mở khoá, đội, cấu hình) đã qua, nên bị từ chối vào thì không mất thể lực. Nếu không lưu được thì không vào trận và không mất thể lực.
- Nút DEV "Chiến đấu thử" (panel Cài đặt) vào thẳng, không trừ thể lực và không có thưởng.
- Không mua thể lực bằng Gem, không có vật phẩm hồi thể lực.
- Đồng hồ máy chạy lùi thì không hồi và không mất thể lực. Chỉnh đồng hồ tiến để gian lận thì chấp nhận, như [AFK](#afk).
- Reset Save Data đưa thể lực về đầy.

## Gacha

- Banner có chi phí Gem cho 1 lượt và 10 lượt, tỉ lệ theo độ hiếm, ngưỡng đảm bảo. MVP có **1 banner**; bộ đếm đảm bảo lưu theo Id banner nên thêm banner sau không đổi save.
- Roll trên máy, vì game offline. Thiếu Gem thì không quay và không đổi gì.
- Số *khởi điểm* (user chốt 2026-10-31), đặt trong banner SO: tỉ lệ **R 70 / SR 25 / SSR 5**; **100 Gem** một lượt, **900 Gem** mười lượt. Trong cùng độ hiếm, mọi nhân vật của banner có xác suất như nhau.
- **Đảm bảo SSR:** lượt thứ **50** kể từ SSR gần nhất chắc chắn ra SSR. Đếm theo từng banner, quay lẻ hay quay 10 đều tính từng lượt, về 0 khi ra SSR (kể cả trúng tỉ lệ thường), lưu trong save. Không soft pity. Ngưỡng 0 là tắt.
- **Đảm bảo quay 10:** lượt thứ 10 mà chưa có SR trở lên thì ra SR hoặc SSR (theo tỉ lệ SR:SSR). Bật/tắt trong banner.
- **Quay không đổi trạng thái dở dang:** banner sai cấu hình (ví dụ chi phí 0, độ hiếm có tỉ lệ mà không có nhân vật) bị từ chối thay vì thành lượt quay miễn phí. Roll xong mới trừ Gem, rồi mới trao nhân vật hoặc mảnh và lưu một lần. Trong một lần quay 10, bản thứ hai của cùng nhân vật đã là bản trùng.
- **Luồng hiển thị** (user chốt 2026-10-31): bấm Quay → cutscene thường 3,5 giây (chạm để bỏ qua) → từng nhân vật hiện lần lượt (artwork, tên, số sao, lóe sáng; chạm để sang nhân vật kế) → trang tổng. SSR luôn đứng cuối, mỗi SSR có cutscene riêng 2,6 giây **không bỏ qua được**, rồi lóe sáng như nhân vật khác. Quay 10 có nút Bỏ qua: tới SSR đầu tiên, hoặc tới trang tổng nếu không có SSR. Quay 1 cũng có trang tổng (1 icon). Trang tổng của Quay 10 là lưới 5 + 5 icon hiện lần lượt, có nút OK.
- **Quay tiếp:** bấm OK hiện popup Quay tiếp (cùng số lượt vừa quay) hoặc Thôi (về trang roll). Không đủ Gem thì popup cảnh báo và nút Quay tiếp mờ.
- **Lịch sử quay:** lưu trong save, 50 lượt gần nhất (mỗi lần bấm Quay 1 hoặc Quay 10 là một mục), Reset Save Data xoá. Trang roll có nút Lịch sử mở popup chỉ đọc, mới nhất trên cùng.
- Nhân vật trùng thành mảnh: xem mục [Nhân vật](#nhân-vật). Thiết kế đầy đủ: [spec Gacha](../superpowers/specs/2026-10-31-gacha-design.md).

## Kiếm tiền

(user duyệt hướng 2026-11-02, [D-007](../decisions/decisions.md); chưa có spec, làm sau Combat và UI hub)

- Game tải miễn phí. Năm nguồn thu: **gói Gem**, **thẻ tháng**, **gói tân thủ**, **gói nhân vật khởi đầu**, **quảng cáo thưởng** (xem quảng cáo để nhận Gem).
- Luật hiện hành không đổi: thể lực không mua được bằng Gem ([Thể lực](#thể-lực)); gacha giữ nguyên số khởi điểm.
- `TODO(user)`, chốt trong spec riêng: nội dung và giá từng gói (số Gem mỗi gói; thẻ tháng nhận bao nhiêu Gem mỗi ngày; gói nhân vật khởi đầu gồm mấy nhân vật; gói tân thủ khác gói nhân vật khởi đầu ở đâu); quảng cáo thưởng cho bao nhiêu Gem mỗi lượt và tối đa mấy lượt mỗi ngày.
- `TODO(user)`: Reset Save Data có xoá quyền lợi đã mua hay không. [D-005](../decisions/decisions.md) đang nói xoá toàn bộ, trong khi quyền lợi mua gắn với tài khoản cửa hàng và thường khôi phục được.
- `TODO(user)`: save và roll gacha nằm trên máy, người chơi sửa được; khi có tiền thật phải quyết định chống gian lận tới mức nào ([D-003](../decisions/decisions.md) từng chấp nhận vì không có tiền thật).
- Giá và tỉ lệ mua trong sheet master plan (tab *Giả định KD*) chỉ là ước lượng để dự báo doanh thu 2027, không phải luật chơi.
