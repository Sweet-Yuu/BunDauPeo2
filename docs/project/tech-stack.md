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
| Khác | MCP for Unity 10.3.0 (git), Timeline 1.8.12, Visual Scripting 1.9.11, Test Framework 1.6.0 |
| IDE | Rider / Visual Studio |

## Ghi chú

- Package `com.unity.pipeline` (cầu nối CLI `unity`) là bản experimental, và `com.coplaydev.unity-mcp` (MCP for Unity, git tag `v10.3.0`, [D-010](../decisions/decisions.md)) là công cụ Editor — đừng dựa vào chúng cho code game. `com.unity.ai.assistant` đã gỡ.
- Active Input Handling: Input System only (`activeInputHandler: 1` trong `ProjectSettings.asset`).
- Nền tảng build mục tiêu: mobile màn ngang ([D-003](../decisions/decisions.md)). Android trước ([D-007](../decisions/decisions.md)); cấu hình build ở [D-011](../decisions/decisions.md). Module Android (SDK 34–36, NDK, OpenJDK) đã cài cho 6000.3.15f1.
- CLI: `unity` 1.0.0-beta.15 là công cụ chính. Lệnh và quy tắc dùng ở [unity-workflow.md](../guides/unity-workflow.md).
