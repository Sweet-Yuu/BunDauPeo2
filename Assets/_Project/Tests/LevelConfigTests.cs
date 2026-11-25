using System;
using System.Linq;
using BunDauPeo.Characters;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class LevelConfigTests
    {
        private LevelConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<LevelConfig>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_config);

        private void Set(string field, int value) => TestData.Edit(_config, so => so.FindProperty(field).intValue = value);

        [Test]
        public void Defaults_AreValid()
        {
            Assert.IsEmpty(_config.Validate());
            Assert.AreEqual(120, _config.ExpToNext(1));
            Assert.AreEqual(480, _config.ExpToNext(19));
        }

        [Test]
        public void ExpToNext_BelowLevel1_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => _config.ExpToNext(0));

        [Test]
        public void Validate_RejectsMaxPlayerLevelBelowOne()
        {
            Set("_maxPlayerLevel", 0);
            Assert.IsTrue(_config.Validate().Any(e => e.StartsWith("MaxPlayerLevel")));
        }

        [Test]
        public void Validate_RejectsNegativeCostsAndRewards()
        {
            Set("_potionPerTargetLevel", -1);
            Set("_coinPerTargetLevel", -1);
            Set("_levelUpRewardCoinPerLevel", -1);
            Set("_levelUpRewardPotion", -1);
            var errors = _config.Validate();
            foreach (var name in new[] { "PotionPerTargetLevel", "CoinPerTargetLevel", "LevelUpRewardCoinPerLevel", "LevelUpRewardPotion" })
                Assert.IsTrue(errors.Any(e => e.StartsWith(name)), name);
        }

        [Test]
        public void Validate_RejectsChanceOutsideZeroTo100()
        {
            Set("_restaurantPotionChancePercent", -1);
            Assert.IsTrue(_config.Validate().Any(e => e.StartsWith("RestaurantPotionChancePercent")));
            Set("_restaurantPotionChancePercent", 101);
            Assert.IsTrue(_config.Validate().Any(e => e.StartsWith("RestaurantPotionChancePercent")));
        }

        [Test]
        public void Validate_RejectsAfkCoinPerPotionBelowOne()
        {
            Set("_afkCoinPerPotion", 0);
            Assert.IsTrue(_config.Validate().Any(e => e.StartsWith("AfkCoinPerPotion")));
        }
    }
}
