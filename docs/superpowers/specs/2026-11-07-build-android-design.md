# Spec: Build Android thử sớm

> Ngày: 2026-11-07 · Trạng thái: **đã duyệt** (user, 2026-11-07, cả thiết kế lẫn bản viết) · Phạm vi: một class Editor `AndroidBuild` (kiểm cấu hình và build APK), mở rộng `ProjectSetup.ApplyPlayerSettings` và thứ tự scene, vài test EditMode, một lần build thật.
> Nguồn: [roadmap](../../roadmap.md) (dòng "Build Android thử sớm"), [D-003](../../decisions/decisions.md) (màn ngang), [D-007](../../decisions/decisions.md) (Android trước), [unity-workflow.md](../../guides/unity-workflow.md). Kế hoạch: [plans/2026-11-07-build-android.md](../plans/2026-11-07-build-android.md).

## 1. Mục tiêu

Có một lệnh build ra file APK cài thử được. Chạy lại lệnh này sau mỗi feature để bắt sớm lỗi chỉ có trên Android (IL2CPP/AOT, stripping, đường dẫn save, màn ngang, chạm) trước hạn 31/12/2026. Đây **không** phải bản phát hành. Mục "Build Android, test trên máy thật, Closed Test" ở cuối roadmap vẫn giữ nguyên.

Thành công khi:
1. Với Editor đóng, một lệnh `unity build` (mục 5) thoát mã 0, log có dòng `BuildAndroid OK`, và có file APK tại `D:/bdp_BunDauPeo/android/BunDauPeo.apk`, nằm ngoài repo.
2. Cấu hình sai (package, màn dọc, backend, scene…) làm lệnh dừng **trước** khi build, thoát mã khác 0, và log ghi rõ chỗ sai.
3. Test EditMode bắt được cấu hình lệch ngay trong `unity test`, không cần build (build mất hàng chục phút).
4. Lệnh build không ghi gì vào `ProjectSettings/` hay `Assets/`: `git status` sau khi build giống trước khi build.

### Quyết định đã chốt với user (2026-11-07)

| Chủ đề | Quyết định |
|---|---|
| Package name (applicationId) | `com.sweetyuu.bundaupeo`. Lên Google Play rồi thì không đổi được, và trên Android nó quyết định thư mục chứa save (`/Android/data/<package>/files`) |
| Tên app trên máy | Giữ `BunDauPeo` (`productName`). Đổi tên này sẽ đổi thư mục save trong Editor/Windows |
| Icon | Icon mặc định của Unity. Icon thật làm ở mục Closed Test |
| `SampleScene` | Bỏ khỏi danh sách build, không xoá file |
| Hướng làm | Phương án A (mục 4): cấu hình ghi một lần qua `ProjectSetup`; lệnh build chỉ kiểm rồi build |

## 2. Hiện trạng (kiểm trên máy ngày 2026-11-07, chỉ đọc)

| Mục | Kết quả |
|---|---|
| Editor 6000.3.15f1 | Đã cài, module: Android, Android SDK & NDK Tools, OpenJDK (cùng iOS, Web). Kiểm bằng `unity editors --installed --format json` |
| SDK trên đĩa | `…/6000.3.15f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK`: platforms `android-34`, `android-35`, `android-36`; build-tools `36.0.0`; có `platform-tools` (`adb`). NDK và OpenJDK có đủ thư mục |
| Việc user phải cài | **Không có.** |
| CLI | `unity` 1.0.0-beta.15 (có bản beta.17; cập nhật là việc của user, không bắt buộc cho spec này). `unity build` có `--target Android`, `--execute-method`, `--output-path` (chuyển thành `-buildOutput` khi có `--execute-method`), `--log-file`, `--timeout`, `--android-export-type` (mặc định `apk`), `--allow-dirty-build` |
| Màn ngang | Đã đúng: `defaultScreenOrientation: 4` (AutoRotation), chỉ cho xoay sang hai hướng ngang (`ProjectSetup.ApplyPlayerSettings` đặt) |
| Android trong `ProjectSettings.asset` | `scriptingBackend` Android = IL2CPP; `AndroidTargetArchitectures: 2` (ARM64); `AndroidMinSdkVersion: 25`; `AndroidTargetSdkVersion: 0` (tự chọn bản cao nhất đã cài, tức 36); `androidUseCustomKeystore: 0` (ký debug) |
| Package name | **Chưa có cho Android.** `applicationIdentifier` chỉ có `Standalone: com.DefaultCompany.2D-URP` |
| Build Settings | Boot, Gacha, Restaurant, StageSelect, Battle (đúng thứ tự `ProjectSetup.PutScenesInBuildOrder`), rồi `Assets/Scenes/SampleScene.unity`, scene thừa của template |
| Build Profile | Chưa có (`Assets/Settings/Build Profiles` không tồn tại); spec này không tạo |
| Mã phụ thuộc nền tảng | Không có `#if UNITY_ANDROID`. Save dùng `Application.persistentDataPath`. `DevOverlay`, `PlaytestKit` và vài nút DEV bật khi `UNITY_EDITOR \|\| DEVELOPMENT_BUILD` |

## 3. Cấu hình Android cho bản thử

Ghi thành [D-011](../../decisions/decisions.md).

| Mục | Giá trị | Có sẵn / mới | Lý do |
|---|---|---|---|
| applicationId (Android) | `com.sweetyuu.bundaupeo` | **mới** | user chốt |
| Tên app | `BunDauPeo` | có sẵn | user chốt |
| Hướng màn | chỉ ngang (AutoRotation, trái + phải) | có sẵn | D-003 |
| Minimum API | 25 (Android 7.1) | có sẵn | mặc định của template; kiểm là ≥ 25 |
| Target API | tự chọn (cao nhất đã cài, 36) | có sẵn | không ghim số, đỡ phải sửa khi Unity nâng SDK |
| Scripting backend | IL2CPP | có sẵn | bắt lỗi AOT/stripping, là loại lỗi chỉ có trên máy; Google Play bắt buộc 64-bit |
| Kiến trúc | có ARM64 | có sẵn | 64-bit; không thêm ARMv7 để build nhanh hơn |
| Ký | keystore debug của Unity | có sẵn | bản thử; không tạo keystore thật |
| Định dạng | APK, không AAB | có sẵn (`buildAppBundle` tắt) | cài thẳng bằng `adb install` |
| Kiểu build | Development Build | **mới**, chỉ trong `BuildPlayerOptions` | logcat có stack trace; `DevOverlay` có mặt trên máy để chuyển màn, Reset save |
| Icon | mặc định | có sẵn | user chốt |
| versionCode / version | 1 / `1.0` | có sẵn | bản thử không tăng số |

`productName` và `companyName` giữ nguyên, vì chúng quyết định thư mục save trong Editor (ghi chú sẵn có trong `ProjectSetup.ApplyPlayerSettings`).

## 4. Thành phần

Phương án đã chọn (A): cấu hình **ghi một lần** bằng code `ProjectSetup` có sẵn rồi commit. Lệnh build **chỉ kiểm**: lệch thì dừng, không tự sửa.

Hai phương án đã loại:
- (B) Build Profile của Unity 6 cùng `unity build --profile`: không cần code, nhưng cấu hình nằm trong asset, khó test bằng EditMode, và brief yêu cầu một method chạy được bằng `-executeMethod`.
- (C) Ghi Player Settings mỗi lần build: đơn giản hơn, nhưng mỗi lần build làm bẩn `ProjectSettings.asset` và che chỗ cấu hình bị lệch.

### 4.1 `ProjectSetup` (có sẵn, sửa nhỏ)

- `ApplyPlayerSettings()` thêm: `PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidBuild.PackageName)`. Các mục Android khác đã đúng giá trị nên không ghi lại; kiểm ở 4.2.
- `PutScenesInBuildOrder()`: danh sách build chỉ gồm 5 scene của game theo thứ tự hiện có. Scene ngoài `Assets/_Project/Scenes/` bị bỏ khỏi danh sách (hiện chỉ có `SampleScene`), file scene vẫn giữ. Bỏ lối thoát sớm hiện có, vì nó coi danh sách là đúng khi 5 mục đầu đúng, kể cả khi còn mục thừa phía sau.
- Thêm `public static void ApplyBuildSettings()` gồm `PutScenesInBuildOrder()`, `ApplyPlayerSettings()` và `AssetDatabase.SaveAssets()`. `Run()` gọi nó thay cho hai lời gọi riêng. Mục đích: 🔨 chạy được riêng phần này ở batch mode, không phải chạy cả `Setup Project`:
  `unity run . -- -executeMethod BunDauPeo.EditorTools.ProjectSetup.ApplyBuildSettings -logFile ../../bdp_BunDauPeo/android/apply.log`

### 4.2 `AndroidBuild` (mới, `Assets/_Project/Editor/AndroidBuild.cs`, namespace `BunDauPeo.EditorTools`)

```csharp
public static class AndroidBuild
{
    public const string PackageName = "com.sweetyuu.bundaupeo";
    public const int MinSdk = 25;
    public const string DefaultOutput = "../../bdp_BunDauPeo/android/BunDauPeo.apk"; // so với thư mục project

    public sealed class Config            // ảnh chụp cấu hình; test tự dựng bản sai
    {
        public string PackageName;
        public UIOrientation DefaultOrientation;
        public bool Portrait, PortraitUpsideDown, LandscapeLeft, LandscapeRight;
        public ScriptingImplementation Backend;
        public AndroidArchitecture Architectures;
        public int MinSdk;
        public bool CustomKeystore;
        public bool AppBundle;
        public string[] Scenes;           // đường dẫn scene đang bật trong Build Settings, theo thứ tự
    }

    public static Config Current();                    // đọc PlayerSettings, EditorUserBuildSettings, EditorBuildSettings
    public static List<string> Problems(Config c);     // rỗng = build được; mỗi chỗ sai một dòng tiếng Việt
    public static string OutputPath(string[] args);    // giá trị sau "-buildOutput", không có thì DefaultOutput
    public static void Build();                        // điểm vào cho -executeMethod
}
```

`Problems` báo một dòng cho mỗi điều sai dưới đây:

1. `PackageName` khác `com.sweetyuu.bundaupeo`.
2. Cho phép màn dọc: `Portrait` hoặc `PortraitUpsideDown` bật, hoặc hướng mặc định là dọc.
3. Không có hướng ngang nào: AutoRotation mà cả `LandscapeLeft` lẫn `LandscapeRight` đều tắt.
4. `Backend` khác IL2CPP.
5. `Architectures` không có ARM64.
6. `MinSdk` < 25.
7. `CustomKeystore` bật (bản thử phải ký debug).
8. `AppBundle` bật (bản thử là APK).
9. Danh sách scene rỗng.
10. Scene đầu tiên khác `ProjectSetup.BootScenePath`.
11. Có scene nằm ngoài `Assets/_Project/Scenes/`. Mỗi scene một dòng, ghi đường dẫn.
12. Có scene không có file. Mỗi scene một dòng, ghi đường dẫn.

Luồng của `Build()`:

1. `var c = Current(); var p = Problems(c);` Có lỗi thì log `BuildAndroid FAILED: cấu hình sai` kèm từng dòng, rồi thoát mã 1.
2. `path = OutputPath(Environment.GetCommandLineArgs())`, rồi tạo thư mục cha.
3. `BuildPipeline.BuildPlayer` với `scenes = c.Scenes`, `locationPathName = path`, `target = BuildTarget.Android`, `options = BuildOptions.Development`.
4. `report.summary.result != BuildResult.Succeeded` thì log `BuildAndroid FAILED: <result>, <totalErrors> lỗi` và thoát mã 1. Thành công thì log `BuildAndroid OK <đường dẫn tuyệt đối> <MB> MB <phút> phút` và thoát mã 0.
5. Chỉ gọi `EditorApplication.Exit(code)` khi `Application.isBatchMode`. Lỡ gọi `Build()` từ Editor đang mở (qua `eval`) thì chỉ log, không tắt Editor của user.

Không có mục menu. Build trong Editor đang mở sẽ chuyển target ngay trước mặt user, và CLI là đường chính.

**Target của Editor sau khi build:** `unity build --target Android` mở Unity với target Android. Lựa chọn đó lưu trong `Library/`, không trong repo, nên lần sau mở Editor sẽ ở target Android. Spec **không** chuyển lại Windows: đổi target mỗi lần build tốn thời gian import, và D-007 đã chọn Android là nền tảng chính. Lần chuyển đầu tiên phải import lại asset một lần. Test và Play trong Editor vẫn chạy như cũ, vì dự án không có mã `#if UNITY_ANDROID`.

## 5. Cách chạy và cách biết kết quả

Điều kiện: Editor đóng. Kiểm bằng `unity status` chạy **một mình một lệnh**, vì nó thoát mã 12 khi không có Editor (L-020).

Lệnh chính:

```bash
unity build . --target Android --execute-method BunDauPeo.EditorTools.AndroidBuild.Build --output-path ../../bdp_BunDauPeo/android/BunDauPeo.apk --log-file ../../bdp_BunDauPeo/android/build.log --timeout 3600
```

Lệnh dự phòng, khi `unity build` từ chối hoặc chạy sai:

```bash
unity run . --timeout 3600 -- -executeMethod BunDauPeo.EditorTools.AndroidBuild.Build -buildTarget Android -logFile ../../bdp_BunDauPeo/android/build.log
```

Lệnh này dùng `DefaultOutput` vì không truyền `-buildOutput`.

Kết quả:
- **Thành công:** mã thoát 0, `build.log` có dòng `BuildAndroid OK`, và file APK có giờ sửa mới hơn lúc bắt đầu chạy (log cũ trông y như log mới, L-020).
- **Thất bại:** mã khác 0. Với `--execute-method`, CLI lấy mã thoát của Unity, thường ra 6 hoặc 15. Tìm `BuildAndroid FAILED` trong log; nếu không có dòng đó thì lỗi xảy ra trước khi method chạy (compile, license…), tìm `error` trong log.
- Sau mỗi lần build: `git status` phải không có `ProjectSettings/` hay `Assets/`.
- Cài thử (user làm, khi đã nối điện thoại): `adb install -r D:/bdp_BunDauPeo/android/BunDauPeo.apk`. `adb` nằm ở `…/AndroidPlayer/SDK/platform-tools/adb.exe`. Bước này không nằm trong điều kiện xong của kế hoạch.

Lệnh build và lệnh dự phòng ghi vào bảng của [unity-workflow.md](../../guides/unity-workflow.md) (dòng "Build").

## 6. Test (EditMode, `Assets/_Project/Tests/AndroidBuildTests.cs`)

| Test | Kiểm | Cài đặt sai nào bị bắt |
|---|---|---|
| `CurrentProject_HasNoProblems` | `Problems(Current())` rỗng; thông báo lỗi liệt kê các dòng | cấu hình thật lệch (chưa chạy `ApplyBuildSettings`, có người đổi Player Settings, scene mới chưa vào build) |
| `Problems_EachBrokenSetting_ReportsExactlyOne` | Dựng một `Config` hợp lệ bằng tay. Lần lượt làm sai từng mục 1–8 (cả hai nhánh của mục 2, và mục 3), mỗi lần đúng **một** dòng. Lặp trong một method, thông báo ghi tên mục (L-012). Thêm ca hợp lệ ra 0 dòng | `Problems` luôn trả rỗng, bỏ sót một mục, hoặc báo trùng |
| `Problems_BadSceneLists_Reported` | Danh sách rỗng, Boot không đứng đầu, `Assets/Scenes/SampleScene.unity`, đường dẫn không có file: mỗi ca ra đúng số dòng mong đợi và dòng có đường dẫn scene | kiểm scene chỉ nhìn phần tử đầu, hoặc bỏ qua scene thừa phía sau (đúng lỗi lối thoát sớm cũ) |
| `OutputPath_ReadsBuildOutputArg_ElseDefault` | `[..., "-buildOutput", "X.apk"]` ra `X.apk`; không có cờ ra `DefaultOutput`; cờ đứng cuối không có giá trị ra `DefaultOutput` | đọc nhầm phần tử, hoặc ném lỗi khi thiếu giá trị |

Tổng EditMode tăng 4. PlayMode giữ nguyên.

Các test sẵn có đọc Build Settings vẫn phải xanh: `ProjectSetupTests.BootScene_IsFirstInBuild_…`, `GachaSceneTests` (hai mục đầu), `BattleSceneTests.BattleScene_InBuildSettings_AfterRestaurant` (kiểm `>= 5` scene).

Build thật không có test tự động, vì một lần build mất 10–30 phút. Build thật là bước cuối của kế hoạch, do 🔨 chạy một lần và đọc log.

## 7. Gói triển khai

Một phiên 🔨, 4 task, làm tuần tự (cùng chạm `ProjectSetup` và Build Settings, nên không chia subagent song song):

1. `AndroidBuild.Config`, `Problems`, `OutputPath` và ba test thuần (TDD).
2. Sửa `ProjectSetup` (4.1), chạy `ApplyBuildSettings` ở batch, rồi test `CurrentProject_HasNoProblems` xanh. `git diff ProjectSettings/` chỉ có applicationId Android và bớt `SampleScene`.
3. `Build()` và chạy build thật lần đầu. Ghi thời gian build và cỡ APK. Kiểm `git status` sạch sau khi build.
4. Cập nhật docs: `unity-workflow.md` (lệnh build), `tech-stack.md` (nền tảng, cấu hình Android), D-011, roadmap.

## 8. Ngoài phạm vi (cố ý không làm)

- AAB, keystore thật, ký phát hành, tăng versionCode: thuộc mục Closed Test.
- Closed Test, Google Play Console, store listing, icon thật, splash.
- IAP và quảng cáo thưởng (D-007): mỗi việc có spec riêng.
- Cài tự động qua `adb`, chạy test trên máy, chụp màn trên máy.
- Kiểm save trên Android thật (`File.Replace`, lưu khi vào nền): mục roadmap riêng, chờ user nối điện thoại. APK này là đầu vào cho mục đó.
- Build bản không Development: các nhánh `#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)` (ví dụ ở `BattlePausePopup`) chưa được build thử. Mục Closed Test sẽ làm.
- CI ([D-006](../../decisions/decisions.md)), Build Profile, menu Editor cho build, chuyển target về Windows sau khi build.

## 9. Rủi ro và cách kiểm sớm

| Rủi ro | Kiểm ở |
|---|---|
| `unity build` từ chối vì git có thay đổi chưa commit. Help nói rõ điều này với versioning, nhưng chưa rõ khi dùng `--versioning-strategy none` | Task 3. Bị từ chối thì thêm `--allow-dirty-build` (build thử không cần cây sạch) và ghi vào lệnh ở unity-workflow |
| `PlayerSettings` đổi ở batch mode không được ghi xuống `ProjectSettings.asset` | Task 2: `git diff ProjectSettings/ProjectSettings.asset` phải thấy `Android: com.sweetyuu.bundaupeo` |
| Gradle lần đầu cần mạng hoặc chạy rất lâu | Task 3: đặt `--timeout 3600`; hỏng thì đọc log Gradle trong `build.log` rồi báo user, không tự cài gì |
| IL2CPP strip mất kiểu mà `JsonUtility` hoặc Input System cần, nên APK chạy được nhưng mất save hoặc mất chạm | Không kiểm được nếu không có máy. Ghi vào báo cáo để user thử khi cài; hiện lỗi thì thêm `link.xml` ở mục sau |
| Lần đầu chuyển target sang Android làm import lại toàn bộ asset (lâu, Editor sau đó mở ở Android) | Task 3 ghi thời gian; báo user trong báo cáo cuối |
| `unity build` không truyền `-buildOutput` như help ghi | Task 3: dòng `BuildAndroid OK` in đường dẫn tuyệt đối; khác `D:/bdp_BunDauPeo/android/` thì dùng lệnh dự phòng và ghi lesson `#build` |

## 10. Câu hỏi còn mở

Không có.

## 11. Việc của user

- Cài APK lên điện thoại khi muốn thử (mục 5). Không bắt buộc để xong kế hoạch.
- Cập nhật `unity` CLI lên beta.17 (`unity self-update`) nếu muốn. Không bắt buộc.
