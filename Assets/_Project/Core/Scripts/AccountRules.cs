using System.Linq;

namespace BunDauPeo.Core
{
    // Luật tên đăng nhập và mật khẩu của UGS Authentication (kiểm theo IAuthenticationService 3.8.1), để báo lỗi nhanh phía máy.
    public static class AccountRules
    {
        public const int NameMin = 3;
        public const int NameMax = 20;
        public const int PasswordMin = 8;
        public const int PasswordMax = 30;

        public static bool ValidName(string name) =>
            name != null && name.Length >= NameMin && name.Length <= NameMax && name.All(c => IsAscii(c) && (char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '@' || c == '_'));

        // 8–30 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt.
        public static bool StrongPassword(string pass) =>
            pass != null && pass.Length >= PasswordMin && pass.Length <= PasswordMax
            && pass.Any(c => c >= 'A' && c <= 'Z') && pass.Any(c => c >= 'a' && c <= 'z')
            && pass.Any(c => c >= '0' && c <= '9') && pass.Any(c => !IsAscii(c) || !char.IsLetterOrDigit(c));

        private static bool IsAscii(char c) => c < 128;
    }
}
