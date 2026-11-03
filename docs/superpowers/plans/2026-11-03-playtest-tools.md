# Công cụ thử tự động: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (mặc định, native) hoặc superpowers:subagent-driven-development để làm kế hoạch này từng task (cách thực thi do session điều phối chọn, xem [agent-routing.md](../../guides/agent-routing.md)). Các bước dùng checkbox (`- [ ]`).

**Goal:** Gom các bẫy playtest ở [unity-workflow.md](../../guides/unity-workflow.md) thành code có test (`PlaytestTools`, `PlaytestKit`), và một smoke test PlayMode chạy trọn vòng Boot → bản đồ → đội → trận AUTO → thưởng → nạp lại save, dùng save tạm.

**Architecture:** `GameBootstrap.SavePathOverride` (seam duy nhất trong mã game) cho test dùng save tạm. Assembly runtime nhỏ `PlaytestKit` (`UNITY_EDITOR || DEVELOPMENT_BUILD`) giữ `UiDriver` (tìm, bấm, chờ nút) và `ScreenCapture1080`. Assembly `Editor` thêm `PlaytestTools` (hàm static + menu), `SaveBackup`, `BackgroundPlay`. Assembly test PlayMode mới `PlayModeTests` chứa smoke test.

**Tech Stack:** Unity 6000.3.15f1, C#, uGUI, Input System 1.19.0, Test Framework 1.6.0, CLI `unity` 1.0.0-beta.15. Không thêm package.

**Spec:** [docs/superpowers/specs/2026-11-03-playtest-tools-design.md](../specs/2026-11-03-playtest-tools-design.md) (thiết kế duyệt trong kênh 📐 2026-11-03; user duyệt bản viết).

## Global Constraints

- Không sửa/xoá tay `.meta`; không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`. Không sửa tay `.unity`, `.asset`, `.prefab`; kế hoạch này **không đụng scene, prefab hay asset**. `ProjectSettings` chỉ bị mượn lúc chạy (`runInBackground`) và phải trả về nguyên (kiểm `git status`, Task 4).
- **Git:** nhánh `feature/playtest-tools` cắt từ `origin/main` mới nhất (sau khi nhánh `docs/playtest-tools-spec` đã merge, hoặc cắt từ chính nhánh docs đó nếu chưa merge). Commit **và push** sau mỗi task, chỉ khi test của task xanh; `git add` theo đường dẫn cụ thể (cả thư mục để không sót `.meta`, L-005), không `git add .`/`-A`; `git status --short` trước mỗi commit. **Không bao giờ add:** `Assets/_Recovery/`, `Assets/_Recovery.meta`, `ProjectSettings/Packages/com.unity.pipeline/`. Không force push, không mở PR khi user chưa cho. Commit nhiều dòng: ghi message ra file rồi `git commit -F` (L-017).
- Code: `PascalCase` cho class/method/property, field private `_camelCase`, comment chỉ nói vì sao; tên code tiếng Anh, docs và chữ trong thông điệp công cụ tiếng Việt. Namespace `BunDauPeo.Playtest` cho `PlaytestKit`, `BunDauPeo.EditorTools` cho code Editor.
- `PlaytestKit` **không tham chiếu assembly game nào** (chỉ `UnityEngine.UI`, `Unity.InputSystem`); `defineConstraints: ["UNITY_EDITOR || DEVELOPMENT_BUILD"]`. Code Editor dùng `UnityEditor` nằm trong assembly `Editor` (L-001). Không `PlayerPrefs`, không `Resources`, không `Time.timeScale`.
- Hàm công cụ trả `string` và không ném lỗi cho việc người dùng làm sai được: thông điệp bắt đầu bằng `ERROR:` (spec 3.3). Lỗi IO thật (đĩa, khoá file) không được nuốt thành "thành công" (L-003).
- Test: không `[TestCase]` (L-012); mỗi test tự hỏi "cài đặt sai nào vẫn qua" (L-006); thời gian chờ dùng giờ thật. Test đụng `PlayerSettings` hoặc thư mục save luôn khôi phục trong `finally`/TearDown.
- Sau mỗi task: Console không có lỗi compile hoặc warning mới từ code dự án.

### Lệnh dùng chung

Kiểm `unity status` **thành một lệnh riêng** trước mọi lệnh batch (L-020). Lệnh batch cần Editor đóng; lệnh `unity command …` cần Editor mở.

```bash
unity test . --mode EditMode --output ../bdp-tests.xml --timeout 600 -- -logFile ../bdp-tests.log
unity test . --mode PlayMode --output ../bdp-playtests.xml --timeout 900 -- -logFile ../bdp-playtests.log
```

Exit `0` đạt, `8` có test đỏ, `6` lỗi khác (kể cả compile). Đọc `../bdp-tests.xml` / `../bdp-playtests.xml` (`failed="0"`, `total="N"`) và log, không dựa mỗi mã thoát. Số kỳ vọng: EditMode **692 + 23 = 715** khi hết kế hoạch; PlayMode **4**. Ghi tổng cộng dồn sau mỗi task vào checkbox.

## Review Focus

Năm điều spec không nói hết mà dễ làm hỏng công cụ nhất; mỗi dòng có test ở task ghi trong ngoặc.

1. `BackupSave` gọi hai lần: lần hai **không** được ghi đè bản sao lưu đầu bằng save đã bị lượt thử làm bẩn (T3).
2. Lúc backup chưa có save nào rồi lượt thử tạo ra save: `RestoreSave` phải xoá save đó, không để lại (T3).
3. `EnableBackgroundPlay` gọi hai lần: giá trị gốc vẫn là giá trị trước lần gọi đầu, kể cả khi gốc là `true` (T4).
4. `Click` bấm nút bị che bởi popup hay nền chặn: phải `Blocked` kèm tên vật che, không bấm xuyên (T5 `Find` + smoke test phủ đường bấm thật, T8).
5. Smoke test chạy xong (xanh hay đỏ) mà `SavePathOverride` còn khác `null` hay save thật đổi: phải bị bắt bằng băm save thật ở TearDown (T8).

## Điều hướng agent

### Chia session

| Phiên | Task | Ghi chú |
|---|---|---|
| 1 | 1–5 | nền. T1 trước (tạo asmdef), rồi T2, T3, T4 độc lập, rồi T5 |
| 2 | 6–9 | nối, chạm Play và docs; dừng playtest sau T8 (user xem ảnh chụp và chạy thử menu) |

Task 1–5 là 5 task nhỏ, đủ cho một phiên. Phiên 2 mở khi phiên 1 báo xong và 🎯 đã kiểm.

### Task nào chạm gì, task nào độc lập

| Task | Chạm Unity/Editor | Chạm `ProjectSettings` | Độc lập (subagent song song được) |
|---|---|---|---|
| 1 Kiểm hạ tầng | mở Editor để thử `eval`; tạo 2 asmdef | không | không (làm trước) |
| 2 Seam Boot | chỉ chạy test | không | **có** (sau T1) |
| 3 `SaveBackup` | chỉ chạy test | không | **có** (sau T1) |
| 4 `BackgroundPlay` | chỉ chạy test | **có lúc chạy** (đọc/ghi `runInBackground`, trả lại) | **có** viết code; kênh 🔨 tự kiểm `git status` |
| 5 `UiDriver` | chỉ chạy test | không | không (sau T1; sửa `PlaytestKit`) |
| 6 `PlaytestTools` | Editor mở, `eval`, menu | gián tiếp (T4) | không |
| 7 `ScreenCapture1080` | vào Play, render | không | không |
| 8 Smoke test | PlayMode batch | không | không |
| 9 Docs | không | không | **có** (chỉ docs) |

Subagent (nếu dùng cho T2–T4) chỉ viết code và test, **không chạy Unity, không commit**; kênh 🔨 đọc diff, chạy `unity test` một lần cho cả đợt (luật ở [studio](../../studio/README.md#subagent-song-song-trong-kênh-)). Phần save không giao cho Haiku.

### Bàn giao

Mỗi task xong: tick checkbox, commit + push, cập nhật dòng "Đang làm" trong [roadmap.md](../../roadmap.md). Kết phiên bằng khối "Báo cáo cuối kênh" của skill worker.

## File Structure

```
Assets/_Project/
  Boot/Scripts/GameBootstrap.cs                    sửa: SavePathOverride (T2)
  Playtest/                                        mới
    BunDauPeo.Playtest.asmdef                      T1  (assembly BunDauPeo.PlaytestKit, namespace BunDauPeo.Playtest)
    UiDriver.cs                                    T1 (khung) → T5
    ScreenCapture1080.cs                           T7
  Editor/
    BunDauPeo.Editor.asmdef                        sửa: + BunDauPeo.PlaytestKit (T1)
    PlaytestTools.cs                               T1 (Ping) → T6
    SaveBackup.cs                                  T3
    BackgroundPlay.cs                              T4
  Tests/
    BunDauPeo.Tests.asmdef                         sửa: + BunDauPeo.PlaytestKit (T1)
    GameBootstrapSavePathTests.cs                  T2
    SaveBackupTests.cs                             T3
    BackgroundPlayTests.cs                         T4
    UiDriverTests.cs                               T5
    PlaytestToolsTests.cs                          T6
    PlayMode/
      BunDauPeo.PlayModeTests.asmdef               T1
      PlaytestKitPlayTests.cs                      T1 (khung) → T7 (capture), T8 (WaitForButton timeout)
      SmokeLoopTests.cs                            T8
docs/ : unity-workflow.md, structure.md, decisions.md (D-009), roadmap.md  T9
```

## Giai đoạn A: nền (Phiên 1)

### Task 1: Kiểm hạ tầng: asmdef mới và gọi `PlaytestTools` từ `eval`

**Files:**
- Create: `Assets/_Project/Playtest/BunDauPeo.Playtest.asmdef`, `Assets/_Project/Playtest/UiDriver.cs` (khung rỗng), `Assets/_Project/Editor/PlaytestTools.cs` (chỉ `Ping`), `Assets/_Project/Tests/PlayMode/BunDauPeo.PlayModeTests.asmdef`, `Assets/_Project/Tests/PlayMode/PlaytestKitPlayTests.cs`
- Modify: `BunDauPeo.Editor.asmdef`, `BunDauPeo.Tests.asmdef` (thêm tham chiếu `BunDauPeo.PlaytestKit`)

**Interfaces:**
- Produces: assembly `BunDauPeo.PlaytestKit` (namespace `BunDauPeo.Playtest`, file asmdef tên `BunDauPeo.Playtest.asmdef`, `name: "BunDauPeo.PlaytestKit"`); `public static class UiDriver` (khung); `public static string PlaytestTools.Ping()` trả `"pong"`; assembly `BunDauPeo.PlayModeTests`.
- Consumes: không.

Task này chứng minh ba giả định của spec mục 7 **trước khi** xây thật. Một giả định sai thì dừng và báo 🎯 (đổi spec), không tự đổi hướng.

- [x] **Step 1: Tạo `PlaytestKit`.** asmdef: `name: "BunDauPeo.PlaytestKit"`, `rootNamespace: "BunDauPeo.Playtest"`, `references: ["UnityEngine.UI", "Unity.InputSystem"]`, `includePlatforms: []`, `defineConstraints: ["UNITY_EDITOR || DEVELOPMENT_BUILD"]`, `autoReferenced: false`. `UiDriver` là `public static class` rỗng có một hằng để biên dịch.
- [x] **Step 2: Tạo `PlayModeTests`.** asmdef: `name: "BunDauPeo.PlayModeTests"`, `rootNamespace: "BunDauPeo.PlayModeTests"`, `references` gồm `BunDauPeo.Core`, `.Characters`, `.Combat`, `.Boot`, `.UI`, `.PlaytestKit`, `UnityEngine.UI`, `Unity.InputSystem`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`; `overrideReferences: true`, `precompiledReferences: ["nunit.framework.dll"]`, `includePlatforms: []`, `defineConstraints: ["UNITY_INCLUDE_TESTS"]`, `autoReferenced: false`. Test mẫu `PlaytestKitPlayTests.PlayModeAssembly_RunsAndSeesPlaytestKit`: `[UnityTest]` chờ một khung rồi `Assert.IsTrue(Application.isPlaying)` và nhắc tới `typeof(UiDriver)`.
- [x] **Step 3: Thêm tham chiếu** `BunDauPeo.PlaytestKit` vào hai asmdef có sẵn (`Editor`, `Tests`). Thêm `PlaytestTools.Ping()` ở `Editor`.
- [x] **Step 4: Chạy EditMode** (Editor đóng): vẫn **692** xanh, không lỗi compile. Chạy PlayMode: `PlayModeAssembly_RunsAndSeesPlaytestKit` xanh (1/1). Đỏ vì asmdef không ghép được → ghi nguyên văn lỗi, dừng, báo 🎯. **Đã chạy: EditMode 692/692, PlayMode 1/1 (cần guard `-runTests` ở `BootScenePlayMode`, L-023).**
- [x] **Step 5: Kiểm `eval`** (Editor mở: `unity open .`, rồi `unity recompile`): `unity command eval 'Debug.Log(BunDauPeo.EditorTools.PlaytestTools.Ping());'` rồi `unity command console get --level error`/đọc log: phải thấy `pong`. Kiểu không thấy được → dùng `[MenuItem]` + `unity commands --grep menu` để gọi, ghi cách gọi đã chạy được vào spec mục 3.3 và báo 🎯 trước khi sang Task 6. Đóng Editor sau khi xong (`unity close .`, rồi `unity status` lặp tới khi hết dòng Editor). **Đã chạy: `eval` gọi `Debug.Log(Ping())` thấy `pong` ở console; `return PlaytestTools.Ping();` với `--result-only` cũng trả `pong`.**
- [x] **Step 6: Ghi tên nút thật** đã tra ở builder vào mục "Tên nút" bên dưới (không đoán thêm) và commit.

**Tên nút thật (đã tra trong builder, ngày 2026-11-03):** nút `DevOverlay` khớp theo chữ: `"Màn chơi"`. Bản đồ: `Node{i}` (i từ 0), popup màn `ChallengeButton`. Chọn đội: `TeamCard{i}` (Button nằm trên chính GameObject thẻ; i = thứ tự sở hữu, 0–11 mỗi trang), `StartButton`, xác nhận đội dưới 5 người `SmallConfirmYesButton`. Trận: `AutoButton`, `SpeedButton` (x1 → x2), kết quả `ConfirmButton` (thắng), `RetryButton`/`BackButton` (thua). Task 8 phải kiểm lại nếu builder đổi.

- [x] **Step 7: Commit**

```bash
git add Assets/_Project/Playtest Assets/_Project/Editor/PlaytestTools.cs Assets/_Project/Editor/PlaytestTools.cs.meta Assets/_Project/Editor/BunDauPeo.Editor.asmdef Assets/_Project/Tests/BunDauPeo.Tests.asmdef Assets/_Project/Tests/PlayMode docs/superpowers/plans/2026-11-03-playtest-tools.md
git commit -F <file>   # "feat(playtest): PlaytestKit and PlayModeTests assemblies, Ping check"
git push -u origin feature/playtest-tools
```

### Task 2: Seam `GameBootstrap.SavePathOverride`

**Files:**
- Modify: `Assets/_Project/Boot/Scripts/GameBootstrap.cs`
- Test: `Assets/_Project/Tests/GameBootstrapSavePathTests.cs`

**Interfaces:**
- Produces: `public static string SavePathOverride { get; set; }`; `SavePath` trả `SavePathOverride ?? Path.Combine(Application.persistentDataPath, "save.json")`.

- [ ] **Step 1: Viết test (3 test).** Mỗi test đặt override trong `try` và trả `null` trong `finally`:
  `SavePath_NoOverride_IsPersistentDataPathSaveJson` (so với `Path.Combine(Application.persistentDataPath, "save.json")`), `SavePath_WithOverride_ReturnsOverride` (override `Path.Combine(Path.GetTempPath(), "x", "save.json")`), `SavePath_OverrideSetBackToNull_IsRealPathAgain`.
- [ ] **Step 2: Chạy, thấy đỏ** (`error CS0117`: chưa có thành viên).
- [ ] **Step 3: Thêm property** như Interfaces. Không đặt lại trong `ResetStatics`. Một dòng comment nói vì sao (người đặt phải trả `null`; domain reload tắt thì giá trị sống tiếp).
- [ ] **Step 4: Chạy EditMode:** 695 xanh.
- [ ] **Step 5: Commit** `git add Assets/_Project/Boot Assets/_Project/Tests/GameBootstrapSavePathTests.cs Assets/_Project/Tests/GameBootstrapSavePathTests.cs.meta` — "feat(boot): SavePathOverride seam for temp saves".

### Task 3: `SaveBackup`

**Files:**
- Create: `Assets/_Project/Editor/SaveBackup.cs`
- Test: `Assets/_Project/Tests/SaveBackupTests.cs`

**Interfaces:**
- Produces: `public static class SaveBackup { public const string FolderName = "playtest-backup"; public const string NoSaveMarker = "NO_SAVE"; public static string Backup(string saveDirectory); public static string Restore(string saveDirectory); }`. Thông điệp bắt đầu `ERROR:` khi từ chối. "File save" = `save.json` và `save.json.*` ở thư mục `saveDirectory` (không đệ quy; **không** tính thư mục `FolderName`).

- [ ] **Step 1: Viết test (6 test, thư mục tạm riêng mỗi test, dọn ở `finally`):**
  1. `BackupThenRestore_BringsBackOriginalFiles`: tạo `save.json`, `save.json.bak`, `save.json.corrupt` với nội dung khác nhau; `Backup`; ghi đè `save.json`, thêm `save.json.tmp`; `Restore`; ba file gốc về đúng nội dung, `save.json.tmp` biến mất, thư mục sao lưu biến mất.
  2. `Backup_Twice_RefusesAndKeepsFirstBackup`: lần hai trả `ERROR:` kèm nhắc chạy Restore; nội dung bản sao lưu vẫn là của lần đầu (đã đổi `save.json` giữa hai lần gọi).
  3. `Backup_NoSaveYet_ThenRestore_DeletesSaveCreatedMeanwhile`: không có file nào lúc Backup (thư mục có thể chưa tồn tại); tạo `save.json`; `Restore` xoá nó.
  4. `Restore_WithoutBackup_ReturnsError_AndTouchesNothing`.
  5. `Restore_ReplacesFilesPresentNow_NotMerges`: save hiện tại có `save.json.bak` mà bản sao lưu không có → sau Restore `.bak` không còn.
  6. `Backup_FileLocked_ReturnsError_AndLeavesNoBackupFolder`: mở `save.json` bằng `FileStream` với `FileShare.None`, `Backup` trả `ERROR:`, không còn thư mục `FolderName` lẫn thư mục tạm.
- [ ] **Step 2: Chạy, thấy đỏ** (chưa có lớp).
- [ ] **Step 3: Implement.** Backup chép vào thư mục tạm `FolderName + ".tmp"` rồi `Directory.Move` thành `FolderName` (nguyên tử); lỗi thì xoá thư mục tạm và trả `ERROR:`. Restore: chép lại **trước khi** xoá thư mục sao lưu, chỉ xoá thư mục sao lưu khi chép xong. Lỗi `IOException`/`UnauthorizedAccessException` bắt ở biên hàm và trả `ERROR:` kèm `Message` (L-003: không nuốt thành thành công).
- [ ] **Step 4: Chạy EditMode:** 701 xanh.
- [ ] **Step 5: Commit** — "feat(playtest): SaveBackup (backup and restore save files)".

### Task 4: `BackgroundPlay`

**Files:**
- Create: `Assets/_Project/Editor/BackgroundPlay.cs`
- Test: `Assets/_Project/Tests/BackgroundPlayTests.cs`

**Interfaces:**
- Produces: `public static class BackgroundPlay { public static string Enable(); public static string Restore(); public static void HandlePlayModeState(PlayModeStateChange state); }`. Khoá `SessionState` `"BunDauPeo.Playtest.RunInBackgroundOriginal"` (`-1` = chưa bật, `0`/`1` = giá trị gốc). `[InitializeOnLoadMethod]` đăng ký `EditorApplication.playModeStateChanged` → `HandlePlayModeState`; `EnteredEditMode` gọi `Restore()`.

- [ ] **Step 1: Viết test (5 test).** Mỗi test lưu `PlayerSettings.runInBackground` và khoá `SessionState` thật lúc đầu và khôi phục trong `finally`:
  1. `EnableThenRestore_OriginalFalse_BackToFalse` (giữa chừng `runInBackground == true`).
  2. `EnableThenRestore_OriginalTrue_BackToTrue`.
  3. `Enable_Twice_RestoresValueBeforeFirstCall` (gốc `false`; sau `Enable` hai lần rồi `Restore` phải `false`, không `true`).
  4. `Restore_WithoutEnable_ChangesNothing` (đặt `runInBackground = true` bằng tay; `Restore` không đổi).
  5. `HandlePlayModeState_EnteredEditMode_Restores` (và `EnteredPlayMode` thì **không** hoàn tác).
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Implement** theo Interfaces. Không gọi `AssetDatabase.SaveAssets`.
- [ ] **Step 4: Chạy EditMode:** 706 xanh. Rồi `git status --short`: **`ProjectSettings/ProjectSettings.asset` không được xuất hiện** (L-007). Có xuất hiện thì `git diff` xem dòng nào đổi, sửa code cho trả nguyên, không `checkout` che lỗi.
- [ ] **Step 5: Commit** — "feat(playtest): BackgroundPlay with SessionState restore".

### Task 5: `UiDriver`

**Files:**
- Modify: `Assets/_Project/Playtest/UiDriver.cs`
- Test: `Assets/_Project/Tests/UiDriverTests.cs`

**Interfaces:**
- Produces (namespace `BunDauPeo.Playtest`):
  ```csharp
  public enum ClickStatus { Clicked, NotFound, Ambiguous, NotInteractable, Blocked }
  public readonly struct ClickResult { public ClickStatus Status { get; } public string Message { get; } }
  public static class UiDriver {
      public static IReadOnlyList<Button> FindButtons(string name);     // hiện + interactable; tên GameObject, không có thì chữ Text con
      public static ClickResult Click(string name);
      public static IEnumerator WaitForButton(string name, float timeoutSeconds); // ném System.TimeoutException khi hết giờ
      public static string DescribeVisibleButtons();                     // "tên (chữ), …" mọi nút đang hiện
  }
  ```

- [ ] **Step 1: Viết `UiDriverTests` (6 test, GameObject dựng bằng code, `DestroyImmediate` ở `finally`):** `FindButtons_ByGameObjectName`; `FindButtons_NoNameMatch_FallsBackToChildTextLabel`; `FindButtons_IgnoresInactiveAndNonInteractable`; `FindButtons_NameMatchWinsOverLabelMatch` (nút A tên "X", nút B chữ "X" → chỉ A); `Click_TwoMatches_ReturnsAmbiguousAndClicksNeither` (kèm đường dẫn cả hai trong `Message`, cả hai `onClick` chưa được gọi); `Click_NoMatch_ReturnsNotFound` (và `NotInteractable` khi chỉ có nút bị tắt: gộp vào cùng method với hai `Assert`).
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Implement.** `Click` theo spec 3.2: điểm bấm là tâm nút (`RectTransformUtility.WorldToScreenPoint(canvas.worldCamera nếu ScreenSpaceCamera, null nếu Overlay, …)`), `EventSystem.current.RaycastAll`, hit rỗng → `Blocked` ("toạ độ sai"), hit đầu không thuộc nút (không phải nút hay con của nút) → `Blocked` kèm tên vật che, đúng → `ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler)`. `WaitForButton` dùng `Time.realtimeSinceStartup`; thông điệp `TimeoutException` gồm tên cần và `DescribeVisibleButtons()`. Đường `Blocked`/`Clicked` cần EventSystem sống nên chỉ được phủ ở Task 8.
- [ ] **Step 4: Chạy EditMode:** 712 xanh (mốc cuối phiên 1: **712**).
- [ ] **Step 5: Commit** — "feat(playtest): UiDriver (find, click, wait for buttons)".

## Giai đoạn B: nối và kiểm chứng (Phiên 2)

### Task 6: `PlaytestTools` và menu

**Files:**
- Modify: `Assets/_Project/Editor/PlaytestTools.cs` (bỏ `Ping` khi hàm thật đã chạy được từ `eval`)
- Test: `Assets/_Project/Tests/PlaytestToolsTests.cs`

**Interfaces:**
- Consumes: `SaveBackup.Backup/Restore(string)` (T3), `BackgroundPlay.Enable/Restore()` (T4), `UiDriver.Click` (T5), `ScreenCapture1080.Capture` (T7: ở task này `Capture1080` gọi một hàm khung ném `NotImplementedException`, thay ở T7), `ProjectSetup.BootScenePath`, `GameBootstrap.SavePath`.
- Produces: `public static class PlaytestTools { string OpenBoot(); string BackupSave(); string RestoreSave(); string EnableBackgroundPlay(); string RestoreBackgroundPlay(); string Capture1080(string fileName = null); string ClickButton(string name); }`. Menu `Tools/BunDauPeo/Playtest/` cho `OpenBoot`, `BackupSave`, `RestoreSave` (hộp xác nhận `EditorUtility.DisplayDialog`, theo mẫu `ResetSaveData`), `Capture1080`.

- [ ] **Step 1: Viết `PlaytestToolsTests` (3 test):**
  1. `BackupAndRestore_FollowSavePathOverride`: đặt `GameBootstrap.SavePathOverride` vào thư mục tạm có `save.json`; `BackupSave` rồi `RestoreSave` chạy trên đúng thư mục đó (bản sao lưu nằm cạnh file tạm; không có gì xuất hiện ở thư mục save thật); trả `null` ở `finally`.
  2. `ClickButton_UnknownName_ReportsNotFound` (chuỗi trả về chứa `NotFound`).
  3. `Capture1080_NotPlaying_ReturnsError` (bắt đầu `ERROR:`).
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Implement** theo spec 3.3. `OpenBoot` từ chối khi `EditorApplication.isPlaying` hoặc `SceneManager.GetActiveScene().isDirty`, ngược lại `EditorSceneManager.OpenScene(ProjectSetup.BootScenePath, OpenSceneMode.Single)`. `RestoreSave` từ chối khi `EditorApplication.isPlaying`. Menu chỉ gọi hàm tương ứng rồi `Debug.Log` kết quả.
- [ ] **Step 4: Chạy EditMode:** 715 xanh. Rồi kiểm tay với Editor mở: `unity command eval 'Debug.Log(BunDauPeo.EditorTools.PlaytestTools.BackupSave());'` rồi `RestoreSave()`: đọc log, và `git status --short` không có file nào của `ProjectSettings/`.
- [ ] **Step 5: Commit** — "feat(playtest): PlaytestTools static facade and menu".

### Task 7: `ScreenCapture1080`

**Files:**
- Create: `Assets/_Project/Playtest/ScreenCapture1080.cs`
- Modify: `Assets/_Project/Editor/PlaytestTools.cs` (nối `Capture1080`), `Assets/_Project/Tests/PlayMode/PlaytestKitPlayTests.cs` (bỏ test mẫu của T1, thêm test thật)

**Interfaces:**
- Produces: `public static class ScreenCapture1080 { public static string Capture(string fileName = null, string directory = null); }` (trả đường dẫn PNG; `directory` mặc định `<cha của thư mục project>/bdp-shots`).

- [ ] **Step 1: Viết test PlayMode** `Capture_Overlay_WritesPng1920x1080_AndRestoresOverlay`: dựng Canvas Overlay có `Image` đỏ và một `Camera`, gọi `Capture(null, <thư mục tạm>)`; `Texture2D.LoadImage` thấy 1920×1080; `canvas.renderMode == RenderMode.ScreenSpaceOverlay` sau khi chụp. Thêm `Capture_RenderFails_StillRestoresOverlay` (camera bị `Destroy` trước nên chụp ném lỗi; Canvas vẫn trả về Overlay: bằng chứng của `finally`).
- [ ] **Step 2: Chạy PlayMode, thấy đỏ.**
- [ ] **Step 3: Implement** theo spec 3.4: camera tạm `HideAndDontSave` có `targetTexture` 1920×1080; mọi `Canvas` gốc đang `ScreenSpaceOverlay` đổi sang `ScreenSpaceCamera` rồi trả lại trong `finally`; `Canvas.ForceUpdateCanvases()`, `Render()`, `ReadPixels`, `EncodeToPNG`, ghi file; huỷ camera và texture tạm trong `finally`. Nối `PlaytestTools.Capture1080`.
- [ ] **Step 4: Chạy PlayMode:** 2 xanh (test mẫu T1 đã thay). Chạy EditMode: 715 xanh (`Capture1080_NotPlaying_ReturnsError` vẫn xanh).
- [ ] **Step 5: Commit** — "feat(playtest): ScreenCapture1080".

### Task 8: Smoke test PlayMode trọn vòng

**Files:**
- Create: `Assets/_Project/Tests/PlayMode/SmokeLoopTests.cs`
- Modify: `Assets/_Project/Tests/PlayMode/PlaytestKitPlayTests.cs` (thêm `WaitForButton_Timeout_ListsVisibleButtons`)

**Interfaces:**
- Consumes: `GameBootstrap.SavePathOverride/SavePath/Session` (T2), `UiDriver` (T5), `ScreenCapture1080` (T7), tên nút ở Task 1, `ServiceLocator.Get<CombatService>()` (có `Config.Stages`, `Stamina.Current`, `Progress.GetStatus(id)`), `Wallet.Get(CurrencyType)`, `SaveService.Load()`.

- [ ] **Step 1: Viết `WaitForButton_Timeout_ListsVisibleButtons`:** dựng một nút tên `Present`, gọi `WaitForButton("Missing", 0.5f)`; ném `TimeoutException` với `Message` chứa `Missing` và `Present`.
- [ ] **Step 2: Viết `SmokeLoopTests.FullLoop_NewGame_Stage1_AutoWin_RewardsAndSaveReload`** (`[UnityTest]`, timeout test 180 giây) đúng các bước 1–9 của spec mục 3.5. Mỗi bước bọc bằng hàm nhỏ `Step(string label)` ghi `label` vào thông điệp lỗi để đỏ nói rõ kẹt ở bước nào. Giá trị mong đợi lấy từ `CombatService.Config.Stages`: màn đầu (`Stages` phần tử 0): thể lực giảm `StaminaCost`; `Wallet` tăng `FirstClearReward` từng dòng; màn kế `GetStatus == Available`. Thể lực kiểm bằng `Stamina.Current` đọc một lần trước để chốt số thô (L-021). Chọn 3 thẻ `TeamCard0..2` theo số nhân vật khởi đầu thật (`Session.Roster.GetOwned().Count`, không viết cứng 3).
- [ ] **Step 3: SetUp/TearDown** đúng spec: thư mục tạm, băm `save.json*` thật trước (không có thì băm rỗng), `SavePathOverride` đặt **sau khi Play đã chạy và trước khi nạp Boot**; TearDown trả `null`, `Object.Destroy` đối tượng `GameBootstrap` (DontDestroyOnLoad), `ServiceLocator.Clear()`, xoá thư mục tạm, khẳng định băm save thật không đổi. TearDown chạy cả khi test đỏ.
- [ ] **Step 4: Chạy PlayMode** (Editor đóng, `unity status` riêng): 4 xanh. Đỏ vì thua màn 1 hay không dựng được hình → ghi vào báo cáo: thua là lỗi cân bằng thật (báo 🎯), không dựng được hình thì thử bỏ `-nographics` rồi ghi cờ chạy được vào [unity-workflow.md](../../guides/unity-workflow.md) ở Task 9. Ảnh chụp bước 7 được giữ ở `D:/Unity/bdp-shots/` để user xem.
- [ ] **Step 5: Chạy EditMode** (715 xanh) rồi **`git status --short`**: `ProjectSettings/` sạch, save thật không đổi. Commit — "test(playtest): PlayMode smoke test for Boot to reward loop".
- [ ] **Dừng playtest:** user xem ảnh chụp, thử tay `Tools/BunDauPeo/Playtest/*`, báo 🎯 trước khi sang Task 9.

### Task 9: Docs

**Files:**
- Modify: `docs/guides/unity-workflow.md`, `docs/project/structure.md`, `docs/decisions/decisions.md`, `docs/roadmap.md`, `docs/lessons/lesson.md` (chỉ nếu phát sinh bài học), `docs/superpowers/plans/2026-11-03-playtest-tools.md` (tick)

- [ ] **Step 1: `unity-workflow.md`.** Thêm mục "Công cụ thử tự động" (bảng hàm `PlaytestTools` → việc, cách gọi qua `eval`, lệnh `unity test --mode PlayMode`, cờ batch đã chạy được ở Task 8). Thu gọn các bullet bẫy đã có hàm thay (chạy nền, chụp 1920×1080, bấm nút, mở Boot, sao lưu save) thành một dòng trỏ tới hàm, **giữ lại một câu lý do** và số lesson.
- [ ] **Step 2: `structure.md`** thêm `Playtest/` (`BunDauPeo.PlaytestKit`), `Tests/PlayMode/` (`BunDauPeo.PlayModeTests`) và các lớp Editor mới; cập nhật số test (715 EditMode, 4 PlayMode).
- [ ] **Step 3: `decisions.md`** thêm **D-009** (seam `SavePathOverride`; hai asmdef mới; công cụ mượn `runInBackground` rồi trả; chỉ vòng Combat). Trạng thái Accepted.
- [ ] **Step 4: `roadmap.md`** chuyển mục "Công cụ thử tự động" sang "Đã xong" (số test, nhánh, link spec và kế hoạch); xoá dòng "Đang làm" của task này.
- [ ] **Step 5: Commit** — "docs: playtest tools in unity-workflow, structure, D-009, roadmap".

## Hoàn tất đợt

- [ ] Kênh 🔍 review toàn nhánh `feature/playtest-tools` so với `origin/main` (mức: **high**; phần save, `SaveBackup` và `SavePathOverride` ở mức **xhigh**), nâng lesson của nhánh thành luật theo [doc-maintenance](../../guides/doc-maintenance.md).
- [ ] User chạy thử tay một vòng playtest bằng `PlaytestTools` và báo kết quả; hỏi mở PR vào `main`.
