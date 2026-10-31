using System;
using System.Collections.Generic;

namespace BunDauPeo.Core
{
    public sealed class Wallet
    {
        // Đọc ghi thẳng vào save, không giữ bản sao, để lưu game luôn thấy số mới nhất.
        private readonly PlayerSave _save;

        public event Action Changed;

        public Wallet(PlayerSave save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        // Gọi khi save bị thay từ bên ngoài (reset), để view nghe Changed vẽ lại.
        public void NotifyChanged() => Changed.SafeInvoke();

        public long Get(CurrencyType type)
        {
            var index = IndexOf(type);
            return index < 0 ? 0 : _save.Balances[index].Amount;
        }

        public void Add(CurrencyType type, long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Use TrySpend to remove currency.");
            if (amount == 0) return;

            var balance = Get(type);
            var updated = balance > long.MaxValue - amount ? long.MaxValue : balance + amount;
            if (updated == balance) return;

            Set(type, updated);
            Changed.SafeInvoke();
        }

        // Không ghi gì, không phát Changed; luôn khớp với TrySpend vì cả hai dùng chung Plan.
        public bool CanAfford(params CurrencyAmount[] costs) => Plan(costs) != null;

        public bool TrySpend(params CurrencyAmount[] costs)
        {
            var remaining = Plan(costs);
            if (remaining == null) return false;
            if (remaining.Count == 0) return true;

            foreach (var pair in remaining) Set(pair.Key, pair.Value);
            Changed.SafeInvoke();
            return true;
        }

        // Trả null nếu không đủ, ngược lại là số dư còn lại của từng loại bị trừ (rỗng khi không có dòng nào > 0).
        private Dictionary<CurrencyType, long> Plan(CurrencyAmount[] costs)
        {
            var remaining = new Dictionary<CurrencyType, long>();
            if (costs == null) return remaining;

            // Trừ dần vào số dư còn lại của từng loại thay vì cộng gộp chi phí, để các dòng cực lớn không tràn long.
            foreach (var cost in costs)
            {
                if (cost.Amount < 0) return null;
                if (cost.Amount == 0) continue;

                if (!remaining.TryGetValue(cost.Type, out var left)) left = Get(cost.Type);
                if (cost.Amount > left) return null;
                remaining[cost.Type] = left - cost.Amount;
            }

            return remaining;
        }

        private int IndexOf(CurrencyType type)
        {
            for (var i = 0; i < _save.Balances.Count; i++)
                if (_save.Balances[i].Type == type) return i;
            return -1;
        }

        private void Set(CurrencyType type, long amount)
        {
            var entry = new CurrencyAmount(type, amount);
            var index = IndexOf(type);
            if (index < 0) _save.Balances.Add(entry);
            else _save.Balances[index] = entry;
        }
    }
}
