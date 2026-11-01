using System;
using System.Linq;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Cấu hình mẫu: Chuyên cần tối đa 50, Sự cố tối đa 50, mục tiêu 300 giây, hệ số AFK 0,1 đến 0,5.
    // Giờ máy ở 12:00 ngày 2026-11-10 (hôm nay = D0); ngày d trước là D0 - d.
    public class RestaurantQualityTests
    {
        private static readonly DateTime Epoch = new DateTime(2000, 1, 1);

        private PlayerSave _save;
        private FakeClock _clock;
        private RestaurantQuality _quality;

        [SetUp]
        public void SetUp()
        {
            _save = new PlayerSave();
            _clock = new FakeClock();
            _quality = new RestaurantQuality(_save, RestaurantTestData.Config(), _clock);
        }

        // Tính độc lập với RestaurantQuality để lỗi cách tính DayIndex không tự che.
        private static int Idx(DateTime date) => (int)(date.Date - Epoch).TotalDays;

        private int DayAgo(int days) => Idx(_clock.LocalNow.Date.AddDays(-days));

        private void SeedDay(int daysAgo, double seconds) =>
            _save.DailyPresence.Add(new DailyPresenceEntry { DayIndex = DayAgo(daysAgo), Seconds = seconds });

        private void SeedFullWeekEndingYesterday()
        {
            for (var d = 1; d <= 7; d++) SeedDay(d, 300);
        }

        private void Incidents(IncidentOutcome outcome, int count)
        {
            for (var i = 0; i < count; i++) _quality.RecordIncident(outcome);
        }

        [Test]
        public void Today_IsNeverCounted()
        {
            _quality.RecordPresence(600);

            Assert.AreEqual(0, _quality.Attendance, 1e-9);
            Assert.AreEqual(600, _quality.TodaySeconds, 1e-9);

            _clock.Advance(TimeSpan.FromDays(1));

            Assert.AreEqual(50.0 / 7, _quality.Attendance, 1e-9);
            Assert.AreEqual(0, _quality.TodaySeconds, 1e-9);
        }

        [Test]
        public void FullYesterday_GivesOneSeventhOfAttendance()
        {
            SeedDay(1, 300);

            Assert.AreEqual(50.0 / 7, _quality.Attendance, 1e-9);
            Assert.AreEqual(1f, _quality.DayCredits[0]);
        }

        [Test]
        public void PartialDay_IsProportional_AndCappedAtOne()
        {
            SeedDay(1, 150);
            Assert.AreEqual(0.5f, _quality.DayCredits[0], 1e-6f);
            Assert.AreEqual(25.0 / 7, _quality.Attendance, 1e-9);

            _save.DailyPresence.Clear();
            SeedDay(1, 600);
            Assert.AreEqual(1f, _quality.DayCredits[0]);
            Assert.AreEqual(50.0 / 7, _quality.Attendance, 1e-9);
        }

        [Test]
        public void MissingDays_CountAsZero()
        {
            // Ngày 7 trước là ngày cuối của cửa sổ; ngày 8 trước đã ra ngoài. Mỗi ngày một mức khác nhau để sai thứ tự hay sai cửa sổ đều lệch.
            SeedDay(1, 300);
            SeedDay(3, 150);
            SeedDay(7, 300);
            SeedDay(8, 300);

            Assert.AreEqual(7, _quality.DayCredits.Count);
            CollectionAssert.AreEqual(new[] { 1f, 0f, 0.5f, 0f, 0f, 0f, 1f }, _quality.DayCredits.ToArray());
            Assert.AreEqual(50.0 * 2.5 / 7, _quality.Attendance, 1e-6);
        }

        [Test]
        public void AbsenceTable_MatchesSpec()
        {
            // Người chơi đủ mục tiêu bảy ngày liên tiếp (D0 - 7 … D0 - 1), sự cố tối đa; rồi vắng k ngày nữa.
            SeedFullWeekEndingYesterday();
            Incidents(IncidentOutcome.Fast, 10);
            var expected = new[] { 100.0, 93.0, 86.0, 79.0, 71.0, 64.0, 57.0, 50.0 };

            for (var k = 0; k <= 7; k++)
            {
                if (k > 0) _clock.Advance(TimeSpan.FromDays(1));
                Assert.AreEqual(expected[k], _quality.Total, 0.5, $"vắng {k} ngày");
                Assert.GreaterOrEqual(_quality.Total, _quality.IncidentPoints, $"vắng {k} ngày");
            }
        }

        [Test]
        public void Incidents_AveragePoints_FastLateMissed()
        {
            Incidents(IncidentOutcome.Fast, 10);
            Assert.AreEqual(50, _quality.IncidentPoints, 1e-9);

            _save.IncidentScores.Clear();
            Incidents(IncidentOutcome.Fast, 5);
            Incidents(IncidentOutcome.Missed, 5);
            Assert.AreEqual(25, _quality.IncidentPoints, 1e-9);

            _save.IncidentScores.Clear();
            Incidents(IncidentOutcome.Late, 10);
            Assert.AreEqual(25, _quality.IncidentPoints, 1e-9);

            // 6 kịp + 2 trễ + 2 bỏ lỡ: (12 + 2) / 20 = 0,7; coi trễ là 1 điểm hay bỏ lỡ là 1 điểm đều lệch.
            _save.IncidentScores.Clear();
            Incidents(IncidentOutcome.Fast, 6);
            Incidents(IncidentOutcome.Late, 2);
            Incidents(IncidentOutcome.Missed, 2);
            Assert.AreEqual(35, _quality.IncidentPoints, 1e-9);
        }

        [Test]
        public void EmptyIncidentSlots_CountAsZero()
        {
            Incidents(IncidentOutcome.Fast, 1);

            Assert.AreEqual(5, _quality.IncidentPoints, 1e-9);
        }

        [Test]
        public void Incidents_KeepOnlyLastTen()
        {
            Incidents(IncidentOutcome.Fast, 1);
            Incidents(IncidentOutcome.Missed, 10);

            Assert.AreEqual(10, _save.IncidentScores.Count);
            Assert.AreEqual(0, _quality.IncidentPoints, 1e-9);
            CollectionAssert.AreEqual(new int[10], _save.IncidentScores);
        }

        [Test]
        public void RecordPresence_UpsertsToday_AndKeepsEightRows()
        {
            _clock.LocalNow = new DateTime(2000, 1, 5, 12, 0, 0, DateTimeKind.Local);
            _quality.RecordPresence(10);
            _quality.RecordPresence(10);
            _quality.RecordPresence(10);

            Assert.AreEqual(1, _save.DailyPresence.Count);
            Assert.AreEqual(4, _save.DailyPresence[0].DayIndex);
            Assert.AreEqual(30, _save.DailyPresence[0].Seconds, 1e-9);

            _save.DailyPresence.Clear();
            var start = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Local);
            for (var i = 0; i < 10; i++)
            {
                _clock.LocalNow = start.AddDays(i);
                _quality.RecordPresence(1);
            }

            Assert.AreEqual(8, _save.DailyPresence.Count);
            Assert.AreEqual(Idx(start.AddDays(2)), _save.DailyPresence.Min(e => e.DayIndex));
            Assert.AreEqual(Idx(start.AddDays(9)), _save.DailyPresence.Max(e => e.DayIndex));
        }

        [Test]
        public void ClockBackwards_FutureRowsIgnoredButNotDeleted()
        {
            SeedDay(10, 300);
            SeedDay(1, 300);
            SeedDay(0, 300);
            _clock.LocalNow = _clock.LocalNow.AddDays(-3);

            // Ngày D0 - 10 là ngày thứ 7 trước của "hôm nay" mới; D0 - 1 và D0 là tương lai.
            Assert.AreEqual(50.0 / 7, _quality.Attendance, 1e-9);
            Assert.AreEqual(1f, _quality.DayCredits[6]);
            Assert.AreEqual(0f, _quality.DayCredits[0]);

            _quality.RecordPresence(5);

            Assert.AreEqual(4, _save.DailyPresence.Count);
            CollectionAssert.Contains(_save.DailyPresence.Select(e => e.DayIndex).ToList(), Idx(new DateTime(2026, 11, 10)));
            CollectionAssert.Contains(_save.DailyPresence.Select(e => e.DayIndex).ToList(), Idx(new DateTime(2026, 11, 9)));
        }

        [Test]
        public void AfkFactor_AtZeroFiftyAndHundredPoints()
        {
            Assert.AreEqual(0.1, _quality.AfkFactor, 1e-6);

            Incidents(IncidentOutcome.Fast, 10);
            Assert.AreEqual(0.3, _quality.AfkFactor, 1e-6);

            SeedFullWeekEndingYesterday();
            Assert.AreEqual(0.5, _quality.AfkFactor, 1e-6);
        }

        [Test]
        public void Stars_IsTotalOverTwenty()
        {
            Incidents(IncidentOutcome.Fast, 6);
            Incidents(IncidentOutcome.Late, 2);
            Incidents(IncidentOutcome.Missed, 2);
            SeedDay(1, 300);

            // Sự cố 35 + Chuyên cần 50/7 → sao lẻ, không làm tròn.
            Assert.AreEqual((35 + 50.0 / 7) / 20, _quality.Stars, 1e-9);
        }

        [Test]
        public void Presence_RespectsLocalMidnight()
        {
            _clock.UtcNow = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
            _clock.LocalNow = new DateTime(2026, 11, 10, 23, 59, 0, DateTimeKind.Local);
            _quality.RecordPresence(10);
            _clock.LocalNow = new DateTime(2026, 11, 11, 0, 1, 0, DateTimeKind.Local);
            _quality.RecordPresence(20);

            Assert.AreEqual(2, _save.DailyPresence.Count);
            Assert.AreEqual(10, _save.DailyPresence.Single(e => e.DayIndex == Idx(new DateTime(2026, 11, 10))).Seconds, 1e-9);
            Assert.AreEqual(20, _save.DailyPresence.Single(e => e.DayIndex == Idx(new DateTime(2026, 11, 11))).Seconds, 1e-9);
        }
    }
}
