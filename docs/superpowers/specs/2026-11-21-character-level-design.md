# Spec: Cấp người chơi, Cấp nhân vật, Thuốc EXP

> Ngày: 2026-11-21 · Trạng thái: **bản nháp, chờ user duyệt** · Kênh: 🎨 Nhân vật · Spec Cấp nhân vật · Nhánh: `docs/cap-nhan-vat-spec` (từ `feature/online-2`; PR #22 chưa merge, merge xong thì rebase về `main`)
> Phạm vi: cấp người chơi (EXP từ thể lực), cấp nhân vật (tiêu thuốc EXP + Coin), thuốc EXP (rơi từ màn qua server, rơi từ khách ở quán), chỉ số trận theo cấp, quy tắc chi phí "chính + Coin phụ".
> Nguồn: [vision](../../vision/vision.md) (cấp nhân vật vào bản 31/12, user chốt 2026-11-21), [game-design](../../design/game-design.md#nhân-vật), [spec Nâng sao](2026-11-08-star-up-design.md), [spec Online đợt 2](2026-11-16-online-2-design.md), [D-005](../../decisions/decisions.md), [ideas](../../design/ideas.md) (ý "nâng cấp nhân vật bằng Coin", 2026-11-09 và 2026-11-11).

## 1. Mục tiêu

Người chơi **nâng cấp nhân vật và thấy chỉ số trận tăng**. Cấp nhân vật bị chặn bởi **cấp người chơi**, cấp người chơi tăng khi **tiêu thể lực vào trận**. Nâng cấp tốn **thuốc EXP** (chi phí chính, lấy từ trận và từ quán) và **Coin** (chi phí phụ).

Vì sao: sao (nâng bằng mảnh gacha) chỉ cho người chơi gặp may; cấp cho mọi người chơi một đường tăng sức mạnh đều đặn nhờ chơi, và nối ba vòng lặp lại với nhau: quán cho Coin và một ít thuốc, trận cho thuốc và Material, thuốc + Coin đổi thành sức mạnh trận. Cấp tăng trần theo cấp người chơi để người chơi luôn có lý do quay lại đánh màn.

Tiêu chí xong (người chơi thấy được):
1. Mỗi lần vào trận thành công, EXP người chơi tăng đúng bằng thể lực đã trừ; đủ EXP thì lên cấp, nhận thưởng lên cấp, thanh EXP hiện trên màn.
2. Thắng màn đánh nhận thêm thuốc EXP ngoài Gem và Material; kết quả trận hiện số thuốc.
3. Khách ở quán trả tiền xong có tỉ lệ nhỏ rơi 1 thuốc; thuốc cũng cộng khi vắng mặt (AFK).
4. Ở màn Nhân vật, chọn nhân vật rồi bấm Nâng cấp: cấp +1, thuốc và Coin giảm đúng giá, chỉ số HP/Tấn công/Thủ trên panel tăng đúng công thức. Thiếu thuốc, thiếu Coin hoặc cấp nhân vật đã bằng cấp người chơi thì nút mờ; gọi thẳng service cũng không đổi gì.
5. Vào trận, nhân vật đánh với chỉ số đã cộng cả sao lẫn cấp.
6. Tắt rồi mở lại: cấp, EXP, thuốc còn nguyên. Save cũ (chưa có dữ liệu này) nạp được, mọi nhân vật Lv1, người chơi Lv1 và 0 EXP. Reset Save Data xoá cả cấp, EXP và thuốc.

## 2. Quyết định của user (2026-11-21)

| # | Chủ đề | Chốt |
|---|---|---|
| 1 | Cấp tăng chỉ số nào | **HP, Tấn công, Thủ**, giữ Tốc độ và chỉ số quán, như nâng sao. Cộng thêm phần trăm; chi tiết ở mục 3 (lựa chọn của 🎨). |
| 2 | Nơi lưu | **Cấp lưu phía máy** (`PlayerSave`) cho bản 31/12. **Chuyển lên server là việc bắt buộc sau 31/12** (ghi thành quyết định trong `decisions.md`, mục 10). |
| 3 | Mô hình cấp | **Theo Princess Connect:** trần cấp nhân vật = **cấp người chơi**, không theo sao. Thêm hẳn **cấp người chơi** (EXP tài khoản). |
| 4 | Quy tắc chi phí | Mọi nâng cấp có **chi phí chính** (thuốc EXP cho cấp; mảnh cho sao; nguyên liệu cho skill về sau) và **Coin là chi phí phụ**. |
| 5 | Nguồn thuốc | **Rơi từ màn** (qua server, như Material) và **quán rơi theo tỉ lệ khách**, quán là nguồn **phụ** (khoảng 1/4 tổng), có cộng khi AFK. |
| 6 | Số liệu | Mọi số ở đây là *khởi điểm* trong dữ liệu cân bằng; **chỉ tính lại khi có bản cuối tháng 12**. Không cân bằng trước đó. |

## 3. Lựa chọn của 🎨 (user chưa xét riêng, sửa được)

1. **Công thức chỉ số cộng dồn phần trăm:** `HP/Tấn công/Thủ = (gốc × (100 + StatPercentPerStar × lầnNângSao + StatPercentPerLevel × (cấp − 1)) + 50) / 100`, làm tròn nửa lên, số nguyên `long`, đúng kiểu `StarService.GetCombatStats`. Cộng (không nhân) để cân bằng dễ và Lv1 không đổi gì so với hôm nay. *Khởi điểm* `StatPercentPerLevel = 2` (Lv20 = +38%, ngang tổng sao +40%).
2. **Mỗi lần bấm nâng 1 cấp.** Không có thanh EXP riêng của nhân vật, không "nâng tối đa" (để sau).
3. **Giá nâng theo cấp đích T:** thuốc = `2 × T`, Coin = `100 × T` (*khởi điểm*). Nâng một nhân vật từ Lv1 lên Lv20 tốn 418 thuốc và 20.900 Coin.
4. **Cấp người chơi tối đa 20**; EXP từ cấp L lên L+1 = `100 + 20 × L`; tổng tới Lv20 khoảng 5.700 EXP. Đủ EXP ở cấp tối đa thì **bỏ phần dư**. Chương 1 mới có 5 màn đánh nên không đặt cao hơn.
5. **EXP người chơi = thể lực đã trừ khi vào trận thành công** (màn đánh 10). Máy cộng ở lần trả lời thành công đầu của một lượt bấm, y như luật Material (spec Online đợt 2, mục 3.7), nên gửi lại không cộng hai lần. Màn cốt truyện không tốn thể lực nên không cho EXP. EXP vẫn tính khi trận sau đó thua hoặc bỏ cuộc (thể lực đã trừ, D-008).
6. **Thưởng lên cấp người chơi:** Coin và thuốc, *khởi điểm* `200 × cấp mới` Coin và `5` thuốc. **Không có Gem** (Gem do server giữ, máy không được cộng).
7. **Thuốc từ màn** *khởi điểm* thắng lần đầu **10**, đánh lại **3**; thuốc từ quán mỗi khách trả tiền **3%** rơi 1. Tính lại khi cân bằng.
8. **Cấp hiệu lực** của một nhân vật = `min(cấp đã lưu, cấp người chơi, cấp tối đa)`, nên chỉ số không bao giờ vượt trần dù save bị sửa hay cấp người chơi tụt khi reset. UI hiện **cấp hiệu lực**; cấp đã lưu **không bị ghi đè** (không clamp rồi lưu). Reset Save Data xoá cả `PlayerExp` lẫn `CharacterLevels`, nên trường hợp cấp lưu cao hơn cấp người chơi chỉ xảy ra khi save bị sửa tay.
11. **Cộng thưởng lên cấp cho từng cấp đạt được:** lên 3 cấp trong một lần `AddExp` thì nhận thưởng 3 lần (mỗi cấp mới một phần).
9. **`MinProtocolVersion` giữ 2, `ProtocolVersion` lên 3.** Bản cũ vẫn chơi được, chỉ không nhận thuốc (`JsonUtility` bỏ qua trường lạ). Người thử bản cũ không bị ép cập nhật.
10. **Không thêm màn mới.** Cấp người chơi hiện ở thanh trên; nâng cấp nhân vật nằm trong panel màn Nhân vật. Thuốc hiện trong panel đó, **không** thêm vào thanh tài nguyên (đợt B UI hub đã chốt thanh này).

## 4. Phương án đã cân

Tầng code, ba phương án:

- **A (user chọn): hai service mới.** `PlayerLevelService` (EXP, cấp, thưởng lên cấp), `CharacterLevelService` (nâng cấp nhân vật, trần theo cấp người chơi) và một hàm gộp chỉ số (sao + cấp) đưa vào `BattleSession` qua tham số `statsOf` có sẵn. Dữ liệu nằm trong `PlayerSave`, mọi hàm đồng bộ. *Đánh đổi:* thêm vài file, đổi lại test độc lập và không dính phần `async` của server.
- **B: nhét cấp vào `StarService`.** Loại: `StarService` đã `async` vì gọi server, còn cấp là cục bộ, hai kiểu hàm lẫn nhau và test khó hơn.
- **C: lưu cấp trong `Roster`.** Loại: `Roster` giữ dữ liệu server giữ (nhân vật, mảnh); cấp cục bộ lẫn vào sẽ sai ranh giới, dễ mất khi máy áp lại `state`.

Nguồn thuốc, ba phương án (user chọn B):

- A: thuốc chỉ phát trên máy (thưởng cấp, nhiệm vụ, quán): không đổi server nhưng nguồn thuốc không gắn với chiến đấu.
- **B (user chọn): thuốc rơi từ màn qua server** (như Material), cộng quán rơi theo khách. Phải đổi dữ liệu thưởng màn và triển khai lại server.
- C: dùng Material làm chi phí chính, không có thuốc riêng: Material đã dùng cho nâng cấp quán nên hai thứ tranh nhau.

Trần cấp: so với Epic Seven (trần theo sao) và AFK Arena (trần theo bậc thăng), user chọn kiểu Princess Connect (trần theo cấp người chơi) vì sao vốn đã có trục riêng (hệ số) và không muốn SSR có trần cao hơn R.

## 5. Dữ liệu

### 5.1 `PlayerSave` (máy giữ, D-005)

```csharp
public long PlayerExp = 0;                              // tổng EXP đã nhận; cấp suy ra từ đường cong
public List<CharacterLevelEntry> CharacterLevels = new(); // {string CharacterId, int Level}; chưa có dòng = Lv1
public long AfkPotionRemainderCoin = 0;                 // phần Coin AFK lẻ chưa đủ đổi 1 thuốc (6.5)
```

- Cả hai nằm trong khoá `save` (không nằm trong `ServerState`); `ServerStateApplier.ToUploadJson` không bỏ chúng.
- Thuốc EXP là một dòng trong `Balances` (loại tiền mới, 5.2), nên đi chung đường lưu, tải lên và reset của `Wallet`.
- Giá trị khởi tạo của game mới và của Reset: EXP 0, danh sách rỗng, thuốc 0 (D-005 điều 2).
- `ServerStateApplier.PruneUnowned` dọn thêm dòng `CharacterLevels` của nhân vật không còn trong `OwnedCharacterIds` (cùng chỗ đã dọn `BattleTeam`). Dòng cấp mồ côi không được tính vào chỉ số.

### 5.2 Loại tiền mới

`CurrencyType` thêm `ExpPotion = 3` (`Rules/CurrencyType.cs`; giá trị cũ giữ nguyên). Đây là số dư do máy giữ như Material.

### 5.3 Dữ liệu cân bằng (máy, ScriptableObject)

`LevelConfig.asset` (assembly `Characters`, mẫu theo `StarConfig`), có `Validate()` báo lỗi theo tên field:

| Field | Khởi điểm | Ý nghĩa |
|---|---|---|
| `MaxPlayerLevel` | 20 | Cấp người chơi tối đa |
| `ExpBase`, `ExpPerLevel` | 100, 20 | EXP từ cấp L lên L+1 = `ExpBase + ExpPerLevel × L` |
| `StatPercentPerLevel` | 2 | Phần trăm chỉ số gốc mỗi cấp nhân vật |
| `PotionPerTargetLevel`, `CoinPerTargetLevel` | 2, 100 | Giá nâng lên cấp đích T = hệ số × T |
| `LevelUpRewardCoinPerLevel`, `LevelUpRewardPotion` | 200, 5 | Thưởng khi lên cấp người chơi |
| `RestaurantPotionChancePercent` | 3 | Tỉ lệ khách trả tiền rơi 1 thuốc |
| `AfkCoinPerPotion` | 500 | Mỗi bấy nhiêu Coin AFK thu về đổi 1 thuốc (6.5); giả định, đặt cho khớp tỉ lệ quán, tính lại khi cân bằng |

`Validate` từ chối: `MaxPlayerLevel < 1`, hệ số âm, tỉ lệ ngoài 0–100. Config lỗi thì mọi hàm ghi từ chối bằng `InvalidConfig` và `GetCombatStats` trả chỉ số gốc (trận vẫn chạy), như `StarService`.

### 5.4 Thưởng thuốc của màn (đi qua server)

- `RewardData` (`Rules/GameData.cs`) thêm `long ExpPotion`.
- `StageRewardPayload` (`Rules/ServerHandlers.Stage.cs`) thêm `long ExpPotion`; `CompleteStage` trả đúng `reward.ExpPotion` (không đổi `ServerState`, vì thuốc là số dư của máy).
- `StageDefinition` (SO của máy): các dòng thưởng `CurrencyAmount` được phép có `ExpPotion` (hiện `CheckRewardLines` chặn loại tiền lạ và Coin); `GameDataBuilder.Reward(...)` map thêm. Thưởng màn vẫn **không** được có Coin; màn cốt truyện vẫn không có thưởng.
- Máy cộng thuốc vào `Wallet` ở `StageProgressService.CompleteStageAsync`, ngay cạnh dòng cộng Material, và thêm dòng thuốc vào danh sách `rewards` để popup kết quả hiện.
- `ServerProtocol.Version` = 3, `MinProtocolVersion` giữ 2.

## 6. Phía máy

### 6.1 `PlayerLevelService` (`Characters` hoặc `Core`, builder chọn theo asmdef)

- `int GetLevel()`, `long GetExp()`, `long GetExpToNext()`, `bool IsMaxLevel`.
- `void AddExp(long amount)`: `amount <= 0` thì bỏ qua; ở cấp tối đa thì bỏ phần dư; một lần cộng có thể lên nhiều cấp. Mỗi cấp mới cộng thưởng (Coin + thuốc) vào `Wallet`. Phát một sự kiện `Changed` sau khi mọi thứ đã đổi; `persist()` đúng một lần.
- Gọi từ `CombatService` ở bước vào trận thành công, với số thể lực màn vừa trừ (xem 6.4).

### 6.2 `CharacterLevelService`

- `int GetLevel(id)`: cấp hiệu lực theo mục 3.8; id lạ hoặc `null` trả 1.
- `int MaxLevel` = cấp người chơi hiện tại.
- `GetNextCost(id)`: `(thuốc, Coin)` cho cấp đích.
- `LevelUpResult TryLevelUp(id)` (đồng bộ), mọi bước từ chối đứng trước bước đổi:
  1. `!IsConfigValid` → `InvalidConfig`;
  2. id không có trong catalog → `Unknown`;
  3. chưa sở hữu → `NotOwned`;
  4. `GetLevel(id) >= MaxLevel` → `AtPlayerLevel` (nút mờ, báo "cần nâng cấp người chơi");
  5. thiếu thuốc → `NotEnoughPotion`; thiếu Coin → `NotEnoughCoin`;
  6. trừ thuốc và Coin, tăng `Level` (chưa có dòng thì thêm dòng `Level = 2`), `persist()` một lần; `false` thì `Saved = false` và bộ nhớ giữ nguyên, view báo "chưa lưu được dữ liệu" như nâng sao.
- Từ chối ở 1–5: ví, save, sự kiện, `persist` đều không đổi.

### 6.3 Gộp chỉ số

Một hàm thuần `GetCombatStats(character, starUps, level)` theo công thức mục 3.1, đặt cạnh `StarService` (assembly `Characters`). `CombatService` thôi nhận `stars.GetCombatStats` và nhận hàm gộp: lấy `starUps` từ `StarService.GetStarUps` và `level` từ `CharacterLevelService.GetLevel`. `BattleSession` không đổi chữ ký (đã có `statsOf`); `BattleUnit` không đổi. Các chỗ gọi `CombatService` (`OnlineStartup`, `CombatRig`, `CombatTestData`) phải sửa và đã nằm trong danh sách ở mục 10.

### 6.4 Chỗ cộng EXP

Thể lực do server trừ (D-008). Máy chỉ cộng EXP **một lần**, trong `CombatService.EnterStageAsync` (đường vào trận thật; `StartBattle` chỉ cho DEV, không trừ thể lực nên không cho EXP), ngay trước `_apply(response.State)` để một lần lưu ghi cả EXP, bằng `StaminaCost` của màn. `CommandGate` gửi lại cùng `RequestId` bên trong và chỉ trả về một lần, nên đoạn này chạy đúng một lần cho mỗi lượt bấm; không cần cờ riêng. Nếu app bị giết sau khi server trừ thể lực mà trước khi máy nhận trả lời thì EXP của lượt đó mất, chấp nhận cùng lý do với Material (rủi ro 3 của spec Online đợt 2).

### 6.5 Thuốc từ quán

- Mỗi lần `RestaurantService.OnCustomerPaid` chạy, tung một lần theo `RestaurantPotionChancePercent`; trúng thì `Wallet.Add(ExpPotion, 1)`. Dùng nguồn ngẫu nhiên tiêm được (như các service khác) để test.
- **AFK (lựa chọn của 🎨, `AfkState` chỉ có Coin lý thuyết mỗi giây, không có số khách):** thuốc AFK tính **từ Coin AFK thu về**, không từ số khách. Khi thu AFK (`AfkService`, chỗ `_wallet.Add(Coin, …)`), thuốc = `(Coin vừa thu + AfkPotionRemainderCoin) / AfkCoinPerPotion` (chia nguyên), phần dư ghi lại vào `AfkPotionRemainderCoin`. Vì Coin AFK đã có trần và hệ số chất lượng, thuốc AFK tự dùng **cùng trần**. `AfkState` không đổi.
- Mục tiêu cân bằng: quán cho khoảng 1/4 tổng thuốc; chỉnh bằng `RestaurantPotionChancePercent` khi cân bằng.

## 7. Server

- Đổi: `RewardData.ExpPotion`, `StageRewardPayload.ExpPotion`, `GameDataBuilder` đọc dòng `ExpPotion`, `Validate` từ chối `ExpPotion` âm, `ServerProtocol.Version = 3`.
- **Phải dựng lại và triển khai lại server** (`ServerModule`). Mốc này xếp vào cùng đợt với Online đợt 3 theo lịch của 🧭 Nhân vật và 🧭 Online.
- Không đổi `ServerState`, không thêm lệnh mới. Server không biết cấp nhân vật, cấp người chơi hay Coin.
- `LocalGameServer` (chế độ Local trong Editor) dùng cùng `GameData` nên tự nhận thuốc.
- **Đã kiểm trong code:** server đọc `GameData` bằng `JsonConvert.DeserializeObject` (Newtonsoft, mặc định bỏ qua trường lạ và để 0 khi thiếu trường), máy đọc bằng `JsonUtility` (cũng bỏ qua trường lạ). Hai chiều đều an toàn. Kế hoạch vẫn thêm một test đọc dữ liệu thiếu `ExpPotion` → 0. Nếu phần server chậm triển khai, các phần còn lại (cấp người chơi, cấp nhân vật, thuốc từ quán, thuốc AFK) chạy được độc lập.

## 8. Màn hình

Chỉ lấy bố cục từ `ReferenceImages/character-upgrade.png` và `character-list-en.png`; không lấy tab Lv/trang bị/kỹ năng. **Phác thảo bằng chữ, cần user duyệt trước khi 🔨 dựng UI** (ảnh mô phỏng thật làm ở bước riêng của kế hoạch, chỉ dùng hình tạm có sẵn của Unity).

```
Thanh trên:   [Lv 7  ▓▓▓▓░░ 340/520]  Tên người chơi          Coin  Material  Gem
Màn Nhân vật:
┌──────────────────────────────────────────────────────────────┐
│ Nhân vật                                           [Cài đặt] │
│ ┌────┐┌────┐┌────┐┌────┐   │  Mắm Tôm   SR  ★★★☆☆           │
│ │ Lv5││ Lv3││ Lv1││ Lv1│   │  Lv 5 / 7   (trần = cấp người   │
│ └────┘└────┘└────┘└────┘   │  chơi)                          │
│                            │  HP 1210   Tấn công 133  Thủ 61  │
│                            │  Tốc độ 95                       │
│                            │  [Nâng sao: 4 mảnh]  (có sẵn)   │
│                            │  [Nâng cấp: 12 thuốc · 600 Coin] │
│                            │  Thuốc EXP: 40    Mảnh: 6        │
└──────────────────────────────┴───────────────────────────────┘
```

- Thẻ nhân vật thêm nhãn "Lv n" cạnh sao.
- Nút Nâng cấp mờ kèm lý do khi: thiếu thuốc, thiếu Coin, hoặc cấp nhân vật đã bằng cấp người chơi ("Cần nâng cấp người chơi").
- Popup khi lên cấp người chơi hiện cấp mới và thưởng.
- Popup kết quả trận thêm dòng thuốc ngoài Gem và Material.
- Chữ mới vào `UiText` (Việt + Anh) theo [D-009](../../decisions/decisions.md), một file `partial` cho khu "Cấp" (tên key theo `khu.tên`).

## 9. Test

Không cần mạng; chạy bằng `LocalGameServer` và `FakeStore` có sẵn.

- `PlayerLevelServiceTests`: EXP tăng đúng, lên nhiều cấp một lần, dừng ở cấp tối đa và bỏ dư, thưởng lên cấp đúng, `amount <= 0` không đổi gì, config lỗi, `persist` lỗi.
- `CharacterLevelServiceTests`: mọi nhánh từ chối không đổi trạng thái; thành công trừ đúng thuốc và Coin; cấp hiệu lực bị chặn bởi trần; dòng cấp mồ côi bị dọn; save cũ nạp Lv1.
- `GrowthStatsTests`: công thức cộng dồn (ví dụ Mắm Tôm 3★ Lv5), làm tròn, config lỗi trả gốc, Tốc độ không đổi, khớp `StarService` khi Lv1.
- `StageRewardPotionTests` (Rules + máy): server trả `ExpPotion` đúng cho lần đầu và đánh lại; máy cộng một lần khi trả lời gửi lại; `Validate` từ chối âm; bản cũ không có trường thì mặc định 0.
- `RestaurantPotionTests`: tỉ lệ đúng với nguồn ngẫu nhiên giả; thu AFK đổi Coin thành thuốc đúng `AfkCoinPerPotion`, phần dư giữ qua các lần thu và qua lưu/nạp; dùng chung trần.
- PlayMode: một ca đi hết đường (vào trận → EXP → lên cấp → nâng nhân vật → vào trận lại với chỉ số mới), Reset xoá hết.

Số test và chia phiên 🔨 chi tiết ở kế hoạch.

## 10. Docs phải sửa (trong kế hoạch)

- `docs/design/game-design.md`: mục Nhân vật (thay dòng "Chưa thiết kế: cấp nhân vật"), bảng tài nguyên (thêm Thuốc EXP), mục Quán (khách rơi thuốc), mục Online (cấp phía máy), thêm mục "Cấp người chơi", "Cấp nhân vật".
- `docs/decisions/decisions.md`: thêm **D-xxx: Cấp nhân vật và EXP người chơi lưu phía máy cho bản 31/12; chuyển lên server là việc bắt buộc sau 31/12** (user chốt 2026-11-21), kèm lý do và đánh đổi mục 11.
- `docs/design/ideas.md`: ý "nâng cấp nhân vật bằng Coin" chuyển thành **Đã làm** và trỏ tới spec này; thêm ý **trang bị** (sau 31/12) và **nâng skill** (dùng quy tắc chi phí ở mục 2.4).
- `docs/roadmap.md`, `docs/studio/streams/characters.md`, `docs/studio/board.md`: ghi tiến độ (Leader ghi).
- `docs/references/ui-screens.md`: không thêm ảnh mới (đã có `character-upgrade.png`).

## 11. Rủi ro

1. **Cấp ở máy sửa được.** Người chơi sửa `save` là lên cấp không tốn gì, và nâng sức mạnh trận. Chấp nhận cho bản 31/12 vì quán và Coin vốn đã vậy; **bắt buộc chuyển lên server sau 31/12** trước khi có xếp hạng, đấu người chơi hoặc bán gói liên quan cấp.
2. **Hai nguồn sự thật cho nhân vật** (server giữ danh sách, máy giữ cấp): xử lý bằng dọn cấp mồ côi (mục 5.1) và cấp hiệu lực bị chặn (mục 3.8).
3. **Server phải được triển khai lại.** Chậm lịch server (Online đợt 3) thì phần thuốc từ màn chậm theo; phần cấp người chơi và cấp nhân vật vẫn thử được với thuốc từ quán và DEV.
4. **Server có bỏ qua trường lạ khi đọc JSON hay không** chưa kiểm (mục 7); nếu không thì phải sửa phía server trước.
5. **Số cân bằng chưa kiểm bằng dữ liệu người chơi.** Mục 3 và 5.3 chỉ là điểm xuất phát; tính lại cuối tháng 12 (user chốt).
6. **Thuốc AFK tính từ Coin, không từ số khách**, nên tỉ lệ thật phụ thuộc giá món; `AfkCoinPerPotion` phải chỉnh khi cân bằng để quán đạt khoảng 1/4 tổng thuốc.

## 12. Ngoài phạm vi

Trang bị (sau 31/12; chỉ ghi `ideas.md`), nâng skill, chỗ tiêu mảnh dư ở 5★, nâng nhiều cấp một lần ("nâng tối đa"), nút DEV cộng thuốc hay EXP (playtest bằng cách đánh màn), nhiệm vụ cho thuốc, cấp người chơi quá 20, chuyển cấp lên server (sau 31/12, **bắt buộc**), thêm thuốc vào thanh tài nguyên.
