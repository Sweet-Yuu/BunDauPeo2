# Spec: Online (đăng nhập, save trên server, Gem do server giữ, Nhiệm vụ, Hộp quà)

> Ngày: 2026-11-13 · Trạng thái: **đã duyệt** (user, 2026-11-13: từng câu hỏi thiết kế, bản viết và ảnh mô phỏng đợt 1 bản 1 ở mục 10; project Unity đã liên kết Unity Cloud `51b908d5-2e99-4d12-89eb-65d9be5b6923`) · Phạm vi: bốn đợt Online cho bản công khai trên CH Play ngày 31/12/2026. Kế hoạch đợt 1: [2026-11-13-online-1.md](../plans/2026-11-13-online-1.md) (user duyệt 2026-11-13).
> Nguồn: brainstorm kênh 📐 Online với user ngày 2026-11-13; [vision](../../vision/vision.md) dòng Online; [roadmap](../../roadmap.md) dòng Online; [ideas](../../design/ideas.md) mục "Đang bàn" và "Để sau"; [D-003, D-005, D-007, D-012](../../decisions/decisions.md); [spec UI hub đợt C](2026-11-12-ui-hub-c-design.md) mục 5.3–5.4; skill `unity:build-live-game`.

## 1. Mục tiêu

User cần một bản game **chơi được và công khai trên CH Play ngày 31/12/2026** để đưa tới người dùng thật. User chọn: **phải có mạng mới chơi được**, và **server chống gian lận ở mức Gem** (mức 2, mục 4). Online là điều kiện để bản công khai an toàn: tiến độ không mất khi đổi máy, người chơi không tự cộng Gem hay quay gacha gian.

Thành công khi (cuối đợt 4):
1. Mở app → Tiêu đề → chạm → **đăng nhập tự động** (ẩn danh; Android có Google Play Games từ đợt 3) → Lobby. Không có mạng thì không vào được, có nút Thử lại.
2. Save nằm trên server. Gỡ game rồi cài lại (đã liên kết Google) hoặc sang máy mới → đăng nhập → đúng tiến độ cũ.
3. Gem, nhân vật sở hữu, mảnh, nâng sao, bộ đếm đảm bảo và lịch sử gacha, thể lực, màn đã qua, nhận nhiệm vụ, nhận quà **chỉ server ghi**. Sửa file trên máy hay chỉnh giờ máy không đổi được những thứ đó.
4. Thể lực, AFK, ngày mới của nhiệm vụ tính theo **giờ server**.
5. Reset Save Data xoá **toàn bộ** save trên máy và trên server, giữ ID người chơi ([D-005](../../decisions/decisions.md), user chốt 2026-11-13). Nút **Xoá tài khoản** xoá save và tài khoản (luật Google Play).
6. Nhiệm vụ ngày và tân thủ chạy, thưởng Gem do server trao. Hộp quà nhận quà phát từ server. Nút Thông báo có dòng phần thưởng chưa nhận.
7. Test EditMode và PlayMode chạy **không cần mạng** (server giả trong máy chạy đúng logic của server thật).

### Quyết định đã chốt với user (2026-11-13)

| Chủ đề | Quyết định |
|---|---|
| Vì sao online | Bản game công khai trên CH Play cho người dùng thật |
| Hạn | **Công khai** cho mọi người tải ngày 31/12 (không chỉ thử kín) |
| Tài khoản Play Console | User tạo sau (chưa có). Là mốc trễ nhất của lịch, xem mục 9 |
| Nạp tiền, quảng cáo | **Sau 31/12.** Bản 31/12 không có tiền thật |
| Dịch vụ server | **Unity Gaming Services (UGS)** |
| Đăng nhập | Đợt 1 **ẩn danh** tự động; đợt 3 **liên kết Google Play Games**; Play Console trễ thì **tên đăng nhập + mật khẩu** của UGS thay |
| Mạng | **Phải có mạng mới chơi được** (đổi vision: bỏ "phần chơi cốt lõi chạy offline") |
| Chống gian lận | **Mức 2**: save trên server, giờ server, Gem và mọi thứ sinh ra Gem hay nhân vật do server tính. Coin, Material, quán vẫn tính trên máy |
| Reset Save Data | Xoá save trên máy và server, **giữ ID**; liên kết Google còn nguyên |
| Xoá tài khoản | Nút riêng ở Menu (đề xuất, user không phản đối) |
| Phạm vi 31/12 | Đợt 1 nền, đợt 2 logic server, đợt 3 Google + Nhiệm vụ, đợt 4 Hộp quà + dòng Thông báo. **Bỏ Thông báo tin.** Sau 31/12: Hội, xếp hạng/PvP (ô Đấu trường giữ khoá), nạp tiền, quảng cáo |
| Nhiệm vụ | **Làm trước 31/12**, cả ngày và tân thủ, danh sách và thưởng như mục 5.10 (user: "chốt như hiện tại, sẽ đổi sau") |
| Trễ thì cắt | 1. Hộp quà → 2. Nhiệm vụ ngày (giữ tân thủ) → 3. dời ngày công khai sang đầu tháng 1. **Không cắt**: đăng nhập, liên kết tài khoản, Xoá tài khoản, Gem trên server |
| Bản itch.io | **WebGL**, bản demo hoàn chỉnh ra **một lần, không cập nhật**. Build là việc riêng của 🎯 (mục 12) |

### Lựa chọn của 📐 (user chưa xét riêng, sửa được)

- Giữ **Unity 6000.3**; logic server viết bằng C# trong **module Cloud Code kiểu `.ccmr`** (project .NET 8 ngoài `Assets/`). Không nâng Editor lên 6000.5 để dùng `.ccmu` vì rủi ro vỡ dự án sát hạn.
- Logic dùng chung (quay gacha, thể lực, thưởng màn, nhiệm vụ…) viết **một lần** bằng C# thuần, dùng cho cả module server lẫn server giả trong test.
- Danh sách quà nằm ở **Cloud Save Custom data** (sửa trên Unity Dashboard), không thêm Remote Config.
- Ngày mới của nhiệm vụ: **00:00 giờ Việt Nam (UTC+7)** theo giờ server, giống nhau cho mọi người chơi.
- Hai **môi trường UGS**: `development` (thử, có lệnh DEV) và `production` (bản phát hành).
- Hai máy cùng dùng một tài khoản: **máy đăng nhập sau thắng**, máy kia bị đưa về Tiêu đề (mục 5.5). Không có hộp chọn bản.

## 2. Hiện trạng (kiểm trong code ngày 2026-11-13, chỉ đọc)

- Unity **6000.3.15f1**. `Packages/manifest.json` chưa có package `com.unity.services.*`. Chưa liên kết Unity Cloud project.
- Mọi lần ghi save đi qua `GameSession.Persist()` → `SaveService.Save(PlayerSave)` (ghi `.tmp`, thay file chính, giữ `.bak`). Service nhận `Func<bool> persist`, đồng bộ, trả kết quả ngay.
- `GameBootstrap.Awake` (scene Boot) tạo `GameSession` **đồng bộ** từ `SaveService.Load()`, đăng ký service vào `ServiceLocator`, áp ngôn ngữ, rồi nạp `_startScene` = `"Title"`. `Title.unity` chỉ có "Chạm để bắt đầu" → Lobby (`Restaurant.unity`).
- Logic tiền và nhân vật chạy trên máy, dùng `System.Random`:
  - `GachaService.PullSingle/PullTen(bannerId)`: roll (`GachaRoller`), trừ Gem, trao nhân vật hoặc mảnh, cập nhật bộ đếm đảm bảo và lịch sử, lưu một lần.
  - `CombatService.EnterStage(stageId)`: kiểm, trừ thể lực, lưu ([D-008](../../decisions/decisions.md)). `StageProgressService.CompleteBattle(won)` / `CompleteStory(id)`: trao Gem + Material, đánh dấu màn đã qua.
  - `StarService.TryStarUp(id)`: trừ mảnh, tăng số lần nâng sao.
  - `StaminaService(save, config, Func<DateTime> utcNow)`: hồi theo giờ máy. `AfkService` và `RestaurantService` dùng `IClock`.
- Dữ liệu cân bằng nằm trong ScriptableObject: `CharacterCatalog`, `GachaBannerCatalog`, `StageCatalog`, `CombatConfig`, `StarConfig`, `NewGameConfig`.
- `GameSession.ResetToDefaults()` (D-005): xoá file, ghi đè `PlayerSave` bằng bản mới, cấp lại đồ game mới, báo `Wallet.Changed`, `Roster.Changed`, `ResetCompleted`. `HubNav.CloseOpenPages()` gọi trước (L-035).
- Panel DEV có +Gem, +Material, Hồi thể lực, Tua AFK, Chiến đấu thử, Reset. Công cụ thử (D-009) dùng `GameBootstrap.SavePathOverride` và thư mục `playtest-backup/`.
- Nút Thông báo: `NotifyRule` (UI) với `NotifyKind { Incident, StaffMissing, AfkCoins, StaminaFull, StarUp }`. Hồ sơ: hộp giữa màn 1200×800 (`NavBarBuilder`), có tên và avatar, chưa có ID.
- Lobby: hàng nút tròn có `HubRoundBtnShop_PH_110x110`; chỗ cho Hộp quà, Nhiệm vụ, Hội đã chừa (screen-flow).
- `Mota/` **chưa có** ảnh tham khảo màn Nhiệm vụ và Hộp quà (screen-flow ghi `hub-missions.jpg`, `hub-presents-popup.jpg` là ảnh còn thiếu).
- Nền test: **910 EditMode + 34 PlayMode** xanh.

## 3. Ngoài phạm vi (cố ý không làm)

- Nạp tiền thật, quảng cáo thưởng, Shop (sau 31/12; khi làm thì Gem đã sẵn trên server).
- Hội, bảng xếp hạng, PvP, chế độ Đấu trường (ô giữ khoá "Sắp có").
- Thông báo tin từ server (user đồng ý bỏ khỏi bản 31/12).
- Server chạy lại trận đánh để kiểm thắng thua thật (mức 3). Server chỉ kiểm điều kiện vào trận và trao thưởng một lần cho mỗi lượt vào.
- Server tính quán, Coin, Material, nâng cấp quán, xếp nhân viên, xếp đội (vẫn trên máy).
- Chuyển save cục bộ cũ lên server: chưa có người chơi thật; save thử trên máy bỏ qua.
- Quà gửi riêng từng người chơi (chỉ quà cho mọi người).
- Chuẩn bị phát hành CH Play và build WebGL (việc riêng, mục 12).
- Đặt tên lúc mới vào game, hướng dẫn lần đầu (vẫn chờ user; Nhiệm vụ tân thủ dẫn đường thay một phần).

## 4. Các phương án đã xét

**Mức chống gian lận** (user chọn mức 2):

| Mức | Nội dung | Chặn | Công |
|---|---|---|---|
| 1 | Phải có mạng, save trên server, giờ server | Sửa file save, chỉnh giờ máy | ~1,5 tuần |
| **2 (chọn)** | Mức 1 + Gem, gacha, thể lực, thưởng màn, nâng sao, thưởng nhiệm vụ và quà do server tính | Thêm: tự cộng Gem, quay gacha gian | thêm ~3–4 tuần |
| 3 | Server tính cả quán và trận | Gần như mọi kiểu | ≥ 8 tuần, không kịp |

**Cách lưu** (trước khi user chọn mức 2, 📐 đề xuất A; user chọn hướng server làm chủ):

| Cách | Mô tả | Lý do không chọn / chọn |
|---|---|---|
| A. Máy là bản chính, server giữ bản sao một khoá | Chơi offline, đẩy save lên sau | Không chặn được gian lận; user muốn server làm chủ |
| B. Chia save thành nhiều khoá, gộp từng phần | | Gộp lẫn hai bản dễ ra trạng thái lệch, ngược D-005 |
| **C (chọn, mức 2)** | Server là bản chính. Phần "server giữ" chỉ server ghi; phần "máy giữ" máy ghi nhưng nằm trên server | User chọn |

**Dịch vụ:** UGS (chọn: package có trong Package Manager, gói miễn phí đủ cho game mới, không có khoá bí mật trong game); Firebase (SDK nặng, ngoài Package Manager); PlayFab (SDK ngoài Package Manager, ít tài liệu cho Unity 6).

**Cách viết logic server:** `.ccmr` C# trên 6000.3 (chọn); nâng Editor 6000.5 để dùng `.ccmu` (rủi ro vỡ dự án); script JavaScript của Cloud Code (kiểu cũ, phải viết lại logic gacha bằng ngôn ngữ khác, không dùng chung với test).

**Danh sách quà:** Cloud Save Custom data (chọn: không thêm package, máy chỉ đọc, server đọc dễ); Remote Config (thêm một package chỉ để giữ một danh sách).

## 5. Thiết kế

### 5.1 Bốn đợt và lịch

| Đợt | Nội dung | Xong khoảng | Điểm dừng playtest |
|---|---|---|---|
| **1. Nền** | Package UGS, module server tối thiểu (khởi tạo người chơi, giờ server, reset), đăng nhập ẩn danh, cổng mạng ở Tiêu đề và giữa game, save trên server (phần "máy giữ"), giờ server cho thể lực/AFK/quán, Reset trên server, Xoá tài khoản, ID trong Hồ sơ | ~24/11 | Có, trên Editor (môi trường `development`) và APK |
| **2. Logic server** | Logic dùng chung, xuất dữ liệu SO cho server, chuyển Gem, nhân vật, mảnh, nâng sao, gacha, thể lực, vào trận và thưởng màn sang server; màn chờ khi gọi server | ~12/12 | Có |
| **3. Google + Nhiệm vụ** | Liên kết Google Play Games (hoặc tên + mật khẩu), Nhiệm vụ ngày và tân thủ | ~22/12 | Có |
| **4. Hộp quà** | Hộp quà, hai dòng phần thưởng chưa nhận trong nút Thông báo | ~28/12 | Có, cũng là playtest cuối trước khi xin phát hành |

Vòng thử kín CH Play (12 người, 14 ngày) bắt đầu khoảng 10–15/12, trong lúc đợt 2–3 còn làm; người thử nhận bản cập nhật. Mỗi đợt có spec con nếu cần chi tiết UI (ảnh mô phỏng duyệt trước khi dựng); spec này giữ luật chung. Kế hoạch viết từng đợt, sau khi đợt trước xong.

### 5.2 Kiến trúc

```
UI, Restaurant, Combat, Gacha, Characters (như cũ)
        │ gọi service như cũ; service gọi IGameServer cho phần "server giữ"
        ▼
Core: PlayerSave, GameSession, IGameServer, IServerClock       ← không biết UGS
        ▲                               ▲
Online (mới): UgsGameServer,            Rules (mới, C# thuần, noEngineReferences):
  UgsAuth, CloudSync                      ServerState, GameData, ServerHandlers
  → com.unity.services.*                  (gacha, thể lực, màn, nâng sao,
        │ Cloud Code                       nhiệm vụ, quà, game mới, reset)
        ▼                                        ▲            ▲
Module .ccmr "BunDauPeoServer" (.NET 8) ─────────┘            │ dùng chung file nguồn
LocalGameServer (Core hoặc Online, cho test và Editor) ───────┘
```

- **`BunDauPeo.Rules`** (assembly mới, `noEngineReferences: true`, không tham chiếu assembly nào): kiểu dữ liệu server (`ServerState`, `GameData`), và `ServerHandlers`, hàm thuần dạng `(state, request, data, nowUtc, random) → (state mới, response)`. Đây là logic thật của server. Module `.ccmr` đưa các file này vào bằng `<Compile Include="..\Assets\_Project\Rules\**\*.cs" />`. Không file nào trong `Rules` được dùng `UnityEngine`.
- **`IGameServer`** (Core): mỗi lệnh server một hàm `Task<…>`. Hai bản:
  - `UgsGameServer` (assembly mới `BunDauPeo.Online`, tham chiếu package UGS): gọi module qua `CloudCodeService.Instance.CallModuleEndpointAsync` (hoặc binding sinh từ `.ccmr`), đọc/ghi Cloud Save.
  - `LocalGameServer`: gọi thẳng `ServerHandlers` trên một `ServerState` trong máy, lưu bằng `SaveService` (file cục bộ). Dùng cho **mọi test** và cho **Editor ở chế độ Local** (mục 5.12). Vì cùng gọi `ServerHandlers`, server giả và server thật chạy đúng một logic.
- **D-002 giữ nguyên:** tính năng chỉ phụ thuộc `Core` (+ `Characters`); `Core` tham chiếu thêm `Rules`. Chỉ `Online` và `Boot` biết UGS.
- **Luật code:** service không chờ server trên luồng chính bằng `.Result`/`.Wait()`; luồng UI dùng `async` và màn chờ (mục 5.7). Mọi lệnh server có thể thất bại vì mạng: thất bại thì **không đổi gì trong bộ nhớ** (giống luật "lưu lỗi thì hoàn" hiện có), và mở hộp Mất kết nối (mục 5.4).

### 5.3 Dữ liệu: server giữ và máy giữ

Một `PlayerSave` trong bộ nhớ như hiện nay (code game ít đổi), nhưng trên server chia hai khoá Cloud Save:

| Khoá | Lớp truy cập Cloud Save | Ai ghi | Chứa |
|---|---|---|---|
| `state` | **Protected** (người chơi đọc được, chỉ server ghi) | Module server | `ServerState`: `Version`, `Revision`, Gem, `OwnedCharacterIds`, `Fragments`, `StarUps`, `GachaPity`, `GachaHistory`, `Stamina`, `ClearedStages`, `PendingBattle`, `Missions`, `ClaimedGiftIds` |
| `save` | **Default** (máy ghi) | Máy | Phần còn lại của `PlayerSave`: Coin, Material, `BattleTeam`, `Staff`, `Upgrades`, `DailyPresence`, `IncidentScores`, `Afk`, `Language`, `RestaurantBarOpen`, `PlayerName`, `AvatarCharacterId`, tiến độ nhiệm vụ do máy đếm |

- **Đợt 1:** chưa có `state`; toàn bộ `PlayerSave` nằm ở khoá `save`. Đợt 2 chuyển các field ở dòng `state` sang và từ đó máy **xoá các field đó khỏi bản `save` trước khi đẩy lên**; khi nạp, field "server giữ" luôn lấy từ `state`, bỏ qua giá trị trong `save`.
- **File trên máy** (`save.json` qua `SaveService`) còn lại làm **bản đệm**: ghi sau mỗi lần đổi như hiện nay, dùng để hiện ngôn ngữ đã lưu ở Tiêu đề trước khi đăng nhập và để Editor chạy chế độ Local. Bản đệm **không bao giờ** được đẩy đè lên server khi mở game: server là bản chính.
- **Material từ thưởng màn và quà:** server trả số lượng trong response, máy cộng vào `Wallet` rồi lưu `save`. Server không ghi khoá `save` (tránh hai bên cùng ghi một khoá).
- **Dữ liệu cân bằng cho server** (đợt 2): menu Editor `Tools/BunDauPeo/Export Server Data` xuất các SO ở mục 2 thành `GameData.json`, đặt trong module (nhúng làm `EmbeddedResource`). Test EditMode xuất lại trong bộ nhớ và so với file đã commit: đổi SO mà quên xuất thì test đỏ. Đổi dữ liệu cân bằng = xuất + deploy module.
- **Trường save mới** (luật D-005: có giá trị khởi tạo cho game mới, nằm trong `PlayerSave`/`ServerState`, Reset ghi đè bằng bản mặc định):

| Trường | Nơi | Đợt | Mặc định | Ý nghĩa |
|---|---|---|---|---|
| `SaveRevision` (`long`) | `PlayerSave` | 1 | `0` | Tăng mỗi lần đẩy `save` thành công; dùng ghi log và kiểm đồng bộ |
| `ServerState.Version`, `Revision` | `ServerState` | 2 | `1`, `0` | Phiên bản định dạng; tăng mỗi lần server ghi |
| `ActiveSessionId` (`string`) | `ServerState` | 2 | `""` | `Bootstrap` đặt id mới mỗi lần đăng nhập; lệnh mang id khác → lỗi "máy khác" (mục 5.4) |
| `PendingBattle` (`string` ticket, `string` stageId) | `ServerState` | 2 | rỗng | Lượt vào trận đã trả thể lực, chờ báo kết quả; mỗi ticket nhận thưởng một lần |
| `Missions` (ngày hiện tại, bộ đếm ngày, đã nhận ngày, đã nhận tân thủ, bộ đếm tân thủ) | `ServerState` | 3 | rỗng | Mục 5.10 |
| `MissionProgress` (bộ đếm việc máy làm) | `PlayerSave` | 3 | rỗng | Mục 5.10 |
| `ClaimedGiftIds` (`List<string>`) | `ServerState` | 4 | rỗng | Id quà đã nhận |

### 5.4 Đăng nhập và cổng mạng (đợt 1)

**Luồng mở app:** Boot (như nay, nhưng **chưa tạo `GameSession`**) → Tiêu đề (ngôn ngữ lấy từ bản đệm, không có thì tiếng Anh) → chạm → chữ "Chạm để bắt đầu" đổi thành "Đang kết nối…" → `UnityServices.InitializeAsync()` → đăng nhập (đã có phiên lưu trên máy thì dùng lại, không thì ẩn danh; đợt 3 Android thử Google Play Games trước) → lệnh server `Bootstrap` (tạo `ServerState` game mới nếu chưa có, trả `ServerState` và giờ server) → tải khoá `save` (chưa có = game mới: cấp đồ game mới theo `NewGameConfig` như nay) → tạo `GameSession` từ hai phần → đăng ký service → Lobby.

- Bước nào lỗi → hộp **Không kết nối được** (mục 10), nút **Thử lại** chạy lại từ bước lỗi. Không có đường vào game khi chưa xong `Bootstrap`.
- `GameBootstrap` tách: phần dựng session chuyển thành hàm `async` gọi từ Tiêu đề; về từ trận, đổi ngôn ngữ, Reset vẫn không qua Tiêu đề (luật đợt C giữ).

**Mất mạng giữa game:** lệnh server thất bại vì mạng, hoặc đẩy `save` thất bại liên tục quá **30 giây** → hộp **Mất kết nối** chặn mọi chạm (vẽ trên cả thanh dưới và nút Thông báo, như hộp xác nhận). Nút **Thử lại**: gọi lại lệnh đang dở (nếu có), đẩy `save`; thành công thì đóng hộp. Quán vẫn chạy phía sau như khi mở popup (không dừng mô phỏng); không có gì được lưu lên server cho tới khi có mạng.

**Module ở đợt 1** chỉ có `Bootstrap` (trả giờ server; chưa có `ServerState`) và `ResetPlayer` (xoá khoá `save`). Phần game mới đợt 1 vẫn cấp trên máy như nay; đợt 2 server cấp phần "server giữ".

**Phiên đăng nhập:** SDK Authentication tự cất token phiên trong `PlayerPrefs`. Đây là dữ liệu của SDK, không phải dữ liệu người chơi: ngoại lệ của D-005 luật 1, ghi vào D-005. Reset giữ token (giữ ID); Xoá tài khoản xoá token (`SignOut(clearCredentials: true)`).

**Máy khác dùng cùng tài khoản:** đẩy `save` bị Cloud Save báo xung đột write lock (máy khác đã ghi), hoặc (từ đợt 2) server báo `ActiveSessionId` không khớp → hộp **Tài khoản đang được dùng ở máy khác** với nút **Về màn Tiêu đề** (bỏ thay đổi chưa đẩy, nạp lại Tiêu đề, đăng nhập lại lấy bản trên server). Máy đăng nhập sau thắng.

### 5.5 Đẩy phần "máy giữ" lên server (đợt 1)

- `GameSession.Persist()` giữ nguyên chữ ký `bool`: ghi bản đệm (như cũ), đánh dấu `save` "bẩn", trả kết quả ghi bản đệm. `CloudSync` (Online) đẩy khoá `save` khi bẩn: **gộp lượt**, tối đa một lần mỗi **5 giây**, và đẩy ngay khi app vào nền (`OnApplicationPause(true)`) hay thoát.
- Đẩy dùng write lock của Cloud Save (`SaveItem` kèm lock nhận được lần trước). Xung đột → mục 5.4.
- Đang đẩy mà có thay đổi mới → đẩy thêm một lượt sau. Đẩy lỗi mạng → thử lại sau 5 giây; quá 30 giây → hộp Mất kết nối.
- Lần đẩy cuối trước khi thoát có thể không kịp (hệ điều hành giết app): mất tối đa ~5 giây thay đổi Coin/quán. Chấp nhận: phần quan trọng (Gem, nhân vật) server đã ghi ngay khi xảy ra.

### 5.6 Giờ server (đợt 1)

- `IServerClock` (Core): `UtcNow = DateTime.UtcNow + offset`, `offset` đo từ giờ server trong mỗi response của module (`Bootstrap` và mọi lệnh sau). Đổi giờ máy giữa lúc chơi không đổi `UtcNow` quá vài giây vì offset đo lại ở mỗi lệnh.
- `StaminaService` (`utcNow`), `AfkService`, `RestaurantService` (`IClock`) nhận đồng hồ này thay giờ máy. Đợt 2 server tự tính thể lực bằng giờ của nó; giờ trên máy chỉ để hiện.

### 5.7 Logic server (đợt 2)

Lệnh của module (mỗi lệnh đọc `state`, chạy `ServerHandlers`, ghi `state` một lần, trả kết quả + `ServerState` mới + giờ server):

| Lệnh | Kiểm | Ghi | Thay cho |
|---|---|---|---|
| `Bootstrap()` | — | tạo `ServerState` game mới (nhân vật khởi đầu, Gem khởi đầu, thể lực đầy) nếu chưa có | phần "server giữ" của `GameSession` game mới |
| `GachaPull(bannerId, count)` | banner hợp lệ, `count` 1 hoặc 10, đủ Gem | roll bằng random của server; trừ Gem, trao nhân vật hoặc mảnh, bộ đếm, lịch sử | `GachaService.Pull` |
| `EnterStage(stageId, team[])` | màn đã mở, đội 1–n người **đều đã sở hữu**, đủ thể lực theo giờ server | trừ thể lực, tạo `PendingBattle` (ticket mới, thay ticket cũ nếu có) | `CombatService.EnterStage` |
| `CompleteStage(ticket, won)` | ticket khớp `PendingBattle` | `won`: Gem, đánh dấu màn đã qua, trả Material; luôn xoá ticket | `StageProgressService.CompleteBattle` |
| `CompleteStory(stageId)` | màn cốt truyện, đã mở | đánh dấu đã qua, thưởng (nếu có) | `CompleteStory` |
| `StarUp(characterId)` | sở hữu, chưa tối đa, đủ mảnh | trừ mảnh, tăng số lần nâng sao | `StarService.TryStarUp` |
| `ResetPlayer()` | — | xoá mọi khoá của người chơi (`state`, `save`), tạo lại `ServerState` game mới | mục 5.8 |
| `Dev*` (+Gem, hồi thể lực, chiến đấu thử) | **chỉ môi trường `development`** | như nút DEV | panel DEV |

- Service phía máy giữ tên và vai trò, đổi phần ghi thành `async` gọi `IGameServer`, rồi áp `ServerState` trả về vào `PlayerSave` và bắn `Wallet.Changed`/`Roster.Changed`. Thứ tự kiểm phía máy giữ lại để báo lỗi nhanh (thiếu Gem thì không gọi server), nhưng **server mới quyết định**.
- **Màn chờ:** bấm Quay gacha, Vào trận, Nâng sao, Nhận → nút mờ + vòng chờ nhỏ tới khi server trả; quá 10 giây → hộp Mất kết nối. Không bấm được hai lần.
- Trận đánh vẫn chạy trên máy như hiện nay. Thắng thua do máy báo (không kiểm được, mục 3); server chỉ đảm bảo mỗi lần trả thể lực nhận thưởng tối đa một lần.
- "Chiến đấu thử" của DEV (không trừ thể lực, không thưởng) vẫn chạy trên máy, không gọi server.

### 5.8 Reset Save Data và Xoá tài khoản (đợt 1)

**Reset Save Data** (Menu, hộp xác nhận như nay) — luật D-005 mở rộng cho server:
1. Chưa xác nhận thì không xoá gì. Hộp xác nhận thêm dòng: "Xoá cả dữ liệu trên máy chủ. Tài khoản và liên kết vẫn giữ."
2. `HubNav.CloseOpenPages()` (L-035), dừng đẩy `save`.
3. Gọi `ResetPlayer()` (cần mạng; mất mạng → hộp Mất kết nối, **không xoá gì**).
4. Thành công → xoá bản đệm trên máy như `ResetToDefaults()` hiện nay (mọi `save.json.*`, trừ `playtest-backup/` theo ngoại lệ D-009), ghi đè `PlayerSave` bằng bản mới + `ServerState` trả về, cấp đồ game mới phần "máy giữ", lưu và đẩy `save`, báo `Wallet.Changed`, `Roster.Changed`, `ResetCompleted`, áp ngôn ngữ tiếng Anh.
5. ID người chơi giữ nguyên.

Lý do làm server trước rồi mới xoá máy: nếu xoá máy trước mà lệnh server lỗi, lần mở sau sẽ nạp lại tiến độ từ server, người chơi tưởng Reset không chạy.

**Xoá tài khoản** (nút mới ở Menu, dưới Reset, hộp xác nhận riêng ghi "vĩnh viễn, không lấy lại được, mất cả liên kết Google"): `ResetPlayer()` → `AuthenticationService.Instance.DeleteAccountAsync()` → xoá bản đệm như Reset → đăng nhập ẩn danh mới → `Bootstrap` → game mới. Đây là đường xoá trong app theo luật Google Play; đường xoá qua web (email hỗ trợ trong trang chính sách quyền riêng tư) thuộc việc chuẩn bị phát hành (mục 12).

**Menu Editor `Tools/BunDauPeo/Reset Save Data...`:** chế độ Local như nay (xoá file); đang Play với UGS thì đi qua session đang chạy (D-005 luật 4).

### 5.9 Hồ sơ online và liên kết tài khoản

- **Đợt 1:** Hồ sơ thêm dòng **ID: `<PlayerId>`** và nút **Sao chép** (chép vào clipboard, hiện "Đã chép" 2 giây). ID là `AuthenticationService.Instance.PlayerId`.
- **Đợt 3, Android:** Hồ sơ thêm dòng **Tài khoản**: "Chưa liên kết" + nút **Liên kết Google Play Games**, hoặc "Đã liên kết Google Play Games".
  - Mở app trên Android: thử đăng nhập Google Play Games im lặng; được thì `SignInWithGooglePlayGamesAsync(authCode)` (vào đúng người chơi đã liên kết), không thì ẩn danh như đợt 1.
  - Bấm Liên kết: lấy auth code → `LinkWithGooglePlayGamesAsync`. Tài khoản Google đã gắn với người chơi khác → hộp: "Tài khoản Google này đã có tiến độ khác. Chuyển sang tiến độ đó? Tiến độ hiện tại (chưa liên kết) sẽ mất." [Chuyển] [Huỷ]. Chuyển = đăng xuất, đăng nhập bằng Google.
  - Dự phòng (Play Console chưa có vào đầu tháng 12): dòng Tài khoản cho **đặt tên đăng nhập + mật khẩu** (`AddUsernamePasswordAsync`) và màn Tiêu đề có nút **Đăng nhập bằng tài khoản**. Chi tiết UI ở spec con đợt 3.
- **WebGL:** không có dòng Tài khoản (chỉ ẩn danh; bản demo không cập nhật nên chấp nhận).

### 5.10 Nhiệm vụ (đợt 3)

User chốt danh sách và thưởng dưới đây làm số khởi điểm (đổi sau trong asset). Danh sách nằm trong `MissionCatalog` (SO) và đi vào `GameData.json` cho server.

**Nhiệm vụ ngày** (làm mới 00:00 UTC+7 theo giờ server):

| Id | Việc | Mục tiêu | Thưởng | Ai đếm |
|---|---|---|---|---|
| `daily_enter_3` | Vào trận | 3 | 20 Gem | server (`EnterStage`) |
| `daily_win_1` | Thắng màn | 1 | 20 Gem | server (`CompleteStage` thắng) |
| `daily_pull_1` | Quay gacha | 1 lượt | 20 Gem | server (`GachaPull`, quay 10 tính 10) |
| `daily_afk_1` | Thu tiền AFK | 1 | 20 Gem | máy |
| `daily_incident_1` | Xử lý sự cố ở quán | 1 | 20 Gem | máy |
| `daily_all` | Đủ mục tiêu cả 5 việc trên (chưa cần nhận) | 5 | 50 Gem | server |

**Nhiệm vụ tân thủ** (một lần, hiện theo thứ tự, làm trước cũng được tính):

| # | Id | Việc | Thưởng | Ai đếm |
|---|---|---|---|---|
| 1 | `new_name` | Đặt tên trong Hồ sơ | 50 Gem | máy |
| 2 | `new_staff` | Xếp 1 nhân viên | 50 Gem | máy |
| 3 | `new_first_win` | Xếp đội và thắng màn đầu tiên | 100 Gem | server |
| 4 | `new_first_pull` | Quay gacha lần đầu | 50 Gem | server |
| 5 | `new_upgrade` | Mua nâng cấp quán đầu tiên | 50 Gem | máy |
| 6 | `new_afk` | Thu tiền AFK lần đầu | 50 Gem | máy |
| 7 | `new_star_up` | Nâng sao một nhân vật | 100 Gem | server |
| 8 | `new_win_10` | Thắng 10 màn đánh khác nhau (không tính màn cốt truyện) | 200 Gem | server (`ClearedStages`) |

- **Nhận:** lệnh server `ClaimMission(id)` / `ClaimAllMissions()`. Server kiểm: chưa nhận (ngày: trong ngày hiện tại; tân thủ: chưa bao giờ), đủ mục tiêu.
  - Việc **server đếm**: server dùng bộ đếm của chính nó trong `ServerState.Missions`.
  - Việc **máy đếm** (AFK, sự cố, tên, nhân viên, nâng cấp): máy gửi kèm bộ đếm của nó; server tin (mức 2 không bảo vệ Coin và quán) nhưng vẫn giữ giới hạn một lần mỗi ngày / một lần mãi mãi. Gian lận tối đa 100 Gem/ngày + 200 Gem một lần: chấp nhận.
- **Màn Nhiệm vụ:** mở từ nút tròn **Nhiệm vụ** ở hàng nút tròn của Lobby. Trang kín màn (L-033) với tab Ngày / Tân thủ, mỗi dòng: tên, thanh tiến độ, thưởng, nút Nhận (mờ khi chưa đủ; "Đã nhận" khi xong); nút Nhận hết; tab Ngày ghi giờ còn lại tới lượt làm mới. Ảnh mô phỏng duyệt trong spec con đợt 3. **Cần ảnh tham khảo** `Mota/hub-missions.jpg` (màn nhiệm vụ của game tham khảo) nếu user có.
- Reset xoá toàn bộ tiến độ và lượt nhận (D-005).

### 5.11 Hộp quà và dòng Thông báo (đợt 4)

- Danh sách quà ở Cloud Save **Custom data**, id `gifts`, khoá `list`, user sửa trên Unity Dashboard: `[{ "id", "titleVi", "titleEn", "rewards": [{ "type": "Gem|Coin|Material", "amount" }], "startUtc", "endUtc" }]`. Máy đọc (chỉ đọc), server đọc khi nhận.
- **Hộp quà:** nút tròn **Hộp quà** ở Lobby → trang kín màn: danh sách quà đang trong hạn chưa nhận (tên, thưởng, hạn), nút Nhận, Nhận hết. `ClaimGift(id)` / `ClaimAllGifts()`: server kiểm trong hạn và chưa nhận; Gem ghi vào `state`, Coin/Material trả cho máy cộng. Ảnh mô phỏng ở spec con đợt 4. **Cần ảnh tham khảo** `Mota/hub-presents-popup.jpg` nếu user có.
- **Nút Thông báo:** thêm `NotifyKind.MissionReward` ("Có phần thưởng nhiệm vụ chưa nhận" → mở Nhiệm vụ, tab có thưởng) và `NotifyKind.GiftReward` ("Có quà chưa nhận" → mở Hộp quà), đứng cuối thứ tự enum.
- Reset **không** xoá danh sách quà (dữ liệu game, không phải dữ liệu người chơi) nhưng xoá `ClaimedGiftIds`, nên quà còn hạn nhận lại được sau Reset: chấp nhận (Reset mất toàn bộ tiến độ, không có lợi).

### 5.12 Editor, panel DEV và công cụ thử

- **Chế độ server trong Editor:** menu `Tools/BunDauPeo/Online Mode/Local` (mặc định) và `.../UGS (development)`. Local = `LocalGameServer` + file như hiện nay: mọi công cụ D-009 (`SavePathOverride`, `playtest-backup/`, Backup/Restore Save) giữ nguyên. UGS = đăng nhập thật vào môi trường `development`. Lựa chọn lưu trong `EditorPrefs` (thiết lập của Editor, không phải dữ liệu người chơi, không thuộc D-005). Bản build luôn dùng UGS.
- **Test** (EditMode, PlayMode) luôn dùng `LocalGameServer`, không bao giờ chạm mạng hay save thật.
- **Panel DEV:** +Gem, Hồi thể lực gọi lệnh `Dev*` của server (Local: chạy thẳng; UGS: chỉ môi trường `development` chấp nhận). +Material, Tua AFK vẫn trên máy.

### 5.13 WebGL

Phần Online phải chạy trên WebGL: không dùng API chỉ có trên Android ngoài `#if UNITY_ANDROID` (Google Play Games). Kiểm ở đợt 1 rằng Authentication, Cloud Save, Cloud Code chạy trong bản WebGL (bước thử của kế hoạch; nếu không chạy thì báo 🎯 trước khi làm tiếp). Build WebGL và tải lên itch.io là việc riêng (mục 12).

### 5.14 Chữ mới (`UiText`, D-012)

File mới `UiText.Online.cs` (khu `online.`); thêm khoá vào khu có sẵn khi thuộc khu đó. Mỗi khoá có bản Anh, `HardcodedTextTests` và `UiTextTests` xanh. Đợt 1:

| Khoá | Tiếng Việt | English |
|---|---|---|
| `online.connecting` | Đang kết nối… | Connecting… |
| `online.connect_failed_title` | Không kết nối được | Can't connect |
| `online.connect_failed_body` | Cần có mạng để chơi. Kiểm tra mạng rồi thử lại. | An internet connection is required. Check your connection and try again. |
| `online.lost_title` | Mất kết nối | Connection lost |
| `online.lost_body` | Đang chờ kết nối lại với máy chủ. | Waiting to reconnect to the server. |
| `online.retry` | Thử lại | Retry |
| `online.other_device_title` | Tài khoản đang dùng ở máy khác | Account in use on another device |
| `online.other_device_body` | Tài khoản này vừa đăng nhập ở máy khác. Về màn Tiêu đề để tải lại. | This account just signed in on another device. Return to the title screen to reload. |
| `online.to_title` | Về màn Tiêu đề | Back to title |
| `profile.player_id` | ID: {0} | ID: {0} |
| `profile.copy` | Sao chép | Copy |
| `profile.copied` | Đã chép | Copied |
| `menu.delete_account` | Xoá tài khoản | Delete account |
| `menu.delete_account_confirm` | Xoá vĩnh viễn tài khoản và mọi dữ liệu, kể cả liên kết Google. Không lấy lại được. | Permanently delete your account and all data, including linked Google account. This cannot be undone. |
| (sửa) hộp xác nhận Reset | thêm câu: Xoá cả dữ liệu trên máy chủ. Tài khoản và liên kết vẫn giữ. | Also deletes your data on the server. Your account and links are kept. |

Khoá của đợt 2–4 (màn chờ, Nhiệm vụ, Hộp quà, Tài khoản, hai dòng Thông báo) liệt kê trong kế hoạch từng đợt. Tên khoá Reset hiện có: người dựng tra trong `UiText.Hub.cs`.

## 6. Package, công cụ và việc user tự làm

**Package mới (hỏi user trước khi thêm; người dựng kiểm bản có trên 6000.3 bằng skill `unity:unity-package-management`):**

| Package | Đợt | Bản tối thiểu (theo skill) | Dùng cho |
|---|---|---|---|
| `com.unity.services.core` | 1 | 1.16.0 | Khởi tạo UGS |
| `com.unity.services.authentication` | 1 | 3.6.1 | Đăng nhập, liên kết, xoá tài khoản |
| `com.unity.services.cloudsave` | 1 | 3.4.0 | Khoá `save`, đọc `state`, Custom data `gifts` |
| `com.unity.services.cloudcode` | 1 | dòng **2.10.x** (≥ 2.10.3, deploy `.ccmr`; dòng 3.x dành cho `.ccmu` 6000.5+) | Gọi module |
| `com.unity.services.deployment` | 1 | 1.7.2 | Deploy module từ Editor (Editor-only) |
| Google Play Games plugin for Unity (Google, ngoài Package Manager, kèm External Dependency Manager) | 3 | bản v2 mới nhất hỗ trợ Unity 6 | Đăng nhập Google |

**Công cụ trên máy (hỏi user trước khi cài):** .NET 8 SDK (build và test module). Module WebGL của Unity 6000.3.15f1 thuộc việc build WebGL (mục 12).

**Việc chỉ user làm được (agent không tạo tài khoản hay khoá):**
1. Đợt 1, trước khi 🔨 chạy thật: tạo project trên Unity Cloud (cloud.unity.com) bằng tài khoản Unity đang dùng, tạo môi trường `development` (có sẵn `production`), liên kết project Unity (Edit > Project Settings > Services), bật Authentication (ẩn danh), Cloud Save, Cloud Code trên Dashboard. Gói miễn phí: Cloud Save 1 triệu lượt đọc/tháng, Cloud Code 1 triệu lượt gọi/tháng ([bảng giá UGS](https://unity.com/products/gaming-services/pricing)); có thể phải khai thẻ để bật trả theo dùng — user tự quyết.
2. Đợt 3: tài khoản Google Play Console (25 USD), app trên Play Console, cấu hình Play Games Services, OAuth client trên Google Cloud; nhập Client ID/Secret vào Unity Dashboard (Authentication → Google Play Games). Kế hoạch đợt 3 ghi từng bước.
3. Đợt 4: tạo Custom data `gifts` và quà đầu tiên trên Dashboard (kế hoạch ghi mẫu JSON).

**Khoá và bí mật:** game chỉ chứa Project ID và tên môi trường (không bí mật). Không có khoá API nào trong repo. Client Secret của Google chỉ nằm trên Unity Dashboard. Nếu cần khoá service account để deploy bằng CLI, khoá nằm ngoài repo (biến môi trường), không commit.

## 7. Test

- **Không mạng:** mọi test dùng `LocalGameServer` + `ServerHandlers`. Logic server (gacha, thể lực, màn, nâng sao, nhiệm vụ, quà, reset) test ở EditMode trên `ServerHandlers` trực tiếp với `GameData` dựng trong test và đồng hồ, random cố định.
- **Lỗi mạng giả:** `LocalGameServer` có chế độ "lỗi lệnh kế tiếp" và "trễ N ms" để test: lệnh lỗi không đổi bộ nhớ; hộp Mất kết nối hiện; Thử lại chạy lại đúng lệnh; không bấm hai lần khi đang chờ.
- **Đồng bộ:** `CloudSync` test với kho giả (`ICloudStore`): gộp lượt 5 giây, đẩy khi vào nền, xung đột write lock → sự kiện "máy khác", lỗi liên tục 30 giây → sự kiện mất kết nối.
- **Reset/Xoá tài khoản:** server lỗi thì không xoá gì trên máy; thành công thì mọi field về mặc định (mở rộng test D-005 hiện có), ID giữ (Reset) hoặc đổi (Xoá tài khoản).
- **Dữ liệu server:** test so `GameData.json` đã commit với bản xuất từ SO.
- **Module `.ccmr`:** test .NET (`dotnet test`) chỉ cho lớp bọc endpoint (đọc/ghi Cloud Save đúng khoá và lớp truy cập, JSON tương thích `JsonUtility` ↔ `System.Text.Json`); logic đã phủ bởi test Unity.
- **UGS thật:** không có test tự động. Kiểm ở điểm dừng playtest từng đợt trên môi trường `development` (Editor chế độ UGS và APK), theo danh sách trong kế hoạch.
- Test PlayMode hiện có (`SmokeLoopTests` nạp Boot rồi chờ Restaurant) phải đi qua Tiêu đề và bước kết nối với `LocalGameServer`.
- Số test dự kiến ghi trong kế hoạch từng đợt.

## 8. Docs phải sửa (làm trong kế hoạch, sau khi spec được duyệt)

| File | Sửa | Đợt |
|---|---|---|
| `docs/vision/vision.md` | Một câu mô tả: bỏ "phần chơi cốt lõi chạy offline" → "cần mạng để chơi; tiến độ lưu trên server". Nền tảng: bỏ "Quán, chiến đấu và gacha chạy offline". Trong phạm vi: dòng Online ghi phạm vi đã chốt (link spec này), bỏ `TODO(user)`. User đã đồng ý đổi hướng 2026-11-13 | 1 |
| `docs/decisions/decisions.md` | **D-013** mới: online bằng UGS, phải có mạng, server giữ Gem (mức 2), `.ccmr` trên 6000.3, `Rules` dùng chung. **D-003**: trạng thái thành "Superseded by D-007 và D-013" (phần "save JSON cục bộ, roll gacha trên máy" cũng bị thay). **D-007**: thêm "Cập nhật 2026-11-13: quán, chiến đấu, gacha không còn chạy offline (D-013); nạp tiền và quảng cáo sau 31/12". **D-005**: luật 1 thêm "và `ServerState` do server ghi", và ngoại lệ token phiên của SDK Authentication trong `PlayerPrefs` (mục 5.4); thêm luật 5: Reset gọi `ResetPlayer` trên server trước, lỗi thì không xoá gì; Xoá tài khoản là đường riêng. **D-008**: "theo giờ máy" → "theo giờ server" | 1 (D-013, D-003, D-007, D-005), 2 (D-008) |
| `docs/project/tech-stack.md` | Bảng thêm dòng Online (package UGS và bản), .NET 8 SDK, module `ServerModule/` (vị trí, cách deploy); ghi chú hai môi trường | 1 |
| `docs/project/structure.md` | Thêm `Rules/`, `Online/`, thư mục module ngoài `Assets/` | 1 |
| `docs/guides/unity-workflow.md` | Mục mới "Online": chế độ Local/UGS trong Editor, deploy module, xuất `GameData.json` | 1–2 |
| `docs/roadmap.md` | Dòng "Đang làm" và dòng Online (đợt 1–4) | mỗi đợt |
| `docs/design/*` | **Không sửa** (file của 🎨); nhắn 🎨 mục 11 | — |

## 9. Rủi ro

1. **Tài khoản Play Console chưa có.** Vòng thử kín 14 ngày chỉ chạy khi có tài khoản đã xác minh. Tạo sau khoảng 25/11 thì gần như không kịp công khai 31/12. Google có thể từ chối lần xin phát hành đầu, khi đó chạy lại 14 ngày. Giảm: user tạo tài khoản sớm nhất có thể.
2. **Lịch không còn chỗ dự phòng.** Đợt 2 chạm gần như mọi luồng có Gem (gacha, trận, nâng sao) và đổi sang `async`. Giảm: thứ tự cắt đã chốt; đợt 1 làm phần rủi ro công cụ (.NET SDK, deploy module) trước tiên.
3. **Công cụ `.ccmr` trên Windows** (.NET 8 SDK, publish `linux-x64`, Deployment Window) chưa từng chạy trong dự án. Giảm: task đầu đợt 1 dựng module rỗng và deploy thử, dừng báo nếu không được.
4. **JSON hai phía khác nhau:** `JsonUtility` (máy) và `System.Text.Json` (server) có thể khác cách ghi enum, `long`, field null. Giảm: test tương thích ở mục 7; kiểu dữ liệu trong `Rules` chỉ dùng field công khai, `List`, kiểu cơ bản.
5. **Độ trễ mạng** làm gacha, vào trận chậm hơn; mạng yếu ở Việt Nam có thể hiện hộp Mất kết nối thường xuyên. Giảm: màn chờ, ngưỡng 10 giây, Thử lại.
6. **Trận đánh không kiểm được:** người sửa bộ nhớ có thể báo thắng mọi trận. Thưởng vẫn bị giới hạn bởi thể lực theo giờ server (~2.880 Gem/ngày tối đa như D-008). Chấp nhận ở mức 2.
7. **UGS trên WebGL** chưa kiểm trong dự án. Giảm: bước thử ở đợt 1 (mục 5.13).
8. **Google Play Games plugin** là package ngoài, kéo thư viện Android (EDM4U), có thể làm vỡ build Android đang ổn (D-011). Giảm: thêm ở đầu đợt 3, build APK ngay sau khi thêm; dự phòng tên + mật khẩu.
9. **Xoá tài khoản và dữ liệu Cloud Save:** chưa kiểm `DeleteAccountAsync` có tự xoá Cloud Save hay không; vì vậy luồng gọi `ResetPlayer` trước (mục 5.8).
10. **Giới hạn Cloud Code** (số lượt gọi mỗi người chơi mỗi phút) chưa kiểm. Người dựng đọc trang Limits của Cloud Code ở đợt 1; gộp lượt đẩy `save` 5 giây đã giảm phần lớn lượt gọi.
11. **Gian lận còn lại** ở phần máy giữ (Coin, Material, quán) và bộ đếm nhiệm vụ do máy đếm: chấp nhận đến khi có nạp tiền.

## 10. Ảnh mô phỏng đợt 1 (chờ user duyệt)

Khung 1920×1080. Hộp giữ cỡ, nền mờ kín màn vẽ trên thanh dưới (như hộp xác nhận Reset). Object mới đặt tên `_PH_<w>x<h>`.

**Tiêu đề, sau khi chạm** (bản 1):
```
                     [Logo_PH_900x300: "Bun Dau Peo"]

                          Đang kết nối…   (thay chỗ "Chạm để bắt đầu")
                                                              Ver 1.0
```

**Hộp Không kết nối được / Mất kết nối** (bản 1; cùng một hộp, đổi chữ; `OnlineBox_PH_900x420`):
```
┌──────────── Không kết nối được ────────────┐
│                                             │
│   Cần có mạng để chơi.                      │
│   Kiểm tra mạng rồi thử lại.                │
│                                             │
│               [  Thử lại  ]                 │
└─────────────────────────────────────────────┘
```

**Hộp Tài khoản đang dùng ở máy khác** (bản 1; cùng khung, nút "Về màn Tiêu đề").

**Hồ sơ, thêm dòng ID** (bản 1; dưới dòng gợi ý "Tối đa 12 ký tự", cột trái):
```
┌──────────────────────── Hồ sơ ────────────────────────┐
│   ( A )  avatar lớn          Đổi avatar                 │
│                              (●)(●)(●)(●)(●)            │
│  [Người chơi        ]        (●)(●)(●)(●)(●)  cuộn      │
│  Tối đa 12 ký tự             (●)(●)                     │
│  ID: 4fK2…(đủ ID)  [Sao chép]                           │
│                    [ Đóng ]                             │
└─────────────────────────────────────────────────────────┘
```

**Menu, thêm nút Xoá tài khoản** (bản 1; ngay dưới nút Reset dữ liệu, cùng cỡ, chữ đỏ; bấm → hộp xác nhận có [Xoá] [Huỷ]):
```
  Ngôn ngữ   [ Tiếng Việt | English ]
  [ Reset dữ liệu ]
  [ Xoá tài khoản ]      ← mới
  [ DEV ]  (chỉ bản dev)
```

Màn chờ (đợt 2), Nhiệm vụ, Tài khoản (đợt 3), Hộp quà (đợt 4): mô phỏng trong spec con của đợt đó.

## 11. Khác với luồng màn (báo 🎨)

📐 không sửa `screen-flow.md`, `ideas.md`, `game-design.md`; nhắn 🎨 các điểm này:
1. **Phải có mạng mới chơi được.** Tiêu đề có bước "Đang kết nối…", hộp Không kết nối được, hộp Mất kết nối giữa game, hộp Tài khoản đang dùng ở máy khác.
2. **Hồ sơ** có dòng ID + Sao chép (đợt 1), dòng Tài khoản + Liên kết Google Play Games (đợt 3, chỉ Android).
3. **Menu** có nút **Xoá tài khoản** dưới Reset. Hộp xác nhận Reset thêm câu về dữ liệu trên máy chủ.
4. **Nhiệm vụ** (đợt 3) và **Hộp quà** (đợt 4) là trang kín màn mở từ hàng nút tròn của Lobby. Nút tròn **Hội** vẫn chưa dựng. **Thông báo tin** bỏ khỏi bản 31/12.
5. Nút Thông báo có thêm 2 loại: **Phần thưởng nhiệm vụ chưa nhận**, **Quà chưa nhận** (đợt 4).
6. `game-design.md`: Gacha "roll trên máy, vì game offline" → server roll (đợt 2); Thể lực "theo giờ máy" → giờ server; Tiền tệ: Gem thêm nguồn Nhiệm vụ và Hộp quà; mục mới **Nhiệm vụ** (bảng mục 5.10); Kiếm tiền: nạp tiền và quảng cáo sau 31/12, Gem đã nằm trên server.
7. `ideas.md`: Phạm vi online đã chốt (link spec này); Nhiệm vụ chuyển từ "Để sau" sang làm (user chốt 2026-11-13, đảo lại quyết định 2026-11-03 bỏ nhiệm vụ ngày/tuần); Thông báo tin vẫn "Để sau"; Hồ sơ online làm ở đợt 1 và 3.

## 12. Việc ngoài spec (đề xuất cho 🎯 mở kênh riêng)

1. **Chuẩn bị phát hành CH Play** (gấp, chạy song song đợt 1): AAB thay APK, ký bản phát hành (keystore ngoài repo), target API 36 ([yêu cầu Google Play](https://support.google.com/googleplay/android-developer/answer/11926878)), trang chính sách quyền riêng tư có đường xoá tài khoản qua web ([luật xoá tài khoản](https://support.google.com/googleplay/android-developer/answer/13327111)), khai báo Data safety (UGS thu Player ID), trang cửa hàng, phân loại độ tuổi, vòng thử kín 12 người × 14 ngày ([luật thử kín](https://support.google.com/googleplay/android-developer/answer/14151465)). Bản đầu lên thử kín có thể là game hiện tại; Online vào bằng bản cập nhật.
2. **Build WebGL cho itch.io**: cài module WebGL, cấu hình (nén tương thích itch), build bản demo một lần khi 🎯 chọn đợt Online nào là bản demo.
