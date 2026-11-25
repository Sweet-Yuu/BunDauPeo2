using System;
using BunDauPeo.Rules;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class MissionRulesTests
    {
        static DateTime Utc(int y, int mo, int d, int h, int mi, int s) => new DateTime(y, mo, d, h, mi, s, DateTimeKind.Utc);

        [Test]
        public void DayIndex_RollsAtMidnightUtcPlus7()
        {
            var before = MissionRules.DayIndex(Utc(2026, 11, 20, 16, 59, 59));
            var after = MissionRules.DayIndex(Utc(2026, 11, 20, 17, 0, 0));
            Assert.AreEqual(before + 1, after);
            Assert.AreEqual(after, MissionRules.DayIndex(Utc(2026, 11, 21, 16, 59, 59)));
        }

        [Test]
        public void RollDay_ResetsDailyKeepsFreePullAndNewbie()
        {
            var m = new MissionState
            {
                DailyDay = 5, DailyWins = 4, DailyPulls = 3, DailyClaimed = { "a" }, FreePullDay = 5, NewbieClaimed = { "n" },
            };
            var now = Utc(2026, 11, 21, 3, 0, 0);

            MissionRules.RollDay(m, now);

            Assert.AreEqual(MissionRules.DayIndex(now), m.DailyDay);
            Assert.AreEqual(0, m.DailyWins);
            Assert.AreEqual(0, m.DailyPulls);
            Assert.AreEqual(0, m.DailyClaimed.Count);
            Assert.AreEqual(5, m.FreePullDay);
            CollectionAssert.AreEqual(new[] { "n" }, m.NewbieClaimed);

            m.DailyWins = 2;
            MissionRules.RollDay(m, now.AddHours(1));
            Assert.AreEqual(2, m.DailyWins, "same day: nothing reset");
        }

        [Test]
        public void InWindow_Boundaries()
        {
            var am = new MissionData { WindowStartHour = 0, WindowEndHour = 12 };
            var pm = new MissionData { WindowStartHour = 12, WindowEndHour = 24 };
            // Giờ VN = UTC + 7.
            var v1159 = Utc(2026, 11, 21, 4, 59, 59);
            var v1200 = Utc(2026, 11, 21, 5, 0, 0);
            var v2359 = Utc(2026, 11, 21, 16, 59, 59);
            var v0000 = Utc(2026, 11, 21, 17, 0, 0);

            Assert.IsTrue(MissionRules.InWindow(am, v1159));
            Assert.IsFalse(MissionRules.InWindow(pm, v1159));
            Assert.IsFalse(MissionRules.InWindow(am, v1200));
            Assert.IsTrue(MissionRules.InWindow(pm, v1200));
            Assert.IsTrue(MissionRules.InWindow(pm, v2359));
            Assert.IsFalse(MissionRules.InWindow(am, v2359));
            Assert.IsTrue(MissionRules.InWindow(am, v0000));
            Assert.IsFalse(MissionRules.InWindow(pm, v0000));
        }

        [Test]
        public void UntilNextDay_CountsToMidnightUtcPlus7()
        {
            Assert.AreEqual(TimeSpan.FromHours(1), MissionRules.UntilNextDay(Utc(2026, 11, 21, 16, 0, 0)));
            Assert.AreEqual(TimeSpan.FromHours(24), MissionRules.UntilNextDay(Utc(2026, 11, 21, 17, 0, 0)));
            Assert.AreEqual(TimeSpan.FromSeconds(1), MissionRules.UntilNextDay(Utc(2026, 11, 21, 16, 59, 59)));
        }
    }
}
