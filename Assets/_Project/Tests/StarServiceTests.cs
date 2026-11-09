using System.Reflection;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class StarServiceTests
    {
        private CharacterDefinition _a;     // R, 1★, HP 800 / Atk 80 / Def 40 / Spd 100
        private CharacterDefinition _b;     // SR, 2★, HP 1000 / Atk 110 / Def 50 / Spd 110
        private CharacterDefinition _c;     // trong catalog nhưng chưa sở hữu
        private CharacterCatalog _catalog;
        private PlayerSave _save;
        private Roster _roster;
        private StarConfig _config;
        private int _changed;
        private int _persistCalls;
        private bool _persistResult;

        [SetUp]
        public void SetUp()
        {
            _a = Stats(TestData.Character("a", Rarity.R), 800, 80, 40, 100);
            _b = Stats(TestData.Character("b", Rarity.SR), 1000, 110, 50, 110);
            TestData.Edit(_b, so => so.FindProperty("_stars").intValue = 2);
            _c = TestData.Character("c", Rarity.R);
            _catalog = TestData.Catalog(_a, _b, _c);
            _save = new PlayerSave();
            _roster = new Roster(_save, _catalog);
            _roster.Add("a");
            _roster.Add("b");
            _config = ScriptableObject.CreateInstance<StarConfig>();
            _persistResult = true;
            _persistCalls = 0;
            _changed = 0;
            _roster.Changed += () => _changed++;
        }

        private static CharacterDefinition Stats(CharacterDefinition character, int hp, int attack, int defense, int speed)
        {
            TestData.Edit(character, so =>
            {
                var stats = so.FindProperty("_combatStats");
                stats.FindPropertyRelative("HP").intValue = hp;
                stats.FindPropertyRelative("Attack").intValue = attack;
                stats.FindPropertyRelative("Defense").intValue = defense;
                stats.FindPropertyRelative("Speed").intValue = speed;
            });
            return character;
        }

        // Tạo sau khi test đã sửa config: IsConfigValid chỉ tính một lần ở constructor.
        private StarService Service() => new StarService(_save, _roster, _catalog, _config, () =>
        {
            _persistCalls++;
            return _persistResult;
        });

        private void Give(string id, long fragments)
        {
            _roster.AddFragments(id, fragments);
            _changed = 0;
        }

        private void SetStarUps(string id, int count) => _save.StarUps.Add(new StarUpEntry { CharacterId = id, Count = count });

        private void BreakCosts() => typeof(StarConfig)
            .GetField("_costs", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_config, null);

        // Từ chối thì save (mảnh, StarUps), Changed và persist đều không đổi.
        private void AssertRejected(string id, StarUpStatus expected, int expectedStars)
        {
            var before = JsonUtility.ToJson(_save);
            var service = Service();
            var result = service.TryStarUp(id);
            Assert.AreEqual(expected, result.Status);
            Assert.AreEqual(expectedStars, result.StarsAfter);
            Assert.AreEqual(before, JsonUtility.ToJson(_save));
            Assert.AreEqual(0, _changed);
            Assert.AreEqual(0, _persistCalls);
        }

        [Test]
        public void GetStars_NoEntry_IsStartingStars()
        {
            var service = Service();
            Assert.AreEqual(1, service.GetStars("a"));
            Assert.AreEqual(2, service.GetStars("b"));
            Assert.AreEqual(0, service.GetStarUps("a"));
        }

        [Test]
        public void GetStars_NegativeCount_TreatedAsZero()
        {
            SetStarUps("a", -3);
            var service = Service();
            Assert.AreEqual(1, service.GetStars("a"));
            Assert.AreEqual(0, service.GetStarUps("a"));
        }

        [Test]
        public void GetStars_CountBeyondMax_ClampedAtFive()
        {
            SetStarUps("a", 9);
            var service = Service();
            Assert.AreEqual(5, service.GetStars("a"));
            Assert.AreEqual(4, service.GetStarUps("a"));
        }

        [Test]
        public void GetStars_UnknownId_IsZero()
        {
            var service = Service();
            Assert.AreEqual(0, service.GetStars("ghost"));
            Assert.AreEqual(0, service.GetStars(null));
            Assert.AreEqual(0, service.GetStarUps("ghost"));
            Assert.AreEqual(0, service.GetStarUps(null));
        }

        [Test]
        public void GetNextCost_ByTargetStar()
        {
            var service = Service();
            Assert.AreEqual(2, service.GetNextCost("a"));

            _save.StarUps.Clear();
            SetStarUps("a", 3);
            Assert.AreEqual(8, service.GetNextCost("a"));

            _save.StarUps.Clear();
            SetStarUps("a", 4);
            Assert.AreEqual(0, service.GetNextCost("a"));
            Assert.AreEqual(0, service.GetNextCost("ghost"));
        }

        [Test]
        public void TryStarUp_Success_SpendsCostAddsStarSavesOnce()
        {
            Give("a", 3);
            var result = Service().TryStarUp("a");

            Assert.AreEqual(StarUpStatus.Success, result.Status);
            Assert.AreEqual(2, result.StarsAfter);
            Assert.IsTrue(result.Saved);
            Assert.AreEqual(1, _roster.GetFragments("a"));
            Assert.AreEqual(1, _save.StarUps.Count);
            Assert.AreEqual("a", _save.StarUps[0].CharacterId);
            Assert.AreEqual(1, _save.StarUps[0].Count);
            Assert.AreEqual(1, _changed);
            Assert.AreEqual(1, _persistCalls);
        }

        [Test]
        public void TryStarUp_InvalidConfig_ChangesNothing()
        {
            Give("a", 5);
            BreakCosts();
            AssertRejected("a", StarUpStatus.InvalidConfig, 1);
            Assert.AreEqual(0, Service().GetNextCost("a"));
        }

        [Test]
        public void TryStarUp_StartingStarsBelowOne_RejectedNotFree()
        {
            // SO chưa chạy Setup Project có _stars = 0: giá bậc kế bằng 0, không được nâng miễn phí.
            TestData.Edit(_a, so => so.FindProperty("_stars").intValue = 0);
            Give("a", 5);
            AssertRejected("a", StarUpStatus.InvalidConfig, 0);
            Assert.AreEqual(5, _roster.GetFragments("a"));
        }

        [Test]
        public void TryStarUp_UnknownId_ChangesNothing()
        {
            Give("a", 5);
            AssertRejected("ghost", StarUpStatus.Unknown, 0);
        }

        [Test]
        public void TryStarUp_NotOwned_ChangesNothing()
        {
            // Mảnh được ghi thẳng vào save để chỉ còn bước kiểm sở hữu chặn lại.
            _save.Fragments.Add(new FragmentCount { CharacterId = "c", Count = 10 });
            AssertRejected("c", StarUpStatus.NotOwned, 1);
        }

        [Test]
        public void TryStarUp_AtFiveStars_ChangesNothing()
        {
            Give("a", 20);
            SetStarUps("a", 4);
            AssertRejected("a", StarUpStatus.MaxStars, 5);
        }

        [Test]
        public void TryStarUp_NotEnoughFragments_ChangesNothing()
        {
            Give("a", 1);
            AssertRejected("a", StarUpStatus.NotEnoughFragments, 1);
        }

        [Test]
        public void TryStarUp_FromOneToFive_CostsTwentyThenMaxStars()
        {
            Give("a", 20);
            var service = Service();
            for (var expected = 2; expected <= 5; expected++)
            {
                var result = service.TryStarUp("a");
                Assert.AreEqual(StarUpStatus.Success, result.Status, $"to {expected}★");
                Assert.AreEqual(expected, result.StarsAfter);
            }

            Assert.AreEqual(StarUpStatus.MaxStars, service.TryStarUp("a").Status);
            Assert.AreEqual(0, _roster.GetFragments("a"));
            Assert.AreEqual(4, _save.StarUps[0].Count);
        }

        [Test]
        public void TryStarUp_PersistFails_SavedFalseButApplied()
        {
            Give("a", 3);
            _persistResult = false;
            var result = Service().TryStarUp("a");

            Assert.AreEqual(StarUpStatus.Success, result.Status);
            Assert.IsFalse(result.Saved);
            Assert.AreEqual(2, result.StarsAfter);
            Assert.AreEqual(1, _roster.GetFragments("a"));
            Assert.AreEqual(1, _persistCalls);
        }

        [Test]
        public void TryStarUp_NegativeCountEntry_ResetsToOne()
        {
            SetStarUps("a", -2);
            Give("a", 2);
            var result = Service().TryStarUp("a");

            Assert.AreEqual(StarUpStatus.Success, result.Status);
            Assert.AreEqual(2, result.StarsAfter);
            Assert.AreEqual(1, _save.StarUps.Count);
            Assert.AreEqual(1, _save.StarUps[0].Count);
        }

        [Test]
        public void GetCombatStats_ScalesHpAttackDefenseNotSpeed()
        {
            SetStarUps("a", 4);
            SetStarUps("b", 1);
            var service = Service();

            var a = service.GetCombatStats(_a);
            Assert.AreEqual(1120, a.HP);
            Assert.AreEqual(112, a.Attack);
            Assert.AreEqual(56, a.Defense);
            Assert.AreEqual(100, a.Speed);

            var b = service.GetCombatStats(_b);
            Assert.AreEqual(1100, b.HP);
            Assert.AreEqual(121, b.Attack);
            Assert.AreEqual(55, b.Defense);
            Assert.AreEqual(110, b.Speed);
        }

        [Test]
        public void GetCombatStats_RoundsHalfUp()
        {
            Stats(_a, 805, 15, 5, 100);
            SetStarUps("a", 1);
            var stats = Service().GetCombatStats(_a);
            Assert.AreEqual(886, stats.HP);
            Assert.AreEqual(17, stats.Attack);
            Assert.AreEqual(6, stats.Defense);
        }

        [Test]
        public void GetCombatStats_InvalidConfig_ReturnsBase()
        {
            BreakCosts();
            SetStarUps("a", 2);
            var stats = Service().GetCombatStats(_a);
            Assert.AreEqual(800, stats.HP);
            Assert.AreEqual(80, stats.Attack);
            Assert.AreEqual(40, stats.Defense);
            Assert.AreEqual(100, stats.Speed);
        }

        [Test]
        public void GetCombatStats_StartRaisedAfterStarUps_UsesEffectiveStarUps()
        {
            SetStarUps("a", 3);
            TestData.Edit(_a, so => so.FindProperty("_stars").intValue = 3);
            var service = Service();

            Assert.AreEqual(5, service.GetStars("a"));
            Assert.AreEqual(2, service.GetStarUps("a"));
            Assert.AreEqual(960, service.GetCombatStats(_a).HP);
        }
    }
}
