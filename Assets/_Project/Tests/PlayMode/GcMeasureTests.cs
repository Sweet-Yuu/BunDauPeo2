using System;
using System.Collections;
using System.IO;
using BunDauPeo.Boot;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Playtest;
using BunDauPeo.UI;
using Unity.Profiling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BunDauPeo.PlayModeTests
{
    // Đo GC mỗi khung của main thread (docs/studio/performance.md): 0 B khi đứng yên, ≤ 1 KB trung bình trong trận.
    // Đo trong Editor/batch nên gồm cả phần của Editor; số tuyệt đối chỉ để so trước/sau và tìm nguồn, không thay Profiler trên máy thật.
    public class GcMeasureTests
    {
        private string _dir;
        private LocalGameServer _server;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.IsNull(Object.FindFirstObjectByType<GameBootstrap>(), "Chạy bằng `unity test --mode PlayMode` (Editor đóng).");
            _dir = Path.Combine(Path.GetTempPath(), "bdp-gc-" + Path.GetRandomFileName());
            yield return null;
            GameBootstrap.SavePathOverride = Path.Combine(_dir, "save.json");
            _server = new LocalGameServer();
            GameBootstrap.ServerFactory = () => _server;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameBootstrap.SavePathOverride = null;
            GameBootstrap.ServerFactory = null;
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null) Object.Destroy(bootstrap.gameObject);
            foreach (var name in new[] { "DevOverlay", "OnlineOverlay", "WaitOverlay" })
            {
                var overlay = GameObject.Find(name);
                if (overlay != null) Object.Destroy(overlay);
            }
            ServiceLocator.Clear();
            yield return null;
            var game = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("GcEmpty"));
            if (game.isLoaded) yield return SceneManager.UnloadSceneAsync(game);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private static IEnumerator WaitForScene(string name)
        {
            var deadline = Time.realtimeSinceStartup + 30f;
            while (SceneManager.GetActiveScene().name != name)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "chờ scene " + name);
                yield return null;
            }
            yield return null;
        }

        private static IEnumerator Click(string name)
        {
            var deadline = Time.realtimeSinceStartup + 15f;
            while (UiDriver.Click(name).Status != ClickStatus.Clicked)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "chờ nút " + name);
                yield return null;
            }
            yield return null;
        }

        // Trung bình và lớn nhất byte GC mỗi khung (bộ đếm "GC Allocated In Frame" của Profiler), sau 1 giây khởi động.
        private static IEnumerator Sample(string label, int frames)
        {
            yield return new WaitForSecondsRealtime(1f);
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            yield return null;
            var total = 0L;
            var worst = 0L;
            for (var i = 0; i < frames; i++)
            {
                yield return null;
                var delta = recorder.LastValue;
                total += delta;
                if (delta > worst) worst = delta;
            }

            Debug.Log($"[GC] {label}: mean {total / (double)frames:F0} B/frame, worst {worst} B over {frames} frames, valid={recorder.Valid}");
        }

        // Tắt từng component có Update (một lần một cái) rồi đo lại: chênh so với "tất cả bật" là phần của component đó.
        private static IEnumerator Bisect(string label)
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;
            var targets = new System.Collections.Generic.List<MonoBehaviour>();
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (mb != null && mb.enabled && mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("BunDauPeo") && mb.GetType().GetMethod("Update", flags) != null)
                    targets.Add(mb);
            foreach (var mb in targets)
            {
                mb.enabled = false;
                yield return Sample($"{label} without {mb.GetType().Name}", 60);
                if (mb != null) mb.enabled = true;
            }
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator Measure_Idle_And_Battle()
        {
            yield return Sample("Empty scene (Editor and test runner only)", 200);
            // Đợt 4: Hộp thư đo với 20 thư quà (kế hoạch Task 7 Step 3).
            var now = System.DateTime.UtcNow;
            _server.Gifts = new System.Collections.Generic.List<BunDauPeo.Rules.GiftData>();
            for (var i = 0; i < 20; i++)
                _server.Gifts.Add(new BunDauPeo.Rules.GiftData
                {
                    Id = "perf_" + i, TitleVi = "Quà " + i, TitleEn = "Gift " + i, StartUtcTicks = now.AddDays(-1).Ticks, EndUtcTicks = now.AddDays(7).Ticks,
                    Rewards = { new BunDauPeo.Rules.GiftReward { Type = "Gem", Amount = 10 }, new BunDauPeo.Rules.GiftReward { Type = "Coin", Amount = 100 } },
                });
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            yield return Click("TapArea");
            yield return WaitForScene("Restaurant");
            yield return Sample("Restaurant idle", 300);
            // Đợt 3: trang Nhiệm vụ và Hộp thư (đứng yên, đã mở).
            yield return Click("HubRoundBtnMissions_PH_110x110");
            yield return Sample("Missions page idle", 300);
            yield return Click("CloseButton");
            yield return Click("HubRoundBtnMail_PH_110x110");
            yield return Sample("Mail page idle (20 gifts)", 300);
            yield return Click("CloseButton");
            yield return Bisect("Restaurant");

            // Cấp nhân vật: danh sách và panel chi tiết (tab Cấp) đứng yên.
            yield return Click("NavTabCharacters_PH_320x120");
            yield return Sample("Characters list idle", 300);
            yield return Click("CharacterCard0");
            yield return Sample("Characters detail idle", 300);

            var combat = ServiceLocator.Get<CombatService>();
            var stages = combat.Config.Stages.Items;
            var battleIndex = 0;
            while (stages[battleIndex].Kind != StageKind.Battle) battleIndex++;
            yield return Click("NavTabStages_PH_320x120");
            yield return Click("ModeMain_PH_820x560");
            for (var i = 0; i < battleIndex; i++)
            {
                yield return Click("Node" + i);
                yield return Click("ContinueButton");
            }

            yield return Sample("Stage map idle", 300);
            yield return Click("Node" + battleIndex);
            yield return Click("ChallengeButton");
            yield return Click("TeamCard0");
            yield return Click("StartButton");
            if (GameObject.Find("SmallConfirmYesButton") != null) yield return Click("SmallConfirmYesButton");
            yield return WaitForScene(BattleView.BattleScene);
            yield return Click("AutoButton");
            yield return Sample("Battle auto", 300);
            yield return Bisect("Battle");
        }
    }
}
