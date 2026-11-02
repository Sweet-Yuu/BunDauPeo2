using System;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class AutoPolicyTests
    {
        private static CombatConfig FullEnergyOnBasic() => CombatTestData.Config(so =>
            so.FindProperty("_energyOnBasic").intValue = 100);

        private static SkillDefinition HealSkill() =>
            CombatTestData.Skill(SkillKind.Skill, SkillTarget.SingleAlly, SkillEffect.Heal, 1f, 3);

        private static EnemyDefinition Foe(int attack = 10, int speed = 50) =>
            CombatTestData.Enemy("e", 100000, attack, 0, speed);

        private static void PassEnemyTurns(BattleSession s)
        {
            while (s.State == BattleState.EnemyTurn) s.EnemyAct();
        }

        // Một Basic đầu trận cho đủ 100 năng lượng, rồi quái đánh xong thì tới lượt ta lần nữa.
        private static BattleSession UltimateReady(CharacterDefinition hero)
        {
            var s = CombatTestData.Battle(new[] { hero }, CombatTestData.Stage("s", new[] { Foe() }), FullEnergyOnBasic());
            s.PlayerAct(BattleActionKind.Basic);
            PassEnemyTurns(s);
            Assert.AreEqual(ActionAvailability.Available, s.GetAvailability(BattleActionKind.Ultimate), "setup");
            return s;
        }

        // Quái nhanh nhất đánh trước và ScriptedRandom 0.0 chọn người đầu hàng (f1); f1 đánh thường; tới lượt người hồi máu h.
        // Ultimate mặc định của h cần 100 năng lượng nên chưa dùng được.
        private static BattleSession HealerTurn(int foeAttack)
        {
            var team = new[]
            {
                CombatTestData.Hero("f1", speed: 100),
                CombatTestData.Hero("h", CharacterClass.Support, speed: 90, skill: HealSkill()),
            };
            var s = CombatTestData.Battle(team, CombatTestData.Stage("s", new[] { Foe(foeAttack, 500) }));
            s.EnemyAct();
            s.PlayerAct(BattleActionKind.Basic);
            Assert.AreEqual("h", s.Current.DisplayName, "setup");
            return s;
        }

        [Test]
        public void UltimateDamageReady_ChoosesUltimate()
        {
            var s = UltimateReady(CombatTestData.Hero("a", speed: 200));
            Assert.AreEqual(ActionAvailability.Available, s.GetAvailability(BattleActionKind.Skill), "the skill is ready too");

            Assert.AreEqual(BattleActionKind.Ultimate, AutoPolicy.Choose(s).Kind);
        }

        [Test]
        public void NoUltimate_SkillDamageReady_ChoosesSkill()
        {
            var s = CombatTestData.Battle(new[] { CombatTestData.Hero("a", speed: 200) }, CombatTestData.Stage("s", new[] { Foe() }));

            Assert.AreEqual(BattleActionKind.Skill, AutoPolicy.Choose(s).Kind);
        }

        [Test]
        public void NothingReady_ChoosesBasic()
        {
            var s = CombatTestData.Battle(new[] { CombatTestData.Hero("a", speed: 200) }, CombatTestData.Stage("s", new[] { Foe() }));
            s.PlayerAct(BattleActionKind.Skill);
            PassEnemyTurns(s);
            Assert.AreEqual(ActionAvailability.OnCooldown, s.GetAvailability(BattleActionKind.Skill), "setup");
            Assert.AreEqual(ActionAvailability.NotEnoughEnergy, s.GetAvailability(BattleActionKind.Ultimate), "setup");

            Assert.AreEqual(BattleActionKind.Basic, AutoPolicy.Choose(s).Kind);
        }

        [Test]
        public void HealSkill_AllyBelowThreshold_ChoosesHeal()
        {
            // 301 sát thương: 699/1000, dưới 0,7. 300 sát thương thì đúng ngưỡng, xem test kế.
            var s = HealerTurn(301);

            var choice = AutoPolicy.Choose(s);

            Assert.AreEqual(BattleActionKind.Skill, choice.Kind);
            Assert.AreSame(s.Allies[0], choice.Target, "the wounded fighter");
        }

        [Test]
        public void HealSkill_AllyExactlyAtThreshold_ChoosesBasic()
        {
            var s = HealerTurn(300);
            Assert.AreEqual(700, s.Allies[0].Hp, "setup: 700/1000 at a 0.7 threshold");

            Assert.AreEqual(BattleActionKind.Basic, AutoPolicy.Choose(s).Kind);
        }

        [Test]
        public void HealUltimate_EveryoneHealthy_Skipped()
        {
            var heal = CombatTestData.Skill(SkillKind.Ultimate, SkillTarget.AllAllies, SkillEffect.Heal, 1f, 0, 100);
            foreach (var foeAttack in new[] { 10, 400 })
            {
                var s = CombatTestData.Battle(new[] { CombatTestData.Hero("h", CharacterClass.Support, speed: 200, ultimate: heal) },
                    CombatTestData.Stage("s", new[] { Foe(foeAttack) }), FullEnergyOnBasic());
                s.PlayerAct(BattleActionKind.Basic);
                PassEnemyTurns(s);
                Assert.AreEqual(ActionAvailability.Available, s.GetAvailability(BattleActionKind.Ultimate), $"setup, foe {foeAttack}");

                // Khoẻ (990/1000) thì bỏ ultimate hồi máu để dùng skill gây sát thương; bị thương (600/1000) thì dùng.
                var expected = foeAttack == 10 ? BattleActionKind.Skill : BattleActionKind.Ultimate;
                Assert.AreEqual(expected, AutoPolicy.Choose(s).Kind, $"foe attack {foeAttack}");
            }
        }

        [Test]
        public void Target_IsDefaultTarget()
        {
            var healer = HealerTurn(400);
            var heal = AutoPolicy.Choose(healer);
            Assert.AreSame(healer.DefaultTarget(heal.Kind), heal.Target);

            var s = CombatTestData.Battle(new[] { CombatTestData.Hero("a", speed: 200) },
                CombatTestData.Stage("s", new[] { Foe(), Foe() }));
            var strike = AutoPolicy.Choose(s);
            Assert.AreSame(s.Enemies[0], strike.Target);
            Assert.AreSame(s.DefaultTarget(strike.Kind), strike.Target);

            var area = AutoPolicy.Choose(UltimateReady(CombatTestData.Hero("b", speed: 200)));
            Assert.AreEqual(BattleActionKind.Ultimate, area.Kind);
            Assert.IsNull(area.Target, "an area ultimate has no default target");
        }

        [Test]
        public void NotPlayerTurn_Throws()
        {
            var enemyTurn = CombatTestData.Battle(new[] { CombatTestData.Hero("a", speed: 10) },
                CombatTestData.Stage("s", new[] { Foe(10, 200) }));
            Assert.Throws<InvalidOperationException>(() => AutoPolicy.Choose(enemyTurn));

            var ended = CombatTestData.Battle(new[] { CombatTestData.Hero("a", speed: 200) },
                CombatTestData.Stage("s", new[] { CombatTestData.Enemy("e", 1) }));
            ended.PlayerAct(BattleActionKind.Basic);
            Assert.AreEqual(BattleState.Won, ended.State);
            Assert.Throws<InvalidOperationException>(() => AutoPolicy.Choose(ended));
        }
    }
}
