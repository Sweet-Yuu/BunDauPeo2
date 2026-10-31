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

- Lệnh batch (`test`, `run`, `build`) không chạy được khi project đang mở trong Editor; lệnh nối vào Editor (`command`, `recompile`) cần Editor đang mở. Chạy `unity status` trước khi chọn. Không kill process Unity; nhờ user đóng Editor.
- Mã thoát (theo `unity -h`): `0` thành công, `8` test chạy xong nhưng có test đỏ, `6` lệnh hỏng vì lý do khác (kể cả lỗi compile), `12` không có Editor nào trả lời, `10`/`14` quá thời gian. Luôn **đọc file kết quả và log thật**, không dựa mỗi mã thoát.
- Điều khiển Editor qua MCP (`Unity_RunCommand`):
  - Vào Play mất 10–25 giây vì nạp lại domain, và cổng MCP rớt trong lúc đó; `isPlaying` báo `False` suốt quãng này. Bắn `EnterPlaymode` ở lệnh riêng, chờ, rồi kiểm `isPlaying` trước khi làm gì.
  - Công cụ đọc mã lệnh: nếu có `ExitPlaymode()` hoặc `isPlaying = false` thì nó **thoát Play trước khi chạy lệnh**, nên việc khác trong cùng lệnh chạy trên session đã chết. Dừng Play bằng một lệnh riêng, chỉ chứa việc dừng.
  - Không dùng `System.Reflection` trong lệnh (bị chặn). Lớp lồng trong lệnh bị kéo ra ngoài, nên đừng để nó dùng hằng của lớp bao.
  - Chạy test ngay trong Editor đang mở: `TestRunnerApi.Execute` với `ICallbacks` ghi kết quả ra file (không cần đóng Editor). Sửa `.cs` làm Editor biên dịch và rớt cổng MCP vài chục giây; chờ rồi mới gọi lại.
- Điều khiển Play qua `unity command` (Editor mở): `editor.playmode start|stop`, `capture game_view --save_path`, `console get --level error`, `eval '<câu lệnh C#>;'` (phải là câu lệnh, ví dụ `Debug.Log(...)`; đọc kết quả ở `console get`).
  - `capture --save_path` chỉ nhận đường dẫn trong thư mục project và đường dẫn dạng `D:/.../x.png` lại ghi vào `Assets/`, sinh thêm `.meta`: xoá cả hai khi xong, đừng commit.
  - `runtime simulate_pointer` báo thành công nhưng không đổi `Mouse.current` (vẫn 0,0), nên không bấm được UI. Kiểm nút bằng `EventSystem.RaycastAll` tại tâm nút rồi `ExecuteEvents.Execute(nút, new PointerEventData(es), ExecuteEvents.pointerClickHandler)`.
  - Lượt thử Play ghi vào save thật: sao lưu `save.json*` trước, chép lại sau khi đóng Editor (`unity close .`).
- Test, đường dẫn log và lệnh cụ thể của từng đợt nằm trong kế hoạch tương ứng ở `superpowers/plans/`, không chép lại ở đây.

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

Ngoài phạm vi hiện tại (offline, không nạp tiền thật): `unity:build-live-game`, `unity:implement-in-app-purchases`, `unity:levelplay-unity-integration`, `unity:setup-multiplayer-services`, `unity:setup-vivox-voice-chat`. `unity:localization` chưa dùng, vì chưa có yêu cầu đa ngôn ngữ.

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
- Tạo bằng `unity command` hoặc script Editor, không import ảnh ngoài. Khi có art thật thì đổi tham chiếu sprite, không đổi cấu trúc UI.

## Git

- **Session code:** commit và **push** sau mỗi nhiệm vụ nhỏ đã xong (một cụm bước có test xanh) và sau mỗi task lớn của kế hoạch. Push lên `origin` (`https://github.com/Sweet-Yuu/BunDauPeo2.git`); nhánh chưa có upstream thì `git push -u origin <nhánh>`. Không force push, không sửa lịch sử đã push. Chỉ push khi test của phần đó đã xanh; test đỏ thì sửa trước.
- **Session điều phối và việc không phải code:** chỉ commit/push khi user yêu cầu.
- Cuối ngày làm việc, user tự kiểm tra và push các commit còn sót; không có lịch tự động.
- `.gitignore` đã chặn `Library/`, `Logs/`, `UserSettings/`, `Temp/`, `*.sln`, `*.csproj`. `git add` theo đường dẫn cụ thể, không `git add -A`.

## Kiểm tra sau thay đổi

- Compile: `unity recompile` (Editor đang mở) hoặc chạy `unity test`; không lỗi compile, không "Missing script" / "Missing reference".
- Với thay đổi gameplay: chạy Play Mode scene liên quan và xác nhận hành vi mong muốn.
