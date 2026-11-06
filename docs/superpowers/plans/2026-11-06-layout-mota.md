# Sửa layout theo Mota: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (mặc định, native) hoặc superpowers:subagent-driven-development để làm kế hoạch này từng task (cách thực thi do 🎯 chọn, xem [agent-routing.md](../../guides/agent-routing.md)). Các bước dùng checkbox (`- [ ]`).

**Goal:** Sửa bố cục 13 chỗ lệch giữa các màn đang có (Battle, StageSelect, Gacha, Restaurant) và ảnh `Mota/`, không đổi luật chơi.

**Architecture:** Mọi thay đổi bố cục nằm trong bốn builder (`*SceneBuilder.cs`, assembly `Editor`) rồi dựng lại scene ở batch mode; view (assembly `UI`) chỉ đổi ở chỗ spec ghi: thêm `RewardRow`, 16 thẻ mỗi trang, thẻ HP trong popup tạm dừng, số HP quái, chữ sao. Test EditMode đọc lại scene đã lưu để kiểm khối trong màn và không đè nhau.

**Tech Stack:** Unity 6000.3.15f1, C#, uGUI (`Image`, `Text`), Input System 1.19.0, Test Framework, CLI `unity` 1.0.0-beta.15. Không thêm package, không thêm file ảnh.

**Spec:** [docs/superpowers/specs/2026-11-06-layout-mota-design.md](../specs/2026-11-06-layout-mota-design.md) (user duyệt thiết kế và bản viết 2026-11-06). Mã mục (K1, G4…) dưới đây là mã ở bảng mục 2 của spec; toạ độ ở spec mục 4.

## Global Constraints

- Không sửa/xoá tay `.meta`; không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`, `Không động tới/`. **Không sửa tay `.unity`**: scene chỉ đổi bằng builder rồi lệnh `Rebuild…Scene` ở batch mode.
- **Git: worker không commit, không push** (chỉ 📋 Thư ký, user chốt 2026-11-06). Nhánh `feature/layout-mota` cắt từ `origin/main` mới nhất sau khi nhánh `docs/layout-mota-spec` đã merge (chưa merge thì cắt từ chính nhánh docs đó). Mỗi task kết thúc bằng test xanh và tick checkbox; phiên xong thì nhắn 📋. **Không bao giờ để lẫn vào thay đổi:** `Assets/_Recovery/`, `Assets/InitTestScene*.unity`, `ProjectSettings/` (trừ khi task ghi rõ), ảnh chụp.
- UI: chỉ `Image` với sprite có sẵn (`UI/Skin/UISprite.psd`, `Knob.psd`, hoặc không gán sprite = vuông đặc), chữ `Text` font `LegacyRuntime.ttf`; không `RawImage`, không TextMeshPro, không file ảnh ([unity-workflow, UI và hình tạm](../../guides/unity-workflow.md#ui-và-hình-tạm)). Tên GameObject tiếng Anh, chữ hiện trên màn tiếng Việt.
- **Giữ nguyên tên mọi nút đang có** (`ConfirmButton`, `RetryButton`, `BackButton`, `TeamCard{i}`, `StartButton`, `MenuButton`, `AutoButton`, `SpeedButton`, `SkipButton`, `SingleButton`, `TenButton`, `HistoryButton`, `Node{i}`…): `SmokeLoopTests` và `PlaytestTools.ClickButton` bấm theo tên. Nút mới không được trùng tên nút cũ (`UiDriver` báo `Ambiguous`).
- Code: `PascalCase` cho class/method/property, field private `_camelCase`, comment chỉ nói vì sao; theo [code-style.md](../../guides/code-style.md). Toạ độ trong spec là khung 1920×1080 gốc trái trên; builder đổi sang anchor, lệch ±20 px được nếu test chứng minh không đè.
- Test: không `[TestCase]` (L-012); mỗi test tự hỏi "bố cục sai nào vẫn qua" (L-006); test đè nhau dùng khuôn ở `BattleScene_CardsAndButtonsInCardArea` (đè > 1 px cả hai chiều mới tính). Test vị trí cũ mâu thuẫn bố cục mới thì sửa theo spec, không xoá.
- Sau mỗi task: Console không có lỗi compile hay warning mới từ code dự án; `git status --short` chỉ có file của task (scene đã dựng lại là `.unity` của task đó).

### Lệnh dùng chung

Kiểm `unity status` **thành một lệnh riêng** trước mọi lệnh batch (L-020). Lệnh batch cần Editor **đóng**; Editor đang mở thì nhờ user đóng, không kill.

```bash
unity run . -- -executeMethod BunDauPeo.EditorTools.ProjectSetup.RebuildBattleScene -logFile ../bdp-rebuild.log
unity test . --mode EditMode --output ../bdp-tests.xml --timeout 600 -- -logFile ../bdp-tests.log
unity test . --mode PlayMode --output ../bdp-playtests.xml --timeout 900 -- -logFile ../bdp-playtests.log
```

Thay `RebuildBattleScene` bằng `RebuildStageSelectScene`, `RebuildRestaurantScene` hoặc `RebuildGachaScene` (có từ Task 1) theo scene của task. Sau `unity run` kiểm `git status` thấy `.unity` đổi trước khi tin log (L-020). Đọc `../bdp-tests.xml` (`failed="0"`, `total="N"`), không dựa mỗi mã thoát (`0` đạt, `8` test đỏ, `6` lỗi khác kể cả compile).

Số kỳ vọng: nền **720 EditMode + 12 PlayMode**; mỗi task ghi số mới (+ thêm, − xoá). Hết kế hoạch: **737 EditMode, 12 PlayMode**. Ghi tổng cộng dồn sau mỗi task vào checkbox.

## Review Focus

1. **Nút đang tắt bị nút DEV che** (G4): nút chỉ hiện trong luồng (Bỏ qua, nút popup, nút kết quả) phải được `AssertSettingsButtonClear` xét. Test ở Task 3.
2. **Trùng tên nút**: thẻ HP trong popup tạm dừng dựng bằng `BuildCard` phải tên `PauseCard{i}`, không `Card{i}`; nếu trùng, `ClickButton("Card0")` báo `Ambiguous` và smoke test đỏ. Test ở Task 6 (`BattleScene_ButtonNamesUnique`).
3. **Hơn 16 nhân vật**: trang 2 của chọn đội vẫn hiện đúng người thứ 17 trở đi, nút `>` bật. Test ở Task 5 (`TeamSelect_SeventeenCharacters_SecondPageShowsSeventeenth`).
4. **Trận DEV không có thưởng** (`NotPaid`): `RewardRow` tắt hẳn, dòng "Trận thử, không có thưởng" hiện đúng chỗ hàng ô, không có ô rỗng. Test ở Task 2 (`BattleResult_NotPaid_HidesRewardRow`).
5. **Tên dài trong ô 150 px** (chip chọn đội, thẻ tạm dừng, chân dung chọn đội): chữ không tràn ra ngoài ô; dùng `resizeTextForBestFit` (min 16) và `HorizontalWrapMode.Wrap`. Test ở Task 5 (`TeamSelect_CardTexts_FitInsideCard`: mọi `Text` con nằm trong rect của thẻ và bật best fit).

## Điều hướng agent

### Chia session

| Phiên | Task | Dừng playtest |
|---|---|---|
| A | 1–4 (K1, K2, G4, S1 và nền chung) | sau Task 4: chụp kết quả thắng/thua, popup màn, mở 10 lượt Gacha |
| B | 5–8 (T1, P1, B1, B2) | sau Task 8: chụp chọn đội, trận, tạm dừng |
| C | 9–12 (G1, G2, G3, M1, R1) và hoàn tất đợt | sau Task 12: chụp banner, mở thẻ, trang tổng, bản đồ, quán |

Phiên sau mở khi phiên trước đã báo xong, 📋 đã commit và user đã xem ảnh.

### Task nào chạm gì

| Task | Scene dựng lại | Code | Song song |
|---|---|---|---|
| 1 Nền: `RewardRow`, `RebuildGachaScene` | Gacha (dựng lại không đổi bố cục) | `UI`, `Editor` | không |
| 2 K1 K2 kết quả trận | Battle | `UI`, `Editor` | không |
| 3 G4 nút Bỏ qua | Gacha | `Editor`, PlayMode test | không |
| 4 S1 popup màn | StageSelect | `UI`, `Editor` | không |
| 5 T1 chọn đội | StageSelect | `UI`, `Editor` | không |
| 6 P1 tạm dừng | Battle | `UI`, `Editor` | không |
| 7 B1 màn trận | Battle | `Editor`, docs | không |
| 8 B2 số HP quái | Battle | `UI`, `Editor` | không |
| 9 G1 banner | Gacha | `Editor`, `UI` (`GachaViewText`) | không |
| 10 G2 G3 mở thẻ, trang tổng | Gacha | `Editor`, `UI` | không |
| 11 M1 bản đồ | StageSelect | `UI`, `Editor` | không |
| 12 R1 quán | Restaurant | `Editor` | không |

Mọi task đều dựng lại một scene, nên **không task nào chạy subagent song song** với task khác. Trong một task, phần thuần code (hàm tĩnh ở `CombatViewText`/`GachaViewText` và test của nó) có thể giao subagent viết; subagent không chạy Unity, không commit; kênh 🔨 chạy `unity test` một lần.

### Bàn giao

Mỗi task xong: tick checkbox kèm tổng test. Đầu phiên cập nhật dòng "Đang làm" trong [roadmap.md](../../roadmap.md). Kết phiên: chụp ảnh của lần dừng playtest, rồi khối "Báo cáo cuối kênh" của skill worker gửi 📋.

## File Structure

```
Assets/_Project/
  UI/Scripts/RewardRow.cs              mới: hàng ô thưởng (T1)
  UI/Scripts/CombatViewText.cs         sửa: CurrencyColor, CurrencyCell (T1); xoá ResultRewards (T2), PauseHpLine (T6)
  UI/Scripts/BattleResultView.cs       sửa: RewardRow (T2)
  UI/Scripts/StageInfoPopup.cs         sửa: RewardRow thay _rewardHeader/_rewardLine (T4)
  UI/Scripts/TeamSelectPopup.cs        sửa: CardsPerPage 16 (T5)
  UI/Scripts/TeamCardView.cs           sửa: dấu tích (T5)
  UI/Scripts/BattlePausePopup.cs       sửa: thẻ BattleCardView, tên màn (T6)
  UI/Scripts/BattleUnitView.cs         sửa: _hpText tuỳ chọn (T8)
  UI/Scripts/GachaViewText.cs          sửa: PullLabel hai dòng (T9), StarsText bằng ★ (T10)
  UI/Scripts/StageMapView.cs           sửa: NodeCenter (T11)
  Editor/ProjectSetup.cs               sửa: RebuildGachaScene + menu (T1)
  Editor/GachaSceneBuilder.cs          sửa: BuildRewardRow (T1), Skip (T3), banner (T9), reveal/tổng (T10)
  Editor/BattleSceneBuilder.cs         sửa: kết quả (T2), tạm dừng (T6), thanh trên/thẻ/cột phải (T7), HpText (T8)
  Editor/StageSelectSceneBuilder.cs    sửa: popup màn (T4), chọn đội (T5), ổ khoá (T11)
  Editor/RestaurantSceneBuilder.cs     sửa: thẻ tiền, InfoBar, MessageRoot (T12)
  Scenes/{Battle,StageSelect,Gacha,Restaurant}.unity   dựng lại bằng builder
  Tests/RewardRowTests.cs              mới (T1)
  Tests/{CombatViewText,GachaViewText,BattleScene,StageSelectScene,GachaScene,RestaurantScene}Tests.cs   sửa/thêm
  Tests/PlayMode/SmokeLoopTests.cs     sửa: AssertSettingsButtonClear (T3)
docs/  spec Combat 5.4 (T7), unity-workflow (T1), lesson (T3), roadmap (mỗi phiên)
```

---

## Giai đoạn A: kết quả trận, nút Bỏ qua, popup màn (Phiên A)

### Task 1: Nền chung — `RewardRow` và `RebuildGachaScene`

**Files:**
- Create: `Assets/_Project/UI/Scripts/RewardRow.cs`, `Assets/_Project/Tests/RewardRowTests.cs`
- Modify: `Assets/_Project/UI/Scripts/CombatViewText.cs`, `Assets/_Project/Editor/GachaSceneBuilder.cs`, `Assets/_Project/Editor/ProjectSetup.cs`, `Assets/_Project/Tests/CombatViewTextTests.cs`, `docs/guides/unity-workflow.md`
- Scene: `Gacha.unity` (dựng lại, chưa đổi bố cục)

**Interfaces:**
- Produces: `public class RewardRow : MonoBehaviour` với `[SerializeField] Text _label; Image[] _cells; Text[] _amounts;` (đúng 4 ô), `public const int MaxCells = 4;`, `public void Show(IReadOnlyList<CurrencyAmount> lines, string label)`, `public void Hide()`.
- Produces: `CombatViewText.CurrencyColor(CurrencyType type) -> Color` (Coin vàng `(0.95, 0.80, 0.25)`, Material nâu `(0.65, 0.45, 0.28)`, Gem xanh lam `(0.35, 0.65, 0.95)`), `CombatViewText.CurrencyCell(CurrencyAmount line) -> string` ("30 Material").
- Produces: `GachaSceneBuilder.BuildRewardRow(RectTransform parent, string name, Font font, Vector2 anchor, Vector2 position) -> RewardRow` (`internal static`, ô 96×96 cách 24, nhãn 200 px bên trái, số 28 px dưới ô).
- Produces: `ProjectSetup.RebuildGachaScene()` + `[MenuItem("Tools/BunDauPeo/Rebuild Gacha Scene")]`, gọi `GachaSceneBuilder.Create(GachaScenePath)`.

- [ ] **Step 1: Dựng lại Gacha trước khi đổi gì** (rủi ro spec mục 8): thêm `RebuildGachaScene`, Editor đóng, chạy lệnh dựng lại. Chạy `GachaSceneTests` và `GachaRevealAssetTests` (cả bộ EditMode). Expected: 720/720 xanh; `git diff --stat Assets/_Project/Scenes/Gacha.unity` chỉ đổi fileID/thứ tự, không mất tham chiếu (test nối dây xanh là bằng chứng). Đỏ thì dừng, báo 🎯: scene có chỉnh tay mà builder không có.
- [ ] **Step 2: Viết test đỏ** trong `CombatViewTextTests`: `CurrencyColor_DistinctPerCurrency` (ba màu khác nhau từng đôi), `CurrencyCell_ShowsAmountAndName` (`"30 Material"`, `"150 Gem"`, `"500 Coin"`). Trong `RewardRowTests` (dựng `RewardRow` bằng `BuildRewardRow` trên Canvas tạm, huỷ ở TearDown): `RewardRow_Show_TwoLines_TwoCellsWithAmountsAndColors` (ô 0–1 bật, ô 2–3 tắt, `_amounts[1].text == "150 Gem"`, màu ô bằng `CurrencyColor`, nhãn đúng chuỗi truyền vào), `RewardRow_Show_FiveLines_ShowsFirstFour` (`shortcut:` ở spec 4.1).
- [ ] **Step 3: Chạy test, thấy đỏ** vì chưa có kiểu/hàm.
- [ ] **Step 4: Cài** `RewardRow`, hai hàm `CombatViewText`, `BuildRewardRow`. `Show` với `lines.Count == 0` bật nhãn, tắt mọi ô; `Hide` tắt cả object.
- [ ] **Step 5: Chạy EditMode.** Expected: 723/723 xanh (+3).
- [ ] **Step 6: Docs**: dòng "Dựng lại scene bằng builder" ở [unity-workflow.md](../../guides/unity-workflow.md) thêm `GachaSceneBuilder` và `RebuildGachaScene`.

### Task 2: K1, K2 — Kết quả trận

**Files:**
- Modify: `Assets/_Project/Editor/BattleSceneBuilder.cs` (`BuildResultView`), `Assets/_Project/UI/Scripts/BattleResultView.cs`, `Assets/_Project/UI/Scripts/CombatViewText.cs`, `Assets/_Project/Tests/BattleSceneTests.cs`, `Assets/_Project/Tests/CombatViewTextTests.cs`
- Scene: `Battle.unity`

**Interfaces:**
- Consumes: `RewardRow`, `BuildRewardRow` (Task 1).
- Produces: `BattleResultView` field `[SerializeField] RewardRow _rewardRow;` thay `_rewardText` cho thưởng; `_rewardText` giữ để hiện gợi ý thua và dòng "Trận thử…". Object `ResultBox` (1400×760, `CardColor` alpha 1) là cha của mọi chữ và nút kết quả.

- [ ] **Step 1: Viết test đỏ** trong `BattleSceneTests`: `BattleScene_ResultContentInsideBox_ButtonsApart` — mọi `Text`, `Button` và `RewardRow` con của `ResultRoot` (trừ chính nền phủ) có rect nằm trong rect `ResultBox`; `ConfirmButton`, `RetryButton`, `BackButton` không đè nhau; `BackButton` ở nửa trái hộp, `RetryButton` ở nửa phải; `_rewardRow` nối dây. Trong một test view (khuôn test `BattleResultView` hiện có, hoặc PlayMode nếu cần `CombatService` thật; chọn khuôn đang dùng trong `BattleSceneTests`/`BattleSessionTests`): `BattleResult_NotPaid_HidesRewardRow` (Review Focus 4).
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Sửa builder** theo spec 4.2 (banner đỉnh hộp; thắng: `RewardRow` giữa, thể lực, cảnh báo, Xác nhận đáy giữa; thua: gợi ý và thể lực canh trái, Về bản đồ dưới-trái, Thử lại dưới-phải, dòng lý do trên Thử lại).
- [ ] **Step 4: Sửa `BattleResultView.Show`**: `Success` → `_rewardRow.Show(result.Rewards, result.FirstClear ? "Lần đầu" : "")`, `_rewardText` rỗng; `NotPaid`/khác → `_rewardRow.Hide()`, `_rewardText = NoRewardLine(...)`; thua → `_rewardRow.Hide()`. Thêm `_rewardRow` vào `HasAllReferences`. Xoá `CombatViewText.ResultRewards` và test `ResultRewards_FirstClearTagOnlyOnFirst`.
- [ ] **Step 5: Dựng lại Battle, chạy EditMode.** Expected: 724/724 (+2 −1).

### Task 3: G4 — Nút Bỏ qua và test nút DEV

**Files:**
- Modify: `Assets/_Project/Editor/GachaSceneBuilder.cs` (`SkipButton`), `Assets/_Project/Tests/PlayMode/SmokeLoopTests.cs` (`AssertSettingsButtonClear`), `Assets/_Project/Tests/GachaSceneTests.cs`, `docs/lessons/lesson.md`
- Scene: `Gacha.unity`

- [ ] **Step 1: Test đỏ trước**: trong `AssertSettingsButtonClear` đổi `FindObjectsInactive.Exclude` → `Include` (nút tắt vẫn có `RectTransform` đúng chỗ). Chạy PlayMode. Expected: `DevOverlay_SettingsButton_DoesNotCoverGameButtons_AndMenuButtonClicks` **đỏ** ở bước "D2 Gacha" với thông điệp nhắc `SkipButton`. Đỏ ở nút khác thì ghi lại, sửa nút đó trong cùng task theo cùng luật (không dời nút Cài đặt).
- [ ] **Step 2: Test EditMode đỏ** `GachaScene_SkipButtonTopRight`: rect `SkipButton` (khung 1920×1080) có `xMin >= 1500` và đỉnh cách mép trên ≤ 140.
- [ ] **Step 3: Dời `SkipButton`** lên góc trên phải (`TopRight`, `(-40, -40)`, 300×90).
- [ ] **Step 4: Dựng lại Gacha, chạy EditMode rồi PlayMode.** Expected: 725/725 EditMode (+1), 12/12 PlayMode.
- [ ] **Step 5: Lesson** (#ui #process) ở đầu "Đang hiệu lực": test đè nút chỉ xét nút đang bật bỏ sót nút chỉ hiện trong luồng (nút Bỏ qua Gacha, L-025 lặp lại); luật: kiểm đè nút luôn gồm nút tắt. L-025 lặp lần hai nên đánh dấu chờ 🔍 nâng thành luật.

### Task 4: S1 — Popup màn

**Files:**
- Modify: `Assets/_Project/Editor/StageSelectSceneBuilder.cs` (`BuildInfoPopup`), `Assets/_Project/UI/Scripts/StageInfoPopup.cs`, `Assets/_Project/Tests/StageSelectSceneTests.cs`
- Scene: `StageSelect.unity`

**Interfaces:**
- Consumes: `RewardRow`, `BuildRewardRow` (Task 1).
- Produces: `StageInfoPopup` field `[SerializeField] RewardRow _rewardRow;` thay `_rewardHeader` và `_rewardLine`; `Refresh` gọi `_rewardRow.Show(firstClear ? stage.FirstClearReward : stage.RepeatReward, CombatViewText.RewardHeader(firstClear))`.

- [ ] **Step 1: Test đỏ** `StageSelectScene_InfoPopup_TwoColumns_InsideBox`: `InfoBox` 1600×820; mọi `Text`/`Button`/`RewardRow` con nằm trong hộp; `Title` canh trái ở 1/3 trái hộp; ô quái và `RewardRow` ở 2/3 trái; `StaminaText`, `CancelButton`, `ChallengeButton` ở 1/3 phải; hai nút không đè nhau và cùng hàng (tâm y lệch ≤ 5).
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Sửa builder và view** theo spec 4.2 S1. `CombatViewText.RewardLine` còn dùng ở chỗ khác thì giữ, không còn thì xoá cùng test của nó (ghi số test).
- [ ] **Step 4: Dựng lại StageSelect, chạy EditMode và PlayMode.** Expected: 726/726 (+1, trừ đi nếu xoá `RewardLine`), 12/12.
- [ ] **Step 5: Dừng playtest Phiên A**: quy trình "Công cụ thử tự động" ([unity-workflow](../../guides/unity-workflow.md#công-cụ-thử-tự-động)), chụp `Capture1080` kết quả thắng, kết quả thua (tắt AUTO rồi Bỏ cuộc), popup màn 1-1, mở 10 lượt Gacha (lúc có nút Bỏ qua). `RestoreSave`, `git status` sạch `ProjectSettings/`. Gửi user đường dẫn ảnh trong báo cáo.

## Giai đoạn B: chọn đội, tạm dừng, màn trận (Phiên B)

### Task 5: T1 — Chọn đội

**Files:**
- Modify: `Assets/_Project/Editor/StageSelectSceneBuilder.cs` (`BuildTeamPopup`, `BuildTeamCard`), `Assets/_Project/UI/Scripts/TeamSelectPopup.cs`, `Assets/_Project/UI/Scripts/TeamCardView.cs`, `Assets/_Project/Tests/StageSelectSceneTests.cs` (và test phân trang đang có của `TeamSelectPopup`/`TeamSelectRows`)
- Scene: `StageSelect.unity`

**Interfaces:**
- Produces: `TeamSelectPopup.CardsPerPage = 16`; `TeamCardView` thêm `[SerializeField] GameObject _check;` bật khi `row.InTeam`.

- [ ] **Step 1: Test đỏ**: `StageSelectScene_TeamPopup_SixteenCards_NoOverlap_ButtonsBesideChips` (16 `TeamCard{i}` 150×190 trong `TeamBox`, 8 cột × 2 hàng, không đè; 5 chip 150×120 cùng hàng; `CancelButton`, `StartButton` cùng hàng chip, bên phải chip cuối, không đè nhau); `TeamSelect_CardTexts_FitInsideCard` (Review Focus 5); `TeamSelect_SeventeenCharacters_SecondPageShowsSeventeenth` (Review Focus 3, khuôn test phân trang đang có). Sửa test phân trang cũ đang giả định 12.
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Sửa builder** theo spec 4.3 T1 (chân dung vuông, sao trong ô, nhãn "Quán: <vai>" thu gọn, dấu tích dựng như `CheckIcon` của nút bản đồ; chữ bật `resizeTextForBestFit` min 16, wrap). Sửa `CardsPerPage`, `TeamCardView.Bind` bật `_check`, thêm `_check` vào kiểm tham chiếu.
- [ ] **Step 4: Dựng lại StageSelect, chạy EditMode và PlayMode** (smoke test bấm `TeamCard0…4`). Expected: 729/729 (+3), 12/12.

### Task 6: P1 — Tạm dừng

**Files:**
- Modify: `Assets/_Project/Editor/BattleSceneBuilder.cs` (`BuildPausePopup`, `BuildCard`), `Assets/_Project/UI/Scripts/BattlePausePopup.cs`, `Assets/_Project/UI/Scripts/CombatViewText.cs`, `Assets/_Project/Tests/BattleSceneTests.cs`, `Assets/_Project/Tests/CombatViewTextTests.cs`
- Scene: `Battle.unity`

**Interfaces:**
- Produces: `BuildCard(RectTransform parent, string name, Vector2 anchor, Vector2 position, Sprite sprite, Font font) -> BattleCardView` (thẻ dưới trận gọi với `canvas, $"Card{i}", BottomLeft, …`); `BattlePausePopup` field `[SerializeField] BattleCardView[] _cards;` (5, tên `PauseCard{i}`) thay `_hpLines`, `[SerializeField] Text _stageName;`. `Open(BattleSession session)` không đổi chữ ký; `maxEnergy` lấy như `BattleView` đang tính khi gọi `Refresh`.

- [ ] **Step 1: Test đỏ**: `BattleScene_PauseCards_InsideBox` (`PauseBox` 1100×760, 5 `PauseCard{i}` trong hộp, cùng hàng, không đè; nút của thẻ `interactable == false`; `_stageName` nối dây; popup tắt sẵn, nền chặn chạm như test cũ); `BattleScene_ButtonNamesUnique` (mọi `Button` trong scene, kể cả tắt, tên khác nhau từng đôi — Review Focus 2).
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Sửa** builder và `BattlePausePopup.Open` (`_stageName.text = session.Stage.DisplayName`; thẻ `i < Allies.Count` thì `Bind` + `Refresh(unit.Hp, maxEnergy, false)`, còn lại `Hide`). Xoá `CombatViewText.PauseHpLine` và test `PauseHpLine_AliveAndFallen`.
- [ ] **Step 4: Dựng lại Battle, chạy EditMode và PlayMode.** Expected: 730/730 (+2 −1), 12/12.

### Task 7: B1 — Bố cục màn trận

**Files:**
- Modify: `Assets/_Project/Editor/BattleSceneBuilder.cs` (`BuildTopBar`, vị trí thẻ, `BuildActionPanel` cho `_turnLine`), `Assets/_Project/Tests/BattleSceneTests.cs`, `docs/superpowers/specs/2026-11-02-combat-design.md` (mục 5.4)
- Scene: `Battle.unity`

- [ ] **Step 1: Sửa test cho đỏ** theo spec 4.3 B1: `BattleScene_TopBarWired_InsideCanvas` — `WaveText` và ô lượt ở góc trên trái (x < 700, đáy ≤ 180); `MenuButton` góc trên phải (đáy ≤ 180); `AutoButton`, `SpeedButton` có `xMin >= 1720`, nằm trong dải y 540–820, không đè hộp nào của `BattleLayout.EnemyRect(slot, boss)` với mọi slot và boss; không đè ba nút chiêu. `BattleScene_CardsAndButtonsInCardArea` — thẻ i có `xMin == 480 + i × 192` (±2), không đè nút chiêu; `TurnLine` ở trên trái (đáy ≤ 200). `BattleLayout` và test của nó không đổi.
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Sửa builder** theo toạ độ spec 4.3 B1. AUTO/x2 neo `BottomRight` (cùng neo ba nút chiêu) để ở màn rộng hơn 16:9 cột này đi cùng bảng chiêu.
- [ ] **Step 4: Dựng lại Battle, chạy EditMode và PlayMode.** Expected: 730/730, 12/12.
- [ ] **Step 5: Docs**: phác thảo spec Combat 5.4 vẽ lại theo bố cục mới, thêm câu trỏ về spec layout; mục 5.3, 5.5 thêm một dòng trỏ về spec layout.

### Task 8: B2 — Số HP quái

**Files:**
- Modify: `Assets/_Project/Editor/BattleSceneBuilder.cs` (`BuildUnit`), `Assets/_Project/UI/Scripts/BattleUnitView.cs`, `Assets/_Project/Tests/BattleSceneTests.cs`, `Assets/_Project/Tests/BattleUnitTests.cs`
- Scene: `Battle.unity`

**Interfaces:**
- Produces: `BattleUnitView` field `[SerializeField] Text _hpText;` (chỉ quái có, đồng minh để `null`); chữ `CombatViewText.UnitHp(ShownHp, Unit.MaxHp)` mỗi khi `ShownHp` đổi và trong `Bind`.

- [ ] **Step 1: Test đỏ**: `BattleScene_OnlyEnemiesHaveHpText` (`Enemy{i}` có `HpText` nối dây, nằm dưới `Name`, trong khung 1920×1080 khi đặt ở `EnemyRect`; `Ally{i}` không có); `BattleUnitView_EnemyHpText_FollowsDelta` (khuôn `BattleUnitTests`: `Bind` quái 1000 HP, `ApplyDelta(-200)`, `Tick` tới hết hoạt hình, chữ == `UnitHp(800, 1000)`).
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Cài** (`BuildUnit` thêm `HpText` 22 px chỉ khi `!ally`; view ghi khi `_hpText != null`; kiểm tham chiếu không đòi `_hpText`).
- [ ] **Step 4: Dựng lại Battle, chạy EditMode và PlayMode.** Expected: 732/732 (+2), 12/12. Lệch kỳ vọng ở bất kỳ task nào thì ghi lý do cạnh số.
- [ ] **Step 5: Dừng playtest Phiên B**: chụp chọn đội (đủ 5 người), màn trận lúc tới lượt phe ta, popup tạm dừng.

## Giai đoạn C: Gacha, bản đồ, quán (Phiên C)

Số test cộng dồn tiếp từ 732.

### Task 9: G1 — Trang banner

**Files:**
- Modify: `Assets/_Project/Editor/GachaSceneBuilder.cs` (trang roll), `Assets/_Project/UI/Scripts/GachaViewText.cs` (`PullLabel`), `Assets/_Project/Tests/GachaSceneTests.cs`, `Assets/_Project/Tests/GachaViewTextTests.cs`
- Scene: `Gacha.unity`

- [ ] **Step 1: Test đỏ**: `GachaScene_BannerPageBlocksInCorners` (`BannerPanel` chứa `BannerNameText` và `PityText`, ở góc trên trái; `GemText` góc trên phải; `BannerArt` ở giữa, `raycastTarget == false`; `SingleButton`, `TenButton` 300×160 ở góc dưới phải, `TenButton` bên phải `SingleButton`; `HistoryButton` trên hai nút quay; mọi khối không đè nhau và không đè vùng nút Cài đặt DEV 150×60 ở góc dưới phải); `PullLabel_TwoLines` (`"Quay 1\n100 Gem"`).
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Sửa builder** theo spec 4.4 G1 (giữ tên `GemText`, `BannerNameText`, `PityText`, `MessageText`); `PullLabel` trả `$"Quay {count}\n{CostText(cost)}"`.
- [ ] **Step 4: Dựng lại Gacha, chạy EditMode và PlayMode.** Expected: 734/734 (+2), 12/12.

### Task 10: G2, G3 — Mở từng thẻ và trang tổng

**Files:**
- Modify: `Assets/_Project/Editor/GachaSceneBuilder.cs` (reveal, trang tổng), `Assets/_Project/UI/Scripts/GachaViewText.cs` (`StarsText`), `Assets/_Project/Tests/GachaSceneTests.cs`, `Assets/_Project/Tests/GachaViewTextTests.cs`
- Scene: `Gacha.unity`

- [ ] **Step 1: Test đỏ**: sửa `StarsText_ShowsCount` thành `AreEqual("★★★", StarsText(3))`; `GachaScene_RevealNameBottomRight_TotalInsidePanel` (`RevealPanel` 1800×1000, `Portrait` 760×760 tâm ở nửa trái, `NameText`/`StarsText` canh phải ở 1/4 dưới-phải panel; `TotalPanel` 1300×640, `Grid` nằm trong panel, ô 200×220; `OkButton` dưới panel, không đè panel). `GachaScene_TotalGridIsFiveColumns` giữ, sửa cỡ ô nếu nó kiểm cỡ.
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Sửa** builder theo spec 4.4 G2, G3 (tiêu đề "Kết quả quay"); `StarsText` trả `new string('★', stars)`.
- [ ] **Step 4: Dựng lại Gacha, chạy EditMode và PlayMode.** Expected: 735/735 (+1), 12/12.

### Task 11: M1 — Bản đồ

**Files:**
- Modify: `Assets/_Project/Editor/StageSelectSceneBuilder.cs` (`BuildNode`), `Assets/_Project/UI/Scripts/StageMapView.cs` (`NodeCenter`), `Assets/_Project/Tests/StageSelectSceneTests.cs`
- Scene: `StageSelect.unity`

- [ ] **Step 1: Test đỏ**: sửa `NodeCenters_SevenNodes_InsideMapArea_NoOverlap` theo `NodeCenter(i) = (240 + 240 i, i chẵn ? 720 : 400)` (vẫn trong vùng bản đồ dưới thanh trên, không đè nhau, "NEXT" đặt trên mỗi nút không ra khỏi màn và không đè thanh trên); `StageSelectScene_LockIconOnTopOfNode` (`LockIcon` tâm cách tâm nút (0, +60) ±4, thân khoá màu `Gold`, rect khoá không đè rect nút bên cạnh).
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Sửa** `NodeCenter` và `BuildNode` theo spec 4.4 M1.
- [ ] **Step 4: Dựng lại StageSelect, chạy EditMode và PlayMode.** Expected: 736/736 (+1), 12/12.

### Task 12: R1 — Quán

**Files:**
- Modify: `Assets/_Project/Editor/RestaurantSceneBuilder.cs` (thẻ tiền, `InfoBar`, `MessageRoot`), `Assets/_Project/Tests/RestaurantSceneTests.cs`
- Scene: `Restaurant.unity`

- [ ] **Step 1: Test đỏ** `RestaurantScene_InfoBarClearOfRoomAndColumn`: ba thẻ tiền rộng 260; `InfoBar` ở hàng trên (đỉnh ≤ 30 từ mép trên), bên phải thẻ tiền cuối, `xMax` < `xMin` của cột nút phải; `InfoBar` và `MessageRoot` không đè hộp sàn và tường phòng (dùng rect `Room` và các con tường như `RestaurantScene_LandmarksAreInsideFloor` đang lấy) và không đè cột phải.
- [ ] **Step 2: Chạy, thấy đỏ.**
- [ ] **Step 3: Sửa builder** theo spec 4.4 R1. `RestaurantScene_HudAndColumnInsideCanvas` và test popup mở từ `InfoBar` phải vẫn xanh.
- [ ] **Step 4: Dựng lại Restaurant, chạy EditMode và PlayMode.** Expected: 737/737 (+1), 12/12.
- [ ] **Step 5: Dừng playtest Phiên C**: chụp banner, mở một thẻ, trang tổng 10 lượt, bản đồ (có khoá và đã clear), quán.

## Hoàn tất đợt

- [ ] Tổng test ghi ở checkbox cuối khớp `../bdp-tests.xml` và `../bdp-playtests.xml`.
- [ ] [roadmap.md](../../roadmap.md): mục "Sửa layout theo Mota" chuyển xuống "Đã xong" kèm số test, link spec và kế hoạch; dòng "Đang làm" dọn.
- [ ] Báo cáo cuối kênh gửi 📋; 🎯 mở 🔍 review cuối nhánh (mức theo [agent-routing](../../guides/agent-routing.md); không đụng tiền/save nên không cần xhigh).
