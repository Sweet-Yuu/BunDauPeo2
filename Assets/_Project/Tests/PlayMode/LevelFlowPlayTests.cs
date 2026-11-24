using System;
using System.Collections;
using System.IO;
using System.Linq;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Playtest;
using BunDauPeo.Rules;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BunDauPeo.PlayModeTests
{
    // Cấp nhân vật đầu-cuối qua game thật (Boot → Lobby, server giả, save tạm): EXP khi vào trận, nâng cấp bằng thuốc + Coin, vòng đầy đủ.
    public class LevelFlowPlayTests
    {
        private const float SceneTimeout = 30f;
        private const float ButtonTimeout = 15f;

        private string _dir;
        private string _step = "SetUp";
        private LocalGameServer _server;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.IsNull(Object.FindFirstObjectByType<GameBootstrap>(),
                "Đã có GameBootstrap chạy với save thật. Chạy bằng `unity test --mode PlayMode` (Editor đóng).");
            _dir = Path.Combine(Path.GetTempPath(), "bdp-level-" + Path.GetRandomFileName());
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
            GameLanguage.Current = Language.Vi;
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null) Object.Destroy(bootstrap.gameObject);
            foreach (var name in new[] { "DevOverlay", "OnlineOverlay" })
            {
                var overlay = GameObject.Find(name);
                if (overlay != null) Object.Destroy(overlay);
            }

            ServiceLocator.Clear();
            yield return null;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private string Where(string message) => $"[bước {_step}] {message}";

        private IEnumerator WaitForScene(string name)
        {
            var deadline = Time.realtimeSinceStartup + SceneTimeout;
            while (SceneManager.GetActiveScene().name != name)
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail(Where($"chờ scene '{name}', đang ở '{SceneManager.GetActiveScene().name}'. Nút: {UiDriver.DescribeVisibleButtons()}"));
                yield return null;
            }

            yield return null;
        }

        private IEnumerator ClickWhenReady(string name)
        {
            var inner = UiDriver.WaitForButton(name, ButtonTimeout);
            while (true)
            {
                try
                {
                    if (!inner.MoveNext()) break;
                }
                catch (TimeoutException e)
                {
                    Assert.Fail(Where(e.Message));
                }

                yield return inner.Current;
            }

            var result = UiDriver.Click(name);
            Assert.AreEqual(ClickStatus.Clicked, result.Status, Where(result.ToString()));
            yield return null;
        }

        private IEnumerator BootToLobby()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            yield return ClickWhenReady("TapArea");
            yield return WaitForScene("Restaurant");
        }

        // Mở màn Battle đầu: clear các màn Truyện trước nó ở server, có đội, thể lực đầy.
        private StageDefinition PrepareFirstBattle(GameSession session, CombatService combat)
        {
            var stages = combat.Config.Stages.Items;
            var battle = stages.First(st => st.Kind == StageKind.Battle);
            foreach (var st in stages)
            {
                if (st == battle) break;
                _server.State.ClearedStages.Add(st.Id);
            }

            session.ApplyServerState(_server.State);
            var roster = ServiceLocator.Get<Roster>();
            if (roster.GetBattleTeam().Count == 0) roster.AddToTeam(roster.GetOwned()[0].Id);
            RefillStamina(session, combat);
            return battle;
        }

        private void RefillStamina(GameSession session, CombatService combat)
        {
            _server.State.Stamina = new StaminaState { Current = combat.Stamina.Max, AnchorUtcTicks = 0 };
            session.ApplyServerState(_server.State);
        }

        private IEnumerator Enter(CombatService combat, string stageId)
        {
            var enter = combat.EnterStageAsync(stageId);
            yield return new WaitUntil(() => enter.IsCompleted);
            Assert.AreEqual(EnterStageStatus.Entered, enter.Result.Status, Where("vào trận"));
        }

        private static void Win(BattleSession played)
        {
            for (var i = 0; i < 5000 && (played.State == BattleState.PlayerTurn || played.State == BattleState.EnemyTurn); i++)
            {
                if (played.State == BattleState.PlayerTurn)
                {
                    var choice = AutoPolicy.Choose(played);
                    played.PlayerAct(choice.Kind, choice.Target);
                }
                else played.EnemyAct();
            }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator EnterStage_GivesExp_AndLevelsUpPlayer()
        {
            _step = "E1 Boot";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            var combat = ServiceLocator.Get<CombatService>();
            var player = ServiceLocator.Get<PlayerLevelService>();
            var battle = PrepareFirstBattle(session, combat);
            var coinBefore = session.Wallet.Get(CurrencyType.Coin);
            Assert.AreEqual(1, player.Level, Where("game mới Lv1"));

            _step = "E2 vào màn tới khi lên cấp";
            for (var i = 0; i < 20 && player.Level < 2; i++)
            {
                yield return Enter(combat, battle.Id);
                combat.EndBattle();
                RefillStamina(session, combat);
            }

            Assert.AreEqual(2, player.Level, Where("người chơi phải Lv2"));
            var popup = Object.FindFirstObjectByType<LevelUpPopupView>();
            Assert.IsNotNull(popup, Where("Lobby phải có popup lên cấp"));
            Assert.IsTrue(popup.IsOpen, Where("popup lên cấp phải hiện"));
            Assert.AreEqual(coinBefore + 400, session.Wallet.Get(CurrencyType.Coin), Where("thưởng 200 × cấp mới Coin"));
            Assert.AreEqual(5, session.Wallet.Get(CurrencyType.ExpPotion), Where("thưởng 5 thuốc"));
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator UpgradeCharacter_RaisesStatsAndSpendsCost()
        {
            _step = "U1 Boot, cấp thuốc và Coin";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            var levels = ServiceLocator.Get<CharacterLevelService>();
            var config = ServiceLocator.Get<LevelConfig>();
            var starter = ServiceLocator.Get<Roster>().GetOwned()[0].Id;
            // Người chơi Lv2 (trần cấp nhân vật 2); thuốc và Coin đủ cho một bậc (2 × 2 thuốc, 100 × 2 Coin).
            session.Save.PlayerExp = config.ExpToNext(1);
            session.Wallet.Add(CurrencyType.ExpPotion, 10);
            session.Wallet.Add(CurrencyType.Coin, 1000);
            session.Wallet.NotifyChanged();
            yield return null;
            yield return null; // canvas của popup vừa bật cần vài khung để nhận raycast
            yield return ClickWhenReady("LevelUpOkButton"); // Lv2 mở popup lên cấp, đóng trước khi bấm thẻ
            var coinBefore = session.Wallet.Get(CurrencyType.Coin);
            Assert.AreEqual(1, levels.GetLevel(starter), Where("Lv1 lúc đầu"));

            _step = "U2 mở thẻ, bấm Nâng cấp";
            yield return ClickWhenReady("NavTabCharacters_PH_320x120");
            yield return ClickWhenReady("CharacterCard0");
            yield return null;
            var hpBefore = GameObject.Find("HpText").GetComponent<Text>().text;
            yield return ClickWhenReady("LevelUpButton");

            _step = "U3 kiểm";
            Assert.AreEqual(2, levels.GetLevel(starter), Where("cấp +1"));
            Assert.AreEqual(6, session.Wallet.Get(CurrencyType.ExpPotion), Where("trừ 4 thuốc"));
            Assert.AreEqual(coinBefore - 200, session.Wallet.Get(CurrencyType.Coin), Where("trừ 200 Coin"));
            StringAssert.Contains("2/2", GameObject.Find("LevelLineText").GetComponent<Text>().text, Where("dòng cấp"));
            Assert.AreNotEqual(hpBefore, GameObject.Find("HpText").GetComponent<Text>().text, Where("panel hiện HP mới"));
            Assert.IsFalse(GameObject.Find("LevelUpButton").GetComponent<Button>().interactable, Where("đã chạm trần cấp người chơi"));
            yield return null;
            ScreenCapture1080.Capture("level-detail.png"); // ảnh đính kèm báo cáo; đặt cuối vì Capture đổi Canvas
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator FullLoop_EnterStageWinCollectPotionUpgradeEnterAgain()
        {
            _step = "F1 Boot, vào màn";
            yield return BootToLobby();
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            var combat = ServiceLocator.Get<CombatService>();
            var levels = ServiceLocator.Get<CharacterLevelService>();
            var config = ServiceLocator.Get<LevelConfig>();
            var battle = PrepareFirstBattle(session, combat);
            var starter = ServiceLocator.Get<Roster>().GetBattleTeam()[0].Id;
            session.Save.PlayerExp = config.ExpToNext(1); // Lv2: trần cấp nhân vật 2
            session.Wallet.NotifyChanged();
            yield return Enter(combat, battle.Id);
            var first = combat.Current.Allies.First(u => u.Character.Id == starter);
            var hpLv1 = first.MaxHp;

            _step = "F2 thắng, nhận thuốc";
            var played = combat.Current;
            Win(played);
            Assert.AreEqual(BattleState.Won, played.State, Where("thắng màn Battle đầu"));
            var done = combat.Progress.CompleteBattleAsync(played);
            yield return new WaitUntil(() => done.IsCompleted);
            Assert.AreEqual(StageCompleteStatus.Success, done.Result.Status, Where("nhận thưởng"));
            var potion = session.Wallet.Get(CurrencyType.ExpPotion);
            Assert.GreaterOrEqual(potion, 10, Where("thuốc lần đầu về ví"));

            _step = "F3 nâng nhân vật";
            session.Wallet.Add(CurrencyType.Coin, 1000);
            var result = levels.TryLevelUp(starter);
            Assert.AreEqual(LevelUpStatus.Success, result.Status, Where("nâng cấp"));
            Assert.IsTrue(result.Saved, Where("lưu"));

            _step = "F4 vào lại, chỉ số tăng";
            combat.EndBattle();
            RefillStamina(session, combat);
            yield return Enter(combat, battle.Id);
            var after = combat.Current.Allies.First(u => u.Character.Id == starter);
            Assert.Greater(after.MaxHp, hpLv1, Where("HP đồng minh tăng sau nâng cấp"));
            var expected = GrowthStats.Get(after.Character, ServiceLocator.Get<StarService>().GetStarUps(starter), 2,
                ServiceLocator.Get<StarConfig>().StatPercentPerStar, config.StatPercentPerLevel);
            Assert.AreEqual(expected.HP, after.MaxHp, Where("HP đúng công thức"));

            _step = "F5 Reset xoá hết";
            Assert.AreEqual(OnlineStatus.Ok, ServiceLocator.Get<GameResetter>().ResetAsync().Result, Where("Reset"));
            Assert.AreEqual(0, session.Save.PlayerExp, Where("EXP sau Reset"));
            Assert.AreEqual(0, session.Save.CharacterLevels.Count, Where("cấp nhân vật sau Reset"));
            Assert.AreEqual(0, session.Wallet.Get(CurrencyType.ExpPotion), Where("thuốc sau Reset"));
        }
    }
}
