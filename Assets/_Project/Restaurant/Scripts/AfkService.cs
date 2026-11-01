using System;
using BunDauPeo.Core;

namespace BunDauPeo.Restaurant
{
    public readonly struct AfkCollectResult
    {
        public AfkCollectResult(long coins, bool saved)
        {
            Coins = coins;
            Saved = saved;
        }

        public long Coins { get; }
        public bool Saved { get; }
    }

    // Heartbeat / CatchUp theo spec 4.6. Mọi trạng thái cần lưu nằm trong PlayerSave.Afk; đọc `_save.Afk` mỗi lần
    // vì Reset ghi đè save tại chỗ. Chỉ số giây vắng đang chờ không lưu (view lấy một lần).
    public sealed class AfkService
    {
        private readonly PlayerSave _save;
        private readonly Wallet _wallet;
        private readonly RestaurantConfig _config;
        private readonly IClock _clock;
        private readonly RestaurantQuality _quality;
        private readonly Func<double> _currentIncomePerSecond;
        private readonly Func<bool> _persist;
        private double _pendingAbsence;

        public AfkService(PlayerSave save, Wallet wallet, RestaurantConfig config, IClock clock, RestaurantQuality quality,
            Func<double> currentIncomePerSecond, Func<bool> persist)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _quality = quality ?? throw new ArgumentNullException(nameof(quality));
            _currentIncomePerSecond = currentIncomePerSecond ?? throw new ArgumentNullException(nameof(currentIncomePerSecond));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
        }

        public double PendingTheoretical => _save.Afk.PendingTheoretical;
        public double AccumulatedSeconds => _save.Afk.AccumulatedSeconds;
        public bool IsFull => _save.Afk.AccumulatedSeconds >= MaxSeconds - 1e-6;

        private double MaxSeconds => _config.AfkMaxHours * 3600.0;

        // Rate chốt ở đây (không đọc lại lúc CatchUp), để không gian lận bằng cách xếp người giỏi sát lúc thu.
        public void Heartbeat()
        {
            var rate = _currentIncomePerSecond();
            _save.Afk.LastActiveUtcTicks = _clock.UtcNow.Ticks;
            _save.Afk.RatePerSecond = rate > 0 ? rate : 0;
        }

        public void CatchUp()
        {
            var afk = _save.Afk;
            var now = _clock.UtcNow.Ticks;
            if (afk.LastActiveUtcTicks == 0)
            {
                afk.LastActiveUtcTicks = now;
                return;
            }

            var elapsed = Math.Max(0.0, (now - afk.LastActiveUtcTicks) / (double)TimeSpan.TicksPerSecond);
            // Quán không kiếm tiền thì khoảng vắng không ăn vào trần, nếu không đầy trần mà không có gì để thu.
            if (afk.RatePerSecond > 0)
            {
                var add = Math.Max(0.0, Math.Min(elapsed, MaxSeconds - afk.AccumulatedSeconds));
                afk.PendingTheoretical += afk.RatePerSecond * add;
                afk.AccumulatedSeconds += add;
            }

            // Tiêu thụ khoảng thời gian đúng một lần: gọi lại liền sau cộng 0.
            afk.LastActiveUtcTicks = now;
            if (elapsed >= _config.AfkPopupMinSeconds) _pendingAbsence = elapsed;
        }

        // Hệ số tính lúc bấm, không phải lúc thoát.
        public long PreviewCollect()
        {
            var coins = Math.Floor(_save.Afk.PendingTheoretical * _quality.AfkFactor);
            return coins >= long.MaxValue ? long.MaxValue : coins > 0 ? (long)coins : 0;
        }

        public AfkCollectResult Collect()
        {
            if (_save.Afk.PendingTheoretical <= 0) return new AfkCollectResult(0, true);

            var coins = PreviewCollect();
            _wallet.Add(CurrencyType.Coin, coins);
            _save.Afk.PendingTheoretical = 0;
            _save.Afk.AccumulatedSeconds = 0;
            // Lưu lỗi thì giữ nguyên bộ nhớ (đã cộng Coin, đã về 0); lần lưu sau thử lại.
            return new AfkCollectResult(coins, _persist());
        }

        public double TakePendingAbsence()
        {
            var seconds = _pendingAbsence;
            _pendingAbsence = 0;
            return seconds;
        }

        // Chỉ DevOverlay dùng: lùi mốc để thử tích AFK mà không phải chờ.
        public void Rewind(TimeSpan by)
        {
            if (_save.Afk.LastActiveUtcTicks == 0) _save.Afk.LastActiveUtcTicks = _clock.UtcNow.Ticks;
            _save.Afk.LastActiveUtcTicks -= by.Ticks;
        }
    }
}
