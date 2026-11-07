# Song ngữ Việt–Anh: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (mặc định, native) hoặc superpowers:subagent-driven-development để làm kế hoạch này từng task (cách thực thi do 🎯 chọn, xem [agent-routing.md](../../guides/agent-routing.md)). Các bước dùng checkbox (`- [ ]`).

**Goal:** Mọi chữ người chơi thấy có bản tiếng Việt và tiếng Anh. Ngôn ngữ lưu trong `PlayerSave`, đổi được trong panel Cài đặt DEV.

**Architecture:**
- Core có `Language`, `GameLanguage` (ngôn ngữ đang dùng, chọn theo save hoặc theo máy) và `LanguageSwitcher` (cầu nối cho UI, như `GameResetter`).
- Boot áp ngôn ngữ lúc khởi động và sau reset.
- UI có bảng `UiText` (mỗi khu một file `partial`) và component `LocalizedText` cho nhãn cố định do builder dựng.
- Chín lớp SO có thêm `_displayNameEn`.
- Đổi ngôn ngữ thì lưu rồi nạp lại scene đang mở; riêng Battle không nạp lại.

**Tech Stack:** Unity 6000.3.15f1, C#, uGUI `Text` (font `LegacyRuntime.ttf`), Test Framework, CLI `unity` 1.0.0-beta.15. Không thêm package, không TextMeshPro, không file ảnh.

**Spec:** [docs/superpowers/specs/2026-11-07-bilingual-design.md](../specs/2026-11-07-bilingual-design.md) (user duyệt thiết kế và bản viết 2026-11-07). "Spec 5.3" dưới đây là mục của spec đó.

## Global Constraints

- Không sửa/xoá tay `.meta`; không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`, `Không động tới/`. **Không sửa tay `.unity` hay `.asset`**: scene chỉ đổi bằng builder rồi `Rebuild…Scene`; asset SO chỉ đổi bằng `Setup Project`.
- **Git: worker không commit, không push** (chỉ 📋 Thư ký). Nhánh `feature/bilingual` cắt từ `origin/main` mới nhất, sau khi nhánh `docs/bilingual-spec` đã merge. Chưa merge thì cắt từ chính nhánh docs đó. Mỗi task kết thúc bằng test xanh và tick checkbox. Không để lẫn vào thay đổi: `Assets/_Recovery/`, `Assets/InitTestScene*.unity`, `ProjectSettings/`, ảnh chụp.
- Mã ngôn ngữ trong save: đúng `"vi"` / `"en"`; `""` = chưa chọn (theo máy). Máy `SystemLanguage.Vietnamese` → `Vi`, mọi giá trị khác → `En`.
- Key bảng chữ: `khu.tên`, viết thường, `snake_case` (`combat.wave`, `common.close`). Key thiếu → `UiText.Get` trả `"#" + key`.
- Bản tiếng Anh do 🔨 dịch nháp. Tên nhân vật và món: tiếng Việt **bỏ dấu**, viết hoa chữ đầu mỗi từ ("Cha Com", "Bun Dau Mam Tom"). Tên khác dịch nghĩa. Chuỗi tiếng Việt cũ giữ **đúng từng ký tự** (test cũ so chuỗi).
- Bản tiếng Anh giữ nguyên `\n` và mọi chỗ `{n}` của bản tiếng Việt, cùng ký hiệu `·`, `→`, `★`, `≈`.
- Chữ của `DevOverlay` giữ tiếng Việt, trừ nhãn nút ngôn ngữ (Task 9).
- **Giữ nguyên tên mọi GameObject và nút** (`SmokeLoopTests`, `PlaytestTools.ClickButton` bấm theo tên).
- Test: không `[TestCase]` (L-012); tự hỏi "cài đặt sai nào vẫn qua test này" (L-006). Test nào đặt `GameLanguage.Current` thì trả về `Language.Vi` trong `[TearDown]`.
- Code theo [code-style.md](../../guides/code-style.md). Sau mỗi task: Console không có lỗi compile hay warning mới; `git status --short` chỉ có file của task.

### Lệnh dùng chung

Kiểm `unity status` **thành một lệnh riêng** trước mọi lệnh batch (L-020). Lệnh batch cần Editor **đóng**; Editor đang mở thì nhờ user đóng, không kill.

```bash
unity test . --mode EditMode --output ../../bdp_BunDauPeo/tests.xml --timeout 600 -- -logFile ../../bdp_BunDauPeo/tests.log
unity test . --mode PlayMode --output ../../bdp_BunDauPeo/playtests.xml --timeout 900 -- -logFile ../../bdp_BunDauPeo/playtests.log
unity run . -- -executeMethod BunDauPeo.EditorTools.ProjectSetup.RebuildGachaScene -logFile ../../bdp_BunDauPeo/rebuild.log
unity run . -- -executeMethod BunDauPeo.EditorTools.ProjectSetup.Run -logFile ../../bdp_BunDauPeo/setup.log
```

- Lọc một lớp test: thêm `--filter <TênLớp>`, nếu CLI không có cờ này thì kiểm `unity test -h` trước, đừng đoán.
- Đọc `../../bdp_BunDauPeo/tests.xml` (`failed="0"`, `total="N"`), không chỉ dựa vào mã thoát (`0` đạt, `8` test đỏ, `6` lỗi khác).
- Sau `unity run` kiểm `git status` thấy file đổi rồi mới tin log (L-020).
- Tên hàm của menu Setup Project: đọc `[MenuItem("Tools/BunDauPeo/Setup Project")]` trong `ProjectSetup.cs` (dòng ~125) rồi dùng đúng tên đó thay cho `Run`.

Số test: nền **747 EditMode + 12 PlayMode**. Mỗi task ghi số mới vào checkbox cuối. Dự kiến hết kế hoạch **784 EditMode + 13 PlayMode**. Số thật có thể lệch nếu Task 10 phải thêm ca.

## Review Focus

1. **Bật lại popup ghi đè chữ của view**: `LocalizedText` gắn nhầm vào `Text` mà view ghi lúc chạy ("Đợt 2/3", dòng trạng thái). Khi popup bật lại, `OnEnable` đặt lại chữ giữ chỗ, đè chữ thật. Test ở Task 7 (`*Scene_LocalizedTexts_KeysExistAndNotWrittenByViews`: không `Text` nào có `LocalizedText` lại được field `Text` của một view trỏ tới).
2. **Nút hai dòng thành một dòng**: bản tiếng Anh của `PullLabel` ("Quay {0}\n{1}") hay `AfkButton` thiếu `\n` thì chữ tràn nút. Test ở Task 3 (`AllEntries_PlaceholdersAndLineBreaksMatch`: số `\n` cũng phải khớp).
3. **Reset khi đang chọn English**: sau Reset Save Data, game phải về ngôn ngữ theo máy chứ không kẹt ở English. Ngôn ngữ chỉ đổi khi `GameBootstrap` nghe `ResetCompleted`. Test ở Task 9 (PlayMode, bước reset).
4. **Đổi ngôn ngữ giữa trận**: nạp lại Battle sẽ làm hỏng trận đang chạy. Test ở Task 9 (`ShouldReloadScene_Battle_ReturnsFalse`).
5. **SO mới (sau này) quên tên tiếng Anh**: màn phải hiện tên tiếng Việt chứ không để trống. Test ở Task 1 (`Pick_EnglishEmpty_FallsBackToVietnamese`) và Task 8 (`LocalizedNames_AllCatalogAssetsHaveEnglish`).

## Điều hướng agent

### Chia session

| Phiên | Task | Dừng |
|---|---|---|
| A | 1–3: nền (Core, Boot, bảng chữ) | Không playtest; báo xong cho 📋 |
| B | 4–7: chuyển chữ 3 khu, dựng lại 4 scene | Không playtest; báo xong cho 📋 |
| C | 8–11: tên SO, nút đổi ngôn ngữ, test chặn chữ viết cứng, ảnh tiếng Anh và docs | **Dừng playtest sau Task 11**: user xem 4 ảnh tiếng Anh và duyệt bảng `UiText.*.cs` |

Phiên sau mở khi phiên trước đã báo xong và 📋 đã commit.

### Task nào chạm gì

| Task | Code | Unity (scene / asset) | Song song |
|---|---|---|---|
| 1 | Core: `GameLanguage.cs`, `LanguageSwitcher.cs`, `PlayerSave.cs` | Không | Không (nền) |
| 2 | Boot: `GameSession.cs`, `GameBootstrap.cs` | Không | Không |
| 3 | UI: `Text/UiText*.cs`, `Text/LocalizedText.cs`; Editor: helper trong `GachaSceneBuilder.cs` | Không | Không |
| 4 | `UiText.Gacha.cs`, Gacha view + builder | Không (scene dựng ở Task 7) | **Có, với 5 và 6** |
| 5 | `UiText.Restaurant.cs`, Restaurant view + builder | Không | **Có, với 4 và 6** |
| 6 | `UiText.Combat.cs`, Combat view + 2 builder | Không | **Có, với 4 và 5** |
| 7 | Test scene | Dựng lại 4 scene | Không |
| 8 | 9 SO, `ProjectSetup.cs` | Chạy Setup Project (asset SO) | Không |
| 9 | `DevOverlay.cs`, `SmokeLoopTests.cs` | Không | Không |
| 10 | `HardcodedTextTests.cs` | Không | Không |
| 11 | Docs | Chụp ảnh (Play) | Không |

Task 4, 5 và 6 chỉ sửa `.cs`, không chung file (mỗi khu một file bảng), nên giao được 3 subagent song song. Subagent không chạy Unity, không commit, không sửa `UiText.Common.cs`: thiếu chữ chung thì đặt key trong file khu của mình. Kênh 🔨 đọc diff rồi chạy `unity test` một lần cho cả đợt.

---

## Phiên A

### Task 1: Ngôn ngữ trong Core và save

**Files:**
- Create: `Assets/_Project/Core/Scripts/GameLanguage.cs` (`enum Language`, `static class GameLanguage`)
- Create: `Assets/_Project/Core/Scripts/LanguageSwitcher.cs`
- Modify: `Assets/_Project/Core/Scripts/PlayerSave.cs` (thêm field cuối lớp)
- Test: `Assets/_Project/Tests/GameLanguageTests.cs` (mới), `Assets/_Project/Tests/SaveCompatibilityTests.cs`

**Interfaces:**
- Produces:
  - `public enum Language { Vi, En }`.
  - `GameLanguage.Current { get; set; }`, mặc định `Language.Vi`.
  - `GameLanguage.Code(Language) → string` (`"vi"`/`"en"`).
  - `GameLanguage.TryParse(string, out Language) → bool`.
  - `GameLanguage.Resolve(string saved, SystemLanguage system) → Language`.
  - `GameLanguage.Pick(string vi, string en) → string`.
  - `PlayerSave.Language` (`string`, khởi tạo `""`).
  - `LanguageSwitcher(Func<Language, bool> set)` với `bool Set(Language)`. Constructor ném `ArgumentNullException` khi `set` null, như `GameResetter`.

- [x] **Step 1: Viết test đỏ** trong `GameLanguageTests` (`[TearDown]` đặt `Current = Language.Vi`):
  - `Resolve_SavedCodeWins_RegardlessOfDevice`: lặp `saved` ∈ {"vi","en"} × `system` ∈ {Vietnamese, English, Japanese, Unknown}; kỳ vọng `"vi"`→`Vi`, `"en"`→`En`; thông báo `$"saved={saved}, system={system}"`.
  - `Resolve_EmptyOrUnknownCode_FollowsDevice`: lặp `saved` ∈ {"", null, "fr", "VI"}; `Vietnamese`→`Vi`, `English`/`Japanese`/`Unknown`→`En`. Ca `"VI"` chứng minh mã phân biệt hoa thường; mã chỉ là chữ thường do code ghi.
  - `Code_RoundTripsThroughTryParse`: với mỗi giá trị enum, `TryParse(Code(x))` trả `true` và ra `x`; `Code(Language.En) == "en"`.
  - `Pick_EnglishEmpty_FallsBackToVietnamese`: `Current = En`: `Pick("A","B") == "B"`, `Pick("A","") == "A"`, `Pick("A",null) == "A"`; `Current = Vi`: `Pick("A","B") == "A"`.
  - `SaveCompatibilityTests.OldSave_WithoutLanguage_LoadsEmpty`: JSON save cũ không có `Language` (theo khuôn các ca cũ trong file) → `loaded.Language == ""`.
  - Mở rộng `DevSettingsTests` (cạnh `GameResetter_Reset_ReturnsDelegateResult`): `LanguageSwitcher_Set_PassesLanguageAndReturnsResult`. Delegate ghi lại tham số và trả `false`; kiểm tham số là `En` và kết quả `false`.
- [x] **Step 2: Chạy EditMode**, kỳ vọng đỏ vì lỗi compile (thiếu kiểu).
- [x] **Step 3: Cài đặt** theo spec 5.1. `TryParse` so `string.Equals(..., StringComparison.Ordinal)`. Field `PlayerSave.Language` có comment: `""` = chưa chọn, theo máy.
- [x] **Step 4: Chạy EditMode**, kỳ vọng xanh: 747 + 6 = **753** (đã chạy: 753/753, failed 0).

### Task 2: Lưu và áp ngôn ngữ (Boot)

**Files:**
- Modify: `Assets/_Project/Boot/Scripts/GameSession.cs`
- Modify: `Assets/_Project/Boot/Scripts/GameBootstrap.cs` (khối đăng ký service, dòng ~91–112)
- Test: `Assets/_Project/Tests/GameSessionTests.cs`

**Interfaces:**
- Consumes: Task 1.
- Produces:
  - `GameSession.SetLanguage(Language language) → bool`.
  - `GameSession.ApplyLanguage(SystemLanguage system) → void`.
  - `LanguageSwitcher` có trong `ServiceLocator` khi Boot chạy xong.

- [x] **Step 1: Viết test đỏ** trong `GameSessionTests` (thêm `GameLanguage.Current = Language.Vi` vào `TearDown`):
  - `SetLanguage_Saved_ChangesSaveFileAndCurrent`: `SetLanguage(En)` trả `true`; `Save.Language == "en"`; `GameLanguage.Current == En`. Mở `GameSession` mới trên cùng file thì `Save.Language == "en"` (đã ghi đĩa).
  - `SetLanguage_WhenFileLocked_KeepsOldLanguage`: đặt trước `SetLanguage(Vi)`; khoá file theo khuôn `ResetToDefaults_WhenFilesLocked_ChangesNothing` (`FileShare.None`); `SetLanguage(En)` trả `false`; `Save.Language == "vi"`; `Current == Vi`.
  - `ApplyLanguage_UsesSavedChoiceElseDevice`: `Save.Language = ""` → `ApplyLanguage(English)` → `En`, `ApplyLanguage(Vietnamese)` → `Vi`. `Save.Language = "en"` → `ApplyLanguage(Vietnamese)` → `En`.
  - Mở rộng `ResetToDefaults_ClearsEveryFieldOfTheSave`: trước reset đặt `session.Save.Language = "en"`; sau reset `Assert.AreEqual("", session.Save.Language)`.
- [x] **Step 2: Chạy EditMode**, kỳ vọng đỏ.
- [x] **Step 3: Cài đặt** theo spec 5.2. `SetLanguage` lưu lỗi thì trả `Save.Language` về giá trị cũ. Trong `GameBootstrap`, ngay sau dòng đăng ký `GameResetter`:
  - `_session.ApplyLanguage(Application.systemLanguage);`
  - `ServiceLocator.Register(new LanguageSwitcher(_session.SetLanguage));`
  - `_session.ResetCompleted += () => _session.ApplyLanguage(Application.systemLanguage);`

  Comment một dòng: reset ghi `Language = ""` nên quay về theo máy (D-005 luật 3).
- [x] **Step 4: Chạy EditMode**, kỳ vọng xanh: **756**. Chạy PlayMode, kỳ vọng 12/12 (Boot vẫn chạy). (Đã chạy: EditMode 756/756, PlayMode 12/12, failed 0.)

### Task 3: Bảng chữ `UiText`, `LocalizedText`, helper builder

**Files:**
- Create: `Assets/_Project/UI/Scripts/Text/UiText.cs`, `UiText.Common.cs`, `UiText.Gacha.cs`, `UiText.Restaurant.cs`, `UiText.Combat.cs` (ba file khu lúc này là mảng rỗng)
- Create: `Assets/_Project/UI/Scripts/Text/LocalizedText.cs`
- Modify: `Assets/_Project/Editor/GachaSceneBuilder.cs` (thêm helper cạnh `NewText`/`AddText`, dòng ~334)
- Test: `Assets/_Project/Tests/UiTextTests.cs`, `Assets/_Project/Tests/LocalizedTextTests.cs` (mới)

**Interfaces:**
- Consumes: `GameLanguage.Current` (Task 1).
- Produces:
  - `UiText.Get(string key) → string`.
  - `UiText.Format(string key, params object[] args) → string`, dùng `CultureInfo.InvariantCulture`.
  - `UiText.Decimal(double value, string format) → string`: Vi đổi `.` thành `,`; En giữ `.`.
  - `UiText.AllEntries → IReadOnlyList<(string Key, string Vi, string En)>`.
  - `UiText.BuildTable(IEnumerable<(string Key, string Vi, string En)>) → Dictionary<string, (string Vi, string En)>`, `public` để test, ném `ArgumentException` (thông báo có key) khi trùng key.
  - Mỗi file khu khai báo `private static readonly (string Key, string Vi, string En)[] <Khu> = { … };`, với `<Khu>` ∈ `Common`, `Gacha`, `Restaurant`, `Combat`. `UiText.cs` nối 4 mảng này theo thứ tự đó.
  - `LocalizedText : MonoBehaviour` (`[RequireComponent(typeof(Text))]`), field `[SerializeField] private string _key`, `public string Key`, `public void SetKey(string key)` để builder gán; `OnEnable` gán `Text.text = UiText.Get(_key)`.
  - Builder: `internal static Text NewLabel(string name, RectTransform parent, Font font, string key, int size, TextAnchor align, Color color)` và `internal static Text AddLabel(RectTransform rect, Font font, string key, int size, TextAnchor align, Color color)`. Hai hàm gọi `NewText`/`AddText` với `UiText.Get(key)` rồi gắn `LocalizedText` với `key`. Khu khác gọi qua `GachaSceneBuilder.NewLabel`, như đang gọi `NewText`.
- Key dùng chung có sẵn trong `UiText.Common.cs` (Task 4–6 dùng, không thêm vào file này):

  | Key | Vi | En |
  |---|---|---|
  | `common.close` | Đóng | Close |
  | `common.cancel` | Hủy | Cancel |
  | `common.continue` | Tiếp tục | Continue |
  | `common.no` | Không | No |
  | `common.not_now` | Thôi | Not now |
  | `common.move` | Chuyển | Move |
  | `common.role_cook` | Đầu bếp | Cook |
  | `common.role_server` | Phục vụ | Server |
  | `common.role_cashier` | Thu ngân | Cashier |

  Chuỗi "Hủy" và "Huỷ" đang cùng có trong code. Builder đang ghi "Hủy" thì dùng `common.cancel`. `DevOverlay` giữ "Huỷ" của nó.

- [x] **Step 1: Viết test đỏ.** `UiTextTests`:
  - `AllEntries_HaveBothLanguages`: mọi dòng có `Key`, `Vi`, `En` không rỗng hay trắng; thông báo ghi key.
  - `AllEntries_PlaceholdersAndLineBreaksMatch`: với mỗi dòng, tập chỉ số `{n}` (regex `\{(\d+)`) của `Vi` và `En` bằng nhau, và số `\n` bằng nhau.
  - `AllEntries_KeysFollowFormat`: key khớp `^[a-z]+\.[a-z0-9_]+$`.
  - `BuildTable_DuplicateKey_Throws`: hai dòng cùng key `"x.a"` → `ArgumentException`, thông báo chứa `"x.a"`. Ca hai key khác nhau → không ném, `Count == 2`.
  - `Get_FollowsCurrentLanguage_AndUnknownKeyShowsHash`: `Get("common.close")` ra "Đóng" (Vi) và "Close" (En); `Get("nope.key") == "#nope.key"`.
  - `Decimal_CommaInVietnamese_DotInEnglish`: `Decimal(1.5, "0.0")` ra "1,5" (Vi) và "1.5" (En).

  `LocalizedTextTests`: tạo `GameObject` có `Text` + `LocalizedText`, gọi `SetKey("common.close")`.
  - `OnEnable_SetsTextForCurrentLanguage`: Vi → "Đóng".
  - `Reenable_AfterLanguageChange_UpdatesText`: tắt, đặt `Current = En`, bật lại → "Close".

  Huỷ `GameObject` trong `TearDown`.
- [x] **Step 2: Chạy EditMode**, kỳ vọng đỏ.
- [x] **Step 3: Cài đặt** theo Interfaces và spec 5.3–5.4. Bảng gộp lười (lần `Get` đầu) qua `BuildTable(AllEntries)`.
- [x] **Step 4: Chạy EditMode**, kỳ vọng xanh: **764** (đã chạy: 764/764, failed 0). `LocalizedTextTests` gọi `OnEnable` bằng reflection vì EditMode không tự gọi (L-029).

## Phiên B

Ba task 4–6 làm cùng một khuôn:
1. Thêm dòng vào file bảng của khu.
2. Thay chuỗi trong hàm chữ và view bằng `UiText.Get`/`Format`/`Decimal`, giữ nguyên chữ ký hàm.
3. Builder đổi nhãn cố định sang `NewLabel`/`AddLabel` (key). Chữ giữ chỗ mà view ghi đè lúc chạy thì dùng `NewText(…, UiText.Get(key), …)`, không gắn `LocalizedText`. Phân biệt bằng cách xem field `Text` của view: builder có gán `Text` đó vào một field của view (`SerializedObject`/`FindProperty`) thì đó là chữ view ghi.
4. Test cũ (tiếng Việt) giữ nguyên và phải xanh. Thêm ca tiếng Anh: lớp test thêm `[TearDown]` trả `Current = Vi`.
5. Không dựng lại scene (Task 7).

### Task 4: Chuyển chữ Gacha

**Files:**
- Modify: `UI/Scripts/Text/UiText.Gacha.cs`, `UI/Scripts/GachaViewText.cs`, `UI/Scripts/GachaHistoryPopup.cs:33`, `Editor/GachaSceneBuilder.cs` (8 nhãn: Lịch sử, Kết quả quay, Bỏ qua, Quay tiếp?, Quay tiếp, Thôi, Lịch sử quay, Đóng)
- Test: `Tests/GachaViewTextTests.cs`

**Interfaces:** Consumes Task 3. Key bắt đầu bằng `gacha.`.

- [ ] **Step 1: Viết test đỏ** (`Current = En`):
  - `Describe_English`: mới → "NEW"; trùng 3 mảnh → chứa "+3" và không có ký tự có dấu.
  - `PullLabel_English_KeepsTwoLines`: `PullLabel(10, cost)` có đúng một `\n`, dòng đầu chứa "10", dòng sau giống bản Vi (giá không dịch).
  - `StatusMessage_English_EveryStatus`: lặp mọi `GachaPullStatus` (và `saved` false với `Success`). Kết quả không bắt đầu bằng `"#"`, không có chữ có dấu (regex `[À-ỹ]`), và khác bản Vi trừ chuỗi rỗng.
- [ ] **Step 2: Chạy EditMode**, kỳ vọng đỏ.
- [ ] **Step 3: Chuyển chữ.** `HistoryLine` giữ `HH:mm dd/MM`. `CostText` không dịch (số và tên tiền).
- [ ] **Step 4: Chạy EditMode**, kỳ vọng xanh: test cũ của Gacha không đổi, **+3**.

### Task 5: Chuyển chữ Restaurant

**Files:**
- Modify: `UI/Scripts/Text/UiText.Restaurant.cs`, `UI/Scripts/RestaurantViewText.cs`, `UI/Scripts/StaffPanel.cs` (dòng 150–151, 168, 216, 240), `UI/Scripts/StaffPanelRows.cs` (`RoleName` → `common.role_*`; dòng 88–112), `UI/Scripts/AfkPopup.cs:106`, `Editor/RestaurantSceneBuilder.cs` (15 nhãn)
- Test: `Tests/RestaurantViewTextTests.cs`, `Tests/StaffPanelRowsTests.cs`

**Interfaces:** Consumes Task 3. Key bắt đầu bằng `restaurant.`. `IncidentHint` dùng `StaffPanelRows.RoleName` (bỏ `switch` lặp lại).

- [ ] **Step 1: Viết test đỏ** (`Current = En`):
  - `IncomeLine_English_UsesDotAndPerSecond`: thu nhập 1.5 → "≈ 1.5 Coin/s"; thiếu đầu bếp → chuỗi chứa "Cook".
  - `AfkButton_English_KeepsTwoLines`: có đúng một `\n`, chứa số coin.
  - `RoleName_English_EveryRole`: lặp mọi `StaffRole` → "Cook", "Server", "Cashier".
  - Ca Vi `IncomeLine` cũ (dấu phẩy) vẫn xanh: đó là test chứng minh `Decimal` theo ngôn ngữ.
- [ ] **Step 2: Chạy EditMode**, kỳ vọng đỏ.
- [ ] **Step 3: Chuyển chữ.** Số thập phân qua `UiText.Decimal`.
- [ ] **Step 4: Chạy EditMode**, kỳ vọng xanh: **+3**.

### Task 6: Chuyển chữ Combat và bản đồ

**Files:**
- Modify: `UI/Scripts/Text/UiText.Combat.cs`, `UI/Scripts/CombatViewText.cs`, `UI/Scripts/TeamSelectPopup.cs` (dòng 61, 167), `UI/Scripts/BattleResultView.cs:71`, `Editor/BattleSceneBuilder.cs` (15 nhãn), `Editor/StageSelectSceneBuilder.cs` (14 nhãn; `"Đợt {row + 1}"` → `UiText.Format("combat.wave_row", row + 1)`)
- Test: `Tests/CombatViewTextTests.cs`

**Interfaces:** Consumes Task 3. Key bắt đầu bằng `combat.`. `NodeLabel` của màn Truyện dùng key `combat.node_story` ("Truyện" / "Story"). `ClassName`, `PositionName`, `EndBanner` cũng chuyển; "CLEAR" giống nhau ở cả hai tiếng.

- [ ] **Step 1: Viết test đỏ** (`Current = En`):
  - `EnterFailedLine_English_EveryStatus`: lặp mọi `EnterStageStatus`. Kết quả không rỗng, không `"#"`, không chữ có dấu.
  - `StaminaCard_English`: `StaminaCard(5, 100, 61)` chứa "5/100" và "1:01".
  - `ClassAndPosition_English_EveryValue`: lặp mọi `CharacterClass` và `BattlePosition`. Kết quả không rỗng, không chữ có dấu.
- [ ] **Step 2: Chạy EditMode**, kỳ vọng đỏ.
- [ ] **Step 3: Chuyển chữ.**
- [ ] **Step 4: Chạy EditMode**, kỳ vọng xanh: **+3**. Tổng sau Task 4–6: **773**.

### Task 7: Dựng lại 4 scene, test đọc lại `LocalizedText`

**Files:**
- Modify (dựng lại): `Assets/_Project/Scenes/Gacha.unity`, `Restaurant.unity`, `StageSelect.unity`, `Battle.unity`
- Test: `Tests/GachaSceneTests.cs`, `RestaurantSceneTests.cs`, `StageSelectSceneTests.cs`, `BattleSceneTests.cs`

**Interfaces:**
- Consumes: Task 3–6.
- Produces: helper test `SceneTextAsserts.AssertLocalizedTexts(Scene scene)`, đặt trong `Tests/SceneTextAsserts.cs` mới.

- [ ] **Step 1: Viết test đỏ.** `AssertLocalizedTexts` duyệt mọi `LocalizedText` trong scene, gồm cả đối tượng tắt (`GetComponentsInChildren<LocalizedText>(true)`, L-013), và kiểm:
  1. số `LocalizedText` > 0;
  2. mọi `Key` có trong `UiText.AllEntries`;
  3. không `Text` nào có `LocalizedText` lại được field kiểu `Text` của một `MonoBehaviour` trong scene trỏ tới. Duyệt mọi `MonoBehaviour` bằng `SerializedObject`, lặp property `ObjectReference`; mảng `Text[]` cũng tính. Thông báo ghi đường dẫn GameObject và tên field.

  Mỗi lớp `*SceneTests` thêm một test `<Scene>Scene_LocalizedTexts_KeysExistAndNotWrittenByViews`, mở scene từ file như các test cũ của lớp đó.
- [ ] **Step 2: Chạy EditMode** (scene cũ chưa có `LocalizedText`), kỳ vọng 4 test đỏ ở điều kiện 1.
- [ ] **Step 3: Dựng lại 4 scene** bằng `RebuildGachaScene`, `RebuildRestaurantScene`, `RebuildStageSelectScene`, `RebuildBattleScene` (lệnh dùng chung). Kiểm `git status` thấy 4 file `.unity` đổi.
- [ ] **Step 4: Chạy EditMode** (**777**) và PlayMode (12/12). Điều kiện 3 đỏ nghĩa là builder gắn nhầm `LocalizedText` vào chữ do view ghi: sửa builder ở task khu tương ứng rồi dựng lại.

## Phiên C

### Task 8: Tên tiếng Anh cho ScriptableObject

**Files:**
- Modify (thêm `[SerializeField] private string _displayNameEn;` ngay dưới `_displayName`, đổi `DisplayName => GameLanguage.Pick(_displayName, _displayNameEn)`):
  - `Characters/Scripts/CharacterDefinition.cs`, `SkillDefinition.cs`
  - `Gacha/Scripts/GachaBannerDefinition.cs` (khởi tạo `= "Standard Banner"`)
  - `Restaurant/Scripts/DishDefinition.cs`, `CustomerDefinition.cs`, `IncidentDefinition.cs`, `UpgradeDefinition.cs`
  - `Combat/Scripts/EnemyDefinition.cs`, `StageDefinition.cs`
- Modify: `Editor/ProjectSetup.cs`:
  - các bảng `CharacterTable`, `SkillTable`, `EnemySkillTable`, `DishTable`, `CustomerTable`, `IncidentTable`, `UpgradeTable`, `EnemyTable`, `StageTable` thêm cột `En`;
  - hàm tạo asset ghi `_displayNameEn`;
  - hàm mới `FillEnglishNames()` chạy trong Setup Project sau khi tạo asset.
- Modify (do Setup Project ghi, không sửa tay): các `.asset` ở `Characters/Data/`, `Gacha/Data/`, `Restaurant/Data/`, `Combat/Data/`
- Test: `Tests/LocalizedNameTests.cs` (mới)

**Interfaces:**
- Consumes: `GameLanguage.Pick` (Task 1).
- Produces: `internal static void FillEnglishNames()` trong `ProjectSetup`. Hàm duyệt từng bảng, nạp asset theo đường dẫn (sau mọi `NewScene`, theo luật L-002 ở code-style), và chỉ ghi `_displayNameEn` khi ô đang rỗng (không đè bản user đã sửa).
- Tên tiếng Anh của nhân vật và món:

  | Vi | En |
  |---|---|
  | Nem Chua | Nem Chua |
  | Chả Cốm | Cha Com |
  | Đậu Phụ | Dau Phu |
  | Mắm Tôm | Mam Tom |
  | Lòng Dồi | Long Doi |
  | Bún Lá | Bun La |
  | Bún đậu mắm tôm | Bun Dau Mam Tom |
  | Chả cốm | Cha Com |
  | Nem chua rán | Nem Chua Ran |

  Các tên khác 🔨 dịch nghĩa; màn số "1-1"…"1-5" giữ nguyên.

- [ ] **Step 1: Viết test đỏ** (`LocalizedNameTests`; nạp asset bằng `AssetDatabase.LoadAssetAtPath`):
  - `LocalizedNames_AllCatalogAssetsHaveEnglish`: mọi `CharacterDefinition` trong `CharacterCatalog` và chiêu của chúng; banner trong `GachaBannerCatalog`; món, khách, sự cố trong `RestaurantConfig`; nâng cấp trong `UpgradeCatalog`; màn trong `StageCatalog` và quái, chiêu quái trong các đợt. Đọc `_displayNameEn` qua `SerializedObject`, kỳ vọng không rỗng; thông báo ghi tên asset.
  - `LocalizedNames_CharactersAndDishesHaveNoDiacritics`: `_displayNameEn` của nhân vật và món không khớp `[À-ỹĐđ]`.
  - `DisplayName_FollowsLanguage_AndFallsBack`: `ScriptableObject.CreateInstance<DishDefinition>()`, ghi `_displayName = "Món"`, `_displayNameEn = "Dish"` qua `SerializedObject`. Kiểm `DisplayName` ra "Món" ở Vi, "Dish" ở En; xoá `_displayNameEn` thì ra "Món" ở En.
- [ ] **Step 2: Chạy EditMode**, kỳ vọng đỏ.
- [ ] **Step 3: Cài đặt**, rồi chạy Setup Project (lệnh dùng chung). Kiểm `git status`: các `.asset` SO đổi, có thêm dòng `_displayNameEn`; số khác không đổi (`git diff` chỉ thêm dòng).
- [ ] **Step 4: Chạy EditMode** (**780**) và PlayMode (12/12).

### Task 9: Nút đổi ngôn ngữ trong panel DEV

**Files:**
- Modify: `Assets/_Project/UI/Scripts/DevOverlay.cs` (`BuildSettings`, lưới hai cột; nút đặt ở ô trống kế tiếp dưới "Màn thử")
- Test: `Tests/DevSettingsTests.cs`, `Tests/PlayMode/SmokeLoopTests.cs`

**Interfaces:**
- Consumes: `LanguageSwitcher` (Task 2), `GameLanguage` (Task 1).
- Produces:
  - `public static string LanguageButtonLabel(Language current)`: Vi → `"Ngôn ngữ: Tiếng Việt"`, En → `"Language: English"`.
  - `public static bool ShouldReloadScene(string activeScene)`: `false` khi là `BattleView.BattleScene`.
  - `public static string SwitchLanguage()`: hàm mà nút gọi. Hàm lấy `LanguageSwitcher`, gọi `Set` với ngôn ngữ kia, rồi trả dòng trạng thái:
    - `""` khi đã nạp lại scene;
    - `"Ngôn ngữ đổi khi rời trận"` khi đang ở Battle;
    - `"Chưa lưu được, giữ ngôn ngữ cũ"` khi `Set` trả `false`.
  - Nút tên GameObject `LanguageButton`, không trùng nút nào.

- [ ] **Step 1: Viết test đỏ.**
  - EditMode `DevSettingsTests`:
    - `LanguageButtonLabel_EachLanguage`: hai nhãn đúng như trên.
    - `ShouldReloadScene_Battle_ReturnsFalse`: `"Battle"` → `false`; `"Restaurant"`, `"Gacha"`, `"StageSelect"` → `true`.
  - PlayMode `SmokeLoopTests.LanguageSwitch_ToEnglish_ReloadsPersistsAndResetFollowsDevice` (save tạm như các test cũ):
    1. Boot.
    2. Vào Restaurant bằng `SceneManager.LoadScene`.
    3. `DevOverlay.Build()`, bấm "Cài đặt" rồi `LanguageButton` qua `UiDriver`.
    4. Chờ scene nạp lại. Kiểm chữ của nút mở panel Nhân viên là bản En của key `restaurant.*` tương ứng (đọc qua `UiText`, không viết cứng), `session.Save.Language == "en"`, `GameLanguage.Current == En`.
    5. Gọi `ServiceLocator.Get<GameResetter>().Reset()`. Kiểm `Save.Language == ""` và `Current == GameLanguage.Resolve("", Application.systemLanguage)`.
    6. `TearDown` đặt `Current = Vi`.
- [ ] **Step 2: Chạy EditMode và PlayMode**, kỳ vọng đỏ.
- [ ] **Step 3: Cài đặt.** Nạp lại bằng `SceneManager.LoadScene(SceneManager.GetActiveScene().name)` sau khi đóng panel. Nhánh Battle có comment `// shortcut: Battle không vẽ lại khi đổi ngôn ngữ, làm khi hub có nút đổi ngôn ngữ cho người chơi.` Dòng trạng thái dùng `Text` trạng thái sẵn có của panel; panel chưa có thì thêm một `Text` dưới lưới.
- [ ] **Step 4: Chạy EditMode** (**782**) và PlayMode (**13**), gồm `DevOverlay_SettingsButton_DoesNotCoverGameButtons_AndMenuButtonClicks` (nút mới nằm trong panel, không ra ngoài màn).

### Task 10: Test chặn chữ tiếng Việt viết cứng

**Files:**
- Create: `Assets/_Project/Tests/HardcodedTextTests.cs`
- Modify (nếu test tìm ra chuỗi sót): file UI hoặc builder tương ứng và bảng khu của nó

**Interfaces:** Consumes `UiText` (Task 3).

- [ ] **Step 1: Viết test** `UiAndBuilders_HaveNoHardcodedVietnamese`:
  - Quét `Path.Combine(Application.dataPath, "_Project/UI/Scripts")` (đệ quy, `*.cs`) và `_Project/Editor/*SceneBuilder.cs`.
  - Bỏ qua file tên bắt đầu bằng `UiText` và file `DevOverlay.cs`.
  - Với mỗi dòng: bỏ phần từ `//` trở đi (khi `//` nằm ngoài chuỗi; chấp nhận cách thô là cắt ở `//` đầu tiên không nằm sau `"` lẻ), bỏ dòng có `Debug.Log`. Lấy các chuỗi `"…"` (cả `$"…"`) bằng regex, rồi tìm ký tự `[À-ỹĐđ]`.
  - Gom mọi chỗ vi phạm vào thông báo `file:dòng: chuỗi`, rồi `Assert.IsEmpty`.
  - Comment `// shortcut: chỉ bắt chuỗi có dấu; chữ Việt không dấu hay tiếng Anh viết cứng thì lọt, dựa vào review.`
  - Thêm test `Scanner_FindsVietnameseLiteral_IgnoresComment`. Test gọi hàm quét (tách thành `internal static List<string> FindViolations(string fileName, string[] lines)` trong chính lớp test) trên mảng dòng mẫu, gồm:
    - `var a = "Đóng";` → 1 vi phạm;
    - `// "Đóng"` → 0;
    - `Debug.LogError("Lỗi");` → 0.

    Không có test này thì test quét xanh cả khi regex hỏng (L-006).
- [ ] **Step 2: Chạy EditMode.** Test quét đỏ thì sửa từng chỗ sót (chuyển vào bảng) cho tới khi xanh. Chỗ cố ý giữ (nếu có) thì ghi lý do vào báo cáo cuối kênh, **không** thêm danh sách bỏ qua khi chưa hỏi 🎯.
- [ ] **Step 3: Chạy EditMode**, kỳ vọng xanh: **784**.

### Task 11: Ảnh tiếng Anh, docs, dừng cho user duyệt

**Files:**
- Modify: `docs/decisions/decisions.md` (thêm D-012), `docs/guides/code-style.md`, `docs/guides/unity-workflow.md`, `docs/project/structure.md`, `docs/roadmap.md`
- Không đổi code, trừ khi phải rút gọn bản dịch bị tràn (chỉ sửa `UiText.*.cs` hoặc cột `En` của `ProjectSetup`, rồi chạy lại test).

- [ ] **Step 1: Chụp 4 màn bằng tiếng Anh**, theo quy trình một lượt thử tay ở [unity-workflow, Công cụ thử tự động](../../guides/unity-workflow.md#công-cụ-thử-tự-động):
  1. `BackupSave` → `OpenBoot` → `EnableBackgroundPlay` → Play.
  2. Panel Cài đặt → `LanguageButton`.
  3. `Capture1080("bi-restaurant.png")`, `bi-staff.png` (panel Nhân viên mở), `bi-gacha.png` (trang tổng sau Quay 10), `bi-team.png` (popup chọn đội), `bi-battle.png` (trận).
  4. Thoát Play → `RestoreSave` → `RestoreBackgroundPlay` → `git status` (không có `ProjectSettings/`).
- [ ] **Step 2: Rà ảnh.** Chữ tràn ô hoặc bị cắt thì rút gọn bản tiếng Anh, chạy lại EditMode, rồi chụp lại màn đó.
- [ ] **Step 3: Sửa docs** theo spec mục 8:
  - D-012 "Song ngữ bằng bảng C# tự viết, ngôn ngữ lưu trong `PlayerSave`": bối cảnh, ba phương án, lý do, khi nào chuyển sang package.
  - code-style: luật mục 5.7 của spec.
  - unity-workflow: thay câu về `unity:localization`; thêm nút `LanguageButton` vào mục Công cụ thử tự động.
  - structure: `UI/Scripts/Text/`, `GameLanguage` và `LanguageSwitcher` trong Core, số test.
  - roadmap: dòng UI hub thêm "kèm nút đổi ngôn ngữ cho người chơi, dùng `LanguageSwitcher`"; dòng "Đang làm".
- [ ] **Step 4: Chạy EditMode và PlayMode lần cuối** (**784** và **13**, ghi số thật), rồi **dừng**. Báo cáo cuối kênh liệt kê đường dẫn 5 ảnh ở `D:/bdp_BunDauPeo/shots/` và 4 file bảng `UiText.*.cs` cho user duyệt. User duyệt xong thì 🎯 mở 🔍 review.
