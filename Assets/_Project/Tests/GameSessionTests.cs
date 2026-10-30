using System;
using System.IO;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using NUnit.Framework;

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
        public void ConfigWithBadEntries_DoesNotThrow()
        {
            var config = TestData.Config(new[] { new CurrencyAmount(CurrencyType.Coin, -5) }, new CharacterDefinition[] { null });
            var session = new GameSession(_service, _catalog, config);
            Assert.AreEqual(0, session.Wallet.Get(CurrencyType.Coin));
        }
    }
}
