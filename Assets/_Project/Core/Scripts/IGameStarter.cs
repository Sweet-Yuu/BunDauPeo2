using System;
using System.Threading.Tasks;

namespace BunDauPeo.Core
{
    // Dựng session từ server sau khi đăng nhập; Tiêu đề gọi StartAsync, Về Tiêu đề gọi Shutdown.
    public interface IGameStarter
    {
        bool IsStarted { get; }

        // Giữa game: đẩy save lỗi liên tục (NetworkError) hoặc máy khác đã ghi (OtherDevice). UI (OnlineOverlay) nghe để hiện hộp;
        // Boot không tham chiếu UI nên nối qua đây.
        event Action<OnlineStatus> ConnectionProblem;
        event Action ConnectionRestored;

        // Server không còn state của người chơi (Reset mất trả lời, tài khoản đã xoá): về Tiêu đề dựng lại, không hộp.
        event Action NoState;

        // Chạy tiếp từ bước lỗi lần trước; đã xong thì trả Ok; đang chạy thì trả cùng Task.
        Task<OnlineStatus> StartAsync();

        // Bỏ session và service game, để lần StartAsync sau dựng lại từ server.
        void Shutdown();
    }
}
