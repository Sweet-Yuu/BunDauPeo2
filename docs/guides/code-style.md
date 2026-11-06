# Quy ước code C#

> **Đọc khi:** viết/sửa script C#.
> **Cập nhật khi:** user chốt thêm quy ước, hoặc một lesson được nâng thành luật (ghi số lesson trong ngoặc).

- Naming: `PascalCase` cho class/method/property; `camelCase` cho biến cục bộ/tham số; field private `_camelCase`.
- Field chỉnh trong Inspector: `[SerializeField] private`, không dùng `public` chỉ để hiện Inspector.
- Không dùng `Find*` / `GetComponent` trong `Update`; cache ở `Awake`/`Start`.
- Vật lý trong `FixedUpdate`; input đọc ở `Update`.
- Input: dùng Input System (xem [tech-stack](../project/tech-stack.md)), không dùng `UnityEngine.Input` cũ.
- Một MonoBehaviour = một trách nhiệm. Dữ liệu cấu hình tách ra `ScriptableObject`.
- Comment chỉ giải thích *vì sao*, không lặp lại code làm gì.
- Sau khi sửa code: kiểm tra Console không có lỗi compile/warning mới.
- File có `using UnityEngine` thì viết `System.Random` đầy đủ cho tham số và field RNG (L-018).

## Service, tiền và save

- Mọi `event` bắn sau khi đã đổi trạng thái dùng `SafeInvoke()` (Core), không `?.Invoke()` trần (L-003).
- Lỗi IO (file đang bị khoá, tạm không đọc được) không được coi là dữ liệu hỏng: để lỗi nổi lên thay vì trả `null` (L-003).
- Field nào làm mẫu số, chỉ số mảng hay ngưỡng thì phải có trong `Validate()` và có test. Khi `IsRunning = false`, phần còn lại của constructor không được dùng config (L-016).
- Trạng thái "bị chặn" mà nhiều view cùng bật/tắt thì theo chủ (`SetX(owner, bool)`, idempotent theo chủ), không dùng một bool hay bộ đếm tăng giảm (L-015).
- Mô phỏng bước cố định: trong mỗi bước, hệ thống trừ giờ chạy trước hệ thống đổi trạng thái (L-008).

## Code Editor

- Nạp asset bằng `AssetDatabase.LoadAssetAtPath` **sau** khi tạo/đổi scene, không giữ biến từ trước `NewScene(Single)`. Code dùng `UnityEditor` nằm trong assembly Editor (L-002, L-001).

## Test

- Với mỗi test, hỏi "cài đặt sai nào vẫn qua test này?" và chọn đầu vào làm cài đặt sai cho kết quả khác. Test RNG hỏng hay lỗi phải có ca giữa chừng, không chỉ lần gọi đầu (L-006).
- Logic tiền/save phải có ca "tài nguyên đang bị khoá" và "handler ném lỗi" (L-003).
- Test thời gian của mô phỏng bước cố định chặn ở mốc ±nửa bước, không dùng dung sai rộng hơn một bước (L-008).
- Nhiều đầu vào biên thì lặp trong một method kèm thông báo có đầu vào (`$"u = {u}"`), không dùng `[TestCase]`, để tổng số test khớp kế hoạch (L-012).
- Test duyệt scene có đối tượng tắt dùng `GetComponentInParent<T>(true)` / `GetComponentsInChildren<T>(true)` (L-013).
- Sinh scene bằng code thì phải có test đọc lại tham chiếu từ file scene (L-002).
- Test kiểm nút đè nút luôn gồm nút đang tắt (`FindObjectsInactive.Include` / `GetComponentsInChildren<Button>(true)`): nút Bỏ qua, nút popup, nút kết quả chỉ bật trong luồng chơi nhưng vẫn có `RectTransform` đúng chỗ. Test chỉ xét nút đang bật sẽ xanh dù nút bị che (L-025, L-027).
