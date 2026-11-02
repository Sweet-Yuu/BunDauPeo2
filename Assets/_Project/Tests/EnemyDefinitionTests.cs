using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class EnemyDefinitionTests
    {
        private static List<string> Errors(EnemyDefinition enemy, System.Action<UnityEditor.SerializedObject> edit)
        {
            TestData.Edit(enemy, edit);
            return enemy.Validate();
        }

        [Test]
        public void Validate_Valid_NoErrors() => CollectionAssert.IsEmpty(CombatTestData.Enemy().Validate());

        [Test]
        public void Validate_NoSkill_NoErrors() => CollectionAssert.IsEmpty(CombatTestData.Enemy(skill: null).Validate());

        [Test]
        public void Validate_NonPositiveStat_Reported()
        {
            Assert.That(CombatTestData.Enemy(hp: 0).Validate(), Has.Some.StartsWith("Stats.HP"));
            Assert.That(CombatTestData.Enemy(attack: 0).Validate(), Has.Some.StartsWith("Stats.Attack"));
            Assert.That(CombatTestData.Enemy(speed: 0).Validate(), Has.Some.StartsWith("Stats.Speed"));
        }

        [Test]
        public void Validate_NegativeDefense_Reported()
        {
            Assert.That(CombatTestData.Enemy(defense: -1).Validate(), Has.Some.StartsWith("Stats.Defense"));
            Assert.That(CombatTestData.Enemy(defense: 0).Validate(), Has.None.StartsWith("Stats.Defense"));
        }

        [Test]
        public void Validate_MissingBasic_Reported() =>
            Assert.That(Errors(CombatTestData.Enemy(), so => so.FindProperty("_basicAttack").objectReferenceValue = null),
                Has.Some.StartsWith("BasicAttack"));

        [Test]
        public void Validate_SkillWrongKind_Reported()
        {
            var skill = CombatTestData.Skill(SkillKind.Ultimate, energy: 1);
            Assert.That(CombatTestData.Enemy(skill: skill).Validate(), Has.Some.StartsWith("Skill.Kind"));
        }

        [Test]
        public void Validate_SkillNotDamage_Reported()
        {
            var heal = CombatTestData.Skill(SkillKind.Skill, SkillTarget.Self, SkillEffect.Heal, cooldown: 3);
            Assert.That(CombatTestData.Enemy(skill: heal).Validate(), Has.Some.StartsWith("Skill.Effect"));
        }

        [Test]
        public void Validate_SkillBreaksRules_Reported()
        {
            var noCooldown = CombatTestData.Skill(SkillKind.Skill, cooldown: 0);
            Assert.That(CombatTestData.Enemy(skill: noCooldown).Validate(), Has.Some.StartsWith("Skill.Cooldown"));
        }
    }
}
