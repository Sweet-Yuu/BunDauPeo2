# Spec: Chuẩn bị phát hành CH Play (AAB, ký, chính sách, thử kín, production UGS)

> Ngày: 2026-11-16 · Trạng thái: **đã duyệt** (user, 2026-11-16, trong kênh 📐 này, kèm mốc: itch.io giữa T12, thử kín Play ~12–13/12, CH Play công khai đầu T1/2027) · Phạm vi: build bản phát hành Android (AAB ký), chuẩn bị Play Console, trang chính sách và xoá tài khoản qua web, deploy module Cloud Code lên `production`, lịch ngược từ ngày công khai CH Play (đầu tháng 1/2027, user chốt 2026-11-16). Không đổi luật chơi, không đổi nội dung Online đợt 2–4.
> Nguồn: [roadmap](../../roadmap.md), [spec Online](2026-11-13-online-design.md) (mục 5.8, 9, 12), [spec Build Android thử](2026-11-07-build-android-design.md), [D-005](../../decisions/decisions.md), [D-011](../../decisions/decisions.md), [D-013](../../decisions/decisions.md), [D-014](../../decisions/decisions.md), [unity-workflow](../../guides/unity-workflow.md). Kế hoạch: [plans/2026-11-16-release-prep.md](../plans/2026-11-16-release-prep.md). Bản nháp chữ cho user dán lên Google Sites và Play Console: [docs/release/drafts.md](../../release/drafts.md).
> Các luật của Google Play ghi ở đây là hiểu biết của 📐 tại ngày viết; **user đối chiếu lại trên Play Console khi điền**, vì Google đổi luật thường xuyên (liên kết ở mục 10).

## 1. Mục tiêu

Có một bản **AAB ký phát hành** lên Google Play, đi qua thử kín (12 người × 14 ngày liên tục) rồi công khai **đầu tháng 1/2027**, trên môi trường UGS `production` đã có module. Bản APK debug của [D-011](../../decisions/decisions.md) vẫn giữ làm công cụ thử nội bộ (môi trường `development`).

Thành công khi:
1. Một lệnh batch ra `BunDauPeo-<versionCode>.aab` ký bằng khoá upload, `targetSdk 36`, không Development, kèm symbols; lệnh dừng trước khi build nếu cấu hình hay biến môi trường ký lệch; **không bí mật nào nằm trong repo hay `ProjectSettings/`**.
2. Môi trường `production` có module Cloud Code và bản phát hành gọi được nó (người chơi qua Tiêu đề vào được Lobby).
3. Có URL công khai: Chính sách quyền riêng tư và trang Xoá tài khoản (qua email); hai trang khớp với nút Xoá tài khoản đã có trong game.
4. Play Console điền đủ để đẩy bản lên thử kín: Data safety, xếp hạng nội dung, đối tượng, store listing.
5. Vòng thử kín trên Play bắt đầu **~12–13/12 (mục tiêu), hạn cứng 15/12** (mục 5), sớm hơn itch.io khoảng 2–3 ngày.

## 2. Quyết định đã chốt với user (2026-11-16)

| Chủ đề | Quyết định |
|---|---|
| Thứ tự phát hành | **itch.io (WebGL) trước**, CH Play không cần đẩy quá sớm; user cố **mở tài khoản Play Console trước khi sang tháng 12** |
| Mốc (user chốt 2026-11-16, sửa lại sau bản đầu) | Giữa tháng 12 = itch.io; **thử kín Play bắt đầu sớm hơn itch.io 2–3 ngày (~12–13/12)**; **công khai CH Play đầu tháng 1/2027**. Mốc 31/12 không còn là hạn của CH Play |
| Loại tài khoản | Giả định **cá nhân** (25 USD, bắt buộc thử kín 12 × 14). User nói khác thì báo 📐 để đổi mục 5 |
| Hosting trang chính sách | **Google Sites** (user tự dán nội dung từ `docs/release/drafts.md`) |
| Xoá tài khoản qua web | **Email hỗ trợ** công khai trên trang; người chơi gửi ID (hiện ở Hồ sơ); user xoá tay trên bảng điều khiển UGS trong 30 ngày |
| Deploy Cloud Code lên `production` | **User tự chạy theo checklist** do 🔨 viết; không ai khác chạm `production` |

### Lựa chọn của 📐 (user chưa xét riêng, sửa được)

| Chủ đề | Mặc định | Lý do |
|---|---|---|
| Định dạng bản đưa lên Play | AAB, Play App Signing (bắt buộc với app mới) | Play không nhận APK cho app mới |
| Khoá | Khoá **upload** tự tạo (`keytool`), để ngoài repo; khoá ký chính do Google giữ | mất khoá upload vẫn xin Google đặt lại được |
| Target API | Ghim `36` (D-011 để tự chọn; tự chọn không cho test bắt) | yêu cầu Play, và test bắt được lệch |
| Bản thử kín đầu | Game hiện tại (Online đợt 1: đăng nhập ẩn danh, save server) | Online đợt 2–4 vào bằng bản cập nhật (spec Online mục 12) |
| Đối tượng | **13+** (không vào chính sách Families) | tránh luật dành cho trẻ em; user xác nhận ở mục 10 |
| Quảng cáo / mua trong app | Khai **không có** | game chưa có; khi thêm phải khai lại (mục 9) |
| Ghi bản đã phát hành | Bảng `docs/release/versions.md` (versionCode, ngày, commit, bản module production) | truy vết để không deploy module phá bản đang chơi |

## 3. Hiện trạng (kiểm trong code ngày 2026-11-16, chỉ đọc)

| Mục | Kết quả |
|---|---|
| Build | `AndroidBuild.Build` chỉ ra **APK Development debug** (D-011); không có đường AAB phát hành, không có ký phát hành |
| Package | `com.sweetyuu.bundaupeo`, min API 25, IL2CPP, ARM64, chỉ màn ngang |
| Target API | `AndroidTargetSdkVersion: 0` (tự chọn bản cao nhất đã cài; SDK 36 có sẵn) → chưa ghim |
| versionCode | `AndroidBundleVersionCode: 1`; version `1.0` |
| Ký | `androidUseCustomKeystore: 0` (keystore debug) |
| Môi trường UGS | `ServerChoice`: bản Development → `development`, bản phát hành → `production` (đã đúng, đợt 1). Môi trường `production` **chưa có module** nên bản phát hành kẹt ở Tiêu đề (D-013) |
| Mã chỉ chạy ở bản không Development | các nhánh `#if !(UNITY_EDITOR \|\| DEVELOPMENT_BUILD)` chưa từng được build (D-011 ghi nợ này) |
| Icon, splash | mặc định của Unity |
| Xoá tài khoản | Có trong Menu (Online đợt 1, mục 5.8 spec Online): `ResetPlayer` rồi `DeleteAccountAsync`; đường qua web chưa có |
| Kiểm save trên Android thật | user hoãn từ 2026-11-01; từ Online đợt 1 server là bản chính, bản đệm trên máy chỉ để mở nhanh nên rủi ro `File.Replace` thấp hơn, nhưng vẫn phải kiểm một lần |
| Tài khoản Play Console | chưa có |

## 4. Thiết kế

### 4.1 Build phát hành (việc của 🔨)

- `AndroidBuild` thêm kiểu build `ReleaseAab` cạnh kiểu cũ `DebugApk` (giữ nguyên hành vi D-011). `Problems(config, kind)`:
  - Chung: target API **= 36** (ghim bằng `ProjectSetup.ApplyPlayerSettings`), các luật cũ 1–12 của spec Build Android.
  - `ReleaseAab` thêm: ký bằng khoá upload hợp lệ (mục 4.2), `versionCode` ≥ 1, không Development.
- Điểm vào mới `AndroidBuild.BuildRelease()` (`-executeMethod`): đuôi `-buildOutput` quyết định kiểu: `.aab` → AAB, `.apk` → **APK phát hành** (cùng cấu hình, dùng để `adb install` thử mã không Development và kiểm căn lề 16 KB). Mặc định `D:/bdp_BunDauPeo/android/release/BunDauPeo-<versionCode>.aab`. `BuildOptions.None`, IL2CPP, ARM64, symbols công khai (`androidCreateSymbols`) để đọc crash trên Play Console.
- Môi trường UGS tự là `production` (không cần code mới); log thêm một dòng `UGS environment: production` khi bản phát hành khởi tạo để smoke thấy được (chỉ đổi nếu chưa có dòng log tương đương).
- 16 KB page size (Play bắt buộc với app nhắm Android 15+ từ 1/11/2025): Unity 6000.3 dựng sẵn thư viện căn lề đúng; Task 3 **kiểm bằng `zipalign -P 16`** trên APK phát hành, không giả định.
- `versionCode` là hằng trong code (`AndroidBuild.VersionCode`), ghi vào `PlayerSettings` qua `ApplyBuildSettings`; **mỗi lần upload lên Play tăng 1**, ghi vào `docs/release/versions.md`. `version` giữ `1.0`.
- Nợ mã không Development (D-011): Task 3 build thật hai lần (APK phát hành, AAB) nên compile đủ; 🔍 đọc lại các `#if !DEVELOPMENT_BUILD` (DevOverlay, PlaytestKit, nút DEV) để không có luồng nào chỉ chạy trong bản có DEV (ví dụ Reset save phải còn bằng nút Menu).
- Quyền trong manifest: kiểm AAB không có `AD_ID` và quyền thừa (`aapt2 dump permissions`); game không dùng quảng cáo nên khai "không dùng advertising ID".

### 4.2 Ký

- **Play App Signing.** Khoá upload tạo bằng `keytool` của JDK đi kèm Unity, **user tự chạy** lệnh trong checklist (🔨 không thấy mật khẩu), lưu tại `D:/bdp_BunDauPeo/keys/` (ngoài repo, ngoài thư mục `Library`). Sao lưu file `.keystore` và mật khẩu ở hai nơi (trình quản lý mật khẩu của user + một bản chép ngoại tuyến). Mất khoá upload không mất app (xin Google đặt lại), nhưng mất thời gian.
- Build đọc 4 biến môi trường: `BDP_KEYSTORE_PATH`, `BDP_KEYSTORE_PASS`, `BDP_KEY_ALIAS`, `BDP_KEY_PASS`. `BuildRelease` gán vào `PlayerSettings.Android` **trong bộ nhớ**, build, rồi **trả lại như cũ trong `finally`**; không ghi mật khẩu vào log, `ProjectSettings/` hay file nào. Thiếu biến, file không tồn tại, hoặc file nằm trong thư mục project thì dừng, lỗi chỉ ghi **tên** biến thiếu.
- `androidUseCustomKeystore` giữ `0` trong repo (build debug vẫn ký debug). Có test đọc `ProjectSettings.asset` bảo đảm không có mật khẩu và đường keystore trống.
- Task 3 dùng một **keystore thử tạm** do 🔨 tạo ngoài repo, mật khẩu bỏ đi, chỉ để chứng minh đường ký chạy. **Khoá upload thật chỉ do user tạo** và chỉ dùng cho AAB gửi lên Play (Task 6).

### 4.3 Môi trường UGS `production`

Hiện trống: không có người chơi, không có dữ liệu để phá. Các việc cần làm trên Unity Dashboard (user, theo checklist `docs/release/production-deploy.md` do 🔨 viết ở Task 4):

1. Môi trường `production` có sẵn; bật **Authentication (ẩn danh)**, **Cloud Save**, **Cloud Code** cho môi trường này (cài đặt có thể theo từng môi trường).
2. Deploy module Cloud Code `ServerModule/` lên `production` bằng đúng cách đã deploy lên `development` ở Online đợt 1 (ghi trong [unity-workflow](../../guides/unity-workflow.md#online)), chọn môi trường `production`.
3. Smoke: chạy **APK phát hành** (mục 4.1) trên điện thoại: Tiêu đề → "Đang kết nối…" → Lobby; vào Hồ sơ thấy ID. Dọn: dùng Xoá tài khoản để xoá người chơi thử (production phải sạch trước thử kín).
4. Ghi dòng vào `docs/release/versions.md`: ngày deploy, commit của `ServerModule/`.

**Luật không phá dữ liệu** (có từ lúc người thử đầu tiên vào):
- Lần deploy đầu lên môi trường trống: an toàn.
- Sau đó mỗi lần deploy chỉ **thêm**: không đổi tên, không xoá endpoint, không đổi tên khoá Cloud Save, không đổi nghĩa trường trong `save` khi còn bản app cũ đang chạy. Việc phá tương thích phải có hàng rào bản app (mục 4.8) và người thử đã cập nhật trước.
- Chỉ deploy từ commit đã `dotnet test` xanh và đã vào `main`; hỏng thì deploy lại commit trước (không có "undo" riêng).
- Deploy `production` luôn do user bấm, sau khi `development` đã chạy bản đó đủ một vòng playtest.
- itch.io (WebGL) dùng cùng `production`: việc deploy này mở đường cho cả hai kênh, nên **làm sớm** (mục 5).

### 4.4 Chính sách quyền riêng tư và xoá tài khoản qua web

- Hai trang Google Sites công khai, không cần đăng nhập, URL ổn định: **Chính sách quyền riêng tư** và **Xoá tài khoản** (có thể là hai mục của cùng một site). Nội dung nháp ở [drafts.md](../../release/drafts.md) (tiếng Việt và Anh; chỗ cần user điền: tên người/pháp nhân phát hành, email hỗ trợ).
- Trang Xoá tài khoản phải nêu: tên game và nhà phát hành (khớp Play Console); **cách 1** xoá trong game (Menu → Xoá tài khoản); **cách 2** gửi email kèm ID người chơi (Hồ sơ → ID), xoá trong 30 ngày; **dữ liệu bị xoá** (save, hồ sơ, ID đăng nhập) và dữ liệu giữ lại nếu có (hiện: không).
- Giới hạn đã biết: tài khoản ẩn danh mất khi gỡ app (token ở máy) thì không còn cách chứng minh quyền sở hữu ID; trang nói rõ "cần ID trong Hồ sơ; không có ID thì không xác định được tài khoản". Chấp nhận cho bản đầu; làm tốt hơn khi có liên kết Google (đợt 3).
- URL điền vào hai chỗ Play Console: ô **Privacy policy** và ô **Xoá dữ liệu / tài khoản** trong mục Data safety.

### 4.5 Data safety (bản nháp trả lời, user kiểm lại trên Console)

| Loại dữ liệu | Thu thập | Chia sẻ | Bắt buộc | Mục đích |
|---|---|---|---|---|
| Personal info → **User IDs** (Player ID của UGS Authentication) | Có | Không (Unity là bên xử lý thay mặt, không tính là chia sẻ) | Có | App functionality, Account management |
| Personal info → **Name** (tên hiển thị tối đa 12 ký tự ở Hồ sơ, biệt danh) | Có, nếu người chơi đặt | Không | Không | App functionality |
| App activity → **In-app actions / other** (tiến độ game: save, nhân vật, Gem) | Có | Không | Có | App functionality |
| Device or other IDs, Location, Financial, Contacts, Photos, Audio, Advertising ID | **Không** | | | |
| Mã hoá khi truyền | Có (HTTPS của UGS) | | | |
| Cho người dùng yêu cầu xoá | Có: nút trong game **và** URL web (mục 4.4) | | | |

Việc phải kiểm trước khi nộp (Task 4 liệt kê gói trong `manifest.json`): game **không** có gói Analytics/Ads/Crash reporting; Unity runtime thu gì thì theo hướng dẫn "Data safety" của Unity (user/📐 đọc ở thời điểm điền). Có gói thu dữ liệu thì bảng này và chính sách phải sửa theo.

### 4.6 Xếp hạng nội dung, đối tượng và các khai báo khác

| Mục | Câu trả lời đề xuất |
|---|---|
| Xếp hạng nội dung (IARC) | Danh mục "Game". Bạo lực: hoạt hình/giả tưởng, đánh theo lượt với tôm (mức nhẹ). Không máu me, không cờ bạc thật, không ngôn ngữ thô, không nội dung người lớn. Người chơi **không** tương tác/nhắn tin với nhau; không chia sẻ vị trí. Mua trong app: **không** (D-007). Gacha dùng Gem kiếm trong game: trả lời đúng sự thật là "không tốn tiền thật". Kết quả dự kiến: 7+/PEGI 7 |
| Đối tượng | 13+ (mục 2); không chọn nhóm dưới 13 |
| Quảng cáo | Không |
| App access | Không cần tài khoản hay thông tin đăng nhập (đăng nhập ẩn danh tự động) |
| Ứng dụng chính phủ, tin tức, sức khoẻ, tài chính | Không |
| Danh mục / thẻ | Game → (Casual / Role Playing / Simulation; user chọn) |

### 4.7 Store listing

Cần (user cung cấp, 🔨 không tự thêm ảnh vào repo trừ file user gửi): tên (30 ký tự), mô tả ngắn (80), mô tả đầy đủ (4000), **icon 512×512** PNG, **feature graphic 1024×500**, **ít nhất 2 ảnh chụp** (màn ngang, chụp từ bản phát hành; bản nháp chữ ở drafts.md, ảnh chụp lấy từ điện thoại hoặc `ScreenCapture1080` trên build thử, loại bỏ DevOverlay). Ngôn ngữ mặc định tiếng Anh; thêm tiếng Việt cho store.

Icon trong game: Task 5 dùng đúng file PNG user gửi (đặt icon mặc định + legacy; không tự vẽ icon).

### 4.8 Thử kín và tương thích bản

- Track: **Closed testing**. Cần ≥ 12 người đã opt-in liên tục 14 ngày. Mời **15–20** người (có người bỏ giữa chừng thì còn đủ), theo email Google hoặc một Google Group; đây là việc của user. Mỗi bản cập nhật qua thử kín không làm mất đồng hồ 14 ngày.
- Lịch 14 ngày tính **từ ngày đủ 12 người opt-in**, nên bản upload phải qua duyệt của Google (thường 1–3 ngày, có thể tới 7 với app mới) trước đó.
- Bản cập nhật khi module `production` đổi (đợt 2 ~12/12, đợt 3 ~22/12, đợt 4 ~28/12; đợt 4 có thể vào thẳng bản production đầu nếu kịp trước khi nộp bản): trước khi upload bản mới, user deploy module mới lên `production` theo luật 4.3. Người thử đang ở bản cũ không bị buộc cập nhật ngay, nên **module mới phải tương thích bản cũ** (chỉ thêm) cho tới khi hết người dùng bản đó.
- **Hàng rào bản cũ** (đề nghị cho 🎯, không nằm trong kế hoạch này vì chạm Online đợt 2): nếu đợt 2 buộc phá tương thích (Gem do server tính khác hẳn) thì cần mã lỗi `ClientTooOld` từ module và hộp "Cập nhật" mở Play Store ở client. Đây là điều kiện để 📐 Online 2 và 🎯 chốt; tôi chỉ đặt ra, không tự làm.

### 4.9 Kiểm save trên Android thật

Chạy một lần khi user nối điện thoại (đã hoãn từ 2026-11-01): cài APK phát hành; chơi → vào nền → Lobby còn nguyên; đóng hẳn app, mở lại → tiến độ khớp; tắt mạng giữa chừng → hộp mạng (Thử lại); Xoá tài khoản chạy được trên máy thật. Kịch bản từng bước ở Task 3 của kế hoạch; kết quả ghi vào `docs/release/versions.md`.

## 5. Lịch ngược từ công khai CH Play đầu tháng 1/2027

Điều kiện Play (cá nhân): 14 ngày liên tục ≥ 12 người → nộp đơn "Production access" → Google phản hồi (thường 1–3 ngày, tối đa ~7) → tạo bản production → duyệt bản (vài giờ tới vài ngày; app mới có thể lâu). S = ngày đủ 12 người opt-in.

| Mốc | Việc | Ai |
|---|---|---|
| **~2–7/1/2027** | Công khai CH Play (rollout 100%) | |
| ~30/12–3/1 | Production access được cấp, nộp bản production (có thể đã gồm đợt 4) | user + 🔨 R3 |
| **S + 14 ≈ 26–27/12** | Nộp đơn Production access | user |
| **S ≈ 12–13/12 (mục tiêu), 15/12 (hạn cứng)** | Đủ 12 người opt-in, đồng hồ 14 ngày chạy; itch.io mở sau đó 2–3 ngày (~15/12) | user + người thử |
| ≤ 7/12 | Upload AAB v1 lên Closed testing, nộp duyệt (1–5 ngày); mời người thử | user |
| ≤ 6/12 | Store listing, Data safety, rating, URL chính sách điền xong | user |
| 3–5/12 | Smoke APK phát hành trên điện thoại + kiểm save (4.9) | user + 🔨 |
| 1–3/12 | 🔨 R2: icon, AAB v1 ký bằng khoá upload thật | 🔨 |
| ≤ 1/12 | Khoá upload tạo xong; deploy module lên `production` (cần cho smoke và itch.io); 2 trang Google Sites lên | user |
| ≤ 28/11 | 🔨 R1: build phát hành, ký, checklist deploy | 🔨 |
| **≤ 30/11 (user đã hẹn "trước tháng 12"; sớm hơn thì dự phòng xác minh tốt hơn)** | Tạo tài khoản Play Console, qua xác minh danh tính và điện thoại | user |
| 16–20/11 | User duyệt spec, đưa email hỗ trợ, bắt đầu gom người thử | user |

**Nhận định:** lịch này **khả thi và có dự phòng ~1 tuần** (hết thử kín ~27/12, còn ~1 tuần cho hai lần duyệt). Không còn dự phòng nếu S > 15/12: khi đó hai lần duyệt dồn vào ≤ 7 ngày và công khai trượt sang giữa tháng 1. Hai việc quyết định S: tài khoản Play xong (xác minh có thể mất vài ngày) và **đủ 12 người opt-in sớm** (mời 15–20 người).

**Cắt được khi trễ (theo thứ tự):** 1) bản production đầu chưa có Hộp quà (đợt 4 vào bằng cập nhật); 2) ảnh store/feature graphic làm đơn giản; 3) tiếng Việt cho store; 4) lùi công khai CH Play sang giữa tháng 1 (itch.io không bị ảnh hưởng). **Không cắt:** AAB ký, target 36, chính sách + xoá tài khoản (cả trong game lẫn web), Data safety, thử kín 14 ngày, deploy `production`.

## 6. Phân việc và phụ thuộc

| Việc | Ai | Cần có trước |
|---|---|---|
| U1 duyệt spec; đưa email hỗ trợ công khai | user | |
| U2 tạo Play Console (25 USD), xác minh | user | |
| U3 tạo khoá upload (`keytool`), cất + sao lưu | user | checklist (Task 4) |
| U4 dựng 2 trang Google Sites từ drafts.md | user | U1 (email) |
| U5 deploy module lên `production`, ghi `versions.md` | user | Task 4 (checklist), Online đợt 1 đã trên `main` ✓ |
| U6 gom 15–20 người thử (email Google) | user | |
| U7 gửi icon PNG, feature graphic, ảnh chụp, xác nhận tên/mô tả store | user | |
| U8 điền Play Console (Data safety, rating, đối tượng, listing) theo mục 4.5–4.7 | user | U2, U4, U7 |
| U9 smoke điện thoại + kiểm save | user | Task 3, U5 |
| U10 upload AAB, mời tester, theo dõi 14 ngày, nộp Production access | user | U2, U8, Task 6 |
| 🔨 R1 Task 1–4: cấu hình phát hành, ký, `BuildRelease`, build thử, quét mã, checklist, docs | 🔨 | spec duyệt |
| 🔨 R2 Task 5–6: icon, AAB v1 ký thật | 🔨 | U3, U7 (icon), R1 |
| 🔨 lặp ở mỗi cập nhật đợt Online | 🔨 | đợt Online xong, U5 (module mới) |
| 🔍 review nhánh ở mức high | 🔍 | R2 |

**Chặn nhau:** U2 → U8 → U10 → đồng hồ thử kín → công khai đầu tháng 1 (đường găng). U5 chặn U9 và cũng chặn itch.io. U3 chặn Task 6. U7 (icon) chặn Task 5. Editor chỉ một kênh một lúc: 🔨 R1/R2 và 🔨 Online 2 phải xếp lịch Unity (mỗi lần build thật ~10–30 phút, đóng Editor).

## 7. Test

- Chạy được không cần mạng, không dùng Play hay UGS thật.
- EditMode dự kiến +8 (nền kiểm lại lúc bắt đầu, hiện ghi 938 EditMode + 41 PlayMode): `Problems` theo `BuildKind` (lỗi thiếu ký, versionCode, target API sai; debug không bị đòi ký), target API ghim 36 trong project hiện tại, `AndroidSigning` đọc biến môi trường (thiếu/đường dẫn trong repo/hợp lệ; lỗi không chứa giá trị), tên file AAB, `ProjectSettings` không chứa mật khẩu/đường keystore, `VersionCode` khớp `PlayerSettings`, icon đã đặt. PlayMode không đổi.
- Kiểm tay (không tự động được): build thật, `aapt2`/`zipalign`, smoke điện thoại, Play Console.

## 8. Docs phải sửa (làm trong kế hoạch)

| File | Sửa |
|---|---|
| `docs/decisions/decisions.md` | **D-015** mới (sau khi user duyệt spec): AAB + Play App Signing, khoá upload ngoài repo, target 36 ghim, quy tắc deploy production, lịch thử kín. **D-011**: ghi "bản phát hành xem D-015"; sửa câu "nhánh không Development chưa build thử" |
| `docs/guides/unity-workflow.md` | mục Build: lệnh `BuildRelease`, biến môi trường ký, nơi để khoá, cách smoke; mục Online: production |
| `docs/project/tech-stack.md` | AAB, targetSdk 36, Play App Signing |
| `docs/release/` (mới) | `drafts.md`, `production-deploy.md` (Task 4), `versions.md` (Task 6) |
| `docs/roadmap.md` | dòng Đang làm; mục "Build Android, test trên máy thật, Closed Test" chuyển sang trạng thái theo kế hoạch này |

## 9. Rủi ro

1. **Tài khoản Play chậm hoặc bị từ chối xác minh** → trễ đồng hồ 14 ngày, đẩy công khai CH Play sang giữa tháng 1. Giảm: tạo tài khoản trước 30/11, sớm hơn càng tốt; có lối lui itch.io.
2. **Google từ chối đơn Production access lần đầu** (thường vì người thử ít hoạt động) → phải chạy lại 14 ngày. Giảm: 15–20 người, nhắc họ mở app đều, ghi phản hồi thật để trả lời đơn.
3. **Mã chưa từng chạy ở bản không Development** có thể lỗi chỉ trên bản phát hành. Giảm: APK phát hành thử trên máy ở Task 3/U9 trước khi gửi AAB.
4. **Lộ hoặc mất khoá upload.** Giảm: ngoài repo, sao lưu hai nơi, test chặn mật khẩu vào `ProjectSettings`; mất thì xin Google đặt lại.
5. **Deploy `production` sai lúc có người thử** phá bản đang chơi. Giảm: luật "chỉ thêm" mục 4.3, bảng `versions.md`.
6. **Lệch Data safety/chính sách so với dữ liệu thật** có thể bị Google gỡ app. Giảm: Task 4 liệt kê gói, đối chiếu khi Online đợt 3 thêm Google Play Games (khai thêm Google account ID, cập nhật chính sách).
7. **Unity runtime hay gói thu thập dữ liệu** chưa liệt kê. Giảm: Task 4 kiểm; user đọc hướng dẫn Data safety của Unity khi điền.
8. **Hai kênh cạnh tranh Unity** (R1/R2 với Online 2 🔨). Giảm: xếp cửa sổ build, mỗi lần build báo trước.
9. **Chặn của người thử:** người chơi phải có mạng; thử kín ở nơi mạng yếu có thể ít chơi. Chấp nhận.
10. **Sau bản công khai đầu tiên:** thêm nạp tiền hay quảng cáo thì phải khai lại Data safety, quảng cáo, công bố tỉ lệ gacha (luật loot box của Play), cập nhật chính sách.

## 10. Việc ngoài spec và liên kết

- Itch.io/WebGL: việc riêng (spec Online mục 12). Chung `production`.
- Online đợt 3 (Google Play Games): thêm OAuth client và khai dữ liệu mới; khi đó sửa chính sách.
- Hàng rào bản cũ `ClientTooOld`: đề nghị cho 🎯 và 📐 Online 2 (mục 4.8).
- Liên kết đối chiếu: [Target API](https://support.google.com/googleplay/android-developer/answer/11926878) · [Xoá tài khoản](https://support.google.com/googleplay/android-developer/answer/13327111) · [Thử kín](https://support.google.com/googleplay/android-developer/answer/14151465) · [Data safety](https://support.google.com/googleplay/android-developer/answer/10787469) · [Xếp hạng nội dung](https://support.google.com/googleplay/android-developer/answer/9859655).
- `TODO(user)`: xác nhận đối tượng 13+; xem chính sách có cần theo Nghị định 13/2023/NĐ-CP (bảo vệ dữ liệu cá nhân, Việt Nam) cho người phát hành cá nhân; đây không phải tư vấn pháp lý.
