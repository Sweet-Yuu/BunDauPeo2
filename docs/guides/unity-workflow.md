# Quy trình thao tác với file Unity

> **Đọc khi:** sửa scene, prefab, asset, ProjectSettings, hoặc tạo/xoá/di chuyển file trong `Assets/`.
> **Cập nhật khi:** phát hiện cách làm an toàn/không an toàn mới (kèm 1 mục trong [lesson.md](../lessons/lesson.md)).

## Luật

- Mỗi file/thư mục trong `Assets/` có `.meta` đi kèm (chứa GUID). **Không xoá, không sửa tay.** Di chuyển/đổi tên phải đi cùng `.meta`, tốt nhất làm trong Editor.
- File `.unity`, `.prefab`, `.asset` là YAML do Unity quản lý. **Không sửa tay** trừ thay đổi rất nhỏ và chắc chắn; ưu tiên thao tác qua Editor / skill `unity-cli`.
- Không đụng: `Library/`, `Logs/`, `UserSettings/`, `Temp/`, `obj/`, các `*.csproj`/`*.sln` (do Unity sinh, đã bị ignore).
- Script mới: tạo file `.cs` rồi để Unity import/sinh `.meta`; không tự tạo `.meta`.
- Đổi ProjectSettings (Player, Physics, Tags/Layers…) → ghi lý do vào [decisions.md](../decisions/decisions.md).

## Git

- Chưa có commit nào ở thời điểm ghi nhận (2026-10-30). Không commit/push khi user chưa yêu cầu.
- `.gitattributes`/`.gitignore` đã có sẵn; kiểm tra lại trước commit đầu tiên.

## Kiểm tra sau thay đổi

- Mở Console: không lỗi compile, không "Missing script" / "Missing reference".
- Với thay đổi gameplay: chạy Play Mode scene liên quan và xác nhận hành vi mong muốn.
