using System;

namespace BunDauPeo.Core
{
    // Cầu nối để UI đọc/ghi cờ thanh quán mà không phụ thuộc Boot (như LanguageSwitcher).
    public sealed class RestaurantBarPref
    {
        private readonly Func<bool> _get;
        private readonly Func<bool, bool> _set;

        public RestaurantBarPref(Func<bool> get, Func<bool, bool> set)
        {
            _get = get ?? throw new ArgumentNullException(nameof(get));
            _set = set ?? throw new ArgumentNullException(nameof(set));
        }

        public bool IsOpen => _get();

        // true khi đã lưu; false khi lưu thất bại (giá trị mới vẫn giữ trong bộ nhớ).
        public bool Set(bool open) => _set(open);
    }
}
