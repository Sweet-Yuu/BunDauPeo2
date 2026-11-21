using System.Text;

namespace BunDauPeo.Rules
{
    // FNV-1a 64 bit trên byte UTF-8, 16 ký tự hex thường; không cần thư viện mã hoá nên chạy cả WebGL.
    public static class DataHash
    {
        public static string Of(string json)
        {
            var hash = 0xcbf29ce484222325UL;
            foreach (var b in Encoding.UTF8.GetBytes(json))
            {
                hash ^= b;
                hash *= 0x100000001b3UL;
            }
            return hash.ToString("x16");
        }
    }
}
