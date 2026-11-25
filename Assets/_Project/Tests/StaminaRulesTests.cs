using System;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Max 100, hồi 1 điểm mỗi 300 giây (như StaminaServiceTests); giờ là tham số ticks, không có đồng hồ giả.
    public class StaminaRulesTests
    {
        const int Max = 100;
        const long Regen = 300L * TimeSpan.TicksPerSecond;
        static readonly long T0 = new DateTime(2026, 11, 3, 8, 0, 0, DateTimeKind.Utc).Ticks;

        static long At(double seconds) => T0 + (long)(seconds * TimeSpan.TicksPerSecond);

        static StaminaState Full() => new StaminaState();

        // Đã trừ 10 từ đầy lúc T0: 90, mốc T0.
        static StaminaState Spent10()
        {
            var s = Full();
            Assert.IsTrue(StaminaRules.TrySpend(s, 10, T0, Max, Regen), "setup");
            return s;
        }

        static int Read(StaminaState s, long now, int max = Max, long regen = Regen)
        {
            StaminaRules.Settle(s, now, max, regen);
            return s.Current;
        }

        [Test]
        public void AnchorZero_IsFull_AtAnyMax()
        {
            var s = new StaminaState { Current = 3, AnchorUtcTicks = 0 };

            Assert.AreEqual(Max, Read(s, T0));
            Assert.AreEqual(0L, s.AnchorUtcTicks);
            Assert.AreEqual(150, Read(s, T0, max: 150), "full follows a raised Max");
            var low = new StaminaState { Current = 3, AnchorUtcTicks = 0 };
            Assert.AreEqual(80, Read(low, T0, max: 80), "and a lowered one (over-cap values are kept now, see Settle_OverMax)");
        }

        [Test]
        public void Regen_HalfSecondAroundInterval()
        {
            var s = Spent10();

            Assert.AreEqual(90, Read(s, At(299.5)), "half a second short of the first point");
            Assert.AreEqual(91, Read(s, At(300.5)), "half a second past it");
        }

        [Test]
        public void Regen_KeepsPartialProgress_AnchorMovesByWholeIntervals()
        {
            var s = Spent10();

            Assert.AreEqual(91, Read(s, At(450)));
            Assert.AreEqual(T0 + Regen, s.AnchorUtcTicks);
            Assert.AreEqual(91, Read(s, At(450)), "settling twice at the same time changes nothing");
            Assert.AreEqual(T0 + Regen, s.AnchorUtcTicks);
        }

        [Test]
        public void Regen_ReachesMax_AnchorBackToZero()
        {
            var s = Spent10();

            Assert.AreEqual(Max, Read(s, At(3000)));
            Assert.AreEqual(0L, s.AnchorUtcTicks, "full again: no regen is pending");
            Assert.AreEqual(Max, Read(s, At(1000000)), "stays at Max however long the absence");
        }

        [Test]
        public void LongAbsence_FromZero_FullAfter500Minutes()
        {
            var s = Full();
            Assert.IsTrue(StaminaRules.TrySpend(s, Max, T0, Max, Regen));
            Assert.AreEqual(0, s.Current);

            Assert.AreEqual(99, Read(s, At(30000 - 0.5)), "half a second before 500 minutes");
            Assert.AreEqual(Max, Read(s, At(30000)));
            Assert.AreEqual(0L, s.AnchorUtcTicks);
        }

        [Test]
        public void TrySpend_FromFull_SetsAnchorToNow()
        {
            var s = Full();
            var now = At(7 * 60);

            Assert.IsTrue(StaminaRules.TrySpend(s, 10, now, Max, Regen));

            Assert.AreEqual((90, now), (s.Current, s.AnchorUtcTicks), "regen starts when leaving full");
        }

        [Test]
        public void TrySpend_NotFull_KeepsAnchor()
        {
            var s = Spent10();

            Assert.IsTrue(StaminaRules.TrySpend(s, 10, At(450), Max, Regen));

            Assert.AreEqual((81, T0 + Regen), (s.Current, s.AnchorUtcTicks), "settled to 91 first, then spent; partial progress kept");
        }

        [Test]
        public void TrySpend_SettlesBeforeComparing_RejectKeepsOnlySettledPart()
        {
            var s = new StaminaState { Current = 5, AnchorUtcTicks = T0 };
            var now = At(650);

            Assert.IsFalse(StaminaRules.TrySpend(s, 8, now, Max, Regen), "5 + 2 regen points = 7 < 8");
            Assert.AreEqual((7, T0 + 2 * Regen), (s.Current, s.AnchorUtcTicks), "the settled regen stays, nothing is spent");

            Assert.IsTrue(StaminaRules.TrySpend(s, 7, now, Max, Regen), "7 is affordable once regen counts, a stale 5 would reject");
            Assert.AreEqual((0, T0 + 2 * Regen), (s.Current, s.AnchorUtcTicks));
        }

        [Test]
        public void TrySpend_ExactBalance_ToZero_OneMoreRejected()
        {
            var s = Full();

            Assert.IsFalse(StaminaRules.TrySpend(s, Max + 1, T0, Max, Regen), "one over the balance");
            Assert.AreEqual((Max, 0L), (s.Current, s.AnchorUtcTicks), "a rejection from full changes nothing");

            Assert.IsTrue(StaminaRules.TrySpend(s, Max, T0, Max, Regen), "exactly the balance");
            Assert.AreEqual((0, T0), (s.Current, s.AnchorUtcTicks));
        }

        [Test]
        public void TrySpend_ZeroOrNegative()
        {
            var s = Full();

            Assert.IsTrue(StaminaRules.TrySpend(s, 0, T0, Max, Regen), "a story stage costs nothing");
            Assert.AreEqual((Max, 0L), (s.Current, s.AnchorUtcTicks), "nothing changed, no anchor set");

            Assert.IsFalse(StaminaRules.TrySpend(s, -1, T0, Max, Regen));
            Assert.AreEqual((Max, 0L), (s.Current, s.AnchorUtcTicks), "a negative cost never refills");

            Assert.IsTrue(StaminaRules.TrySpend(s, 30, T0, Max, Regen));
            Assert.IsFalse(StaminaRules.TrySpend(s, -5, T0, Max, Regen));
            Assert.AreEqual((70, T0), (s.Current, s.AnchorUtcTicks), "also not when partly spent");
        }

        [Test]
        public void ClockBack_NoRegen_AnchorMovesToNow_CurrentKept()
        {
            var s = Spent10();
            var back = At(-3600);

            Assert.AreEqual(90, Read(s, back), "a clock moved back never reduces or adds");
            Assert.AreEqual(back, s.AnchorUtcTicks);

            Assert.AreEqual(91, Read(s, back + (long)(300.5 * TimeSpan.TicksPerSecond)), "regen resumes from the new anchor");
        }

        [Test]
        public void MaxLowered_CurrentKept_AnchorZero()
        {
            var s = new StaminaState { Current = 90, AnchorUtcTicks = T0 };

            Assert.AreEqual(90, Read(s, T0, max: 50), "over the lowered Max: kept, never taken away (spec 5.4)");
            Assert.AreEqual(0L, s.AnchorUtcTicks, "at or over Max nothing regenerates");
        }

        [Test]
        public void CorruptNegativeCurrent_TreatedAsZero()
        {
            var s = new StaminaState { Current = -50, AnchorUtcTicks = T0 };

            Assert.AreEqual(0, Read(s, T0));
            Assert.AreEqual(1, Read(s, At(300.5)), "regen counts from zero, not from -50");
        }

        [Test]
        public void InvalidRegen_NoThrow_NoRegen()
        {
            foreach (var regen in new[] { 0L, -Regen })
            {
                var s = Full();
                Assert.DoesNotThrow(() => StaminaRules.TrySpend(s, 10, T0, Max, regen), $"regen {regen}");

                Assert.AreEqual(90, Read(s, At(30 * 86400), regen: regen), $"regen {regen}: nothing comes back");
                Assert.AreEqual(T0, s.AnchorUtcTicks, $"regen {regen}: the anchor stays");
            }
        }
            [Test]
        public void Settle_OverMax_KeepsCurrent_NoRegen()
        {
            var s = new StaminaState { Current = 250, AnchorUtcTicks = 0 };

            Assert.AreEqual(250, Read(s, At(10 * 3600)));
            Assert.AreEqual(0L, s.AnchorUtcTicks);
        }

        [Test]
        public void TrySpend_FromOverCap_StaysOverCapUntilBelowMax()
        {
            var s = new StaminaState { Current = 250, AnchorUtcTicks = 0 };

            Assert.IsTrue(StaminaRules.TrySpend(s, 10, T0, Max, Regen));
            Assert.AreEqual(240, s.Current);
            Assert.AreEqual(0L, s.AnchorUtcTicks, "still over Max: nothing regenerates");
            Assert.AreEqual(240, Read(s, At(3 * 3600)), "not pulled back to 100");
            Assert.AreEqual(0L, s.AnchorUtcTicks);
        }

        [Test]
        public void TrySpend_CrossingBelowMax_StartsRegen()
        {
            var s = new StaminaState { Current = 105, AnchorUtcTicks = 0 };

            Assert.IsTrue(StaminaRules.TrySpend(s, 20, T0, Max, Regen));
            Assert.AreEqual(85, s.Current);
            Assert.AreEqual(T0, s.AnchorUtcTicks);
            Assert.AreEqual(86, Read(s, At(300 + 5)));
        }

        [Test]
        public void TrySpend_FromOver_DownToExactlyMax_NoAnchor()
        {
            var s = new StaminaState { Current = 110, AnchorUtcTicks = 0 };

            Assert.IsTrue(StaminaRules.TrySpend(s, 10, T0, Max, Regen));
            Assert.AreEqual(100, s.Current);
            Assert.AreEqual(0L, s.AnchorUtcTicks, "full, not regenerating");
        }

        [Test]
        public void Add_CapsAt999_ReportsGranted()
        {
            var s = new StaminaState { Current = 950, AnchorUtcTicks = 0 };

            Assert.AreEqual(49, StaminaRules.Add(s, 100, T0, Max, Regen, 999));
            Assert.AreEqual(999, s.Current);
            Assert.AreEqual(0L, s.AnchorUtcTicks);
        }

        [Test]
        public void Add_BelowMax_NoWaste()
        {
            var s = new StaminaState { Current = 40, AnchorUtcTicks = T0 };

            Assert.AreEqual(100, StaminaRules.Add(s, 100, T0, Max, Regen, 999));
            Assert.AreEqual(140, s.Current);
            Assert.AreEqual(0L, s.AnchorUtcTicks, "over Max: not regenerating");
        }

        [Test]
        public void Add_WhileRegenerating_KeepsAnchorAndNoWaste()
        {
            var s = Spent10();

            Assert.AreEqual(5, StaminaRules.Add(s, 5, At(450), Max, Regen, 999));
            Assert.AreEqual(96, s.Current, "91 regenerated + 5");
            Assert.AreEqual(T0 + Regen, s.AnchorUtcTicks, "still below Max, regen continues");
        }

        [Test]
        public void Add_UnderMaxRegenState_SettlesFirst()
        {
            var s = Spent10();

            Assert.AreEqual(20, StaminaRules.Add(s, 20, At(3000), Max, Regen, 999));
            Assert.AreEqual(120, s.Current, "regenerated to 100 first, then +20");
            Assert.AreEqual(0L, s.AnchorUtcTicks);
        }

        [Test]
        public void Add_ClockBackwards_NoLoss()
        {
            var s = Spent10();

            Assert.AreEqual(3, StaminaRules.Add(s, 3, At(-1000), Max, Regen, 999));
            Assert.AreEqual(93, s.Current);
        }

        [Test]
        public void Add_NonPositiveOrCapBelowCurrent_GrantsNothing_TakesNothing()
        {
            var s = new StaminaState { Current = 500, AnchorUtcTicks = 0 };

            Assert.AreEqual(0, StaminaRules.Add(s, 0, T0, Max, Regen, 999));
            Assert.AreEqual(0, StaminaRules.Add(s, 10, T0, Max, Regen, 300), "cap below current");
            Assert.AreEqual(500, s.Current);
        }
}
}
