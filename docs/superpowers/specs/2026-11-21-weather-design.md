# Spec: Thời tiết (mưa làm khách ít đi)

> Ngày: 2026-11-21 · Trạng thái: **user duyệt thiết kế và ảnh mô phỏng 2026-11-21**, chờ user duyệt bản viết · Phạm vi: Restaurant (mô phỏng quán, dịch vụ mới), `PlayerSave`, Lobby (thẻ + lớp phủ), panel DEV
> Nguồn: [roadmap](../../roadmap.md) dòng Thời tiết (master plan), [D-005](../../decisions/decisions.md), [spec Restaurant](2026-11-01-restaurant-design.md), [spec Online đợt 2](2026-11-16-online-2-design.md) (mục 1: quán, Coin do máy tính). Kế hoạch: [weather](../plans/2026-11-21-weather.md).

## 1. Mục tiêu

Thỉnh thoảng trời mưa và quán vắng khách hơn trong 10 phút. Mưa là ngẫu nhiên; người chơi liền tay càng lâu thì càng dễ gặp mưa. Mưa **chỉ có hại**, không có thưởng.

Thành công khi:
1. Trời mưa thì lượng khách đến giảm đúng một nửa (khoảng cách giữa hai khách ×2) cho tới khi mưa tạnh. Mọi thứ khác của quán (thời gian nấu, giá, kiên nhẫn) giữ nguyên.
2. Mưa kéo dài đúng 10 phút theo giờ thật, kể cả khi ra nền hoặc tắt app.
3. Xác suất mưa theo đường cong ở mục 3; tắt mở app không lách được (mưa còn hạn vẫn còn, bộ đếm chơi liên tục vẫn đúng).
4. Người chơi thấy rõ mưa (thẻ "Mưa · còn m:ss" + lớp xanh mờ phủ quán). Người thử có panel DEV để gọi mưa, tạnh mưa, tua bộ đếm mà không phải chơi 3 tiếng.
5. Reset Save Data xoá hết trạng thái thời tiết; AFK và sự cố không đổi.

**Ngoài phạm vi (anti-goal):** thời tiết có lợi (nắng đông khách), nhiều loại thời tiết, hạt mưa rơi, âm thanh, thưởng/phạt ngoài lượng khách, ảnh hưởng AFK, dòng trong nút Thông báo, popup giải thích. Ý "chơi lâu thì khó hơn" không có thông báo hay lời nhắc nghỉ; nó chỉ là xác suất.

## 2. Quyết định đã chốt với user (2026-11-21)

| # | Câu hỏi | Chốt |
|---|---|---|
| 1 | Mưa dài bao lâu | **10 phút cố định** (thay "tối đa 30 phút" của roadmap). Ra nền hoặc tắt app vẫn tính theo giờ thật |
| 2 | "Chơi liên tục" | Cộng thời gian app ở tiền cảnh (kể cả đang đánh trận). **Ra nền từ 10 phút trở lên thì tính lại từ 0.** Khi mưa tạnh cũng tính lại từ 0 |
| 3 | Đường cong xác suất | Bảng mục 3, đã chỉnh: **0–15 phút = 0%** |
| 4 | Nơi giữ trạng thái | **Máy, trong `PlayerSave`** (phương án A, mục 6). Server và luật tiền không đổi |
| 5 | UI | Ảnh mô phỏng đã duyệt: thẻ `WeatherBadge`, lớp phủ xanh mờ, mục "Thời tiết" ở panel DEV (mục 5) |

**Lựa chọn của 🎨 (user chưa xét riêng, sửa được):** mưa làm khách đến chậm ×2 (khởi điểm, nằm trong config); thời tiết không ảnh hưởng AFK, sự cố, Chuyên cần, và không ảnh hưởng số "≈ Coin/giây" trên `InfoBar` (số đó vẫn là thu nhập lý thuyết); mưa vẫn chạy khi đánh trận vì quán chạy nền; quay xác suất mỗi 5 phút đang chơi.

## 3. Luật

**Bộ đếm "chơi liên tục"** (`ContinuousSeconds`): tăng theo thời gian chạy của quán (tiền cảnh). Không tăng khi đang mưa (mưa tạnh thì về 0 nên không cần). Về 0 khi: mưa tạnh, hoặc lúc mở lại app/quay lại từ nền mà đã vắng **từ 10 phút trở lên**.

**Quay mưa:** khi không mưa, cứ mỗi 5 phút đang chơi (`SecondsSinceRoll` đủ 300) thì quay một lần với xác suất `Chance(phút chơi liên tục)`. Trúng thì mưa bắt đầu. Đang mưa thì không quay.

**Xác suất** (mọi số là config, khởi điểm):

| Chơi liên tục (phút) | Xác suất mỗi lần quay |
|---|---|
| 0 đến dưới 15 | 0% |
| 15 đến dưới 90 | 5% |
| 90 đến dưới 180 | tăng đều từ 5% lên 15% (t=135 → 10%) |
| từ 180 | 15% + 15% × số lần quay đã trôi từ phút 180 (180→15%, 185→30%, 190→45% … ) |
| trần | 90% |

Công thức từ phút 180: `min(MaxChance; FastStartChance + FastStepChance × floor((t − FastStartMinutes) / RollIntervalMinutes))`. Quay ở phút 205 trở đi luôn là 90%.

**Mưa:** bắt đầu ở thời điểm `now` (giờ `IClock.UtcNow`: giờ server khi online) và kết thúc ở `now + 10 phút`; lưu `RainEndsUtcTicks`. Khi mưa: khoảng cách giữa hai khách ×2 (mục 4). Hết mưa: `RainEndsUtcTicks = 0`, `ContinuousSeconds = 0`, `SecondsSinceRoll = 0`; vì phải qua 15 phút mới có xác suất khác 0 nên sau mưa luôn có ít nhất 15 phút trời quang.

**Mốc hoạt động** (`LastActiveUtcTicks`): ghi mỗi lần autosave (60 giây, cùng chỗ `Afk.Heartbeat`) và khi app vào nền. Lúc mở app hoặc quay lại từ nền (`CatchUp`): khoảng vắng = `now − LastActiveUtcTicks` (âm thì tính 0). Rồi theo thứ tự:
1. Đang có `RainEndsUtcTicks` mà `now ≥ RainEndsUtcTicks` → mưa tạnh (reset như trên).
2. Chưa mưa mà khoảng vắng ≥ 10 phút → `ContinuousSeconds = 0`, `SecondsSinceRoll = 0`.
3. Còn lại: giữ nguyên (khoảng vắng dưới 10 phút không cộng vào bộ đếm).
Rồi `LastActiveUtcTicks = now`.

Hệ quả: app bị kill thì mất tối đa một chu kỳ 60 giây của mốc; chấp nhận được (cùng giới hạn với AFK).

## 4. Ghép vào mô phỏng quán

- `RestaurantSimulation.AdvanceArrivals` hiện trừ `_arrivalTimer -= Step`. Thêm thuộc tính `ArrivalSlowdown` (mặc định 1, ≥ 1) và đổi thành `_arrivalTimer -= Step / ArrivalSlowdown`. Cách này có hiệu lực ngay giữa một khoảng chờ khách, và không đụng `RestaurantProfile` (hồ sơ dựng lại mỗi khi đổi nhân viên/nâng cấp).
- `RestaurantService.Update` mỗi khung đặt `Simulation.ArrivalSlowdown = Weather.ArrivalSlowdown` (bằng `RainArrivalMultiplier` khi mưa, 1 khi quang) rồi `Tick`.
- `IncomeCalculator`, `AfkService`, `IncidentDirector`, `RestaurantQuality` **không đổi**.

## 5. UI

Ảnh mô phỏng user duyệt 2026-11-21 (tên object theo [unity-workflow](../../guides/unity-workflow.md) mục UI và hình tạm; hình chỉ là `Image` vuông/tròn có sẵn):

| Vùng | Tên object | Ghi chú |
|---|---|---|
| Thẻ thời tiết | `WeatherBadge_PH_380x68` | Ở Lobby, bên phải `InfoBar` (cùng hàng, cách 8 px). Nền xanh lam đậm, viền sáng, chấm tròn trắng + chữ "Mưa · còn m:ss" (đếm ngược mỗi giây). **Chỉ hiện khi mưa**; trời quang thì ẩn hẳn, không chiếm chỗ. Không bấm được |
| Lớp phủ | `WeatherTint_PH` | `Image` kéo kín khung quán (dưới thanh tài nguyên và thanh dưới), xanh lam, alpha khoảng 0,28, **không chặn chạm** (`raycastTarget` tắt). Chỉ hiện khi mưa |
| Panel DEV | mục "Thời tiết" trong `DevOverlay` | Trạng thái: "Đang: Mưa, còn m:ss / Quang", "Chơi liên tục: N phút", "Xác suất mỗi lần quay: P%", "Lần quay kế: m:ss". Bốn nút: **Gọi mưa** (mưa 10 phút ngay), **Hết mưa** (tạnh ngay, đếm lại từ 0), **Tua +30 phút** (cộng 1800 giây vào `ContinuousSeconds`, không quay), **Quay ngay** (quay một lần theo xác suất hiện tại; không đổi `SecondsSinceRoll`). Chữ tiếng Việt cố định như phần còn lại của panel DEV |

Chữ thẻ nằm trong `UiText` (khu mới `weather`, hai ngôn ngữ, [D-010](../../decisions/decisions.md)): `weather.rain` = "Mưa · còn {0}" / "Rain · {0} left". Đổi ngôn ngữ nạp lại scene nên không cần cập nhật chữ trực tiếp. Thẻ và lớp phủ do `RestaurantSceneBuilder` dựng (không sửa tay scene); một `WeatherView` đọc `WeatherService` mỗi khung và ghi chữ khi giây đổi.

Thẻ nằm ở Lobby, **không** hiện ở panel kín màn (chúng phủ lên Lobby), như `InfoBar`. Ở Trận không có (Trận là scene riêng); quay lại Lobby thì thẻ hiện đúng thời gian còn lại.

## 6. Dữ liệu và nơi giữ trạng thái

### 6.1 Các phương án đã cân

| | Cách | Kết luận |
|---|---|---|
| **A. Máy, `PlayerSave.Weather`** | Dịch vụ thuần C# trong Restaurant, giờ từ `IClock` (server khi online) | **Chọn.** Quán, Coin do máy tính (spec Online đợt 2 mục 1) nên cùng chỗ. Không đổi server, `GameData`, luật tiền. Tắt app vẫn đúng 10 phút. D-005 xử lý sẵn |
| B. Server (`ServerState`) | Lệnh Cloud Code để quay mưa | Loại: mỗi lần quay (5 phút) phải chặn màn bằng `WaitOverlay`, xuất lại dữ liệu cân bằng, sửa luật đợt 2, mà Coin vẫn do máy giữ. Chi phí lớn, không lợi |
| C. Chỉ bộ nhớ | Không lưu | Loại: tắt mở app là hết mưa, trái ý "ra nền vẫn tính 10 phút" |

Sửa file save để bớt mưa thì được (không ai hại ai, thời tiết chỉ làm khách ít đi). Đổi giờ máy không lách được vì `IClock` là `ServerClock` khi online (cùng cách AFK).

### 6.2 `PlayerSave.Weather` (D-005)

```csharp
[Serializable]
public class WeatherState   // Core/Scripts/WeatherState.cs, namespace BunDauPeo.Core
{
    public long RainEndsUtcTicks;      // 0 = không mưa
    public double ContinuousSeconds;   // chơi liên tục, giây
    public double SecondsSinceRoll;    // tính từ lần quay trước, giây
    public long LastActiveUtcTicks;    // 0 = chưa có mốc
}
// PlayerSave: public WeatherState Weather = new();
```

| Luật D-005 | Cách đáp ứng |
|---|---|
| Dữ liệu cần lưu chỉ ở `PlayerSave`, đi qua `SaveService` | `Weather` là field của `PlayerSave`; không `PlayerPrefs`, không file riêng |
| Field mới có giá trị game mới đúng | `new WeatherState()` = toàn 0 = trời quang, chưa có mốc. Save cũ không có field vẫn nạp được, ra giá trị đó |
| Trạng thái chạy ngoài save tự reset | `WeatherService` không giữ bản sao: đọc `_save.Weather` mỗi lần (như `AfkService` đọc `_save.Afk`), nên `ResetToDefaults()` (ghi đè `PlayerSave`) tự xoá. `ResetRuntime()` của `RestaurantService` gọi thêm `Weather.ResetRuntime()` để gạt sự kiện và phát `Changed` cho view |
| Online (D-014, đợt 2) | `Weather` nằm trong khoá `save` (do máy giữ), **không** ở `ServerState`. Bản sao tải lên của đợt 2 chỉ bỏ các field server giữ nên `Weather` đi theo. Đợt 2 ghi "`PlayerSave` không thêm field" chỉ cho đợt 2; thời tiết thêm sau khi đợt 2 đã merge. `ResetPlayer` trên server không cần đổi |

Hai máy cùng tài khoản: `save` đi theo cơ chế `ActiveSessionId` có sẵn, máy nào đang chơi thì giữ trạng thái của máy đó; không xử lý riêng.

### 6.3 Config (`RestaurantConfig`, Header "Thời tiết", chỉnh trong Inspector)

| Field | Khởi điểm | Ghi chú |
|---|---|---|
| `RainMinutes` | 10 | thời lượng mưa |
| `AwayResetMinutes` | 10 | vắng từ đây thì đếm lại |
| `RollIntervalMinutes` | 5 | chu kỳ quay |
| `GraceMinutes` | 15 | dưới mốc này xác suất 0 |
| `BaseChance` | 0,05 | |
| `SlowStartMinutes` | 90 | từ đây xác suất tăng đều từ `BaseChance` lên `FastStartChance` |
| `FastStartMinutes` | 180 | từ đây tăng nhanh; cũng là điểm cuối của đoạn tăng chậm |
| `FastStartChance` | 0,15 | xác suất đúng ở phút 180 (cuối đoạn chậm, bậc đầu đoạn nhanh) |
| `FastStepChance` | 0,15 | cộng thêm mỗi chu kỳ quay sau phút 180 |
| `MaxChance` | 0,9 | trần |
| `RainArrivalMultiplier` | 2 | khoảng cách khách ×2 |

`Validate()` thêm: các mốc tăng dần (`Grace ≤ SlowStart < FastStart`), xác suất trong [0, 1], `BaseChance ≤ FastStartChance ≤ MaxChance`, các thời lượng > 0, `RainArrivalMultiplier ≥ 1`. Config chưa có asset field mới thì Unity dùng giá trị khởi tạo trong code; không sửa tay `.asset`.

## 7. Cấu trúc code

| Thành phần | Nơi | Ghi chú |
|---|---|---|
| `WeatherState` | `Core/Scripts` | mục 6.2 |
| `WeatherCurve.Chance(minutes, config)` | `Restaurant/Scripts` | hàm tĩnh thuần, mục 3 |
| `WeatherService` | `Restaurant/Scripts` | thuần C#, tiêm `RestaurantConfig`, `PlayerSave`, `IClock`, `System.Random`, `Func<bool> persist`. API: `IsRaining`, `RainRemainingSeconds`, `ContinuousMinutes`, `CurrentChance`, `SecondsToNextRoll`, `ArrivalSlowdown`, `Changed`, `Update(dt)`, `CatchUp()`, `Heartbeat()`, `ForceRain()`, `ForceClear()`, `AddContinuous(seconds)`, `RollNow()`, `ResetRuntime()` |
| Nối vào `RestaurantService` | `Restaurant/Scripts` | thuộc tính `Weather`; ctor gọi `CatchUp` (như AFK); `Update`, autosave (Heartbeat), `OnAppPause`, `OnAppResume`, `ResetRuntime` gọi tương ứng. Bắt đầu mưa gọi `persist()` ngay một lần (để kill app không xoá mưa); tạnh mưa thì để autosave |
| `RestaurantSimulation.ArrivalSlowdown` | `Restaurant/Scripts` | mục 4 |
| `WeatherView`, thẻ, lớp phủ | `UI/Scripts`, `Editor/RestaurantSceneBuilder` | mục 5 |
| Mục DEV | `UI/Scripts/DevOverlay.cs` | theo mẫu nút "Tua AFK" |
| Chữ | `UiText.Weather.cs` (file `partial` mới) | key `weather.*` |

`GameBootstrap` không đổi: nó đã gọi `OnAppPause`/`OnAppResume` của `RestaurantService`.

## 8. Test

Tất cả EditMode trong `BunDauPeo.Tests` (cùng chỗ `LobbySceneTests`); đồng hồ giả như `AfkServiceTests`, `System.Random` giả bằng lớp con ghi đè `NextDouble`. Danh sách cụ thể ở [kế hoạch](../plans/2026-11-21-weather.md); tổng cộng **+39 EditMode, +0 PlayMode** (có kiểm thủ công ở playtest, mục 9). Phủ: đường cong (từng bậc, biên 15/90/180, trần), chu kỳ quay, mưa 10 phút, qua nền, tạnh khi vắng, vắng 9 và 10 phút, đồng hồ lùi, nạp lúc mở app, Reset, lệnh DEV, khách giảm một nửa trong mô phỏng, AFK không đổi, bản sao tải lên giữ `Weather`, thẻ và lớp phủ ẩn/hiện, chữ hai ngôn ngữ.

## 9. Playtest (người thử, trên Editor chế độ Local trước)

1. Panel DEV → Gọi mưa: thẻ hiện "Mưa · còn 10:00", quán xanh mờ, khách đến thưa rõ rệt; mở panel khác thì thẻ ẩn khỏi tầm nhìn nhưng mưa vẫn đếm.
2. Gọi mưa, thoát Play, mở lại sau 3 phút: còn khoảng 7 phút; mở lại sau 11 phút: đã quang.
3. Quang, Tua +30 phút nhiều lần: xác suất hiển thị khớp bảng; Quay ngay nhiều lần ở 180+ phút thì gần như luôn mưa.
4. Reset Save Data: quang, bộ đếm 0, thẻ ẩn.

## 10. Rủi ro và đã biết

- Mưa bắt đầu giữa lúc đang chờ khách kế tiếp: có hiệu lực ngay nhờ chia bước thời gian, không đợi lần đến kế.
- Đổi `RainArrivalMultiplier` hay đường cong sau này là việc cân bằng riêng, chỉ sửa config.
- "Chơi liên tục" tính cả lúc đang ở Trận và panel; chỉ app ra nền mới ngắt. Người chơi để màn sáng suốt đêm sẽ gặp mưa liên tục: đúng ý user (chơi lâu thì khó hơn), không xử lý thêm.
- Roadmap ghi "tối đa 30 phút"; đã sửa thành 10 phút cố định theo user.

## 11. Docs phải sửa (trong kế hoạch)

`roadmap.md` (đã làm / chuyển Đã xong khi xong), `game-design.md` mục Quán (đã ghi luật chốt), `screen-flow.md` (bố cục Lobby thêm `WeatherBadge`, panel DEV), `docs/project/structure.md` (file mới), `decisions.md` không cần (không đổi hướng).
