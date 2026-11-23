# Cấu trúc thư mục & đặt tên

> **Đọc khi:** tạo file/thư mục mới, tìm chỗ đặt asset hoặc script.
> **Cập nhật khi:** thêm thư mục cấp 1–2 dưới `Assets/` hoặc đổi quy ước đặt tên.

## Hiện tại

```
Assets/
  Scenes/      SampleScene.unity (scene mẫu của template)
  Settings/    URP, Renderer2D, Input actions (do template sinh ra)
  _Project/
    Core/          BunDauPeo.Core        Wallet, SaveService, PlayerSave (kèm UpgradeLevel, DailyPresenceEntry, AfkState, `RestaurantBarOpen` cờ thanh quán, `PlayerName` và `AvatarCharacterId` của Hồ sơ, đều `""` khi chưa đặt, Reset về `""`), Catalog<T>, ServiceLocator, GameResetter (UI gọi reset không qua Boot), GameLanguage (chưa chọn thì tiếng Anh) + LanguageSwitcher (ngôn ngữ đang dùng, nút đổi ngôn ngữ không qua Boot), RestaurantBarPref (đọc/ghi cờ thanh quán, `GameBootstrap` đăng ký), PlayerProfile (đọc/ghi tên và avatar, sự kiện `Changed`, `GameBootstrap` đăng ký), EventExtensions, kiểu tiền/vai trò
    Characters/    BunDauPeo.Characters  CharacterDefinition, SkillDefinition, CharacterCatalog, Roster (kèm mảnh nhân vật, `SpendFragments`), StarConfig (giá và % mỗi sao), StarService (`TryStarUpAsync` gọi server, sao và chỉ số theo sao). Core: StarUpEntry (số lần đã nâng, `PlayerSave.StarUps`)
    Gacha/         BunDauPeo.Gacha       GachaBannerDefinition, GachaBannerCatalog, GachaService (`PullSingleAsync`/`PullTenAsync` gọi server; roll nằm ở `Rules/GachaRules`), GachaPullResult
    Restaurant/    BunDauPeo.Restaurant  RestaurantConfig và SO (món, khách, sự cố, nâng cấp), RestaurantProfile, StaffingService, IncomeCalculator, RestaurantSimulation, UpgradeService, RestaurantQuality, IncidentDirector, AfkService, IClock, RestaurantService + RestaurantRunner (chỉ tham chiếu Core và Characters)
    UI/            BunDauPeo.UI          Gacha: GachaView (điều phối), GachaRevealSequence, GachaRevealView, GachaTotalView, GachaRollAgainPopup, GachaHistoryPopup, GachaViewText. Nhân vật: CharactersView, CharacterCardView, CharacterListRows, CharactersViewText. Quán: RestaurantView (người đi lại có nhún và đung đưa, "!" sự cố, bóng trạng thái khách, đĩa món, lấy số giây vắng), RestaurantHud (sao, thu nhập, nút Thu AFK, bong bóng "+N"), RestaurantBarView (thanh quán thu gọn, nhớ qua `PlayerSave.RestaurantBarOpen`), WalkPose + CustomerBubble + TableDishes (hàm thuần cho người, bóng, đĩa), StaffPanel + StaffPanelRows (lưới 3×3 ô 500×220), UpgradeTestPanel + UpgradeCardView (lưới thẻ 3×2, nút Mua riêng), AfkPopup, QualityPopup, RestaurantViewText, IsoProjection, DevOverlay (chỉ Editor và Development Build; mở từ Menu → DEV, không còn nút ở góc), `Text/`: UiText (bảng chữ Việt–Anh, một file mỗi khu, có `UiText.Hub.cs` cho key `hub.*`) và LocalizedText. Hub (spec UI hub): HubNav (thanh dưới 6 ô, 3 panel, Menu, ô chờ mở, ẩn thanh, bật `LobbyHud`, `ShowComingSoon`), ResourceBarView + ResourceBarRule + ResourceSlots/IResourcePage (thanh tài nguyên chung, ô hiện theo màn), HubTab, HubNavState, HubPending, IHubPage (trang kín màn đóng được từ ô khác), MenuView + MenuViewText (Ngôn ngữ, Reset có xác nhận, DEV) Đợt C: TitleView + TitlePulse (màn Tiêu đề, chạm vào Lobby), ModeSelectView (Chọn chế độ: thẻ Chương chính, 4 ô khoá "Sắp có"), NotifyRule (+ `NotifyKind`, `NotifySnapshot`; luật 5 loại việc), NotifyView (nút Thông báo, chấm đỏ, đích của từng dòng), NotifyPopup (hộp thả xuống), WorldFocus (kéo khung quán tới "!"), ProfilePopup (hộp Hồ sơ giữa màn: tên tối đa 12 ký tự, lưới avatar), AvatarView (hình tròn màu theo độ hiếm + chữ cái đầu), AvatarRule, PlayerNameRule, IHubOverlay (hộp nổi: không đổi bố cục Lobby, vẫn đóng được qua `HubNav.CloseOpenPages`).
    Boot/          BunDauPeo.Boot        GameBootstrap (chọn server, nạp scene khởi đầu; không dựng session), OnlineStartup (`IGameStarter`: đăng nhập → Bootstrap → tải save → dựng session và nối service; Xoá tài khoản), ServerChoice (Local/UGS theo Editor và loại build), GameSession (có `ResetCompleted`, `Persisted`, `ResetToDefaultsAsync` gọi server trước), NewGameConfig
    Rules/         BunDauPeo.Rules       C# thuần (`noEngineReferences`), dùng chung với module server: ServerProtocol (tên module, lệnh, khoá `save`/`state`), BootstrapResponse, các kiểu save (CurrencyAmount, PityEntry, StarUpEntry, StaminaState... giữ namespace `BunDauPeo.Core`), `ServerState`, `GameData`, `CommandRunner` + `ServerHandlers` (từng lệnh: gacha, nâng sao, vào trận, thưởng, DEV), GachaRules, StaminaRules, DataHash
    Online/        BunDauPeo.Online      UgsGameServer (`IGameServer` qua Authentication, Cloud Save, Cloud Code), UgsErrors (ngoại lệ SDK → `OnlineStatus`); `Cloud/BunDauPeoServer.ccmr` trỏ tới module ; đợt 3: UgsAccountService (tên đăng nhập + mật khẩu, Google qua IGoogleTokenProvider), UgsErrors.MapAccount
    Missions/      BunDauPeo.Missions    MissionCatalog (SO), MissionService (Nhiệm vụ do server trao), MailRule/MailService/LinkAccountMailSource (Hộp thư và thư ghim)
    (Core, Online đợt 1)                 IGameServer, OnlineStatus, ServerTime (giờ server), LocalGameServer (server giả cho test và Editor chế độ Local), CloudSync (đẩy khoá `save`: gộp lượt 5 giây, mất kết nối sau 30 giây), IGameStarter, AccountDeleter; đợt 3: IAccountService, LocalAccountService, AccountRules, AccountSwitcher, LocalProgress, IGoogleTokenProvider. UI: OnlineOverlay (hộp Không kết nối được / Mất kết nối / Máy khác, dựng bằng code, vẽ trên mọi scene)
    Editor/        BunDauPeo.Editor      Tools/BunDauPeo/Setup Project, Reset Save Data, BootScenePlayMode, RestaurantSceneBuilder (dựng cả Lobby), NavBarBuilder (thanh dưới, Menu, `OverBar`, nút và hộp Thông báo, hộp Hồ sơ), TitleSceneBuilder (scene Tiêu đề), GachaSceneBuilder, StageSelectSceneBuilder, CharactersSceneBuilder (ba panel của Lobby), BattleSceneBuilder; công cụ thử: PlaytestTools (mặt tiền + menu `Playtest/`), SaveBackup, BackgroundPlay
    Playtest/      BunDauPeo.PlaytestKit UiDriver (tìm, bấm, chờ nút), ScreenCapture1080; không tham chiếu assembly game, chỉ có trong Editor và Development Build (`defineConstraints`)
    Tests/         BunDauPeo.Tests       EditMode (932 test)
    Tests/PlayMode/ BunDauPeo.PlayModeTests  PlayMode (39 test): SmokeLoopTests (vòng Boot → trận AUTO → thưởng → nạp lại save, save tạm), PlaytestKitPlayTests
    Scenes/        Boot.unity (đầu Build Settings; thứ tự Boot, Title, Restaurant, Battle), Title.unity (màn Tiêu đề; Boot nạp nó qua `_startScene`, chạm vào Restaurant), Restaurant.unity (Lobby: quán, panel Nhân vật, Bản đồ, Gacha, Menu, thanh dưới), Battle.unity. Gacha, Characters, StageSelect không còn là scene, là panel trong Restaurant.unity
    Combat/                              asmdef BunDauPeo.Combat (chỉ tham chiếu Core và Characters; nhánh feature/combat). SO: CombatConfig, EnemyDefinition, WaveDefinition, StageDefinition, StageCatalog; logic thuần: CombatSkillRules, DamageCalculator, BattleFormation, BattleUnit, BattleSession, AutoPolicy, StaminaService (chỉ hiện thể lực theo giờ server, không ghi save), StageProgressService (mở màn, `CompleteBattleAsync`/`CompleteStoryAsync` gọi server, Material cộng vào ví một lần), CombatService (`EnterStageAsync` là đường vào trận thật, nhận vé của server; `StartBattle` chỉ cho dải DEV, không gọi server). Asset mẫu (4 quái, 7 màn, `CombatConfig`, `StageCatalog`) ở Combat/Data/
```

UI của Combat nằm trong `UI/Scripts/`: màn trận BattleView, BattleLayout, BattleUnitView, BattleCardView, BattleActionPanel, BattlePausePopup, BattleResultView; bản đồ StageMapView, StageNodeView, StageInfoPopup, StoryPlaceholderPopup, TeamSelectPopup, TeamCardView, TeamSelectRows; chữ CombatViewText. Builder `BattleSceneBuilder`, `StageSelectSceneBuilder` ở `Editor/`; scene `Battle.unity` (Build Settings: Boot, Restaurant, Battle; bản đồ là panel của Lobby). Test: 886 EditMode và 29 PlayMode. Mọi nút DEV nằm trong panel Cài đặt, mở từ Menu → DEV (hoặc Menu tạm dừng của trận): Chiến đấu thử, Màn thử, Hồi đầy thể lực, +50 Material, +1000 Gem, Tua AFK +1 giờ, Ngôn ngữ, Reset dữ liệu (luôn hỏi xác nhận, qua GameResetter).

Asset mẫu (6 nhân vật, 4 skill, `CharacterCatalog`, `NewGameConfig`, banner `banner_standard`, `GachaBannerCatalog`, và cho quán: `RestaurantConfig`, 3 món, 3 loại khách, 3 sự cố, 6 nâng cấp + `UpgradeCatalog`) nằm ở `Characters/Data/`, `Boot/Data/`, `Gacha/Data/` và `Restaurant/Data/`, do `Setup Project` sinh; chạy lại không ghi đè số đã chỉnh. Scene `Restaurant.unity` cũng do `Setup Project` sinh (một lần, có rồi thì giữ nguyên; đổi builder thì dựng lại bằng `ProjectSetup.RebuildRestaurantScene`, xem [unity-workflow](../guides/unity-workflow.md)); `Boot.unity` được vá chứ không tạo lại. Cutscene Gacha (2 clip + controller ở `Gacha/Animations/Summoning/`, 2 prefab ở `Gacha/Prefabs/Cutscenes/`, 3 PNG Spark/Ring/Glow ở `Gacha/Art/Summoning/`) do user cung cấp, copy nguyên từ Project_04 kèm `.meta`; GUID phải giữ nguyên (test `GachaRevealAssetTests`).
Save: `Application.persistentDataPath/save.json` (kèm `.bak`, `.tmp`, `.corrupt`), xem [D-004](../decisions/decisions.md). Từ Online đợt 1 file này chỉ là bản đệm; bản chính là khoá `save` trên Cloud Save ([D-014](../decisions/decisions.md)).

Ngoài `Assets/`: `ServerModule/` là module Cloud Code `.ccmr` (.NET 8): `BunDauPeoServer.sln`, `BunDauPeoServer/` (lệnh `Bootstrap`, `ResetPlayer`, `GachaPull`, `EnterStage`, `CompleteStage`, `CompleteStory`, `StarUp`, `DevAddGem`, `DevRefillStamina`; biên dịch kèm nguồn `Assets/_Project/Rules/` và nhúng `GameData.json` xuất từ project), `BunDauPeoServer.Tests/` (`dotnet test`). `bin/`, `obj/` không commit.

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
