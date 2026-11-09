# Lessons — sổ bài học

> **Đọc khi:** trước khi sửa code/asset, **chỉ phần có tag liên quan**: `Grep "#tag" docs/lessons/lesson.md -A 4` (tag nằm ở dòng tiêu đề, thiếu `-A` thì chỉ ra tiêu đề), đừng đọc cả file.
> **Cập nhật khi:** agent làm sai, user chỉnh lại, hoặc phát hiện một cái bẫy của dự án. Ghi ngay trong task đó.

## Tag dùng chung

`#unity-files` `#scene-prefab` `#script` `#physics` `#input` `#ui` `#art-import` `#build` `#git` `#docs` `#process`
(Thêm tag mới ở đây trước khi dùng.)

## Cách ghi

- Thêm mục mới ở **đầu** mục "Đang hiệu lực" (mới nhất trên cùng).
- Viết thành **luật hành động được**, không kể lể. Một mục ≤ 6 dòng.
- Nâng thành luật khi lesson lặp lại ≥ 2 lần **hoặc** là quy ước áp cho mọi code/task sau: chép luật vào `guides/` (ghi số lesson trong ngoặc), rồi rút mục ở đây thành một dòng trong "Đã nâng thành luật".
- Dọn sổ: reviewer cuối nhánh làm bước nâng luật này trước khi coi nhánh là xong, để mục "Đang hiệu lực" luôn ngắn.

## Mẫu

```
### L-NNN — Tiêu đề ngắn (YYYY-MM-DD)  #tag1 #tag2
- Triệu chứng: điều đã xảy ra sai
- Nguyên nhân gốc:
- Luật từ nay: làm X / không làm Y
```

## Đang hiệu lực

### L-034 — MonoBehaviour giả trong assembly Tests không gắn được (2026-11-09)  #script #process
- Triệu chứng: `FakeHubPage : MonoBehaviour` trong `Assets/_Project/Tests` làm 4 test `HubNavTests` đỏ: "Can't add script behaviour 'FakeHubPage' because it is an editor script"; `AddComponent` trả null nên `_pages` không có phần tử.
- Nguyên nhân gốc: `BunDauPeo.Tests` chỉ có ở Editor (`includePlatforms: Editor`), nên mọi MonoBehaviour khai trong đó đều là "editor script".
- Luật từ nay: test EditMode cần một giao diện của view thì tách phần logic thành hàm tĩnh nhận `IEnumerable<IGiaoDien>` (như `HubNav.ClosePages`) và dùng lớp giả thuần C#; không khai MonoBehaviour giả trong assembly Tests.
### L-034 — Chụp Canvas ở scale mới nhưng chữ cố định giữ raster cũ (2026-11-10) #ui #process
- Triệu chứng: ảnh 1920×1080 có artwork/header rõ nhưng chữ skill và nút Menu mềm dù font cố định 21; tắt BestFit không giải quyết.
- Bằng chứng: lúc capture, Game view 640×480, canvas/renderingDisplaySize đã 1920×1080, scaleFactor/pixelsPerUnit=1. CanvasScaler có callback preWillRenderCanvases, nên không thiếu lượt Update; nhãn kích thước cố định không bị dirty như nhãn stretch.
- Sửa: sau ForceUpdateCanvases cập nhật scale, SetVerticesDirty cho Text của các canvas vừa đổi rồi ForceUpdateCanvases lần nữa. Khi trả Overlay cũng dựng lại chữ theo scale cũ. Chỉ tác động công cụ chụp, không sửa font/scale gameplay để che lỗi ảnh QA.

### L-033 — Mô phỏng UI để popup thành hộp giữa màn, user sửa thành kín màn (2026-11-09)  #ui #process
- Triệu chứng: mô phỏng hub bản 1 có banner Gacha và Menu là hộp giữa màn, các popup con giữ cỡ cũ; user sửa: popup phải che kín màn, chỉ hộp xác nhận và phần thưởng giữ cỡ.
- Nguyên nhân gốc: 📐 lấy cỡ popup đang có làm mặc định, không hỏi luật kín màn trước khi vẽ.
- Luật từ nay: popup và trang mới mặc định kín 1920×1080 (thanh dưới vẫn hiện); chỉ popup xác nhận và phần thưởng là hộp, nền mờ kín màn che cả thanh ([spec UI hub 5.5](../superpowers/specs/2026-11-09-ui-hub-design.md)).
### L-029 — Kế hoạch ghi test EditMode cho `OnEnable`, nhưng EditMode không gọi `OnEnable` của MonoBehaviour thường (2026-11-07)  #script #process
- Triệu chứng: `LocalizedTextTests` theo kế hoạch (tạo GameObject, bật lại, đọc `Text`) đỏ vì chữ vẫn rỗng; code đúng.
- Nguyên nhân gốc: ngoài Play, Unity chỉ chạy `Awake`/`OnEnable` cho lớp có `[ExecuteAlways]`. Thêm `ExecuteAlways` chỉ để qua test thì `OnEnable` chạy cả khi mở scene trong Editor.
- Luật từ nay: test EditMode cần chạy `OnEnable` thì gọi hàm đó bằng reflection (`GetMethod("OnEnable", NonPublic | Instance)`), không thêm `ExecuteAlways`. Việc "bật lên Unity có gọi thật" phủ bằng test PlayMode hoặc test đọc scene.
### L-026 — `Capture1080("tên")` không tự thêm đuôi `.png` (2026-11-06)  #ui #process
- Triệu chứng: lúc rà layout, `Capture1080("lm-restaurant")` ghi ra file `lm-restaurant` không đuôi; công cụ đọc ảnh không mở được, phải đổi tên tay.
- Nguyên nhân gốc: `ScreenCapture1080.Capture` chỉ dùng `Path.GetFileName(fileName)`, tên mặc định mới có `.png`.
- Luật từ nay: truyền tên có đuôi (`Capture1080("x.png")`). Sửa công cụ để tự thêm `.png` khi thiếu là việc nhỏ, chờ 🎯 xếp.

### L-022 — Lệnh Bash dài có heredoc Python lẫn tiếng Việt báo `unexpected EOF` và không chạy gì (2026-11-03)  #process
- Triệu chứng: ba lần `python - <<'EOF'` sửa nhiều file một lượt thoát mã 2 ("unexpected EOF while looking for matching `''`"), không file nào đổi; chỉ thấy khi kiểm `git status`.
- Nguyên nhân gốc: lệnh Bash nhiều chục dòng chứa chuỗi ba nháy, nháy đơn và tiếng Việt bị shell phân tích sai trước khi tới Python.
- Luật từ nay: tạo file bằng công cụ Write, sửa chỗ lớn bằng công cụ Edit; Bash chỉ cho thay thế ngắn. Sau lệnh sửa hàng loạt, kiểm `git diff --stat` có đúng file đổi trước khi chạy tiếp.

### L-021 — Test "màn hỏng" dựng config hỏng nên service không chạy, và số thô của thể lực chưa chốt thì lệch (2026-11-03)  #script #process
- Triệu chứng: ca `Invalid` của `EnterStage`/`CanEnter` dựng catalog có màn hỏng ngay từ đầu nên `CombatService.IsRunning = false` (`NullReferenceException`, log "invalid config"); và so `Save.Stamina.Current` thô trước/sau ra 0 so với 100 dù không đổi gì.
- Nguyên nhân gốc: `CombatConfig.Validate()` gom lỗi của mọi màn nên màn hỏng làm cả config hỏng; số thô trong save chỉ được chốt (đầy lúc `Anchor == 0`) khi có người đọc `Stamina.Current`.
- Luật từ nay: muốn thử trạng thái "màn hỏng" thì dựng service với màn hợp lệ rồi `TestData.Edit` làm hỏng sau; so số thô của thể lực thì đọc `Stamina.Current` một lần trước để chốt.

### L-001 — Spec tự vi phạm luật phụ thuộc assembly (2026-10-30)  #process #script
- Triệu chứng: spec nền móng đặt `StageDefinition` (Progression) trỏ tới `EnemyDefinition` (Combat) trong khi cấm hai assembly này phụ thuộc nhau; thiếu assembly Editor, thời điểm lưu save.
- Nguyên nhân gốc: viết bảng SO theo từng mảng, không kiểm từng tham chiếu với sơ đồ phụ thuộc.
- Luật từ nay: trước khi chốt spec kiến trúc, đối chiếu từng tham chiếu giữa các SO với sơ đồ assembly; save phải nói rõ khi nào ghi và ghi thế nào để không mất dữ liệu. (Phần "code `UnityEditor` nằm trong assembly Editor" đã vào code-style.)

## Đã nâng thành luật

Nội dung đầy đủ nằm trong lịch sử git của file này (trước 2026-11-03; L-025, L-027, L-028 ở commit `eef6ade`). Luật hiện hành ở file được trỏ tới.

- L-032 (2026-11-09) Test giữ biến asset qua `ProjectSetup.Run()` (dựng scene bằng `NewScene(Single)` dỡ asset): `finally` ném lỗi và để id `renamed_by_test` trên đĩa, 9 test đỏ theo (lặp L-002, lần này ở test)  #unity-files #script #process → [code-style, Test](../guides/code-style.md#test)
- L-030, L-031 (2026-11-08, `#process`): id kênh lấy bằng `get_session` với `"self"`, không suy từ đường dẫn scratchpad hay transcript → [session-brief.md](../guides/session-brief.md), skill worker (Báo cáo cuối kênh), skill coordinator (Mở kênh con).

- L-028 (2026-11-06) Play đứng ở khung 2 khi cửa sổ Editor không ở trước, dù đã bật chạy nền (lặp L-009)  #process #unity-files → [unity-workflow](../guides/unity-workflow.md#công-cụ-chính-unity-cli-unity)
- L-027 (2026-11-06) Test đè nút chỉ xét nút đang bật bỏ sót nút Bỏ qua của Gacha (lặp L-025)  #ui #process → [code-style, Test](../guides/code-style.md#test)
- L-025 (2026-11-06) Nút DEV đè nút của màn game vì `DevOverlay` vẽ trên cùng  #ui #process → [unity-workflow, UI và hình tạm](../guides/unity-workflow.md#ui-và-hình-tạm), [code-style, Test](../guides/code-style.md#test)
- L-024 (2026-11-04) Kịch bản smoke test đoán theo builder: màn đầu là Truyện, `DevOverlay` không có trong test PlayMode  #process #ui → [unity-workflow, Công cụ thử tự động](../guides/unity-workflow.md#công-cụ-thử-tự-động)
- L-023 (2026-11-03) `BootScenePlayMode` làm `unity test --mode PlayMode` treo và ghi save thật  #unity-files #process → [unity-workflow, Công cụ thử tự động](../guides/unity-workflow.md#công-cụ-thử-tự-động), [D-009](../decisions/decisions.md)
- L-020 (2026-11-03) `unity status && unity run` cắt vế sau; log cũ trông như mới  #process #unity-files → [unity-workflow](../guides/unity-workflow.md#công-cụ-chính-unity-cli-unity)
- L-019 (2026-11-03) Play không qua Boot khi Editor mở bằng CLI  #process #unity-files → [unity-workflow](../guides/unity-workflow.md)
- L-018 (2026-11-02) `Random` mơ hồ giữa `System` và `UnityEngine`  #script → [code-style](../guides/code-style.md)
- L-017 (2026-11-02) Commit nhiều dòng có `"` bị PowerShell 5.1 cắt  #git #process → [unity-workflow, mục Git](../guides/unity-workflow.md#git)
- L-016 (2026-11-02) `Validate()` thiếu field làm NaN hoặc làm Boot ném lỗi  #script #process → [code-style, Service](../guides/code-style.md#service-tiền-và-save)
- L-015 (2026-11-02) Cờ bool chặn dùng chung cho nhiều chủ  #script #ui → [code-style, Service](../guides/code-style.md#service-tiền-và-save)
- L-014 (2026-11-02) Lệnh thử đổi trạng thái rồi quên khôi phục  #process → [unity-workflow](../guides/unity-workflow.md)
- L-013 (2026-11-02) `GetComponentInParent` bỏ qua đối tượng tắt  #script #unity-files → [code-style, Test](../guides/code-style.md#test)
- L-012 (2026-11-02) `[TestCase]` làm tổng số test lệch kế hoạch  #process #script → [code-style, Test](../guides/code-style.md#test)
- L-011 (2026-11-02) Chụp màn bằng camera `targetTexture` 1920×1080; `unity close .` quá 30 giây  #ui #process → [unity-workflow](../guides/unity-workflow.md)
- L-010 (2026-11-02) Ảnh `capture game_view` bị bóp 16:9; cách chụp của mục này đã bị L-011 thay  #ui #process → [unity-workflow](../guides/unity-workflow.md)
- L-009 (2026-11-01) Play đứng im khi Editor không focus  #process #unity-files → [unity-workflow](../guides/unity-workflow.md)
- L-008 (2026-11-01) Dung sai test rộng hơn một bước che lỗi lệch bước  #process #script → [code-style, Test](../guides/code-style.md#test)
- L-007 (2026-11-01) Tưởng lỗi vì độ trễ lệnh CLI; bật chạy nền ghi vào ProjectSettings  #process #unity-files → [unity-workflow](../guides/unity-workflow.md)
- L-006 (2026-10-31) Test đặt tên theo tính chất nhưng vẫn xanh khi tính chất sai  #process #script → [code-style, Test](../guides/code-style.md#test)
- L-005 (2026-10-31) `git add` từng file `.cs` bỏ sót `.meta`  #git #unity-files → [unity-workflow, mục Git](../guides/unity-workflow.md#git)
- L-004 (2026-10-31) Kết luận cơ chế khi chưa tách biến  #process #unity-files → [unity-workflow](../guides/unity-workflow.md)
- L-003 (2026-10-31) Code tiền/save nuốt lỗi IO và bắn event trần  #script → [code-style, Service](../guides/code-style.md#service-tiền-và-save)
- L-002 (2026-10-30) Tham chiếu asset giữ qua `NewScene(Single)` thành null  #unity-files #scene-prefab #script → [code-style, Code Editor](../guides/code-style.md#code-editor), [code-style, Test](../guides/code-style.md#test) (lặp ở L-032)
