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
        public const string DefaultOutput = "../../bdp_BunDauPeo/android/BunDauPeo.apk"; // so với thư mục project

        private const string SceneFolder = "Assets/_Project/Scenes/";

        public sealed class Config
        {
            public string PackageName;
            public UIOrientation DefaultOrientation;
            public bool Portrait, PortraitUpsideDown, LandscapeLeft, LandscapeRight;
            public ScriptingImplementation Backend;
            public AndroidArchitecture Architectures;
            public int MinSdk;
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
            CustomKeystore = PlayerSettings.Android.useCustomKeystore,
            AppBundle = EditorUserBuildSettings.buildAppBundle,
            Scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
        };

        public static List<string> Problems(Config c)
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
            if (c.CustomKeystore)
                p.Add("Đang dùng keystore riêng; bản thử phải ký debug.");
            if (c.AppBundle)
                p.Add("Đang bật Build App Bundle; bản thử là APK.");

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
            Debug.Log($"BuildAndroid OK {Path.GetFullPath(path)} {s.totalSize / 1048576.0:0.0} MB {s.totalTime.TotalMinutes:0.0} phút");
            Finish(0);
        }

        // Không tắt Editor của user khi lỡ gọi từ eval.
        private static void Finish(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
