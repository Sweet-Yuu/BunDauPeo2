# Spec: Combat nhiều chương/chapter (Bản đồ gộp + banner chuyển chapter)

> **Đọc khi:** làm hệ thống chapter của Combat (dữ liệu chapter, Bản đồ màn, mở khoá theo chapter), hoặc chia nội dung Chương 0 vào các chapter.
> **Cập nhật khi:** user đổi luật chapter hoặc bố cục Bản đồ. Spec cốt truyện chỉ quyết **nội dung** từng chapter; spec này quyết **cấu trúc và UI**.
> Trạng thái: user duyệt hướng và ảnh mô phỏng qua hội thoại 2026-11-26 (hai lần chỉnh: bỏ màn danh sách riêng, banner có mũi tên). Chờ user duyệt bản viết.

## 1. Mục tiêu

Combat hiện có **1 chương cố định 7 nút** ([game-design, Màn chơi](../../design/game-design.md#màn-chơi)). Chương 0 "Sự khởi đầu" ([story-core](../../design/story-core.md)) chia thành nhiều **chapter**, mỗi chapter một tiêu đề và một map, kết bằng trận thủ lĩnh, rồi một chapter quãng nghỉ chỉ có truyện. Spec này làm Combat chứa được nhiều chapter, người chơi đổi chapter ngay trên Bản đồ, chapter sau mở khi chapter trước xong, và **thêm chapter mới về sau chỉ bằng dữ liệu** (asset + dòng trong `ProjectSetup`), không sửa code.

**Không làm (non-goals):**
- Nội dung, tên, thứ tự màn của từng chapter, lời thoại: thuộc spec Cốt truyện (🎨 Cốt truyện). Spec này chỉ đưa bảng chapter **tạm** để chạy được.
- Mở khoá **nhân vật người dân thân mềm** theo chapter: user chốt 2026-11-26 để ngoài, ghi vào sổ ý tưởng "Để sau" (cần lệnh server, chạm luật server/tiền).
- Thưởng khi clear chapter: không có; thưởng vẫn nằm ở từng màn.
- Độ khó Hard, quét nhanh, tự tiến màn kế, các chế độ khác ở màn Chọn chế độ.
- Đổi luật server, tiền, thể lực.

**Anti-goal:** thêm field vào `PlayerSave`/`ServerState`, hay đổi thứ tự `StageCatalog` làm khoá lại người đã qua màn.

## 2. Quyết định đã chốt với user (2026-11-26)

| # | Quyết định | Nguồn |
|---|---|---|
| 1 | Bản 31/12 cần **2–3 chapter tối thiểu** của Chương 0 (có truyện + trận thủ lĩnh); hệ thống đủ để chapter sau thêm bằng dữ liệu | user qua CEO/🧭 |
| 2 | Mỗi chapter một tiêu đề + map; chapter kết bằng trận thủ lĩnh; chapter cuối Chương 0 là quãng nghỉ chỉ có truyện | story-core (user chốt) |
| 3 | Nhân vật thân mềm mở khoá theo chapter: **để ngoài** (hướng 3) | user |
| 4 | Phương án **A**: `ChapterDefinition` + `ChapterCatalog`, `StageCatalog` giữ phẳng | user |
| 5 | **Không có màn danh sách chapter riêng.** Chọn "Chương chính" vào thẳng Bản đồ; chọn chapter bằng **banner góc trái dưới có hai mũi tên ◀ ▶**, ghi rõ "Chương X · Chapter Y" | user |
| 6 | Mỗi chapter **không bị giới hạn 5 nút**, có thể dài hơn (map kéo ngang) | user |

Quyết định của 🎨 (user chưa nói, sửa được): vào lần đầu/sau Reset mở chapter đầu, các lần sau mở chapter có màn NEXT; chapter khoá chỉ hiện "???" và không tiết lộ tên; không có thưởng clear chapter; trận thủ lĩnh là cờ trên màn, không phải loại màn mới; **trong bảng seed chỉ chapter cuối (0-3) có thủ lĩnh**, chapter 0-1 và 0-2 kết bằng trận thường (seed chỉ để chạy thử, Cốt truyện quyết chapter nào có thủ lĩnh, `Validate` cho phép chapter không có `IsBoss`).

## 3. Các phương án đã cân

| | Cách làm | Đánh đổi |
|---|---|---|
| **A (chọn)** | Asset `ChapterDefinition` + `ChapterCatalog`; `ProjectSetup` ghép các chapter theo thứ tự thành `StageCatalog` phẳng | Thêm 2 loại asset, nhưng tên/tóm tắt/nền map có chỗ ở; server, `StageProgressService`, `GameDataBuilder` không đổi |
| B | Chỉ thêm `_chapterId` vào `StageDefinition` | Ít file nhất, nhưng tên chapter, nền map không có chỗ (phải nhét vào code/`UiText`, là dữ liệu cố định) |
| C | Mỗi chapter một `StageCatalog` | Server và `StageProgressService` giả định một danh sách phẳng: phải sửa cả hai, đúng chỗ muốn né |

**Vì sao A không đụng server:** server mở màn theo vị trí trong `data.Stages` (màn i mở khi i-1 đã clear; [ServerHandlers.Stage.cs](../../../Assets/_Project/Rules/ServerHandlers.Stage.cs) `IsStageOpen`), máy cũng vậy (`StageProgressService.GetStatus`). Chapter nối đuôi nhau trong danh sách phẳng thì chapter k+1 **tự khoá** tới khi màn cuối chapter k clear. Không cần luật mới ở hai phía.

## 4. Dữ liệu

### 4.1 Asset mới và field mới (`Assets/_Project/Combat/Scripts`)

| Loại | Field | Ghi chú |
|---|---|---|
| `ChapterDefinition : GameDefinition` (mới) | `_part` (int, "Chương", Chương 0 = 0), `_number` (int, "Chapter" trong chương, từ 1), `_title`, `_titleEn`, `_summary`, `_summaryEn` (dùng ở phần Cốt truyện sau, có thể trống), `_stages` (`StageDefinition[]`, đúng thứ tự trên map), `_mapTint` (Color, nền map hình tạm), `_mapSprite` (Sprite, **trống**, chỗ để thay art thật; view dùng nếu có), `_nodePositions` (`Vector2[]`, **tuỳ chọn**, mức đầy đủ; trống = zíc-zắc mặc định) | `DisplayName` qua `GameLanguage.Pick`, cùng kiểu `StageDefinition` |
| `ChapterCatalog : Catalog<ChapterDefinition>` (mới) | thứ tự mảng = thứ tự chapter | cùng kiểu `StageCatalog` |
| `StageDefinition` (sửa) | thêm **cuối** `_isBoss` (bool) + thuộc tính `IsBoss` | giá trị lưu trong asset chỉ thêm ở cuối. Spec Cốt truyện thêm `_beforeScene/_afterScene` sau field này, thứ tự cuối do 🔨 làm sau giữ nguyên "chỉ thêm ở cuối" |
| `CombatConfig` (sửa) | thêm **cuối** `_chapters` (`ChapterCatalog`) + `Chapters`, và `_maxStagesPerChapter` (int, khởi điểm 12, chỉ để cảnh báo) | **tuỳ chọn**: null/rỗng thì toàn bộ `StageCatalog` là một chapter ảo (không banner) để test và fixture cũ không vỡ. `// shortcut: chapter ảo cho cấu hình thiếu catalog, gỡ khi mọi fixture có chapter` |

Nhãn hiển thị: `chapter.label` = "Chương {0} · Chapter {1}" / "Part {0} · Chapter {1}" (số lấy từ `_part`, `_number`). Tên màn trên map (vd "1-1") do `StageDefinition.DisplayName` như hiện nay; cách đánh số tên màn theo chapter do spec Cốt truyện quyết.

### 4.2 `Validate()` (mỗi lỗi bắt đầu bằng tên field)

- `ChapterDefinition`: `Id` không trống; `Stages` ≥ 1, không phần tử null; `Number` ≥ 1, `Part` ≥ 0; tiêu đề Việt và Anh không trống; `NodePositions` rỗng hoặc đúng số màn.
- Cảnh báo (không lỗi): chapter > `MaxStagesPerChapter` màn (config, khởi điểm 12) vì map cuộn dài khó đọc.
- Trận thủ lĩnh: `IsBoss` chỉ đặt cho `Kind == Battle`; trong một chapter chỉ **một** màn `IsBoss` và nó là **màn đánh cuối** của chapter (sau nó chỉ có thể là màn Story). Chapter quãng nghỉ (chỉ Story) không có màn `IsBoss`.
- `CombatConfig` (khi có `Chapters`): **hợp các chapter theo thứ tự đúng bằng `StageCatalog`** (cùng phần tử, cùng thứ tự, mỗi màn thuộc đúng một chapter); id chapter không trùng; cặp `(Part, Number)` không trùng.

### 4.3 Server không đổi

`GameDataBuilder` đọc `StageCatalog` phẳng như cũ, `StageData` không có `IsBoss` và không có chapter, `ServerDataExport` không đổi. Test khẳng định `GameData` sinh ra từ bảng seed **giống hệt** trước khi thêm chapter (id, thứ tự, thưởng).

### 4.4 Lưu tiến độ: không có field mới (D-005)

| Luật D-005 | Cách đáp ứng |
|---|---|
| Dữ liệu cần lưu chỉ ở `PlayerSave`, đi qua `SaveService` | **Không có dữ liệu mới cần lưu.** Chapter xong / mở / NEXT đều **suy ra** từ `PlayerSave.ClearedStages` (server giữ) |
| Reset Save Data xoá toàn bộ | `ClearedStages` về rỗng thì mọi chapter tự về trạng thái đầu; chapter đang xem chỉ nằm trong `StageMapView` (bộ nhớ), scene nạp lại sau Reset nên về chapter đầu. Có test |
| Server/luật tiền | Không đổi (mục 4.3) |
| Save cũ | Id màn cũ (`st_1_*`) giữ nguyên, thứ tự phẳng giữ nguyên nên người đã qua màn không bị khoá lại |

## 5. Luật

- **Trạng thái chapter** (`ChapterStatus`: `Unknown`, `Locked`, `Available`, `Cleared`): `Locked` khi màn đầu của chapter là `StageStatus.Locked`; `Cleared` khi mọi màn `Cleared`; còn lại `Available`. Suy ra thuần từ `StageProgressService.GetStatus`, không lưu.
- **Chapter mặc định khi mở Bản đồ:** chapter chứa màn NEXT (`StageMapView.NextStageIndex`); nếu mọi màn đã clear thì **chapter cuối**. Về từ Trận thì mở **chapter của màn vừa chơi** (mục 6.3).
- **Chuyển chapter** (◀ ▶ trên banner): ▶ tới chapter kế kể cả khi nó `Locked` (để người chơi thấy còn nội dung), nhưng **không đi xa hơn chapter khoá đầu tiên** (▶ mờ). ◀ mờ ở chapter đầu.
- **Chapter khoá:** banner hiện "Chương X · Chapter Y" và tiêu đề "???"; map không dựng nút, hiện một ô giữa map "Clear chapter {trước} để mở" (không lộ tên màn hay truyện). Chapter quãng nghỉ khoá cũng "???".
- **Mỗi lần `Open()` Bản đồ tính lại chapter** (về từ Trận thì theo `focusStageId`, còn lại theo chapter chứa NEXT); không giữ chapter vừa xem giữa hai lần mở. `NextStageIndex` trả chỉ số **phẳng toàn cục**: view quy đổi sang chỉ số trong chapter (`IndexOfStage`) khi đặt dấu NEXT.
- **Đủ thể lực, đội, thể lực trừ khi vào, thưởng, thoại**: giữ nguyên, không thuộc spec này.
- **Màn Story trong chapter** (kể cả chapter chỉ-truyện): đi qua `StageMapView.OnNodeClicked` như hiện nay (spec Cốt truyện thay `StoryPlaceholderPopup` bằng hội thoại).

## 6. UI

Ảnh mô phỏng user đã xem (3 trạng thái: đang mở, chapter khoá, chapter đầu): [2026-11-26-multi-chapter-mockup.html](2026-11-26-multi-chapter-mockup.html). Ảnh tham khảo: `quest-stage-map.jpg`, `quest-stage-map-small-en.jpg` ([ui-screens](../../references/ui-screens.md)). Bản đồ vẫn là trang kín màn con của Chọn chế độ (spec UI hub C); Quay lại → Chọn chế độ như cũ. Hình chỉ là `Image` vuông/tròn/tam giác có sẵn; object chưa có hình đặt tên `<Tên>_PH_<rộng>x<cao>`.

### 6.1 Bố cục Bản đồ (user duyệt hướng 2026-11-26; bản có mũi tên chờ duyệt lại)

```
┌──────────────────────────────────────────────────────────────┐
│ ◀ Quay lại                                    ⚡ thể lực     │ thanh trên (có sẵn)
│                                                              │
│        ■──────■            ●  = màn thủ lĩnh (tròn, viền vàng)│
│   ■───/        \──■ NEXT                                     │ map kéo ngang khi
│                       \──■──────●                            │ chapter dài
│                                                              │
│ ┌─────────────────────────────┐                              │
│ │ ◀  CHƯƠNG 0 · CHAPTER 2  ▶  │  banner góc trái dưới        │
│ │    Kho mắm có khách lạ      │  tiêu đề; dòng nhỏ: 1/6 màn  │
│ └─────────────────────────────┘                              │
└──────────────────────────────────────────────────────────────┘
```

| Vùng | Tên object | Ghi chú |
|---|---|---|
| Nền map | `MapBackground_PH` | `Image` phủ panel, màu = `Chapter.MapTint`; có `MapSprite` thì dùng sprite |
| Vùng cuộn | `MapScroll_PH` | `ScrollRect` ngang quanh `_nodesRoot`; chỉ cuộn khi bề rộng nội dung > 1920; tới NEXT khi đổi chapter, về đầu khi chapter không có NEXT |
| Nút màn | `Node{i}` (tái dùng pool, bật `n` nút đầu) | tái dùng `StageNodeView`; bố cục `NodeCenter(i)` zíc-zắc hiện có (bước 240, y 720/400), có `NodePositions` thì dùng toạ độ đó |
| Nút thủ lĩnh | `BossNode_PH_160x160` | **biến thể trong cùng `StageNodeView` và cùng pool** (`Bind` đổi hình tròn + viền vàng + kích thước + chữ "BOSS" khi `IsBoss`, trả lại hình vuông khi không); không có template thứ hai. Bấm như màn đánh thường |
| Banner | `ChapterBanner_PH_620x120` | góc trái dưới, cách mép 24 px, nền tối alpha 0,85, viền vàng khi chapter đang mở |
| Mũi tên | `ChapterPrev_PH_88x88`, `ChapterNext_PH_88x88` | hai ô vuông (chữ ◀ ▶) hai đầu banner; mờ khi không đi tiếp được |
| Nhãn | `ChapterLabel_PH` (dòng nhỏ "CHƯƠNG 0 · CHAPTER 2"), `ChapterTitle_PH` (tiêu đề / "???"), `ChapterProgress_PH` ("1/6 màn · Truyện" nếu chapter chỉ có truyện) | |
| Ô khoá | `ChapterLockedPanel_PH` | giữa map khi chapter `Locked`: "Clear chapter 0-2 để mở" |

Ngoại lệ đã biết của quy tắc "không che nút": banner nằm góc trái dưới nên không giao thanh trên, nút Quay lại, và không giao popup Màn/Chọn đội (hai popup đó phủ trên banner). Nếu kiểm ở 16:10 và 4:3 thấy che nút thì **dừng, báo 🧭** (bài học L-036: test bằng chạm thật).

Chữ mới (`UiText`, khu `chapter`, hai ngôn ngữ): `chapter.label`, `chapter.locked_title` ("???"), `chapter.locked_hint` ("Clear chapter {0} để mở" / "Clear chapter {0} to unlock"), `chapter.progress` ("{0}/{1} màn" / "{0}/{1} stages"), `chapter.story_only` ("Truyện" / "Story"), `chapter.boss` ("BOSS"). Tên chapter nằm trong asset (không qua `UiText`).

Thẻ "Chương chính" ở Chọn chế độ: giữ dòng NEXT hiện có (mức tối thiểu); mức đầy đủ thêm nhãn chapter đang tới ("Chương 0 · Chapter 2").

### 6.2 `Update` không cấp phát (L-044)

Đổi chapter chỉ gán chữ và bật/tắt nút khi **đổi chapter hoặc trạng thái đổi** (`_dirty`), không trong `Update`. Nút và liên kết là pool dựng một lần ở số màn lớn nhất của một chapter, tái dùng khi đổi chapter (không `Instantiate` mỗi lần).

### 6.3 Về từ Trận

`BattleView`/`BattleResultView` gọi `HubNav.OpenOnNextLoad(HubTab.Stages, menu: false, stageMap: true)` ([BattleResultView.cs](../../../Assets/_Project/UI/Scripts/BattleResultView.cs), [BattleView.cs](../../../Assets/_Project/UI/Scripts/BattleView.cs)). Thêm tham số tuỳ chọn `focusStageId`; `StageMapView` mở chapter chứa id đó, thiếu thì chapter mặc định (mục 5). Cờ này chỉ sống một lần nạp scene, không lưu. Đi qua `HubPending.Set/TryTake` (nơi `HubNav` giữ `stageMap`), nên thêm `focusStageId` ở cả hai chỗ.

## 7. Cấu trúc code

| Thành phần | Nơi | Ghi chú |
|---|---|---|
| `ChapterDefinition`, `ChapterCatalog`, `StageDefinition._isBoss`, `CombatConfig._chapters` | `Combat/Scripts` | mục 4.1 |
| `ChapterService` (thuần C#) | `Combat/Scripts` | tiêm `CombatConfig` + `StageProgressService`; API: `Count`, `Get(i)`, `GetStatus(i)`, `ClearedCount(i)`, `IndexOfStage(id)`, `DefaultIndex()`, `MaxReachableIndex()` (chapter khoá đầu tiên hoặc cuối). Không giữ bản sao tiến độ, đọc `_progress` mỗi lần. Gắn vào `CombatService` (nơi đã giữ `Config` và `Progress`) như một thuộc tính `Chapters` |
| `StageMapView` | `UI/Scripts` | thêm chapter hiện tại, banner, mũi tên, dựng lại pool nút theo chapter, `MapScroll`; `NodeCenter(int)` giữ chữ ký, thêm bản có `ChapterDefinition` |
| Builder | `StageSelect`/`HubSceneBuilder` (nơi dựng Bản đồ hiện có, 🔨 tìm) | dựng banner và vùng cuộn; không sửa tay `.unity` |
| `ProjectSetup` | `Editor` | bảng chapter (mục 8), tạo asset, ghép `StageCatalog` từ chapter, tên/bản dịch |
| Dev | `DevOverlay` | không đổi (chọn màn thử vẫn đi theo `StageCatalog` phẳng) |

Không đổi: `Rules`, `GameData`, `ServerModule`, `StageProgressService`, `StaminaService`, `BattleSession`, `PlayerSave`.

## 8. Bảng chapter tạm (seed) và quan hệ với spec Cốt truyện

Bảng này để hệ thống chạy và test được **trước** khi spec Cốt truyện chốt nội dung. Id màn `st_1_*` và thứ tự giữ nguyên (save cũ, test cũ). 🔨 Cốt truyện **sửa bảng này** (không sửa code chapter).

| Chapter (Id) | Chương · Chapter | Tiêu đề tạm (🎨, theo story-core) | Màn (đúng thứ tự phẳng hiện có) |
|---|---|---|---|
| `ch0_1` | 0 · 1 | Tỉnh dậy ở Threat City | `st_1_story1`, `st_1_1`, `st_1_2` |
| `ch0_2` | 0 · 2 | Kho mắm có khách lạ | `st_1_story2`, `st_1_3`, `st_1_4` |
| `ch0_3` | 0 · 3 | Thủ lĩnh giáp xác | `st_1_5` (`IsBoss`) |

3 chapter = **mức tối thiểu** của user. Mức đầy đủ (6 chapter của story-core, thêm 0-4 Vòng vây, 0-5 Thủ lĩnh, 0-6 Quãng nghỉ chỉ truyện) cần thêm màn mới do spec Cốt truyện định; hệ thống không cần đổi code, chỉ thêm dòng vào bảng và asset màn.

**Thứ tự và nhánh nền (🎨 quyết, theo hàng khoá):** Nhiều chương làm **trước** Cốt truyện. `docs/cot-truyen-spec` hiện chỉ chứa docs (spec/kế hoạch), code Cốt truyện chưa làm; nhánh `feature/multi-chapter` cắt từ `origin/main` sau khi Thời tiết merge. 🔨 Cốt truyện cắt sau khi nhánh này merge và áp kế hoạch của nó lên bảng chapter (thứ tự field cuối `StageDefinition`: `_isBoss`, rồi `_beforeScene`, `_afterScene`).

`ProjectSetup` **sửa tại chỗ, idempotent** như các bảng hiện có (`GetOrCreateAsset` + `Apply`, `MigrateInt`): asset `st_1_*` và `StageCatalog` đã có giữ GUID/`.meta`, chỉ gán lại `_items`; không tạo lại, không xoá.

**Xung đột cần 🔨 xếp thứ tự:** hàng khoá là Thời tiết → **Nhiều chương** → Cốt truyện → Vẽ lại quán. Spec Cốt truyện (nhánh `docs/cot-truyen-spec`, kế hoạch Task 2: đổi tên quái trong `ProjectSetup`; thêm `_beforeScene/_afterScene` cuối `StageDefinition`) và spec này cùng sửa `ProjectSetup` (`StageTable`) và `StageDefinition`; làm lần lượt, không song song. Khi 🔨 Cốt truyện làm sau, nó chỉ cần: đặt thoại vào màn, sửa bảng chapter nếu chia khác, và thay `StoryPlaceholderPopup` (mục 5). Việc **chia chapter nội dung và đánh số tên màn theo chapter** thuộc 🎨 Cốt truyện (spec này không giành việc đó).

## 9. Kiểm thử

EditMode (thuần C#, ~28 mới):
1. `ChapterDefinitionTests`: `Validate` hợp lệ; thiếu id/tiêu đề/màn; `Number` < 1; `NodePositions` sai số lượng; hai `IsBoss` trong một chapter; `IsBoss` trên màn Story; `IsBoss` không phải màn đánh cuối; chapter chỉ-truyện không `IsBoss`.
2. `ChapterCatalogTests`/`CombatConfigTests`: hợp chapter bằng `StageCatalog` (khớp, thiếu màn, thừa màn, sai thứ tự, màn thuộc hai chapter); id và cặp `(Part, Number)` trùng; `Chapters` null → chapter ảo chứa mọi màn.
3. `ChapterServiceTests`: trạng thái khi chưa clear gì (chapter 1 `Available`, các chapter sau `Locked`); clear màn cuối chapter 1 → chapter 2 `Available`; `Cleared` khi đủ màn; `ClearedCount`; `IndexOfStage` (id lạ = -1); `DefaultIndex` (chứa NEXT; hết màn → chapter cuối); `MaxReachableIndex`; **sau `ResetToDefaults` về chapter đầu**; id màn lạ trong `ClearedStages` không làm vỡ.
4. `ProjectSetupTests` (sửa): bảng seed tạo đúng 3 chapter; hợp màn bằng `StageCatalog` hiện có; `GameData` sinh ra từ bảng seed **bằng** `GameData` trước khi thêm chapter (id, thứ tự, thưởng) và `StageData` không có `IsBoss`.
5. `SaveCompatibilityTests` không đổi (khẳng định `PlayerSave` không thêm field: JSON cũ vẫn nạp).

PlayMode (+2): (a) mở Bản đồ lần đầu → chapter 1, bấm ▶ tới chapter khoá hiện "???" và ô khoá, ▶ mờ, ◀ về được; (b) thắng đủ màn chapter 1 → ▶ mở chapter 2 dựng đúng số nút, về từ Trận mở đúng chapter của màn vừa chơi. Bấm bằng raycast thật (`UiDriver.Click`, L-036). `StageSelectSceneTests`, `SmokeLoopTests` (phụ thuộc thứ tự màn) sửa theo chapter đầu.

Chạy `unity test . --mode EditMode` / `--mode PlayMode`. Đo GC (`GcMeasureTests`) cho Bản đồ nếu có khung đứng yên.

## 10. Mức tối thiểu và mức đầy đủ (cắt khi trễ)

| Mức | Gồm | Task |
|---|---|---|
| **Tối thiểu (lõi, không cắt)** | Dữ liệu chapter + `_isBoss` + `ChapterService` + test; 3 chapter seed; Bản đồ có banner ◀ ▶, nút thủ lĩnh, chapter khoá "???", về từ Trận mở đúng chapter, dùng zíc-zắc có sẵn và màu nền theo chapter | 1, 2, 3 |
| Luôn làm | Docs, kiểm cuối, báo cáo | 4 |
| + Đầy đủ | Toạ độ nút riêng cho từng chapter (`NodePositions`), chỗ cắm `MapSprite` dùng thật, nhãn chapter ở thẻ Chương chính, mở rộng seed thành 6 chapter khi spec Cốt truyện có màn | 5 |

**Thứ tự cắt khi trễ:** Task 5 (đầy đủ) trước; nếu vẫn trễ: chỉ giữ chapter ảo (mọi màn một chapter, không banner) và để Cốt truyện làm việc chapter sau, nhưng như vậy **không đạt yêu cầu 2–3 chapter**, nên chỉ cắt khi CEO đồng ý. Không cắt được: dữ liệu + luật (Task 1) vì Cốt truyện cần bảng chapter.

## 11. Playtest (người thử, Editor chế độ Local trước, rồi một lần bản UGS `development`)

1. Game mới: Màn chơi → Chương chính → vào thẳng Bản đồ, banner "Chương 0 · Chapter 1", 3 nút, ◀ mờ.
2. ▶ sang chapter 2 (khoá): "???", ô "Clear chapter… để mở", map không nút, ▶ mờ. ◀ về được.
3. Đánh hết chapter 1 → ▶ mở được chapter 2 với các nút; chapter 3 vẫn "???".
4. Vào một trận chapter 2 rồi Quay lại: Bản đồ mở sẵn ở **chapter 2**, NEXT đúng.
5. Chapter 3: nút thủ lĩnh hình tròn viền vàng, thắng → mọi chapter `Cleared`; mở lại Bản đồ: chapter cuối.
6. Chapter dài (thêm tạm 9 màn trong asset thử): cuộn ngang được, NEXT hiện trong tầm nhìn khi đổi chapter.
7. Reset Save Data: về chapter 1, chapter 2/3 khoá lại, banner đúng.
8. Mở ở 16:10 và 4:3: banner không che nút nào.

## 12. Rủi ro và đã biết

- **Một danh sách phẳng là điều kiện mở khoá:** chèn màn mới vào giữa danh sách vẫn an toàn với người đã qua (màn đã clear luôn mở, `StageProgressService.GetStatus`), nhưng người chưa tới đó sẽ phải qua màn mới. Chèn chapter giữa các chapter đã có cũng thế; chấp nhận.
- **`StageMapView` đang có `shortcut: đủ cho một chương 7 nút` và `NodeCenter` tĩnh:** thay bằng pool nút theo chapter; test phụ thuộc `NodeCenter(int)` và tên `Node{i}` cần sửa theo.
- **Bản đồ cuộn trên nền panel kín màn:** kéo ngang có thể xung đột với vuốt đổi trang khác của hub; 🔨 kiểm khi dựng (spec UI hub C). Nút chapter nằm ngoài vùng cuộn nên bấm được khi đang cuộn. **Tiêu chí:** nếu xung đột mà không gỡ được gọn, **bỏ cuộn, giới hạn chapter ≤ 7 nút** (vừa 1920 px) và báo 🧭; 3 chapter seed (tối đa 3 nút) không bị ảnh hưởng.
- **Chapter chỉ-truyện lệ thuộc spec Cốt truyện:** tới lúc đó các màn Story vẫn là nút giữ chỗ, chapter 0-6 chưa có nội dung thật.
- **Hiệu chỉnh UI:** ảnh mô phỏng là bản user đã nhìn hai lần (2026-11-26); khi dựng xong người thử chụp Bản đồ ở 3 tỉ lệ để user duyệt lại hình thật trước khi coi là xong.

## 13. Docs phải sửa (trong kế hoạch)

`game-design.md` mục Màn chơi (nhiều chapter đã làm; chapter, boss, banner), `screen-flow.md` (Bản đồ gộp, banner chapter, về từ Trận), `roadmap.md` (chuyển sang Đã xong), `docs/project/structure.md` (`ChapterService` và asset chapter), `docs/design/story-core.md` (chỉ dẫn "Combat nhiều chapter: xem spec này"), `ideas.md` (nhân vật thân mềm mở khoá theo chapter, đã ghi "Để sau"), lesson nếu có.
