# Cấp người chơi, Cấp nhân vật, Thuốc EXP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **Kế hoạch không có bước commit** (chỉ 📋 Thư ký commit, xem `docs/studio/README.md`); xong thì gửi "Báo cáo cuối kênh" cho 📋 và kênh mở mình.

**Goal:** Người chơi tiêu thể lực để lên cấp người chơi, dùng thuốc EXP (rơi từ màn và từ quán) cộng Coin để nâng cấp nhân vật tới trần bằng cấp người chơi, và thấy HP/Tấn công/Thủ trong trận tăng.

**Architecture:** Hai service đồng bộ phía máy (`PlayerLevelService`, `CharacterLevelService`) trong assembly `Characters`, dữ liệu nằm trong `PlayerSave` và `Wallet`; một lớp `GrowthStats` gộp phần trăm sao và phần trăm cấp rồi đưa vào `BattleSession` qua tham số `statsOf` có sẵn. Chỉ có một thay đổi server: thưởng màn thêm trường `ExpPotion` (server trả số lượng, máy cộng vào `Wallet`, như Material).

**Tech Stack:** Unity 6 (C#), NUnit EditMode + PlayMode, `JsonUtility` phía máy, Newtonsoft + .NET (`ServerModule`).

**Spec:** [docs/superpowers/specs/2026-11-21-character-level-design.md](../specs/2026-11-21-character-level-design.md) (đã duyệt 2026-11-21). Số mục trích dẫn ở dưới là số mục của spec. Đọc thêm khi cần: `docs/guides/code-style.md`, `docs/guides/unity-workflow.md`; grep bài học `#save #ui #server` trong `docs/lessons/lesson.md`.

## Global Constraints

- Tài liệu tiếng Việt, tên class/biến/file tiếng Anh. Chữ người chơi thấy đi qua `UiText` (khoá `khu.tên`, có Việt và Anh), không viết cứng chuỗi (`HardcodedTextTests`).
- D-005: dữ liệu lưu chỉ trong `PlayerSave` qua `SaveService`; field mới có giá trị khởi tạo đúng cho game mới (EXP 0, danh sách rỗng, thuốc 0, `AfkPotionRemainderCoin` 0); Reset Save Data xoá hết.
- `JsonUtility` không đọc `Dictionary`: mọi tập hợp là `List`; field công khai, kiểu cơ bản.
- `CurrencyType.ExpPotion = 3`; chỉ thêm ở cuối enum, không đổi giá trị cũ.
- Công thức chỉ số (mục 3.1): `HP/Attack/Defense = (gốc × (100 + StatPercentPerStar × lầnNângSao + StatPercentPerLevel × (cấp − 1)) + 50) / 100`, số nguyên `long`, Speed giữ gốc. Config lỗi trả chỉ số gốc.
- *Khởi điểm* (user chốt tính lại cuối tháng 12): `MaxPlayerLevel` 20; EXP lên cấp L→L+1 = `100 + 20 × L`; `StatPercentPerLevel` 2; giá nâng lên cấp đích T = `2 × T` thuốc + `100 × T` Coin; thưởng lên cấp người chơi `200 × cấp mới` Coin và `5` thuốc; thuốc từ màn: thắng lần đầu 10, đánh lại 3; quán `3`% mỗi khách trả tiền; `AfkCoinPerPotion` 500.
- Cấp hiệu lực của nhân vật = `min(cấp đã lưu, cấp người chơi, MaxPlayerLevel)`, tối thiểu 1; không ghi đè cấp đã lưu.
- Server: `ServerProtocol.Version` do Online O3 đặt (3), đợt này **không nâng thêm**; `MinProtocolVersion` giữ **2**. Thưởng màn vẫn không có Coin; màn cốt truyện không có thưởng. Server không biết cấp, EXP hay Coin.
- Service ghi theo mẫu `StarService`: mọi bước từ chối đứng trước bước đổi; `persist()` đúng một lần; `persist` trả `false` thì `Saved = false` và giữ nguyên bộ nhớ.
- Không thêm nút DEV cộng thuốc hay EXP. Hình tạm chỉ dùng hình có sẵn của Unity; object chưa có hình đặt tên `<Tên>_PH_<rộng>x<cao>`.
- Test đếm theo kế hoạch: **86 EditMode mới + 1 test .NET (`ServerModule`) + 3 PlayMode mới**; toàn bộ test cũ vẫn xanh.

## Review Focus

Các tình huống spec không nói rõ mà người chơi dễ gặp; mỗi dòng có test ở task sở hữu:

1. Save cũ (chưa có `PlayerExp`, `CharacterLevels`, `AfkPotionRemainderCoin`, dòng thuốc) nạp được: người chơi Lv1, mọi nhân vật Lv1, thuốc 0. → Task 1.
2. Phản hồi server cũ thiếu `ExpPotion` (hoặc dữ liệu `GameData` cũ) → thuốc 0, không lỗi. → Task 2.
3. Cấp đã lưu cao hơn cấp người chơi (save sửa tay hoặc sau reset một phần) → chỉ số và nút dùng cấp hiệu lực, không ghi đè. → Task 3.
4. Nhân vật không còn trong `state` (server reset) → dòng cấp mồ côi không tính vào chỉ số và bị dọn. → Task 3.
5. `AddExp` với số rất lớn không tràn `long`, không lặp vô hạn; EXP dư ở cấp tối đa bị bỏ. → Task 3.
6. Gửi lại cùng `RequestId` hoặc phản hồi Offline không cộng EXP hai lần hay cộng nhầm. → Task 4.

## Cấu trúc file

Tạo mới:
- `Assets/_Project/Rules/CharacterLevelEntry.cs`: struct `{ string CharacterId; int Level }` (cạnh `StarUpEntry.cs`).
- `Assets/_Project/Characters/Scripts/LevelConfig.cs`: SO số cân bằng (mục 5.3), mẫu theo `StarConfig`.
- `Assets/_Project/Characters/Scripts/GrowthStats.cs`: hàm gộp chỉ số.
- `Assets/_Project/Characters/Scripts/PlayerLevelService.cs`, `CharacterLevelService.cs`, `LevelUpStatus.cs` (enum + struct kết quả).
- `Assets/_Project/Characters/Data/LevelConfig.asset` (tạo bằng `ProjectSetup`).
- `Assets/_Project/UI/Scripts/PlayerLevelBarRule.cs`, `PlayerLevelBarView.cs`, `LevelUpPopupView.cs`.
- Test mới: `LevelConfigTests`, `GrowthStatsTests`, `PlayerLevelServiceTests`, `CharacterLevelServiceTests`, `RestaurantPotionTests`, `PlayerLevelBarRuleTests` trong `Assets/_Project/Tests/`; `Tests/PlayMode/LevelFlowPlayTests.cs`; `UI/Scripts/CharacterDetailView.cs`, `CharacterDetailNav.cs`, `CharacterUpgradePreview.cs` (Task 7).

Sửa: `Rules/CurrencyType.cs`, `Rules/GameData.cs`, `Rules/ServerHandlers.Stage.cs`, `Rules/ServerProtocol.cs`, `Core/Scripts/PlayerSave.cs`, `Core/Scripts/ServerStateApplier.cs`, `Boot/Scripts/GameDataBuilder.cs`, `Boot/Scripts/OnlineStartup.cs`, `Combat/Scripts/StageDefinition.cs`, `StageProgressService.cs`, `CombatService.cs`, `Restaurant/Scripts/AfkService.cs`, `RestaurantService.cs`, `Editor/ProjectSetup.cs`, `UI/Scripts/CharactersView.cs`, `CharactersViewText.cs`, `CharacterListRows.cs`, `CharacterCardView.cs`, `CombatViewText.cs`, `ResourceBarView.cs` hoặc nơi hiện tên người chơi, `UiText` (file `partial` mới `UiText.Level.cs`), `Editor/ServerDataExport.cs` (chỉ kiểm). Dữ liệu `Assets/_Project/Combat/Data/Stages/st_1_1..5.asset` thêm dòng thuốc.

## Chia phiên và phụ thuộc

| Phiên 🔨 | Task | Chạy song song được | Khoá Unity |
|---|---|---|---|
| **#1 "Lõi cấp"** | 1, 2, 3, 4, 5 | Sau Task 1: Task 2, 3, 5 độc lập (file khác nhau); Task 4 sau Task 3 | Cần (chạy EditMode) |
| **#2 "Nối và UI"** | 6, 7, 8 | Không | Cần (scene, PlayMode) |

Thứ tự khoá Unity (user chốt 2026-11-21): O3-a → O3-b → O3-c → Cấp nhân vật #1 → #2 → Thời tiết → Cốt truyện → Vẽ lại quán. **Phiên #1 cắt từ nhánh code O3 sau O3-c**; nhánh gốc ghi trong prompt mở phiên.

Cổng ảnh mô phỏng: **đã được user duyệt 2026-11-21 (bản v2, mục 8 của spec)**; không cần dừng trước Task 7. Nếu server chưa triển khai lại, Task 2 vẫn xong được phía máy và Local; phần thuốc từ màn thử trên server thật chờ 🧭 Online triển khai (không chặn các task khác). Playtest dừng giữa hai phiên: sau phiên #1 chỉ có test EditMode (chưa có UI) nên chưa cần user chơi; user chơi thử sau phiên #2.

---

### Task 1: Nền tảng dữ liệu (loại tiền, config, save, công thức)

**Files:**
- Modify: `Rules/CurrencyType.cs`, `Core/Scripts/PlayerSave.cs`
- Create: `Rules/CharacterLevelEntry.cs`, `Characters/Scripts/LevelConfig.cs`, `Characters/Scripts/GrowthStats.cs`
- Test: `Tests/LevelConfigTests.cs`, `Tests/GrowthStatsTests.cs`, `Tests/SaveCompatibilityTests.cs` (thêm), `Tests/WalletTests.cs` (thêm)

**Interfaces:**
- Produces:
  - `CurrencyType.ExpPotion = 3`.
  - `[Serializable] public struct CharacterLevelEntry { public string CharacterId; public int Level; }`.
  - `PlayerSave.PlayerExp` (`long = 0`), `PlayerSave.CharacterLevels` (`List<CharacterLevelEntry> = new()`), `PlayerSave.AfkPotionRemainderCoin` (`long = 0`).
  - `LevelConfig : ScriptableObject` (menu `BunDauPeo/Characters/Level Config`) với property chỉ đọc: `int MaxPlayerLevel`, `int ExpBase`, `int ExpPerLevel`, `int StatPercentPerLevel`, `int PotionPerTargetLevel`, `int CoinPerTargetLevel`, `int LevelUpRewardCoinPerLevel`, `int LevelUpRewardPotion`, `int RestaurantPotionChancePercent`, `int AfkCoinPerPotion`; hàm `long ExpToNext(int level)` (= `ExpBase + ExpPerLevel × level`, ném `ArgumentOutOfRangeException` khi `level < 1`) và `List<string> Validate()` (mỗi lỗi bắt đầu bằng tên field).
  - `public static class GrowthStats { public static CombatStats Get(CharacterDefinition c, int starUps, int level, int statPercentPerStar, int statPercentPerLevel); }`; `starUps < 0` coi là 0, `level < 1` coi là 1.

- [ ] **Step 1: Viết test (đỏ)**
  - `LevelConfigTests`: `Defaults_AreValid` (asset mới `CreateInstance` → `Validate()` rỗng; `ExpToNext(1) == 120`, `ExpToNext(19) == 480`); `Validate_RejectsMaxPlayerLevelBelowOne`; `Validate_RejectsNegativeCostsAndRewards`; `Validate_RejectsChanceOutsideZeroTo100` (`RestaurantPotionChancePercent` −1 và 101); `Validate_RejectsAfkCoinPerPotionBelowOne`.
  - `GrowthStatsTests`: `Level1NoStar_ReturnsBase`; `Level1_MatchesStarServiceFormula` (cùng kết quả `StarService.GetCombatStats(c, starUps)` với `starUps` 0..4); `MamTomOneStarUpLevel5_AddsBothPercents` (Mắm Tôm HP 1000, Atk 110, Def 50, `starUps=1`, `level=5`, 10%/2% → HP 1180, Attack 130, Defense 59, Speed gốc); `RoundsHalfUp` (gốc 5 ở +10% cho 6 vì `(5×110+50)/100 = 6`); `Speed_NeverChanges`; `NegativeInputs_ClampedToBase`.
  - `SaveCompatibilityTests.OldSave_WithoutLevelFields_LoadsAsLevel1` (JSON save cũ không có 3 field → `PlayerExp == 0`, `CharacterLevels.Count == 0`, `AfkPotionRemainderCoin == 0`); `WalletTests.ExpPotion_AddAndGet_RoundTripsThroughSave`.
- [ ] **Step 2: Chạy, xác nhận đỏ** (`unity` chạy EditMode lọc theo tên fixture; đỏ vì thiếu kiểu).
- [ ] **Step 3: Cài đặt** các kiểu trên đúng chữ ký. `GrowthStats.Get` dùng `long` như `StarService.GetCombatStats(character, starUps)`; `LevelConfig` copy kiểu `Validate()`/`OnValidate()` của `StarConfig` với số *khởi điểm* ở Global Constraints. `PlayerSave` thêm 3 field kèm comment một dòng; không đổi `Version`.
- [ ] **Step 4: Chạy lại, xác nhận xanh** (13 test).

---

### Task 2: Thuốc rơi từ màn (Rules, Boot, máy)

**Files:**
- Modify: `Rules/GameData.cs` (`RewardData`), `Rules/ServerHandlers.Stage.cs` (`StageRewardPayload`, `CompleteStage`), `Rules/ServerProtocol.cs`, `Boot/Scripts/GameDataBuilder.cs`, `Combat/Scripts/StageDefinition.cs`, `Combat/Scripts/StageProgressService.cs`
- Test: `Tests/HandlerStageTests.cs`, `Tests/GameDataBuilderTests.cs`, `Tests/StageDefinitionTests.cs`, `Tests/StageProgressServiceTests.cs`, `Tests/ServerProtocolTests.cs`, `ServerModule/BunDauPeoServer.Tests/ServerModuleTests.cs`

**Interfaces:**
- Consumes: `CurrencyType.ExpPotion` (Task 1).
- Produces: `RewardData.ExpPotion` (`long`), `StageRewardPayload.ExpPotion` (`long`); `StageCompleteResult.Rewards` có thêm dòng `CurrencyAmount(ExpPotion, n)` khi `n > 0`; `ServerProtocol.Version` không đổi (O3 đặt 3), `MinProtocolVersion == 2`.

- [ ] **Step 1: Viết test (đỏ)**
  - `HandlerStageTests`: `CompleteStage_FirstClear_ReturnsExpPotion`, `CompleteStage_Repeat_ReturnsExpPotion`, `CompleteStage_Lost_ReturnsNoPotion` (dữ liệu màn có `FirstClear{Gem:150,Material:30,ExpPotion:10}`, `Repeat{…ExpPotion:3}`; `ServerState` không đổi ngoài Gem và `ClearedStages`).
  - `GameDataBuilderTests`: `Build_MapsExpPotionLine` (dòng `ExpPotion` trong SO → `RewardData.ExpPotion`); `Validate_AllowsExpPotionInStageReward`; `Validate_StillRejectsCoinInStageReward`.
  - `StageDefinitionTests`: `ExpPotionRewardLine_IsAllowed`; `CoinRewardLine_StillRejected`.
  - `StageProgressServiceTests`: `CompleteStage_AddsExpPotionToWallet`; `CompleteStage_ResultListsExpPotionLine`; `CompleteStage_ReplyWithoutExpPotionField_AddsNothing` (JSON trả lời không có trường).
  - `ServerProtocolTests.Version_IsNotRaisedByThisFeature_MinProtocolStays2` (Version bằng giá trị O3 đang đặt, Min vẫn 2).
  - `ServerModuleTests.ReadsGameDataWithoutExpPotionField_DefaultsToZero` (.NET, `dotnet test ServerModule/BunDauPeoServer.sln`).
- [ ] **Step 2: Chạy, xác nhận đỏ.**
- [ ] **Step 3: Cài đặt.**
  - `RewardData` và `StageRewardPayload` thêm `public long ExpPotion;`; `ServerHandlers.CompleteStage` trả `ExpPotion = reward.ExpPotion` (không đổi `ServerState`).
  - `GameDataBuilder.Reward` thêm `ExpPotion = Sum(lines, CurrencyType.ExpPotion)`; `CheckReward` cho phép `ExpPotion` (comment "Server chỉ biết Gem, Material, ExpPotion" cập nhật).
  - `StageDefinition.CheckRewardLines` vẫn chặn Coin, không chặn `ExpPotion`.
  - `StageProgressService.CompleteStageAsync` cộng `payload.ExpPotion` vào `_wallet` cạnh dòng Material, trước `_apply`, và thêm dòng vào `rewards`.
  - `ServerProtocol.Version` không đổi (O3 đã đặt 3), `MinProtocolVersion` giữ 2.
- [ ] **Step 4: Chạy lại, xác nhận xanh** (12 EditMode + 1 .NET).

---

### Task 3: `PlayerLevelService`, `CharacterLevelService`, dọn cấp mồ côi

**Files:**
- Create: `Characters/Scripts/PlayerLevelService.cs`, `CharacterLevelService.cs`, `LevelUpStatus.cs`
- Modify: `Core/Scripts/ServerStateApplier.cs` (`PruneUnowned`)
- Test: `Tests/PlayerLevelServiceTests.cs`, `Tests/CharacterLevelServiceTests.cs`, `Tests/ServerStateApplierTests.cs` (thêm)

**Interfaces:**
- Consumes: `LevelConfig`, `GrowthStats` (Task 1); `Wallet`, `Roster`, `CharacterCatalog`, `PlayerSave` có sẵn.
- Produces:
  ```csharp
  public sealed class PlayerLevelService {
      public PlayerLevelService(PlayerSave save, Wallet wallet, LevelConfig config, Func<bool> persist);
      public bool IsConfigValid { get; }
      public int Level { get; }                 // 1..MaxPlayerLevel, suy ra từ PlayerExp
      public long Exp { get; }                  // EXP trong cấp hiện tại (không phải tổng)
      public long ExpToNext { get; }            // 0 khi IsMaxLevel
      public bool IsMaxLevel { get; }
      public event Action Changed;
      public void AddExp(long amount);          // amount <= 0 hoặc config lỗi: bỏ qua; lên nhiều cấp được; dư ở max bị bỏ; Changed 1 lần; persist 1 lần
  }
  public enum LevelUpStatus { Success, InvalidConfig, Unknown, NotOwned, AtPlayerLevel, NotEnoughPotion, NotEnoughCoin }
  public struct LevelUpResult { public LevelUpStatus Status; public int LevelAfter; public bool Saved; }
  public sealed class CharacterLevelService {
      public CharacterLevelService(PlayerSave save, Roster roster, Wallet wallet, CharacterCatalog catalog,
                                   LevelConfig config, PlayerLevelService player, Func<bool> persist);
      public bool IsConfigValid { get; }
      public int MaxLevel { get; }              // = player.Level
      public int GetLevel(string id);           // cấp hiệu lực; id lạ/null → 1
      public (int Potion, int Coin) GetNextCost(string id);   // theo cấp đích GetLevel(id)+1
      public LevelUpResult TryLevelUp(string id);             // đồng bộ
      public event Action Changed;
  }
  ```
  `PlayerSave` lưu EXP tổng ở `PlayerExp`; `Level`, `Exp` suy ra, không lưu riêng.

- [ ] **Step 1: Viết test (đỏ)**
  - `PlayerLevelServiceTests` (9): `NewSave_IsLevel1WithZeroExp`; `AddExp_BelowThreshold_KeepsLevel` (`AddExp(119)` → Lv1, `Exp == 119`, `ExpToNext == 120`); `AddExp_CrossesThreshold_LevelsUpAndGrantsReward` (`AddExp(120)` → Lv2, ví +400 Coin và +5 thuốc); `AddExp_ManyLevelsAtOnce_GrantsEachLevelsReward`; `AddExp_AtMaxLevel_DropsExcess` (`IsMaxLevel`, `ExpToNext == 0`, PlayerExp không vượt tổng tới cấp 20); `AddExp_ZeroOrNegative_NoChange` (không phát `Changed`, không `persist`); `AddExp_LongMaxValue_DoesNotOverflowOrHang`; `InvalidConfig_AddExpIgnored`; `PersistFailure_KeepsMemoryState`.
  - `CharacterLevelServiceTests` (11): `TryLevelUp_Success_SpendsPotionAndCoinAndRaisesLevel` (nhân vật Lv1 → Lv2 tốn 4 thuốc và 200 Coin khi người chơi ≥ Lv2); `TryLevelUp_UnknownId_Unknown`; `TryLevelUp_NotOwned_NotOwned`; `TryLevelUp_AtPlayerLevel_AtPlayerLevel`; `TryLevelUp_NotEnoughPotion`; `TryLevelUp_NotEnoughCoin`; `TryLevelUp_InvalidConfig_InvalidConfig`; `Rejections_LeaveWalletSaveAndEventsUntouched` (mọi mã từ chối: ví, `CharacterLevels`, `Changed`, `persist` không đổi); `PersistFailure_SavedFalse_KeepsMemory`; `GetLevel_StoredAbovePlayerLevel_ReturnsEffective` (dòng `Level=15`, người chơi Lv3 → `GetLevel == 3`, dòng lưu vẫn 15); `GetNextCost_UsesTargetLevel` (Lv4 → thuốc 10, Coin 500).
  - `ServerStateApplierTests.PruneUnowned_RemovesCharacterLevelsOfUnownedIds` (và nhân vật còn sở hữu giữ nguyên).
- [ ] **Step 2: Chạy, xác nhận đỏ.**
- [ ] **Step 3: Cài đặt** đúng chữ ký. Mẫu theo `StarService` (thứ tự từ chối, `persist` một lần). `PlayerLevelService.Level`: lặp trừ `ExpToNext` từ tổng `PlayerExp`, dừng ở `MaxPlayerLevel` (không đệ quy; tránh tràn bằng cách kẹp EXP tổng ở tổng tới cấp tối đa trước khi cộng). Thưởng lên cấp `Wallet.Add(Coin, LevelUpRewardCoinPerLevel × cấpMới)` và `Wallet.Add(ExpPotion, LevelUpRewardPotion)` cho từng cấp. `TryLevelUp` dùng `Wallet.TrySpend` cho cả hai khoản một lần (nguyên tử). `PruneUnowned` thêm `save.CharacterLevels.RemoveAll(e => !owned.Contains(e.CharacterId))` và gộp kết quả `changed`.
- [ ] **Step 4: Chạy lại, xác nhận xanh** (21 test).

---

### Task 4: Nối vào trận (chỉ số theo cấp, EXP khi vào trận)

**Files:**
- Modify: `Combat/Scripts/CombatService.cs` (ctor + `EnterStageAsync`), `Boot/Scripts/OnlineStartup.cs` (chỉ chỗ `new CombatService`), `Tests/CombatRig.cs`, `Tests/CombatTestData.cs`
- Test: `Tests/CombatServiceTests.cs`

**Interfaces:**
- Consumes: `PlayerLevelService`, `CharacterLevelService`, `GrowthStats`, `LevelConfig`, `StarConfig` (Task 1, 3).
- Produces: `CombatService(CombatConfig config, Roster roster, StarService stars, CharacterLevelService levels, PlayerLevelService playerLevel, LevelConfig levelConfig, StarConfig starConfig, StaminaService stamina, StageProgressService progress, System.Random random, Func<bool> persist, IGameServer server, CommandGate gate, Func<ServerState, bool> apply)`; tham số mới chèn sau `stars`. `BattleSession` nhận `statsOf = c => GrowthStats.Get(c, stars.GetStarUps(c.Id), levels.GetLevel(c.Id), starConfig.StatPercentPerStar, levelConfig.StatPercentPerLevel)`; config lỗi thì dùng `Stars.GetCombatStats` cũ.

- [ ] **Step 1: Viết test (đỏ)** trong `CombatServiceTests`: `EnterStage_Success_AddsExpEqualToStaminaCost` (màn 10 thể lực → `PlayerLevelService` +10 EXP); `EnterStage_Rejected_AddsNoExp` (thiếu thể lực, màn khoá, đội rỗng); `EnterStage_Offline_AddsNoExp`; `EnterStage_LostReplyRetrySameRequest_AddsExpOnce` (dùng cách mô phỏng của `LocalGameServerTests.Local_LostReply_RetrySameRequest_AppliesOnce`); `BattleAllyStats_IncludeStarAndLevel` (nhân vật Lv5 1 lần nâng sao: HP đơn vị đồng minh bằng 1180 cho Mắm Tôm). `StartBattle` (DEV) không cộng EXP: một test `StartBattle_Dev_AddsNoExp` (tổng 6 test).
- [ ] **Step 2: Chạy, xác nhận đỏ.** Cập nhật `CombatRig`, `CombatTestData`, `OnlineStartup` để dựng thêm hai service và hai config (chưa có `LevelConfig` trong Boot: `OnlineStartup` nhận qua `[SerializeField]` ở Task 6; tạm dùng `ScriptableObject.CreateInstance<LevelConfig>()` trong test rig).
- [ ] **Step 3: Cài đặt.** Trong `EnterStageAsync`, `playerLevel.AddExp(stage.StaminaCost)` ngay trước `_apply(response.State)`; không đụng nhánh từ chối/Offline. `StartBattle` không đổi.
- [ ] **Step 4: Chạy lại, xác nhận xanh** (6 test). Toàn bộ `CombatServiceTests`, `BattleSessionTests` cũ vẫn xanh.

---

### Task 5: Thuốc từ quán (khách trả tiền và AFK)

**Files:**
- Modify: `Restaurant/Scripts/RestaurantService.cs` (`OnCustomerPaid`, ctor nhận `LevelConfig`), `Restaurant/Scripts/AfkService.cs` (`Collect`, ctor nhận `LevelConfig`), `Tests/RestaurantTestData.cs` (3 chỗ `new RestaurantService`)
- Test: `Tests/RestaurantPotionTests.cs`, `Tests/AfkServiceTests.cs` (thêm)

**Interfaces:**
- Consumes: `LevelConfig.RestaurantPotionChancePercent`, `AfkCoinPerPotion`, `PlayerSave.AfkPotionRemainderCoin` (Task 1). `RestaurantService` đã có `System.Random random`.
- Produces: `RestaurantService(…, LevelConfig levelConfig, …)` và `AfkService(…, LevelConfig levelConfig, …)` (tham số cuối trước `persist`); `AfkCollectResult` giữ nguyên chữ ký (số thuốc thu được cộng thẳng vào ví, không thêm vào kết quả).

- [ ] **Step 1: Viết test (đỏ).**
  - `RestaurantPotionTests`: `CustomerPaid_ChanceHit_AddsOnePotion` (random giả trả giá trị dưới ngưỡng 3%); `CustomerPaid_ChanceMiss_AddsNone`; `ChanceZero_NeverDrops`; `ChanceHundred_AlwaysDrops`; `InvalidLevelConfig_NoPotionAndCoinStillPaid`.
  - `AfkServiceTests`: `Collect_ConvertsCoinToPotionByAfkCoinPerPotion` (thu 1.250 Coin → 2 thuốc, dư 250 trong `AfkPotionRemainderCoin`); `Collect_KeepsRemainderAcrossCollects` (250 dư + lần sau thu 300 → +1 thuốc, dư 50); `Collect_RemainderSurvivesSaveAndLoad`; `Collect_ZeroCoin_NoPotionNoChange`.
- [ ] **Step 2: Chạy, xác nhận đỏ.**
- [ ] **Step 3: Cài đặt.** `OnCustomerPaid` thêm: `if (_levelConfigValid && _random.Next(100) < chance) _wallet.Add(ExpPotion, 1)` (dùng nguồn ngẫu nhiên của service để test được). `AfkService.Collect` sau khi cộng Coin: `total = coins + AfkPotionRemainderCoin`; `potions = total / AfkCoinPerPotion`; `AfkPotionRemainderCoin = total % AfkCoinPerPotion`; cộng thuốc vào ví trước `_persist()`; config lỗi thì bỏ phần thuốc, giữ dư cũ.
- [ ] **Step 4: Chạy lại, xác nhận xanh** (9 test). `RestaurantServiceTests`, `RestaurantInvariantTests` cũ vẫn xanh.

---

### Task 6: Nối Boot, ProjectSetup, dữ liệu màn, Reset, xuất dữ liệu server

**Files:**
- Modify: `Boot/Scripts/OnlineStartup.cs`, `Boot/Scripts/GameBootstrap.cs` (thêm `[SerializeField] LevelConfig _levelConfig`, báo lỗi như `_starConfig`), `Editor/ProjectSetup.cs`, `Editor/ServerDataExport.cs` (chỉ kiểm), `Assets/_Project/Combat/Data/Stages/st_1_1..5.asset`
- Test: `Tests/ProjectSetupTests.cs`, `Tests/ResetSaveDataTests.cs`, `Tests/GameSessionTests.cs`, `Tests/ServerStateApplierTests.cs` hoặc `Tests/GameDataBuilderTests.cs`

**Interfaces:**
- Consumes: mọi thứ Task 1–5.
- Produces: `ProjectSetup.LevelConfigPath = "Assets/_Project/Characters/Data/LevelConfig.asset"`; `ServiceLocator` có `PlayerLevelService`, `CharacterLevelService`, `LevelConfig`; `Boot.unity` gán `_levelConfig` (vá khi trống, như `_starConfig`, bài học L-002).

- [ ] **Step 1: Viết test (đỏ).** `ProjectSetupTests`: `CreatesLevelConfigAsset`; `BootScene_LevelConfigIsAssigned` (đọc lại tham chiếu từ file scene); `BattleStages_HaveExpPotionRewards` (5 màn đánh có dòng `ExpPotion`: lần đầu 10, đánh lại 3; màn cốt truyện không có). `ResetSaveDataTests.Reset_ClearsPlayerExpCharacterLevelsPotionAndAfkRemainder`. `GameSessionTests.Bootstrap_RegistersLevelServices` (`ServiceLocator.Get<PlayerLevelService>()` và `<CharacterLevelService>` không null sau Boot). `GameDataBuilderTests.ExportedJson_ContainsExpPotionForBattleStages` (dữ liệu server xuất từ SO có `ExpPotion` 10 và 3).
- [ ] **Step 2: Chạy, xác nhận đỏ.**
- [ ] **Step 3: Cài đặt.** `OnlineStartup` dựng `PlayerLevelService`, `CharacterLevelService`, đăng ký cả hai và `LevelConfig` vào `ServiceLocator` ngay sau khi đăng ký `StarService`; truyền vào `CombatService` và `RestaurantService`. `ProjectSetup`: `GetOrCreateAsset<LevelConfig>`, vá `Boot.unity`, thêm dòng thưởng thuốc cho 5 màn đánh theo kiểu cách `ProjectSetup` đang gán thưởng màn (bám phần code hiện có, không sửa tay file `.meta`). Chạy lại `ServerDataExport` (menu có sẵn) để dữ liệu server có `ExpPotion`; ghi đường dẫn file xuất vào báo cáo để 🧭 Online triển khai server.
- [ ] **Step 4: Chạy lại, xác nhận xanh** (6 test) và Boot chạy được trong Editor Play không lỗi console.

---

### Task 7: Giao diện (ảnh mô phỏng v2 đã được user duyệt 2026-11-21)

**Files:**
- Create: `UI/Scripts/PlayerLevelBarRule.cs`, `PlayerLevelBarView.cs`, `LevelUpPopupView.cs`, `CharacterDetailView.cs`, `CharacterDetailNav.cs`, `CharacterUpgradePreview.cs`, `UiText.Level.cs`
- Modify: `UI/Scripts/CharactersView.cs` (chỉ còn danh sách, bấm thẻ mở panel chi tiết), `CharactersViewText.cs`, `CharacterListRows.cs`, `CharacterCardView.cs`, `CombatViewText.cs`, nơi hiện tên người chơi ở thanh trên, `Editor/CharactersSceneBuilder.cs` (dựng panel chi tiết, tab, mũi tên, nút Quay lại)
- Test: `Tests/CharactersViewTextTests.cs`, `Tests/CharacterListRowsTests.cs`, `Tests/PlayerLevelBarRuleTests.cs`, `Tests/CharacterDetailNavTests.cs`, `Tests/CharacterUpgradePreviewTests.cs`, `Tests/CharactersSceneTests.cs`, `Tests/RewardRowTests.cs`

**Interfaces:**
- Consumes: `PlayerLevelService`, `CharacterLevelService`, `StarService`, `GrowthStats`, `LevelUpResult`, `Wallet`.
- Produces:
  - `PlayerLevelBarRule.Fill(long exp, long toNext, bool isMax) → float` (0..1; max → 1).
  - `CharacterDetailNav.Neighbor(IReadOnlyList<string> ids, string currentId, int step) → string` (vòng tròn; `ids` rỗng → `""`; `currentId` không có trong `ids` → phần tử đầu).
  - `CharacterUpgradePreview.Build(CharacterDefinition c, int starUps, int level, int maxLevel, LevelConfig lc, int statPercentPerStar) → { bool HasNext; CombatStats Before, After; }` (`HasNext = false` khi `level >= maxLevel`).
  - `CharactersViewText.LevelLine(int level, int max)`, `UpgradeButtonLabel(int potion, int coin)`, `UpgradeDisabledReason(LevelUpStatus s)`; `CharacterListRows.Row` thêm `Level`; `CombatViewText.CurrencyCell` hiện tên đã dịch cho `ExpPotion`; khoá `UiText` Việt + Anh cho mọi chữ mới.

- [ ] **Step 1: Viết test (đỏ).**
  - `CharactersViewTextTests` (5): `LevelLine_ShowsLevelOverMax`; `UpgradeButtonLabel_ShowsPotionAndCoin`; `UpgradeDisabledReason_AtPlayerLevel_MentionsPlayerLevel`; `UpgradeDisabledReason_NotEnoughPotion`; `UpgradeDisabledReason_NotEnoughCoin`.
  - `CharacterListRowsTests.Rows_IncludeEffectiveLevel` (1).
  - `PlayerLevelBarRuleTests` (2): `Fill_IsExpOverToNext`; `Fill_AtMaxLevel_IsOne`.
  - `CharacterDetailNavTests` (4): `Next_WrapsAroundList`; `Prev_WrapsAroundList`; `SingleCharacter_StaysSame`; `CurrentMissing_FallsBackToFirst`.
  - `CharacterUpgradePreviewTests` (2): `Preview_ShowsBeforeAndAfterStats` (Mắm Tôm một lần nâng sao, Lv5 → Lv6: HP 1180 → 1200, Attack 130 → 132, Defense 59 → 60); `Preview_AtMaxLevel_HasNoNext`.
  - `CharactersSceneTests` (4, các ca cũ kiểm nút Nâng sao trên danh sách phải sửa theo luồng mới): `Scene_ClickingCard_OpensDetailPanelOfThatCharacter`; `Detail_BackButton_ReturnsToList`; `Detail_HasLevelAndStarTabs_StarUpButtonOnlyInStarTab`; `Detail_ArrowsSwitchToNeighborCharacter`.
  - `RewardRowTests.CurrencyCell_ExpPotion_UsesLocalizedName` (1).
  - `UiTextTests` và `HardcodedTextTests` hiện có phải vẫn xanh; thêm khoá mới đủ Việt và Anh.
- [ ] **Step 2: Chạy, xác nhận đỏ.**
- [ ] **Step 3: Cài đặt** theo mục 8 của spec và ảnh mô phỏng v2 đã duyệt. Danh sách chỉ chọn nhân vật; panel chi tiết có tab Cấp (xem trước trước → sau, nút Nâng cấp gọi `CharacterLevelService.TryLevelUp`) và tab Sao (nút Nâng sao gọi `StarService.TryStarUpAsync`, hành vi cũ giữ nguyên); `Saved == false` thì báo "chưa lưu được dữ liệu" như nâng sao. Thanh Lv + EXP, popup lên cấp người chơi, dòng thuốc ở popup kết quả trận. Dùng hình tạm Unity, `Image` trên Canvas; không thêm file ảnh. Bám pattern `CharactersView` (đăng ký `Changed`, huỷ đăng ký khi tắt). Không thêm lọc/sắp xếp.
- [ ] **Step 4: Chạy lại, xác nhận xanh** (19 test) và mở màn Nhân vật trong Play kiểm bằng mắt rồi chụp bằng công cụ `ScreenCapture1080` để đính kèm báo cáo.

---

### Task 8: PlayMode đầu-cuối, hiệu năng, docs

**Files:**
- Create: `Tests/PlayMode/LevelFlowPlayTests.cs`
- Modify: `docs/design/game-design.md`, `docs/decisions/decisions.md`, `docs/design/ideas.md`, `docs/roadmap.md`, `docs/references/ui-screens.md` (chỉ nếu thêm ảnh; hiện không), `CHANGELOG` nếu dự án có
- Test: `Tests/PlayMode/LevelFlowPlayTests.cs`

**Interfaces:** Consumes mọi task trước. Produces: không có kiểu mới.

- [ ] **Step 1: Viết test PlayMode (đỏ).** `LevelFlowPlayTests` (3): `EnterStage_GivesExp_AndLevelsUpPlayer` (vào màn 10 lần → người chơi Lv2, popup xuất hiện, thưởng vào ví); `UpgradeCharacter_RaisesStatsAndSpendsCost` (cấp thuốc và Coin qua `Wallet`, bấm thẻ nhân vật để mở panel chi tiết rồi bấm Nâng cấp ở tab Cấp → cấp +1, panel hiện chỉ số mới); `FullLoop_EnterStageWinCollectPotionUpgradeEnterAgain` (vào màn → thắng → thuốc về ví → nâng nhân vật → vào lại, chỉ số đồng minh của `BattleSession` tăng đúng; Reset xoá hết).
- [ ] **Step 2: Chạy, xác nhận đỏ rồi xanh** sau khi sửa lỗi nối còn sót.
- [ ] **Step 3: Docs** (mục 10 của spec): `game-design.md` (thay "Chưa thiết kế: cấp nhân vật"; thêm Thuốc EXP vào bảng tài nguyên; mục Quán có thuốc; mục Online ghi cấp phía máy; mục "Cấp người chơi", "Cấp nhân vật"); `decisions.md` thêm D-xxx (cấp phía máy cho 31/12, chuyển lên server là việc bắt buộc sau 31/12; ghi lý do và đánh đổi mục 11 của spec); `ideas.md` chuyển ý Coin → **Đã làm**, thêm ý trang bị (sau 31/12) và nâng skill (dùng quy tắc "chi phí chính + Coin phụ"); `roadmap.md` tick mục. Bài học mới (nếu có sai sót khi làm) ghi vào `docs/lessons/lesson.md` ngay trong task đó.
- [ ] **Step 4: Chạy toàn bộ EditMode + PlayMode + `dotnet test ServerModule/BunDauPeoServer.sln`**, đo hiệu năng màn Nhân vật và màn quán theo `docs/studio/performance.md` (không vượt ngưỡng), kiểm console không lỗi. Gửi "Báo cáo cuối kênh" (mẫu `docs/studio/brief.md`): test 86/86 EditMode mới + 1 .NET + 3/3 PlayMode, đường dẫn dữ liệu server cần triển khai, ảnh chụp màn.

---

## Bảng đếm test

| Task | EditMode mới | .NET | PlayMode mới |
|---|---|---|---|
| 1 | 13 | 0 | 0 |
| 2 | 12 | 1 | 0 |
| 3 | 21 | 0 | 0 |
| 4 | 6 | 0 | 0 |
| 5 | 9 | 0 | 0 |
| 6 | 6 | 0 | 0 |
| 7 | 19 | 0 | 0 |
| 8 | 0 | 0 | 3 |
| **Tổng** | **86** | **1** | **3** |


## Self-review (đã chạy)

- **Phủ spec:** mục 1 (tiêu chí 1–6) → Task 3, 4, 6, 7, 8; mục 3.1–3.11 → Task 1, 3; mục 5.1 → Task 1, 3; 5.2 → Task 1; 5.3 → Task 1; 5.4 → Task 2; 6.1–6.2 → Task 3; 6.3–6.4 → Task 4; 6.5 → Task 5; mục 7 → Task 2, 6; mục 8 → Task 7; mục 9 → các test của từng task và Task 8; mục 10 → Task 8. Không thiếu mục nào.
- **Nhất quán kiểu:** `GrowthStats.Get(c, starUps, level, statPercentPerStar, statPercentPerLevel)` ở Task 1 và 4 trùng; `CharacterLevelService.GetLevel/GetNextCost/TryLevelUp` ở Task 3, 4, 7 trùng; `LevelUpStatus` ở Task 3 và 7 trùng.
- **Lệch spec cần báo:** spec 6.4 ban đầu ghi `StartBattle`; code thật cộng EXP ở `EnterStageAsync` (đã sửa spec). Spec 6.3 ghi `GameBootstrap` là chỗ gọi; code thật dựng service ở `OnlineStartup` (đã sửa spec).
