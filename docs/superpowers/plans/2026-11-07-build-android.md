# Build Android thử sớm: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (mặc định, native) hoặc superpowers:subagent-driven-development để làm kế hoạch này từng task. Cách thực thi do 🎯 chọn, xem [agent-routing.md](../../guides/agent-routing.md). Các bước dùng checkbox (`- [ ]`).

**Goal:** Có lệnh `unity build` ra file APK cài thử (`D:/bdp_BunDauPeo/android/BunDauPeo.apk`). Lệnh dừng trước khi build nếu cấu hình Android lệch, và có test EditMode bắt chỗ lệch đó.

**Architecture:** Class Editor mới `AndroidBuild` gồm ba phần: `Config` (ảnh chụp cấu hình), `Problems` (hàm kiểm thuần) và `Build` (điểm vào cho `-executeMethod`). Cấu hình chỉ ghi một lần, qua `ProjectSetup.ApplyBuildSettings()` (mới, gom `PutScenesInBuildOrder` và `ApplyPlayerSettings` có sẵn), rồi commit. Lệnh build chỉ kiểm, không ghi `ProjectSettings`.

**Tech Stack:** Unity 6000.3.15f1 (module Android, SDK 34–36, NDK, OpenJDK đã cài), C#, `UnityEditor.Build` (`NamedBuildTarget`), `BuildPipeline`, Test Framework, CLI `unity` 1.0.0-beta.15. Không thêm package.

**Spec:** [docs/superpowers/specs/2026-11-07-build-android-design.md](../specs/2026-11-07-build-android-design.md). User duyệt thiết kế và bản viết ngày 2026-11-07. Số mục kiểm 1–12 bên dưới là danh sách ở spec mục 4.2.

## Global Constraints

- Package name: `com.sweetyuu.bundaupeo`. Tên app: giữ `BunDauPeo`. **Không đổi `productName`, `companyName`**, vì chúng quyết định thư mục save trong Editor. Icon giữ mặc định.
- Android: chỉ màn ngang, minimum API ≥ 25, IL2CPP, có ARM64, ký debug (không custom keystore), APK (không AAB). `BuildOptions.Development`.
- APK và log chỉ để trong `../../bdp_BunDauPeo/android/` (tức `D:/bdp_BunDauPeo/android/`), **không** để trong repo.
- Không sửa/xoá tay `.meta`. Không đụng `Library/`, `Logs/`, `UserSettings/`, `Temp/`, `Không động tới/`. **Không sửa tay** `ProjectSettings/*.asset`: chỉ đổi qua `ProjectSetup.ApplyBuildSettings` chạy ở batch mode.
- **Git: worker không commit, không push** (chỉ 📋 Thư ký, user chốt 2026-11-06). Nhánh `feature/build-android` cắt từ `origin/main` mới nhất sau khi nhánh `docs/build-android-spec` đã merge; chưa merge thì cắt từ chính nhánh docs đó. Mỗi task kết thúc bằng test xanh và tick checkbox.
- Code theo [code-style.md](../../guides/code-style.md): namespace `BunDauPeo.EditorTools`, comment chỉ nói vì sao. Dòng lỗi của `Problems` và dòng log viết tiếng Việt; tên code viết tiếng Anh.
- Test: không dùng `[TestCase]`, lặp trong một method và ghi thông báo theo từng ca (L-012). Mỗi test phải trả lời được "cài đặt sai nào vẫn qua test này?" (L-006).
- Sau mỗi task: Console không có lỗi compile hay warning mới; `git status --short` chỉ có file của task.

### Lệnh dùng chung

Chạy `unity status` **thành một lệnh riêng** trước mọi lệnh batch (L-020). Lệnh batch cần Editor **đóng**; Editor đang mở thì nhờ user đóng, không kill. Lần đầu tạo thư mục: `mkdir -p ../../bdp_BunDauPeo/android`.

```bash
unity test . --mode EditMode --output ../../bdp_BunDauPeo/tests.xml --timeout 600 -- -logFile ../../bdp_BunDauPeo/tests.log
unity test . --mode PlayMode --output ../../bdp_BunDauPeo/playtests.xml --timeout 900 -- -logFile ../../bdp_BunDauPeo/playtests.log
```

Đọc `../../bdp_BunDauPeo/tests.xml` (`failed="0"`, `total="N"`), không dựa mỗi mã thoát (`0` đạt, `8` test đỏ, `6` lỗi khác kể cả compile).

Số kỳ vọng: nền **743 EditMode + 12 PlayMode**. Sau Task 1: 746. Sau Task 2: 747. Hết kế hoạch: **747 EditMode, 12 PlayMode**.

## Review Focus

1. **`SampleScene` quay lại sau khi chạy lại `Setup Project`:** lối thoát sớm cũ của `PutScenesInBuildOrder` coi danh sách là đúng khi 5 mục đầu đúng. Bắt bởi `CurrentProject_HasNoProblems` (Task 2) và ca `SampleScene` ở `Problems_BadSceneLists_Reported` (Task 1).
2. **`-buildOutput` là phần tử cuối, không có giá trị:** phải ra `DefaultOutput`, không ném `IndexOutOfRange`. Bắt bởi `OutputPath_ReadsBuildOutputArg_ElseDefault` (Task 1).
3. **Gọi `Build()` từ Editor đang mở** (qua `unity command eval`): không được tắt Editor của user. `EditorApplication.Exit` chỉ chạy khi `Application.isBatchMode`. Không test tự động được (sẽ tắt Editor test); 🔍 kiểm bằng cách đọc code của Task 3.
4. **Build hỏng giữa chừng (Gradle, IL2CPP):** mã thoát phải khác 0, và log phải có `BuildAndroid FAILED`, không chỉ có lỗi của Unity. Không test tự động được; 🔍 đọc nhánh `report.summary.result` ở Task 3. Nếu lần build thật ở Task 3 hỏng thì đó chính là phép thử.
5. **Danh sách scene rỗng:** chỉ báo một dòng (mục 9), không báo thêm "Boot không đứng đầu", không ném lỗi. Bắt bởi ca rỗng ở `Problems_BadSceneLists_Reported` (Task 1).

## File Structure

| File | Việc |
|---|---|
| `Assets/_Project/Editor/AndroidBuild.cs` (**mới**) | `PackageName`, `MinSdk`, `DefaultOutput`, `Config`, `Current()`, `Problems()`, `OutputPath()`, `Build()` |
| `Assets/_Project/Editor/ProjectSetup.cs` (sửa) | `ApplyPlayerSettings` thêm applicationId Android; `PutScenesInBuildOrder` bỏ scene ngoài `Assets/_Project/Scenes/` và bỏ lối thoát sớm; thêm `ApplyBuildSettings()` |
| `Assets/_Project/Tests/AndroidBuildTests.cs` (**mới**) | 4 test EditMode |
| `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/EditorBuildSettings.asset` | đổi bởi `ApplyBuildSettings` ở batch mode (Task 2), không sửa tay |
| `docs/guides/unity-workflow.md`, `docs/project/tech-stack.md`, `docs/decisions/decisions.md`, `docs/roadmap.md` | Task 4 |

## Chia phiên

**Một phiên 🔨 (Task 1–4)**, làm tuần tự. Không giao subagent song song: Task 2 và 3 dùng code của Task 1, Task 2 đổi Build Settings mà Task 3 đọc, và chỉ một kênh được chạy Unity. Task 3 có một lần build thật, mất khoảng 10–30 phút: chạy nền và chờ thông báo, không poll.

---

### Task 1: `AndroidBuild` — `Config`, `Problems`, `OutputPath`

**Files:**
- Create: `Assets/_Project/Editor/AndroidBuild.cs`
- Test: `Assets/_Project/Tests/AndroidBuildTests.cs`

**Interfaces:**
- Consumes: `ProjectSetup.BootScenePath`, `GachaScenePath`, `RestaurantScenePath`, `StageSelectScenePath`, `BattleScenePath` (public, có sẵn).
- Produces:
  - `public static class AndroidBuild`, namespace `BunDauPeo.EditorTools`, gồm:
    - `public const string PackageName = "com.sweetyuu.bundaupeo";`
    - `public const int MinSdk = 25;`
    - `public const string DefaultOutput = "../../bdp_BunDauPeo/android/BunDauPeo.apk";`
  - `public sealed class AndroidBuild.Config` có các field public: `string PackageName; UIOrientation DefaultOrientation; bool Portrait, PortraitUpsideDown, LandscapeLeft, LandscapeRight; ScriptingImplementation Backend; AndroidArchitecture Architectures; int MinSdk; bool CustomKeystore; bool AppBundle; string[] Scenes;`
  - `public static Config Current()`
  - `public static List<string> Problems(Config c)`
  - `public static string OutputPath(string[] args)`

- [x] **Step 1: Viết 3 test thuần trong `AndroidBuildTests.cs`**

```csharp
private static readonly string[] GameScenes =
{
    ProjectSetup.BootScenePath, ProjectSetup.GachaScenePath, ProjectSetup.RestaurantScenePath,
    ProjectSetup.StageSelectScenePath, ProjectSetup.BattleScenePath,
};

private static AndroidBuild.Config Valid() => new AndroidBuild.Config
{
    PackageName = AndroidBuild.PackageName, DefaultOrientation = UIOrientation.AutoRotation,
    Portrait = false, PortraitUpsideDown = false, LandscapeLeft = true, LandscapeRight = true,
    Backend = ScriptingImplementation.IL2CPP, Architectures = AndroidArchitecture.ARM64,
    MinSdk = 25, CustomKeystore = false, AppBundle = false, Scenes = (string[])GameScenes.Clone(),
};

[Test]
public void Problems_EachBrokenSetting_ReportsExactlyOne()
{
    CollectionAssert.IsEmpty(AndroidBuild.Problems(Valid()), "valid config");
    var cases = new (string Name, Action<AndroidBuild.Config> Break)[]
    {
        ("package", c => c.PackageName = "com.DefaultCompany.2D-URP"),
        ("portrait allowed", c => c.Portrait = true),
        ("portrait upside down allowed", c => c.PortraitUpsideDown = true),
        ("default portrait", c => c.DefaultOrientation = UIOrientation.Portrait),
        ("no landscape", c => { c.LandscapeLeft = false; c.LandscapeRight = false; }),
        ("mono", c => c.Backend = ScriptingImplementation.Mono2x),
        ("armv7 only", c => c.Architectures = AndroidArchitecture.ARMv7),
        ("min sdk 24", c => c.MinSdk = 24),
        ("custom keystore", c => c.CustomKeystore = true),
        ("app bundle", c => c.AppBundle = true),
    };
    foreach (var (name, breakIt) in cases)
    {
        var c = Valid();
        breakIt(c);
        Assert.AreEqual(1, AndroidBuild.Problems(c).Count, name);
    }
    var both = Valid();
    both.Architectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
    CollectionAssert.IsEmpty(AndroidBuild.Problems(both), "ARMv7 + ARM64 is fine");
}

[Test]
public void Problems_BadSceneLists_Reported()
{
    const string sample = "Assets/Scenes/SampleScene.unity";
    const string missing = "Assets/_Project/Scenes/Missing.unity";
    var cases = new (string Name, string[] Scenes, string MustMention)[]
    {
        ("empty", new string[0], null),
        ("boot not first", new[] { GameScenes[1], GameScenes[0], GameScenes[2], GameScenes[3], GameScenes[4] }, null),
        ("sample scene at the end", GameScenes.Append(sample).ToArray(), sample),
        ("missing file", GameScenes.Append(missing).ToArray(), missing),
    };
    foreach (var (name, scenes, mention) in cases)
    {
        var c = Valid();
        c.Scenes = scenes;
        var problems = AndroidBuild.Problems(c);
        Assert.AreEqual(1, problems.Count, $"{name}: {string.Join(" | ", problems)}");
        if (mention != null) StringAssert.Contains(mention, problems[0], name);
    }
}

[Test]
public void OutputPath_ReadsBuildOutputArg_ElseDefault()
{
    Assert.AreEqual("X.apk", AndroidBuild.OutputPath(new[] { "Unity.exe", "-buildOutput", "X.apk", "-quit" }));
    Assert.AreEqual(AndroidBuild.DefaultOutput, AndroidBuild.OutputPath(new[] { "Unity.exe", "-quit" }));
    Assert.AreEqual(AndroidBuild.DefaultOutput, AndroidBuild.OutputPath(new[] { "Unity.exe", "-buildOutput" }));
}
```

`using`: `System`, `System.Linq`, `BunDauPeo.EditorTools`, `NUnit.Framework`, `UnityEditor`.

- [x] **Step 2: Chạy EditMode, xác nhận đỏ** vì lỗi compile (`AndroidBuild` chưa có, mã thoát 6). Đây là trạng thái đỏ mong đợi.

- [x] **Step 3: Viết `AndroidBuild.cs` theo khối Interfaces**
  - `Problems` kiểm đủ 12 mục của spec 4.2, mỗi chỗ sai thêm một dòng tiếng Việt. Mục 11 và 12 thêm một dòng cho mỗi scene, có ghi đường dẫn.
  - Chỉ kiểm mục 10 khi danh sách không rỗng.
  - Mục 2 là một dòng duy nhất cho cả ba điều kiện dọc (`Portrait`, `PortraitUpsideDown`, mặc định `Portrait`/`PortraitUpsideDown`).
  - Mục 5 dùng `(c.Architectures & AndroidArchitecture.ARM64) == 0`.
  - Mục 12 dùng `File.Exists(path)`, đường dẫn tính so với thư mục project.
  - `Current()` đọc:
    - `PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)`
    - `PlayerSettings.defaultInterfaceOrientation`, `allowedAutorotateTo*`
    - `PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)`
    - `PlayerSettings.Android.targetArchitectures`
    - `(int)PlayerSettings.Android.minSdkVersion`
    - `PlayerSettings.Android.useCustomKeystore`
    - `EditorUserBuildSettings.buildAppBundle`
    - `EditorBuildSettings.scenes` đang bật, giữ thứ tự
  - `Build()` chưa viết (Task 3).

- [x] **Step 4: Chạy EditMode:** 746/746 xanh.

---

### Task 2: `ProjectSetup.ApplyBuildSettings` và cấu hình thật

**Files:**
- Modify: `Assets/_Project/Editor/ProjectSetup.cs` (`Run`, `PutScenesInBuildOrder` khoảng dòng 503–515, `ApplyPlayerSettings` khoảng dòng 517–528)
- Test: `Assets/_Project/Tests/AndroidBuildTests.cs`
- Đổi bởi Unity: `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/EditorBuildSettings.asset`

**Interfaces:**
- Consumes: `AndroidBuild.PackageName`, `AndroidBuild.Current()`, `AndroidBuild.Problems()` (Task 1).
- Produces: `public static void ProjectSetup.ApplyBuildSettings()` = `PutScenesInBuildOrder(); ApplyPlayerSettings(); AssetDatabase.SaveAssets();`. `Run()` gọi nó thay cho hai lời gọi riêng.

- [x] **Step 1: Thêm test, chạy, xác nhận đỏ**

```csharp
[Test]
public void CurrentProject_HasNoProblems()
{
    var problems = AndroidBuild.Problems(AndroidBuild.Current());
    CollectionAssert.IsEmpty(problems, string.Join("\n", problems));
}
```

Đỏ ít nhất vì thiếu package name Android và vì `SampleScene`. Ghi lại các dòng lỗi trong thông báo.

- [x] **Step 2: Sửa `ProjectSetup`**
  - `ApplyPlayerSettings`: thêm `PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidBuild.PackageName);` (`using UnityEditor.Build;`).
  - `PutScenesInBuildOrder`: luôn gán `ordered`, nối thêm các scene còn lại có đường dẫn bắt đầu bằng `"Assets/_Project/Scenes/"`, bỏ dòng `return` sớm. File `SampleScene.unity` giữ nguyên.
  - Thêm `ApplyBuildSettings()` có `public static`. Comment một dòng giải thích vì sao tách khỏi `Run`: để chạy riêng ở batch mode mà không phải chạy cả Setup Project.

- [x] **Step 3: Ghi cấu hình ở batch mode** (Editor đóng):

```bash
unity run . -- -executeMethod BunDauPeo.EditorTools.ProjectSetup.ApplyBuildSettings -logFile ../../bdp_BunDauPeo/android/apply.log
```

- [x] **Step 4: Kiểm diff.** `git diff ProjectSettings/` phải có hai thay đổi:
  - `ProjectSettings.asset`: thêm dòng `Android: com.sweetyuu.bundaupeo` dưới `applicationIdentifier`.
  - `EditorBuildSettings.asset`: mất mục `Assets/Scenes/SampleScene.unity`.

  Có dòng nào khác đổi thì ghi vào báo cáo, không sửa tay. Không thấy dòng `Android:` thì PlayerSettings chưa được lưu (spec mục 9): đọc `apply.log`, rồi báo, không chạy tiếp.

- [x] **Step 5: Chạy EditMode** được 747/747 xanh, trong đó có `ProjectSetupTests.BootScene_IsFirstInBuild_…`, `GachaSceneTests`, `BattleScene_InBuildSettings_AfterRestaurant`. Chạy PlayMode được 12/12 xanh (smoke test đi qua Build Settings).

---

### Task 3: `AndroidBuild.Build()` và build thật lần đầu

**Files:**
- Modify: `Assets/_Project/Editor/AndroidBuild.cs`

**Interfaces:**
- Consumes: `Current()`, `Problems()`, `OutputPath()` (Task 1); cấu hình đã ghi ở Task 2.
- Produces: `public static void Build()`, gọi bằng `-executeMethod BunDauPeo.EditorTools.AndroidBuild.Build`.

- [x] **Step 1: Viết `Build()` theo luồng ở spec mục 4.2**
  - Có lỗi cấu hình: `Debug.LogError` với dòng `BuildAndroid FAILED: cấu hình sai`, mỗi lỗi một dòng, rồi thoát mã 1.
  - Tạo thư mục cha của `OutputPath(Environment.GetCommandLineArgs())`.
  - Gọi `BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = c.Scenes, locationPathName = path, target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android, options = BuildOptions.Development })`.
  - Kết quả khác `Succeeded`: log `BuildAndroid FAILED: {result}, {totalErrors} lỗi` và thoát mã 1.
  - Thành công: log `BuildAndroid OK {Path.GetFullPath(path)} {MB:0.0} MB {phút:0.0} phút` và thoát mã 0.
  - `EditorApplication.Exit(code)` chỉ gọi khi `Application.isBatchMode`. Comment: "Không tắt Editor của user khi lỡ gọi từ eval."

- [x] **Step 2: Chạy EditMode** được 747/747 xanh (compile sạch).

- [x] **Step 3: Ghi trạng thái trước khi build.** `git status --short > ../../bdp_BunDauPeo/android/status-before.txt`, rồi ghi giờ bắt đầu.

- [x] **Step 4: Build thật** (Editor đóng; chạy nền, chờ thông báo):

```bash
unity build . --target Android --execute-method BunDauPeo.EditorTools.AndroidBuild.Build --output-path ../../bdp_BunDauPeo/android/BunDauPeo.apk --log-file ../../bdp_BunDauPeo/android/build.log --timeout 3600
```

  - CLI từ chối vì git có thay đổi chưa commit: chạy lại với `--allow-dirty-build`, rồi ghi vào báo cáo.
  - `unity build` chạy sai (không truyền `-buildOutput`, không nhận `--execute-method`…): dùng lệnh dự phòng ở spec mục 5 và ghi lesson `#build`.

- [x] **Step 5: Kiểm kết quả.**
  - Mã thoát 0.
  - `grep "BuildAndroid" ../../bdp_BunDauPeo/android/build.log` có `BuildAndroid OK` với đường dẫn `D:\bdp_BunDauPeo\android\BunDauPeo.apk`.
  - File APK có giờ sửa mới hơn giờ bắt đầu.
  - `git status --short` giống `status-before.txt`: không có `ProjectSettings/` hay `Assets/` mới.
  - Ghi thời gian build và cỡ APK vào checkbox.
  - Build hỏng: đọc lỗi trong log (Gradle, IL2CPP), sửa nếu lỗi nằm trong code dự án; nếu cần cài thêm hay cần mạng thì dừng và báo user. Sai lần hai cùng một việc thì dừng ở Task 2 và báo (luật của worker).

- [x] **Step 6: Chạy lại EditMode và PlayMode** được 747/747 và 12/12. Bước này kiểm rằng target Android không làm hỏng test trong Editor (spec mục 4.2).

---

### Task 4: Docs

**Files:**
- Modify: `docs/guides/unity-workflow.md`, `docs/project/tech-stack.md`, `docs/decisions/decisions.md`, `docs/roadmap.md`

- [x] **Step 1: `unity-workflow.md`**
  - Thay dòng "Build" trong bảng lệnh bằng lệnh build thật đã chạy được ở Task 3 (kèm `--allow-dirty-build` nếu đã phải thêm), và ghi chú "Editor sau đó mở ở target Android".
  - Thêm một câu về cách biết kết quả: mã 0, có `BuildAndroid OK`, APK có giờ sửa mới.
  - Thêm một câu cho `ApplyBuildSettings`.

- [x] **Step 2: `tech-stack.md`**
  - Đổi dòng `TODO(user): Android, iOS hay cả hai` thành "Android trước ([D-007](../decisions/decisions.md)); cấu hình build ở [D-011](../decisions/decisions.md)".
  - Ghi module Android đã cài cho 6000.3.15f1.

- [x] **Step 3: `decisions.md`.** Thêm D-011 ở đầu danh sách: "Build Android thử: APK debug, IL2CPP ARM64, package `com.sweetyuu.bundaupeo` (2026-11-07)". Gồm:
  - Bối cảnh: roadmap, hạn 31/12.
  - Quyết định: bảng ở spec mục 3; build chỉ kiểm, không ghi cấu hình; Development Build; Editor ở target Android.
  - Đánh đổi: package không đổi được sau khi lên Play và quyết định thư mục save; IL2CPP build chậm; nhánh không Development chưa được build thử.
  - Trạng thái: Accepted.

- [x] **Step 4: `roadmap.md`.** Dòng "Đang làm" ghi "Build Android thử sớm: code xong, chờ 🔍", kèm số test, thời gian build và cỡ APK. Mục chỉ chuyển xuống "Đã xong" (kèm ngày, link spec và kế hoạch) sau khi 🔍 duyệt, như các đợt trước.

## Hoàn tất đợt

- [x] Tổng test ở checkbox cuối khớp `../../bdp_BunDauPeo/tests.xml` (747/747) và `playtests.xml` (12/12).
- [ ] Báo cáo cuối kênh gửi 📋 (ghi thời gian build, cỡ APK, có phải thêm `--allow-dirty-build` không, Editor nay ở target Android). 🎯 mở 🔍 review cuối nhánh. Đợt này không đụng tiền hay save nên không cần mức xhigh.
- [ ] User cài thử APK khi có điện thoại (`adb install -r D:/bdp_BunDauPeo/android/BunDauPeo.apk`). Không chặn việc đóng đợt.
