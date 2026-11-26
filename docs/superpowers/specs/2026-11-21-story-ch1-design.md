# Spec: Cốt truyện Chương 1 (Péo, Hợp chất X, địch toàn tôm)

> ⚠ **Tạm dừng 2026-11-26: user đổi cốt truyện** (Péo thành nhân vật chuyển sinh, hồi ức và hai nhánh kết; xem [story-core.md](../../design/story-core.md)). Mục 1–2 (kịch bản Hợp chất X, tôm), mục 7 (đổi quái sang tôm) và phụ lục **lỗi thời, không làm**. Mục 3–6 và 8–9 (dữ liệu hội thoại, `SeenStoryScenes`, màn hội thoại, thoại trước/sau trận, ô Cốt truyện, Log/Auto) **giữ nguyên làm nền**; viết lại spec khi user chốt chia chương.

> Ngày: 2026-11-21 · Trạng thái: **user duyệt spec, kế hoạch, ảnh mô phỏng và lời thoại nháp 2026-11-21** · Phạm vi: Combat (`StageDefinition`, quái mẫu), UI (màn hội thoại, ô Cốt truyện), `PlayerSave`, `ProjectSetup`
> Nguồn: [roadmap](../../roadmap.md) dòng Cốt truyện Chương 1, [D-005](../../decisions/decisions.md), [spec Combat](2026-11-02-combat-design.md), [spec Online đợt 2](2026-11-16-online-2-design.md) (`CompleteStory`, `ClearedStages` do server giữ). Kế hoạch: [story-ch1](../plans/2026-11-21-story-ch1.md). Ảnh tham khảo: `ReferenceImages/story-dialogue-en.webp`.

## 1. Mục tiêu

Hai nút cốt truyện giữ chỗ của Combat thành hội thoại thật; một vài trận đánh có thoại trước hoặc sau; phe địch của Chương 1 toàn tôm; người chơi xem lại được truyện đã qua ở ô **Cốt truyện** trên thanh dưới.

Thành công khi:
1. Chạm nút "Truyện" trên bản đồ: chiếu hội thoại, hết thoại (hoặc Bỏ qua) thì màn truyện **clear** qua `CompleteStory` như hiện nay, mở màn kế.
2. Vào trận 1-5 (boss): sau khi trận bắt đầu, chiếu thoại trước, trận tạm dừng đúng lúc đó. Thắng 1-5: bấm Tiếp tục ở màn kết quả thì chiếu thoại sau rồi về bản đồ.
3. Mỗi đoạn thoại trước/sau chỉ tự chiếu **một lần** (cờ "đã xem" trong save); xem lại ở ô Cốt truyện.
4. Ruồi, chuột, gián không còn xuất hiện: quái là Tép Nhảy, Tôm Sú, Tôm Càng, boss Tôm Chúa; chỉ số và luật trận **không đổi**.
5. Reset Save Data xoá hết cờ "đã xem"; server, tiền, thể lực không đổi luật.

**Ngoài phạm vi (anti-goal):** nhiều chương, nhân vật gacha nói chuyện, tiếng nói, nhạc, hiệu ứng chữ chạy từng ký tự, lựa chọn rẽ nhánh, thưởng cho việc xem truyện, nền cảnh, hình nhân vật thật (art đổi sau, xem mục 5), dịch ngôn ngữ thứ ba.

## 2. Quyết định đã chốt với user (2026-11-21)

| # | Câu hỏi | Chốt |
|---|---|---|
| 1 | Cốt truyện | Péo là chủ quán bún đậu; Hợp chất X lẫn vào lô mắm tôm, tôm nổi loạn kéo đến quán. Hai truyện: **Truyện 1** (trước 1-1) tôm bò ra khỏi hũ; **Truyện 2** (sau 1-2) manh mối Hợp chất X, gợi kẻ đứng sau (để Chương 2). Boss 1-5 là **Tôm Chúa**. Đội ta không phải tôm |
| 2 | Loại tôm | Tép Nhảy (thay Ruồi Bàn), Tôm Sú (thay Chuột Bếp), Tôm Càng (thay Gián Kho), Tôm Chúa (thay Chuột Chúa) |
| 3 | Phạm vi | Làm mức **lớn nhất**: thoại trước/sau trận, ô Cốt truyện xem lại, nút Log và Auto. Bảng cắt ở mục 9 |
| 4 | Thoại trước/sau trận phát ở đâu | **Trong scene Trận.** Thoại trước chiếu sau khi vào trận, **thể lực đã trừ, thoát giữa thoại là mất** như luật hiện tại. Thoại sau chỉ có ở một số trận |
| 5 | Ảnh mô phỏng | Duyệt: màn hội thoại (người nói sáng, người kia mờ, thanh tên + khung thoại dưới, nút Bỏ qua trên phải, ▼ chạm để sang câu) và danh sách ô Cốt truyện; thêm nút **Log** và **Auto** (mục 6) |

**Lựa chọn của 🎨 (user chưa xét riêng, sửa được):** thoại sau chỉ chiếu khi bấm Tiếp tục ở màn kết quả (thưởng đã nhận xong, không mất thưởng nếu tắt app giữa thoại); Bỏ qua tính là đã xem; thoại trước vẫn chiếu ở trận DEV "Chiến đấu thử" (để thử truyện) và đánh dấu đã xem; chỉ 4 đoạn thoại ở Chương 1 (phụ lục); tên tiếng Anh các tôm ở mục 7.

## 3. Luật

- **Đoạn thoại** (`StorySceneDefinition`): danh sách câu. Mỗi câu có người nói (hoặc trống = lời dẫn), bên (trái/phải) và chữ hai ngôn ngữ. Chạm bất kỳ đâu = sang câu kế; câu cuối thì kết thúc. **Bỏ qua** kết thúc ngay (hiệu lực như đọc hết).
- **Màn Story** (`StageKind.Story`): có `BeforeScene` bắt buộc. Kết thúc thoại thì gọi `StageProgressService.CompleteStoryAsync(stageId)` (đã có). Màn đã clear thì chạm nút chỉ xem lại, **không** gọi server. Mất mạng: hộp mạng có sẵn báo, thoại giữ ở câu cuối, chạm lại để thử lại (như nút Tiếp tục hiện nay).
- **Màn Battle**: `BeforeScene`, `AfterScene` tuỳ chọn. Chương 1 chỉ 1-5 có cả hai.
- **Thoại trước** (Battle): `BattleView` mở thoại khi trận vào được và `BeforeScene` chưa xem; trong lúc thoại mở, `IsPaused` đúng (không ai hành động, AUTO và đếm giờ đứng yên, như popup tạm dừng). Hết thoại thì đánh dấu đã xem và trận chạy. Thua rồi Thử lại: không chiếu lại.
- **Thoại sau** (Battle): màn kết quả thắng vẫn nhận thưởng như hiện nay. Bấm Tiếp tục: nếu `AfterScene` chưa xem thì chiếu, hết thì đánh dấu đã xem rồi về bản đồ. Đã xem thì về bản đồ ngay.
- **Đã xem:** Story: là "màn đã clear" (server giữ, không thêm cờ). Battle: `PlayerSave.SeenStoryScenes` chứa `Id` đoạn thoại (mục 4.3).
- **Ô Cốt truyện:** danh sách đoạn thoại theo thứ tự `StageCatalog` (trước rồi sau của từng màn). Mở = Story: màn đã clear; Battle: đã xem. Chưa mở hiện "???" và gợi ý ("Thắng 1-5 để xem"), không chạm được. Chạm đoạn mở: xem lại (không gọi server, không đổi cờ).
- **Log:** nút ở góc trên, mở danh sách câu đã chiếu trong đoạn này. **Auto:** bật/tắt, tự sang câu sau `AutoLineSeconds` (khởi điểm 2,5 giây + 0,04 giây mỗi ký tự, config); chạm tay vẫn sang ngay; tắt khi mở Log.

## 4. Dữ liệu

### 4.1 Nơi để hội thoại: ScriptableObject, cùng kiểu quái và màn

Hội thoại là dữ liệu cố định của game (như `EnemyDefinition`, `StageDefinition`), nên là ScriptableObject do `ProjectSetup` tạo, hai ngôn ngữ theo [D-010](../../decisions/decisions.md) (`GameLanguage.Pick`). Không dùng `UiText` (bảng chữ UI, không hợp văn bản dài) và không dùng JSON/CSV ngoài (thêm đường nạp, không lợi).

| Loại | Nơi (`Assets/_Project/Combat/Scripts`) | Field |
|---|---|---|
| `SpeakerDefinition : GameDefinition` | mới | `_displayName`, `_displayNameEn`, `_tint` (Color, hình tạm), `_portrait` (Sprite, trống; để thay art sau) |
| `StoryLine` (`[Serializable]`) | mới | `_speaker` (SpeakerDefinition, trống = lời dẫn), `_side` (`Left`/`Right`), `_text`, `_textEn` |
| `StorySceneDefinition : GameDefinition` | mới | `_title`, `_titleEn` (cho danh sách), `_lines[]` |
| `StageDefinition` | sửa | thêm **cuối** hai field `_beforeScene`, `_afterScene` (StorySceneDefinition) và thuộc tính chỉ đọc |

Thêm cuối vì "giá trị số lưu trong asset: chỉ thêm ở cuối" (chú thích hiện có). Không thêm `StoryCatalog`: đoạn thoại được trỏ từ màn, ô Cốt truyện duyệt `StageCatalog`.

`Validate()` (mỗi lỗi bắt đầu bằng tên field, kiểu có sẵn):
- `StorySceneDefinition`: `Id` không trống; `Lines` ≥ 1; mỗi câu có `Text` và `TextEn` không trống; `Side` hợp lệ.
- `StageDefinition`: Story phải có `BeforeScene` và không có `AfterScene`; Battle: hai field tuỳ chọn; `AfterScene` ≠ `BeforeScene`; hai màn khác nhau không dùng chung một đoạn thoại.

### 4.2 Server không đổi

`CompleteStory` và `ClearedStages` giữ nguyên; `ServerDataExport` xuất `StageDefinition` theo field hiện có và **không** xuất hai field mới (test khẳng định, mục 8). Không đổi `Rules`, `GameData`, `ServerModule`. Đổi tên quái chỉ là tên và id asset phía máy (server không mô phỏng trận).

### 4.3 Lưu tiến độ: `PlayerSave.SeenStoryScenes` (D-005)

```csharp
// PlayerSave: id các đoạn thoại Battle đã tự chiếu hoặc đã xem. Story không dùng (xem ClearedStages).
public List<string> SeenStoryScenes = new();
```

| Luật D-005 | Cách đáp ứng |
|---|---|
| Dữ liệu cần lưu chỉ ở `PlayerSave`, đi qua `SaveService` | Field của `PlayerSave`; `StoryService.MarkSeen` ghi bằng hàm lưu có sẵn (cùng kiểu `AfkService`/`StaminaService`), không `PlayerPrefs` |
| Field mới có giá trị game mới đúng | Danh sách rỗng = chưa xem gì. Save cũ không có field vẫn nạp ra rỗng |
| Trạng thái chạy ngoài save tự reset | `StoryService` không giữ bản sao, đọc `_save.SeenStoryScenes` mỗi lần; `ResetToDefaults()` ghi đè `PlayerSave` nên tự xoá. Dialogue đang mở khi Reset: view đóng theo sự kiện reset có sẵn của hub |
| Online (D-014, đợt 2) | Nằm trong khoá `save` (do máy giữ), **không** ở `ServerState`; bản sao tải lên chỉ bỏ các field server giữ nên field này đi theo. `ResetPlayer` trên server không cần đổi. Cờ này không phải tiền hay thể lực nên sửa file save để xem lại thoại là vô hại |

Lệch giữa hai nơi (server đã clear 1-5 nhưng `SeenStoryScenes` rỗng, vd đổi máy khi save chưa kịp tải): lần thắng 1-5 kế thoại sau chiếu một lần. Chấp nhận.

## 5. UI

Ảnh mô phỏng user duyệt 2026-11-21 (hình chỉ là `Image` vuông/tròn có sẵn; tên object theo [unity-workflow](../../guides/unity-workflow.md)). Một component dùng chung `StoryDialogueView` cho cả scene Hub và scene Trận (mỗi scene có builder riêng nên một hàm dựng chung `StoryViewBuilder` gọi từ cả hai).

| Vùng | Tên object | Ghi chú |
|---|---|---|
| Phủ toàn màn | `StoryDialogue_PH` | `Image` đen alpha 0,45, **chặn chạm** xuống dưới, vẽ trên cùng (cả trên panel kín màn của hub và trận) |
| Ô người nói | `StoryPortraitLeft_PH_288x480`, `StoryPortraitRight_PH_288x480` | `Image` vuông, màu = `Speaker.Tint`, viền trắng, chữ tên nhỏ ở đáy ô. Người đang nói sáng (alpha 1), người kia mờ (alpha 0,4); lời dẫn thì cả hai mờ |
| Thanh tên | `StoryNameplate_PH_300x44` | trên khung thoại, dính bên người nói; ẩn khi lời dẫn |
| Khung thoại | `StoryTextBar_PH_1920x280` | sát đáy, nền tối alpha 0,85, viền trên sáng; chữ ~30 pt, tối đa 3 dòng; `▼` nhấp nháy góc phải dưới |
| Nút | `StorySkip_PH_160x56` (trên phải), `StoryAuto_PH_120x56`, `StoryLog_PH_120x56` (bên trái nút Bỏ qua) | Auto sáng khi bật. Log mở `StoryLogPanel_PH` (cuộn, tên + câu); chạm ngoài để đóng |
| Danh sách | `StoryListPage_PH` (trang hub, `IHubPage`) | hàng `StoryRow_PH_760x80` (ô vuông + tên + "Đã xem · chạm để xem lại"), hàng khoá mờ "???" |

Chữ (`UiText`, khu mới `story`, hai ngôn ngữ): "Bỏ qua/Skip", "Auto", "Log", "Chạm để xem lại/Tap to replay", "???" và gợi ý mở khoá (`story.locked_hint` = "Thắng {0} để xem" / "Win {0} to view"), tiêu đề trang. **Lời thoại và tên người nói không qua `UiText`** mà nằm trong asset (mục 4.1). Đổi ngôn ngữ nạp lại scene (trừ Trận) nên không cần cập nhật chữ trực tiếp; trong Trận đổi ngôn ngữ không có, nên không xét.

`HubNavState`: ô `Story` hết là `ComingSoon`, mở `StoryListPage_PH`; nút "Cốt truyện" trên thanh dưới hết dạng khoá. Màn Story trên bản đồ vẫn mở dialogue từ `StageMapView.OnNodeClicked` (thay `StoryPlaceholderPopup`).

**Art thật sau này:** chỉ gán `Speaker._portrait`; view dùng sprite nếu có, không thì ô màu. Không sửa code.

## 6. Cấu trúc code

| Thành phần | Nơi | Ghi chú |
|---|---|---|
| `SpeakerDefinition`, `StoryLine`, `StorySceneDefinition`, `StageDefinition` (+2 field) | `Combat/Scripts` | mục 4.1 |
| `PlayerSave.SeenStoryScenes` | `Core/Scripts` | mục 4.3 |
| `StoryService` | `Combat/Scripts` | thuần C#, tiêm `PlayerSave`, `StageCatalog`, `StageProgressService` (hỏi trạng thái màn), `Func<bool> persist`. API: `IsSeen(scene)`, `MarkSeen(scene)`, `NeedsBefore(stage)`, `NeedsAfter(stage)`, `Entries()` (danh sách cho ô Cốt truyện: scene, màn sở hữu, mở/khoá, gợi ý), `Changed` |
| `StoryDialogueView` + `StoryViewBuilder` | `UI/Scripts`, `Editor` | mục 5; chạy một `StorySceneDefinition`, API `Play(scene, onFinished)`, `IsOpen`; Log và Auto nằm trong view |
| `StoryListPage` | `UI/Scripts` | `IHubPage` |
| Nối vào trận | `BattleView` (thoại trước, `IsPaused`), `BattleResultView` (Tiếp tục → thoại sau) | không đổi `BattleSession`, `CombatService` |
| Gỡ | `StoryPlaceholderPopup` và phần dựng của nó, chữ `combat.story_empty` | thay bằng `StoryDialogueView` |
| Config | `CombatConfig` (Header "Cốt truyện") | `AutoBaseSeconds` 2,5, `AutoSecondsPerChar` 0,04 |

`StoryService` đăng ký vào `ServiceLocator` cùng chỗ các service Combat. Không cấp phát trong `Update` của view (lesson L-044): chỉ ghi chữ khi đổi câu.

## 7. Đổi tên quái (làm đồng bộ trong `ProjectSetup`)

Chỉ số, kỹ năng, thứ tự trong màn **giữ nguyên**; đổi tên hiển thị, id/file asset và bản dịch.

| Cũ (file / Việt / Anh) | Mới (file / Việt / Anh) | Chỉ số giữ | Kỹ năng |
|---|---|---|---|
| `Enemy_Fly` / Ruồi Bàn / Table Fly | `Enemy_Shrimplet` / **Tép Nhảy** / Leaping Shrimplet | 500·60·30·90 | — |
| `Enemy_Rat` / Chuột Bếp / Kitchen Rat | `Enemy_TigerPrawn` / **Tôm Sú** / Tiger Prawn | 900·85·45·100 | — |
| `Enemy_Roach` / Gián Kho / Storeroom Roach | `Enemy_ClawShrimp` / **Tôm Càng** / Claw Shrimp | 1300·110·60·85 | Cào mạnh (giữ) |
| `Enemy_RatKing` / Chuột Chúa / Rat King | `Enemy_ShrimpLord` / **Tôm Chúa** / Shrimp Lord | 7000·135·70·105 | Gầm (giữ; 🎨 có thể đổi tên chiêu sau) |

Asset đổi file bằng lệnh di chuyển của Unity (giữ GUID, không sửa tay `.meta`); `ProjectSetup` (bảng `EnemyTable`, `EnglishNames`) và test tham chiếu tên cũ (`ProjectSetupTests`, `CombatViewTextTests`) sửa theo. Spec và kế hoạch Combat cũ là lịch sử, **không sửa**.

## 8. Test

EditMode trong `BunDauPeo.Tests`; đồng hồ không cần. Danh sách cụ thể ở [kế hoạch](../plans/2026-11-21-story-ch1.md); tổng **+72 EditMode, +1 PlayMode** (ước tính, 🔨 báo số thật). Phủ: `Validate` của scene và màn (thiếu thoại, chữ trống, Story thiếu `BeforeScene`, dùng chung scene), JSON round-trip và save cũ của `SeenStoryScenes`, Reset xoá cờ, `ServerDataExport` không xuất field mới, `StoryService` (cần/không cần chiếu, đánh dấu một lần, danh sách mở/khoá), chạm qua câu và Bỏ qua, màn Story clear đúng một lần và xem lại không gọi server, thoại trước dừng trận (`IsPaused`), thoại sau chỉ sau Tiếp tục, ô Cốt truyện mở/khoá, Log và Auto, tên quái mới hai ngôn ngữ, không còn tên ruồi/chuột/gián trong dữ liệu.

## 9. Mức tối thiểu và mức đầy đủ (cắt nếu trễ)

| Mức | Gồm | Task |
|---|---|---|
| **Tối thiểu (lõi, không cắt)** | Dữ liệu thoại, hai màn Story chiếu hội thoại thật, đổi quái sang tôm, test | 1, 2, 3 |
| + Thoại trong trận | Thoại trước/sau 1-5, cờ `SeenStoryScenes` dùng thật | 4 |
| + Ô Cốt truyện | Danh sách xem lại, mở khoá ô thanh dưới | 5 |
| + Log/Auto | Hai nút | 6 |
| Luôn làm | Docs, kiểm cuối, báo cáo | 7 |

**Thứ tự cắt khi trễ:** Task 6 (Log/Auto) → Task 5 (ô Cốt truyện; ô thanh dưới quay lại "Sắp có") → Task 4 (thoại 1-5; `SeenStoryScenes` và hai field `StageDefinition` có thể ở lại hoặc bỏ). Cắt Task 4 không cần đổi gì ở Task 1–3 vì `NeedsBefore/After` trả false khi scene trống.

## 10. Playtest (người thử, Editor chế độ Local trước, rồi một lần bản UGS `development`)

1. Bản đồ: chạm "Truyện" → thoại, chạm qua từng câu, hết thì màn mở 1-1; mất mạng giữa chừng (UGS) → thoại giữ câu cuối, chạm lại được.
2. Đánh 1-1, 1-2 → Truyện 2 → 1-3, 1-4: toàn tôm, không còn ruồi/chuột/gián.
3. Vào 1-5: thoại trước sau khi vào trận (trận đứng yên), hết thì đánh được; thoát giữa thoại: thể lực đã mất. Thua, Thử lại: không chiếu lại.
4. Thắng 1-5 → Tiếp tục → thoại sau → bản đồ. Đánh lại 1-5 thắng: không chiếu lại.
5. Ô Cốt truyện: 4 đoạn xem lại được; trước khi đánh 1-5 hai dòng cuối "???".
6. Reset Save Data: hết cờ đã xem, ô Cốt truyện quay về khoá, Truyện 1 chiếu lại lúc đầu game.

## 11. Rủi ro và đã biết

- Thoại trước chiếu **sau** khi trừ thể lực, theo ý user; người thoát giữa thoại mất 10 thể lực. Thoại ngắn (≤ 8 câu) và có Bỏ qua để giảm thiệt.
- `BattleView` đã nhiều trách nhiệm; chỉ thêm một điểm mở thoại và gộp trạng thái vào `IsPaused`, không thêm vào `BattleSession`.
- `StageDefinition` là asset đã có: thêm field cuối, asset cũ nạp ra `null` (không thoại) nên không vỡ; không sửa tay `.asset`, dựng lại bằng `ProjectSetup`.
- `docs/release/drafts.md` ghi "đội nhân vật tôm của Péo", lệch với "đội ta không phải tôm": 🧭/📋 xem lại câu mô tả cửa hàng khi viết bản thật.
- `docs/vision/vision.md` còn ghi "Cốt truyện và hội thoại (sẽ làm sau MVP)" ở mục Ngoài phạm vi: user đã đưa vào bản 31/12 từ 2026-11-03, `TODO(user)` cập nhật vision (agent không tự sửa).
- Lời thoại là bản nháp của 🎨 (phụ lục); user sửa chữ không đụng code, chỉ asset do `ProjectSetup` dựng, nên sửa nội dung nằm ở bảng trong `ProjectSetup`.

## 12. Docs phải sửa (trong kế hoạch)

`roadmap.md` (đã làm / chuyển Đã xong), `game-design.md` mục Màn chơi (bỏ "giữ chỗ", ghi luật thoại), `screen-flow.md` (ô Cốt truyện, màn hội thoại), `docs/references/ui-screens.md` (dòng Cốt truyện), `docs/project/structure.md`, lesson nếu có.

## Phụ lục: lời thoại nháp (🎨 viết, chờ user sửa)

> **Nguồn lời thoại thật là [design/story-ch1.md](../../design/story-ch1.md)** (user sửa ở đó). Bản dưới chỉ là nháp ban đầu; khi hai bản khác nhau thì `story-ch1.md` thắng, 🔨 nạp từ đó vào `ProjectSetup`.

Người nói: **Péo** (trái), **Tép Nhảy** / **Tôm Chúa** (phải). Mỗi dòng: Việt / Anh.

**`story_1_open`** — "Mắm tôm dậy mùi" / "Shrimp Paste Rising" (Truyện 1, trước 1-1)
1. Péo: Chào buổi sáng! Hôm nay nhập lô mắm tôm mới, thơm nức cả phố. / Good morning! A fresh batch of shrimp paste, the whole street can smell it.
2. Péo: Ủa? Sao hũ mắm sủi bọt xanh lè thế này? / Huh? Why is the jar bubbling green?
3. Tép Nhảy: Xì xì... mắm... mắm... / Hiss... paste... paste...
4. Péo: Trời ơi, tôm bò ra khỏi hũ! Còn biết nói nữa?! / The shrimp are crawling out of the jar! And they talk?!
5. Tép Nhảy: Trả mắm cho bọn ta! / Give us back our paste!
6. Péo: Mắm của tôi mà... Thôi, nhân viên đâu, ra tiếp khách đặc biệt! / It's MY paste... Fine. Staff, serve our special guests!

**`story_1_x`** — "Hợp chất X" / "Compound X" (Truyện 2, sau 1-2, trước 1-3)
1. Péo: Mấy con tôm này mạnh hơn hẳn lúc nãy. / These shrimp are much stronger than before.
2. Péo: Hũ mắm có dán nhãn lạ: "Hợp chất X". Ai đã trộn thứ này vào? / The jar has a strange label: "Compound X". Who mixed this in?
3. Tép Nhảy: Hợp chất X... làm bọn ta tỉnh dậy. Tôm Chúa đang gọi. / Compound X... woke us up. Lord Shrimp is calling.
4. Péo: Tôm Chúa? Nghe tên đã thấy mặn rồi. Đi thôi! / Shrimp Lord? Sounds salty already. Let's go!

**`story_1_boss_before`** — "Chum mắm của Tôm Chúa" / "The Lord's Paste Vat" (Trước 1-5)
1. Tôm Chúa: Ai dám bén mảng đến chum mắm của ta? / Who dares approach my vat of paste?
2. Péo: Mắm của tôi! Trả đây! / That's MY paste! Give it back!
3. Tôm Chúa: Hợp chất X cho ta sức mạnh. Ngươi chỉ là người bán bún! / Compound X gave me power. You are just a noodle seller!
4. Péo: Bán bún thì sao? Cả đội, lên! / So what if I sell noodles? Team, charge!

**`story_1_boss_after`** — "Hũ mắm sạch lại" / "A Clean Jar" (Sau 1-5)
1. Tôm Chúa: Ta... thua... một quán bún đậu sao... / I... lost... to a tofu-and-noodle shop...
2. (lời dẫn) Hợp chất X tan khỏi mắm tôm, hũ mắm trong trở lại. / Compound X faded from the paste, and the jar turned clear again.
3. Péo: Xong rồi... Nhưng ai đã bỏ thứ đó vào mắm của mình? / It's over... But who put that in my paste?
4. (lời dẫn) Hết Chương 1. / End of Chapter 1.
