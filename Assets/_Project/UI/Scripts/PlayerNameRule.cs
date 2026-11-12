using System.Globalization;
using System.Text;
namespace BunDauPeo.UI
{
    public static class PlayerNameRule
    {
        public const int MaxLength = 12;
        // null → ""; bỏ ký tự điều khiển (xuống dòng, tab) và surrogate lẻ (InputField cắt theo char nên có thể để nửa emoji); trim hai đầu;
        // cắt còn MaxLength ký tự theo text element (không cắt đôi ký tự ghép).
        public static string Normalize(string raw)
        {
            if (raw == null) return "";
            var sb = new StringBuilder(raw.Length);
            for (var i = 0; i < raw.Length; i++)
            {
                var c = raw[i];
                if (char.IsControl(c)) continue;
                if (char.IsHighSurrogate(c) && i + 1 < raw.Length && char.IsLowSurrogate(raw[i + 1])) sb.Append(c).Append(raw[++i]);
                else if (!char.IsSurrogate(c)) sb.Append(c);
            }
            var text = sb.ToString().Trim();
            var info = new StringInfo(text);
            return info.LengthInTextElements > MaxLength ? info.SubstringByTextElements(0, MaxLength) : text;
        }
        // "" → tên mặc định; còn lại trả nguyên.
        public static string Display(string saved) => string.IsNullOrEmpty(saved) ? UiText.Get("profile.default_name") : saved;
    }
}
