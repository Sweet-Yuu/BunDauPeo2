using System;
using System.IO;
using BunDauPeo.Boot;
using BunDauPeo.Core;
using BunDauPeo.EditorTools;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class ResetSaveDataTests
    {
        private string _dir;
        private string _path;
        private SaveService _service;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_dir);
            _path = Path.Combine(_dir, "save.json");
            _service = new SaveService(_path);
            _service.Save(new PlayerSave());
        }

        [TearDown]
        public void TearDown()
        {
            // Run_Confirmed_WithLiveSession...: ResetToDefaults đưa GameLanguage.Current về En.
            GameLanguage.Current = Language.Vi;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void Run_NotConfirmed_DeletesNothing()
        {
            var asked = false;
            Assert.IsFalse(ResetSaveData.Run(() => { asked = true; return false; }, _service, null));
            Assert.IsTrue(asked);
            Assert.IsTrue(File.Exists(_path));
        }

        [Test]
        public void Run_Confirmed_DeletesTheSave()
        {
            Assert.IsTrue(ResetSaveData.Run(() => true, _service, null));
            Assert.IsFalse(File.Exists(_path));
        }

        [Test]
        public void Run_Confirmed_WithLiveSession_ResetsItToDefaults()
        {
            var a = TestData.Character("a");
            var config = TestData.Config(new[] { new CurrencyAmount(CurrencyType.Coin, 500) }, a);
            var live = new GameSession(_service, TestData.Catalog(a), config);
            live.Wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 200));

            Assert.IsTrue(ResetSaveData.Run(() => true, _service, live));

            Assert.AreEqual(500, live.Wallet.Get(CurrencyType.Coin));
        }

        [Test]
        public void Reset_ClearsPlayerExpCharacterLevelsPotionAndAfkRemainder()
        {
            var a = TestData.Character("a");
            var config = TestData.Config(new[] { new CurrencyAmount(CurrencyType.Coin, 500) }, a);
            var live = new GameSession(_service, TestData.Catalog(a), config);
            live.Save.PlayerExp = 77;
            live.Save.CharacterLevels.Add(new CharacterLevelEntry { CharacterId = "a", Level = 5 });
            live.Save.AfkPotionRemainderCoin = 123;
            live.Wallet.Add(CurrencyType.ExpPotion, 9);

            Assert.IsTrue(ResetSaveData.Run(() => true, _service, live));

            Assert.AreEqual(0, live.Save.PlayerExp);
            Assert.AreEqual(0, live.Save.CharacterLevels.Count);
            Assert.AreEqual(0, live.Save.AfkPotionRemainderCoin);
            Assert.AreEqual(0, live.Wallet.Get(CurrencyType.ExpPotion));
        }

        // Review O1 mục 1: chế độ UGS mà chưa có session (chưa Play, hay còn ở Tiêu đề) không với tới save trên server; chỉ xoá bản đệm
        // thì Play lại tiến độ về như cũ (D-005). Từ chối, báo cần vào game, không hỏi và không xoá gì.
        [Test]
        public void Run_UgsWithoutSession_RefusesAndDeletesNothing()
        {
            var asked = false;
            var told = 0;
            Assert.IsFalse(ResetSaveData.Run(() => { asked = true; return true; }, _service, null, useUgs: true, needsPlay: () => told++));
            Assert.IsFalse(asked, "nothing to confirm");
            Assert.AreEqual(1, told, "tells the user to enter the game first");
            Assert.IsTrue(File.Exists(_path), "the cache is kept");
        }
    }
}
