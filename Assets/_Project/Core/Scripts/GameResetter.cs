using System;
using System.Threading.Tasks;

namespace BunDauPeo.Core
{
    // Cầu nối để UI gọi Reset mà không phụ thuộc Boot (UI và Boot là hai assembly ngang hàng, xem structure.md).
    public sealed class GameResetter
    {
        private readonly Func<Task<OnlineStatus>> _reset;

        public GameResetter(Func<Task<OnlineStatus>> reset) => _reset = reset ?? throw new ArgumentNullException(nameof(reset));

        // Chỉ gọi sau khi người chơi đã xác nhận (D-005). NetworkError: server chưa xoá nên không xoá gì (hộp Mất kết nối, Thử lại);
        // lỗi khác (server, hay ghi bản đệm): Reset chưa xong, bộ nhớ và server vẫn khớp nhau.
        public Task<OnlineStatus> ResetAsync() => _reset();
    }
}
