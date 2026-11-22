# Vẽ lại quán bằng tilemap + Nội thất: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (mặc định, native) hoặc superpowers:subagent-driven-development để làm kế hoạch này từng task (cách thực thi do 🧭 Leader chọn, xem [agent-routing.md](../../studio/agent-routing.md)). Các bước dùng cú pháp checkbox (`- [ ]`) để theo dõi; tick khi xong.

**Goal:** Màn quán là sàn hình thoi 8×8 dựng bằng Unity Tilemap + camera thế giới; người chơi mua nội thất bằng Coin, đặt/xoay/cất trên lưới, bàn–bếp–thu ngân cũng di chuyển được; nội thất trang trí cộng điểm vào Chất lượng quán (40/40/20); nút "Quản lý" gộp Nhân viên + Nâng cấp + Nội thất; cảnh quán cũ cất vào `_Old/`.

**Architecture:** Logic thuần C# trong `Restaurant` (`FurnitureLayout` luật lưới, `FurnitureService` mua/đặt/cất, đọc `PlayerSave.Furniture` mỗi lần, như `UpgradeService`); `RestaurantQuality` thêm phần Trang trí. Hiển thị: `RestaurantSceneBuilder` dựng lại từ đầu (Grid Isometric + Tilemap + `SpriteRenderer` + camera orthographic), HUD vẫn Canvas; `RestaurantView` đọc điểm đứng từ bố cục thay cho `RectTransform` cũ. Không đổi server, `GameData`, `RestaurantSimulation`.

**Tech Stack:** Unity 6000.3.15f1, C#, `com.unity.2d.tilemap` (đã có trong `Packages/manifest.json`), uGUI cho HUD (hình có sẵn), Test Framework (EditMode + PlayMode), CLI `unity`.

**Spec:** [2026-11-23-restaurant-tilemap-furniture-design.md](../specs/2026-11-23-restaurant-tilemap-furniture-design.md) (user duyệt thiết kế và ảnh mô phỏng 2026-11-23). "Mục N" là mục của spec.

## Trước khi bắt đầu

1. **Thời tiết và Cốt truyện Ch1 phải merge vào `main` trước** (cùng đụng `PlayerSave`, `ProjectSetup`, `RestaurantSceneBuilder`; 🔨 làm lần lượt: Thời tiết → Cốt truyện → kế hoạch này). Nhánh `feature/restaurant-tilemap` cắt từ `origin/main` sau đó. Khi dựng builder mới (Task 3), **chuyển** `WeatherBadge_PH_380x68` và `WeatherTint_PH` (nếu Thời tiết đã thêm) sang builder mới, giữ tên và test của chúng.
2. Cần **khoá Unity** từ 👑 CEO (test, dựng lại scene, playtest). Chung scene `Restaurant.unity`: không phiên 🔨 nào khác sửa scene này trong lúc làm.
3. Không package mới (Tilemap đã có), không tài khoản, không deploy server.
4. Model: Task 1–2 dùng **Opus 5.5 high** (save, Coin); Task 3–6 Sonnet 5.5 high ([agent-routing.md](../../studio/agent-routing.md)).

## Global Constraints

- Không sửa/xoá tay `.meta`; không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`, `_Scratch/`. **Không sửa tay `.unity`/`.prefab`/`.asset`**: scene đổi bằng `RestaurantSceneBuilder` rồi Rebuild; file chuyển vào `_Old/` bằng Unity (`AssetDatabase.MoveAsset`/`PrefabUtility`) để `.meta` đi theo.
- **Git: không commit, không push** (chỉ 📋 Thư ký). Không có bước commit trong kế hoạch.
- **D-005:** `PlayerSave.Furniture` có giá trị game mới đúng; `FurnitureService` không giữ bản sao, đọc `_save.Furniture` mỗi lần.
- **Server không đổi.** Không thêm field vào `ServerState`, không đụng `Rules`, `GameData`, `ServerModule`. Coin trừ qua `Wallet.TrySpend` (như `UpgradeService`).
- **`RestaurantSimulation`, `IncomeCalculator`, `AfkService`, `IncidentDirector` không đổi** (chỉ `RestaurantQuality`, `RestaurantConfig`, `RestaurantService`, view).
- Hình chỉ dùng hình có sẵn của Unity (`UISprite.psd`, `Knob.psd`, sprite hình học); **không thêm file ảnh**. `Tile` asset (`.asset`) không phải ảnh nên được. Object chưa có hình đặt tên `<Tên>_PH_<rộng>x<cao>`. Tile thoi không dựng được từ hình vuông thì **dừng, báo 🧭**, không tự thêm ảnh.
- Chữ qua `UiText` (khu `furniture`, hai ngôn ngữ); chữ panel DEV tiếng Việt cố định.
- **`Update` của view không cấp phát** (L-044: không LINQ, không `foreach` qua interface, không `new`, không chuỗi mỗi khung).
- Luật UI: nút mới trong `LobbyHud`, không giao vạch 120 px và vùng nút Thông báo ([unity-workflow](../../guides/unity-workflow.md)).
- Chạy test: `unity test . --mode EditMode --output <file>`; PlayMode: `--mode PlayMode`. Tìm bài học trước khi sửa: `Grep "#script" docs/lessons/lesson.md -A 4`, `#ui`, `#save`.

## Chia phiên 🔨

- **Phiên 1: Task 1–2** (luật lưới, dữ liệu, dịch vụ, điểm trang trí, chất lượng 40/40/20). Không UI, không dựng scene.
- **Phiên 2: Task 3** (cất cảnh cũ, quán Tilemap, camera, `RestaurantView` theo bố cục). Cần khoá Unity.
- **Phiên 3: Task 4** (Quản lý + Cửa hàng + Kho + chế độ đặt + docs + playtest).
- **Phiên 4 (nếu còn thời gian): Task 5–6** (mức đầy đủ, mục 9 của spec).
- Các task đều chạm `RestaurantSceneBuilder`, nên **không chạy song song**; subagent chỉ dùng trong một task (vd. viết test với code cài đặt song song).
- Điểm dừng playtest sau Task 4.

## Task 1: Dữ liệu và luật lưới (thuần C#)

**Files:**
- Tạo: `Assets/_Project/Core/Scripts/FurnitureState.cs` (`FurnitureState`, `FurniturePlacement`, `FurnitureOwned`), `Assets/_Project/Restaurant/Scripts/FurnitureDefinition.cs`, `FurnitureCatalog.cs`, `FurnitureLayout.cs` (luật lưới), `GridMath.cs` (ô ↔ toạ độ thế giới, thứ tự vẽ; đặt ở `Restaurant` hoặc `UI` theo asmdef nào view dùng)
- Sửa: `Assets/_Project/Core/Scripts/PlayerSave.cs` (field `Furniture`), `Assets/_Project/Restaurant/Scripts/RestaurantConfig.cs` (Header "Nội thất": `DecorMax`, `CameraSize`, `PanLimit`, ô mặc định, sảnh cửa; sửa `AttendanceMax = 40`, `IncidentMax = 40`, `Validate`: tổng ba phần = 100), `Assets/_Project/Editor/ProjectSetup.cs` (tạo `FurnitureDefinition` + `FurnitureCatalog` + chuyển 50/50 → 40/40/20 một lần trên asset đã có)
- Test: `Assets/_Project/Tests/FurnitureLayoutTests.cs`, `GridMathTests.cs`, `RestaurantConfigTests.cs` (sửa), `SaveCompatibilityTests.cs` (+2)

- [ ] **Step 1: Viết test đỏ** (~24 test):
  `FurnitureLayoutTests`:
  1. `Footprint_Rotated90_SwapsWidthAndHeight`; `Point_Rotated90_UsesFormula` (điểm `(x, y)` → `(y, w − x)`, kiểm 4 góc dấu chân 3×1 và 1×1).
  2. `CanPlace_InsideEmptyFloor_True`; `CanPlace_OutsideFloor_False` (cột/hàng −1, 8, dấu chân tràn); `CanPlace_OverlapsOtherPiece_False`; `CanPlace_ExactlyAdjacent_True`; `CanPlace_InDoorLobby_False` (bốn ô `(0,5) (0,6) (1,5) (1,6)`).
  3. `TryRotate_Invalid_KeepsPlacement` (xoay làm tràn sàn hoặc chồng thì giữ nguyên, trả false).
  4. `DefaultLayout_PlacesKitchenCashierAndEightTables_NoOverlap` (bàn n theo số bàn 3…8; bố cục mặc định mục 3; không ô nào chồng, không ô nào trong sảnh cửa).
  5. `DefaultLayout_Spots_InsideFloorAndNotInsideOtherFootprints` (mọi điểm đứng của bếp/thu ngân sau xoay nằm trong 0..8 và không nằm trong dấu chân món khác).
  6. `Resolve_InvalidFunctionalPlacement_FallsBackToDefaultThenFirstFree`; `Resolve_InvalidDecor_GoesToStorage`; `Resolve_UnknownDefinitionId_IsKept_NotPlaced`; `Resolve_MorePlacedThanOwned_DropsExtras`.
  7. `Resolve_TableCountGrows_NewTableTakesDefaultSlotOrFirstFree`; `Resolve_NoRoomForFunctional_StoresOneDecorAndRetries`.
  8. `FirstFreeNear_CenterIsPreferred` (ô hợp lệ gần tâm sàn nhất cho món mới đặt; hết chỗ trả không có).
  `GridMathTests`: `CellToWorld_WorldToCell_RoundTrip` (64 ô, kể cả điểm lẻ); `DepthKey_IncreasesTowardFrontCorner`; `FloorBounds_Contains8x8`.
  Config/Save: `Config_Defaults_AreValid` (tổng 40/40/20 = 100); `Config_Validate_RejectsQualityNotSummingTo100`; `PlayerSave_NewGame_FurnitureIsEmpty`; `PlayerSave_OldJsonWithoutFurniture_LoadsEmpty` (`{"Version":1}`); `PlayerSave_JsonRoundTrip_KeepsFurniture`.
- [ ] **Step 2: Chạy test, xác nhận đỏ.**
- [ ] **Step 3: Cài đặt.** `FurnitureLayout` là lớp tĩnh/thuần (không `UnityEngine` ngoài `Vector2Int` nếu cần): `Footprint(def, rotation)`, `RotatePoint`, `CanPlace(layout, def, col, row, rotation, ignore)`, `Resolve(save, config, catalog, tableCount)` trả bố cục dùng được (danh sách mọi món đặt, gồm chức năng) **không ghi** save, `FirstFreeNear`. Bố cục mặc định và điểm đứng đúng bảng mục 3 (số liệu nằm trong `RestaurantConfig`/`FurnitureDefinition`, không hằng số rải rác). `FurnitureDefinition` theo mục 7.3; hai món trang trí mẫu `Furniture_plant`, `Furniture_shelf` và ba món chức năng `Furniture_table`, `Furniture_kitchen`, `Furniture_cashier` do `ProjectSetup` tạo khi còn thiếu (số khởi điểm mục 2). Đổi `Validate`. Bước chuyển một lần trên asset `RestaurantConfig` đã có (`50/50` → `40/40/20`; số khác thì giữ và log cảnh báo).
- [ ] **Step 4: Chạy test, xác nhận xanh** (cả bộ EditMode cũ vẫn xanh; test cũ gắn với `50/50` sửa theo `40/40/20`). Kỳ vọng: +24.
- [ ] **Step 5: Ghi vào `docs/lessons/lesson.md`** (`#process` hoặc `#save`) nếu agent sai hay user sửa điều gì; không có thì bỏ qua.

## Task 2: `FurnitureService`, điểm trang trí, Chất lượng, nối vào quán

**Files:**
- Tạo: `Assets/_Project/Restaurant/Scripts/FurnitureService.cs`
- Sửa: `Assets/_Project/Restaurant/Scripts/RestaurantService.cs` (thuộc tính `Furniture`, tạo trong ctor cùng `Upgrades`; `ResetRuntime`; phát `Changed` khi số bàn đổi để bố cục resolve lại), `RestaurantQuality.cs` (`Decor`, `Total`), `Assets/_Project/UI/Scripts/QualityPopup.cs` + `RestaurantViewText.cs` (dòng "Trang trí: x / 20"), `Assets/_Project/Core/Scripts/` nơi dựng bản sao `save` tải lên nếu cần (kiểm `Furniture` đi theo)
- Test: `Assets/_Project/Tests/FurnitureServiceTests.cs` (mới), `RestaurantQualityTests.cs`, `RestaurantServiceTests.cs`, `ResetSaveDataTests.cs`, `RestaurantViewTextTests.cs` (sửa/thêm)

- [ ] **Step 1: Viết test đỏ** (~22 test; `FakeClock` và `Wallet` dùng như `UpgradeServiceTests`):
  `FurnitureServiceTests`:
  1. Mua: `Buy_Enough Coin_AddsOwnedAndSpends`; `Buy_NotEnoughCoin_ChangesNothing`; `Buy_AtMaxOwned_Rejected` (5); `Buy_UnknownOrFunctionalId_Rejected`; `Buy_PersistFails_StillAppliedAndSavedFalse` (như `UpgradeService`).
  2. `BuyAndPlace_PlacesAtFreeCell_InStorageWhenFloorFull` (hết chỗ → vào kho, trả trạng thái `NoRoom`).
  3. `Place_FromStorage_UsesOwnedMinusPlaced`; `Place_WhenStorageEmpty_Rejected`; `Move_ToValidCell_Persists`; `Move_ToInvalidCell_Rejected_KeepsOld`; `Rotate_Valid_TogglesRotation`; `Rotate_Invalid_KeepsPlacement`.
  4. `Store_Decor_ReturnsToStorage_LosesPoints`; `Store_Functional_Rejected`; `StoreAllDecor_LeavesFunctionalInPlace`; `StoreAllDecor_WhenNoneDecor_ReturnsNone`.
  5. `DecorPoints_SumsPlaced_CappedAt20` (5 chậu + 5 kệ = 40 → 20); `DecorPoints_ChangedEventFired`.
  6. `Layout_ReadsSaveEachTime` (sửa `save.Furniture` trực tiếp thì dịch vụ thấy ngay); `ResetToDefaults_ClearsFurniture` (reset `PlayerSave` → bố cục mặc định, kho trống).
  Chất lượng: `Quality_Total_IsAttendancePlusIncidentPlusDecor`; `Quality_Decor20_RaisesAfkFactor` (0 điểm → 0,1; thêm 20 → +0,08 so với trước theo `Lerp(0,1 → 0,5, Total/100)`); `QualityPopup` chữ có dòng Trang trí (`RestaurantViewTextTests`).
  Nối: `RestaurantService_UpgradeTables_ResolvesNewTableCell`.
- [ ] **Step 2: Chạy test, xác nhận đỏ.**
- [ ] **Step 3: Cài đặt.** Mỗi thao tác theo mẫu `UpgradeService.TryBuy`: mọi bước có thể thất bại đứng **trước** bước đầu tiên đổi trạng thái (`TrySpend`), trả struct kết quả (`Status`, `Saved`). Kết quả `Placement` trả bố cục sau `Resolve`. `Changed` phát khi mua/đặt/cất/dời/xoay. Món trang trí mới đặt dùng `FirstFreeNear`. `RestaurantQuality.Decor = min(DecorMax, Σ DecorPoints món đặt)`; `Total` cộng ba phần; `QualityPopup` thêm dòng Trang trí (hình tạm).
- [ ] **Step 4: Chạy test, xác nhận xanh** (cả bộ cũ xanh). Kỳ vọng: +22 (tổng sau Task 2 ≈ **+46 EditMode**).
- [ ] **Step 5: `PlayerSave` và `UploadCopy`:** xác nhận bản sao tải lên giữ `Furniture` (một test vào `CloudSyncTests` hoặc test sẵn có; lỗi ở đây làm mất nội thất mỗi lần đẩy save).

## Task 3: Cất cảnh cũ, quán Tilemap hình thoi, camera, `RestaurantView` theo bố cục

**Files:**
- Tạo: `Assets/_Project/Restaurant/Prefabs/_Old/RoomOld.prefab`, `GardenOld.prefab` (bằng Unity, `.meta` đi theo); `Assets/_Project/Restaurant/Data/Tiles/Tile_*.asset` (`Tile` tạm); `Assets/_Project/UI/Scripts/RestaurantCamera.cs` (kéo + kẹp), `FurnitureWorldView.cs` (dựng/đồng bộ `SpriteRenderer` món theo `FurnitureService.Layout`)
- Sửa: `Assets/_Project/Editor/RestaurantSceneBuilder.cs` (**dựng lại từ đầu** phần thế giới, giữ HUD/popup; bỏ `Block`, `Wall`, `BuildGarden`, `BuildCrops`), `Assets/_Project/UI/Scripts/RestaurantView.cs` (điểm đứng từ bố cục), `WorldFocus.cs` (hoặc thay bằng `CameraFocus`) và nơi gọi (`NotifyView` đang tâm bằng `_worldScroll`), xoá `IsoProjection.cs` + `IsoProjectionTests.cs` khi không còn ai dùng
- Test: `RestaurantSceneTests.cs`, `LobbySceneTests.cs`, `WorldFocusTests.cs` (sửa/viết lại), test mới `RestaurantWorldSceneTests.cs`

- [ ] **Step 1: Spike tile thoi (làm trước, ≤ 1 giờ).** Dựng một `Tile` từ `UISprite.psd`, thử `Tile.transform` (xoay 45°, co trục 0,5) trên Tilemap của `Grid` Isometric `(1, 0.5, 1)`: có ra hình thoi khít không? **Không** thì dừng task, báo 🧭 (xin ảnh ô thoi từ user), không đoán lần hai. Ghi kết quả vào lesson nếu bất ngờ.
- [ ] **Step 2: Cất cảnh cũ (trước khi đụng builder).** Mở `Restaurant.unity` hiện có, lưu `Room` thành `Assets/_Project/Restaurant/Prefabs/_Old/RoomOld.prefab`, `Garden` thành `GardenOld.prefab` (`PrefabUtility.SaveAsPrefabAsset`, qua `unity` CLI hoặc MCP, chỉ phần hình, không kèm `RestaurantView`/`RestaurantHud`/popup/nút); xác nhận hai prefab mở được. **Chưa xoá object khỏi scene tới khi Step 4 dựng lại.** Ghi tên hai prefab vào báo cáo cuối kênh.
- [ ] **Step 3: Viết test đỏ** (~16 test, `RestaurantWorldSceneTests` và sửa test cũ):
  1. `RestaurantScene_HasGridIsometric_WithFloorTilemap64Cells`; `..._HasTwoBackWalls_AndDoor`; `..._WorldCameraIsOrthographic_AndHudCanvasIsOverlay`.
  2. `Furniture_DefaultLayout_PiecesMatchFootprintsAndNamesKept` (`Kitchen`, `Counter`, `Table{i}` tồn tại, vị trí khớp `GridMath` của ô neo); `Furniture_DoNotOverlapInCellSpace` (thay `RestaurantScene_FurnitureDoesNotOverlap`).
  3. `RestaurantView_SpotsComeFromLayout_AndAreInsideFloor` (thay `LandmarksAreInsideFloor`); `Tables_HaveDishesInOrder` (giữ); `ActorTemplates_HaveShadowFigureBubble` (giữ).
  4. `RestaurantCamera_Clamp_KeepsCenterInsidePanLimit`; `RestaurantCamera_DragMovesCenter`.
  5. `Scene_DoesNotDependOnOldFolder` (`AssetDatabase.GetDependencies(scenePath)` không có đường dẫn chứa `/_Old/`).
  6. `Scene_HasNoSpriteOrImageFilesAdded` (các `Image`/`SpriteRenderer` dùng sprite `unity_builtin_extra`/`unity default resources`, không file trong `Assets/**/*.png|jpg`).
  7. Test HUD cũ (`RestaurantScene_HudAndColumnInsideCanvas`, `InfoBarAndBannerClearOfFloorAndColumn`, `LobbySceneTests` về `WorldScroll`) sửa theo bố cục mới hoặc bỏ nếu không còn nghĩa (ghi rõ trong báo cáo cái nào bỏ và lý do).
- [ ] **Step 4: Cài đặt.** Dựng lại thế giới trong builder mới: `Grid`, `Floor` (caro hai màu bằng `Tile.color`), tường sau (hai dải cao, `SpriteRenderer` hoặc tile), cửa, vườn (Knob/UISprite), `Furniture` root, `Actors` root, camera orthographic (`CameraSize`), `RestaurantCamera` (kéo trên chỗ không phải món; ngưỡng kéo 12 px; kẹp `PanLimit`), thứ tự vẽ theo y. `FurnitureWorldView` tạo/đồng bộ món theo `FurnitureService.Layout` (hình tạm: mặt trên hình thoi theo dấu chân + hai mặt bên theo `Height`, tô màu `Tint`); khi bố cục đổi (event `Changed`) cập nhật **không cấp phát mỗi khung**. `RestaurantView` đổi `RectTransform` spots thành `Transform` hoặc tra từ bố cục (`CookSpot(i)`, `ServerSpot(i)`, `CashierSpot(i)`, `TableCenter(i)`, `DoorSpot`); dấu "!" sự cố và nút `IncidentButton` neo bằng `Camera.WorldToScreenPoint`. Chuyển `WeatherBadge`/`WeatherTint` sang builder mới nếu Thời tiết đã merge. **Xoá object cũ** khỏi scene (chỉ còn prefab `_Old`), chạy **Tools ▸ BunDauPeo ▸ Rebuild Restaurant Scene**. `ProjectSetup.CreateRestaurantScene` giữ hành vi (không dựng lại scene đang có trừ khi `migrating`).
- [ ] **Step 5: Chạy test, xác nhận xanh** (cả bộ EditMode cũ xanh; PlayMode `SmokeLoopTests` các bước quán vẫn qua: khách đi tới bàn, sự cố "!" bấm được). Kỳ vọng: +16, trừ các test cũ bỏ.
- [ ] **Step 6: Chụp màn** bằng `PlaytestTools.Capture1080` (Lobby thường và camera kéo sang hai bên) để 🧭/user so với ảnh mô phỏng; không thêm ảnh vào repo.
- [ ] **Step 7: Hiệu năng sơ bộ** (Profiler, Lobby 60 giây): GC/khung của `RestaurantView.Update`, số draw call; ghi số vào báo cáo.

## Task 4: Quản lý, Cửa hàng, Kho, chế độ đặt; docs; playtest

**Files:**
- Tạo: `Assets/_Project/UI/Scripts/ManageMenu.cs`, `FurnitureShopPanel.cs`, `FurnitureStoragePanel.cs`, `PlaceController.cs` (chế độ đặt: chọn, kéo, nút nổi), `UiText.Furniture.cs` (khu `furniture`)
- Sửa: `RestaurantSceneBuilder.cs` (`ManageButton_PH_130x130`, `ManageMenu`, `FurnitureShopRoot`, `FurnitureStorageRoot`, `PlaceControls_PH`; thay `StaffButton`/`UpgradeButton` ở cột phải bằng `ManageButton`, hai nút đó chuyển vào menu **giữ tên**), `RestaurantHud.cs`/`RestaurantBarView.cs` (cột phải còn Quản lý + Thu AFK), `HubNav` (mở trang khác thì thoát chế độ đặt), `PlaytestTools`/`UiDriver` (bấm `StaffButton`/`UpgradeButton` mở menu trước), `QualityPopup` (đã có ở Task 2)
- Test: `ManageMenuTests.cs`, `FurnitureShopPanelTests.cs`, `FurnitureStoragePanelTests.cs`, `PlaceControllerTests.cs`, `UiTextTests.cs` (+khoá `furniture.*`), `LobbyPagesTests.cs`/`LobbySceneTests.cs` (sửa), `PlayMode/SmokeLoopTests.cs` (+2, sửa các bước bấm `StaffButton`/`UpgradeButton`)

**Trước khi dựng:** mở lại ảnh mô phỏng bốn khung user đã duyệt ([mục 6](../specs/2026-11-23-restaurant-tilemap-furniture-design.md#6-ui)) và `ReferenceImages/restaurant-furniture-*`; lệch với bố cục đã duyệt thì hỏi 🧭, không tự đổi.

- [ ] **Step 1: Viết test đỏ** (~22 EditMode + 2 PlayMode):
  `ManageMenuTests`: `Menu_HasStaffUpgradeShopStorageButtons_NamesKept` (nút Cất tất cả chỉ vào menu ở Task 5); `ManageButton_TogglesMenu_AndOutsideClickCloses`; `StaffButton_InMenu_OpensStaffPanelRoot_AndClosesMenu` (tương tự `UpgradeButton`); `RightColumn_HasOnlyManageAndAfk`; `Buttons_DoNotCrossBottomBarLine_AllRatios` (kể cả đang tắt).
  `FurnitureShopPanelTests`: thẻ hai món đúng giá và "sở hữu x/5"; `BuyToStorage_Disabled_WhenNotEnoughCoin_OrMaxOwned`; `BuyAndPlace_ClosesPopupAndEntersPlaceMode`; `BuyAndPlace_NoRoom_ShowsNoRoomMessage_AndGoesToStorage`; chân hộp hiện "Đã đặt / Trang trí x/20".
  `FurnitureStoragePanelTests`: số lượng = sở hữu − đang đặt; `Place_Disabled_WhenZeroInStorage`; `Place_ClosesPopupAndEntersPlaceMode`.
  `PlaceControllerTests` (logic thuần/`EditMode`, giả lập con trỏ qua hàm công khai): chọn món bằng chạm; kéo bám lưới (`WorldToCell`); thả ô hợp lệ thì lưu, **thả ô không hợp lệ thì về ô cũ**; Xoay/Cất kho/Xong; Cất kho ẩn với món chức năng; kéo ngoài món thì không dời món (camera dời); mở trang khác thì thoát; dấu "!" ẩn khi đang đặt.
  `UiTextTests`: mọi khoá `furniture.*` có vi và en.
  PlayMode: `Furniture_BuyPlaceDone_RaisesDecorPoints` (Quản lý → Cửa hàng → Mua và đặt → Xong → `Quality.Decor` > 0 sau khi cho đủ Coin bằng DEV); `Furniture_ReloadKeepsPlacement` (tắt mở scene).
- [ ] **Step 2: Chạy test, xác nhận đỏ.**
- [ ] **Step 3: Cài đặt** theo mục 6 và luồng đặt mục 6 của spec. Menu, hộp Cửa hàng/Kho dựng bằng `PageFrame`/`Image` hình có sẵn như `StaffPanelRoot`; hộp phải theo luật "trang/hộp mới kín màn" hoặc hộp giữa 1200×800 như Hồ sơ (chọn theo ảnh mô phỏng: hộp giữa). `PlaceController` giữ trạng thái chọn (trạng thái view, `ResetRuntime` thoát chế độ đặt). Tên đối tượng mới theo quy ước `_PH_`.
- [ ] **Step 4: Chạy test, xác nhận xanh** (cả bộ EditMode + PlayMode). Kỳ vọng: **+22 EditMode, +2 PlayMode** cho task này; tổng cả mức tối thiểu ≈ **+60 EditMode, +2 PlayMode** (ước ở spec mục 8; ghi số thật).
- [ ] **Step 5: Sửa docs:**
  - `docs/roadmap.md`: chuyển dòng Vẽ lại quán + Nội thất sang "Đã xong" kèm link spec/kế hoạch, số test, ngày; ghi đã cất gì vào `_Old/`.
  - `docs/design/game-design.md`: mục Quán (bố cục thoi, Chất lượng 40/40/20, nội thất) đã sửa lúc viết spec; kiểm lại khớp code, bỏ chữ "(🔨 chưa làm)" nếu có.
  - `docs/design/screen-flow.md`: bố cục Lobby có nút Quản lý + menu + ba trang nội thất.
  - `docs/project/structure.md`: thêm `FurnitureLayout`, `FurnitureService`, `FurnitureDefinition`, `GridMath`, `RestaurantCamera`, `FurnitureWorldView`, các panel; ghi `_Old/` và việc `IsoProjection` đã gỡ.
  - `docs/guides/unity-workflow.md`: mục "UI và hình tạm" ghi Lobby giờ có camera thế giới (nút mới vẫn vào `LobbyHud`); bỏ chữ `WorldScroll` nếu còn.
  - Nếu agent sai hay user sửa: ghi `docs/lessons/lesson.md` ngay trong task.
- [ ] **Step 6: Playtest (người thử)**, trên Editor chế độ Local rồi một lần trên máy thật: (1) mở Quản lý, thấy đủ năm mục; Nhân viên/Nâng cấp mở như cũ; (2) mua chậu cây, đặt, kéo sang ô chồng bàn thì về chỗ cũ, xoay kệ; (3) tắt mở game: bố cục còn; (4) Chất lượng quán hiện "Trang trí" đúng; thu AFK hệ số đổi theo; (5) nâng cấp Bàn thêm bàn xuất hiện đúng ô; (6) kéo camera không văng ra ngoài; (7) Reset Save Data xoá nội thất. Ghi kết quả vào báo cáo.
- [ ] **Step 7: Hiệu năng** theo [performance.md](../../studio/performance.md): Lobby 60 giây (ms/khung, GC/khung, draw call), dung lượng scene; báo lên nếu vượt ngưỡng.
- [ ] **Step 8: Gửi "Báo cáo cuối kênh"** ([brief.md](../../studio/brief.md)) cho 📋 và Leader; ghi rõ **đã cất vào `_Old/`:** `Restaurant/Prefabs/_Old/RoomOld.prefab`, `GardenOld.prefab`, và code đã thay (`IsoProjection`, `Block`/`Wall`).

## Task 5 (mức đầy đủ): Cất tất cả, zoom, DEV, chấm báo

- [ ] **Step 1: Viết test đỏ** (~8): `StoreAllConfirm_ShowsCount_AndOkStoresAllDecor`; `StoreAllButton_Disabled_WhenNothingPlaced`; `Camera_Zoom_ClampedToRange`; `DevPanel_HasFurnitureSection` (tặng 1 món, tặng đủ, xoá nội thất, "+10.000 Coin" nếu chưa có); `ManageButton_ShowsBadge_WhenStaffMissing`; `UiText` các khoá mới.
- [ ] **Step 2: Chạy test, xác nhận đỏ.**
- [ ] **Step 3: Cài đặt:** nút `FurnitureStoreAllButton` vào menu và hộp `FurnitureStoreAllConfirm` theo `restaurant-furniture-bulk-store.jpg` (vẽ trên thanh dưới như `StaffConfirmPopup`); zoom hai ngón trong `[ZoomMin, ZoomMax]`; mục "Nội thất" trong `DevOverlay` theo mẫu các nút DEV khác; chấm báo ở `ManageButton`.
- [ ] **Step 4: Chạy test, xác nhận xanh** (+8). Sửa docs liên quan (mục DEV trong screen-flow).

## Task 6 (mức đầy đủ, chỉ khi còn thời gian): Bộ cài sẵn

- [ ] **Step 1: Hỏi 🧭 chốt số ô (khởi điểm 3) và có đổi tên không** (theo `restaurant-furniture-myset.jpg`; user chưa chốt số ô).
- [ ] **Step 2: Viết test đỏ + cài đặt:** `PlayerSave.Furniture.Sets` (danh sách bộ: tên + danh sách `FurniturePlacement`); lưu bố cục hiện tại vào ô; áp dụng ô (chỉ món sở hữu đủ; thiếu thì bỏ qua và báo); xoá ô; hộp `FurnitureMySetRoot`; mục thứ 6 trong menu.
- [ ] **Step 3: Chạy test, xanh; sửa docs; báo cáo.**

## Review Focus (cho 🔍 hoặc review cuối nhánh)

- Reset Save Data lúc đang ở chế độ đặt: thoát chế độ, xoá nội thất, bố cục về mặc định.
- Thả món ở ô không hợp lệ không bao giờ lưu; dữ liệu hỏng/sửa tay (`Resolve`) không làm mất món (trang trí về kho).
- Mua Coin: thiếu Coin không đổi gì; đã đủ 5 thì chặn; `persist` thất bại vẫn áp dụng và báo "chưa lưu được".
- `RestaurantView.Update` không cấp phát; `FurnitureWorldView` chỉ cập nhật khi bố cục đổi.
- Scene `Restaurant.unity` không phụ thuộc `_Old/`; `StaffButton`/`UpgradeButton` giữ tên và vẫn bấm được qua menu (smoke test, `PlaytestTools`).
- `UploadCopy` giữ `Furniture`; chất lượng 40/40/20 khớp mọi nơi hiển thị (`QualityPopup`, `InfoBar`).
