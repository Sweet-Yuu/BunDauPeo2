# Quyết định kỹ thuật

> **Đọc khi:** sắp đổi hướng kỹ thuật, hoặc cần biết "tại sao dự án làm thế này".
> **Cập nhật khi:** chốt một lựa chọn có đánh đổi (package, kiến trúc, setting). Thêm mục mới ở **đầu** danh sách; không xoá mục cũ — nếu bị thay thế thì đánh dấu `Superseded by`.

## Mẫu

```
### D-NNN — Tiêu đề (YYYY-MM-DD)
- Bối cảnh:
- Quyết định:
- Lý do / đánh đổi:
- Trạng thái: Accepted | Superseded by D-xxx
```

## Danh sách

### D-011 — Build Android thử: APK debug, IL2CPP ARM64, package `com.sweetyuu.bundaupeo` (2026-11-07)
- Bối cảnh: roadmap có mục "Build Android thử sớm" để bắt lỗi riêng của nền tảng trước hạn 31/12/2026. Spec: [build-android-design](../superpowers/specs/2026-11-07-build-android-design.md).
- Quyết định: APK (không AAB), IL2CPP, ARM64, min API 25, chỉ màn ngang, ký debug, `BuildOptions.Development`, Boot là scene đầu, bỏ `SampleScene` khỏi build. Build chỉ kiểm cấu hình (`AndroidBuild.Problems`), không ghi `ProjectSettings`; cấu hình ghi một lần qua `ProjectSetup.ApplyBuildSettings`. Editor ở lại target Android sau khi build.
- Lý do / đánh đổi: package không đổi được sau khi lên Play; `productName`/`companyName` giữ nguyên vì quyết định thư mục save. IL2CPP build chậm (~7 phút lần đầu); nhánh không Development chưa được build thử.
- Trạng thái: Accepted

### D-010 — Điều khiển Editor bằng MCP for Unity của CoplayDev, gỡ MCP cũ (2026-11-06)
- Bối cảnh: MCP cũ (`com.unity.ai.assistant`, tool `Unity_RunCommand`) rớt cổng mỗi lần compile hoặc vào Play và chỉ chạy được lệnh C#. User yêu cầu đổi sang CoplayDev để có tool riêng cho scene, GameObject, console, camera.
- Quyết định: thêm `com.coplaydev.unity-mcp` từ `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.3.0` (khớp server `mcpforunityserver==10.3.0`), gỡ `com.unity.ai.assistant`. Không package nào khác phụ thuộc nó (kiểm `packages-lock.json`). CLI `unity` (`com.unity.pipeline`) vẫn là công cụ chính.
- Lý do / đánh đổi: tool có kiểu rõ hơn. Đánh đổi: package git cần mạng khi cài; cầu nối phải bật lại sau khi Editor mở; `run_tests` PlayMode của MCP không chạy được ở đây. Nâng bản thì đổi tag và `mcpforunityserver` cùng lúc.
- Trạng thái: Accepted

### D-009 — Công cụ thử tự động: seam save, hai assembly mới, chỉ vòng Combat (2026-11-04)
- Bối cảnh: mỗi lần playtest tay phải nhớ nhiều bẫy (save thật bị ghi, `runInBackground` lọt vào `ProjectSettings`, ảnh 16:9 méo, bấm xuyên popup). Roadmap chốt làm công cụ ngay sau Combat; spec và kế hoạch user duyệt 2026-11-03.
- Quyết định: (1) `GameBootstrap.SavePathOverride` là chỗ duy nhất trong mã game cho phép đổi nơi lưu (null = đường dẫn thật; người đặt phải tự trả null). (2) Hai assembly mới: `BunDauPeo.PlaytestKit` (`Playtest/`, `UiDriver` và `ScreenCapture1080`, không tham chiếu assembly game, `defineConstraints` `UNITY_EDITOR || DEVELOPMENT_BUILD` nên bản phát hành không chứa nó) và `BunDauPeo.PlayModeTests` (tách khỏi `Tests` vì Test Framework chỉ coi assembly Editor-only là EditMode). (3) Công cụ chỉ mượn `PlayerSettings.runInBackground` rồi trả (giá trị gốc nằm trong `SessionState`, trả khi về Edit Mode); không đổi `ProjectSettings`. (4) `BootScenePlayMode` đặt `playModeStartScene = null` khi có `-runTests` (L-023). (5) Smoke test chỉ phủ vòng Combat, không phủ Gacha, Restaurant, AFK, Reset Save Data.
- Ngoại lệ với [D-005](#d-005--reset-save-data-xoá-toàn-bộ-dữ-liệu-đã-lưu-luôn-hỏi-xác-nhận-2026-10-31) (user chốt 2026-11-06): Reset Save Data (menu Editor và panel DEV) **giữ** thư mục `playtest-backup/`. Nó là lưới an toàn của người thử, không phải dữ liệu của người chơi; `Restore Save` sau Reset hồi lại save cũ, đó là chủ ý. Có test `ResetDeleteAll_KeepsPlaytestBackup`.
- Lý do / đánh đổi: seam là cách nhỏ nhất để test không chạm save thật, kiểm bằng băm save thật ở TearDown. Đánh đổi: static `SavePathOverride` sống qua lần Play khi tắt domain reload; `DevOverlay` không có trong test PlayMode nên cú bấm nút DEV chưa được phủ (L-024); smoke test phụ thuộc thứ tự màn trong catalog. Thoát hẳn Play rồi mở lại để kiểm save vẫn là việc của người thử. Chi tiết: [spec](../superpowers/specs/2026-11-03-playtest-tools-design.md).
- Trạng thái: Accepted

### D-008 — Thể lực trừ ngay khi vào trận, tối đa 100, không hoàn (2026-11-03)
- Bối cảnh: spec Combat duyệt 2026-11-02 đặt thể lực tối đa 60, boss 15 và **chỉ trừ khi thắng** (thua, bỏ cuộc, tắt app không mất). Ngày 2026-11-03 user đối chiếu master plan (mục 20.7: stamina 100, hồi 1 mỗi 5 phút kể cả lúc đóng, 10 mỗi màn chiến đấu, "trừ một lần lúc bắt đầu hợp lệ; không hoàn khi thua/rút/tắt; truyện 0") và chốt theo đó. Việc này chưa chạm code: Task 1–2 mới có số mặc định 60 và 15 trong config và asset mẫu, Task 11 trở đi chưa làm.
- Quyết định: tối đa **100**; hồi 1 điểm mỗi 5 phút theo giờ máy, kể cả khi tắt game; mỗi màn đánh tốn **10**, kể cả boss; màn cốt truyện tốn 0. Trừ **một lần ngay khi vào trận hợp lệ** và lưu ngay; thua, bỏ cuộc, tắt app giữa trận đều không hoàn; "Thử lại" là lần vào mới, trừ lại. Không mua bằng Gem (giữ như cũ). Thưởng và mọi luật khác của spec giữ nguyên. Cài đặt: mọi trận thật vào qua `CombatService.EnterStage` (kiểm hết, rồi trừ và lưu một lần; lưu thất bại thì hoàn đúng như cũ và không cho vào); dải DEV "Chiến đấu thử" dùng `StartBattle`, không trừ, không thưởng; `CompleteBattle` chỉ trao thưởng cho trận đã trả thể lực.
- Lý do / đánh đổi: nếu chỉ trừ khi thắng thì người chơi thoát ra khi sắp thua để giữ thể lực, mà trận không khôi phục được nên không có cách chặn khác. Trừ lúc vào thì thể lực là giá một lần thử và khớp master plan. (Chưa quyết, thuộc D-007: có bán hay thưởng thể lực hay không; luật hiện hành là không mua bằng Gem.) Đánh đổi: (1) thua là mất 10 thể lực, đội yếu bị phạt nặng hơn; (2) mọi màn cùng giá nên màn 1-5 luôn là chỗ cày tốt nhất, tối đa khoảng 2.880 Gem và 2.880 Material mỗi ngày, cao hơn ước lượng cũ (khoảng 1.900), cần xem ở playtest 3 (spec mục 9); (3) thêm một lần ghi save lúc vào trận; (4) không lưu được thì không chơi được, thay vì cho chơi mà thể lực có thể quay lại; (5) phải sửa số mặc định đã làm và chuyển asset mẫu cũ (kế hoạch Task 11).
- Trạng thái: Accepted. Nguồn: master plan mục 20.7; [spec Combat](../superpowers/specs/2026-11-02-combat-design.md) mục 4.5 và 4.7.

### D-007 — Android trước, tải miễn phí, có mua trong game và quảng cáo thưởng (2026-11-02)
- Bối cảnh: [D-003](#d-003--offline-mobile-màn-ngang-không-nạp-tiền-thật-2026-10-30) chọn offline và không IAP. User duyệt đổi hướng kiếm tiền khi cập nhật master plan.
- Quyết định: Android là nền tảng đầu tiên; game tải miễn phí; nguồn thu là gói Gem, thẻ tháng, gói tân thủ, gói nhân vật khởi đầu và quảng cáo thưởng. Quán, chiến đấu và gacha vẫn chạy offline; mua và xem quảng cáo cần mạng. Màn ngang và save cục bộ giữ nguyên.
- Lý do / đánh đổi: có doanh thu để duy trì dự án. Theo D-003, phải xét lại `Wallet`, gacha và save khi làm spec: cách trao Gem sau khi mua (chống mất/nhận trùng), Reset Save Data với quyền lợi đã mua, chống sửa save. Chưa quyết, ghi `TODO(user)` ở [game-design.md](../design/game-design.md#kiếm-tiền).
- Trạng thái: Accepted (hướng; chi tiết chờ spec)

### D-006 — Chưa dùng CI chạy test Unity (2026-10-31)
- Bối cảnh: cân nhắc GitHub Actions chạy test EditMode. Image Unity 6000.3.15f1 của game-ci có sẵn, nhưng máy user dùng license Personal kiểu mới (không có `Unity_lic.ulf`), mà kích hoạt thủ công cho Personal đã bị Unity bỏ.
- Quyết định: không làm CI (user chốt). Test chạy trên máy trước mỗi commit/push như hiện nay.
- Lý do / đánh đổi: dự án nhỏ, một người làm; CI cần runner tự host hoặc tài khoản Unity riêng (vùng xám điều khoản). Mất khả năng bắt lỗi "thiếu file chưa commit" trên bản clone sạch. Xét lại khi có nhiều người hoặc nhiều session làm song song; hướng đã tra: runner tự host dùng `unity test`, hoặc `RageAgainstThePixel/activate-unity-license` với tài khoản CI riêng.
- Trạng thái: Accepted

### D-005 — Reset Save Data xoá toàn bộ dữ liệu đã lưu, luôn hỏi xác nhận (2026-10-31)
- Bối cảnh: user muốn đưa game về mặc định bằng một nút thay vì xoá file tay, và nút phải có hiệu lực với **mọi thứ đã lưu**, kể cả dữ liệu các tính năng thêm về sau.
- Quyết định: menu Editor `Tools/BunDauPeo/Reset Save Data...` (`ResetSaveData.Run`) và lõi `GameSession.ResetToDefaults()` cho nút trong game (làm ở UI hub). Chưa xác nhận thì không xoá gì. Reset xoá mọi file `save.json.*` (`.bak`, `.tmp`, `.corrupt`) rồi file chính, ghi đè cả `PlayerSave` bằng `new PlayerSave()`, cấp lại đồ game mới, lưu, rồi báo `Wallet.Changed` và `Roster.Changed`.
- Luật cho code về sau (**bắt buộc**):
  1. Dữ liệu cần lưu chỉ nằm trong `PlayerSave` và đi qua `SaveService`. Không dùng `PlayerPrefs` hay file riêng; nếu bắt buộc thì phải thêm vào đường reset và có test.
  2. Field mới của `PlayerSave` phải có giá trị khởi tạo đúng cho game mới, vì reset ghi đè bằng bản mặc định.
  3. Trạng thái chạy nằm ngoài `PlayerSave` (ví dụ simulation của quán, bộ đếm AFK trong bộ nhớ) phải tự reset khi `ResetToDefaults()` chạy; thêm cách nghe sự kiện reset khi các tính năng đó ra đời.
  4. Đang Play thì phải reset qua session đang chạy, không xoá file ngầm, nếu không `OnApplicationQuit` ghi lại dữ liệu cũ.
- Lý do / đánh đổi: phải xoá `.bak`, nếu không `Load` hồi sinh dữ liệu cũ; xoá luôn `.corrupt` nên mất bản cứu hộ cuối, hộp xác nhận nói rõ điều này. File chính xoá sau cùng để lỗi giữa chừng không để lại `.bak` mà mất file chính; xoá thất bại thì bộ nhớ giữ nguyên.
- Ngoại lệ (user chốt 2026-11-06, [D-009](#d-009--công-cụ-thử-tự-động-seam-save-hai-assembly-mới-chỉ-vòng-combat-2026-11-04)): thư mục `playtest-backup/` của công cụ thử (Editor) không bị reset xoá. Chỉ áp dụng cho thư mục đó, nằm ngoài `PlayerSave`; không mở rộng cho file riêng của tính năng.
- Trạng thái: Accepted

### D-004 — Company `Sweet-Yuu`, product `BunDauPeo` (2026-10-30)
- Bối cảnh: template đặt `DefaultCompany` / `My project (1)`; `Application.persistentDataPath` (nơi lưu `save.json`) suy ra từ hai tên này.
- Quyết định: `companyName = Sweet-Yuu`, `productName = BunDauPeo`. Save nằm ở `%USERPROFILE%\AppData\LocalLow\Sweet-Yuu\BunDauPeo\` trên Windows. Không đổi Bundle Identifier.
- Lý do / đánh đổi: đổi sau khi có save thật thì save cũ bị bỏ lại ở thư mục cũ, nên chốt trước khi có người chơi. `Setup Project` đặt lại hai giá trị này mỗi lần chạy.
- Trạng thái: Accepted

### D-003 — Offline, mobile màn ngang, không nạp tiền thật (2026-10-30)
- Bối cảnh: game gacha thường cần server để chống gian lận; template đang cho phép xoay cả màn dọc.
- Quyết định: chơi offline, save JSON cục bộ, roll gacha trên máy, không IAP. Player Settings chỉ cho phép màn ngang.
- Lý do / đánh đổi: không cần server và backend. Người chơi có thể sửa save hoặc chỉnh đồng hồ để gian lận, chấp nhận được vì không có tiền thật. Nếu thêm IAP phải xét lại `Wallet`, gacha và save.
- Trạng thái: Superseded by D-007 (phần "không IAP" và "offline hoàn toàn"; màn ngang và save cục bộ vẫn giữ)

### D-002 — Folder theo tính năng, asmdef riêng, không event bus (2026-10-30)
- Bối cảnh: game gồm nhiều hệ thống làm lần lượt, cần giữ chúng không dính nhau.
- Quyết định: mỗi tính năng một folder và một assembly, chỉ phụ thuộc `Core` và `Characters`. Dùng `event` C# trên service, `ServiceLocator` cho view, không DI container, không event bus chung.
- Lý do / đánh đổi: compiler chặn phụ thuộc chéo. Sau khi đặt `StageDefinition` vào Combat và `UpgradeDefinition` vào Restaurant, không tính năng nào cần nghe tính năng ngang hàng, nên event bus là thừa. Thêm event bus khi xuất hiện nhu cầu thật. Chi tiết: [spec](../superpowers/specs/2026-10-30-foundation-architecture-design.md).
- Trạng thái: Accepted

### D-001 — Dùng Unity 6 + URP 2D + Input System (2026-10-30)
- Bối cảnh: khởi tạo dự án từ template 2D (URP).
- Quyết định: giữ Unity 6000.3.15f1, URP Renderer2D, Input System mới.
- Lý do / đánh đổi: là mặc định của template; không có lý do đổi khi chưa có code.
- Trạng thái: Accepted
