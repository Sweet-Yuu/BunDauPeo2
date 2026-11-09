# Bản remake hệ thống nhà hàng

Ngày triển khai: 08/11/2026. Phạm vi lấy từ hai tài liệu nguồn đi kèm trong thư mục này; không thực thi các prompt Claude hoặc hướng dẫn push của lịch sử cũ.

## Mở và chơi

1. Mở `Assets/_Hoang/Scenes/RestaurantSystems.unity`, hoặc chọn `Hoang > Restaurant Remake > Open scene`.
2. Bấm Play. Có thể chọn trực tiếp `Hoang > Restaurant Remake > Play remake`.
3. Ba tab: Skill tree, Kitchen grid, Shop & daily. Skill tree mở mặc định. Giao diện tiếng Anh và chỉnh được trong Edit Mode.

Scene có bootstrap riêng, không chạy song song với `GameBootstrap` gốc. Khi Play scene remake, editor tạm đổi scene khởi động và phục hồi lựa chọn cũ sau khi dừng, kể cả có domain reload. Các scene gốc và Build Settings chung không đổi.

## Các hệ thống

| Hệ thống | Hành vi |
| --- | --- |
| Skill Tree | 9 skill mẫu, 3 nhánh, prerequisite theo cấp, điểm thường/đặc biệt, max level, hoàn đúng số điểm đã dùng; stable ID bảo toàn tiến trình khi đổi tên |
| Facilities | Lưới 11x11, 7 loại thiết bị mẫu và 3 đoạn dây; xoay 4 hướng chuẩn hóa, collision, kéo/thả hoặc chọn rồi bấm ô, di chuyển/thu hồi/return all |
| Power Grid | BFS 4 hướng, nguồn/conductor/relay, giới hạn công suất, thiết bị requiresRelay phải cạnh nguồn hoặc relay đang hoạt động; chỉ thiết bị có điện cấp buff |
| Buff Bridge | Cộng skill + thiết bị có điện; tốc độ nấu/doanh thu/kiên nhẫn; giảm giá nguyên liệu tối đa 80%; vệ sinh và thông số điện |
| API kiểm chứng buff | `RemakeRestaurant` giữ phần tích hợp tốc độ nấu, doanh thu, kiên nhẫn, giá nguyên liệu và vệ sinh để kiểm chứng qua service/test |
| Tooltip | 5 tầng nội dung, Mouse/Touch qua Input System, lật pivot và fit/clamp safe area; tắt khi đóng UI |
| Daily | Bấm nhận 100 Gold + 100 Diamond một lần mỗi ngày lịch UTC+7, reset 00:00, countdown và dấu [READY] ở tab |
| Shop | 50/200/500 Diamond đổi 1000/4500/12000 Gold; thiếu tiền hoặc tràn long thì không giao dịch |
| Rewarded ad | Hoàn tất +50 Diamond, tối đa 3/ngày UTC+7; cancel/error không mất lượt; ticket chống lặp và mất hiệu lực khi reset/dispose |

Trong Kitchen grid: đặt Generator trước, đặt Stove sát nguồn để có +20% cooking. Dùng Power cable nối xa; Cold storage phải cạnh nguồn hoặc Power relay có điện. Tổng tiêu thụ của mọi thiết bị mẫu vượt 120W nên cần lựa chọn bố trí. Phím R hoặc nút Rotate xoay thiết bị; kéo thiết bị đã đặt về inventory hoặc chọn Return selected để thu hồi.

Các skill mẫu bắt đầu với 20 điểm thường và 3 điểm đặc biệt. Tài liệu nguồn không chốt một cơ chế kiếm thêm điểm; API `SkillTreeManager.AddPoints` là điểm tích hợp cho nhiệm vụ/cấp độ sau này. Không tự tạo thêm hệ nhiệm vụ hay streak.

Theo yêu cầu người dùng, tab Restaurant, giao diện mô phỏng quán và vòng tick demo tự động đã được bỏ. Không còn mô phỏng chạy ngầm tự sinh khách hoặc cộng Gold từ màn mẫu. `RemakeRestaurant` vẫn giữ API tính buff và tích hợp để kiểm chứng trong test; không có trang khách/nấu/phục vụ/thu ngân, nút Restock hay điểm Reputation trên giao diện.

## Save và Reset

Toàn bộ economy, skill, inventory, vị trí thiết bị và marker Daily/Ad nằm trong **một** `HoangSaveData : PlayerSave`. `Gold = Coin`, `Diamond = Gem`, `Ingredients = Material` theo enum core.

Đường dẫn: `Application.persistentDataPath/HoangRemake/save.json`. Bản remake tách bộ save của bản thử nghiệm khỏi save game gốc, không tạo save riêng cho Shop hoặc Skill Tree. Dữ liệu mặc định 1000 Gold, 5000 Diamond, 0 Ingredients nằm trong `Assets/_Hoang/Data/RemakeDefaults.asset`; nhân vật và dữ liệu món/khách tham chiếu asset hiện có.

`RemakeContext.Commit` tạo candidate, ghi qua core `SaveService`, chỉ thành công mới áp dụng vào RAM và phát sự kiện. IO thất bại không cấp thưởng, tiêu tiền, tiêu điểm hoặc mất đồ. Do API `SaveService` hiện có, retry sẽ serialize lại **cùng candidate**, tối đa 3 lần sau lần đầu, cách 10ms; không chạy lại grant/spend. Các giao dịch chạy đồng bộ trên main thread nên không có proposal async xen kẽ.

Typed load giữ cả field kế thừa và mở rộng; save schema cũ thiếu field mới được cấp default. Backup của SaveService được giữ. Khi Reset, chuẩn bị file default trước, tạm giữ recovery artifacts, thay main nguyên tử rồi xóa artifacts; nếu replacement thất bại thì phục hồi backup. Nếu process bị dừng giữa thao tác, Load nhận diện backup reset đang dở; không lấy candidate default chưa commit. Test bao gồm main hỏng nhưng backup hợp lệ bị gặp khóa khi reset và mô phỏng crash giữa lúc giữ backup/thay main.

Settings > Reset all progress có xác nhận và xóa toàn bộ tiến trình của bản remake. Không có reset riêng trong Shop/Daily. Tools > Hoang > Save & Load có Save current game state, Open save directory và Clear all save data; mục xóa cũng có xác nhận.

Đồng hồ UTC+7 dựa vào thiết bị. Chặn ngày lùi không phải cơ chế chống gian lận server. Quảng cáo mới có simulator `TEST AD - EDITOR`; bản build báo unavailable và không cấp thưởng giả. Không tích hợp SDK quảng cáo hay IAP vì tài liệu bàn giao xác nhận chưa có.

## Asset và công cụ

- UI dự phòng: `Assets/_Hoang/Resources/HoangRestaurantSystems.prefab`.
- Scene và prefab đều có đủ reference, graph nodes và inventory rows chỉnh được trước Play. Runtime tái sử dụng view thiết bị theo instance ID.
- Skill/facility SO: `Assets/_Hoang/Data/Skills` và `Data/Facilities`.
- `Hoang > Restaurant Remake > Rebuild scene and prefab`: dựng lại UI từ generator, giữ SO đã có. Thao tác sẽ thay layout scene/prefab đã sinh; lưu bản sao nếu muốn giữ thay đổi bố cục thủ công.
- `Tools > Hoang > Skill Tree`: validate và căn chỉnh graph đang chọn.
- `Hoang > Restaurant Remake > Build Windows player`: build riêng scene remake qua `BuildPlayerOptions.scenes`, không sửa Build Settings chung.

UI dùng uGUI `Text` và font Unity có sẵn, đồng nhất với dự án hiện tại, thay cho yêu cầu TMP của dự án cũ. Không thêm package/font/SDK. User duyệt artwork ImageGen ngày 09–10/11/2026: ba trang remake và Lobby gốc dùng chung bộ hình quán Việt xanh–kem–gỗ; xem [RestaurantArtHandoff.md](RestaurantArtHandoff.md) để biết phạm vi, file và cách áp dụng lại.

## Kiểm chứng

Kết quả và giới hạn được ghi trong `Validation.md`. Test file dùng thư mục tạm, không reset save thật. XML/log/ảnh chụp QA nằm trong `Assets/_Hoang/Validation~` (không đưa vào Git).
