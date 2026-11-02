using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class BattleActionPanelTests
    {
        private static EnemyDefinition Foe(string name, int hp = 100000, int attack = 10, int speed = 50) =>
            CombatTestData.Enemy(name, hp, attack, 0, speed);

        private static BattleSession Battle(CharacterDefinition[] team, params EnemyDefinition[][] waves) =>
            CombatTestData.Battle(team, CombatTestData.Stage("s", waves));

        // f1 mất 100/1000 (0,9), f2 mất 2500/5000 (0,5, nhưng còn nhiều máu tuyệt đối hơn); tới lượt người hồi máu cuối đội.
        private static BattleSession HealerTurn(SkillDefinition heal)
        {
            var team = new[]
            {
                CombatTestData.Hero("f1"), CombatTestData.Hero("f2", hp: 5000), CombatTestData.Hero("h", CharacterClass.Support, skill: heal),
            };
            var foes = new[] { Foe("e1", attack: 100, speed: 500), Foe("e2", attack: 2500, speed: 499) };
            var s = CombatTestData.Battle(team, CombatTestData.Stage("s", foes), random: new ScriptedRandom(0.0, 0.1, 0.5));
            while (s.Current != s.Allies.Last())
            {
                if (s.State == BattleState.EnemyTurn) s.EnemyAct();
                else s.PlayerAct(BattleActionKind.Basic);
            }

            return s;
        }

        private static SkillDefinition Heal(SkillTarget target) =>
            CombatTestData.Skill(SkillKind.Skill, target, SkillEffect.Heal, 2f, 3);

        [Test]
        public void ResolveTarget_AliveChosenEnemy_Kept()
        {
            var s = Battle(new[] { CombatTestData.Hero("a", speed: 200) }, new[] { Foe("e0"), Foe("e1") });

            Assert.AreSame(s.Enemies[1], BattleActionPanel.ResolveTarget(s, BattleActionKind.Basic, s.Enemies[1], null));
            Assert.AreSame(s.Enemies[0], BattleActionPanel.ResolveTarget(s, BattleActionKind.Basic, null, null), "nothing chosen: the default");
            Assert.AreSame(s.Enemies[1], BattleActionPanel.ResolveTarget(s, BattleActionKind.Skill, s.Enemies[1], null), "the default skill is single-target too");
        }

        [Test]
        public void ResolveTarget_DeadChosenEnemy_FallsBackToDefault()
        {
            var s = Battle(new[] { CombatTestData.Hero("a", speed: 200, attack: 1000) },
                new[] { Foe("w0a", hp: 1, speed: 10), Foe("w0b", hp: 1000, speed: 10) }, new[] { Foe("w1", speed: 10) });
            var dead = s.Enemies[0];
            var oldWaveTwin = s.Enemies[1];
            s.PlayerAct(BattleActionKind.Basic, dead);
            while (s.State == BattleState.EnemyTurn) s.EnemyAct();

            Assert.AreSame(oldWaveTwin, BattleActionPanel.ResolveTarget(s, BattleActionKind.Basic, dead, null), "dead chosen enemy: default");

            s.PlayerAct(BattleActionKind.Basic, oldWaveTwin);
            while (s.State == BattleState.EnemyTurn) s.EnemyAct();
            Assert.AreEqual(1, s.WaveIndex);
            Assert.AreSame(s.Enemies[0], BattleActionPanel.ResolveTarget(s, BattleActionKind.Basic, oldWaveTwin, null), "a unit of the old wave is not in Enemies");
        }

        [Test]
        public void ResolveTarget_Heal_UsesChosenAllyElseLowestRatio()
        {
            var s = HealerTurn(Heal(SkillTarget.SingleAlly));
            Assert.AreEqual((900, 2500), (s.Allies[0].Hp, s.Allies[1].Hp));

            Assert.AreSame(s.Allies[0], BattleActionPanel.ResolveTarget(s, BattleActionKind.Skill, null, s.Allies[0]), "chosen ally kept");
            Assert.AreSame(s.Allies[1], BattleActionPanel.ResolveTarget(s, BattleActionKind.Skill, null, null), "nothing chosen: lowest HP ratio");
            Assert.AreSame(s.Allies[1], BattleActionPanel.ResolveTarget(s, BattleActionKind.Skill, s.Enemies[0], s.Enemies[0]), "an enemy is not a heal target: default");
            Assert.AreSame(s.Allies[1], BattleActionPanel.ResolveTarget(s, BattleActionKind.Skill, s.Allies[0], null), "a chosen enemy slot does not feed an ally skill");
        }

        [Test]
        public void ResolveTarget_NoTargetSkill_Null()
        {
            var aoe = CombatTestData.Skill(SkillKind.Skill, SkillTarget.AllEnemies, SkillEffect.Damage, 1f, 3);
            var s = Battle(new[] { CombatTestData.Hero("a", speed: 200, skill: aoe) }, new[] { Foe("e0"), Foe("e1") });
            Assert.IsNull(BattleActionPanel.ResolveTarget(s, BattleActionKind.Skill, s.Enemies[1], null), "AllEnemies skill");
            Assert.IsNull(BattleActionPanel.ResolveTarget(s, BattleActionKind.Ultimate, s.Enemies[1], null), "the default ultimate hits all enemies");

            foreach (var target in new[] { SkillTarget.AllAllies, SkillTarget.Self })
            {
                var healer = HealerTurn(Heal(target));
                Assert.IsNull(BattleActionPanel.ResolveTarget(healer, BattleActionKind.Skill, healer.Enemies[0], healer.Allies[0]), $"target = {target}");
            }
        }
    }
}
