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

### L-024 — Kế hoạch smoke test viết theo builder mà không xem scene chạy thật: màn đầu là Truyện, `DevOverlay` không có trong test (2026-11-04)  #process #ui
- Triệu chứng: lượt PlayMode đầu của smoke test đỏ ở bước 3: sau `Node0` không có `ChallengeButton`, chỉ có `ContinueButton` (popup Truyện); và nút "Màn chơi" của `DevOverlay` không tồn tại khi test chạy.
- Nguyên nhân gốc: `Stages[0]` là màn Truyện (Kind Story) và màn Battle sau nó chỉ mở khi Truyện clear; `DevOverlay.Build` chạy ở `AfterSceneLoad` lúc Play khởi động, khi test PlayMode chưa nạp Boot nên chưa có service và nó bỏ qua.
- Luật từ nay: kế hoạch có kịch bản UI thì tra thứ tự màn trong asset (`Kind` từng màn) và đọc thông điệp lỗi `UiDriver` (nó liệt kê nút đang hiện) trước khi kết luận; test PlayMode vào màn bằng `SceneManager.LoadScene`, không trông chờ `DevOverlay`.

### L-023 — `BootScenePlayMode` làm `unity test --mode PlayMode` treo vì Play vào Boot thay cho scene của test (2026-11-03)  #unity-files #process
- Triệu chứng: lượt PlayMode đầu tiên chạy 15 phút không có file kết quả rồi bị `unity` tắt; log chỉ thấy nạp scene Boot và Restaurant, không có test nào chạy; save thật bị ghi lại (10:59) vì `GameBootstrap` chạy với `persistentDataPath` thật.
- Nguyên nhân gốc: `BootScenePlayMode` đặt `EditorSceneManager.playModeStartScene = Boot` sau mỗi lần nạp lại domain, đè scene khởi tạo của Test Runner. Tách biến: thêm một guard `-runTests` trong `Init` thì cùng test xanh ngay (1/1).
- Luật từ nay: `BootScenePlayMode` đặt `playModeStartScene = null` khi dòng lệnh có `-runTests`. Chạy PlayMode từ cửa sổ Test Runner trong Editor thì guard này không phủ; nếu hay chạy kiểu đó, đổi sang đăng ký `TestRunnerApi` callbacks. Mọi lượt PlayMode chạy khi chưa có guard đều có thể đã ghi vào save thật: sao lưu `save.json*` trước (Task 3 `SaveBackup`).

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

Nội dung đầy đủ nằm trong lịch sử git của file này (trước 2026-11-03). Luật hiện hành ở file được trỏ tới.

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
- L-002 (2026-10-30) Tham chiếu asset giữ qua `NewScene(Single)` thành null  #unity-files #scene-prefab #script → [code-style, Code Editor](../guides/code-style.md#code-editor)
