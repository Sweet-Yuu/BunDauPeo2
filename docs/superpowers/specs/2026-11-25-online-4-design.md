# Spec con: Online đợt 4 (quà phát từ server trong Hộp thư, hai dòng Thông báo)

> Spec cha: [online-design](2026-11-13-online-design.md) (mục 5.11, 16 và bảng đợt). Đợt 3: [online-3-design](2026-11-21-online-3-design.md) (mục 8.2 Hộp thư, khung `IMailSource`). Ngày viết 2026-11-25, 🎨 Online. **Chờ user duyệt.**
> "Spec 5.x" là mục của spec cha; "đợt 3 mục x" là spec con đợt 3.

## 1. Mục tiêu

User (chủ game) phát quà bằng cách sửa một danh sách trên Unity Dashboard; mọi người chơi đang trong hạn nhận được quà đúng **một lần** qua Hộp thư. Tiền và vật phẩm do **server** trao, máy không tự cộng Gem. Nút Thông báo thêm hai dòng nhắc phần thưởng chưa nhận.

Thành công khi:
1. Tạo quà trên Dashboard (không deploy module) thì lần mở Hộp thư kế tiếp có một thư quà, có chấm đỏ ở nút Hộp thư và dòng "Có quà chưa nhận" ở Thông báo.
2. Nhận một thư (hoặc Nhận hết) cộng đúng số một lần: gửi lại, mất trả lời rồi Thử lại, hai máy cùng nhận, chỉnh giờ máy đều không cộng hai lần.
3. Quà hết hạn không nhận được; quà sai định dạng trên Dashboard không làm hỏng danh sách của người khác.
4. **Bỏ cả đợt khi trễ mà đợt 3 vẫn chạy y nguyên** (mục 9).

**Ngoài phạm vi:** quà gửi riêng từng người hay theo ngày tạo tài khoản; Hộp quà là trang riêng (đợt 3 đã thay bằng Hộp thư); thư văn bản không kèm quà; thư người chơi gửi nhau; Hội; sửa quà ngay trong game (chỉ Dashboard); ExpPotion làm loại quà (luồng Nhân vật thêm sau, mục 4.2).

## 2. Đã chốt với user (2026-11-25)

| # | Chốt |
|---|---|
| 1 | Hộp quà **nằm trong Hộp thư** (đợt 3): mỗi quà là một thư. Khung ảnh theo `hub-mailbox-en.png`; không cần ảnh `hub-presents-popup.jpg`. Nút Hộp thư có chấm đỏ khi có thư mới |
| 2 | Quà phát cho **mọi người chơi**, kể cả tài khoản tạo sau `startUtc`, miễn còn trong hạn |
| 3 | **Cách B:** server trả danh sách quà (lệnh `ListGifts`); máy không tự đọc Custom data, không biết luật "trong hạn / đã nhận" |
| 4 | Hai dòng Thông báo `MissionReward`, `GiftReward` (spec cha 5.11, user duyệt 2026-11-13) |

## 3. Lựa chọn của 🎨 (user chưa xét riêng, sửa được)

1. **Hạn:** `startUtc <= giờ server < endUtc` (`endUtc` không gồm). Giờ server, không dùng giờ máy.
2. **Giới hạn đọc Dashboard** (chặn gõ nhầm, user chỉnh số được): tối đa **200 quà**, mỗi quà tối đa **10 dòng thưởng**, mỗi dòng `amount` từ 1 tới **1.000.000**. Quà vi phạm bị **bỏ qua riêng quà đó**, không nhận được; các quà khác vẫn chạy.
3. **Không tỉa `ClaimedGiftIds`** (tối đa vài chục id mỗi năm). Luật cho người vận hành: **không dùng lại id**, không xoá quà đã phát khỏi Dashboard rồi tạo lại cùng id (sẽ nhận lại được). Ghi vào `unity-workflow.md`.
4. **Thư quà đã nhận** hiện mờ tới khi quà hết hạn hoặc người chơi bấm **Xoá đã đọc** (cần `PlayerSave.MailDeleted`, mục 5.3). Thư quà **chưa nhận** không xoá được dù đã đọc.
5. **Chấm đỏ nút Hộp thư** = có thư chưa đọc **hoặc** có quà chưa nhận.
6. Lỗi mạng khi `ListGifts`: giữ danh sách quà lần trước, thư ghim vẫn hiện, không chặn trang. Lỗi khi **Nhận**: hộp Mất kết nối như Nhận nhiệm vụ.
7. **Editor Local:** quà đọc từ file `gifts.json` cạnh file save (không có file = không có quà); nút DEV (chỉ Local) ghi một quà mẫu. Test dùng danh sách dựng trong bộ nhớ.

## 4. Dữ liệu và luật

### 4.1 Danh sách quà (Dashboard)
Cloud Save **Custom data**, id `gifts`, khoá `list` (JSON, user sửa trên Unity Dashboard, không deploy):

```json
[
  { "id": "launch_2026", "titleVi": "Quà ra mắt", "titleEn": "Launch gift",
    "rewards": [ { "type": "Gem", "amount": 300 }, { "type": "Coin", "amount": 5000 } ],
    "startUtc": "2026-12-31T00:00:00Z", "endUtc": "2027-01-14T00:00:00Z" }
]
```

- `type` thuộc `Gem | Coin | Material`; loại lạ thì quà đó bị bỏ qua (thêm loại mới về sau là việc cộng thêm).
- `id` không rỗng, không trùng, tối đa 40 ký tự `[a-z0-9_]`; `endUtc > startUtc`.
- Chỉ server đọc. Máy không có quyền nhìn danh sách thô.

### 4.2 Trạng thái trên server
`ServerState.ClaimedGiftIds` (`List<string>`, mặc định rỗng, D-005): id quà đã nhận. Reset xoá (ghi đè bằng game mới); quà còn hạn nhận lại được sau Reset (spec cha 5.11, chấp nhận).

Luồng Nhân vật thêm `ExpPotion` vào `RewardData`: **không** nằm trong loại quà đợt này. Nếu cần thì thêm `ExpPotion` vào cuối bảng loại ở 4.1 và vào payload sau; không đổi tên chỗ nào của đợt 3.

### 4.3 Lệnh server (thêm cuối, `ProtocolVersion` giữ 3, `MinProtocolVersion` giữ 3: user chốt 2026-11-25, D-016, bản đợt 2 bị chặn)

| Lệnh | Vào | Đổi state? | Trả |
|---|---|---|---|
| `ListGifts` | (không) | Không | `GiftListPayload`: các quà **đang trong hạn**: `Id`, `Title` (theo ngôn ngữ máy gửi), `Rewards`, `EndUtcTicks`, `Claimed` |
| `ClaimGift` | `requestId`, `giftId` | `ClaimedGiftIds`, `Gem` | `GiftClaimPayload { Gem, Coin, Material }` |
| `ClaimAllGifts` | `requestId` | như trên, mọi quà chưa nhận trong hạn | như trên (tổng) |

- `ListGifts` kèm `lang` (`vi`/`en`) để server chọn `titleVi`/`titleEn`; thiếu bản đúng thì dùng bản kia.
- `ClaimGift`: `giftId` rỗng hoặc không có trong danh sách hợp lệ → `Invalid`; có nhưng ngoài hạn → `NotInWindow`; đã nhận → `AlreadyClaimed`. `ClaimAllGifts` không có quà nào nhận được → `NothingToClaim`.
- **Không thêm `CommandCode` mới.** Từ chối luôn đứng trước bước đổi đầu tiên (như `ClaimMission`).
- **Gem** cộng vào `ServerState.Gem`. **Coin, Material** trả trong payload, máy cộng vào `Wallet` rồi lưu `save` (cùng cách thưởng màn, spec 5.2). Máy chỉ gửi `giftId`, không bao giờ gửi số lượng.
- Mọi lệnh đổi state theo luật chung của đợt 2: `requestId` (một lần chạy, lặp lại thì trả kết quả đã ghi), phiên khác → `OtherDevice`.
- Server đọc Custom data mỗi lệnh (không cache lâu trong module); đọc lỗi → lỗi server (`ServerError`), **không** coi như danh sách rỗng.

## 5. Máy

### 5.1 `GiftService` (thư mục `Missions/`)
- Giữ bản sao `ListGifts` gần nhất (danh sách + thời điểm). `RefreshAsync()` gọi lúc vào game (sau Bootstrap) và mỗi lần mở Hộp thư; `ClaimAsync(id)`, `ClaimAllAsync()` gọi lệnh, áp `ServerState` trả về, cộng Coin/Material vào `Wallet`, lưu, rồi làm mới danh sách.
- `Changed` để trang và nút vẽ lại. Không gọi server từ UI trực tiếp.

### 5.2 Thư quà vào khung đợt 3
- `GiftMailSource : IMailSource` đọc `GiftService`, đăng ký thêm trong `OnlineStartup` (một dòng cạnh `LinkAccountMailSource`).
- `MailItem` thêm: `Rewards` (danh sách dòng thưởng), `ExpiresUtcTicks`, `Claimed`. Id thư `gift:<id>`. `MailAction` thêm `ClaimGift` ở cuối. Thư quà **không ghim**, xếp dưới thư ghim, mới nhất (hạn gần nhất) lên trước.
- `HasAttachments` đúng cho thư quà; nút **Nhận hết** sáng khi còn quà chưa nhận (đợt 3 đang mờ luôn).

### 5.3 Save
`PlayerSave.MailDeleted` (`List<string>`, mặc định rỗng, D-005): id thư quà đã nhận và bị **Xoá đã đọc**; thư trong danh sách này không dựng lại. Reset ghi đè rỗng. Không thêm field nào khác.

## 6. Giao diện

Theo `hub-mailbox-en.png` (đã có trong đợt 3), chỉ thêm trạng thái thư quà. Không thêm trang, không thêm nút tròn.

```
┌──────────────────────────────────────────────────────────────────────────┐
│ [‹ Quay lại]  Hộp thư                                                    │
│ ┌────────────────────────┐ ┌───────────────────────────────────────────┐ │
│ │ ✉ Liên kết tài khoản ★•│ │ Quà ra mắt                                │ │
│ │ ✉ Quà ra mắt         • │ │ Đội ngũ BunDauPeo                2026-12-31│ │
│ │   Hạn 14/01 07:00      │ │                                           │ │
│ │ ✉ Quà tuần 1 (mờ)      │ │ Cảm ơn bạn đã chơi ngay từ ngày đầu!      │ │
│ │   Đã nhận              │ │                                           │ │
│ │ Số lượng 3/500         │ │ Đính kèm  [Gem 300] [Coin 5.000]          │ │
│ │ [Nhận hết][Xoá đã đọc] │ │ Hạn: 14/01/2027 07:00        [   Nhận   ] │ │
│ └────────────────────────┘ └───────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────┘
```

- Thư quà chưa nhận: chấm đỏ nếu chưa đọc; **Nhận** sáng. Đã nhận: mờ, dòng "Đã nhận", **Nhận** thành "Đã nhận" mờ. Hết hạn khi trang đang mở: **Nhận** trả `NotInWindow`, trang làm mới và thư biến mất kèm toast "Quà đã hết hạn".
- Nhận thành công: toast tổng ("Nhận Gem 300, Coin 5.000"); Gem trên thanh đầu cập nhật.
- Giờ hạn hiển thị theo UTC+7, tính từ `ServerTime`, không dùng giờ máy.
- Tên object thêm (`_PH_<w>x<h>`): `MailAttachChip_PH_220x70`; còn lại dùng lại object đợt 3 (`MailItem_PH_600x120`, `MailAttach_PH_1000x120`, `MailBtn_PH_300x90`, `MailClaimAll_PH_280x80`, `MailDeleteRead_PH_280x80`).
- **Nút Hộp thư ở Lobby:** chấm đỏ theo lựa chọn 5.

### 6.1 Nút Thông báo
`NotifyKind` thêm **cuối enum**: `MissionReward`, `GiftReward` (thứ tự đứng sau `StarUp`, `NotifyPopup` dựng 7 dòng).

| Dòng | Hiện khi | Chữ | Bấm |
|---|---|---|---|
| `MissionReward` | còn thưởng Nhiệm vụ nhận được (lấy từ `MissionService`) | "Có phần thưởng nhiệm vụ chưa nhận" | mở Nhiệm vụ, tab Ngày nếu tab đó có thưởng, không thì Tân thủ |
| `GiftReward` | còn quà chưa nhận trong hạn | "Có quà chưa nhận" | mở Hộp thư, chọn thư quà đầu tiên chưa nhận |

### 6.2 Chữ mới (`UiText.Hub.cs`, D-012)
Khu `mail.gift_*` (đính kèm, hạn, Nhận, Đã nhận, Hết hạn, toast nhận, "Chưa có quà"), `notify.mission_reward`, `notify.gift_reward`. Mỗi khoá có bản Anh; `HardcodedTextTests`, `UiTextTests` xanh; kiểm bề rộng bản Anh dài nhất (L-039). Tên thư và nội dung quà lấy từ Dashboard theo ngôn ngữ, không qua `UiText`.

## 7. Lỗi và biên

| Tình huống | Hành vi |
|---|---|
| Danh sách rỗng, JSON hỏng, đọc lỗi | JSON hỏng/lỗi đọc: `ListGifts` lỗi, máy giữ danh sách cũ; rỗng hợp lệ: không có thư quà |
| Quà vi phạm giới hạn 3.2 | bỏ riêng quà đó, ghi log server |
| Hai quà cùng id | giữ quà đầu, bỏ quà sau (log) |
| Nhận cùng lúc từ hai máy | máy sau → `OtherDevice` (phiên) hoặc `AlreadyClaimed`; không cộng hai lần |
| Mất trả lời rồi Thử lại | server trả kết quả đã ghi (`LastResultJson`), máy cộng Coin/Material một lần |
| Máy tắt đúng lúc giữa "server ghi" và "máy cộng Coin/Material" | mất Coin/Material của lượt đó (Gem vẫn đúng). **Giống thưởng màn đợt 2** (spec cha 5.2); chấp nhận ở mức 2, ghi vào rủi ro |
| Sang quá `endUtc` khi đang mở trang | `NotInWindow`, làm mới |
| Người chơi đợt 3 (bản cũ) chơi sau deploy | lệnh mới chỉ thêm, bản cũ không gọi: không đổi gì |
| `state` đợt 3 mở bằng bản đợt 4 | `ClaimedGiftIds` thiếu = rỗng |

## 8. Khác với spec cha (đã chốt)

1. 5.11: **Hộp quà là thư trong Hộp thư**, không có nút tròn/trang riêng (đã chốt ở đợt 3).
2. 5.11: máy **không đọc** Custom data; server trả danh sách qua `ListGifts` (user chốt B, 2026-11-25).
3. 5.11: danh sách `ListGifts` kèm cờ `Claimed`, để thư đã nhận còn mờ cho tới khi hết hạn/xoá.
4. Thêm `PlayerSave.MailDeleted` (spec cha chỉ nêu `ClaimedGiftIds`).

## 9. Thứ tự cắt khi trễ (đề xuất, chờ user duyệt)

Thứ tự của user: Hộp quà là mục cắt đầu tiên của cả dự án. Trong đợt 4:
1. **Hai dòng Thông báo** (Task 6) — cắt riêng được; Hộp thư vẫn có chấm đỏ.
2. **Nút DEV và `gifts.json` Local** (Task 7 Step 1) — chỉ phục vụ thử.
3. **Cả đợt (Task 1–7)** — **không đổi chữ ký nào của đợt 3**: lệnh, enum, trường đều chỉ thêm cuối; bỏ cả đợt thì đợt 3 chạy y nguyên. Không có Task nào của đợt này là điều kiện của đợt 3.

Không cắt dở nửa đợt (chỉ server hay chỉ UI đều không có ích cho người chơi). Ngày công khai 31/12 không dời vì đợt này.

## 10. Rủi ro

1. **Quà là tiền:** trùng nhận, hết hạn, mất trả lời (Review Focus trong kế hoạch).
2. **Dashboard sai tay:** JSON hỏng hay quà sai định dạng. Giảm: server kiểm từng quà, chỉ bỏ quà hỏng; hướng dẫn mẫu JSON trong `unity-workflow.md`.
3. **Đọc Custom data trong Cloud Code** chưa từng dùng ở đợt trước: Task 3 phải **gọi thật trên `development`** trước khi tin (như O2-b).
4. **Khoảng hở Coin/Material** (mục 7): chấp nhận, đã có ở thưởng màn.
5. **Chồng khoá Unity:** đợt 4 sửa `ProjectSetup.cs`, `UiText.Hub.cs`, `NotifyPopup.cs` cùng nơi với luồng Nhân vật (thứ tự khoá Unity: cấp nhân vật trước, đợt 4 sau); `RewardData` do Nhân vật sửa (thêm `ExpPotion`), đợt 4 không đụng; **nhánh code O4 cắt từ `main` sau khi PR Cấp nhân vật merge** (chung `PlayerSave`/`ServerProtocol`/module; `development` đang chạy bản Cấp nhân vật, deploy bản thiếu `ExpPotion` sẽ gỡ nó).
6. **Ngày:** O4 ~28/12, sát công khai 31/12; vì thế cắt được toàn bộ.

## 11. Docs phải cập nhật khi xong

| File | Việc |
|---|---|
| `docs/design/screen-flow.md` | Hộp thư trạng thái thư quà (O4), dòng Thông báo `MissionReward`/`GiftReward`, bỏ dòng ảnh còn thiếu `hub-mailbox-gift.png` |
| `docs/references/ui-screens.md` | ghi `hub-mailbox-en.png` là ảnh chuẩn cho thư quà |
| `docs/guides/unity-workflow.md` | mục Online: lệnh mới, **hướng dẫn tạo quà trên Dashboard** (JSON mẫu, luật không dùng lại id), `gifts.json` Local |
| `docs/design/game-design.md` | Gem nguồn "Hộp quà trong Hộp thư" (đã có), bảng loại quà |
| `docs/roadmap.md`, `docs/project/structure.md` | dòng Online đợt 4; thư mục/ file mới |
| spec cha 5.11 | ghi chú trỏ về spec con này |
