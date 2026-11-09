using System;
using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class BattleSessionTests
    {
        private static CharacterDefinition Hero(string id, CharacterClass cls = CharacterClass.Fighter, int hp = 1000,
            int attack = 100, int defense = 0, int speed = 100) => CombatTestData.Hero(id, cls, hp, attack, defense, speed);

        private static EnemyDefinition Foe(string name, int hp = 1000, int attack = 10, int defense = 0, int speed = 50) =>
            CombatTestData.Enemy(name, hp, attack, defense, speed);

        private static BattleSession Battle(CharacterDefinition[] team, params EnemyDefinition[][] waves) =>
            CombatTestData.Battle(team, CombatTestData.Stage("s", waves));

        private static string[] Names(IEnumerable<BattleUnit> units) => units.Select(u => u.DisplayName).ToArray();

        private static void PassEnemyTurns(BattleSession s)
        {
            while (s.State == BattleState.EnemyTurn) s.EnemyAct();
        }

        // Mọi thứ một hành động thất bại không được đổi.
        private static string Snapshot(BattleSession s) =>
            $"{s.State}|{s.Round}|{s.WaveIndex}|{s.Current?.DisplayName}|{string.Join(",", s.Allies.Concat(s.Enemies).Select(u => u.Hp))}|{string.Join(",", Names(s.RemainingOrder))}";

        private static void AssertRejected(BattleActionResult r, BattleActionStatus status, BattleSession s, string before, string label)
        {
            Assert.AreEqual(status, r.Status, label);
            CollectionAssert.IsEmpty(r.Hits, label);
            Assert.AreEqual(s.State, r.StateAfter, label);
            Assert.AreEqual(before, Snapshot(s), $"{label}: nothing changed");
        }

        [Test]
        public void Ctor_InvalidArguments_Throw()
        {
            var team = new[] { Hero("a") };
            var stage = CombatTestData.Stage("s", new[] { Foe("e") });
            var config = CombatTestData.Config();
            var random = new ScriptedRandom();

            Assert.Throws<ArgumentNullException>(() => new BattleSession(team, stage, null, random));
            Assert.Throws<ArgumentNullException>(() => new BattleSession(team, stage, config, null));
            Assert.Throws<ArgumentException>(() => new BattleSession(null, stage, config, random));
            Assert.Throws<ArgumentException>(() => new BattleSession(new CharacterDefinition[0], stage, config, random));
            Assert.Throws<ArgumentException>(() => new BattleSession(team, null, config, random));
            Assert.Throws<ArgumentException>(() => new BattleSession(team, CombatTestData.Story("story"), config, random));
            Assert.Throws<ArgumentException>(() => new BattleSession(team, CombatTestData.Stage("no-waves"), config, random));
            Assert.Throws<ArgumentException>(() => new BattleSession(team, CombatTestData.Stage("empty-wave", new EnemyDefinition[0]), config, random));
            Assert.DoesNotThrow(() => new BattleSession(team, stage, config, random));
        }

        [Test]
        public void StatsOf_Given_AlliesUseIt()
        {
            var team = new[] { Hero("a"), Hero("b") };
            var stage = CombatTestData.Stage("s", new[] { Foe("e", hp: 500, attack: 20, defense: 5, speed: 40) });
            var session = CombatTestData.Battle(team, stage, statsOf: c => c.Id == "a"
                ? new CombatStats { HP = 999, Attack = 77, Defense = 33, Speed = 50 }
                : new CombatStats { HP = 888, Attack = 66, Defense = 22, Speed = 60 });

            var a = session.Allies.Single(u => u.DisplayName == "a");
            var b = session.Allies.Single(u => u.DisplayName == "b");
            Assert.AreEqual((999, 999, 77, 33, 50), (a.MaxHp, a.Hp, a.Attack, a.Defense, a.Speed));
            Assert.AreEqual((888, 888, 66, 22, 60), (b.MaxHp, b.Hp, b.Attack, b.Defense, b.Speed));
            var e = session.Enemies[0];
            Assert.AreEqual((500, 500, 20, 5, 40), (e.MaxHp, e.Hp, e.Attack, e.Defense, e.Speed));
        }

        [Test]
        public void StatsOf_Null_UsesDefinitionStats()
        {
            var hero = Hero("a", hp: 900, attack: 70, defense: 25, speed: 88);
            var session = Battle(new[] { hero }, new[] { Foe("e") });

            var a = session.Allies[0];
            Assert.AreEqual((900, 900, 70, 25, 88), (a.MaxHp, a.Hp, a.Attack, a.Defense, a.Speed));
        }

        [Test]
        public void Start_RoundOneWaveZero_OrderBySpeedDescending()
        {
            var s = Battle(new[] { Hero("a1", speed: 120), Hero("a2", speed: 100), Hero("a3", speed: 80) },
                new[] { Foe("e1", speed: 110), Foe("e2", speed: 90) });

            Assert.AreEqual((1, 0, 1), (s.Round, s.WaveIndex, s.WaveCount));
            CollectionAssert.AreEqual(new[] { "a1", "e1", "a2", "e2", "a3" }, Names(s.RemainingOrder));
            Assert.AreEqual("a1", s.Current.DisplayName);
            Assert.AreEqual(BattleState.PlayerTurn, s.State);
        }

        [Test]
        public void Order_SpeedTie_AlliesBeforeEnemies_ThenLowerSlot()
        {
            // Mage ở ô 4 dù đứng đầu đội: phải xếp sau hai Đấu sĩ (ô 0, 1) và trước quái cùng tốc độ.
            var s = Battle(new[] { Hero("m", CharacterClass.Mage), Hero("f1"), Hero("f2") },
                new[] { Foe("e0", speed: 100), Foe("e1", speed: 100) });

            CollectionAssert.AreEqual(new[] { "f1", "f2", "m", "e0", "e1" }, Names(s.RemainingOrder));
        }

        [Test]
        public void Start_EnemyFastest_StateIsEnemyTurn()
        {
            var s = Battle(new[] { Hero("a", speed: 100) }, new[] { Foe("e", speed: 200) });
            Assert.AreEqual(BattleState.EnemyTurn, s.State);
            Assert.AreEqual("e", s.Current.DisplayName);
        }

        [Test]
        public void RemainingOrder_StartsWithCurrent_AndShrinks()
        {
            var s = Battle(new[] { Hero("a", speed: 120) }, new[] { Foe("e1", hp: 1, speed: 110), Foe("e2", speed: 100) });
            Assert.AreEqual(3, s.RemainingOrder.Count);
            Assert.AreSame(s.Current, s.RemainingOrder[0]);

            s.PlayerAct(BattleActionKind.Basic, s.Enemies[0]);

            CollectionAssert.AreEqual(new[] { "e2" }, Names(s.RemainingOrder), "acted and dead units are gone");
            Assert.AreSame(s.Current, s.RemainingOrder[0]);
        }

        [Test]
        public void Basic_NoTarget_HitsFirstAliveEnemy()
        {
            var s = Battle(new[] { Hero("a", speed: 200, attack: 100) },
                new[] { Foe("e0", hp: 1, speed: 10), Foe("e1", hp: 1000, speed: 10), Foe("e2", hp: 500, speed: 10) });

            var first = s.PlayerAct(BattleActionKind.Basic);
            Assert.AreSame(s.Enemies[0], first.Hits.Single().Target, "slot 0 first");
            Assert.AreEqual(DamageCalculator.Damage(100, 1f, 0), first.Hits.Single().Amount);

            PassEnemyTurns(s);
            var second = s.PlayerAct(BattleActionKind.Basic);
            Assert.AreSame(s.Enemies[1], second.Hits.Single().Target, "slot 0 is dead; slot 1 before the weaker slot 2");
        }

        [Test]
        public void Basic_ExplicitTarget_ResultFilled()
        {
            var s = Battle(new[] { Hero("a", speed: 200, attack: 100) },
                new[] { Foe("e0", hp: 1000), Foe("e1", hp: 50, defense: 25) });
            var actor = s.Allies[0];

            var r = s.PlayerAct(BattleActionKind.Basic, s.Enemies[1]);

            Assert.AreEqual(BattleActionStatus.Success, r.Status);
            Assert.AreSame(actor, r.Actor);
            Assert.AreSame(actor.BasicAttack, r.Skill);
            var hit = r.Hits.Single();
            Assert.AreSame(s.Enemies[1], hit.Target);
            Assert.AreEqual(DamageCalculator.Damage(100, 1f, 25), hit.Amount, "damage uses the target's defense");
            Assert.AreEqual(80, hit.Amount, "Amount is the formula value, not clamped to the 50 HP left");
            Assert.IsFalse(hit.IsHeal);
            Assert.IsTrue(hit.Killed);
            Assert.AreEqual(0, s.Enemies[1].Hp);
            Assert.IsFalse(r.WaveCleared);
            Assert.IsFalse(r.NextWaveStarted);
            Assert.AreEqual(s.State, r.StateAfter);
        }

        [Test]
        public void Target_DeadEnemyOrAlly_InvalidTarget_ChangesNothing()
        {
            var s = Battle(new[] { Hero("a", speed: 200) }, new[] { Foe("e0", hp: 1, speed: 10), Foe("e1", speed: 10) });
            var dead = s.Enemies[0];
            s.PlayerAct(BattleActionKind.Basic, dead);
            PassEnemyTurns(s);
            var before = Snapshot(s);

            AssertRejected(s.PlayerAct(BattleActionKind.Basic, dead), BattleActionStatus.InvalidTarget, s, before, "dead enemy");
            AssertRejected(s.PlayerAct(BattleActionKind.Basic, s.Allies[0]), BattleActionStatus.InvalidTarget, s, before, "ally");
        }

        [Test]
        public void DeadUnit_SkippedInRound()
        {
            // Quái chết trước lượt của nó.
            var s = Battle(new[] { Hero("a", speed: 120), Hero("b", speed: 100) }, new[] { Foe("e1", hp: 1, speed: 110), Foe("e2", speed: 90) });
            s.PlayerAct(BattleActionKind.Basic, s.Enemies[0]);
            Assert.AreEqual("b", s.Current.DisplayName, "e1 died before its turn");

            // Người phe ta chết trước lượt của mình: u = 0,9 chọn người thứ hai trong hai người cùng hàng trước.
            var random = new ScriptedRandom(0.0, 0.9);
            var t = CombatTestData.Battle(new[] { Hero("a", speed: 100), Hero("b", hp: 1, speed: 80) },
                CombatTestData.Stage("s", new[] { Foe("e", hp: 10000, attack: 100, speed: 120) }), random: random);
            t.EnemyAct();
            Assert.IsFalse(t.Allies[1].IsAlive);
            Assert.AreEqual("a", t.Current.DisplayName);
            t.PlayerAct(BattleActionKind.Basic);
            Assert.AreEqual(2, t.Round, "dead b has no turn, so the round ended after a");
            CollectionAssert.AreEqual(new[] { "e", "a" }, Names(t.RemainingOrder));
        }

        [Test]
        public void RoundEnd_StartsNextRound()
        {
            var s = Battle(new[] { Hero("a", speed: 100) }, new[] { Foe("e", speed: 50) });
            s.PlayerAct(BattleActionKind.Basic);
            Assert.AreEqual(1, s.Round);
            Assert.AreEqual("e", s.Current.DisplayName);
            CollectionAssert.AreEqual(new[] { "e" }, Names(s.RemainingOrder));

            s.EnemyAct();

            Assert.AreEqual(2, s.Round);
            Assert.AreEqual("a", s.Current.DisplayName);
            Assert.AreEqual(BattleState.PlayerTurn, s.State);
            CollectionAssert.AreEqual(new[] { "a", "e" }, Names(s.RemainingOrder));
        }

        [Test]
        public void EnemyAct_SingleTarget_OneRandomCall_EvenWithOneAllyAlive()
        {
            var random = new ScriptedRandom(0.0);
            var s = CombatTestData.Battle(new[] { Hero("a", hp: 1, speed: 10), Hero("b", hp: 1, speed: 10) },
                CombatTestData.Stage("s", new[] { Foe("e1", attack: 100, speed: 500), Foe("e2", attack: 100, speed: 500), Foe("e3", attack: 100, speed: 500) }),
                random: random);

            s.EnemyAct();
            Assert.AreEqual(1, random.Calls);
            Assert.IsFalse(s.Allies[0].IsAlive, "u = 0 hits the first ally");

            var last = s.EnemyAct();
            Assert.AreEqual(2, random.Calls, "one call even though only one ally was left to pick");
            Assert.AreEqual(BattleState.Lost, last.StateAfter);
        }

        [Test]
        public void EnemyTarget_Weights321_AtBoundaries()
        {
            var rows = new[]
            {
                (0.49, BattlePosition.Front), (0.51, BattlePosition.Mid), (0.83, BattlePosition.Mid), (0.84, BattlePosition.Back),
            };
            foreach (var (u, expected) in rows)
            {
                var team = new[] { Hero("tank", CharacterClass.Tank), Hero("sup", CharacterClass.Support), Hero("mage", CharacterClass.Mage) };
                var s = CombatTestData.Battle(team, CombatTestData.Stage("s", new[] { Foe("e", speed: 500) }), random: new ScriptedRandom(u));

                var hit = s.EnemyAct().Hits.Single();

                Assert.AreEqual(expected, hit.Target.Position, $"u = {u}");
            }
        }

        [Test]
        public void EnemyAct_OnPlayerTurn_NotYourTurn_ChangesNothing()
        {
            var random = new ScriptedRandom();
            var s = CombatTestData.Battle(new[] { Hero("a", speed: 200) }, CombatTestData.Stage("s", new[] { Foe("e") }), random: random);
            var before = Snapshot(s);

            AssertRejected(s.EnemyAct(), BattleActionStatus.NotYourTurn, s, before, "EnemyAct");
            Assert.AreEqual(0, random.Calls);
        }

        [Test]
        public void PlayerAct_OnEnemyTurn_NotYourTurn_ChangesNothing()
        {
            var s = Battle(new[] { Hero("a", speed: 50) }, new[] { Foe("e", speed: 200) });
            var before = Snapshot(s);

            AssertRejected(s.PlayerAct(BattleActionKind.Basic), BattleActionStatus.NotYourTurn, s, before, "PlayerAct");
        }

        [Test]
        public void LastEnemyOfOnlyWave_Won()
        {
            var s = Battle(new[] { Hero("a", speed: 200) }, new[] { Foe("e", hp: 1) });

            var r = s.PlayerAct(BattleActionKind.Basic);

            Assert.IsTrue(r.WaveCleared);
            Assert.IsFalse(r.NextWaveStarted);
            Assert.AreEqual(BattleState.Won, r.StateAfter);
            Assert.AreEqual(BattleState.Won, s.State);
        }

        [Test]
        public void LastAllyDies_Lost()
        {
            var s = Battle(new[] { Hero("a", hp: 1, speed: 10) }, new[] { Foe("e", attack: 100, speed: 200) });

            var r = s.EnemyAct();

            Assert.IsFalse(r.WaveCleared);
            Assert.AreEqual(BattleState.Lost, r.StateAfter);
            Assert.AreEqual(BattleState.Lost, s.State);
        }

        [Test]
        public void AfterEnd_AllActionsReturnEnded_ChangeNothing()
        {
            var won = Battle(new[] { Hero("a", speed: 200) }, new[] { Foe("e", hp: 1) });
            won.PlayerAct(BattleActionKind.Basic);
            var lost = Battle(new[] { Hero("a", hp: 1, speed: 10) }, new[] { Foe("e", attack: 100, speed: 200) });
            lost.EnemyAct();

            foreach (var (label, s) in new[] { ("won", won), ("lost", lost) })
            {
                var state = s.State;
                var before = Snapshot(s);
                AssertRejected(s.PlayerAct(BattleActionKind.Basic), BattleActionStatus.Ended, s, before, $"{label} PlayerAct");
                AssertRejected(s.EnemyAct(), BattleActionStatus.Ended, s, before, $"{label} EnemyAct");
                AssertRejected(s.Surrender(), BattleActionStatus.Ended, s, before, $"{label} Surrender");
                Assert.AreEqual(state, s.State, $"{label}: Surrender cannot turn a finished battle into another outcome");
            }
        }

        [Test]
        public void Surrender_Lost_NoHpChange_ThenEnded()
        {
            foreach (var enemyFirst in new[] { false, true })
            {
                var s = Battle(new[] { Hero("a", speed: enemyFirst ? 10 : 200) }, new[] { Foe("e", speed: 100) });
                var hp = s.Allies.Concat(s.Enemies).Select(u => u.Hp).ToArray();

                var r = s.Surrender();

                Assert.AreEqual(BattleActionStatus.Success, r.Status, $"enemyFirst {enemyFirst}");
                Assert.AreEqual(BattleState.Lost, r.StateAfter, $"enemyFirst {enemyFirst}");
                Assert.AreEqual(BattleState.Lost, s.State, $"enemyFirst {enemyFirst}");
                CollectionAssert.AreEqual(hp, s.Allies.Concat(s.Enemies).Select(u => u.Hp).ToArray(), $"enemyFirst {enemyFirst}");
                Assert.AreEqual(BattleActionStatus.Ended, s.PlayerAct(BattleActionKind.Basic).Status, $"enemyFirst {enemyFirst}");
                Assert.AreEqual(BattleActionStatus.Ended, s.EnemyAct().Status, $"enemyFirst {enemyFirst}");
            }
        }

        [Test]
        public void WaveCleared_NextWaveStarts_HpKept_EnemiesReplaced()
        {
            var s = Battle(new[] { Hero("a", speed: 100, attack: 1000) },
                new[] { Foe("old", hp: 100, attack: 50, speed: 200) },
                new[] { Foe("new1", hp: 777, speed: 10), Foe("new2", hp: 888, speed: 10) });
            var oldEnemy = s.Enemies[0];
            s.EnemyAct();
            Assert.AreEqual(950, s.Allies[0].Hp);

            var r = s.PlayerAct(BattleActionKind.Basic);

            Assert.IsTrue(r.WaveCleared);
            Assert.IsTrue(r.NextWaveStarted);
            Assert.AreNotEqual(BattleState.Won, r.StateAfter);
            Assert.AreEqual((1, 2), (s.WaveIndex, s.Round));
            Assert.AreEqual(950, s.Allies[0].Hp, "allies keep their HP across waves");
            CollectionAssert.AreEqual(new[] { "new1", "new2" }, Names(s.Enemies));
            Assert.AreNotSame(oldEnemy, s.Enemies[0]);
            Assert.AreEqual((777, 888), (s.Enemies[0].Hp, s.Enemies[1].Hp), "new enemies start at full HP");
            Assert.AreEqual("a", s.Current.DisplayName);
        }

        [Test]
        public void NextWave_RoundPlusOne_UnactedUnitsDoNotActFirst()
        {
            var s = Battle(new[] { Hero("fast", speed: 200), Hero("slow", speed: 100) },
                new[] { Foe("old", hp: 1, speed: 10) },
                new[] { Foe("new", speed: 250) });

            var r = s.PlayerAct(BattleActionKind.Basic);

            Assert.IsTrue(r.NextWaveStarted);
            Assert.AreEqual(2, s.Round);
            Assert.AreEqual("new", s.Current.DisplayName, "slow never acted in round 1 and must not act before the new queue");
            Assert.AreEqual(BattleState.EnemyTurn, s.State);
            CollectionAssert.AreEqual(new[] { "new", "fast", "slow" }, Names(s.RemainingOrder));
        }

        [Test]
        public void LastWaveCleared_Won_NoNextWaveFlag()
        {
            var s = Battle(new[] { Hero("a", speed: 100) },
                new[] { Foe("w0", hp: 1, speed: 10) }, new[] { Foe("w1", hp: 1, speed: 10) });
            Assert.IsTrue(s.PlayerAct(BattleActionKind.Basic).NextWaveStarted);

            var last = s.PlayerAct(BattleActionKind.Basic);

            Assert.IsTrue(last.WaveCleared);
            Assert.IsFalse(last.NextWaveStarted);
            Assert.AreEqual(BattleState.Won, last.StateAfter);
            Assert.AreEqual(1, s.WaveIndex);
        }

        [Test]
        public void GetAvailability_Basic_PerState()
        {
            var playerTurn = Battle(new[] { Hero("a", speed: 200) }, new[] { Foe("e", hp: 1) });
            var enemyTurn = Battle(new[] { Hero("a", speed: 10) }, new[] { Foe("e", speed: 200) });
            Assert.AreEqual(ActionAvailability.Available, playerTurn.GetAvailability(BattleActionKind.Basic));
            Assert.AreEqual(ActionAvailability.NotYourTurn, enemyTurn.GetAvailability(BattleActionKind.Basic));

            playerTurn.PlayerAct(BattleActionKind.Basic);
            Assert.AreEqual(BattleState.Won, playerTurn.State);
            Assert.AreEqual(ActionAvailability.Ended, playerTurn.GetAvailability(BattleActionKind.Basic));

            enemyTurn.Surrender();
            Assert.AreEqual(ActionAvailability.Ended, enemyTurn.GetAvailability(BattleActionKind.Basic));
        }

        [Test]
        public void Allies_SortedBySlotIndex()
        {
            var s = Battle(new[] { Hero("mage", CharacterClass.Mage), Hero("sup", CharacterClass.Support), Hero("tank", CharacterClass.Tank), Hero("fighter") },
                new[] { Foe("e") });

            CollectionAssert.AreEqual(new[] { "tank", "fighter", "sup", "mage" }, Names(s.Allies));
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 4 }, s.Allies.Select(a => a.SlotIndex).ToArray());
        }

        private static BattleSession Capped(int maxRounds, params EnemyDefinition[][] waves) =>
            CombatTestData.Battle(new[] { Hero("a", speed: 100) }, CombatTestData.Stage("s", waves),
                CombatTestData.Config(so => so.FindProperty("_maxRounds").intValue = maxRounds));

        [Test]
        public void Round_AboveMaxRounds_Lost()
        {
            for (var max = 1; max <= 4; max++)
            {
                var s = Capped(max, new[] { Foe("e", hp: 100000, speed: 50) });
                for (var round = 1; round <= max; round++)
                {
                    Assert.AreEqual(round, s.Round, $"max {max}");
                    s.PlayerAct(BattleActionKind.Basic);
                    var r = s.EnemyAct();
                    if (round < max) continue;

                    Assert.AreEqual(BattleState.Lost, r.StateAfter, $"max {max}: the round after the last allowed one");
                    Assert.AreEqual(BattleState.Lost, s.State, $"max {max}");
                    Assert.IsNull(s.Current, $"max {max}");
                    Assert.AreEqual(BattleActionStatus.Ended, s.PlayerAct(BattleActionKind.Basic).Status, $"max {max}");
                }
            }
        }

        [Test]
        public void Round_EqualMaxRounds_StillPlaying()
        {
            var one = Capped(1, new[] { Foe("e", hp: 100000) });
            Assert.AreEqual((1, BattleState.PlayerTurn), (one.Round, one.State), "round 1 of 1");

            var two = Capped(2, new[] { Foe("e", hp: 100000) });
            two.PlayerAct(BattleActionKind.Basic);
            two.EnemyAct();
            Assert.AreEqual((2, BattleState.PlayerTurn), (two.Round, two.State), "round 2 of 2");
            Assert.AreEqual(BattleActionStatus.Success, two.PlayerAct(BattleActionKind.Basic).Status);
            Assert.AreEqual(BattleState.EnemyTurn, two.State, "still no loss before the round ends");
        }

        [Test]
        public void NextWave_PastMaxRounds_Lost()
        {
            var late = Capped(1, new[] { Foe("w0", hp: 1) }, new[] { Foe("w1", hp: 1) });
            var r = late.PlayerAct(BattleActionKind.Basic);
            Assert.IsTrue(r.WaveCleared);
            Assert.IsFalse(r.NextWaveStarted, "round 2 would exceed the limit of 1");
            Assert.AreEqual(BattleState.Lost, r.StateAfter);
            Assert.AreEqual(0, late.WaveIndex);

            var inTime = Capped(2, new[] { Foe("w0", hp: 1) }, new[] { Foe("w1", hp: 1) });
            var ok = inTime.PlayerAct(BattleActionKind.Basic);
            Assert.IsTrue(ok.NextWaveStarted, "round 2 is allowed when the limit is 2");
            Assert.AreEqual((1, 2), (inTime.WaveIndex, inTime.Round));
        }

        [Test]
        public void ClearingLastWave_InFinalRound_Won()
        {
            var single = Capped(1, new[] { Foe("e", hp: 1) });
            single.PlayerAct(BattleActionKind.Basic);
            Assert.AreEqual((BattleState.Won, 1), (single.State, single.Round));

            var two = Capped(2, new[] { Foe("w0", hp: 1) }, new[] { Foe("w1", hp: 1) });
            two.PlayerAct(BattleActionKind.Basic);
            two.PlayerAct(BattleActionKind.Basic);
            Assert.AreEqual((BattleState.Won, 2), (two.State, two.Round), "winning does not advance the round");
        }

        [Test]
        public void ThreeWaves_FullRun_CarriesHpEnergyCooldown()
        {
            var config = CombatTestData.Config();
            var s = CombatTestData.Battle(new[] { Hero("a", speed: 100, attack: 100) },
                CombatTestData.Stage("s", new[] { Foe("w0", hp: 100, attack: 20, speed: 50) },
                    new[] { Foe("w1", hp: 150, attack: 20, speed: 50) }, new[] { Foe("w2", hp: 100, attack: 20, speed: 50) }), config);
            var hero = s.Allies[0];
            int Gain(int energy) => System.Math.Min(config.MaxEnergy, energy);

            Assert.IsTrue(s.PlayerAct(BattleActionKind.Skill).NextWaveStarted, "skill (200) kills wave 0");
            var energy = Gain(config.EnergyOnSkill);
            Assert.AreEqual((1, 2, 1000, energy, 2), (s.WaveIndex, s.Round, hero.Hp, hero.Energy, hero.SkillCooldown), "after wave 0");

            s.PlayerAct(BattleActionKind.Basic);
            s.EnemyAct();
            energy = Gain(Gain(energy + config.EnergyOnBasic) + config.EnergyOnHit);
            Assert.AreEqual((1, 3, 980, energy, 1), (s.WaveIndex, s.Round, hero.Hp, hero.Energy, hero.SkillCooldown), "wave 1 mid-fight");

            Assert.IsTrue(s.PlayerAct(BattleActionKind.Basic).NextWaveStarted);
            energy = Gain(energy + config.EnergyOnBasic);
            Assert.AreEqual((2, 4, 980, energy, 0), (s.WaveIndex, s.Round, hero.Hp, hero.Energy, hero.SkillCooldown), "after wave 1");

            var last = s.PlayerAct(BattleActionKind.Skill);
            Assert.AreEqual(BattleState.Won, last.StateAfter);
            Assert.AreEqual(980, hero.Hp);
        }
    }
}
