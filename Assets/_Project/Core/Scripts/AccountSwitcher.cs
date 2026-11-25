using System;
using System.Threading.Tasks;

namespace BunDauPeo.Core
{
    // Cầu nối để UI đổi người chơi (đăng nhập tài khoản khác) mà không phụ thuộc Boot; Boot làm phần việc (dừng đẩy save, bỏ session).
    public sealed class AccountSwitcher
    {
        private readonly Func<Func<Task<AccountResult>>, Task<AccountResult>> _switch;

        public AccountSwitcher(Func<Func<Task<AccountResult>>, Task<AccountResult>> doSwitch) =>
            _switch = doSwitch ?? throw new ArgumentNullException(nameof(doSwitch));

        // signIn đổi người chơi (IAccountService.SignIn*). Ok: session đã bỏ, người gọi nạp Tiêu đề; lỗi: giữ nguyên session.
        public Task<AccountResult> SwitchAsync(Func<Task<AccountResult>> signIn) => _switch(signIn);
    }
}
