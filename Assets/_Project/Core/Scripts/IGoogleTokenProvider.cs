using System.Threading.Tasks;

namespace BunDauPeo.Core
{
    // Lấy Google ID token từ SDK đăng nhập của nền tảng (Android, iOS, WebGL). UGS đổi token này (Sign in with Google, OIDC),
    // nên không gắn với Play Games. Người chơi đóng hộp chọn tài khoản thì ném OperationCanceledException.
    public interface IGoogleTokenProvider
    {
        Task<string> GetIdTokenAsync();
    }
}
