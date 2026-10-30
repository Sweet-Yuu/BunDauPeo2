# Quy ước code C#

> **Đọc khi:** viết/sửa script C#.
> **Cập nhật khi:** user chốt thêm quy ước, hoặc một lesson lặp lại ≥ 2 lần nên trở thành luật.

- Naming: `PascalCase` cho class/method/property; `camelCase` cho biến cục bộ/tham số; field private `_camelCase`.
- Field chỉnh trong Inspector: `[SerializeField] private`, không dùng `public` chỉ để hiện Inspector.
- Không dùng `Find*` / `GetComponent` trong `Update`; cache ở `Awake`/`Start`.
- Vật lý trong `FixedUpdate`; input đọc ở `Update`.
- Input: dùng Input System (xem [tech-stack](../project/tech-stack.md)), không dùng `UnityEngine.Input` cũ.
- Một MonoBehaviour = một trách nhiệm. Dữ liệu cấu hình tách ra `ScriptableObject`.
- Comment chỉ giải thích *vì sao*, không lặp lại code làm gì.
- Sau khi sửa code: kiểm tra Console không có lỗi compile/warning mới.
