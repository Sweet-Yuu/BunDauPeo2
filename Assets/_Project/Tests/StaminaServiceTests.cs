using System;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Max 100, hồi 1 điểm mỗi 300 giây (số mặc định của CombatConfig); đồng hồ giả là một biến UTC.
    public class StaminaServiceTests
    {
        private const long Regen = 300L * TimeSpan.TicksPerSecond;

        private static readonly DateTime T0 = new DateTime(2026, 11, 3, 8, 0, 0, DateTimeKind.Utc);

        private PlayerSave _save;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _save = new PlayerSave();
            _now = T0;
        }

        private StaminaService Service(Action<UnityEditor.SerializedObject> edit = null) =>
            new StaminaService(_save, CombatTestData.Config(edit), () => _now);

        private static bool Persisted() => true;

        // Stub persist: đếm số lần gọi và chụp save lúc được gọi.
        private sealed class PersistStub
        {
            private readonly PlayerSave _save;
            private readonly bool _result;

            public PersistStub(PlayerSave save, bool result = true)
            {
                _save = save;
                _result = result;
            }

            public int Calls { get; private set; }
            public int SeenCurrent { get; private set; }
            public long SeenAnchor { get; private set; }

            public bool Persist()
            {
                Calls++;
                SeenCurrent = _save.Stamina.Current;
                SeenAnchor = _save.Stamina.AnchorUtcTicks;
                return _result;
            }
        }

        [Test]
        public void AnchorZero_IsFull_SecondsToNextZero()
        {
            var s = Service();

            Assert.AreEqual(100, s.Current);
            Assert.AreEqual(100, s.Max);
            Assert.AreEqual(0d, s.SecondsToNext);
        }

        [Test]
        public void Regen_HalfSecondAroundInterval()
        {
            var s = Service();
            Assert.AreEqual(StaminaSpendStatus.Spent, s.TrySpend(10, Persisted));

            _now = T0.AddSeconds(299.5);
            Assert.AreEqual(90, s.Current, "half a second short of the first point");
            _now = T0.AddSeconds(300.5);
            Assert.AreEqual(91, s.Current, "half a second past it");
        }

        [Test]
        public void Regen_KeepsPartialProgress()
        {
            var s = Service();
            s.TrySpend(10, Persisted);

            _now = T0.AddSeconds(450);

            Assert.AreEqual(91, s.Current);
            Assert.AreEqual(150d, s.SecondsToNext, 1e-6, "halfway to the next point, not reset to 300");
            Assert.AreEqual(T0.Ticks + Regen, _save.Stamina.AnchorUtcTicks, "the anchor moves by whole intervals only");
        }

        [Test]
        public void Regen_ReachesMax_AnchorBackToZero()
        {
            var s = Service();
            s.TrySpend(10, Persisted);

            _now = T0.AddSeconds(3000);
            Assert.AreEqual(100, s.Current);
            Assert.AreEqual(0L, _save.Stamina.AnchorUtcTicks, "full again: no regen is pending");
            Assert.AreEqual(0d, s.SecondsToNext);

            _now = T0.AddSeconds(1000000);
            Assert.AreEqual(100, s.Current, "stays at Max however long the absence");
        }

        [Test]
        public void LongAbsence_FromZero_FullAfter500Minutes()
        {
            var s = Service();
            Assert.AreEqual(StaminaSpendStatus.Spent, s.TrySpend(100, Persisted));
            Assert.AreEqual(0, s.Current);

            _now = T0.AddSeconds(30000 - 0.5);
            Assert.AreEqual(99, s.Current, "half a second before 500 minutes");
            _now = T0.AddSeconds(30000);
            Assert.AreEqual(100, s.Current);
            Assert.AreEqual(0L, _save.Stamina.AnchorUtcTicks);
        }

        [Test]
        public void TrySpend_FromFull_SetsAnchorToNow()
        {
            var s = Service();
            _now = T0.AddMinutes(7);

            Assert.AreEqual(StaminaSpendStatus.Spent, s.TrySpend(10, Persisted));

            Assert.AreEqual(90, s.Current);
            Assert.AreEqual(_now.Ticks, _save.Stamina.AnchorUtcTicks, "regen starts when leaving full");
        }

        [Test]
        public void TrySpend_NotFull_KeepsAnchor()
        {
            var s = Service();
            s.TrySpend(10, Persisted);
            _now = T0.AddSeconds(450);
            var anchor = _save.Stamina.AnchorUtcTicks;
            Assert.AreEqual(91, s.Current, "setup: settle once so the anchor moved");
            anchor = _save.Stamina.AnchorUtcTicks;

            Assert.AreEqual(StaminaSpendStatus.Spent, s.TrySpend(10, Persisted));

            Assert.AreEqual(81, s.Current);
            Assert.AreEqual(anchor, _save.Stamina.AnchorUtcTicks, "the partial progress toward the next point is kept");
            Assert.AreEqual(150d, s.SecondsToNext, 1e-6);
        }

        [Test]
        public void TrySpend_NotEnough_Rejected_ChangesNothing_NoPersist()
        {
            var s = Service();
            s.TrySpend(95, Persisted);
            _now = T0.AddSeconds(100);
            var stub = new PersistStub(_save);
            Assert.AreEqual(5, s.Current);
            var current = _save.Stamina.Current;
            var anchor = _save.Stamina.AnchorUtcTicks;

            Assert.AreEqual(StaminaSpendStatus.Rejected, s.TrySpend(6, stub.Persist));

            Assert.AreEqual(0, stub.Calls);
            Assert.AreEqual((current, anchor), (_save.Stamina.Current, _save.Stamina.AnchorUtcTicks));
            Assert.AreEqual(5, s.Current);
        }

        [Test]
        public void TrySpend_ZeroOrNegative()
        {
            var s = Service();
            var stub = new PersistStub(_save);

            Assert.AreEqual(StaminaSpendStatus.Spent, s.TrySpend(0, stub.Persist), "a story stage costs nothing");
            Assert.AreEqual((100, 0L, 0), (s.Current, _save.Stamina.AnchorUtcTicks, stub.Calls), "nothing changed and nothing saved");

            Assert.AreEqual(StaminaSpendStatus.Rejected, s.TrySpend(-1, stub.Persist));
            Assert.AreEqual((100, 0L, 0), (s.Current, _save.Stamina.AnchorUtcTicks, stub.Calls), "a negative cost never refills");

            s.TrySpend(30, Persisted);
            Assert.AreEqual(StaminaSpendStatus.Rejected, s.TrySpend(-5, stub.Persist));
            Assert.AreEqual((70, 0), (s.Current, stub.Calls), "also not when partly spent");
        }

        [Test]
        public void CanAfford_AgreesWithTrySpend()
        {
            foreach (var spent in new[] { 0, 40, 100 })
            {
                var s = Service();
                _save.Stamina = new StaminaState();
                if (spent > 0) s.TrySpend(spent, Persisted);
                var current = s.Current;

                foreach (var cost in new[] { current, current + 1 })
                {
                    var expected = cost <= current;
                    Assert.AreEqual(expected, s.CanAfford(cost), $"spent {spent}, cost {cost}");
                    var fresh = new PlayerSave { Stamina = new StaminaState { Current = _save.Stamina.Current, AnchorUtcTicks = _save.Stamina.AnchorUtcTicks } };
                    var probe = new StaminaService(fresh, CombatTestData.Config(), () => _now);
                    Assert.AreEqual(expected ? StaminaSpendStatus.Spent : StaminaSpendStatus.Rejected, probe.TrySpend(cost, Persisted), $"spent {spent}, cost {cost}");
                }

                Assert.IsFalse(s.CanAfford(-1), "a negative cost is never affordable");
                _save = new PlayerSave();
            }
        }

        [Test]
        public void TrySpend_PersistedOnce_AfterTheChange()
        {
            var s = Service();
            var fromFull = new PersistStub(_save);
            Assert.AreEqual(StaminaSpendStatus.Spent, s.TrySpend(10, fromFull.Persist));
            Assert.AreEqual((1, 90, T0.Ticks), (fromFull.Calls, fromFull.SeenCurrent, fromFull.SeenAnchor), "saved after the deduction, from full");

            _now = T0.AddSeconds(100);
            var notFull = new PersistStub(_save);
            Assert.AreEqual(StaminaSpendStatus.Spent, s.TrySpend(10, notFull.Persist));
            Assert.AreEqual((1, 80, T0.Ticks), (notFull.Calls, notFull.SeenCurrent, notFull.SeenAnchor), "saved after the deduction, not full");
        }

        [Test]
        public void TrySpend_SaveFails_RestoresExactState_ReturnsSaveFailed()
        {
            var s = Service();

            var fromFull = new PersistStub(_save, result: false);
            Assert.AreEqual(StaminaSpendStatus.SaveFailed, s.TrySpend(10, fromFull.Persist));
            Assert.AreEqual(1, fromFull.Calls);
            Assert.AreEqual((100, 0L), (_save.Stamina.Current, _save.Stamina.AnchorUtcTicks), "back to full, anchor 0");
            Assert.AreEqual((100, 0d), (s.Current, s.SecondsToNext));

            // Chưa đầy, có phần hồi dở: cả Current lẫn Anchor về đúng số thô trước đó.
            s.TrySpend(10, Persisted);
            _now = T0.AddSeconds(450);
            Assert.AreEqual(91, s.Current);
            var current = _save.Stamina.Current;
            var anchor = _save.Stamina.AnchorUtcTicks;
            var failed = new PersistStub(_save, result: false);

            Assert.AreEqual(StaminaSpendStatus.SaveFailed, s.TrySpend(20, failed.Persist));

            Assert.AreEqual(1, failed.Calls);
            Assert.AreEqual((current, anchor), (_save.Stamina.Current, _save.Stamina.AnchorUtcTicks), "the raw numbers are restored");
            Assert.AreEqual((91, 150d), (s.Current, s.SecondsToNext), "reads as if never spent, partial regen kept");

            // persist ném lỗi cũng không để thể lực trừ trong bộ nhớ.
            Assert.Throws<InvalidOperationException>(() => s.TrySpend(20, () => throw new InvalidOperationException("disk")));
            Assert.AreEqual((current, anchor), (_save.Stamina.Current, _save.Stamina.AnchorUtcTicks), "restored when persist throws");
        }

        [Test]
        public void ClockBack_NoRegen_AnchorMovesToNow_CurrentKept()
        {
            var s = Service();
            s.TrySpend(10, Persisted);
            _now = T0.AddSeconds(-3600);

            Assert.AreEqual(90, s.Current, "a clock moved back never reduces or adds");
            Assert.AreEqual(_now.Ticks, _save.Stamina.AnchorUtcTicks);

            _now = _now.AddSeconds(300.5);
            Assert.AreEqual(91, s.Current, "regen resumes from the new anchor");
        }

        [Test]
        public void MaxLowered_CurrentClampedOnRead()
        {
            var full = Service(so => so.FindProperty("_maxStamina").intValue = 80);
            Assert.AreEqual(80, full.Current, "full stays full at the lower Max");
            Assert.AreEqual(80, full.Max);

            _save = new PlayerSave { Stamina = new StaminaState { Current = 90, AnchorUtcTicks = T0.Ticks } };
            var partial = Service(so => so.FindProperty("_maxStamina").intValue = 50);
            Assert.AreEqual(50, partial.Current, "90 is clamped to 50");
            Assert.AreEqual(0L, _save.Stamina.AnchorUtcTicks, "clamped to Max means full, no regen pending");
        }

        [Test]
        public void MaxRaised_WhileFull_ReadsNewMax()
        {
            var s = Service(so => so.FindProperty("_maxStamina").intValue = 150);

            Assert.AreEqual(150, s.Current);
            Assert.AreEqual(150, s.Max);
        }

        [Test]
        public void Refill_SetsFull()
        {
            var s = Service();
            s.TrySpend(60, Persisted);
            Assert.AreEqual(40, s.Current);

            s.Refill();

            Assert.AreEqual(100, s.Current);
            Assert.AreEqual(0L, _save.Stamina.AnchorUtcTicks);
            Assert.AreEqual(0d, s.SecondsToNext);
        }

        [Test]
        public void CorruptNegativeCurrent_TreatedAsZero()
        {
            _save.Stamina = new StaminaState { Current = -50, AnchorUtcTicks = T0.Ticks };
            var s = Service();

            Assert.AreEqual(0, s.Current);

            _now = T0.AddSeconds(300.5);
            Assert.AreEqual(1, s.Current, "regen counts from zero, not from -50");
        }

        [Test]
        public void InvalidRegen_NoThrow_NoRegen()
        {
            foreach (var regen in new[] { 0, -300 })
            {
                _save = new PlayerSave();
                _now = T0;
                StaminaService s = null;
                Assert.DoesNotThrow(() => s = Service(so => so.FindProperty("_staminaRegenSeconds").intValue = regen), $"regen {regen}");

                Assert.AreEqual(StaminaSpendStatus.Spent, s.TrySpend(10, Persisted), $"regen {regen}");
                _now = T0.AddDays(30);
                Assert.AreEqual(90, s.Current, $"regen {regen}: nothing comes back");
                Assert.AreEqual(0d, s.SecondsToNext, $"regen {regen}: no division by zero");
            }
        }
    }
}
