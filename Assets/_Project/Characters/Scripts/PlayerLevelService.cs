using System;
using BunDauPeo.Core;
using BunDauPeo.Rules;

namespace BunDauPeo.Characters
{
    // Cấp người chơi suy ra từ tổng EXP trong PlayerSave; không giữ trạng thái chạy nên Reset chỉ cần báo Changed.
    public sealed class PlayerLevelService
    {
        private readonly PlayerSave _save;
        private readonly Wallet _wallet;
        private readonly LevelConfig _config;
        private readonly Func<bool> _persist;

        public bool IsConfigValid { get; }
        public event Action Changed;

        public PlayerLevelService(PlayerSave save, Wallet wallet, LevelConfig config, Func<bool> persist)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
            IsConfigValid = _config.Validate().Count == 0;
        }

        // Cấp 1..MaxPlayerLevel; config lỗi coi như cấp 1.
        public int Level => IsConfigValid ? Resolve(out _) : 1;

        // EXP trong cấp hiện tại (không phải tổng).
        public long Exp
        {
            get
            {
                if (!IsConfigValid) return 0;
                Resolve(out var left);
                return left;
            }
        }

        public bool IsMaxLevel => IsConfigValid && Level >= _config.MaxPlayerLevel;
        public long ExpToNext => !IsConfigValid || IsMaxLevel ? 0 : _config.ExpToNext(Level);

        // amount <= 0 hoặc config lỗi: bỏ qua. Lên nhiều cấp được; EXP dư ở cấp tối đa bị bỏ.
        // persist = false khi người gọi lưu ngay sau đó (CombatService: lần lưu của áp state ghi cả hai).
        public void AddExp(long amount, bool persist = true)
        {
            if (amount <= 0 || !IsConfigValid) return;

            var cap = TotalToMax();
            var before = Math.Clamp(_save.PlayerExp, 0, cap);
            // Kẹp trước khi cộng để amount cực lớn không tràn long.
            var after = amount >= cap - before ? cap : before + amount;
            if (after == _save.PlayerExp) return;

            var oldLevel = Level;
            _save.PlayerExp = after;
            var newLevel = Level;
            for (var level = oldLevel + 1; level <= newLevel; level++)
            {
                _wallet.Add(CurrencyType.Coin, (long)_config.LevelUpRewardCoinPerLevel * level);
                _wallet.Add(CurrencyType.ExpPotion, _config.LevelUpRewardPotion);
            }

            if (persist) _persist();
            Changed.SafeInvoke();
        }

        // Lặp trừ ExpToNext từ tổng EXP, dừng ở cấp tối đa.
        private int Resolve(out long left)
        {
            left = Math.Max(_save.PlayerExp, 0);
            var level = 1;
            while (level < _config.MaxPlayerLevel)
            {
                var need = _config.ExpToNext(level);
                if (left < need) break;
                left -= need;
                level++;
            }

            if (level >= _config.MaxPlayerLevel) left = 0;
            return level;
        }

        private long TotalToMax()
        {
            long total = 0;
            for (var level = 1; level < _config.MaxPlayerLevel; level++) total += _config.ExpToNext(level);
            return total;
        }
    }
}
