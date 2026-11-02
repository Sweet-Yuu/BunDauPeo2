# Lessons — sổ bài học

> **Đọc khi:** trước khi sửa code/asset, **chỉ phần có tag liên quan** (dùng `Grep "#tag" docs/lessons/lesson.md`, đừng đọc cả file).
> **Cập nhật khi:** agent làm sai, user chỉnh lại, hoặc phát hiện một cái bẫy của dự án. Ghi ngay trong task đó.

## Tag dùng chung

`#unity-files` `#scene-prefab` `#script` `#physics` `#input` `#ui` `#art-import` `#build` `#git` `#docs` `#process`
(Thêm tag mới ở đây trước khi dùng.)

## Cách ghi

- Thêm mục mới ở **đầu** danh sách (mới nhất trên cùng).
- Viết thành **luật hành động được**, không kể lể. Một mục ≤ 6 dòng.
- Lesson lặp lại ≥ 2 lần → nâng thành luật trong `guides/` hoặc `CLAUDE.md`, rồi ghi `Promoted to: <file>` vào mục.

## Mẫu

```
### L-NNN — Tiêu đề ngắn (YYYY-MM-DD)  #tag1 #tag2
- Triệu chứng: điều đã xảy ra sai
- Nguyên nhân gốc:
- Luật từ nay: làm X / không làm Y
```

## Danh sách

### L-021 — Test "màn hỏng" dựng config hỏng nên service không chạy, và số thô của thể lực chưa chốt thì lệch (2026-11-03)  #script #process
- Triệu chứng: ca `Invalid` của `EnterStage`/`CanEnter` dựng catalog có màn hỏng ngay từ đầu nên `CombatService.IsRunning = false` (`NullReferenceException`, log "invalid config"); và so `Save.Stamina.Current` thô trước/sau ra 0 so với 100 dù không đổi gì.
- Nguyên nhân gốc: `CombatConfig.Validate()` gom lỗi của mọi màn nên màn hỏng làm cả config hỏng; số thô trong save chỉ được chốt (đầy lúc `Anchor == 0`) khi có người đọc `Stamina.Current`.
- Luật từ nay: muốn thử trạng thái "màn hỏng" thì dựng service với màn hợp lệ rồi `TestData.Edit` làm hỏng sau; so số thô của thể lực thì đọc `Stamina.Current` một lần trước để chốt.

### L-020 — `unity status && unity run …` không chạy lệnh sau khi không có Editor, và log cũ trông như log mới (2026-11-03)  #process #unity-files
- Triệu chứng: `unity status && unity run . -- -executeMethod …RebuildBattleScene` thoát mã 12 mà scene không đổi; tôi đọc `../bdp-setup.log` còn từ lần trước ("Exiting batchmode successfully") và suýt coi là đã dựng xong.
- Nguyên nhân gốc: `unity status` thoát 12 khi không có Editor nào (đúng ý khi Editor phải đóng), nên `&&` cắt chuỗi; file log ghi đè chỉ khi lệnh `run` thật sự chạy.
- Luật từ nay: `unity status` là một lệnh riêng, đọc kết quả rồi mới chạy `test`/`run`; sau `run` kiểm `git status` có file đổi (hoặc giờ sửa của log) trước khi tin log.

### L-019 — Play không qua Boot khi Editor mở bằng CLI: không có dịch vụ, không có dải DEV (2026-11-03)  #process #unity-files
- Triệu chứng: `unity open .` rồi `editor.playmode start` chạy scene Untitled; `eval` báo "Service CombatService is not registered", không có `DevOverlay`. Nạp `Boot` bằng `SceneManager.LoadScene` trong Play thì có dịch vụ nhưng vẫn không có dải DEV (nó chỉ dựng ở lần nạp scene đầu tiên).
- Nguyên nhân gốc: Editor mới mở chưa đặt Boot làm scene khởi động cho Play; `DevOverlay` dựng ở `AfterSceneLoad` đầu tiên và thoát sớm nếu chưa có dịch vụ.
- Luật từ nay: trước khi Play, `unity command scene open Assets/_Project/Scenes/Boot.unity` rồi `unity command editor.playmode start`; kiểm `GameObject.Find("DevOverlay")` có trước khi thử gì.

### L-018 — `Random` mơ hồ khi file test có cả `using System` và `using UnityEngine` (2026-11-02)  #script
- Triệu chứng: `CombatTestData` khai báo tham số `Random random = null` và báo `CS0104: 'Random' is an ambiguous reference between 'UnityEngine.Random' and 'System.Random'`, làm cả assembly test không biên dịch.
- Nguyên nhân gốc: helper test cần `UnityEngine` (`ScriptableObject`) và nhận RNG của `BattleSession` (`System.Random`), nên cả hai namespace cùng được import.
- Luật từ nay: trong file có `using UnityEngine`, luôn viết `System.Random` đầy đủ cho tham số và field RNG.

### L-017 — Message commit nhiều dòng có dấu ngoặc kép bị PowerShell 5.1 cắt thành pathspec (2026-11-02)  #git #process
- Triệu chứng: `git commit -m @'...'@` báo `pathspec 'in' did not match any file(s)`, không commit gì; message có `"not in scope"`.
- Nguyên nhân gốc: Windows PowerShell 5.1 truyền chuỗi chứa `"` cho chương trình ngoài mà không escape, nên git nhận phần sau dấu ngoặc là đối số riêng.
- Luật từ nay: message commit nhiều dòng thì ghi ra file trong scratchpad và dùng `git commit -F <file>`; chỉ dùng `-m @'...'@` khi message không có `"`.

### L-016 — `Validate()` thiếu một field thì config xấu hoặc làm NaN, hoặc làm Boot ném lỗi (2026-11-02)  #script #process
- Triệu chứng: review độc lập tìm ra `_dailyGoalSeconds = 0` qua `Validate` làm chất lượng NaN (Thu AFK mất Coin chờ), `_maxWaitingLine = 0` làm mọi khách bỏ về, và `_baseTables = -1` làm `RestaurantService` ném lỗi trong `Awake` dù `IsRunning` đã là false.
- Nguyên nhân gốc: `Validate()` chỉ liệt kê field của những task đã nghĩ tới; code chạy tiếp sau khi báo lỗi cấu hình (`RebuildProfile`) vẫn dùng giá trị xấu.
- Luật từ nay: field nào xuất hiện trong mẫu số, chỉ số mảng hay ngưỡng thì phải có trong `Validate()` và test; khi `IsRunning = false`, mọi bước dựng còn lại của constructor không được dùng config.

### L-015 — Một cờ bool dùng chung cho nhiều chủ làm panel này mở khoá panel kia (2026-11-02)  #script #ui
- Triệu chứng: `SetSpawnAllowed(bool)` được panel Nhân viên, Nâng cấp, popup AFK và popup Chất lượng cùng gọi; popup AFK chồng lên panel rồi đóng thì sự cố sinh ra dưới panel đang che màn, quá 20 giây là bỏ lỡ oan.
- Nguyên nhân gốc: trạng thái "bị chặn" là một bool ghi đè; plan và code chỉ nghĩ tới từng panel riêng lẻ, không tới panel chồng nhau.
- Luật từ nay: trạng thái chặn dùng chung giữa nhiều view phải theo chủ (`SetSpawnBlocked(owner, bool)`, idempotent theo chủ), không phải bool hay bộ đếm tăng giảm; test phải có ca hai chủ gỡ lần lượt.

### L-014 — Lệnh thử đổi trạng thái rồi quên khôi phục, kịch bản sau cho kết quả 0 (2026-11-02)  #process
- Triệu chứng: thử Tua AFK ở màn Gacha ra `pend=0`, tưởng AFK hỏng; thật ra một lệnh `eval` trước đó đã gỡ Thu ngân (lệnh sau của cùng cặp bị lỗi biên dịch nên không xếp lại).
- Nguyên nhân gốc: không ghi lại trạng thái đã đổi và không đọc `Estimate.MissingRole` đầu mỗi kịch bản.
- Luật từ nay: mỗi lệnh thử Play đổi trạng thái thì ghi một dòng "đã đổi X" và đặt lại ngay trong lệnh kế; đầu mỗi kịch bản in `Estimate.CoinsPerSecond` và `MissingRole` trước khi tin con số nào.

### L-013 — `GetComponentInParent` bỏ qua đối tượng đang tắt, test popup đóng sai (2026-11-02)  #script #unity-files
- Triệu chứng: test độ tương phản chữ của popup đỏ vì chữ trắng trên nút bị tính là "chữ trên thẻ" (`Expected: greater than 0.45, but was 0`).
- Nguyên nhân gốc: popup mở scene là đóng, mà `GetComponentInParent<Button>()` mặc định không tìm trong nhánh inactive nên trả null.
- Luật từ nay: test duyệt scene có đối tượng tắt dùng `GetComponentInParent<T>(true)` / `GetComponentsInChildren<T>(true)`; kiểm test bằng một ca mà cài đặt sai sẽ đỏ (test này đỏ đúng chỗ nên bắt được).

### L-012 — Số test trong kế hoạch đếm theo method, `[TestCase]` làm tổng lệch (2026-11-02)  #process #script
- Triệu chứng: viết test tham số hoá (`[TestCase]`) cho Task 12 thì tổng vượt số kế hoạch ghi (389 thay vì 382) dù đủ method.
- Nguyên nhân gốc: NUnit đếm mỗi `[TestCase]` là một test trong XML; kế hoạch đếm method.
- Luật từ nay: test cần nhiều đầu vào để bắt lỗi biên (L-006) thì lặp trong một method kèm thông báo có đầu vào (`$"u = {u}"`), không dùng `[TestCase]`, để tổng khớp kế hoạch.

### L-011 — Game view hẹp thì chụp màn hình ra ảnh bé, và `unity close .` có thể quá 30 giây (2026-11-02)  #ui #process
- Triệu chứng: `ScreenCapture.CaptureScreenshot` ra ảnh 401×664 dù `Screen.width/height` báo 2658×960 (cửa sổ Game bị thu hẹp); `unity close .` báo "did not close within 30s" nhưng Editor vẫn đóng sau đó.
- Nguyên nhân gốc: Game view phụ thuộc bố cục cửa sổ Editor lúc mở; lệnh đóng chỉ chờ 30 giây.
- Luật từ nay: khi thử màn UI, đổi Canvas sang `ScreenSpaceCamera` với camera có `targetTexture` 1920×1080, `Canvas.ForceUpdateCanvases()`, `cam.Render()`, `ReadPixels` rồi ghi PNG ngoài repo và trả Canvas về Overlay, ngay trong một lệnh `eval`. Sau `unity close .` lỗi 30 giây, chạy `unity status` lặp lại cho tới khi không còn Editor, không kill process.

### L-010 — Ảnh chụp Game view bị bóp về 16:9 nên kết luận sai về bố cục (2026-11-02)  #ui #process
- Triệu chứng: `capture game_view` luôn trả ảnh 1280×720 dù Game view thật là 2658×960, nên hình tròn thành bầu dục và phòng trông méo; kèm theo, với `matchWidthOrHeight = 0,5` canvas chỉ cao 865 và cắt phía dưới bố cục cao 1080.
- Nguyên nhân gốc: tin kích thước ảnh chụp thay vì đọc `Screen.width/height` và `Canvas.scaleFactor` của lượt chạy.
- Luật từ nay: khi thử màn UI, đọc `Screen.width/height` rồi chụp bằng `ScreenCapture.CaptureScreenshot("D:/…/x.png")` (đúng tỉ lệ thật, ghi ngoài repo); màn có bố cục cố định theo chiều cao thì scaler khớp theo chiều cao.

### L-009 — Play đứng im khi Editor không focus, dễ nhầm với lỗi mô phỏng (2026-11-01)  #process #unity-files
- Triệu chứng: thử quán qua `unity command`, Coin đứng ở 583 hơn 50 giây dù đủ ba nhân viên; chưa kiểm đã định kết luận khách không đến nữa.
- Nguyên nhân gốc: Editor không focus thì vòng lặp Play không chạy (`Time.frameCount` đứng yên), không phải lỗi `RestaurantSimulation`. Cờ của hai lệnh bật chạy nền là `--enable true` (không phải `--enabled`).
- Luật từ nay: vào Play xong là chạy ngay `unity command editor set_auto_tick --enable true` và `editor.playmode set_run_in_background --enable true`; trước khi tin số đo hay nghi lỗi, đọc `Time.frameCount` hai lần cách vài giây.

### L-008 — Dung sai của kế hoạch che lỗi lệch một bước trong mô phỏng thời gian (2026-11-01)  #process #script
- Triệu chứng: `RestaurantSimulation` xanh 268/268 nhưng bữa ăn ngắn đi 0,1 giây (khách trả tiền ở 27,4 thay vì 27,5), vì bước "ăn" chạy sau bước "worker" nên bị trừ giờ ngay trong bước vừa chuyển trạng thái; kế hoạch cho dung sai ±0,2 giây nên test không bắt được.
- Nguyên nhân gốc: dung sai của test rộng hơn đúng một bước cố định (0,1 giây), nên mọi lỗi lệch một bước đều lọt.
- Luật từ nay: test thời gian của mô phỏng bước cố định phải chặn ở mốc ±nửa bước (vd. chưa xong ở 27,45, xong ở 27,55), và trong mỗi bước, hệ thống trừ giờ phải chạy trước hệ thống đổi trạng thái.

### L-007 — Tưởng lỗi vì độ trễ lệnh, và để lại thay đổi ngoài ý muốn (2026-11-01)  #process #unity-files
- Triệu chứng: ảnh chụp 1 giây sau khi bấm Quay đã ở bước reveal, tôi tưởng cutscene bị bỏ; thực ra mỗi lệnh CLI tốn ~3 giây. Lệnh bật chạy nền còn ghi `runInBackground: 1` vào `ProjectSettings.asset`.
- Nguyên nhân gốc: kết luận từ ảnh chụp mà chưa đo thời gian thật; bật tuỳ chọn Editor mà không kiểm `git status`.
- Luật từ nay: đo bằng `Time.realtimeSinceStartup` trong chính lệnh trước khi nghi lỗi; sau mỗi lượt thử Play chạy `git status` và hoàn tác file ProjectSettings bị đổi.

### L-006 — Test đặt tên theo tính chất nhưng vẫn xanh khi tính chất sai (2026-10-31)  #process #script
- Triệu chứng: reviewer chỉ ra test "lượt 10 nâng SR có thể ra SSR" dùng 0.99 (ra SSR dù không nâng) và test "dòng chi phí âm" chỉ có một dòng âm (lỗi "tổng bằng 0" khớp thay).
- Nguyên nhân gốc: chọn đầu vào theo ví dụ trong kế hoạch mà không hỏi "cài đặt sai nào vẫn qua test này?".
- Luật từ nay: mỗi test logic tiền/gacha phải có đầu vào mà cài đặt sai sẽ cho kết quả khác; test RNG hỏng và lỗi phải có ca giữa chừng chứ không chỉ ở lần gọi đầu.

### L-005 — `git add` theo từng file `.cs` bỏ sót `.meta` (2026-10-31)  #git #unity-files
- Triệu chứng: commit Task 3 và 4 của Gacha đẩy lên `origin` các file `.cs` mới mà thiếu `.meta` đi kèm; `git status` vẫn còn `?? *.cs.meta`.
- Nguyên nhân gốc: lệnh add liệt kê từng `.cs`, mà Unity sinh `.meta` thành file riêng; tôi không đọc lại `git status` sau khi add.
- Luật từ nay: khi tạo file mới trong `Assets/`, add theo thư mục hoặc add cả `<file>.meta`; trước mỗi commit chạy `git status --short` và không để `?? *.meta` nào còn lại.

### L-004 — Kết luận cơ chế khi chưa tách biến (2026-10-31)  #process #unity-files
- Triệu chứng: sau vài lần Play qua MCP, file save lệch số so với bộ nhớ; tôi báo "biên dịch lệnh làm dừng Play", rồi "dừng Play không lưu", cả hai đều sai.
- Nguyên nhân gốc: công cụ `RunCommand` tự thoát Play khi mã lệnh chứa `ExitPlaymode`/`isPlaying = false`, nên phép thử "trừ tiền rồi dừng" chạy trên session đã chết; tôi kết luận trước khi đọc mã của công cụ.
- Luật từ nay: khi hành vi của công cụ lạ, đọc mã nguồn của công cụ (`Library/PackageCache`) trước, rồi mới đặt giả thuyết; mỗi phép thử chỉ đổi một thứ, dừng Play bằng lệnh riêng.

### L-003 — Code tiền/save nuốt lỗi IO và bắn event trần (2026-10-31)  #script
- Triệu chứng: reviewer chỉ ra `SaveService.Load` coi file bị khoá như save hỏng (game mới ghi đè save thật), và `Changed?.Invoke()` để handler lỗi làm `TrySpend` ném lỗi sau khi tiền đã trừ.
- Nguyên nhân gốc: kế hoạch và test chỉ có đầu vào "hỏng/rỗng", không có "tạm thời không đọc được"; event bắn thẳng sau khi đổi trạng thái.
- Luật từ nay: lỗi IO không được đồng nhất với dữ liệu hỏng, để nó nổi lên thay vì trả `null`; mọi `event` bắn sau khi đã đổi trạng thái dùng `SafeInvoke()` (Core). Test phải có ca "tài nguyên đang bị khoá" và "handler ném lỗi".

### L-002 — Tham chiếu asset giữ qua `NewScene(Single)` thành null (2026-10-30)  #unity-files #scene-prefab #script
- Triệu chứng: `ProjectSetup` sinh `Boot.unity` có `_catalog`/`_newGameConfig` trống; test `BootScene_IsFirstInBuild_AndBootstrapIsWired` đỏ. Lần chạy đầu còn lỗi "Cannot create a new scene additively with an untitled scene unsaved" ở batch mode.
- Nguyên nhân gốc: kế hoạch dùng `NewScene(Additive)`, batch mode buộc dùng `Single`; `Single` dỡ asset không được scene nào dùng nên biến C# đang giữ thành đối tượng đã huỷ.
- Luật từ nay: trong code Editor, nạp asset bằng `AssetDatabase.LoadAssetAtPath` **sau** khi tạo/đổi scene, không giữ biến từ trước. Sinh scene xong phải có test đọc lại tham chiếu từ file.

### L-001 — Spec tự vi phạm luật phụ thuộc assembly (2026-10-30)  #process #script
- Triệu chứng: spec nền móng đặt `StageDefinition` (Progression) trỏ tới `EnemyDefinition` (Combat) trong khi cấm hai assembly này phụ thuộc nhau; thiếu assembly Editor, thời điểm lưu save.
- Nguyên nhân gốc: viết bảng SO theo từng mảng, không kiểm từng tham chiếu với sơ đồ phụ thuộc.
- Luật từ nay: trước khi chốt spec kiến trúc, đối chiếu từng tham chiếu giữa các SO với sơ đồ assembly; code dùng `UnityEditor` phải nằm trong assembly Editor; save phải nói rõ khi nào ghi và ghi thế nào để không mất dữ liệu.
