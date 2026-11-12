using System.Globalization;
using System.Linq;

namespace BunDauPeo.UI
{
    public static class PlayerNameRule
    {
        public const int MaxLength = 12;

        // null → ""; bỏ ký tự điều khiển (xuống dòng, tab); trim hai đầu; cắt còn MaxLength ký tự theo text element (không cắt đôi ký tự ghép).
        public static string Normalize(string raw)
        {
            if (raw == null) return "";

            var text = new string(raw.Where(c => !char.IsControl(c)).ToArray()).Trim();
            var info = new StringInfo(text);
            return info.LengthInTextElements > MaxLength ? info.SubstringByTextElements(0, MaxLength) : text;
        }

        // "" → tên mặc định; còn lại trả nguyên.
        public static string Display(string saved) => string.IsNullOrEmpty(saved) ? UiText.Get("profile.default_name") : saved;
    }
}
