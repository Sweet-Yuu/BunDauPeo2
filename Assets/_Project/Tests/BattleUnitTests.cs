using BunDauPeo.Characters;
using BunDauPeo.Combat;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class BattleUnitTests
    {
        [Test]
        public void ForAlly_CopiesStatsSkillsAndSlot()
        {
            var hero = CombatTestData.Hero("h", CharacterClass.Support, hp: 900, attack: 70, defense: 25, speed: 88);
            var unit = BattleUnit.ForAlly(new FormationSlot(hero, BattlePosition.Mid, 1, 3), hero.CombatStats);

            Assert.IsTrue(unit.IsAlly);
            Assert.IsFalse(unit.IsBoss);
            Assert.IsTrue(unit.IsAlive);
            Assert.AreEqual("h", unit.DisplayName);
            Assert.AreEqual((900, 900, 70, 25, 88), (unit.Hp, unit.MaxHp, unit.Attack, unit.Defense, unit.Speed));
            Assert.AreEqual(1f, unit.HpRatio);
            Assert.AreEqual((0, 0), (unit.Energy, unit.SkillCooldown));
            Assert.AreEqual((BattlePosition.Mid, 3), (unit.Position, unit.SlotIndex));
            Assert.AreSame(hero, unit.Character);
            Assert.IsNull(unit.Enemy);
            Assert.AreSame(hero.BasicAttack, unit.BasicAttack);
            Assert.AreSame(hero.Skill, unit.Skill);
            Assert.AreSame(hero.Ultimate, unit.Ultimate);
        }

        [Test]
        public void ForEnemy_HasNoUltimate_KeepsBossFlag()
        {
            var claw = CombatTestData.Skill(SkillKind.Skill, power: 1.8f, cooldown: 3);
            var boss = CombatTestData.Enemy("boss", hp: 700, attack: 55, defense: 12, speed: 66, skill: claw, boss: true);
            var unit = BattleUnit.ForEnemy(boss, 2);

            Assert.IsFalse(unit.IsAlly);
            Assert.IsTrue(unit.IsBoss);
            Assert.IsTrue(unit.IsAlive);
            Assert.AreEqual("boss", unit.DisplayName);
            Assert.AreEqual((700, 700, 55, 12, 66), (unit.Hp, unit.MaxHp, unit.Attack, unit.Defense, unit.Speed));
            Assert.AreEqual(2, unit.SlotIndex);
            Assert.AreSame(boss, unit.Enemy);
            Assert.IsNull(unit.Character);
            Assert.AreSame(boss.BasicAttack, unit.BasicAttack);
            Assert.AreSame(claw, unit.Skill);
            Assert.IsNull(unit.Ultimate);

            var plain = BattleUnit.ForEnemy(CombatTestData.Enemy(), 0);
            Assert.IsFalse(plain.IsBoss);
            Assert.IsNull(plain.Skill);
        }
    }
}
