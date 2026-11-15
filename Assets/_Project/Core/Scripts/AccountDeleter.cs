using System;
using System.Threading.Tasks;

namespace BunDauPeo.Core
{
    // Cầu nối để Menu xoá tài khoản (spec 5.8) mà không phụ thuộc Boot; Boot làm phần việc (server, bản đệm, bỏ session).
    public sealed class AccountDeleter
    {
        private readonly Func<Task<OnlineStatus>> _delete;

        public AccountDeleter(Func<Task<OnlineStatus>> delete) => _delete = delete ?? throw new ArgumentNullException(nameof(delete));

        // Chỉ gọi sau khi người chơi đã xác nhận. Ok: tài khoản đã xoá, session đã bỏ, người gọi nạp Tiêu đề; lỗi: không xoá gì.
        public Task<OnlineStatus> DeleteAsync() => _delete();
    }
}
