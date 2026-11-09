# Kết quả kiểm chứng — 08/11/2026

Môi trường: Unity 6000.3.15f1, Windows Editor, nhánh Hoang, commit nền a9027e8. Working tree ban đầu sạch. Không có test baseline chạy trước khi thêm code; báo cáo dưới đây là regression toàn bộ suite sau triển khai, không suy diễn baseline chưa đo.

## Thay đổi sau yêu cầu bỏ tab Restaurant

Người dùng yêu cầu bỏ màn Restaurant mẫu. Giao diện hiện còn ba tab Skill tree, Kitchen grid, Shop & daily, mở mặc định ở Skill tree. Vòng tick demo tự động đã được bỏ để không có mô phỏng ngầm sinh khách/cộng Gold. Service `RemakeRestaurant` vẫn giữ vai trò tính buff và kiểm chứng API tích hợp.

Kiểm chứng mới: `unity recompile` thành công; chạy `unity command test run --mode EditMode --filter Hoang.Remake.Tests.RemakeSceneTests --async_tests true` trong Editor đang mở, kết quả **2/2 passed**. Test xác nhận prefab có đúng ba tab và Skill tree mở mặc định, đủ serialized references; scene không missing script và chỉ có một bootstrap/Input System. Scene/prefab được sửa trực tiếp, không dựng lại các phần UI còn lại. Scene đang mở được giữ lại và đã lưu; phạm vi Git chỉ trong `_Hoang`. PlayMode smoke đã cập nhật chỉ số ba tab, chưa chạy lại sau lần bỏ tab này.

## Bằng chứng trước khi bỏ tab Restaurant

| Phép kiểm | Kết quả |
| --- | --- |
| Unity compile + Editor API sinh scene/prefab | Thành công, method `Hoang.Remake.Editor.RemakeSceneBuilder.Build` thoát 0 |
| Toàn bộ EditMode | **833 passed / 833**, 0 failed; gồm 45 test mới của remake |
| Toàn bộ PlayMode | **14 passed / 14**, 0 failed; gồm smoke end-to-end remake và 13 test gốc |
| Serialized scene/prefab | Reference UI kể cả object inactive đầy đủ; một bootstrap remake, một controller mỗi tính năng, graph 9 node, EventSystem Input System, không missing script |
| Review độc lập | Đã sửa các lỗi reset/recovery/crash window, ServiceLocator qua domain reload, dirty scene/cancel và cấu hình ảnh hưởng discovery test gốc; reviewer xác nhận không còn blocker trong các sửa đổi |
| Phạm vi Git | Chỉ có file mới trong `Assets/_Hoang`; không đổi code/asset `_Project`, Packages, ProjectSettings hoặc Build Settings chung |

Smoke test của bản bốn tab chạy scene thật với save tạm: bấm Daily hai lần, hoàn tất ad và gọi hoàn tất lặp, bấm gói Gold; bấm skill node, chọn thiết bị và đặt qua GridUI; xác nhận điện 35/120W và cooking multiplier 1.25; prerequisite và nhập nguyên liệu giá 96 Gold qua API; đọc lại typed save; đi qua Settings/Reset với cancel rồi confirm; xác nhận điểm/buff/đồ/claim về mặc định. Dữ liệu game gốc được các test gốc bảo vệ bằng đường dẫn tạm và hash.

Ảnh chụp PlayMode 1920x1080 đã được xem trực tiếp ở đợt triển khai ban đầu là ảnh của bản bốn tab. Cần dùng ảnh mới khi kiểm chứng bố cục ba tab sau yêu cầu bỏ Restaurant. Tooltip có kiểm thử tính toán các góc safe area rất hẹp và nội dung dài.

## Những lỗi đã phát hiện và khắc phục khi kiểm tra

- Lần EditMode đầu có 2 test Shop thất bại vì fixture giữ `_written` từ test trước. Đã reset field trong SetUp; không bỏ test.
- Lần full PlayMode đầu sau khi thêm config có 1 test gốc thất bại do `FindAssets(t:NewGameConfig).Single()`. Đã dùng type và asset khởi tạo riêng cho remake; không chỉnh test gốc.
- Review tìm ra reset có thể mất main/backup khi ghi lỗi hoặc crash giữa bước; đã giữ và nhận diện recovery backup, thêm test tái hiện cả hai trường hợp.

## Lệnh tái chạy

```powershell
unity run . --log-file 'Assets/_Hoang/Validation~/generate.log' --no-tail -- -executeMethod Hoang.Remake.Editor.RemakeSceneBuilder.Build
unity test . --mode EditMode --output 'Assets/_Hoang/Validation~/editmode.xml'
unity test . --mode PlayMode --output 'Assets/_Hoang/Validation~/playmode-all.xml'
```

Đóng Editor trước batch test/run. Không chạy song song hai tiến trình Unity trên cùng project. Rebuild thay layout sinh tự động, nên lưu bản sao scene/prefab nếu đã sửa thủ công.

Kết quả XML/log/ảnh nằm trong `Assets/_Hoang/Validation~`, được Git bỏ qua. Hướng dẫn sử dụng và các khác biệt tích hợp nằm trong `RestaurantRemake.md`.

## Giới hạn đã xác định

- Chưa build player/APK hoặc kiểm trên thiết bị, chưa kiểm tra notch thực, cảm ứng thiết bị thật hay hiệu năng thiết bị.
- Chưa tích hợp SDK quảng cáo; Editor simulator hoạt động, code player chỉ báo unavailable. Chưa xác minh callback SDK thật.
- Không có thời gian server/chống sửa save hoặc chống chỉnh đồng hồ tiến; đây là giới hạn có sẵn của đặc tả.
- Giao diện dùng hình/kiểu chữ cơ bản của Unity, không có artwork/âm thanh/VFX mới. Các asset nguồn trong `_Project` chỉ được tham chiếu đọc.
- Không commit hoặc push trong phiên này.
