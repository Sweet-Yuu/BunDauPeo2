using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class GrowthStatsTests
    {
        private static CharacterDefinition Make(int hp, int atk, int def, int spd)
        {
            var c = TestData.Character("x", Rarity.SR);
            TestData.Edit(c, so =>
            {
                var stats = so.FindProperty("_combatStats");
                stats.FindPropertyRelative("HP").intValue = hp;
                stats.FindPropertyRelative("Attack").intValue = atk;
                stats.FindPropertyRelative("Defense").intValue = def;
                stats.FindPropertyRelative("Speed").intValue = spd;
            });
            return c;
        }

        [Test]
        public void Level1NoStar_ReturnsBase()
        {
            var c = Make(1000, 110, 50, 110);
            var s = GrowthStats.Get(c, 0, 1, 10, 2);
            Assert.AreEqual((1000, 110, 50, 110), (s.HP, s.Attack, s.Defense, s.Speed));
        }

        [Test]
        public void Level1_MatchesStarServiceFormula()
        {
            var c = Make(1000, 110, 50, 110);
            var config = ScriptableObject.CreateInstance<StarConfig>();
            var catalog = TestData.Catalog(c);
            var save = new BunDauPeo.Core.PlayerSave();
            var stars = ServerRig.IdleStars(save, new Roster(save, catalog), catalog, config);
            for (var n = 0; n <= 4; n++)
            {
                var expected = stars.GetCombatStats(c, n);
                var actual = GrowthStats.Get(c, n, 1, config.StatPercentPerStar, 2);
                Assert.AreEqual((expected.HP, expected.Attack, expected.Defense, expected.Speed),
                    (actual.HP, actual.Attack, actual.Defense, actual.Speed), $"starUps {n}");
            }
        }

        [Test]
        public void MamTomOneStarUpLevel5_AddsBothPercents()
        {
            var c = Make(1000, 110, 50, 110);
            var s = GrowthStats.Get(c, 1, 5, 10, 2);
            Assert.AreEqual((1180, 130, 59, 110), (s.HP, s.Attack, s.Defense, s.Speed));
        }

        [Test]
        public void RoundsHalfUp()
        {
            var c = Make(5, 5, 5, 5);
            Assert.AreEqual(6, GrowthStats.Get(c, 1, 1, 10, 2).HP);
        }

        [Test]
        public void Speed_NeverChanges()
        {
            var c = Make(1000, 100, 100, 77);
            Assert.AreEqual(77, GrowthStats.Get(c, 4, 20, 10, 2).Speed);
        }

        [Test]
        public void NegativeInputs_ClampedToBase()
        {
            var c = Make(1000, 110, 50, 110);
            var s = GrowthStats.Get(c, -3, -5, 10, 2);
            Assert.AreEqual((1000, 110, 50, 110), (s.HP, s.Attack, s.Defense, s.Speed));
        }
    }
}
