# Luồng màn

> **Đọc khi:** làm UI hub, thêm màn mới, nối các màn với nhau, viết spec có chuyển màn.
> **Cập nhật khi:** user đổi cách đi giữa các màn, thêm hay bỏ một màn. Kênh 🎨 Game designer giữ file này.

Bàn với user và **user duyệt ngày 2026-11-09** (kênh 🎨). Ảnh tham khảo: [ui-screens.md](../references/ui-screens.md). Ý tưởng chưa chốt ở [ideas.md](ideas.md).

Trạng thái: **có** = đã có trong game · **mới** = user chốt, chưa làm · **khoá** = có nút nhưng hiện "Sắp có" · **sau** = để sau, chưa hiện.

**UI hub đã xong** cả ba đợt: A khung ([spec](../superpowers/specs/2026-11-09-ui-hub-design.md)), B bố cục Lobby ([spec](../superpowers/specs/2026-11-11-ui-hub-b-design.md)), C Tiêu đề, Chọn chế độ, Thông báo, Hồ sơ ([spec](../superpowers/specs/2026-11-12-ui-hub-c-design.md), PR #17, 2026-11-13). Chỗ spec khác file này đã chép vào các mục dưới (mục 11 của mỗi spec).

**Online** ([spec](../superpowers/specs/2026-11-13-online-design.md), user duyệt 2026-11-13) chia bốn đợt, nhãn dưới đây: **O1** nền (đăng nhập, cổng mạng, ID, Reset trên server, Xoá tài khoản), **O2** logic server, **O3** liên kết tài khoản (Google, tên đăng nhập + mật khẩu) + Nhiệm vụ + Hộp thư (thư ghim) + quay miễn phí ([spec con O3](../superpowers/specs/2026-11-21-online-3-design.md), user chốt 2026-11-21, chờ duyệt bản viết), **O4** quà trong Hộp thư + hai dòng Thông báo.

## Cấu trúc (user chốt 2026-11-09)

1. **Lobby = màn quán.** Quán chạy thật, chiếm cả màn hình, vuốt để kéo xem quanh quán (hai chiều, dừng ở mép, không zoom).
2. **Các màn khác, trừ Trận và Tiêu đề, là panel kín màn phủ lên Lobby**, bật/tắt trong `Restaurant.unity`. Bản build có Boot, Title, Restaurant, Battle. Quán vẫn chạy bên dưới. Thanh dưới vẫn hiện để đổi mục. Bấm mục Quán thì về Lobby.
3. **Trận là màn riêng.** Không có thanh dưới, không có Menu, không có nút Thông báo. Đánh xong hay bỏ cuộc, bấm Quay lại thì về Lobby, panel bản đồ mở sẵn.
4. **Nút Thông báo nằm trên mọi panel và trang của hub** (trừ lúc thanh dưới ẩn). Có **chấm đỏ** khi có ít nhất một việc, không hiện số. Bấm vào hiện danh sách việc, bấm một dòng thì tới đúng chỗ ([mục dưới](#nút-thông-báo)).
5. **Phải có mạng mới chơi được** (O1, user chốt 2026-11-13, đổi vision). Tiêu đề: chạm → "Đang kết nối…" → Lobby. Ba hộp mạng (mục [Hộp mạng](#hộp-mạng)).

**Trang và hộp:** popup màn, chọn đội, màn cốt truyện giữ chỗ, Nhân viên (kèm chọn người cho ô), Nâng cấp, Chất lượng, Nhiệm vụ (O3), Hộp thư (O3) là **trang kín màn**, nút **Quay lại** ở góc trên-trái, thanh dưới vẫn hiện. Bấm ô khác của thanh thì trang đang mở đóng như khi bấm Quay lại. **Giữ cỡ hộp**, nền mờ che cả thanh: hộp xác nhận (chuyển nhân viên, đội thiếu người, quay tiếp, Reset, Xoá tài khoản), popup thu AFK, Lịch sử quay, hộp mạng. Hai ngoại lệ của luật "trang mới kín màn" (L-033, user chốt 2026-11-12): danh sách Thông báo là hộp thả xuống dưới nút, Hồ sơ là hộp giữa màn 1200×800.

## Sơ đồ

```mermaid
flowchart TD
  Boot --> Title["Tiêu đề (có)"] -->|chạm → Đang kết nối… (O1)| Lobby
  Title -.->|lỗi mạng (O1)| NoNet["Hộp Không kết nối được: Thử lại"]
  Lobby -.->|vắng ≥ 5 phút| AFK["Popup thu AFK (có)"]
  subgraph Hub["Restaurant.unity: Lobby = quán + thanh dưới 6 ô + panel kín màn"]
    Lobby["Quán / Lobby (có)"]
    Chars["Nhân vật (có)"]
    Story["Cốt truyện (khoá)"]
    Stages["ô Màn chơi"] --> Mode["Chọn chế độ (có)"] -->|Chương chính| Map["Bản đồ (có)"]
    Map -->|Quay lại| Mode
    Gacha["Gacha (có)"] --> History["Lịch sử quay (có, hộp)"]
    Menu["Menu (có)"]
  end
  Map --> Info["Popup màn (có)"] --> Team["Chọn đội (có)"] --> Battle["Trận (có, scene riêng)"]
  Map --> StoryStage["Màn cốt truyện giữ chỗ (có)"]
  Battle -->|Quay lại: về Lobby, bản đồ mở sẵn| Map
  Battle -->|Thử lại khi thua| Battle
  Lobby -.->|Shop, nút +| Shop["Shop (khoá)"]
  Lobby -->|avatar| Profile["Hồ sơ, hộp (có; ID O1, Google O3)"]
  Lobby -.->|nút tròn| Missions["Nhiệm vụ (mới, O3)"]
  Lobby -.->|nút tròn, hình phong thư| Mail["Hộp thư (mới, O3: thư ghim; O4: quà)"]
  Title -.->|nút Đăng nhập bằng tài khoản (O3)| Pick["Hộp chọn cách + tên/mật khẩu (O3)"]
  Profile -.->|Liên kết tài khoản (O3)| Pick
  Notify["Nút Thông báo, hộp thả xuống (có)"] -->|bấm dòng| Lobby
  Menu --> Lang["Ngôn ngữ Việt ↔ Anh (có)"]
  Menu --> Reset["Reset dữ liệu + xác nhận (có)"]
  Menu -.-> DelAcc["Xoá tài khoản + xác nhận (O1)"] -.->|Xoá| Title
  Menu -->|chỉ bản dev| Dev["Panel DEV (có)"]
  Battle -->|menu tạm dừng → Cài đặt, bản dev| Dev
  Dev -->|Chiến đấu thử| Battle
```

## Các màn

| Màn | Mở từ | Quay về | Ảnh `ReferenceImages/` | Trạng thái |
|---|---|---|---|---|
| Tiêu đề | Boot, mỗi lần mở app (không hiện khi về từ trận, đổi ngôn ngữ, Reset, quay lại từ nền) | chạm → Lobby | `title-touch-to-start.jpg` | có: scene `Title`, "Bun Dau Peo" trong `Logo_PH_900x300`, "Chạm để bắt đầu", số phiên bản, không có nút góc (O3 thêm nút nhỏ **Đăng nhập bằng tài khoản** dưới dòng chạm, mở hộp chọn cách; cảnh báo mất tiến độ ẩn danh). O1: chạm → chữ đổi thành "Đang kết nối…" → đăng nhập → Lobby; hộp "Tài khoản đang dùng ở máy khác" cũng đưa về đây |
| Lobby (quán) | ô **Quán**; về từ Trận | — | `hub-home-annotated-en.png`, `restaurant-layout-*` | có: quán 2.5D kéo được + bố cục hub ([mục dưới](#bố-cục-lobby-user-duyệt-ảnh-mô-phỏng-bản-2-2026-11-09)) |
| Thanh dưới 6 ô | mọi màn trừ Trận, Tiêu đề | — | `hub-home-annotated-en.png` | có: `NavBar_PH_1920x120`, 6 ô `NavTab*_PH_320x120`, ô đang mở tô nổi. Ẩn trong cutscene, lộ, tổng của Gacha |
| Popup thu AFK | tự hiện khi vào Lobby sau khi vắng ≥ 5 phút; nút Thu AFK; dòng AFK của Thông báo | Lobby | — | có, hộp |
| Nhân viên, Nâng cấp, Chất lượng | Nhân viên, Nâng cấp: thanh quán thu gọn bên phải; Chất lượng: bấm `InfoBar` | Quay lại → Lobby | `quest-team-select-popup.jpg` (Nhân viên) | có, trang kín màn. Ba trang này vẫn chặn sinh sự cố ([game-design](game-design.md#quán)). Nhân viên 3 cột × 3 ô to; Nâng cấp lưới thẻ có nút Mua |
| Nhân vật | ô **Nhân vật**; dòng "đủ mảnh" của Thông báo | ô khác của thanh | `character-list-en.png`, `character-upgrade.png` | có, panel kín màn |
| Cốt truyện | ô **Cốt truyện** | — | — (thiếu) | khoá: ổ khoá, bấm hiện "Sắp có" tới khi có hội thoại Chương 1 |
| Chọn chế độ | ô **Màn chơi** (bấm lại luôn về đây); dòng "Thể lực đầy" | ô khác của thanh | `quest-mode-select.jpg` | có: thẻ "Chương chính" (→ Bản đồ) và 4 ô khoá **Khó, Sự kiện, Thử thách, Đấu trường** ("Sắp có"; Đấu trường giữ khoá sau 31/12) |
| Bản đồ màn | Chọn chế độ → Chương chính; về từ Trận thì mở sẵn | **Quay lại** → Chọn chế độ | `quest-stage-map*.jpg` | có, panel kín màn |
| Popup màn, chọn đội | bấm một nút màn đánh trên bản đồ | Quay lại → Bản đồ | `quest-stage-info-popup.jpg`, `quest-team-select-popup.jpg` | có, trang kín màn. Popup màn: quái bên trái, thưởng và thể lực ở cột phải, nút Thách đấu neo mép phải |
| Màn cốt truyện giữ chỗ | bấm một nút màn cốt truyện | Tiếp tục / Quay lại → Bản đồ | — | có, trang kín màn |
| Trận | chọn đội → vào trận; DEV "Chiến đấu thử" | Quay lại → Lobby, bản đồ mở sẵn; Thử lại → trận mới | `battle-*` | có (`Battle`), không có thanh. Vào trận là rời quán, sự cố bị huỷ. O2: server trừ thể lực khi vào, trao thưởng khi thắng |
| Gacha | ô **Gacha**; banner Gacha ở Lobby | ô khác của thanh | `gacha-*` | có, panel kín màn, banner kín panel. Lịch sử quay là hộp tối viền sáng. O2: roll trên server, nút mờ + vòng chờ tới khi server trả. O3: mỗi ngày 1 lượt **Quay miễn phí** (nút Quay ×1 đổi chữ tới khi dùng), chỉ banner hiện tại |
| Shop | nút tròn Shop; nút + của Thể lực, Coin, Gem | — | — (thiếu) | khoá ("Sắp có"); nạp tiền và quảng cáo làm sau 31/12 |
| Menu | ô **Menu**, mở trên panel đang đứng | Đóng → panel đang đứng; bấm ô khác → ô đó | — (thiếu) | có, panel kín màn: Ngôn ngữ (đổi xong vẫn ở đúng ô, Menu vẫn mở; game mới và sau Reset là **tiếng Anh**), Reset dữ liệu, **Xoá tài khoản** (O1, dưới Reset), DEV (chỉ bản dev), Đóng |
| Hộp Reset dữ liệu | Menu → Reset | Huỷ → Menu; Xoá hết → Lobby như game mới | — | có, hộp ([D-005](../decisions/decisions.md)). O1: câu xác nhận thêm "Xoá cả dữ liệu trên máy chủ. Tài khoản và liên kết vẫn giữ."; Reset giữ ID người chơi; mất mạng thì không xoá gì |
| Hộp Xoá tài khoản | Menu → nút **Xoá tài khoản** (ngay dưới Reset, cùng cỡ, chữ đỏ) | Huỷ → Menu; Xoá → về **Tiêu đề**, chạm vào là người chơi mới | — | đang làm, O1 (nhánh `feature/online-1`, ảnh `online1-delete-account*.png`): hộp xác nhận đỏ "Xoá vĩnh viễn tài khoản và mọi dữ liệu, kể cả liên kết Google. Không lấy lại được.", nút [Xoá] [Huỷ]. Đường xoá trong app theo luật Google Play |
| Hồ sơ | bấm avatar | Đóng → Lobby | — (thiếu) | có, **hộp giữa màn 1200×800**: tên sửa được (tối đa 12 ký tự, rỗng thì "Người chơi" / "Player"), lưới nhân vật đang sở hữu để đổi avatar; Reset xoá cả hai. O1 (đang làm, ảnh `online1-profile*.png`): dòng **"ID: <PlayerId>"** + nút **Sao chép** ("Đã chép" 2 giây), ở cột trái dưới dòng "Tối đa 12 ký tự". O3: dòng **Tài khoản**: "Chưa liên kết" + nút **Liên kết tài khoản** (hộp chọn Google / tên đăng nhập + mật khẩu), hoặc "Đã liên kết: …" |
| Nhiệm vụ | nút tròn Nhiệm vụ ở Lobby; dòng Đi tới của việc tân thủ | Quay lại → Lobby | `hub-missions.png`, `hub-missions-daily.jpg`, `hub-missions-normal.jpg` | mới, O3, trang kín màn: tab Ngày / Tân thủ (số đỏ = số dòng nhận được); mỗi dòng nhãn, tên, thanh tiến độ, thưởng, nút **Đi tới** (chưa đủ) → **Nhận** (đủ) → **Đã nhận** (xám, tắt); việc khung giờ có "Chưa tới giờ"/"Đã lỡ"; việc khoá "Sắp có". Hàng dưới: Còn lại hh:mm (tab Ngày), Hoàn thành x/y, **Nhận hết** theo tab. Nhận thể lực chạm trần 999 thì hộp xác nhận. Không có thanh mốc, không tab Thành tựu. Luật ở [game-design](game-design.md#nhiệm-vụ); ảnh mô phỏng ở [spec con O3 mục 8.1](../superpowers/specs/2026-11-21-online-3-design.md) chờ user duyệt |
| Hộp thư | nút tròn Hộp thư (hình phong thư) ở Lobby; O4: dòng "quà chưa nhận" của Thông báo | Quay lại → Lobby | `hub-mailbox-en.png`, `restaurant-layout-target-en.png` (nút) | mới, O3, trang kín màn: danh sách thư trái, nội dung phải, Đính kèm, Nhận, **Nhận hết**, **Xoá đã đọc**, "Số lượng x/500", dấu ghim, chấm chưa đọc. O3 chỉ có **thư ghim "Liên kết tài khoản"** (không xoá được, biến mất sau khi liên kết, nút Liên kết). O4 thêm **thư quà từ server** (đính kèm, hạn, Nhận, Nhận hết; đã nhận thì mờ; chấm đỏ nút khi có thư chưa đọc hoặc quà chưa nhận; ảnh mô phỏng ở [spec con O4 mục 6](../superpowers/specs/2026-11-25-online-4-design.md)). Ảnh mô phỏng ở [spec con O3 mục 8.2](../superpowers/specs/2026-11-21-online-3-design.md) |
| Panel DEV | Menu → DEV; menu tạm dừng trong trận → Cài đặt | Đóng | — | có: Chiến đấu thử, Màn thử, Hồi thể lực, +Material, +Gem, Tua AFK, Ngôn ngữ, Reset, Đóng |
| Hội | — | — | — | sau 31/12, nút tròn chưa dựng |
| Thông báo tin từ server | — | — | — | bỏ khỏi bản 31/12 ([ideas](ideas.md#để-sau)) |

Mỗi panel chỉ hiện loại tiền nó dùng (user chốt 2026-11-09 và 2026-11-11). Lobby hiện đủ 4 loại; bản đồ, popup màn và chọn đội hiện Thể lực; Gacha (trang roll) hiện Gem; Shop hiện Thể lực, Coin, Gem; **Nhân vật hiện Coin** (nâng cấp nhân vật tốn gì đang bàn ở [ideas](ideas.md#đang-bàn)); **Nâng cấp quán hiện Material**; Nhân viên, Chất lượng, Cốt truyện, màn cốt truyện giữ chỗ, Menu, Hồ sơ không hiện; hộp che thanh nên không cần. Trong cutscene và màn lộ của Gacha thì ẩn thanh dưới và nút Thông báo.

## Hộp mạng

O1, user chốt 2026-11-13; đang làm trên nhánh `feature/online-1` (ảnh `online1-title-connecting*.png`, `online1-connect-failed*.png`). Cả ba dùng chung một hộp `OnlineBox_PH_900x420`, vẽ trên mọi màn, kể cả thanh dưới và nút Thông báo.

| Hộp | Khi nào | Nút |
|---|---|---|
| Không kết nối được | ở Tiêu đề, một bước đăng nhập lỗi | **Thử lại** (chạy lại từ bước lỗi). Chưa xong thì không vào được game |
| Mất kết nối | giữa game: lệnh server lỗi vì mạng, đẩy save lỗi quá 30 giây, hay chờ server quá 10 giây | **Thử lại**. Hộp chặn mọi chạm; quán vẫn chạy phía sau |
| Tài khoản đang dùng ở máy khác | máy khác vừa đăng nhập cùng tài khoản (máy đăng nhập sau thắng) | **Về màn Tiêu đề** (bỏ thay đổi chưa đẩy, đăng nhập lại) |
| Cần cập nhật (O2) | bản game quá cũ so với server: ở Tiêu đề khi đăng nhập, hay giữa game khi gửi lệnh bất kỳ | **không có nút**: "Bản game này đã cũ. Hãy cập nhật lên bản mới nhất để chơi tiếp." Chưa có nút mở cửa hàng |

**Màn chờ** (O2, user duyệt 2026-11-16, [spec con O2 mục 7.1](../superpowers/specs/2026-11-16-online-2-design.md)). Lệnh server do người chơi bấm (Quay ×1/×10, Vào trận, Kết quả trận, Nâng sao, Nhận, nút DEV gọi server) bật một lớp trong suốt chặn chạm toàn màn, kể cả thanh dưới và nút Thông báo, nằm dưới hộp mạng. Màn không tối đi. Sau 0,5 giây mới hiện ô vuông trắng xoay `WaitSpinner_PH_64x64` và chữ "Đang gửi…" `WaitLabel_PH_400x60` ở giữa màn; trả lời sớm hơn thì không nháy gì. Quá 10 giây hoặc lỗi mạng thì tắt lớp chờ, mở hộp Mất kết nối; Thử lại gửi lại đúng lượt đó.

## Bố cục Lobby (user duyệt ảnh mô phỏng bản 2, 2026-11-09)

Dựng ở UI hub đợt B, nút Thông báo ở đợt C. Khung 1920×1080, kích thước ghi trong tên là cỡ thật. Phòng quán lệch phải 155 px để banner không che quầy. Quán vẽ 2.5D: đồ có chiều cao và bóng; người có thân, đầu, bóng, đi thì nhún và đung đưa; bàn có đĩa khi khách ăn; khách chờ món có bóng "...", chờ trả tiền có đồng xu.

```
[Avatar] [Thể lực 80/100 + | Coin + | Material | Gem +]                 [Báo •]
         [★ chất lượng · ≈ Coin/giây · Phục vụ]
[Banner                                                            ]  [‹]  ← thanh quán thu gọn
 → Gacha]        QUÁN ĐANG CHẠY, FULL MÀN, VUỐT KÉO                (mở: Nhân viên,
                                                                     Nâng cấp, Thu AFK)
                                                      (Nhiệm vụ O3) (Hộp thư O3) (Shop)
[ Quán | Nhân vật | Cốt truyện (khoá) | Màn chơi | Gacha | Menu ]
```

| Vùng | Tên object | Ghi chú |
|---|---|---|
| Avatar trên-trái | `Avatar_PH_140x140` | bấm mở Hồ sơ |
| Thanh tài nguyên | `ResourceBar_PH_1120x76` | một thanh: Thể lực x/100 (kèm "+1 sau m:ss" khi chưa đầy), Coin, Material, Gem. Thể lực, Coin, Gem có nút + mở Shop ("Sắp có"); Material không có |
| Thanh chất lượng | `InfoBar` (tên cũ, 800×68, một hàng) | sao chất lượng, Coin/giây, bộ đếm. Chỉ ở Lobby; bấm mở Chất lượng |
| Nút Thông báo trên-phải | `NotifyBtn_PH_110x100` | chấm đỏ khi có việc |
| Banner trái | `BannerGacha_PH_360x200` | "Gacha / Quay ngay", bấm mở ô Gacha, đặt ở `(20, 340)`. Chấm trang làm khi có nhiều banner |
| Thanh quán thu gọn | `RestaurantBarToggle_PH_60x160`, `RestaurantBar_PH_180x460` | Nhân viên, Nâng cấp, Thu AFK (chỉ hiện khi có Coin chờ). Mặc định đóng; nhớ qua tắt mở game, Reset về đóng; nút ‹ có chấm báo khi có Coin AFK chờ |
| Hàng nút tròn | `HubRoundBtnShop_PH_110x110` | có Shop (khoá). Thêm **Nhiệm vụ** (O3) và **Hộp thư** (O3, hình phong thư, thay Hộp quà). Hội chưa dựng, Thông báo tin bỏ |
| Thanh dưới | `NavTab<Ô>_PH_320x120` × 6 trong `NavBar_PH_1920x120` | Quán · Nhân vật · Cốt truyện (khoá) · Màn chơi · Gacha · Menu |

Bỏ từ ảnh: cấp và EXP người chơi, Mana, nhân vật lớn và bong bóng thoại, 4 icon nhỏ trên-phải, Guildhouse, Story tách khỏi bản đồ, Clan và Announcement dạng cũ.

## Nút Thông báo

Có từ UI hub đợt C. Bấm thì hiện hộp thả xuống dưới nút, nền mờ che thanh dưới, bấm nền mờ thì đóng. Mỗi loại việc một dòng, theo thứ tự (user chốt 2026-11-09, đích sửa 2026-11-12, hai dòng cuối 2026-11-13):

| Dòng | Bấm thì |
|---|---|
| Sự cố đang chờ | về Lobby, kéo khung quán cho "!" ra giữa màn |
| Quán thiếu người ở một vai | về Lobby, mở trang Nhân viên |
| Coin AFK chờ thu | về Lobby, mở popup thu AFK |
| Thể lực đầy | mở Chọn chế độ |
| Nhân vật đủ mảnh nâng sao | mở Nhân vật, chọn sẵn người đầu tiên đủ mảnh |
| Phần thưởng nhiệm vụ chưa nhận (O4) | mở Nhiệm vụ, tab Ngày nếu có thưởng, không thì Tân thủ |
| Quà chưa nhận (O4) | mở Hộp thư, chọn thư quà đầu tiên chưa nhận |

## Luật đổi theo luồng màn

- **Sự cố khi mở popup** và **chuyên cần**: user chốt 2026-11-09, ở [game-design mục Quán](game-design.md#quán).
- **Online**, Nhiệm vụ, giờ server, Gem trên server: user chốt 2026-11-13, ở [game-design](game-design.md#tiền-tệ).
- **Thể lực mua được trong Shop**: còn bàn, ngược [D-008](../decisions/decisions.md), xem [ideas](ideas.md#đang-bàn).

Luật làm UI (ảnh mô phỏng duyệt trước, tên `<Tên>_PH_<rộng>x<cao>`, trang mới kín màn) ở [unity-workflow, UI và hình tạm](../guides/unity-workflow.md#ui-và-hình-tạm).

## Ảnh còn thiếu

User thả vào `ReferenceImages/` theo tên gợi ý, agent cập nhật [ui-screens.md](../references/ui-screens.md).

| Màn | Cần ảnh gì | Tên gợi ý |
|---|---|---|
| Shop | trang shop chính: các ô gói, giá, ô xem quảng cáo nhận Gem | `shop-main.jpg` |
| Menu | popup hoặc trang menu: danh sách mục, nút đóng | `menu-popup.jpg` |
| Cài đặt | trang cài đặt: ngôn ngữ, âm thanh | `menu-settings.jpg` |
| Hồ sơ | popup hồ sơ người chơi: avatar, tên, ID, liên kết tài khoản | `profile-popup.jpg` |
| Cốt truyện | danh sách chương hoặc đoạn đã xem | `story-list.jpg` |

## Câu còn chờ user

- Thể lực mua trong Shop bằng gì, giá, giới hạn mỗi ngày.
- Luồng lần đầu chơi (hướng dẫn) chưa bàn. Đặt tên hiện chỉ có trong Hồ sơ (nhiệm vụ tân thủ "Đặt tên" ở O3 nhắc người chơi làm), chưa có bước đặt tên lúc mới vào game.
