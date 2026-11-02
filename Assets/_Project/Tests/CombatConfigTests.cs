using System;
using System.Collections.Generic;
using BunDauPeo.Combat;
using NUnit.Framework;
using UnityEditor;

namespace BunDauPeo.Tests
{
    public class CombatConfigTests
    {
        // "_frontWeight" → "FrontWeight": lỗi phải bắt đầu bằng tên field PascalCase.
        private static string Pascal(string field) => char.ToUpper(field[1]) + field.Substring(2);

        private static List<string> Validate(Action<SerializedObject> edit) => CombatTestData.Config(edit).Validate();

        [Test]
        public void Defaults_MatchSpecTable()
        {
            var c = CombatTestData.Config();
            Assert.AreEqual(3f, c.FrontWeight);
            Assert.AreEqual(2f, c.MidWeight);
            Assert.AreEqual(1f, c.BackWeight);
            Assert.AreEqual(100, c.MaxEnergy);
            Assert.AreEqual(25, c.EnergyOnBasic);
            Assert.AreEqual(10, c.EnergyOnSkill);
            Assert.AreEqual(15, c.EnergyOnHit);
            Assert.AreEqual(60, c.MaxRounds);
            Assert.AreEqual(0.7f, c.AutoHealBelow);
            Assert.AreEqual(60, c.MaxStamina);
            Assert.AreEqual(300, c.StaminaRegenSeconds);
            Assert.AreEqual(3f, c.Weight(BattlePosition.Front));
            Assert.AreEqual(2f, c.Weight(BattlePosition.Mid));
            Assert.AreEqual(1f, c.Weight(BattlePosition.Back));
        }

        [Test]
        public void Validate_Defaults_NoErrors() => CollectionAssert.IsEmpty(CombatTestData.Config().Validate());

        [Test]
        public void Validate_NonPositiveWeight_Reported()
        {
            foreach (var field in new[] { "_frontWeight", "_midWeight", "_backWeight" })
            foreach (var value in new[] { 0f, -1f })
            {
                var errors = Validate(so => so.FindProperty(field).floatValue = value);
                Assert.That(errors, Has.Some.StartsWith(Pascal(field)), $"{field} = {value}");
            }
        }

        [Test]
        public void Validate_MaxEnergyBelowOne_Reported()
        {
            foreach (var value in new[] { 0, -1 })
                Assert.That(Validate(so => so.FindProperty("_maxEnergy").intValue = value),
                    Has.Some.StartsWith("MaxEnergy"), $"MaxEnergy = {value}");
            Assert.That(Validate(so => so.FindProperty("_maxEnergy").intValue = 1), Has.None.StartsWith("MaxEnergy"));
        }

        [Test]
        public void Validate_NegativeEnergyGain_Reported()
        {
            foreach (var field in new[] { "_energyOnBasic", "_energyOnSkill", "_energyOnHit" })
            {
                Assert.That(Validate(so => so.FindProperty(field).intValue = -1),
                    Has.Some.StartsWith(Pascal(field)), $"{field} = -1");
                Assert.That(Validate(so => so.FindProperty(field).intValue = 0),
                    Has.None.StartsWith(Pascal(field)), $"{field} = 0");
            }
        }

        [Test]
        public void Validate_MaxRoundsBelowOne_Reported()
        {
            foreach (var value in new[] { 0, -1 })
                Assert.That(Validate(so => so.FindProperty("_maxRounds").intValue = value),
                    Has.Some.StartsWith("MaxRounds"), $"MaxRounds = {value}");
            Assert.That(Validate(so => so.FindProperty("_maxRounds").intValue = 1), Has.None.StartsWith("MaxRounds"));
        }

        [Test]
        public void Validate_AutoHealBelowOutOfRange_Reported()
        {
            foreach (var value in new[] { 0f, -0.1f, 1.01f })
                Assert.That(Validate(so => so.FindProperty("_autoHealBelow").floatValue = value),
                    Has.Some.StartsWith("AutoHealBelow"), $"AutoHealBelow = {value}");
            Assert.That(Validate(so => so.FindProperty("_autoHealBelow").floatValue = 1f), Has.None.StartsWith("AutoHealBelow"));
        }

        [Test]
        public void Validate_MaxStaminaBelowOne_Reported()
        {
            foreach (var value in new[] { 0, -1 })
                Assert.That(Validate(so => so.FindProperty("_maxStamina").intValue = value),
                    Has.Some.StartsWith("MaxStamina"), $"MaxStamina = {value}");
            Assert.That(Validate(so => so.FindProperty("_maxStamina").intValue = 1), Has.None.StartsWith("MaxStamina"));
        }

        [Test]
        public void Validate_MissingStages_Reported() =>
            Assert.That(Validate(so => so.FindProperty("_stages").objectReferenceValue = null), Has.Some.StartsWith("Stages"));

        [Test]
        public void Validate_EmptyStages_Reported() =>
            Assert.That(Validate(so => so.FindProperty("_stages").objectReferenceValue = CombatTestData.Catalog()),
                Has.Some.StartsWith("Stages").And.Some.Contains("empty"));

        [Test]
        public void Validate_DuplicateStageId_Reported()
        {
            var catalog = CombatTestData.Catalog(
                CombatTestData.Stage("dup", new[] { CombatTestData.Enemy() }),
                CombatTestData.Stage("dup", new[] { CombatTestData.Enemy() }));
            Assert.That(Validate(so => so.FindProperty("_stages").objectReferenceValue = catalog),
                Has.Some.StartsWith("Stages").And.Some.Contains("duplicate"));
        }

        [Test]
        public void Validate_BrokenStage_Reported()
        {
            var catalog = CombatTestData.Catalog(CombatTestData.Stage("bad", new[] { CombatTestData.Enemy(hp: 0) }));
            Assert.That(Validate(so => so.FindProperty("_stages").objectReferenceValue = catalog),
                Has.Some.StartsWith("Stages[0] (bad)"));
        }

        [Test]
        public void Validate_StageCostAboveMaxStamina_Reported()
        {
            foreach (var (cost, reported) in new[] { (61, true), (60, false) })
            {
                var stage = CombatTestData.Stage("st", new[] { CombatTestData.Enemy() });
                TestData.Edit(stage, so => so.FindProperty("_staminaCost").intValue = cost);
                var errors = Validate(so => so.FindProperty("_stages").objectReferenceValue = CombatTestData.Catalog(stage));
                if (reported) Assert.That(errors, Has.Some.StartsWith("Stages[0] (st): StaminaCost"), $"cost {cost}");
                else Assert.That(errors, Has.None.Contains("StaminaCost"), $"cost {cost}");
            }
        }

        // 0 chia cho 0 ở StaminaService, nên phải bị chặn ở config (L-016).
        [Test]
        public void Validate_StaminaRegenNonPositive_Reported()
        {
            foreach (var value in new[] { 0, -1 })
                Assert.That(Validate(so => so.FindProperty("_staminaRegenSeconds").intValue = value),
                    Has.Some.StartsWith("StaminaRegenSeconds"), $"StaminaRegenSeconds = {value}");
            Assert.That(Validate(so => so.FindProperty("_staminaRegenSeconds").intValue = 1),
                Has.None.StartsWith("StaminaRegenSeconds"));
        }
    }
}
