# Spec: Hiển thị kết quả quay Gacha (cutscene, reveal, trang tổng, quay tiếp)

> Ngày: 2026-10-31 · Trạng thái: **chờ user duyệt** · Phạm vi: luồng hiển thị sau khi bấm Quay, không đổi cách roll.
> Nền: [spec Gacha](2026-10-31-gacha-design.md) (mục 8 loại trừ "hiệu ứng, animation quay"; spec này đưa một phần vào phạm vi). Luật chơi: [game-design.md](../../design/game-design.md#gacha).

## 1. Mục tiêu

Sau khi bấm Quay 1 hoặc Quay 10, người chơi xem một cutscene ngắn, rồi thấy từng nhân vật vừa quay với artwork, tên, số sao và hiệu ứng lóe sáng. SSR luôn đứng cuối và có cutscene riêng không bỏ qua được. Cả Quay 1 và Quay 10 đều kết thúc ở trang tổng, từ đó người chơi quay tiếp hoặc thôi.

Roll, trừ Gem, pity và lưu không đổi ([spec Gacha 4.3](2026-10-31-gacha-design.md#43-gachaservice)). Toàn bộ spec này chỉ là lớp trình bày.

## 2. Quyết định đã chốt với user (2026-10-31)

| Chủ đề | Quyết định |
|---|---|
| Số sao | Mỗi nhân vật có field số sao khởi điểm, chỉnh trong Inspector. Mặc định R = 1, SR = 2, SSR = 3. Đây chỉ là giá trị khởi điểm, không kèm luật nâng sao (spec nâng sao làm sau) |
| Cutscene thường | Clip `CommonSummon` (3,5 giây). Chạm để bỏ qua |
| Cutscene SSR | Clip `LegendarySummon` (2,6 giây). Không bỏ qua được, không nhận chạm |
| Trang tổng | Quay 10: lưới 2 hàng × 5 icon (5 trên, 5 dưới). Quay 1: một icon. Icon hiện lần lượt, mượt. Bố cục tham chiếu theo ảnh user gửi (mục 9) |
| Quay 1 | **Có trang tổng**, không có nút Bỏ qua |
| Thoát trang tổng | Không có nút Đóng riêng. Người chơi chuyển trang bằng tab bên dưới (hub, ngoài phạm vi spec này). Trong spec này, nút OK là đường thoát khỏi trang |
| Quay tiếp | Bấm OK hiện popup: **Quay tiếp** (cùng số lượt vừa quay) hoặc **Thôi** (về banner). Không đủ Gem cho lượt đó thì hiện cảnh báo và nút Quay tiếp mờ |
| Thôi | Về trang roll (banner), không hiện lại kết quả cũ |
| Lịch sử quay | Lưu trong save, 50 mục gần nhất, Reset xoá. Trang roll có nút Lịch sử mở popup chỉ đọc |
| Art và animation | Copy cả bộ cutscene từ `Project_04_BunDauPeo/Assets/__Vu/Gacha` (mục 6). File do user gửi và yêu cầu copy được phép dùng |

## 3. Luồng

**Bấm Quay 1 hoặc Quay 10**
1. Khoá các nút quay. Phát `CommonSummon` (3,5 giây).
2. Chạm trong lúc cutscene thường: bỏ qua phần còn lại của cutscene, vào reveal nhân vật đầu tiên.
3. Hết cutscene: hiện lần lượt từng nhân vật thường (theo thứ tự quay). Mỗi nhân vật hiện artwork (`Portrait`), tên, số sao, và một lần lóe sáng theo màu độ hiếm. Chạm để sang nhân vật kế.

**Nút Bỏ qua (chỉ Quay 10, hiện trong lúc reveal)**
- Có SSR trong lần quay: nhảy tới cutscene SSR đầu tiên.
- Không có SSR: nhảy tới trang tổng.
- Không hiện khi Quay 1.

**SSR**
- Mỗi SSR có cutscene `LegendarySummon` không bỏ qua được. Chạm và nút Bỏ qua đều không tác dụng trong lúc chạy.
- Hết cutscene: lóe sáng như nhân vật thường. Chạm để sang SSR kế.
- SSR luôn sau mọi nhân vật thường. Thứ tự trong nhóm SSR là thứ tự quay.
- Sau SSR cuối: vào trang tổng.

**Trang tổng (cả Quay 1 và Quay 10)**
- Quay 10: 10 icon theo lưới 5 + 5, Quay 1: 1 icon. Mỗi icon có artwork, khung màu độ hiếm, và nhãn MỚI hoặc TRÙNG như hiện nay.
- Icon hiện lần lượt như reveal, khoảng cách 0,08 giây.
- Nút **OK** ở dưới trang. Bấm OK mở popup quay tiếp (xem dưới).

**Popup quay tiếp**
- Nội dung: "Quay tiếp?" kèm số lượt và giá, ví dụ "Quay tiếp (900 Gem)".
- Nút **Quay tiếp**: chạy lại đúng luồng với cùng số lượt vừa quay. Nếu không đủ Gem cho lượt đó, nút này mờ và popup hiện cảnh báo "Không đủ Gem" kèm số Gem cần.
- Nút **Thôi**: đóng popup, về trang roll (banner). Trang roll không hiện lại kết quả cũ.

**Lịch sử quay**
- Mỗi lần bấm Quay 1 hoặc Quay 10 (đã roll thành công) ghi một mục vào lịch sử: thời điểm, banner, số lượt (1 hoặc 10), và danh sách Id nhân vật theo thứ tự quay.
- Lịch sử lưu trong save (`PlayerSave.GachaHistory`), giữ **50 mục gần nhất**; mục cũ nhất bị bỏ khi đủ 50. Reset Save Data xoá sạch.
- Ghi lịch sử nằm trong cùng bước lưu của lượt quay (một lần `persist`), để không có lượt nào lưu thiếu lịch sử hoặc ngược lại.
- Trang roll có nút **Lịch sử**. Bấm mở popup liệt kê các mục, mới nhất trên cùng. Mỗi dòng: thời điểm, "Quay 1" hoặc "Quay 10", và tên các nhân vật (tên từ `CharacterCatalog`; Id không còn trong catalog thì hiện Id).
- Popup lịch sử chỉ đọc, có nút Đóng.

**Trạng thái nút quay trên banner**
- Khoá trong suốt cutscene, reveal và trang tổng. Mở lại khi về trang roll (sau Thôi), hoặc khi popup đóng.
- Nút Quay 1 và Quay 10 mờ khi không đủ Gem, như hiện nay.

## 4. Kiến trúc

- `GachaRevealSequence` (class C# thuần, assembly `UI`): nhận danh sách `GachaPullEntry`, trả các bước theo thứ tự ở mục 3 và trạng thái hiện tại. Có hàm `SkipToSsrOrTotal()` và `Tap()`. Không dùng `UnityEngine` để test được bằng EditMode.
- `GachaRevealView` (MonoBehaviour, assembly `UI`): đọc bước hiện tại từ sequence, chạy cutscene bằng `Animator`, tween lóe sáng bằng coroutine, và chuyển bước khi clip kết thúc hoặc khi chạm. Không chứa logic thứ tự.
- `GachaTotalView` (MonoBehaviour): dựng lưới icon, hiện lần lượt, nút OK. Không chứa logic quay.
- `GachaRollAgainPopup` (MonoBehaviour): hiện popup, cảnh báo, nút Quay tiếp và Thôi. Quyết định bật/tắt nút Quay tiếp dựa trên `Wallet.CanAfford` và giá của lượt vừa quay, qua hàm tĩnh trong `GachaViewText` để test được.
- `GachaHistoryPopup` (MonoBehaviour): đọc `PlayerSave.GachaHistory` qua hàm tĩnh trong `GachaViewText` để định dạng từng dòng, hiện danh sách, nút Đóng.
- `GachaService` (assembly `Gacha`) thêm mục lịch sử vào `save.GachaHistory` trong bước áp kết quả, trước `persist()` (mục 5).
- `GachaView` giữ vai trò điều phối: bấm nút quay, gọi `GachaService`, giao kết quả cho `GachaRevealView`, rồi `GachaTotalView`. Không còn vẽ lưới kết quả trên banner.
- `CharacterDefinition` thêm `[SerializeField] private int _stars` và property `Stars`. Đây là thay đổi interface của assembly `Characters`. `ProjectSetup` đặt giá trị mẫu theo độ hiếm.
- Cutscene dựng bằng `Animator` của clip copy sang. Controller có một state `Summon`, không lặp. Thời lượng do clip quyết định, không có trường thời lượng riêng.

Clip và prefab dùng `unscaled time` (theo ghi chú `NativeCutscenes.md` của Project_04), để không phụ thuộc `Time.timeScale`.

## 5. Dữ liệu và lưu

- `PlayerSave` thêm `public List<GachaHistoryEntry> GachaHistory = new();` (giá trị khởi tạo, `Version` giữ 1). `GachaHistoryEntry` là struct `[Serializable]` với `public long Utc`, `public string BannerId`, `public int Count`, `public List<string> CharacterIds`. Struct này nằm trong `Core` cùng `PityEntry`, vì `PlayerSave` ở `Core`.
- Số sao là dữ liệu định nghĩa của nhân vật (`CharacterDefinition`), không lưu trong save.
- Không đổi roll, pity, mảnh. Nút Quay tiếp gọi lại `GachaService.PullSingle` hoặc `PullTen` như một lượt quay mới, và ghi thêm một mục lịch sử.
- Field mới phải được Reset xoá: `ResetToDefaults` ghi đè cả `PlayerSave` (D-005), test phải có assert `GachaHistory` rỗng sau reset.
- Trình tự hiển thị chỉ tồn tại trong bộ nhớ. Thoát game giữa chừng không để lại trạng thái nào, vì kết quả đã lưu từ lúc roll.

## 6. Tài sản copy từ Project_04

Copy từ `Assets/__Vu/Gacha/` sang BunDauPeo, giữ nguyên tên file và `.meta` để GUID khớp:

| Nguồn (Project_04) | Đích (BunDauPeo) |
|---|---|
| `Animations/Summoning/CommonSummon.anim`, `.controller` (+ `.meta`) | `Assets/_Project/Gacha/Animations/Summoning/` |
| `Animations/Summoning/LegendarySummon.anim`, `.controller` (+ `.meta`) | `Assets/_Project/Gacha/Animations/Summoning/` |
| `Prefabs/Cutscenes/CommonSummon.prefab`, `LegendarySummon.prefab` (+ `.meta`) | `Assets/_Project/Gacha/Prefabs/Cutscenes/` |
| `Art/Summoning/Spark.png`, `Ring.png`, `Glow.png` (+ `.meta`) | `Assets/_Project/Gacha/Art/Summoning/` |

- Prefab dùng component `Image` của uGUI và ba PNG trên. Không cần script riêng (GUID `fe87c0e1…` là `Image` của uGUI).
- Đây là file user cung cấp và yêu cầu copy, không phải ảnh do agent tạo. Không sửa `CLAUDE.md`.
- Copy file `.meta` là sao chép nguyên văn, không sửa tay. Phải kiểm sau khi copy rằng Unity không sinh GUID mới.

## 7. Test

EditMode, cùng assembly `BunDauPeo.Tests`:
- Thứ tự bước: nhân vật thường theo thứ tự quay, SSR sau cùng, theo thứ tự quay trong nhóm SSR.
- Chạm trong cutscene thường vào reveal đầu tiên; chạm trong reveal sang nhân vật kế.
- Chạm trong cutscene SSR không làm gì; nút Bỏ qua không làm gì.
- Bỏ qua khi có SSR đi tới SSR đầu tiên; không có SSR đi tới trang tổng.
- Quay 1: không có nút Bỏ qua, vẫn có trang tổng với 1 icon.
- Quay 10 có 2 SSR: cả hai đều có cutscene, theo thứ tự.
- Popup: đủ Gem thì nút Quay tiếp bật; thiếu Gem thì nút Quay tiếp mờ và có cảnh báo. Thôi luôn bật.
- Lịch sử: mỗi lượt quay thành công thêm đúng một mục; lượt bị từ chối (thiếu Gem, banner lỗi) không thêm mục; mục thứ 51 đẩy mục cũ nhất đi; mục lưu qua round-trip `SaveService`; Reset xoá sạch; định dạng dòng lịch sử (tên hoặc Id khi thiếu trong catalog).
- Số sao mặc định theo độ hiếm trong `ProjectSetup` (R = 1, SR = 2, SSR = 3).
- Tài sản copy: các clip, controller, prefab, PNG có mặt; prefab tham chiếu được clip và PNG qua GUID (đọc lại từ file, như L-002).

Kiểm trong Editor (Play Mode, bằng tay với user): thời lượng cutscene, độ mượt của lưới trang tổng, lóe sáng có đúng màu độ hiếm, popup khi thiếu Gem.

## 8. Tài liệu cần sửa khi làm

- `docs/superpowers/specs/2026-10-31-gacha-design.md` mục 8: bỏ "hiệu ứng ... animation quay" và "Nhật ký lịch sử quay" khỏi danh sách ngoài phạm vi; ghi rõ spec này làm phần reveal, trang tổng và lịch sử. Animation nhân vật thật và âm thanh vẫn ngoài phạm vi.
- `docs/design/game-design.md` mục Gacha: thêm luật SSR đứng cuối và không bỏ qua; quay 10 có nút bỏ qua tới SSR hoặc trang tổng; mọi lượt quay kết thúc ở trang tổng có OK và popup quay tiếp.
- `docs/project/structure.md`: thêm `Gacha/Prefabs/Cutscenes` và `Gacha/Art/Summoning`.
- `docs/lessons/lesson.md` khi có lỗi.

## 9. Chưa chốt

- **Không có mục mở về luật.** Đã chốt: không có thanh "Char. Exchange Pts" và không có dòng chú thích trùng; Thôi về trang roll; lịch sử lưu trong save 50 mục; Quay 1 có trang tổng 1 icon căn giữa.
- **Bố cục chi tiết:** theo ảnh tham chiếu user gửi (lưới 5 + 5, khung màu độ hiếm, nhãn MỚI / TRÙNG, nút OK). Kích thước và màu cụ thể chờ kế hoạch.

## 10. Ngoài phạm vi

- Nâng sao và luật số sao theo cấp (spec nâng sao).
- Âm thanh, animation nhân vật thật, art thật cho `Portrait`. Hiện `Portrait` vẫn là placeholder.
- Thanh điểm đổi và thanh chuyển trang dưới (hub, spec UI hub).
- Banner chọn được và lọc lịch sử theo banner (theo spec Gacha mục 8).
