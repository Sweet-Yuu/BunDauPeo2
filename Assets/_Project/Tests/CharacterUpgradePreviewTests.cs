using BunDauPeo.Characters;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class CharacterUpgradePreviewTests
    {
        private LevelConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<LevelConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        // Mắm Tôm gốc HP 1000, Tấn công 110, Thủ 50; một lần nâng sao (+10%), mỗi cấp +2%.
        [Test]
        public void Preview_ShowsBeforeAndAfterStats()
        {
            var c = CombatTestData.Hero("mam_tom", hp: 1000, attack: 110, defense: 50, speed: 95);

            var p = CharacterUpgradePreview.Build(c, 1, 5, 7, _config, 10);

            Assert.IsTrue(p.HasNext);
            Assert.AreEqual(1180, p.Before.HP);
            Assert.AreEqual(1200, p.After.HP);
            Assert.AreEqual(130, p.Before.Attack);
            Assert.AreEqual(132, p.After.Attack);
            Assert.AreEqual(59, p.Before.Defense);
            Assert.AreEqual(60, p.After.Defense);
            Assert.AreEqual(95, p.After.Speed);
        }

        [Test]
        public void Preview_AtMaxLevel_HasNoNext()
        {
            var c = CombatTestData.Hero("mam_tom", hp: 1000, attack: 110, defense: 50, speed: 95);

            var p = CharacterUpgradePreview.Build(c, 1, 7, 7, _config, 10);

            Assert.IsFalse(p.HasNext);
            Assert.AreEqual(p.Before.HP, p.After.HP);
        }
    }
}
