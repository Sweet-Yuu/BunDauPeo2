using UnityEngine;

namespace BunDauPeo.Core
{
    public enum Language { Vi, En }

    public static class GameLanguage
    {
        // Mặc định Vi: test EditMode và scene builder (chạy trong Editor) luôn thấy tiếng Việt, trừ khi test tự đặt.
        public static Language Current { get; set; } = Language.Vi;

        public static string Code(Language language) => language == Language.En ? "en" : "vi";

        // Chỉ nhận đúng "vi"/"en" (phân biệt hoa thường); chuỗi khác, kể cả "" và null, trả false.
        public static bool TryParse(string code, out Language language)
        {
            if (string.Equals(code, "vi", System.StringComparison.Ordinal)) { language = Language.Vi; return true; }
            if (string.Equals(code, "en", System.StringComparison.Ordinal)) { language = Language.En; return true; }
            language = Language.Vi;
            return false;
        }

        // Đã chọn thì theo lựa chọn; chưa chọn (hoặc mã lạ) thì theo máy.
        public static Language Resolve(string saved, SystemLanguage system) =>
            TryParse(saved, out var chosen) ? chosen : system == SystemLanguage.Vietnamese ? Language.Vi : Language.En;

        // Bản tiếng Anh rỗng thì lùi về tiếng Việt để màn không trống chữ.
        public static string Pick(string vi, string en) =>
            Current == Language.En && !string.IsNullOrEmpty(en) ? en : vi;
    }
}
