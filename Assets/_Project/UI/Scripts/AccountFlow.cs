using System;
using System.Threading.Tasks;
using BunDauPeo.Core;

namespace BunDauPeo.UI
{
    // Luật của các hộp tài khoản (không UI, test được): liên kết, đăng nhập, chuyển tài khoản (spec đợt 3 8.3).
    // Lỗi thì không đổi gì; đăng nhập thành công thì session cũ đã bỏ và afterSwitch đưa về Tiêu đề.
    public sealed class AccountFlow
    {
        private readonly IAccountService _account;
        private readonly AccountSwitcher _switcher;
        private readonly Action _afterSwitch;
        private readonly Func<bool> _hasProgress;

        public AccountFlow(IAccountService account, AccountSwitcher switcher, Action afterSwitch, Func<bool> hasProgress)
        {
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _switcher = switcher ?? throw new ArgumentNullException(nameof(switcher));
            _afterSwitch = afterSwitch ?? throw new ArgumentNullException(nameof(afterSwitch));
            _hasProgress = hasProgress ?? throw new ArgumentNullException(nameof(hasProgress));
        }

        public IAccountService Account => _account;

        // Đăng nhập thay người chơi: tiến độ ẩn danh chưa liên kết sẽ mất nên phải hỏi trước.
        public bool NeedsLoseWarning => !_account.IsLinked && _hasProgress();

        // Conflict = danh tính đã thuộc người khác: UI hỏi Chuyển hay Huỷ (Huỷ giữ nguyên tiến độ, không gọi gì thêm).
        public Task<AccountResult> LinkAsync(AccountMethod method, string user, string pass) =>
            method == AccountMethod.Google ? _account.LinkGoogleAsync() : _account.LinkUserPassAsync(user, pass);

        // confirmedLoss: người chơi đã bấm xác nhận cảnh báo mất tiến độ (hộp Đăng nhập hay hộp Chuyển tài khoản).
        public async Task<AccountResult> SignInAsync(AccountMethod method, string user, string pass, bool confirmedLoss)
        {
            if (NeedsLoseWarning && !confirmedLoss) return new AccountResult(AccountStatus.Cancelled);

            var result = await _switcher.SwitchAsync(() =>
                method == AccountMethod.Google ? _account.SignInGoogleAsync() : _account.SignInUserPassAsync(user, pass));
            if (result.Ok) _afterSwitch();
            return result;
        }
    }
}
