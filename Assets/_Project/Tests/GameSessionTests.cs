using System;
using System.IO;
using System.Text.RegularExpressions;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.Tests
{
    public class GameSessionTests
    {
        private string _dir;
        private string _path;
        private SaveService _service;
        private CharacterCatalog _catalog;
        private NewGameConfig _config;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_dir);
            _path = Path.Combine(_dir, "save.json");
            _service = new SaveService(_path);
            var a = TestData.Character("a");
            _catalog = TestData.Catalog(a);
            _config = TestData.Config(new[] { new CurrencyAmount(CurrencyType.Coin, 500) }, a);
        }

        [TearDown]
        public void TearDown()
        {
            GameLanguage.Current = Language.Vi;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void NewGame_GrantsStarter_AndWritesFile()
        {
            var session = new GameSession(_service, _catalog, _config);
            Assert.IsTrue(session.IsNewGame);
            Assert.AreEqual(500, session.Wallet.Get(CurrencyType.Coin));
            Assert.IsTrue(session.Roster.IsOwned("a"));
            Assert.IsTrue(File.Exists(_path));
        }

        [Test]
        public void Reopen_DoesNotGrantStarterAgain()
        {
            var first = new GameSession(_service, _catalog, _config);
            first.Wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 100));
            first.Persist();
            var second = new GameSession(_service, _catalog, _config);
            Assert.IsFalse(second.IsNewGame);
            Assert.AreEqual(400, second.Wallet.Get(CurrencyType.Coin));
        }

        [Test]
        public void CorruptSave_StartsNewGame_AndKeepsCopy()
        {
            File.WriteAllText(_path, "{broken");
            var session = new GameSession(_service, _catalog, _config);
            Assert.IsTrue(session.IsNewGame);
            Assert.IsTrue(File.Exists(_path + ".corrupt"));
        }

        [Test]
        public void ResetToDefaults_RestoresNewGameState_AndPersistsIt()
        {
            var session = new GameSession(_service, _catalog, _config);
            session.Wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 200));
            session.Roster.AddToTeam("a");

            Assert.IsTrue(session.ResetToDefaults());

            Assert.AreEqual(500, session.Wallet.Get(CurrencyType.Coin));
            CollectionAssert.IsEmpty(session.Roster.GetBattleTeam());
            Assert.IsTrue(session.Roster.IsOwned("a"));
            var reopened = new GameSession(_service, _catalog, _config);
            Assert.IsFalse(reopened.IsNewGame);
            Assert.AreEqual(500, reopened.Wallet.Get(CurrencyType.Coin));
            CollectionAssert.IsEmpty(reopened.Roster.GetBattleTeam());
        }

        [Test]
        public void ResetToDefaults_ClearsEveryFieldOfTheSave()
        {
            // Dữ liệu mà NewGameConfig không cấp (Gem, Id lạ, nhân viên) cũng phải biến mất: reset ghi đè cả PlayerSave chứ không xoá từng field.
            var session = new GameSession(_service, _catalog, _config);
            session.Save.Balances.Add(new CurrencyAmount(CurrencyType.Gem, 99));
            session.Save.OwnedCharacterIds.Add("ghost");
            session.Save.BattleTeam.Add("ghost");
            session.Save.Staff.Add(new StaffAssignment { CharacterId = "ghost", Role = StaffRole.Cook });
            session.Save.Fragments.Add(new FragmentCount { CharacterId = "a", Count = 3 });
            session.Save.GachaPity.Add(new PityEntry { BannerId = "b", Pulls = 7 });
            session.Save.GachaHistory.Add(new GachaHistoryEntry { BannerId = "b", Count = 1, CharacterIds = new[] { "a" } });
            session.Save.Language = "en";
            session.Save.StarUps.Add(new StarUpEntry { CharacterId = "a", Count = 3 });

            Assert.IsTrue(session.ResetToDefaults());

            Assert.AreEqual(0, session.Save.StarUps.Count);
            Assert.AreEqual(0, session.Wallet.Get(CurrencyType.Gem));
            Assert.IsFalse(session.Roster.IsOwned("ghost"));
            Assert.AreEqual(0, session.Save.BattleTeam.Count);
            Assert.AreEqual(0, session.Save.Staff.Count);
            Assert.AreEqual(0, session.Save.Fragments.Count);
            Assert.AreEqual(0, session.Save.GachaPity.Count);
            Assert.AreEqual(0, session.Save.GachaHistory.Count);
            Assert.AreEqual(0, session.Roster.GetFragments("a"));
            Assert.AreEqual("", session.Save.Language);
        }

        [Test]
        public void ResetToDefaults_ClearsRestaurantFields()
        {
            var session = new GameSession(_service, _catalog, _config);
            session.Save.Upgrades.Add(new UpgradeLevel { UpgradeId = "up_tables", Level = 2 });
            session.Save.DailyPresence.Add(new DailyPresenceEntry { DayIndex = 7300, Seconds = 120.5 });
            session.Save.IncidentScores.Add(2);
            session.Save.Afk.LastActiveUtcTicks = 99;
            session.Save.Afk.RatePerSecond = 1.5;
            session.Save.Afk.AccumulatedSeconds = 60;
            session.Save.Afk.PendingTheoretical = 90;

            Assert.IsTrue(session.ResetToDefaults());

            Assert.AreEqual(0, session.Save.Upgrades.Count);
            Assert.AreEqual(0, session.Save.DailyPresence.Count);
            Assert.AreEqual(0, session.Save.IncidentScores.Count);
            Assert.IsNotNull(session.Save.Afk);
            Assert.AreEqual(0, session.Save.Afk.LastActiveUtcTicks);
            Assert.AreEqual(0, session.Save.Afk.RatePerSecond);
            Assert.AreEqual(0, session.Save.Afk.AccumulatedSeconds);
            Assert.AreEqual(0, session.Save.Afk.PendingTheoretical);
        }

        [Test]
        public void ResetToDefaults_RaisesResetCompletedOnce_AfterWalletAndRosterChanged_BeforeFileWritten()
        {
            var session = new GameSession(_service, _catalog, _config);
            var events = new System.Collections.Generic.List<string>();
            var fileExistedInHandler = true;
            session.Wallet.Changed += () => events.Add("wallet");
            session.Roster.Changed += () => events.Add("roster");
            session.ResetCompleted += () =>
            {
                events.Add("reset");
                fileExistedInHandler = File.Exists(_path);
            };

            Assert.IsTrue(session.ResetToDefaults());

            Assert.AreEqual(1, events.FindAll(e => e == "reset").Count);
            Assert.AreEqual("reset", events[events.Count - 1]);
            Assert.GreaterOrEqual(events.IndexOf("wallet"), 0);
            Assert.GreaterOrEqual(events.IndexOf("roster"), 0);
            Assert.IsFalse(fileExistedInHandler);
            Assert.IsTrue(File.Exists(_path));
        }

        [Test]
        public void ResetCompleted_HandlerThrows_StillPersistsAndCallsOtherHandlers()
        {
            var session = new GameSession(_service, _catalog, _config);
            var secondCalled = false;
            session.ResetCompleted += () => throw new InvalidOperationException("boom");
            session.ResetCompleted += () => secondCalled = true;
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            Assert.IsTrue(session.ResetToDefaults());

            Assert.IsTrue(secondCalled);
            Assert.IsTrue(File.Exists(_path));
        }

        [Test]
        public void GachaService_AfterReset_SeesClearedPityAndFragments()
        {
            // Nhân vật khởi đầu `a` là R duy nhất của banner, nên mọi lượt quay đều là mảnh của `a`.
            var session = new GameSession(_service, _catalog, _config);
            _catalog.TryGet("a", out var a);
            var banner = TestData.Banner("b", a);
            TestData.Edit(banner, so =>
            {
                var weights = so.FindProperty("_rarityWeights");
                weights.FindPropertyRelative("R").floatValue = 100;
                weights.FindPropertyRelative("SR").floatValue = 0;
                weights.FindPropertyRelative("SSR").floatValue = 0;
                so.FindProperty("_ssrPityThreshold").intValue = 0;
                so.FindProperty("_guaranteeSrInTen").boolValue = false;
            });
            session.Wallet.Add(CurrencyType.Gem, 1000);
            var gacha = new GachaService(session.Save, session.Wallet, session.Roster, _catalog,
                TestData.GachaCatalog(banner), new ScriptedRandom(), session.Persist);

            gacha.PullSingle("b");
            gacha.PullSingle("b");
            Assert.AreEqual(2, gacha.GetPity("b"));
            Assert.AreEqual(2, session.Roster.GetFragments("a"));

            var rosterChanged = 0;
            session.Roster.Changed += () => rosterChanged++;
            Assert.IsTrue(session.ResetToDefaults());

            Assert.AreEqual(0, gacha.GetPity("b"));
            Assert.AreEqual(0, session.Roster.GetFragments("a"));
            Assert.GreaterOrEqual(rosterChanged, 1);
        }

        [Test]
        public void ResetToDefaults_RemovesBackups_SoNothingComesBack()
        {
            var session = new GameSession(_service, _catalog, _config);
            session.Wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 100));
            session.Persist();
            File.WriteAllText(_path + ".corrupt", "x");

            Assert.IsTrue(session.ResetToDefaults());

            Assert.IsFalse(File.Exists(_path + ".bak"));
            Assert.IsFalse(File.Exists(_path + ".corrupt"));
            File.Delete(_path);
            Assert.IsNull(_service.Load());
        }

        [Test]
        public void ResetToDefaults_NotifiesViews_WithTheFinalState()
        {
            // Cấp đồ ban đầu cũng bắn Changed nên có thể nhiều hơn một lần; thông báo cuối phải thấy trạng thái đã reset xong.
            var session = new GameSession(_service, _catalog, _config);
            session.Wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 200));
            session.Roster.AddToTeam("a");
            long lastCoinSeen = -1;
            var lastTeamSizeSeen = -1;
            session.Wallet.Changed += () => lastCoinSeen = session.Wallet.Get(CurrencyType.Coin);
            session.Roster.Changed += () => lastTeamSizeSeen = session.Roster.GetBattleTeam().Count;

            session.ResetToDefaults();

            Assert.AreEqual(500, lastCoinSeen);
            Assert.AreEqual(0, lastTeamSizeSeen);
        }

        [Test]
        public void ResetToDefaults_WhenFilesLocked_ChangesNothing()
        {
            var session = new GameSession(_service, _catalog, _config);
            session.Wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 200));
            LogAssert.Expect(LogType.Error, new Regex("Reset failed"));
            using (new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.IsFalse(session.ResetToDefaults());
            Assert.AreEqual(300, session.Wallet.Get(CurrencyType.Coin));
        }

        [Test]
        public void SetLanguage_Saved_ChangesSaveFileAndCurrent()
        {
            var session = new GameSession(_service, _catalog, _config);
            Assert.IsTrue(session.SetLanguage(Language.En));
            Assert.AreEqual("en", session.Save.Language);
            Assert.AreEqual(Language.En, GameLanguage.Current);

            var reopened = new GameSession(new SaveService(_path), _catalog, _config);
            Assert.AreEqual("en", reopened.Save.Language, "đã ghi xuống đĩa");
        }

        [Test]
        public void SetLanguage_WhenFileLocked_KeepsOldLanguage()
        {
            var session = new GameSession(_service, _catalog, _config);
            Assert.IsTrue(session.SetLanguage(Language.Vi));
            LogAssert.Expect(LogType.Error, new Regex("Save failed"));
            using (new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.IsFalse(session.SetLanguage(Language.En));
            Assert.AreEqual("vi", session.Save.Language);
            Assert.AreEqual(Language.Vi, GameLanguage.Current);
        }

        [Test]
        public void ApplyLanguage_UsesSavedChoiceElseEnglish()
        {
            var session = new GameSession(_service, _catalog, _config);
            GameLanguage.Current = Language.Vi;
            session.Save.Language = "";
            session.ApplyLanguage();
            Assert.AreEqual(Language.En, GameLanguage.Current, "chưa chọn thì tiếng Anh");
            session.Save.Language = "vi";
            session.ApplyLanguage();
            Assert.AreEqual(Language.Vi, GameLanguage.Current, "đã chọn Vi");
            session.Save.Language = "en";
            session.ApplyLanguage();
            Assert.AreEqual(Language.En, GameLanguage.Current, "đã chọn En");
        }

        // Reset về tiếng Anh, và đổi TRƯỚC ResetCompleted để view nạp lại thấy ngôn ngữ mới.
        [Test]
        public void ResetToDefaults_AfterChoosingVietnamese_IsEnglish()
        {
            var session = new GameSession(_service, _catalog, _config);
            Assert.IsTrue(session.SetLanguage(Language.Vi));
            Assert.AreEqual(Language.Vi, GameLanguage.Current);
            var seenInHandler = new System.Collections.Generic.List<Language>();
            session.ResetCompleted += () => seenInHandler.Add(GameLanguage.Current);

            Assert.IsTrue(session.ResetToDefaults());

            Assert.AreEqual("", session.Save.Language);
            Assert.AreEqual(Language.En, GameLanguage.Current);
            CollectionAssert.AreEqual(new[] { Language.En }, seenInHandler, "ngôn ngữ đã đổi trước khi ResetCompleted chạy");
        }

        [Test]
        public void RestaurantBarOpen_DefaultFalse_PersistsAndResets()
        {
            var session = new GameSession(_service, _catalog, _config);
            Assert.IsFalse(session.Save.RestaurantBarOpen, "game mới: thanh quán đóng");

            Assert.IsTrue(session.SetRestaurantBarOpen(true));
            Assert.IsTrue(session.Save.RestaurantBarOpen);
            var reopened = new GameSession(new SaveService(_path), _catalog, _config);
            Assert.IsTrue(reopened.Save.RestaurantBarOpen, "đã ghi xuống đĩa");

            Assert.IsTrue(session.ResetToDefaults());
            Assert.IsFalse(session.Save.RestaurantBarOpen);
            var afterReset = new GameSession(new SaveService(_path), _catalog, _config);
            Assert.IsFalse(afterReset.Save.RestaurantBarOpen, "file sau Reset cũng false");
        }

        // Khác SetLanguage: cờ giao diện nên lưu lỗi vẫn giữ giá trị mới trong bộ nhớ, lần lưu sau sẽ ghi lại.
        [Test]
        public void SetRestaurantBarOpen_SaveFails_KeepsValueInMemory()
        {
            var session = new GameSession(_service, _catalog, _config);
            LogAssert.Expect(LogType.Error, new Regex("Save failed"));
            using (new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.IsFalse(session.SetRestaurantBarOpen(true));
            Assert.IsTrue(session.Save.RestaurantBarOpen);

            Assert.IsTrue(session.Persist(), "khoá đã nhả, lần lưu sau ghi lại được");
            Assert.IsTrue(new GameSession(new SaveService(_path), _catalog, _config).Save.RestaurantBarOpen);
        }

        [Test]
        public void ConfigWithBadEntries_DoesNotThrow()
        {
            var config = TestData.Config(new[] { new CurrencyAmount(CurrencyType.Coin, -5) }, new CharacterDefinition[] { null });
            var session = new GameSession(_service, _catalog, config);
            Assert.AreEqual(0, session.Wallet.Get(CurrencyType.Coin));
        }

        [Test]
        public void ResetToDefaults_ClearsCombatFields()
        {
            var session = new GameSession(_service, _catalog, _config);
            session.Save.ClearedStages.Add("st_1_1");
            session.Save.Stamina.Current = 5;
            session.Save.Stamina.AnchorUtcTicks = 638_000_000_000_000_000;

            Assert.IsTrue(session.ResetToDefaults());

            Assert.AreEqual(0, session.Save.ClearedStages.Count);
            Assert.IsNotNull(session.Save.Stamina);
            Assert.AreEqual((0, 0L), (session.Save.Stamina.Current, session.Save.Stamina.AnchorUtcTicks), "anchor 0 reads as a full bar");
        }
    }
}
