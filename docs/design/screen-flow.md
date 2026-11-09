# Luồng màn

> **Đọc khi:** làm UI hub, thêm màn mới, nối các màn với nhau, viết spec có chuyển màn.
> **Cập nhật khi:** user đổi cách đi giữa các màn, thêm hay bỏ một màn. Kênh 🎨 Game designer giữ file này.

Bàn với user và **user duyệt ngày 2026-11-09** (kênh 🎨). Ảnh tham khảo: [ui-screens.md](../references/ui-screens.md). Ý tưởng chưa chốt ở [ideas.md](ideas.md).

Trạng thái: **có** = đã có trong game · **mới** = user chốt, chưa làm (ghi đợt nếu đã chia) · **khoá** = có nút nhưng hiện "Sắp có" · **sau** = để sau, chưa hiện.

UI hub chia ba đợt ([spec đợt A](../superpowers/specs/2026-11-09-ui-hub-design.md)): **A** khung (xong, user playtest và duyệt 2026-11-09, nhánh `feature/ui-hub`), **B** bố cục Lobby ([mục dưới](#bố-cục-lobby-user-duyệt-ảnh-mô-phỏng-bản-2-2026-11-09)), **C** Tiêu đề, Chọn chế độ, nút Thông báo, Hồ sơ.

## Cấu trúc (user chốt 2026-11-09)

1. **Lobby = màn quán.** Quán chạy thật, chiếm cả màn hình, vuốt để kéo camera xem quanh quán (kéo camera: đợt B).
2. **Các màn khác, trừ Trận, là panel kín màn phủ lên Lobby**, bật/tắt ngay trong `Restaurant.unity` (đợt A; scene `Gacha`, `Characters`, `StageSelect` đã xoá, bản build còn Boot, Restaurant, Battle). Quán vẫn chạy bên dưới. Thanh dưới vẫn hiện để đổi mục. Bấm mục Quán thì về Lobby.
3. **Trận là màn riêng.** Không có thanh dưới, không có Menu, không có nút Thông báo. Đánh xong hay bỏ cuộc, bấm Quay lại thì về Lobby, panel bản đồ mở sẵn (đợt A).
4. **Nút Thông báo nằm trên mọi panel**, có số đếm. Bấm vào hiện danh sách việc cần làm, bấm một dòng thì tới đúng chỗ ([mục dưới](#nút-thông-báo); đợt C).

**Trang con và hộp** (đợt A): popup màn, chọn đội, màn cốt truyện giữ chỗ, lịch sử quay, Nhân viên (kèm chọn người cho ô), Nâng cấp, Chất lượng đều là **trang kín màn**, nút **Quay lại** ở góc trên-trái, thanh dưới vẫn hiện. Hộp xác nhận (chuyển nhân viên, đội thiếu người, quay tiếp, Reset) và popup thu AFK **giữ cỡ hộp**, nền mờ che cả thanh. Bấm một ô khác của thanh thì trang đang mở đóng như khi bấm Quay lại.

## Sơ đồ

Nhãn `(A)`, `(B)`, `(C)` là đợt làm.

```mermaid
flowchart TD
  Boot -->|đợt A: vào thẳng| Lobby
  Boot -.->|đợt C| Title["Tiêu đề (mới, C)"] -.->|chạm| Lobby
  Lobby -.->|vắng ≥ 5 phút| AFK["Popup thu AFK (có)"]
  subgraph Hub["Restaurant.unity: Lobby = quán + thanh dưới 6 ô + panel kín màn"]
    Lobby["Quán / Lobby (có, A)"]
    Chars["Nhân vật (có, A)"]
    Story["Cốt truyện (khoá, A: báo Sắp có)"]
    Stages["ô Màn chơi"] -->|đợt A| Map["Bản đồ (có, A)"]
    Stages -.->|đợt C| Mode["Chọn chế độ (mới, C)"] -.-> Map
    Gacha["Gacha (có, A)"] --> History["Lịch sử quay (có)"]
    Menu["Menu (có, A)"]
  end
  Map --> Info["Popup màn (có)"] --> Team["Chọn đội (có)"] --> Battle["Trận (có, scene riêng)"]
  Map --> StoryStage["Màn cốt truyện giữ chỗ (có)"]
  Battle -->|Quay lại: về Lobby, bản đồ mở sẵn| Map
  Battle -->|Thử lại khi thua| Battle
  Lobby -.->|Shop, nút + (B)| Shop["Shop (khoá, B)"]
  Lobby -.->|avatar| Profile["Hồ sơ (mới, C)"]
  Menu --> Lang["Ngôn ngữ Việt ↔ Anh (có)"]
  Menu --> Reset["Reset dữ liệu + xác nhận (có)"]
  Menu -->|chỉ bản dev| Dev["Panel DEV (có)"]
  Battle -->|menu tạm dừng → Cài đặt, bản dev| Dev
  Dev -->|Chiến đấu thử| Battle
```

## Các màn

| Màn | Mở từ | Quay về | Ảnh `Mota/` | Trạng thái |
|---|---|---|---|---|
| Tiêu đề | Boot, lúc mở app | chạm → Lobby | `title-touch-to-start.jpg` | mới, đợt C. Đợt A mở app vào thẳng Lobby |
| Lobby (quán) | ô **Quán**; về từ Trận | — | `hub-home-annotated-en.png`, `restaurant-layout-*` | có (đợt A): quán + thanh 6 ô. Phần hub của bố cục (avatar, thanh tài nguyên…) là đợt B |
| Thanh dưới 6 ô | mọi màn trừ Trận | — | `hub-home-annotated-en.png` | có (đợt A): `NavBar_PH_1920x120`, 6 ô `NavTab*_PH_320x120`, ô đang mở tô nổi. Ẩn trong cutscene, lộ, tổng của Gacha |
| Popup thu AFK | tự hiện khi vào Lobby sau khi vắng ≥ 5 phút; nút Thu AFK; dòng AFK trong Thông báo (C) | Lobby | — | có, giữ cỡ hộp |
| Nhân viên, Nâng cấp, Chất lượng | cột nút phải và thẻ sao ở Lobby | Quay lại → Lobby | `quest-team-select-popup.jpg` (Nhân viên) | có, thành trang kín màn (đợt A). Ba trang này vẫn chặn sinh sự cố ([game-design](game-design.md#quán)) |
| Nhân vật | ô **Nhân vật**; dòng "đủ mảnh" trong Thông báo (C) | ô khác của thanh | `character-list-en.png`, `character-upgrade.png` | có, panel kín màn (đợt A) |
| Cốt truyện | ô **Cốt truyện** | — | — (thiếu, xem [dưới](#ảnh-còn-thiếu)) | khoá: ô có ổ khoá, bấm hiện "Sắp có" (đợt A) tới khi có hội thoại Chương 1 |
| Chọn chế độ | ô **Màn chơi** | Lobby | `quest-mode-select.jpg` | mới, đợt C: thẻ "Chương chính" mở, ô khác khoá. Đợt A ô Màn chơi mở thẳng bản đồ |
| Bản đồ màn | ô **Màn chơi** (đợt A); về từ Trận | ô khác của thanh (nút Quay lại cũ đã bỏ) | `quest-stage-map*.jpg` | có, panel kín màn (đợt A) |
| Popup màn, chọn đội | bấm một nút màn đánh trên bản đồ | Quay lại → Bản đồ | `quest-stage-info-popup.jpg`, `quest-team-select-popup.jpg` | có, trang kín màn (đợt A). Popup màn: quái bên trái, thưởng và thể lực ở cột phải, nút Thách đấu neo mép phải |
| Màn cốt truyện giữ chỗ | bấm một nút màn cốt truyện trên bản đồ | Tiếp tục / Quay lại → Bản đồ | — | có, trang kín màn (đợt A) |
| Trận | chọn đội → vào trận; DEV "Chiến đấu thử" | Quay lại → Lobby, bản đồ mở sẵn; Thử lại → trận mới | `battle-*` | có (`Battle`), scene riêng, không có thanh. Vào trận là rời quán, sự cố bị huỷ |
| Gacha | ô **Gacha**; banner Gacha ở Lobby (B) | ô khác của thanh | `gacha-*` | có, panel kín màn (đợt A); banner kín panel; Lịch sử quay là trang kín màn |
| Shop | nút tròn Shop; nút + của Thể lực, Coin, Gem | Lobby | — (thiếu) | khoá, đợt B (nút); nội dung khi làm quảng cáo thưởng / mua |
| Menu | ô **Menu**, mở trên panel đang đứng | Đóng → panel đang đứng; bấm ô khác → ô đó | — (thiếu) | có, panel kín màn (đợt A): Ngôn ngữ (đổi xong vẫn ở đúng ô, Menu vẫn mở), Reset dữ liệu (hỏi xác nhận, xoá hết rồi về Lobby như game mới, [D-005](../decisions/decisions.md)), DEV (chỉ bản dev), Đóng |
| Hồ sơ | bấm avatar | Lobby | — (thiếu) | mới, đợt C: tên người chơi, đổi avatar bằng nhân vật đã có; online thêm ID, tài khoản. Chưa kịp 31/12 thì avatar chỉ là hình |
| Panel DEV | Menu → DEV; menu tạm dừng trong trận → Cài đặt | Đóng | — | có (đợt A): bỏ nút góc và 4 nút chuyển màn; còn Chiến đấu thử, Màn thử, Hồi thể lực, +Material, +Gem, Tua AFK, Ngôn ngữ, Reset, Đóng |
| Hộp quà, Thông báo tin, Nhiệm vụ, Hội | nút tròn ở Lobby | Lobby | — | sau, cần online ([ideas](ideas.md#để-sau)) |

Mỗi panel chỉ hiện loại tiền nó dùng (user chốt 2026-11-09; làm ở đợt B cùng thanh tài nguyên, đợt A mỗi màn giữ khối số tiền cũ). Lobby hiện đủ 4 loại; bản đồ và chọn đội hiện Thể lực; Gacha hiện Gem; Shop hiện Thể lực, Coin, Gem; **Nhân vật hiện Coin**, vì chọn một nhân vật sẽ sang màn nâng cấp nhân vật (`character-upgrade.png`; nâng cấp tốn gì đang bàn ở [ideas](ideas.md#đang-bàn)); Cốt truyện, Menu, Hồ sơ không hiện. Trong cutscene và màn lộ của Gacha thì ẩn thanh dưới và nút Thông báo.

## Bố cục Lobby (user duyệt ảnh mô phỏng bản 2, 2026-11-09)

**Đợt B, chưa làm.** Đợt A mới có thanh dưới 6 ô; Lobby vẫn giữ khối số tiền, thanh chất lượng và cột nút phải cũ (ảnh `hub-lobby.png` của đợt A). Khung 1920×1080. Kích thước ghi trong tên là cỡ thật.

```
[Avatar] [Thể lực 80/100 + | Coin + | Material | Gem +]                 [Báo 3]
         [★ chất lượng · ≈ Coin/giây · Phục vụ]
[Banner                                                            ]  [‹]  ← thanh quán thu gọn
 → Gacha]        QUÁN ĐANG CHẠY, FULL MÀN, VUỐT KÉO CAMERA         (mở: Nhân viên,
 ● ○ ○                                                               Nâng cấp, Thu AFK)
                                          (Shop) (Hộp quà) (Thông báo) (Nhiệm vụ) (Hội)
[ Quán | Nhân vật | Cốt truyện (khoá) | Màn chơi | Gacha | Menu ]
```

| Vùng | Tên object | Ghi chú |
|---|---|---|
| Avatar trên-trái | `Avatar_PH_140x140` | bấm mở Hồ sơ |
| Thanh tài nguyên | `ResourceBar_PH_1120x76` | **một** thanh chung: Thể lực x/100, Coin, Material, Gem. Thể lực, Coin, Gem có nút + mở Shop; Material không có |
| Thanh chất lượng | `QualityBar_PH_800x68` | sao chất lượng, Coin/giây, bộ đếm. Chỉ ở Lobby |
| Nút Thông báo trên-phải | `NotifyBtn_PH_110x100` | thay 4 icon nhỏ trong ảnh; có số đếm |
| Banner trái | `BannerGacha_PH_360x200` | giữ chỗ từ giờ, banner đầu trỏ tới Gacha; có chấm trang |
| Thanh quán thu gọn | `RestaurantBarToggle_PH_60x160`, `RestaurantBar_PH_180x460` | 3 nút đã có: Nhân viên, Nâng cấp, Thu AFK. Mặc định đóng, nhớ trạng thái lần trước; có Coin AFK chờ thu thì nút ‹ có chấm báo |
| Hàng nút tròn | `HubRoundBtn_PH_110x110` × 5 | nay chỉ hiện Shop (khoá). 4 ô còn lại giữ chỗ, mở khi có online |
| Thanh dưới | `NavTab<Ô>_PH_320x120` × 6 trong `NavBar_PH_1920x120` | Quán · Nhân vật · Cốt truyện (khoá) · Màn chơi · Gacha · Menu. **Có từ đợt A** |

Bỏ từ ảnh: cấp và EXP người chơi, Mana, nhân vật lớn và bong bóng thoại, 4 icon nhỏ trên-phải, Guildhouse, Story tách khỏi bản đồ (Cốt truyện ở đây là mục xem lại), Clan và Announcement dạng cũ.

## Nút Thông báo

**Đợt C, chưa làm.** Danh sách, theo thứ tự (user chốt 2026-11-09):

| Dòng | Bấm thì |
|---|---|
| Sự cố đang chờ | đóng popup, về Lobby, chỉ vào trạm có "!" |
| Quán thiếu người ở một vai (quán đứng yên) | về Lobby, mở bảng Nhân viên |
| Coin AFK chờ thu | mở popup thu AFK |
| Thể lực đầy | mở bản đồ màn |
| Nhân vật đủ mảnh nâng sao | mở Nhân vật, chọn sẵn nhân vật đó |
| Phần thưởng chưa nhận (sau này: Hộp quà, Nhiệm vụ) | mở đúng màn đó |

## Luật đổi theo luồng mới

- **Sự cố khi mở popup** và **chuyên cần**: user chốt 2026-11-09, đã chép sang [game-design.md mục Quán](game-design.md#quán).
- **Thể lực mua được trong Shop** (luật cũ: không mua bằng Gem, [D-008](../decisions/decisions.md)). Mua bằng gì, giá bao nhiêu: `TODO(user)`, xem [ideas](ideas.md#đang-bàn).
- **Online:** user nói game sẽ có online và **xong online trong bản 31/12**. Lệch với [vision](../vision/vision.md) (online đang ở "ngoài phạm vi") và roadmap. Chưa sửa vision.

## Luật làm UI (user đặt 2026-11-09)

- Mọi việc làm UI phải có **ảnh mô phỏng** cho user duyệt trước, rồi mới làm thật.
- Object chưa có hình đặt tên `<Tên>_PH_<rộng>x<cao>`, ví dụ `BannerGacha_PH_360x200`, để user hoặc agent dễ thay hình sau.

Hai luật này áp dụng cho mọi kênh; 🎯 đã ghi vào [unity-workflow.md](../guides/unity-workflow.md#ui-và-hình-tạm) mục "UI và hình tạm" (2026-11-09), nơi đó là bản chính.

## Khác với đề xuất UI hub

**Đã xử lý** (2026-11-09). Đề xuất đầu của 📐 (thanh 5 ô, thanh dựng trong từng scene, Menu là scene riêng) đã đổi theo luồng màn này trước khi user duyệt spec. Spec đợt A lấy file này làm bản chính và làm đúng: 6 ô, panel kín màn trong `Restaurant.unity` (user chọn panel, không nạp scene chồng), Menu là panel, DEV vào từ Menu, trận không có thanh. Phần còn lại chia cho đợt B và C (mục đầu file).

Chỗ đợt A làm khác ảnh mô phỏng, user duyệt cùng ảnh chụp lúc playtest:
- Gacha: banner kéo kín panel.
- Popup màn: phần thưởng nằm ở cột phải.
- Popup màn: nút Thách đấu neo mép phải.

Đợt A cũng chốt thêm một ngoại lệ của luật sự cố: trang Nhân viên, Nâng cấp, Chất lượng và popup AFK vẫn chặn sinh sự cố ([game-design](game-design.md#quán)).

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
- Luồng lần đầu chơi (đặt tên, hướng dẫn) chưa bàn.
