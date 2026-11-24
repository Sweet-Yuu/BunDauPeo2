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

### L-047 — `GetOrCreateAsset` không áp init lên asset đã có; đổi builder thì scene phải dựng lại (2026-11-24)  #unity-files #scene-prefab
- Thêm dòng dữ liệu mới vào `init` của `ProjectSetup` (ví dụ thưởng thuốc cho `st_1_*`) không chạm asset đã tồn tại; test dữ liệu đỏ dù code đúng. Cần bước vá riêng, idempotent, chỉ thêm dòng còn thiếu (xem `EnsureExpPotionRewards`), và test bảng mẫu (`SampleStages_MatchSpecTable`) phải cập nhật cùng lúc.
- Đổi `*SceneBuilder` rồi chạy test scene mà chưa `RebuildRestaurantScene` thì test đọc file scene cũ và đỏ ("field null"): dựng lại scene trước khi chạy EditMode.

### L-046 — Overlay tĩnh giữ danh sách `Text` đã bị huỷ; plugin Unity ngoài đòi bản Unity mới hơn (2026-11-24)  #ui #build #process
- Overlay dựng bằng code, sống qua đổi scene (`AccountBoxes`, `OnlineOverlay`) mà giữ danh sách `Text` trong static: test dọn object thì danh sách còn trỏ vào cái đã huỷ (`MissingReferenceException` ở lần chạy sau, chỉ lộ khi chạy cả bộ). Khi dựng lại hộp (`_root == null` theo Unity) phải xoá danh sách đó.
- Plugin ngoài (Sign in with Google của Google) ghi "Unity 6.5+" thì build Android trên Unity 6.3 vỡ vì thiếu Kotlin cho `.androidlib` (AGP cũ). Đọc ghi chú bản Unity và gradle template của plugin trước khi nhập; thử xong mà vỡ thì gỡ sạch (manifest, `Assets/Plugins`, `ProjectSettings/AndroidResolverDependencies.xml`) và báo, không vá gradle đoán mò (D-011).

### L-045 — Danh sách cuộn chỉ kéo được khi bấm trúng nút trong hàng, và lăn chuột không chạy (2026-11-24)  #ui #input
- Triệu chứng: trang Nhiệm vụ phải ấn đúng nút của một dòng rồi vuốt mới cuộn; vuốt trên chữ hay khe giữa hai dòng thì đứng yên; lăn chuột không cuộn (user playtest 1, bản itch.io chạy trên máy tính).
- Nguyên nhân gốc: `ScrollRect` chỉ nhận kéo và lăn khi điểm chạm trúng một `Graphic` raycast bên dưới nó; hàng nền và khung cuộn đều `raycast: false` nên chỉ nút (Graphic duy nhất) bắt được sự kiện rồi chuyển lên `ScrollRect`. Tick lăn của Input System còn nhỏ nên độ nhạy mặc định (1) cuộn không thấy.
- Luật từ nay: dựng danh sách cuộn trong builder thì gọi `GachaSceneBuilder.ConfigureScroll(scroll)` (Image alpha ~0,004 raycast trên chính `ScrollRect`, `cullTransparentMesh = false`, `scrollSensitivity` 40); alpha 0 hẳn thì Canvas bỏ qua khi raycast. Có `ScrollListTests` đọc scene và PlayMode raycast từ dòng, nút, khe giữa hai dòng.

### L-044 — `Update` của view dùng LINQ, `foreach` qua interface, `new` và chuỗi mỗi khung: ~1 KB/khung (2026-11-18)  #script #ui
- Triệu chứng: Profiler đo ~1,5 KB GC mỗi khung ở mọi màn (ngưỡng 0 B khi đứng yên, ≤ 1 KB trong trận). Một phần là Editor (khung trống cũng ~740 B); phần của code: `_pages.OfType<>()` và `Where().ToList()` trong HubNav/BattleSession, `foreach` qua `IReadOnlyList`/`IEnumerable`, `new StaminaState` trong `StaminaService`, `new bool[]` mỗi khung, và chuỗi dựng lại mỗi khung (`WaveLabel`, `SpeedLabel`, `Substring`).
- Nguyên nhân gốc: viết view theo kiểu "vẽ lại mỗi khung, rẻ" mà không đo; `GetAllocatedBytesForCurrentThread` trả 0 trong Unity nên đo nhầm là sạch.
- Luật từ nay: trong `Update` chỉ dùng vòng `for` theo chỉ số, danh sách và mảng dựng sẵn, và chỉ gán chuỗi khi giá trị đổi. Đo bằng `ProfilerRecorder` "GC Allocated In Frame" (`GcMeasureTests`), trừ khung trống làm nền.

### L-043 — Cổng một-lệnh giữ cờ bận qua một phiên bị bỏ thì kẹt mãi (2026-11-18)  #script #process
- Triệu chứng: lệnh mất trả lời, hộp Mất kết nối bị hộp Máy khác thay, Về Tiêu đề; `Busy` vẫn true, mọi lệnh sau đó nhận lỗi im lặng.
- Nguyên nhân gốc: lời hứa "Thử lại" chỉ được trả khi người chơi bấm nút; mọi đường thoát khác (`Shutdown`) không biết cổng đang chờ.
- Luật từ nay: thứ chờ người bấm thì mọi đường bỏ phiên phải huỷ nó (`CommandGate.Abort`). Test ca "bỏ giữa chừng" cho mọi cờ bận, cả khi đang gửi lẫn khi hộp đang hiện.

### L-042 — Sửa .cs khi `unity test` đang biên dịch làm lượt chạy hỏng (2026-11-17)  #process #script
- Triệu chứng: lệnh `unity test` chạy nền báo `CS0103/CS1061` ở file tôi đang sửa dở, mất cả lượt (vài phút) mà chẳng kiểm được gì.
- Nguyên nhân gốc: Unity đọc nguồn lúc biên dịch đầu lượt; sửa tiếp trong lúc đó cho nó thấy bản nửa vời.
- Luật từ nay: đã bắt đầu `unity test`/`unity run` thì không sửa `.cs` cho tới khi nó xong (sửa docs thì được); gom xong cả nhóm sửa rồi mới chạy một lần.

### L-041 — Test service qua LocalGameServer không được khẳng định kết quả roll (2026-11-17)  #script
- Triệu chứng: `Pull_Ok_AppliesStateAndRaisesChanged` đỏ ngẫu nhiên (pity mong 1, thực 0).
- Nguyên nhân gốc: `LocalGameServer` roll bằng `System.Random` thật; 5% quay trúng SSR thì pity về 0.
- Luật từ nay: test service (Gacha) chỉ khẳng định thứ không phụ thuộc roll (Gem, số dòng, khớp với `server.State`); ca cần roll cụ thể thuộc `HandlerGachaTests` (random giả có kịch bản, L-006).

### L-040 — Trả cài đặt ký bằng setter làm Unity ghi rác vào ProjectSettings.asset (2026-11-16)  #build #editor
- Triệu chứng: sau `BuildRelease` thật, `AndroidKeystoreName` trong `ProjectSettings.asset` thành `'{inproject}: '` dù `Restore()` đã gán lại giá trị cũ (rỗng, kể cả `null`); test đọc file đỏ.
- Nguyên nhân gốc: setter `PlayerSettings.Android.keystoreName` luôn chuẩn hoá đường dẫn, không giữ được chuỗi rỗng.
- Luật từ nay: trả lại cài đặt Android mà setter chuẩn hoá bằng `SerializedObject` của `PlayerSettings`; test kiểm giá trị `SerializedObject` sau `Apply`/`Restore`, và `git diff ProjectSettings` sau build chỉ có dòng định đổi.

### L-039 — Dòng chữ dài thay đổi (ID người chơi) dựng đúng ảnh mô phỏng nhưng co chữ không đọc được (2026-11-15)  #ui #process
- Triệu chứng: Hồ sơ đặt ID và nút Sao chép cùng hàng như ảnh mô phỏng; ô ID còn 260 px nên ID 38 ký tự co về ~14 px, chỉ thấy khi xem ảnh chụp. User chọn cho ID một dòng riêng.
- Nguyên nhân gốc: ảnh mô phỏng vẽ ID rút gọn ("4fK2…"), không ai tính bề rộng của chuỗi dài nhất (ID UGS ~28 ký tự, ID `LocalGameServer` 38 ký tự).
- Luật từ nay: chữ có độ dài thay đổi (ID, tên, số lớn) thì trước khi dựng, tính bề rộng chuỗi dài nhất ở cỡ chữ đã chốt (Arial ~0,5 em mỗi ký tự) so với ô; không vừa thì hỏi user trước. Xem ảnh chụp có chuỗi thật dài nhất, không chỉ chuỗi mẫu.

### L-038 — Gọi UGS thật từ Editor mở bằng CLI: 403 rồi "chỉ khởi tạo trong Play Mode" (2026-11-15)  #process #unity-files
- Triệu chứng: O1-a đọc danh sách môi trường bị 403 (Editor từ `unity open` chưa đăng nhập tài khoản Unity, `CloudProjectSettings.projectId` rỗng); sau khi user đăng nhập, project chỉ có `production`; gọi `UgsGameServer` bằng `unity command script run` ở Edit mode thì `ServicesInitializationException`.
- Nguyên nhân gốc: Deployment và Environments cần Editor đã đăng nhập; `UnityServices.InitializeAsync` từ chối Edit mode.
- Luật từ nay: trước khi deploy, chạy script đọc `EnvironmentsApi` (refresh) để thấy `projectId`, người dùng và đủ môi trường; thiếu thì nhờ user đăng nhập trong cửa sổ Editor hoặc tạo môi trường, không tự làm. Gọi UGS thật thì vào Play (Boot chỉ tới Tiêu đề), `EnableBackgroundPlay()`, rồi `script run` từ file ngoài repo; thoát Play và kiểm `git status`.

### L-036 — Test chạm gửi sự kiện thẳng lên nút không bắt được nút bị che (2026-11-11)  #ui #process
- Triệu chứng: review cuối nhánh UI hub B thấy banner Gacha che "!" Thu ngân ở 16:10 và 4:3 mà mọi test xanh. `Swipe` của test kéo quán tự đặt `eligibleForClick = false`, ca bấm "!" dùng `ClickDirect` (`ExecuteEvents` thẳng lên nút).
- Nguyên nhân gốc: giả lập chạm bỏ qua raycast và chuỗi pointerDown → kéo → pointerUp của input module, nên "nút bấm được" và "kéo không bấm nhầm" luôn đúng dù code làm gì.
- Luật từ nay: test PlayMode mà điều cần kiểm là chạm hay kéo thì đi qua raycast thật (`UiDriver.Click`, `Swipe` của `SmokeLoopTests`), không `ExecuteEvents` thẳng lên nút. Nút trong `World` có thể bị HUD neo góc che tuỳ tỉ lệ màn: kèm test EditMode xét 16:9, 16:10, 4:3 (`IncidentBubbles_ClearOfHudButtons_AtEveryAspect`).

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

- L-037 (2026-11-13) Test đọc Build Settings, Boot, Title và chỉ xanh sau khi `ProjectSetupTests` gọi `ProjectSetup.Run()` ghi lại file; `Rebuild` bị chặn quyền ở C1 nên lượt test đầu đỏ và tự sinh file đem commit (lần thứ 3 sau L-002, L-032)  #unity-files #process → [code-style, Test](../guides/code-style.md#test)
- L-035 (2026-11-11) Ảnh chụp để Huỷ trả về còn sống qua Reset, Huỷ ghi người đã mất vào save mới  #script #process → [code-style, Service, tiền và save](../guides/code-style.md#service-tiền-và-save)
- L-026 (2026-11-06) `Capture1080("tên")` không tự thêm đuôi `.png` (lặp ở UI hub B)  #ui #process → đã sửa công cụ: `ScreenCapture1080` tự thêm `.png`, có test; không còn luật
- L-029, L-034 (2026-11-07, 2026-11-09) Test EditMode với MonoBehaviour: `OnEnable` không chạy ngoài Play, MonoBehaviour giả trong assembly Tests không gắn được  #script #process → [code-style, Test](../guides/code-style.md#test)
- L-033 (2026-11-09) Mô phỏng UI để popup thành hộp giữa màn, user sửa thành kín màn  #ui #process → [unity-workflow, UI và hình tạm](../guides/unity-workflow.md#ui-và-hình-tạm)
- L-032 (2026-11-09) Test giữ biến asset qua `ProjectSetup.Run()` (dựng scene bằng `NewScene(Single)` dỡ asset): `finally` ném lỗi và để id `renamed_by_test` trên đĩa, 9 test đỏ theo (lặp L-002, lần này ở test; lặp tiếp ở L-037)  #unity-files #script #process → [code-style, Test](../guides/code-style.md#test)
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
- L-002 (2026-10-30) Tham chiếu asset giữ qua `NewScene(Single)` thành null  #unity-files #scene-prefab #script → [code-style, Code Editor](../guides/code-style.md#code-editor), [code-style, Test](../guides/code-style.md#test) (lặp ở L-032, L-037)
