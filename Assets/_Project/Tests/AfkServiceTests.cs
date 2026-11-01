using System;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Đồng hồ giả bắt đầu 12:00 UTC ngày 2026-11-10. Cấu hình mẫu: trần 12 giờ, hệ số 0,1 (0 điểm) đến 0,5 (100 điểm), popup từ 300 giây.
    public class AfkServiceTests
    {
        private const double MaxSeconds = 12 * 3600;

        private PlayerSave _save;
        private Wallet _wallet;
        private FakeClock _clock;
        private RestaurantQuality _quality;
        private AfkService _afk;
        private double _rate;
        private int _persists;
        private bool _persistResult;

        [SetUp]
        public void SetUp()
        {
            _save = new PlayerSave();
            _wallet = new Wallet(_save);
            _clock = new FakeClock();
            _rate = 1;
            _persists = 0;
            _persistResult = true;
            var config = RestaurantTestData.Config();
            _quality = new RestaurantQuality(_save, config, _clock);
            _afk = new AfkService(_save, _wallet, config, _clock, _quality, () => _rate, () => { _persists++; return _persistResult; });
        }

        // Giống một lần thoát app rồi mở lại sau `span`: Heartbeat lúc thoát, CatchUp lúc mở.
        private void Away(TimeSpan span)
        {
            _afk.Heartbeat();
            _clock.Advance(span);
            _afk.CatchUp();
        }

        private static TimeSpan Hours(double h) => TimeSpan.FromHours(h);

        // Hệ số 0,35: Chuyên cần 12,5 (hôm qua đủ, hôm kia 3/4) cộng Sự cố 50.
        private void SetQualityTo62Point5()
        {
            var yesterday = (int)(_clock.LocalNow.Date.AddDays(-1) - new DateTime(2000, 1, 1)).TotalDays;
            _save.DailyPresence.Add(new DailyPresenceEntry { DayIndex = yesterday, Seconds = 300 });
            _save.DailyPresence.Add(new DailyPresenceEntry { DayIndex = yesterday - 1, Seconds = 225 });
            for (var i = 0; i < 10; i++) _quality.RecordIncident(IncidentOutcome.Fast);
        }

        [Test]
        public void Heartbeat_StoresTicksAndCurrentRate()
        {
            _rate = 1.5;
            _afk.Heartbeat();
            Assert.AreEqual(_clock.UtcNow.Ticks, _save.Afk.LastActiveUtcTicks);
            Assert.AreEqual(1.5, _save.Afk.RatePerSecond);

            _clock.Advance(TimeSpan.FromMinutes(7));
            _rate = 0.25;
            _afk.Heartbeat();
            Assert.AreEqual(_clock.UtcNow.Ticks, _save.Afk.LastActiveUtcTicks);
            Assert.AreEqual(0.25, _save.Afk.RatePerSecond);
        }

        [Test]
        public void CatchUp_Accumulates_RateTimesElapsed()
        {
            _rate = 2;

            Away(TimeSpan.FromSeconds(100));

            Assert.AreEqual(200, _afk.PendingTheoretical, 1e-9);
            Assert.AreEqual(100, _afk.AccumulatedSeconds, 1e-9);
            Assert.AreEqual(_clock.UtcNow.Ticks, _save.Afk.LastActiveUtcTicks);
        }

        [Test]
        public void Cap_IsTotalAcrossTwoExits()
        {
            Away(Hours(8));
            Assert.AreEqual(8 * 3600, _afk.AccumulatedSeconds, 1e-6);

            Away(Hours(8));

            Assert.AreEqual(MaxSeconds, _afk.AccumulatedSeconds, 1e-6);
            Assert.AreEqual(MaxSeconds, _afk.PendingTheoretical, 1e-6);
            Assert.IsTrue(_afk.IsFull);
        }

        [Test]
        public void Full_AccumulatesNothingMoreUntilCollect()
        {
            Away(Hours(13));
            Assert.IsTrue(_afk.IsFull);

            Away(Hours(1));
            Assert.AreEqual(MaxSeconds, _afk.AccumulatedSeconds, 1e-6);
            Assert.AreEqual(MaxSeconds, _afk.PendingTheoretical, 1e-6);

            _afk.Collect();
            Assert.IsFalse(_afk.IsFull);
            Away(Hours(1));
            Assert.AreEqual(3600, _afk.AccumulatedSeconds, 1e-6);
            Assert.AreEqual(3600, _afk.PendingTheoretical, 1e-6);
        }

        [Test]
        public void ClockBackwards_AddsZero_AndResetsAnchor()
        {
            _afk.Heartbeat();
            _clock.Advance(-Hours(2));

            _afk.CatchUp();

            Assert.AreEqual(0, _afk.PendingTheoretical, 1e-9);
            Assert.AreEqual(_clock.UtcNow.Ticks, _save.Afk.LastActiveUtcTicks);

            // Mốc đã về đồng hồ mới nên khoảng đi tiếp tính được; giữ mốc cũ thì vẫn âm và cộng 0.
            _clock.Advance(TimeSpan.FromSeconds(100));
            _afk.CatchUp();
            Assert.AreEqual(100, _afk.PendingTheoretical, 1e-9);
        }

        [Test]
        public void ClockForwardTwentyHours_CapsAtMax()
        {
            Away(Hours(20));

            Assert.AreEqual(MaxSeconds, _afk.AccumulatedSeconds, 1e-6);
            Assert.AreEqual(MaxSeconds, _afk.PendingTheoretical, 1e-6);
        }

        [Test]
        public void TwoConsecutiveCatchUps_SecondAddsZero()
        {
            Away(Hours(1));
            Assert.AreEqual(3600, _afk.PendingTheoretical, 1e-6);

            _afk.CatchUp();

            Assert.AreEqual(3600, _afk.PendingTheoretical, 1e-6);
            Assert.AreEqual(3600, _afk.AccumulatedSeconds, 1e-6);
        }

        [Test]
        public void NewGame_ZeroTicks_OnlyAnchorsNothingAdded()
        {
            // Tick = 0 mà cộng thì "now - 0" là cả thế kỷ, ra ngay trần.
            _save.Afk.RatePerSecond = 5;

            _afk.CatchUp();

            Assert.AreEqual(0, _afk.PendingTheoretical, 1e-9);
            Assert.AreEqual(0, _afk.AccumulatedSeconds, 1e-9);
            Assert.AreEqual(_clock.UtcNow.Ticks, _save.Afk.LastActiveUtcTicks);
            Assert.AreEqual(0, _afk.TakePendingAbsence());
        }

        [Test]
        public void ZeroRate_TwelveHourAbsence_DoesNotEatTheCap()
        {
            _rate = 0;
            Away(Hours(12));
            Assert.AreEqual(0, _afk.AccumulatedSeconds, 1e-9);
            Assert.AreEqual(0, _afk.PendingTheoretical, 1e-9);
            Assert.IsFalse(_afk.IsFull);

            _rate = 1;
            Away(Hours(12));

            Assert.AreEqual(MaxSeconds, _afk.AccumulatedSeconds, 1e-6);
            Assert.AreEqual(MaxSeconds, _afk.PendingTheoretical, 1e-6);
        }

        [Test]
        public void TakePendingAbsence_ReturnsSecondsThenZero()
        {
            Away(Hours(1));

            Assert.AreEqual(3600, _afk.TakePendingAbsence(), 1e-6);
            Assert.AreEqual(0, _afk.TakePendingAbsence());
        }

        [Test]
        public void ShortCatchUp_AfterLongAbsence_DoesNotOverwritePendingAbsence()
        {
            Away(Hours(1));

            // OnApplicationPause(false) lúc khởi động chạy CatchUp lần hai ngay sau lần đầu.
            _afk.CatchUp();

            Assert.AreEqual(3600, _afk.TakePendingAbsence(), 1e-6);
        }

        [Test]
        public void AbsenceBelowPopupMin_NoPendingAbsence()
        {
            Away(TimeSpan.FromSeconds(299));
            Assert.AreEqual(0, _afk.TakePendingAbsence());

            // Đúng ngưỡng 300 giây thì có.
            Away(TimeSpan.FromSeconds(300));
            Assert.AreEqual(300, _afk.TakePendingAbsence(), 1e-9);
        }

        [Test]
        public void StaleHeartbeat_FiftyNineSecondsOld_CountsOnlyThatGap()
        {
            // Lần lưu định kỳ cuối cách lúc app bị kill 59 giây; mở lại ngay thì chỉ tính khoảng đó (nghiêng về người chơi, dưới trần).
            _rate = 3;
            _afk.Heartbeat();
            _clock.Advance(TimeSpan.FromSeconds(59));

            _afk.CatchUp();

            Assert.AreEqual(3 * 59, _afk.PendingTheoretical, 1e-9);
            Assert.AreEqual(59, _afk.AccumulatedSeconds, 1e-9);
            Assert.AreEqual(0, _afk.TakePendingAbsence());
        }

        [Test]
        public void PreviewCollect_UsesFactorAtCollectTime()
        {
            // 1005 × 0,1 = 100,5 → 100; 1005 × 0,3 = 301,5 → 301.
            Away(TimeSpan.FromSeconds(1005));
            Assert.AreEqual(100, _afk.PreviewCollect());

            for (var i = 0; i < 10; i++) _quality.RecordIncident(IncidentOutcome.Fast);

            Assert.AreEqual(301, _afk.PreviewCollect());
        }

        [Test]
        public void Collect_AddsFlooredCoins_ZeroesPendingAndSeconds_PersistsOnce()
        {
            // Vắng 5 giờ 12 phút ở 1,57 Coin/giây: chờ 29 390,4; hệ số 0,35 → 10 286,64 → 10 286 (làm tròn sẽ ra 10 287).
            _rate = 1.57;
            SetQualityTo62Point5();
            Away(TimeSpan.FromSeconds(18720));
            Assert.AreEqual(29390.4, _afk.PendingTheoretical, 1e-6);

            var result = _afk.Collect();

            Assert.AreEqual(10286, result.Coins);
            Assert.IsTrue(result.Saved);
            Assert.AreEqual(10286, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(0, _afk.PendingTheoretical);
            Assert.AreEqual(0, _afk.AccumulatedSeconds);
            Assert.AreEqual(1, _persists);
        }

        [Test]
        public void Collect_PersistFails_SavedFalse_KeepsState()
        {
            Away(TimeSpan.FromSeconds(1005));
            _persistResult = false;

            var result = _afk.Collect();

            Assert.IsFalse(result.Saved);
            Assert.AreEqual(100, result.Coins);
            Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(0, _afk.PendingTheoretical);
            Assert.AreEqual(1, _persists);
        }

        [Test]
        public void Collect_NothingPending_ReturnsZero_NoPersist()
        {
            var result = _afk.Collect();

            Assert.AreEqual(0, result.Coins);
            Assert.AreEqual(0, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(0, _persists);
        }

        [Test]
        public void Rate_FrozenAtHeartbeat_ChangesOnNextHeartbeat()
        {
            _rate = 2;
            _afk.Heartbeat();
            _rate = 5;
            Assert.AreEqual(2, _save.Afk.RatePerSecond);

            _clock.Advance(TimeSpan.FromSeconds(100));
            _afk.CatchUp();
            Assert.AreEqual(200, _afk.PendingTheoretical, 1e-9);

            _afk.Heartbeat();
            Assert.AreEqual(5, _save.Afk.RatePerSecond);
        }

        [Test]
        public void Rewind_MovesAnchorBack_ThenCatchUpAccumulates()
        {
            _afk.Heartbeat();

            _afk.Rewind(Hours(1));
            Assert.AreEqual(_clock.UtcNow.Ticks - Hours(1).Ticks, _save.Afk.LastActiveUtcTicks);
            _afk.CatchUp();

            Assert.AreEqual(3600, _afk.PendingTheoretical, 1e-6);
            Assert.AreEqual(_clock.UtcNow.Ticks, _save.Afk.LastActiveUtcTicks);
        }
    }
}
