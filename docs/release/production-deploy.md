# Checklist deploy `production` và phát hành (việc của user)

Spec: [release-prep-design](../superpowers/specs/2026-11-16-release-prep-design.md), mục 6. Kế hoạch: [release-prep](../superpowers/plans/2026-11-16-release-prep.md). 🔨 không làm các bước này; chỉ user làm.

Ba việc chờ user (điểm dừng R1): **U3** tạo khoá upload (mục 3), **U5** deploy `production` (mục 1–2), **U9** thử trên điện thoại (mục 4–5).

## 1. Bật dịch vụ ở môi trường `production`

Unity Dashboard → project BunDauPeo → chọn môi trường `production` (không phải `development`):

- [ ] Authentication: bật đăng nhập **ẩn danh** (Anonymous).
- [ ] Cloud Save: bật.
- [ ] Cloud Code: bật.

## 2. Deploy module lên `production`

Cùng cách đã dùng cho `development` (mục Online của [unity-workflow](../guides/unity-workflow.md)), chỉ đổi tên môi trường:

- [ ] `dotnet test ServerModule/BunDauPeoServer.sln` phải xanh.
- [ ] Mở Editor → Project Settings → Services, đặt môi trường là `production` (chỉ để deploy; xong đặt lại `development`, D-013).
- [ ] Mở Deployment Window, chọn `Assets/_Project/Online/Cloud/BunDauPeoServer.ccmr`, Deploy.
- [ ] Kiểm trên Dashboard: module `BunDauPeoServer` có mặt ở `production`.
- [ ] Đặt môi trường Editor về `development`; `git status` không được có file cấu hình môi trường đổi.

## 3. Tạo khoá upload (chỉ user làm, mật khẩu không gửi vào chat)

Lệnh mẫu (chạy trong Terminal của bạn, tự nhập mật khẩu khi được hỏi):

```bash
"C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool" -genkeypair -v -keystore D:/bdp_BunDauPeo/keys/upload.keystore -alias upload -keyalg RSA -keysize 2048 -validity 10000
```

- [ ] Cất ở `D:/bdp_BunDauPeo/keys/` (ngoài repo). **Sao lưu hai nơi** (ví dụ ổ ngoài và kho mật khẩu); mất khoá thì phải xin Google đặt lại khoá upload.
- [ ] Ghi lại bốn thứ cho 🔨 R2: đường file, mật khẩu keystore, alias, mật khẩu khoá. Đặt thành biến môi trường trong Terminal của bạn khi build, không gửi cho Claude:
  `BDP_KEYSTORE_PATH`, `BDP_KEYSTORE_PASS`, `BDP_KEY_ALIAS`, `BDP_KEY_PASS`.
- [ ] Bật Play App Signing khi tạo app trên Play Console; khoá này chỉ là khoá **upload**.

## 4. Smoke APK phát hành trên điện thoại (U9)

Bản APK phát hành (cùng `versionCode` với AAB) do 🔨 R2 dựng ra `D:/bdp_BunDauPeo/android/release/`.

- [ ] Cài APK, mở game, chạm Tiêu đề: vào được Lobby (đăng nhập ẩn danh ở `production`).
- [ ] Chơi một vòng ngắn: quán, một trận, gacha một lần; không thấy nút DEV.
- [ ] Menu → Reset dữ liệu: hỏi xác nhận, xoá hết, về Lobby như game mới.
- [ ] Menu → Xoá tài khoản: hỏi xác nhận, về Tiêu đề; chạm lại thì thành người chơi mới. Dùng bước này để dọn dữ liệu thử ở `production`.

## 5. Kiểm save trên Android thật (spec 4.9)

- [ ] Chơi đến khi có tiến độ (vài nhân vật, tiền khác ban đầu), đưa app vào nền, đợi 1 phút, mở lại: tiến độ còn.
- [ ] Vuốt tắt hẳn app, mở lại: tiến độ còn.
- [ ] Gỡ cài đặt, cài lại, đăng nhập: tiến độ không còn (tài khoản ẩn danh mới); đây là hành vi đúng.
- [ ] Bật chế độ máy bay giữa chừng: hộp mất kết nối hiện, Thử lại hoạt động khi có mạng.

## 6. Luật "chỉ thêm" khi deploy sau này

Bản game đã phát hành vẫn gọi module `production`. Mỗi lần deploy module mới:

- Chỉ **thêm** endpoint, trường dữ liệu (có giá trị mặc định); không đổi tên, không xoá, không đổi nghĩa trường cũ.
- Bản game cũ phải chạy đúng với module mới (kiểm bằng APK phát hành hiện có trước khi deploy).
- Deploy module **trước**, rồi mới upload bản game cần nó.

## 7. Mẫu dòng ghi `docs/release/versions.md`

```
| versionCode | ngày | commit | AAB (MB) | module production | smoke |
| 1 | 2026-12-03 | abc1234 | 00.0 | 2026-12-02 | đạt |
```

## Dữ liệu thu thập (để điền Data safety)

Gói UGS thật trong `Packages/manifest.json` (kiểm 2026-11-16, không có gói Analytics, Ads, Crash reporting nào):

| Gói | Phiên bản | Ghi chú |
|---|---|---|
| `com.unity.services.core` | 1.19.0 | khởi tạo UGS, gắn Project ID và môi trường |
| `com.unity.services.authentication` | 3.8.1 | đăng nhập ẩn danh, sinh Player ID |
| `com.unity.services.cloudsave` | 3.4.2 | lưu save của người chơi trên server |
| `com.unity.services.cloudcode` | 2.10.4 | gọi module server |
| `com.unity.services.deployment` | 1.7.2 | công cụ Editor, không vào bản build |

Module Unity `com.unity.modules.unityanalytics` có trong manifest mặc định của Unity; code game không gọi. User kiểm lại trên Data safety xem UGS Core có gửi chẩn đoán/ID thiết bị theo cấu hình dịch vụ không trước khi khai "không thu thập".

Dữ liệu người dùng: Player ID ẩn danh và save game (UGS, lưu trên máy chủ Unity). Không có email, vị trí, danh bạ, quảng cáo, thanh toán.
