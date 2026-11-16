using System;
using System.Linq;
using BunDauPeo.EditorTools;
using NUnit.Framework;
using UnityEditor;

namespace BunDauPeo.Tests
{
    public class AndroidBuildTests
    {
        private static readonly string[] GameScenes =
        {
            ProjectSetup.BootScenePath, ProjectSetup.TitleScenePath, ProjectSetup.RestaurantScenePath, ProjectSetup.BattleScenePath,
        };

        private static AndroidBuild.Config Valid() => new AndroidBuild.Config
        {
            PackageName = AndroidBuild.PackageName, DefaultOrientation = UIOrientation.AutoRotation,
            Portrait = false, PortraitUpsideDown = false, LandscapeLeft = true, LandscapeRight = true,
            Backend = ScriptingImplementation.IL2CPP, Architectures = AndroidArchitecture.ARM64,
            MinSdk = 25, TargetSdk = 36, VersionCode = 1, Development = false, CustomKeystore = false, AppBundle = false, Scenes = (string[])GameScenes.Clone(),
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

        private static AndroidBuild.Config ValidRelease()
        {
            var c = Valid();
            c.CustomKeystore = true;
            c.AppBundle = true;
            return c;
        }

        [Test]
        public void Problems_ReleaseKind_ReportsEachFault()
        {
            CollectionAssert.IsEmpty(AndroidBuild.Problems(ValidRelease(), AndroidBuild.BuildKind.ReleaseAab), "valid release");
            var cases = new (string Name, Action<AndroidBuild.Config> Break, string Mention)[]
            {
                ("target sdk", c => c.TargetSdk = 35, "Target API"),
                ("version code", c => c.VersionCode = 0, "versionCode"),
                ("no signing", c => c.CustomKeystore = false, "ký phát hành"),
                ("no app bundle", c => c.AppBundle = false, "App Bundle"),
                ("development", c => c.Development = true, "Development"),
            };
            foreach (var (name, breakIt, mention) in cases)
            {
                var c = ValidRelease();
                breakIt(c);
                var problems = AndroidBuild.Problems(c, AndroidBuild.BuildKind.ReleaseAab);
                Assert.AreEqual(1, problems.Count, $"{name}: {string.Join(" | ", problems)}");
                StringAssert.Contains(mention, problems[0], name);
            }
        }

        [Test]
        public void Problems_DebugKind_DoesNotDemandRelease()
        {
            var c = Valid();
            c.TargetSdk = 0;
            c.VersionCode = 0;
            c.Development = true;
            CollectionAssert.IsEmpty(AndroidBuild.Problems(c, AndroidBuild.BuildKind.DebugApk));
        }

        [Test]
        public void ReleaseOutputPath_UsesVersionCode()
        {
            Assert.AreEqual("../../bdp_BunDauPeo/android/release/BunDauPeo-7.aab", AndroidBuild.ReleaseOutputPath(new[] { "Unity.exe", "-quit" }, 7));
            Assert.AreEqual("X.aab", AndroidBuild.ReleaseOutputPath(new[] { "Unity.exe", "-buildOutput", "X.aab" }, 7));
            Assert.AreEqual("../../bdp_BunDauPeo/android/release/BunDauPeo-7.aab", AndroidBuild.ReleaseOutputPath(new[] { "Unity.exe", "-buildOutput" }, 7));
        }

        [Test]
        public void ProjectSettings_HoldNoKeystoreSecrets()
        {
            var text = System.IO.File.ReadAllText("ProjectSettings/ProjectSettings.asset");
            foreach (var key in new[] { "AndroidKeystoreName:", "AndroidKeyaliasName:" })
            {
                var line = text.Split((char)10).First(l => l.TrimStart().StartsWith(key));
                Assert.AreEqual(key, line.Trim(), key);
            }

            // Ký rồi trả lại không được để lại gì trong cài đặt (Unity ghi '{inproject}: ' nếu gán "").
            var saved = AndroidSigning.Apply(k => "x");
            AndroidSigning.Restore(saved);
            var so = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            Assert.AreEqual("", so.FindProperty("AndroidKeystoreName").stringValue);
            Assert.AreEqual("", so.FindProperty("AndroidKeyaliasName").stringValue);
        }

        [Test]
        public void CurrentProject_TargetSdk36()
        {
            Assert.AreEqual(AndroidSdkVersions.AndroidApiLevel36, PlayerSettings.Android.targetSdkVersion);
            Assert.AreEqual(AndroidBuild.VersionCode, PlayerSettings.Android.bundleVersionCode);
        }

        [Test]
        public void Problems_BadSceneLists_Reported()
        {
            const string sample = "Assets/Scenes/SampleScene.unity";
            const string missing = "Assets/_Project/Scenes/Missing.unity";
            var cases = new (string Name, string[] Scenes, string MustMention)[]
            {
                ("empty", new string[0], null),
                ("boot not first", new[] { GameScenes[1], GameScenes[0], GameScenes[2] }, null),
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
        public void CurrentProject_HasNoProblems()
        {
            var problems = AndroidBuild.Problems(AndroidBuild.Current());
            CollectionAssert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void OutputPath_ReadsBuildOutputArg_ElseDefault()
        {
            Assert.AreEqual("X.apk", AndroidBuild.OutputPath(new[] { "Unity.exe", "-buildOutput", "X.apk", "-quit" }));
            Assert.AreEqual(AndroidBuild.DefaultOutput, AndroidBuild.OutputPath(new[] { "Unity.exe", "-quit" }));
            Assert.AreEqual(AndroidBuild.DefaultOutput, AndroidBuild.OutputPath(new[] { "Unity.exe", "-buildOutput" }));
        }
    }
}
