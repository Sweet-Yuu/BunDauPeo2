using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BunDauPeo.Core;

namespace Hoang.Remake
{
    [Serializable]
    public sealed class ShopDailySaveState
    {
        public int DailyRewardDay;
        public int RewardedAdDay;
        public int RewardedAdClaims;
    }

    public enum ShopRewardResult
    {
        Success, Unavailable, AlreadyClaimed, InsufficientFunds, DailyLimit, Cancelled, SaveFailed, BalanceLimit
    }

    public sealed class GoldOffer
    {
        public string Name { get; }
        public long DiamondCost { get; }
        public long GoldAmount { get; }

        internal GoldOffer(string name, long diamondCost, long goldAmount)
        {
            Name = name;
            DiamondCost = diamondCost;
            GoldAmount = goldAmount;
        }
    }

    public sealed class AdTicket
    {
        internal AdTicket() { }
    }

    public sealed class ShopDailyRewardsService : IDisposable
    {
        public const long DailyGold = 100;
        public const long DailyDiamond = 100;
        public const long AdDiamond = 50;
        public const int MaxAdsPerDay = 3;

        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static readonly DateTime Epoch = new(1970, 1, 1);
        private static readonly IReadOnlyList<GoldOffer> GoldOffers = Array.AsReadOnly(new[]
        {
            new GoldOffer("Gold pouch", 50, 1000),
            new GoldOffer("Gold chest", 200, 4500),
            new GoldOffer("Gold vault", 500, 12000),
        });

        private readonly RemakeContext _context;
        private readonly Func<DateTimeOffset> _utcNow;
        private bool _disposed;
        private bool _busy;
        private AdTicket _pending;

        public static IReadOnlyList<GoldOffer> Offers => GoldOffers;
        public bool IsReady => !_disposed && !_busy;
        public bool HasPendingAd => _pending != null;
        public DateTimeOffset Now => _utcNow();

        public ShopDailyRewardsService(RemakeContext context, Func<DateTimeOffset> utcNow = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
            _context.ResetCompleted += InvalidateAd;
        }

        public static int VietnamDay(DateTimeOffset now) => (int)(now.ToOffset(VietnamOffset).Date - Epoch).TotalDays;

        public static TimeSpan UntilVietnamMidnight(DateTimeOffset now)
        {
            var local = now.ToOffset(VietnamOffset);
            return local.Date.AddDays(1) - local.DateTime;
        }

        public bool CanClaimDaily(DateTimeOffset now) => IsReady && VietnamDay(now) > State.DailyRewardDay;

        public int RemainingAds(DateTimeOffset now)
        {
            if (_disposed) return 0;
            var today = VietnamDay(now);
            if (today < State.RewardedAdDay) return 0;
            return today > State.RewardedAdDay ? MaxAdsPerDay : Math.Max(0, MaxAdsPerDay - Math.Max(0, State.RewardedAdClaims));
        }

        public ShopRewardResult ClaimDaily(DateTimeOffset now)
        {
            if (!IsReady) return ShopRewardResult.Unavailable;
            if (!CanClaimDaily(now)) return ShopRewardResult.AlreadyClaimed;
            if (WouldOverflow(CurrencyType.Coin, DailyGold) || WouldOverflow(CurrencyType.Gem, DailyDiamond))
                return ShopRewardResult.BalanceLimit;

            return Commit(candidate =>
            {
                Add(candidate, CurrencyType.Coin, DailyGold);
                Add(candidate, CurrencyType.Gem, DailyDiamond);
                candidate.ShopDaily ??= new ShopDailySaveState();
                candidate.ShopDaily.DailyRewardDay = VietnamDay(now);
            });
        }

        public ShopRewardResult BuyGold(int offerIndex)
        {
            if (!IsReady || offerIndex < 0 || offerIndex >= Offers.Count) return ShopRewardResult.Unavailable;
            var offer = Offers[offerIndex];
            if (_context.Wallet.Get(CurrencyType.Gem) < offer.DiamondCost) return ShopRewardResult.InsufficientFunds;
            if (WouldOverflow(CurrencyType.Coin, offer.GoldAmount)) return ShopRewardResult.BalanceLimit;
            return Commit(candidate =>
            {
                Add(candidate, CurrencyType.Gem, -offer.DiamondCost);
                Add(candidate, CurrencyType.Coin, offer.GoldAmount);
            });
        }

        public AdTicket BeginAd(DateTimeOffset now)
        {
            if (!IsReady || _pending != null || RemainingAds(now) <= 0) return null;
            _pending = new AdTicket();
            return _pending;
        }

        public ShopRewardResult FinishAd(AdTicket ticket, bool completed, DateTimeOffset now)
        {
            if (!IsReady || ticket == null || !ReferenceEquals(ticket, _pending)) return ShopRewardResult.Unavailable;
            // Consume the callback capability before writing; a retry requires a new ad, never a repeated grant.
            _pending = null;
            if (!completed) return ShopRewardResult.Cancelled;
            if (RemainingAds(now) <= 0) return ShopRewardResult.DailyLimit;
            if (WouldOverflow(CurrencyType.Gem, AdDiamond)) return ShopRewardResult.BalanceLimit;
            return Commit(candidate =>
            {
                Add(candidate, CurrencyType.Gem, AdDiamond);
                candidate.ShopDaily ??= new ShopDailySaveState();
                var today = VietnamDay(now);
                var count = candidate.ShopDaily.RewardedAdDay == today ? Math.Max(0, candidate.ShopDaily.RewardedAdClaims) : 0;
                candidate.ShopDaily.RewardedAdDay = today;
                candidate.ShopDaily.RewardedAdClaims = count + 1;
            });
        }

        public ShopRewardResult ClaimDaily() => ClaimDaily(Now);
        public bool CanClaimDaily() => CanClaimDaily(Now);
        public int RemainingAds() => RemainingAds(Now);
        public AdTicket BeginAd() => BeginAd(Now);
        public ShopRewardResult FinishAd(AdTicket ticket, bool completed) => FinishAd(ticket, completed, Now);
        public Task<ShopRewardResult> ClaimDailyAsync(DateTimeOffset now) => Task.FromResult(ClaimDaily(now));
        public Task<ShopRewardResult> BuyGoldAsync(int offerIndex) => Task.FromResult(BuyGold(offerIndex));
        public Task<ShopRewardResult> FinishAdAsync(AdTicket ticket, bool completed, DateTimeOffset now) => Task.FromResult(FinishAd(ticket, completed, now));

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            InvalidateAd();
            _context.ResetCompleted -= InvalidateAd;
        }

        private ShopDailySaveState State => _context.Save.ShopDaily ?? new ShopDailySaveState();
        private void InvalidateAd() => _pending = null;
        private bool WouldOverflow(CurrencyType currency, long amount) => _context.Wallet.Get(currency) > long.MaxValue - amount;

        private ShopRewardResult Commit(Action<HoangSaveData> mutation)
        {
            _busy = true;
            try { return _context.Commit(mutation) ? ShopRewardResult.Success : ShopRewardResult.SaveFailed; }
            finally { _busy = false; }
        }

        private static void Add(HoangSaveData save, CurrencyType currency, long amount)
        {
            var index = save.Balances.FindIndex(entry => entry.Type == currency);
            var old = index < 0 ? 0 : save.Balances[index].Amount;
            var entry = new CurrencyAmount(currency, checked(old + amount));
            if (index < 0) save.Balances.Add(entry);
            else save.Balances[index] = entry;
        }
    }
}
