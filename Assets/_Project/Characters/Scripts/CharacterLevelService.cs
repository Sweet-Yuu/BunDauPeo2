using System;
using BunDauPeo.Core;
using BunDauPeo.Rules;

namespace BunDauPeo.Characters
{
    // Nâng cấp bằng thuốc EXP và Coin, đồng bộ phía máy (server không biết cấp). Mẫu theo StarService.
    public sealed class CharacterLevelService
    {
        private readonly PlayerSave _save;
        private readonly Roster _roster;
        private readonly Wallet _wallet;
        private readonly CharacterCatalog _catalog;
        private readonly LevelConfig _config;
        private readonly PlayerLevelService _player;
        private readonly Func<bool> _persist;

        public bool IsConfigValid { get; }
        public event Action Changed;

        public CharacterLevelService(PlayerSave save, Roster roster, Wallet wallet, CharacterCatalog catalog,
            LevelConfig config, PlayerLevelService player, Func<bool> persist)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
            IsConfigValid = _config.Validate().Count == 0 && _player.IsConfigValid;
        }

        public int MaxLevel => _player.Level;

        // Cấp hiệu lực = min(cấp đã lưu, cấp người chơi, cấp tối đa), tối thiểu 1; không ghi đè cấp đã lưu.
        public int GetLevel(string id)
        {
            if (id == null || !_catalog.TryGet(id, out _)) return 1;
            var index = FindEntry(id);
            var stored = index < 0 ? 1 : _save.CharacterLevels[index].Level;
            return Math.Max(1, Math.Min(stored, Math.Min(_player.Level, _config.MaxPlayerLevel)));
        }

        // Giá theo cấp đích GetLevel(id) + 1.
        public (int Potion, int Coin) GetNextCost(string id)
        {
            var target = GetLevel(id) + 1;
            return (_config.PotionPerTargetLevel * target, _config.CoinPerTargetLevel * target);
        }

        public LevelUpResult TryLevelUp(string id)
        {
            if (!IsConfigValid) return Rejected(LevelUpStatus.InvalidConfig, id);
            if (id == null || !_catalog.TryGet(id, out _)) return Rejected(LevelUpStatus.Unknown, id);
            if (!_roster.IsOwned(id)) return Rejected(LevelUpStatus.NotOwned, id);
            if (GetLevel(id) >= _player.Level) return Rejected(LevelUpStatus.AtPlayerLevel, id);

            var (potion, coin) = GetNextCost(id);
            if (_wallet.Get(CurrencyType.ExpPotion) < potion) return Rejected(LevelUpStatus.NotEnoughPotion, id);
            if (_wallet.Get(CurrencyType.Coin) < coin) return Rejected(LevelUpStatus.NotEnoughCoin, id);

            if (!_wallet.TrySpend(new CurrencyAmount(CurrencyType.ExpPotion, potion), new CurrencyAmount(CurrencyType.Coin, coin)))
                return Rejected(LevelUpStatus.NotEnoughCoin, id);

            var level = GetLevel(id) + 1;
            var entry = new CharacterLevelEntry { CharacterId = id, Level = level };
            var index = FindEntry(id);
            if (index < 0) _save.CharacterLevels.Add(entry);
            else _save.CharacterLevels[index] = entry;

            var saved = _persist();
            Changed.SafeInvoke();
            return new LevelUpResult { Status = LevelUpStatus.Success, LevelAfter = level, Saved = saved };
        }

        private LevelUpResult Rejected(LevelUpStatus status, string id) =>
            new() { Status = status, LevelAfter = GetLevel(id), Saved = false };

        private int FindEntry(string id)
        {
            for (var i = 0; i < _save.CharacterLevels.Count; i++)
                if (_save.CharacterLevels[i].CharacterId == id) return i;
            return -1;
        }
    }
}
