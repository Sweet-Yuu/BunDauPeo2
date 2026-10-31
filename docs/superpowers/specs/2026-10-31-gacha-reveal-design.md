# Spec: Hiển thị kết quả quay Gacha (cutscene, reveal, trang tổng)

> Ngày: 2026-10-31 · Trạng thái: **chờ user duyệt** · Phạm vi: luồng hiển thị sau khi bấm Quay, không đổi cách roll.
> Nền: [spec Gacha](2026-10-31-gacha-design.md) (mục 8 loại trừ "hiệu ứng, animation quay"; spec này đưa một phần vào phạm vi). Luật chơi: [game-design.md](../../design/game-design.md#gacha).

## 1. Mục tiêu

Sau khi bấm Quay 1 hoặc Quay 10, người chơi xem một cutscene ngắn, rồi thấy từng nhân vật vừa quay với artwork, tên, số sao và hiệu ứng lóe sáng. SSR luôn đứng cuối và có cutscene riêng không bỏ qua được. Quay 10 kết thúc ở trang tổng.

Roll, trừ Gem, pity và lưu không đổi ([spec Gacha 4.3](2026-10-31-gacha-design.md#43-gachaservice)). Toàn bộ spec này chỉ là lớp trình bày.

## 2. Quyết định đã chốt với user (2026-10-31)

| Chủ đề | Quyết định |
|---|---|
| Số sao | Mỗi nhân vật có field số sao khởi điểm, chỉnh trong Inspector. Mặc định R = 1, SR = 2, SSR = 3. Đây chỉ là giá trị khởi điểm, không kèm luật nâng sao (spec nâng sao làm sau) |
| Cutscene thường | Clip `CommonSummon` (3,5 giây). Chạm để bỏ qua |
| Cutscene SSR | Clip `LegendarySummon` (2,6 giây). Không bỏ qua được, không nhận chạm |
| Trang tổng | Lưới 2 hàng × 5 icon (5 trên, 5 dưới), hiện lần lượt, mượt. Bố cục chi tiết theo ảnh user sẽ gửi (xem mục 9) |
| Art và animation | Copy cả bộ cutscene từ `Project_04_BunDauPeo/Assets/__Vu/Gacha` (xem mục 6) |

## 3. Luồng

**Quay 1 hoặc Quay 10**
1. Khoá các nút quay. Phát `CommonSummon` (3,5 giây).
2. Chạm trong lúc cutscene thường: bỏ qua phần còn lại của cutscene, vào reveal nhân vật đầu tiên.
3. Hết cutscene: hiện lần lượt từng nhân vật thường (theo thứ tự quay). Mỗi nhân vật hiện artwork (`Portrait`), tên, số sao, và một lần lóe sáng theo màu độ hiếm. Chạm để sang nhân vật kế.

**Nút Bỏ qua (chỉ Quay 10)**
- Có SSR trong lần quay: nhảy tới cutscene SSR đầu tiên.
- Không có SSR: nhảy tới trang tổng.
- Nút này không hiện khi Quay 1.

**SSR**
- Mỗi SSR có cutscene `LegendarySummon` không bỏ qua được. Chạm và nút Bỏ qua đều không tác dụng trong lúc chạy.
- Hết cutscene: lóe sáng như nhân vật thường. Chạm để sang SSR kế.
- SSR luôn sau mọi nhân vật thường. Thứ tự trong nhóm SSR là thứ tự quay.
- Sau SSR cuối: vào trang tổng.

**Quay 1**
- Không có trang tổng. Sau reveal của nhân vật, quay về màn banner, ô kết quả hiện như hiện nay.

**Trang tổng (Quay 10)**
- Hiện 10 icon theo bố cục ở mục 2, mỗi icon có độ hiếm và trạng thái MỚI / TRÙNG như hiện nay.
- Có nút Đóng để quay về màn banner.

**Trạng thái nút quay**
- Khoá trong suốt quá trình reveal và cutscene. Mở lại sau khi đóng trang tổng hoặc sau reveal của Quay 1.

## 4. Kiến trúc

- `GachaRevealSequence` (class C# thuần, assembly `UI`): nhận danh sách `GachaPullEntry`, trả các bước theo thứ tự ở mục 3 và trạng thái hiện tại. Có hàm `SkipToSsrOrTotal()` và `Tap()`. Không dùng `UnityEngine` để test được bằng EditMode.
- `GachaRevealView` (MonoBehaviour, assembly `UI`): đọc bước hiện tại từ sequence, chạy cutscene bằng `Animator`, tween lóe sáng bằng coroutine, và chuyển bước khi clip kết thúc hoặc khi chạm. Không chứa logic thứ tự.
- `GachaView` giữ nguyên vai trò: bấm nút, gọi `GachaService`, rồi giao kết quả cho `GachaRevealView`. Vẫn vẽ lưới Quay 1 như hiện nay.
- `CharacterDefinition` thêm `[SerializeField] private int _stars` và property `Stars`. Đây là thay đổi interface của assembly `Characters`. `ProjectSetup` đặt giá trị mẫu theo độ hiếm.
- Cutscene dựng bằng `Animator` của clip copy sang. Controller có một state `Summon`, không lặp. Thời lượng do clip quyết định, không có trường thời lượng riêng.

Clip và prefab dùng `unscaled time` (theo ghi chú `NativeCutscenes.md` của Project_04), để không phụ thuộc `Time.timeScale`.

## 5. Dữ liệu và lưu

- Không đổi `PlayerSave`. Số sao là dữ liệu định nghĩa của nhân vật (`CharacterDefinition`), không lưu trong save.
- Không đổi roll, pity, mảnh.
- Trình tự hiển thị chỉ tồn tại trong bộ nhớ. Thoát game giữa chừng không để lại trạng thái nào, vì kết quả đã lưu từ lúc roll.

## 6. Tài sản copy từ Project_04

Copy từ `Assets/__Vu/Gacha/` sang BunDauPeo, giữ nguyên tên file và `.meta` để GUID khớp:

| Nguồn (Project_04) | Đích (BunDauPeo) |
|---|---|
| `Animations/Summoning/CommonSummon.anim`, `.controller` (+ `.meta`) | `Assets/_Project/Gacha/Animations/Summoning/` |
| `Animations/Summoning/LegendarySummon.anim`, `.controller` (+ `.meta`) | `Assets/_Project/Gacha/Animations/Summoning/` |
| `Prefabs/Cutscenes/CommonSummon.prefab`, `LegendarySummon.prefab` (+ `.meta`) | `Assets/_Project/Gacha/Prefabs/Cutscenes/` |
| `Art/Summoning/Spark.png`, `Ring.png`, `Glow.png` (+ `.meta`) | `Assets/_Project/Gacha/Art/Summoning/` |

- Prefab dùng component `Image` của uGUI và ba PNG trên. Không cần script riêng.
- Đây là file user cung cấp, không phải ảnh do agent tạo. Việc copy nằm ngoài lệnh "không thêm file ảnh" của `CLAUDE.md`, nên cần thêm một ngoại lệ có phạm vi hẹp (xem mục 8).
- Copy file `.meta` là sao chép nguyên văn, không sửa tay. Phải kiểm sau khi copy rằng Unity không sinh GUID mới.

## 7. Test

EditMode, cùng assembly `BunDauPeo.Tests`:
- Thứ tự bước: nhân vật thường theo thứ tự quay, SSR sau cùng, theo thứ tự quay trong nhóm SSR.
- Chạm trong cutscene thường vào reveal đầu tiên; chạm trong reveal sang nhân vật kế.
- Chạm trong cutscene SSR không làm gì; nút Bỏ qua không làm gì.
- Bỏ qua khi có SSR đi tới SSR đầu tiên; không có SSR đi tới trang tổng.
- Quay 1 không có nút Bỏ qua và không có trang tổng.
- Quay 10 có 2 SSR: cả hai đều có cutscene, theo thứ tự.
- Số sao mặc định theo độ hiếm trong `ProjectSetup` (R = 1, SR = 2, SSR = 3).
- Tài sản copy: các clip, controller, prefab, PNG có mặt; prefab tham chiếu được clip và PNG qua GUID (đọc lại từ file, như L-002).

Kiểm trong Editor (Play Mode, bằng tay với user): thời lượng cutscene, độ mượt của lưới trang tổng, lóe sáng có đúng màu độ hiếm.

## 8. Tài liệu cần sửa khi làm

- `CLAUDE.md`, luật "UI và hình tạm": thêm ngoại lệ hẹp cho art do user cung cấp trong `Assets/_Project/Gacha/Art/Summoning/`. Agent vẫn không tự tạo hay thêm ảnh.
- `docs/superpowers/specs/2026-10-31-gacha-design.md` mục 8: bỏ "hiệu ứng ... animation quay" khỏi danh sách ngoài phạm vi, ghi rõ spec này làm phần reveal. Animation nhân vật thật và âm thanh vẫn ngoài phạm vi.
- `docs/design/game-design.md` mục Gacha: thêm luật SSR đứng cuối, không bỏ qua; quay 10 có nút bỏ qua tới SSR hoặc trang tổng.
- `docs/project/structure.md`: thêm `Gacha/Prefabs/Cutscenes` và `Gacha/Art/Summoning`.
- `docs/lessons/lesson.md` khi có lỗi.

## 9. Chưa chốt

- **Bố cục trang tổng:** cần ảnh user đã nhắc. Tin nhắn không có ảnh đính kèm. Cho đến khi có ảnh, bố cục là 2 hàng × 5 như mục 2 và chưa có chi tiết kích thước.
- **Nút Đóng trang tổng:** đặt ở đâu, chữ gì. Mặc định: nút dưới cùng, chữ "Đóng".
- **Tốc độ lần lượt của trang tổng:** mặc định mỗi icon hiện cách nhau 0,08 giây.

## 10. Ngoài phạm vi

- Nâng sao và luật số sao theo cấp (spec nâng sao).
- Âm thanh, animation nhân vật thật, art thật cho `Portrait`. Hiện `Portrait` vẫn là placeholder.
- Lịch sử quay, banner chọn được (theo spec Gacha mục 8).
