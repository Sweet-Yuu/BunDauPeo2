using System;
using BunDauPeo.Core;

namespace BunDauPeo.Restaurant
{
    public enum UpgradeBuyStatus { Success, UnknownUpgrade, InvalidUpgrade, MaxLevel, NotEnoughCurrency }

    // NewLevel là cấp sau lần mua; khi thất bại là cấp hiện tại.
    public sealed class UpgradeBuyResult
    {
        public UpgradeBuyStatus Status;
        public bool Saved;
        public int NewLevel;
    }

    public sealed class UpgradeService
    {
        private readonly PlayerSave _save;
        private readonly Wallet _wallet;
        private readonly UpgradeCatalog _catalog;
        private readonly Func<bool> _persist;

        public event Action Changed;

        public UpgradeService(PlayerSave save, Wallet wallet, UpgradeCatalog catalog, Func<bool> persist)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
        }

        // Lấy dòng cao nhất như RestaurantProfile, để hai nơi không bao giờ lệch nhau khi save có dòng trùng.
        public int GetLevel(string id)
        {
            var level = 0;
            foreach (var entry in _save.Upgrades)
                if (entry.UpgradeId == id && entry.Level > level) level = entry.Level;
            return level;
        }

        // Thứ tự là hợp đồng (spec 4.8): tra id, Validate, MaxLevel, đủ tiền, trừ tiền, tăng cấp, Changed, lưu.
        // Mọi bước có thể thất bại đứng trước bước đầu tiên đổi trạng thái (TrySpend).
        public UpgradeBuyResult TryBuy(string id)
        {
            if (!_catalog.TryGet(id, out var upgrade)) return Failure(UpgradeBuyStatus.UnknownUpgrade, 0);
            var level = GetLevel(id);
            if (upgrade.Validate().Count > 0) return Failure(UpgradeBuyStatus.InvalidUpgrade, level);
            if (level >= upgrade.MaxLevel) return Failure(UpgradeBuyStatus.MaxLevel, level);

            var cost = upgrade.Levels[level].Cost;
            if (!_wallet.CanAfford(cost)) return Failure(UpgradeBuyStatus.NotEnoughCurrency, level);
            if (!_wallet.TrySpend(cost)) return Failure(UpgradeBuyStatus.NotEnoughCurrency, level);

            SetLevel(id, level + 1);
            Changed.SafeInvoke();
            // Lưu lỗi thì giữ nguyên bộ nhớ và không hoàn tiền; OnApplicationPause/Quit sẽ thử lưu lại.
            return new UpgradeBuyResult { Status = UpgradeBuyStatus.Success, Saved = _persist(), NewLevel = level + 1 };
        }

        private void SetLevel(string id, int level)
        {
            var entry = new UpgradeLevel { UpgradeId = id, Level = level };
            var index = _save.Upgrades.FindIndex(e => e.UpgradeId == id);
            if (index < 0) _save.Upgrades.Add(entry);
            else _save.Upgrades[index] = entry;
        }

        private static UpgradeBuyResult Failure(UpgradeBuyStatus status, int level) =>
            new() { Status = status, Saved = false, NewLevel = level };
    }
}
