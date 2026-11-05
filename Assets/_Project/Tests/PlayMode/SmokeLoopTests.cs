using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Playtest;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BunDauPeo.PlayModeTests
{
    // Vòng Combat trọn vẹn qua UI thật: Boot → bản đồ → đội → trận AUTO → thưởng → nạp lại save. Chạy trên save tạm.
    public class SmokeLoopTests
    {
        private const float SceneTimeout = 30f;
        private const float ButtonTimeout = 15f;
        private const float BattleTimeout = 60f;

        private string _dir;
        private string _savePath;
        private string _realSaveHash;
        private string _step = "SetUp";
        private bool _refused;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Chạy từ Test Runner trong Editor không có -runTests nên Boot đã chạy với save thật (L-023): dừng trước khi đụng gì.
            _refused = Object.FindFirstObjectByType<GameBootstrap>() != null;
            Assert.IsFalse(_refused, "Đã có GameBootstrap chạy với save thật (Play từ Boot trong Editor). Chạy smoke test bằng `unity test --mode PlayMode` (Editor đóng), không chạy từ Test Runner.");
            _dir = Path.Combine(Path.GetTempPath(), "bdp-smoke-" + Path.GetRandomFileName());
            _savePath = Path.Combine(_dir, "save.json");
            _realSaveHash = HashRealSave();
            yield return null; // Play đã chạy hẳn rồi mới đặt override và nạp Boot
            GameBootstrap.SavePathOverride = _savePath;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_refused) yield break; // bản Boot đang chạy là của người chơi, không phải của test: không huỷ, không xoá
            GameBootstrap.SavePathOverride = null;
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null) Object.Destroy(bootstrap.gameObject); // DontDestroyOnLoad nên không tự mất
            ServiceLocator.Clear();
            yield return null;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Assert.AreEqual(_realSaveHash, HashRealSave(), "save thật đã bị đổi");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator FullLoop_NewGame_Stage1_AutoWin_RewardsAndSaveReload()
        {
            _step = "1 nạp Boot";
            SceneManager.LoadScene("Boot");
            yield return WaitForScene("Restaurant");
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            Assert.IsNotNull(session, Where("GameBootstrap không có Session"));
            Assert.IsTrue(session.IsNewGame, Where("save tạm phải là game mới"));
            var newGame = LoadNewGameConfig();
            foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
                Assert.AreEqual(newGame.StartingCurrency.Where(l => l.Type == type).Sum(l => l.Amount), session.Wallet.Get(type),
                    Where($"ví {type} lúc mới"));

            var combat = ServiceLocator.Get<CombatService>();
            // "Màn 1" là màn Battle đầu tiên; các màn Truyện đứng trước nó phải qua trước (màn sau chỉ mở khi màn trước đã clear).
            var stages = combat.Config.Stages.Items;
            var battleIndex = stages.Select((st, i) => (st, i)).First(x => x.st.Kind == StageKind.Battle).i;
            var stage1 = stages[battleIndex];
            var stage2 = stages[battleIndex + 1];
            var walletBefore = Enum.GetValues(typeof(CurrencyType)).Cast<CurrencyType>().ToDictionary(t => t, t => session.Wallet.Get(t));
            var staminaBefore = combat.Stamina.Current; // đọc một lần để chốt số thô (L-021)
            Assert.AreEqual(combat.Stamina.Max, staminaBefore, Where("game mới phải đầy thể lực"));

            // DevOverlay chỉ được dựng lúc Play khởi động (khi chưa có service) nên không có nút "Màn chơi" trong test: nạp bản đồ trực tiếp.
            _step = "2 vào bản đồ";
            SceneManager.LoadScene(BattleView.StageSelectScene);
            yield return WaitForScene(BattleView.StageSelectScene);

            _step = "3 chọn màn 1 và đội";
            for (var i = 0; i < battleIndex; i++)
            {
                yield return ClickWhenReady("Node" + i);
                yield return ClickWhenReady("ContinueButton");
                Assert.AreEqual(StageStatus.Cleared, combat.Progress.GetStatus(stages[i].Id), Where($"màn Truyện {i} chưa clear"));
            }
            yield return ClickWhenReady("Node" + battleIndex);
            yield return ClickWhenReady("ChallengeButton");
            var teamSize = Math.Min(session.Roster.GetOwned().Count, Roster.MaxBattleTeamSize);
            Assert.Greater(teamSize, 0, Where("không có nhân vật khởi đầu"));
            for (var i = 0; i < teamSize; i++) yield return ClickWhenReady("TeamCard" + i);
            Assert.AreEqual(teamSize, session.Roster.GetBattleTeam().Count, Where("đội sau khi chọn thẻ"));

            _step = "4 vào trận";
            yield return ClickWhenReady("StartButton");
            if (BattleFormation.GetEntryStatus(teamSize) == TeamEntryStatus.NeedsConfirm)
                yield return ClickWhenReady("SmallConfirmYesButton");
            yield return WaitForScene(BattleView.BattleScene);

            _step = "5 bật x2 và AUTO";
            Assert.IsFalse(combat.AutoEnabled, Where("AUTO phải tắt khi vào trận"));
            yield return ClickWhenReady("SpeedButton");
            yield return ClickWhenReady("AutoButton");
            Assert.IsTrue(combat.FastEnabled, Where("x2 chưa bật"));
            Assert.IsTrue(combat.AutoEnabled, Where("AUTO chưa bật"));

            _step = "6 chờ kết quả trận";
            var deadline = Time.realtimeSinceStartup + BattleTimeout;
            while (combat.Current == null || (combat.Current.State != BattleState.Won && combat.Current.State != BattleState.Lost))
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail(Where($"{BattleTimeout}s mà trận chưa xong (state {combat.Current?.State}). Nút: {UiDriver.DescribeVisibleButtons()}"));
                yield return null;
            }
            Assert.AreEqual(BattleState.Won, combat.Current.State, Where("thua màn 1 bằng AUTO: lỗi cân bằng thật"));
            yield return WaitForButtonStep("ConfirmButton"); // màn kết quả đã dựng nghĩa là thưởng đã trả (BattleResultView.Show)

            _step = "7 kiểm kết quả";
            Assert.AreEqual(staminaBefore - stage1.StaminaCost, combat.Stamina.Current, Where("thể lực sau trận"));
            foreach (var line in stage1.FirstClearReward)
                Assert.AreEqual(walletBefore[line.Type] + line.Amount, session.Wallet.Get(line.Type), Where($"thưởng lần đầu {line.Type}"));
            Assert.AreEqual(StageStatus.Cleared, combat.Progress.GetStatus(stage1.Id), Where("màn 1"));
            Assert.AreEqual(StageStatus.Available, combat.Progress.GetStatus(stage2.Id), Where("màn 2 phải mở"));
            Assert.Contains(stage1.Id, session.Save.ClearedStages.ToList());
            var shot = ScreenCapture1080.Capture("smoke-result.png");
            Assert.IsFalse(shot.StartsWith("ERROR:"), Where(shot));
            Assert.IsTrue(File.Exists(shot), Where("ảnh chụp không có: " + shot));
            Debug.Log("Smoke test screenshot: " + shot);

            _step = "8 về bản đồ";
            yield return ClickWhenReady("ConfirmButton");
            yield return WaitForScene(BattleView.StageSelectScene);

            // So từng trường chứ không so cả JSON: quán còn chạy ngầm và đổi Afk/DailyPresence trong bộ nhớ sau lần lưu cuối.
            _step = "9 nạp lại save";
            var loaded = new SaveService(_savePath).Load();
            Assert.IsNotNull(loaded, Where("không đọc lại được save tạm"));
            var loadedWallet = new Wallet(loaded);
            foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
                Assert.AreEqual(session.Wallet.Get(type), loadedWallet.Get(type), Where($"ví {type} trong save"));
            CollectionAssert.AreEqual(session.Save.ClearedStages, loaded.ClearedStages, Where("ClearedStages trong save"));
            CollectionAssert.AreEqual(session.Save.OwnedCharacterIds, loaded.OwnedCharacterIds, Where("nhân vật trong save"));
            CollectionAssert.AreEqual(session.Save.BattleTeam, loaded.BattleTeam, Where("đội trong save"));
            Assert.AreEqual(staminaBefore - stage1.StaminaCost, loaded.Stamina.Current, Where("thể lực trong save"));
        }

        private string Where(string message) => $"[bước {_step}] {message}";

        private IEnumerator WaitForScene(string name)
        {
            var deadline = Time.realtimeSinceStartup + SceneTimeout;
            while (SceneManager.GetActiveScene().name != name)
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail(Where($"{SceneTimeout}s chờ scene '{name}', đang ở '{SceneManager.GetActiveScene().name}'. Nút: {UiDriver.DescribeVisibleButtons()}"));
                yield return null;
            }
            yield return null; // để Awake/Start/Update đầu của scene chạy
        }

        private IEnumerator WaitForButtonStep(string name)
        {
            var inner = UiDriver.WaitForButton(name, ButtonTimeout);
            while (true)
            {
                try
                {
                    if (!inner.MoveNext()) yield break;
                }
                catch (TimeoutException e)
                {
                    Assert.Fail(Where(e.Message));
                }
                yield return inner.Current;
            }
        }

        private IEnumerator ClickWhenReady(string name)
        {
            yield return WaitForButtonStep(name);
            var result = UiDriver.Click(name);
            Assert.AreEqual(ClickStatus.Clicked, result.Status, Where(result.ToString()));
            yield return null; // cho UI xử lý cú bấm
        }

        private static NewGameConfig LoadNewGameConfig()
        {
#if UNITY_EDITOR
            var guid = UnityEditor.AssetDatabase.FindAssets("t:NewGameConfig").Single();
            return UnityEditor.AssetDatabase.LoadAssetAtPath<NewGameConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
#else
            throw new NotSupportedException("Smoke test chỉ chạy trong Editor.");
#endif
        }

        // Băm mọi file save.json* thật (không có thì băm rỗng) để TearDown biết test có chạm vào save thật không.
        private static string HashRealSave()
        {
            var dir = Application.persistentDataPath;
            var files = Directory.Exists(dir)
                ? Directory.GetFiles(dir).Where(f =>
                {
                    var n = Path.GetFileName(f);
                    return n == "save.json" || n.StartsWith("save.json.", StringComparison.Ordinal);
                }).OrderBy(f => f, StringComparer.Ordinal).ToArray()
                : new string[0];
            using var sha = SHA256.Create();
            foreach (var f in files)
            {
                var name = System.Text.Encoding.UTF8.GetBytes(Path.GetFileName(f));
                sha.TransformBlock(name, 0, name.Length, null, 0);
                var bytes = File.ReadAllBytes(f);
                sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
            }
            sha.TransformFinalBlock(new byte[0], 0, 0);
            return BitConverter.ToString(sha.Hash);
        }
    }
}
