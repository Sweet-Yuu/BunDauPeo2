# Tech stack

> **Đọc khi:** thêm/xoá package, đổi render pipeline hoặc input, viết code phụ thuộc API của engine.
> **Cập nhật khi:** `Packages/manifest.json` hoặc ProjectSettings đổi theo hướng đáng kể. Nguồn sự thật gốc: `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`.

*Ghi nhận 2026-10-30.*

| Hạng mục | Giá trị |
|---|---|
| Unity Editor | 6000.3.15f1 |
| Render pipeline | URP 17.3.0, renderer 2D (`Assets/Settings/Renderer2D.asset`) |
| Input | Input System 1.19.0 — actions ở `Assets/Settings/InputSystem_Actions.inputactions` |
| UI | uGUI 2.0.0 (+ module UIElements có sẵn) |
| 2D | Animation 13.0.4, Aseprite importer 3.0.1, PSD importer, SpriteShape, Tilemap (+ extras) |
| Online ([D-014](../decisions/decisions.md)) | UGS: Core 1.19.0, Authentication 3.8.1, Cloud Save 3.4.2, Cloud Code 2.10.4 (dòng 2.10.x cho `.ccmr`), Deployment 1.7.2 (chỉ Editor) |
| Module server | `ServerModule/` ngoài `Assets/`: module Cloud Code `.ccmr` .NET 8 (`BunDauPeoServer`), test .NET (`dotnet test ServerModule/BunDauPeoServer.sln`); cần .NET SDK 8 hoặc 9: project test tự chọn `net9.0` khi có SDK 9, `net8.0` khi chỉ có SDK 8 (cần runtime khớp để chạy test) |
| Khác | MCP for Unity 10.3.0 (git), Timeline 1.8.12, Visual Scripting 1.9.11, Test Framework 1.6.0 |
| IDE | Rider / Visual Studio |

## Ghi chú

- Package `com.unity.pipeline` (cầu nối CLI `unity`) là bản experimental, và `com.coplaydev.unity-mcp` (MCP for Unity, git tag `v10.3.0`, [D-010](../decisions/decisions.md)) là công cụ Editor — đừng dựa vào chúng cho code game. `com.unity.ai.assistant` đã gỡ.
- Active Input Handling: Input System only (`activeInputHandler: 1` trong `ProjectSettings.asset`).
- Nền tảng build mục tiêu: mobile màn ngang ([D-003](../decisions/decisions.md)). Android trước ([D-007](../decisions/decisions.md)); cấu hình build ở [D-011](../decisions/decisions.md). Module Android (SDK 34–36, NDK, OpenJDK) đã cài cho 6000.3.15f1.
- Online: hai môi trường UGS, `development` (Editor chế độ UGS và build Development; file cấu hình Editor commit theo [D-013](../decisions/decisions.md)) và `production` (bản phát hành). Game chỉ chứa Project ID và tên môi trường, không có khoá bí mật. Project test của module chạy trên net9.0 khi máy chưa có .NET 8 SDK; module thật vẫn là net8.0. Cách chạy và deploy: mục "Online" của [unity-workflow.md](../guides/unity-workflow.md).
- CLI: `unity` 1.0.0-beta.15 là công cụ chính. Lệnh và quy tắc dùng ở [unity-workflow.md](../guides/unity-workflow.md).
