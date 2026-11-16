# Chuẩn bị phát hành CH Play: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (mặc định, native) hoặc superpowers:subagent-driven-development để làm kế hoạch này từng task. Cách thực thi do 🎯 chọn, xem [agent-routing.md](../../guides/agent-routing.md). Các bước dùng checkbox (`- [ ]`).

**Goal:** Có lệnh batch ra AAB ký phát hành (`targetSdk 36`, không Development, kèm symbols, khoá upload ngoài repo), đã kiểm bằng build thật; có checklist deploy `production`, icon, và AAB v1 ký bằng khoá upload thật để user đưa lên thử kín.

**Architecture:** Mở rộng `AndroidBuild` (D-011) với `BuildKind` (`DebugApk` giữ nguyên, `ReleaseAab` mới) và điểm vào `BuildRelease()`. Ký đọc từ biến môi trường, gán trong bộ nhớ rồi trả lại; không ghi bí mật vào repo. Việc trên Play Console, Google Sites, deploy `production` là của user (spec mục 6).

**Tech Stack:** Unity 6000.3.15f1 (Android SDK 36, NDK, OpenJDK đã cài), C#, `BuildPipeline`, `aapt2` + `zipalign` trong SDK build-tools, CLI `unity`. Không thêm package.

**Spec:** [docs/superpowers/specs/2026-11-16-release-prep-design.md](../specs/2026-11-16-release-prep-design.md) (đã duyệt 2026-11-16). Bản nháp chữ: [docs/release/drafts.md](../../release/drafts.md).

## Global Constraints

- Nhánh `feature/release-prep`, cắt từ `origin/main` mới nhất sau khi nhánh `docs/release-prep` đã merge; chưa merge thì cắt từ chính nhánh docs. **Worker không commit, không push** (chỉ 📋 Thư ký). Mỗi task kết thúc bằng test xanh và tick checkbox.
- **Không build, deploy hay chạy gì lên `production`**; việc đó là của user. 🔨 chỉ viết checklist. Không tạo tài khoản, không chi tiền.
- Khoá upload **thật** chỉ do user tạo; mật khẩu không bao giờ vào chat, log, repo, `ProjectSettings/`. Task 1–4 dùng keystore thử tạm ở `D:/bdp_BunDauPeo/keys/test.keystore` (mật khẩu bỏ đi) và **không** dùng nó cho AAB gửi Play.
- Ngoài repo: `D:/bdp_BunDauPeo/android/release/` (bản build), `D:/bdp_BunDauPeo/keys/` (khoá). Không để file `.aab`, `.keystore`, `symbols` trong repo (kiểm `.gitignore`, thêm nếu thiếu).
- Không sửa/xoá tay `.meta`. Không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`, `Không động tới/`. Không sửa tay `ProjectSettings/*.asset`: chỉ qua `ProjectSetup.ApplyBuildSettings` ở batch mode.
- Không đổi nội dung Online đợt 2 (`Rules/`, `ServerModule/`, `Online/`, `PlayerSave`); nếu cần chạm thì dừng và báo 🎯.
- Code theo [code-style.md](../../guides/code-style.md): namespace `BunDauPeo.EditorTools`, comment chỉ nói vì sao. Dòng lỗi và log tiếng Việt, tên code tiếng Anh. Test không dùng `[TestCase]` (L-012); mỗi test trả lời được "cài đặt sai nào vẫn qua test này?" (L-006).
- Sau mỗi task: Console không có lỗi/warning mới; `git status --short` chỉ có file của task. Nếu build làm URP ghi lại hai asset (D-011), `git checkout` hai file đó.

### Lệnh dùng chung

Chạy `unity status` **thành một lệnh riêng** trước mọi lệnh batch (L-020). Lệnh batch cần Editor **đóng**; Editor đang mở thì nhờ user đóng, không kill. Một lúc chỉ một kênh được dùng Unity: 🔨 Online 2 có thể đang chạy, báo trước khi build.

```bash
unity test . --mode EditMode --output ../../bdp_BunDauPeo/tests.xml --timeout 600 -- -logFile ../../bdp_BunDauPeo/tests.log
unity test . --mode PlayMode --output ../../bdp_BunDauPeo/playtests.xml --timeout 900 -- -logFile ../../bdp_BunDauPeo/playtests.log
```

Đọc `tests.xml` (`failed="0"`, `total="N"`). Số nền: kiểm lúc bắt đầu (lúc viết: **938 EditMode + 41 PlayMode**). Dự kiến: Task 1 +3, Task 2 +3, Task 5 +1, Task 6 +1 → **946 EditMode, 41 PlayMode** (nếu nền khác thì cộng theo nền).

## Review Focus

1. **Bí mật lọt vào file:** mật khẩu/đường keystore xuất hiện trong `ProjectSettings.asset`, log, hay `git status`. Bắt bởi test "không có mật khẩu" (Task 2) và `git diff --stat` sau build (Task 3); 🔍 đọc phần `finally` trả lại cài đặt.
2. **`Problems` kiểu `DebugApk` bị siết nhầm:** APK debug của D-011 không được đòi khoá hay AAB. Bắt bởi test ở Task 1.
3. **`BuildRelease` gọi từ Editor đang mở** không được tắt Editor của user (chỉ `Application.isBatchMode` mới `Exit`), như `Build()` hiện có.
4. **Bản phát hành chọn nhầm môi trường:** `ServerChoice` phải ra `production` khi không Development; test hiện có giữ nguyên, 🔍 xác nhận không có đường nào ép `development` trong bản phát hành.
5. **Luồng chỉ có trong bản DEV:** Reset save/Xoá tài khoản phải còn bằng nút Menu ở bản phát hành.
6. **Tương thích module:** không sửa gì ở `ServerModule/`/`Rules/` trong kế hoạch này.

## File Structure

| File | Việc |
|---|---|
| `Assets/_Project/Editor/AndroidBuild.cs` (sửa) | `BuildKind`, `Problems(config, kind)`, `VersionCode`, `TargetSdk`, `BuildRelease()`, tên file AAB |
| `Assets/_Project/Editor/AndroidSigning.cs` (**mới**) | đọc 4 biến môi trường, kiểm, gán và trả lại cài đặt ký |
| `Assets/_Project/Editor/ProjectSetup.cs` (sửa) | ghim target API 36, ghi versionCode, (Task 5) icon |
| `Assets/_Project/Tests/AndroidBuildTests.cs` (sửa), `AndroidSigningTests.cs` (**mới**) | test EditMode |
| `ProjectSettings/ProjectSettings.asset` | đổi bởi `ApplyBuildSettings` ở batch mode, không sửa tay |
| `docs/release/production-deploy.md`, `versions.md` (**mới**) | Task 4, Task 6 |
| `docs/decisions/decisions.md` (D-015), `docs/guides/unity-workflow.md`, `docs/project/tech-stack.md`, `docs/roadmap.md` | Task 4 |
| `.gitignore` | chặn `*.aab`, `*.keystore`, `*.jks` nếu thiếu |

## Chia phiên

**Phiên 🔨 R1 (Task 1–4), sau khi user duyệt spec, xong ≤ 28/11.** Tuần tự, không giao subagent: Task 2 dùng code Task 1, Task 3 dùng cả hai, và chỉ một kênh chạy Unity. Task 3 có hai lần build thật (~10–30 phút mỗi lần): chạy nền, chờ thông báo, không poll. **Điểm dừng:** sau Task 4 dừng, báo user các việc U3 (khoá), U5 (deploy production), U9 (điện thoại).

**Phiên 🔨 R2 (Task 5–6), chỉ bắt đầu khi có đủ: khoá upload thật (U3) và icon PNG của user (U7).** Mục tiêu 1–3/12. Sau đó 🔍 review nhánh ở mức high (không chạm tiền/save/gacha/AFK nên không cần xhigh).

**Phiên lặp R3** (không viết trước): mỗi lần xong một đợt Online (~12/12, ~22/12, ~28/12) 🎯 mở một phiên ngắn theo Task 6 với `versionCode` +1, sau khi user đã deploy module mới lên `production`.

---

## Task 1: Cấu hình phát hành và kiểm theo kiểu build

**Files:** Sửa `AndroidBuild.cs`, `ProjectSetup.cs`, `AndroidBuildTests.cs`. Nền cho Task 2–3.

- [x] **Step 1: Viết test đỏ** (3 test): (a) `Problems_ReleaseKind_ReportsEachFault`: lặp qua các ca sai (target API ≠ 36, `versionCode` 0, không bật ký phát hành, `AppBundle` tắt, Development bật) và đòi đúng một dòng lỗi cho mỗi ca, ghi thông báo theo ca; (b) `Problems_DebugKind_DoesNotDemandRelease`: cấu hình APK debug của D-011 vẫn rỗng lỗi, kể cả không có khoá và `AppBundle` tắt; (c) `CurrentProject_TargetSdk36`: `PlayerSettings.Android.targetSdkVersion == AndroidApiLevel36` sau `ApplyBuildSettings`.
- [x] **Step 2: Chạy `unity test`** (EditMode), thấy ba test đỏ vì chưa có code.
- [x] **Step 3: Cài đặt:** `BuildKind { DebugApk, ReleaseAab }`; `Config` thêm `TargetSdk`, `VersionCode`, `Development`; `Problems(Config, BuildKind)` (bản `Problems(Config)` cũ chuyển thành `DebugApk`, mọi gọi cũ giữ nguyên hành vi); `AndroidBuild.TargetSdk = 36`, `VersionCode = 1`; `ProjectSetup.ApplyPlayerSettings` ghim `targetSdkVersion` và `bundleVersionCode`.
- [x] **Step 4: Ghi cấu hình:** `unity run . -- -executeMethod BunDauPeo.EditorTools.ProjectSetup.ApplyBuildSettings -logFile ../../bdp_BunDauPeo/android/apply.log` (Editor đóng). Kiểm `git diff ProjectSettings/ProjectSettings.asset` chỉ đổi `AndroidTargetSdkVersion`.
- [x] **Step 5: Test xanh.** `unity test` EditMode: 3 test mới + toàn bộ cũ xanh.

## Task 2: Ký bằng biến môi trường và điểm vào `BuildRelease`

**Files:** Mới `AndroidSigning.cs`, `AndroidSigningTests.cs`; sửa `AndroidBuild.cs`, `AndroidBuildTests.cs`, `.gitignore`.

- [x] **Step 1: Viết test đỏ** (3 test): (a) `Signing_FromEnvironment_Cases`: hàm thuần nhận `Func<string,string>` (không đọc môi trường thật) và trả danh sách lỗi: thiếu biến nào thì ghi **tên** biến đó, không bao giờ ghi giá trị; đường keystore không tồn tại; đường nằm **trong thư mục project** bị từ chối; đủ và hợp lệ thì rỗng; (b) `ReleaseOutputPath_UsesVersionCode`: không có `-buildOutput` thì ra `…/android/release/BunDauPeo-<versionCode>.aab`, có thì dùng giá trị đó, `-buildOutput` là phần tử cuối không giá trị không ném `IndexOutOfRange`; (c) `ProjectSettings_HoldNoKeystoreSecrets`: đọc `ProjectSettings/ProjectSettings.asset`, `AndroidKeystoreName` và `AndroidKeyaliasName` trống.
- [x] **Step 2: Chạy test, thấy đỏ.**
- [x] **Step 3: Cài đặt `AndroidSigning`:** `FromEnvironment(Func<string,string>)`, `Apply()` gán vào `PlayerSettings.Android` (`useCustomKeystore`, `keystoreName`, `keystorePass`, `keyaliasName`, `keyaliasPass`) và `Restore()` trả giá trị cũ; `BuildRelease()`: kiểm `Problems(ReleaseAab)` + lỗi ký → dừng mã ≠ 0 trước khi build; `EditorUserBuildSettings.buildAppBundle = đuôi .aab`, `androidCreateSymbols = Public`, `BuildOptions.None`; **`try/finally` gọi `Restore()` và trả `buildAppBundle`**; log `BuildAndroidRelease OK` / `FAILED` (mật khẩu không bao giờ vào log); `EditorApplication.Exit` chỉ khi `Application.isBatchMode`.
- [x] **Step 4: `.gitignore`** thêm `*.aab`, `*.keystore`, `*.jks`, `symbols*.zip` nếu chưa có.
- [x] **Step 5: Test xanh.** EditMode toàn bộ xanh.

## Task 3: Build thật (APK phát hành, AAB) và kiểm

**Files:** Chỉ build ngoài repo, ghi kết quả vào `docs/release/versions.md` (tạo ở Task 6; ở đây ghi vào ghi chú báo cáo).

- [x] **Step 1: Keystore thử tạm** (ngoài repo, mật khẩu bỏ đi, chỉ để thử đường ký):
  `keytool` trong JDK đi kèm Unity: `…/6000.3.15f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool -genkeypair -v -keystore D:/bdp_BunDauPeo/keys/test.keystore -alias test -keyalg RSA -keysize 2048 -validity 365`. Đặt 4 biến `BDP_*` trong phiên shell hiện tại, không ghi ra file.
- [x] **Step 2: Build APK phát hành** (chạy nền): `unity build . --target Android --execute-method BunDauPeo.EditorTools.AndroidBuild.BuildRelease --output-path ../../bdp_BunDauPeo/android/release/BunDauPeo-test.apk --log-file ../../bdp_BunDauPeo/android/release/build-apk.log --timeout 3600`. Đạt: mã thoát 0, log có `BuildAndroidRelease OK`.
- [x] **Step 3: Kiểm APK:** `aapt2 dump badging` (package `com.sweetyuu.bundaupeo`, `targetSdkVersion '36'`, `versionCode '1'`, **không** `debuggable`), `aapt2 dump permissions` (không `AD_ID`, không quyền thừa; ghi danh sách), `zipalign -c -P 16 -v 4` (căn lề 16 KB; không đạt thì báo 🎯, không tự sửa Unity).
- [x] **Step 4: Build AAB** cùng cách, `--output-path ../../bdp_BunDauPeo/android/release/BunDauPeo-test.aab`; kiểm bằng `unzip -l` có `base/lib/arm64-v8a/…` và symbols đi kèm.
- [x] **Step 5: Không bí mật lọt vào:** `git status --short` và `git diff --stat` không có file lạ ngoài URP (D-011); `grep` mật khẩu thử trong `ProjectSettings/` và log trả rỗng. Biến `BDP_*` xoá khỏi phiên.
- [x] **Step 6: Test xanh.** EditMode toàn bộ xanh. (Không có test mới.) Nếu build hỏng ở mã `#if !DEVELOPMENT_BUILD` thì sửa gốc và ghi vào lesson (`#build`).

## Task 4: Quét mã phát hành, checklist production và docs

**Files:** Mới `docs/release/production-deploy.md`; sửa `decisions.md` (D-015, D-011), `unity-workflow.md`, `tech-stack.md`, `roadmap.md`.

- [x] **Step 1: Quét mã bản phát hành** (đọc, không build): liệt kê mọi `#if … DEVELOPMENT_BUILD` và `UNITY_EDITOR` trong `Assets/_Project`; xác nhận Reset dữ liệu và Xoá tài khoản vẫn đạt được bằng nút Menu ở bản phát hành, `DevOverlay`/`PlaytestKit` không phải đường duy nhất tới tính năng nào. Ghi kết quả vào báo cáo.
- [x] **Step 2: Quét gói:** đọc `Packages/manifest.json`; xác nhận không có Analytics, Ads, Crash reporting; ghi danh sách gói UGS thật vào `production-deploy.md` mục "Dữ liệu thu thập" để user điền Data safety (spec 4.5). Có gói thu dữ liệu ngoài dự kiến thì dừng báo 🎯.
- [x] **Step 3: Viết `docs/release/production-deploy.md`** cho **user** làm, từng bước có thể bấm: (1) bật Authentication ẩn danh, Cloud Save, Cloud Code ở môi trường `production` trên Unity Dashboard; (2) deploy `ServerModule/` lên `production` đúng cách đã dùng cho `development` (copy từ mục Online của `unity-workflow.md`, đổi tên môi trường); (3) tạo khoá upload bằng `keytool` kèm lệnh mẫu và nơi cất `D:/bdp_BunDauPeo/keys/`, cách sao lưu hai nơi, **không** gửi mật khẩu vào chat; (4) smoke APK phát hành trên điện thoại + dọn bằng Xoá tài khoản; (5) kiểm save trên Android (spec 4.9, từng bước); (6) luật "chỉ thêm" khi deploy sau này; (7) mẫu dòng ghi `versions.md`.
- [x] **Step 4: Docs:** D-015 (trạng thái "Proposed", chuyển Accepted khi user duyệt spec); sửa câu cuối D-011; `unity-workflow.md` mục Build (lệnh `BuildRelease`, biến `BDP_*`, nơi để khoá/bản build, kiểm `aapt2`/`zipalign`); `tech-stack.md` (AAB, target 36, Play App Signing); `roadmap.md` (mục Build Android/Closed Test trỏ spec này, dòng Đang làm).
- [ ] **Step 5: Test xanh** (không đổi code ở task này, chạy lại để chắc) và báo user ba việc chờ ở Điểm dừng. **Dừng, kết thúc phiên R1.**

## Task 5: Icon ứng dụng (phiên R2, cần PNG của user)

**Files:** Asset ảnh user gửi (đặt vào `Assets/_Project/Art/AppIcon/`, import Sprite/Default, không nén lỗi), sửa `ProjectSetup.cs`, `AndroidBuildTests.cs`.

- [ ] **Step 1: Test đỏ** `AppIcon_IsSetForAndroid`: icon Android mặc định của `PlayerSettings` không rỗng và không phải icon mặc định của Unity.
- [ ] **Step 2: Cài đặt:** import đúng file user gửi (không vẽ, không thêm ảnh khác); `ProjectSetup.ApplyPlayerSettings` đặt `PlayerSettings.SetIcons(NamedBuildTarget.Android, …)` từ file đó (đủ kích cỡ legacy; adaptive để sau nếu user có lớp riêng).
- [ ] **Step 3: Ghi:** chạy `ApplyBuildSettings` (batch). `git status` chỉ có ảnh + `.meta` do Unity sinh + `ProjectSettings.asset`.
- [ ] **Step 4: Test xanh.**

## Task 6: AAB v1 ký bằng khoá upload thật

**Files:** Mới `docs/release/versions.md`; sửa `AndroidBuild.cs` nếu cần đổi `VersionCode`, `AndroidBuildTests.cs`.

- [ ] **Step 1: Test đỏ** `VersionCode_MatchesPlayerSettings`: `AndroidBuild.VersionCode == PlayerSettings.Android.bundleVersionCode` (hằng và asset không lệch sau `ApplyBuildSettings`).
- [ ] **Step 2: Chuẩn bị:** `VersionCode` cho lần upload này (v1 = 1; bản sau +1); chạy `ApplyBuildSettings`.
- [ ] **Step 3: User đặt 4 biến `BDP_*` bằng khoá thật** trong phiên shell mà 🔨 dùng (🔨 không hỏi, không ghi, không in mật khẩu: nhờ user mở terminal và chạy lệnh build, hoặc user nhập biến vào đúng tab Terminal của họ). Chạy `BuildRelease` ra `D:/bdp_BunDauPeo/android/release/BunDauPeo-<versionCode>.aab`; kiểm như Task 3 Step 3–5. APK phát hành cùng `versionCode` dựng thêm để user smoke.
- [ ] **Step 4: Ghi `docs/release/versions.md`:** bảng (versionCode, ngày, commit, kích thước AAB, bản module `production` đang chạy, kết quả smoke). Đưa user đường dẫn AAB để upload thủ công.
- [ ] **Step 5: Test xanh**, rồi phiên kết thúc; 🎯 mở 🔍 review nhánh (mức high).

---

## Công việc của user (không phải task 🔨)

Danh sách đầy đủ và thứ tự ở spec mục 6 và checklist `docs/release/production-deploy.md` (Task 4). Tóm tắt "Chờ user": duyệt spec; email hỗ trợ công khai; tạo tài khoản Play Console (trước 30/11); tạo khoá upload; 2 trang Google Sites; deploy module `production`; gom 15–20 người thử; icon PNG, feature graphic, ảnh chụp, mô tả store; điền Play Console; smoke điện thoại + kiểm save; upload AAB và theo dõi 14 ngày; xác nhận đối tượng 13+.
