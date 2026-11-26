# Combat nhiều chương/chapter: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (mặc định, native) hoặc superpowers:subagent-driven-development để làm kế hoạch này từng task (cách thực thi do 🧭 Leader chọn, xem [agent-routing.md](../../studio/agent-routing.md)). Các bước dùng cú pháp checkbox (`- [ ]`) để theo dõi; tick khi xong.

**Goal:** Combat chứa được nhiều chapter: `ChapterDefinition`/`ChapterCatalog` ghép thành `StageCatalog` phẳng, Bản đồ có banner ◀ ▶ "Chương X · Chapter Y" ở góc trái dưới để đổi chapter, nút thủ lĩnh riêng, chapter khoá "???", về từ Trận mở đúng chapter; tiến độ suy ra từ `ClearedStages` (không field save mới, server không đổi). Mức tối thiểu: 3 chapter seed dùng lại 7 màn có sẵn.

**Architecture:** Dữ liệu cố định (ScriptableObject do `ProjectSetup` dựng) + `ChapterService` thuần C# đọc `StageProgressService` mỗi lần (như `StageProgressService` đọc `PlayerSave`); `StageMapView` dựng nút theo chapter đang xem từ một pool.

**Tech Stack:** Unity 6000.3.15f1, C#, uGUI (hình có sẵn), Test Framework (EditMode + PlayMode), CLI `unity`.

**Spec:** [2026-11-26-multi-chapter-design.md](../specs/2026-11-26-multi-chapter-design.md). "Mục N" là mục của spec.

## Trước khi bắt đầu

1. **Thời tiết phải merge vào `main` trước** (hàng khoá do CEO/user: Thời tiết → **Nhiều chương** → Cốt truyện → Vẽ lại quán). Nhánh `feature/multi-chapter` cắt từ `origin/main` sau đó. Cốt truyện làm **sau** kế hoạch này (cùng sửa `ProjectSetup` `StageTable` và `StageDefinition`; xem mục 8 spec).
2. Cần **khoá Unity** từ 👑 CEO (test, dựng scene, playtest). Chung scene Hub/Bản đồ: không phiên 🔨 nào khác sửa trong lúc làm.
3. Không package mới, không tài khoản, không deploy server.
4. Model: Task 1 dùng **Opus 5.5 high** (luật mở khoá, `Validate`); Task 2–5 Sonnet 5.5 high ([agent-routing.md](../../studio/agent-routing.md)).
5. Tìm bài học trước khi sửa: `Grep "#ui" docs/lessons/lesson.md -A 4`, `#script`, `#save`, `#scene-prefab`.

## Global Constraints

- Không sửa/xoá tay `.meta`; không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`, `_Scratch/`. **Không sửa tay `.unity`/`.prefab`/`.asset`**: asset dựng bằng `ProjectSetup`, scene bằng builder rồi Rebuild.
- **Git: không commit, không push** (chỉ 📋 Thư ký). Không có bước commit trong kế hoạch.
- **D-005:** không thêm field vào `PlayerSave`; tiến độ chapter suy ra từ `ClearedStages`; chapter đang xem chỉ ở bộ nhớ view.
- **Server không đổi.** Không đụng `Rules`, `GameData`, `ServerModule`, `StageProgressService`, `StaminaService`, `BattleSession`. `StageData` không có `IsBoss`.
- **`StageDefinition._isBoss` và `CombatConfig._chapters` thêm CUỐI** (giá trị lưu trong asset chỉ thêm ở cuối).
- Hình chỉ dùng hình có sẵn của Unity; **không thêm file ảnh**. Object chưa có hình đặt tên `<Tên>_PH_<rộng>x<cao>` (tên theo mục 6.1 spec).
- Chữ qua `UiText` (khu `chapter`, hai ngôn ngữ). Tên/tiêu đề chapter trong asset (`GameLanguage.Pick`).
- **`Update` của view không cấp phát** (L-044): không LINQ, không `foreach` qua interface, không `new`, không chuỗi mỗi khung.
- Test chạm dùng raycast thật (`UiDriver.Click`), không `ExecuteEvents` thẳng lên nút (L-036).
- Chạy test: `unity test . --mode EditMode --output <file>`; PlayMode: `--mode PlayMode`.

## Chia phiên 🔨

- **Phiên 1: Task 1–2** (dữ liệu, luật, `ChapterService`, seed 3 chapter trong `ProjectSetup`, `UiText`). Không UI. Test EditMode đủ.
- **Phiên 2: Task 3–4** (Bản đồ banner/mũi tên/nút thủ lĩnh/cuộn/về từ Trận, docs, playtest). Cần khoá Unity.
- **Phiên 3 (nếu còn thời gian): Task 5** (mức đầy đủ, mục 10 spec).
- Task 1–2 và Task 3 cùng chạm `CombatConfig`/`ProjectSetup`/`StageMapView` nên **không chạy song song**; subagent chỉ dùng trong một task (vd. viết test song song với code).
- Điểm dừng playtest sau Task 4.

## Task 1: Dữ liệu và luật chapter (thuần C#)

**Files:**
- Tạo: `Assets/_Project/Combat/Scripts/ChapterDefinition.cs`, `ChapterCatalog.cs`, `ChapterService.cs`
- Sửa: `Assets/_Project/Combat/Scripts/StageDefinition.cs` (thêm cuối `_isBoss` + `IsBoss`; `Validate`: `IsBoss` chỉ cho Battle), `CombatConfig.cs` (thêm cuối `_chapters` + `Chapters`, `MaxStagesPerChapter`; `CheckStages` + kiểm hợp chapter), `CombatService.cs` (thuộc tính `Chapters`)
- Test: `Assets/_Project/Tests/ChapterDefinitionTests.cs`, `ChapterServiceTests.cs`, `CombatConfigTests` hoặc `CatalogTests.cs` (sửa), `StageDefinitionTests.cs` (+1)

- [ ] **Step 1: Viết test đỏ** (~28 test, theo mục 9 spec):
  1. `ChapterDefinitionTests`: `Validate_Valid_NoErrors`; `MissingId/Title/TitleEn_Error`; `EmptyStages_Error`; `NullStage_Error`; `NumberBelowOne_Error`; `NodePositionsWrongLength_Error`; `TwoBoss_Error`; `BossOnStory_Error`; `BossNotLastBattle_Error`; `BossFollowedByStory_Ok`; `StoryOnlyChapterWithBoss_Error`; `OverMaxStages_IsWarningNotError`.
  2. `CombatConfig`: `Chapters_UnionEqualsStageCatalog_Ok`; `Chapters_MissingStage/ExtraStage/WrongOrder/StageInTwoChapters_Error`; `Chapters_DuplicateId/DuplicatePartNumber_Error`; `Chapters_Null_VirtualSingleChapter` (không banner).
  3. `ChapterServiceTests`: `Status_Fresh_FirstAvailableRestLocked`; `Status_ClearLastStageOfChapter1_Chapter2Available`; `Status_AllCleared_Cleared`; `ClearedCount_CountsOnlyOwnStages`; `IndexOfStage_Unknown_MinusOne`; `DefaultIndex_ContainsNext`; `DefaultIndex_AllCleared_LastChapter`; `MaxReachableIndex_FirstLockedOrLast`; `AfterResetToDefaults_BackToFirstChapter`; `UnknownIdInClearedStages_DoesNotBreak`.
- [ ] **Step 2: Chạy, xác nhận đỏ.**
- [ ] **Step 3: Cài đặt** theo mục 4–5 spec. `ChapterService` đọc `StageProgressService.GetStatus` mỗi lần, không giữ bản sao. Chapter ảo khi `Chapters` null.
- [ ] **Step 4: Chạy EditMode xanh** (toàn bộ, không chỉ test mới).
- [ ] **Step 5: Báo 🧭** kết quả và số test; không commit.

## Task 2: Seed 3 chapter trong `ProjectSetup` và chữ `UiText`

**Files:**
- Sửa: `Assets/_Project/Editor/ProjectSetup.cs` (bảng chapter `ChapterTable` theo mục 8 spec; tạo `ChapterDefinition` asset vào `Combat/Data/Chapters/`; **ghép `StageCatalog` từ chapter**; gán `_chapters` của `CombatConfig`; đặt `_isBoss` cho `st_1_5`; tiêu đề Việt/Anh, màu nền khác nhau), bảng `UiText` (khu `chapter`)
- Test: `Assets/_Project/Tests/ProjectSetupTests.cs`, `GameDataBuilderTests.cs`, `UiTextTests` (key thiếu/`{n}`)

- [ ] **Step 1: Test đỏ:** `Seed_CreatesThreeChaptersWithBossLast`; `Seed_UnionEqualsExistingStageOrder` (thứ tự `st_1_*` không đổi); `Seed_GameDataEqualsBeforeChapters` (id, thứ tự, thưởng; không `IsBoss` trong `StageData`); `UiText` có đủ key `chapter.*` hai ngôn ngữ.
- [ ] **Step 2: Cài đặt** (idempotent như `ProjectSetup` hiện có; `StageCatalog` ghép từ chapter, không gõ lại hai nơi).
- [ ] **Step 3: Chạy `ProjectSetup` qua CLI** để dựng asset, **EditMode xanh toàn bộ**.
- [ ] **Step 4: Báo 🧭**; dừng phiên 1.

## Task 3: Bản đồ: banner, mũi tên, nút thủ lĩnh, cuộn, về từ Trận (cần khoá Unity)

**Files:**
- Sửa: `Assets/_Project/UI/Scripts/StageMapView.cs`, `StageNodeView.cs` (kiểu nút thủ lĩnh), `HubNav.cs` (`OpenOnNextLoad` thêm `focusStageId` tuỳ chọn), `BattleView.cs`, `BattleResultView.cs` (truyền id màn), builder dựng Bản đồ (tìm nơi dựng `StageMapView`; thêm `ChapterBanner`, `MapScroll`, `BossNode`, `ChapterLockedPanel`)
- Test: `Assets/_Project/Tests/StageSelectSceneTests.cs` (sửa), `Tests/PlayMode/` (+2, mục 9 spec), `SmokeLoopTests` (sửa theo chapter đầu)

- [ ] **Step 1: Test đỏ** (PlayMode, raycast thật): (a) mở Bản đồ → chapter 1, ◀ mờ, ▶ sang chapter 2 "???" + ô khoá, ▶ tiếp mờ, ◀ về; (b) thắng đủ màn chapter 1 → chapter 2 có nút; vào trận chapter 2 rồi về: Bản đồ ở chapter 2.
- [ ] **Step 2: Cài đặt** theo mục 5–6 spec: pool nút/liên kết dựng một lần theo số màn lớn nhất, `_dirty` chỉ khi đổi chapter hoặc trạng thái, không cấp phát trong `Update`; chữ qua `UiText`; chapter đang xem chỉ trong view; Reset → nạp lại scene → chapter đầu.
- [ ] **Step 3: Dựng lại scene qua builder, chạy EditMode + PlayMode xanh toàn bộ.**
- [ ] **Step 4: Chụp Bản đồ ở 16:9, 16:10, 4:3** (mỗi ảnh: chapter mở, chapter khoá, nút thủ lĩnh), kiểm banner không che nút (L-036); ảnh lưu vào thư mục ghi chú chung của nhóm, báo 🧭 để user duyệt hình thật.
- [ ] **Step 5: Báo 🧭.**

## Task 4: Docs, kiểm cuối, playtest

- [ ] **Step 1:** sửa các docs ở mục 13 spec: `game-design.md` (Màn chơi), `screen-flow.md` (Bản đồ gộp + banner), `roadmap.md` (chuyển Đã xong), `docs/project/structure.md`, lesson nếu có. Không còn tham chiếu "chưa làm nhiều chương".
- [ ] **Step 2:** chạy toàn bộ EditMode + PlayMode, ghi số test vào báo cáo; kiểm `git status` chỉ có file thuộc kế hoạch này.
- [ ] **Step 3:** **Dừng cho user playtest** theo mục 11 spec (Editor Local, rồi một lần UGS `development`).
- [ ] **Step 4:** "Báo cáo cuối kênh" ([brief.md](../../studio/brief.md)) cho 📋 và 🧭.

## Task 5 (mức đầy đủ, nếu còn thời gian)

- [ ] **Step 1:** `NodePositions` dùng thật: `StageMapView` đọc toạ độ riêng của chapter khi có (test: có/không có, sai số lượng → `Validate` lỗi đã có từ Task 1).
- [ ] **Step 2:** `MapSprite` dùng khi có (chưa có art; test bằng sprite hình có sẵn trong test).
- [ ] **Step 3:** thẻ "Chương chính" ở Chọn chế độ hiện nhãn chapter đang tới (`ModeSelectView.SubText`).
- [ ] **Step 4:** mở rộng seed lên 6 chapter **chỉ khi** spec Cốt truyện đã có màn tương ứng (bảng chapter do 🎨 Cốt truyện cung cấp); không tự đặt màn mới.
- [ ] **Step 5:** chạy toàn bộ test, báo 🧭.

## Cắt khi trễ

Task 5 trước. Task 1–4 là lõi (đủ 3 chapter và yêu cầu 2–3 chapter của user). Xem mục 10 spec.
