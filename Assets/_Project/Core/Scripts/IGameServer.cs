using System.Threading.Tasks;
using BunDauPeo.Rules;

namespace BunDauPeo.Core
{
    // Lệnh lỗi thì không đổi gì phía server; người gọi cũng không đổi gì trong bộ nhớ (spec 5.2).
    public interface IGameServer
    {
        // "" khi chưa đăng nhập.
        string PlayerId { get; }

        // Đã đăng nhập thì trả Ok ngay, không tạo người chơi mới.
        Task<OnlineStatus> SignInAsync();

        Task<OnlineResult<BootstrapResponse>> BootstrapAsync();

        // JSON khoá save, null nếu chưa có; nhớ write lock cho lần đẩy sau.
        Task<OnlineResult<string>> LoadSaveAsync();

        // Xung đột write lock (máy khác vừa ghi) → OtherDevice.
        Task<OnlineStatus> UploadSaveAsync(string json);

        // Xoá dữ liệu trên server, giữ tài khoản và ID.
        Task<OnlineStatus> ResetPlayerAsync();

        // Xoá dữ liệu và tài khoản, xoá token phiên; PlayerId về "".
        Task<OnlineStatus> DeleteAccountAsync();
    }
}
