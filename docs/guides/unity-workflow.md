# Quy trình làm việc với Unity

> **Đọc khi:** làm bất kỳ việc nào cần chạy Unity (test, build, mở Editor, sửa scene/prefab/asset/ProjectSettings), hoặc chọn skill của plugin Unity.
> **Cập nhật khi:** phát hiện lệnh CLI hoặc cách làm mới/sai (kèm 1 mục trong [lesson.md](../lessons/lesson.md)), hoặc plugin Unity đổi danh sách skill.

## Công cụ chính: Unity CLI (`unity`)

Mọi thao tác với Unity đi qua `unity` (bản ghi nhận: 1.0.0-beta.15) trước. Chỉ dùng cách khác (menu trong Editor, sửa tay) khi CLI không làm được, và ghi lý do vào báo cáo.

- Lệnh chưa chắc: đọc `unity -h`, `unity <lệnh> -h`, hoặc `unity commands --grep <từ khoá>`. `unity skill show` in hướng dẫn CLI theo task. Đừng đoán cờ lệnh.
- Tra tài liệu Unity đúng phiên bản Editor của project: `unity docs <chủ đề>`.

| Việc | Lệnh | Editor |
|---|---|---|
| Xem Editor nào đang chạy | `unity status` | — |
| Mở / đóng project | `unity open .` / `unity close .` (đóng không lưu) | — |
| Biên dịch lại, xem lỗi compile | `unity recompile` | phải đang mở |
| Làm việc trên Editor đang mở (GameObject, scene, asset, chạy C#) | `unity command <tên> [args]`; liệt kê bằng `unity command`, tìm bằng `unity command --query <từ khoá>` | phải đang mở |
| Chạy test | `unity test . --mode EditMode --output <file>` | phải đóng |
| Chạy method Editor ở batch mode | `unity run . -- -executeMethod <Class.Method>` | phải đóng |
| Build | `unity build` | phải đóng |

Quy tắc:

- Lệnh batch (`test`, `run`, `build`) không chạy được khi project đang mở trong Editor; lệnh nối vào Editor (`command`, `recompile`) cần Editor đang mở. Chạy `unity status` trước khi chọn, **thành một lệnh riêng**: nó thoát mã 12 khi không có Editor nên `unity status && unity run …` không chạy vế sau (L-020). Không kill process Unity; nhờ user đóng Editor.
- Sau `unity run`, kiểm `git status` (hoặc giờ sửa của file log) trước khi tin log: log cũ trông y như log mới (L-020).
- Mã thoát (theo `unity -h`): `0` thành công, `8` test chạy xong nhưng có test đỏ, `6` lệnh hỏng vì lý do khác (kể cả lỗi compile), `12` không có Editor nào trả lời, `10`/`14` quá thời gian. Luôn **đọc file kết quả và log thật**, không dựa mỗi mã thoát.
- Điều khiển Editor qua MCP (`Unity_RunCommand`):
  - Vào Play mất 10–25 giây vì nạp lại domain, và cổng MCP rớt trong lúc đó; `isPlaying` báo `False` suốt quãng này. Bắn `EnterPlaymode` ở lệnh riêng, chờ, rồi kiểm `isPlaying` trước khi làm gì.
  - Công cụ đọc mã lệnh: nếu có `ExitPlaymode()` hoặc `isPlaying = false` thì nó **thoát Play trước khi chạy lệnh**, nên việc khác trong cùng lệnh chạy trên session đã chết. Dừng Play bằng một lệnh riêng, chỉ chứa việc dừng.
  - Không dùng `System.Reflection` trong lệnh (bị chặn). Lớp lồng trong lệnh bị kéo ra ngoài, nên đừng để nó dùng hằng của lớp bao.
  - Chạy test ngay trong Editor đang mở: `TestRunnerApi.Execute` với `ICallbacks` ghi kết quả ra file (không cần đóng Editor). Sửa `.cs` làm Editor biên dịch và rớt cổng MCP vài chục giây; chờ rồi mới gọi lại.
- Trước khi Play: mở Boot bằng `PlaytestTools.OpenBoot()` (xem mục "Công cụ thử tự động"). Lý do: Editor mở bằng CLI không tự chạy Boot, nên không có dịch vụ và không có `DevOverlay`; kiểm `GameObject.Find("DevOverlay")` có trước khi thử gì (L-019).
- Mỗi lệnh thử Play đổi trạng thái (gỡ nhân viên, tắt AUTO…) thì ghi một dòng "đã đổi X" và đặt lại ngay lệnh kế; đầu mỗi kịch bản in các số nền (vd. `Estimate.CoinsPerSecond`, `MissingRole`) trước khi tin con số nào (L-014).
- Hành vi công cụ lạ: đọc mã nguồn của công cụ trong `Library/PackageCache` trước khi đặt giả thuyết; mỗi phép thử chỉ đổi một thứ (L-004).
- Điều khiển Play qua `unity command` (Editor mở): `editor.playmode start|stop`, `capture game_view --save_path`, `console get --level error`, `eval '<câu lệnh C#>;'` (phải là câu lệnh, ví dụ `Debug.Log(...)`; đọc kết quả ở `console get`).
  - `capture --save_path` chỉ nhận đường dẫn trong thư mục project và đường dẫn dạng `D:/.../x.png` lại ghi vào `Assets/`, sinh thêm `.meta`: xoá cả hai khi xong, đừng commit.
  - `runtime simulate_pointer` báo thành công nhưng không đổi `Mouse.current` (vẫn 0,0), nên không bấm được UI. Kiểm nút bằng `EventSystem.RaycastAll` tại tâm nút rồi `ExecuteEvents.Execute(nút, new PointerEventData(es), ExecuteEvents.pointerClickHandler)`.
  - `unity command test run` báo kết quả **cũ** khi project đang lỗi compile (vẫn "N/N passed"): kiểm `unity command console status` có `"compilationFailed":false` trước khi tin con số.
  - Mỗi lệnh CLI tốn ~3 giây. Muốn đo thời gian trong Play, ghi `Time.realtimeSinceStartup` trong chính lệnh `eval` (`Debug.Log`), đừng dựa vào `sleep`.
  - Bơm chạm chuột: `InputSystem.EnableDevice(Mouse.current)` (thiết bị bị tắt khi Editor không focus), rồi `QueueStateEvent(Mouse.current, new MouseState { buttons = 1 })` ở một lệnh và `buttons = 0` ở lệnh sau.
  - Play đứng im khi Editor không focus (L-009, `Time.frameCount` đứng yên) và bật chạy nền có thể ghi `runInBackground` vào `ProjectSettings.asset` (L-007): dùng `PlaytestTools.EnableBackgroundPlay()`, nó nhớ giá trị gốc trong `SessionState` và tự trả lại khi về Edit Mode. Cuối lượt vẫn kiểm `git status`.
  - Lượt thử Play ghi vào save thật: `PlaytestTools.BackupSave()` trước, `RestoreSave()` sau khi thoát Play (nó từ chối khi đang Play vì `OnApplicationQuit` sẽ ghi đè). Chạy PlayMode batch cũng ghi vào save thật nếu không có save tạm (L-023).
  - Chụp màn hình để kiểm UI: `PlaytestTools.Capture1080()` (ra `D:/Unity/bdp-shots/`, ngoài repo). Lý do (L-010, L-011): `capture game_view` luôn bóp ảnh về 16:9 (1280×720) dù Game view thật khác, nên hình méo và dễ kết luận sai bố cục; hàm này đổi Canvas sang `ScreenSpaceCamera` với camera `targetTexture` 1920×1080 rồi trả về Overlay. Màn có bố cục cố định theo chiều cao thì `CanvasScaler` khớp theo chiều cao. `capture --save_path` chỉ nhận đường dẫn trong project và sinh `.meta` ở `Assets/`: đừng dùng.
  - `unity close .` có thể báo "did not close within 30s" dù Editor vẫn đóng: chạy `unity status` lặp lại tới khi hết dòng Editor, không kill process.
  - `unity command eval '<mã>'`: mã chạy trong thân một hàm, nên không có `using` (dùng tên đầy đủ, ví dụ `BunDauPeo.EditorTools.PlaytestTools.ClickButton("AutoButton")`), và `return x;` trả giá trị về trường `result` (thêm `--result-only` cho gọn). Bấm nút UI trong Play bằng `PlaytestTools.ClickButton(tên)`: nó raycast tại tâm nút như người chơi (nút bị popup che báo `Blocked`, không bấm xuyên), khác `onClick.Invoke()` bấm được cả nút bị che.
  - `unity command console get` không lọc sẽ trả cả trăm cảnh báo shader và stack trace: luôn thêm `--level error` rồi `grep -o '"message":"[^"]*'`.
  - Dựng lại scene bằng builder (`RestaurantSceneBuilder`, `GachaSceneBuilder`, `BattleSceneBuilder`, `StageSelectSceneBuilder`) chỉ chạy được ở batch mode với Editor đóng: `unity run . -- -executeMethod BunDauPeo.EditorTools.ProjectSetup.RebuildRestaurantScene` (hoặc `RebuildBattleScene`, `RebuildStageSelectScene`). Trong Editor đang mở sẽ lỗi "Cannot create a new scene additively with an untitled scene unsaved". Đổi field `[SerializeField]` của view thì phải dựng lại scene tương ứng, rồi chạy test (test scene đọc lại file).
  - Thử Combat trong Play: bấm nút theo tên bằng `ClickButton`; vào `StageSelect` bằng `SceneManager.LoadScene` từ Restaurant (Boot đã chạy). AUTO với x2 hạ trận trong vài giây giữa hai lệnh CLI (mỗi lệnh ~3 giây): muốn thử đường thua thì tắt `CombatService.AutoEnabled` trước khi vào trận, kẻo trận đã thắng khi bạn gọi `Surrender()`.
  - Dòng ".. is not a valid directory name" khi chạy `unity test`/`unity run` với đường dẫn `../…` là vô hại; xem file kết quả.
- Test, đường dẫn log và lệnh cụ thể của từng đợt nằm trong kế hoạch tương ứng ở `superpowers/plans/`, không chép lại ở đây.

## Công cụ thử tự động

Gom các bẫy playtest ở trên thành code có test ([D-009](../decisions/decisions.md), [spec](../superpowers/specs/2026-11-03-playtest-tools-design.md)). Hàm nằm ở `BunDauPeo.EditorTools.PlaytestTools` (assembly `Editor`), trả `string`, bắt đầu bằng `ERROR:` khi từ chối; gọi bằng `unity command eval 'return BunDauPeo.EditorTools.PlaytestTools.<Hàm>();' --result-only` (Editor mở).

| Hàm | Việc | Menu `Tools/BunDauPeo/Playtest/` |
|---|---|---|
| `OpenBoot()` | mở `Boot.unity`; từ chối khi đang Play hoặc scene sửa dở | Open Boot |
| `BackupSave()` | chép `save.json*` vào `<thư mục save>/playtest-backup/`; từ chối nếu đã có bản sao lưu (thông điệp ghi giờ tạo); Reset Save Data không xoá thư mục này ([D-009](../decisions/decisions.md)) | Backup Save |
| `RestoreSave()` | thay save hiện tại bằng bản sao lưu rồi xoá bản sao lưu; từ chối khi đang Play hoặc chưa có bản sao lưu | Restore Save... (hỏi xác nhận, ghi giờ tạo bản sao lưu) |
| `EnableBackgroundPlay()` / `RestoreBackgroundPlay()` | bật rồi trả `runInBackground` | không |
| `Capture1080(tên)` | chụp màn đang Play 1920×1080 ra `D:/Unity/bdp-shots/` | Capture 1920x1080 |
| `ClickButton(tên)` | bấm nút qua raycast; báo `Clicked`, `NotFound`, `Ambiguous`, `NotInteractable`, `Blocked` | không |

Quy trình một lượt thử tay: `BackupSave` → `OpenBoot` → `EnableBackgroundPlay` → Play và thử → thoát Play → `RestoreSave` → `git status` (không có `ProjectSettings/`).

Smoke test tự động: `unity test . --mode PlayMode --output ../bdp-playtests.xml --timeout 900 -- -logFile ../bdp-playtests.log` (Editor đóng; chạy được ở batch mode có dựng hình, không cần cờ riêng; vòng Boot → bản đồ → đội → trận AUTO → thưởng → nạp lại save mất khoảng 10 giây). Test dùng save tạm qua `GameBootstrap.SavePathOverride`, ảnh kết quả ở `D:/Unity/bdp-shots/smoke-result.png`. Test PlayMode không có `DevOverlay` sẵn (nó chỉ dựng khi Play khởi động, L-024) nên vào màn bằng `SceneManager.LoadScene`; test panel DEV gọi `DevOverlay.Build()` sau khi Boot xong, rồi bấm "Cài đặt" trước khi bấm nút DEV nào (xem `SmokeLoopTests`: nút Cài đặt không che nút game, luồng xác nhận Reset, Reset thất bại). Màn test là 4:3 nhỏ (640×480): nút neo trái nằm ngoài biên 16:9 có thể ra ngoài màn. Viết kịch bản UI thì tra thứ tự màn thật trong asset (`Kind` từng màn: màn đầu là Truyện) thay vì đoán theo builder (L-024). Test đỏ thì thông điệp ghi bước và danh sách nút đang hiện. Chạy PlayMode từ cửa sổ Test Runner trong Editor không có guard `-runTests` của `BootScenePlayMode` (L-023).


## Skills của plugin Unity

Việc thuộc nhóm bên dưới thì **nạp skill tương ứng bằng Skill tool trước khi làm** và làm theo. Khi skill mâu thuẫn với docs dự án (luật chơi, cấu trúc thư mục, code-style, luật cứng trong `CLAUDE.md`), docs dự án thắng; báo user chỗ mâu thuẫn.

| Việc | Skill |
|---|---|
| Điều khiển Editor, scene, asset, GameObject, chạy test/build | `unity:unity-cli` |
| Thêm / xoá / đổi package | `unity:unity-package-management` (rồi ghi [decisions.md](../decisions/decisions.md) và [tech-stack.md](../project/tech-stack.md)) |
| UI (dự án dùng uGUI) | `unity:ui-ugui`; chọn hệ UI thì `unity:ui` |
| Cắt sprite, sprite 9-slice, sprite atlas | `unity:sprite-editor`, `unity:sprite-segment-3x3grid`, `unity:manage-sprite-atlas` |
| Tilemap | `unity:tilemap-palette-create`, `unity:tilemap-ruletile-createempty`, `unity:tilemap-ruletile-createfromsegment` |
| Chữ TextMeshPro | `unity:optimize-text-mesh-pro` |
| Hậu kỳ URP | `unity:urp-postprocessing` |
| Âm thanh | `unity:audio-setup-mixers`, `unity:optimize-audio` |
| Pixel art | `unity:2d-pixel-perfect` (chỉ khi art cuối là pixel art, đang `TODO(user)` ở [vision](../vision/vision.md)) |

Mua trong game và quảng cáo thưởng (user duyệt 2026-11-02, [D-007](../decisions/decisions.md)): `unity:implement-in-app-purchases`, `unity:levelplay-unity-integration`; nạp khi làm tính năng đó, sau khi có spec riêng.

Ngoài phạm vi hiện tại (không online, không server): `unity:build-live-game`, `unity:setup-multiplayer-services`, `unity:setup-vivox-voice-chat`. `unity:localization` chưa dùng, vì chưa có yêu cầu đa ngôn ngữ.

Quy trình (brainstorming, viết kế hoạch, TDD, review) dùng skill `superpowers:*`, xem [agent-routing.md](agent-routing.md).

## Luật với file Unity

- Mỗi file/thư mục trong `Assets/` có `.meta` đi kèm (chứa GUID). **Không xoá, không sửa tay.** Di chuyển/đổi tên phải đi cùng `.meta`, tốt nhất làm qua Unity (CLI hoặc Editor).
- File `.unity`, `.prefab`, `.asset` là YAML do Unity quản lý. **Không sửa tay** trừ thay đổi rất nhỏ và chắc chắn; ưu tiên `unity command` hoặc script Editor chạy bằng `unity run`.
- Không đụng: `Library/`, `Logs/`, `UserSettings/`, `Temp/`, `obj/`, các `*.csproj`/`*.sln` (do Unity sinh, đã bị ignore).
- Script mới: tạo file `.cs` rồi để Unity import/sinh `.meta`; không tự tạo `.meta`.
- Đổi ProjectSettings (Player, Physics, Tags/Layers…) → ghi lý do vào [decisions.md](../decisions/decisions.md).

## UI và hình tạm

- UI và mọi hình cần tạm chỉ dùng hình có sẵn của Unity: sprite hình học (vuông, tròn, tam giác… trong `Assets > Create > 2D > Sprites`) và sprite mặc định `UI/Skin/UISprite.psd`, `Knob.psd`. Không thêm file ảnh; phân biệt thành phần bằng màu, kích thước và chữ.
- Việc liên quan đến Canvas (nút, panel, thanh, biểu tượng…): dùng component `Image` (uGUI) và gán các sprite có sẵn ở trên vào `Image`; không dùng `Raw Image` hay ảnh import.
- Ngoại lệ đã được user cho phép: 3 PNG cutscene Gacha (`Gacha/Art/Summoning/`), xem mục 6 của [spec reveal](../superpowers/specs/2026-10-31-gacha-reveal-design.md).
- Tạo bằng `unity command` hoặc script Editor, không import ảnh ngoài. Khi có art thật thì đổi tham chiếu sprite, không đổi cấu trúc UI.

## Git

- **Session code:** commit và **push** sau mỗi nhiệm vụ nhỏ đã xong (một cụm bước có test xanh) và sau mỗi task lớn của kế hoạch. Push lên `origin` (`https://github.com/Sweet-Yuu/BunDauPeo2.git`); nhánh chưa có upstream thì `git push -u origin <nhánh>`. Không force push, không sửa lịch sử đã push. Chỉ push khi test của phần đó đã xanh; test đỏ thì sửa trước.
- **Session điều phối và việc không phải code:** chỉ commit/push khi user yêu cầu.
- Cuối ngày làm việc, user tự kiểm tra và push các commit còn sót; không có lịch tự động.
- `.gitignore` đã chặn `Library/`, `Logs/`, `UserSettings/`, `Temp/`, `*.sln`, `*.csproj`. `git add` theo đường dẫn cụ thể, không `git add -A` (đã chặn trong `.claude/settings.json`).
- File mới trong `Assets/` thì add theo thư mục hoặc add cả `<file>.meta`; trước mỗi commit chạy `git status --short`, không để sót `?? *.meta` (L-005).
- Message commit nhiều dòng: ghi ra file trong scratchpad rồi `git commit -F <file>`. Windows PowerShell 5.1 cắt chuỗi có `"` thành pathspec (L-017).

## Kiểm tra sau thay đổi

- Compile: `unity recompile` (Editor đang mở) hoặc chạy `unity test`; không lỗi compile, không "Missing script" / "Missing reference".
- Với thay đổi gameplay: chạy Play Mode scene liên quan và xác nhận hành vi mong muốn.
