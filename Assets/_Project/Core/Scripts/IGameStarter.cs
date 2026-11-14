using System.Threading.Tasks;

namespace BunDauPeo.Core
{
    // Dựng session từ server sau khi đăng nhập; Tiêu đề gọi StartAsync, Về Tiêu đề gọi Shutdown.
    public interface IGameStarter
    {
        bool IsStarted { get; }

        // Chạy tiếp từ bước lỗi lần trước; đã xong thì trả Ok; đang chạy thì trả cùng Task.
        Task<OnlineStatus> StartAsync();

        // Bỏ session và service game, để lần StartAsync sau dựng lại từ server.
        void Shutdown();
    }
}
