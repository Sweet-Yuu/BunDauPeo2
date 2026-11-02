using System;
using BunDauPeo.Core;

namespace BunDauPeo.Combat
{
    public enum StaminaSpendStatus { Spent, Rejected, SaveFailed }

    // Thể lực hồi theo giờ máy kể cả khi tắt game (spec Combat 4.5). Mọi hàm đọc chốt phần hồi vào save trước, trên số long ticks.
    public sealed class StaminaService
    {
        private readonly PlayerSave _save;
        private readonly CombatConfig _config;
        private readonly Func<DateTime> _utcNow;

        public StaminaService(PlayerSave save, CombatConfig config, Func<DateTime> utcNow)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        }

        public int Max => Math.Max(0, _config.MaxStamina);

        public int Current
        {
            get
            {
                Settle();
                return _save.Stamina.Current;
            }
        }

        // 0 khi đầy. Config hồi lỗi (<= 0) thì không bao giờ hồi, trả 0 để khỏi chia cho 0 (L-016).
        public double SecondsToNext
        {
            get
            {
                Settle();
                var state = _save.Stamina;
                if (state.AnchorUtcTicks == 0 || RegenTicks <= 0) return 0d;
                return Math.Max(0d, (double)(state.AnchorUtcTicks + RegenTicks - _utcNow().Ticks) / TimeSpan.TicksPerSecond);
            }
        }

        private long RegenTicks => _config.StaminaRegenSeconds <= 0 ? 0 : _config.StaminaRegenSeconds * TimeSpan.TicksPerSecond;

        public bool CanAfford(int cost) => cost >= 0 && Current >= cost;

        // Trừ và lưu trong một bước: persist đúng một lần sau khi trừ; trả false (hoặc ném) thì đặt lại đúng hai số thô như chưa từng trừ.
        public StaminaSpendStatus TrySpend(int cost, Func<bool> persist)
        {
            if (persist == null) throw new ArgumentNullException(nameof(persist));

            Settle();
            if (cost < 0) return StaminaSpendStatus.Rejected;
            if (cost == 0) return StaminaSpendStatus.Spent;
            var state = _save.Stamina;
            if (state.Current < cost) return StaminaSpendStatus.Rejected;

            var previousCurrent = state.Current;
            var previousAnchor = state.AnchorUtcTicks;
            state.Current -= cost;
            // Từ đầy thì bắt đầu hồi từ bây giờ; chưa đầy thì giữ mốc, phần hồi dở không mất.
            if (previousAnchor == 0) state.AnchorUtcTicks = _utcNow().Ticks;

            bool saved;
            try
            {
                saved = persist();
            }
            catch
            {
                Restore(state, previousCurrent, previousAnchor);
                throw;
            }

            if (saved) return StaminaSpendStatus.Spent;

            Restore(state, previousCurrent, previousAnchor);
            return StaminaSpendStatus.SaveFailed;
        }

        // Chỉ cho dải DEV; không lưu, lần lưu kế tiếp của session sẽ ghi.
        public void Refill()
        {
            _save.Stamina.Current = Max;
            _save.Stamina.AnchorUtcTicks = 0;
        }

        private static void Restore(StaminaState state, int current, long anchor)
        {
            state.Current = current;
            state.AnchorUtcTicks = anchor;
        }

        private void Settle()
        {
            _save.Stamina ??= new StaminaState();
            var state = _save.Stamina;
            var max = Max;
            if (state.AnchorUtcTicks == 0)
            {
                state.Current = max;
                return;
            }

            var current = Math.Max(0, state.Current);
            var now = _utcNow().Ticks;
            if (now < state.AnchorUtcTicks)
            {
                // Đồng hồ lùi: không hồi và không trừ, chỉ dời mốc về hiện tại.
                state.AnchorUtcTicks = now;
            }
            else if (RegenTicks > 0)
            {
                var points = (now - state.AnchorUtcTicks) / RegenTicks;
                current = (int)Math.Min(max, current + points);
                state.AnchorUtcTicks += points * RegenTicks;
            }

            if (current >= max)
            {
                current = max;
                state.AnchorUtcTicks = 0;
            }

            state.Current = current;
        }
    }
}
