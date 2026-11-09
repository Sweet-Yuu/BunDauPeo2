# Bàn giao artwork Restaurant — 10/11/2026

## Bắt đầu ở đây

- Nhánh **Hoang**, baseline `5e4327d` (đã merge main/UI hub A); không merge sang main trong task này.
- User duyệt **phương án 1** theo ảnh `Mota/restaurant-skills-art-preview.png`, đồng thời yêu cầu làm đẹp **Lobby gốc** và ghi chép rõ cho chat sau.
- Thiết kế: `docs/superpowers/specs/2026-11-09-restaurant-art-design.md`; kế hoạch/checkbox: `docs/superpowers/plans/2026-11-09-restaurant-art.md`.
- Đã đọc `docs/README.md`, `CLAUDE.md`, `docs/studio/README.md`, quy trình Git/Unity và code-style. Không có README ở root; `CLAUDE.md` trỏ tới `docs/README.md` làm cổng tài liệu.

## Phạm vi được user cho phép lần này

1. Remake: `Assets/_Hoang/Scenes/RestaurantSystems.unity` và `Resources/HoangRestaurantSystems.prefab`, ba trang Skills/Kitchen/Shop.
2. Màn gốc: `Assets/_Project/Scenes/Restaurant.unity`, builder và helper phần nhìn liên quan. Không nhầm với bản sao cũ `Assets/_Hoang/Scenes/Restaurant.unity` (không phải Lobby đang chạy của game gốc).
3. Artwork dùng chung: `Assets/_Project/UI/Resources/RestaurantArt/`; helper `BunDauPeo.UI.RestaurantArt`.
4. Tài liệu trong `docs/` và `_Hoang/Docs/`, ảnh mẫu trong `Mota/`.

Đây là ngoại lệ **riêng task artwork này** đối với giới hạn chỉ sửa `_Hoang` và luật hình tạm mặc định. Không đổi Packages, ProjectSettings/Build Settings, save, economy, phần thưởng, giá, graph prerequisite hoặc luật chơi. Các số trong ảnh mẫu không thay số thật.

## Đang làm / đường tiếp tục

- Đã tạo 3 PNG bằng ImageGen, chép vào repo: sheet 16 skill/thiết bị, sheet 8 tài nguyên/quà và nền vườn.
- **Đã hoàn tất triển khai và QA trong Editor.** Đã tích hợp helper/importer, skin remake/Lobby, cập nhật scene và prefab bằng Unity API. Full suite sau lượt chỉnh UI cuối: **910 EditMode + 24 PlayMode**, 0 failed, 0 skipped. Sau sửa lỗi raster chữ của công cụ chụp (L-034), chạy lại toàn bộ **24/24 PlayMode**, 0 failed/skipped; ảnh xác nhận chữ skill/Menu rõ.
- Chỉ phiên chính chạy Unity, không chạy hai batch/Editor đồng thời. Subagent chỉ sửa code, không chạy Unity hoặc commit.
- Import/apply: `unity run . -- -executeMethod Hoang.Remake.Editor.RestaurantArtSetup.Apply -logFile D:/bdp-restaurant-art/apply.log` (Editor đóng; `unity status` thành lệnh riêng trước).
- Skin dùng Editor API để chỉnh scene/prefab hiện hữu, không sửa tay YAML/meta. Không gọi `RemakeSceneBuilder.Build()` để tránh mất bố cục đã chỉnh tay.
- TextureImporter dùng `ISpriteEditorDataProvider`, giữ spriteID theo tên. Package Sprite thực tế chưa có `ISpriteFrameEditCapability` như skill mới: kiểm provider/`ISpriteNameFileIdDataProvider` theo API đang có; không nâng package.

## Kiểm chứng dự kiến

- Baseline trước task: **906 EditMode + 24 PlayMode**, 0 failed (phiên merge main vào Hoang).
- Test trước gán ảnh: 4 ca art, 3 đỏ đúng chỗ thiếu ảnh, 1 idempotence xanh. Sau import: toàn bộ **910/910** EditMode xanh, gồm check asset/sprite, raycast trang trí, idempotence và serialized references.
- PlayMode bằng save tạm: **24/24**. Luồng giữ nguyên gồm skill upgrade, đặt thiết bị/điện, Daily/Shop/Ad, lưu/Reset, incident, hub và hai ngôn ngữ. Các test chỉ assert bấm theo luồng sẵn có; chưa thay thế thử cảm ứng thật.
- Đã xem ảnh 1920×1080 cả ba trang remake và Lobby/Menu gốc, trạng thái locked/claimed, thiết bị SOURCE/ON và nhãn dài. Chưa xác minh notch/màn 4:3/điện thoại; không coi ảnh 16:9 là bằng chứng các tỉ lệ khác.
- Review độc lập không còn lỗi chặn; đã sửa cache sprite sau import bằng `RestaurantArt.ClearCache()`. Font skill cố định 21 px, viền card vàng nhẹ; đã xem đủ ba trang và Lobby/Menu Việt–Anh.

## Các file/nhóm file đã thay đổi

| Nhóm | Thay đổi |
| --- | --- |
| `_Project/UI/Resources/RestaurantArt` | 24 sprite, nền garden, primitive frame, `.meta` do Unity tạo |
| `_Project/UI/Scripts/RestaurantArt.cs` | Nạp sprite và style card dùng chung, illustration không raycast, cache được xóa sau reimport |
| `_Project/Editor/RestaurantLobbySkin.cs`, `RestaurantSceneBuilder.cs` | Làm đẹp nền, sàn, tường, bếp/quầy/bàn, HUD, nút quán, Menu và nav; hook giữ style khi rebuild |
| `_Project/UI/Scripts/HubNav.cs` | Hai màu tab; không đổi logic |
| `_Project/Scenes/Restaurant.unity` | Scene Lobby gốc đã có skin, giữ mốc actors/incident và điều hướng |
| `_Hoang/Scripts/UI`, SkillTree, Facilities, ShopDaily | Theme, icon/khung/phân cấp chữ, số dư thật, skill locked/max, thiết bị trong đúng ô chiếm; giữ giao dịch/binding |
| `_Hoang/Editor/RestaurantArtSetup.cs`, `RemakeRestaurantSkin.cs` | Import/slice giữ spriteID; gán icon cho SO; cập nhật scene/prefab hiện có |
| `_Hoang/Data/Skills` và `Data/Facilities` | Chỉ trường icon; không đổi số liệu, shape, buff, prerequisite, giá |
| `_Hoang/Scenes/RestaurantSystems.unity`, `Resources/HoangRestaurantSystems.prefab` | UI đã cập nhật và reference mới đã lưu |
| asmdef remake/editor/test | Tham chiếu UI/Editor và Sprite Editor sẵn có; không thêm package |
| test art + remake smoke | 4 EditMode mới; smoke giữ toàn bộ luồng, thêm check artwork không chặn pointer |
| `_Project/Playtest/ScreenCapture1080.cs` | Công cụ QA dựng lại glyph sau khi đổi scale capture và trả Overlay; nhãn cố định không còn phóng to raster 640×480 |
| `docs/`, `_Hoang/Docs`, `Mota/` | Spec/plan/handoff, nguồn prompt, trạng thái, ngoại lệ user đã duyệt và mẫu phong cách |

## Bằng chứng và cách mở

- Remake: mở `Assets/_Hoang/Scenes/RestaurantSystems.unity`, bấm Play (hoặc menu Hoang > Restaurant Remake > Play remake).
- Lobby gốc: mở Boot gốc rồi Play, không mở bản sao cũ trong `_Hoang/Scenes/Restaurant.unity` để đánh giá task này.
- Báo cáo UI cuối: `D:/bdp-restaurant-art/editmode-final.xml`, `playmode-final.xml` và log cùng tên. Lượt kiểm thêm capture: `playmode-capture-final.xml` và log cùng tên.
- Ảnh remake: `Assets/_Hoang/Validation~/shots/remake-0.png`, `remake-1.png`, `remake-2.png` (thư mục bị Git ignore).
- Ảnh Lobby/Menu/Staff/Reset Việt–Anh: `D:/MakeGame/MyGame/bdp_BunDauPeo/shots/hub-*.png`. Đây là đường thực tế theo `ScreenCapture1080` trên máy này, không phải `D:/bdp_BunDauPeo` ghi trong hướng dẫn cũ.
- Nguồn và prompt: `docs/art/restaurant-art.md` (ImageGen tích hợp).
- Chưa build APK/thử thiết bị/cảm ứng/notch. Chưa đo hiệu năng trên điện thoại. Chưa commit/push artwork; không tự merge main.

## Trạng thái bàn giao

- Các thay đổi đã lưu trong working tree nhánh Hoang; chat sau đọc tài liệu này rồi kiểm `git status`, không chạy lại builder remake chỉ để xem UI.
- Việc tiếp theo nếu user yêu cầu: chơi thử/duyệt giao diện, commit và push đúng nhánh Hoang theo quy trình Git; không tự suy ra yêu cầu merge main từ task artwork.
- Không còn diagnostic log tạm trong code. Ghi chú giới hạn UI cũ trong tài liệu 08/11 chỉ là lịch sử.
