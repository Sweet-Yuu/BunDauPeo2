using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BunDauPeo.EditorTools
{
    // Build APK debug Android. Chỉ kiểm cấu hình, không ghi ProjectSettings (xem ProjectSetup.ApplyBuildSettings).
    public static class AndroidBuild
    {
        public const string PackageName = "com.sweetyuu.bundaupeo";
        public const int MinSdk = 25;
        public const int TargetSdk = 36; // Google Play yêu cầu target mới nhất cho app mới
        public const int VersionCode = 1; // +1 mỗi lần upload; ProjectSetup ghi vào PlayerSettings
        public const string DefaultOutput = "../../bdp_BunDauPeo/android/BunDauPeo.apk"; // so với thư mục project

        private const string SceneFolder = "Assets/_Project/Scenes/";

        public enum BuildKind { DebugApk, ReleaseAab }

        public sealed class Config
        {
            public string PackageName;
            public UIOrientation DefaultOrientation;
            public bool Portrait, PortraitUpsideDown, LandscapeLeft, LandscapeRight;
            public ScriptingImplementation Backend;
            public AndroidArchitecture Architectures;
            public int MinSdk;
            public int TargetSdk;
            public int VersionCode;
            public bool Development;
            public bool CustomKeystore;
            public bool AppBundle;
            public string[] Scenes; // scene đang bật trong Build Settings, theo thứ tự
        }

        public static Config Current() => new Config
        {
            PackageName = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
            DefaultOrientation = PlayerSettings.defaultInterfaceOrientation,
            Portrait = PlayerSettings.allowedAutorotateToPortrait,
            PortraitUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown,
            LandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft,
            LandscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight,
            Backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android),
            Architectures = PlayerSettings.Android.targetArchitectures,
            MinSdk = (int)PlayerSettings.Android.minSdkVersion,
            TargetSdk = (int)PlayerSettings.Android.targetSdkVersion,
            VersionCode = PlayerSettings.Android.bundleVersionCode,
            Development = EditorUserBuildSettings.development,
            CustomKeystore = PlayerSettings.Android.useCustomKeystore,
            AppBundle = EditorUserBuildSettings.buildAppBundle,
            Scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
        };

        public static List<string> Problems(Config c) => Problems(c, BuildKind.DebugApk);

        public static List<string> Problems(Config c, BuildKind kind)
        {
            var p = new List<string>();
            if (c.PackageName != PackageName)
                p.Add($"Package name là '{c.PackageName}', phải là '{PackageName}'.");
            if (c.Portrait || c.PortraitUpsideDown
                || c.DefaultOrientation == UIOrientation.Portrait || c.DefaultOrientation == UIOrientation.PortraitUpsideDown)
                p.Add("Đang cho phép màn dọc; game chỉ chạy màn ngang.");
            if (c.DefaultOrientation == UIOrientation.AutoRotation && !c.LandscapeLeft && !c.LandscapeRight)
                p.Add("Tự xoay nhưng không cho hướng ngang nào.");
            if (c.Backend != ScriptingImplementation.IL2CPP)
                p.Add($"Scripting backend là {c.Backend}, phải là IL2CPP.");
            if ((c.Architectures & AndroidArchitecture.ARM64) == 0)
                p.Add("Thiếu kiến trúc ARM64.");
            if (c.MinSdk < MinSdk)
                p.Add($"Minimum API level {c.MinSdk} < {MinSdk}.");
            if (kind == BuildKind.DebugApk)
            {
                if (c.CustomKeystore)
                    p.Add("Đang dùng keystore riêng; bản thử phải ký debug.");
                if (c.AppBundle)
                    p.Add("Đang bật Build App Bundle; bản thử là APK.");
            }
            else
            {
                if (c.TargetSdk != TargetSdk)
                    p.Add($"Target API level là {c.TargetSdk}, phải là {TargetSdk}.");
                if (c.VersionCode < 1)
                    p.Add("versionCode phải từ 1 trở lên.");
                if (!c.CustomKeystore)
                    p.Add("Chưa bật ký phát hành (keystore riêng).");
                if (!c.AppBundle)
                    p.Add("Chưa bật Build App Bundle; bản phát hành là AAB.");
                if (c.Development)
                    p.Add("Đang bật Development Build; bản phát hành không được.");
            }

            if (c.Scenes.Length == 0)
            {
                p.Add("Danh sách scene trong Build Settings rỗng.");
                return p;
            }
            if (c.Scenes[0] != ProjectSetup.BootScenePath)
                p.Add($"Scene đầu tiên là '{c.Scenes[0]}', phải là '{ProjectSetup.BootScenePath}'.");
            foreach (var s in c.Scenes)
            {
                if (!s.StartsWith(SceneFolder)) p.Add($"Scene ngoài {SceneFolder}: {s}");
                if (!File.Exists(s)) p.Add($"Scene không có file: {s}");
            }
            return p;
        }

        public static string OutputPath(string[] args)
        {
            var i = System.Array.IndexOf(args, "-buildOutput");
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : DefaultOutput;
        }

        public static string ReleaseOutputPath(string[] args, int versionCode)
        {
            var i = System.Array.IndexOf(args, "-buildOutput");
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : $"../../bdp_BunDauPeo/android/release/BunDauPeo-{versionCode}.aab";
        }

        // Điểm vào cho -executeMethod: ký bằng biến BDP_*, ra AAB (hoặc APK nếu -buildOutput đuôi .apk, để smoke).
        public static void BuildRelease()
        {
            var args = System.Environment.GetCommandLineArgs();
            System.Func<string, string> env = System.Environment.GetEnvironmentVariable;
            var signing = AndroidSigning.Problems(env, Path.GetFullPath("."));
            if (signing.Count > 0)
            {
                Debug.LogError("BuildAndroidRelease FAILED: ký sai: " + string.Join(" | ", signing));
                Finish(1);
                return;
            }

            var savedSigning = AndroidSigning.Apply(env);
            var savedBundle = EditorUserBuildSettings.buildAppBundle;
            var savedSymbols = EditorUserBuildSettings.androidCreateSymbols;
            var code = 1;
            try
            {
                var path = ReleaseOutputPath(args, PlayerSettings.Android.bundleVersionCode);
                EditorUserBuildSettings.buildAppBundle = path.EndsWith(".aab");
                EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
                var kind = EditorUserBuildSettings.buildAppBundle ? BuildKind.ReleaseAab : BuildKind.DebugApk;
                var c = Current();
                // APK phát hành vẫn ký bằng khoá phát hành, nên chỉ kiểm đầy đủ khi là AAB.
                var problems = kind == BuildKind.ReleaseAab ? Problems(c, kind) : Problems(c, BuildKind.ReleaseAab).Where(m => !m.Contains("App Bundle")).ToList();
                if (problems.Count > 0)
                {
                    Debug.LogError("BuildAndroidRelease FAILED: cấu hình sai: " + string.Join(" | ", problems));
                    return;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = c.Scenes,
                    locationPathName = path,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None,
                });
                var s = report.summary;
                if (s.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"BuildAndroidRelease FAILED: {s.result}, {s.totalErrors} lỗi");
                    return;
                }
                Debug.Log($"BuildAndroidRelease OK {Path.GetFullPath(path)} {new FileInfo(path).Length / 1048576.0:0.0} MB {s.totalTime.TotalMinutes:0.0} phút");
                code = 0;
            }
            finally
            {
                AndroidSigning.Restore(savedSigning);
                EditorUserBuildSettings.buildAppBundle = savedBundle;
                EditorUserBuildSettings.androidCreateSymbols = savedSymbols;
                Finish(code);
            }
        }

        // Điểm vào cho -executeMethod. Không có mục menu: build trong Editor đang mở sẽ đổi target trước mặt user.
        public static void Build()
        {
            var c = Current();
            var problems = Problems(c);
            if (problems.Count > 0)
            {
                Debug.LogError("BuildAndroid FAILED: cấu hình sai\n" + string.Join("\n", problems));
                Finish(1);
                return;
            }

            var path = OutputPath(System.Environment.GetCommandLineArgs());
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = c.Scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            });

            var s = report.summary;
            if (s.result != BuildResult.Succeeded)
            {
                Debug.LogError($"BuildAndroid FAILED: {s.result}, {s.totalErrors} lỗi");
                Finish(1);
                return;
            }
            Debug.Log($"BuildAndroid OK {Path.GetFullPath(path)} {new FileInfo(path).Length / 1048576.0:0.0} MB {s.totalTime.TotalMinutes:0.0} phút");
            Finish(0);
        }

        // Không tắt Editor của user khi lỡ gọi từ eval.
        private static void Finish(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
