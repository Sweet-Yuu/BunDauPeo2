using System;
using System.IO;
using System.Text.RegularExpressions;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Core;
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

            Assert.IsTrue(session.ResetToDefaults());

            Assert.AreEqual(0, session.Wallet.Get(CurrencyType.Gem));
            Assert.IsFalse(session.Roster.IsOwned("ghost"));
            Assert.AreEqual(0, session.Save.BattleTeam.Count);
            Assert.AreEqual(0, session.Save.Staff.Count);
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
        public void ConfigWithBadEntries_DoesNotThrow()
        {
            var config = TestData.Config(new[] { new CurrencyAmount(CurrencyType.Coin, -5) }, new CharacterDefinition[] { null });
            var session = new GameSession(_service, _catalog, config);
            Assert.AreEqual(0, session.Wallet.Get(CurrencyType.Coin));
        }
    }
}
