# Spec con: Online đợt 2 — logic server

> **Trạng thái:** user duyệt 2026-11-16. Spec cha: [online-design](2026-11-13-online-design.md) (mục 5.2, 5.3, 5.7). Spec này chỉ ghi phần đợt 2 mà spec cha chưa chốt; chỗ nào không nói thì theo spec cha.

## 1. Mục tiêu

Server giữ và tính **Gem, nhân vật sở hữu, mảnh, nâng sao, gacha, thể lực, vào trận và thưởng màn**. Máy vẫn tính **quán, Coin, Material** (spec cha 5.3). Xong khoảng 12/12, có điểm dừng playtest trên `development`.

**Ngoài phạm vi đợt 2:** Google Play Games, Nhiệm vụ (`Missions`), Hộp quà (`ClaimedGiftIds`): đợt 3–4. Không kiểm trận trên server (spec cha mục 3).

## 2. Quyết định đã chốt với user (2026-11-16)

| # | Câu hỏi | Chốt |
|---|---|---|
| 1 | Màn chờ khi gọi server | **Chặn cả màn**: lớp trong suốt chặn mọi chạm, kể cả thanh dưới và nút Thông báo. Sau **0,5 giây** hiện ô vuông xoay nhỏ và chữ "Đang gửi…". Quá **10 giây** → hộp Mất kết nối; **Thử lại** gửi lại đúng lượt đó, không trừ Gem hai lần. Thay cho "nút mờ + vòng chờ nhỏ" ở spec cha 5.7 |
| 2 | Save đợt 1 có Gem, nhân vật trong khoá `save` | **Không chuyển.** Người chơi chưa có `state` thì server tạo phần server giữ như game mới |
| 3 | Dữ liệu cân bằng cho server | **Xuất tay + deploy** (spec cha 5.3), thêm **mã băm dữ liệu**: lệch thì máy ghi cảnh báo |
| 4 | Kiểm trận | **Như spec cha**: máy báo thắng/thua, server chỉ bảo đảm mỗi lần trả thể lực nhận thưởng tối đa một lần |

## 3. Lựa chọn của 📐 (user chưa xét riêng, sửa được)

1. **Chống gửi hai lần bằng `RequestId`.** Mỗi lượt bấm tạo một mã mới (`Guid`); Thử lại gửi lại **cùng mã**. Server nhớ mã và kết quả của lệnh cuối (`LastRequestId`, `LastResultJson`); gặp lại mã đó thì trả kết quả cũ, không chạy lại. Đủ vì máy chỉ gửi một lệnh mỗi lúc (màn chờ chặn chạm), nên lượt Thử lại luôn là của lệnh cuối.
2. **Phát hiện máy khác bằng `ActiveSessionId`.** `Bootstrap` cấp mã phiên mới, ghi vào `state` và trả về máy; mọi lệnh sau mang mã đó, lệch thì trả `OtherDevice`. Khi đẩy `save` bị xung đột write lock, máy đọc `state.ActiveSessionId` (Protected: máy đọc được):
   - khác mã của mình → `OtherDevice`;
   - trùng → đó là lượt đẩy mất trả lời của chính máy này: tải lại write lock rồi đẩy lại;
   - chưa có `state` (Reset hay Xoá tài khoản mất trả lời) → về màn Tiêu đề như `NoState` (mục 6.1).

   `IGameServer` thêm `LoadActiveSessionIdAsync()` đọc khoá `state` lớp Protected cho việc này.

   Cách này **thay** việc so `SaveRevision` (spec cha 5.5, ghi chú "Sửa sau review") và sửa luôn lỗi hai máy trùng `SaveRevision`. `SaveRevision` giữ lại, chỉ để ghi log.
3. **Hàng rào bản cũ (`ClientTooOld`).** Mọi lệnh gửi `ProtocolVersion` (`ServerProtocol.Version = 2`). Server có `MinProtocolVersion = 2`; lệnh không có số (bản đợt 1) coi là 0. Thấp hơn mức tối thiểu thì server **ném lỗi** chứ không trả response, để bản đợt 1 (chỉ đọc `ServerUtcTicks`) không đi tiếp được: nó kẹt ở hộp "Không kết nối được" và không bao giờ đẩy `save` có Gem lên (bản đợt 1 luôn gọi `Bootstrap` trước khi tải `save`). Thông báo lỗi chứa chữ `ClientTooOld`; Cloud Code bọc thông báo nên `UgsErrors.Map` dò bằng `Contains`, không `StartsWith`, rồi đổi → `OnlineStatus.ClientTooOld` → hộp **Cần cập nhật** (mục 7). Đợt sau tăng `MinProtocolVersion` khi giao thức đổi không tương thích.
4. **Kiểu save sang `Rules`.** `CurrencyType`, `CurrencyAmount`, `FragmentCount`, `PityEntry`, `GachaHistoryEntry`, `StarUpEntry`, `StaminaState` chuyển file từ `Core/Scripts/` sang `Rules/`, **giữ `namespace BunDauPeo.Core`** (không phải sửa `using` ở chỗ khác; SO lưu struct theo tên field nên asset không đổi). Các file này đã là C# thuần. Di chuyển bằng `AssetDatabase.MoveAsset` qua `unity` CLI (giữ GUID), không sửa tay `.meta`. Cùng task đó thêm `"BunDauPeo.Rules"` vào `references` của mọi asmdef đang dùng các kiểu này mà chưa có: Characters, Combat, Gacha, Restaurant, UI, Editor, Playtest (hiện chỉ Boot, Core, Online và hai asmdef Tests có), không thì project không biên dịch.
5. **Hồi thể lực dùng chung.** Phép tính `Settle` của `StaminaService` chuyển thành hàm thuần `StaminaRules.Settle(state, nowTicks, max, regenTicks)` trong `Rules`. Server dùng nó với giờ server; máy dùng nó với `ServerTime` (Core) **chỉ để hiện** số thể lực.
6. **`GameData` dựng bằng builder chạy lúc runtime** (`GameDataBuilder` ở Boot, đọc các SO). `LocalGameServer` nhận `GameData` từ builder; menu xuất ghi kết quả của cùng builder ra file (mục 5).
7. **Material trong thưởng:** server trả số lượng, máy cộng vào `Wallet` rồi lưu `save` (spec cha 5.3). Máy chỉ cộng ở **lần trả lời thành công đầu tiên** của một lượt bấm; Thử lại sau lỗi chưa từng thành công nên không cộng hai lần.
8. **Quá 10 giây là `NetworkError`.** Câu trả lời đến muộn sau mốc 10 giây bị bỏ; Thử lại gửi cùng `RequestId` nên lấy được đúng kết quả server đã ghi.
9. **Chặn `Dev*` ngoài `development`** bằng tên môi trường trong `IExecutionContext` (người dựng kiểm tên thuộc tính trong package `Com.Unity.Services.CloudCode.Core` 0.0.7). Không đọc được tên môi trường thì từ chối.
10. **Chế độ Local trong Editor không thêm file mới.** `LocalGameServer` lấy phần server giữ từ bản đệm `save.json` (bản đệm vẫn ghi đủ mọi field, như đợt 1), nên công cụ D-009 (Backup/Restore) và Reset giữ nguyên. Trường chỉ server cần (`Revision`, `ActiveSessionId`, `LastRequestId`…) ở Local chỉ nằm trong bộ nhớ.

## 4. Dữ liệu

### 4.1 `ServerState` (khoá `state`, Protected)

```csharp
[Serializable] public class ServerState   // Rules, namespace BunDauPeo.Rules
{
    public int Version = 1;
    public long Revision;                  // tăng mỗi lần server ghi
    public long Gem;
    public List<string> OwnedCharacterIds = new();
    public List<FragmentCount> Fragments = new();
    public List<StarUpEntry> StarUps = new();
    public List<PityEntry> GachaPity = new();
    public List<GachaHistoryEntry> GachaHistory = new();   // tối đa 50, như nay
    public StaminaState Stamina = new();
    public List<string> ClearedStages = new();
    public string PendingTicket = "";      // PendingBattle của spec cha: hai field phẳng cho JsonUtility
    public string PendingStageId = "";
    public string ActiveSessionId = "";
    public string LastRequestId = "";
    public string LastResultJson = "";     // phần payload của response lệnh cuối, không gồm State
}
```

Chỉ field công khai, `List`, kiểu cơ bản; enum ghi dạng số (spec cha rủi ro 4). Thứ tự field cố định để test so JSON hai phía.

### 4.2 Luật D-005 cho trường mới

| Trường | Nơi | Mặc định game mới | Reset |
|---|---|---|---|
| `Version`, `Revision` | `ServerState` | `1`, `0` | `ResetPlayer` xoá khoá `state` rồi tạo lại bản game mới |
| `PendingTicket`, `PendingStageId` | `ServerState` | `""` | như trên |
| `ActiveSessionId` | `ServerState` | `""`, `Bootstrap` đặt mã mới | như trên; máy đang chạy giữ mã phiên của mình (Reset không đăng nhập lại) nên `ResetPlayer` ghi lại mã phiên hiện tại vào bản mới |
| `LastRequestId`, `LastResultJson` | `ServerState` | `""` | như trên |

`PlayerSave` **không thêm field**. Save cũ vẫn nạp được: khoá `save` đợt 1 còn Gem và nhân vật, máy bỏ qua các field đó (luôn lấy từ `state`). Sau khi áp `state`, máy dọn `BattleTeam`, `Staff`, `AvatarCharacterId` khỏi nhân vật không còn sở hữu (có test với save đợt 1 thật, có Gem và nhân vật gacha).

### 4.3 Khoá `save` sau đợt 2

- **Tải lên:** bản sao `PlayerSave` bỏ các field server giữ: mục `Gem` trong `Balances`, `OwnedCharacterIds`, `Fragments`, `StarUps`, `GachaPity`, `GachaHistory`, `Stamina`, `ClearedStages`.
- **Nạp:** field server giữ lấy từ `ServerState` của `Bootstrap`; phần còn lại từ `save`. Chưa có `save` → cấp Coin/Material khởi đầu theo `NewGameConfig` như nay (Gem khởi đầu do server cấp).
- **Bản đệm** `save.json`: vẫn ghi đủ mọi field (như đợt 1), không bao giờ đẩy đè lên server.

## 5. Dữ liệu cân bằng (`GameData`)

```
GameData (Rules)
  DataVersion
  Characters[]  : Id, Rarity (số), BaseStars
  Banners[]     : Id, SingleCostGem, TenCostGem, WeightR/SR/SSR, SsrPityThreshold,
                  GuaranteeSrInTen, DupFragmentsR/SR/SSR, CharacterIds[]
  StarCosts[]   : số mảnh để lên mỗi sao (StarConfig)
  Stamina       : Max, RegenSeconds (CombatConfig)
  MaxTeamSize   : hằng Roster.MaxBattleTeamSize (= 5, không phải SO)
  Stages[]      : Id, Kind (số), StaminaCost, FirstClear{Gem, Material}, Repeat{Gem, Material}  (đúng thứ tự trong StageCatalog;
                  thưởng màn không được có Coin, màn cốt truyện không có thưởng: StageDefinition.Validate đã chặn)
  NewGame       : StartingGem, StarterCharacterIds[]
```

- **Builder** `GameDataBuilder.Build(...)` (Boot) đọc SO; `Validate` từ chối dữ liệu server không xử lý được: giá banner có loại tiền khác Gem, thưởng màn có loại tiền lạ.
- **Xuất:** menu `Tools/BunDauPeo/Export Server Data` ghi `ServerModule/BunDauPeoServer/GameData.json` bằng `JsonUtility.ToJson` (một dòng, không xuống dòng cuối: tránh lệch CRLF giữa máy và Git), nhúng làm `EmbeddedResource`.
- **Test EditMode:** dựng lại từ SO và so **từng ký tự** với file đã commit. Đổi SO mà quên xuất → đỏ.
- **Mã băm:** `DataHash = FNV-1a 64 bit` của chuỗi JSON, hàm nằm trong `Rules` (dùng chung, không cần thư viện mã hoá). `Bootstrap` trả `DataHash` của file server đang chạy; máy băm `JsonUtility.ToJson(builder)` của mình, lệch thì `Debug.LogWarning` (không chặn chơi: bản build cũ hơn server một nhịp vẫn chơi được, server mới quyết định).
- Đổi dữ liệu cân bằng = sửa SO → xuất → `dotnet test` → deploy module (user cho phép).

## 6. Giao thức

### 6.1 Chung

Mọi lệnh (trừ `Bootstrap` không có `SessionId`, `RequestId`) gửi:

| Tham số | Kiểu | Ý nghĩa |
|---|---|---|
| `protocolVersion` | `int` | `ServerProtocol.Version` |
| `sessionId` | `string` | mã phiên `Bootstrap` cấp |
| `requestId` | `string` | mã lượt bấm |

Module làm theo thứ tự:
1. `protocolVersion < MinProtocolVersion` → ném `ClientTooOld`.
2. Đọc `state` kèm write lock. Chưa có → `Code = NoState` (xảy ra sau Xoá tài khoản mà bước xoá tài khoản lỗi: máy về màn Tiêu đề, chạm lại thì `Bootstrap` tạo `state` mới).
3. `sessionId` khác `ActiveSessionId` → `Code = OtherDevice`, không ghi.
4. `requestId == LastRequestId` → trả `LastResultJson` + `state` hiện tại, không ghi.
5. Chạy `ServerHandlers.X(state, request, data, ctx)` (`ctx`: `NowUtc`, `Random`, `NewId`). Handler làm trên **bản sao**; từ chối thì trả mã lỗi và **không ghi**.
6. Thành công: `Revision++`, lưu `LastRequestId`/`LastResultJson`, ghi `state` một lần bằng write lock. Xung đột lock (hai lượt cùng lúc) → đọc lại từ bước 2, tối đa 3 lần, rồi `ServerError`.

`ResetPlayer` chạy bước 1 và 3 (khi có `state`), bỏ bước 2 (chưa có `state` vẫn xoá được) và bước 4 (Reset lại lần nữa vô hại).

Response chung (`CommandResponse`, Rules):

```csharp
public int Code;              // CommandCode, 0 = Ok
public long ServerUtcTicks;   // máy gọi ServerTime.Sync như đợt 1
public ServerState State;     // có khi Code = Ok (trừ ResetPlayer recreate=false); null khi bị từ chối
public string ResultJson;     // payload riêng của lệnh (bảng 6.2), "" khi bị từ chối
```

`CommandCode` (Rules, số cố định): `Ok, Invalid, NotEnoughGem, Locked, WrongKind, EmptyTeam, NotOwned, NotEnoughStamina, NotEnoughFragments, MaxStars, AlreadyCleared, TicketMismatch, OtherDevice, NoState, DevOnly`. Máy đổi sang enum trạng thái đang có của từng service (`GachaPullStatus`, `EnterStageStatus`, `StarUpStatus`…), nên UI báo lỗi như cũ.

Lỗi mạng, hết 10 giây, lỗi server ném → `OnlineStatus` như đợt 1 (`NetworkError`, `ServerError`, thêm `ClientTooOld`). Máy xử lý lệnh do người chơi bấm như sau:

| Kết quả | Máy làm |
|---|---|
| `NetworkError`, hết 10 giây, `ServerError` (kể cả 3 lần xung đột lock) | Hộp Mất kết nối; Thử lại gửi lại **cùng `RequestId`** |
| `Code = OtherDevice` | Hộp Tài khoản đang dùng ở máy khác (như đợt 1) |
| `Code = NoState` | Về màn Tiêu đề, không hộp |
| `ClientTooOld` | Hộp Cần cập nhật; `CloudSync` ngừng đẩy `save` |
| Mã từ chối khác | Không hộp mạng; service trả trạng thái lỗi như hiện nay |

### 6.2 Từng lệnh

| Lệnh | Tham số riêng | Kiểm (đúng thứ tự) | Ghi | Payload `ResultJson` |
|---|---|---|---|---|
| `Bootstrap` | `protocolVersion` | — | chưa có `state` → tạo game mới (`StartingGem`, `StarterCharacterIds`, thể lực đầy); luôn đặt `ActiveSessionId` mới, `Revision++` | không dùng `CommandResponse`: `BootstrapResponse` mở rộng thành `ServerUtcTicks` (giữ như đợt 1), `SessionId`, `DataHash`, `State` |
| `GachaPull` | `bannerId`, `count` | banner có, `count` 1 hoặc 10, đủ Gem | roll như `GachaRoller` hiện tại (cùng số lần gọi `NextDouble`, trọng số giữ kiểu `float` để tổng tính như cũ; `GachaRoller` trên máy được thay bằng bản trong `Rules` dùng `GameData`, bản cũ xoá), trừ Gem, trao nhân vật/mảnh, pity, lịch sử | `Pulls[]`: `CharacterId`, `IsNew`, `Fragments` |
| `EnterStage` | `stageId`, `team[]` | màn có và là màn trận, đã mở, đội 1..`MaxTeamSize` không trùng và **đều đã sở hữu**, đủ thể lực theo giờ server | `Settle` rồi trừ thể lực, `PendingTicket = NewId()`, `PendingStageId` (thay ticket cũ) | `Ticket`, `StaminaCost` |
| `CompleteStage` | `ticket`, `won` | `ticket` khác rỗng và khớp `PendingTicket` | luôn xoá ticket; `won`: lần đầu qua → `FirstClear`, đã qua → `Repeat`; cộng Gem, thêm `ClearedStages` | `FirstClear`, `Gem`, `Material` |
| `CompleteStory` | `stageId` | màn có, là màn cốt truyện, đã mở, chưa qua (đã qua → `AlreadyCleared`, như `StageProgressService` hiện nay) | thêm `ClearedStages`; không thưởng (màn cốt truyện không có thưởng) | — |
| `StarUp` | `characterId` | id có, đã sở hữu, chưa tối đa, đủ mảnh | trừ mảnh, tăng `StarUps` | `NewStars` |
| `ResetPlayer` | `recreate` (`bool`) | — | xoá mọi khoá Default **và Protected**; `recreate = true` → tạo lại `state` game mới, giữ `ActiveSessionId` hiện tại | — (`State` của response chung) |
| `DevAddGem` | `amount` (1..100000) | môi trường `development` | cộng Gem | — |
| `DevRefillStamina` | — | môi trường `development` | thể lực đầy | — |

- `Settle` thể lực chạy ở **mọi** lệnh đọc/ghi thể lực bằng giờ server; máy không gửi giờ.
- Xoá tài khoản gọi `ResetPlayer(recreate: false)` rồi xoá tài khoản (không để lại `state` mồ côi).
- "Chiến đấu thử" của DEV vẫn chạy trên máy, không gọi server. +Material, Tua AFK vẫn trên máy.
- Đọc/ghi khoá Protected từ module dùng API Protected của Cloud Save (service token). Người dựng kiểm tên hàm trong `Com.Unity.Services.CloudCode.Apis` 0.0.27 trước khi viết.

### 6.3 Phía máy

- `IGameServer` thêm một hàm `Task<OnlineResult<CommandResponse>>` cho mỗi lệnh. `UgsGameServer` gọi module, bọc 10 giây; `LocalGameServer` gọi thẳng `ServerHandlers` (cùng bước 1–6 ở 6.1, trong bộ nhớ) và giữ các chế độ lỗi giả của đợt 1, thêm `DelayNext(ms)` và "mất trả lời lệnh kế tiếp" (server đã ghi, máy nhận lỗi).
- Service (`GachaService`, `CombatService`, `StageProgressService`, `StarService`, `StaminaService`) giữ tên và vai trò; hàm ghi đổi sang `async Task<…>`. Kiểm phía máy (thiếu Gem, màn khoá…) giữ lại để báo nhanh, không gọi server.
- Áp kết quả: `ServerStateApplier.Apply(PlayerSave, ServerState)` chép field server giữ (Gem vào `Balances`), rồi `Persist()` (ghi bản đệm, đánh dấu `save` bẩn) và bắn `Wallet.Changed`/`Roster.Changed` bằng `SafeInvoke`. Lệnh lỗi → không đổi gì trong bộ nhớ.
- Gem chỉ đổi qua `Apply`. `DevOverlay.GrantGems` đổi sang lệnh `DevAddGem`.
- `GameSession.GrantNewGame` (game mới và Reset) thôi cấp Gem và nhân vật khởi đầu; chỉ cấp Coin/Material trong `NewGameConfig`. Phần còn lại do `Bootstrap`/`ResetPlayer` trả về.
- **Trang đang mở giữ ảnh chụp (L-035):** `Apply` là đường ghi mới; trang Chọn đội giữ ảnh chụp đội, nên bước dọn nhân vật không còn sở hữu chạy ở Boot trước khi có trang nào mở. `ResetPlayer` đi qua đường Reset hiện có (đã gọi `HubNav.CloseOpenPages()`).

## 7. Màn hình

### 7.1 Màn chờ (`WaitOverlay`)

Một lớp, dùng chung cho mọi lệnh server do người chơi bấm (Quay ×1/×10, Vào trận, Kết quả trận, Nâng sao, Nhận thưởng cốt truyện, nút DEV gọi server).

- Bật ngay khi gửi: `Image` kín màn, trong suốt, `raycastTarget = true`, vẽ trên thanh dưới và nút Thông báo, dưới `OnlineOverlay` (hộp mạng phải bấm được).
- Sau 0,5 giây (đồng hồ không phụ thuộc `timeScale`): hiện ô vuông xoay và chữ "Đang gửi…". Trả lời trước 0,5 giây thì không nháy gì.
- Quá 10 giây hoặc lỗi mạng → tắt lớp chờ, mở hộp Mất kết nối; Thử lại bật lại lớp chờ và gửi lại cùng `RequestId`.
- Bật/tắt theo chủ (`SetWaiting(owner, bool)`, code-style L-015).

Khung 1920×1080:
```
┌────────────────────────────────────────────────────────────┐
│                  (màn hiện tại, không tối đi)              │
│                                                            │
│                         ┌──────┐                           │
│                         │ ◇ xoay│  WaitSpinner_PH_64x64     │
│                         └──────┘                           │
│                        Đang gửi…   WaitLabel_PH_400x60      │
│                                                            │
│ [thanh dưới: bấm không ăn]                                 │
└────────────────────────────────────────────────────────────┘
```
Ô vuông trắng 64×64 (hình có sẵn của Unity), xoay 360°/giây; chữ cỡ như chữ thường của hộp mạng, có viền tối để đọc được trên mọi nền. Nằm giữa màn.

### 7.2 Hộp Cần cập nhật

Cùng khung `OnlineBox_PH_900x420` của `OnlineOverlay`, **không có nút** (không có đường đi tiếp). Hiện ở Tiêu đề (bước `Bootstrap`) và giữa game (lệnh bất kỳ).

```
┌────────────────── Cần cập nhật ──────────────────┐
│                                                   │
│   Bản game này đã cũ.                             │
│   Hãy cập nhật lên bản mới nhất để chơi tiếp.     │
│                                                   │
└───────────────────────────────────────────────────┘
```

Không có nút mở cửa hàng: đợt này chưa cần, và itch.io không có cửa hàng để mở.

### 7.3 Chữ mới (`UiText.Online.cs`)

| Khoá | Tiếng Việt | English |
|---|---|---|
| `online.sending` | Đang gửi… | Sending… |
| `online.too_old_title` | Cần cập nhật | Update required |
| `online.too_old_body` | Bản game này đã cũ.\nHãy cập nhật lên bản mới nhất để chơi tiếp. | This version is out of date.\nPlease update to the latest version to keep playing. |

Bề rộng: `online.too_old_body` tiếng Anh dài nhất, kiểm vừa 900 px như hộp mạng hiện có (L-039).

## 8. WebGL

Không thêm API mới ngoài những gì đợt 1 đã dùng (`Task`, UGS SDK). Hẹn giờ 10 giây và 0,5 giây dùng `Task.Delay`/đồng hồ thực, chạy được trên WebGL (một luồng). Không dùng `Thread`, `.Result`, `.Wait()`.

## 9. Test (không cần mạng)

- `ServerHandlers`: EditMode, `GameData` dựng trong test, giờ và `Random` cố định. Phần lớn test service hiện có (gacha, nâng sao, màn, thể lực) chuyển thành test handler; test service giữ phần "kiểm phía máy, áp kết quả, lệnh lỗi không đổi gì".
- Ca bắt buộc: gửi lại cùng `RequestId` không trừ hai lần (gacha, vào trận, kết quả trận); `OtherDevice`; `ClientTooOld` với số 0 và số 1; mất trả lời lệnh rồi Thử lại; ticket cũ/sai; `Settle` khi đồng hồ lùi; save đợt 1 có Gem và nhân vật nạp được và bị bỏ qua đúng; bản tải lên không còn field server giữ; `ResetPlayer` → `ServerState` bằng bản game mới (Gem và nhân vật khởi đầu, thể lực đầy, các field khác mặc định) cộng mã phiên cũ.
- `GameData.json` khớp từng ký tự với bản dựng từ SO; `DataHash` giống nhau ở hai phía.
- `dotnet test`: đọc/ghi khoá Protected đúng khoá; `ServerState`, `CommandResponse`, `GameData` qua JSON hai phía cho cùng giá trị (Newtonsoft ↔ `JsonUtility`); nạp `GameData.json` nhúng; bước 1–6 của 6.1 với kho giả.
- UGS thật: kiểm ở điểm dừng playtest (kế hoạch ghi danh sách).

## 10. Docs phải sửa (trong kế hoạch)

| File | Sửa |
|---|---|
| `docs/decisions/decisions.md` | D-008 "theo giờ máy" → "theo giờ server" (spec cha mục 8); D-005 thêm: `ResetPlayer` xoá cả khoá Protected và tạo lại `ServerState` |
| `docs/guides/unity-workflow.md` | Mục Online: xuất `GameData.json`, mã băm, deploy sau khi đổi dữ liệu cân bằng; `WaitOverlay`; lỗi giả mới của `LocalGameServer` |
| `docs/superpowers/specs/2026-11-13-online-design.md` | 5.5 ghi chú: so `SaveRevision` đã thay bằng `ActiveSessionId` (link spec này); 5.7 màn chờ: link mục 7.1 |
| `docs/project/structure.md` | `Rules/` có thêm kiểu save chuyển sang; `GameData.json` trong module |
| `docs/roadmap.md` | Dòng "Đang làm" và dòng Online |

## 11. Rủi ro

1. **Thông báo lỗi ném từ module có tới máy không** (để nhận `ClientTooOld`). Kiểm ở lần deploy đầu. Không tới thì máy chỉ thấy `ServerError` → hộp "Không kết nối được": vẫn chặn được, chỉ sai chữ.
2. **API Protected và tên môi trường trong package Cloud Code bản 0.0.x** chưa dùng trong dự án. Kiểm tên hàm trước khi viết; thiếu thì dừng báo 🎯.
3. **App bị giết sau khi server ghi, trước khi máy nhận trả lời:** Gem, nhân vật, thể lực đúng (lấy từ `state` lần mở sau); Material của trận đó mất. Chấp nhận (ít, và là tài nguyên máy giữ).
4. **Đổi sang `async`** chạm mọi luồng có Gem; trận đánh, gacha, nâng sao phải test lại cả PlayMode. Giảm: chia phiên theo luồng, playtest giữa đợt.
5. **Người thử đợt 1 mất phần server giữ** (quyết định 2): Gem, nhân vật gacha, mảnh, sao, màn đã qua, thể lực. Coin, quán, nâng cấp giữ nguyên. Báo người thử trước khi phát bản đợt 2.
6. **Hai máy mở game lần đầu cùng lúc** (chưa có `state`): cả hai `Bootstrap` cùng tạo, bản ghi sau đè bản trước. Cả hai đều là game mới nên không mất gì; máy thua nhận `OtherDevice` ở lệnh kế tiếp. Chấp nhận.

## 12. Khác với luồng màn (báo 🎨)

Nhắn 🎨 sau khi spec này được duyệt; 📐 không sửa `docs/design/*`:
1. Gacha do server roll; thể lực theo giờ server (spec cha mục 11, điểm 6).
2. **Màn chờ** khi bấm Quay, Vào trận, Nâng sao, Nhận: chặn cả màn, sau 0,5 giây hiện "Đang gửi…".
3. **Hộp Cần cập nhật** không có nút, ở Tiêu đề và giữa game.
