using BunDauPeo.Characters;
using BunDauPeo.Combat;
using NUnit.Framework;
using UnityEditor;

namespace BunDauPeo.Tests
{
    public class CombatSkillRulesTests
    {
        [Test]
        public void SampleCharacterSkills_PassRules()
        {
            var rows = new[]
            {
                ("BasicStrike", SkillKind.Basic), ("PowerStrike", SkillKind.Skill),
                ("Heal", SkillKind.Skill), ("Finisher", SkillKind.Ultimate),
            };
            foreach (var (file, kind) in rows)
            {
                var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>($"Assets/_Project/Characters/Data/Skills/Skill_{file}.asset");
                Assert.IsNotNull(skill, file);
                CollectionAssert.IsEmpty(CombatSkillRules.Validate(skill, kind), file);
            }
        }

        [Test]
        public void Validate_ValidOfEachKind_NoErrors()
        {
            CollectionAssert.IsEmpty(CombatSkillRules.Validate(CombatTestData.Skill(SkillKind.Basic), SkillKind.Basic));
            CollectionAssert.IsEmpty(CombatSkillRules.Validate(
                CombatTestData.Skill(SkillKind.Skill, SkillTarget.SingleAlly, SkillEffect.Heal, cooldown: 1), SkillKind.Skill));
            CollectionAssert.IsEmpty(CombatSkillRules.Validate(
                CombatTestData.Skill(SkillKind.Ultimate, SkillTarget.AllEnemies, energy: 1), SkillKind.Ultimate));
        }

        [Test]
        public void Validate_NullSkill_Reported() => CollectionAssert.IsNotEmpty(CombatSkillRules.Validate(null, SkillKind.Basic));

        [Test]
        public void Validate_WrongKind_Reported() =>
            Assert.That(CombatSkillRules.Validate(CombatTestData.Skill(SkillKind.Ultimate, energy: 1), SkillKind.Basic),
                Has.Some.StartsWith("Kind"));

        [Test]
        public void Validate_NonPositivePower_Reported()
        {
            foreach (var power in new[] { 0f, -1f })
                Assert.That(CombatSkillRules.Validate(CombatTestData.Skill(SkillKind.Basic, power: power), SkillKind.Basic),
                    Has.Some.StartsWith("Power"), $"power {power}");
        }

        [Test]
        public void Validate_DamageWithAllyTarget_Reported()
        {
            foreach (var target in new[] { SkillTarget.SingleAlly, SkillTarget.AllAllies, SkillTarget.Self })
                Assert.That(CombatSkillRules.Validate(CombatTestData.Skill(SkillKind.Basic, target), SkillKind.Basic),
                    Has.Some.StartsWith("Target"), $"target {target}");
        }

        [Test]
        public void Validate_HealWithEnemyTarget_Reported()
        {
            foreach (var target in new[] { SkillTarget.SingleEnemy, SkillTarget.AllEnemies })
                Assert.That(CombatSkillRules.Validate(
                        CombatTestData.Skill(SkillKind.Skill, target, SkillEffect.Heal, cooldown: 1), SkillKind.Skill),
                    Has.Some.StartsWith("Target"), $"target {target}");
        }

        [Test]
        public void Validate_SkillCooldownBelowOne_Reported()
        {
            Assert.That(CombatSkillRules.Validate(CombatTestData.Skill(SkillKind.Skill, cooldown: 0), SkillKind.Skill),
                Has.Some.StartsWith("Cooldown"));
            Assert.That(CombatSkillRules.Validate(CombatTestData.Skill(SkillKind.Skill, cooldown: 1), SkillKind.Skill),
                Has.None.StartsWith("Cooldown"));
        }

        [Test]
        public void Validate_UltimateEnergyBelowOne_Reported()
        {
            Assert.That(CombatSkillRules.Validate(CombatTestData.Skill(SkillKind.Ultimate, energy: 0), SkillKind.Ultimate),
                Has.Some.StartsWith("EnergyCost"));
            Assert.That(CombatSkillRules.Validate(CombatTestData.Skill(SkillKind.Ultimate, energy: 1), SkillKind.Ultimate),
                Has.None.StartsWith("EnergyCost"));
        }
    }
}
