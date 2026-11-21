# Spec con: Online đợt 3 — liên kết tài khoản, Nhiệm vụ, Hộp thư

> **Trạng thái:** **user duyệt 2026-11-21** (kèm đổi: dùng chung một nút Google qua Play Games, bỏ Google ID token riêng; ảnh mô phỏng mục 8 vẫn cần user duyệt khi xem ảnh thật trước khi 🔨 dựng UI). Spec cha: [online-design](2026-11-13-online-design.md) (mục 5.9, 5.10). Dựa trên đợt 2: [spec con](2026-11-16-online-2-design.md), [kế hoạch](../plans/2026-11-16-online-2.md). Spec này chỉ ghi phần đợt 3 mà spec cha chưa chốt hoặc user đã đổi; chỗ nào không nói thì theo spec cha và spec con đợt 2. **Chỗ khác spec cha ghi ở mục 2 và mục 14.**
> Ảnh tham khảo: `hub-missions*.{png,jpg}`, `hub-mailbox-en.png`, `restaurant-layout-target-en.png` (nút Hộp thư) trong `ReferenceImages/`, mục lục [ui-screens.md](../../references/ui-screens.md).

## 1. Mục tiêu

Đợt 3 cho người chơi **giữ được tiến độ khi đổi máy** (liên kết tài khoản) và **có lý do mở game mỗi ngày** (Nhiệm vụ, thể lực theo khung giờ, lượt quay miễn phí). Xong khoảng 22/12, có điểm dừng playtest trên `development`.

Gồm:
1. **Liên kết tài khoản** từ tài khoản ẩn danh: **Google** (Android) và **tên đăng nhập + mật khẩu**. Đăng nhập lại bằng các cách đó từ màn Tiêu đề.
2. **Nhiệm vụ**: tab Ngày (4 việc, 1 việc khoá) và tab Tân thủ (9 việc), thưởng do server trao.
3. **Lượt quay miễn phí mỗi ngày** ở Gacha.
4. **Hộp thư** có một thư ghim nhắc liên kết tài khoản.
5. **Thể lực nhận thêm vượt trần 100**, tối đa 999.

**Ngoài phạm vi đợt 3:** Hộp quà phát từ server (`ClaimedGiftIds`, Custom data `gifts`) và hai dòng Thông báo `MissionReward`/`GiftReward` (đợt 4, hỏi lại user); Google (ID token) riêng, Unity Player Accounts, Facebook, Apple, Game Center; hệ thống cấp nhân vật và trang bị; tab Thành tựu; vẽ lại quán (xem [ideas](../../design/ideas.md#để-sau)). Không kiểm trận trên server (như spec cha).

## 2. Quyết định đã chốt với user (2026-11-21)

| # | Chủ đề | Chốt |
|---|---|---|
| 1 | Play Console | **Đã có tài khoản.** Google Play Games là đường chính, không còn "dự phòng chỉ khi trễ" |
| 2 | Phương thức đăng nhập | **Hai cách:** **Google** (một nút "Google" cho người chơi; trên Android làm bằng Google Play Games bên dưới) và **tên đăng nhập + mật khẩu**. Liên kết được cả hai cùng một người chơi. Ban đầu user chọn ba cách; user đổi 2026-11-21: dùng chung một nút Google, bỏ cách Google (ID token) riêng |
| 3 | Cách vào lần đầu | **Luôn vào ẩn danh trước**, chỉ liên kết khi người chơi bấm (không tự đăng nhập Google khi cài mới) |
| 4 | Nhắc liên kết | **Thư ghim** trong Hộp thư (không xoá được, **biến mất sau khi liên kết**) và **việc tân thủ** "Liên kết tài khoản" thưởng **100 Gem**. **Không** có chấm đỏ avatar, **không** có dòng Thông báo |
| 5 | Hộp thư | Hình **phong thư** thay Hộp quà; làm ở **đợt 3**, chỉ có thư ghim; **lấy đủ như ảnh tham khảo** (danh sách trái, nội dung phải, đính kèm, Nhận, Nhận hết, Xoá đã đọc, đếm số lượng, dấu ghim, chấm chưa đọc). Đợt 4 thêm thư quà vào cùng trang |
| 6 | Màn Nhiệm vụ | **Như ảnh `hub-missions*`**: tab Ngày / Tân thủ, dòng có thanh tiến độ, nút bên phải; đã nhận thì nút **xám, không bấm được**; hàng dưới có giờ còn lại, tiến độ chung, Nhận hết. **Không** có thanh mốc thưởng, **không** có tab Thành tựu |
| 7 | Việc ngày (thay hoàn toàn danh sách cũ) | Thắng 5 trận ở chế độ bất kỳ; nâng lv một nhân vật (**khoá "Sắp có"**: game chưa có cấp); quay gacha 1 lần; thể lực khung 00:00–12:00; thể lực khung 12:00–24:00. Tất cả server đếm |
| 8 | Thưởng | 20 Gem cho thắng 5 trận và quay gacha; **100 thể lực** mỗi khung giờ; việc tân thủ giữ số của spec cha, thêm "Liên kết tài khoản" 100 Gem |
| 9 | Khung giờ thể lực | Hai khung **00:00–12:00** và **12:00–24:00** (giờ Việt Nam, theo giờ server), mỗi khung nhận một lần |
| 10 | Trần thể lực | Nhận thưởng được **vượt 100**, **tối đa 999**; phần dư mất. Chạm trần thì **hộp xác nhận hỏi lại** trước khi nhận |
| 11 | Lượt quay miễn phí | Mỗi ngày **1 lượt ×1 miễn phí**, **chỉ banner hiện tại**; tính vào pity, lịch sử và việc "Quay gacha 1 lần" |
| 12 | Cách làm phía server | **Server đếm trực tiếp** trong các handler đợt 2; việc tân thủ do máy làm thì máy báo, server tin nhưng giới hạn một lần |

## 3. Lựa chọn của 🎨 (user chưa xét riêng, sửa được)

1. **Hộp xác nhận thể lực hiện khi lượt nhận làm mất bất kỳ phần nào** (thể lực hiện có + thưởng > 999), không chỉ khi đúng 999. Nhận hết hỏi **một lần** cho cả lượt. Máy tính bằng thể lực hiển thị (đã `Settle` theo giờ server); server vẫn tự cắt ở 999 dù máy tính lệch.
2. **Nhận hết theo tab** đang mở (như ảnh: Receive All gắn với tab). Tiến độ chung = số việc đã nhận / tổng việc đang mở (không tính việc khoá).
3. **"Liên kết tài khoản" đứng đầu tab Tân thủ** để người chơi thấy sớm; 8 việc còn lại giữ thứ tự spec cha. Tổng thưởng tân thủ 750 Gem.
4. **Đã liên kết = có ít nhất một danh tính** trong `PlayerInfo.Identities` hoặc có tên đăng nhập. Không có đường **gỡ** liên kết (UGS không cho gỡ tên đăng nhập; gỡ Google làm người chơi quay lại ẩn danh và dễ mất tiến độ).
5. **Hộp thư và việc "Liên kết" hiện trên mọi nền tảng có ít nhất một cách liên kết**: tên + mật khẩu chạy cả WebGL và Editor, nên **bản WebGL cũng có dòng Tài khoản** (đổi dòng "WebGL: không có dòng Tài khoản" của spec cha mục 5.9).
6. **Nút "Google" dùng Google Play Games bên dưới.** Không có plugin Google Sign-In chính thức cho Unity (ID token cần plugin bên thứ ba); plugin Google Play Games là của Google và đổi được auth code lấy danh tính UGS. Người chơi chỉ thấy chữ "Google"; máy không có Google Play Games (hoặc WebGL) thì nút Google ẩn, còn tên + mật khẩu. Hỏi lại user nếu cần Google ID token (sau 31/12).
7. **Nút trên Tiêu đề "Đăng nhập bằng tài khoản"** (dưới dòng "Chạm để bắt đầu") cho người đã có tài khoản liên kết, kèm cảnh báo mất tiến độ ẩn danh hiện tại.
8. **Nút "Đi tới" ở mỗi dòng chưa đủ điều kiện** (bảng ở mục 8.1). Đủ điều kiện thì thành "Nhận", nhận rồi thì xám "Đã nhận" (như ảnh: Challenge → Receive). **Chờ user duyệt bảng đích.**
9. **Chấm đỏ có số trên tab** (số dòng nhận được) như ảnh, và **chấm đỏ trên nút Hộp thư ở Lobby** khi có thư chưa đọc. Đây không phải chấm đỏ avatar hay Thông báo mà user đã bỏ.
10. **Thư ghim là thư do máy dựng**, trạng thái đã đọc lưu ở `PlayerSave.MailRead`; chưa cần server. Đợt 4 thư quà do server phát đi qua `IMailSource` cùng khung.
11. **Hộp thư giữ trần 500 thư** như ảnh ("Số lượng 1/500"), để đợt 4 không đổi giao diện.
12. **`MissionData.Locked`:** việc "Nâng lv" nằm trong dữ liệu với `Locked = true`; server từ chối nhận (`Locked`), máy hiện "Sắp có". Khi làm cấp nhân vật thì bỏ cờ và thêm bộ đếm, không đổi giao thức.
13. **Giao thức `Version = 3`, `MinProtocolVersion` giữ 2.** Lệnh mới tương thích với bản đợt 2 (bản cũ không gọi chúng, `JsonUtility` bỏ qua field lạ). Không cần "Cần cập nhật".
14. **Lượt miễn phí đánh dấu trong dữ liệu banner** (`FreePullDaily`), không viết cứng id banner: thêm banner sau chỉ cần đánh dấu.
15. **Việc tân thủ do máy làm tính lúc bấm Nhận**, không lưu cờ riêng (trừ "Thu AFK lần đầu" chưa suy ra được từ save): tên khác mặc định, có nhân viên xếp, có nâng cấp quán cấp > 0, đã liên kết. Làm trước cũng tính và người chơi cũ nhận được ngay.

## 4. Các phương án đã xét

| Câu hỏi | Phương án | Kết quả |
|---|---|---|
| Ai đếm việc | **A. Server đếm trong handler đợt 2 (chọn)**; B. máy đếm rồi gửi lên; C. server suy từ state có sẵn | B mở cửa tự nhận Gem, trái mục tiêu Gem trên server; C không đếm được "thắng 5 trận hôm nay". Việc tân thủ suy từ state (`ClearedStages`, `GachaHistory`, `StarUps`) nên không cần bộ đếm mới |
| Vào lần đầu | A. tự đăng nhập Google khi cài mới; **B. ẩn danh trước (chọn, user)** | A tạo người chơi gắn Google ngay (UGS không cho kiểm có sẵn mà không tạo); B nhắc bằng thư và việc tân thủ |
| Hộp thư | A. thư ghim làm ở đợt 4; **B. Hộp thư tối giản ở đợt 3 (chọn, user)** | B thêm một trang kín màn vào lịch 22/12; cắt thứ hai (mục 12) |
| Phương thức | **Google (qua Play Games) + tên đăng nhập + mật khẩu (chọn, user)**; thêm Google ID token riêng, Unity Player Accounts, Facebook | Google ID token cần plugin bên thứ ba; Facebook cần SDK ngoài và app Meta; Unity Player Accounts để sau 31/12 |
| Thể lực vượt trần | **Cùng trường `Current`, cho vượt `Max` (chọn)**; trường riêng `Bonus` | Trường riêng đổi mọi chỗ đọc thể lực. Cùng trường chỉ đổi `Settle`/`TrySpend` (mục 5.4) |

## 5. Dữ liệu

### 5.1 `ServerState.Missions` (khoá `state`, Protected)

```csharp
[Serializable] public class MissionState        // Rules, namespace BunDauPeo.Rules
{
    public long DailyDay;                       // chỉ số ngày (UTC+7) của bộ đếm ngày; 0 = chưa có
    public int DailyWins;                       // số trận thắng hợp lệ trong ngày
    public int DailyPulls;                      // số lượt quay trong ngày (quay 10 tính 10, quay miễn phí tính 1)
    public List<string> DailyClaimed = new();   // id việc ngày đã nhận hôm nay
    public long FreePullDay;                    // chỉ số ngày đã dùng lượt quay miễn phí; 0 = chưa
    public List<string> NewbieClaimed = new();  // id việc tân thủ đã nhận (mãi mãi)
}
```

- `ServerState` thêm `public MissionState Missions = new();` (sau `ClearedStages`) và `Clone()` sao chép sâu hai `List` (test `Clone_IsDeep_ForEveryListField` có sẵn bắt thiếu).
- **Chỉ số ngày:** `DayIndex(nowUtc) = floor((nowUtc.Ticks + 7 giờ) / Ticks mỗi ngày)`. Ngày mới = chỉ số khác `DailyDay`. Hằng giờ lệch 7 và các khung giờ nằm trong `Rules` (`MissionRules`).
- **Sang ngày:** handler nào đụng `Missions` gọi `MissionRules.RollDay(state, now)` trước: nếu `DailyDay != hôm nay` thì xoá `DailyWins`, `DailyPulls`, `DailyClaimed`, đặt `DailyDay`. `FreePullDay` không bị xoá (so sánh với hôm nay).
- **Đếm:** `CompleteStage` với `won == true` và ticket hợp lệ → `DailyWins++` (cả màn đã qua, thưởng lặp lại cũng tính). `GachaPull` → `DailyPulls += count`. Đếm dừng ở mục tiêu lớn nhất của việc (không để số tăng vô hạn).
- `ResetPlayer` tạo lại `ServerState` game mới nên `Missions` về mặc định (D-005).

### 5.2 Trường `PlayerSave` (luật D-005)

| Trường | Loại | Mặc định game mới | Reset |
|---|---|---|---|
| `Missions` (`MissionState`) | server giữ: máy chép từ `ServerState`, **bỏ khỏi bản tải lên** như Gem | rỗng | `ResetPlayer` rồi áp state mới |
| `AfkCollected` (`bool`) | máy giữ | `false` | ghi đè bản mặc định |
| `MailRead` (`List<string>`) | máy giữ: id thư đã đọc | rỗng | ghi đè bản mặc định |

`ServerStateApplier.Apply` thêm chép `Missions`; hàm dựng bản tải lên thêm đặt `Missions = new()`. Test hiện có "bản tải lên không còn field server giữ" mở rộng cho `Missions`. Người chơi đã có `state` đợt 2 mở bằng bản đợt 3: `Missions` thiếu trong JSON nên là mặc định, không lỗi.

### 5.3 `GameData` và SO

`GameData` thêm (Rules, kiểu như `BannerData`):

```
Missions[]  : Id, Tab (0 ngày, 1 tân thủ), Counter (số, bảng 6.3), Target, Locked,
              RewardGem, RewardStamina, WindowStartHour, WindowEndHour
Stamina     : thêm RewardCap (= 999)
Banners[]   : thêm FreePullDaily (bool)
```

- SO mới `MissionCatalog` + `MissionDefinition` trong thư mục `Assets/_Project/Missions/` (asmdef `BunDauPeo.Missions`, tham chiếu `Core`, `Rules`), thứ tự và số khởi điểm theo mục 7. `GachaBannerDefinition` thêm `FreePullDaily` (banner hiện tại đặt `true` bằng script Editor, không sửa tay `.asset`). `CombatConfig` thêm `RewardStaminaCap` (khởi điểm trong code = 999).
- `GameDataBuilder` đọc thêm ba nguồn trên; `Validate` từ chối: thưởng âm, `Target ≤ 0` ở việc không khoá, hai việc trùng id, khung giờ không nằm trong 0–24 hay đảo ngược, `RewardCap < Max`.
- Xuất lại `GameData.json` bằng menu có sẵn, `DataHash` đổi; test "từng ký tự" có sẵn bắt quên xuất.

### 5.4 Thể lực vượt trần

`StaminaRules` hiện **ép `Current` về `Max`** (nhánh `AnchorUtcTicks == 0` và nhánh hồi), nên đổi như sau:
- `Settle`: `Current ≥ Max` thì **giữ nguyên `Current`**, `AnchorUtcTicks = 0`, không hồi thêm. `Current < Max` thì hồi như cũ, tối đa tới `Max`.
- `TrySpend`: sau khi trừ, nếu `Current ≥ Max` thì **giữ `AnchorUtcTicks = 0`** (đang vượt trần thì chưa hồi); nếu `Current < Max` mà anchor đang 0 thì đặt anchor = bây giờ. (Hiện code đặt anchor = bây giờ mỗi khi anchor đang 0, sẽ làm lần `Settle` kế **kéo 240 về 100**: có test riêng.)
- Hàm mới `StaminaRules.Add(state, amount, nowTicks, max, regenTicks, cap) → granted`: `Settle`, `Current = min(cap, Current + amount)`, `granted` = phần thực tăng; `Current ≥ Max` thì anchor = 0. Phần mất = `amount − granted`.
- Máy chỉ **hiện** thể lực: dùng cùng `Settle`, hiện số thật (ví dụ "250/100", không có dòng "+1 sau"). Bản đợt 2 (nếu còn chạy) hiện bị ép 100 vì `Settle` cũ, chỉ ở màn hình; server vẫn tính đúng.
- `EnterStage` đã dùng `TrySpend` nên tự đúng sau khi sửa.

## 6. Giao thức

### 6.1 Lệnh mới

Cùng khung `CommandRunner` bước 1–6 của spec con đợt 2 (mọi lệnh mang `protocolVersion`, `sessionId`, `requestId`; trùng `requestId` trả kết quả cũ không ghi lại). `ServerProtocol.Version = 3`.

| Lệnh | Tham số riêng | Kiểm (đúng thứ tự) | Ghi | Payload |
|---|---|---|---|---|
| `ClaimMission` | `missionId`, `clientDone[]` | id có → không `Locked` → chưa nhận (ngày: hôm nay; tân thủ: chưa bao giờ) → đủ điều kiện (server đếm; hoặc id có trong `clientDone`) → việc khung giờ: giờ server trong khung | `RollDay`; cộng Gem; `StaminaRules.Add`; ghi vào `DailyClaimed`/`NewbieClaimed` | `Gem`, `Stamina` (đã nhận), `StaminaWasted` |
| `ClaimAllMissions` | `tab`, `clientDone[]` | như trên cho mọi việc của tab; không việc nào nhận được → `NothingToClaim` | cộng tổng, ghi các id | như trên (tổng) |
| `GachaFreePull` | `bannerId` | banner có và `FreePullDaily` → `FreePullDay != hôm nay` | như `GachaPull` ×1 **không trừ Gem** (cùng hàm roll, pity, lịch sử); `FreePullDay = hôm nay`; `DailyPulls++` | như `GachaPull` (`Pulls[]`) |
| `DevResetMissions` | — | môi trường `development` | `Missions = new()` | — |
| `DevAddStamina` | `amount` (1..1000) | môi trường `development` | `StaminaRules.Add` (cắt ở 999); chỉ để playtest hộp xác nhận thể lực | — |

- `CommandCode` thêm (cuối, số cố định): `NotReady = 15`, `AlreadyClaimed = 16`, `NotInWindow = 17`, `NothingToClaim = 18`, `FreePullUsed = 19`.
- **Sửa handler đợt 2:** `CompleteStage` và `GachaPull` nhận thêm `ServerContext` (giờ server) để `RollDay` và đếm (mục 5.1). Thất bại thì không đổi gì (luật bản sao của `CommandRunner`).
- **Máy báo:** `clientDone` là danh sách id việc tân thủ mà máy nói đã làm (mục 3.15). Server tin nhưng mỗi id chỉ nhận một lần; gian lận tối đa 100 + 50×4 = 300 Gem một lần mỗi người chơi, như mức spec cha chấp nhận.
- **Việc "Liên kết tài khoản" do máy báo:** server không đọc được danh tính liên kết từ module (người dựng kiểm; nếu đọc được thì chuyển thành server kiểm). Mức gian lận 100 Gem một lần.

### 6.2 Phía máy

- `IGameServer` thêm 4 hàm; `LocalGameServer` chạy `CommandRunner` + handler; `UgsGameServer` gọi module bọc 10 giây (như đợt 2). Mọi lệnh đi qua `CommandGate`/`WaitOverlay` (màn chờ).
- `MissionService` (thư mục `Missions`): dựng danh sách dòng từ `GameData.Missions` + `PlayerSave.Missions` + giờ server bằng **cùng hàm `MissionRules.View` trong `Rules`** (tiến độ, nhận được, trạng thái khung giờ, đã nhận), nên máy và server không lệch. `ClaimAsync(id)`, `ClaimAllAsync(tab)` gọi lệnh, áp `ServerState` qua `ServerStateApplier`, **cộng Material nếu có** (đợt 3 không có thưởng Material).
- `GachaService.PullFreeAsync(bannerId)`: kiểm trên máy trước (đã dùng hôm nay → không gọi), lỗi mạng → `Offline`, không đổi gì.
- `Offline`/`OtherDevice`/`NoState`: như mọi lệnh đợt 2.

### 6.3 Bộ đếm (`MissionData.Counter`)

| Số | Tên | Nguồn |
|---|---|---|
| 0 | `Wins` | `Missions.DailyWins` |
| 1 | `Pulls` | `Missions.DailyPulls` |
| 2 | `LoginWindow` | không có bộ đếm: nhận được khi giờ server trong khung và chưa nhận hôm nay |
| 3 | `BattleStagesCleared` | số màn **trận** trong `ClearedStages` (dùng `Stages[].Kind`) |
| 4 | `GachaEver` | 1 nếu `GachaHistory.Count > 0` |
| 5 | `StarUpEver` | 1 nếu có mục trong `StarUps` |
| 6 | `ClientFlag` | id có trong `clientDone` |
| 7 | `None` | việc khoá |

## 7. Danh sách nhiệm vụ (số khởi điểm, đặt trong `MissionCatalog`, đổi sau trong asset)

**Ngày** (làm mới 00:00 giờ Việt Nam theo giờ server):

| Id | Việc | Mục tiêu | Thưởng | Bộ đếm |
|---|---|---|---|---|
| `daily_win_5` | Thắng 5 trận ở chế độ bất kỳ | 5 | 20 Gem | `Wins` |
| `daily_pull_1` | Quay gacha 1 lần (gồm lượt miễn phí) | 1 | 20 Gem | `Pulls` |
| `daily_level_1` | Nâng lv một nhân vật | — | — | **khoá, "Sắp có"** |
| `daily_login_am` | Nhận thể lực 00:00–12:00 | 1 | 100 thể lực | `LoginWindow` |
| `daily_login_pm` | Nhận thể lực 12:00–24:00 | 1 | 100 thể lực | `LoginWindow` |

Mỗi ngày nhận tối đa **40 Gem + 200 thể lực**.

**Tân thủ** (một lần, theo thứ tự này, làm trước cũng tính, tổng **750 Gem**):

| # | Id | Việc | Thưởng | Bộ đếm |
|---|---|---|---|---|
| 1 | `new_link` | Liên kết tài khoản | 100 Gem | `ClientFlag` |
| 2 | `new_name` | Đặt tên trong Hồ sơ | 50 Gem | `ClientFlag` |
| 3 | `new_staff` | Xếp 1 nhân viên | 50 Gem | `ClientFlag` |
| 4 | `new_first_win` | Xếp đội và thắng màn đầu tiên | 100 Gem | `BattleStagesCleared` ≥ 1 |
| 5 | `new_first_pull` | Quay gacha lần đầu | 50 Gem | `GachaEver` |
| 6 | `new_upgrade` | Mua nâng cấp quán đầu tiên | 50 Gem | `ClientFlag` |
| 7 | `new_afk` | Thu tiền AFK lần đầu | 50 Gem | `ClientFlag` |
| 8 | `new_star_up` | Nâng sao một nhân vật | 100 Gem | `StarUpEver` |
| 9 | `new_win_10` | Thắng 10 màn đánh khác nhau (không tính màn cốt truyện) | 200 Gem | `BattleStagesCleared` ≥ 10 |

Việc ngày cũ của spec cha (Vào trận ×3, Thắng 1, Thu AFK, Xử lý sự cố, `daily_all`) **bị bỏ**; sau này user thêm việc mới vào danh sách này (đổi `MissionCatalog`, xuất, deploy).

## 8. Màn hình

Khung 1920×1080. Trang mới kín màn (L-033), hộp giữ cỡ, nền mờ kín màn vẽ trên thanh dưới. Object mới đặt tên `_PH_<w>x<h>`; hình chỉ dùng hình có sẵn của Unity (vuông, tròn, tam giác). **Ảnh mô phỏng dưới đây cần user duyệt trước khi 🔨 dựng UI**; lệch bố cục thì hỏi user.

### 8.1 Màn Nhiệm vụ (`MissionsPage`)

Mở từ nút tròn **Nhiệm vụ** ở Lobby. Quay lại → Lobby. Thanh dưới vẫn hiện. Theo `hub-missions-daily.jpg` / `hub-missions-normal.jpg`; không lấy nhân vật lớn, bong bóng thoại, dòng cảnh báo hộp quà, tab Danh hiệu.

```
┌──────────────────────────────────────────────────────────────────────────┐
│ [‹ Quay lại]  Nhiệm vụ                         [ Ngày (2) ][ Tân thủ (1) ]│
│ ┌──────────────────────────────────────────────────────────────────────┐ │
│ │ [Ngày] Thắng 5 trận ở chế độ bất kỳ                                  │ │
│ │ Tiến độ  ▓▓▓▓▓▓▓░░░ 3/5      Thưởng [◆ Gem x20]            [ Đi tới ]│ │
│ ├──────────────────────────────────────────────────────────────────────┤ │
│ │ [Ngày] Quay gacha 1 lần                                              │ │
│ │ Tiến độ  ▓▓▓▓▓▓▓▓▓▓ 1/1      Thưởng [◆ Gem x20]            [ Nhận ]  │ │  ← xanh sáng
│ ├──────────────────────────────────────────────────────────────────────┤ │
│ │ [Ngày] Nhận thể lực 00:00–12:00                                      │ │
│ │ Tiến độ  ▓▓▓▓▓▓▓▓▓▓ 1/1      Thưởng [⚡ Thể lực x100]     [Đã nhận] │ │  ← xám, tắt
│ ├──────────────────────────────────────────────────────────────────────┤ │
│ │ [Ngày] Nhận thể lực 12:00–24:00          Thưởng [⚡ x100] [Chưa tới giờ]│ │  ← xám, tắt
│ ├──────────────────────────────────────────────────────────────────────┤ │
│ │ [Ngày] Nâng lv một nhân vật   🔒                          [ Sắp có ]  │ │
│ └──────────────────────────────────────────────────────────────────────┘ │
│  Còn lại 15:44                    Hoàn thành 1/4              [ Nhận hết ]│
│ [ thanh dưới ]                                                           │
└──────────────────────────────────────────────────────────────────────────┘
```

- Tên object (cỡ thật): `MissionsPage`, `MissionTab_PH_300x80` × 2, `MissionRow_PH_1500x170`, `MissionTag_PH_150x44`, `MissionBar_PH_520x26`, `MissionReward_PH_150x80`, `MissionBtn_PH_220x90`, `MissionClaimAll_PH_380x100`, `MissionFooter_PH_1500x80`. Danh sách cuộn, nền quán mờ phía sau.
- **Nút theo trạng thái:** chưa đủ → **Đi tới** (trắng); đủ → **Nhận** (xanh); đã nhận → **Đã nhận** (xám, tắt); việc khung giờ: chưa tới giờ → "Chưa tới giờ" (xám, tắt), khung đã qua mà chưa nhận → "Đã lỡ" (xám, tắt); việc khoá → "Sắp có" (xám, tắt, kèm ổ khoá).
- Số đỏ trên tab = số dòng **Nhận** được của tab đó; không số khi bằng 0. Tab Ngày ghi **Còn lại hh:mm** tới 00:00 (giờ server); tab Tân thủ ẩn dòng đó. **Nhận hết** mờ khi không dòng nào nhận được.
- Bấm Nhận/Nhận hết đi qua màn chờ. Việc có thể lực: nếu sẽ mất phần nào thì hiện **hộp xác nhận thể lực** (mục 8.5) **trước** khi gửi lệnh.
- Thưởng thể lực hiện icon thể lực của `ResourceBarView` (hình có sẵn). Nhận xong: số Gem/thể lực trên thanh tài nguyên đổi qua `Wallet.Changed`/`Roster.Changed` như các lệnh khác; không cần hộp báo thưởng (đợt 3).
- **Bảng "Đi tới"** (đề xuất, chờ duyệt): đóng trang rồi mở:

| Việc | Đi tới |
|---|---|
| Thắng 5 trận, Thắng màn đầu, Thắng 10 màn | Chọn chế độ |
| Quay gacha, Quay gacha lần đầu | ô Gacha |
| Nâng sao một nhân vật | Nhân vật |
| Thu tiền AFK | Lobby, mở popup thu AFK nếu có Coin chờ |
| Xếp 1 nhân viên | Lobby, mở trang Nhân viên |
| Mua nâng cấp quán | Lobby, mở trang Nâng cấp |
| Đặt tên trong Hồ sơ | Lobby, mở Hồ sơ |
| Liên kết tài khoản | Lobby, mở Hồ sơ (dòng Tài khoản) |
| Thể lực theo khung giờ | không có Đi tới; chỉ Nhận (hoặc mờ kèm lý do) |

### 8.2 Hộp thư (`MailPage`)

Nút tròn **Hộp thư** thay chỗ Hộp quà ở hàng nút tròn Lobby, **hình phong thư** ghép bằng hai hình có sẵn (vuông + tam giác) cho tới khi có art thật; chấm đỏ khi có thư chưa đọc. Trang kín màn, Quay lại → Lobby. Theo `hub-mailbox-en.png`, lấy đủ các phần tử.

```
┌──────────────────────────────────────────────────────────────────────────┐
│ [‹ Quay lại]  Hộp thư                                                    │
│ ┌────────────────────────┐ ┌───────────────────────────────────────────┐ │
│ │ ✉ Liên kết tài khoản ★•│ │ Liên kết tài khoản                        │ │
│ │   Đội ngũ BunDauPeo    │ │ Đội ngũ BunDauPeo                2026-11-21│ │
│ │                        │ │                                           │ │
│ │  (một thư; đợt 4 thêm  │ │ Chào bạn! Hãy liên kết tài khoản để giữ   │ │
│ │   thư quà ở dưới)      │ │ tiến độ khi đổi máy hoặc cài lại game.    │ │
│ │                        │ │                                           │ │
│ │                        │ │ Đính kèm   (trống ở thư này)              │ │
│ │ Số lượng 1/500         │ │                                [ Liên kết ]│ │
│ │ [Nhận hết][Xoá đã đọc] │ └───────────────────────────────────────────┘ │
│ [ thanh dưới ]                                                           │
└──────────────────────────────────────────────────────────────────────────┘
```

- Tên object: `MailPage`, `MailList_PH_620x720`, `MailItem_PH_600x120`, `MailDetail_PH_1100x720`, `MailAttach_PH_1000x120`, `MailBtn_PH_300x90`, `MailClaimAll_PH_280x80`, `MailDeleteRead_PH_280x80`.
- **Thư ghim:** dấu ★ (ghim) và chấm đỏ khi chưa đọc; mở thư là đã đọc (lưu `MailRead`). **Không xoá được**: **Xoá đã đọc** bỏ qua thư ghim (mờ khi không có thư nào xoá được). Nút **Liên kết** của thư mở hộp chọn cách liên kết (mục 8.3); không có phần đính kèm, không nút Nhận. **Biến mất ngay khi liên kết xong** (và lần sau mở game đã liên kết thì không dựng). Hộp thư trống hiện "Chưa có thư".
- **Nhận hết** và nút Nhận của thư có đính kèm chưa dùng ở đợt 3 (không có thư quà) nhưng **có trong khung** để đợt 4 chỉ thêm nguồn thư: nút **Nhận hết** mờ khi không có thư quà.
- Nguồn thư qua `IMailSource` (đợt 3: `LinkAccountMailSource`; đợt 4: thư quà từ server).

### 8.3 Liên kết và đăng nhập tài khoản

**Hồ sơ**, thêm một dòng (cột trái, dưới dòng ID, cùng kiểu):

```
  ID: 4fK2…(đủ ID)
  [ Sao chép ]
  Tài khoản: Chưa liên kết      [ Liên kết tài khoản ]       ← chưa liên kết
  Tài khoản: Đã liên kết: Google, tên đăng nhập "abc"   ← đã liên kết, không nút
```

**Hộp chọn cách** (`AccountBox_PH_900x520`, cùng khung `OnlineBox`; dùng cho Liên kết và cho Đăng nhập):

```
┌────────── Liên kết tài khoản ──────────┐          ┌──────── Đăng nhập bằng tài khoản ────────┐
│  [ Google ]  (Android)                 │          │  (các nút như bên trái, đổi chữ)         │
│  [ Tên đăng nhập và mật khẩu ]         │          │  Tiến độ hiện tại (chưa liên kết) sẽ mất │
│                  [ Huỷ ]               │          └──────────────────────────────────────────┘
└────────────────────────────────────────┘
```

**Hộp tên + mật khẩu** (`CredentialBox_PH_900x620`; hai ô nhập, nút **Tạo tài khoản** khi liên kết, **Đăng nhập** khi đăng nhập; dòng lỗi dưới ô): tên 3–20 ký tự (`A–Z a–z 0–9 . - @ _`, không phân biệt hoa thường), mật khẩu theo luật UGS (người dựng đọc `authentication.md`, mục Username/Password Constraints). Lỗi: tên đã dùng, mật khẩu yếu, sai tên/mật khẩu, mất mạng (hộp Mất kết nối như thường).

**Hộp chuyển tài khoản** (khi liên kết mà danh tính đó đã thuộc người chơi khác):

```
┌──── Tài khoản này đã có tiến độ khác ────┐
│ Chuyển sang tiến độ đó? Tiến độ hiện tại │
│ (chưa liên kết) sẽ mất.                  │
│          [ Chuyển ]   [ Huỷ ]            │
└──────────────────────────────────────────┘
```

Chuyển = đăng xuất rồi đăng nhập bằng danh tính đó, về màn Tiêu đề, chạm vào thì `Bootstrap` như bình thường. **Không dùng `ForceLink`** (không âm thầm cướp liên kết của người khác).

**Màn Tiêu đề:** thêm nút nhỏ **Đăng nhập bằng tài khoản** dưới dòng "Chạm để bắt đầu" (`TitleAccountBtn_PH_480x90`), mở hộp chọn cách (kiểu Đăng nhập). Đăng nhập xong vào luồng `Bootstrap` như chạm. Đang có phiên ẩn danh với tiến độ: hộp đăng nhập ghi cảnh báo mất tiến độ (chưa liên kết) và cần xác nhận. Nút không hiện ở bước "Đang kết nối…".

### 8.4 Gacha: lượt quay miễn phí

Trên trang roll: khi còn lượt hôm nay, nút **Quay ×1** đổi thành **Quay miễn phí** (nhãn "Miễn phí" thay chỗ giá Gem), hết lượt thì trở lại nút ×1 có giá. Quay ×10 không đổi. Bấm Quay miễn phí đi qua màn chờ, rồi cutscene và trang tổng như quay thường; lịch sử ghi như một lượt ×1. Không cần Gem. Nhãn nhỏ "Còn 1 lượt miễn phí hôm nay" khi còn.

### 8.5 Hộp xác nhận thể lực

Hộp xác nhận giữ cỡ (cỡ và tên theo hộp xác nhận hiện có, người dựng tra), nền mờ che cả thanh:

```
┌──────────── Thể lực sắp vượt giới hạn ────────────┐
│ Bạn đang có 950 thể lực. Nhận thêm chỉ được 49    │
│ (tối đa 999), mất 51. Vẫn nhận?                   │
│                [ Nhận ]    [ Huỷ ]                │
└───────────────────────────────────────────────────┘
```

Nhận hết thì tổng cả lượt (ví dụ "Nhận 200, chỉ được 49"). Huỷ không gửi lệnh gì.

### 8.6 Lobby

Hàng nút tròn: **Nhiệm vụ** (`HubRoundBtnMissions_PH_110x110`), **Hộp thư** (`HubRoundBtnMail_PH_110x110`, chấm đỏ khi thư chưa đọc), Shop (có sẵn, khoá). Hội chưa dựng. Nút tròn Hộp quà của spec cha **được thay bằng Hộp thư**.

### 8.7 Chữ mới (`UiText.Online.cs` và `UiText.Hub.cs`, D-012)

Mỗi khoá có bản Anh; `HardcodedTextTests`, `UiTextTests` xanh; kiểm bề rộng hộp bản tiếng Anh dài nhất (L-039). Khu `missions.`, `mail.`, `account.`, `stamina.`, `gacha.`:

| Khoá | Tiếng Việt | English |
|---|---|---|
| `missions.title` / `missions.tab_daily` / `missions.tab_newbie` | Nhiệm vụ / Ngày / Tân thủ | Missions / Daily / Beginner |
| `missions.progress` / `missions.reward` | Tiến độ / Thưởng | Progress / Reward |
| `missions.goto` / `missions.claim` / `missions.claimed` / `missions.claim_all` | Đi tới / Nhận / Đã nhận / Nhận hết | Go / Claim / Claimed / Claim all |
| `missions.remaining` | Còn lại {0} | Time left {0} |
| `missions.completed` | Hoàn thành {0}/{1} | Completed {0}/{1} |
| `missions.not_yet` / `missions.missed` / `missions.locked` | Chưa tới giờ / Đã lỡ / Sắp có | Not yet / Missed / Coming soon |
| `missions.daily_win_5` | Thắng 5 trận ở chế độ bất kỳ | Win 5 battles in any mode |
| `missions.daily_pull_1` | Quay gacha 1 lần | Pull gacha once |
| `missions.daily_level_1` | Nâng lv một nhân vật | Level up a character |
| `missions.daily_login_am` / `missions.daily_login_pm` | Nhận thể lực 00:00–12:00 (giờ VN) / 12:00–24:00 (giờ VN) | Claim stamina 00:00–12:00 (Vietnam time) / 12:00–24:00 (Vietnam time) |
| `missions.new_link` … `missions.new_win_10` | tên 9 việc theo mục 7 | tên Anh tương ứng |
| `mail.title` / `mail.count` | Hộp thư / Số lượng {0}/{1} | Mail / Quantity {0}/{1} |
| `mail.claim_all` / `mail.delete_read` / `mail.empty` / `mail.attachments` | Nhận hết / Xoá đã đọc / Chưa có thư / Đính kèm | Claim all / Delete read / No mail / Attachments |
| `mail.sender` | Đội ngũ BunDauPeo | BunDauPeo Team |
| `mail.link_title` | Liên kết tài khoản | Link your account |
| `mail.link_body` | Chào bạn! Hãy liên kết tài khoản để giữ tiến độ khi đổi máy hoặc cài lại game. | Hello! Link your account to keep your progress when you switch devices or reinstall. |
| `mail.link_btn` | Liên kết | Link |
| `account.row` / `account.not_linked` / `account.linked` | Tài khoản / Chưa liên kết / Đã liên kết: {0} | Account / Not linked / Linked: {0} |
| `account.link_btn` / `account.signin_btn` | Liên kết tài khoản / Đăng nhập bằng tài khoản | Link account / Sign in with an account |
| `account.choose_link` / `account.choose_signin` | Liên kết tài khoản / Đăng nhập bằng tài khoản | Link account / Sign in with an account |
| `account.google` / `account.userpass` | Google / Tên đăng nhập và mật khẩu | Google / Username and password |
| `account.username` / `account.password` | Tên đăng nhập / Mật khẩu | Username / Password |
| `account.create` / `account.signin` | Tạo tài khoản / Đăng nhập | Create account / Sign in |
| `account.err_taken` / `account.err_weak` / `account.err_wrong` | Tên đã được dùng / Mật khẩu chưa đủ mạnh / Sai tên đăng nhập hoặc mật khẩu | Username already taken / Password is too weak / Wrong username or password |
| `account.warn_lose` | Tiến độ hiện tại (chưa liên kết) sẽ mất. | Your current progress (not linked) will be lost. |
| `account.conflict_title` / `account.conflict_body` | Tài khoản này đã có tiến độ khác / Chuyển sang tiến độ đó? Tiến độ hiện tại (chưa liên kết) sẽ mất. | This account already has other progress / Switch to that progress? Your current progress (not linked) will be lost. |
| `account.switch` | Chuyển | Switch |
| `stamina.cap_title` / `stamina.cap_body` | Thể lực sắp vượt giới hạn / Bạn đang có {0} thể lực. Nhận thêm chỉ được {1} (tối đa {2}), mất {3}. Vẫn nhận? | Stamina will exceed the cap / You have {0} stamina. You will only get {1} more (max {2}) and lose {3}. Claim anyway? |
| `gacha.free_pull` / `gacha.free_left` | Quay miễn phí / Còn 1 lượt miễn phí hôm nay | Free pull / 1 free pull left today |

Hộp xác nhận dùng lại nút Huỷ/Nhận có sẵn nếu đã có khoá chung (người dựng tra `UiText.Hub.cs`, không tạo trùng).

## 9. Package, công cụ và việc user tự làm

**Package (hỏi user trước khi thêm; không thêm package nào mà chưa hỏi):**

| Package | Cho | Ghi chú |
|---|---|---|
| Google Play Games plugin for Unity (Google, ngoài Package Manager, kèm External Dependency Manager) | Google Play Games | Có thể làm vỡ build Android đang ổn (D-011): thêm ở đầu phiên, build APK ngay; không dùng được thì báo, không đoán |
| Tên đăng nhập + mật khẩu | — | Có sẵn trong package Authentication đã có, không thêm gì |

**Việc chỉ user làm được:**
1. **Merge PR #22** (đợt 2) sau playtest, rồi cắt `feature/online-3` từ `main`.
2. Google Play Games: tạo app trên Play Console, cấu hình Play Games Services, tạo credentials OAuth (loại Android với SHA-1 keystore, và loại Web/"game server" để UGS đổi auth code), thêm tài khoản thử; nhập Client ID/Secret vào Unity Dashboard (Authentication → Google Play Games). Kế hoạch ghi từng bước.
3. Bật phương thức tên đăng nhập + mật khẩu trên Dashboard (kiểm trạng thái hiện tại).
4. **Deploy module lên `development`** (Task 4) cần user cho phép, và người thử đợt 2 nhận bản mới. Deploy `production` không thuộc kế hoạch.
5. Xuất `GameData.json` đổi, `DataHash` đổi: người thử dùng bản đợt 2 chỉ thấy cảnh báo hash lệch trong log, không chặn chơi.

**Khoá và bí mật:** Client Secret chỉ nằm trên Unity Dashboard; không khoá nào trong repo.

## 10. Test (không cần mạng)

- **Rules (EditMode):** `StaminaRules` vượt trần (spend từ 250 vẫn ≥ 100 sau `Settle`; `Add` cắt ở 999, `granted/wasted`; dưới `Max` hồi như cũ; đồng hồ lùi); `MissionRules` (chỉ số ngày quanh 00:00 UTC+7, `RollDay`, khung giờ biên 11:59:59/12:00:00/23:59:59, `View`); handler `ClaimMission`/`ClaimAllMissions` (chưa đủ, đã nhận, khoá, trùng `requestId` không cộng hai lần, `clientDone` giả không có thì không nhận, nhận một việc hai lần trong ngày, sang ngày nhận lại, tân thủ nhận một lần, Nhận hết theo tab); đếm trong `CompleteStage` (thắng tính, thua không, màn lặp tính) và `GachaPull` (quay 10 tính 10); `GachaFreePull` (lần hai trong ngày bị từ chối, qua ngày dùng lại, không trừ Gem, vào pity và lịch sử, banner không có cờ bị từ chối); từ chối thì không đổi state (L-003).
- **Dữ liệu:** `GameData.json` so từng ký tự; `Validate` các ca lỗi; `DataHash` hai phía.
- **Máy:** `ServerStateApplier` chép `Missions`; bản tải lên không còn `Missions`; save đợt 2 nạp được; `MissionService` (dựng dòng, `clientDone`, nhận lỗi mạng không đổi gì); `IAccountService` giả (liên kết, trùng danh tính → xung đột, đăng nhập, tên đã dùng); `LinkAccountMailSource` (hiện khi chưa liên kết, mất khi liên kết, `MailRead`); nút/ trạng thái dòng; hộp xác nhận thể lực (khi nào hiện); UiText có đủ khoá.
- **`dotnet test`:** module có lệnh mới, `Missions` qua JSON hai phía (Newtonsoft ↔ `JsonUtility`).
- **PlayMode:** mở Nhiệm vụ → nhận một việc → Gem đổi; mở Hộp thư → thư ghim → Liên kết (giả) → thư mất; quay miễn phí; nhận thể lực chạm trần hiện hộp; Tiêu đề có nút đăng nhập.
- **UGS thật:** không có test tự động; danh sách playtest ở kế hoạch. Số test dự kiến ở kế hoạch.

## 11. Docs phải sửa

| File | Sửa | Ai |
|---|---|---|
| `docs/design/screen-flow.md` | Nhiệm vụ (tab, nút Đi tới, Nhận hết), Hộp thư thay Hộp quà (đợt 3), Tiêu đề thêm nút Đăng nhập bằng tài khoản, Hồ sơ dòng Tài khoản, Gacha quay miễn phí, hộp thể lực | 🎨 (làm cùng spec) |
| `docs/design/game-design.md` | Mục Nhiệm vụ (danh sách mới), Thể lực (vượt trần 999, thưởng khung giờ), Gacha (quay miễn phí), Tiền tệ (nguồn Gem) | 🎨 (làm cùng spec) |
| `docs/design/ideas.md` | Đã ghi: Thành tựu, cấp nhân vật, vẽ lại quán | 🎨 |
| `docs/references/ui-screens.md` | Đã thêm ảnh | 🎨 |
| `docs/roadmap.md` | Dòng Online đợt 3 (đang bàn → kế hoạch) | 🎨 / 🧭 |
| `docs/decisions/decisions.md` | D-008: thể lực có thể vượt trần khi nhận thưởng (tối đa 999), ngày thể lực nhận thêm tối đa 200; ghi hệ quả cho cân bằng | 🔨 Task 12, **sau khi user xem lại** |
| `docs/superpowers/specs/2026-11-13-online-design.md` | 5.9, 5.10: ghi chú trỏ về spec này (WebGL có Tài khoản; danh sách việc ngày mới; Hộp thư ở đợt 3; không dự phòng "chỉ khi trễ") | 🔨 Task 12 |
| `docs/guides/unity-workflow.md` | Mục Online: lệnh mới, xuất `MissionCatalog`, deploy, liên kết tài khoản trong Editor Local | 🔨 Task 12 |
| `docs/project/tech-stack.md`, `structure.md` | Thư mục `Missions/`, plugin Google, module có lệnh mới | 🔨 Task 12 |

## 12. Thứ tự cắt khi trễ (đề xuất, chờ user duyệt)

Thứ tự của user: Hộp quà → Nhiệm vụ ngày → dời công khai. Trong đợt 3 mình đề xuất cắt **trước khi** chạm các mục đó:
1. **Hộp thư** (thư ghim) — vẫn còn việc tân thủ "Liên kết tài khoản" và Hồ sơ để nhắc.
2. **Lượt quay miễn phí.**
3. **Tên đăng nhập + mật khẩu.**
4. **Nhiệm vụ ngày** (giữ tân thủ; thứ tự của user).
5. Dời ngày công khai.

**Không cắt:** liên kết Google (Play Games), Xoá tài khoản (đợt 1), Gem trên server, thể lực theo khung giờ khi đã có Nhiệm vụ.

## 13. Rủi ro

1. **Plugin Google Play Games** kéo thư viện Android, có thể làm vỡ build (D-011). Giảm: thêm ở đầu phiên O3-c, build APK ngay.
2. **Không có cách gỡ** tên đăng nhập; quên mật khẩu thì không lấy lại (UGS chưa có đường khôi phục trong gói này). Hộp ghi rõ "hãy nhớ mật khẩu". Mất tài khoản tên + mật khẩu = mất tiến độ nếu không có cách liên kết khác: khuyến nghị người chơi Android liên kết cả Google.
3. **Hai danh tính cùng một người chơi:** UGS cho nhiều danh tính; nếu một danh tính đã thuộc người khác thì hộp chuyển tài khoản. Chuyển tài khoản bỏ tiến độ ẩn danh hiện tại: phải xác nhận rõ.
4. **Sửa `StaminaRules`** chạm thể lực mọi nơi (vào trận, hiển thị, DEV Hồi thể lực): test đủ ca biên; bản đợt 2 còn chạy chỉ hiện sai màn hình.
5. **Cân bằng thể lực:** thêm tối đa 200 thể lực/ngày, tức ~20 lượt vào trận hơn hồi tự nhiên (~288/ngày), tăng ~70%; Gem thưởng màn tăng theo. D-008 cần xem lại (mục 11). Máy báo chỉ ảnh hưởng 300 Gem một lần.
6. **Việc tân thủ do máy báo** (`clientDone`): tối đa 300 Gem một lần mỗi người chơi, chấp nhận (mức 2).
7. **Sang ngày lúc đang chơi:** bộ đếm ngày reset ở lệnh kế tiếp; trang Nhiệm vụ đang mở hiển thị theo giờ server nên đổi ngay khi `ServerTime` qua 00:00 (test).
8. **Người thử đợt 2** thiếu `Missions` trong `state`: mặc định rỗng, không mất dữ liệu.
9. **Deploy module** đổi hash dữ liệu và thêm lệnh: bản đợt 2 vẫn chơi được (mục 3.13).

## 14. Khác với spec cha (đã chốt với user 2026-11-21)

1. 5.9: bỏ "dự phòng tên + mật khẩu khi Play Console chưa có"; **Google (qua Play Games) và tên + mật khẩu** đều là cách liên kết chính thức, WebGL có dòng Tài khoản. Không làm Google ID token riêng.
2. 5.10: danh sách việc ngày đổi (mục 7); thêm việc tân thủ "Liên kết tài khoản"; có nút Đi tới; không có `daily_all`; thể lực khung giờ.
3. 5.11/5.12: **Hộp thư ở đợt 3** (spec cha: Hộp quà ở đợt 4); đợt 4 thêm quà vào đó. Bỏ dòng Thông báo `MissionReward` ở đợt 3 (đợt 4 hỏi lại).
4. Thể lực vượt trần (spec cha và D-008: đầy thì không tích thêm).
5. Lượt quay miễn phí hằng ngày (spec cha chưa có).

## 15. Khác với luồng màn (🎨 đã sửa)

`screen-flow.md`, `game-design.md`, `ideas.md`, `ui-screens.md` đã sửa theo mục 11 cùng phiên viết spec này (🎨 giữ các file đó).
