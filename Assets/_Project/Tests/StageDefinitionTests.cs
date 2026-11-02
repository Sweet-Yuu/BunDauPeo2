using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class StageDefinitionTests
    {
        private static StageDefinition Battle() => CombatTestData.Stage("st", new[] { CombatTestData.Enemy() });

        private static EnemyDefinition[] Enemies(int count) =>
            Enumerable.Range(0, count).Select(_ => CombatTestData.Enemy()).ToArray();

        private static List<string> Edited(StageDefinition stage, System.Action<UnityEditor.SerializedObject> edit)
        {
            TestData.Edit(stage, edit);
            return stage.Validate();
        }

        private static CurrencyAmount Amount(CurrencyType type, long amount) => new CurrencyAmount(type, amount);

        [Test]
        public void Validate_ValidBattle_NoErrors() => CollectionAssert.IsEmpty(Battle().Validate());

        [Test]
        public void Validate_ValidStory_NoErrors() => CollectionAssert.IsEmpty(CombatTestData.Story("s").Validate());

        [Test]
        public void Validate_BlankId_Reported()
        {
            foreach (var id in new[] { "", "  " })
                Assert.That(CombatTestData.Stage(id, Enemies(1)).Validate(), Has.Some.StartsWith("Id"), $"id '{id}'");
        }

        [Test]
        public void Validate_BattleWithoutWaves_Reported() =>
            Assert.That(CombatTestData.Stage("s").Validate(), Has.Some.StartsWith("Waves"));

        [Test]
        public void Validate_EmptyWave_Reported() =>
            Assert.That(CombatTestData.Stage("s", Enemies(1), Enemies(0)).Validate(), Has.Some.StartsWith("Waves[1]"));

        [Test]
        public void Validate_WaveOverFiveEnemies_Reported()
        {
            Assert.That(CombatTestData.Stage("s", Enemies(5)).Validate(), Has.None.StartsWith("Waves"));
            Assert.That(CombatTestData.Stage("s", Enemies(6)).Validate(), Has.Some.StartsWith("Waves[0]"));
        }

        [Test]
        public void Validate_NullEnemy_Reported() =>
            Assert.That(CombatTestData.Stage("s", new[] { CombatTestData.Enemy(), null }).Validate(),
                Has.Some.StartsWith("Waves[0].Enemies[1]"));

        [Test]
        public void Validate_BattleStaminaBelowOne_Reported()
        {
            foreach (var cost in new[] { 0, -1 })
                Assert.That(Edited(Battle(), so => so.FindProperty("_staminaCost").intValue = cost),
                    Has.Some.StartsWith("StaminaCost"), $"cost {cost}");
            Assert.That(Edited(Battle(), so => so.FindProperty("_staminaCost").intValue = 1), Has.None.StartsWith("StaminaCost"));
        }

        [Test]
        public void Validate_StoryWithStamina_Reported()
        {
            foreach (var cost in new[] { 1, -1 })
                Assert.That(Edited(CombatTestData.Story("s"), so => so.FindProperty("_staminaCost").intValue = cost),
                    Has.Some.StartsWith("StaminaCost"), $"cost {cost}");
        }

        [Test]
        public void Validate_BattleMissingFirstClearReward_Reported() =>
            Assert.That(Edited(Battle(), so => so.FindProperty("_firstClearReward").arraySize = 0),
                Has.Some.StartsWith("FirstClearReward"));

        [Test]
        public void Validate_RewardLineNonPositive_Reported()
        {
            foreach (var (field, name) in new[] { ("_firstClearReward", "FirstClearReward"), ("_repeatReward", "RepeatReward") })
            foreach (var bad in new long[] { 0, -1 })
            {
                var errors = Edited(Battle(), so => CombatTestData.SetRewards(so.FindProperty(field),
                    Amount(CurrencyType.Material, 5), Amount(CurrencyType.Gem, bad)));
                Assert.That(errors, Has.Some.StartsWith($"{name}[1]"), $"{name} second line {bad}");
                Assert.That(errors, Has.None.StartsWith($"{name}[0]"), $"{name} first line is fine");
            }
        }

        [Test]
        public void Validate_RewardWithCoin_Reported()
        {
            foreach (var (field, name) in new[] { ("_firstClearReward", "FirstClearReward"), ("_repeatReward", "RepeatReward") })
            {
                var errors = Edited(Battle(), so => CombatTestData.SetRewards(so.FindProperty(field),
                    Amount(CurrencyType.Material, 5), Amount(CurrencyType.Coin, 5)));
                Assert.That(errors, Has.Some.StartsWith($"{name}[1]"), name);
            }
        }

        [Test]
        public void Validate_StoryWithWavesOrReward_Reported()
        {
            var withWave = CombatTestData.Story("s");
            TestData.Edit(withWave, so =>
            {
                so.FindProperty("_waves").arraySize = 1;
                var enemies = so.FindProperty("_waves").GetArrayElementAtIndex(0).FindPropertyRelative("_enemies");
                enemies.arraySize = 1;
                enemies.GetArrayElementAtIndex(0).objectReferenceValue = CombatTestData.Enemy();
            });
            Assert.That(withWave.Validate(), Has.Some.StartsWith("Waves"));

            Assert.That(Edited(CombatTestData.Story("s"), so => CombatTestData.SetRewards(so.FindProperty("_firstClearReward"),
                Amount(CurrencyType.Gem, 5))), Has.Some.StartsWith("FirstClearReward"));
            Assert.That(Edited(CombatTestData.Story("s"), so => CombatTestData.SetRewards(so.FindProperty("_repeatReward"),
                Amount(CurrencyType.Gem, 5))), Has.Some.StartsWith("RepeatReward"));
        }

        [Test]
        public void Validate_BrokenEnemy_Reported() =>
            Assert.That(CombatTestData.Stage("s", new[] { CombatTestData.Enemy(), CombatTestData.Enemy(hp: 0) }).Validate(),
                Has.Some.StartsWith("Waves[0].Enemies[1]: Stats.HP"));
    }
}
