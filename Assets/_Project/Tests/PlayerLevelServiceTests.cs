using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class PlayerLevelServiceTests
    {
        private PlayerSave _save;
        private Wallet _wallet;
        private LevelConfig _config;
        private int _persists;
        private bool _persistResult;
        private int _changed;

        [SetUp]
        public void SetUp()
        {
            _save = new PlayerSave();
            _wallet = new Wallet(_save);
            _config = ScriptableObject.CreateInstance<LevelConfig>();
            _persists = 0;
            _changed = 0;
            _persistResult = true;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        private PlayerLevelService Make()
        {
            var service = new PlayerLevelService(_save, _wallet, _config, () => { _persists++; return _persistResult; });
            service.Changed += () => _changed++;
            return service;
        }

        [Test]
        public void NewSave_IsLevel1WithZeroExp()
        {
            var s = Make();
            Assert.AreEqual((1, 0L, 120L, false), (s.Level, s.Exp, s.ExpToNext, s.IsMaxLevel));
        }

        [Test]
        public void AddExp_BelowThreshold_KeepsLevel()
        {
            var s = Make();
            s.AddExp(119);
            Assert.AreEqual((1, 119L, 120L), (s.Level, s.Exp, s.ExpToNext));
            Assert.AreEqual(0, _wallet.Get(CurrencyType.Coin));
        }

        [Test]
        public void AddExp_CrossesThreshold_LevelsUpAndGrantsReward()
        {
            var s = Make();
            s.AddExp(120);
            Assert.AreEqual((2, 0L), (s.Level, s.Exp));
            Assert.AreEqual(400, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(5, _wallet.Get(CurrencyType.ExpPotion));
            Assert.AreEqual((1, 1), (_persists, _changed));
        }

        [Test]
        public void AddExp_ManyLevelsAtOnce_GrantsEachLevelsReward()
        {
            var s = Make();
            s.AddExp(120 + 140 + 5);   // Lv1→2→3, 5 dư
            Assert.AreEqual((3, 5L), (s.Level, s.Exp));
            Assert.AreEqual(400 + 600, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(10, _wallet.Get(CurrencyType.ExpPotion));
            Assert.AreEqual((1, 1), (_persists, _changed));
        }

        [Test]
        public void AddExp_AtMaxLevel_DropsExcess()
        {
            var s = Make();
            long total = 0;
            for (var l = 1; l < 20; l++) total += _config.ExpToNext(l);
            s.AddExp(total + 999);
            Assert.IsTrue(s.IsMaxLevel);
            Assert.AreEqual((20, 0L), (s.Level, s.ExpToNext));
            Assert.AreEqual(total, _save.PlayerExp);
            s.AddExp(500);
            Assert.AreEqual(total, _save.PlayerExp);
        }

        [Test]
        public void AddExp_ZeroOrNegative_NoChange()
        {
            var s = Make();
            s.AddExp(0);
            s.AddExp(-5);
            Assert.AreEqual((0, 0, 0L), (_persists, _changed, _save.PlayerExp));
        }

        [Test]
        public void AddExp_LongMaxValue_DoesNotOverflowOrHang()
        {
            var s = Make();
            s.AddExp(long.MaxValue);
            s.AddExp(long.MaxValue);
            Assert.IsTrue(s.IsMaxLevel);
            Assert.GreaterOrEqual(_save.PlayerExp, 0);
        }

        [Test]
        public void InvalidConfig_AddExpIgnored()
        {
            TestData.Edit(_config, so => so.FindProperty("_maxPlayerLevel").intValue = 0);
            var s = Make();
            s.AddExp(500);
            Assert.IsFalse(s.IsConfigValid);
            Assert.AreEqual((0L, 0, 1), (_save.PlayerExp, _persists, s.Level));
        }

        [Test]
        public void PersistFailure_KeepsMemoryState()
        {
            _persistResult = false;
            var s = Make();
            s.AddExp(120);
            Assert.AreEqual(2, s.Level);
            Assert.AreEqual(400, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(1, _persists);
        }
    }
}
