using System;

namespace BunDauPeo.Core
{
    // Cầu nối để UI đổi ngôn ngữ mà không phụ thuộc Boot (như GameResetter).
    public sealed class LanguageSwitcher
    {
        private readonly Func<Language, bool> _set;

        public LanguageSwitcher(Func<Language, bool> set) => _set = set ?? throw new ArgumentNullException(nameof(set));

        // true khi đã lưu và áp dụng; false khi lưu thất bại (ngôn ngữ cũ được giữ).
        public bool Set(Language language) => _set(language);
    }
}
