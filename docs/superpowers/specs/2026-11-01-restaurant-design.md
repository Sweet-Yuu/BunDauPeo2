# Spec: Restaurant

> Ngày: 2026-11-01 · Trạng thái: **đã duyệt (2026-11-01, user trả lời qua session điều phối)** · Phạm vi: mô phỏng quán, xếp nhân viên, nâng cấp quán, chất lượng quán, sự cố, AFK, màn quán tối thiểu để thử.
> Kiến trúc, `Wallet`, `Roster`, `SaveService`, luật phụ thuộc asmdef: xem [spec nền móng](2026-10-30-foundation-architecture-design.md). Luật chơi: [game-design.md](../../design/game-design.md#quán). Mẫu định dạng và cách nối vào `Boot`: [spec Gacha](2026-10-31-gacha-design.md). Kế hoạch: [plans/2026-11-01-restaurant.md](../plans/2026-11-01-restaurant.md) (viết sau khi duyệt).
> **Đã xong, đây là lịch sử.** Khi spec khác code hoặc [game-design.md](../../design/game-design.md), code và game-design thắng; không sửa spec này để theo kịp.

## 1. Mục tiêu

Quán tự chạy: khách đến ngẫu nhiên, ăn và trả Coin, nhân viên (nhân vật từ `Roster`) làm theo chỉ số ở ba vai nấu, phục vụ, thu ngân. Người chơi xếp nhân viên, mua nâng cấp, xử lý sự cố khi có mặt ở màn quán. Khi vắng mặt, quán tích Coin AFK với hệ số phụ thuộc **chất lượng quán**, một thang điểm 100 đo việc người chơi chăm quán mỗi ngày và xử lý sự cố tốt. Có một màn hình tối thiểu để thử trọn vòng này khi chưa có Combat và UI hub.

### Quyết định đã chốt với user (2026-11-01)

| Chủ đề | Quyết định |
|---|---|
| Mô phỏng | Dây chuyền ba trạm, mỗi trạm ứng một vai: khách vào, ngồi bàn, **nấu**, **phục vụ** bưng ra, ăn (không cần ai), **thu ngân** thanh toán, về. Chỉ số làm việc nhanh hơn: thời gian = thời gian gốc × 100 / (100 + chỉ số). Ô nhân viên mỗi vai khởi điểm 1, bàn khởi điểm 3 (nâng cấp tăng). Khách có hai lần chờ có hạn kiên nhẫn: từ lúc đến tới khi có món, và từ lúc ăn xong tới khi trả tiền xong; quá hạn thì bỏ về **không trả tiền**. Thiếu người ở một vai thì trạm đó đứng yên, quán không thu được gì; không có "chủ quán làm thay" |
| Thu nhập lý thuyết | Công thức nút cổ chai (mục 4.2). Là cận trên của mô phỏng; chỉ dùng cho AFK, nhãn "≈ X Coin/giây" và dòng ảnh hưởng khi xếp nhân viên |
| AFK | Tích dồn đến trần 12 giờ **tổng**; chưa thu thập thì phần cũ giữ và tích tiếp đến đủ trần; đầy thì dừng, phải thu mới tích tiếp |
| AFK theo chất lượng | `AFK/giây` lý thuyết × **hệ số theo chất lượng**, chạy liên tục từ 0,1 (0 điểm) đến 0,5 (100 điểm); thay hệ số 0,3 cố định cũ. Chất lượng **không** ảnh hưởng tiền khi chơi thật. Thu nhập lý thuyết chốt lúc thoát (chặn gian lận bằng cách xếp người giỏi sát lúc thu); chất lượng tính **lúc thu thập** |
| Chất lượng (0–100, 20 điểm = 1 sao, sao lẻ hiển thị liên tục) | Chuyên cần (0–50) + Xử lý sự cố (0–50), mục 4.4 |
| Chuyên cần | 50 × trung bình của **7 ngày trọn vẹn gần nhất** (hôm qua trở về trước) của min(1, giây ở màn quán trong ngày / 5 phút). **Hôm nay không bao giờ được tính**, nên chơi hôm nay chỉ tăng chất lượng từ ngày mai, và vắng 7 ngày thì phải đi đủ một tuần mới về lại mức cũ (kiểu quest tuần; quà AFK thấp là cái giá của việc không chăm). Vắng ngày thứ 2 mới bắt đầu giảm, tối đa sau 7 ngày |
| Xử lý sự cố | 50 × trung bình điểm của 10 sự cố gần nhất (kịp 1, trễ 0,5, bỏ lỡ 0, ô trống 0). Cập nhật ngay sau mỗi sự cố |
| Sự cố (bản đầu = loại A) | Biểu tượng "!" trên một trạm, bấm một lần để xử lý: trong 8 giây là kịp, trong 20 giây là trễ, quá là bỏ lỡ. Chưa xử lý thì trạm đó đứng yên. Vai không có người vẫn sinh sự cố, nhưng **không xử lý được nếu vai đó không có nhân vật**. Chỉ sinh khi người chơi đang ở màn quán và không có panel hay popup nào đang mở; tối đa 1 sự cố cùng lúc, cách nhau 60–120 giây có mặt. Loại B (bấm rồi chọn cách xử lý) **để dành**: thêm sau không đổi save |
| Đội và quán | Một nhân vật chỉ ở một nơi. Quán cần đủ cả ba vai mới kiếm được tiền, nên 3 nhân vật khởi đầu chạy quán thì đội chiến đấu trống: đúng cân nhắc "đội mạnh hay quán đông". Luật cho **spec Combat** (user chốt): đội cần tối thiểu **1 người** để vào trận, dưới 5 người thì phải qua hộp xác nhận |
| Nâng cấp | Sáu loại (mục 3.4). Mọi cấp tốn cả Coin và Material, không có điều kiện cần có trước. **UI nâng cấp hoàn chỉnh do người khác làm** (user sẽ gửi mô tả); bản này chỉ có một cột nút để test |
| Thử khi chưa có Combat | "Dải DEV" nổi trên mọi màn, chỉ có trong Editor và Development Build: chuyển Quán ↔ Gacha, +50 Material, Tua AFK +1 giờ. Luật "Material chỉ từ chiến đấu" và `NewGameConfig` (0 Material) giữ nguyên |
| Chạy xuyên scene | `RestaurantRunner` (MonoBehaviour) thêm vào GameObject `DontDestroyOnLoad` của `GameBootstrap`, tick bằng `Time.unscaledDeltaTime` kẹp 0,5 giây mỗi khung |
| Điều hướng | `_startScene` = `Restaurant`; Gacha vào được qua dải DEV. Hub là việc cuối, không làm trước |
| Hình | Bố cục theo ba ảnh `Mota/restaurant-layout-*` (mục 5.1), dựng **trong Canvas bằng `Image`**; mô phỏng không biết tọa độ, view tự cho nhân vật đi giữa các điểm mốc |
| Save | Mọi dữ liệu mới trong `PlayerSave` qua `SaveService`. Lưu ngay sau mua nâng cấp và thu AFK; lưu định kỳ mỗi 60 giây; tắt đột ngột mất tối đa 60 giây tiền khách trả, giây có mặt và kết quả sự cố |
| Reset ([D-005](../../decisions/decisions.md)) | `GameSession.ResetCompleted` bắn sau khi ghi đè save; `GameBootstrap` nối sang `RestaurantService.ResetRuntime()` để xoá trạng thái chạy trong bộ nhớ |
| Đồng hồ | Giờ hệ thống UTC qua `IClock` tiêm vào; mô hình `Heartbeat` / `CatchUp` (mục 4.6) |
| Mở rộng sau | Công thức nấu ăn và đồ trang trí là ý tưởng **chưa thiết kế**; spec chỉ chừa đường mở rộng (mục 4.7), không định nghĩa chúng làm gì |

Mọi con số ở trên và ở các bảng dưới là **số khởi điểm đặt trong ScriptableObject**, chỉnh trong Inspector không cần sửa code.

### Vision

Không sửa [vision.md](../../vision/vision.md). Sự cố chỉ xuất hiện khi người chơi có mặt và chỉ ảnh hưởng AFK nên quán vẫn "gần như tự chạy". Nếu user thấy mô tả "gần như tự chạy" cần chỉnh thì user quyết.

## 2. Cấu trúc và ranh giới asmdef

```
Core  <-  Characters  <-  Gacha, Restaurant, Combat  <-  UI, Boot
```

| Assembly | Thêm mới / thay đổi |
|---|---|
| `Core` | `UpgradeLevel`, `DailyPresenceEntry`, `AfkState`; field mới của `PlayerSave` |
| `Characters` | không đổi |
| `Restaurant` (asmdef mới, chỉ tham chiếu `Core` và `Characters`) | SO: `RestaurantConfig`, `DishDefinition`, `CustomerDefinition`, `IncidentDefinition`, `UpgradeDefinition`, `UpgradeCatalog`. Logic thuần: `RestaurantProfile`, `IncomeCalculator`, `RestaurantSimulation`, `UpgradeService`, `StaffingService`, `RestaurantQuality`, `IncidentDirector`, `AfkService`, `IClock`/`SystemClock`, `RestaurantService`. MonoBehaviour: `RestaurantRunner` |
| `UI` (thêm tham chiếu `Restaurant`) | `RestaurantView`, `IsoProjection`, `RestaurantHud`, `StaffPanel`, `UpgradeTestPanel`, `QualityPopup`, `AfkPopup`, `RestaurantViewText`, `DevOverlay` |
| `Boot` (thêm tham chiếu `Restaurant`) | `GameBootstrap` nối `RestaurantService`; `GameSession` thêm event `ResetCompleted` |
| `Editor` (thêm tham chiếu `Restaurant`) | `ProjectSetup` sinh dữ liệu mẫu, `RestaurantSceneBuilder`, vá `Boot.unity` |
| `Tests` | thêm tham chiếu `Restaurant` |

`Restaurant` không tham chiếu `Gacha`, `Combat`, `Boot`, `UI`. Mọi SO của Restaurant chỉ trỏ tới SO cùng assembly hoặc tầng dưới.

## 3. Mô hình dữ liệu

### 3.1 Save (`Core`)

```csharp
[Serializable] public struct UpgradeLevel       { public string UpgradeId; public int Level; }
[Serializable] public struct DailyPresenceEntry { public int DayIndex;    public double Seconds; }
[Serializable] public class  AfkState {
    public long   LastActiveUtcTicks;   // 0 = chưa có mốc
    public double RatePerSecond;        // Coin lý thuyết/giây tại lần Heartbeat gần nhất
    public double AccumulatedSeconds;   // đã tích, tối đa trần
    public double PendingTheoretical;   // Coin lý thuyết đang chờ, chưa nhân hệ số chất lượng
}

// PlayerSave: thêm bốn field, có giá trị khởi tạo để save cũ vẫn đọc được.
public List<UpgradeLevel> Upgrades = new();
public List<DailyPresenceEntry> DailyPresence = new();   // tối đa 8 mục: 7 ngày trọn vẹn và hôm nay
public List<int> IncidentScores = new();                 // tối đa 10, giá trị 0 bỏ lỡ, 1 trễ, 2 kịp
public AfkState Afk = new();
```

- `Version` giữ `1`: chỉ thêm field có giá trị khởi tạo (spec nền móng 3.6). Test dựng một JSON save cũ không có bốn field này và kiểm chúng về giá trị khởi tạo, field cũ còn nguyên.
- `DayIndex` = số ngày từ 2000-01-01 theo giờ máy (`IClock.LocalNow.Date`). `Level` 0 hoặc không có dòng nghĩa là chưa mua.
- `GameSession` thêm `public event Action ResetCompleted`, bắn bằng `SafeInvoke()` trong `ResetToDefaults()` **sau** `Wallet.NotifyChanged()` và `Roster.NotifyChanged()`, **trước** `Persist()`. `GameBootstrap` nối event này vào `RestaurantService.ResetRuntime()`; Combat sau này nối cùng chỗ. Đây là cách [D-005](../../decisions/decisions.md) điều 3 yêu cầu khi có trạng thái chạy ngoài `PlayerSave`. Không cần kiểu mới ở `Core`, vì `Boot` đã tham chiếu mọi tính năng.

### 3.2 SO của `Restaurant`

| SO | Trường (private `[SerializeField]`, property đọc cùng tên) | Khởi điểm |
|---|---|---|
| `DishDefinition` (không có `Id`, không được save tham chiếu) | `_displayName`, `_icon`, `_basePrice` (long), `_cookSeconds` | mẫu 3 món: Bún đậu mắm tôm 20 Coin 6 giây; Chả cốm 15 Coin 4 giây; Nem chua rán 25 Coin 5 giây |
| `CustomerDefinition` (không `Id`) | `_displayName`, `_sprite`, `_spawnWeight`, `_patienceSeconds`, `_payMultiplier` | khách thường 6 / 40 giây / ×1,0; sành ăn 3 / 30 giây / ×1,5; khách vội 1 / 20 giây / ×1,2 |
| `IncidentDefinition` (không `Id`) | `_displayName`, `_role` (`StaffRole` bị ảnh hưởng), `_weight` | "Bếp tắc ga" (Cook), "Khách làm đổ đồ" (Server), "Máy tính tiền treo" (Cashier), trọng số 1 |
| `UpgradeDefinition : GameDefinition` | `_displayName`, `_icon`, `_effect` (`UpgradeEffect`), `_levels` (`UpgradeLevelDef[]`: `CurrencyAmount[] Cost`, `float Value`) | sáu loại ở mục 3.4 |
| `UpgradeCatalog : Catalog<UpgradeDefinition>` | | sáu nâng cấp mẫu |
| `RestaurantConfig` | bảng 3.3 | bảng 3.3 |

`UpgradeEffect { Tables = 0, CookSlots = 1, ServeSlots = 2, CashierSlots = 3, ArrivalInterval = 4, DishPrice = 5 }`: giá trị số được ghim, chỉ thêm ở cuối (bài học: `JsonUtility` và SO lưu enum dưới dạng số). `UpgradeLevelDef.Value` là **giá trị tuyệt đối tại cấp đó**: số bàn hoặc số ô cho ba loại đếm; **hệ số nhân** lên khoảng cách khách cho `ArrivalInterval`; **hệ số nhân** lên giá cho `DishPrice`. Cấp 0 lấy giá trị gốc từ `RestaurantConfig`. Số cấp tối đa = độ dài `_levels`.

### 3.3 `RestaurantConfig`

| Nhóm | Field | Khởi điểm |
|---|---|---|
| Danh sách | `_dishes`, `_customers`, `_incidents`, `_upgrades` (`UpgradeCatalog`) | mẫu như trên |
| Quán | `_baseTables` 3, `_baseCookSlots` 1, `_baseServeSlots` 1, `_baseCashierSlots` 1, `_baseArrivalInterval` 15 giây (trung bình; khoảng cách thực tế = giá trị × ngẫu nhiên đều từ 0,5 đến 1,5), `_maxWaitingLine` 4 (hàng chờ bàn tối đa; khách đến khi hàng đầy thì không vào và tính là bỏ về) | |
| Thời gian gốc | `_serveSeconds` 4, `_eatSeconds` 8, `_cashierSeconds` 3 (thời gian nấu theo từng món) | |
| Chất lượng | `_attendanceMax` 50, `_incidentMax` 50, `_dailyGoalSeconds` 300 | |
| Sự cố | `_incidentSpawnMin` 60, `_incidentSpawnMax` 120, `_incidentFastSeconds` 8, `_incidentLateSeconds` 20 | |
| AFK | `_afkMaxHours` 12, `_afkFactorMin` 0,1, `_afkFactorMax` 0,5, `_afkPopupMinSeconds` 300 | |
| Lưu | `_autosaveSeconds` 60 | |

`List<string> Validate()` (và `OnValidate` chỉ `LogWarning`) báo: danh sách món, khách, sự cố rỗng hoặc có phần tử null; `_basePrice < 1`, `_cookSeconds <= 0`, `_spawnWeight <= 0`, `_patienceSeconds <= 0`, `_payMultiplier <= 0`; mọi thời gian gốc, khoảng cách khách và `_baseTables`, ba `_base*Slots` `<= 0`; `_attendanceMax + _incidentMax != 100`; `_afkFactorMin < 0`, `_afkFactorMin > _afkFactorMax`, `_afkFactorMax >= 1`; `_afkMaxHours <= 0`; `_incidentSpawnMin > _incidentSpawnMax`, `_incidentFastSeconds >= _incidentLateSeconds`; `_autosaveSeconds <= 0`; `_dailyGoalSeconds <= 0` (chia cho 0 làm chất lượng NaN); `_afkPopupMinSeconds <= 0` (lần `CatchUp` ngắn ghi đè số giây vắng thật); `_maxWaitingLine < 1` (mọi khách bỏ về ngay); `_upgrades` null hoặc có hai nâng cấp cùng `_effect`; mỗi nâng cấp mà `Validate()` còn lỗi. `UpgradeDefinition.Validate()` báo: `Id` trống, không có cấp nào, cấp có chi phí rỗng hoặc tổng bằng 0 hoặc có dòng âm (nếu không, `TrySpend` thành công mà không trừ gì), `Value <= 0`, hoặc loại đếm mà `Value` không nguyên.

### 3.4 Sáu nâng cấp mẫu

| Nâng cấp (`Id`) | Hiệu ứng | Cấp | `Value` mỗi cấp | Coin mỗi cấp | Material mỗi cấp |
|---|---|---|---|---|---|
| Bàn (`up_tables`) | số bàn | 5 | 4, 5, 6, 7, 8 | 300, 600, 1200, 2400, 4800 | 5, 10, 15, 20, 25 |
| Ô đầu bếp (`up_cook_slots`) | số ô nấu | 2 | 2, 3 | 500, 1500 | 10, 30 |
| Ô phục vụ (`up_serve_slots`) | số ô bưng | 2 | 2, 3 | 500, 1500 | 10, 30 |
| Ô thu ngân (`up_cashier_slots`) | số ô thu ngân | 2 | 2, 3 | 500, 1500 | 10, 30 |
| Biển hiệu (`up_arrivals`) | khách đến nhanh hơn | 5 | ×0,9, ×0,81, ×0,73, ×0,66, ×0,59 | 300, 600, 1200, 2400, 4800 | 5, 10, 15, 20, 25 |
| Thực đơn (`up_prices`) | giá món | 5 | ×1,1, ×1,2, ×1,3, ×1,4, ×1,5 | 400, 800, 1600, 3200, 6400 | 5, 10, 15, 20, 25 |

### 3.5 Khác với bản nháp ở spec nền móng mục 3.3

| Bản nháp | Chốt ở đây | Lý do |
|---|---|---|
| `UpgradeDefinition`: loại hiệu ứng gồm tốc độ nấu; có "nâng cấp cần có trước" | Bỏ cả hai. Có ba loại ô, số bàn, tốc độ khách, giá món | Chỉ số nhân viên đã quyết định tốc độ nấu; giá tăng theo cấp đã làm thứ tự mua tự nhiên (user duyệt) |
| `DishDefinition.CookTime` | `CookSeconds` | Đơn vị rõ ràng |
| `RestaurantConfig`: BaseTables, khoảng thời gian khách đến, danh sách món và khách | Thêm thời gian gốc từng trạm, hàng chờ, chất lượng, sự cố, AFK, lưu định kỳ, danh sách sự cố và catalog nâng cấp | Các luật mới của spec này |
| `CustomerDefinition` | Giữ nguyên | |
| Không có | `IncidentDefinition`, `AfkState` | Luật mới |

Không có chỗ nào của spec nền móng sai hoặc thiếu đến mức phải hỏi user.

## 4. Logic

### 4.1 Hồ sơ quán (`RestaurantProfile`)

Một struct bất biến gom mọi thứ mô phỏng và công thức AFK cần, tính từ `RestaurantConfig`, cấp nâng cấp và `Roster`:

- `Tables`, `CookSlots`, `ServeSlots`, `CashierSlots` (giá trị nâng cấp nếu có, ngược lại giá trị gốc). Cấp trong save lớn hơn số cấp của SO (user vừa rút bớt cấp trong Inspector) thì lấy cấp cao nhất còn lại, giữ nguyên số trong save; `UpgradeId` lạ bị bỏ qua;
- `ArrivalIntervalMultiplier`, `PriceMultiplier`;
- ba danh sách nhân viên (`WorkerInfo { CharacterId, Stat }`) theo vai: lấy từ `Roster.GetStaff(role)` theo thứ tự trong save, **cắt còn tối đa số ô của vai** (người thừa bị bỏ qua, không bị gỡ khỏi save), `Stat` là `RestaurantStats.Cooking/Serving/Cashier`.

`RestaurantProfile.Build(config, upgrades, roster)` và một biến thể thay danh sách nhân viên của một vai (để tính dòng "→ ±X Coin/giây" khi chọn nhân viên). Đây là **điểm duy nhất** mà mô phỏng và AFK đọc: công thức nấu ăn hay đồ trang trí sau này chỉ cần đưa thêm giá trị vào đây (mục 4.7).

### 4.2 Thu nhập lý thuyết (`IncomeCalculator`, hàm tĩnh thuần)

```
thời gian xử lý của người i ở vai R = thời gian gốc(R) × 100 / (100 + chỉ số i)
  thời gian gốc: Cook = trung bình CookSeconds các món; Server = _serveSeconds; Cashier = _cashierSeconds
năng lực(R)  = Σ 1 / thời gian xử lý của từng người ở R            (khách/giây; 0 nếu không có ai)
năng lực bàn = Tables / (thời gian nấu + bưng + _eatSeconds + thanh toán của người nhanh nhất mỗi vai)
khách đến    = 1 / (_baseArrivalInterval × ArrivalIntervalMultiplier)
thông lượng  = min(khách đến, năng lực Cook, Server, Cashier, năng lực bàn)
tiền/khách   = trung bình giá món × (Σ spawnWeight × payMultiplier / Σ spawnWeight) × PriceMultiplier
Thu nhập/giây = thông lượng × tiền/khách
```

Trả `IncomeEstimate { CoinsPerSecond, MissingRole (StaffRole?) }`. `MissingRole` là vai đầu tiên không có ai (thu nhập bằng 0) để HUD ghi "Thiếu đầu bếp, quán không kiếm tiền".

Đây là **cận trên** của mô phỏng: người nhanh nhất mỗi vai, không tính khách hết kiên nhẫn, không tính ngẫu nhiên. Mô phỏng chạy thật chỉ có thể bằng hoặc kém nó (mục 6 có test kiểm).

### 4.3 Mô phỏng (`RestaurantSimulation`, class thuần)

```csharp
public RestaurantSimulation(RestaurantConfig config, System.Random random);
public void SetProfile(RestaurantProfile profile);
public void SetStationBlocked(StaffRole role, bool blocked);   // sự cố
public void Tick(float dt);                                    // chia thành bước cố định 0,1 giây
public void Reset();                                           // xoá khách, bộ đếm, hàng đợi
public IReadOnlyList<CustomerSnapshot> Customers { get; }
public IReadOnlyList<WorkerSnapshot> Workers { get; }
public int Served { get; } public int Abandoned { get; }       // bộ đếm của phiên, không lưu
public event Action<int, long> CustomerPaid;                   // id khách, số Coin; bắn bằng SafeInvoke
```

Mô phỏng **không biết `Wallet`**: `RestaurantService` nghe `CustomerPaid` rồi `Wallet.Add(Coin, số tiền)`. Mô phỏng không biết tọa độ. Ngoài `CustomerPaid` không có event nào khác: view đọc `Customers` và `Workers` mỗi khung, nên chỉ có một đường vẽ và mở lại màn không bị lệch.

**Khách** đi qua các trạng thái (`CustomerState`):

```
WaitingForTable → WaitingForCook → Cooking → WaitingForServer → Serving → Eating
                → WaitingForCashier → Paying → (trả tiền, rời)
```

- **Đến:** đồng hồ khoảng cách đếm ngược; hết thì xét hàng chờ trước: đã đủ `_maxWaitingLine` thì không tạo khách, không gọi RNG cho loại khách và món, `Abandoned` tăng. Còn chỗ thì tạo khách: một lần RNG chọn loại khách (theo `SpawnWeight`), một lần chọn món (đều). Khoảng cách kế tiếp = `_baseArrivalInterval × ArrivalIntervalMultiplier × (0,5 + RNG)`, một lần RNG. Số lần gọi RNG là hợp đồng vì test kịch bản dựa vào nó.
- **Bàn:** khách ở đầu hàng ngồi vào bàn trống thấp nhất. Ngồi xong thì vào hàng đợi nấu ngay (đặt món tự động).
- **Việc:** mỗi người trong `Profile` làm một việc một lúc, nhận khách đầu hàng đợi của vai (FIFO theo lúc vào trạm). Thời gian việc = thời gian gốc × 100 / (100 + chỉ số), cố định lúc nhận việc. Cook nấu món của khách, Server bưng món ra bàn, Cashier thanh toán.
- **Ăn:** `_eatSeconds`, không cần ai.
- **Kiên nhẫn:** hai đồng hồ `PatienceSeconds` của loại khách. Đồng hồ 1 chạy từ lúc khách đến tới lúc bắt đầu `Eating` (gồm chờ bàn, chờ nấu, nấu, chờ bưng, bưng). Đồng hồ 2 chạy từ lúc ăn xong tới lúc thanh toán xong. Hết một trong hai thì khách rời **không trả tiền**, việc đang làm của khách bị huỷ ngay (người làm rảnh), nhả bàn, `Abandoned` tăng.
- **Trả tiền:** `Math.Round(BasePrice × PayMultiplier × PriceMultiplier, MidpointRounding.AwayFromZero)`, tối thiểu 1, `CustomerPaid`, `Served` tăng, khách rời.
- **Thiếu người ở một vai** (danh sách rỗng): hàng đợi của vai đứng yên; khách chờ tới hết kiên nhẫn rồi bỏ về.
- **Trạm bị chặn** (`SetStationBlocked`): người của vai đó ngừng nhận việc mới và việc đang làm đóng băng tiến độ, cho tới khi mở lại.
- **Đổi hồ sơ giữa chừng** (`SetProfile`, gọi khi `Roster.Changed` hoặc mua nâng cấp): người vẫn còn ở cùng vai thì giữ việc; người bị rút đi hoặc vượt số ô thì việc của họ **quay lại đầu hàng đợi của trạm**, mất tiến độ; thêm bàn hoặc người thì có hiệu lực ngay. Không bao giờ tính tiền cho nhân vật đã chuyển sang đội chiến đấu.
- **Tick:** `Tick(dt)` cộng dồn `dt` và chạy bước cố định 0,1 giây, nên kết quả không phụ thuộc tốc độ khung hình. Người gọi kẹp `dt`.

### 4.4 Chất lượng (`RestaurantQuality`)

```
Chuyên cần  (0–50) = _attendanceMax × (Σ min(1, giây của ngày d / _dailyGoalSeconds), d = hôm qua … 7 ngày trước) / 7
                     ngày không có dòng trong save tính 0
Sự cố       (0–50) = _incidentMax   × (Σ điểm / 2 của 10 sự cố gần nhất) / 10     ô trống tính 0
Chất lượng  (0–100) = Chuyên cần + Sự cố            Sao = Chất lượng / 20 (số thực, hiển thị sao lẻ)
Hệ số AFK = Lerp(_afkFactorMin, _afkFactorMax, Chất lượng / 100)
```

- `RecordPresence(seconds)`: cộng vào dòng của hôm nay (`IClock.LocalNow`), tạo dòng nếu chưa có, giữ tối đa 8 dòng (bỏ dòng có `DayIndex` nhỏ nhất). Dòng "tương lai" (đồng hồ lùi hay đổi múi giờ) không bị xoá trừ khi danh sách vượt 8, và không nằm trong cửa sổ 7 ngày.
- `RecordIncident(outcome)`: thêm điểm (0/1/2), giữ 10 mục mới nhất.
- Ví dụ người chơi đều đặn có phần Sự cố tối đa rồi bỏ game: chất lượng 100, 93, 86, 79, 71, 64, 57, 50 sau 0 … 7 ngày vắng đầy đủ, hệ số AFK 0,50 → 0,30. Chất lượng không bao giờ thấp hơn phần Sự cố.

### 4.5 Sự cố (`IncidentDirector`)

- Chỉ chạy khi `SetPresent(true)` (view quán đang mở, app ở tiền cảnh). Đếm ngược ngẫu nhiên đều từ `_incidentSpawnMin` đến `_incidentSpawnMax` (RNG tiêm vào); hết thì chọn một `IncidentDefinition` theo trọng số trong **mọi** sự cố, kể cả sự cố của vai đang không có người, và kích hoạt, nếu chưa có sự cố nào đang chạy.
- **Vai không có người:** `bool Resolve()` trả `false` và không đổi gì khi vai của sự cố không có nhân vật; "!" ghi rõ cần xếp người vào vai nào. Đồng hồ 8 và 20 giây vẫn chạy, người chơi xếp người kịp thì bấm xử lý được như thường, không thì thành bỏ lỡ (0 điểm). Vì vậy quán chưa xếp ai không cày được điểm. Sự cố đang chạy mà người duy nhất của vai bị rút đi cũng theo luật này.
- **Không sinh** khi còn ít nhất một chủ chặn (`SetSpawnBlocked(owner, true)`): mỗi panel hoặc popup tự gọi với chính nó lúc mở và gỡ lúc đóng, nên popup AFK chồng lên panel Nhân viên rồi đóng thì panel vẫn chặn; vì "!" bị che thì bỏ lỡ là phạt oan. Khi đó giữ đồng hồ ở 0 và sinh ngay khi mở lại. Sự cố đang chạy khi người chơi tự mở panel vẫn đếm tiếp, kể cả panel Nhân viên mở ra để xếp người xử lý sự cố.
- Khi kích hoạt: `RestaurantSimulation.SetStationBlocked(role, true)`. `Resolve()` trong `_incidentFastSeconds` là kịp (2), trong `_incidentLateSeconds` là trễ (1); quá hạn thì tự bỏ lỡ (0). Kết thúc bằng bất cứ cách nào cũng mở lại trạm và `RestaurantQuality.RecordIncident`.
- **Rời màn quán hoặc app vào nền khi sự cố đang chạy:** sự cố bị huỷ, **không tính điểm**, trạm mở lại (nếu không, quán nghẽn mãi khi đang ở Combat). Hệ quả chấp nhận: người chơi tránh phạt bằng cách rời màn; họ không có điểm cộng nào từ việc đó.
- Sự cố loại B (có lựa chọn) để dành: `IncidentDefinition` thêm field loại xử lý, mặc định là loại hiện tại, nên save không đổi.

### 4.6 AFK (`AfkService`) và đồng hồ

`IClock { DateTime UtcNow; DateTime LocalNow; }` tiêm vào (`SystemClock` ở production, đồng hồ giả ở test). `AfkService(save, wallet, config, clock, quality, Func<double> currentIncomePerSecond, Func<bool> persist)`.

```
Heartbeat():   Afk.LastActiveUtcTicks = clock.UtcNow.Ticks;  Afk.RatePerSecond = Coin lý thuyết/giây hiện tại
CatchUp():     gọi lúc mở app và lúc quay lại từ nền
   nếu LastActiveUtcTicks == 0: chỉ đặt mốc (game mới), không cộng gì
   elapsed = now − LastActive;  nếu elapsed ≤ 0 (đồng hồ lùi): elapsed = 0
   nếu RatePerSecond > 0:                                                ← quán không kiếm tiền thì không ăn vào trần
      thêm = min(elapsed, _afkMaxHours×3600 − AccumulatedSeconds)        ← trần tổng
      PendingTheoretical += RatePerSecond × thêm;  AccumulatedSeconds += thêm
   LastActiveUtcTicks = now                                              ← tiêu thụ khoảng thời gian đúng một lần
   nếu elapsed ≥ _afkPopupMinSeconds: PendingAbsenceSeconds = elapsed    ← giữ cho view, lần CatchUp ngắn không ghi đè
PreviewCollect(): floor(PendingTheoretical × quality.AfkFactor)           ← hệ số tính lúc bấm
Collect():        Wallet.Add(Coin, PreviewCollect()); PendingTheoretical = 0; AccumulatedSeconds = 0;
                  Saved = persist(); trả AfkCollectResult { Coins, Saved }
TakePendingAbsence(): trả PendingAbsenceSeconds rồi đặt về 0 (không lưu vào save)
```

Vì sao có nhánh `RatePerSecond > 0`: nếu thoát lúc chưa xếp đủ ba vai, 12 giờ vắng làm đầy trần mà `PendingTheoretical` vẫn bằng 0. Nút Thu AFK không hiện, nên không có cách đặt lại trần và AFK bằng 0 mãi mãi.

- **Khi nào `Heartbeat`:** `OnApplicationPause(true)`, `OnApplicationQuit`, và mỗi lần lưu định kỳ. App bị hệ điều hành kill hoặc crash (không kịp Pause/Quit) chỉ lệch tối đa `_autosaveSeconds`, nghiêng về phía người chơi và nằm dưới trần 12 giờ.
- **Không cộng đôi:** `CatchUp` luôn đẩy mốc lên "bây giờ", gọi lần hai liền sau đó cộng 0. Khoảng thời gian ở tiền cảnh là chơi thật; lúc đó mốc được cập nhật liên tục bằng lưu định kỳ.
- **Đồng hồ lùi:** không phạt, chỉ đặt lại mốc. **Đồng hồ tiến** gian lận: chấp nhận ([D-003](../../decisions/decisions.md)), trần 12 giờ chặn.
- **Popup:** không dùng event, vì `CatchUp` đầu tiên chạy trong `GameBootstrap.Awake` khi màn quán chưa tồn tại, và người chơi có thể quay lại app lúc đang ở Gacha. `RestaurantView` gọi `TakePendingAbsence()` ở `OnEnable` và mỗi khung; kết quả `> 0` và `PreviewCollect() > 0` thì hiện `AfkPopup`. Nút "Thu AFK (X Coin)" cố định trên HUD hiện khi `PendingTheoretical > 0`.
- **Ví dụ minh hoạ** (số mẫu, không phải cân bằng): thu nhập lý thuyết ≈ 1,56 Coin/giây, vắng 5 giờ 12 phút, chất lượng 62 (hệ số 0,35): chờ 1,56 × 18 720 = 29 203 Coin lý thuyết, nhận ⌊29 203 × 0,35⌋ = 10 221 Coin.

### 4.7 Chừa đường mở rộng (ý tưởng chưa thiết kế)

Công thức nấu ăn, đồ trang trí và bất cứ ý tưởng nào của user sau này gắn vào qua ba điểm, không phải viết lại mô phỏng hay AFK:

1. **Hồ sơ quán** (mục 4.1) là điểm duy nhất cung cấp chỉ số cho `RestaurantSimulation` và `IncomeCalculator`; nguồn mới chỉ thêm giá trị vào đó.
2. **Field save mới** luôn có giá trị khởi tạo ([D-005](../../decisions/decisions.md)); enum chỉ thêm ở cuối (`UpgradeEffect`, `StaffRole`).
3. **`RestaurantService`** là cổng duy nhất tạo và nối các dịch vụ của Restaurant; thêm dịch vụ mới ở đó, và `ResetRuntime()` xoá luôn trạng thái chạy của dịch vụ đó.

Đây là phần của spec **không** định nghĩa công thức nấu ăn hay đồ trang trí làm gì, vì user chưa nghĩ ra. Ghi `TODO(user)` ở Ngoài phạm vi.

### 4.8 Nâng cấp (`UpgradeService`) và nhân viên (`StaffingService`)

```csharp
public UpgradeService(PlayerSave save, Wallet wallet, UpgradeCatalog catalog, Func<bool> persist);
public int GetLevel(string id);
public UpgradeBuyResult TryBuy(string id);       // Status, Saved, NewLevel
public event Action Changed;                      // sau khi mua
public enum UpgradeBuyStatus { Success, UnknownUpgrade, InvalidUpgrade, MaxLevel, NotEnoughCurrency }
```

`TryBuy`, theo thứ tự, khớp spec nền móng 4.3 và hợp đồng Gacha: tra id (`UnknownUpgrade`) → `Validate()` còn lỗi (`InvalidUpgrade`) → đã tối đa (`MaxLevel`) → `wallet.CanAfford(chi phí cấp kế)` (`NotEnoughCurrency`, chưa đổi gì) → `wallet.TrySpend` (sai thì `NotEnoughCurrency`) → tăng cấp trong `save.Upgrades` → `Changed` → `persist()` đúng **một** lần; `false` thì `Saved = false`, trạng thái bộ nhớ giữ nguyên, không hoàn tiền (`OnApplicationPause`/`Quit` sẽ thử lại).

```csharp
public StaffingService(Roster roster, Func<RestaurantProfile> currentProfile);   // số ô lấy từ hồ sơ, không phụ thuộc UpgradeService
public StaffAssignResult TryAssign(string characterId, StaffRole role);   // Success, NotOwned, RoleFull
public StaffAssignResult TryReplace(string oldId, string newId, StaffRole role);
public bool Remove(string characterId);
```

`Roster.AssignStaff` không kiểm số ô (spec nền móng 3.5 giao việc đó cho Restaurant), nên `StaffingService` từ chối khi vai đã đầy. `TryAssign` một nhân vật đang giữ vai khác thì chuyển vai (giữ nguyên hành vi `Roster`); đang trong đội chiến đấu thì `Roster.AssignStaff` tự gỡ khỏi đội, việc xác nhận là của UI.

### 4.9 `RestaurantService`, `RestaurantRunner` và nối dây

`RestaurantService` (class thuần) tạo và nối mọi thứ trên: `RestaurantProfile` mới mỗi khi `Roster.Changed` hoặc `UpgradeService.Changed`, rồi `SetProfile`; nghe `CustomerPaid` để `Wallet.Add(Coin)`. `ResetRuntime()` gọi `RestaurantSimulation.Reset()`, huỷ sự cố, xoá `PendingAbsenceSeconds`, đặt lại bộ đếm phiên và đồng hồ lưu định kỳ (dữ liệu trong `PlayerSave` đã được ghi đè nên không cần xoá). Constructor chạy `CatchUp()` một lần.

**Cấu hình lỗi:** `config.Validate()` còn lỗi thì `IsRunning = false`, `Debug.LogError` một lần liệt kê lỗi. `Update` không tick, `Estimate` trả 0 và AFK không tích thêm, giữ nguyên số đã có trong save; HUD ghi "Cấu hình quán lỗi". Gacha và phần còn lại của game vẫn chạy. Nếu không có nhánh này, danh sách món rỗng sẽ gây lỗi ở mọi khung.

Có:

- `Update(float unscaledDt)`: `Tick` mô phỏng; `IncidentDirector.Update`; nếu `SetPresent(true)` thì `RecordPresence(dt)`; đếm `_autosaveSeconds` rồi `Heartbeat()` + `persist()`.
- `SetPresent(bool)` từ view quán; `OnAppPause()` (huỷ sự cố, `Heartbeat()`); `OnAppResume()` (`CatchUp()`).
- `Estimate` (`IncomeEstimate` hiện tại), `Quality`, `Upgrades`, `Staffing`, `Afk`, `Simulation`, `Incidents`.

`RestaurantRunner : MonoBehaviour`: `Init(RestaurantService)` và `Update() => service.Update(Mathf.Min(Time.unscaledDeltaTime, 0.5f))`. Tick bằng `unscaledDeltaTime` để nút tăng tốc hay tạm dừng của Combat (`Time.timeScale`) không đổi tốc độ quán; kẹp 0,5 giây để khung đứng hình (nạp scene) không làm khách đến dồn một lúc. Khi app ở nền Unity không gọi `Update`, nên quán dừng và khoảng đó được tính là AFK, không bù cho mô phỏng.

`GameBootstrap` (cùng khuôn nối Gacha, spec Gacha 4.4): thêm `[SerializeField] RestaurantConfig _restaurantConfig` (kiểm null giống các field khác); tạo `RestaurantService`, `AddComponent<RestaurantRunner>()` lên chính GameObject, đăng ký `RestaurantService` vào `ServiceLocator`, `session.ResetCompleted += service.ResetRuntime`; `OnApplicationPause(true)` gọi `OnAppPause()` **trước** `Persist()`, `OnApplicationPause(false)` gọi `OnAppResume()`; `OnApplicationQuit` gọi `OnAppPause()` rồi `Persist()`.

## 5. UI tối thiểu

Chỉ để thử; hub thật ở spec UI hub. Màn ngang, `CanvasScaler` tham chiếu 1920×1080. **Mọi hình trong Canvas là component `Image`**: không gán sprite (hình vuông đặc) hoặc dùng `UISprite`/`Knob` có sẵn, không `Raw Image`, không thêm file ảnh ([unity-workflow.md](../../guides/unity-workflow.md#ui-và-hình-tạm)). Chữ dùng `Text` uGUI với font dựng sẵn. Skill nạp khi làm: `unity:ui-ugui` (Canvas), `unity:unity-cli` (dựng scene). Không cần Tilemap hay sprite atlas.

### 5.1 Màn quán (`Restaurant.unity`)

Bố cục theo ba ảnh `Mota/restaurant-layout-garden-en.webp`, `-farm-en`, `-sea-en` (user chốt 2026-11-01). Ảnh dùng art thật; game này dựng cùng bố cục bằng hình có sẵn.

```
 [Coin 1.250] [Material 35] [Gem 1500]                                       ┌───┐
          ┌ ★★★☆☆  ≈ 1,6 Coin/giây  ·  Phục vụ 12 · Bỏ về 2 ┐                │(N)│ Nhân viên
          └ (khi = 0: ⚠ Thiếu đầu bếp...) ─────────────────┘  ▂▂▂ tường sau  │   │
      cây        ▛▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀ dãy bếp ▀▀▀▀▀▀▀▀▀▀▀▄▄▄             │(N)│ Nâng cấp
                ▌                    nhân viên      bàn   bàn    ▀▀▀▄▄       │   │
     tường trái ▌  quầy thu ngân                                       ▀▄    │(N)│ Thu AFK
               ▌   +24 ↑              bàn   bàn   bàn   bàn   bàn       ▌    └───┘
               ▙▄▄▄▄▄[cửa]▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▌
 [dải DEV]         luống · luống · luống · luống · luống      (chỉ trang trí)
```

- **Phòng:** một sàn lớn **một màu** kem sáng, không còn lưới ô màu xen kẽ. Sàn là **một `Image` chữ nhật không gán sprite, xoay khoảng −11,3°**, nên hai cạnh dài dốc nhẹ xuống bên phải, hai cạnh ngắn dốc mạnh xuống bên trái như trong ảnh; không cần cha nén trục y. Lưới 12 × 8 ô: `IsoProjection.CellToLocal(gx, gy)` = (90·gx − 13·gy, −18·gx − 65·gy), hai trục vuông góc nên khớp đúng phép xoay; `DepthKey` = 18·gx + 65·gy. Sàn chiếm khoảng 1180 × 740 trên Canvas 1920×1080, lệch sang trái để chừa cột nút.
- **Tường:** có tường thấp ở cạnh sau và cạnh trái (`Image` thanh dài, cùng góc xoay với sàn). Cạnh trước và cạnh phải để mở.
- **Đồ vật:** hộp màu gỗ cùng góc xoay với sàn, to và có lối đi giữa các hộp. Bếp là dãy dài sát tường sau, nửa bên phải (gx khoảng 5,5–11,5). Quầy thu ngân ở bên trái phòng (gx khoảng 0,6–3,2, gy khoảng 3–4). Tám bàn xếp hai hàng bốn ở giữa và bên phải phòng (gy khoảng 3,4 và 5,8; gx khoảng 4,6–10,6). Cửa là một khoảng thảm ở cạnh trước, phía trái (gx khoảng 2–4), gần quầy để khách trả tiền xong về nhanh. Đây là vị trí khởi điểm, chỉnh khi thử cho không đè nhau.
- **Người:** khách là hình tròn, nhân viên là `Knob` kèm tên, to rõ so với đồ, bong bóng sự cố là `Image` "!". Người đứng thẳng, không xoay theo sàn: lớp `Actors` nằm ngoài phần xoay. Thứ tự vẽ sắp theo toạ độ y khi người di chuyển.
- **Cảnh quanh phòng:** nền lấp kín màn bằng màu pastel (xanh lá nhạt, theo ảnh vườn), không còn nền tối. Trang trí tĩnh tối đa khoảng 20 `Image`: vài cây hình tròn phía sau, hàng luống hình vuông phía trước phòng, lối đi từ cửa. **Chỉ để nhìn**, tắt raycast, không có luật.
- **HUD:** ba thẻ tiền trên-trái (nền trắng bo góc `UISprite`, chữ tối: Coin, Material, Gem). Giữa-trên, dưới hàng thẻ, là một thanh trắng nửa trong suốt chứa sao chất lượng, nhãn thu nhập và bộ đếm; thanh được phép đè lên tường sau như ảnh. Chữ phải đọc rõ trên nền sáng.
- **Cột nút phải:** một dải dọc nền trắng bo góc sát mép phải, chứa nút tròn (`Knob`) có chữ, đủ 3 ô, cỡ chạm từ 110 pixel: **Nhân viên** (có sẵn), **Nâng cấp** (Task 10), **Thu AFK** (Task 13). Ô chưa có nút thì để trống, không tạo nút giả.
- **Không lấy từ ảnh** (ngoài [vision](../../vision/vision.md) và luật game): thanh tiến độ màn và hộp quà, nút chân mèo và nhân vật người chơi điều khiển, trồng trọt, nhập hàng lên kệ, chồng đồ trên tay, bong bóng "0/4", nút SAVE, menu, túi quà. Panel Nhân viên giữ nguyên mục 5.2.
- **Điểm mốc** (do builder đặt): cửa, bếp (đến 3 ô), bàn (đến 8), quầy thu ngân (đến 3 ô). `IsoProjection` là hàm tĩnh thuần quy đổi ô lưới sang vị trí trong Canvas, có test.
- **Nhân vật đi lại:** mỗi khung view đọc `Simulation.Customers` và `Workers` rồi đối chiếu theo id: khách mới thì tạo ở cửa, khách biến mất thì gỡ, trạng thái đổi thì nội suy tới điểm mốc mới (khoảng 1,5 giây, nhảy thẳng tới đích nếu trạng thái đổi sớm hơn). Đi lại chỉ là hoạt hình, không ảnh hưởng thu nhập. Mở lại màn thì đặt thẳng mọi người vào điểm mốc của trạng thái hiện tại. Số bàn hoặc ô vượt số điểm mốc thì dùng chung điểm mốc cuối, chỉ ảnh hưởng hình.
- **Kiếm tiền:** chữ "+N" bay lên từ quầy mỗi khi `CustomerPaid` (bể 8 đối tượng), số Coin trên thanh nhảy theo `Wallet.Changed`, nhãn "≈ X Coin/giây" từ `RestaurantService.Estimate`, bộ đếm "Phục vụ / Bỏ về" từ `Simulation`, và khi thu nhập bằng 0 thì thay nhãn bằng lý do từ `IncomeEstimate.MissingRole`. Sao và cảnh báo dựng bằng `Image` (hình vuông hay tròn tô màu, `Image.type = Filled` cho sao lẻ), vì font dựng sẵn có thể thiếu ★ và ⚠.
- **Có mặt:** `RestaurantView.OnEnable` gọi `SetPresent(true)`, `OnDisable` gọi `SetPresent(false)`.
- **Số cuối sau khi thử (Task 9b, 2026-11-02).** Gốc lưới (góc sau-trái của sàn) ở (310, 220) tính từ góc trái trên của Canvas 1920×1080, neo giữa-trên (x = −650) nên màn rộng hơn 16:9 thì phòng ở lại gần giữa; `CanvasScaler` của scene này khớp theo chiều cao (`matchWidthOrHeight = 1`) vì bố cục cao đúng 1080 và Game view rộng (2658×960) với 0,5 chỉ còn canvas cao 865 và cắt phía dưới. Sàn 12 × 8 ô = 1102 × 530 pixel, xoay −11,31°. Tường sau và trái rộng 0,55 ô, kèm dải sáng nhích lên 26 pixel. Bếp: gx 5,5–11,5, gy 0,15–1,35. Quầy: gx 0,6–3,2, gy 3,0–4,0. Bàn 1,2 × 1,0 ô, tâm gx 5,2 / 7,0 / 8,8 / 10,6, hàng gy 3,4 rồi 5,8. Cửa là thảm gx 2–4, gy 7,7–8,3, điểm mốc (3, 8). Điểm đứng: đầu bếp gy 2,3 ở gx 6,5 / 8,5 / 10,5; phục vụ (4,4, 1,6), (4,4, 2,3), (3,6, 1,9); thu ngân gy 2,4 ở gx 1 / 2 / 3. Khách trả tiền đứng ở điểm thu ngân đầu cộng 150 pixel xuống (phía trước quầy). Người: khách 54, nhân viên 64 pixel. Thẻ tiền 340 × 76 tại x 30 / 390 / 750, y 20; thanh giữa-trên 1120 × 120 tại (240, 108); cột nút phải 170 × 520, cách mép phải 20 và mép trên 110, nút Nhân viên tròn 130. Trang trí: 8 vòng tròn (cây, bụi hoa), lối từ cửa, 6 luống và 3 mầm, tất cả tắt raycast. Test `RestaurantSceneTests` giữ điểm mốc trong sàn, đồ không đè nhau và thẻ/cột nằm trong Canvas.

### 5.2 Panel Nhân viên (`StaffPanel`, theo mẫu `Mota/quest-team-select-popup.jpg`)

Ba cột (Đầu bếp, Phục vụ, Thu ngân), mỗi cột là các ô: số ô mở theo nâng cấp, ô chưa mở hiện khoá kèm gợi ý nâng cấp. Bấm ô → danh sách nhân vật đã có, sắp theo chỉ số của vai, mỗi dòng ghi tên, độ hiếm, chỉ số và "→ ±X Coin/giây" (tính bằng `IncomeCalculator` với danh sách giả định). Nhân vật đang trong đội chiến đấu ghi nhãn "Đội chiến đấu" và hỏi xác nhận ("X đang ở đội chiến đấu, chuyển vào quán?") vì `Roster.AssignStaff` tự gỡ khỏi đội. Chọn nhân vật đang giữ vai khác thì chuyển vai, không xác nhận. Ô đang có người có nút "Gỡ".

### 5.3 Nâng cấp (`UpgradeTestPanel`)

Chỉ là một cột nút test: mỗi nâng cấp một nút ghi tên, cấp hiện tại/tối đa và giá; nút mờ khi thiếu tiền hoặc đã tối đa (`Wallet.CanAfford`); bấm khi `Status != Success` hoặc `Saved == false` thì hiện dòng thông báo giống Gacha. **Không có thu nhập trước/sau, không có bố cục hoàn chỉnh**: UI nâng cấp thật do người khác làm theo mô tả user sẽ gửi, và thay view này mà không đụng `UpgradeService`.

### 5.4 Popup

- **`AfkPopup`:** khi vào màn quán sau khi vắng từ 5 phút: thời gian vắng, thu nhập lý thuyết chờ, chất lượng và sao, hệ số AFK, số Coin nhận, mẹo "vào quán đủ 5 phút mỗi ngày để tăng chất lượng", nút Thu thập và Để sau; nếu đã đầy trần thì thêm dòng "Đã đầy, hãy thu thập để tích tiếp".
- **`QualityPopup`** (bấm thẻ sao): Chuyên cần x/50 với 7 ô ngày, dòng "Hôm nay: a/b phút (tính vào ngày mai)", Xử lý sự cố y/50 với "Kịp · Trễ · Bỏ lỡ · Chưa có", hệ số AFK hiện tại.

### 5.5 Dải DEV (`DevOverlay`)

Toàn bộ file `DevOverlay.cs` nằm trong `#if UNITY_EDITOR || DEVELOPMENT_BUILD`. Một hàm `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` tự dựng Canvas bằng code (`Image`, `Button`, `Text`), `DontDestroyOnLoad`. Không có prefab, không có field trong `GameBootstrap`, không phải vá scene, nên không có gì của nó trong bản phát hành. Không đặt `[SerializeField]` trong `#if` (lệch bố cục serialize giữa Editor và bản build), và không dùng `OnGUI` (khi Active Input Handling chỉ là Input System, bản build không nhận sự kiện IMGUI).

Nút: **Quán / Gacha** (chuyển scene), **+50 Material** (`Wallet.Add`), **Tua AFK +1 giờ** (lùi `LastActiveUtcTicks` đi một giờ rồi gọi `CatchUp()` ngay, nếu không lần lưu định kỳ kế tiếp sẽ đè mốc). Mọi nút đi qua dịch vụ, không chạm file save; Reset Save Data xoá sạch như thường. Chưa có service (Boot lỗi) thì không dựng dải. Hub sau này xoá dải này.

### 5.6 Scene và Boot

Scene `Restaurant.unity` do `RestaurantSceneBuilder` sinh một lần (có rồi thì giữ nguyên): `Camera`, `EventSystem` với **`InputSystemUIInputModule`** (không `StandaloneInputModule`, xem spec Gacha mục 5), Canvas, các view trên. `ProjectSetup` thêm vào Build Settings sau `Gacha` (thứ tự `Boot`, `Gacha`, `Restaurant`), gán `_restaurantConfig` khi còn trống, và đổi `_startScene` từ `Gacha` sang `Restaurant` **một lần** (chỉ khi đang là `Gacha`, để không ghi đè lựa chọn khác của user); `Gacha.unity` không đổi. Có test đọc lại từ file (bài học L-002).

## 6. Test

EditMode, cùng assembly `BunDauPeo.Tests`. Số test cụ thể ở kế hoạch. Mọi test logic tiền hay thời gian phải có đầu vào mà cài đặt sai cho kết quả khác (bài học L-006), và có ca "handler ném lỗi" cho event mới (L-003).

- **Save và tương thích:** JSON cũ không có bốn field mới đọc được, field cũ còn nguyên; round-trip qua `SaveService`; `ResetToDefaults` ghi đè hết bốn field và bắn `ResetCompleted` đúng một lần, sau `Wallet.Changed` và `Roster.Changed`, trước `Persist()`; handler ném lỗi không chặn việc còn lại.
- **Config và SO:** từng lỗi `Validate()` ở mục 3.3 có một ca; `UpgradeDefinition.Validate()` (chi phí rỗng, tổng 0, dòng âm, `Value <= 0`, loại đếm không nguyên); cấu hình mẫu hợp lệ; hai nâng cấp cùng hiệu ứng bị báo.
- **`UpgradeService`:** thiếu Coin hoặc thiếu Material (một trong hai) thì không đổi ví, cấp, `Changed`, số lần `persist`; đủ thì trừ cả hai đúng và tăng một cấp; đã tối đa; id lạ; nâng cấp lỗi cấu hình; `persist` trả `false` thì `Saved = false` mà bộ nhớ đúng; `persist` đúng một lần; mua liên tiếp trừ đúng từng cấp.
- **`StaffingService`:** vai đầy thì `RoleFull`; số ô tăng thì gán được; `TryReplace` đổi người, vai khác chuyển vai; nhân vật trong đội chiến đấu được gỡ khỏi đội; chưa sở hữu thì `NotOwned`; `Remove`.
- **`RestaurantProfile`:** cắt nhân viên thừa theo số ô mà không gỡ khỏi save; giá trị gốc khi cấp 0, giá trị nâng cấp khi có; Id lạ bị bỏ qua; cấp trong save vượt số cấp của SO thì lấy cấp cao nhất, không lỗi, số trong save giữ nguyên.
- **`IncomeCalculator`:** từng nút cổ chai (Arrival, Cook, Server, Cashier, Tables) có một ca với số tay tính được; thiếu vai thì 0 và `MissingRole` đúng; thêm người, thêm ô, thêm bàn tăng đúng; đổi chỉ số đổi đúng theo công thức 100/(100+chỉ số).
- **`RestaurantSimulation` (RNG kịch bản, `ScriptedRandom` có sẵn):** một khách đi hết dây chuyền với đúng thời gian và đúng số Coin; hai khách xếp hàng FIFO ở một đầu bếp; hàng chờ đầy thì khách không vào và không gọi RNG cho loại khách và món; giá làm tròn đúng ở x,5 (15 × 1,5 = 23); hết kiên nhẫn ở từng trạng thái thì không trả tiền và nhả bàn; thiếu người ở mỗi vai (ba ca) thì khách bỏ về; chỉ số cao làm việc nhanh hơn đúng công thức; trạm bị chặn đóng băng và tiếp tục khi mở; rút người giữa chừng trả việc về đầu hàng đợi; thêm bàn hoặc người có hiệu lực ngay; `Tick` với `dt` lớn bằng nhiều `dt` nhỏ (kết quả không phụ thuộc tốc độ khung); số lần gọi RNG đúng hợp đồng; `Reset` xoá hết.
- **Bất biến mô phỏng và công thức** (seed cố định, không flaky; đỏ nghĩa là thuật toán đổi, **không** đổi seed): chạy đủ lâu với vài seed trên cấu hình mẫu ở nhiều trạng thái (mới, đủ ba vai, sau nâng cấp, quá tải): thu nhập thật/giây **không vượt** thu nhập lý thuyết (dung sai nhỏ) và **lớn hơn** thu nhập lý thuyết × `_afkFactorMax`; tức AFK luôn thấp hơn chơi thật và công thức đúng là cận trên.
- **`RestaurantQuality`:** đồng hồ giả: hôm nay không bao giờ được tính; một ngày đủ mục tiêu tính 1/7 của phần Chuyên cần; ngày vắng tính 0; vắng 1 … 7 ngày giảm đúng bảng ở mục 4.4; sự cố kịp/trễ/bỏ lỡ; ô trống tính 0; giữ đúng 10 mục và 8 dòng; đồng hồ lùi và dòng "tương lai" không làm hỏng; hệ số AFK tại 0, 50, 100 điểm.
- **`IncidentDirector`:** không sinh khi không có mặt; vai không có người vẫn sinh; `Resolve` trả `false` và không tính điểm khi vai không có người, trả `true` sau khi xếp người trong hạn; hết hạn mà không có người thì bỏ lỡ; rút người giữa chừng thì không xử lý được; không sinh khi còn chủ chặn (hai chủ, gỡ một thì vẫn chặn; cùng chủ chặn hai lần gỡ một lần là hết) và sinh ngay khi chủ cuối gỡ; sự cố đang chạy vẫn đếm khi mở panel; chỉ một sự cố cùng lúc; kịp/trễ/bỏ lỡ đúng ở biên 8 và 20 giây (đúng tại 8,0 và 20,0 và sát hai bên); trạm bị chặn rồi mở lại; rời màn hoặc vào nền thì huỷ không tính điểm và mở lại trạm; chọn theo trọng số; số lần gọi RNG đúng hợp đồng.
- **`AfkService` (đồng hồ giả):** tích đúng `rate × elapsed`; trần 12 giờ **tổng** qua hai lần thoát (8 giờ + 8 giờ chỉ cộng 4 giờ ở lần hai); đầy rồi không tích thêm cho đến khi thu; đồng hồ lùi cộng 0 và đặt lại mốc; đồng hồ tiến 20 giờ chặn ở trần; hai lần `CatchUp` liên tiếp cộng 0 (không cộng đôi khi vào nền rồi mở lại); game mới (`LastActive = 0`) không cộng gì; **`RatePerSecond = 0` vắng 12 giờ không ăn vào trần**, lần sau có người thì vẫn tích đủ; `TakePendingAbsence` trả số giây vắng rồi về 0, và `CatchUp` ngắn liền sau không ghi đè số đang chờ (`OnApplicationPause(false)` lúc khởi động); kill giữa chừng với heartbeat cũ 59 giây; `Collect` cộng đúng vào ví theo hệ số **lúc thu**, về 0 và lưu một lần, `Saved = false` khi `persist` thất bại mà bộ nhớ đúng; nhân vật chuyển sang đội sau lần `Heartbeat` không đổi `RatePerSecond` đã chốt (rate chốt lúc thoát) nhưng lần `Heartbeat` kế tiếp thì đổi.
- **`RestaurantService`:** `CustomerPaid` thành `Wallet.Add(Coin)` đúng một lần; nhân vật chuyển sang đội thì mô phỏng thôi tính ngay (không còn tiền từ người đó); mua nâng cấp có hiệu lực ngay trong mô phỏng; `ResetRuntime` xoá khách, sự cố, bộ đếm, số giây vắng đang chờ, và `Wallet`/`Roster`/save đúng như D-005; cấu hình lỗi thì `IsRunning = false`, `Update` không ném lỗi, `Estimate` bằng 0, AFK không tích thêm; lưu định kỳ `Heartbeat` + `persist` đúng chu kỳ; `RecordPresence` chỉ tăng khi `SetPresent(true)`.
- **`IsoProjection`, `RestaurantViewText`, `StaffPanel` (logic chọn và ảnh hưởng thu nhập):** hàm tĩnh thuần có test.
- **`ProjectSetup`:** dữ liệu mẫu hợp lệ; chạy lại không ghi đè số đã chỉnh; `Boot.unity` có `_restaurantConfig`, `_startScene == "Restaurant"`; scene `Restaurant` có `Camera`, `EventSystem` + `InputSystemUIInputModule` (và không `StandaloneInputModule`), không `RawImage`; `Restaurant` thứ ba trong Build Settings; test `GachaSceneTests.BootScene_StartSceneIsGacha` đổi thành kiểm `Restaurant`.

## 7. Gói triển khai

Chi tiết từng task, model/effort, lệnh và số test cộng dồn ở [kế hoạch](../plans/2026-11-01-restaurant.md) (viết sau khi duyệt). Dự kiến **15 task, chia ba giai đoạn**, mỗi giai đoạn chơi thử được và là chỗ dừng tự nhiên giữa hai session ([agent-routing.md](../../guides/agent-routing.md), không chia giữa một task). Làm quán thấy được trước, vì đợt Gacha cho thấy cảm giác chơi chỉ chỉnh được khi đã bấm thử, và nếu phải đổi mô phỏng thì Chất lượng, Sự cố, AFK chưa phải viết lại.

**Giai đoạn A: quán chạy và thấy được. Dừng để user thử.**

1. `Core`: kiểu save, `GameSession.ResetCompleted`, xác nhận mốc test.
2. Asmdef `Restaurant`, các SO và `Validate()`.
3. `RestaurantProfile`, `StaffingService`.
4. `IncomeCalculator`.
5. `RestaurantSimulation`: dây chuyền và trả tiền.
6. `RestaurantSimulation`: kiên nhẫn, thiếu người, trạm bị chặn, đổi hồ sơ, Reset.
7. `RestaurantService`, `RestaurantRunner`, bất biến mô phỏng và công thức, `GameBootstrap`, `ProjectSetup`: dữ liệu mẫu, vá Boot, Build Settings.
8. UI: `IsoProjection`, `RestaurantView`, `RestaurantHud`, `RestaurantSceneBuilder`, `DevOverlay` (chuyển scene).
9. UI: `StaffPanel`; thử trong Play Mode.

**Giai đoạn B: nâng cấp.**

10. `UpgradeService`, `UpgradeTestPanel`, nút +50 Material.

**Giai đoạn C: chất lượng, sự cố, AFK.**

11. `IClock`, `RestaurantQuality`.
12. `IncidentDirector`, biểu tượng "!" trong view.
13. `AfkService`, nút Thu AFK, nút Tua AFK.
14. UI: `AfkPopup`, `QualityPopup`; thử trong Play Mode.
15. Cập nhật docs.

## 8. Ngoài phạm vi

- **Công thức nấu ăn, đồ trang trí:** ý tưởng đã có nhưng chưa thiết kế, `TODO(user)`: chúng làm gì (mở khoá món, cộng chỉ số, chỉ làm đẹp…). Spec chỉ chừa đường mở rộng ở mục 4.7.
- **UI nâng cấp hoàn chỉnh** (bố cục, thu nhập trước/sau): người khác làm, `TODO(user)` gửi mô tả. Bản này chỉ có `UpgradeTestPanel`.
- Sự cố loại B (bấm rồi chọn cách xử lý), kéo thả nhân viên vào trạm, "tự xếp tối ưu", biểu đồ thu nhập, chất lượng ảnh hưởng tiền khi chơi thật, nâng cấp cần có trước.
- Hub, chấm báo AFK ở nút Quán, âm thanh, hoạt hình thật.
- Kiểm trên Android thật (đã hoãn ở [roadmap](../../roadmap.md)).
- Luật vào trận của đội chiến đấu (tối thiểu 1 người, xác nhận khi dưới 5): thuộc spec Combat, đã ghi vào [game-design.md](../../design/game-design.md#nhân-vật) để Combat theo.

## 9. Câu hỏi còn mở

- `TODO(user)`: công thức nấu ăn và đồ trang trí (mục 8).
- `TODO(user)`: mô tả UI nâng cấp hoàn chỉnh.
- Số cân bằng (thu nhập khởi điểm khoảng 1,6 Coin/giây, giá nâng cấp, hệ số 0,1–0,5) chỉ là số khởi điểm; cân bằng khi có Combat cho Material thật. **Đã thấy trước:** Coin mẫu quá dễ so với giá nâng cấp. Tổng mọi nâng cấp là 37.000 Coin và 345 Material; ở mức thu khởi điểm, một lần AFK đầy 12 giờ cho 6.739 Coin (hệ số 0,1) đến 33.696 Coin (hệ số 0,5), một giờ chơi thật cho khoảng 5.600 Coin. Với số này, Material gần như là thứ duy nhất giới hạn nâng cấp, và Coin hết chỗ tiêu sớm. `TODO(user)`: giữ vậy hay tăng giá Coin khi cân bằng.
- Số nhân vật khởi đầu (3) giữ nguyên; user đổi trong `NewGameConfig` khi cân bằng nếu muốn đội chiến đấu không trống ngay từ đầu.

## 10. Rủi ro

- **AFK vượt trần hoặc sai khi đồng hồ lùi:** trần tổng, `elapsed ≤ 0`, mốc đẩy lên "bây giờ" sau mỗi `CatchUp`; sai ở ranh giới trần (hai lần thoát) là lỗi dễ mắc nhất. Test ở mục 6.
- **AFK kẹt vĩnh viễn:** vắng khi quán chưa kiếm tiền mà vẫn tính vào trần thì đầy trần mà không có gì để thu. Nhánh `RatePerSecond > 0` ở mục 4.6 chặn việc này, có test riêng.
- **AFK cao hơn hoặc bằng chơi thật:** phụ thuộc công thức là cận trên đúng *và* hệ số tối đa 0,5 nhỏ hơn tỉ lệ thật/lý thuyết. Công thức sai (lỡ cho cận dưới) hoặc đổi `_afkFactorMax` lên cao vẫn qua `Validate` mà làm AFK vượt chơi thật; test bất biến mô phỏng–công thức bắt cả hai.
- **Tiền cộng đôi khi app vào nền rồi mở lại:** `CatchUp` phải luôn đẩy mốc; `OnAppPause` phải `Heartbeat` **trước** `Persist()`; Unity không đảm bảo thứ tự gọi giữa các MonoBehaviour nên `GameBootstrap` gọi tường minh.
- **Trừ tiền nâng cấp không nhất quán:** thứ tự tra → kiểm → `CanAfford` → `TrySpend` → tăng cấp → lưu ở mục 4.8 là hợp đồng, test phải bắt từng bước hỏng, kể cả thiếu một trong hai loại tiền.
- **Nhân vật chuyển sang đội mà mô phỏng vẫn tính:** `SetProfile` phải chạy trên `Roster.Changed` và trả việc dở về hàng đợi; test kiểm cả thu nhập thôi tính và việc không biến mất. Hai sự kiện liên tiếp của `TryReplace` (gỡ rồi gán) chỉ làm hồ sơ đổi hai lần, không sai.
- **Reset bỏ sót trạng thái chạy:** mô phỏng, sự cố đang chạy, bộ đếm phiên, số giây vắng đang chờ, đồng hồ lưu định kỳ nằm ngoài `PlayerSave` nên `ResetRuntime()` phải xoá; test kiểm từng thứ (D-005). Dữ liệu trong `PlayerSave` tự xoá nhưng mỗi field mới phải có assert.
- **Save cũ không đọc được:** bốn field mới đều có giá trị khởi tạo, `Version` giữ 1, test JSON cũ.
- **`OnApplicationPause` chạy trước hay sau `Start`:** `CatchUp` ở constructor và `OnAppResume` đều idempotent nhờ mốc, nên thứ tự không gây cộng đôi.
- **Hai Editor tranh project:** Play thử ghi vào save thật; sao lưu và Reset sau lượt thử (xem L-004, L-007).
- **Ký hiệu trong font dựng sẵn:** ★ và ⚠ có thể hiện ô vuông; dùng `Image` thay vì ký tự.
- **Tránh phạt bằng cách rời màn khi có sự cố** (mục 4.5): chấp nhận, vì không sinh ra điểm cộng.
- **Chuyên cần dựa vào giờ máy:** đổi múi giờ hay chỉnh đồng hồ làm số ngày nhảy; chấp nhận vì offline ([D-003](../../decisions/decisions.md)), lịch sử không bị xoá vì một lần đổi giờ.
- **Dải DEV lọt vào bản phát hành:** cả file chỉ biên dịch theo `UNITY_EDITOR || DEVELOPMENT_BUILD` và không có asset nào trỏ tới nó, nên bản phát hành không có gì để lọt.
