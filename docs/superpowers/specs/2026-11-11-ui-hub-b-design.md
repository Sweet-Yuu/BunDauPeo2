# Spec: UI hub, đợt B (bố cục Lobby)

> Ngày: 2026-11-11 · Trạng thái: **đã duyệt** (user, 2026-11-11: thiết kế, ảnh mô phỏng bản 1 và bản viết, kể cả khung kéo 2800×1400, save cũ chưa chọn ngôn ngữ thành tiếng Anh, dòng "SR · 3★" ở trang Nhân viên) · Phạm vi: Lobby theo ảnh bản 2 của luồng màn (avatar, thanh tài nguyên chung, thanh chất lượng một hàng, banner Gacha, nút tròn Shop khoá, thanh quán thu gọn có nhớ, kéo quán); quán vẽ kiểu 2.5D (khối có chiều cao, bóng, người đứng và đi có nhún, đĩa món, bóng trạng thái khách); làm lại trang Nhân viên và Nâng cấp; Lịch sử quay thành hộp; mặc định tiếng Anh.
> Nguồn: [luồng màn](../../design/screen-flow.md) mục "Bố cục Lobby" (user duyệt ảnh bản 2, 2026-11-09) và "Các màn" (luật tiền theo màn), [roadmap](../../roadmap.md) dòng "UI hub đợt B", [spec đợt A](2026-11-09-ui-hub-design.md) (khung, `HubNav`, vạch 120 px, `OverBar`), [spec Restaurant 5.1](2026-11-01-restaurant-design.md#51-màn-quán-restaurantunity) (bố cục phòng), [D-005](../../decisions/decisions.md) (save, Reset), [D-012](../../decisions/decisions.md) (chữ qua `UiText`), [unity-workflow, UI và hình tạm](../../guides/unity-workflow.md#ui-và-hình-tạm). Ảnh tham khảo: `Mota/hub-home-annotated-en.png`, `Mota/restaurant-layout-{garden,farm,sea}-en.webp`, `Mota/character-upgrade.png`. Kế hoạch: [plans/2026-11-11-ui-hub-b.md](../plans/2026-11-11-ui-hub-b.md).

## 1. Mục tiêu

Lobby trông và dùng như màn chính của một game quản lý quán: quán chạy kín màn, kéo xem quanh được, đồ và người có khối; trên màn chỉ có những nút người chơi cần, mỗi màn chỉ hiện loại tiền nó dùng. Đây là đợt cuối về bố cục trước đợt C và online, nên phạm vi giữ nhỏ: không thêm luật chơi, không thêm màn mới.

Thành công khi:
1. Lobby khớp ảnh mô phỏng bản 1 (mục 10): avatar trên-trái, thanh tài nguyên 4 ô, thanh chất lượng một hàng, banner Gacha bên trái, thanh quán thu gọn bên phải, nút Shop khoá dưới-phải, thanh dưới 6 ô của đợt A.
2. Vuốt trên quán kéo được khung nhìn hai chiều, dừng ở mép; bấm "!" của sự cố vẫn ăn; không phóng to.
3. Thanh tài nguyên đổi theo màn đúng bảng ở mục 5.3; khối số tiền cũ ở Lobby, Bản đồ, Gacha không còn.
4. Thanh quán mặc định đóng; mở/đóng xong, tắt và mở lại game vẫn đúng như lần trước; Reset dữ liệu đưa về đóng.
5. Avatar, nút Shop, nút + của Thể lực, Coin, Gem đều hiện dòng "Sắp có"; banner mở ô Gacha.
6. Quán nhìn 2.5D: bếp, quầy, bàn, tường, luống, cây có mặt trước tối hơn và bóng; khách và nhân viên đứng có thân, đầu, bóng; người đang đi nhún và đung đưa hai bên; bàn có khách đang ăn thì có đĩa; khách chờ món hay chờ trả tiền có bóng trạng thái.
7. Trang Nhân viên và Nâng cấp dùng hết vùng trên vạch, chữ to, đúng ảnh mô phỏng; Lịch sử quay là hộp giữa màn che cả thanh.
8. Mở game lần đầu (hoặc sau Reset) là tiếng Anh, bất kể ngôn ngữ máy.
9. Mọi chữ mới đi qua `UiText`, có bản tiếng Anh; `HardcodedTextTests` xanh. Object cũ giữ tên; object mới chưa có hình đặt tên `<Tên>_PH_<rộng>x<cao>`. Không nút nào lọt xuống dải 0–120 px (luật vạch của đợt A).

### Quyết định đã chốt với user (2026-11-11)

| Chủ đề | Quyết định |
|---|---|
| Tiền theo màn | Lobby: Thể lực, Coin, Material, Gem. Nhân vật: Coin. Bản đồ, Popup màn, Chọn đội: Thể lực. Gacha (trang roll): Gem. Nâng cấp quán: **Material**. Nhân viên, Chất lượng, Cốt truyện giữ chỗ, Menu: không hiện. Lịch sử quay: hộp che thanh, không cần |
| Nâng cấp và Coin | User: "nâng cấp nhân vật bằng Coin, quán bằng Material". Đợt B **chỉ đổi thanh tài nguyên**; giá nâng cấp quán (đang Coin + Material) và trang nâng cấp nhân vật theo `character-upgrade.png` để việc riêng, 📐 nhắn 🎨 ghi vào sổ ý tưởng |
| Lịch sử quay | Thành **hộp** giống hộp xác nhận (đợt A là trang kín màn) |
| Thanh quán | Mặc định đóng; **nhớ qua tắt mở game** bằng `PlayerSave`; Reset về đóng. Chấm báo khi có Coin AFK chờ thu |
| Kéo quán | Hai chiều, chặn ở mép, có quán tính, **không zoom** |
| Ô hiện / khoá | Banner: 1 banner Gacha, bấm mở ô Gacha, không chấm trang khi chỉ có 1 banner. Nút tròn: chỉ Shop, khoá. Avatar, Shop, nút +: "Sắp có". Nút Thông báo: không làm, chừa chỗ cho đợt C |
| Quán 2.5D | User yêu cầu thêm: khối có chiều cao và bóng; người đứng, nhún **và đung đưa hai bên như bước hai chân**; đĩa món trên bàn; bóng trạng thái trên đầu khách |
| Trang của quán | Làm lại Nhân viên và Nâng cấp trong đợt B |
| Ngôn ngữ | User thêm 2026-11-11: **vào game mặc định tiếng Anh** (thay "theo máy" của D-012) |
| Ảnh mô phỏng | Lobby bản 1 và trang bản 1 (mục 10) |

## 2. Hiện trạng (kiểm trong code ngày 2026-11-11, chỉ đọc)

- `RestaurantSceneBuilder.Create` dựng một Canvas (`CanvasScaler` 1920×1080, `matchWidthOrHeight = 1`). Thứ tự: `Background` (kéo kín canvas), `Garden` (8 vòng tròn cây/hoa), `Room` (neo giữa-trên, chứa `Crops`, `Floor`, 4 thanh tường, `Furniture`, `Landmarks`, `Templates`, `Actors`, `IncidentBubble`), `RestaurantView`, cột `RightColumn` 170×520 với `StaffButton`, `UpgradeButton`, `AfkButton`; HUD: `CoinCard`, `MaterialCard`, `GemCard` (260×76), `InfoBar` 840×120 hai hàng (là `Button` mở `QualityPopup`), `AfkMessage`, 8 `FloatText` (con của canvas), `RestaurantHud`. Sau đó là trang của quán, 3 panel, Menu, thanh dưới (đợt A).
- Không có camera thế giới: phòng là `RectTransform` trong Canvas. `Block(...)` vẽ mỗi đồ một `Image` xoay −11,31°, chưa có chiều cao. Người là `Image` một khối (`CustomerTemplate` tròn 54, `WorkerTemplate` `Knob` 64 kèm `NameText`); `RestaurantView.Move` nội suy vị trí, `SortByDepth` sắp theo y.
- Lobby không có ô Thể lực. `StageMapView` có thẻ Thể lực (kèm "+1 sau m:ss"), Material, Gem; `GachaView` có `_gemText`.
- `HubNav` có `ComingSoonToast` (tự tắt sau 2 giây), `SetBarHidden(owner, hidden)`, `_pages` (8 `IHubPage`), `CloseOpenPages()`.
- `GameLanguage.Resolve(saved, system)`: chưa chọn hoặc mã lạ thì theo máy (Việt nếu máy tiếng Việt, khác thì Anh). `GameSession` nhận `Func<SystemLanguage>`; Reset đưa ngôn ngữ về theo máy (`GameSessionTests.ResetToDefaults_AfterChoosingEnglish_FollowsDeviceLanguage`, PlayMode `SmokeLoopTests` dòng kiểm "sau Reset phải theo máy").
- Test đọc tên HUD cũ: `RestaurantSceneTests` (`CoinCard`, `MaterialCard`, `GemCard`, `RightColumn`, `InfoBar`), `LobbySceneTests.DrawOrder_PanelsMenuBar` (`Room`, `RightColumn`), `GachaSceneTests`.
- Nền test: **864 EditMode + 26 PlayMode** (cuối đợt A).

## 3. Ngoài phạm vi (cố ý không làm)

- Đợt C: màn Tiêu đề, Chọn chế độ, nút Thông báo (chỉ chừa chỗ), Hồ sơ.
- Shop và mọi thứ mua bán, quảng cáo; 4 nút tròn online (Hộp quà, Thông báo tin, Nhiệm vụ, Hội).
- Đổi giá nâng cấp quán sang chỉ Material; trang nâng cấp nhân vật bằng Coin (luật chưa chốt, xem mục 11).
- Zoom quán; tự lật banner; chấm trang banner; nhiều banner.
- Hoạt hình mở/đóng thanh quán và panel (bật/tắt tức thì như đợt A); âm thanh.
- Đổi luật quán, service, số liệu cân bằng. Chỉ đổi phần dựng hình và phần view đọc trạng thái có sẵn.
- Đổi các panel Nhân vật, Bản đồ, Gacha ngoài việc bỏ khối số tiền cũ.

## 4. Các phương án đã xét

| Việc | Chọn | Bỏ | Lý do |
|---|---|---|---|
| Kéo quán | `ScrollRect` của uGUI, nội dung là khung thế giới | Bộ kéo tự viết bằng `IDragHandler` | Có sẵn kéo, quán tính, chặn mép; uGUI tự huỷ click khi ngón tay đã kéo. Tự viết chỉ có lợi khi cần zoom, mà user chọn không zoom |
| Thanh tài nguyên | **Một** `ResourceBar` vẽ trên panel và trang, luật hiện ô là hàm thuần | Mỗi panel một thanh riêng | Dựng một lần, luật một chỗ có test; panel không phải biết ví |
| Quán 2.5D | Ghép `Image` có sẵn: mặt trên + mặt trước tối hơn + bóng; người = bóng + thân + đầu | Đổi sang SpriteRenderer/camera thế giới, hay vẽ sprite mới | Không đổi cấu trúc Canvas, test phòng giữ được, không thêm file ảnh |
| Nhớ thanh quán | Field trong `PlayerSave` | `PlayerPrefs`; chỉ nhớ trong lượt chơi | D-005: dữ liệu lưu chỉ qua `PlayerSave`/`SaveService` để Reset xoá hết; user chọn nhớ qua tắt mở |

## 5. Thiết kế

Toạ độ theo khung tham chiếu 1920×1080, ghi theo kiểu "từ góc trên-trái" như builder của quán (y tính xuống) trừ chỗ ghi rõ. Kích thước trong tên `_PH_` là cỡ thật. **Object đã có giữ tên cũ** (test và `UiDriver` bấm theo tên); **object mới chưa có hình đặt tên `<Tên>_PH_<rộng>x<cao>`**.

### 5.1 Lớp vẽ trong `Restaurant.unity`

Thứ tự anh em từ dưới lên (đợt A có 6 lớp; đợt B tách lớp 1 và thêm lớp 3b):

1. **Thế giới** `WorldScroll` (mục 5.2): `World_PH_2800x1400` chứa `Background`, `Garden`, `Room`, `FloatText*`.
2. **HUD Lobby** `LobbyHud`: `Avatar_PH_140x140`, `InfoBar`, `AfkMessage`, `BannerGacha_PH_360x200`, `RestaurantBar_PH_180x460` + `RestaurantBarToggle_PH_60x160`, `HubRoundBtnShop_PH_110x110`. Cả nhóm chỉ bật khi ở Lobby (ô Quán, không trang, không Menu); `RestaurantView` và `RestaurantHud` không nằm trong nhóm nên quán vẫn chạy khi nhóm tắt.
3. Trang của quán (Nhân viên, Nâng cấp, Chất lượng), rồi 3 panel của thanh (như đợt A).
   - 3b. **`ResourceBar_PH_1120x76`** (mục 5.3): trên mọi panel và trang, dưới Menu.
4. `MenuPanel_PH_1920x1080`.
5. `NavBar_PH_1920x120`.
6. Hộp vẽ trên thanh (`OverBar`): hộp xác nhận, `AfkPopup`, `ResetConfirm`, và **hộp Lịch sử quay** (mục 5.7).

`ComingSoonToast` giữ chỗ của đợt A (vẽ trên thanh tài nguyên).

### 5.2 Kéo quán (`WorldScroll`)

- `WorldScroll`: `RectTransform` kéo kín canvas, `Image` trong suốt có raycast (để nhận vuốt), `ScrollRect`: `horizontal = vertical = true`, `movementType = Clamped`, `inertia = true`, `scrollSensitivity = 0` (không cuộn chuột), `viewport` = chính nó, `content` = `World_PH_2800x1400`.
- `World_PH_2800x1400` neo giữa, pivot giữa, cỡ 2800×1400. `Background` kéo kín World; `Garden`, `Room`, `FloatText*` đặt lại sao cho **lúc mở Lobby, phòng nằm cách chỗ cũ 155 px sang phải trên màn 16:9** (World ở giữa, `normalizedPosition = (0.5, 0.5)`; user chốt 2026-11-11 sau review cuối đợt B: banner Gacha `(20, 340)` không che "!" Thu ngân ở 16:10 và 4:3, mép phải phòng còn cách thanh quán mở ở 16:10; cây và bụi hoa bên phải dời cùng). Vùng World ngoài màn có thêm vài cây và bụi (mục 5.6), tắt raycast.
- Cỡ 2800 (ảnh mô phỏng ghi "khoảng 2400"): canvas khớp theo chiều cao nên màn 20:9 rộng 2400 đơn vị; World 2400 thì máy 20:9 không kéo ngang được. 2800 cho 440 đơn vị mỗi bên ở 16:9 và 200 ở 20:9. Cao 1400 cho 160 trên dưới, đủ kéo hàng luống ra khỏi thanh dưới.
- `IncidentBubble`, người, đĩa nằm trong `Room` nên đi theo khi kéo. `RestaurantHud._floatOrigin` (quầy) cũng trong `Room`; `FloatText*` chuyển vào `World` để bay đúng chỗ khi đã kéo.
- Bấm "!" khi không kéo: click ăn như cũ (uGUI chỉ huỷ click khi ngón tay đi quá `EventSystem.pixelDragThreshold`). Builder để ngưỡng mặc định; chạm thử trên máy thật ở lần dừng playtest (mục 9).
- Mỗi lần `RestaurantView.OnEnable` (vào Lobby, nạp lại scene) **không** đặt lại vị trí kéo: nạp lại scene thì tự về giữa, đổi ô của thanh thì giữ chỗ đang xem.

### 5.3 Thanh tài nguyên chung

**Luật (UI, thuần):**

```csharp
[Flags] public enum ResourceSlots { None = 0, Stamina = 1, Coin = 2, Material = 4, Gem = 8, All = Stamina | Coin | Material | Gem }

public interface IResourcePage { ResourceSlots Slots { get; } }   // trang nào cần ô riêng thì cài; trang không cài = None

public static class ResourceBarRule
{
    // menuOpen → None. Có trang mở → hợp Slots của các trang đang mở (trang không cài IResourcePage tính None).
    // Không trang: Restaurant → All, Characters → Coin, Stages → Stamina, Gacha → Gem, ô khác → None.
    public static ResourceSlots For(HubTab current, bool menuOpen, IEnumerable<IHubPage> openPages);
    // Lobby (ô Quán, không trang, không Menu) dùng bố cục trái cạnh avatar; còn lại neo trên-phải.
    public static bool IsLobbyLayout(HubTab current, bool menuOpen, bool anyPageOpen);
}
```

| Màn | Ô | Cài ở |
|---|---|---|
| Lobby | Thể lực, Coin, Material, Gem | luật theo ô |
| Nhân vật | Coin | luật theo ô |
| Bản đồ | Thể lực | luật theo ô |
| Popup màn (`StageInfoPopup`), Chọn đội (`TeamSelectPopup`) | Thể lực | `IResourcePage` |
| Màn cốt truyện giữ chỗ (`StoryPlaceholderPopup`) | không | không cài |
| Gacha trang roll | Gem | luật theo ô |
| Gacha cutscene, lộ, tổng | ẩn (thanh ẩn cùng thanh dưới, mục dưới) | — |
| Nhân viên (`StaffPanel`), Chất lượng (`QualityPopup`) | không | không cài |
| Nâng cấp (`UpgradeTestPanel`) | Material | `IResourcePage` |
| Menu | không | `menuOpen` |
| Lịch sử quay, hộp xác nhận, AFK | (hộp che cả thanh) | — |

**Object:** `ResourceBar_PH_1120x76` (nền trắng bo góc), 4 ô `ResStamina_PH_280x76`, `ResCoin_PH_280x76`, `ResMaterial_PH_280x76`, `ResGem_PH_280x76` theo thứ tự đó, mỗi ô có `Text`; Thể lực, Coin, Gem có nút `ResPlus_PH_56x56` ở mép phải ô; Material không có.
- Bố cục Lobby: neo trên-trái, `(180, 20)`, rộng 1120, 4 ô liền nhau.
- Bố cục khác: neo trên-phải, cách mép phải 20, mép trên 20; chỉ bật ô cần, xếp từ phải sang (rộng = số ô × 280). Ngoài Lobby không màn nào quá 1 ô, nên không đè nút Quay lại (trên-trái) hay tiêu đề (giữa).
- `None` → tắt cả thanh.
- Ô Thể lực: dòng trên `combat.stamina_full` ("Thể lực 80/100"), dòng nhỏ dưới `hub.res_stamina_next` ("+1 sau 4:59") khi chưa đầy. Ô khác: "Coin 1.250", "Material 35", "Gem 1.500" theo cách `RestaurantHud` đang ghi (tên tiền giữ nguyên ở hai bản).

**`ResourceBarView` (MonoBehaviour):** builder gán `HubNav`, 4 ô, 3 nút +. `OnEnable` lấy `Wallet` và `StaminaService` từ `ServiceLocator` (thiếu thì `Debug.LogError` rồi thôi, như các view khác). `Update`: tính `ResourceBarRule.For(...)` từ `HubNav.State` và `HubNav` trang đang mở; bật/tắt ô, đặt bố cục; chỉ ghi `Text` khi số đổi (giữ số đã hiện, như `RestaurantHud._shownAfkCoins`). Nút + → `HubNav.ShowComingSoon()`.
- **Ẩn theo thanh:** `HubNav.SetBarHidden` (Gacha lộ) ẩn cả `ResourceBar` (cùng bộ chủ, không thêm bộ mới).
- `HubNav` thêm: `public void ShowComingSoon()` (đường "Sắp có" có sẵn của ô Cốt truyện tách ra) và `public IEnumerable<IHubPage> OpenPages` (lọc `_pages` đang mở).
- Bỏ: `CoinCard`, `MaterialCard`, `GemCard` và field tương ứng của `RestaurantHud`; thẻ `StaminaCard`, `MaterialCard`, `GemCard` của bản đồ và field của `StageMapView`; khối Gem của Gacha và `GachaView._gemText`. Tiêu đề chương bản đồ, tên banner Gacha, nút Lịch sử giữ chỗ.

### 5.4 HUD Lobby

| Object | Chỗ (từ trên-trái) | Hành vi |
|---|---|---|
| `Avatar_PH_140x140` (`Button`, `Knob`, vòng tròn trong nhạt hơn, không chữ) | `(20, 20)` | `HubNav.ShowComingSoon()` (Hồ sơ là đợt C) |
| `ResourceBar_PH_1120x76` | `(180, 20)` | mục 5.3 |
| `InfoBar` (giữ tên; đổi cỡ thành 800×68, một hàng) | `(180, 108)` | vẫn là `Button` mở `QualityPopup`. Trái: 5 sao 36 px; giữa: `IncomeText` ("≈ 1,6 Coin/giây", hay lý do khi bằng 0 kèm `WarningIcon`, như hiện nay); phải: `CounterText` ("Phục vụ 12 · Bỏ về 2 · Chất lượng 37/100", co chữ) |
| `AfkMessage` (giữ tên) | `(180, 186)`, rộng 800 | như cũ |
| Chỗ nút Thông báo | `(1790, 20)`, 110×100 | **không dựng gì** ở đợt B; test giữ vùng này trống nút |
| `BannerGacha_PH_360x200` (`Button`) | `(20, 340)` | chữ `hub.banner_gacha`; bấm → `HubNav.Select(HubTab.Gacha)` |
| `RestaurantBarToggle_PH_60x160`, `RestaurantBar_PH_180x460` | mục 5.5 | |
| `HubRoundBtnShop_PH_110x110` (`Button`, `Knob` xám, chữ `hub.shop`) + `ShopLock_PH_40x48` (ổ khoá như `Lock_PH_40x48`) | neo dưới-phải, cách mép phải 20, đáy cách mép dưới 140 | `HubNav.ShowComingSoon()` |

`LobbyHud` bật khi `ResourceBarRule.IsLobbyLayout(...)` (do `HubNav` đặt mỗi khi trạng thái đổi và khi trang mở/đóng; trang của quán mở từ chính nút trong `RestaurantBar` nên `HubNav` xét lại ở `Update`, rẻ).

### 5.5 Thanh quán thu gọn

- `RestaurantBarToggle_PH_60x160` (`Button`): neo giữa-phải, sát mép phải, tâm y 540. Chữ `"<"` khi đóng, `">"` khi mở (ký hiệu, không phải chữ ngôn ngữ). Con `RestaurantBarDot_PH_24x24` (`Knob` đỏ) ở góc trên.
- `RestaurantBar_PH_180x460`: neo giữa-phải, sát bên trái toggle, tâm y 540; nền trắng bo góc; chứa `StaffButton` (y −20), `UpgradeButton` (y −170), `AfkButton` (y −320), tròn 130, giữ tên và hành vi (`AfkButton` vẫn chỉ hiện khi có Coin AFK chờ, `RestaurantHud` lo). `RightColumn` bị bỏ.
- Đóng: `RestaurantBar` tắt (`SetActive(false)`). Mở: bật. Không hoạt hình.
- Chấm: bật khi **thanh đóng** và `AfkService.PreviewCollect() > 0`.
- `RestaurantBarView` (MonoBehaviour): `OnEnable` đọc `PlayerSave.RestaurantBarOpen` (qua `GameSession` trong `ServiceLocator`), áp trạng thái. Bấm toggle → đảo, ghi `PlayerSave.RestaurantBarOpen`, lưu qua đường lưu có sẵn của `GameSession` (như `LanguageSwitcher`). Lưu lỗi: thanh vẫn đổi trên màn, `Debug.LogWarning`, không hộp báo (cờ giao diện, lần lưu định kỳ kế tiếp ghi lại).
- **Save:** `PlayerSave` thêm `public bool RestaurantBarOpen = false;` ở cuối lớp. Save cũ thiếu field thì `JsonUtility` để `false` (đóng), không đổi `Version`. Reset (`ResetToDefaults`) tạo `PlayerSave` mới nên về `false` ([D-005](../../decisions/decisions.md)).

### 5.6 Quán 2.5D

Chỉ đổi `RestaurantSceneBuilder` (dựng hình) và `RestaurantView` (vẽ người, đĩa, bóng trạng thái). Lưới, `IsoProjection`, điểm mốc, `DepthKey`, luật quán giữ nguyên.

**Khối có chiều cao.** `Block(...)` thêm tham số `height` (px). Với `height > 0`, dưới mặt trên (`Image` như hiện nay, giữ tên `Kitchen`, `Counter`, `Table{i}`…) builder dựng hai anh em vẽ trước nó:
- `<tên>Side`: cùng cỡ, cùng góc xoay, dời xuống `height` px, màu mặt trên × 0,7. Phần lòi ra dưới mặt trên đọc như mặt trước của khối.
- `<tên>Shadow`: cùng cỡ và góc, dời `(10, −height − 8)`, đen alpha 0,18.
- Chiều cao: tường sau/trái 90 (thay dải sáng `lift 26` hiện có bằng mặt trên + mặt trước), bếp 44, quầy 40, bàn 22, luống 12, cửa và lối đi 0.
- Bếp có 3 nồi `Pot{i}` (`Knob` xám đậm 40 px) trên mặt; quầy có `Register` (hộp nhỏ). Chỉ để nhìn.
- Cây trong `Garden`: thân `Trunk` (chữ nhật nâu) + tán (`Knob` như cũ) + bóng elip. Thêm khoảng 10 cây/bụi ở vùng World ngoài màn (tổng trang trí vẫn tắt raycast).

**Người.** `CustomerTemplate` và `WorkerTemplate` đổi pivot về chân và có con:
- `Shadow`: `Knob` đen alpha 0,25, elip (rộng = cỡ người, cao 1/3).
- `Figure`: chứa `Body` (`UISprite` bo, cao ~1 cỡ người) và `Head` (`Knob`, ~0,75 cỡ người). Màu thân khách như hiện nay; nhân viên theo vai: Bếp cam, Phục vụ xanh, Thu ngân tím (3 màu hằng trong builder). `NameText` của nhân viên giữ dưới chân.
- `StatusBubble_PH_64x44` (chỉ khách): bóng trắng bo góc trên đầu, con `FoodDots` (`Text` "...") và `PayCoin` (`Knob` vàng).
- **Đi:** trong `RestaurantView.Move`, khi actor đang nội suy (chưa tới đích), `Figure` lấy tư thế `WalkPose.At(elapsed)`; tới đích thì `WalkPose.Idle`.

```csharp
public static class WalkPose   // UI, thuần
{
    public const float StepsPerSecond = 2f, LiftPx = 6f, SwayDegrees = 6f;
    // Mỗi bước: nhún lên LiftPx rồi về, nghiêng sang một bên SwayDegrees; bước sau nghiêng bên kia (như hai chân).
    public static (float Lift, float Angle) At(float seconds);   // Lift = LiftPx·|sin(π·s·StepsPerSecond)|, Angle = SwayDegrees·sin(π·s·StepsPerSecond)
    public static readonly (float Lift, float Angle) Idle = (0f, 0f);
}
```

Bóng không nhún, không nghiêng (chỉ `Figure`).

**Bóng trạng thái.**

```csharp
public enum CustomerBubbleKind { None, Food, Pay }
public static class CustomerBubble   // UI, thuần
{
    // WaitingForCook, Cooking, WaitingForServer, Serving → Food; WaitingForCashier, Paying → Pay; WaitingForTable, Eating → None.
    public static CustomerBubbleKind For(CustomerState state);
}
```

`RestaurantView` bật `StatusBubble` khi khác `None`, bật đúng một trong `FoodDots`/`PayCoin`.

**Đĩa món.** Mỗi `Table{i}` có con `Dish_PH_40x24` (`Knob` trắng viền xám, lúc đầu tắt), nằm trên mặt bàn. Mỗi khung `RestaurantView` bật đĩa theo `TableDishes.Occupied(customers, tableCount) → bool[]` (UI, thuần: bàn `i` `true` khi có khách `State == Eating` và `TableIndex == i`; chỉ số ngoài khoảng bỏ qua). Builder gán mảng `_dishes` (8 phần tử, đúng thứ tự `TableCells`).

### 5.7 Trang Nhân viên, Nâng cấp và hộp Lịch sử quay

Theo mô phỏng trang bản 1 (mục 10). Khung trang kín màn, nút Quay lại trên-trái, vạch 120 px như đợt A; không đổi luật.

**Nhân viên** (`StaffPanel`): 3 cột Bếp, Phục vụ, Thu ngân (tiêu đề cột giữ), mỗi cột 3 ô 500×220, cột tâm x 390 / 960 / 1530, hàng từ y 210 / 460 / 710 (đáy hàng cuối y 930, trên vạch).
- Ô có người: `Portrait_PH_140x140` (`Knob` màu theo độ hiếm), tên (`_slotTexts`, chữ 40), dòng phụ `"<độ hiếm> · <sao>★"` (`_slotInfoTexts` mới, ví dụ "SR · 3★"), `RemoveButton` 120×60 góc dưới-phải ô (giữ tên, nhãn `restaurant.remove`).
- Ô trống: chữ "Trống · bấm để chọn" như cũ, nền xanh viền sáng. Ô khoá: nền xám đậm, chữ gợi ý khoá như cũ (`StaffPanelRows.LockedHint`), không ảnh, không nút Gỡ.
- Danh sách Chọn người cho ô và hộp chuyển nhân viên giữ như đợt A.

**Nâng cấp** (`UpgradeTestPanel`): lưới 3 cột × 2 hàng thẻ 540×360, cột trái x 120 / 690 / 1260, hàng y 150 / 550 (đáy y 910). Thẻ `UpgradeCard` (template, nền xanh, **không** còn là `Button`) gồm: tên (chữ 42), "Cấp x/y" (`restaurant.level`), giá (giữ chữ giá hiện có, ví dụ "300 Coin + 5 Material", hay "Tối đa"), nút `BuyButton_PH_210x80` "Mua" (`restaurant.buy`) dưới-phải thẻ. Hết cấp hay thiếu tiền: nút vẫn bấm được và báo ở `MessageText` như hiện nay (không tắt nút). Hiện có 6 nâng cấp; thêm cái thứ 7 thì phải làm cuộn (shortcut ghi trong code).
- Tiêu đề trang đổi từ "Nâng cấp (bản thử)" thành "Nâng cấp" (`restaurant.upgrade_title`).

**Lịch sử quay** (`GachaHistoryPopup`): từ trang kín màn thành hộp. Root kéo kín màn, nền đen alpha 0,55 chặn bấm, qua `NavBarBuilder.OverBar` (vẽ trên thanh); hộp `GachaHistoryBox_PH_1200x800` giữa màn, tiêu đề, danh sách cuộn (`_listText` như cũ), `CloseButton` 300×100 "Đóng" (`common.close`) giữa đáy hộp. Vẫn cài `IHubPage` để Reset và đổi ngôn ngữ đóng được nó (L-035). Hộp miễn luật vạch như mọi hộp `OverBar`.

### 5.8 Mặc định tiếng Anh

- `GameLanguage.Resolve(string saved)`: `"vi"`/`"en"` thì theo lựa chọn; `""`, `null`, mã lạ → `Language.En`. Bỏ tham số `SystemLanguage` và `Func<SystemLanguage>` của `GameSession`; `GameBootstrap` thôi đọc `Application.systemLanguage`.
- Người đã có save mà chưa từng chọn (`Language == ""`) cũng thành tiếng Anh ở lần mở sau. Ai đã chọn tiếng Việt giữ tiếng Việt.
- Reset dữ liệu xoá lựa chọn → tiếng Anh.
- `GameLanguage.Current` mặc định `Vi` **trong Editor** giữ nguyên (test EditMode và builder thấy tiếng Việt, như comment hiện có); chỉ đường Boot/`GameSession` đổi.
- Sửa [D-012](../../decisions/decisions.md): `""` = mặc định tiếng Anh (thay "theo máy"); Reset về tiếng Anh.

### 5.9 Chữ mới (`UiText`, có bản Anh)

Mọi chữ mới vào bảng `UiText`, có bản tiếng Anh (nháp, user duyệt ở playtest). Key mới:

| Key | File | Việt | Anh |
|---|---|---|---|
| `hub.banner_gacha` | `UiText.Hub.cs` | Gacha\nQuay ngay | Gacha\nPull now |
| `hub.shop` | `UiText.Hub.cs` | Shop | Shop |
| `hub.res_stamina_next` | `UiText.Hub.cs` | +1 sau {0} | +1 in {0} |
| `restaurant.level` | `UiText.Restaurant.cs` | Cấp {0}/{1} | Level {0}/{1} |
| `restaurant.buy` | `UiText.Restaurant.cs` | Mua | Buy |

Đổi: `restaurant.upgrade_title` "Nâng cấp" / "Upgrade". Dùng lại: `combat.stamina_full`, `common.close`, `restaurant.remove`, `hub.coming_soon`. Ký hiệu `"<"`, `">"`, `"..."` và tên tiền "Coin", "Material", "Gem" không phải chữ ngôn ngữ (đang viết cứng như `RestaurantHud`); nếu `HardcodedTextTests` bắt thì đưa vào `UiText`. Nhãn builder dựng gắn `LocalizedText`; chữ view ghi lúc chạy thì không.

## 6. Luồng chạy

```
Vào Lobby: HubNav.Awake → LobbyHud bật, ResourceBar bố cục trái 4 ô, RestaurantBarView đọc PlayerSave.RestaurantBarOpen
Kéo quán: vuốt trên WorldScroll → World dời trong khung 2800×1400; thả tay → quán tính, chặn mép
Bấm ô Gacha: HubNav.Select → LobbyHud tắt → ResourceBar trên-phải chỉ Gem → Quay → SetBarHidden ẩn cả hai thanh → về trang roll hiện lại
Mở Nâng cấp từ thanh quán: LobbyHud tắt → ResourceBar trên-phải chỉ Material → Quay lại → Lobby
Bấm thanh quán: RestaurantBarView đảo → PlayerSave.RestaurantBarOpen → lưu
Avatar / Shop / nút + → HubNav.ShowComingSoon ("Sắp có")
Banner → HubNav.Select(Gacha)
Mở game lần đầu / Reset: GameLanguage.Resolve("") → En
```

## 7. Test

Nền **864 EditMode + 26 PlayMode**. Dự kiến **886 EditMode + 29 PlayMode** (kế hoạch chốt từng task; kế hoạch gộp ca, đích 884). Không `[TestCase]` (L-012). Test bố cục xét cả nút đang tắt (L-025, L-027). Test gọi `ProjectSetup.Run()`/`*SceneBuilder.Create` nạp lại asset sau lệnh gọi (L-032); scene sinh bằng code có test đọc lại từ file (L-002).

**EditMode:**

| Test | Bắt lỗi gì | Số |
|---|---|---|
| `ResourceBarRuleTests` | Lobby = All; mỗi ô đúng bảng 5.3; Menu → None; trang mở thắng ô (Nâng cấp → Material, Nhân viên → None, Chọn đội → Stamina); hai trang mở → hợp; `IsLobbyLayout` sai khi có trang hay Menu | +4 |
| `WalkPoseTests` | `At(0)` = Idle; nửa bước nghiêng một bên, bước sau nghiêng bên kia (dấu ngược); `Lift` luôn trong `[0, LiftPx]` | +2 |
| `CustomerBubbleTests` | Đủ 8 trạng thái đúng bảng (một test, lặp mảng) | +1 |
| `GameLanguageTests` (sửa, thêm) | `Resolve("")`, `Resolve(null)`, `Resolve("xx")` → En; `"vi"` → Vi | +1 |
| `GameSessionTests` (sửa, thêm) | Reset sau khi chọn Việt → En (thay ca "theo máy"); `RestaurantBarOpen` mặc định false, lưu rồi nạp lại giữ true, Reset về false | +2 |
| `LobbySceneTests` (thêm) | HUD Lobby đủ object đúng tên, cỡ, chỗ (5.4), trong canvas; vùng nút Thông báo trống nút; `ResourceBarView` đủ tham chiếu, 4 ô đúng thứ tự, Material không có nút +; `WorldScroll` có `ScrollRect` Clamped, không zoom, content `World_PH_2800x1400` chứa `Room`, `Garden`, `Background`, `FloatText*`; thanh quán đủ tham chiếu, chứa 3 nút cũ, `RightColumn` không còn; thứ tự vẽ 5.1 (World < LobbyHud < trang quán < panel < ResourceBar < Menu < NavBar) | +6 |
| `RestaurantSceneTests` (thêm, sửa) | Mỗi đồ có chiều cao có `Side` và `Shadow` vẽ trước mặt trên, dời xuống đúng `height`; 8 `Dish_PH_40x24` tắt, gán đúng thứ tự bàn; template người có `Shadow`, `Figure/Body`, `Figure/Head`, khách có `StatusBubble_PH_64x44`; ca cũ đọc `CoinCard`/`RightColumn` sửa theo tên mới, ca điểm mốc trong sàn giữ | +3 |
| `TableDishesTests` (mới) | `TableDishes.Occupied(customers, 8)`: khách `Eating` ở bàn 2 → chỉ phần tử 2 `true`; khách bàn 2 đang `WaitingForServer` → `false`; `TableIndex` −1 hay ≥ 8 bị bỏ qua | +1 |
| `LobbyPagesTests` (thêm, sửa) | Nhân viên: 9 ô đúng lưới, mọi nút trên vạch, ô có `Portrait_PH_140x140` và text phụ; Nâng cấp: template thẻ không phải `Button`, có `BuyButton_PH_210x80`, 6 thẻ vừa vùng trên vạch | +2 |
| `PanelPagesTests`, `LobbyPagesTests.Boxes_…` (sửa) | Lịch sử quay: bỏ ca trang kín màn, thêm vào ca hộp `OverBar` (nền kín màn, vẽ trên thanh, hộp 1200×800) | −1 |
| `StageSelectSceneTests`, `GachaSceneTests` (sửa) | Không còn thẻ tiền cũ, view không còn field tiền; panel vẫn đủ tham chiếu | +1 |
| `HardcodedTextTests`, `UiTextTests` (có sẵn) | Tự quét view và builder mới, key mới có bản Anh | 0 |

**PlayMode** (save tạm như `SmokeLoopTests`):

| Test | Kiểm gì |
|---|---|
| `Hub_WorldDrag_PansAndClamps_IncidentStillClicks` (mới) | Kéo bằng sự kiện `PointerEventData` giả: `World` dời; kéo quá mép thì dừng ở mép; ép một sự cố rồi bấm `IncidentBubble` (không kéo) → sự cố được xử lý |
| `Hub_RestaurantBar_RemembersAcrossReload` (mới) | Mặc định đóng; bấm toggle → mở; nạp lại `Restaurant` → vẫn mở; nạp lại save tạm thấy `RestaurantBarOpen == true`; Menu → Reset → Xoá hết → đóng |
| `Hub_ResourceBar_FollowsTabsAndPages` (mới) | Lobby 4 ô bố cục trái; Nhân vật chỉ Coin; Màn chơi chỉ Thể lực, mở Popup màn vẫn Thể lực; Gacha chỉ Gem, quay → thanh ẩn; về Quán → mở Nâng cấp → chỉ Material; Menu → tắt; avatar, Shop, nút + đều bật `ComingSoonToast` |
| `Hub_Screenshots_VietnameseAndEnglish` (sửa) | Thêm ảnh Lobby thanh quán mở, Lobby đã kéo, Nâng cấp, Nhân viên, hộp Lịch sử quay |
| Ca Reset/ngôn ngữ (sửa) | "Sau Reset phải theo máy" đổi thành "sau Reset là tiếng Anh" |

**Kiểm bằng mắt (dừng playtest cuối đợt B):** ảnh `D:/bdp_BunDauPeo/shots/hubb-<màn>.png` và `-en.png` (L-026 có đuôi): Lobby (thanh đóng, thanh mở, đã kéo sang trái và xuống), Nhân vật, Bản đồ, Gacha trang roll, Nâng cấp, Nhân viên, hộp Lịch sử quay, Menu. User so với mục 10. Playtest: mở game (tiếng Anh) → kéo quán bốn hướng → bấm "!" khi có sự cố → mở/đóng thanh quán, tắt mở lại game → bấm avatar, Shop, các nút + → banner sang Gacha → quay 10 (thanh ẩn) → xem Lịch sử quay → Nâng cấp mua một món → Nhân viên xếp một người → Menu đổi Tiếng Việt → Reset → game về tiếng Anh, thanh quán đóng. **Chạm thử trên điện thoại thật** (nếu đã nối) để xem ngưỡng kéo và bấm "!".

## 8. Docs phải sửa (khi xong, trong kế hoạch)

- [roadmap.md](../../roadmap.md): dòng "Đang làm" (📐 đặt khi viết spec, 🔨 cập nhật theo phiên); khi xong chuyển dòng đợt B sang "Đã xong".
- [project/structure.md](../../project/structure.md): `ResourceBarRule`, `ResourceBarView`, `RestaurantBarView`, `WalkPose`, `CustomerBubble`, `TableDishes`, `PlayerSave.RestaurantBarOpen`, số test.
- [D-012](../../decisions/decisions.md): mặc định tiếng Anh (mục 5.8).
- [unity-workflow.md](../../guides/unity-workflow.md#ui-và-hình-tạm): một câu: Lobby có `WorldScroll` (vùng kéo kín màn dưới HUD), nút mới của Lobby đặt trong `LobbyHud`, không đặt vào `World`.
- [game-design.md](../../design/game-design.md#quán): không đổi luật. Ngôn ngữ mặc định không thuộc game-design.
- [screen-flow.md](../../design/screen-flow.md) và [ideas.md](../../design/ideas.md) là file của 🎨: 📐/🔨 không sửa; nhắn 🎨 các điểm ở mục 11.

## 9. Rủi ro

| Rủi ro | Cách giữ |
|---|---|
| Ngưỡng kéo mặc định nhỏ so với màn điện thoại mật độ cao: chạm "!" bị tính là kéo, hoặc kéo nhẹ lại bấm nhầm | PlayMode kiểm bấm không kéo; chạm thử trên máy thật ở lần dừng playtest; cần thì đặt `pixelDragThreshold` theo `Screen.dpi` (việc nhỏ, ghi lesson) |
| Dời `Room`/`Garden` vào `World` làm lệch toạ độ test phòng, điểm mốc, bong bóng "+N" | Test điểm mốc giữ; `FloatText*` chuyển cùng `World`; ảnh Lobby lúc chưa kéo so với ảnh `hub-lobby.png` của đợt A |
| `Image` trong suốt của `WorldScroll` nuốt bấm của HUD | `WorldScroll` là lớp dưới cùng (5.1); test thứ tự vẽ |
| Nhiều `Image` hơn (mặt trước, bóng, người 4 phần) làm chậm máy yếu | Đồ tĩnh trong một canvas, không đổi mỗi khung; người tối đa khoảng 20; ảnh chụp và chơi thử trên máy thật |
| Trang Nâng cấp chỉ hiện Material trong khi giá còn Coin | User chọn (mục 1); giá Coin vẫn ghi trên thẻ; đổi giá là việc riêng (mục 11) |
| Mặc định tiếng Anh làm người chơi Việt cũ (chưa từng chọn) thấy đổi ngôn ngữ | User chốt; Menu → Ngôn ngữ đổi lại một chạm |
| Bản Anh dài ("Pull now", "Level 0/5") tràn ô | Ảnh tiếng Anh ở lần dừng playtest; rút gọn trong `UiText` |
| Lịch sử quay đổi thành hộp làm hỏng đường đóng khi Reset/đổi ngôn ngữ | Vẫn cài `IHubPage`, `CloseOpenPages()` đóng (L-035); ca hộp `OverBar` |

## 10. Ảnh mô phỏng đã duyệt

Vẽ và duyệt trong kênh 📐 UI hub đợt B ngày 2026-11-11 (ảnh nằm ngoài repo, chỉ ở hội thoại). Bản chép bố cục:

**Lobby bản 1** (user duyệt), thanh quán đang mở:

```
[Avatar] [Thể lực 80/100 (+1 sau 4:59) + | Coin 1.250 + | Material 35 | Gem 1.500 +]   [chỗ Thông báo, đợt C]
         [●●●○○  ≈ 1,6 Coin/giây                 Phục vụ 12 · Bỏ về 2]
[Banner Gacha     ]                                                 ┌──────────┐
[bấm → ô Gacha    ]   cây   ▛▀▀ tường sau có chiều cao ▀▀▀▀▀▀▀▀▄    │(Nhân viên)│┌─┐
                           ▌ quầy▄   [bếp: 3 nồi]▄▄▄▄▄▄▄         │(Nâng cấp) ││>│● chấm AFK
                           ▌  (người: bóng+thân+đầu, đung đưa)    │(Thu 320)  │└─┘
                           ▌  bàn▄ (đĩa)  bàn▄  bàn▄  "..." khách  └──────────┘
                           ▙▄▄▄[cửa]▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▌
                              luống · luống · luống                        (Shop 🔒)
[ Quán | Nhân vật | Cốt truyện 🔒 | Màn chơi | Gacha | Menu ]
```

**Trang bản 1** (user duyệt):

| Màn | Bố cục |
|---|---|
| Bản đồ (và panel khác) | thanh tiền neo trên-phải, chỉ ô của màn (Bản đồ: Thể lực kèm "+1 sau") |
| Nâng cấp | [Quay lại] trên-trái, tiêu đề giữa, thanh Material trên-phải; lưới 3 × 2 thẻ: tên, cấp, giá, [Mua] dưới-phải thẻ |
| Nhân viên | [Quay lại], tiêu đề, 3 cột Bếp/Phục vụ/Thu ngân, mỗi cột 3 ô to: ô có người (hình tròn, tên, "SR · 3★", [Bỏ]), ô trống viền sáng, ô khoá xám |
| Lịch sử quay | nền mờ che cả thanh, hộp tối viền sáng giữa màn (như các hộp Gacha khác; user chốt 2026-11-12, thay "hộp trắng"): tiêu đề, danh sách cuộn 50 lượt, [Đóng] |

## 11. Khác với luồng màn (báo 🎨)

📐 không sửa `screen-flow.md`/`ideas.md`; đã nhắn 🎨 các điểm này:
1. Thanh chất lượng giữ tên object cũ `InfoBar` (cỡ 800×68 như luồng màn), không đặt `QualityBar_PH_800x68`, vì test và `UiDriver` bấm theo tên cũ.
2. Lịch sử quay là **hộp** (luồng màn và đợt A: trang kín màn).
3. Luật tiền theo màn thêm: Popup màn và Chọn đội hiện Thể lực; **Nâng cấp quán hiện Material**; Lịch sử quay, Nhân viên, Chất lượng không hiện.
4. Hàng nút tròn chỉ dựng Shop; 4 ô online chưa dựng (luồng màn ghi "giữ chỗ"). Banner chưa có chấm trang (chỉ 1 banner). Nút Thông báo chỉ chừa chỗ.
5. Quán vẽ kiểu 2.5D (user yêu cầu 2026-11-11).
6. Vào game mặc định tiếng Anh (user 2026-11-11).
7. Sổ ý tưởng: user nói "nâng cấp nhân vật bằng Coin, quán bằng Material" (giá nâng cấp quán hiện là Coin + Material; đổi giá là việc cân bằng riêng, cùng câu "Coin dùng để nâng gì").
