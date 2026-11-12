# Luồng màn

> **Đọc khi:** làm UI hub, thêm màn mới, nối các màn với nhau, viết spec có chuyển màn.
> **Cập nhật khi:** user đổi cách đi giữa các màn, thêm hay bỏ một màn. Kênh 🎨 Game designer giữ file này.

Bàn với user và **user duyệt ngày 2026-11-09** (kênh 🎨). Ảnh tham khảo: [ui-screens.md](../references/ui-screens.md). Ý tưởng chưa chốt ở [ideas.md](ideas.md).

Trạng thái: **có** = đã có trong game · **mới** = user chốt, chưa làm (ghi đợt nếu đã chia) · **khoá** = có nút nhưng hiện "Sắp có" · **sau** = để sau, chưa hiện.

UI hub chia ba đợt ([spec đợt A](../superpowers/specs/2026-11-09-ui-hub-design.md)): **A** khung (xong, user playtest và duyệt 2026-11-09, nhánh `feature/ui-hub`), **B** bố cục Lobby (xong, user playtest và duyệt 2026-11-12, nhánh `feature/ui-hub-b`, [spec đợt B](../superpowers/specs/2026-11-11-ui-hub-b-design.md), [mục dưới](#bố-cục-lobby-user-duyệt-ảnh-mô-phỏng-bản-2-2026-11-09)), **C** Tiêu đề, Chọn chế độ, nút Thông báo, Hồ sơ (spec và kế hoạch user duyệt 2026-11-12, chưa code, [spec đợt C](../superpowers/specs/2026-11-12-ui-hub-c-design.md)).

## Cấu trúc (user chốt 2026-11-09)

1. **Lobby = màn quán.** Quán chạy thật, chiếm cả màn hình, vuốt để kéo xem quanh quán (có từ đợt B: kéo hai chiều, dừng ở mép, không zoom).
2. **Các màn khác, trừ Trận, là panel kín màn phủ lên Lobby**, bật/tắt ngay trong `Restaurant.unity` (đợt A; scene `Gacha`, `Characters`, `StageSelect` đã xoá, bản build còn Boot, Restaurant, Battle; đợt C thêm `Title`). Quán vẫn chạy bên dưới. Thanh dưới vẫn hiện để đổi mục. Bấm mục Quán thì về Lobby.
3. **Trận là màn riêng.** Không có thanh dưới, không có Menu, không có nút Thông báo. Đánh xong hay bỏ cuộc, bấm Quay lại thì về Lobby, panel bản đồ mở sẵn (đợt A).
4. **Nút Thông báo nằm trên mọi panel và trang của hub** (trừ lúc thanh dưới ẩn). Có **chấm đỏ** khi có ít nhất một việc, không hiện số (user chốt 2026-11-12). Bấm vào hiện danh sách việc cần làm, bấm một dòng thì tới đúng chỗ ([mục dưới](#nút-thông-báo); đợt C).

**Trang con và hộp** (đợt A): popup màn, chọn đội, màn cốt truyện giữ chỗ, lịch sử quay, Nhân viên (kèm chọn người cho ô), Nâng cấp, Chất lượng đều là **trang kín màn**, nút **Quay lại** ở góc trên-trái, thanh dưới vẫn hiện. Hộp xác nhận (chuyển nhân viên, đội thiếu người, quay tiếp, Reset) và popup thu AFK **giữ cỡ hộp**, nền mờ che cả thanh. Bấm một ô khác của thanh thì trang đang mở đóng như khi bấm Quay lại. Từ đợt B, Lịch sử quay chuyển sang nhóm hộp. Đợt C thêm hai hộp, ngoại lệ của luật "trang mới kín màn" (L-033, user chốt 2026-11-12): danh sách Thông báo là hộp thả xuống dưới nút, Hồ sơ là hộp giữa màn 1200×800; cả hai có nền mờ che thanh dưới.

## Sơ đồ

Nhãn `(A)`, `(B)`, `(C)` là đợt làm.

```mermaid
flowchart TD
  Boot -->|đợt A, B: vào thẳng| Lobby
  Boot -.->|đợt C: mỗi lần mở app| Title["Tiêu đề, scene Title (mới, C)"] -.->|chạm| Lobby
  Lobby -.->|vắng ≥ 5 phút| AFK["Popup thu AFK (có)"]
  subgraph Hub["Restaurant.unity: Lobby = quán + thanh dưới 6 ô + panel kín màn"]
    Lobby["Quán / Lobby (có, A)"]
    Chars["Nhân vật (có, A)"]
    Story["Cốt truyện (khoá, A: báo Sắp có)"]
    Stages["ô Màn chơi"] -->|đợt A, B| Map["Bản đồ (có, A)"]
    Stages -.->|đợt C: luôn mở| Mode["Chọn chế độ (mới, C)"] -.->|Chương chính| Map
    Map -.->|Quay lại (C)| Mode
    Gacha["Gacha (có, A)"] --> History["Lịch sử quay (có)"]
    Menu["Menu (có, A)"]
  end
  Map --> Info["Popup màn (có)"] --> Team["Chọn đội (có)"] --> Battle["Trận (có, scene riêng)"]
  Map --> StoryStage["Màn cốt truyện giữ chỗ (có)"]
  Battle -->|Quay lại: về Lobby, bản đồ mở sẵn| Map
  Battle -->|Thử lại khi thua| Battle
  Lobby -.->|Shop, nút + (B)| Shop["Shop (khoá, B)"]
  Lobby -.->|avatar| Profile["Hồ sơ, hộp (mới, C)"]
  Notify["Nút Thông báo, hộp thả xuống (mới, C)"] -.->|bấm dòng| Lobby
  Menu --> Lang["Ngôn ngữ Việt ↔ Anh (có)"]
  Menu --> Reset["Reset dữ liệu + xác nhận (có)"]
  Menu -->|chỉ bản dev| Dev["Panel DEV (có)"]
  Battle -->|menu tạm dừng → Cài đặt, bản dev| Dev
  Dev -->|Chiến đấu thử| Battle
```

## Các màn

| Màn | Mở từ | Quay về | Ảnh `Mota/` | Trạng thái |
|---|---|---|---|---|
| Tiêu đề | Boot, lúc mở app | chạm → Lobby | `title-touch-to-start.jpg` | mới, đợt C: scene riêng `Title`, hiện mỗi lần mở app (không hiện khi về từ trận, đổi ngôn ngữ, Reset, quay lại từ nền). Tên game "Bun Dau Peo" trong `Logo_PH_900x300`, "Chạm để bắt đầu", số phiên bản; không có nút góc. Đợt A, B mở app vào thẳng Lobby |
| Lobby (quán) | ô **Quán**; về từ Trận | — | `hub-home-annotated-en.png`, `restaurant-layout-*` | có: quán + thanh 6 ô (đợt A); bố cục hub (avatar, thanh tài nguyên, thanh chất lượng, banner, thanh quán thu gọn, nút Shop), kéo quán và quán 2.5D (đợt B, [mục dưới](#bố-cục-lobby-user-duyệt-ảnh-mô-phỏng-bản-2-2026-11-09)) |
| Thanh dưới 6 ô | mọi màn trừ Trận | — | `hub-home-annotated-en.png` | có (đợt A): `NavBar_PH_1920x120`, 6 ô `NavTab*_PH_320x120`, ô đang mở tô nổi. Ẩn trong cutscene, lộ, tổng của Gacha |
| Popup thu AFK | tự hiện khi vào Lobby sau khi vắng ≥ 5 phút; nút Thu AFK; dòng AFK trong Thông báo (C) | Lobby | — | có, giữ cỡ hộp |
| Nhân viên, Nâng cấp, Chất lượng | Nhân viên, Nâng cấp: nút trong thanh quán thu gọn bên phải; Chất lượng: bấm `InfoBar` ở Lobby | Quay lại → Lobby | `quest-team-select-popup.jpg` (Nhân viên) | có, trang kín màn (đợt A). Ba trang này vẫn chặn sinh sự cố ([game-design](game-design.md#quán)). Đợt B đã làm lại Nhân viên (3 cột, mỗi cột 3 ô to) và Nâng cấp (lưới thẻ có nút Mua) |
| Nhân vật | ô **Nhân vật**; dòng "đủ mảnh" trong Thông báo (C) | ô khác của thanh | `character-list-en.png`, `character-upgrade.png` | có, panel kín màn (đợt A) |
| Cốt truyện | ô **Cốt truyện** | — | — (thiếu, xem [dưới](#ảnh-còn-thiếu)) | khoá: ô có ổ khoá, bấm hiện "Sắp có" (đợt A) tới khi có hội thoại Chương 1 |
| Chọn chế độ | ô **Màn chơi** (bấm lại luôn về đây); dòng "Thể lực đầy" của Thông báo | ô khác của thanh | `quest-mode-select.jpg` | mới, đợt C: thẻ lớn "Chương chính" (→ Bản đồ) và 4 ô khoá **Khó, Sự kiện, Thử thách, Đấu trường**, có ổ khoá và chữ "Sắp có", bấm báo "Sắp có". Đợt A, B ô Màn chơi mở thẳng bản đồ |
| Bản đồ màn | Chọn chế độ → Chương chính (đợt C; đợt A, B: ô **Màn chơi**); về từ Trận thì mở sẵn | đợt C: nút **Quay lại** → Chọn chế độ (đợt A, B: bỏ nút này, đổi bằng ô khác của thanh) | `quest-stage-map*.jpg` | có, panel kín màn (đợt A) |
| Popup màn, chọn đội | bấm một nút màn đánh trên bản đồ | Quay lại → Bản đồ | `quest-stage-info-popup.jpg`, `quest-team-select-popup.jpg` | có, trang kín màn (đợt A). Popup màn: quái bên trái, thưởng và thể lực ở cột phải, nút Thách đấu neo mép phải |
| Màn cốt truyện giữ chỗ | bấm một nút màn cốt truyện trên bản đồ | Tiếp tục / Quay lại → Bản đồ | — | có, trang kín màn (đợt A) |
| Trận | chọn đội → vào trận; DEV "Chiến đấu thử" | Quay lại → Lobby, bản đồ mở sẵn; Thử lại → trận mới | `battle-*` | có (`Battle`), scene riêng, không có thanh. Vào trận là rời quán, sự cố bị huỷ |
| Gacha | ô **Gacha**; banner Gacha ở Lobby (B) | ô khác của thanh | `gacha-*` | có, panel kín màn (đợt A); banner kín panel. Lịch sử quay: đợt A là trang kín màn; đợt B đổi thành **hộp** tối viền sáng giữa màn (như các hộp Gacha khác), nền mờ che cả thanh |
| Shop | nút tròn Shop; nút + của Thể lực, Coin, Gem | Lobby | — (thiếu) | khoá: nút Shop và các nút + có từ đợt B, bấm hiện "Sắp có"; nội dung làm khi có quảng cáo thưởng / mua |
| Menu | ô **Menu**, mở trên panel đang đứng | Đóng → panel đang đứng; bấm ô khác → ô đó | — (thiếu) | có, panel kín màn (đợt A): Ngôn ngữ (đổi xong vẫn ở đúng ô, Menu vẫn mở; từ đợt B vào game lần đầu và sau Reset mặc định **tiếng Anh**, không theo máy, user chốt 2026-11-11), Reset dữ liệu (hỏi xác nhận, xoá hết rồi về Lobby như game mới, [D-005](../decisions/decisions.md)), DEV (chỉ bản dev), Đóng |
| Hồ sơ | bấm avatar | Đóng → Lobby | — (thiếu) | mới, đợt C: **hộp giữa màn 1200×800**. Tên sửa được, tối đa 12 ký tự, rỗng thì hiện "Người chơi" / "Player"; lưới nhân vật đang sở hữu, bấm là đổi avatar (hình tạm: tròn màu theo độ hiếm + chữ cái đầu). Tên và avatar lưu trong save, Reset xoá. ID và tài khoản làm ở đợt Online, chưa chừa chỗ. Đợt B: avatar bấm báo "Sắp có" |
| Panel DEV | Menu → DEV; menu tạm dừng trong trận → Cài đặt | Đóng | — | có (đợt A): bỏ nút góc và 4 nút chuyển màn; còn Chiến đấu thử, Màn thử, Hồi thể lực, +Material, +Gem, Tua AFK, Ngôn ngữ, Reset, Đóng |
| Hộp quà, Thông báo tin, Nhiệm vụ, Hội | nút tròn ở Lobby | Lobby | — | sau, cần online ([ideas](ideas.md#để-sau)) |

Mỗi panel chỉ hiện loại tiền nó dùng (user chốt 2026-11-09; có từ đợt B qua thanh tài nguyên chung). Lobby hiện đủ 4 loại; bản đồ, popup màn và chọn đội hiện Thể lực; Gacha (trang roll) hiện Gem; Shop hiện Thể lực, Coin, Gem; **Nhân vật hiện Coin**, vì chọn một nhân vật sẽ sang màn nâng cấp nhân vật (`character-upgrade.png`; nâng cấp tốn gì đang bàn ở [ideas](ideas.md#đang-bàn)); **Nâng cấp quán hiện Material**; Nhân viên, Chất lượng, Cốt truyện, màn cốt truyện giữ chỗ, Menu, Hồ sơ không hiện; Lịch sử quay là hộp che thanh nên không cần (phần thêm user chốt 2026-11-11, [spec đợt B](../superpowers/specs/2026-11-11-ui-hub-b-design.md)). Trong cutscene và màn lộ của Gacha thì ẩn thanh dưới và nút Thông báo.

## Bố cục Lobby (user duyệt ảnh mô phỏng bản 2, 2026-11-09)

**Có từ đợt B** (user playtest và duyệt 2026-11-12; [spec](../superpowers/specs/2026-11-11-ui-hub-b-design.md), ảnh `hubb-lobby*.png`). Riêng nút Thông báo và 4 nút tròn online vẫn chưa có. Khung 1920×1080. Kích thước ghi trong tên là cỡ thật.

Đợt B còn thêm (user yêu cầu 2026-11-11):
- **Kéo quán hai chiều**, dừng ở mép, không zoom. Bấm "!" của sự cố vẫn ăn khi không kéo. Lúc mở Lobby, phòng quán nằm lệch sang phải 155 px so với đợt A (user chốt sau review cuối đợt B), để banner không che quầy và mép phải phòng không đụng thanh quán khi mở.
- **Quán vẽ kiểu 2.5D** như ảnh `restaurant-layout-*`: bếp, quầy, bàn, tường, luống, cây có chiều cao (mặt trước tối hơn) và bóng; khách và nhân viên đứng có thân, đầu, bóng, khi đi thì nhún và đung đưa hai bên; bàn có khách đang ăn thì có đĩa; khách chờ món có bóng "...", chờ trả tiền có đồng xu.

```
[Avatar] [Thể lực 80/100 + | Coin + | Material | Gem +]                 [Báo •]
         [★ chất lượng · ≈ Coin/giây · Phục vụ]
[Banner                                                            ]  [‹]  ← thanh quán thu gọn
 → Gacha]        QUÁN ĐANG CHẠY, FULL MÀN, VUỐT KÉO CAMERA         (mở: Nhân viên,
 ● ○ ○                                                               Nâng cấp, Thu AFK)
                                          (Shop) (Hộp quà) (Thông báo) (Nhiệm vụ) (Hội)
[ Quán | Nhân vật | Cốt truyện (khoá) | Màn chơi | Gacha | Menu ]
```

| Vùng | Tên object | Ghi chú |
|---|---|---|
| Avatar trên-trái | `Avatar_PH_140x140` | bấm mở Hồ sơ (đợt C); hiện bấm báo "Sắp có" |
| Thanh tài nguyên | `ResourceBar_PH_1120x76` | **một** thanh chung: Thể lực x/100 (kèm "+1 sau m:ss" khi chưa đầy), Coin, Material, Gem. Thể lực, Coin, Gem có nút + mở Shop (hiện báo "Sắp có"); Material không có |
| Thanh chất lượng | `InfoBar` (giữ tên object cũ vì test và `UiDriver` bấm theo tên; cỡ 800×68, một hàng) | sao chất lượng, Coin/giây, bộ đếm. Chỉ ở Lobby; bấm mở Chất lượng |
| Nút Thông báo trên-phải | `NotifyBtn_PH_110x100` | thay 4 icon nhỏ trong ảnh; chấm đỏ khi có việc, không hiện số. Đợt B chỉ chừa chỗ, nút làm ở đợt C |
| Banner trái | `BannerGacha_PH_360x200` | "Gacha / Quay ngay", bấm mở ô Gacha. Đặt ở `(20, 340)` (hạ thấp sau review để không che "!" của quầy thu ngân ở màn 16:10 và 4:3). Chấm trang làm khi có nhiều banner (hiện chỉ có 1) |
| Thanh quán thu gọn | `RestaurantBarToggle_PH_60x160`, `RestaurantBar_PH_180x460` | 3 nút đã có: Nhân viên, Nâng cấp, Thu AFK (Thu AFK chỉ hiện khi có Coin chờ). Mặc định đóng; nhớ qua tắt mở game (lưu trong `PlayerSave`), Reset về đóng; có Coin AFK chờ thu thì nút ‹ có chấm báo |
| Hàng nút tròn | `HubRoundBtnShop_PH_110x110` | chỉ có Shop (khoá, ổ khoá, bấm báo "Sắp có"), neo dưới-phải trên thanh. 4 ô online (Hộp quà, Thông báo, Nhiệm vụ, Hội) chưa dựng; chừa chỗ, dựng khi có online |
| Thanh dưới | `NavTab<Ô>_PH_320x120` × 6 trong `NavBar_PH_1920x120` | Quán · Nhân vật · Cốt truyện (khoá) · Màn chơi · Gacha · Menu. **Có từ đợt A** |

Bỏ từ ảnh: cấp và EXP người chơi, Mana, nhân vật lớn và bong bóng thoại, 4 icon nhỏ trên-phải, Guildhouse, Story tách khỏi bản đồ (Cốt truyện ở đây là mục xem lại), Clan và Announcement dạng cũ.

## Nút Thông báo

**Đợt C, chưa code.** Nút ở góc trên-phải, chấm đỏ khi có việc. Bấm thì hiện hộp thả xuống dưới nút, nền mờ che thanh dưới, bấm nền mờ thì đóng. Mỗi loại việc một dòng, theo thứ tự (user chốt 2026-11-09, đích sửa 2026-11-12):

| Dòng | Bấm thì |
|---|---|
| Sự cố đang chờ | về Lobby, kéo khung quán cho "!" ra giữa màn (không có mũi tên) |
| Quán thiếu người ở một vai (quán đứng yên) | về Lobby, mở trang Nhân viên |
| Coin AFK chờ thu | về Lobby, mở popup thu AFK |
| Thể lực đầy | mở **Chọn chế độ** (không mở thẳng bản đồ) |
| Nhân vật đủ mảnh nâng sao | mở Nhân vật, chọn sẵn người đầu tiên đủ mảnh |
| Phần thưởng chưa nhận (Hộp quà, Nhiệm vụ) | chưa làm, sang đợt Online |

## Luật đổi theo luồng mới

- **Sự cố khi mở popup** và **chuyên cần**: user chốt 2026-11-09, đã chép sang [game-design.md mục Quán](game-design.md#quán).
- **Thể lực mua được trong Shop** (luật cũ: không mua bằng Gem, [D-008](../decisions/decisions.md)). Mua bằng gì, giá bao nhiêu: `TODO(user)`, xem [ideas](ideas.md#đang-bàn).
- **Online:** xong trong bản 31/12 (user chốt 2026-11-09, đã ghi ở [vision](../vision/vision.md)); phạm vi còn chờ spec Online ([ideas](ideas.md#đang-bàn)).

Luật làm UI (ảnh mô phỏng duyệt trước, tên `<Tên>_PH_<rộng>x<cao>`, trang mới kín màn) ở [unity-workflow, UI và hình tạm](../guides/unity-workflow.md#ui-và-hình-tạm).

## Khác với spec UI hub

Spec từng đợt lấy file này làm bản chính. Chỗ spec khác file này thì 📐 báo 🎨 và đã chép vào các mục trên: đợt A (banner Gacha kín panel; thưởng popup màn ở cột phải; Thách đấu neo mép phải; ba trang quán và popup AFK vẫn chặn sinh sự cố), [đợt B mục 11](../superpowers/specs/2026-11-11-ui-hub-b-design.md), [đợt C mục 11](../superpowers/specs/2026-11-12-ui-hub-c-design.md).

## Ảnh còn thiếu

User thả vào `Mota/` theo tên gợi ý, agent cập nhật [ui-screens.md](../references/ui-screens.md).

| Màn | Cần ảnh gì | Tên gợi ý |
|---|---|---|
| Shop | trang shop chính: các ô gói, giá, ô xem quảng cáo nhận Gem | `shop-main.jpg` |
| Menu | popup hoặc trang menu: danh sách mục, nút đóng | `menu-popup.jpg` |
| Cài đặt | trang cài đặt: ngôn ngữ, âm thanh | `menu-settings.jpg` |
| Hồ sơ | popup hồ sơ người chơi: avatar, tên, ID | `profile-popup.jpg` |
| Cốt truyện | danh sách chương hoặc đoạn đã xem | `story-list.jpg` |
| Hộp quà | danh sách quà, nút nhận hết | `hub-presents-popup.jpg` |
| Nhiệm vụ | danh sách nhiệm vụ, nút nhận | `hub-missions.jpg` |

## Câu còn chờ user

- Thể lực mua trong Shop bằng gì, giá, giới hạn mỗi ngày.
- Luồng lần đầu chơi (hướng dẫn) chưa bàn. Đặt tên: đợt C cho sửa tên trong Hồ sơ, chưa có bước đặt tên lúc mới vào game.
