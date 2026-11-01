# Restaurant: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans hoặc superpowers:subagent-driven-development để làm kế hoạch này từng task (cách thực thi do session điều phối chọn, xem [agent-routing.md](../../guides/agent-routing.md)). Các bước dùng checkbox (`- [ ]`).

**Goal:** Quán tự chạy (khách, nhân viên theo chỉ số, nâng cấp, sự cố, chất lượng) kiếm Coin khi người chơi ở trong game và tích Coin AFK theo chất lượng khi vắng mặt; có màn quán tối thiểu để thử trọn vòng này khi chưa có Combat và UI hub.

**Architecture:** `Core` thêm kiểu save; `GameSession` thêm `ResetCompleted`. Assembly `Restaurant` mới (chỉ tham chiếu `Core` và `Characters`) chứa SO, mô phỏng thuần (`RestaurantSimulation`), công thức thu nhập lý thuyết, dịch vụ nâng cấp, nhân viên, chất lượng, sự cố, AFK, và `RestaurantService` nối tất cả; `RestaurantRunner` (MonoBehaviour) tick bằng `Time.unscaledDeltaTime`. `UI` thêm màn quán dựng bằng `Image` trong Canvas; `Boot` nối dây; `DevOverlay` (chỉ Editor và Development Build) để thử.

**Tech Stack:** Unity 6000.3.15f1, C#, `JsonUtility`, uGUI 2.0.0, Input System 1.19.0, Test Framework 1.6.0, CLI `unity` 1.0.0-beta.15.

**Spec:** [docs/superpowers/specs/2026-11-01-restaurant-design.md](../specs/2026-11-01-restaurant-design.md) (kiến trúc, `Wallet`, `Roster`, `SaveService` ở [spec nền móng](../specs/2026-10-30-foundation-architecture-design.md); cách nối `Boot` theo [kế hoạch Gacha](2026-10-31-gacha.md)). Số cân bằng nêu trong kế hoạch là số khởi điểm của spec.

## Global Constraints

- Không sửa/xoá tay `.meta`; không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`. Không sửa tay `.unity`, `.asset`, `.prefab`: asset và scene do `ProjectSetup` sinh qua `SerializedObject`.
- **Git (CLAUDE.md, [unity-workflow.md](../../guides/unity-workflow.md#git)):** làm trên nhánh `feature/restaurant`; commit **và push** sau mỗi task, chỉ khi test của task đó xanh; `git add` theo đường dẫn cụ thể (cả thư mục để không sót `.meta`, L-005), không `git add -A`; chạy `git status --short` trước mỗi commit, không để `?? *.meta` nào còn lại; không force push.
- Code: `PascalCase` cho class/method/property; field private `_camelCase`; field chỉnh trong Inspector dùng `[SerializeField] private`. Kiểu thuần dữ liệu được lưu JSON (`UpgradeLevel`, `DailyPresenceEntry`, `AfkState`, `PlayerSave`) dùng field `public`. Comment chỉ giải thích vì sao. Tên code tiếng Anh, docs tiếng Việt.
- Namespace trùng tên assembly (`BunDauPeo.Restaurant`). `Restaurant` chỉ tham chiếu `Core` và `Characters`, **không** `Gacha`, `Combat`, `Boot`, `UI`.
- `OnValidate` và cảnh báo dùng `Debug.LogWarning`, không `LogError` (làm test đỏ). Event bắn sau khi đổi trạng thái dùng `SafeInvoke()` (L-003). Enum được lưu hoặc nằm trong SO: giá trị số được ghim, chỉ thêm ở cuối.
- Dữ liệu cần lưu chỉ nằm trong `PlayerSave` qua `SaveService`, field mới có giá trị khởi tạo ([D-005](../../decisions/decisions.md)); trạng thái chạy ngoài `PlayerSave` phải xoá trong `RestaurantService.ResetRuntime()`.
- Mọi test logic tiền hoặc thời gian có đầu vào mà cài đặt sai cho kết quả khác (L-006). Số lần gọi `System.Random.NextDouble()` là hợp đồng; test dùng `ScriptedRandom` có sẵn.
- UI: mọi hình trong Canvas là component `Image` (không gán sprite cho hình vuông đặc, hoặc `UISprite`/`Knob`); không `Raw Image`, không thêm file ảnh. Chữ dùng `Text` uGUI với font dựng sẵn (`Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`), không TextMeshPro. EventSystem dùng `InputSystemUIInputModule`. Nạp skill `unity:ui-ugui` trước task UI. Khi skill mâu thuẫn với docs dự án thì docs thắng, báo user.
- Nạp tài sản bằng `AssetDatabase.LoadAssetAtPath` **sau** khi tạo/đổi scene (L-002).
- Không thêm package, không dùng `Resources`, không `PlayerPrefs`.
- Sau mỗi task: Console không có lỗi compile hoặc warning mới từ code dự án.

### Lệnh dùng chung

**Chạy test** (Editor phải đóng):

```bash
unity test . --mode EditMode --output ../bdp-tests.xml --timeout 600 -- -logFile ../bdp-tests.log
```

Exit `0` là đạt, `8` là test chạy xong nhưng có test đỏ, `6` là lỗi khác (kể cả compile). Luôn đọc `../bdp-tests.xml` (`failed="0"`, `total="N"`) và log, không dựa mỗi mã thoát. Khi đỏ:

```bash
grep -E "error CS|Failed|Exception" ../bdp-tests.log | head -30
```

Editor đang mở thì chạy trong Test Runner hoặc qua `TestRunnerApi` ([unity-workflow.md](../../guides/unity-workflow.md)). Một test tham chiếu class chưa tồn tại làm cả assembly không compile, nên bước "test đỏ" là lỗi `error CS0246`/`CS1061`, không phải assert fail. `../bdp-tests.*` nằm ngoài repo, xoá khi xong đợt.

**Chạy Setup (sinh asset/scene)** (Editor phải đóng):

```bash
unity run . --timeout 600 -- -executeMethod BunDauPeo.EditorTools.ProjectSetup.Run -logFile ../bdp-setup.log
```

## Review Focus

Mỗi dòng có test ở task ghi trong ngoặc.

1. **AFK vượt trần hoặc sai khi đồng hồ lùi:** trần tổng 12 giờ qua hai lần thoát, đồng hồ lùi cộng 0 và đặt lại mốc, đồng hồ tiến chặn ở trần (Task 13).
2. **AFK cao hơn hoặc bằng chơi thật:** thu nhập thật không vượt thu nhập lý thuyết và lớn hơn lý thuyết × hệ số tối đa, trong nhiều trạng thái quán (Task 7). Nếu trạng thái quá tải làm cận dưới đỏ vì khách bỏ về lúc đang nấu, **dừng và báo user** (spec sai hoặc cần cân bằng), không nới test.
3. **Tiền cộng đôi khi app vào nền rồi mở lại:** `CatchUp` đẩy mốc, hai lần liên tiếp cộng 0; `OnAppPause` `Heartbeat` trước `Persist` (Task 13).
4. **Trừ tiền nâng cấp không nhất quán:** thiếu Coin hoặc thiếu Material thì không đổi gì; `persist` đúng một lần; handler `Changed` ném lỗi không chặn việc lưu (Task 10).
5. **Nhân vật chuyển sang đội mà mô phỏng vẫn tính:** thu nhập thôi tính ngay, việc dở về đầu hàng đợi (Task 6 và 7).
6. **Reset bỏ sót trạng thái chạy:** mọi field `PlayerSave` mới và mô phỏng, sự cố, bộ đếm, số giây vắng đang chờ (Task 1, 7, 12, 13).
7. **Save cũ không đọc được:** JSON không có bốn field mới (Task 1).
8. **AFK kẹt khi `RatePerSecond = 0`:** vắng 12 giờ lúc quán chưa kiếm tiền không ăn vào trần (Task 13).
9. **Popup AFK mất ở lần mở app:** `CatchUp` ngắn liền sau không ghi đè số giây vắng đang chờ (Task 13).
10. **Sự cố của vai không có người:** vẫn sinh, không xử lý được, quán không cày điểm (Task 12).

## Điều hướng agent

Quy tắc chung ở [agent-routing.md](../../guides/agent-routing.md).

| Task | Model | Effort |
|---|---|---|
| 1. Core: kiểu save, `ResetCompleted` | Sonnet 5.5 | high |
| 2. Asmdef và SO | Sonnet 5.5 | medium |
| 3. `RestaurantProfile`, `StaffingService` | Sonnet 5.5 | medium |
| 4. `IncomeCalculator` | Sonnet 5.5 | high |
| 5. Mô phỏng: dây chuyền, trả tiền | Sonnet 5.5 | high |
| 6. Mô phỏng: kiên nhẫn, thiếu người, chặn, đổi hồ sơ | Sonnet 5.5 | high |
| 7. `RestaurantService`, Runner, bất biến, nối `Boot`, dữ liệu mẫu | Sonnet 5.5 | high |
| 8. UI: phòng, HUD, scene, dải DEV | Sonnet 5.5 | medium (nạp `unity:ui-ugui`) |
| 9. UI: `StaffPanel` | Sonnet 5.5 | medium |
| 10. `UpgradeService`, nút test | Sonnet 5.5 | high (đụng ví) |
| 11. `IClock`, `RestaurantQuality` | Sonnet 5.5 | high |
| 12. `IncidentDirector`, biểu tượng "!" | Sonnet 5.5 | high |
| 13. `AfkService`, nút Thu AFK, Tua AFK | Sonnet 5.5 | high |
| 14. UI: `AfkPopup`, `QualityPopup` | Sonnet 5.5 | medium |
| 15. Docs | Haiku 5.5 | low |
| Review toàn nhánh | Opus 5.5 | xhigh (đụng ví, save, AFK) |

### Chia session (kế hoạch 15 task, trên 10 task)

Không chia giữa một task. Ranh giới đề xuất, mỗi ranh giới là chỗ test xanh, đã push:

1. **Task 1–6:** logic thuần (Core, SO, hồ sơ, công thức, mô phỏng). Chưa có gì để xem.
2. **Task 7–9:** nối dây và màn quán. **Dừng sau Task 9 để user thử Play** (giai đoạn A): quán chạy, đổi nhân viên, thấy thu nhập. Nếu phải đổi mô phỏng thì Chất lượng, Sự cố, AFK chưa viết, chưa phải làm lại.
3. **Task 10:** giai đoạn B (nâng cấp), ngắn.
4. **Task 11–13:** giai đoạn C, logic.
5. **Task 14–15 và review toàn nhánh** (Opus 5.5 xhigh theo mục "Hoàn tất đợt").

Chỉ một session đụng Unity tại một thời điểm.

## File Structure

```
Core/Scripts/        UpgradeLevel.cs, DailyPresenceEntry.cs, AfkState.cs (mới), PlayerSave.cs (sửa)
Boot/Scripts/        GameSession.cs, GameBootstrap.cs, BunDauPeo.Boot.asmdef (sửa)
Restaurant/Scripts/  BunDauPeo.Restaurant.asmdef, DishDefinition.cs, CustomerDefinition.cs, IncidentDefinition.cs,
                     UpgradeDefinition.cs, UpgradeCatalog.cs, RestaurantConfig.cs, RestaurantProfile.cs, StaffingService.cs,
                     IncomeCalculator.cs, RestaurantSimulation.cs, RestaurantService.cs, RestaurantRunner.cs,
                     UpgradeService.cs, IClock.cs, RestaurantQuality.cs, IncidentDirector.cs, AfkService.cs
UI/Scripts/          BunDauPeo.UI.asmdef (sửa), IsoProjection.cs, RestaurantViewText.cs, RestaurantView.cs, RestaurantHud.cs,
                     StaffPanel.cs, StaffPanelRows.cs, UpgradeTestPanel.cs, AfkPopup.cs, QualityPopup.cs, DevOverlay.cs (cả file trong #if)
Editor/              BunDauPeo.Editor.asmdef (sửa), ProjectSetup.cs (sửa), RestaurantSceneBuilder.cs
Tests/               BunDauPeo.Tests.asmdef (sửa), RestaurantTestData.cs, RestaurantConfigTests.cs, UpgradeDefinitionTests.cs,
                     RestaurantProfileTests.cs, StaffingServiceTests.cs, IncomeCalculatorTests.cs, RestaurantSimulationTests.cs,
                     RestaurantServiceTests.cs, RestaurantInvariantTests.cs, IsoProjectionTests.cs, RestaurantViewTextTests.cs,
                     RestaurantSceneTests.cs, StaffPanelRowsTests.cs, UpgradeServiceTests.cs, RestaurantQualityTests.cs,
                     IncidentDirectorTests.cs, AfkServiceTests.cs
                     + thêm test vào SaveCompatibilityTests, GameSessionTests, ProjectSetupTests, GachaSceneTests
```

## Trước khi bắt đầu

- [x] Nhánh `docs/restaurant-spec` (spec và kế hoạch này) đã vào `main` hoặc user cho phép làm tiếp từ nó. Tạo `feature/restaurant`: `git checkout -b feature/restaurant`.
- [ ] Đọc `docs/guides/code-style.md`, `docs/guides/unity-workflow.md`, và `Grep "#script" docs/lessons/lesson.md` (L-001, L-003, L-006), `Grep "#unity-files" docs/lessons/lesson.md` (L-002, L-004, L-005, L-007).

---

## Giai đoạn A: quán chạy và thấy được

### Task 1: Core: kiểu save, `ResetCompleted`, xác nhận mốc test

**Files:**
- Create: `Assets/_Project/Core/Scripts/UpgradeLevel.cs`, `DailyPresenceEntry.cs`, `AfkState.cs`
- Modify: `Assets/_Project/Core/Scripts/PlayerSave.cs`, `Assets/_Project/Boot/Scripts/GameSession.cs`
- Test: `Assets/_Project/Tests/SaveCompatibilityTests.cs`, `GameSessionTests.cs`

**Interfaces:**
- Produces, namespace `BunDauPeo.Core`: `[Serializable] struct UpgradeLevel { string UpgradeId; int Level; }`, `[Serializable] struct DailyPresenceEntry { int DayIndex; double Seconds; }`, `[Serializable] class AfkState { long LastActiveUtcTicks; double RatePerSecond; double AccumulatedSeconds; double PendingTheoretical; }` (field `public`). `PlayerSave` thêm `public List<UpgradeLevel> Upgrades = new(); public List<DailyPresenceEntry> DailyPresence = new(); public List<int> IncidentScores = new(); public AfkState Afk = new();` (`Version` giữ 1).
- Produces, `GameSession`: `public event Action ResetCompleted;` bắn bằng `SafeInvoke()` trong `ResetToDefaults()` sau `Wallet.NotifyChanged()` và `Roster.NotifyChanged()`, trước `Persist()`.

- [x] **Step 1: Xác nhận mốc test.** Chạy lệnh test chung trước khi sửa gì. Kỳ vọng: exit `0`, `failed="0"`, `total="185"` (roadmap ghi 185, `structure.md` còn ghi 184, và `git log origin/main -1` hiện merge PR #2). Số khác 185 thì **dừng và hỏi user**; số ở các task sau cộng dồn từ mốc này.
- [x] **Step 2: Viết test đỏ (5 test)**
  - `SaveCompatibilityTests.OldSave_WithoutRestaurantFields_LoadsWithDefaults`: `FromJson<PlayerSave>("{\"Version\":1,\"Balances\":[{\"Type\":1,\"Amount\":5}],\"OwnedCharacterIds\":[\"a\"]}")` cho `Upgrades`, `DailyPresence`, `IncidentScores` rỗng, `Afk` khác `null` với `LastActiveUtcTicks == 0` và `PendingTheoretical == 0`, `Balances[0].Amount == 5`.
  - `SaveCompatibilityTests.RestaurantFields_RoundTripThroughSaveService`: lưu rồi `Load()` một save có `Upgrades = {("up_tables", 2)}`, `DailyPresence = {(7300, 120.5)}`, `IncidentScores = {2, 0, 1}`, `Afk` = (ticks `123456789012345`, rate `1.56`, accumulated `3600.25`, pending `5614.5`); đọc lại đúng từng giá trị (kể cả `double` và `long` lớn).
  - `GameSessionTests.ResetToDefaults_ClearsRestaurantFields`: gán dữ liệu vào bốn field, reset, kiểm cả bốn về giá trị khởi tạo.
  - `GameSessionTests.ResetToDefaults_RaisesResetCompletedOnce_AfterWalletAndRosterChanged_BeforeFileWritten`: ghi lại thứ tự sự kiện `Wallet.Changed`, `Roster.Changed`, `ResetCompleted`; `ResetCompleted` đúng 1 lần, sau hai cái kia; trong handler `File.Exists(_path)` là `false` (file chỉ được ghi lại bởi `Persist()` sau đó), và sau reset thì `File.Exists` là `true`.
  - `GameSessionTests.ResetCompleted_HandlerThrows_StillPersistsAndCallsOtherHandlers`: hai handler, cái đầu ném lỗi (`LogAssert.Expect(LogType.Exception, ...)`); cái sau vẫn được gọi, `ResetToDefaults()` trả `true`, file tồn tại.
- [x] **Step 3: Chạy test, kỳ vọng đỏ** (`error CS1061` về `Upgrades`, `ResetCompleted`).
- [x] **Step 4: Implement** theo Interfaces.
- [x] **Step 5: Chạy test, kỳ vọng xanh:** `failed="0"`, `total="190"`.
- [x] **Step 6: Commit và push**

```bash
git add Assets/_Project/Core/Scripts Assets/_Project/Boot/Scripts/GameSession.cs Assets/_Project/Tests/SaveCompatibilityTests.cs Assets/_Project/Tests/GameSessionTests.cs
git commit -m "feat: add restaurant save fields and GameSession.ResetCompleted"
git push -u origin feature/restaurant
```

---

### Task 2: Asmdef `Restaurant` và các SO

**Files:**
- Create: `Assets/_Project/Restaurant/Scripts/BunDauPeo.Restaurant.asmdef` (references `BunDauPeo.Core`, `BunDauPeo.Characters`; `autoReferenced: true`), `DishDefinition.cs`, `CustomerDefinition.cs`, `IncidentDefinition.cs`, `UpgradeDefinition.cs` (kèm `UpgradeEffect`, `UpgradeLevelDef`), `UpgradeCatalog.cs`, `RestaurantConfig.cs`
- Modify: `Assets/_Project/Tests/BunDauPeo.Tests.asmdef` (thêm `BunDauPeo.Restaurant`)
- Create (test): `Assets/_Project/Tests/RestaurantTestData.cs`, `RestaurantConfigTests.cs`, `UpgradeDefinitionTests.cs`

**Interfaces:**
- Produces, namespace `BunDauPeo.Restaurant`; field private `[SerializeField]` với property đọc cùng tên PascalCase, giá trị khởi tạo theo [spec 3.2–3.3](../specs/2026-11-01-restaurant-design.md#32-so-của-restaurant) ngay ở field initializer:
  - `DishDefinition : ScriptableObject` (`DisplayName`, `Icon`, `long BasePrice`, `float CookSeconds`), `CustomerDefinition : ScriptableObject` (`DisplayName`, `Sprite`, `float SpawnWeight`, `float PatienceSeconds`, `float PayMultiplier`), `IncidentDefinition : ScriptableObject` (`DisplayName`, `StaffRole Role`, `float Weight`). Cả ba có `[CreateAssetMenu(menuName = "BunDauPeo/Restaurant/...")]`, không có `Id`.
  - `enum UpgradeEffect { Tables = 0, CookSlots = 1, ServeSlots = 2, CashierSlots = 3, ArrivalInterval = 4, DishPrice = 5 }`; `[Serializable] struct UpgradeLevelDef { CurrencyAmount[] Cost; float Value; }`; `UpgradeDefinition : GameDefinition` (`DisplayName`, `Icon`, `UpgradeEffect Effect`, `IReadOnlyList<UpgradeLevelDef> Levels`, `int MaxLevel`, `List<string> Validate()`); `UpgradeCatalog : Catalog<UpgradeDefinition>`.
  - `RestaurantConfig : ScriptableObject`: đúng bảng [spec 3.3](../specs/2026-11-01-restaurant-design.md#33-restaurantconfig): `Dishes`, `Customers`, `Incidents` (`IReadOnlyList<...>`), `UpgradeCatalog Upgrades`, `BaseTables`, `BaseCookSlots`, `BaseServeSlots`, `BaseCashierSlots` (int), `BaseArrivalInterval`, `MaxWaitingLine` (int), `ServeSeconds`, `EatSeconds`, `CashierSeconds`, `AttendanceMax`, `IncidentMax`, `DailyGoalSeconds`, `IncidentSpawnMin`, `IncidentSpawnMax`, `IncidentFastSeconds`, `IncidentLateSeconds`, `AfkMaxHours`, `AfkFactorMin`, `AfkFactorMax`, `AfkPopupMinSeconds`, `AutosaveSeconds`, `List<string> Validate()`. `OnValidate` chỉ `LogWarning` từng dòng của `Validate()`.
- Produces, test helper `RestaurantTestData` (dùng `SerializedObject` như `TestData`): `DishDefinition Dish(long price = 20, float cook = 5)`, `CustomerDefinition Customer(float weight = 1, float patience = 40, float pay = 1)`, `IncidentDefinition Incident(StaffRole role, float weight = 1)`, `UpgradeLevelDef Level(float value, long coin, long material)`, `UpgradeDefinition Upgrade(string id, UpgradeEffect effect, params UpgradeLevelDef[] levels)`, `UpgradeCatalog UpgradeCatalog(params UpgradeDefinition[] items)`, `RestaurantConfig Config(UpgradeCatalog upgrades = null)` (một món 20 Coin 5 giây, một khách, ba sự cố mỗi vai một cái, catalog rỗng nếu `null`, còn lại số mặc định của spec), `CharacterDefinition Worker(string id, int cook, int serve, int cashier)` (đặt `RestaurantStats`).

- [x] **Step 1: Viết test đỏ (36 test)**
  - `UpgradeDefinitionTests` (9): `Validate_Valid_NoErrors`; `Validate_BlankId_Reported`; `Validate_NoLevels_Reported`; `Validate_LevelWithEmptyCost_Reported`; `Validate_LevelWithZeroTotalCost_Reported`; `Validate_LevelWithNegativeCostLine_Reported` (hai dòng 5 và -1 để tổng khác 0, L-006); `Validate_NonPositiveValue_Reported`; `Validate_CountEffectWithFractionalValue_Reported` (Tables = 4,5; còn `ArrivalInterval` = 0,9 hợp lệ); `MaxLevel_IsLevelsLength`. Mỗi ca lỗi dùng `TestData.Edit`, chuỗi lỗi bắt đầu bằng tên field.
  - `RestaurantConfigTests` (27): `Validate_SampleConfig_NoErrors`; `[TestCase]` `Validate_EmptyOrNullList_Reported` 4 ca (món rỗng, phần tử null trong món, khách rỗng, sự cố rỗng); `Validate_NonPositiveDishValue_Reported` 2 ca (`BasePrice` 0, `CookSeconds` 0); `Validate_NonPositiveCustomerValue_Reported` 3 ca (`SpawnWeight`, `PatienceSeconds`, `PayMultiplier` = 0); `Validate_NonPositiveBaseValue_Reported` 6 ca (`BaseArrivalInterval`, `ServeSeconds`, `EatSeconds`, `CashierSeconds`, `BaseTables`, `BaseCookSlots` = 0); `Validate_QualityMaxNotSumTo100_Reported` (50 + 40); `Validate_AfkFactor_Reported` 3 ca (min -0,1; min 0,6 > max 0,5; max 1,0); `Validate_AfkMaxHoursNonPositive_Reported`; `Validate_IncidentTimes_Reported` 2 ca (spawn min 130 > max 120; fast 20 >= late 20); `Validate_AutosaveNonPositive_Reported`; `Validate_Upgrades_Reported` 3 ca (catalog null; hai nâng cấp cùng `Effect`; một nâng cấp có `Validate()` lỗi). Test kiểm chuỗi lỗi chứa tên field.
- [x] **Step 2: Chạy test, kỳ vọng đỏ** với `error CS0246` về `RestaurantConfig`/`UpgradeDefinition`.
- [x] **Step 3: Implement** asmdef, các SO và `RestaurantTestData`. `RestaurantConfig.Validate()` gọi `Validate()` của từng nâng cấp trong catalog và `Catalog<T>.Validate()`; chuỗi lỗi bắt đầu bằng tên field (`"Dishes: ..."`).
- [x] **Step 4: Chạy test, kỳ vọng xanh:** `failed="0"`, `total="226"`.
- [x] **Step 5: Commit và push**

```bash
git add Assets/_Project/Restaurant/Scripts Assets/_Project/Tests/BunDauPeo.Tests.asmdef Assets/_Project/Tests/RestaurantTestData.cs Assets/_Project/Tests/RestaurantConfigTests.cs Assets/_Project/Tests/UpgradeDefinitionTests.cs
git commit -m "feat: add Restaurant assembly with config, dish, customer, incident and upgrade definitions"
git push
```

---

### Task 3: `RestaurantProfile` và `StaffingService`

**Files:**
- Create: `Assets/_Project/Restaurant/Scripts/RestaurantProfile.cs`, `StaffingService.cs`
- Test: `Assets/_Project/Tests/RestaurantProfileTests.cs`, `StaffingServiceTests.cs`

**Interfaces:**
- Consumes: `RestaurantConfig`, `UpgradeDefinition`, `PlayerSave.Upgrades`, `Roster.GetStaff/AssignStaff/RemoveStaff/AddToTeam`, `RestaurantTestData` (Task 2).
- Produces, namespace `BunDauPeo.Restaurant`:
  ```csharp
  public readonly struct WorkerInfo { public WorkerInfo(string characterId, int stat); public string CharacterId { get; } public int Stat { get; } }
  public readonly struct RestaurantProfile {
      public int Tables, CookSlots, ServeSlots, CashierSlots;            // property đọc
      public float ArrivalIntervalMultiplier, PriceMultiplier;
      public IReadOnlyList<WorkerInfo> Workers(StaffRole role);
      public int Slots(StaffRole role);
      public static RestaurantProfile Build(RestaurantConfig config, PlayerSave save, Roster roster);
      public RestaurantProfile WithWorkers(StaffRole role, IReadOnlyList<WorkerInfo> workers);   // biến thể giả định cho UI
  }
  public enum StaffAssignResult { Success, NotOwned, RoleFull }
  public sealed class StaffingService {
      public StaffingService(Roster roster, Func<RestaurantProfile> currentProfile);   // số ô từ hồ sơ, không phụ thuộc UpgradeService
      public StaffAssignResult TryAssign(string characterId, StaffRole role);
      public StaffAssignResult TryReplace(string oldId, string newId, StaffRole role);
      public bool Remove(string characterId);
  }
  ```
  `Build`: số ô và bàn lấy từ nâng cấp cùng `Effect` trong `config.Upgrades` theo cấp trong `save.Upgrades` (`Value` của cấp đó; cấp 0 hoặc không có dòng thì giá trị gốc của config; cấp lớn hơn `MaxLevel` thì dùng cấp cao nhất, **không đổi save**; `UpgradeId` lạ bị bỏ qua); `ArrivalIntervalMultiplier`/`PriceMultiplier` tương tự (gốc 1); nhân viên từ `Roster.GetStaff(role)` theo thứ tự, cắt còn `Slots(role)`, `Stat` từ `RestaurantStats.Cooking/Serving/Cashier`. `TryAssign`: vai đã đủ (đếm theo `currentProfile().Workers`, không tính chính nhân vật đó nếu nó đã ở vai này) thì `RoleFull`, chưa sở hữu thì `NotOwned`, ngược lại `Roster.AssignStaff`. `TryReplace`: kiểm `newId` sở hữu trước khi đổi gì, rồi `RemoveStaff(old)` và `AssignStaff(new, role)`.

- [x] **Step 1: Viết test đỏ (17 test)**
  - `RestaurantProfileTests` (8): `Build_NoUpgrades_UsesConfigBases`; `Build_UpgradeLevels_UseLevelValues` (Tables cấp 2 → 5; `ArrivalInterval` cấp 1 → 0,9; `DishPrice` cấp 3 → 1,3); `Build_LevelBeyondSo_UsesHighestLevel_KeepsSave`; `Build_UnknownUpgradeId_Ignored`; `Build_TrimsStaffToSlots_WithoutRemovingFromSave` (3 người ở Cook, 1 ô: `Workers(Cook).Count == 1`, `Roster.GetStaff(Cook).Count == 3`); `Build_StaffStatsComeFromRestaurantStats` (cook 30, serve 20, cashier 10 đúng vai); `Build_UnknownCharacterIdSkipped` (id không trong catalog nằm trong `save.Staff`); `WithWorkers_ReplacesOnlyThatRole`.
  - `StaffingServiceTests` (9): `TryAssign_FreeRole_Success`; `TryAssign_RoleFull_ReturnsRoleFull_AndChangesNothing`; `TryAssign_AfterSlotsIncrease_Succeeds` (func trả hồ sơ 2 ô); `TryAssign_NotOwned`; `TryAssign_CharacterInBattleTeam_LeavesTeam`; `TryAssign_CharacterAtOtherRole_MovesRole`; `TryAssign_SameRoleSameCharacter_NotRoleFull` (không tự chặn mình); `TryReplace_SwapsCharacter_KeepsRoleCount`; `TryReplace_NewNotOwned_ChangesNothing`. `Remove` được kiểm trong `TryAssign_FreeRole_Success`: sau khi gán, `Remove` trả `true` và `Roster.GetStaff` rỗng.
- [x] **Step 2: Chạy test, kỳ vọng đỏ** (`error CS0246` về `RestaurantProfile`).
- [x] **Step 3: Implement** theo Interfaces.
- [x] **Step 4: Chạy test, kỳ vọng xanh:** `failed="0"`, `total="243"`.
- [x] **Step 5: Commit và push** (`git add Assets/_Project/Restaurant/Scripts Assets/_Project/Tests/RestaurantProfileTests.cs Assets/_Project/Tests/StaffingServiceTests.cs`; message `feat: add RestaurantProfile and StaffingService`).

---

### Task 4: `IncomeCalculator`

**Files:**
- Create: `Assets/_Project/Restaurant/Scripts/IncomeCalculator.cs`
- Test: `Assets/_Project/Tests/IncomeCalculatorTests.cs`

**Interfaces:**
- Consumes: `RestaurantConfig`, `RestaurantProfile`, `WorkerInfo` (Task 2–3).
- Produces: `public readonly struct IncomeEstimate { public double CoinsPerSecond { get; } public StaffRole? MissingRole { get; } }` và `public static class IncomeCalculator { public static IncomeEstimate Estimate(RestaurantConfig config, RestaurantProfile profile); }` theo công thức [spec 4.2](../specs/2026-11-01-restaurant-design.md#42-thu-nhập-lý-thuyết-incomecalculator-hàm-tĩnh-thuần). `MissingRole` là vai đầu tiên theo thứ tự Cook, Server, Cashier không có ai; khi đó `CoinsPerSecond == 0`.

- [x] **Step 1: Viết test đỏ (13 test).** Cấu hình thử: `RestaurantTestData.Config()` (món 20 Coin cook 5 giây, một khách ×1, serve 4, eat 8, cashier 3, arrival 15, 3 bàn); `profile` dựng bằng `RestaurantProfile.Build` với `WorkerInfo` giả (qua `WithWorkers`). Thời gian xử lý = gốc × 100/(100 + chỉ số); năng lực vai = Σ (100 + chỉ số)/(100 × gốc).
  - `Arrival_IsBottleneck`: mỗi vai một người chỉ số 0, 3 bàn: năng lực (0,2 / 0,25 / 0,333), bàn 3/20 = 0,15; khách đến 1/15 → `CoinsPerSecond ≈ 1,3333`.
  - `Cook_IsBottleneck`: arrival 1 giây, 100 bàn, cook chỉ số 0 (0,2), serve và cashier chỉ số 100 → `≈ 4,0`.
  - `Server_IsBottleneck`: arrival 1, 100 bàn, cook 100 (0,4), serve 0 (0,25), cashier 100 → `≈ 5,0`.
  - `Cashier_IsBottleneck`: arrival 1, 100 bàn, cook 100, serve 100 (0,5), cashier 0 (0,3333) → `≈ 6,6667`.
  - `Tables_IsBottleneck`: arrival 1, 3 bàn, mỗi vai 3 người chỉ số 100 (nấu 2,5 + bưng 2 + ăn 8 + thu 1,5 = 14 giây) → thông lượng 3/14 → `≈ 4,2857`.
  - `MissingRole_GivesZeroAndNamesRole`: 3 ca `[TestCase]` bỏ Cook / Server / Cashier (hai vai còn lại có người) → `CoinsPerSecond == 0` và `MissingRole` đúng vai.
  - `TwoWorkersInOneRole_AddCapacities`: cook stat 0 và stat 100 cho năng lực 0,6 (arrival 1, 100 bàn, các vai khác chỉ số 100) → thông lượng min(1, 0,6, 0,5, 0,667) = 0,5 nên thêm người ở Cook **không** đổi kết quả; sau đó bỏ người serve giỏi (serve stat 0 → 0,25) thì Server là cổ chai. Kiểm cả hai.
  - `StatDoublesSpeed`: cook chỉ số 100 cho năng lực gấp đôi chỉ số 0.
  - `MoneyPerCustomer_UsesWeightedMeans`: hai món 20 và 40 (trung bình 30), hai khách (trọng số 1 ×1,0 và trọng số 3 ×2,0 → hệ số trung bình 1,75), `PriceMultiplier` 1,1 → tiền/khách 57,75 (kiểm `CoinsPerSecond / thông lượng`).
  - `ArrivalMultiplier_ScalesArrival`: `ArrivalIntervalMultiplier` 0,5 gấp đôi thu nhập khi arrival là cổ chai.
  - `CookTime_IsAverageOfDishes`: hai món cook 4 và 6 cho cùng kết quả với một món cook 5.
- [x] **Step 2: Chạy test, kỳ vọng đỏ.**
- [x] **Step 3: Implement** theo công thức (năng lực bàn dùng người nhanh nhất mỗi vai).
- [x] **Step 4: Chạy test, kỳ vọng xanh:** `failed="0"`, `total="256"`.
- [x] **Step 5: Commit và push** (`feat: add IncomeCalculator`).

---

### Task 5: `RestaurantSimulation`: dây chuyền và trả tiền

**Files:**
- Create: `Assets/_Project/Restaurant/Scripts/RestaurantSimulation.cs` (kèm `CustomerState`, `CustomerSnapshot`, `WorkerSnapshot`)
- Test: `Assets/_Project/Tests/RestaurantSimulationTests.cs`

**Interfaces:**
- Consumes: `RestaurantConfig`, `RestaurantProfile`, `ScriptedRandom`, `ThrowingRandom` (có sẵn trong Tests).
- Produces, namespace `BunDauPeo.Restaurant`:
  ```csharp
  public enum CustomerState { WaitingForTable, WaitingForCook, Cooking, WaitingForServer, Serving, Eating, WaitingForCashier, Paying }
  public readonly struct CustomerSnapshot { public int Id; public CustomerDefinition Definition; public DishDefinition Dish; public CustomerState State; public int TableIndex; /* -1 khi chưa có bàn */ public float PatienceRemaining; }
  public readonly struct WorkerSnapshot { public string CharacterId; public StaffRole Role; public int CustomerId; /* -1 khi rảnh */ }
  public sealed class RestaurantSimulation {
      public RestaurantSimulation(RestaurantConfig config, System.Random random);
      public void SetProfile(RestaurantProfile profile);
      public void Tick(float dt);
      public IReadOnlyList<CustomerSnapshot> Customers { get; }
      public IReadOnlyList<WorkerSnapshot> Workers { get; }
      public int Served { get; } public int Abandoned { get; }
      public event Action<int, long> CustomerPaid;     // SafeInvoke
  }
  ```
  `SetStationBlocked`, `Reset` và logic kiên nhẫn thêm ở Task 6. Hợp đồng RNG theo [spec 4.3](../specs/2026-11-01-restaurant-design.md#43-mô-phỏng-restaurantsimulation-class-thuần): constructor gọi **một** lần để bốc khoảng cách đến đầu tiên; mỗi lần khách đến: nếu hàng chờ bàn chưa đầy thì một lần chọn loại khách (cộng dồn `SpawnWeight`, `u × tổng`, so `<`), một lần chọn món (`(int)(u × n)` chặn ở `n − 1`); rồi **luôn** một lần bốc khoảng cách kế = `BaseArrivalInterval × ArrivalIntervalMultiplier × (0,5 + u)`. Bước cố định 0,1 giây; cộng dồn `dt` dư; thời gian việc cố định lúc nhận việc; dùng `double` hoặc đếm bước nguyên để 50 bước × 0,1 đúng 5 giây. Tiền = `Math.Round(BasePrice × PayMultiplier × PriceMultiplier, MidpointRounding.AwayFromZero)` tối thiểu 1. Hàng chờ đầy (`MaxWaitingLine`): không tạo khách, `Abandoned` tăng.

- [x] **Step 1: Viết test đỏ (12 test).** Cấu hình `RestaurantTestData.Config()`, `ScriptedRandom()` mặc định trả 0,0 → khoảng cách đầu = 0,5 × 15 = 7,5 giây; nhân viên stat 0, mỗi vai một người. "Tại t" nghĩa là sau `Tick` đủ t giây, dung sai ±0,2 giây.
  - `OneCustomer_FullPipeline_PaysAtExpectedTimeAndAmount`: khách đến t = 7,5, nấu 5, bưng 4, ăn 8, thu 3 → trả tiền t = 27,5; trước 27,3 `Served == 0`, sau 27,7 `Served == 1`; `CustomerPaid` đúng một lần với số tiền 20; `Customers` rỗng sau đó.
  - `HigherStat_ShortensWork`: cook stat 100 làm trả tiền sớm hơn 2,5 giây (t = 25,0).
  - `TwoCustomers_AreCookedFifoByOneCook`: hai khách đến cách nhau, người đến trước có `State` đi trước (kiểm `Id` đang `Cooking` đúng thứ tự).
  - `TableLimit_SecondCustomerWaitsForTable`: 1 bàn → khách hai `WaitingForTable` tới khi khách một trả tiền.
  - `WaitingLineFull_NewCustomerNotCreated_AbandonedIncrements_NoRngForTypeAndDish`: 1 bàn, `MaxWaitingLine` 1, ép nhiều khách đến: khi hàng đầy, `Abandoned` tăng, `Customers.Count` không tăng, và số lần gọi RNG tăng đúng 1 (chỉ khoảng cách kế) thay vì 3.
  - `Price_RoundsHalfAwayFromZero`: món 15 Coin, khách ×1,5 → 23 (không phải 22).
  - `PriceMultiplier_Applies`: hồ sơ `PriceMultiplier` 1,5 cho 30.
  - `TickLargeDt_EqualsManySmallTicks`: cùng seed, `Tick(30)` một lần và 300 lần `Tick(0,1)` cho cùng `Served`, `Customers`, số lần gọi RNG.
  - `RngCallContract_ConstructorOne_PerArrivalThree`: `Calls == 1` sau constructor; sau lần khách đến đầu `Calls == 4`.
  - `Snapshots_ShowStatesInOrder`: một khách đi qua `WaitingForCook` hoặc `Cooking`, `Serving`, `Eating`, `Paying` theo thứ tự (lấy mẫu mỗi 0,1 giây, chuỗi trạng thái đã khử trùng lặp bằng đúng thứ tự dây chuyền).
  - `PaidHandlerThrows_DoesNotBreakSimulation`: handler ném lỗi (`LogAssert.Expect`), `Served` vẫn tăng và mô phỏng tiếp tục.
  - `Workers_ReportCustomerWhileBusy`: người nấu có `CustomerId` bằng `Id` khách đang `Cooking`, rảnh thì `-1`.
- [x] **Step 2: Chạy test, kỳ vọng đỏ.**
- [x] **Step 3: Implement** theo Interfaces và hợp đồng RNG.
- [x] **Step 4: Chạy test, kỳ vọng xanh:** `failed="0"`, `total="268"`.
- [x] **Step 5: Commit và push** (`feat: add RestaurantSimulation customer pipeline and payment`).

---

### Task 6: `RestaurantSimulation`: kiên nhẫn, thiếu người, trạm bị chặn, đổi hồ sơ, Reset

**Files:**
- Modify: `Assets/_Project/Restaurant/Scripts/RestaurantSimulation.cs`
- Test: `Assets/_Project/Tests/RestaurantSimulationTests.cs`

**Interfaces:**
- Consumes: Task 5.
- Produces, thêm vào `RestaurantSimulation`: `public void SetStationBlocked(StaffRole role, bool blocked);` `public void Reset();`. Luật theo [spec 4.3](../specs/2026-11-01-restaurant-design.md#43-mô-phỏng-restaurantsimulation-class-thuần): hai đồng hồ kiên nhẫn (1: từ lúc đến tới lúc bắt đầu `Eating`; 2: từ lúc ăn xong tới lúc thanh toán xong, đều dùng `PatienceSeconds` của loại khách); hết một trong hai thì khách rời không trả tiền, việc đang làm của khách bị huỷ ngay, nhả bàn, `Abandoned` tăng. Trạm chặn: người của vai ngừng nhận việc mới và việc đang làm đóng băng tiến độ. `SetProfile` đổi giữa chừng: người còn ở cùng vai giữ việc; người bị rút đi hoặc vượt số ô thì việc của họ quay lại **đầu** hàng đợi của trạm, mất tiến độ; thêm bàn hoặc người có hiệu lực ngay. `Reset()` xoá khách, hàng đợi, `Served`, `Abandoned`, rồi bốc lại khoảng cách đến đầu tiên (một lần RNG).

- [x] **Step 1: Viết test đỏ (17 test)** (thêm vào `RestaurantSimulationTests`; khách có `PatienceSeconds` ngắn khi cần, trạm cố tình chậm bằng chỉ số 0 và thời gian gốc lớn):
  - `Patience1_ExpiresInEachState_NoPayment_FreesResources`: `[TestCase]` 5 ca ứng với khách đang `WaitingForTable`, `WaitingForCook`, `Cooking`, `WaitingForServer`, `Serving`: khi hết kiên nhẫn `Served == 0`, `Abandoned == 1`, không `CustomerPaid`, khách biến mất, bàn được nhả (khách kế ngồi được), người làm rảnh (`CustomerId == -1`).
  - `Patience2_ExpiresWhileWaitingForCashier_NoPayment` và `Patience2_ExpiresWhilePaying_NoPayment` (2 test): cashier chậm hơn kiên nhẫn.
  - `Eating_DoesNotRunPatience`: kiên nhẫn 5 giây, ăn 8 giây vẫn trả tiền nếu kịp các trạm.
  - `MissingRole_CustomersLeaveUnpaid`: `[TestCase]` 3 ca (không có Cook / Server / Cashier): sau đủ thời gian `Served == 0`, `Abandoned >= 1`.
  - `StationBlocked_FreezesProgress_ThenResumes`: chặn Cook giữa lúc nấu, 10 giây sau tiến độ không đổi; mở lại thì xong phần còn lại, không nấu lại từ đầu.
  - `StationBlocked_NoNewJobsStarted`: chặn Server khi khách đang `WaitingForServer` thì không có `Serving` mới tới khi mở.
  - `SetProfile_MoreTables_AppliesImmediately` và `SetProfile_NewWorker_AppliesImmediately` (2 test).
  - `SetProfile_RemovedWorker_JobReturnsToFrontOfQueue_ProgressLost`: rút người nấu giữa chừng, khách về `WaitingForCook` đứng **trước** khách đến sau; người thay vào nấu lại đủ thời gian.
  - `Reset_ClearsCustomersCountersAndQueues`: sau `Reset`, `Customers` rỗng, `Served == 0`, `Abandoned == 0`, khách không tự xuất hiện trước khoảng cách đầu mới.
- [x] **Step 2: Chạy test, kỳ vọng đỏ** (`error CS1061` về `SetStationBlocked`/`Reset`).
- [x] **Step 3: Implement.**
- [x] **Step 4: Chạy test, kỳ vọng xanh:** `failed="0"`, `total="285"`.
- [x] **Step 5: Commit và push** (`feat: add patience, blocked stations, profile changes and reset to simulation`).

---

### Task 7: `RestaurantService`, Runner, bất biến, nối `Boot`, dữ liệu mẫu

**Files:**
- Create: `Assets/_Project/Restaurant/Scripts/RestaurantService.cs`, `RestaurantRunner.cs`
- Modify: `Assets/_Project/Boot/Scripts/BunDauPeo.Boot.asmdef` (thêm `BunDauPeo.Restaurant`), `GameBootstrap.cs`, `Assets/_Project/Editor/BunDauPeo.Editor.asmdef` (thêm `BunDauPeo.Restaurant`), `ProjectSetup.cs`
- Test: `Assets/_Project/Tests/RestaurantServiceTests.cs`, `RestaurantInvariantTests.cs`, `ProjectSetupTests.cs`
- Generate (bởi `ProjectSetup.Run`): `Assets/_Project/Restaurant/Data/` (món, khách, sự cố, sáu nâng cấp, `UpgradeCatalog.asset`, `RestaurantConfig.asset`); sửa `Scenes/Boot.unity`.

**Interfaces:**
- Consumes: Task 1–6.
- Produces:
  ```csharp
  public sealed class RestaurantService {
      public RestaurantService(RestaurantConfig config, PlayerSave save, Wallet wallet, Roster roster, System.Random random, Func<bool> persist);
      public bool IsRunning { get; }                      // false khi config.Validate() còn lỗi (LogError một lần)
      public RestaurantSimulation Simulation { get; }
      public StaffingService Staffing { get; }
      public RestaurantProfile Profile { get; }
      public IncomeEstimate Estimate { get; }             // 0 khi !IsRunning
      public bool IsPresent { get; }
      public event Action ProfileChanged;
      public void SetPresent(bool present);               // chỉ ghi cờ; Task 11 dùng
      public void Update(float unscaledDt);               // Tick, đếm AutosaveSeconds rồi persist(); không làm gì khi !IsRunning
      public void ResetRuntime();                         // Simulation.Reset, xoá bộ đếm, đồng hồ lưu định kỳ
  }
  public sealed class RestaurantRunner : MonoBehaviour {
      public const float MaxDelta = 0.5f;
      public static float Clamp(float unscaledDeltaTime);   // Mathf.Min(giá trị, MaxDelta)
      public void Init(RestaurantService service);           // Update() => service.Update(Clamp(Time.unscaledDeltaTime))
  }
  ```
  Dựng hồ sơ bằng `RestaurantProfile.Build` ở constructor và mỗi khi `Roster.Changed`, rồi `Simulation.SetProfile`; nghe `Simulation.CustomerPaid` để `Wallet.Add(Coin, số)` đúng một lần; sau `Persist` lỗi thì chỉ log, không ném. `ProjectSetup` thêm hằng `RestaurantConfigPath = "Assets/_Project/Restaurant/Data/RestaurantConfig.asset"`, `UpgradeCatalogPath = ".../UpgradeCatalog.asset"`, tạo dữ liệu mẫu đúng [spec 3.2–3.4](../specs/2026-11-01-restaurant-design.md#32-so-của-restaurant) (3 món, 3 khách, 3 sự cố, 6 nâng cấp theo bảng 3.4; asset đã có thì giữ nguyên) và vá `Boot.unity` gán `_restaurantConfig` khi còn trống (nạp asset **sau** khi mở scene, L-002). **`_startScene` chưa đổi ở task này** (Task 8, khi scene đã có). `GameBootstrap`: `[SerializeField] private RestaurantConfig _restaurantConfig;` (kiểm null cùng khuôn `_bannerCatalog`); sau khi đăng ký `GachaService`: tạo `RestaurantService(_restaurantConfig, _session.Save, _session.Wallet, _session.Roster, new System.Random(), _session.Persist)`, `AddComponent<RestaurantRunner>().Init(service)` lên chính GameObject, `ServiceLocator.Register(service)`, `_session.ResetCompleted += service.ResetRuntime`.
- Lưu ý Play: từ đây đến hết bước vá Boot của task này, bấm Play sẽ log lỗi vì `Boot.unity` chưa gán `_restaurantConfig`.

- [x] **Step 1: Viết test đỏ (17 test)**
  - `RestaurantServiceTests` (8, thêm `Autosave_PersistFailure_DoesNotThrow_AndRetriesNextInterval` vì giao diện đòi "Persist lỗi thì chỉ log, không ném"; fixture dựng service qua một hàm `Make(...)` duy nhất để Task 10, 11, 13 chỉ sửa một chỗ): `CustomerPaid_AddsCoinToWallet_ExactlyOnce`; `StaffMovedToBattleTeam_StopsIncomeAndEstimate` (sau `Roster.AddToTeam` người nấu duy nhất, `Estimate.CoinsPerSecond == 0` với `MissingRole == Cook`, khách đang chờ không còn được nấu, và Coin không tăng thêm từ người đó); `RosterChanged_RebuildsProfile_AndRaisesProfileChanged`; `InvalidConfig_IsNotRunning_UpdateDoesNotThrow_EstimateZero` (danh sách món rỗng, `LogAssert.Expect(LogType.Error, ...)` một lần); `ResetRuntime_ClearsSimulationAndCounters`; `Update_Autosaves_EveryAutosaveSeconds` (persist 0 lần ở 59,9 giây, 1 lần ở 60; `Update` bằng nhiều bước nhỏ); `Estimate_ReportsMissingRole`.
  - `RestaurantInvariantTests` (4, mỗi test là một trạng thái quán, `[TestCase]` hoặc 4 hàm; seed 1, 2, 3, mô phỏng 7200 giây qua `Tick(0,5)`; thu nhập thật/giây = Coin trả / 7200, lấy trung bình ba seed): **mới** (ba người chỉ số 30 mỗi vai, cấu hình mẫu), **đủ ba vai chỉ số cao**, **sau nâng cấp** (`Tables` 8, `ArrivalInterval` ×0,59, `DishPrice` ×1,5, mỗi vai 2 người), **quá tải** (arrival 3 giây, 3 bàn). Mỗi test: `thật <= lý thuyết × 1,10` và `thật >= lý thuyết × AfkFactorMax (0,5)`. Đỏ nghĩa là thuật toán đổi: **không** đổi seed hay nới dung sai để xanh; nếu chỉ trạng thái quá tải đỏ ở cận dưới thì dừng và báo user (xem Review Focus 2).
  - `RestaurantServiceTests.Clamp_CapsAtHalfSecond` (1): `RestaurantRunner.Clamp(0.1f) == 0.1f`, `Clamp(5f) == 0.5f`.
  - `ProjectSetupTests` (4): `SampleRestaurantConfig_IsValid`; `SampleUpgrades_MatchSpecTable` (sáu `Id`, số cấp, hiệu ứng, `Value` và chi phí Coin/Material từng cấp theo [spec 3.4](../specs/2026-11-01-restaurant-design.md#34-sáu-nâng-cấp-mẫu)); `BootScene_HasRestaurantConfig` (đọc `_restaurantConfig` từ `Boot.unity`, L-002); `Setup_RerunKeepsEditedRestaurantValues` (đổi `_baseTables` của config mẫu, chạy lại `Run`, giá trị không đổi; trả giá trị cũ trong `finally`).
- [x] **Step 2: Chạy test, kỳ vọng đỏ.**
- [x] **Step 3: Implement** `RestaurantService`, `RestaurantRunner`, nối `GameBootstrap` và `ProjectSetup`.
- [x] **Step 4: Chạy Setup ở batch mode** (lệnh chung): exit `0`, asset mẫu xuất hiện, `Boot.unity` có `_restaurantConfig`.
- [x] **Step 5: Chạy test, kỳ vọng xanh:** `failed="0"`, `total="302"`. Chạy lại Setup rồi test lần nữa; `git status` không có thay đổi mới ở `Restaurant/Data` sau commit.
- [x] **Step 6: Commit và push**

```bash
git add Assets/_Project/Restaurant Assets/_Project/Boot Assets/_Project/Editor Assets/_Project/Scenes/Boot.unity Assets/_Project/Tests
git commit -m "feat: add RestaurantService and runner, wire into Boot, add sample restaurant data"
git push
```

---

### Task 8: UI: phòng, HUD, scene Restaurant, dải DEV (chuyển scene)

Nạp skill `unity:ui-ugui` và `unity:unity-cli` trước khi làm.

**Files:**
- Modify: `Assets/_Project/UI/Scripts/BunDauPeo.UI.asmdef` (thêm `BunDauPeo.Restaurant`), `Assets/_Project/Tests/GachaSceneTests.cs`, `ProjectSetup.cs`
- Create: `Assets/_Project/UI/Scripts/IsoProjection.cs`, `RestaurantViewText.cs`, `RestaurantView.cs`, `RestaurantHud.cs`, `DevOverlay.cs`; `Assets/_Project/Editor/RestaurantSceneBuilder.cs`
- Generate: `Assets/_Project/Scenes/Restaurant.unity`; sửa `ProjectSettings/EditorBuildSettings.asset`
- Test: `Assets/_Project/Tests/IsoProjectionTests.cs`, `RestaurantViewTextTests.cs`, `RestaurantSceneTests.cs`

**Interfaces:**
- Consumes: `RestaurantService`, `RestaurantSimulation.Customers/Workers/CustomerPaid`, `IncomeEstimate`, `Wallet.Changed`, `ServiceLocator.Get<T>`, `GachaSceneBuilder` (mẫu dựng Canvas, EventSystem, nút, chữ).
- Produces, namespace `BunDauPeo.UI`:
  ```csharp
  public static class IsoProjection {
      public static Vector2 CellToLocal(float gx, float gy);   // ô 120×60: x = (gx − gy) × 60, y = −(gx + gy) × 30
      public static float DepthKey(float gx, float gy);        // gx + gy; lớn hơn = gần hơn = vẽ sau
  }
  public static class RestaurantViewText {
      public static string IncomeLine(IncomeEstimate estimate);          // "≈ 1,6 Coin/giây" (dấu phẩy thập phân, 1 chữ số); thiếu vai thì "Thiếu đầu bếp, quán không kiếm tiền" / "Thiếu nhân viên phục vụ, ..." / "Thiếu thu ngân, ..."
      public static string CounterLine(int served, int abandoned);        // "Phục vụ 12 · Bỏ về 2"
      public static string InvalidConfigLine();                           // "Cấu hình quán lỗi"
      public static float StarFill(double stars, int index);             // 0..1 cho ngôi sao thứ index (0..4)
  }
  ```
  `RestaurantView` (`MonoBehaviour`, `OnEnable` gọi `SetPresent(true)`, `OnDisable` gọi `SetPresent(false)`; mỗi khung đối chiếu `Simulation.Customers/Workers` theo `Id` và nội suy giữa điểm mốc, đặt thẳng khi mở lại màn); `RestaurantHud` (thanh trên: Coin/Material/Gem từ `Wallet.Changed`, `IncomeLine`, `CounterLine`, chữ "+N" bay từ quầy khi `CustomerPaid` với bể 8 đối tượng, dải sao dựng bằng `Image` `type = Filled` theo `StarFill`; ở Task này chất lượng hiện 0 sao). `DevOverlay`: **cả file trong `#if UNITY_EDITOR || DEVELOPMENT_BUILD`**; `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` tự dựng Canvas (`Image`, `Button`, `Text`) bằng code, `DontDestroyOnLoad`, không dựng khi chưa có `RestaurantService`; nút **Quán** và **Gacha** (`SceneManager.LoadScene` theo tên), hằng tên scene `"Restaurant"`, `"Gacha"`. `RestaurantSceneBuilder.Create(string scenePath)`: `Camera`, `EventSystem` + `InputSystemUIInputModule`, Canvas 1920×1080, sàn lưới 8×8 hình thoi đỏ/xanh xen kẽ (cha xoay 45° trong cha nén y 0,5, `Image` không gán sprite), điểm mốc (cửa, bếp, 8 bàn, quầy), `RestaurantView`, `RestaurantHud`; bỏ tường nếu ghép hình bình hành quá rườm rà. `ProjectSetup`: hằng `RestaurantScenePath = "Assets/_Project/Scenes/Restaurant.unity"`; `Run()` tạo scene nếu chưa có; Build Settings thứ tự `Boot`, `Gacha`, `Restaurant`; đổi `_startScene` từ `Gacha` sang `Restaurant` **một lần**, chỉ khi giá trị hiện là `Gacha`. Đổi `GachaSceneTests.BootScene_StartSceneIsGacha` thành `BootScene_StartSceneIsRestaurant` (kiểm `"Restaurant"`).

- [x] **Step 1: Viết test đỏ (16 test mới, 1 test đổi)**
  - `IsoProjectionTests` (5): `CellToLocal_Origin_IsZero`; `CellToLocal_StepInX_GoesRightAndDown` (1,0 → (60, −30)); `CellToLocal_StepInY_GoesLeftAndDown` (0,1 → (−60, −30)); `CellToLocal_Combined` (2,3 → (−60, −150)); `DepthKey_NearerCellIsLarger` (2,3 > 1,1).
  - `RestaurantViewTextTests` (7): `IncomeLine_FormatsRateWithCommaDecimal` (1,56 → chứa `"1,6"` và `"Coin/giây"`, không phụ thuộc culture máy); `IncomeLine_MissingRole_NamesRole` 3 ca Cook/Server/Cashier; `CounterLine_ShowsServedAndAbandoned`; `InvalidConfigLine_NotEmpty`; `StarFill_PartialStar` (3,1 sao → 1, 1, 1, 0,1, 0).
  - `RestaurantSceneTests` (4, mở scene Additive và đóng lại như `GachaSceneTests`): `RestaurantScene_HasCameraAndEventSystemWithInputSystemModule` (có `Camera`, `EventSystem`, `InputSystemUIInputModule`, không `StandaloneInputModule`); `RestaurantScene_ViewAndHudAreWired_AndUseOnlyImageAndText` (`RestaurantView` và `RestaurantHud` đủ tham chiếu `[SerializeField]`, không `RawImage`); `RestaurantScene_IsThirdInBuildSettings`; `DevOverlay_SceneTargetsAreInBuildSettings` (hai tên scene của `DevOverlay` có trong `EditorBuildSettings.scenes`).
  - Đổi `GachaSceneTests.BootScene_StartSceneIsGacha` → `BootScene_StartSceneIsRestaurant`.
- [x] **Step 2: Chạy test, kỳ vọng đỏ** (`error CS0246` về `IsoProjection`, `RestaurantScenePath`).
- [x] **Step 3: Implement** theo Interfaces; giữ view mỏng, mọi chọn chữ/số nằm trong hàm tĩnh có test.
- [x] **Step 4: Chạy Setup ở batch mode:** exit `0`, `Scenes/Restaurant.unity` xuất hiện.
- [x] **Step 5: Chạy test, kỳ vọng xanh:** `failed="0"`, `total="318"`.
- [x] **Step 6: Commit và push** (`git add Assets/_Project/UI Assets/_Project/Editor Assets/_Project/Scenes Assets/_Project/Tests ProjectSettings/EditorBuildSettings.asset`; `git status` trước, chỉ đưa file của task này; message `feat: add Restaurant scene, room view, HUD and dev overlay scene switch`).

---

### Task 9: UI: `StaffPanel`; thử trong Play Mode (điểm dừng giai đoạn A)

**Files:**
- Create: `Assets/_Project/UI/Scripts/StaffPanelRows.cs`, `StaffPanel.cs`
- Modify: `Assets/_Project/Editor/RestaurantSceneBuilder.cs`
- Test: `Assets/_Project/Tests/StaffPanelRowsTests.cs`, `RestaurantSceneTests.cs`

**Interfaces:**
- Consumes: `RestaurantService.Staffing/Profile/Estimate`, `RestaurantProfile.WithWorkers`, `IncomeCalculator.Estimate`, `Roster.GetOwned/GetBattleTeam`, `RestaurantConfig`.
- Produces: `public static class StaffPanelRows` (hàm thuần trong `BunDauPeo.UI`):
  ```csharp
  public readonly struct CandidateRow { public CharacterDefinition Character; public int Stat; public double IncomeDelta; public bool NeedsConfirm; public StaffRole? CurrentRole; }
  public static IReadOnlyList<CandidateRow> Candidates(StaffRole role, RestaurantConfig config, RestaurantProfile profile, Roster roster, string replacingId /* null nếu ô trống */);
  ```
  `Stat` là chỉ số của vai `role`, sắp giảm dần; `NeedsConfirm` khi đang ở đội chiến đấu; `IncomeDelta` = thu nhập lý thuyết của hồ sơ giả định (đặt người này vào `role`, bỏ `replacingId` nếu có, **và bỏ người này khỏi vai cũ nếu nó đang ở vai khác**) trừ thu nhập hiện tại. `StaffPanel`: ba cột, ô mở theo `Profile.Slots`, ô chưa mở khoá kèm gợi ý nâng cấp, bấm ô mở danh sách (mỗi dòng ghi tên, độ hiếm, chỉ số, "→ ±X Coin/giây"), hộp xác nhận khi `NeedsConfirm` ("X đang ở đội chiến đấu, chuyển vào quán?"), nút "Gỡ" ở ô có người; nút "Nhân viên" của `RestaurantHud` mở nó.

- [ ] **Step 1: Viết test đỏ (8 test)**
  - `StaffPanelRowsTests` (7): `Candidates_SortedByRoleStatDescending`; `Candidates_IncludeOnlyOwned`; `Candidates_BattleTeamMember_NeedsConfirm`; `Candidates_IncomeDelta_PositiveWhenBetterStat`; `Candidates_IncomeDelta_CountsLeavingOtherRole` (người đang là cashier duy nhất, chuyển sang Cook thì delta âm vì Cashier trống); `Candidates_ReplacingId_ComparesAgainstReplacedCharacter` (thay người chỉ số 30 bằng 50 cho delta dương, thay bằng người 10 cho delta âm); `Candidates_CurrentRole_ReportedForCharacterAlreadyStaffed`.
  - `RestaurantSceneTests.RestaurantScene_StaffPanelWired` (1): đủ tham chiếu `[SerializeField]` của `StaffPanel`.
- [ ] **Step 2: Chạy test, kỳ vọng đỏ.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Chạy Setup (batch) rồi test, kỳ vọng xanh:** `failed="0"`, `total="326"`.
- [ ] **Step 5: Thử trong Editor (Play Mode)** (theo [unity-workflow.md](../../guides/unity-workflow.md): `unity open .`, Play từ bất kỳ scene). Kiểm: Console không lỗi từ code dự án; thấy Coin 500, nhãn "≈ ... Coin/giây" (0 và "Thiếu ..." khi chưa xếp đủ ba vai); mở panel Nhân viên, xếp 3 nhân vật khởi đầu vào ba vai thì khách đến, ngồi, ăn, trả và "+N" bay lên, Coin tăng; rút người nấu thì quán đứng yên và nhãn đổi; xếp nhân vật khỏi đội chiến đấu hỏi xác nhận. Dải DEV chuyển được sang Gacha và về lại. **Lượt thử ghi vào save thật:** sao lưu `save.json*` trước, xong dùng `Tools/BunDauPeo/Reset Save Data...`; sau khi thử `git status` và hoàn tác file `ProjectSettings` bị đổi (L-007). Không tự thử được thì nhờ user và ghi rõ trong báo cáo.
- [ ] **Step 6: Commit và push** (`feat: add StaffPanel with income preview`).
- [ ] **Step 7: Dừng.** Báo user quán đã chạy được và thử được (giai đoạn A xong); đợi user thử rồi mới sang giai đoạn B, vì nếu cần đổi mô phỏng thì Chất lượng, Sự cố, AFK chưa phải viết lại.

---

## Giai đoạn B: nâng cấp

### Task 10: `UpgradeService`, `UpgradeTestPanel`, nút +50 Material

**Files:**
- Create: `Assets/_Project/Restaurant/Scripts/UpgradeService.cs`, `Assets/_Project/UI/Scripts/UpgradeTestPanel.cs`
- Modify: `RestaurantService.cs`, `RestaurantViewText.cs`, `DevOverlay.cs`, `RestaurantSceneBuilder.cs`, `RestaurantHud.cs`
- Test: `Assets/_Project/Tests/UpgradeServiceTests.cs`, `RestaurantServiceTests.cs`, `RestaurantViewTextTests.cs`, `RestaurantSceneTests.cs`

**Interfaces:**
- Consumes: `UpgradeCatalog`, `UpgradeDefinition.Validate/Levels/MaxLevel`, `Wallet.CanAfford/TrySpend/Get`, `PlayerSave.Upgrades`, `RestaurantProfile.Build`.
- Produces:
  ```csharp
  public enum UpgradeBuyStatus { Success, UnknownUpgrade, InvalidUpgrade, MaxLevel, NotEnoughCurrency }
  public sealed class UpgradeBuyResult { public UpgradeBuyStatus Status; public bool Saved; public int NewLevel; }
  public sealed class UpgradeService {
      public UpgradeService(PlayerSave save, Wallet wallet, UpgradeCatalog catalog, Func<bool> persist);
      public int GetLevel(string id);                 // 0 khi chưa có dòng
      public UpgradeBuyResult TryBuy(string id);
      public event Action Changed;                    // SafeInvoke, sau khi mua thành công
  }
  ```
  `TryBuy` đúng thứ tự [spec 4.8](../specs/2026-11-01-restaurant-design.md#48-nâng-cấp-upgradeservice-và-nhân-viên-staffingservice): tra id → `Validate()` còn lỗi → `MaxLevel` → `CanAfford(chi phí cấp kế)` → `TrySpend` (sai thì `NotEnoughCurrency`) → tăng cấp trong `save.Upgrades` → `Changed` → `persist()` đúng một lần (`false` thì `Saved = false`, bộ nhớ giữ nguyên, không hoàn tiền). `RestaurantService` thêm `public UpgradeService Upgrades { get; }` (tạo từ `config.Upgrades`), nghe `Upgrades.Changed` để dựng lại hồ sơ. `UpgradeTestPanel`: một cột nút, mỗi nâng cấp một nút ghi tên, cấp/tối đa, giá; mờ khi `!CanAfford` hoặc tối đa; thông báo khi `Status != Success` hoặc `Saved == false`; `RestaurantViewText.UpgradeButton(UpgradeDefinition def, int level)` và `UpgradeStatus(UpgradeBuyStatus status, bool saved)`. `DevOverlay` thêm nút **+50 Material** (`Wallet.Add`).

- [ ] **Step 1: Viết test đỏ (16 test)** (`wallet` có Coin 5000, Material 100 trừ khi ghi khác; catalog gồm nâng cấp `up_tables` cấp 1 = 4 bàn tốn 300 Coin + 5 Material):
  - `UpgradeServiceTests` (11): `Buy_SpendsBothCurrencies_RaisesLevel_PersistsOnce`; `Buy_MissingCoinOnly_ChangesNothing` (Coin 299); `Buy_MissingMaterialOnly_ChangesNothing` (Material 4); với hai ca này kiểm ví, cấp, `Changed` (0 lần) và số lần `persist` (0); `Buy_AtMaxLevel_ReturnsMaxLevel`; `Buy_UnknownId`; `Buy_InvalidUpgrade_NothingSpent` (cấp có chi phí tổng 0); `Buy_PersistFails_SavedFalse_KeepsState`; `Buy_PersistedExactlyOnce` (mua một lần, `persist` 1 lần); `Buy_Consecutive_SpendsEachLevelCost` (cấp 1 rồi cấp 2: tổng trừ 300 + 600 Coin, 5 + 10 Material); `Changed_RaisedOnSuccessOnly`; `Buy_ChangedHandlerThrows_StillPersistsAndKeepsState` (L-003).
  - `RestaurantServiceTests.BuyUpgrade_TakesEffectInSimulation_Immediately` (1): mua Bàn thì `Profile.Tables` tăng ngay và `ProfileChanged` được bắn.
  - `RestaurantViewTextTests` (3): `UpgradeButton_ShowsNameLevelAndCost`; `UpgradeButton_AtMax_ShowsMax`; `UpgradeStatus_EveryFailureHasDistinctText_SuccessSavedIsEmpty_SuccessUnsavedWarns`.
  - `RestaurantSceneTests.RestaurantScene_UpgradePanelWired` (1).
- [ ] **Step 2: Chạy test, kỳ vọng đỏ.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Chạy Setup (batch) rồi test, kỳ vọng xanh:** `failed="0"`, `total="342"`.
- [ ] **Step 5: Thử trong Play Mode:** bấm **+50 Material** vài lần, mua Bàn và ô; thấy ví trừ cả Coin lẫn Material, nút mờ khi thiếu; sau khi mua ô nấu thì xếp thêm người được. Dọn như Task 9 Step 5.
- [ ] **Step 6: Commit và push** (`feat: add UpgradeService and test upgrade panel`).

---

## Giai đoạn C: chất lượng, sự cố, AFK

### Task 11: `IClock`, `RestaurantQuality`

**Files:**
- Create: `Assets/_Project/Restaurant/Scripts/IClock.cs` (kèm `SystemClock`), `RestaurantQuality.cs`
- Modify: `RestaurantService.cs`, `RestaurantHud.cs`
- Test: `Assets/_Project/Tests/RestaurantQualityTests.cs`, `RestaurantServiceTests.cs`

**Interfaces:**
- Produces: `public interface IClock { DateTime UtcNow { get; } DateTime LocalNow { get; } }`, `SystemClock : IClock`; `public enum IncidentOutcome { Missed = 0, Late = 1, Fast = 2 }`;
  ```csharp
  public sealed class RestaurantQuality {
      public RestaurantQuality(PlayerSave save, RestaurantConfig config, IClock clock);
      public void RecordPresence(double seconds);              // cộng vào dòng hôm nay (LocalNow), tối đa 8 dòng
      public void RecordIncident(IncidentOutcome outcome);     // giữ 10 mục mới nhất
      public double Attendance { get; }   // 0..AttendanceMax
      public double IncidentPoints { get; } // 0..IncidentMax
      public double Total { get; }        // 0..100
      public double Stars { get; }        // Total / 20
      public double AfkFactor { get; }    // Lerp(AfkFactorMin, AfkFactorMax, Total / 100)
      public IReadOnlyList<float> DayCredits { get; }   // 7 phần tử, [0] = hôm qua, mỗi phần tử min(1, giây / DailyGoalSeconds)
      public double TodaySeconds { get; }
  }
  ```
  Công thức theo [spec 4.4](../specs/2026-11-01-restaurant-design.md#44-chất-lượng-restaurantquality): hôm nay **không** tính; `DayIndex` = số ngày từ 2000-01-01 của `LocalNow.Date`; dòng "tương lai" không nằm trong cửa sổ và không bị xoá trừ khi danh sách vượt 8 (bỏ dòng `DayIndex` nhỏ nhất). `RestaurantService` thêm tham số `IClock clock` ở constructor (cập nhật hàm `Make` của fixture Task 7), `Quality`, và `Update` gọi `Quality.RecordPresence(dt)` khi `IsPresent`. `RestaurantHud` hiện sao và điểm từ `Quality`.
- Test helper: `FakeClock : IClock` (đặt `UtcNow`, `LocalNow` tuỳ ý, `Advance(TimeSpan)`) trong `RestaurantTestData`.

- [ ] **Step 1: Viết test đỏ (14 test: 13 ở `RestaurantQualityTests`, 1 ở `RestaurantServiceTests`).** `AttendanceMax`/`IncidentMax` = 50/50, mục tiêu 300 giây.
  - `RestaurantServiceTests.RecordPresence_OnlyWhenPresent`: `Update(10)` khi `SetPresent(false)` không đổi `Quality.TodaySeconds`; khi `SetPresent(true)` thì cộng 10.
  - `Today_IsNeverCounted`: `RecordPresence(600)` hôm nay, `Attendance == 0`; sang ngày hôm sau (`Advance` 1 ngày) thì ngày đó thành "hôm qua" và được tính.
  - `FullYesterday_GivesOneSeventhOfAttendance`: một ngày đủ 300 giây → `50/7`.
  - `PartialDay_IsProportional_AndCappedAtOne`: 150 giây → nửa credit, 600 giây → bằng 300 giây.
  - `MissingDays_CountAsZero`.
  - `AbsenceTable_MatchesSpec`: người chơi đủ mục tiêu 7 ngày liên tiếp, sự cố tối đa (10 × kịp), rồi vắng 0 … 7 ngày: `Total` ≈ 100, 93, 86, 79, 71, 64, 57, 50 (±0,5); `Total >= IncidentPoints` luôn đúng.
  - `Incidents_AveragePoints_FastLateMissed`: 10 kịp → 50; 5 kịp + 5 bỏ lỡ → 25; 10 trễ → 25.
  - `EmptyIncidentSlots_CountAsZero`: 1 sự cố kịp → 5.
  - `Incidents_KeepOnlyLastTen`: 11 sự cố, cái đầu bị bỏ.
  - `RecordPresence_UpsertsToday_AndKeepsEightRows`: gọi nhiều lần cùng ngày chỉ một dòng; 10 ngày liên tiếp giữ 8 dòng, bỏ dòng cũ nhất.
  - `ClockBackwards_FutureRowsIgnoredButNotDeleted`: lùi đồng hồ 3 ngày, dòng cũ không nằm trong cửa sổ và không bị xoá khi chưa vượt 8 dòng.
  - `AfkFactor_AtZeroFiftyAndHundredPoints`: 0,1 / 0,3 / 0,5.
  - `Stars_IsTotalOverTwenty`.
  - `Presence_RespectsLocalMidnight`: `LocalNow` 23:59 rồi 00:01 ngày sau ghi vào hai ngày khác nhau (khác với `UtcNow`).
- [ ] **Step 2: Chạy test, kỳ vọng đỏ.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Chạy test, kỳ vọng xanh:** `failed="0"`, `total="356"`.
- [ ] **Step 5: Commit và push** (`feat: add IClock and RestaurantQuality`).

---

### Task 12: `IncidentDirector`, biểu tượng "!" trong view

**Files:**
- Create: `Assets/_Project/Restaurant/Scripts/IncidentDirector.cs`
- Modify: `RestaurantService.cs`, `RestaurantView.cs`, `RestaurantViewText.cs`, `StaffPanel.cs` (gọi `SetSpawnAllowed(false)` khi mở), `UpgradeTestPanel.cs` (cùng việc đó), `RestaurantSceneBuilder.cs`
- Test: `Assets/_Project/Tests/IncidentDirectorTests.cs`, `RestaurantServiceTests.cs`, `RestaurantViewTextTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public readonly struct ActiveIncident { public IncidentDefinition Definition; public StaffRole Role; public float Elapsed; public bool CanResolve; /* vai đang có người */ }
  public sealed class IncidentDirector {
      public IncidentDirector(RestaurantConfig config, System.Random random, RestaurantSimulation simulation, RestaurantQuality quality, Func<StaffRole, bool> roleHasStaff);
      public void SetPresent(bool present);          // false khi đang có sự cố thì Cancel()
      public void SetSpawnAllowed(bool allowed);     // false: không sinh mới, đồng hồ giữ ở 0; sự cố đang chạy vẫn đếm
      public void Update(float dt);
      public bool Resolve();                         // false và không đổi gì khi vai không có người hoặc không có sự cố
      public void Cancel();                          // huỷ không tính điểm, mở lại trạm
      public void Reset();
      public ActiveIncident? Active { get; }
  }
  ```
  Luật theo [spec 4.5](../specs/2026-11-01-restaurant-design.md#45-sự-cố-incidentdirector): chỉ chạy khi `SetPresent(true)`; chọn theo trọng số trong **mọi** sự cố kể cả vai trống; tối đa một sự cố; kích hoạt thì `SetStationBlocked(role, true)`; `Resolve()` ≤ `IncidentFastSeconds` là kịp, ≤ `IncidentLateSeconds` là trễ, quá hạn thì tự bỏ lỡ; kết thúc bằng cách nào cũng mở trạm và `Quality.RecordIncident`. Hợp đồng RNG: mỗi lần đặt đồng hồ sinh một lần (`SpawnMin + u × (SpawnMax − SpawnMin)`), mỗi lần sinh một lần chọn sự cố (cộng dồn `Weight`). `RestaurantService` thêm `Incidents`, truyền `roleHasStaff = role => Profile.Workers(role).Count > 0`, chuyển `SetPresent` sang director và gọi `Incidents.Update(dt)` trong `Update`, `ResetRuntime` gọi `Incidents.Reset()`. `RestaurantView` vẽ "!" (và dòng "Cần xếp người vào vai X" khi `!CanResolve`) ở trạm của sự cố, bấm gọi `Resolve()`; `RestaurantViewText.IncidentLabel(ActiveIncident)` và `IncidentHint(ActiveIncident)`.

- [ ] **Step 1: Viết test đỏ (21 test)** (`FakeClock`, `ScriptedRandom`; `SpawnMin 60`, `SpawnMax 120`, kịp 8, trễ 20; `roleHasStaff` điều chỉnh được):
  - `IncidentDirectorTests` (17): `NoSpawn_WhenNotPresent`; `Spawns_AfterRandomInterval` (u = 0 → 60 giây, u = 1 → 120 giây); `EmptyRole_StillSpawns`; `OnlyOneIncidentAtATime`; `SpawnNotAllowed_NoSpawn_ThenSpawnsImmediatelyWhenReallowed`; `ActiveIncident_KeepsCounting_WhenSpawnNotAllowed`; `Resolve_FastBoundary` (tại 8,0 là kịp, tại 8,01 là trễ → điểm `Fast`/`Late` đúng); `Resolve_LateBoundary` (tại 20,0 là trễ, tại 20,01 tự bỏ lỡ trước khi `Resolve`); `Missed_AutoAtLateSeconds_RecordsZero`; `Resolve_EmptyRole_ReturnsFalse_NoScoreNoStateChange`; `Resolve_AfterStaffAssignedInTime_ReturnsTrue`; `Resolve_StaffRemovedMidIncident_ReturnsFalse`; `StationBlocked_WhileActive_UnblockedAfterEnd` (kiểm mô phỏng: trạm đóng băng rồi chạy lại); `Cancel_OnLeavingOrBackground_NoScore_Unblocks`; `Pick_IsWeighted`; `RngCallContract_OnePerTimerOnePerPick`; `Outcome_IsRecordedInQuality` (kịp = 2, trễ = 1, bỏ lỡ = 0 trong `IncidentScores`).
  - `RestaurantServiceTests` (2): `Presence_SpawnsIncidentAndBlocksStation_ThroughService`; `ResetRuntime_CancelsIncidentAndUnblocks`.
  - `RestaurantViewTextTests` (2): `IncidentLabel_NamesDefinition`; `IncidentHint_AsksForStaff_OnlyWhenRoleEmpty`.
- [ ] **Step 2: Chạy test, kỳ vọng đỏ.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Chạy Setup (batch) rồi test, kỳ vọng xanh:** `failed="0"`, `total="377"`.
- [ ] **Step 5: Commit và push** (`feat: add IncidentDirector with empty-role incidents`).

---

### Task 13: `AfkService`, nút Thu AFK, Tua AFK, `OnAppPause`/`OnAppResume`

**Files:**
- Create: `Assets/_Project/Restaurant/Scripts/AfkService.cs`
- Modify: `RestaurantService.cs`, `GameBootstrap.cs`, `RestaurantHud.cs`, `RestaurantViewText.cs`, `DevOverlay.cs`, `RestaurantSceneBuilder.cs`
- Test: `Assets/_Project/Tests/AfkServiceTests.cs`, `RestaurantServiceTests.cs`, `RestaurantViewTextTests.cs`, `RestaurantSceneTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public readonly struct AfkCollectResult { public long Coins; public bool Saved; }
  public sealed class AfkService {
      public AfkService(PlayerSave save, Wallet wallet, RestaurantConfig config, IClock clock, RestaurantQuality quality,
                        Func<double> currentIncomePerSecond, Func<bool> persist);
      public void Heartbeat();             // LastActiveUtcTicks = UtcNow, RatePerSecond = currentIncomePerSecond()
      public void CatchUp();               // theo spec 4.6, gồm nhánh RatePerSecond > 0 và PendingAbsenceSeconds
      public double PendingTheoretical { get; }
      public double AccumulatedSeconds { get; }
      public bool IsFull { get; }
      public long PreviewCollect();        // floor(PendingTheoretical × quality.AfkFactor)
      public AfkCollectResult Collect();   // Wallet.Add(Coin), về 0, persist đúng một lần
      public double TakePendingAbsence();  // trả số giây vắng đang chờ rồi về 0 (không lưu vào save)
      public void Rewind(TimeSpan by);     // lùi LastActiveUtcTicks (chỉ DevOverlay dùng)
  }
  ```
  `RestaurantService` thêm `Afk` (hàm `currentIncomePerSecond = () => Estimate.CoinsPerSecond`, nên bằng 0 khi `!IsRunning`; `CatchUp` ở constructor; `!IsRunning` thì `Heartbeat`/`CatchUp` không tích thêm và giữ số đã có), `OnAppPause()` (`Incidents.Cancel()`, `Afk.Heartbeat()`), `OnAppResume()` (`Afk.CatchUp()`), và `Update` gọi `Afk.Heartbeat()` rồi `persist()` mỗi `AutosaveSeconds`; `ResetRuntime` gọi `TakePendingAbsence()` để xoá số giây vắng đang chờ. `GameBootstrap`: `OnApplicationPause(true)` gọi `service.OnAppPause()` **trước** `Persist()`; `OnApplicationPause(false)` gọi `OnAppResume()`; `OnApplicationQuit` gọi `OnAppPause()` rồi `Persist()`. `RestaurantHud` hiện nút "Thu AFK (X Coin)" khi `PendingTheoretical > 0`; `RestaurantViewText.AfkButton(long coins)`. `DevOverlay` thêm nút **Tua AFK +1 giờ**: `Afk.Rewind(1h)` rồi `Afk.CatchUp()` ngay.

- [ ] **Step 1: Viết test đỏ (25 test)** (`FakeClock`, `Wallet` Coin 0, `quality` có `AfkFactor` điều chỉnh được qua dữ liệu: hệ số 0,1 khi 0 điểm, 0,5 khi 100 điểm; trần 12 giờ; ngưỡng popup 300 giây):
  - `AfkServiceTests` (19): `Heartbeat_StoresTicksAndCurrentRate`; `CatchUp_Accumulates_RateTimesElapsed` (rate 2, vắng 100 giây → pending 200); `Cap_IsTotalAcrossTwoExits` (rate 1: vắng 8 giờ, rồi 8 giờ nữa chỉ cộng thêm 4 giờ, `AccumulatedSeconds == 43200`); `Full_AccumulatesNothingMoreUntilCollect`; `ClockBackwards_AddsZero_AndResetsAnchor`; `ClockForwardTwentyHours_CapsAtMax`; `TwoConsecutiveCatchUps_SecondAddsZero`; `NewGame_ZeroTicks_OnlyAnchorsNothingAdded`; `ZeroRate_TwelveHourAbsence_DoesNotEatTheCap` (rate 0 vắng 12 giờ: `AccumulatedSeconds == 0`; rồi `Heartbeat` với rate 1, vắng 12 giờ nữa vẫn tích đủ 12 giờ); `TakePendingAbsence_ReturnsSecondsThenZero`; `ShortCatchUp_AfterLongAbsence_DoesNotOverwritePendingAbsence` (vắng 1 giờ, `CatchUp`, rồi `CatchUp` ngay lần hai: `TakePendingAbsence` vẫn trả 3600); `AbsenceBelowPopupMin_NoPendingAbsence`; `StaleHeartbeat_FiftyNineSecondsOld_CountsOnlyThatGap` (kill giữa chừng: elapsed tính từ heartbeat cũ 59 giây); `PreviewCollect_UsesFactorAtCollectTime` (cùng pending, hệ số đổi giữa hai lần gọi cho hai kết quả khác); `Collect_AddsFlooredCoins_ZeroesPendingAndSeconds_PersistsOnce` (pending 29203 × 0,35 = 10221,05 → 10221); `Collect_PersistFails_SavedFalse_KeepsState`; `Collect_NothingPending_ReturnsZero_NoPersist`; `Rate_FrozenAtHeartbeat_ChangesOnNextHeartbeat` (đổi `currentIncomePerSecond` sau `Heartbeat` không đổi `RatePerSecond`); `Rewind_MovesAnchorBack_ThenCatchUpAccumulates`.
  - `RestaurantServiceTests` (5): `OnAppPause_HeartbeatsBeforeAnyPersist` (ghi thứ tự: mốc `LastActiveUtcTicks` đã đổi tại lúc `persist` đầu tiên được gọi); `OnAppResume_CatchesUp`; `ResetRuntime_ClearsPendingAbsence`; `Autosave_AlsoHeartbeats`; `InvalidConfig_AfkDoesNotAccumulate_AndKeepsExistingPending`.
  - `RestaurantViewTextTests.AfkButton_ShowsPreviewCoins` (1).
- [ ] **Step 2: Chạy test, kỳ vọng đỏ.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Chạy Setup (batch) rồi test, kỳ vọng xanh:** `failed="0"`, `total="402"`.

- [ ] **Step 5: Thử trong Play Mode:** xếp đủ ba vai, bấm **Tua AFK +1 giờ**, thấy nút "Thu AFK (X Coin)"; thu thì Coin vào ví, nút biến mất; Tua 13 giờ thì pending dừng ở trần 12 giờ. Dọn như Task 9 Step 5.
- [ ] **Step 6: Commit và push** (`feat: add AfkService with heartbeat, catch-up and collect`).

---

### Task 14: UI: `AfkPopup`, `QualityPopup`; thử trong Play Mode

**Files:**
- Create: `Assets/_Project/UI/Scripts/AfkPopup.cs`, `QualityPopup.cs`
- Modify: `RestaurantViewText.cs`, `RestaurantView.cs`, `RestaurantHud.cs`, `RestaurantSceneBuilder.cs`
- Test: `Assets/_Project/Tests/RestaurantViewTextTests.cs`, `RestaurantSceneTests.cs`

**Interfaces:**
- Consumes: `RestaurantService.Afk/Quality/Estimate`, `AfkService.TakePendingAbsence/PreviewCollect/Collect/IsFull`, `RestaurantQuality.DayCredits/TodaySeconds/Attendance/IncidentPoints/Stars/AfkFactor`.
- Produces: `RestaurantViewText.AbsenceText(double seconds)` ("5 giờ 12 phút"), `FullCapLine(bool isFull)` (rỗng khi chưa đầy), `AfkSummary(...)`, `DayCellFill(float credit)`, `TodayLine(double seconds, double goalSeconds)` ("Hôm nay: a/b phút (tính vào ngày mai)"), `IncidentBreakdown(IReadOnlyList<int> scores)` ("Kịp 5 · Trễ 1 · Bỏ lỡ 1 · Chưa có 3"). `RestaurantView` gọi `TakePendingAbsence()` ở `OnEnable` và mỗi khung; kết quả `> 0` và `PreviewCollect() > 0` thì hiện `AfkPopup` (thời gian vắng, Coin lý thuyết chờ, sao và điểm, hệ số, Coin nhận, mẹo, nút Thu thập / Để sau, dòng "Đã đầy, ..." khi `IsFull`). Bấm thẻ sao ở HUD mở `QualityPopup` (Chuyên cần x/50 với 7 ô ngày, dòng hôm nay, Xử lý sự cố y/50, hệ số AFK). Mở popup thì `SetSpawnAllowed(false)`, đóng thì `true`.

- [ ] **Step 1: Viết test đỏ (7 test)**
  - `RestaurantViewTextTests` (6): `AbsenceText_HoursAndMinutes` (18720 → chứa "5" và "12"); `AbsenceText_UnderAnHour_MinutesOnly`; `FullCapLine_OnlyWhenFull`; `DayCellFill_IsCredit`; `TodayLine_ShowsProgressAndNextDayNote`; `IncidentBreakdown_CountsKindLateMissedAndEmpty` (`[2,2,1,0]` → Kịp 2, Trễ 1, Bỏ lỡ 1, Chưa có 6).
  - `RestaurantSceneTests.RestaurantScene_PopupsWired` (1).
- [ ] **Step 2: Chạy test, kỳ vọng đỏ.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Chạy Setup (batch) rồi test, kỳ vọng xanh:** `failed="0"`, `total="409"`.
- [ ] **Step 5: Thử trong Play Mode:** Tua AFK, mở lại màn quán từ Gacha thì popup hiện đúng một lần với số hợp lý; Quality popup khớp số liệu; trong lúc popup mở không sinh sự cố mới; xử lý sự cố bằng cách bấm "!"; vai trống thì "!" ghi cần xếp người. Dọn như Task 9 Step 5.
- [ ] **Step 6: Commit và push** (`feat: add AFK and quality popups`).

---

### Task 15: Cập nhật docs

**Files:**
- Modify: `docs/project/structure.md` ("Hiện tại": thêm `Restaurant/`, mục UI mới, số test thật thay chỗ ghi 184, asset mẫu mới), `docs/roadmap.md` (chuyển Restaurant sang "Đã xong" kèm ngày, link spec và kế hoạch, số test), `docs/design/game-design.md` (đối chiếu mục Quán, AFK, Chất lượng, Sự cố, Nhân vật với những gì đã làm; sửa chỗ lệch), `docs/guides/unity-workflow.md` (nếu có lệnh CLI mới hoặc cái bẫy mới)
- Modify (nếu có lỗi phát sinh): `docs/lessons/lesson.md`

- [ ] **Step 1:** Cập nhật các file trên cho khớp thực tế (số test lấy từ lần chạy cuối, `total="409"` nếu không đổi). Không sửa `docs/vision/vision.md`; không sửa `docs/decisions/decisions.md` trừ khi user yêu cầu.
- [ ] **Step 2:** Mỗi lỗi hoặc điều user sửa lại trong đợt này: một mục lesson ở đầu danh sách theo mẫu.
- [ ] **Step 3: Commit và push** (`docs: record Restaurant in structure, roadmap and game design`).

---

## Hoàn tất đợt

- [ ] Xoá `../bdp-tests.*` và `../bdp-setup.log`.
- [ ] Gọi một subagent mới (Opus 5.5, effort xhigh, theo [agent-routing.md](../../guides/agent-routing.md)) review toàn nhánh `feature/restaurant` theo spec, tập trung `UpgradeService`, `AfkService`, `RestaurantQuality`, `IncidentDirector`, `RestaurantSimulation`, Reset và mục Review Focus.
- [ ] Sửa các lỗi review tìm ra, chạy lại test, báo cáo user.
