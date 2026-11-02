using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class BattleSkillTests
    {
        private static CharacterDefinition Hero(string id, CharacterClass cls = CharacterClass.Fighter, int hp = 1000,
            int attack = 100, int defense = 0, int speed = 100, SkillDefinition skill = null, SkillDefinition ultimate = null) =>
            CombatTestData.Hero(id, cls, hp, attack, defense, speed, skill, ultimate);

        private static EnemyDefinition Foe(string name, int hp = 100000, int attack = 10, int defense = 0, int speed = 50,
            SkillDefinition skill = null) => CombatTestData.Enemy(name, hp, attack, defense, speed, skill);

        private static SkillDefinition Dmg(SkillKind kind, SkillTarget target, float power, int cooldown = 0, int energy = 0) =>
            CombatTestData.Skill(kind, target, SkillEffect.Damage, power, cooldown, energy);

        private static SkillDefinition Heal(SkillTarget target, float power) =>
            CombatTestData.Skill(SkillKind.Skill, target, SkillEffect.Heal, power, 3);

        private static CombatConfig Energy(int onBasic, int onHit) => CombatTestData.Config(so =>
        {
            so.FindProperty("_energyOnBasic").intValue = onBasic;
            so.FindProperty("_energyOnHit").intValue = onHit;
        });

        private static BattleSession Battle(CharacterDefinition[] team, params EnemyDefinition[][] waves) =>
            CombatTestData.Battle(team, CombatTestData.Stage("s", waves));

        private static void PassEnemyTurns(BattleSession s)
        {
            while (s.State == BattleState.EnemyTurn) s.EnemyAct();
        }

        // Mọi thứ một hành động thất bại không được đổi, kể cả năng lượng và hồi chiêu.
        private static string Snapshot(BattleSession s) =>
            $"{s.State}|{s.Round}|{s.WaveIndex}|{s.Current?.DisplayName}|" +
            string.Join(",", s.Allies.Concat(s.Enemies).Select(u => $"{u.Hp}/{u.Energy}/{u.SkillCooldown}"));

        private static void AssertRejected(BattleActionResult r, BattleActionStatus status, BattleSession s, string before, string label)
        {
            Assert.AreEqual(status, r.Status, label);
            CollectionAssert.IsEmpty(r.Hits, label);
            Assert.AreEqual(before, Snapshot(s), $"{label}: nothing changed");
        }

        // Quái nhanh hơn cả đội nên đánh trước, mỗi quái làm người do `rolls` chọn mất máu; rồi cả đội đánh thường tới lượt
        // người cuối đội (người hồi máu). Quái chỉ đánh đúng một lần mỗi quái trước lượt đó.
        private static BattleSession ToHealer(CharacterDefinition[] team, EnemyDefinition[] foes, params double[] rolls)
        {
            var s = CombatTestData.Battle(team, CombatTestData.Stage("s", foes), random: new ScriptedRandom(0.0, rolls));
            var healer = s.Allies.Last();
            while (s.Current != healer)
            {
                if (s.State == BattleState.EnemyTurn) s.EnemyAct();
                else s.PlayerAct(BattleActionKind.Basic);
            }

            return s;
        }

        // f1 và f2 cùng mất 100 máu (u 0,1 và 0,5 với trọng số 3, 3, 2), người hồi máu đầy máu.
        private static BattleSession TwoWoundedAllies(SkillDefinition healSkill, int f2Hp = 1000) =>
            ToHealer(new[] { Hero("f1"), Hero("f2", hp: f2Hp), Hero("h", CharacterClass.Support, skill: healSkill) },
                new[] { Foe("e1", attack: 100, speed: 500), Foe("e2", attack: 100, speed: 499) }, 0.1, 0.5);

        [Test]
        public void Skill_Ready_DealsPowerDamage_SetsCooldown()
        {
            var s = Battle(new[] { Hero("a", speed: 200) }, new[] { Foe("e", hp: 1000, defense: 25) });
            var actor = s.Allies[0];

            var r = s.PlayerAct(BattleActionKind.Skill);

            Assert.AreEqual(BattleActionStatus.Success, r.Status);
            Assert.AreSame(actor.Skill, r.Skill);
            var hit = r.Hits.Single();
            Assert.AreSame(s.Enemies[0], hit.Target);
            Assert.AreEqual(160, hit.Amount, "power 2: round(100 x 2 x 100 / 125)");
            Assert.AreEqual(840, s.Enemies[0].Hp);
            Assert.AreEqual(3, actor.SkillCooldown);
        }

        [Test]
        public void Skill_CooldownThree_ReadyAgainOnFourthOwnTurn()
        {
            var s = Battle(new[] { Hero("a", speed: 100) }, new[] { Foe("e", speed: 50) });
            var seen = new List<ActionAvailability>();
            var cooldowns = new List<int>();

            for (var turn = 1; turn <= 4; turn++)
            {
                var availability = s.GetAvailability(BattleActionKind.Skill);
                seen.Add(availability);
                cooldowns.Add(s.Current.SkillCooldown);
                s.PlayerAct(availability == ActionAvailability.Available ? BattleActionKind.Skill : BattleActionKind.Basic);
                PassEnemyTurns(s);
            }

            CollectionAssert.AreEqual(new[] { ActionAvailability.Available, ActionAvailability.OnCooldown, ActionAvailability.OnCooldown, ActionAvailability.Available }, seen);
            CollectionAssert.AreEqual(new[] { 0, 2, 1, 0 }, cooldowns, "decrements once when the unit's turn comes, before it chooses");
        }

        [Test]
        public void Skill_CooldownOne_UsableEveryTurn()
        {
            var every = Dmg(SkillKind.Skill, SkillTarget.SingleEnemy, 2f, cooldown: 1);
            var s = Battle(new[] { Hero("a", speed: 100, skill: every) }, new[] { Foe("e", speed: 50) });

            for (var turn = 1; turn <= 3; turn++)
            {
                Assert.AreEqual(ActionAvailability.Available, s.GetAvailability(BattleActionKind.Skill), $"turn {turn}");
                Assert.AreEqual(BattleActionStatus.Success, s.PlayerAct(BattleActionKind.Skill).Status, $"turn {turn}");
                PassEnemyTurns(s);
            }
        }

        [Test]
        public void Skill_OnCooldown_Unavailable_ChangesNothing()
        {
            var s = Battle(new[] { Hero("a", speed: 100) }, new[] { Foe("e", speed: 50) });
            s.PlayerAct(BattleActionKind.Skill);
            PassEnemyTurns(s);
            Assert.AreEqual(ActionAvailability.OnCooldown, s.GetAvailability(BattleActionKind.Skill));
            var before = Snapshot(s);

            AssertRejected(s.PlayerAct(BattleActionKind.Skill), BattleActionStatus.Unavailable, s, before, "skill on cooldown");
        }

        [Test]
        public void Energy_Basic_AddsEnergyOnBasic()
        {
            var s = Battle(new[] { Hero("a", speed: 200) }, new[] { Foe("e", speed: 10) });

            s.PlayerAct(BattleActionKind.Basic);

            Assert.AreEqual(25, s.Allies[0].Energy);
            PassEnemyTurns(s);
            Assert.AreEqual(0, s.Enemies[0].Energy, "enemies have no energy");
        }

        [Test]
        public void Energy_Skill_AddsEnergyOnSkill()
        {
            var s = Battle(new[] { Hero("a", speed: 200) }, new[] { Foe("e", speed: 10) });

            s.PlayerAct(BattleActionKind.Skill);

            Assert.AreEqual(10, s.Allies[0].Energy);
        }

        [Test]
        public void Energy_AllyHit_AddsEnergyOnHitPerHit()
        {
            var aoe = Dmg(SkillKind.Skill, SkillTarget.AllEnemies, 1f, cooldown: 3);
            var s = CombatTestData.Battle(new[] { Hero("a"), Hero("b"), Hero("c") },
                CombatTestData.Stage("s", new[] { Foe("e1", speed: 500, skill: aoe), Foe("e2", speed: 499) }), random: new ScriptedRandom(0.0));

            var first = s.EnemyAct();

            Assert.AreEqual(3, first.Hits.Count);
            foreach (var ally in s.Allies) Assert.AreEqual(15, ally.Energy, $"{ally.DisplayName} after the area hit");

            s.EnemyAct();
            CollectionAssert.AreEqual(new[] { 30, 15, 15 }, s.Allies.Select(a => a.Energy).ToArray(), "a single hit only feeds the unit it hit");
            Assert.AreEqual(0, s.Enemies[0].Energy);
        }

        [Test]
        public void Energy_ClampedAtMax()
        {
            var s = CombatTestData.Battle(new[] { Hero("a", speed: 100) }, CombatTestData.Stage("s", new[] { Foe("e", speed: 50) }),
                Energy(onBasic: 60, onHit: 15));

            s.PlayerAct(BattleActionKind.Basic);
            Assert.AreEqual(60, s.Allies[0].Energy);
            PassEnemyTurns(s);
            Assert.AreEqual(75, s.Allies[0].Energy, "the enemy hit adds EnergyOnHit");

            s.PlayerAct(BattleActionKind.Basic);
            Assert.AreEqual(100, s.Allies[0].Energy, "75 + 60 is clamped to MaxEnergy");
        }

        [Test]
        public void Ultimate_Ready_SpendsCost_NoEnergyGain()
        {
            foreach (var (cost, left) in new[] { (100, 0), (60, 40) })
            {
                var ult = Dmg(SkillKind.Ultimate, SkillTarget.AllEnemies, 3f, energy: cost);
                var s = CombatTestData.Battle(new[] { Hero("a", speed: 100, ultimate: ult) },
                    CombatTestData.Stage("s", new[] { Foe("e", speed: 50) }), Energy(onBasic: 100, onHit: 15));
                s.PlayerAct(BattleActionKind.Basic);
                PassEnemyTurns(s);
                Assert.AreEqual(ActionAvailability.Available, s.GetAvailability(BattleActionKind.Ultimate), $"cost = {cost}");

                var r = s.PlayerAct(BattleActionKind.Ultimate);

                Assert.AreEqual(BattleActionStatus.Success, r.Status, $"cost = {cost}");
                Assert.AreSame(s.Allies[0].Ultimate, r.Skill, $"cost = {cost}");
                Assert.AreEqual(left, s.Allies[0].Energy, $"cost = {cost}: spends the cost and gains nothing");
            }
        }

        [Test]
        public void Ultimate_OneShort_NotEnoughEnergy_ChangesNothing()
        {
            var s = CombatTestData.Battle(new[] { Hero("a", speed: 100) }, CombatTestData.Stage("s", new[] { Foe("e", speed: 50) }),
                Energy(onBasic: 99, onHit: 0));
            s.PlayerAct(BattleActionKind.Basic);
            PassEnemyTurns(s);
            Assert.AreEqual(99, s.Allies[0].Energy);
            Assert.AreEqual(ActionAvailability.NotEnoughEnergy, s.GetAvailability(BattleActionKind.Ultimate));
            var before = Snapshot(s);

            AssertRejected(s.PlayerAct(BattleActionKind.Ultimate), BattleActionStatus.Unavailable, s, before, "99 of 100");
        }

        [Test]
        public void Ultimate_CostAboveMax_RequiresMaxOnly()
        {
            var ult = Dmg(SkillKind.Ultimate, SkillTarget.AllEnemies, 3f, energy: 150);
            var s = CombatTestData.Battle(new[] { Hero("a", speed: 100, ultimate: ult) },
                CombatTestData.Stage("s", new[] { Foe("e", speed: 50) }), Energy(onBasic: 99, onHit: 0));

            s.PlayerAct(BattleActionKind.Basic);
            PassEnemyTurns(s);
            Assert.AreEqual(ActionAvailability.NotEnoughEnergy, s.GetAvailability(BattleActionKind.Ultimate), "99 of an effective 100");

            s.PlayerAct(BattleActionKind.Basic);
            PassEnemyTurns(s);
            Assert.AreEqual(100, s.Allies[0].Energy);
            Assert.AreEqual(ActionAvailability.Available, s.GetAvailability(BattleActionKind.Ultimate), "full bar is enough for a 150 cost");

            s.PlayerAct(BattleActionKind.Ultimate);
            Assert.AreEqual(0, s.Allies[0].Energy, "spends 100, not 150");
        }

        [Test]
        public void AllEnemies_HitsEveryAliveEnemyOnly()
        {
            var ult = Dmg(SkillKind.Ultimate, SkillTarget.AllEnemies, 3f, energy: 10);
            var s = Battle(new[] { Hero("a", speed: 200, ultimate: ult) },
                new[] { Foe("e0", hp: 1, speed: 10), Foe("e1", hp: 1000, speed: 10), Foe("e2", hp: 1000, defense: 50, speed: 10) });
            s.PlayerAct(BattleActionKind.Basic, s.Enemies[0]);
            PassEnemyTurns(s);

            var r = s.PlayerAct(BattleActionKind.Ultimate);

            Assert.AreEqual(BattleActionStatus.Success, r.Status);
            CollectionAssert.AreEqual(new[] { s.Enemies[1], s.Enemies[2] }, r.Hits.Select(h => h.Target).ToArray(), "dead e0 is skipped, slot order kept");
            CollectionAssert.AreEqual(new[] { 300, 200 }, r.Hits.Select(h => h.Amount).ToArray(), "each hit uses its own target's defense");
            Assert.AreEqual(0, s.Enemies[0].Hp);
            Assert.IsFalse(r.WaveCleared);
        }

        [Test]
        public void HealSingle_DefaultLowestHpRatio_TieLowerSlot()
        {
            var heal = Heal(SkillTarget.SingleAlly, 2f);

            var tie = TwoWoundedAllies(heal);
            Assert.AreEqual(900, tie.Allies[0].Hp);
            Assert.AreEqual(900, tie.Allies[1].Hp);
            Assert.AreSame(tie.Allies[0], tie.DefaultTarget(BattleActionKind.Skill), "equal ratios: lower slot");
            Assert.AreSame(tie.Allies[0], tie.PlayerAct(BattleActionKind.Skill).Hits.Single().Target);

            // f1 mất 100 trên 1000 (0,9); f2 mất 2500 trên 5000 (0,5) nhưng còn nhiều máu tuyệt đối hơn: phải chọn theo tỉ lệ.
            var ratio = ToHealer(new[] { Hero("f1"), Hero("f2", hp: 5000), Hero("h", CharacterClass.Support, skill: heal) },
                new[] { Foe("e1", attack: 100, speed: 500), Foe("e2", attack: 2500, speed: 499) }, 0.1, 0.5);
            Assert.AreEqual((900, 2500), (ratio.Allies[0].Hp, ratio.Allies[1].Hp));
            Assert.AreSame(ratio.Allies[1], ratio.DefaultTarget(BattleActionKind.Skill), "lowest ratio, not lowest absolute HP");
            Assert.AreSame(ratio.Allies[1], ratio.PlayerAct(BattleActionKind.Skill).Hits.Single().Target);
        }

        [Test]
        public void HealAll_HealsAliveOnly()
        {
            var s = TwoWoundedAllies(Heal(SkillTarget.AllAllies, 2f), f2Hp: 100);
            Assert.IsFalse(s.Allies[1].IsAlive, "f2 was killed by the second enemy");

            var r = s.PlayerAct(BattleActionKind.Skill);

            Assert.AreEqual(BattleActionStatus.Success, r.Status);
            CollectionAssert.AreEqual(new[] { s.Allies[0], s.Allies[2] }, r.Hits.Select(h => h.Target).ToArray());
            Assert.IsTrue(r.Hits.All(h => h.IsHeal));
            Assert.AreEqual(1000, s.Allies[0].Hp);
            Assert.AreEqual(0, s.Allies[1].Hp, "no revive");
        }

        [Test]
        public void HealSelf_IgnoresPassedTarget()
        {
            for (var variant = 0; variant < 3; variant++)
            {
                var s = ToHealer(new[] { Hero("f1"), Hero("f2"), Hero("h", CharacterClass.Support, skill: Heal(SkillTarget.Self, 2f)) },
                    new[] { Foe("e1", attack: 100, speed: 500) }, 0.9);
                var healer = s.Allies[2];
                Assert.AreEqual(900, healer.Hp, $"variant {variant}");
                var passed = variant == 0 ? s.Enemies[0] : variant == 1 ? s.Allies[0] : null;

                var r = s.PlayerAct(BattleActionKind.Skill, passed);

                Assert.AreEqual(BattleActionStatus.Success, r.Status, $"variant {variant}");
                Assert.AreSame(healer, r.Hits.Single().Target, $"variant {variant}");
                Assert.AreEqual(1000, healer.Hp, $"variant {variant}");
            }
        }

        [Test]
        public void Heal_DeadAlly_InvalidTarget_NoRevive()
        {
            var s = TwoWoundedAllies(Heal(SkillTarget.SingleAlly, 2f), f2Hp: 100);
            var before = Snapshot(s);

            AssertRejected(s.PlayerAct(BattleActionKind.Skill, s.Allies[1]), BattleActionStatus.InvalidTarget, s, before, "dead ally");
            Assert.AreEqual(0, s.Allies[1].Hp);
        }

        [Test]
        public void Heal_EnemyTarget_InvalidTarget()
        {
            var s = TwoWoundedAllies(Heal(SkillTarget.SingleAlly, 2f));
            var before = Snapshot(s);

            AssertRejected(s.PlayerAct(BattleActionKind.Skill, s.Enemies[0]), BattleActionStatus.InvalidTarget, s, before, "enemy as heal target");
        }

        [Test]
        public void Heal_RecordsActualAmount()
        {
            var heal = Heal(SkillTarget.SingleAlly, 2f);

            var wounded = TwoWoundedAllies(heal);
            var hit = wounded.PlayerAct(BattleActionKind.Skill, wounded.Allies[1]).Hits.Single();
            Assert.AreSame(wounded.Allies[1], hit.Target, "an explicit target beats the default");
            Assert.AreEqual(100, hit.Amount, "round(100 x 2) = 200 but only 100 HP was missing");
            Assert.IsTrue(hit.IsHeal);
            Assert.IsFalse(hit.Killed);
            Assert.AreEqual((900, 1000), (wounded.Allies[0].Hp, wounded.Allies[1].Hp));

            var full = TwoWoundedAllies(heal);
            var none = full.PlayerAct(BattleActionKind.Skill, full.Allies[2]);
            Assert.AreEqual(BattleActionStatus.Success, none.Status);
            Assert.AreEqual(0, none.Hits.Single().Amount, "healing a full-HP unit restores nothing");
        }

        [Test]
        public void NoTargetSkill_IgnoresPassedTarget()
        {
            var aoe = Dmg(SkillKind.Skill, SkillTarget.AllEnemies, 1f, cooldown: 3);
            var s = Battle(new[] { Hero("a", speed: 200, skill: aoe), Hero("b", speed: 10) }, new[] { Foe("e0"), Foe("e1") });

            var r = s.PlayerAct(BattleActionKind.Skill, s.Allies[1]);

            Assert.AreEqual(BattleActionStatus.Success, r.Status);
            CollectionAssert.AreEqual(s.Enemies.ToArray(), r.Hits.Select(h => h.Target).ToArray());
        }

        [Test]
        public void DefaultTarget_PerTargetKind()
        {
            var rows = new (SkillTarget, SkillEffect, string)[]
            {
                (SkillTarget.SingleEnemy, SkillEffect.Damage, "enemy0"), (SkillTarget.AllEnemies, SkillEffect.Damage, null),
                (SkillTarget.SingleAlly, SkillEffect.Heal, "ally0"), (SkillTarget.AllAllies, SkillEffect.Heal, null),
                (SkillTarget.Self, SkillEffect.Heal, null),
            };
            foreach (var (target, effect, expected) in rows)
            {
                var skill = CombatTestData.Skill(SkillKind.Skill, target, effect, 2f, 3);
                var s = Battle(new[] { Hero("a", speed: 200, skill: skill), Hero("b", speed: 10) }, new[] { Foe("e0"), Foe("e1") });

                var actual = s.DefaultTarget(BattleActionKind.Skill);

                var want = expected == "enemy0" ? s.Enemies[0] : expected == "ally0" ? s.Allies[0] : null;
                Assert.AreSame(want, actual, $"target = {target}");
            }

            var plain = Battle(new[] { Hero("a", speed: 200) }, new[] { Foe("e0"), Foe("e1") });
            Assert.AreSame(plain.Enemies[0], plain.DefaultTarget(BattleActionKind.Basic));
            Assert.IsNull(plain.DefaultTarget(BattleActionKind.Ultimate), "the default ultimate hits all enemies");
        }

        [Test]
        public void Enemy_UsesSkillWhenReady_ThenBasicWhileOnCooldown()
        {
            var skill = Dmg(SkillKind.Skill, SkillTarget.SingleEnemy, 3f, cooldown: 2);
            var foe = Foe("e", attack: 100, speed: 500, skill: skill);
            var s = Battle(new[] { Hero("a", hp: 100000, speed: 100) }, new[] { foe });
            var usedSkill = new List<bool>();
            var amounts = new List<int>();

            for (var round = 1; round <= 4; round++)
            {
                var r = s.EnemyAct();
                usedSkill.Add(ReferenceEquals(r.Skill, foe.Skill));
                amounts.Add(r.Hits.Single().Amount);
                s.PlayerAct(BattleActionKind.Basic);
            }

            CollectionAssert.AreEqual(new[] { true, false, true, false }, usedSkill);
            CollectionAssert.AreEqual(new[] { 300, 100, 300, 100 }, amounts);
        }

        [Test]
        public void Enemy_AoeSkill_NoRandomCall_HitsAllAliveAllies()
        {
            var aoe = Dmg(SkillKind.Skill, SkillTarget.AllEnemies, 1f, cooldown: 3);
            var random = new ScriptedRandom(0.0, 0.95);
            var s = CombatTestData.Battle(new[] { Hero("f1"), Hero("f2"), Hero("m", CharacterClass.Mage, hp: 1) },
                CombatTestData.Stage("s", new[] { Foe("e0", attack: 1000, speed: 500), Foe("e1", attack: 100, speed: 499, skill: aoe) }),
                random: random);

            s.EnemyAct();
            Assert.IsFalse(s.Allies[2].IsAlive, "u = 0,95 picks the back-line mage");
            Assert.AreEqual(1, random.Calls);

            var r = s.EnemyAct();

            CollectionAssert.AreEqual(new[] { s.Allies[0], s.Allies[1] }, r.Hits.Select(h => h.Target).ToArray());
            Assert.AreEqual(1, random.Calls, "the area skill does not draw a random number");
        }

        [Test]
        public void GetAvailability_AllStatuses()
        {
            var noUltimate = Hero("n", speed: 200);
            TestData.Edit(noUltimate, so => so.FindProperty("_ultimate").objectReferenceValue = null);
            var missing = Battle(new[] { noUltimate }, new[] { Foe("e") });
            Assert.AreEqual(ActionAvailability.NoSuchSkill, missing.GetAvailability(BattleActionKind.Ultimate), "checked before energy");
            Assert.AreEqual(ActionAvailability.Available, missing.GetAvailability(BattleActionKind.Skill));
            Assert.AreEqual(ActionAvailability.Available, missing.GetAvailability(BattleActionKind.Basic));

            var s = Battle(new[] { Hero("a", speed: 200) }, new[] { Foe("e", speed: 10) });
            Assert.AreEqual(ActionAvailability.NotEnoughEnergy, s.GetAvailability(BattleActionKind.Ultimate));
            s.PlayerAct(BattleActionKind.Skill);
            PassEnemyTurns(s);
            Assert.AreEqual(ActionAvailability.OnCooldown, s.GetAvailability(BattleActionKind.Skill));
            Assert.AreEqual(ActionAvailability.Available, s.GetAvailability(BattleActionKind.Basic));

            var enemyFirst = Battle(new[] { Hero("a", speed: 10) }, new[] { Foe("e", speed: 200) });
            var ended = Battle(new[] { Hero("a", speed: 200) }, new[] { Foe("e") });
            ended.Surrender();
            foreach (var kind in new[] { BattleActionKind.Basic, BattleActionKind.Skill, BattleActionKind.Ultimate })
            {
                Assert.AreEqual(ActionAvailability.NotYourTurn, enemyFirst.GetAvailability(kind), $"enemy turn, {kind}");
                Assert.AreEqual(ActionAvailability.Ended, ended.GetAvailability(kind), $"after the end, {kind}");
            }
        }

        [Test]
        public void NextWave_KeepsEnergyAndCooldown()
        {
            var s = Battle(new[] { Hero("a", speed: 200) }, new[] { Foe("old", hp: 1, speed: 10) }, new[] { Foe("new", speed: 10) });

            var r = s.PlayerAct(BattleActionKind.Skill);

            Assert.IsTrue(r.NextWaveStarted);
            Assert.AreEqual(10, s.Allies[0].Energy, "energy survives the new wave");
            Assert.AreEqual(2, s.Allies[0].SkillCooldown, "cooldown 3 is kept, minus the one tick for its new turn; a reset would be 0");
            Assert.AreEqual(ActionAvailability.OnCooldown, s.GetAvailability(BattleActionKind.Skill));
            Assert.AreEqual("new", s.Enemies[0].DisplayName);
        }
    }
}
