# Spec: UI hub, đợt A (khung)

> Ngày: 2026-11-09 · Trạng thái: **đã duyệt** (user, 2026-11-09, thiết kế, ảnh mô phỏng và bản viết) · Phạm vi: Lobby = màn quán; Nhân vật, Gacha, Bản đồ màn thành panel kín màn trong `Restaurant.unity`; thanh dưới 6 ô; popup Menu (ngôn ngữ, Reset dữ liệu, DEV); 8 popup con thành trang kín màn; trận xong về Lobby với bản đồ mở sẵn; luật sự cố theo panel; sửa panel DEV.
> Nguồn: [luồng màn](../../design/screen-flow.md) (user duyệt 2026-11-09, **bản chính** của cấu trúc và bố cục Lobby), [roadmap](../../roadmap.md) (dòng "UI hub"), [game-design, Quán](../../design/game-design.md#quán) (sự cố khi mở popup, chuyên cần), [D-005](../../decisions/decisions.md) (Reset), [D-012](../../decisions/decisions.md) (chữ qua `UiText`), [unity-workflow, UI và hình tạm](../../guides/unity-workflow.md#ui-và-hình-tạm) (ảnh mô phỏng trước, tên `_PH_`). Kế hoạch: [plans/2026-11-09-ui-hub.md](../plans/2026-11-09-ui-hub.md).

## 1. Mục tiêu

Người chơi bản phát hành đi được hết game mà không cần panel DEV. Hiện bản phát hành không có cách chuyển màn nào: mọi nút chuyển màn nằm trong `DevOverlay` (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`). Hub cũng là chỗ đợt B, C và quảng cáo thưởng, shop Gem gắn vào sau.

Thành công khi:
1. Mở game vào Lobby (màn quán, như hiện nay). Thanh dưới có 6 ô; bấm Nhân vật, Màn chơi, Gacha thì mở panel kín màn tương ứng, quán vẫn chạy bên dưới; bấm Quán thì về Lobby. Cốt truyện báo "Sắp có".
2. Menu → Ngôn ngữ đổi Việt ↔ Anh, còn sau khi tắt mở game; màn vẫn ở đúng ô và Menu vẫn mở.
3. Menu → Reset dữ liệu luôn hỏi xác nhận; Huỷ thì không đổi gì; Xoá hết thì xoá **toàn bộ** save ([D-005](../../decisions/decisions.md)) và về Lobby như game mới.
4. Đang trong trận thì không có thanh dưới, không mở được Menu, nên không đổi màn hay đổi ngôn ngữ giữa trận được. Bỏ cuộc hoặc đánh xong, bấm Quay lại thì về Lobby với bản đồ mở sẵn.
5. Panel Gacha có banner kín màn. Bốn panel của thanh và 8 trang con phủ kín 1920×1080; hộp xác nhận và hộp phần thưởng giữ cỡ cũ nhưng nền mờ kín màn, che cả thanh.
6. Mở panel thì sự cố vẫn sinh nhưng đồng hồ 8/20 giây dừng; chuyên cần tính cả lúc mở panel.
7. Mọi chữ mới đi qua `UiText`, có bản tiếng Anh; `HardcodedTextTests` xanh.
8. Bản phát hành không còn dấu vết DEV ở hub: dòng DEV trong Menu chỉ có ở Editor và Development Build.

### Quyết định đã chốt với user (2026-11-09)

| Chủ đề | Quyết định |
|---|---|
| Chia đợt | Ba đợt. **A (spec này): khung.** B: bố cục Lobby theo ảnh bản 2 của luồng màn (avatar, thanh tài nguyên chung, thanh chất lượng, banner, hàng nút tròn có Shop khoá, thanh quán thu gọn, kéo camera). C: Tiêu đề, Chọn chế độ, nút Thông báo, Hồ sơ. Mỗi đợt có spec, kế hoạch, điểm dừng playtest riêng |
| Popup | Là **panel bật/tắt trong `Restaurant.unity`**, không nạp scene chồng (user: "chỉ cần pop-up là các panel"). `Characters.unity`, `Gacha.unity`, `StageSelect.unity` bị xoá |
| Thanh dưới | 6 ô: Quán · Nhân vật · Cốt truyện (khoá) · Màn chơi · Gacha · Menu ([luồng màn](../../design/screen-flow.md#bố-cục-lobby-user-duyệt-ảnh-mô-phỏng-bản-2-2026-11-09)) |
| Menu | Panel kín màn như các panel khác (không phải scene riêng): Ngôn ngữ, Reset dữ liệu, DEV (chỉ bản dev), Đóng |
| Kín màn | Panel Gacha có banner kín màn. Popup con thành **trang kín màn 1920×1080**: Popup màn, Chọn đội, Màn cốt truyện giữ chỗ, Lịch sử quay, Nhân viên, Chọn người cho ô, Nâng cấp, Chất lượng. **Giữ hộp**: popup xác nhận và phần thưởng |
| Trận | Không có thanh dưới. Menu tạm dừng và kết quả trận không đổi |
| Sự cố | Panel của thanh mở: vẫn sinh, đồng hồ dừng (game-design). Ba trang của quán (Nhân viên, Nâng cấp, Chất lượng) giữ luật cũ: chặn sinh sự cố |
| Panel DEV | Bỏ nút "Cài đặt" ở góc và 4 nút chuyển màn; mở từ dòng DEV trong Menu và từ menu tạm dừng của trận |
| Ảnh mô phỏng | Hub **bản 2** và trang con **bản 1** (mục 10) |

## 2. Hiện trạng (kiểm trong code ngày 2026-11-09, chỉ đọc)

- Scene trong Build Settings: Boot, Gacha, Restaurant, StageSelect, Battle, Characters. `GameBootstrap._startScene = "Restaurant"`; Boot sống qua mọi scene (`DontDestroyOnLoad`), `RestaurantRunner` chạy quán ở mọi scene.
- Chuyển màn: `DevOverlay` (nút Quán, Gacha, Màn chơi, Nhân vật, Chiến đấu thử), `StageMapView.Back` → Restaurant, `TeamSelectPopup` → Battle, `BattleView.Back` và `BattleResultView` → StageSelect. Hằng tên scene ở `BattleView` (`BattleScene`, `StageSelectScene`, `RestaurantScene`) và `DevOverlay`.
- `DevOverlay`: Canvas `sortingOrder = 1000`, nút "Cài đặt" neo góc dưới phải `(-16, 24)` rộng 150, ẩn ở Battle; panel lưới hai cột; ngôn ngữ qua `LanguageSwitcher` rồi nạp lại scene (trừ Battle); Reset qua hộp xác nhận rồi `GameResetter.Reset()`. `BattlePausePopup` có nút Cài đặt gọi `DevOverlay.OpenSettings()`, ẩn ở bản phát hành.
- `GameSession.ResetToDefaults()` trả `false` khi xoá file lỗi (bộ nhớ giữ nguyên) **và** khi đã ghi đè bộ nhớ nhưng `Persist()` lỗi. Ngôn ngữ về theo máy trước `ResetCompleted`.
- Quán: `RestaurantView.OnEnable/OnDisable` gọi `SetPresent(true/false)`; rời scene thì `IncidentDirector.SetPresent(false)` huỷ sự cố. `IncidentDirector.SetSpawnBlocked(owner, …)` do `StaffPanel`, `UpgradeTestPanel`, `QualityPopup`, `AfkPopup` gọi. Chuyên cần đếm `IsPresent`.
- Bố cục sát mép dưới: nút Quay 1/Quay 10 của Gacha neo dưới-phải `y = 110`, cao 160; dòng gợi ý của bản đồ ở tâm `−400` (đáy ~105 px); phòng quán gần kín màn. Mọi màn dùng `CanvasScaler` 1920×1080, khớp theo chiều cao.
- Test scene đọc lại từ file: `GachaSceneTests`, `CharactersSceneTests`, `StageSelectSceneTests`, `RestaurantSceneTests` (có `DevOverlay_SceneTargetsAreInBuildSettings`), `AndroidBuildTests` (danh sách scene của bản build). PlayMode `SmokeLoopTests` có ca DEV (`DevOverlay_SettingsButton_DoesNotCoverGameButtons_AndMenuButtonClicks`, hai ca Reset) và ca nạp thẳng `Characters`, `StageSelect`.

## 3. Ngoài phạm vi (cố ý không làm)

- Mọi thứ của đợt B và C (mục 1). Mỗi màn giữ khối số tiền riêng hiện có; luật "mỗi popup chỉ hiện tiền nó dùng" làm ở đợt B cùng thanh tài nguyên.
- Online, Shop, quảng cáo thưởng, mua trong game.
- Nút back của Android (chưa có ở đâu trong game).
- Hoạt hình mở/đóng panel, âm thanh.
- Đổi bố cục bên trong các màn ngoài việc dời khỏi thanh, phủ kín màn và nút Quay lại.
- Popup trong trận (tạm dừng, kết quả), hộp xác nhận và hộp phần thưởng: giữ cỡ.

## 4. Các phương án đã xét

| | A. Scene chồng (additive) | B. Panel trong `Restaurant.unity` (**chọn**, user) | C. Vẫn đổi scene, giả popup |
|---|---|---|---|
| Cách làm | Lobby luôn nạp, mục khác nạp scene có sẵn chồng lên | Ba builder dựng panel vào canvas của quán, bật/tắt | Mỗi scene tự có thanh dưới |
| Quán bên dưới | Còn | Còn | Bị gỡ; luật chuyên cần, sự cố phải viết lại trong service |
| Chuyển mục | Nạp bất đồng bộ | Tức thì | Nháy màn, mất trạng thái quán |
| Sửa test | Ít | Test 3 scene đọc lại từ `Restaurant.unity` | Ít |

User chọn B. Thêm được: chuyển mục tức thì, một scene duy nhất cho hub, không phải lo Camera/EventSystem trùng.

## 5. Thiết kế

Toạ độ theo khung tham chiếu 1920×1080, gốc **dưới-trái**, y tính lên. Kích thước ghi trong tên `_PH_` là cỡ thật ([unity-workflow](../../guides/unity-workflow.md#ui-và-hình-tạm)). Object đã có giữ tên cũ (test và `UiDriver` bấm theo tên); object mới chưa có hình đặt tên `<Tên>_PH_<rộng>x<cao>`.

### 5.1 Lớp vẽ trong `Restaurant.unity`

Một Canvas (như hiện nay). Thứ tự anh em từ dưới lên, builder dựng đúng thứ tự này:

1. Quán: phòng, khối số tiền, cột nút phải (Nhân viên, Nâng cấp, Thu AFK).
2. Trang của quán: Nhân viên (kèm Chọn người cho ô), Nâng cấp, Chất lượng.
3. Panel của thanh: `CharactersPanel_PH_1920x1080`, `StageMapPanel_PH_1920x1080`, `GachaPanel_PH_1920x1080`. Trang con của Gacha và Bản đồ nằm trong panel của nó.
4. `MenuPanel_PH_1920x1080`: kín màn, nằm trên panel đang mở và dưới thanh (thanh vẫn hiện, ô Menu sáng).
5. Thanh dưới `NavBar_PH_1920x120`.
6. Hộp xác nhận và hộp phần thưởng (mục 5.5), cùng hộp `ResetConfirm` của Menu: những thứ này vẽ **trên** thanh.

Panel và trang lúc đầu tắt. `RestaurantView` không bị tắt khi mở panel, nên `IsPresent` giữ `true`: quán vẫn chạy, chuyên cần vẫn đếm (game-design).

Hộp ở bước 6 là con của view của nó (ví dụ hộp xác nhận chuyển nhân viên là con của `StaffPanel`, nằm trong lớp 2). Để vẽ trên thanh mà vẫn tắt theo panel cha, mỗi hộp có `Canvas` con `overrideSorting = true` với order cao hơn canvas chính, kèm `GraphicRaycaster`. Menu và thanh nằm trên canvas chính nên mọi hộp đè lên được. Test ở mục 7 kiểm thứ tự vẽ thật (order của canvas, rồi thứ tự anh em), không kiểm cách làm.

Các trang cutscene, lộ, tổng của Gacha nằm trong panel Gacha (lớp 3) như cũ. Chúng không cần vẽ trên thanh, vì thanh bị ẩn trong lúc đó (mục 5.3).

### 5.2 `HubTab`, `HubNavState`, `IHubPage` (UI, thuần)

```csharp
public enum HubTab { Restaurant = 0, Characters = 1, Story = 2, Stages = 3, Gacha = 4, Menu = 5 }   // giá trị ghim, đúng thứ tự ô

public sealed class HubNavState
{
    public HubTab Current { get; }          // Restaurant, Characters, Stages hoặc Gacha; mặc định Restaurant
    public bool MenuOpen { get; }
    public HubTab Highlighted { get; }      // MenuOpen ? Menu : Current
    public HubNavResult Select(HubTab tab); // xem dưới
    public void CloseMenu();
}

public enum HubNavResult { Changed, ComingSoon, MenuOpened }

public interface IHubPage { bool IsOpen { get; } void Close(); }   // Close = đúng việc nút Quay lại của trang làm
```

`Select`:
- `Story` → `ComingSoon`, không đổi gì.
- `Menu` → `MenuOpen = true`, `Current` giữ nguyên → `MenuOpened`.
- Ô khác (kể cả ô đang đứng) → `Current = tab`, `MenuOpen = false` → `Changed`.

**Ô chờ mở** (sau khi nạp lại `Restaurant`): `HubNav.OpenOnNextLoad(HubTab tab, bool menu)` lưu static; `HubNav.Awake` lấy ra **một lần** rồi xoá. Static đặt lại ở `RuntimeInitializeLoadType.SubsystemRegistration` (tắt domain reload). Giá trị lạ (`Story`, `Menu` làm tab) thì về `Restaurant`.

### 5.3 `HubNav` và thanh dưới (UI)

`HubNav` (MonoBehaviour, trong `Restaurant.unity`), builder gán đủ:
- 6 nút ô `NavTabRestaurant_PH_320x120`, `NavTabCharacters_PH_320x120`, `NavTabStory_PH_320x120`, `NavTabStages_PH_320x120`, `NavTabGacha_PH_320x120`, `NavTabMenu_PH_320x120`. Ô Cốt truyện có thêm `Lock_PH_40x48` (hình ổ khoá ghép từ sprite có sẵn, như ổ khoá ở bản đồ màn).
- 3 panel của thanh, `MenuPanel`, dòng báo `ComingSoonToast` (tự tắt sau 2 giây, giờ không theo `timeScale`).
- Danh sách trang `[SerializeField] MonoBehaviour[] _pages`, mỗi phần tử cài `IHubPage` (mục 5.5). Phần tử không cài thì `Debug.LogError` khi kiểm tham chiếu.

Hành vi:
- Bấm một ô → `HubNavState.Select`. `Changed`: gọi `Close()` cho mọi trang đang mở, bật đúng panel, tắt panel khác, đóng Menu. `MenuOpened`: bật `MenuPanel`, không đụng trang. `ComingSoon`: hiện dòng "Sắp có".
- Ô `Highlighted` tô màu nổi; các ô khác màu thường. Ô vẫn bấm được (không `interactable = false`).
- **Thanh:** `NavBar_PH_1920x120` neo dưới, kéo hết bề ngang (`anchorMin (0,0)`, `anchorMax (1,0)`, cao 120). Mỗi ô chiếm 1/6 bề ngang bằng anchor (`i/6 … (i+1)/6`), nên màn 4:3 hay 20:9 đều vừa. `HubNav.BarHeight = 120` là hằng dùng chung cho builder và test.
- **Ẩn thanh:** `HubNav.SetBarHidden(object owner, bool hidden)` (bộ chủ như `SetSpawnBlocked`; ẩn khi còn ít nhất một chủ). Gacha gọi khi vào cutscene, lộ, tổng và trả khi về trang roll. `GachaView` nhận `HubNav` qua `[SerializeField]` do builder gán.
- **Đồng hồ sự cố:** `OnEnable` lấy `RestaurantService` từ `ServiceLocator` (thiếu thì `Debug.LogError` rồi thôi, như các view khác). Mỗi khi trạng thái đổi: `Incidents.SetClockPaused(this, Current != Restaurant || MenuOpen)`. `OnDisable`: `SetClockPaused(this, false)`.
- `Awake`: lấy ô chờ mở (5.2), áp vào trạng thái và bật panel/Menu tương ứng.

### 5.4 Ba panel của thanh

- `GachaSceneBuilder`, `CharactersSceneBuilder`, `StageSelectSceneBuilder` đổi thành dựng panel: `BuildPanel(RectTransform parent, …) → RectTransform` (root kín màn, nền đặc `Image` chặn bấm, lúc đầu tắt). `RestaurantSceneBuilder` gọi cả ba. Các hàm phụ dùng chung (`NewRect`, `Place`…) giữ chỗ cũ.
- View giữ nguyên cách lấy service ở `OnEnable` (`GachaView`, `CharactersView`, `StageMapView`), nên bật panel là view chạy; tắt panel là view gỡ sự kiện.
- **Bản đồ:** bỏ nút "Quay lại" (`StageMapView._backButton`, `Back()`); tiêu đề chương dời sang chỗ nút cũ.
- **Gacha:** banner `GachaBanner_PH_1920x1080` kéo kín panel (kể cả phần dưới thanh); tên banner, số Gem, nút Lịch sử nằm trên banner; nút Quay 1, Quay 10 và dòng báo dời lên trên vạch 120 px.
- **Nhân vật:** lưới và panel chi tiết co lại cho nằm trên vạch.
- **Quy tắc vạch:** trừ nút của chính thanh, hộp ở mục 5.1 bước 6 và các trang Gacha lúc thanh ẩn (cutscene, lộ, tổng), không `Button` nào (kể cả đang tắt, L-025, L-027) có rect giao với dải 0–120 px của khung tham chiếu. Phòng quán bị thanh che một phần mép dưới là chấp nhận được ở đợt A (đợt B có kéo camera), nhưng mọi trạm (nơi bong bóng "!" hiện) phải nằm trên vạch để bấm được.

### 5.5 Trang kín màn và hộp giữ nguyên

**Trang kín màn** (8): `StageInfoPopup`, `TeamSelectPopup`, `StoryPlaceholderPopup`, `GachaHistoryPopup`, `StaffPanel` (gồm danh sách Chọn người cho ô), `UpgradeTestPanel`, `QualityPopup`.
- Root kéo kín 1920×1080, nền đặc chặn bấm, nằm **dưới** thanh (thanh vẫn hiện và bấm được).
- Nội dung bố trí lại trong vùng trên vạch 120 px theo mô phỏng trang con bản 1 (mục 10). Không thêm, bớt thông tin.
- Nút đóng/huỷ đang có dời lên **góc trên-trái**, nhãn "Quay lại" (`hub.back`), giữ tên object cũ (`CloseButton`, `CancelButton`…). Danh sách Chọn người cho ô: Quay lại về trang Nhân viên. Trang không có nút đóng thì thêm `BackBtn_PH_200x80`.
- Mỗi view cài `IHubPage`: `IsOpen` như hiện có; `Close()` làm đúng việc nút Quay lại (với `TeamSelectPopup` là `Cancel()`, trả đội về như lúc mở). Nhờ vậy bấm ô khác của thanh thì trang đóng sạch: `StaffPanel`, `UpgradeTestPanel`, `QualityPopup` trả `SetSpawnBlocked(this, false)` như khi bấm Quay lại.

**Giữ hộp** (cỡ và bố cục không đổi): popup xác nhận (hộp chuyển nhân viên, `SmallConfirm` và `StaffConfirm` của Chọn đội, `GachaRollAgainPopup`, `ResetConfirm` của Menu) và popup phần thưởng (`AfkPopup`). Nền mờ kéo kín 1920×1080, vẽ **trên** thanh, chặn bấm cả thanh. Màn kết quả trận ở scene Battle, không đổi.

### 5.6 Panel Menu

```
┌──────────────────────────────────────────────── MenuPanel_PH_1920x1080 ─┐
│ Menu                                                                   │
│                  [ Ngôn ngữ: Tiếng Việt          ]  MenuLanguageBtn_PH_700x100
│                  [ Reset dữ liệu                 ]  MenuResetBtn_PH_700x100 (đỏ)
│                  [ DEV                           ]  MenuDevBtn_PH_700x100 (chỉ bản dev)
│                         dòng trạng thái              MenuStatus
│                        [ Đóng ]                      MenuCloseBtn_PH_300x90
├────────────────────────────────────────────────────────────────────────┤
│ Quán │ Nhân vật │ Cốt truyện 🔒 │ Màn chơi │ Gacha │ [Menu]              │
└────────────────────────────────────────────────────────────────────────┘
```

`MenuView` (MonoBehaviour), chữ qua `MenuViewText` (thuần):
- Mở Menu thì xoá dòng trạng thái và tắt `ResetConfirm`.
- **Ngôn ngữ:** nhãn `hub.language` (bản Việt "Ngôn ngữ: Tiếng Việt", bản Anh "Language: English", nên nhãn luôn ghi ngôn ngữ hiện tại bằng chính ngôn ngữ đó). Bấm: `LanguageSwitcher.Set(ngôn ngữ kia)`. `true` → `HubNav.OpenOnNextLoad(state.Current, menu: true)` rồi `SceneManager.LoadScene("Restaurant")`: chữ do builder dựng vẽ lại qua `LocalizedText`, người chơi vẫn ở đúng ô với Menu mở. `false` → giữ ngôn ngữ cũ, dòng trạng thái "Chưa lưu được, giữ ngôn ngữ cũ" (`hub.language_not_saved`).
- **Reset dữ liệu:** bật `ResetConfirm_PH_800x420`: câu `hub.reset_confirm` ("Xoá TOÀN BỘ dữ liệu đã lưu và đưa game về như mới?\nKhông thể hoàn tác."), `ResetYesBtn_PH_340x110` "Xoá hết" (đỏ), `ResetNoBtn_PH_340x110` "Huỷ" (`common.cancel`). Chưa bấm Xoá hết thì không gọi gì ([D-005](../../decisions/decisions.md)).
  - Xoá hết → `ServiceLocator.Get<GameResetter>().Reset()`, tức `GameSession.ResetToDefaults()`: xoá mọi `save.json.*`, ghi đè `PlayerSave`, cấp lại đồ game mới, ngôn ngữ về theo máy, phát `ResetCompleted`.
  - `true` → `SceneManager.LoadScene("Restaurant")`, không ô chờ: về Lobby như game mới.
  - `false` → hộp đóng, dòng trạng thái `hub.reset_failed` ("Reset chưa xong: không ghi được file save. Thử lại."). Câu này **không** nói "dữ liệu giữ nguyên", vì `ResetToDefaults` cũng trả `false` khi đã ghi đè bộ nhớ mà lưu lỗi (mục 2).
  - Huỷ → hộp đóng, không đổi gì.
- **DEV:** nút và listener nằm trong `#if UNITY_EDITOR || DEVELOPMENT_BUILD` theo khuôn `BattlePausePopup` (bản phát hành `SetActive(false)` trong `OnEnable`). Bấm → `DevOverlay.OpenSettings()`. Không `[SerializeField]` trong `#if` (lệch bố cục serialize): field nút DEV khai báo ngoài `#if`.
- **Đóng:** `HubNavState.CloseMenu()`, tắt `MenuPanel`; panel và trang đang mở bên dưới vẫn như cũ.
- Bấm một ô khác khi Menu mở thì Menu đóng và ô đó mở (`Select` → `Changed`). Menu chỉ mở từ ô Menu của thanh. Battle không có thanh nên không có đường vào Menu: đổi ngôn ngữ không chạm trận đang chạy (`DevOverlay.ShouldReloadScene` vẫn giữ Battle cho nút DEV).

### 5.7 Trận và đường về

- `Battle.unity` không dựng thanh, không có Menu.
- `BattleView.Back()` (đội trống) và nút Quay lại của `BattleResultView` → `HubNav.OpenOnNextLoad(HubTab.Stages, menu: false)` rồi `SceneManager.LoadScene("Restaurant")`. Thử lại vẫn như cũ.
- `TeamSelectPopup` vào trận bằng `LoadScene(Battle)` như cũ: rời `Restaurant` thì `SetPresent(false)` huỷ sự cố (luật cũ: vào trận là rời màn quán).
- Hằng `BattleView.StageSelectScene` bị bỏ. Tên scene `Restaurant` và `Battle` giữ ở `BattleView` (không thêm lớp hằng mới).

### 5.8 Sự cố (Restaurant)

`IncidentDirector` thêm:

```csharp
// Panel của hub đang che quán: sự cố vẫn sinh, nhưng đồng hồ kịp/trễ của sự cố đang chạy đứng yên (game-design, 2026-11-09).
public void SetClockPaused(object owner, bool paused);   // bộ chủ như SetSpawnBlocked; owner null → ArgumentNullException
```

- `Update(dt)`: có sự cố và còn ít nhất một chủ dừng → không cộng `_elapsed`, không xét Missed. Không có sự cố → bộ đếm sinh chạy và sinh như thường (chủ dừng không chặn sinh).
- Trạm của sự cố đứng yên như hiện nay tới khi xử lý (`SetStationBlocked` không đổi).
- `Cancel`, `Reset`, `SetPresent(false)` không xoá bộ chủ (chủ tự trả ở `OnDisable`).
- `SetSpawnBlocked` giữ nguyên cho trang của quán và `AfkPopup`.

### 5.9 `DevOverlay`

- Bỏ nút "Cài đặt" ở góc, `_openButton`, `OnSceneChanged` và đăng ký `activeSceneChanged`.
- Bỏ 4 nút Quán, Gacha, Màn chơi, Nhân vật và hằng `RestaurantScene`, `GachaScene`, `CharactersScene`. `SceneNames` còn `{ "Restaurant", "Battle" }` (các scene panel DEV còn nạp: nạp lại khi đổi ngôn ngữ, Chiến đấu thử).
- Còn 9 nút: Chiến đấu thử, Màn thử, Hồi đầy thể lực, +Material, +Gem, Tua AFK, Ngôn ngữ, Reset dữ liệu, Đóng. Xếp lại lưới hai cột, thu hộp cho vừa; hộp xác nhận Reset giữ nguyên.
- `OpenSettings()` giữ `public`; người gọi: `MenuView` (dòng DEV), `BattlePausePopup`.
- Panel DEV chỉ mở được khi người thử bấm, nên không còn chuyện nút DEV đè nút game. Câu trong [unity-workflow](../../guides/unity-workflow.md#ui-và-hình-tạm) về nút "Cài đặt" góc dưới phải sửa theo (mục 8).
- Chữ DEV giữ tiếng Việt, ngoài `UiText` (như hiện nay).

### 5.10 `ProjectSetup` và scene

- Tạo/dựng lại `Restaurant.unity` gồm cả ba panel, thanh, Menu. Bỏ menu `Rebuild Gacha Scene`, `Rebuild Characters Scene`, `Rebuild Stage Select Scene`; `Rebuild Restaurant Scene` dựng tất cả.
- Xoá `Gacha.unity`, `Characters.unity`, `StageSelect.unity` bằng `AssetDatabase.DeleteAsset` khi còn (Unity xoá kèm `.meta`; **không xoá tay `.meta`**). Build Settings: **Boot, Restaurant, Battle**, theo thứ tự đó. `_startScene` giữ luật cũ (trống hoặc `"Gacha"` thì đặt `Restaurant`).
- Test gọi `ProjectSetup.Run()` hay `RestaurantSceneBuilder.Create` thì nạp lại asset bằng `AssetDatabase.LoadAssetAtPath` sau lệnh gọi, kể cả trong `finally` (luật Test L-032 trong [code-style](../../guides/code-style.md#test)); `Restaurant.unity` dựng bằng `NewScene(Single)` nên mọi biến asset nạp trước đó đều chết.
- Sinh scene bằng code thì có test đọc lại tham chiếu từ file scene (L-002).
- Không sửa tay `.unity`; chỉ dựng lại bằng builder.

### 5.11 Chữ (`UI/Scripts/Text/UiText.Hub.cs`, key `hub.*`)

| Key | Việt | Anh (nháp, user duyệt ở playtest) |
|---|---|---|
| `hub.tab_restaurant` | Quán | Restaurant |
| `hub.tab_characters` | Nhân vật | Characters |
| `hub.tab_story` | Cốt truyện | Story |
| `hub.tab_stages` | Màn chơi | Quests |
| `hub.tab_gacha` | Gacha | Gacha |
| `hub.tab_menu` | Menu | Menu |
| `hub.coming_soon` | Sắp có | Coming soon |
| `hub.menu_title` | Menu | Menu |
| `hub.language` | Ngôn ngữ: Tiếng Việt | Language: English |
| `hub.language_not_saved` | Chưa lưu được, giữ ngôn ngữ cũ | Couldn't save. Language not changed |
| `hub.reset` | Reset dữ liệu | Reset data |
| `hub.reset_confirm` | Xoá TOÀN BỘ dữ liệu đã lưu và đưa game về như mới?\nKhông thể hoàn tác. | Delete ALL saved data and start over?\nThis can't be undone. |
| `hub.reset_yes` | Xoá hết | Delete all |
| `hub.reset_failed` | Reset chưa xong: không ghi được file save. Thử lại. | Reset didn't finish: couldn't write the save file. Try again. |
| `hub.dev` | DEV | DEV |
| `hub.back` | Quay lại | Back |

Dùng lại `common.close`, `common.cancel`. Nhãn cố định do builder dựng (ô, tiêu đề Menu, Quay lại) gắn `LocalizedText`; chữ view ghi lúc chạy (nhãn Ngôn ngữ, dòng trạng thái) thì không. `UiTextTests` có sẵn bắt key thiếu bản Anh và lệch `\n`.

## 6. Luồng chạy

```
Mở game: Boot → LoadScene(Restaurant) → HubNav.Awake (không ô chờ) → Lobby
Đổi mục: NavTab* → HubNavState.Select → đóng trang (IHubPage.Close) → bật panel → SetClockPaused(this, Current != Restaurant || MenuOpen)
Ngôn ngữ: Menu → LanguageSwitcher.Set → OpenOnNextLoad(Current, menu) → LoadScene(Restaurant) → HubNav.Awake mở lại ô và Menu
Reset: Menu → ResetConfirm → Xoá hết → GameResetter.Reset → true: LoadScene(Restaurant) · false: hub.reset_failed
Vào trận: Bản đồ → Popup màn → Chọn đội → LoadScene(Battle) (rời quán, huỷ sự cố)
Về: Battle Quay lại / kết quả → OpenOnNextLoad(Stages) → LoadScene(Restaurant) → bản đồ mở sẵn
DEV: Menu → DEV → DevOverlay.OpenSettings (chỉ bản dev); Battle → Menu tạm dừng → Cài đặt (như cũ)
```

## 7. Test

Nền: **832 EditMode + 16 PlayMode**. Số dự kiến: **857 EditMode + 20 PlayMode** (kế hoạch chốt từng task). Không `[TestCase]` (L-012). Test bố cục xét cả nút đang tắt (L-025, L-027).

**EditMode:**

| Test | Bắt lỗi gì | Số |
|---|---|---|
| `HubNavStateTests` | Mặc định `Restaurant`; chọn ô đổi `Current` và đóng Menu; `Story` → `ComingSoon`, không đổi; `Menu` → `MenuOpen`, giữ `Current`; `CloseMenu`; `Highlighted`; ô chờ lấy một lần rồi xoá, giá trị lạ về `Restaurant` | +6 |
| `IncidentDirectorTests` (thêm ca) | Dừng thì `Elapsed` đứng và không Missed; dừng vẫn sinh sự cố mới; hai chủ, trả một chủ vẫn dừng | +3 |
| `MenuViewTextTests` | Nhãn Ngôn ngữ đúng ở Việt và Anh; câu Reset lỗi không chứa "giữ nguyên" | +2 |
| `LobbySceneTests` (đọc `Restaurant.unity` từ file) | 6 ô đúng tên và thứ tự, neo dưới, cao 120, mỗi ô 1/6 bề ngang; 4 panel lúc đầu tắt và `HubNav` đủ tham chiếu, `_pages` đều cài `IHubPage`; mọi `LocalizedText` có key; không nút nào nằm dưới thanh (trừ thứ vẽ trên thanh và trang Gacha lúc thanh ẩn); thứ tự vẽ đúng mục 5.1; mọi trạm trên vạch | +6 |
| Trang kín màn (mỗi trang một test) | Root kín 1920×1080 có nền chặn bấm, vẽ dưới thanh; nút Quay lại ở góc trên-trái; mọi nút trên vạch | +8 |
| Hộp giữ nguyên | Nền mờ kín màn và vẽ trên thanh, với mọi hộp xác nhận và phần thưởng ở mục 5.5 | +1 |
| `GachaSceneTests` (thêm ca) | Banner kéo kín panel | +1 |
| `ProjectSetupTests` (thêm ca) | Build Settings đúng Boot, Restaurant, Battle; 3 scene cũ không còn; chạy lại không lỗi (L-032) | +1 |
| `GachaSceneTests`, `CharactersSceneTests`, `StageSelectSceneTests` (sửa) | Đọc panel trong `Restaurant.unity` thay cho scene riêng; bỏ ba ca "scene có Camera, EventSystem" của ba scene cũ; ca của `StageSelect` về nút Quay lại bỏ theo | −3 |
| `AndroidBuildTests`, `RestaurantSceneTests.DevOverlay_SceneTargetsAreInBuildSettings` (sửa) | Danh sách scene mới; `SceneNames` mới | 0 |
| `HardcodedTextTests`, `UiTextTests` (có sẵn) | Tự quét `HubNav`, `MenuView`, builder mới | 0 |

**PlayMode** (save tạm như `SmokeLoopTests`):

| Test | Kiểm gì |
|---|---|
| `Hub_TabLoop_OpensEachPanel` (mới) | Bấm lần lượt 6 ô bằng `UiDriver`: đúng panel bật, Cốt truyện hiện "Sắp có", Quán tắt hết; `RestaurantService.IsPresent` luôn `true` |
| `Hub_Menu_Reset_CancelKeeps_ConfirmClears` (mới) | Menu → Reset → Huỷ: ví và đội không đổi, file save còn. Reset → Xoá hết: save như game mới, đang ở Lobby không panel |
| `Hub_Menu_Language_KeepsTabAndPersists` (mới) | Mở Gacha → Menu → Ngôn ngữ: sau khi nạp lại vẫn Gacha, Menu mở, nhãn đổi; nạp lại save tạm thấy `Language` mới |
| `Hub_BattleBack_OpensStageMap` (mới) | Vào trận, Bỏ cuộc, bấm Quay lại: `Restaurant` nạp, bản đồ mở sẵn |
| Ca DEV và ca nạp thẳng scene (sửa) | Mở panel DEV qua Menu → DEV; ca `DevOverlay_SettingsButton_DoesNotCoverGameButtons_AndMenuButtonClicks` thay bằng ca "Menu DEV mở panel, Đóng trả về Menu"; ca Nhân vật, Bản đồ mở bằng bấm ô thay cho `LoadScene` |

**Kiểm bằng mắt (dừng playtest cuối đợt A):** chụp bằng `Capture1080("hub-<màn>.png")` (L-026 có đuôi) ở tiếng Việt và tiếng Anh: Lobby, Nhân vật, Bản đồ, Popup màn, Chọn đội, Gacha (trang roll), Nhân viên, Menu, hộp Reset. User so với mô phỏng ở mục 10. Playtest: mở game → bấm đủ 6 ô → Gacha quay 10 (thanh ẩn trong lúc lộ) → Nhân vật nâng sao → Màn chơi → đánh 1-1 → Quay lại thấy bản đồ → Menu đổi English → tắt, mở lại vẫn English → Menu → Reset → Huỷ → Reset → Xoá hết → game như mới.

## 8. Docs phải sửa (khi xong, trong kế hoạch)

- [roadmap.md](../../roadmap.md): dòng "Đang làm"; dòng UI hub tách thành đợt A, B, C.
- [project/structure.md](../../project/structure.md): scene còn 3, `HubNav`, `MenuView`, `UiText.Hub.cs`, số test.
- [unity-workflow.md](../../guides/unity-workflow.md#ui-và-hình-tạm): câu về nút "Cài đặt" DEV góc dưới phải và smoke test của nó đổi thành "panel DEV mở từ Menu → DEV"; luật vạch 120 px của thanh dưới cho mọi nút mới.
- [D-012](../../decisions/decisions.md): "đổi ở panel Cài đặt DEV" thêm "và ở Menu của hub".
- Đã sửa 2026-11-09: [game-design.md](../../design/game-design.md#quán) thêm ngoại lệ: trang Nhân viên, Nâng cấp, Chất lượng và popup AFK vẫn chặn sinh sự cố.
- [screen-flow.md](../../design/screen-flow.md) là file của kênh 🎨: 📐 không sửa; báo 🎨 cập nhật trạng thái "có" cho các màn đợt A khi xong.

## 9. Rủi ro

| Rủi ro | Cách giữ |
|---|---|
| `Restaurant.unity` to và dựng chậm; một builder lỗi làm hỏng cả hub | Mỗi panel một hàm `BuildPanel`; test đọc lại từng panel; `Rebuild Restaurant Scene` là đường duy nhất |
| Bấm ô khác khi trang của quán đang mở làm kẹt `SetSpawnBlocked` | `IHubPage.Close()` gọi đúng đường Quay lại; test PlayMode vòng ô kiểm sự cố vẫn sinh |
| Ô chờ static còn sót sau khi tắt domain reload | Lấy một lần rồi xoá; đặt lại ở `SubsystemRegistration`; test ô chờ |
| Nút mới lọt xuống dưới thanh hoặc hộp không che thanh | Test vạch 120 px và thứ tự vẽ (cả nút tắt) |
| Bản tiếng Anh dài làm tràn ô 320 px ("Restaurant", "Characters") | Ảnh tiếng Anh ở lần dừng playtest; rút gọn trong `UiText.Hub.cs` |
| Reset lỗi sau khi đã ghi đè bộ nhớ: người chơi thấy game "mới" mà file cũ còn | Câu báo không hứa "dữ liệu giữ nguyên"; lần lưu sau ghi lại (luật cũ của `GameSession`) |
| Xoá 3 scene làm gãy tham chiếu ngoài code (Build Settings Android, công cụ thử) | `AndroidBuildTests`, `ProjectSetupTests`, grep tên scene ở review |
| Đợt A làm phình phạm vi trước online | Đợt B, C tách spec riêng; đợt A không thêm thông tin mới vào màn |

## 10. Ảnh mô phỏng đã duyệt

Vẽ và duyệt trong kênh 📐 UI hub ngày 2026-11-09 (ảnh nằm ngoài repo, chỉ ở hội thoại). Bản chép bố cục:

**Hub bản 2** (user duyệt; bản 1 bị thay vì banner và Menu chưa kín màn):

```
Gacha:                                       Menu:
┌──────────── GachaBanner_PH_1920x1080 ──┐   ┌──────────── MenuPanel_PH_1920x1080 ──┐
│ [Tên banner]               [Gem 1.200] │   │ Menu                                 │
│                                        │   │        [Ngôn ngữ: Tiếng Việt]        │
│          banner phủ cả màn             │   │        [Reset dữ liệu]  (đỏ)         │
│                                        │   │        [DEV] (chỉ bản dev)           │
│                    [Quay 1] [Quay 10]  │   │          dòng trạng thái  [Đóng]     │
├─ vạch 120 px ──────────────────────────┤   ├──────────────────────────────────────┤
│Quán│Nhân vật│Cốt truyện🔒│Màn chơi│[Gacha]│Menu│ │Quán│…│Gacha│[Menu]           │
└────────────────────────────────────────┘   └──────────────────────────────────────┘
Hộp Reset: nền mờ kín màn (che cả thanh), hộp đỏ giữa màn, [Xoá hết] [Huỷ].
```

**Trang con bản 1** (user duyệt). Mỗi trang: [Quay lại] trên-trái, thanh dưới vẫn hiện, nội dung trên vạch.

| Trang | Bố cục |
|---|---|
| Popup màn | tên màn trên; trái: quái theo wave; phải: thưởng, thể lực, [Thách đấu] dưới-phải |
| Chọn đội | lưới nhân vật 8 × 2 có trang ‹ ›; dưới: hàng đội 5 ô, [Bắt đầu] phải |
| Màn cốt truyện giữ chỗ | tên màn, "Chưa có nội dung", [Tiếp tục] giữa |
| Lịch sử quay | danh sách 50 lượt, cuộn |
| Nhân viên | ba cột Bếp, Phục vụ, Thu ngân |
| Chọn người cho ô | danh sách nhân vật, cuộn |
| Nâng cấp | các dòng nâng cấp, nút Mua |
| Chất lượng | chuyên cần 7 ngày, sự cố, hệ số AFK |
