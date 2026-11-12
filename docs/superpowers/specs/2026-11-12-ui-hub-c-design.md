# Spec: UI hub, đợt C (Tiêu đề, Chọn chế độ, Thông báo, Hồ sơ)

> Ngày: 2026-11-12 · Trạng thái: **đã duyệt** (user, 2026-11-12: từng câu hỏi thiết kế, ảnh mô phỏng bản 1 ở mục 10, và bản viết) · Phạm vi: màn Tiêu đề (scene riêng), màn Chọn chế độ trước bản đồ (Bản đồ có lại nút Quay lại), nút Thông báo có chấm và hộp danh sách 5 loại việc, Hồ sơ (tên sửa được, đổi avatar bằng nhân vật đã có).
> Nguồn: [luồng màn](../../design/screen-flow.md) mục "Cấu trúc", "Sơ đồ", "Các màn", "Nút Thông báo" (user duyệt 2026-11-09), [roadmap](../../roadmap.md) dòng "UI hub đợt C", [spec đợt A](2026-11-09-ui-hub-design.md) (`HubNav`, `HubPending`, `OpenOnNextLoad`, `IHubPage`, `OverBar`, vạch 120 px), [spec đợt B](2026-11-11-ui-hub-b-design.md) (`LobbyHud`, `ResourceBar`, `WorldScroll`, chỗ nút Thông báo), [D-005](../../decisions/decisions.md) (save, Reset), [D-012](../../decisions/decisions.md) (chữ qua `UiText`), [unity-workflow, UI và hình tạm](../../guides/unity-workflow.md#ui-và-hình-tạm), [code-style, Test](../../guides/code-style.md#test) (L-036). Ảnh tham khảo: `Mota/title-touch-to-start.jpg`, `Mota/quest-mode-select.jpg`, `Mota/hub-home-annotated-en.png`; Hồ sơ chưa có ảnh tham khảo, user chọn dựng mô phỏng. Kế hoạch: [plans/2026-11-12-ui-hub-c.md](../plans/2026-11-12-ui-hub-c.md).

## 1. Mục tiêu

Hoàn tất luồng màn đã duyệt trước khi sang Online: mở app có màn Tiêu đề, ô Màn chơi có bước chọn chế độ, người chơi luôn biết việc gì đang chờ mình và bấm một chạm là tới, avatar ở Lobby có Hồ sơ thật. Đây là đợt cuối của UI hub; phạm vi giữ nhỏ (hạn 31/12, sau đây còn Online): không thêm luật chơi, không dựng phần cần mạng.

Thành công khi:
1. Mở app: Boot → **Tiêu đề** (tên game, "Chạm để bắt đầu", số phiên bản) → chạm bất kỳ đâu → Lobby. Về từ trận, đổi ngôn ngữ, Reset, quay lại từ nền đều không qua Tiêu đề.
2. Ô Màn chơi mở **Chọn chế độ**: thẻ lớn "Chương chính" (bấm → Bản đồ) và 4 ô khoá Khó, Sự kiện, Thử thách, Đấu trường (ổ khoá, "Sắp có", bấm → dòng "Sắp có"). Bản đồ có nút Quay lại về Chọn chế độ. Đánh xong bấm Quay lại thì vẫn mở sẵn Bản đồ.
3. **Nút Thông báo** ở góc trên-phải mọi panel và trang của hub (trừ lúc thanh dưới ẩn); có **chấm đỏ** khi có ít nhất một việc (không hiện số). Bấm → hộp thả xuống, mỗi loại việc một dòng, bấm dòng → tới đúng chỗ (mục 5.3).
4. **Hồ sơ** mở khi bấm avatar: tên sửa được (tối đa 12 ký tự, rỗng thì hiện tên mặc định), lưới nhân vật đang sở hữu để đổi avatar; avatar ở Lobby đổi theo. Tên và avatar nhớ qua tắt mở game; Reset xoá cả hai.
5. Mọi chữ mới đi qua `UiText`, có bản tiếng Anh; `HardcodedTextTests` xanh. Object mới chưa có hình đặt tên `<Tên>_PH_<rộng>x<cao>`; object cũ giữ tên. Không nút nào lọt xuống dải 0–120 px (trừ hộp `OverBar`). Không nút nào đè nút Thông báo.
6. Test chạm và kéo trong PlayMode đi qua raycast thật (`UiDriver.Click`, L-036).

### Quyết định đã chốt với user (2026-11-12)

| Chủ đề | Quyết định |
|---|---|
| Tiêu đề là gì | **Scene riêng `Title.unity`** (Build Settings: Boot, Title, Restaurant, Battle) |
| Nút góc Tiêu đề | **Không có nút.** Chỉ tên game, "Chạm để bắt đầu", "Ver x.y" góc dưới-phải. Nút ngôn ngữ / xoá cache / hỗ trợ vẫn "để sau" |
| Khi nào hiện Tiêu đề | **Mỗi lần mở app** (khởi động lạnh). Không hiện khi về từ trận, đổi ngôn ngữ, Reset, quay lại từ nền |
| Tên game trên Tiêu đề | **"Bun Dau Peo"** (không dấu, giống nhau hai ngôn ngữ), trong ô `Logo_PH_900x300` để sau thay logo thật |
| Ô của Chọn chế độ | Thẻ "Chương chính" mở; khoá: **Khó, Sự kiện, Thử thách, Đấu trường** |
| Chữ ô khoá | Ổ khoá + **"Sắp có"**; bấm → dòng "Sắp có" có sẵn. Không ghi điều kiện mở |
| Đường đi màn | Ô Màn chơi → Chọn chế độ → Chương chính → Bản đồ. Bản đồ có **Quay lại → Chọn chế độ**. Bấm lại ô Màn chơi luôn về Chọn chế độ. **Về từ trận → mở sẵn Bản đồ** |
| Loại việc của Thông báo | Sự cố đang chờ; Thiếu người ở một vai; Coin AFK chờ thu; Thể lực đầy; Nhân vật đủ mảnh nâng sao. "Phần thưởng chưa nhận" sang đợt Online |
| Số đếm | **Chỉ chấm đỏ**, không hiện số (luồng màn ghi "có số đếm", xem mục 11) |
| Bấm dòng | Sự cố → về Lobby, kéo khung cho "!" ra giữa màn. Thiếu người → về Lobby, mở Nhân viên. Coin AFK → về Lobby, mở popup thu AFK. **Thể lực đầy → Chọn chế độ** (không mở thẳng Bản đồ). Đủ mảnh → Nhân vật, chọn sẵn người đầu tiên đủ mảnh |
| Dạng danh sách Thông báo | **Hộp thả xuống** dưới nút (ngoại lệ của L-033), nền mờ che cả thanh, bấm nền mờ thì đóng |
| Tên người chơi | **Có, sửa được**, tối đa 12 ký tự; rỗng → "Người chơi" / "Player". Trường save mới theo D-005 |
| Avatar | **Lưới nhân vật đang sở hữu**, bấm là đổi và lưu; hình tạm: tròn màu theo độ hiếm + chữ cái đầu tên. Trường save mới theo D-005 |
| Phần online của Hồ sơ | **Chưa dựng gì** (không ID, không tài khoản) |
| Dạng Hồ sơ | **Hộp giữa màn** 1200×800 vẽ trên thanh (user chọn hộp, không phải trang kín màn; ngoại lệ của L-033) |
| Ảnh mô phỏng | Bản 1, 4 màn (mục 10) |

## 2. Hiện trạng (kiểm trong code ngày 2026-11-12, chỉ đọc)

- `GameBootstrap.Awake` tạo session, đăng ký service, `ApplyLanguage()`, rồi `SceneManager.LoadScene(_startScene)`; `_startScene` hiện là `"Restaurant"`. `ProjectSetup` chỉ đổi `_startScene` khi trống hay là scene đã xoá (`IsStaleStartScene`). Build Settings: Boot, Restaurant, Battle. `RestaurantRunner` gắn vào object Boot nên quán chạy ở mọi scene; vắng Lobby thì tính như vắng mặt.
- `HubNav`: `_stagesPanel` là `StageMapPanel_PH_1920x1080` (bản đồ, `StageMapView`), không có nút Quay lại (đợt A bỏ). `HubPending.Set(tab, menu)` / `TryTake(out tab, out menu)`; `BattleView.Back()` và `BattleResultView` gọi `HubNav.OpenOnNextLoad(HubTab.Stages, false)`. `_pages` (các `IHubPage`) quyết định `AnyPageOpen`, `OpenPages` (dùng cho `ResourceBarRule` và `LobbyHud`) và `CloseOpenPages()`.
- `ResourceBar_PH_1120x76`: bố cục Lobby neo trên-trái `(180, 20)`; bố cục khác neo trên-phải cách mép phải **20**. Chỗ `(1790, 20, 110, 100)` đang trống nút (test đợt B giữ).
- `Avatar_PH_140x140` nằm trong `LobbyHud`, thuộc `HubNav._comingSoonButtons` (bấm ra "Sắp có").
- Dữ liệu cho Thông báo đều có sẵn: `RestaurantService.Incidents.Active`, `RestaurantService.Estimate.MissingRole`, `RestaurantService.Afk.PreviewCollect()` và `AccumulatedSeconds`, `CombatService.Stamina.Current/Max`, `StarService.GetNextCost(id)` (0 khi tối đa sao) với `Roster.GetFragments(id)`. Thứ tự nhân vật như màn Nhân vật: `CharacterListRows.Build(roster, stars)`. `CharactersView.Select(int index)` là private.
- `AfkPopup.Show(double absenceSeconds)`; nút Thu AFK của thanh quán thu thẳng, không mở popup. `StaffPanel.Open()` là private, mở từ `StaffButton`.
- Màu độ hiếm: `GachaViewText.RarityColor(rarity)`. `PlayerSave` cuối lớp là `RestaurantBarOpen`; mẫu lưu cờ giao diện: `GameSession.SetRestaurantBarOpen` + `RestaurantBarPref` (Core) đăng ký ở `GameBootstrap`.
- PlayMode: `SmokeLoopTests` có 16 chỗ `SceneManager.LoadScene("Boot")` rồi `WaitForScene("Restaurant")`.
- `PlayerSettings.bundleVersion` = `1.0`.
- Nền test: **889 EditMode + 30 PlayMode** (cuối đợt B).

## 3. Ngoài phạm vi (cố ý không làm)

- Nút góc màn Tiêu đề; hoạt hình vào game; nhạc.
- Chế độ Khó, Sự kiện, Thử thách, Đấu trường (chỉ ô khoá); luật mở khoá chế độ.
- Dòng "Phần thưởng chưa nhận", Hộp quà, Nhiệm vụ; số đếm trên nút; đánh dấu "đã xem"; thông báo đẩy của hệ điều hành.
- Phần online của Hồ sơ (ID, tài khoản, liên kết); lọc tên tục; số liệu chơi trong Hồ sơ.
- Đổi luật quán, sự cố, AFK, thể lực, nâng sao. Chỉ đọc trạng thái có sẵn.
- Luồng lần đầu chơi (đặt tên lúc đầu, hướng dẫn): vẫn là câu chờ user của luồng màn.

## 4. Các phương án đã xét

| Việc | Chọn | Bỏ | Lý do |
|---|---|---|---|
| Tiêu đề | Scene riêng `Title.unity` (user chọn) | Panel trong `Restaurant.unity`; panel trong Boot | User chọn tách bạch. Lợi thêm: Lobby chỉ nạp sau khi chạm nên popup AFK, đồng hồ sự cố, `HubNav` không phải biết Tiêu đề |
| Số liệu Thông báo | View đọc service mỗi 0,5 giây, luật là hàm thuần trên ảnh chụp | Nghe event từng service | Thể lực hồi theo đồng hồ, không có event; 5 service phải thêm event; đọc 0,5 giây rẻ và một chỗ có test |
| Chọn chế độ và Bản đồ | Chọn chế độ là gốc panel ô Màn chơi; Bản đồ thành trang con `IHubPage` | Ô thứ 7 trên thanh; Bản đồ là panel và Chọn chế độ là trang | Thanh 6 ô đã duyệt; "bấm ô khác hay bấm lại ô Màn chơi thì về Chọn chế độ" có sẵn nhờ `CloseOpenPages()` |
| Hộp Thông báo và Hồ sơ trong `HubNav` | Cài `IHubPage` và đánh dấu `IHubOverlay`: `CloseOpenPages()` đóng được, nhưng không tính vào `AnyPageOpen`/`OpenPages` | Không cài `IHubPage`; cài `IHubPage` thường | Cần đóng khi Reset và đổi ngôn ngữ (L-035); nhưng hộp nổi trên Lobby không được làm tắt `LobbyHud` hay đổi thanh tài nguyên bên dưới |
| Tên và avatar | Field trong `PlayerSave`, lưu qua `GameSession` | `PlayerPrefs` | D-005 |

## 5. Thiết kế

Toạ độ theo khung tham chiếu 1920×1080, "từ góc trên-trái" (y tính xuống) trừ chỗ ghi rõ. Kích thước trong tên `_PH_` là cỡ thật. **Object cũ giữ tên; object mới chưa có hình đặt tên `<Tên>_PH_<rộng>x<cao>`**; object nhóm không hình giữ tên thường.

### 5.1 Màn Tiêu đề (`Title.unity`)

**Scene.** `TitleSceneBuilder.Create(path)` (Editor, như các builder khác) dựng `Title.unity`: `Main Camera` (nền màu `TitleBackground`), `EventSystem` (`InputSystemUIInputModule` như các scene khác), một Canvas (`CanvasScaler` 1920×1080, `matchWidthOrHeight = 1`):

| Object | Chỗ, cỡ | Ghi chú |
|---|---|---|
| `TitleBackground` | kéo kín canvas | `Image` màu xanh lá đậm, tắt raycast |
| `Logo_PH_900x300` | tâm x 960, trên y 260, 900×300 | `Image` `UISprite` vàng, tắt raycast; con `GameNameText` (`title.game_name`, chữ 110, `LocalizedText`) |
| `TapHint` | tâm x 960, tâm y 830, rộng 1200 | `Text` `title.tap_to_start` (chữ 64, `LocalizedText`), tắt raycast |
| `VersionText` | neo dưới-phải, cách mép 30 | `Text` chữ 36, view ghi `UiText.Format("title.version", Application.version)` lúc `OnEnable` |
| `TapArea` | kéo kín canvas, **anh em cuối** (trên cùng) | `Button` + `Image` trong suốt có raycast; chạm bất kỳ đâu |

**`TitleView`** (UI, MonoBehaviour, trên Canvas): `[SerializeField] Button _tapArea; Text _tapHint; Text _versionText`. Thiếu tham chiếu → `Debug.LogError` rồi thôi. `OnEnable`: ghi `_versionText`, gắn `_tapArea.onClick` → `Enter()`. `Enter()`: lần đầu đặt `_entering = true`, `SceneManager.LoadScene(RestaurantScene)` (`"Restaurant"`, cùng hằng `BattleView.RestaurantScene`); bấm lần hai không làm gì. `Update`: alpha của `_tapHint` = `TitlePulse.Alpha(Time.unscaledTime)`.

```csharp
public static class TitlePulse   // UI, thuần
{
    public const float PeriodSeconds = 1.6f, MinAlpha = 0.35f;
    // MinAlpha..1 theo |cos(π·t/PeriodSeconds)|; t = 0 → 1.
    public static float Alpha(float seconds);
}
```

**Boot và Build Settings.**
- `ProjectSetup`: dựng `Title.unity` nếu chưa có (menu `Tools/BunDauPeo/Rebuild Title Scene` → `ProjectSetup.RebuildTitleScene`, như `RebuildRestaurantScene`). Build Settings **Boot, Title, Restaurant, Battle** theo thứ tự đó.
- `_startScene` của `GameBootstrap` đặt `"Title"` khi đang trống, là scene đã xoá, **hoặc là `"Restaurant"`** (mặc định cũ của đợt A). Giá trị khác của user giữ nguyên. Sửa `IsStaleStartScene` cho khớp (đổi tên nếu cần) và comment.
- Ngôn ngữ đã áp trong Boot trước khi nạp Title, nên Tiêu đề hiện đúng ngôn ngữ đã lưu (game mới: tiếng Anh).
- Trong lúc đứng ở Tiêu đề, quán tính như vắng mặt (như lúc đánh trận); không đổi luật AFK. Popup thu AFK vẫn chỉ tự hiện khi vào Lobby.
- Các đường nạp `Restaurant` có sẵn (về từ trận, Ngôn ngữ, Reset) giữ nguyên, nên không qua Tiêu đề.

### 5.2 Chọn chế độ và Bản đồ

**Panel.** `ModeSelectPanel_PH_1920x1080` (mới, dựng trong `StageSelectSceneBuilder`, nền như bản đồ, chặn bấm) là `HubNav._stagesPanel`. `StageMapPanel_PH_1920x1080` (giữ tên) thành trang con: anh em ngay sau `ModeSelectPanel` (vẽ trên nó), lúc đầu tắt, vào `HubNav._pages`.

| Object (trong `ModeSelectPanel_PH_1920x1080`) | Chỗ, cỡ | Ghi chú |
|---|---|---|
| `ModeTitle` | trên-trái `(60, 30)`, 800×90 | `hub.tab_stages` (dùng lại), chữ 56 |
| `ModeMain_PH_820x560` | `(100, 200)` | `Button`, nền teal; `ModeMainName` (`stages.mode_main`, chữ 80), `ModeMainSub` (chữ 40, view ghi: `stages.mode_main_next` "Màn tiếp: {0}" theo `StageMapView.NextStageIndex`, hết màn → `stages.mode_main_done`) |
| `ModeLockedHard_PH_440x260` | `(1000, 200)` | `Button` xám; tên `stages.mode_hard`, dòng `hub.coming_soon`, `Lock_PH_40x48` góc trên-trái ô |
| `ModeLockedEvent_PH_440x260` | `(1460, 200)` | như trên, `stages.mode_event` |
| `ModeLockedChallenge_PH_440x260` | `(1000, 500)` | như trên, `stages.mode_challenge` |
| `ModeLockedArena_PH_440x260` | `(1460, 500)` | như trên, `stages.mode_arena` |

Đáy các ô y 760, trên vạch 120 px (thanh từ y 960). Thanh tài nguyên (Thể lực) và nút Thông báo vẽ ở lớp trên (5.4), không nằm trong panel.

**`ModeSelectView`** (UI, trên `ModeSelectPanel`): `[SerializeField] HubNav _hub; StageMapView _map; Button _mainButton; Text _mainSubText; Button[] _lockedButtons` (4 phần tử, thứ tự bảng). `_mainButton` → `_map.Open()`; mỗi ô khoá → `_hub.ShowComingSoon()`. `OnEnable` ghi `_mainSubText` (lấy `StageProgressService`/catalog qua `ServiceLocator` như `StageMapView`; thiếu thì `Debug.LogError`, để chữ trống).

**Bản đồ thành trang.** `StageMapView` cài `IHubPage` và `IResourcePage` (`Slots = Stamina`, để thanh tài nguyên vẫn chỉ Thể lực khi Bản đồ mở). Thêm `public void Open()` (bật root), `IsOpen` = root đang bật, `Close()` = tắt root (đúng việc nút Quay lại). Thêm `BackButton` (giữ khuôn nút Quay lại của các trang: trên-trái, dùng helper `BackButton` có sẵn của `StageSelectSceneBuilder`, nhãn `hub.back`) → `Close()`. Popup màn, chọn đội, màn cốt truyện giữ chỗ vẫn là trang riêng như cũ, vẽ trên Bản đồ.

**Ô chờ mở Bản đồ.**
- `HubPending.Set(HubTab tab, bool menu, bool stageMap = false)`; `TryTake(out tab, out menu, out stageMap)`; bản hai tham số `TryTake(out tab, out menu)` giữ lại (gọi bản ba rồi bỏ cờ) để test cũ khỏi sửa. Lấy một lần rồi xoá cả ba; đặt lại ở `SubsystemRegistration` như cũ. `stageMap` chỉ có nghĩa khi `tab == Stages`; tab khác thì bỏ qua.
- `HubNav.OpenOnNextLoad(HubTab tab, bool menu, bool stageMap = false)`. `HubNav.Awake`: sau `ApplyPanels()`, nếu `stageMap && State.Current == Stages` thì mở Bản đồ (qua tham chiếu `[SerializeField] StageMapView _stageMap`, tuỳ chọn như `_lobbyHud`).
- `BattleView.Back()` và nút Quay lại của `BattleResultView` gọi `OpenOnNextLoad(HubTab.Stages, false, stageMap: true)`.
- Bấm ô Màn chơi (kể cả khi đang ở Bản đồ) → `State.Select` trả `Changed` → `CloseOpenPages()` đóng Bản đồ → Chọn chế độ. Không cần code mới.

### 5.3 Nút Thông báo

**Luật (UI, thuần):**

```csharp
public enum NotifyKind { Incident, StaffMissing, AfkCoins, StaminaFull, StarUp }   // thứ tự hiện = thứ tự enum

public readonly struct NotifySnapshot
{
    public bool HasIncident { get; }
    public StaffRole? MissingRole { get; }
    public long AfkCoins { get; }
    public int Stamina { get; }
    public int StaminaMax { get; }
    public IReadOnlyList<string> StarUpReadyIds { get; }   // đúng thứ tự màn Nhân vật; null = rỗng
    // ctor đủ 6 tham số
}

public static class NotifyRule
{
    // Incident khi HasIncident; StaffMissing khi MissingRole có giá trị; AfkCoins khi AfkCoins > 0;
    // StaminaFull khi StaminaMax > 0 && Stamina >= StaminaMax; StarUp khi StarUpReadyIds có ít nhất 1 id.
    public static IReadOnlyList<NotifyKind> Rows(NotifySnapshot s);
    public static bool HasAny(NotifySnapshot s);
    // Chữ của dòng (UiText, mục 5.6). StaffMissing ghi tên vai; AfkCoins ghi số Coin; StarUp ghi số nhân vật.
    public static string Text(NotifyKind kind, NotifySnapshot s);
    // Id đủ mảnh: theo thứ tự rows, giữ id có cost > 0 (chưa tối đa sao) và fragments >= cost.
    public static IReadOnlyList<string> StarUpReady(IEnumerable<string> orderedIds, Func<string, int> nextCost, Func<string, int> fragments);
}
```

Tên vai trong chữ dòng Thiếu người dùng nhãn vai đang có (`StaffPanelRows`/`RestaurantViewText` có sẵn chữ Bếp, Phục vụ, Thu ngân; dùng lại, không thêm key vai).

**Object** (dựng trong `NavBarBuilder`, cùng chỗ dựng `ResourceBar`):
- `NotifyBtn_PH_110x100` (`Button`, `UISprite` cam nhạt, chữ `hub.notify`): neo trên-phải, cách mép phải 20, mép trên 20 (tức `(1790, 20)`). Con `NotifyDot_PH_28x28` (`Knob` đỏ) ở góc trên-phải nút, lúc đầu tắt.
- **Lớp vẽ:** nút nằm **sau `MenuPanel_PH_1920x1080`, trước `NavBar_PH_1920x120`** (để Menu cũng thấy nút). Thứ tự canvas: … trang và panel < `ResourceBar_PH_1120x76` < `MenuPanel` < `NotifyBtn_PH_110x100` < `NavBar` < hộp `OverBar`.
- **Thanh tài nguyên ngoài Lobby** dời trái: neo trên-phải, cách mép phải **140** (= 20 + 110 + 10), thay 20. Bố cục Lobby không đổi (đáy-phải thanh ở x 1300, cách nút 490).
- **Ẩn theo thanh:** `HubNav.SetBarHidden` (Gacha cutscene, lộ, tổng) ẩn cả nút (`HubNav` giữ `[SerializeField] GameObject _notifyButton`, tuỳ chọn).
- Không có nút ở Tiêu đề, Trận.

**Hộp** `NotifyPopup` (root kéo kín màn, qua `NavBarBuilder.OverBar`, lúc đầu tắt):
- `NotifyDim` kéo kín màn: `Image` đen alpha 0,45, `Button` → đóng hộp.
- `NotifyBox_PH_640x660`: neo trên-phải, cách mép phải 20, mép trên 180 (ngay dưới nút); nền xám đậm viền sáng như các hộp Gacha. Tiêu đề `notify.title` (chữ 42). 5 hàng `NotifyRow_PH_600x100` (`Button`, chữ 34 căn trái, ký hiệu `›` ở phải), trên y 90 / 200 / 310 / 420 / 530 trong hộp. `NotifyEmpty` (`notify.empty`) khi không có dòng nào.
- `NotifyPopup` (UI) cài `IHubPage` + `IHubOverlay` (5.5): `Open()` dựng hàng từ ảnh chụp mới nhất (hàng thừa tắt), `Close()` tắt root. Mở hộp thì `Incidents.SetSpawnBlocked(this, true)`, đóng thì `false` (như trang của quán; L-015 cờ nhiều chủ).

**`NotifyView`** (UI, trên `NotifyBtn_PH_110x100`): `[SerializeField] HubNav _hub; Button _button; GameObject _dot; NotifyPopup _popup`, và các đích (mục dưới). `OnEnable` lấy `RestaurantService`, `CombatService` (thể lực qua `CombatService.Stamina`, như `ResourceBarView`), `StarService`, `Roster` từ `ServiceLocator` (thiếu thì `Debug.LogError`, chấm tắt, hộp mở ra trống). Mỗi 0,5 giây (giờ không theo `timeScale`) và ngay khi bật: chụp `NotifySnapshot`, đặt `_dot` = `NotifyRule.HasAny`. Bấm nút → `_popup.Open(snapshot)`.

**Bấm dòng** (`NotifyView.Go(NotifyKind kind)`; mọi đường đóng hộp trước):

| Dòng | Làm |
|---|---|
| Incident | `_hub.Select(Restaurant)` (đóng Menu và mọi trang, kể cả hộp); `_worldScroll.normalizedPosition = WorldFocus.Center(world, viewport, bubble)` để `IncidentBubble` ra giữa màn |
| StaffMissing | `_hub.Select(Restaurant)`; `_staffPanel.Open()` (đổi `Open` thành `public`) |
| AfkCoins | `_hub.Select(Restaurant)`; `_afkPopup.Show(afk.AccumulatedSeconds)` |
| StaminaFull | `_hub.Select(Stages)` → Chọn chế độ |
| StarUp | `_hub.Select(Characters)`; `_characters.SelectById(snapshot.StarUpReadyIds[0])` (thêm `public void SelectById(string id)` vào `CharactersView`; id không có trong danh sách thì không làm gì) |

Nếu lúc bấm dòng việc đã hết (ví dụ sự cố vừa tự huỷ), vẫn đi tới đích, không báo gì.

```csharp
public static class WorldFocus   // UI, thuần
{
    // normalizedPosition của ScrollRect để điểm target (toạ độ local trong content, pivot giữa) nằm giữa viewport; kẹp [0, 1].
    // Content không rộng hơn viewport theo trục nào thì trục đó trả 0,5.
    public static Vector2 Center(Vector2 contentSize, Vector2 viewportSize, Vector2 targetInContent);
}
```

`NotifyView` cần `_worldScroll` (`WorldScroll`), `_incidentBubble`, `_staffPanel`, `_afkPopup`, `_characters`: builder gán; thiếu thì dòng đó chỉ đổi ô.

### 5.4 Hồ sơ

**Lobby.** `Avatar_PH_140x140` bỏ khỏi `_comingSoonButtons`; bấm → `ProfilePopup.Open()`. Avatar có `AvatarView` (5.4 dưới): tròn màu theo nhân vật đã chọn, con `AvatarInitial` (`Text` chữ 64); chưa chọn → xám như đợt B, không chữ.

**Hộp** `ProfilePopup` (root kéo kín màn qua `OverBar`, lúc đầu tắt): nền đen alpha 0,5 chặn bấm (bấm nền **không** đóng, để không mất chữ đang gõ); `ProfileBox_PH_1200x800` giữa màn (box nằm `(360, 140)`–`(1560, 940)`). Trong hộp (toạ độ trong hộp, từ trên-trái):

| Object | Chỗ, cỡ | Ghi chú |
|---|---|---|
| `ProfileTitle` | giữa trên, y 30, 800×80 | `profile.title`, chữ 48 |
| `ProfileAvatar_PH_240x240` | `(80, 140)` | `Image` `Knob` + `AvatarInitial` chữ 110; `AvatarView` |
| `NameInput_PH_400x90` | `(40, 450)` | uGUI `InputField`: `characterLimit = 12`, `lineType = SingleLine`, `contentType = Standard`; `Placeholder` hiện `profile.default_name`; chữ 38 |
| `NameHint` | `(40, 550)`, 400×40 | `profile.name_hint` ("Tối đa 12 ký tự"), chữ 26 |
| `ProfileStatus` | `(40, 600)`, 400×80 | dòng báo lưu lỗi (`profile.not_saved`), lúc đầu trống |
| `AvatarLabel` | `(520, 120)`, 600×60 | `profile.change_avatar`, chữ 38 |
| `AvatarGrid` | `(520, 180)`, 640×460 | `ScrollRect` dọc (`Clamped`, `scrollSensitivity` như mặc định), viewport có `RectMask2D`; content xếp 5 cột, ô cách 130 px |
| `AvatarCell_PH_120x120` | template trong `AvatarGrid`, tắt | `Button` + `Image` `Knob` + `AvatarInitial`; con `AvatarSelected_PH_136x136` (`Knob` viền vàng, vẽ dưới ô) |
| `CloseButton` | giữa đáy, đáy cách 30, 300×100 | `common.close` |

**Hành vi `ProfilePopup`** (UI, cài `IHubPage` + `IHubOverlay`):
- `Open()`: bật root, `SetSpawnBlocked(this, true)`, đổ `InputField.text` = tên đã lưu (rỗng → để rỗng, placeholder hiện tên mặc định), dựng lưới: mỗi nhân vật trong `CharacterListRows.Build(roster, stars)` một ô (bật bản sao từ template; mở lại thì dùng lại ô đã có, thừa thì tắt), màu `GachaViewText.RarityColor`, chữ `AvatarRule.Initial(nhân vật)`, viền vàng ở ô đang chọn.
- `InputField.onEndEdit` → `PlayerProfile.SetName(PlayerNameRule.Normalize(text))`; ghi lại `InputField.text` = giá trị đã chuẩn hoá. Lưu lỗi → `ProfileStatus` = `profile.not_saved` (giá trị mới vẫn giữ trong bộ nhớ, như cờ thanh quán).
- Bấm ô → `PlayerProfile.SetAvatar(id)`; viền chuyển ô; lưu lỗi như trên.
- `CloseButton` / `Close()`: kết thúc gõ dở (gọi `onEndEdit` nếu đang focus, để chữ không mất), tắt root, `SetSpawnBlocked(this, false)`.
- Avatar ở Lobby cập nhật qua sự kiện `PlayerProfile.Changed`.

**Luật (UI, thuần):**

```csharp
public static class PlayerNameRule
{
    public const int MaxLength = 12;
    // null → ""; bỏ ký tự điều khiển (xuống dòng, tab); trim hai đầu; cắt còn MaxLength ký tự (theo text element, không cắt đôi ký tự ghép).
    public static string Normalize(string raw);
    // "" → UiText "profile.default_name"; còn lại trả nguyên.
    public static string Display(string saved);
}

public static class AvatarRule
{
    // id rỗng, null, không có trong catalog hoặc chưa sở hữu → null.
    public static CharacterDefinition Resolve(string savedId, Func<string, bool> isOwned, Func<string, CharacterDefinition> lookup);
    // Chữ cái đầu của DisplayName (text element đầu, viết hoa theo ngôn ngữ bất biến); null → "".
    public static string Initial(CharacterDefinition character);
}
```

**`AvatarView`** (UI): `[SerializeField] Image _circle; Text _initial`; `Show(CharacterDefinition c)`: `null` → xám `(0.55, 0.57, 0.62)`, chữ trống; khác → `RarityColor`, `Initial`. Dùng cho avatar Lobby, avatar lớn trong hộp và mỗi ô.

**Save và Core** (theo [D-005](../../decisions/decisions.md)):
- `PlayerSave` thêm ở cuối lớp: `public string PlayerName = "";` và `public string AvatarCharacterId = "";`. Không đổi `Version`. Save cũ thiếu field → `JsonUtility` để giá trị khởi tạo `""`; đọc ra `null` (JSON ghi `null`) thì code đọc coi như `""`.
- Reset (`ResetToDefaults`) ghi đè `new PlayerSave()` nên cả hai về `""`: tên mặc định, avatar xám.
- `GameSession.SetPlayerName(string name) → bool` và `SetAvatar(string id) → bool`: ghi field, `Persist()`, trả kết quả; lưu lỗi vẫn giữ giá trị mới trong bộ nhớ (như `SetRestaurantBarOpen`).
- `PlayerProfile` (Core, như `RestaurantBarPref`): `PlayerProfile(Func<string> getName, Func<string> getAvatar, Func<string, bool> setName, Func<string, bool> setAvatar)` (null → `ArgumentNullException`); `string Name`, `string AvatarId`, `bool SetName(string)`, `bool SetAvatar(string)`, `event Action Changed` (phát sau mỗi lần set, kể cả lưu lỗi). `GameBootstrap` đăng ký cạnh `RestaurantBarPref`.
- Trạng thái chạy ngoài save: không có (Reset nạp lại `Restaurant`, view đọc lại khi bật).

### 5.5 `HubNav` và hộp nổi

- `public interface IHubOverlay : IHubPage { }` (UI, đánh dấu). `HubNav.AnyPageOpen` và `OpenPages` **bỏ qua** trang cài `IHubOverlay`; `CloseOpenPages()` vẫn đóng cả chúng (L-035: Reset, đổi ngôn ngữ, bấm ô khác).
- Tách luật lọc thành hàm tĩnh để test thuần (như `HubNav.ClosePages`): `public static IEnumerable<IHubPage> LayoutPages(IEnumerable<IHubPage> pages)` = trang đang mở và không phải `IHubOverlay`.
- `_pages` thêm `StageMapView`, `NotifyPopup`, `ProfilePopup`.
- `HubNav` thêm tham chiếu tuỳ chọn `_notifyButton` (ẩn theo thanh), `_stageMap` (ô chờ), `_profileButton` (nút avatar → `ProfilePopup.Open()`; builder gán, `_comingSoonButtons` còn Shop).

### 5.6 Chữ mới (`UiText`, có bản Anh)

Bản Anh là nháp, user duyệt ở playtest.

| Key | File | Việt | Anh |
|---|---|---|---|
| `title.game_name` | `UiText.Title.cs` (mới) | Bun Dau Peo | Bun Dau Peo |
| `title.tap_to_start` | `UiText.Title.cs` | Chạm để bắt đầu | Touch to start |
| `title.version` | `UiText.Title.cs` | Ver {0} | Ver {0} |
| `stages.mode_main` | `UiText.Combat.cs` (cùng file chữ bản đồ) | Chương chính | Main story |
| `stages.mode_main_next` | như trên | Màn tiếp: {0} | Next: {0} |
| `stages.mode_main_done` | như trên | Đã xong mọi màn | All stages cleared |
| `stages.mode_hard` | như trên | Khó | Hard |
| `stages.mode_event` | như trên | Sự kiện | Event |
| `stages.mode_challenge` | như trên | Thử thách | Challenge |
| `stages.mode_arena` | như trên | Đấu trường | Arena |
| `hub.notify` | `UiText.Hub.cs` | Báo | Alerts |
| `notify.title` | `UiText.Hub.cs` | Việc cần làm | To do |
| `notify.empty` | `UiText.Hub.cs` | Không có việc cần làm | Nothing to do |
| `notify.incident` | `UiText.Hub.cs` | Quán có sự cố đang chờ | An incident needs you |
| `notify.staff_missing` | `UiText.Hub.cs` | Thiếu {0}, quán đứng yên | No {0}, the shop is stopped |
| `notify.afk` | `UiText.Hub.cs` | Có {0} Coin AFK chờ thu | {0} AFK Coin to collect |
| `notify.stamina_full` | `UiText.Hub.cs` | Thể lực đã đầy | Stamina is full |
| `notify.star_up` | `UiText.Hub.cs` | {0} nhân vật đủ mảnh nâng sao | {0} characters can star up |
| `profile.title` | `UiText.Hub.cs` | Hồ sơ | Profile |
| `profile.default_name` | `UiText.Hub.cs` | Người chơi | Player |
| `profile.name_hint` | `UiText.Hub.cs` | Tối đa 12 ký tự | Up to 12 characters |
| `profile.change_avatar` | `UiText.Hub.cs` | Đổi avatar | Change avatar |
| `profile.not_saved` | `UiText.Hub.cs` | Chưa lưu được, sẽ thử lại | Not saved yet, will retry |

Dùng lại: `hub.tab_stages`, `hub.coming_soon`, `hub.back`, `common.close`, nhãn vai có sẵn. "Bun Dau Peo" là tên riêng nhưng vẫn vào `UiText` (hai bản giống nhau) để `HardcodedTextTests` không cần ngoại lệ. Ký hiệu `›` viết cứng như `<`, `>` của thanh quán; nếu `HardcodedTextTests` bắt thì đưa vào `UiText`. Key `stages.*` đặt trong `UiText.Combat.cs` cạnh chữ bản đồ.

## 6. Luồng chạy

```
Mở app: Boot (ApplyLanguage) → LoadScene(Title) → chạm TapArea → LoadScene(Restaurant) → HubNav.Awake (không ô chờ) → Lobby
Ô Màn chơi: HubNav.Select(Stages) → ModeSelectPanel → Chương chính → StageMapView.Open() → Quay lại → Close() → Chọn chế độ
Ô khoá: ModeSelectView → HubNav.ShowComingSoon()
Về từ trận: OpenOnNextLoad(Stages, false, stageMap: true) → LoadScene(Restaurant) → Awake mở Bản đồ
Thông báo: NotifyView (0,5 giây) → NotifySnapshot → NotifyRule.HasAny → chấm; bấm → NotifyPopup.Open → bấm dòng → NotifyView.Go(kind)
Hồ sơ: Avatar → ProfilePopup.Open → sửa tên (onEndEdit) / bấm ô → PlayerProfile.Set* → GameSession.Persist → Changed → AvatarView ở Lobby
Reset / đổi ngôn ngữ: CloseOpenPages() đóng cả NotifyPopup, ProfilePopup, Bản đồ (L-035) → nạp lại Restaurant (không qua Tiêu đề)
```

## 7. Test

Nền **889 EditMode + 30 PlayMode**. Dự kiến **909 EditMode + 34 PlayMode** (kế hoạch chốt từng task). Không `[TestCase]` (L-012). Test bố cục xét cả nút đang tắt (L-025, L-027). Test gọi `ProjectSetup.Run()`/`*SceneBuilder.Create` nạp lại asset sau lệnh gọi (L-032); scene sinh bằng code có test đọc lại từ file (L-002). Test đổi `GameLanguage.Current` đặt lại `Vi` ở `[TearDown]`. **PlayMode bấm và chạm qua raycast thật** (`UiDriver.Click`), không `ExecuteEvents` thẳng lên nút (L-036).

**EditMode:**

| Test | Bắt lỗi gì | Số |
|---|---|---|
| `NotifyRuleTests` (mới) | Đủ 5 loại đúng thứ tự enum dù ảnh chụp đặt theo thứ tự khác; từng điều kiện biên: `AfkCoins` 0 không hiện, `Stamina == Max` hiện, `Max == 0` không hiện, `StarUpReadyIds` null/rỗng không hiện; `HasAny` sai khi rỗng; `Text` ghi số Coin, tên vai, số nhân vật ở Vi và En; `StarUpReady` bỏ người tối đa sao (cost 0) và người thiếu mảnh, giữ thứ tự | +4 |
| `WorldFocusTests` (mới) | Điểm giữa content → (0,5; 0,5); điểm sát mép trái → 0, quá mép → kẹp; content hẹp hơn viewport → 0,5 | +1 |
| `TitlePulseTests` (mới) | `Alpha(0) == 1`; lặp `t` 0..4: `MinAlpha ≤ Alpha ≤ 1`; nửa chu kỳ → `MinAlpha` | +1 |
| `PlayerNameRuleTests` (mới) | `null`→`""`; trim; xuống dòng và tab bị bỏ; 13 ký tự → 12; chữ có dấu tiếng Việt dạng ghép không bị cắt đôi; `Display("")` = "Người chơi"/"Player", `Display("An")` = "An" | +1 |
| `AvatarRuleTests` (mới) | id rỗng, lạ, chưa sở hữu → null; hợp lệ → đúng nhân vật; `Initial` chữ đầu viết hoa, `null` → `""` | +1 |
| `HubNavStateTests` (thêm) | `HubPending.Set(Stages, false, stageMap: true)` → `TryTake` ba tham số trả `true` một lần rồi `false`; bản hai tham số xoá luôn cờ | +1 |
| `HubNavTests` (thêm) | `LayoutPages` bỏ trang `IHubOverlay` đang mở, giữ trang thường đang mở, bỏ trang đóng; `ClosePages` vẫn đóng overlay (trang giả thuần C#, L-034) | +1 |
| `GameSessionTests` (thêm) | Game mới `PlayerName == ""`, `AvatarCharacterId == ""`; set → session mới trên cùng file đọc lại đúng; Reset → cả hai `""`, file nạp lại cũng `""`; save cũ không có hai field (JSON viết tay) nạp được và ra `""` | +2 |
| `GameSessionTests` (thêm) | `SetPlayerName`/`SetAvatar` khi lưu lỗi: trả `false`, bộ nhớ giữ giá trị mới, không ném | +1 |
| `PlayerProfileTests` (mới) | null tham số → `ArgumentNullException`; `Set*` gọi đúng hàm, trả kết quả, `Changed` phát cả khi lưu lỗi | +1 |
| `TitleSceneTests` (mới, đọc `Title.unity`) | Đủ object đúng tên, cỡ, chỗ; `TapArea` là anh em cuối, kéo kín, có raycast; `TitleView` đủ tham chiếu; nhãn có `LocalizedText` đúng key | +1 |
| `ProjectSetupTests` (thêm) | Build Settings đúng Boot, Title, Restaurant, Battle; `_startScene` thành `"Title"` từ `""`, `"Gacha"`, `"Restaurant"`, giữ `"Battle"`; chạy lại không lỗi (L-032) | +1 |
| `LobbySceneTests` (thêm) | `NotifyBtn_PH_110x100` đúng chỗ, cỡ, có `NotifyDot_PH_28x28`, không nằm trong `LobbyHud`, thứ tự Menu < nút < NavBar; **không `Button` nào khác (kể cả đang tắt, ở mọi panel và trang) giao rect nút**; `NotifyView` đủ tham chiếu | +1 |
| `LobbySceneTests` (thêm) | `NotifyPopup` và `ProfilePopup`: root kéo kín qua `OverBar`, nền mờ, hộp đúng tên và cỡ, cài `IHubOverlay`, có trong `HubNav._pages`; `NameInput_PH_400x90` là `InputField` `characterLimit == 12`; avatar không còn trong `_comingSoonButtons`, `_profileButton` trỏ nó | +1 |
| `LobbySceneTests` (sửa) | Thanh tài nguyên bố cục ngoài Lobby cách mép phải 140 (ca đợt B đổi số) | 0 |
| `StageSelectSceneTests` (thêm) | `ModeSelectPanel_PH_1920x1080` là `HubNav._stagesPanel`; thẻ và 4 ô khoá đúng tên, cỡ, chỗ, ô khoá có `Lock_PH_40x48`; mọi nút trên vạch; `ModeSelectView` đủ tham chiếu | +1 |
| `StageSelectSceneTests` (thêm) | `StageMapPanel_PH_1920x1080` lúc đầu tắt, vẽ sau `ModeSelectPanel`, có `BackButton` trên-trái trên vạch; `StageMapView` cài `IHubPage` và `IResourcePage` (`Stamina`), có trong `_pages` | +1 |
| `HardcodedTextTests`, `UiTextTests` (có sẵn) | Tự quét view và builder mới, key mới có bản Anh | 0 |

**PlayMode** (save tạm như `SmokeLoopTests`): mọi ca đang `LoadScene("Boot")` → `WaitForScene("Restaurant")` đổi sang một helper `BootToLobby()`: chờ `Title`, `UiDriver.Click("TapArea")`, chờ `Restaurant` (0 ca mới).

| Test | Kiểm gì |
|---|---|
| `Title_TapEntersLobby` (mới) | Boot → scene `Title`; chữ `TapHint` theo ngôn ngữ đã lưu (game mới: "Touch to start"); `VersionText` có `Application.version`; bấm `TapArea` qua raycast → `Restaurant`; về từ trận và đổi ngôn ngữ nạp thẳng `Restaurant` (scene `Title` không nạp lại) |
| `Hub_ModeSelect_MapBackAndBattleReturn` (mới) | Ô Màn chơi → Chọn chế độ (Bản đồ tắt); 4 ô khoá → `ComingSoonToast` bật; Chương chính → Bản đồ, thanh tài nguyên chỉ Thể lực; Quay lại → Chọn chế độ; mở Bản đồ → bấm ô Quán → bấm ô Màn chơi → Chọn chế độ; đặt ô chờ như đường về từ trận → nạp lại → Bản đồ mở |
| `Hub_Notify_DotAndRowsGoToRightPlace` (mới) | Game mới đủ người: chấm tắt, hộp ghi "Nothing to do". Ép từng việc (sự cố qua bộ đếm như ca có sẵn, gỡ Thu ngân, tua AFK, `Refill` thể lực, thêm mảnh): chấm bật, hộp đủ 5 dòng đúng thứ tự; bấm từng dòng (mở lại hộp mỗi lần) → đúng đích ở mục 5.3 (sự cố: `IncidentBubble` nằm trong khung màn; Nhân viên mở; popup AFK mở; Chọn chế độ; Nhân vật chọn đúng id). Bấm nền mờ → hộp đóng. Mở Menu → nút vẫn thấy và bấm được. Gacha quay → nút ẩn |
| `Hub_Profile_NameAvatarPersistAndReset` (mới) | Bấm avatar → hộp Hồ sơ (`LobbyHud` vẫn bật); placeholder "Player"; nhập " An  " rồi kết thúc gõ → lưu "An"; bấm ô nhân vật thứ hai → viền chuyển, avatar Lobby đổi màu và chữ; Đóng; nạp lại save tạm → giữ; Menu → Reset → Xoá hết → tên mặc định, avatar xám; Reset khi hộp đang mở (đường DEV) → hộp đóng (L-035) |
| `Hub_Screenshots_VietnameseAndEnglish` (sửa) | Thêm ảnh Tiêu đề, Chọn chế độ, Bản đồ có Quay lại, hộp Thông báo đủ 5 dòng, hộp Thông báo trống, Hồ sơ, Lobby có avatar đã đổi |

**Kiểm bằng mắt (dừng playtest cuối đợt C):** ảnh `D:/bdp_BunDauPeo/shots/hubc-<màn>.png` và `-en.png`. User so với mục 10. Playtest: tắt hẳn game → mở (Tiêu đề, tiếng Anh) → chạm → Lobby → bấm avatar, đặt tên, đổi avatar, tắt mở lại game → Thông báo: thử từng dòng → ô Màn chơi → ô khoá → Chương chính → đánh một màn → Quay lại (mở sẵn Bản đồ) → Quay lại (Chọn chế độ) → Menu đổi Tiếng Việt (không qua Tiêu đề) → Reset (tên và avatar về mặc định). **Trên điện thoại thật** (nếu đã nối): bàn phím khi sửa tên, chạm nút Thông báo.

## 8. Docs phải sửa (khi xong, trong kế hoạch)

- [roadmap.md](../../roadmap.md): dòng "Đang làm" (📐 đặt khi viết spec, 🔨 cập nhật theo phiên); khi xong chuyển dòng đợt C sang "Đã xong".
- [project/structure.md](../../project/structure.md): `Title.unity`, `TitleSceneBuilder`, `TitleView`, `TitlePulse`, `ModeSelectView`, `NotifyRule`/`NotifyKind`/`NotifySnapshot`, `NotifyView`, `NotifyPopup`, `WorldFocus`, `ProfilePopup`, `AvatarView`, `AvatarRule`, `PlayerNameRule`, `IHubOverlay`, `PlayerProfile`, `PlayerSave.PlayerName`/`AvatarCharacterId`, Build Settings, số test.
- [unity-workflow.md](../../guides/unity-workflow.md#ui-và-hình-tạm): một câu: nút Thông báo ở `(1790, 20)` vẽ trên Menu, không nút nào được đè (có test); hộp nổi trên hub cài `IHubOverlay`; ngoại lệ L-033 của đợt C (hộp Thông báo, hộp Hồ sơ, user chốt 2026-11-12).
- [game-design.md](../../design/game-design.md): không đổi luật.
- [screen-flow.md](../../design/screen-flow.md) và [ideas.md](../../design/ideas.md) là file của 🎨: 📐/🔨 không sửa; 📐 nhắn 🎨 các điểm ở mục 11.

## 9. Rủi ro

| Rủi ro | Cách giữ |
|---|---|
| 16 ca PlayMode đang chờ `Restaurant` ngay sau Boot sẽ treo tới timeout vì có Tiêu đề | Helper `BootToLobby()` thay một lượt ở Task đầu có `Title.unity`; chạy cả bộ PlayMode trong task đó |
| `_startScene` của máy user còn `"Restaurant"` nên Tiêu đề không hiện | `ProjectSetup` coi `"Restaurant"` là mặc định cũ (5.1), có test |
| Nút ở góc trên-phải của trang hay panel nào đó đè nút Thông báo | Test EditMode quét mọi `Button` kể cả đang tắt; thanh tài nguyên dời 140 |
| Hộp nổi làm tắt `LobbyHud` hay đổi thanh tài nguyên bên dưới | `IHubOverlay` + `LayoutPages`, test thuần và PlayMode ("`LobbyHud` vẫn bật") |
| Reset hay đổi ngôn ngữ khi hộp đang mở để lại hộp hay ghi tên cũ vào save mới (L-035) | Hai hộp là `IHubPage`, `CloseOpenPages()` đóng trước Reset; `Close()` của Hồ sơ chỉ ghi qua `PlayerProfile` (save hiện tại) và chạy trước khi Reset ghi đè |
| Bàn phím Android che ô tên hoặc `onEndEdit` không chạy khi bấm Đóng | `Close()` tự kết thúc gõ dở; thử trên máy thật ở playtest |
| Kéo khung tới sự cố tính sai khi màn khác 16:9 | `WorldFocus` thuần có test kẹp; PlayMode kiểm bong bóng nằm trong khung |
| Đọc 5 service mỗi 0,5 giây tốn | Chỉ đọc số và một vòng nhân vật (≤ vài chục); không cấp phát mỗi khung ngoài danh sách id |
| Bản Anh dài ("No Cashier, the shop is stopped") tràn hàng 600 px | Ảnh tiếng Anh ở lần dừng playtest; rút gọn trong `UiText` |

## 10. Ảnh mô phỏng đã duyệt

Vẽ và duyệt trong kênh 📐 UI hub đợt C ngày 2026-11-12 (ảnh nằm ngoài repo, chỉ ở hội thoại). Bản chép bố cục:

**Tiêu đề bản 1:**
```
                     [Logo_PH_900x300: "Bun Dau Peo"]

                          Chạm để bắt đầu   (nhấp nháy chậm)
                                                              Ver 1.0
(TapArea kín màn, không nút góc)
```

**Chọn chế độ bản 1:**
```
Màn chơi                                         [Thể lực 80/100 +] [Báo●]
┌──────────────────────────┐  ┌──────🔒─────┐ ┌──────🔒─────┐
│      Chương chính        │  │ Khó  Sắp có │ │Sự kiện Sắp có│
│  Chương 1 · màn tiếp 1-4 │  ├──────🔒─────┤ ├──────🔒─────┤
│                          │  │Thử thách ...│ │Đấu trường ..│
└──────────────────────────┘  └─────────────┘ └─────────────┘
[ Quán | Nhân vật | Cốt truyện 🔒 | Màn chơi | Gacha | Menu ]
```

**Hộp Thông báo bản 1** (trên Lobby, nền mờ che cả thanh trừ nút):
```
[Avatar][thanh tài nguyên 4 ô]                               [Báo●]
                                              ┌ Việc cần làm ─────────┐
                                              │ Quán có sự cố đang chờ › │
                                              │ Thiếu Thu ngân, ...    › │
                                              │ Có 320 Coin AFK chờ thu› │
                                              │ Thể lực đã đầy         › │
                                              │ 2 nhân vật đủ mảnh ... › │
                                              └────────────────────────┘
```

**Hồ sơ bản 1** (hộp giữa màn, nền mờ):
```
┌──────────────────────── Hồ sơ ────────────────────────┐
│   ( A )  avatar lớn          Đổi avatar                 │
│                              (●)(●)(●)(●)(●)            │
│  [Người chơi        ]        (●)(●)(●)(●)(●)  cuộn      │
│  Tối đa 12 ký tự             (●)(●)                     │
│                    [ Đóng ]                             │
└─────────────────────────────────────────────────────────┘
```

## 11. Khác với luồng màn (báo 🎨)

📐 không sửa `screen-flow.md`/`ideas.md`; nhắn 🎨 các điểm này:
1. Nút Thông báo **chỉ có chấm**, không hiện số (luồng màn: "có số đếm").
2. Dòng **Thể lực đầy** mở **Chọn chế độ** (luồng màn: "mở bản đồ màn").
3. Dòng **Coin AFK** về Lobby rồi mở popup AFK; dòng **Thiếu người** mở trang Nhân viên (luồng màn ghi "bảng Nhân viên"); dòng **Sự cố** kéo khung quán cho "!" ra giữa màn ("chỉ vào trạm" = kéo khung, không có mũi tên).
4. **Phần thưởng chưa nhận** chưa làm, sang đợt Online.
5. Danh sách Thông báo là **hộp thả xuống**; **Hồ sơ là hộp giữa màn** (ngoại lệ L-033, user chốt 2026-11-12).
6. Hồ sơ đợt C: **tên sửa được tối đa 12 ký tự** và **avatar bằng nhân vật đã sở hữu**; ID, tài khoản chờ Online (không chừa chỗ).
7. Bản đồ có lại **nút Quay lại** (về Chọn chế độ); về từ trận vẫn mở sẵn Bản đồ.
8. Chọn chế độ có 4 ô khoá **Khó, Sự kiện, Thử thách, Đấu trường**, ghi "Sắp có".
9. Tiêu đề là **scene riêng**, hiện mỗi lần mở app, **không nút góc**, tên game "Bun Dau Peo".
10. Câu chờ user "luồng lần đầu chơi (đặt tên…)": đợt C cho đặt tên trong Hồ sơ, chưa có bước đặt tên lúc đầu.
