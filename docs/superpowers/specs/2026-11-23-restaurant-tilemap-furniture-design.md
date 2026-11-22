# Spec: Vẽ lại quán bằng tilemap + Nội thất lưới 8×8

> Ngày: 2026-11-23 · Trạng thái: **user duyệt thiết kế và ảnh mô phỏng 2026-11-23**, chờ user duyệt bản viết · Phạm vi: màn quán (Lobby) dựng lại bằng Unity Tilemap + camera thế giới; hệ nội thất (mua, kho, đặt, xoay, cất); điểm trang trí trong Chất lượng quán; menu "Quản lý" gộp Nhân viên + Nâng cấp + Nội thất; `PlayerSave`
> Nguồn: [roadmap](../../roadmap.md) dòng Vẽ lại quán + Nội thất, [D-005](../../decisions/decisions.md), [spec Restaurant](2026-11-01-restaurant-design.md) (mục 4.4 chất lượng, 5.1 màn quán), [spec UI hub B](2026-11-11-ui-hub-b-design.md) (mục 5.2 khung kéo), [spec Online đợt 2](2026-11-16-online-2-design.md) (mục 1: quán, Coin do máy tính). Ảnh tham khảo: `ReferenceImages/restaurant-room-isometric-en.jpg`, `restaurant-layout-*`, `restaurant-furniture-*` ([ui-screens.md](../../references/ui-screens.md)). Kế hoạch: [restaurant-tilemap-furniture](../plans/2026-11-23-restaurant-tilemap-furniture.md).

## 1. Mục tiêu

Quán hiện là một hình chữ nhật xoay −11,3° dựng bằng `Image` trong Canvas. Spec này đổi hai thứ cùng lúc:
1. **Vẽ lại quán** thành sàn **hình thoi 8×8 ô** dựng bằng Unity Tilemap (Grid kiểu Isometric) nhìn bằng camera thế giới, có hai tường sau, vườn quanh phòng như bố cục 01/11.
2. **Nội thất:** người chơi mua nội thất trang trí bằng Coin, đặt, xoay, cất trên sàn. Bàn, dãy bếp, quầy thu ngân cũng nằm trên lưới và di chuyển, xoay được. Nội thất trang trí **cộng điểm vào Chất lượng quán** (chỉ ảnh hưởng AFK).

Thành công khi:
1. Màn quán là Tilemap sàn 8×8 (64 ô) + hai tường sau + vườn, nhìn bằng camera orthographic; HUD và popup vẫn là Canvas. Kéo màn là dời camera, có giới hạn.
2. Bàn (3 → 8 theo nâng cấp), bếp, quầy thu ngân đứng đúng ô theo bố cục đã lưu; khách và nhân viên đi tới đúng chỗ. **`RestaurantSimulation`, `IncomeCalculator`, `AfkService`, `IncidentDirector` không đổi** (mô phỏng không có toạ độ).
3. Mua nội thất bằng Coin ở Cửa hàng; Kho liệt kê số lượng; đặt, dời, xoay, cất trên sàn; không đặt được chỗ chồng, ngoài sàn hoặc trong sảnh cửa. Mọi thay đổi lưu ngay, tắt mở app còn nguyên.
4. Chất lượng quán = Chuyên cần 40 + Xử lý sự cố 40 + **Trang trí 20** (mục 4). Hệ số AFK đổi theo; tiền khi chơi thật không đổi.
5. Nút "Quản lý" mở menu nổi gồm Nhân viên, Nâng cấp, Cửa hàng, Kho (và Cất tất cả ở mức đầy đủ, mục 9); cột phải chỉ còn Quản lý + Thu AFK.
6. Reset Save Data xoá bố cục, kho và nội thất đã mua (D-005). Server và luật tiền không đổi.
7. Cảnh quán cũ không bị xoá: lưu thành prefab vào `_Old/` cạnh nó, không còn scene/prefab đang dùng trỏ vào (mục 5.3).

**Ngoài phạm vi (anti-goal):** bán nội thất, Bộ cài sẵn (có ở mức đầy đủ, mục 9), nhiều tầng, nội thất treo tường, nội thất có hiệu ứng sản xuất, nhân vật đứng trang trí, tìm đường cho khách (khách vẫn đi thẳng), nội thất đổi luật chơi (số chỗ, thu nhập), nội thất mua bằng Gem hay Material, thưởng khi đặt đúng chỗ.

## 2. Quyết định đã chốt với user

| # | Câu hỏi | Chốt |
|---|---|---|
| 1 | Nội thất làm gì | **Ảnh hưởng quán, có ở bản 31/12** (2026-11-21). Hướng A: thêm phần **Trang trí** vào Chất lượng quán, chỉ làm đổi hệ số AFK (2026-11-21) |
| 2 | Cách tính điểm | Mỗi món có `DecorPoints`; điểm = tổng của món **đang đặt trên sàn**, trần 20; đặt ở đâu hay xoay thế nào cũng như nhau; cất kho thì mất điểm (2026-11-21) |
| 3 | Lưới 8×8 | **Cả sàn**: bàn, bếp, thu ngân cũng di chuyển được (2026-11-21) |
| 4 | Lấy nội thất | **Mua bằng Coin** (2026-11-21) |
| 5 | Cách dựng | **Tilemap thật** + camera thế giới + HUD Canvas (phương án 1, 2026-11-21) |
| 6 | Hình sàn | **Hình thoi** (user sửa 2026-11-21): **đổi lại quyết định 2026-11-01 "thay phòng isometric"**. Giữ vườn, HUD, cột nút của bố cục 01/11; mẫu hình thoi là `restaurant-room-isometric-en.jpg` |
| 7 | Cách đặt món | Theo các popup của `restaurant-furniture-*` (ảnh user gửi 2026-11-22): Cửa hàng với **Mua cất kho / Mua và đặt**, Kho với **Đặt**, Cất tất cả có xác nhận; đặt bằng **kéo trên sàn + ba nút nổi Xoay / Cất kho / Xong**, không có khay cố định. **Duyệt 2026-11-23** |
| 8 | Nút ngoài màn | **Một nút "Quản lý"** mở menu chứa Nhân viên, Nâng cấp và các mục Nội thất, vì cùng mục đích tăng điểm quán (2026-11-23). Thu AFK ở ngoài |
| 9 | Ảnh mô phỏng | Duyệt 2026-11-23: bốn khung (Lobby + menu nổi, Cửa hàng, Kho, chế độ đặt); mục 6 |

**Lựa chọn của 🎨 (user chưa xét riêng, sửa được):**
- Kích thước (cột × hàng, trước khi xoay): bếp 3×1, thu ngân 2×1, bàn 2×1, chậu cây 1×1, kệ 2×1. Xoay chỉ 0° hoặc 90°.
- Số liệu khởi điểm: chậu cây **3 điểm, 200 Coin**; kệ **5 điểm, 450 Coin**; mỗi món mua tối đa **5** lần; trần trang trí **20**; chất lượng chia lại **40 / 40 / 20**. Tất cả đặt trong config/định nghĩa, chỉnh trong Inspector.
- Nội thất chức năng (bàn, bếp, thu ngân) **không cất vào kho** và không có điểm trang trí; số bàn theo nâng cấp Bàn như cũ.
- Cửa cố định ở tường sau-trái. **Sảnh cửa** (4 ô `(0,5) (0,6) (1,5) (1,6)`) không đặt được món nào.
- Menu "Quản lý" có nhãn ngắn "Quản lý"; Bộ cài sẵn **không** có ở mức tối thiểu.
- Chạm một món trên sàn (không kéo) khi chưa ở chế độ đặt thì **vào chế độ đặt và chọn món đó**; kéo ra ngoài món thì kéo camera.
- Khi ở chế độ đặt: quán vẫn chạy, dấu "!" sự cố ẩn đi (sự cố vẫn tính giờ như đang ở trang Nhân viên, [game-design](../../design/game-design.md#quán)), cột nút phải ẩn; thoát khi bấm Xong, mở trang khác của hub, hoặc rời Lobby.
- Mua "Mua và đặt" khi sàn hết chỗ cho món: món vào kho (như "Mua cất kho") và báo "Sàn hết chỗ".
- Camera: thấy trọn sàn + tường ở độ phóng mặc định; vuốt để dời trong giới hạn (mục 5.2). Zoom hai ngón chỉ ở mức đầy đủ.

## 3. Lưới và luật đặt

**Toạ độ ô** `(c, r)`, `c, r ∈ [0, 8)`. `c` đi xuống-phải, `r` đi xuống-trái; góc `(0,0)` là đỉnh trên của hình thoi. Cạnh `r = 0` là tường sau-phải, cạnh `c = 0` là tường sau-trái. Mỗi món có **neo** là ô nhỏ nhất `(c, r)` của dấu chân.

**Dấu chân:** `w × h` ô khi xoay 0° (w theo `c`, h theo `r`); 90° thì `h × w`. Điểm `(x, y)` trong dấu chân (đơn vị ô, từ neo) sau xoay 90°: `(x, y) → (y, w − x)` (với `w` là bề rộng gốc). Dùng cùng công thức cho các điểm đứng (spot).

**Hợp lệ** (`FurnitureLayout.CanPlace`): mọi ô của dấu chân nằm trong sàn; không ô nào thuộc dấu chân món khác (chức năng hay trang trí); không ô nào thuộc sảnh cửa. Kiểm lại sau xoay; xoay không hợp lệ thì giữ nguyên (nút Xoay vẫn bấm được nhưng món rung và không đổi, ô đỏ).

**Bố cục mặc định** (khởi điểm; save trống hoặc thiếu món nào thì món đó lấy ô mặc định):

| Món | Neo `(c, r)` | Xoay | Ghi chú |
|---|---|---|---|
| Bếp | (2, 0) | 0° | sát tường sau-phải, dấu chân `(2..4, 0)` |
| Thu ngân | (0, 2) | 90° | sát tường sau-trái, dấu chân `(0, 2..3)` |
| Bàn 1…8 | (3,3) (6,3) (3,5) (6,5) (3,7) (6,7) (6,1) (0,0) | 0° | bàn n chỉ có khi đã nâng đủ số bàn; thứ tự cố định |
| Cửa | cạnh `c = 0`, `r ∈ [5, 6.5]` | — | cố định, vẽ trên tường |

**Điểm đứng** (spot, view dùng để dời người; toạ độ trong dấu chân gốc, xoay cùng món):
- Bếp, 3 ô nấu: `(0.5, 1.5) (1.5, 1.5) (2.5, 1.5)` (mặt trước, phía `+r`). 3 ô phục vụ: `(3.5, 0.5) (3.5, 1.5) (4.5, 0.5)` (cạnh đầu bếp).
- Thu ngân, 3 ô: `(0.5, 1.5) (1.5, 1.5) (1.0, 2.5)`.
- Bàn: khách đứng/ngồi ở tâm dấu chân; đĩa món nằm trên mặt bàn (như hiện nay).
- Cửa: khách xuất hiện ở `(0.5, 5.5)` và đi thẳng tới điểm đích (1,5 giây như hiện nay, không tìm đường).

Điểm đứng chỉ là điểm hiển thị; nó có thể nằm ngoài dấu chân nhưng bộ test mục 8 giữ cho **bố cục mặc định** không có điểm đứng nào nằm trong dấu chân món khác. Người chơi xếp chồng chỗ đứng với đồ thì chỉ xấu, không ảnh hưởng thu nhập.

**Khi dữ liệu không hợp lệ** (`Resolve`, lúc nạp và mỗi lần số bàn đổi): món chức năng có vị trí lưu không hợp lệ (ngoài sàn, chồng, sảnh cửa; sửa file hoặc đổi cấu hình) thì lấy ô mặc định, rồi nếu vẫn chồng thì ô trống đầu tiên quét theo hàng (r, rồi c) vừa dấu chân. Món trang trí không hợp lệ thì về kho (không mất). Không còn ô vừa cho món chức năng thì cất một món trang trí (đặt gần nhất) về kho rồi thử lại; vẫn không được thì báo lỗi log và dùng ô mặc định (không xảy ra ở cấu hình khởi điểm: tối đa 8 bàn × 2 + bếp 3 + thu ngân 2 + trang trí tối đa 15 (5 chậu + 5 kệ) = 36 ô trên 60 ô dùng được).

## 4. Điểm trang trí và Chất lượng quán

- `DecorPoints` của một món nằm trong `FurnitureDefinition`. **Điểm trang trí** = `min(DecorMax, Σ DecorPoints của mọi món trang trí đang đặt)`, `DecorMax = 20` (khởi điểm, `RestaurantConfig`).
- [Chất lượng quán](../../design/game-design.md#quán) (spec Restaurant 4.4) đổi từ hai phần 50 + 50 thành ba phần: `AttendanceMax = 40`, `IncidentMax = 40`, `DecorMax = 20`, **tổng luôn 100** (`RestaurantConfig.Validate` kiểm). `RestaurantQuality.Total = Attendance + IncidentPoints + Decor`. 20 điểm vẫn là 1 sao; hệ số AFK = Lerp(0,1 → 0,5, Total / 100) như cũ.
- Hệ số chất lượng tính **lúc thu thập** (luật AFK cũ), nên cất hay đặt nội thất có hiệu lực ở lần thu AFK kế; không có hiệu lực hồi tố.
- `QualityPopup` (bấm `InfoBar`) thêm dòng "Trang trí: x / 20" cạnh Chuyên cần và Xử lý sự cố; có thể xem điểm từng món trong Kho (mục 6).
- Ảnh hưởng lên người chơi hiện hữu: save cũ có Chuyên cần/Sự cố được tính trên thang 40 thay 50, nên cùng thành tích cho **ít điểm hơn tối đa 20%** cho tới khi có nội thất. Chấp nhận (user chọn hướng A); `docs` ghi rõ ở game-design.
- Config trong Inspector đang giữ `50/50`; `ProjectSetup` chỉ tạo asset khi còn thiếu, nên cần **một bước đổi giá trị trên asset đã có** ở Task 2 (mục 7.3).

## 5. Hiển thị

### 5.1 Cấu trúc scene

`RestaurantSceneBuilder` **dựng lại từ đầu** (không sửa trên bản cũ). Hai phần:

- **Thế giới** (camera orthographic riêng, màu nền xanh cỏ):
  - `Grid` (Cell Layout Isometric, cell size `(1, 0.5, 1)`) chứa Tilemap `Floor` (64 ô, caro hai màu), `WallBackLeft`, `WallBackRight` (dải tường cao, vẽ bằng ô hoặc `SpriteRenderer`), `Door`.
  - `Furniture`: mỗi món là một `GameObject` có `SpriteRenderer` (mặt trên hình thoi theo dấu chân + hai mặt bên khi có chiều cao). Tên `<Tên>_PH_<rộng>x<cao>`; bếp, bàn, thu ngân giữ tên cũ (`Kitchen`, `Counter`, `Table{i}`) vì `RestaurantView` và test bám tên.
  - `Actors` (khách, nhân viên, cây và bụi vườn bằng `Knob`), thứ tự vẽ theo trục y của camera (`transparencySortMode = CustomAxis (0,1,0)` hoặc `sortingOrder` theo y), không theo thứ tự tạo.
  - Vườn quanh phòng, bụi hoa, luống: `SpriteRenderer` Knob/UISprite, chỉ để nhìn.
- **HUD** (Canvas Screen Space Overlay như hiện nay): `LobbyHud` (thẻ tiền, `InfoBar`, `RestaurantBar` → cột "Quản lý" + Thu AFK), popup, trang kín màn, thanh dưới hub. Nút mới nằm trong `LobbyHud` (luật vạch 120 px và nút Thông báo vẫn áp dụng).
- Tile tạm: `Tile` asset (`.asset`, không phải ảnh) dùng sprite có sẵn của Unity (`UISprite.psd`/`Knob.psd`), tô màu bằng `Tile.color`; để vuông thành thoi dùng `Tile.transform` (xoay 45°, co trục 0,5) hoặc cách nào 🔨 thấy chạy được. **Nếu cách này không cho ra hình thoi đúng, 🔨 dừng và báo 🧭** để xin ảnh ô thoi từ user (đặt `ReferenceImages/restaurant-tile-*`), không tự thêm ảnh.

### 5.2 Camera

- Mặc định thấy trọn sàn, hai tường và vườn gần (mức phóng `Config.CameraSize`, khởi điểm 3,4 đơn vị; 🔨 chỉnh khi nhìn).
- Vuốt/kéo trên chỗ không có món (hoặc đang kéo món chưa chọn) dời camera; kẹp để tâm camera nằm trong hộp quanh sàn (`PanLimit`, khởi điểm ±3 đơn vị). Không quán tính ở mức tối thiểu.
- Thay `WorldScroll` (ScrollRect) + `World_PH_2800x1400` cũ; `WorldFocus` (đang dùng để tâm màn vào trạm sự cố) đổi thành tính vị trí camera (xem task 3).
- Mức đầy đủ: zoom hai ngón trong `[ZoomMin, ZoomMax]` (khởi điểm 2,4 – 4,6).

### 5.3 Cất cảnh cũ vào `_Old/` (user chốt 2026-11-21)

Quán cũ không có file ảnh (toàn `Image` dựng bằng code), nên "hình cũ" là **các object phòng trong `Restaurant.unity`**. Theo [roles/designer.md](../../studio/roles/designer.md) mục "Hình placeholder và thay ảnh":
1. **Trước khi dựng lại scene**, 🔨 mở `Restaurant.unity` hiện có và lưu thành prefab (bằng Unity, `PrefabUtility.SaveAsPrefabAsset`, để `.meta` đi theo) vào `Assets/_Project/Restaurant/Prefabs/_Old/`: `RoomOld.prefab` (Room: sàn, tường, Furniture, Crops, nhân vật mẫu nếu có) và `GardenOld.prefab` (Garden). Chỉ phần hình; không kéo theo `RestaurantView`, `RestaurantHud`, popup, nút.
2. Dựng lại scene từ builder mới; scene mới không còn object cũ và **không tham chiếu `_Old/`** (test kiểm bằng `AssetDatabase.GetDependencies`, mục 8).
3. Không xoá file; code cũ của builder (`Block`, `Wall`, `BuildGarden`…) và `IsoProjection` được thay trong git (lịch sử giữ lại), không cần `_Old/`.
4. Báo cáo cuối kênh của 🔨 ghi rõ đã cất gì vào `_Old/`.

## 6. UI

Ảnh mô phỏng bốn khung user duyệt 2026-11-23 (ảnh nằm ngoài repo, [unity-workflow](../../guides/unity-workflow.md) mục UI và hình tạm). Hình chỉ là `Image`/`SpriteRenderer` có sẵn.

| Vùng | Tên object | Ghi chú |
|---|---|---|
| Nút gộp | `ManageButton_PH_130x130` | Thay `StaffButton`, `UpgradeButton` trong `RestaurantBar` (cột phải). Cột phải còn **Quản lý** + **Thu AFK** (chỉ hiện khi có Coin chờ). Chấm báo trên nút Quản lý khi có việc cần chú ý (thiếu người: `⚠`) là mức đầy đủ |
| Menu nổi | `ManageMenu_PH_` (lưới 3 cột, bên trái cột nút) | Mở khi bấm Quản lý, đóng khi bấm ngoài hoặc bấm lại. Chứa `StaffButton`, `UpgradeButton` (**giữ tên và hành vi cũ**: mở `StaffPanelRoot`, `UpgradePanelRoot`), `FurnitureShopButton`, `FurnitureStorageButton`, và `FurnitureStoreAllButton` (từ mức đầy đủ). Ô trống của lưới ẩn. Nút trong menu đóng menu rồi mở trang |
| Cửa hàng | `FurnitureShopRoot` (hộp giữa 1200×800, theo mẫu `restaurant-furniture-shop.jpg`) | Thẻ món: hình tạm, tên, "+N điểm · sở hữu x/5", giá Coin. Hai nút **Mua cất kho** / **Mua và đặt** (xám khi thiếu Coin hoặc sở hữu đủ 5). Chân hộp: "Đã đặt: n · Trang trí x/20", số Coin, **Đóng**. Không có tab ở bản 2 món; có tab khi thêm món (mức đầy đủ) |
| Kho | `FurnitureStorageRoot` (theo `restaurant-furniture-storage.jpg`) | Thẻ món: hình, tên, "+N điểm · ×m trong kho" (m = sở hữu − đang đặt). Nút **Đặt** (xám khi m = 0). Chân hộp như Cửa hàng |
| Cất tất cả | `FurnitureStoreAllConfirm` (hộp xác nhận giữ cỡ, vẽ trên thanh dưới như `StaffConfirmPopup`) | Theo `restaurant-furniture-bulk-store.jpg`: "Cất mọi nội thất trang trí về kho? Đã đặt n". Cảnh báo "Không hoàn tác được. Bàn, bếp, thu ngân ở lại sàn." Nút Huỷ / OK. Không bấm được khi n = 0 |
| Chế độ đặt | `PlaceControls_PH` (neo trên món chọn) | Ba nút **Xoay**, **Cất kho** (ẩn với món chức năng), **Xong**. Ô dưới món tô xanh (hợp lệ) hoặc đỏ (không). Viền vàng quanh món chọn. Thanh trên hiện "Trang trí x/20 · ≈ +N nếu đặt" thay `InfoBar`. Cột phải và dấu "!" ẩn. Nút chuyển theo món khi camera dời (neo bằng `Camera.WorldToScreenPoint`) |

Chữ qua `UiText` (khu mới `furniture`, hai ngôn ngữ, [D-010](../../decisions/decisions.md)); chữ panel DEV tiếng Việt cố định. Tên món (`DisplayName`) ở `FurnitureDefinition`, hai ngôn ngữ như `CharacterDefinition`. Hộp, nút xếp theo luật vạch 120 px của thanh dưới và vùng nút Thông báo (có test sẵn, [unity-workflow](../../guides/unity-workflow.md)).

**Luồng đặt:**
1. Kho → **Đặt** (hoặc Cửa hàng → **Mua và đặt**): popup đóng, món xuất hiện ở ô hợp lệ gần tâm sàn nhất, vào chế độ đặt, món đang chọn.
2. **Kéo** món: dấu chân bám lưới theo ô, ô xanh/đỏ cập nhật; thả ở ô đỏ thì món **về ô hợp lệ trước đó**, không bao giờ lưu trạng thái không hợp lệ. Thả ở ô xanh thì lưu ngay.
3. **Xoay**: đổi 0° ↔ 90° quanh neo; **Cất kho**: món về kho (mất điểm); **Xong**: thoát chế độ đặt.
4. Chạm món khác khi đang đặt thì chọn món đó. Chạm sàn trống thì bỏ chọn (vẫn trong chế độ đặt; Xong để thoát).

## 7. Dữ liệu, D-005, server

### 7.1 Các phương án đã cân (nơi giữ bố cục nội thất)

| | Cách | Kết luận |
|---|---|---|
| **A. Máy, `PlayerSave.Furniture`** | Dịch vụ thuần C# trong Restaurant; Coin trừ qua `Wallet` | **Chọn.** Coin, AFK, chất lượng do máy tính (spec Online đợt 2 mục 1), nên cùng chỗ. Không đổi server |
| B. Server (`ServerState`) | Lệnh Cloud Code mua/đặt | Loại: mỗi lần kéo món phải gọi mạng; chặn màn bằng `WaitOverlay`; luật tiền (Coin) vẫn ở máy nên server không bảo vệ được gì |
| C. File riêng ngoài save | `PlayerPrefs`/JSON | Loại: trái D-005 (dữ liệu chỉ đi qua `PlayerSave`/`SaveService`) |

Sửa file save để có nội thất miễn phí thì được (không hại ai; chỉ tăng hệ số AFK ≤ 20 điểm/100 của chính người đó). Server không kiểm.

### 7.2 `PlayerSave.Furniture` (D-005)

```csharp
[Serializable] public struct FurniturePlacement { public string DefId; public int Index; public int Col; public int Row; public int Rotation; } // Rotation 0 hoặc 1 (×90°)
[Serializable] public struct FurnitureOwned { public string DefId; public int Count; }                                                   // tổng đã mua (đặt + kho)
[Serializable] public class FurnitureState   // Core/Scripts/FurnitureState.cs
{
    public List<FurniturePlacement> Placed = new();  // Index = thứ tự bàn (0..n-1) hoặc số thứ tự của món cùng loại
    public List<FurnitureOwned> Owned = new();
}
// PlayerSave: public FurnitureState Furniture = new();
```

| Luật D-005 | Cách đáp ứng |
|---|---|
| Dữ liệu cần lưu chỉ ở `PlayerSave`, đi qua `SaveService` | `Furniture` là field của `PlayerSave`. Mỗi thao tác (mua, đặt, dời, xoay, cất) gọi `persist` như `UpgradeService` |
| Field mới có giá trị game mới đúng | `new FurnitureState()` = không món trang trí, không vị trí → bố cục mặc định mục 3. Save cũ không có field vẫn nạp được |
| Trạng thái chạy ngoài save tự reset | `FurnitureService` không giữ bản sao `Placed`/`Owned`: đọc `_save.Furniture` mỗi lần. Chế độ đặt (món đang chọn) là trạng thái view, `ResetRuntime()` thoát chế độ đặt và phát `Changed` |
| Online (D-014, đợt 2) | `Furniture` thuộc khoá `save` do máy giữ, không ở `ServerState`; đi theo bản sao `save` tải lên. `ResetPlayer` trên server không đổi |
| Reset Save Data | `ResetToDefaults()` ghi đè `PlayerSave` nên xoá `Furniture`, nội thất đã mua và vị trí; vẫn hỏi xác nhận như cũ |

Hai máy cùng tài khoản: theo `ActiveSessionId` sẵn có, không xử lý riêng.

### 7.3 Config và định nghĩa

- `FurnitureDefinition : ScriptableObject` (`Restaurant/Data/Furniture_<Id>.asset`): `Id`, `DisplayName` (vi/en), `Kind` (`Functional`: Table/Kitchen/Cashier, hoặc `Decor`), `Width`, `Height`, `DecorPoints`, `CoinCost`, `MaxOwned`, `Tint`, `Height` (cao tạm, px thế giới). `FurnitureCatalog` liệt kê; `RestaurantConfig` tham chiếu catalog.
- Chức năng (bàn/bếp/thu ngân) có định nghĩa nhưng `CoinCost = 0`, `MaxOwned = 0`, không hiện ở Cửa hàng/Kho.
- `RestaurantConfig` thêm Header "Nội thất": `DecorMax` (20), `CameraSize`, `PanLimit`, danh sách ô mặc định, sảnh cửa; sửa `AttendanceMax = 40`, `IncidentMax = 40` và `Validate` (tổng ba phần = 100). Vì `ProjectSetup` chỉ tạo asset còn thiếu, thêm một bước **chuyển một lần**: nếu asset còn `50/50` thì đặt `40/40/20`; số user đã chỉnh khác `50/50` được giữ và báo cảnh báo "tổng chất lượng ≠ 100".

## 8. Kiểm thử

**EditMode (ước, theo kế hoạch):**
- `FurnitureLayoutTests`: dấu chân và xoay (công thức mục 3), `CanPlace` (ngoài sàn, chồng, sảnh cửa), `Resolve` (dữ liệu hỏng, đổi số bàn, hết chỗ), bố cục mặc định không chồng và mọi điểm đứng nằm trong sàn, không nằm trong dấu chân món khác.
- `GridMathTests`: `CellToWorld`/`WorldToCell` khứ hồi, thứ tự vẽ.
- `FurnitureServiceTests`: mua (đủ/thiếu Coin, đủ 5, id lạ), đặt/dời/xoay/cất, "Mua và đặt" hết chỗ, `persist` thất bại, cất tất cả, điểm trang trí (trần 20, cất thì giảm).
- `RestaurantQualityTests` (sửa): tổng 40/40/20; hệ số AFK theo trang trí. `RestaurantConfigTests` (sửa): validate tổng 100. `SaveCompatibilityTests`: save cũ nạp ra bố cục mặc định. `ResetSaveDataTests` + `SaveServiceTests`: reset xoá `Furniture`.
- Scene: Tilemap `Floor` đủ 64 ô; hai tường; camera orthographic; menu `ManageMenu` có đủ nút và tên cũ `StaffButton`/`UpgradeButton`; không `Button` nào giao vạch 120 px; **scene không phụ thuộc asset trong `_Old/`**; nút đúng tên; tests cũ về hình quán (`IsoProjectionTests`, `RestaurantScene_LandmarksAreInsideFloor`, `RestaurantScene_FurnitureDoesNotOverlap`, `Blocks_HaveSideAndShadow`, `WorldFocusTests`…) viết lại theo ô.
- `UiTextTests`: khoá `furniture.*` có cả hai ngôn ngữ.

**PlayMode (smoke, thêm vào `SmokeLoopTests`):** Quản lý → Cửa hàng → Mua và đặt chậu cây → Xong → điểm trang trí tăng; các bước cũ mở `StaffButton`/`UpgradeButton` thêm một cú bấm `ManageButton` trước.

Số ước: **+60 EditMode, +2 PlayMode** (🔨 chốt lại ở Task 1).

**Thủ công (playtest cuối):** kéo/xoay mượt trên thiết bị; không đè nút; camera không văng ra ngoài; Reset xoá nội thất; đo Profiler theo [performance.md](../../studio/performance.md) (quán giờ là Tilemap + SpriteRenderer, không cấp phát mỗi khung: L-044).

## 9. Mức tối thiểu và đầy đủ (cắt khi trễ)

| | Gồm |
|---|---|
| **Tối thiểu** (Task 1–4) | Dữ liệu + luật lưới + dịch vụ + điểm trang trí + chất lượng 40/40/20 + cảnh cũ vào `_Old/` + quán Tilemap hình thoi + camera kéo + Quản lý/Cửa hàng/Kho/chế độ đặt (Xoay, Xong, Cất kho) + 2 món |
| **Đầy đủ** (Task 5–6) | Cất tất cả (hộp xác nhận), zoom hai ngón, thẻ DEV (tặng nội thất, xoá), chấm báo ở Quản lý, Bộ cài sẵn (lưu/áp dụng bố cục, theo `restaurant-furniture-myset.jpg`) |

Thứ tự cắt khi trễ: **Bộ cài sẵn → zoom → chấm báo → thẻ DEV → hộp Cất tất cả** (cất từng món bằng nút vẫn có). Không cắt chế độ đặt và camera (đó là lõi). Luồng Nội dung game cắt Shipper trước (user chốt 2026-11-21).

## 10. Rủi ro và việc 🔨 quyết

- **Tile hình thoi từ hình vuông có sẵn** (mục 5.1): spike đầu Task 3; không ra thì xin ảnh user.
- **Chồng PlayerSave/ProjectSetup/RestaurantSceneBuilder với Thời tiết và Cốt truyện:** Thời tiết thêm `WeatherBadge`/`WeatherTint` vào builder cũ. 🔨 làm **sau** khi Thời tiết và Cốt truyện đã merge, rồi chuyển hai object đó sang builder mới (lớp phủ vẫn kín khung quán; thẻ ở cạnh `InfoBar`).
- **Kéo món vs kéo camera** trên cảm ứng: ngưỡng kéo 12 px; test PlayMode không mô phỏng cử chỉ, nên kiểm tay trên máy.
- **Khách đi thẳng xuyên đồ**: hiện chấp nhận, ghi trong game-design; tìm đường để sau.
- **Hiệu năng:** SpriteRenderer + Tilemap batching; nội thất tối đa ~40 object. Đo Profiler ở phiên cuối.
- **Thứ tự vẽ:** nếu `CustomAxis` gây sai thứ tự giữa khách và bàn, dùng `sortingOrder` cập nhật theo y trong `LateUpdate` (không cấp phát).

## 11. Đã làm trong lúc viết spec (🎨)

- Thêm 7 ảnh `ReferenceImages/restaurant-furniture-*` và `restaurant-character-placement.jpg`, sửa dòng `restaurant-room-isometric-en.jpg` trong [ui-screens.md](../../references/ui-screens.md).
- Cập nhật [roadmap](../../roadmap.md), [game-design](../../design/game-design.md), [screen-flow](../../design/screen-flow.md), [ideas](../../design/ideas.md) theo mục trên.
