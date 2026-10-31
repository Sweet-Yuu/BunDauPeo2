# Gacha Reveal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sau mỗi lần bấm Quay 1 hoặc Quay 10 người chơi xem cutscene, từng nhân vật lóe sáng, SSR đứng cuối với cutscene không bỏ qua được, trang tổng có OK, popup quay tiếp, và lịch sử quay lưu trong save.

**Architecture:** Thứ tự và luật bỏ qua nằm trong class C# thuần `GachaRevealSequence` (test bằng EditMode). `GachaRevealView`, `GachaTotalView`, `GachaRollAgainPopup`, `GachaHistoryPopup` chỉ hiển thị và gọi sequence. Lịch sử là `PlayerSave.GachaHistory`, ghi trong cùng bước `persist()` của lượt quay. Cutscene là clip và prefab copy từ Project_04.

**Tech Stack:** Unity 6000.3.15f1, C#, `JsonUtility`, uGUI (`Image`, `Text`, `Button`), Animator, Input System 1.19.0, CLI `unity` 1.0.0-beta.15.

**Spec:** [docs/superpowers/specs/2026-10-31-gacha-reveal-design.md](../specs/2026-10-31-gacha-reveal-design.md). Nền: [spec Gacha](../specs/2026-10-31-gacha-design.md), [plan Gacha](2026-10-31-gacha.md) (đã xong, 148 test).

## Global Constraints

- Không sửa/xoá tay `.meta` ngoài việc copy nguyên văn ở Task 1; không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`. Không sửa tay `.unity`, `.asset`, `.prefab`: scene và asset do `ProjectSetup` hoặc `GachaSceneBuilder` sinh.
- Git: làm trên `feature/gacha`; commit **và push** sau mỗi task khi test xanh; `git add` theo đường dẫn cụ thể; không force push. Commit có dòng `Co-Authored-By: Claude Haiku 5.5 <noreply@anthropic.com>`.
- Không thêm package, không dùng `Resources`, không dùng TextMeshPro. Chữ dùng `Text` của uGUI; hình trong Canvas dùng `Image`, không `RawImage`.
- Hình trong Canvas chỉ dùng sprite có sẵn (`UI/Skin/UISprite.psd`, `Knob.psd`) hoặc ba PNG copy ở Task 1 (`Spark`, `Ring`, `Glow`). Agent không tự thêm ảnh.
- Mọi luật "bỏ qua" và "SSR không bỏ qua" được kiểm bằng test EditMode trên `GachaRevealSequence`, không kiểm chỉ bằng mắt.
- `CharacterDefinition.Stars` nằm trong khoảng `1..5` (tối đa 5★ theo vision).
- Thời gian trong cutscene dùng `unscaled time`.
- Field mới trong `PlayerSave` có giá trị khởi tạo; `Version` giữ 1; `ResetToDefaults` xoá lịch sử (D-005).
- Sau mỗi task: Console không có lỗi compile hoặc warning mới từ code dự án.

### Lệnh dùng chung

Chạy test (Editor phải đóng, hoặc dùng `unity command test run --mode EditMode` khi Editor đang mở):

```bash
unity test . --mode EditMode --output ../bdp-tests.xml --timeout 600 -- -logFile ../bdp-tests.log
```

Đọc `../bdp-tests.xml` (`failed="0"`, `total="N"`). Mốc đầu: `total="148"`.

## Review Focus

1. **Chạm trong cutscene SSR:** chạm và nút Bỏ qua không làm SSR bỏ qua cutscene của nó. Test ở Task 4.
2. **Nhiều SSR và Bỏ qua:** Bỏ qua đi tới SSR đầu tiên, mọi SSR sau vẫn chạy đủ cutscene theo thứ tự quay. Test ở Task 4.
3. **Quay tiếp khi thiếu Gem:** nút Quay tiếp mờ theo đúng giá lượt vừa quay, popup có cảnh báo, và không có lượt quay nào được gọi. Test ở Task 5.
4. **Lịch sử:** lượt bị từ chối không vào lịch sử; mục thứ 51 đẩy mục cũ nhất; Reset xoá sạch. Test ở Task 3.
5. **Asset copy:** GUID của clip, controller, prefab và PNG không đổi, và mọi GUID trong prefab đều resolve được. Test ở Task 1.

## File Structure

```
Core/Scripts/          GachaHistoryEntry.cs (mới), PlayerSave.cs (sửa)
Characters/Scripts/    CharacterDefinition.cs (sửa: _stars, Stars, Validate)
Gacha/Scripts/         GachaService.cs (sửa: ghi lịch sử)
Gacha/Animations/Summoning/   copy: 2 clip, 2 controller
Gacha/Prefabs/Cutscenes/      copy: 2 prefab
Gacha/Art/Summoning/          copy: Spark, Ring, Glow
UI/Scripts/            GachaRevealSequence.cs (mới), GachaRevealView.cs, GachaTotalView.cs,
                       GachaRollAgainPopup.cs, GachaHistoryPopup.cs (mới), GachaViewText.cs (sửa), GachaView.cs (sửa)
Editor/                ProjectSetup.cs (sửa: sao mẫu, điền sao còn 0), GachaSceneBuilder.cs (sửa)
Tests/                 GachaRevealAssetTests.cs, GachaRevealSequenceTests.cs (mới);
                       CharacterDefinitionTests, ProjectSetupTests, GachaServiceTests, SaveCompatibilityTests,
                       GameSessionTests, GachaViewTextTests, GachaSceneTests (sửa)
```

---

### Task 1: Copy bộ cutscene từ Project_04

**Files:**
- Copy (kèm `.meta`, nguyên văn) từ `D:\Unity\Project_04_BunDauPeo\Assets\__Vu\Gacha\` theo bảng spec mục 6.
- Test: `Assets/_Project/Tests/GachaRevealAssetTests.cs`

**Interfaces:**
- Produces: các đường dẫn `CommonSummonClipPath`, `LegendarySummonClipPath`, `CommonSummonPrefabPath`, `LegendarySummonPrefabPath` (hằng `const string` trong `GachaRevealAssetTests`, chỉ để test).

- [x] **Step 1: Viết test đỏ.** Với mỗi file cần copy, test kiểm: file tồn tại, `AssetDatabase.AssetPathToGUID(path)` bằng GUID gốc, và mọi `guid:` trong hai prefab đều có `AssetDatabase.GUIDToAssetPath` khác rỗng.

GUID gốc (đọc từ `.meta` của Project_04):

| File | GUID |
|---|---|
| CommonSummon.anim | `ad3d68231e9fbe34296679c68a094c80` |
| LegendarySummon.anim | `268b08577020b744e9f70366c7517a2f` |
| CommonSummon.controller | `eb4df6125cf84fd439f1179c823ef1e7` |
| LegendarySummon.controller | `30b38c69ead93714aa223ead3ad9cd8b` |
| Spark.png | `da69432c42035ea49bf93e416432139e` |
| Ring.png | `829805906caf53643a6c3fdfe23fa60b` |
| Glow.png | `a208deb31132cca4d980f5c1cd2e7649` |

`fe87c0e1cc204ed48ad3b37840f39efc` là component `Image` của uGUI, nằm trong package, nên test chỉ kiểm nó resolve được.

- [x] **Step 2: Chạy test, kỳ vọng đỏ:** `failed` lớn hơn 0, lỗi là file không tồn tại.
- [x] **Step 3: Copy file.** Copy đúng bảng spec, giữ nguyên `.meta`. Không sửa nội dung file nào.
- [x] **Step 4: Mở Editor (hoặc `unity recompile`), kiểm Console không có lỗi "Missing" hoặc "Failed to import".** Chạy test, kỳ vọng xanh.
- [x] **Step 5: Commit và push.** `git add` theo đường dẫn các thư mục đã copy và file test.

---

### Task 2: Số sao trên `CharacterDefinition`

**Files:**
- Modify: `Assets/_Project/Characters/Scripts/CharacterDefinition.cs`
- Modify: `Assets/_Project/Editor/ProjectSetup.cs`
- Test: `Assets/_Project/Tests/CharacterDefinitionTests.cs`, `Assets/_Project/Tests/ProjectSetupTests.cs`

**Interfaces:**
- Produces: `int CharacterDefinition.Stars` (property đọc từ `[SerializeField] private int _stars`, mặc định 1). `List<string> CharacterDefinition.Validate()` thêm lỗi `"Stars: must be 1..5"` khi ngoài khoảng.
- Produces: `ProjectSetup.StarsFor(Rarity rarity) -> int` (R → 1, SR → 2, SSR → 3), `private static`.
- Consumes: `Rarity` (`CharacterEnums.cs`).

Quyết định: `GetOrCreateAsset` không ghi lại asset đã có. Nên `ProjectSetup` có một bước riêng điền `_stars` cho các sample character đang có `_stars == 0`, không ghi đè giá trị user đã chỉnh.

- [ ] **Step 1: Viết test đỏ.**
  - `CharacterDefinitionTests`:
    - `Validate_StarsOutOfRange_Reported`: `Stars = 0` và `Stars = 6` đều có lỗi chứa `"Stars"`.
    - `Validate_StarsInRange_NoStarError`: `Stars` 1 và 5 không có lỗi `Stars`.
  - `ProjectSetupTests`:
    - `SampleCharacters_StarsMatchRarity`: mọi nhân vật trong `CharacterCatalog` mẫu có `Stars == StarsFor(Rarity)`. Test gọi `ProjectSetup.StarsFor` qua `internal`/`public` hoặc kiểm giá trị trực tiếp (1, 2, 3).
    - `SampleCharacters_StarsFilledWhenZero`: đặt `_stars = 0` trên một sample (dùng `SerializedObject`), gọi `ProjectSetup.Run`, rồi `_stars` là giá trị theo độ hiếm. Restore sau test.

- [ ] **Step 2: Chạy test, kỳ vọng đỏ:** lỗi `CS1061` về `Stars`.
- [ ] **Step 3: Implement.** Thêm field, property, lỗi trong `Validate`. Trong `ProjectSetup.Run` thêm `FillMissingStars(characters)`: với mỗi asset có `_stars == 0` thì đặt `StarsFor(rarity)`. Dùng `SerializedObject` như các hàm `Apply` hiện có.
- [ ] **Step 4: Chạy test, kỳ vọng xanh.**
- [ ] **Step 5: Chạy Setup (batch hoặc `unity command`), chạy lại Setup lần hai và kiểm `git status` không đổi thêm.**
- [ ] **Step 6: Commit và push.**

---

### Task 3: Lịch sử quay trong save

**Files:**
- Create: `Assets/_Project/Core/Scripts/GachaHistoryEntry.cs`
- Modify: `Assets/_Project/Core/Scripts/PlayerSave.cs`
- Modify: `Assets/_Project/Gacha/Scripts/GachaService.cs`
- Test: `Assets/_Project/Tests/SaveCompatibilityTests.cs`, `Assets/_Project/Tests/GachaServiceTests.cs`, `Assets/_Project/Tests/GameSessionTests.cs`

**Interfaces:**
- Produces: `[Serializable] public struct GachaHistoryEntry { public long Utc; public string BannerId; public int Count; public string[] CharacterIds; }`. Dùng `string[]` vì C# 9 không cho struct có field initializer.
- Produces: `PlayerSave.GachaHistory` (`List<GachaHistoryEntry>`, `= new()`).
- Produces: `GachaService.MaxHistoryEntries` (const `50`, `public`).
- Consumes: `GachaService` đã có `Pull(bannerId, ten)`. Thứ tự: áp kết quả → ghi pity → **thêm lịch sử** → `persist()` một lần.
- `Utc` lấy bằng `DateTimeOffset.UtcNow.ToUnixTimeSeconds()` trong `GachaService`; test không kiểm giá trị thời gian, chỉ kiểm số mục và Id.

- [x] **Step 1: Viết test đỏ.**
  - `SaveCompatibilityTests`:
    - `OldSave_WithoutGachaHistory_LoadsWithEmptyList`: JSON cũ không có `GachaHistory` cho `GachaHistory` khác `null` và rỗng.
    - `GachaHistory_RoundTripsThroughSaveService`: một mục có `CharacterIds = {"a", "b"}` đọc lại đúng.
  - `GachaServiceTests`:
    - `PullSingle_AppendsOneHistoryEntry`: `Count == 1`, `CharacterIds` có đúng một Id là nhân vật vừa quay.
    - `PullTen_AppendsOneEntryWithTenIds`: một mục, `Count == 10`, 10 Id theo thứ tự quay.
    - `RejectedPull_AddsNoHistory`: thiếu Gem, banner lạ, và RNG ném lỗi đều không thêm mục.
    - `History_KeepsLast50`: 51 lượt quay thành công, `GachaHistory.Count == 50`, mục đầu tiên đã bị bỏ, mục cuối có Id của lượt 51.
  - `GameSessionTests`:
    - Thêm assert vào `ResetToDefaults_ClearsEveryFieldOfTheSave`: thêm một mục vào `session.Save.GachaHistory` trước reset và kiểm rỗng sau reset.

- [x] **Step 2: Chạy test, kỳ vọng đỏ.**
- [x] **Step 3: Implement.** Thêm struct, field, hằng và đoạn ghi lịch sử trong `Pull`. Cắt đầu danh sách khi vượt `MaxHistoryEntries`.
- [x] **Step 4: Chạy test, kỳ vọng xanh.** `total` tăng đúng số test mới.
- [x] **Step 5: Commit và push.**

---

### Task 4: `GachaRevealSequence` (class thuần)

**Files:**
- Create: `Assets/_Project/UI/Scripts/GachaRevealSequence.cs`
- Test: `Assets/_Project/Tests/GachaRevealSequenceTests.cs`

**Interfaces:**
- Consumes: `GachaPullEntry` (`Gacha`), `Rarity` (`Characters`).
- Produces:

```csharp
public enum RevealStepKind { CommonCutscene, LegendaryCutscene, Reveal, Total }

public readonly struct RevealStep
{
    public RevealStep(RevealStepKind kind, int entryIndex);
    public RevealStepKind Kind { get; }
    public int EntryIndex { get; }   // -1 cho CommonCutscene và Total
}

public sealed class GachaRevealSequence
{
    public GachaRevealSequence(IReadOnlyList<GachaPullEntry> entries, bool isTen);
    public RevealStep Current { get; }
    public bool CanSkipAll { get; }          // isTen && Current không phải LegendaryCutscene && Total chưa tới
    public void Tap();                       // CommonCutscene và Reveal: sang bước kế; LegendaryCutscene và Total: không làm gì
    public void ClipFinished();              // CommonCutscene và LegendaryCutscene: sang bước kế; các bước khác: không làm gì
    public void SkipToSsrOrTotal();          // chỉ khi isTen; tới LegendaryCutscene đầu tiên, hoặc Total nếu không có SSR; không làm gì ở LegendaryCutscene
}
```

Thứ tự bước khi dựng: `CommonCutscene`, `Reveal` cho từng nhân vật thường (theo thứ tự quay), rồi với từng SSR theo thứ tự quay: `LegendaryCutscene` và `Reveal`, rồi `Total`. SSR là `Rarity.SSR`.

Quay 1 cũng có `Total`. Nếu `isTen` là false, `SkipToSsrOrTotal` không làm gì và `CanSkipAll` là false.

- [x] **Step 1: Viết test đỏ** (mỗi test dựng entries bằng `TestData.Character` với độ hiếm, và `GachaPullEntry`):
  - `Steps_NormalsInRollOrder_SsrLast`: entries R, SSR, R, SR → thứ tự `Common, Reveal(0), Reveal(2), Reveal(3), Legendary(1), Reveal(1), Total`. Lưu ý SR là thường.
  - `Steps_MultipleSsrInRollOrder`: entries SSR(0), R(1), SSR(2) → `Common, Reveal(1), Legendary(0), Reveal(0), Legendary(2), Reveal(2), Total`.
  - `Tap_OnCommonCutscene_GoesToFirstReveal`.
  - `ClipFinished_OnCommonCutscene_GoesToFirstReveal`.
  - `Tap_OnLegendaryCutscene_DoesNothing`: giữ nguyên `Current`.
  - `SkipToSsrOrTotal_WithSsr_GoesToFirstLegendary`: đứng ở Reveal đầu tiên, gọi, `Current` là `LegendaryCutscene` với entry SSR đầu.
  - `SkipToSsrOrTotal_WithoutSsr_GoesToTotal`.
  - `SkipToSsrOrTotal_OnLegendaryCutscene_DoesNothing`.
  - `SkipToSsrOrTotal_SingleRoll_DoesNothing`: `isTen = false`, `Current` không đổi.
  - `CanSkipAll_FalseOnLegendaryAndSingle`.
  - `Tap_OnTotal_DoesNothing`.
  - `ClipFinished_OnReveal_DoesNothing`.

- [x] **Step 2: Chạy test, kỳ vọng đỏ:** `CS0246` về `GachaRevealSequence`.
- [x] **Step 3: Implement** theo Interfaces. Dựng danh sách bước một lần trong constructor, `_index` trỏ vào bước hiện tại.
- [x] **Step 4: Chạy test, kỳ vọng xanh.**
- [x] **Step 5: Commit và push.**

---

### Task 5: Văn bản và điều kiện cho popup và lịch sử

**Files:**
- Modify: `Assets/_Project/UI/Scripts/GachaViewText.cs`
- Test: `Assets/_Project/Tests/GachaViewTextTests.cs`

**Interfaces:**
- Produces:
  - `string GachaViewText.InsufficientGemText(long needed) -> "Không đủ Gem (cần N)"`.
  - `string GachaViewText.RollAgainLabel(int count, CurrencyAmount[] cost) -> "Quay tiếp (900 Gem)"`. Tái dùng `PullLabel` có sẵn, chỉ đổi tiền tố thành "Quay tiếp".
  - `bool GachaViewText.CanRollAgain(Wallet wallet, CurrencyAmount[] cost) -> wallet.CanAfford(cost)`. Hàm này là điều kiện duy nhất để bật nút Quay tiếp.
  - `string GachaViewText.HistoryLine(GachaHistoryEntry entry, Func<string, string> nameOf) -> "HH:mm dd/MM · Quay 10 · Tên, Tên, ..."`, `Utc` đổi bằng `DateTimeOffset.FromUnixTimeSeconds(...).ToLocalTime()`. `nameOf` trả tên nhân vật hoặc Id.
- Consumes: `Wallet.CanAfford` (Core), `GachaHistoryEntry` (Task 3).

- [x] **Step 1: Viết test đỏ:**
  - `InsufficientGemText_ContainsNeededAmount`: chứa `"900"`.
  - `RollAgainLabel_ShowsCountAndCost`: `RollAgainLabel(10, [Gem 900])` chứa `"Quay tiếp"` và `"900"`.
  - `CanRollAgain_FalseWhenShort_TrueWhenEnough`: ví có Gem 899: `CanRollAgain([Gem 900])` là false; Gem 900 là true.
  - `HistoryLine_ListsNamesAndCount`: entry Count 10 với 2 Id, `nameOf` trả tên cố định. Dòng chứa "Quay 10" và cả hai tên.
  - `HistoryLine_Count1_SaysQuay1`.

- [x] **Step 2: Chạy test, kỳ vọng đỏ.**
- [x] **Step 3: Implement.**
- [x] **Step 4: Chạy test, kỳ vọng xanh.**
- [x] **Step 5: Commit và push.**

---

### Task 6: Các view hiển thị

**Files:**
- Create: `Assets/_Project/UI/Scripts/GachaRevealView.cs`, `GachaTotalView.cs`, `GachaRollAgainPopup.cs`, `GachaHistoryPopup.cs`
- Modify: `Assets/_Project/UI/Scripts/BunDauPeo.UI.asmdef` (không cần đổi nếu tham chiếu đủ; kiểm trước)

**Interfaces:**
- `GachaRevealView : MonoBehaviour` — `public event Action Finished;` (khi sequence tới `Total`). `public void Play(GachaRevealSequence sequence)`. Dùng `Animator` (`[SerializeField] Animator _commonAnimator`, `_legendaryAnimator`), `Animator.Play("Summon")`. Khi clip kết thúc gọi `ClipFinished` trên sequence; bằng cách kiểm `normalizedTime >= 1` trong `Update`. Lóe sáng bằng `Image` `_flash` với alpha tween 0 → 0.8 → 0 trong 0,35 giây, màu theo `GachaViewText.RarityColor`. Chạm: `Update` đọc `Input.GetMouseButtonDown` không được dùng; dùng `UnityEngine.InputSystem.Mouse.current` hoặc `Touchscreen.current` kiểm `wasPressedThisFrame`. Gọi `Tap` trên sequence.
- `GachaTotalView : MonoBehaviour` — `public void Show(IReadOnlyList<GachaPullEntry> entries, Func<string> ...)`; dựng icon trong `[SerializeField] RectTransform _grid` (5 cột, `GridLayoutGroup`), icon hiện lần lượt bằng coroutine với khoảng 0,08 giây (không dùng `Time.timeScale`). `public event Action OkClicked;` (nút `_okButton`).
- `GachaRollAgainPopup : MonoBehaviour` — `public void Open(string label, bool canRollAgain, long missingGem)`, `public event Action RollAgainClicked;`, `public event Action DismissClicked;` (nút Thôi). Cảnh báo bằng `GachaViewText.InsufficientGemText` khi `!canRollAgain`; nút Quay tiếp `interactable = canRollAgain`.
- `GachaHistoryPopup : MonoBehaviour` — `public void Open(IReadOnlyList<GachaHistoryEntry> history, Func<string, string> nameOf)` dùng `GachaViewText.HistoryLine`, `public event Action CloseClicked;`.

Tất cả hình là `Image` và chữ là `Text`. Không có logic thứ tự hay logic Gem trong view.

- [x] **Step 1: Test không viết ở task này cho hành vi Animator/tween.** Thay bằng test kiểm tra tham chiếu trong scene ở Task 8 (`GachaSceneTests`), và kiểm bằng Play Mode ở Task 9. Trước khi viết code, đọc `Assets/_Project/Tests/GachaSceneTests.cs` để thấy hàm kiểm scene.
- [x] **Step 2: Implement** bốn MonoBehaviour theo Interfaces. Giữ mỗi view dưới khoảng 120 dòng.
- [x] **Step 3: Chạy test (chưa có test mới), kỳ vọng xanh và không có lỗi compile.**
- [x] **Step 4: Commit và push.**

---

### Task 7: Điều phối trong `GachaView`

**Files:**
- Modify: `Assets/_Project/UI/Scripts/GachaView.cs`

**Interfaces:**
- Consumes: `GachaService.PullSingle/PullTen`, `GachaRevealSequence`, `GachaRevealView`, `GachaTotalView`, `GachaRollAgainPopup`, `GachaHistoryPopup`, `GachaViewText.CanRollAgain/RollAgainLabel`, `PlayerSave.GachaHistory`.
- Luồng: bấm quay → khoá nút → `GachaRevealView.Play(seq)` → khi `Finished` → `GachaTotalView.Show(entries)` → OK → `GachaRollAgainPopup.Open(label, canRollAgain, missingGem)` → Quay tiếp gọi lại đúng hàm quay với cùng số lượt (`_lastCount`), Thôi đóng popup và mở nút quay. Nút Lịch sử mở `GachaHistoryPopup` với `GachaService` lấy lịch sử từ save (qua `ServiceLocator.Get<PlayerSave>()`, hoặc thêm `GachaService.History` read-only, chọn một và ghi trong commit).
- Bỏ vẽ lưới kết quả trên banner (`_slots`, `Show`, `Slot` cũ) vì kết quả chuyển sang `GachaTotalView`.

- [x] **Step 1: Viết test.** Logic điều phối không có test riêng (đã nằm trong `GachaRevealSequence` và `GachaViewText`). Viết test ở Task 8 cho scene. Đọc trước code hiện tại của `GachaView` (đã có `OnEnable`, `Redraw`, `SetButton`).
- [x] **Step 2: Implement.** Ghi lại `_lastCount` và `_lastBanner` khi bấm quay để Quay tiếp dùng đúng số lượt.
- [x] **Step 3: Chạy test, kỳ vọng xanh. Xác nhận không có chỗ nào gọi pull khi popup mở với Quay tiếp bị mờ.**
- [x] **Step 4: Commit và push.**

---

### Task 8: Dựng lại scene `Gacha.unity`

**Files:**
- Modify: `Assets/_Project/Editor/GachaSceneBuilder.cs`
- Modify: `Assets/_Project/Tests/GachaSceneTests.cs`
- Generate: `Assets/_Project/Scenes/Gacha.unity` (chạy `ProjectSetup.Run` hoặc gọi builder)

**Interfaces:**
- Scene có: `Camera`, `EventSystem` (`InputSystemUIInputModule`), `Canvas`, trang roll (Gem, tên banner, pity, nút Quay 1, Quay 10, nút Lịch sử, dòng thông báo), `GachaRevealView` với hai `Animator` (instance của `CommonSummon.prefab` và `LegendarySummon.prefab` từ `Prefabs/Cutscenes`), `GachaTotalView` (lưới 5 cột), `GachaRollAgainPopup`, `GachaHistoryPopup` (ẩn mặc định).
- `GachaView` gán toàn bộ tham chiếu mới bằng `SerializedObject`.

- [x] **Step 1: Viết test đỏ** trong `GachaSceneTests`:
  - `GachaScene_HasRevealTotalAndPopups`: có đúng một `GachaRevealView`, `GachaTotalView`, `GachaRollAgainPopup`, `GachaHistoryPopup`; các trường tham chiếu khác `null`.
  - `GachaScene_CutscenePrefabsAreInstanced`: có hai instance tham chiếu `Animator` với controller đúng (`CommonSummon`, `LegendarySummon`).
  - `GachaScene_TotalGridIsFiveColumns`: `GridLayoutGroup.constraintCount == 5`.
  - `GachaScene_StillNoRawImage`, `GachaScene_StillNoStandaloneInputModule`: giữ nguyên các test cũ.

- [x] **Step 2: Chạy test, kỳ vọng đỏ:** `CS` hoặc assert "null" trên tham chiếu mới.
- [x] **Step 3: Implement builder.** Dùng `PrefabUtility.InstantiatePrefab` cho hai cutscene. Ẩn trang tổng và popup mặc định. Sau khi dựng, `ProjectSetup.Run` (không phải builder riêng) sinh lại scene, để `GachaSceneBuilder` giữ một nguồn duy nhất.
- [x] **Step 4: Chạy Setup, chạy lại Setup lần hai, chạy test, kỳ vọng xanh.**
- [x] **Step 5: Commit và push.**

---

### Task 9: Tài liệu và kiểm tra trong Play Mode

**Files:**
- Modify: `docs/project/structure.md`, `docs/roadmap.md`, `docs/design/game-design.md`, `docs/superpowers/specs/2026-10-31-gacha-design.md` (mục 8), `docs/lessons/lesson.md` khi có lỗi.

- [x] **Step 1: Cập nhật docs** theo spec mục 8: thêm `Gacha/Prefabs/Cutscenes`, `Gacha/Art/Summoning`, số test mới; game-design thêm luật SSR cuối, không bỏ qua, OK và popup quay tiếp, lịch sử 50 mục.
- [x] **Step 2: Chạy test toàn bộ, kỳ vọng xanh.**
- [x] **Step 3: Kiểm trong Play Mode (`unity command editor.playmode start`, chụp `capture game_view`).** Kiểm: cutscene Common chạm để bỏ qua; SSR không bỏ qua; Quay 10 có nút Bỏ qua; OK mở popup; thiếu Gem thì Quay tiếp mờ và có cảnh báo; Lịch sử hiện mục mới. Sao lưu `save.json*` trước và khôi phục sau.
- [x] **Step 4: Commit và push.** Ghi "chưa kiểm" cho bất kỳ phần Play nào không tự kiểm được.

---

## Ruling ghi nhận

- `CharacterIds` là `string[]` vì C# 9 không cho struct có field initializer.
- Điền số sao cho asset cũ bằng một bước riêng, chỉ khi `_stars == 0`, không ghi đè giá trị user đã chỉnh.
- `Utc` là Unix seconds, không kiểm trong test.
- Nút Lịch sử và `GachaHistoryPopup` đọc lịch sử qua `ServiceLocator.Get<PlayerSave>()` nếu được đăng ký; nếu không, thêm `GachaService.History`. Chọn một trong Task 7, ghi trong commit.

---

Plan complete and saved to `docs/superpowers/plans/2026-10-31-gacha-reveal.md`. Please review the plan. Which execution approach would you prefer?

- **Subagent-driven**: a fresh subagent implements each task and a fresh reviewer checks it before the next one starts, then a whole-branch review at the end. Most thorough, but costs a fresh context per task and per review.
- **Native**: I implement every task in this session, then one fresh reviewer on the most capable model checks the whole branch. Cheapest and fastest, with no independent review until the end.

For this plan I recommend **Native**, because the tasks are sequential and depend on each other's interfaces, and a shipped mistake would mainly affect gacha display, not money or saves. Does the plan capture what you want, and which approach should we use?
