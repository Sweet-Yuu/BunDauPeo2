# Spec: Công cụ thử tự động

> Ngày: 2026-11-03 · Trạng thái: **đã duyệt** (user, 2026-11-03, cả thiết kế lẫn bản viết; ba lựa chọn ở mục 1) · Phạm vi: `PlaytestTools` (assembly Editor), `PlaytestKit` (assembly runtime nhỏ), một seam ở `Boot`, một smoke test PlayMode chạy trọn vòng Boot → bản đồ → đội → trận AUTO → thưởng → nạp lại save bằng save tạm.
> Nguồn các bẫy cần gom: [unity-workflow.md](../../guides/unity-workflow.md) (L-004, L-007, L-009, L-011, L-014, L-019, L-020). Luật save: [D-005](../../decisions/decisions.md). Vòng chơi cần chạy: [spec Combat](2026-11-02-combat-design.md). Kế hoạch: [plans/2026-11-03-playtest-tools.md](../plans/2026-11-03-playtest-tools.md).
> **Đã xong, đây là lịch sử.** Khi spec khác code hoặc [unity-workflow.md](../../guides/unity-workflow.md), code và unity-workflow thắng; không sửa spec này để theo kịp.

## 1. Mục tiêu

Mỗi lần playtest hiện phải nhớ tay một chuỗi bẫy: mở Boot trước khi Play, sao lưu `save.json*`, bật chạy nền mà không để lại dấu trong `ProjectSettings.asset`, chụp UI đúng 1920×1080, bấm nút bằng `EventSystem.RaycastAll` thay vì `simulate_pointer`. Bản này gom chúng thành code có test, và thêm một smoke test để biết "vòng chơi chính còn chạy" sau mỗi feature mà không cần ai ngồi bấm.

Thành công khi: (1) agent thử một vòng bằng vài lệnh `unity command eval` gọi `PlaytestTools`, không cần chép lại các mẹo; (2) `unity test --mode PlayMode` chạy hết vòng chơi và đỏ có lý do đọc được; (3) không lượt thử nào làm đổi save thật hay `ProjectSettings.asset`.

### Quyết định đã chốt với user (2026-11-03)

| Chủ đề | Quyết định |
|---|---|
| Save tạm | Thêm seam `GameBootstrap.SavePathOverride` ở `Boot` (mục 3.1). Không dùng cách sao lưu rồi chép lại save thật cho smoke test |
| Cách gọi | Hàm `public static` gọi được từ `unity command eval`, kèm vài mục menu `Tools/BunDauPeo/Playtest/…` cho user bấm tay |
| Điểm xuất phát smoke test | Game mới (save tạm trống, Boot cấp nhân vật khởi đầu). AUTO phải thắng màn 1 bằng đội đó; thua thì test đỏ, vì đó là lỗi cân bằng thật |
| Hướng thiết kế | Phương án A: `PlaytestKit` dùng chung, `PlaytestTools` ở Editor, assembly test PlayMode riêng |

## 2. Cấu trúc và asmdef

| Assembly | Nội dung | Nền tảng / tham chiếu |
|---|---|---|
| `Boot` (có sẵn) | `GameBootstrap.SavePathOverride` | không đổi asmdef |
| `PlaytestKit` (**mới**, `Assets/_Project/Playtest/`) | `UiDriver`: tìm, bấm, chờ nút; `ScreenCapture1080`: chụp 1920×1080 (chỉ dùng API runtime, để smoke test PlayMode gọi được; `PlaytestTools.Capture1080` chỉ chuyển tiếp) | mọi nền tảng, `defineConstraints: ["UNITY_EDITOR \|\| DEVELOPMENT_BUILD"]` (cùng kiểu `DevOverlay`: bản phát hành không chứa nó); tham chiếu `UnityEngine.UI`, `Unity.InputSystem` |
| `Editor` (có sẵn) | `PlaytestTools`, `SaveBackup`, `BackgroundPlay` | thêm tham chiếu `PlaytestKit` |
| `PlayModeTests` (**mới**, `Assets/_Project/Tests/PlayMode/`) | `SmokeLoopTests` | mọi nền tảng, `defineConstraints: ["UNITY_INCLUDE_TESTS"]`; tham chiếu `Core`, `Characters`, `Combat`, `Boot`, `UI`, `PlaytestKit`, `UnityEngine.UI`, `Unity.InputSystem`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`; `nunit.framework.dll` |
| `Tests` (có sẵn, EditMode) | test cho seam, `SaveBackup`, `BackgroundPlay`, `UiDriver.Find…` | thêm tham chiếu `PlaytestKit` |

`PlaytestKit` không tham chiếu assembly game nào, nên không đụng luật phụ thuộc ở [structure.md](../../project/structure.md). `PlayModeTests` tách khỏi `Tests` vì Test Framework chỉ coi assembly **chỉ có Editor** là EditMode; muốn Play thật thì assembly không được giới hạn ở Editor.

## 3. Thành phần

### 3.1 Seam save ở `Boot`

```csharp
public static string SavePathOverride { get; set; }          // null = đường dẫn thật
public static string SavePath => SavePathOverride ?? Path.Combine(Application.persistentDataPath, "save.json");
```

- Không đặt lại trong `ResetStatics` (tắt domain reload thì giá trị sống qua lần Play); người đặt có trách nhiệm trả `null`.
- `GameBootstrap.Awake` và `ResetSaveData` đã dùng `SavePath` nên đi theo. Session đã tạo giữ `SaveService` với đường dẫn lúc Awake, nên đổi override sau khi Boot chạy không làm save chuyển chỗ giữa chừng.

### 3.2 `UiDriver` (PlaytestKit)

- `FindButtons(string name)`: mọi `Button` **đang hiện** (`activeInHierarchy`) và `interactable`, khớp theo tên GameObject; nếu không có thì khớp theo chữ `Text` con (nút của `DevOverlay` chỉ có chữ).
- `Click(string name)` trả `ClickResult { Clicked, NotFound, Ambiguous, NotInteractable, Blocked }` kèm thông điệp:
  - nhiều nút khớp → `Ambiguous`, liệt kê đường dẫn, **không** bấm;
  - điểm bấm là tâm nút theo `RectTransformUtility.WorldToScreenPoint` với camera của Canvas (null khi Overlay);
  - `EventSystem.RaycastAll` tại điểm đó; hit rỗng → `Blocked` ("toạ độ sai"); hit đầu không thuộc nút (không phải nút hay con của nút) → `Blocked` kèm tên vật che;
  - đúng nút → `ExecuteEvents.Execute(..., pointerClickHandler)`. Không dùng `onClick.Invoke()`, vì nó bấm được cả nút bị che.
- `WaitForButton(name, timeoutSeconds)` là coroutine (`IEnumerator`) dùng giờ thật (`Time.realtimeSinceStartup`), không phụ thuộc `timeScale`. Hết giờ thì ghi lỗi gồm tên nút cần và **danh sách nút đang hiện** (tên + chữ), để test đỏ tự nói đang kẹt ở màn nào.

### 3.3 `PlaytestTools` (Editor)

Tất cả là `public static`, trả `string` mô tả kết quả (cũng `Debug.Log`), không ném lỗi cho việc người dùng có thể làm sai (scene đang sửa dở, đã có bản sao lưu…): trả thông điệp bắt đầu bằng `ERROR:`.

| Hàm | Việc | Menu |
|---|---|---|
| `OpenBoot()` | mở `Boot.unity` (`ProjectSetup.BootScenePath`) ở chế độ Single. Scene đang sửa dở (`isDirty`) thì từ chối, không hiện hộp thoại. Đang Play thì từ chối | có |
| `BackupSave()` | chép mọi `save.json*` vào `<thư mục save>/playtest-backup/`. Nếu thư mục sao lưu đã có thì **từ chối** (ghi đè sẽ mất save gốc). Nếu chưa có save nào, ghi tệp dấu `NO_SAVE` | có |
| `RestoreSave()` | xoá mọi `save.json*` hiện tại rồi chép lại từ bản sao lưu (hoặc chỉ xoá nếu có dấu `NO_SAVE`), sau đó xoá thư mục sao lưu. Từ chối khi đang Play (`OnApplicationQuit` sẽ ghi đè) và khi không có bản sao lưu | có, kèm hộp xác nhận |
| `EnableBackgroundPlay()` | ghi `PlayerSettings.runInBackground` gốc vào `SessionState` **một lần** (gọi lần hai không ghi đè), rồi đặt `true`. Tự trả về giá trị gốc khi về Edit Mode (`playModeStateChanged`) | không |
| `RestoreBackgroundPlay()` | trả về giá trị gốc ngay, xoá dấu trong `SessionState` | không |
| `Capture1080(string fileName = null)` | chụp màn đang Play ở 1920×1080 (mục 3.4), trả đường dẫn PNG | có |
| `ClickButton(string name)` | gọi `UiDriver.Click`, trả `ClickResult` dạng chữ | không |

Thư mục save lấy từ `Path.GetDirectoryName(GameBootstrap.SavePath)`, nên tôn trọng `SavePathOverride`. Các hàm sao lưu nhận thêm tham số thư mục (mặc định lấy từ `SavePath`) để test dùng thư mục tạm.

`EnableBackgroundPlay` dùng `SessionState` vì Vào Play nạp lại domain và làm mất mọi static (L-007 ghi nhận `runInBackground` có thể lọt vào `ProjectSettings.asset`). Hoàn tác tự động là lớp bảo vệ; kiểm `git status` ở cuối vẫn là bước của kế hoạch.

### 3.4 `Capture1080` (logic ở `PlaytestKit.ScreenCapture1080`)

Làm đúng công thức đã chứng minh ở L-011, trong **một** lần gọi:

1. Chỉ chạy khi `Application.isPlaying`; ngược lại trả `ERROR:`.
2. Tạo camera tạm (không lưu vào scene, `HideFlags.HideAndDontSave`) với `targetTexture` 1920×1080; sao chép nền từ `Camera.main` nếu có.
3. Mọi `Canvas` gốc đang ở `ScreenSpaceOverlay` đổi sang `ScreenSpaceCamera` với camera tạm; nhớ danh sách để trả lại.
4. `Canvas.ForceUpdateCanvases()`, `Render()`, `ReadPixels`, `EncodeToPNG`.
5. **`finally`**: trả mọi Canvas về `ScreenSpaceOverlay`, huỷ camera và texture tạm.
6. Ghi vào `<cha của thư mục project>/bdp-shots/` (ví dụ `D:/Unity/bdp-shots/`, ngoài repo nên không sinh `.meta`, không bị commit); tên mặc định `shot-yyyyMMdd-HHmmss.png`.

Màn có bố cục cố định theo chiều cao đã dùng `CanvasScaler` khớp theo chiều cao (luật của Combat), nên 1920×1080 là khung chuẩn của mọi màn.

### 3.5 Smoke test PlayMode (`SmokeLoopTests`)

Một `[UnityTest]` chạy một đường dài, mỗi bước có timeout giờ thật, thất bại nói rõ bước nào:

```
SetUp:  tạo thư mục tạm; ghi băm file save thật (nếu có); GameBootstrap.SavePathOverride = <tạm>/save.json
1  nạp Boot              -> Restaurant hiện; GameBootstrap.Session là game mới (IsNewGame), ví bằng StartingCurrency của NewGameConfig
2  bấm "Màn chơi"        -> scene StageSelect; thể lực = Max
3  bấm Node0 -> "ChallengeButton" -> TeamSelect; bấm TeamCard0..2 (ba nhân vật khởi đầu)
4  bấm "StartButton"; xác nhận đội dưới 5 người -> scene Battle
5  bấm "SpeedButton" (x2) và "AutoButton"
6  chờ nút "ConfirmButton" (màn kết quả) tối đa 60 giây thật
7  khẳng định: thắng; thể lực giảm đúng chi phí màn; Wallet tăng đúng thưởng lần đầu của màn 1;
   ClearedStages chứa màn 1 và màn 2 ở trạng thái Available; Capture1080 ra PNG đúng 1920×1080
8  bấm "ConfirmButton" -> StageSelect
9  new SaveService(<tạm>/save.json).Load() -> JSON bằng JSON của GameSession.Save đang sống
TearDown: SavePathOverride = null; huỷ GameBootstrap (DontDestroyOnLoad); ServiceLocator.Clear();
          xoá thư mục tạm; băm save thật phải KHÔNG đổi
```

- Thưởng và chi phí lấy từ `StageCatalog`/`CombatConfig` đã nạp, không viết cứng số trong test.
- Tên nút thật (xác nhận đội thiếu người, `TeamCard{n}`, nút của `DevOverlay` theo chữ) tra trong builder ở Task 1 của kế hoạch, không đoán.
- Giới hạn: bước 9 đọc lại từ đĩa vào đối tượng mới, chưa phải thoát hẳn Play rồi mở lại. Thoát và mở lại vẫn là bước của người thử (`TODO(user)` đã có ở roadmap cho playtest 3).

## 4. Test

| Nhóm | Chế độ | Phải bắt được lỗi nào |
|---|---|---|
| Seam `SavePathOverride` | EditMode | override `null` → đường dẫn thật; có giá trị → trả đúng giá trị; trả lại `null` thì về đường dẫn thật |
| `SaveBackup` | EditMode, thư mục tạm | backup rồi restore trả đúng nội dung và xoá `.bak/.corrupt` lạ; **backup lần hai bị từ chối và không đổi bản đầu**; thiếu save lúc backup → restore xoá save của lượt thử; restore khi không có bản sao lưu → `ERROR:`; chép lỗi giữa chừng không để thư mục sao lưu nửa vời |
| `BackgroundPlay` | EditMode | bật rồi hoàn tác trả đúng giá trị gốc cả khi gốc là `true` và `false`; **gọi bật hai lần vẫn hoàn tác về giá trị gốc ban đầu**; hoàn tác khi chưa bật không đổi gì |
| `UiDriver.FindButtons` | EditMode, GameObject dựng bằng code | khớp theo tên rồi theo chữ; bỏ nút ẩn và nút `interactable = false`; nhiều nút khớp → `Ambiguous`; không khớp → `NotFound` |
| `UiDriver.Click` | được phủ bởi smoke test | `Blocked` và đường bấm thật chạy ở bước 2–8 |
| `ScreenCapture1080` | PlayMode | PNG đúng 1920×1080; Canvas Overlay được trả về Overlay sau khi chụp (cả khi chụp ném lỗi) |
| Smoke loop | PlayMode | cả vòng ở mục 3.5; thêm ca chứng minh test **đỏ có lý do** khi một bước kẹt không tự động hoá được (dùng timeout ngắn trên nút không tồn tại, kiểm thông điệp có danh sách nút) |

Theo [code-style](../../guides/code-style.md#test): không `[TestCase]` (L-012); mỗi test tự hỏi "cài đặt sai nào vẫn qua" (L-006).

## 5. Gói triển khai

Hai phiên 🔨 theo [kế hoạch](../plans/2026-11-03-playtest-tools.md): phiên 1 là nền độc lập (seam, sao lưu save, chạy nền, `UiDriver`), phiên 2 là nối (`PlaytestTools`, `Capture1080`, smoke test, docs). Chi tiết task và chỗ nào chạm Unity/`ProjectSettings` ở kế hoạch.

## 6. Ngoài phạm vi

- Build Android và mọi thứ của nó (mục riêng ở roadmap). `PlaytestKit` chỉ **tình cờ** không vào bản phát hành nhờ `defineConstraints`; không thêm gì cho Development Build Android.
- Thoát hẳn Play rồi mở lại để kiểm save (người thử tự làm).
- Smoke test cho Gacha, Restaurant, AFK, Reset Save Data (chỉ vòng Combat theo yêu cầu roadmap).
- Tự động hoá chọn skill/chiêu trong trận, kiểm hoạt hình hay bố cục điểm ảnh (chụp ảnh chỉ để người xem).
- Chạy CI (D-006: chưa dùng).
- Thêm package; đổi `ProjectSettings` (công cụ chỉ mượn `runInBackground` rồi trả).

## 7. Rủi ro và cách kiểm sớm (Task 1 của kế hoạch)

| Rủi ro | Cách kiểm |
|---|---|
| `unity command eval` có thấy kiểu trong assembly `BunDauPeo.Editor` không | gọi `PlaytestTools` rỗng từ `eval` với Editor mở. Nếu không thấy thì dùng `[MenuItem]` + `unity command menu` (hoặc lệnh tương đương `unity commands --grep menu`) và sửa spec trước khi làm tiếp |
| Assembly có `defineConstraints` `UNITY_EDITOR \|\| DEVELOPMENT_BUILD` được cả `Editor` lẫn `PlayModeTests` tham chiếu | tạo asmdef rỗng + một lớp, compile, chạy test mẫu |
| `unity test --mode PlayMode` ở batch mode có dựng hình để chụp ảnh được không | chạy một PlayMode test chỉ gọi `ScreenCapture1080` (Task 7 của kế hoạch); nếu cần cờ `-nographics` bị tắt thì ghi vào kế hoạch |
| Domain reload sau khi `unity test` vào Play làm mất `SavePathOverride` | `SavePathOverride` đặt **trong** test sau khi Play đã chạy, trước khi nạp Boot |
| Test ghi nhầm vào save thật | `SavePathOverride` đặt trước khi nạp Boot; băm save thật so ở TearDown |
| Lượt thử tay bỏ quên `RestoreSave` | `RestoreSave` từ chối khi chưa có bản sao lưu; `BackupSave` từ chối khi đã có (thông điệp nhắc chạy Restore trước) |

## 8. Câu hỏi còn mở

Không có.

## 9. Việc cần người khác làm (agent không đụng)

- User duyệt bản viết này và kế hoạch.
- User chạy thử tay các mục menu `Tools/BunDauPeo/Playtest/…` lần đầu sau Task 6 (một lần, vài phút) và xác nhận chúng dùng được.
