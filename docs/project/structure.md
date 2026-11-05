# Cấu trúc thư mục & đặt tên

> **Đọc khi:** tạo file/thư mục mới, tìm chỗ đặt asset hoặc script.
> **Cập nhật khi:** thêm thư mục cấp 1–2 dưới `Assets/` hoặc đổi quy ước đặt tên.

## Hiện tại

```
Assets/
  Scenes/      SampleScene.unity (scene mẫu của template)
  Settings/    URP, Renderer2D, Input actions (do template sinh ra)
  _Project/
    Core/          BunDauPeo.Core        Wallet, SaveService, PlayerSave (kèm UpgradeLevel, DailyPresenceEntry, AfkState), Catalog<T>, ServiceLocator, GameResetter (UI gọi reset không qua Boot), EventExtensions, kiểu tiền/vai trò
    Characters/    BunDauPeo.Characters  CharacterDefinition, SkillDefinition, CharacterCatalog, Roster (kèm mảnh nhân vật)
    Gacha/         BunDauPeo.Gacha       GachaBannerDefinition, GachaBannerCatalog, GachaRoller, GachaService, GachaPullResult
    Restaurant/    BunDauPeo.Restaurant  RestaurantConfig và SO (món, khách, sự cố, nâng cấp), RestaurantProfile, StaffingService, IncomeCalculator, RestaurantSimulation, UpgradeService, RestaurantQuality, IncidentDirector, AfkService, IClock, RestaurantService + RestaurantRunner (chỉ tham chiếu Core và Characters)
    UI/            BunDauPeo.UI          Gacha: GachaView (điều phối), GachaRevealSequence, GachaRevealView, GachaTotalView, GachaRollAgainPopup, GachaHistoryPopup, GachaViewText. Quán: RestaurantView (người đi lại, "!" sự cố, lấy số giây vắng), RestaurantHud (tiền, sao, thu nhập, nút Thu AFK), StaffPanel + StaffPanelRows, UpgradeTestPanel (bản thử), AfkPopup, QualityPopup, RestaurantViewText, IsoProjection, DevOverlay (chỉ Editor và Development Build). Hub thật làm ở spec UI hub
    Boot/          BunDauPeo.Boot        GameBootstrap (nối GachaService, RestaurantService và CombatService, OnAppPause trước Persist, nạp scene khởi đầu), GameSession (có `ResetCompleted`), NewGameConfig
    Editor/        BunDauPeo.Editor      Tools/BunDauPeo/Setup Project, Reset Save Data, BootScenePlayMode, GachaSceneBuilder, RestaurantSceneBuilder, BattleSceneBuilder, StageSelectSceneBuilder; công cụ thử: PlaytestTools (mặt tiền + menu `Playtest/`), SaveBackup, BackgroundPlay
    Playtest/      BunDauPeo.PlaytestKit UiDriver (tìm, bấm, chờ nút), ScreenCapture1080; không tham chiếu assembly game, chỉ có trong Editor và Development Build (`defineConstraints`)
    Tests/         BunDauPeo.Tests       EditMode (715 test)
    Tests/PlayMode/ BunDauPeo.PlayModeTests  PlayMode (4 test): SmokeLoopTests (vòng Boot → trận AUTO → thưởng → nạp lại save, save tạm), PlaytestKitPlayTests
    Scenes/        Boot.unity (đầu Build Settings), Gacha.unity, Restaurant.unity (thứ ba; Boot nạp nó qua `_startScene`, đổi từ Gacha một lần), StageSelect.unity, Battle.unity
    Combat/                              asmdef BunDauPeo.Combat (chỉ tham chiếu Core và Characters; nhánh feature/combat). SO: CombatConfig, EnemyDefinition, WaveDefinition, StageDefinition, StageCatalog; logic thuần: CombatSkillRules, DamageCalculator, BattleFormation, BattleUnit, BattleSession, AutoPolicy, StaminaService (hồi theo giờ máy, `TrySpend` trừ và lưu), StageProgressService (mở màn, thưởng, `CompleteBattle`/`CompleteStory`), CombatService (`EnterStage` là đường vào trận thật, `StartBattle` chỉ cho dải DEV). Asset mẫu (4 quái, 7 màn, `CombatConfig`, `StageCatalog`) ở Combat/Data/
```

UI của Combat nằm trong `UI/Scripts/`: màn trận BattleView, BattleLayout, BattleUnitView, BattleCardView, BattleActionPanel, BattlePausePopup, BattleResultView; bản đồ StageMapView, StageNodeView, StageInfoPopup, StoryPlaceholderPopup, TeamSelectPopup, TeamCardView, TeamSelectRows; chữ CombatViewText. Builder `BattleSceneBuilder`, `StageSelectSceneBuilder` ở `Editor/`; hai scene `StageSelect.unity`, `Battle.unity` (Build Settings: Boot, Gacha, Restaurant, StageSelect, Battle). Test: 718 EditMode và 4 PlayMode. Dải DEV có thêm Màn chơi, Chiến đấu thử, Màn thử, Hồi đầy thể lực; nút Cài đặt (góc trên phải) mở panel DEV có +1000 Gem và Reset dữ liệu (luôn hỏi xác nhận, qua GameResetter).

Asset mẫu (6 nhân vật, 4 skill, `CharacterCatalog`, `NewGameConfig`, banner `banner_standard`, `GachaBannerCatalog`, và cho quán: `RestaurantConfig`, 3 món, 3 loại khách, 3 sự cố, 6 nâng cấp + `UpgradeCatalog`) nằm ở `Characters/Data/`, `Boot/Data/`, `Gacha/Data/` và `Restaurant/Data/`, do `Setup Project` sinh; chạy lại không ghi đè số đã chỉnh. Scene `Gacha.unity` và `Restaurant.unity` cũng do `Setup Project` sinh (một lần, có rồi thì giữ nguyên; đổi builder thì dựng lại bằng `ProjectSetup.RebuildRestaurantScene`, xem [unity-workflow](../guides/unity-workflow.md)); `Boot.unity` được vá chứ không tạo lại. Cutscene Gacha (2 clip + controller ở `Gacha/Animations/Summoning/`, 2 prefab ở `Gacha/Prefabs/Cutscenes/`, 3 PNG Spark/Ring/Glow ở `Gacha/Art/Summoning/`) do user cung cấp, copy nguyên từ Project_04 kèm `.meta`; GUID phải giữ nguyên (test `GachaRevealAssetTests`).
Save: `Application.persistentDataPath/save.json` (kèm `.bak`, `.tmp`, `.corrupt`), xem [D-004](../decisions/decisions.md).

## Quy ước (đã duyệt, áp dụng khi thêm nội dung)

```
Assets/
  _Project/            toàn bộ nội dung của game (tách khỏi asset bên thứ ba)
    Core/              lõi dùng chung: ví, save, ServiceLocator, kiểu dữ liệu cơ sở
    Characters/        định nghĩa nhân vật, skill, Roster
    Gacha/  Restaurant/  Combat/  UI/
    Boot/              khởi động game, nối service
    Editor/            công cụ chỉ chạy trong Editor
    Playtest/          công cụ thử chạy lúc Play (UiDriver, chụp ảnh), chỉ Editor và Development Build
    Scenes/
    Tests/             EditMode tests; Tests/PlayMode/ cho test Play thật
  Settings/            giữ nguyên
```

- Chia theo **tính năng**, không theo loại. Mỗi folder tính năng có các folder con `Scripts/`, `Data/` (asset SO), `Art/`, `Animations/`, `Prefabs/`.
- Mỗi tính năng là một assembly `BunDauPeo.<Tên>`. File `.asmdef` tạo cùng script đầu tiên, vì asmdef không có script sẽ sinh cảnh báo.
- Namespace trùng tên assembly, riêng `Editor` dùng `BunDauPeo.EditorTools` để không che lớp `UnityEditor.Editor`.
- Tên file C# = tên class chính (bắt buộc với MonoBehaviour).
- Prefab/asset: `PascalCase`, không dấu cách. Sprite: `Tên_trạng_thái_frame` (ví dụ `Player_Run_01`).

## Luật phụ thuộc (asmdef thực thi)

```
Core  <-  Characters  <-  Gacha, Restaurant, Combat  <-  UI, Boot
```

- `Gacha`, `Restaurant`, `Combat` **không phụ thuộc lẫn nhau**; dùng chung qua `Wallet`, `Roster`, `PlayerSave`.
- `UI` và `Boot` được tham chiếu mọi tính năng. `Editor` và `Tests` chỉ chạy trong Editor. `PlaytestKit` không tham chiếu assembly game nào; `Editor`, `Tests` và `PlayModeTests` tham chiếu nó.
- Một SO chỉ được trỏ tới SO cùng assembly hoặc ở tầng dưới.
- Logic là class C# thuần nhận phụ thuộc qua constructor. Chỉ MonoBehaviour view mới dùng `ServiceLocator`.

Lý do: [D-002](../decisions/decisions.md).
