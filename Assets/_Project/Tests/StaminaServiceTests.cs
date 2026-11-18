using System;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // StaminaService chỉ HIỆN thể lực (server trừ và hồi, spec con 6.2; luật trừ ở StaminaRulesTests): đọc chốt phần hồi trên
    // bản sao, không ghi save. Max 100, hồi 1 điểm mỗi 300 giây; đồng hồ giả là một biến UTC.
    public class StaminaServiceTests
    {
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

        // Server đã trừ `cost` lúc `at`: ghi đúng số thô như state server áp vào save.
        private void ServerSpent(int cost, DateTime at, int max = 100, int regenSeconds = 300)
        {
            _save.Stamina ??= new StaminaState();
            Assert.IsTrue(StaminaRules.TrySpend(_save.Stamina, cost, at.Ticks, max, regenSeconds * TimeSpan.TicksPerSecond));
        }

        private (int Current, long Anchor) Raw() => (_save.Stamina.Current, _save.Stamina.AnchorUtcTicks);

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
            ServerSpent(10, T0);

            _now = T0.AddSeconds(299.5);
            Assert.AreEqual(90, s.Current, "half a second short of the first point");
            _now = T0.AddSeconds(300.5);
            Assert.AreEqual(91, s.Current, "half a second past it");
        }

        [Test]
        public void Regen_KeepsPartialProgress()
        {
            var s = Service();
            ServerSpent(10, T0);

            _now = T0.AddSeconds(450);

            Assert.AreEqual(91, s.Current);
            Assert.AreEqual(150d, s.SecondsToNext, 1e-6, "halfway to the next point, not reset to 300");
        }

        [Test]
        public void Regen_ReachesMax_SecondsToNextZero()
        {
            var s = Service();
            ServerSpent(10, T0);

            _now = T0.AddSeconds(3000);
            Assert.AreEqual(100, s.Current);
            Assert.AreEqual(0d, s.SecondsToNext);

            _now = T0.AddSeconds(1000000);
            Assert.AreEqual(100, s.Current, "stays at Max however long the absence");
        }

        [Test]
        public void LongAbsence_FromZero_FullAfter500Minutes()
        {
            var s = Service();
            ServerSpent(100, T0);
            Assert.AreEqual(0, s.Current);

            _now = T0.AddSeconds(30000 - 0.5);
            Assert.AreEqual(99, s.Current, "half a second before 500 minutes");
            _now = T0.AddSeconds(30000);
            Assert.AreEqual(100, s.Current);
        }

        [Test]
        public void Reading_NeverWritesTheSave()
        {
            var s = Service();
            ServerSpent(10, T0);
            var raw = Raw();

            _now = T0.AddSeconds(450);
            Assert.AreEqual(91, s.Current);
            Assert.AreEqual(150d, s.SecondsToNext, 1e-6);
            _now = T0.AddSeconds(3000);
            Assert.AreEqual(100, s.Current);
            _now = T0.AddSeconds(-3600);
            Assert.AreEqual(90, s.Current);

            Assert.AreEqual(raw, Raw(), "only applying a server state changes the raw numbers");
        }

        [Test]
        public void CanAfford_FollowsCurrent()
        {
            var s = Service();
            ServerSpent(60, T0);

            Assert.IsTrue(s.CanAfford(40), "exactly what is left");
            Assert.IsFalse(s.CanAfford(41));
            Assert.IsTrue(s.CanAfford(0));
            Assert.IsFalse(s.CanAfford(-1), "a negative cost is never affordable");

            _now = T0.AddSeconds(300.5);
            Assert.IsTrue(s.CanAfford(41), "one point regenerated");
        }

        [Test]
        public void ClockBack_NoRegen_CurrentKept()
        {
            var s = Service();
            ServerSpent(10, T0);
            _now = T0.AddSeconds(-3600);

            Assert.AreEqual(90, s.Current, "a clock moved back never reduces or adds");
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
            Assert.AreEqual(0d, partial.SecondsToNext, "clamped to Max means full, no regen pending");
        }

        [Test]
        public void MaxRaised_WhileFull_ReadsNewMax()
        {
            var s = Service(so => so.FindProperty("_maxStamina").intValue = 150);

            Assert.AreEqual(150, s.Current);
            Assert.AreEqual(150, s.Max);
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
        public void NullStamina_ReadsFull_NoThrow()
        {
            _save.Stamina = null;

            Assert.AreEqual(100, Service().Current);
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

                ServerSpent(10, T0, regenSeconds: regen);
                _now = T0.AddDays(30);
                Assert.AreEqual(90, s.Current, $"regen {regen}: nothing comes back");
                Assert.AreEqual(0d, s.SecondsToNext, $"regen {regen}: no division by zero");
            }
        }
    }
}
