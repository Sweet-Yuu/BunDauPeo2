# TÀI LIỆU ĐẶC TẢ KỸ THUẬT & HƯỚNG DẪN REMAKE HỆ THỐNG NHÀ HÀNG
## DỰ ÁN: BÚN ĐẬU PÈO (BunDauPeo) — NHÁNH `Hoang`

> **Tài liệu bàn giao kỹ thuật:** Dành cho lập trình viên sử dụng **ChatGPT PLUS** để remake, tái cấu trúc hoặc mở rộng toàn bộ các phân hệ đã hoàn thiện thuộc nhánh `Hoang`: Cây Kỹ Năng (Skill Tree), Lưới Thiết Bị Bếp (Facilities Grid), Mạng Lưới Điện (Power Grid), Cầu Nối Chỉ Số (Buff Bridge), Lưu Trữ Tự Động (Save/Load System), Tooltip Thông Minh và Shop & Điểm Danh Hàng Ngày.

---

## MỤC LỤC
1. [Môi Trường & Ràng Buộc Kỹ Thuật Bắt Buộc](#1-môi-trường--ràng-buộc-kỹ-thuật-bắt-buộc)
2. [Sơ Đồ Kiến Trúc Tổng Thể](#2-sơ-đồ-kiến-trúc-tổng-thể)
3. [Đặc Tả Chi Tiết Từng Phân Hệ](#3-đặc-tả-chi-tiết-từng-phân-hệ)
   - [3.1. Phân Hệ Cây Kỹ Năng (Skill Tree System)](#31-phân-hệ-cây-kỹ-năng-skill-tree-system)
   - [3.2. Phân Hệ Thiết Bị & Lưới Điện Kiểu Resident Evil (Facilities Grid & Power Grid)](#32-phân-hệ-thiết-bị--lưới-điện-kiểu-resident-evil-facilities-grid--power-grid)
   - [3.3. Cầu Nối Chỉ Số Thống Nhất (Restaurant Buff Bridge)](#33-cầu-nối-chỉ-số-thống-nhất-restaurant-buff-bridge)
   - [3.4. Hệ Thống Lưu Trữ Trạng Thái Tự Động (Save & Load System)](#34-hệ-thống-lưu-trữ-trạng-thái-tự-động-save--load-system)
   - [3.5. Hệ Thống Tooltip Nổi Thông Minh (Smart UI Tooltip)](#35-hệ-thống-tooltip-nổi-thông-minh-smart-ui-tooltip)
   - [3.6. Hệ Thống Điểm Danh & Shop Tiền Tệ (Daily Rewards & Shop)](#36-hệ-thống-điểm-danh--shop-tiền-tệ-daily-rewards--shop)
4. [Bảng Kê Khai Danh Mục File Mã Nguồn (Codebase Inventory)](#4-bảng-kê-khai-danh-mục-file-mã-nguồn-codebase-inventory)
5. [Prompt Mẫu Chuẩn Dành Riêng Cho Claude Pro](#5-prompt-mẫu-chuẩn-dành-riêng-cho-claude-pro)

---

## 1. MÔI TRƯỜNG & RÀNG BUỘC KỸ THUẬT BẮT BUỘC

Khi thực hiện remake hoặc phát triển tiếp bằng Claude Pro, **bắt buộc phải tuân thủ nghiêm ngặt 5 nguyên tắc sau**:

1. **Phiên bản Engine & Package:**
   * **Unity Engine:** `6000.3.15f1` (Unity 6).
   * **Render Pipeline:** Universal Render Pipeline (URP 17.3.0) với cấu hình `2D Renderer`.
   * **Input System:** **New Input System Package** (`com.unity.inputsystem 1.19.0`, cấu hình `activeInputHandler: 1`).
     * *CẢNH BÁO:* Không được dùng `UnityEngine.Input` cũ (như `Input.mousePosition`, `Input.GetMouseButtonDown`). Việc này sẽ ném ra lỗi ngoại lệ `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class...`.
     * *CHUẨN MỰC:* Phải đọc chuột/cảm ứng thông qua `UnityEngine.InputSystem.Mouse.current.position.ReadValue()` hoặc `UnityEngine.InputSystem.Touchscreen.current`.
   * **UI Framework:** uGUI 2.0.0 kết hợp TextMeshPro.
   * **Serialization:** Newtonsoft Json.NET kết hợp `JsonUtility`.

2. **Quy tắc Phân Nhánh & Thư Mục (Theo `AGENTS.md`):**
   * **Branch Git:** Làm việc trên nhánh `Hoang`.
   * **Thư mục Assets:** Toàn bộ code, prefabs, ScriptableObjects phải nằm trong `Assets/_Hoang/`.
   * **Scene Isolation:** Tuyệt đối không chỉnh sửa scene gốc trong `Assets/__Project/Scenes/`. Mọi thử nghiệm và hoàn thiện giao diện thực hiện trên bản clone tại `Assets/_Hoang/Scene/SkillTreeSystem.unity` hoặc `1_Background.unity`.
   * **Ranh giới module của Vũ (`Assets/__Vu/`):** Không sửa logic gameplay chiến đấu hay core của Vũ, chỉ tích hợp vào `GameSaveCoordinator` và `EconomySaveData`.

3. **Ràng Buộc Zero-Icon / Không Dùng Ký Tự Đặc Biệt (Chống Lỗi Tofu `□`):**
   * Font chữ của dự án (`LiberationSans SDF`) **không có glyph cho Unicode Emoji** (`★`, `👑`, `⚡`, `•`, etc.). Bất kỳ text nào chứa emoji sẽ bị Unity thay thế bằng ô vuông `□` và cảnh báo trong Editor Log.
   * *CHUẨN MỰC:* Toàn bộ UI, Tooltip, chuỗi hiển thị và Log chỉ dùng **Plain ASCII & Tiếng Việt có dấu chuẩn Unicode** (A-Z, a-z, 0-9, %, +, -, :, /, |). Phân cấp trực quan bằng màu sắc (`Color`) và phong cách chữ (`Bold`, `Italic`).

4. **Kiến Trúc Lưu Trữ (Dual-Storage & Atomic Safety):**
   * Phân hệ Nhà Hàng lưu trữ độc lập qua `RestaurantSaveManager` với cơ chế lưu kép: file JSON tại `Application.persistentDataPath/restaurant_save.json` và bản sao lưu `PlayerPrefs`.
   * Phân hệ Shop & Điểm Danh lưu trữ nguyên tử (Atomic Commit) thông qua `GameSaveCoordinator` của Vũ vào `save.json`.

---

## 2. SƠ ĐỒ KIẾN TRÚC TỔNG THỂ

```
                                  +---------------------------------------+
                                  |         Gameplay Vận Hành Quán        |
                                  | (Nấu Ăn, Thực Khách, Thu Ngân, Kho)   |
                                  +-------------------+-------------------+
                                                      |
                                     Truy vấn chỉ số  | (GetTotalCookingSpeedMultiplier,
                                     tập trung 1 dòng |  GetTotalRevenueMultiplier, ...)
                                                      v
                                  +---------------------------------------+
                                  |         RestaurantBuffBridge          | <---+ Phát sự kiện khi có thay đổi
                                  |       (Static Facade Controller)      |     | (OnBuffsChanged)
                                  +-------------------+-------------------+     |
                                                      |                         |
                           +--------------------------+-------------------------+
                           |                                                    |
                           v                                                    v
             +---------------------------+                        +---------------------------+
             |     SkillTreeManager      |                        |      FacilityManager      |
             | - Điểm Thường / Đặc Biệt  |                        | - Lưới 11x11 (Resident)   |
             | - Nâng cấp, Reset điểm    |                        | - Quản lý Kho & Trang bị  |
             | - Quản lý SkillSlot UI    |                        | - Mạng Lưới Điện (BFS)    |
             +-------------+-------------+                        +-------------+-------------+
                           |                                                    |
                           +--------------------------+-------------------------+
                                                      |
                                          Tự động lưu | Auto-Save / Auto-Load
                                          khi thay đổi| khi Start/Awake
                                                      v
                                  +---------------------------------------+
                                  |         RestaurantSaveManager         |
                                  | - Dual Storage: JSON + PlayerPrefs    |
                                  | - Lazy Singleton (Edit & Play Mode)   |
                                  | - Tools > Hoang > Save & Load Editor  |
                                  +---------------------------------------+
```

---

## 3. ĐẶC TẢ CHI TIẾT TỪNG PHÂN HỆ

### 3.1. Phân Hệ Cây Kỹ Năng (Skill Tree System)

* **Vị trí mã nguồn:** `Assets/_Hoang/Scripts/SkillTree/`
* **Mục tiêu:** Cung cấp cây kỹ năng nhà hàng dạng phân nhánh (graph node), mở khóa dần theo cấp độ và điều kiện tiên quyết, cho phép người chơi định hình phong cách quán (tập trung tốc độ nấu, tối đa doanh thu hoặc chất lượng phục vụ).

#### Cấu Trúc Thành Phần:
1. **`SkillSO.cs` (ScriptableObject):**
   * Định nghĩa dữ liệu tĩnh cho từng kỹ năng.
   * `skillName`, `description`, `maxLevel` (mặc định 5), `skillIcon`.
   * `isExclusive`: Cờ đánh dấu kỹ năng độc quyền cao cấp.
   * `specialPointCost`: Số điểm đặc biệt cần dùng (mặc định 1).
   * Các chỉ số buff gia tăng theo từng cấp độ:
     * `productionSpeedBonusPerLevel`: Tăng tốc độ nấu (+% mỗi cấp, ví dụ: 0.05 = +5%).
     * `revenueBonusPerLevel`: Tăng doanh thu bán món (+% mỗi cấp).
     * `customerPatienceBonusPerLevel`: Tăng thời gian kiên nhẫn chờ món của khách.
     * `ingredientCostReductionPerLevel`: Giảm giá vốn nhập nguyên liệu.
     * `cleanlinessBonusPerLevel`: Điểm vệ sinh & danh tiếng quán ăn.

2. **`SkillSlot.cs` (MonoBehaviour):**
   * Đại diện cho một node bấm trên UI Canvas.
   * Quản lý trạng thái: `isUnlocked`, `currentLevel`, `prerequisiteSkills` (danh sách các skill phải đạt cấp tối thiểu trước khi mở khóa).
   * Tự động vẽ các đường nối phân nhánh (Connection Lines) tới các node con.
   * Bắt sự kiện rê chuột (`IPointerEnterHandler`, `IPointerExitHandler`) để gọi `UITooltipSystem.ShowTooltip(...)`.
   * Xử lý nâng cấp: Kiểm tra đủ điểm, trừ điểm tại `SkillTreeManager`, phát âm thanh/hiệu ứng và cập nhật giao diện node.

3. **`SkillTreeManager.cs` (Singleton):**
   * Quản lý tài nguyên điểm: `availablePoints` (Điểm thường) và `availableSpecialPoints` (Điểm đặc biệt).
   * Hỗ trợ nút **Reset**: Hoàn lại toàn bộ điểm đã cộng và hạ tất cả kỹ năng về cấp 0.
   * Tự động đồng bộ với `RestaurantSaveManager` khi khởi chạy (`Start()`) và kích hoạt lưu trạng thái mỗi khi có kỹ năng được nâng cấp hoặc reset.
   * Cung cấp các API tính tổng buff toàn bộ cây:
     * `GetTotalProductionSpeedBonus()`
     * `GetTotalRevenueBonus()`
     * `GetTotalCustomerPatienceBonus()`
     * `GetTotalCostReduction()`
     * `GetTotalCleanlinessBonus()`

4. **`SkillTreeLayoutTool.cs` (Editor Tool):**
   * Menu tiện ích trong Editor hỗ trợ căn chỉnh lưới node tự động, tạo liên kết và kiểm tra tính toàn vẹn của đồ thị kỹ năng.

---

### 3.2. Phân Hệ Thiết Bị & Lưới Điện Kiểu Resident Evil (Facilities Grid & Power Grid)

* **Vị trí mã nguồn:** `Assets/_Hoang/Scripts/Facilities/`
* **Mục tiêu:** Mô phỏng không gian bếp trên lưới ma trận 11x11. Người chơi sắp xếp các khối thiết bị đa hình dạng (tương tự vali Resident Evil 4). Điểm đặc sắc là **Mạng Lưới Điện (Power Grid)**: Thiết bị chỉ kích hoạt và đem lại chỉ số buff khi được nối dây hoặc đặt tiếp giáp với nguồn điện đang phát đủ công suất.

#### Cấu Trúc Thành Phần:
1. **`FacilityItemSO.cs` (ScriptableObject):**
   * `id`, `itemName`, `shortName` (Tên ngắn gọn để không bị tràn chữ khi in lên ô lưới), `themeColor`.
   * **Hình dạng khối:** Danh sách ô `shapeCoords` cấu thành khối (ví dụ: khối chữ L, khối 2x1, khối 2x2).
   * **Thuật toán xoay 4 hướng (`GetRotatedCoords(int rotationIndex)`):**
     * Xoay góc 90 độ thuận chiều kim đồng hồ: `(x, y) -> (y, -x)`.
     * **Chuẩn hóa tọa độ:** Sau khi xoay, tự động dịch chuyển toàn bộ tọa độ sao cho góc nhỏ nhất luôn trở về `(0, 0)`.
   * **Cơ chế điện:**
     * `isPowerSource`: Đánh dấu là Nguồn Phát Điện chính (Máy phát điện, Ắc quy trung tâm).
     * `canConductPower`: Khả năng truyền dẫn điện sang các khối tiếp giáp.
     * `powerOutput`: Công suất phát ra (Watt) nếu là nguồn điện.
     * `powerConsumption`: Lượng điện tiêu thụ (Watt). Nguồn điện tiêu thụ 0W.
   * **Cơ chế Cầu Nối (Relay Mechanism):**
     * `isRelay`: Khối Trạm Cầu Nối. Khi có điện, nó có thể cấp điện cho các thiết bị công suất cao.
     * `requiresRelay`: Thiết bị công suất cao / nhạy cảm — **chỉ nhận được điện khi tiếp giáp với một khối Relay hoặc Nguồn Điện đang hoạt động**. Không thể nhận điện từ các khối dân dụng thường.
   * `FacilityStats stats`: Các chỉ số buff mà thiết bị mang lại khi có điện.

2. **`FacilityGrid.cs` (Data Logic):**
   * Quản lý ma trận lưới `11 x 11` ô.
   * Kiểm tra va chạm và tính hợp lệ khi đặt khối (`CanPlaceItem`).
   * **Thuật toán BFS Flood-Fill Truyền Điện (`UpdatePowerTransmission()`):**
     1. Khởi tạo: Đặt toàn bộ trạng thái `IsPowered = false` (ngoại trừ các Nguồn Điện). Tính tổng công suất nguồn `TotalPowerCapacity = SUM(powerOutput)`.
     2. Đưa tất cả các ô thuộc Nguồn Điện vào hàng đợi `Queue<Vector2Int>`.
     3. Duyệt lan truyền 4 hướng (Trên, Dưới, Trái, Phải):
        * Kiểm tra khối tiếp giáp có yêu cầu Relay (`requiresRelay`) hay không.
        * Kiểm tra tải công suất: `CurrentPowerUsed + item.powerConsumption <= TotalPowerCapacity`. Nếu đủ điện, bật `IsPowered = true` và cộng dồn công suất tiêu thụ; nếu quá tải, ngắt điện khối đó.
        * Nếu khối tiếp giáp có cờ `canConductPower == true`, tiếp tục đưa các ô của nó vào hàng đợi để truyền điện xa hơn.

3. **`FacilityManager.cs` (Controller Singleton):**
   * Quản lý trạng thái lưới runtime và kho trang bị trong túi đồ (`inventoryItemIds`).
   * Cung cấp các thao tác: `TryPlaceFromInventory`, `TryRepositionPlacedItem`, `TryRemoveToInventory`, `ClearAllToInventory`.
   * Tính toán `CurrentTotalStats`: Tổng hợp các chỉ số buff của tất cả các khối **ĐANG CÓ ĐIỆN** trên lưới.
   * Tự động gọi `RestaurantSaveManager.Instance.SaveFacility(this)` sau mỗi thao tác người chơi.

4. **`FacilityGridUI.cs` & `FacilityPlacedItemUI.cs` (Giao Diện Tương Tác):**
   * Tương tác kéo thả trực quan: Hiển thị preview vị trí sắp đặt; khung viền chuyển sang màu Xanh khi vị trí hợp lệ và chuyển sang màu Đỏ khi va chạm hoặc nằm ngoài lưới.
   * **Cơ chế Virtual Diff Map (`_placedItemMap`):** Khi lưới cập nhật, hệ thống không xóa sạch (`Destroy`) rồi sinh lại (`Instantiate`) toàn bộ UI nhằm tránh gây lag và rác bộ nhớ (Garbage Collection). Thay vào đó, nó so sánh `CachedOrigin` và `CachedRotation` để tái sử dụng tối đa các GameObject đã có.
   * Bắt sự kiện New Input System chuột/cảm ứng để xoay khối khi đang kéo thả (Phím R hoặc nút Xoay).

5. **`FacilityInventoryUI.cs` & `FacilityDraggableItemUI.cs`:**
   * Ngăn kéo chứa các trang thiết bị dự trữ chưa đưa lên lưới. Cho phép kéo thả trực tiếp từ ngăn kéo vào ô lưới.

6. **`FacilityStatsDisplayUI.cs`:**
   * Thanh HUD hiển thị theo thời gian thực: Công suất lưới điện (`Đã dùng W / Tổng công suất W`) và các chỉ số buff đang có hiệu lực.

---

### 3.3. Cầu Nối Chỉ Số Thống Nhất (Restaurant Buff Bridge)

* **File:** [`Assets/_Hoang/Scripts/Bridge/RestaurantBuffBridge.cs`](file:///d:/MakeGame/MyGame/GameWithVu/Project_04_BunDauPeo/Assets/_Hoang/Scripts/Bridge/RestaurantBuffBridge.cs)
* **Ý nghĩa kiến trúc:** Là **Facade Tĩnh Tập Trung**. Thay vì các script bên ngoài phải tìm kiếm cả `SkillTreeManager` lẫn `FacilityManager` và tự cộng dồn chỉ số, toàn bộ hệ thống gameplay chỉ cần gọi `RestaurantBuffBridge`.

#### Danh Mục API Công Khai:
```csharp
public static class RestaurantBuffBridge
{
    // Sự kiện duy nhất phát ra khi Skill Tree hoặc Lưới Thiết Bị có thay đổi
    public static event Action OnBuffsChanged;

    // 1. Tốc độ nấu và chế biến món ăn (Base: 1.0 = 100%)
    public static float GetTotalCookingSpeedMultiplier();

    // 2. Doanh thu bán hàng / Giá bán món ăn (Base: 1.0 = 100%)
    public static float GetTotalRevenueMultiplier();

    // 3. Thời gian kiên nhẫn của thực khách khi chờ món (Base: 1.0 = 100%)
    public static float GetCustomerPatienceMultiplier();

    // 4. Tỷ lệ giảm giá vốn nhập nguyên liệu (0.0 đến tối đa 0.80 = 80%)
    public static float GetTotalIngredientCostReduction();

    // 5. Điểm vệ sinh & danh tiếng cửa hàng
    public static int GetTotalCleanlinessBonus();

    // 6. Thông số điện năng
    public static int GetTotalPowerCapacity();
    public static int GetTotalPowerUsed();
    public static int GetRemainingPower();
    public static bool IsPowerSufficient();

    // 7. Báo cáo văn bản sạch không dùng icon (Zero-Icon Summary)
    public static string GetFormattedBuffSummary();
}
```

---

### 3.4. Hệ Thống Lưu Trữ Trạng Thái Tự Động (Save & Load System)

* **Vị trí mã nguồn:** `Assets/_Hoang/Scripts/Data/`
* **Mục tiêu:** Lưu trữ bền vững toàn bộ tiến trình nâng cấp kỹ năng và cách bài trí thiết bị trên lưới.

#### Cấu Trúc Thành Phần:
1. **`RestaurantSaveData.cs` (DTO Model):**
   * Chứa các class tuần tự hóa JSON:
     * `SkillSlotSaveEntry`: `{ skillName, currentLevel, isUnlocked }`.
     * `SkillTreeSaveState`: `{ availablePoints, availableSpecialPoints, slots }`.
     * `FacilityPlacedItemSaveEntry`: `{ itemId, itemName, originX, originY, rotationIndex }`.
     * `FacilitySaveState`: `{ inventoryItemIds, placedItems }`.
     * `RestaurantSaveData`: Cấu trúc gốc chứa version và timestamp.

2. **`RestaurantSaveManager.cs` (Singleton Quản Lý):**
   * **Dual Storage:** Lưu đồng thời ra file JSON (`Application.persistentDataPath/restaurant_save.json`) và biến `PlayerPrefs`. Khi tải, nếu file JSON bị mất hoặc lỗi sẽ tự động fallback sang `PlayerPrefs`.
   * **Tự Khởi Tạo An Toàn (Safe Lazy Init):** Tự động phát hiện hoặc sinh GameObject `RestaurantSaveManager` ngay cả trong Edit Mode mà không quăng lỗi `NullReferenceException`.
   * **Auto-Load:** Khi Scene khởi động (`Start()`), `SkillTreeManager` và `FacilityManager` tự động gọi nạp dữ liệu cũ.
   * **Auto-Save:** Mọi thay đổi về điểm số, đặt/xoay/xóa thiết bị đều tự động trigger ghi đĩa ngầm.

3. **`RestaurantSaveDataEditorTools.cs` (Editor Tools):**
   * Tích hợp thanh menu tiện ích: `Tools > Hoang > Save & Load`:
     * *Save Current Game State*: Lưu nhanh trạng thái trong Editor.
     * *Open Save Directory*: Mở ngay thư mục chứa file save trong Windows Explorer.
     * *Clear All Save Data*: Xóa sạch dữ liệu lưu để kiểm thử luồng người chơi mới.

---

### 3.5. Hệ Thống Tooltip Nổi Thông Minh (Smart UI Tooltip)

* **File:** [`Assets/_Hoang/Scripts/UI/UITooltipSystem.cs`](file:///d:/MakeGame/MyGame/GameWithVu/Project_04_BunDauPeo/Assets/_Hoang/Scripts/UI/UITooltipSystem.cs)
* **Tính năng nổi bật:**
  1. **Dynamic Pivot & Edge Clamping:** Tự động phát hiện vị trí con trỏ chuột so với màn hình (`mousePos.x > Screen.width * 0.72f` hoặc `mousePos.y < Screen.height * 0.28f`) để lật Pivot sang trái hoặc lên trên. Nhờ vậy Tooltip không bao giờ bị tràn/khuất ra ngoài rìa màn hình.
  2. **Tương thích New Input System:** Lấy vị trí chuột qua `UnityEngine.InputSystem.Mouse.current.position.ReadValue()` (kèm cơ chế try-catch an toàn cho di động).
  3. **Tự Động Sinh Giao Diện (`EnsureInstance`):** Nếu trong Scene chưa có sẵn prefab Tooltip, script sẽ tự động tạo một canvas Tooltip chuẩn với nền tối hiện đại (`Color(0.08, 0.11, 0.16)`).
  4. **Cấu trúc hiển thị 5 tầng (Zero-Icon):**
     * Tầng 1: Phân loại (Category) — Chữ hoa, màu xanh Cyan nhạt.
     * Tầng 2: Tiêu đề (Title) — Chữ đậm, màu trắng.
     * Tầng 3: Nội dung & Chỉ số (Body Stats) — Chữ thường rõ ràng.
     * Tầng 4: Trạng thái (Status) — Ví dụ: `STATUS: ACTIVE` (Màu xanh lá) hoặc `STATUS: NO POWER` (Màu đỏ cam).
     * Tầng 5: Gợi ý thao tác (Footer) — In nghiêng nhẹ, hướng dẫn click/kéo thả.

---

### 3.6. Hệ Thống Điểm Danh & Shop Tiền Tệ (Daily Rewards & Shop)

* **Vị trí mã nguồn:** `Assets/_Hoang/Scripts/ShopDaily/`, tài liệu tại `Assets/_Hoang/Docs/ShopDailyRewards.md`
* **Mục tiêu:** Cung cấp tính năng nhận quà đăng nhập hàng ngày, xem video quảng cáo nhận Kim Cương và đổi Kim Cương lấy Vàng.
* **Đặc tính kỹ thuật:**
  * **Daily Claim:** Tặng 100 Vàng + 100 Kim Cương mỗi ngày. Mốc reset: Đúng **00:00 (Nửa đêm) giờ UTC+7**. Có kiểm tra chống đổi lùi giờ hệ thống máy khách.
  * **Shop Đổi Vàng:** 3 gói đổi tỷ lệ cố định (50 Kim Cương = 1.000 Vàng; 200 Kim Cương = 4.500 Vàng; 500 Kim Cương = 12.000 Vàng). Không tích hợp nạp tiền thật IAP.
  * **Quảng Cáo Nhận Thưởng (Rewarded Ad):** Tối đa 3 lượt/ngày, mỗi lượt nhận 50 Kim Cương. Tích hợp sẵn hộp thoại giả lập trong Editor (`Complete`, `Cancel`, `Error`) để test luồng trước khi gắn SDK quảng cáo thật.
  * **Atomic Save Integration:** Mở rộng trực tiếp vào `EconomySaveData.cs` và `EconomySaveModule.cs` của hệ thống lưu trữ cốt lõi thông qua `GameSaveCoordinator`. Khi có lỗi ghi đĩa, hệ thống có cơ chế tự động thử lại 3 lần (Retry) để chống xung đột khóa đọc file trên Windows (`LocalJsonSaveRepository.cs`).
  * **Unit Test:** Bộ 12 bài test tự động tại `ShopDailyRewardsTests.cs` đạt chuẩn 100% GREEN.

---

## 4. BẢNG KÊ KHAI DANH MỤC FILE MÃ NGUỒN (CODEBASE INVENTORY)

| Phân Hệ | Đường Dẫn File | Vai Trò & Chức Năng |
| :--- | :--- | :--- |
| **Bridge** | `Assets/_Hoang/Scripts/Bridge/RestaurantBuffBridge.cs` | Facade API tập trung cung cấp chỉ số tổng hợp cho Gameplay. |
| **Data Save** | `Assets/_Hoang/Scripts/Data/RestaurantSaveData.cs` | Các lớp DTO tuần tự hóa lưu trữ kỹ năng và thiết bị. |
| | `Assets/_Hoang/Scripts/Data/RestaurantSaveManager.cs` | Quản lý Auto-Save/Load kép (JSON + PlayerPrefs). |
| | `Assets/_Hoang/Scripts/Data/Editor/RestaurantSaveDataEditorTools.cs` | Menu Editor: Lưu, mở thư mục, xóa dữ liệu save. |
| **Facilities** | `Assets/_Hoang/Scripts/Facilities/Data/FacilityItemSO.cs` | ScriptableObject định nghĩa thiết bị, hình dạng, điện năng và buff. |
| | `Assets/_Hoang/Scripts/Facilities/Logic/FacilityGrid.cs` | Ma trận 11x11 và thuật toán BFS truyền điện kèm cơ chế Relay. |
| | `Assets/_Hoang/Scripts/Facilities/Logic/FacilityManager.cs` | Controller quản lý đặt, xoay, thu hồi thiết bị và tính toán tổng buff. |
| | `Assets/_Hoang/Scripts/Facilities/UI/FacilityGridUI.cs` | UI hiển thị lưới, kéo thả New Input System, tối ưu Virtual Diff Map. |
| | `Assets/_Hoang/Scripts/Facilities/UI/FacilityPlacedItemUI.cs` | Hiển thị khối thiết bị trên lưới, tooltip và cache tọa độ. |
| | `Assets/_Hoang/Scripts/Facilities/UI/FacilityInventoryUI.cs` | Ngăn kéo danh sách thiết bị dự trữ. |
| | `Assets/_Hoang/Scripts/Facilities/UI/FacilityDraggableItemUI.cs` | Thẻ thiết bị kéo thả từ ngăn kéo vào lưới. |
| | `Assets/_Hoang/Scripts/Facilities/UI/FacilityStatsDisplayUI.cs` | HUD hiển thị công suất điện lưới và chỉ số buff trực quan. |
| | `Assets/_Hoang/Scripts/Facilities/FacilityDemoDataCreator.cs` | Công cụ tự động sinh dữ liệu trang bị mẫu để test. |
| **Skill Tree** | `Assets/_Hoang/Scripts/SkillTree/SkillSO.cs` | ScriptableObject kỹ năng, cấp độ và tỷ lệ buff cộng dồn. |
| | `Assets/_Hoang/Scripts/SkillTree/SkillSlot.cs` | Điều khiển nút kỹ năng trên cây, vẽ đường nối và xử lý nâng cấp. |
| | `Assets/_Hoang/Scripts/SkillTree/SkillTreeManager.cs` | Quản lý kho điểm, logic nâng cấp, Reset và Auto-Save. |
| | `Assets/_Hoang/Scripts/SkillTree/Editor/SkillTreeLayoutTool.cs` | Editor tool tự động tính toán và bố trí đồ thị node kỹ năng. |
| **UI Core** | `Assets/_Hoang/Scripts/UI/UITooltipSystem.cs` | Tooltip thông minh, dynamic pivot, New Input System, Zero-Icon. |
| | `Assets/_Hoang/Scripts/UI/UIRoundedCornerHelper.cs` | Tiện ích hỗ trợ tạo góc bo tròn thẩm mỹ cho uGUI. |
| **Shop & Daily** | `Assets/_Hoang/Scripts/ShopDaily/ShopDailyRewardsService.cs` | Xử lý logic điểm danh UTC+7, đổi tiền tệ và quảng cáo. |
| | `Assets/_Hoang/Scripts/ShopDaily/ShopDailyRewardsUI.cs` | Giao diện uGUI Responsive, đếm ngược nửa đêm, Editor ad dialog. |
| | `Assets/_Hoang/Scripts/ShopDaily/Editor/ShopDailyRewardsPrefabBuilder.cs` | Script Editor hỗ trợ đóng gói prefab giao diện Shop & Daily. |
| | `Assets/_Hoang/Tests/Editor/ShopDailyRewardsTests.cs` | 12 Unit test kiểm thử toàn vẹn logic giao dịch và lưu trữ. |

---

## 5. PROMPT MẪU CHUẨN DÀNH RIÊNG CHO CLAUDE PRO

*Bạn hãy sao chép toàn bộ đoạn văn bản bên dưới và dán vào cửa sổ chat của Claude Pro khi bắt đầu phiên làm việc:*

```text
Xin chào Claude Pro! Tôi cần bạn remake/tái cấu trúc lại một phân hệ lớn trong dự án game Unity 2D "Bún Đậu Pèo" (BunDauPeo) dựa trên Bản Đặc Tả Kỹ Thuật đính kèm.

MÔI TRƯỜNG & RÀNG BUỘC KỸ THUẬT BẮT BUỘC (CRITICAL):
1. Unity 6 (6000.3.15f1) sử dụng Universal Render Pipeline (URP 2D).
2. Input System: BẮT BUỘC dùng New Input System package (com.unity.inputsystem). TUYỆT ĐỐI KHÔNG dùng UnityEngine.Input cũ (không gọi Input.mousePosition vì sẽ gây InvalidOperationException). Chuột/cảm ứng phải đọc từ UnityEngine.InputSystem.Mouse.current.position.ReadValue().
3. Ràng buộc Font chữ (Zero-Icon): Font LiberationSans SDF không hỗ trợ ký tự Unicode Emoji (★, 👑, ⚡, •...). TUYỆT ĐỐI KHÔNG chèn bất kỳ icon/emoji lạ nào vào code, chuỗi string UI hay log nhằm tránh lỗi ô vuông □ (Tofu). Chỉ dùng text ASCII / tiếng Việt có dấu chuẩn kết hợp Color styling.
4. Ranh giới mã nguồn: Toàn bộ code phải nằm trong namespace riêng và đặt tại thư mục Assets/_Hoang/. Không sửa đổi scene gốc trong Assets/__Project/Scenes/ hay code ngoài phạm vi được chỉ định.
5. Kiến trúc lõi cần remake:
   - Cây kỹ năng (Skill Tree) với hệ thống điểm thường và điểm đặc biệt, tính năng Reset điểm, node đồ thị tự nối dây.
   - Lưới thiết bị 11x11 (Resident Evil style) hỗ trợ xoay khối 4 hướng chuẩn hóa tọa độ (0,0), kéo thả tối ưu Virtual Diff Map.
   - Mạng lưới điện (Power Grid) sử dụng thuật toán BFS Flood-fill truyền điện từ Nguồn phát, kiểm tra quá tải công suất Watt và hỗ trợ cơ chế Relay (khối công suất cao requiresRelay chỉ nhận điện từ isRelay hoặc isPowerSource).
   - RestaurantBuffBridge: Facade API tĩnh duy nhất kết nối chỉ số Skill Tree và Thiết bị có điện cho toàn bộ Gameplay (Tốc độ nấu, Doanh thu, Độ kiên nhẫn khách hàng, Giảm giá nguyên liệu, Vệ sinh).
   - Hệ thống Auto-Save/Load lưu trữ kép (JSON file trong Application.persistentDataPath + PlayerPrefs dự phòng), tự động khởi tạo an toàn cả trong Play Mode lẫn Edit Mode.
   - Hệ thống UITooltipSystem tự động tính toán dynamic pivot chống tràn viền màn hình.