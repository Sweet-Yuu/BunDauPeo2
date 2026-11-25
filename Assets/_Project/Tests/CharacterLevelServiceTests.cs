using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class CharacterLevelServiceTests
    {
        private PlayerSave _save;
        private Wallet _wallet;
        private Roster _roster;
        private LevelConfig _config;
        private CharacterCatalog _catalog;
        private int _persists;
        private bool _persistResult;
        private int _changed;

        [SetUp]
        public void SetUp()
        {
            var a = TestData.Character("a");
            var c = TestData.Character("c");
            _catalog = TestData.Catalog(a, c);
            _save = new PlayerSave();
            _wallet = new Wallet(_save);
            _roster = new Roster(_save, _catalog);
            _roster.Add("a");
            _config = ScriptableObject.CreateInstance<LevelConfig>();
            _persists = 0;
            _changed = 0;
            _persistResult = true;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        // Đặt cấp người chơi bằng cách ghi thẳng tổng EXP.
        private void PlayerAt(int level)
        {
            long total = 0;
            for (var l = 1; l < level; l++) total += _config.ExpToNext(l);
            _save.PlayerExp = total;
        }

        private CharacterLevelService Make()
        {
            var player = new PlayerLevelService(_save, _wallet, _config, () => true);
            var service = new CharacterLevelService(_save, _roster, _wallet, _catalog, _config, player, () => { _persists++; return _persistResult; });
            service.Changed += () => _changed++;
            return service;
        }

        private void Give(long potion, long coin)
        {
            _wallet.Add(CurrencyType.ExpPotion, potion);
            _wallet.Add(CurrencyType.Coin, coin);
        }

        [Test]
        public void TryLevelUp_Success_SpendsPotionAndCoinAndRaisesLevel()
        {
            PlayerAt(2);
            Give(10, 1000);
            var s = Make();
            var r = s.TryLevelUp("a");
            Assert.AreEqual((LevelUpStatus.Success, 2, true), (r.Status, r.LevelAfter, r.Saved));
            Assert.AreEqual(2, s.GetLevel("a"));
            Assert.AreEqual(6, _wallet.Get(CurrencyType.ExpPotion));
            Assert.AreEqual(800, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual((1, 1), (_persists, _changed));
        }

        [Test]
        public void TryLevelUp_UnknownId_Unknown()
        {
            PlayerAt(2);
            Give(10, 1000);
            Assert.AreEqual(LevelUpStatus.Unknown, Make().TryLevelUp("nope").Status);
            Assert.AreEqual(LevelUpStatus.Unknown, Make().TryLevelUp(null).Status);
        }

        [Test]
        public void TryLevelUp_NotOwned_NotOwned()
        {
            PlayerAt(2);
            Give(10, 1000);
            Assert.AreEqual(LevelUpStatus.NotOwned, Make().TryLevelUp("c").Status);
        }

        [Test]
        public void TryLevelUp_AtPlayerLevel_AtPlayerLevel()
        {
            Give(10, 1000);
            Assert.AreEqual(LevelUpStatus.AtPlayerLevel, Make().TryLevelUp("a").Status);
        }

        [Test]
        public void TryLevelUp_NotEnoughPotion()
        {
            PlayerAt(2);
            Give(3, 1000);
            Assert.AreEqual(LevelUpStatus.NotEnoughPotion, Make().TryLevelUp("a").Status);
        }

        [Test]
        public void TryLevelUp_NotEnoughCoin()
        {
            PlayerAt(2);
            Give(10, 199);
            Assert.AreEqual(LevelUpStatus.NotEnoughCoin, Make().TryLevelUp("a").Status);
        }

        [Test]
        public void TryLevelUp_InvalidConfig_InvalidConfig()
        {
            PlayerAt(2);
            Give(10, 1000);
            TestData.Edit(_config, so => so.FindProperty("_afkCoinPerPotion").intValue = 0);
            var s = Make();
            Assert.IsFalse(s.IsConfigValid);
            Assert.AreEqual(LevelUpStatus.InvalidConfig, s.TryLevelUp("a").Status);
        }

        [Test]
        public void Rejections_LeaveWalletSaveAndEventsUntouched()
        {
            PlayerAt(2);
            Give(3, 100);
            var s = Make();
            var walletChanged = 0;
            _wallet.Changed += () => walletChanged++;
            foreach (var id in new[] { "nope", "c", "a" })
                Assert.AreNotEqual(LevelUpStatus.Success, s.TryLevelUp(id).Status, id);   // "a": thiếu thuốc
            Give(10, 0);
            walletChanged = 0;
            Assert.AreEqual(LevelUpStatus.NotEnoughCoin, s.TryLevelUp("a").Status);

            Assert.AreEqual(13, _wallet.Get(CurrencyType.ExpPotion));
            Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
            Assert.IsEmpty(_save.CharacterLevels);
            Assert.AreEqual((0, 0, 0), (_changed, _persists, walletChanged));
        }

        [Test]
        public void PersistFailure_SavedFalse_KeepsMemory()
        {
            PlayerAt(2);
            Give(10, 1000);
            _persistResult = false;
            var s = Make();
            var r = s.TryLevelUp("a");
            Assert.AreEqual((LevelUpStatus.Success, false), (r.Status, r.Saved));
            Assert.AreEqual(2, s.GetLevel("a"));
            Assert.AreEqual(1, _persists);
        }

        [Test]
        public void GetLevel_StoredAbovePlayerLevel_ReturnsEffective()
        {
            PlayerAt(3);
            _save.CharacterLevels.Add(new CharacterLevelEntry { CharacterId = "a", Level = 15 });
            var s = Make();
            Assert.AreEqual(3, s.GetLevel("a"));
            Assert.AreEqual(15, _save.CharacterLevels[0].Level, "stored level is never overwritten");
            Assert.AreEqual(1, s.GetLevel("nope"));
            Assert.AreEqual(1, s.GetLevel(null));
        }

        [Test]
        public void GetNextCost_UsesTargetLevel()
        {
            PlayerAt(10);
            _save.CharacterLevels.Add(new CharacterLevelEntry { CharacterId = "a", Level = 4 });
            Assert.AreEqual((10, 500), Make().GetNextCost("a"));
        }
    }
}
