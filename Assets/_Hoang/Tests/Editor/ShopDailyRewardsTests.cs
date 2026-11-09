using System;
using System.Linq;
using System.Text.RegularExpressions;
using BunDauPeo.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hoang.Remake.Tests
{
    public class ShopDailyRewardsTests
    {
        private RemakeContext _context;
        private ShopDailyRewardsService _service;
        private HoangSaveData _written;
        private bool _allowWrite;
        private DateTimeOffset _now;

        [SetUp]
        public void SetUp()
        {
            _written = null;
            _allowWrite = true;
            _now = new DateTimeOffset(2026, 11, 8, 8, 0, 0, TimeSpan.Zero);
            _context = new RemakeContext(new HoangSaveData(), candidate =>
            {
                if (!_allowWrite) return false;
                _written = JsonUtility.FromJson<HoangSaveData>(JsonUtility.ToJson(candidate));
                return true;
            });
            _context.Save.Balances.Add(new CurrencyAmount(CurrencyType.Coin, 1000));
            _context.Save.Balances.Add(new CurrencyAmount(CurrencyType.Gem, 5000));
            _context.Save.Balances.Add(new CurrencyAmount(CurrencyType.Material, 17));
            _service = new ShopDailyRewardsService(_context, () => _now);
        }

        [TearDown]
        public void TearDown() => _service.Dispose();

        [Test]
        public void VietnamDay_ChangesAtMidnight_IndependentOfDisplayedOffset()
        {
            var before = new DateTimeOffset(2026, 11, 8, 16, 59, 59, TimeSpan.Zero);
            var after = before.AddSeconds(1);
            Assert.AreEqual(ShopDailyRewardsService.VietnamDay(before) + 1, ShopDailyRewardsService.VietnamDay(after));
            Assert.AreEqual(ShopDailyRewardsService.VietnamDay(after), ShopDailyRewardsService.VietnamDay(after.ToOffset(TimeSpan.FromHours(-5))));
            Assert.AreEqual(TimeSpan.FromSeconds(1), ShopDailyRewardsService.UntilVietnamMidnight(before));
            Assert.AreEqual(TimeSpan.FromDays(1), ShopDailyRewardsService.UntilVietnamMidnight(after));
        }

        [Test]
        public void Daily_GrantsOnce_PersistsBothBalancesAndMarker()
        {
            Assert.AreEqual(ShopRewardResult.Success, _service.ClaimDaily(_now));
            Assert.AreEqual(1100, _context.Wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(5100, _context.Wallet.Get(CurrencyType.Gem));
            Assert.AreEqual(ShopDailyRewardsService.VietnamDay(_now), _written.ShopDaily.DailyRewardDay);
            Assert.AreEqual(5100, _written.Balances.Single(x => x.Type == CurrencyType.Gem).Amount);
            Assert.AreEqual(ShopRewardResult.AlreadyClaimed, _service.ClaimDaily(_now));
            Assert.AreEqual(17, _context.Wallet.Get(CurrencyType.Material));
            using var reopened = new ShopDailyRewardsService(new RemakeContext(_written, _ => true));
            Assert.IsFalse(reopened.CanClaimDaily(_now));
        }

        [Test]
        public void Daily_NewCalendarDayAllowsClaim_ClockRollbackDoesNot()
        {
            Assert.AreEqual(ShopRewardResult.Success, _service.ClaimDaily(_now));
            Assert.AreEqual(ShopRewardResult.AlreadyClaimed, _service.ClaimDaily(_now.AddDays(-1)));
            Assert.AreEqual(ShopRewardResult.Success, _service.ClaimDaily(_now.AddDays(1)));
            Assert.AreEqual(1200, _context.Wallet.Get(CurrencyType.Coin));
        }

        [Test]
        public void Shop_AllThreeOffersSpendAndGrantExactly_WithoutTouchingMarkersOrMaterial()
        {
            var costs = new long[] { 50, 200, 500 };
            var grants = new long[] { 1000, 4500, 12000 };
            for (var index = 0; index < costs.Length; index++)
            {
                var gold = _context.Wallet.Get(CurrencyType.Coin);
                var diamond = _context.Wallet.Get(CurrencyType.Gem);
                Assert.AreEqual(ShopRewardResult.Success, _service.BuyGold(index), $"offer={index}");
                Assert.AreEqual(gold + grants[index], _context.Wallet.Get(CurrencyType.Coin));
                Assert.AreEqual(diamond - costs[index], _context.Wallet.Get(CurrencyType.Gem));
            }
            Assert.AreEqual(17, _context.Wallet.Get(CurrencyType.Material));
            Assert.AreEqual(0, _context.Save.ShopDaily.DailyRewardDay);
        }

        [Test]
        public void Shop_RejectsInvalidOfferOrInsufficientFunds_WithoutWriting()
        {
            _context.Save.Balances.RemoveAll(x => x.Type == CurrencyType.Gem);
            Assert.AreEqual(ShopRewardResult.InsufficientFunds, _service.BuyGold(0));
            Assert.AreEqual(ShopRewardResult.Unavailable, _service.BuyGold(-1));
            Assert.AreEqual(ShopRewardResult.Unavailable, _service.BuyGold(3));
            Assert.AreEqual(1000, _context.Wallet.Get(CurrencyType.Coin));
            Assert.IsNull(_written);
        }

        [Test]
        public void FailedSave_DoesNotGrantSpendOrConsumeEligibility()
        {
            _allowWrite = false;
            Assert.AreEqual(ShopRewardResult.SaveFailed, _service.ClaimDaily(_now));
            Assert.AreEqual(ShopRewardResult.SaveFailed, _service.BuyGold(0));
            Assert.AreEqual(ShopRewardResult.SaveFailed, _service.FinishAd(_service.BeginAd(_now), true, _now));
            Assert.AreEqual(1000, _context.Wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(5000, _context.Wallet.Get(CurrencyType.Gem));
            Assert.AreEqual(0, _context.Save.ShopDaily.DailyRewardDay);
            Assert.AreEqual(3, _service.RemainingAds(_now));
            _allowWrite = true;
            Assert.AreEqual(ShopRewardResult.Success, _service.ClaimDaily(_now));
        }

        [Test]
        public void BalanceOverflow_RejectsDailyShopAndAd_WithoutConsumingClaims()
        {
            _context.Save.Balances[0] = new CurrencyAmount(CurrencyType.Coin, long.MaxValue - 50);
            Assert.AreEqual(ShopRewardResult.BalanceLimit, _service.ClaimDaily(_now));
            Assert.AreEqual(ShopRewardResult.BalanceLimit, _service.BuyGold(0));
            Assert.AreEqual(5000, _context.Wallet.Get(CurrencyType.Gem));
            _context.Save.Balances[1] = new CurrencyAmount(CurrencyType.Gem, long.MaxValue - 49);
            Assert.AreEqual(ShopRewardResult.BalanceLimit, _service.FinishAd(_service.BeginAd(_now), true, _now));
            Assert.AreEqual(0, _context.Save.ShopDaily.DailyRewardDay);
            Assert.AreEqual(3, _service.RemainingAds(_now));
            Assert.IsNull(_written);
        }

        [Test]
        public void Ad_OnlyCompletedClaimsCount_AndDuplicateCallbackCannotGrantTwice()
        {
            var cancelled = _service.BeginAd(_now);
            Assert.IsNull(_service.BeginAd(_now));
            Assert.AreEqual(ShopRewardResult.Cancelled, _service.FinishAd(cancelled, false, _now));
            Assert.AreEqual(3, _service.RemainingAds(_now));
            for (var i = 0; i < 3; i++)
            {
                var ticket = _service.BeginAd(_now);
                Assert.IsNotNull(ticket);
                Assert.AreEqual(ShopRewardResult.Success, _service.FinishAd(ticket, true, _now));
                Assert.AreEqual(ShopRewardResult.Unavailable, _service.FinishAd(ticket, true, _now));
            }
            Assert.AreEqual(5150, _context.Wallet.Get(CurrencyType.Gem));
            Assert.AreEqual(0, _service.RemainingAds(_now));
            Assert.IsNull(_service.BeginAd(_now));
        }

        [Test]
        public void Ad_TicketCannotBeUsedByAnotherService_OrAfterDispose()
        {
            var ticket = _service.BeginAd(_now);
            using var other = new ShopDailyRewardsService(_context);
            Assert.AreEqual(ShopRewardResult.Unavailable, other.FinishAd(ticket, true, _now));
            _service.Dispose();
            Assert.AreEqual(ShopRewardResult.Unavailable, _service.FinishAd(ticket, true, _now));
            Assert.AreEqual(5000, _context.Wallet.Get(CurrencyType.Gem));
        }

        [Test]
        public void Ad_CompletionAfterMidnightCountsTowardsNewDay_RollbackDoesNotReopenClaims()
        {
            var before = new DateTimeOffset(2026, 11, 8, 16, 59, 59, TimeSpan.Zero);
            var after = before.AddSeconds(1);
            var ticket = _service.BeginAd(before);
            Assert.AreEqual(ShopRewardResult.Success, _service.FinishAd(ticket, true, after));
            Assert.AreEqual(ShopDailyRewardsService.VietnamDay(after), _context.Save.ShopDaily.RewardedAdDay);
            Assert.AreEqual(2, _service.RemainingAds(after));
            Assert.AreEqual(0, _service.RemainingAds(before));
            Assert.AreEqual(3, _service.RemainingAds(after.AddDays(1)));
        }

        [Test]
        public void Reset_InvalidatesPendingAdAndRestoresDailyEligibility()
        {
            _service.ClaimDaily(_now);
            var ticket = _service.BeginAd(_now);
            Assert.IsTrue(_context.Reset());
            Assert.AreEqual(ShopRewardResult.Unavailable, _service.FinishAd(ticket, true, _now));
            Assert.IsTrue(_service.CanClaimDaily(_now));
            Assert.AreEqual(3, _service.RemainingAds(_now));
        }

        [Test]
        public void JsonWithoutShopMarkers_ReceivesDefaults_AndUnrelatedCommitKeepsMarkers()
        {
            var oldSave = JsonUtility.FromJson<HoangSaveData>("{\"Version\":1,\"Balances\":[]}");
            using var service = new ShopDailyRewardsService(new RemakeContext(oldSave, _ => true));
            Assert.IsTrue(service.CanClaimDaily(_now));
            _service.ClaimDaily(_now);
            Assert.IsTrue(_context.Commit(save => save.Language = "en"));
            Assert.AreEqual(ShopDailyRewardsService.VietnamDay(_now), _written.ShopDaily.DailyRewardDay);
        }

        [Test]
        public void SubscriberException_DoesNotInterruptCommittedRewardOrOtherSubscribers()
        {
            var notified = false;
            _context.Wallet.Changed += () => throw new InvalidOperationException("shop listener failed");
            _context.Wallet.Changed += () => notified = true;
            LogAssert.Expect(LogType.Exception, new Regex("shop listener failed"));
            Assert.AreEqual(ShopRewardResult.Success, _service.ClaimDaily(_now));
            Assert.IsTrue(notified);
            Assert.AreEqual(5100, _context.Wallet.Get(CurrencyType.Gem));
            Assert.AreEqual(ShopDailyRewardsService.VietnamDay(_now), _written.ShopDaily.DailyRewardDay);
        }
    }
}
