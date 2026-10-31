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

        public bool TrySpend(params CurrencyAmount[] costs)
        {
            if (costs == null || costs.Length == 0) return true;

            // Trừ dần vào số dư còn lại của từng loại thay vì cộng gộp chi phí, để các dòng cực lớn không tràn long.
            var remaining = new Dictionary<CurrencyType, long>();
            foreach (var cost in costs)
            {
                if (cost.Amount < 0) return false;
                if (cost.Amount == 0) continue;

                if (!remaining.TryGetValue(cost.Type, out var left)) left = Get(cost.Type);
                if (cost.Amount > left) return false;
                remaining[cost.Type] = left - cost.Amount;
            }

            if (remaining.Count == 0) return true;

            foreach (var pair in remaining) Set(pair.Key, pair.Value);
            Changed.SafeInvoke();
            return true;
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
