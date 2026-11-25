using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BunDauPeo.Core
{
    public enum AccountMethod { Google, UserPass }

    public enum AccountStatus { Ok, Conflict, UsernameTaken, WeakPassword, WrongCredentials, Cancelled, Unavailable, Offline }

    public readonly struct AccountResult
    {
        public AccountResult(AccountStatus status) => Status = status;

        public AccountStatus Status { get; }
        public bool Ok => Status == AccountStatus.Ok;
    }

    // Liên kết danh tính (Google, tên + mật khẩu) vào người chơi ẩn danh và đăng nhập lại bằng danh tính đó (spec đợt 3 8.3).
    // Mọi hàm không ném: lỗi trả bằng AccountStatus. Lỗi thì không đổi gì (không phiên nửa vời).
    public interface IAccountService
    {
        bool IsLinked { get; }

        // Ví dụ "Google", "abc".
        IReadOnlyList<string> LinkedNames { get; }

        IReadOnlyList<AccountMethod> Available { get; }

        // Liên kết hoặc đăng nhập xong (và đổi người chơi) thì bắn.
        event Action Changed;

        // Đọc lại danh tính đã liên kết từ server (sau đăng nhập). Lỗi thì giữ nguyên, không ném.
        Task RefreshAsync();

        // Conflict khi danh tính đã thuộc người chơi khác; không bao giờ ForceLink (không cướp liên kết của người khác).
        Task<AccountResult> LinkUserPassAsync(string user, string pass);
        Task<AccountResult> LinkGoogleAsync();

        // Đăng xuất người chơi hiện tại rồi đăng nhập bằng danh tính đó. Lỗi thì giữ nguyên người chơi hiện tại.
        Task<AccountResult> SignInUserPassAsync(string user, string pass);
        Task<AccountResult> SignInGoogleAsync();
    }
}
