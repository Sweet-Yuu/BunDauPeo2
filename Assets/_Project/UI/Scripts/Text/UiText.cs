using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BunDauPeo.Core;

namespace BunDauPeo.UI
{
    // Bảng chữ người chơi thấy; mỗi khu một file partial để các phiên chuyển chữ không sửa chung file.
    public static partial class UiText
    {
        private static Dictionary<string, (string Vi, string En)> _table;

        private static (string Key, string Vi, string En)[] _all;

        // Tính lúc dùng lần đầu: C# không đảm bảo thứ tự khởi tạo static giữa các file partial, nên ghép ngay trong initializer có thể gặp mảng còn null.
        public static IReadOnlyList<(string Key, string Vi, string En)> AllEntries =>
            _all ??= Common.Concat(Gacha).Concat(Restaurant).Concat(Combat).Concat(Characters).ToArray();

        // Key thiếu trả "#key" thay vì ném lỗi: lỗi chữ không đáng làm chết màn, nhưng phải thấy ngay trên màn và test bắt được.
        public static string Get(string key)
        {
            _table ??= BuildTable(AllEntries);
            if (!_table.TryGetValue(key ?? "", out var entry)) return "#" + key;
            return GameLanguage.Current == Language.En ? entry.En : entry.Vi;
        }

        public static string Format(string key, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, Get(key), args);

        // Vi dùng dấu phẩy thập phân (giữ như trước), En dùng dấu chấm.
        public static string Decimal(double value, string format)
        {
            var text = value.ToString(format, CultureInfo.InvariantCulture);
            return GameLanguage.Current == Language.Vi ? text.Replace('.', ',') : text;
        }

        public static Dictionary<string, (string Vi, string En)> BuildTable(IEnumerable<(string Key, string Vi, string En)> entries)
        {
            var table = new Dictionary<string, (string Vi, string En)>();
            foreach (var (key, vi, en) in entries)
            {
                if (!table.TryAdd(key, (vi, en))) throw new ArgumentException($"UiText: duplicate key '{key}'");
            }

            return table;
        }
    }
}
