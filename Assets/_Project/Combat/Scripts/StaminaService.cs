using System;
using BunDauPeo.Core;
using BunDauPeo.Rules;

namespace BunDauPeo.Combat
{
    // Chỉ HIỆN thể lực: server trừ và hồi theo giờ server (spec con 6.2). Mọi hàm đọc chốt phần hồi trên bản sao của
    // save.Stamina, không ghi lại: field này chỉ đổi khi áp ServerState.
    public sealed class StaminaService
    {
        private readonly PlayerSave _save;
        private readonly CombatConfig _config;
        private readonly Func<DateTime> _utcNow;
        private readonly StaminaState _scratch = new StaminaState();

        public StaminaService(PlayerSave save, CombatConfig config, Func<DateTime> utcNow)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        }

        public int Max => Math.Max(0, _config.MaxStamina);

        public int Current => Settled().Current;

        // 0 khi đầy. Config hồi lỗi (<= 0) thì không bao giờ hồi, trả 0 để khỏi chia cho 0 (L-016).
        public double SecondsToNext
        {
            get
            {
                var state = Settled();
                if (state.AnchorUtcTicks == 0 || RegenTicks <= 0) return 0d;
                return Math.Max(0d, (double)(state.AnchorUtcTicks + RegenTicks - _utcNow().Ticks) / TimeSpan.TicksPerSecond);
            }
        }

        private long RegenTicks => _config.StaminaRegenSeconds <= 0 ? 0 : _config.StaminaRegenSeconds * TimeSpan.TicksPerSecond;

        public bool CanAfford(int cost) => cost >= 0 && Current >= cost;

        private StaminaState Settled()
        {
            var raw = _save.Stamina;
            // Dùng lại một bản nháp: thanh tài nguyên đọc mỗi khung, mỗi lần new là một lần cấp phát. Chủ yếu đọc ngay, không giữ.
            _scratch.Current = raw?.Current ?? 0;
            _scratch.AnchorUtcTicks = raw?.AnchorUtcTicks ?? 0;
            StaminaRules.Settle(_scratch, _utcNow().Ticks, Max, RegenTicks);
            return _scratch;
        }
    }
}
