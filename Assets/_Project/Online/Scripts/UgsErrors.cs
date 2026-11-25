using System;
using System.Net.Http;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.Core;

namespace BunDauPeo.Online
{
    // Đổi ngoại lệ của SDK UGS thành OnlineStatus; kiểm theo core 1.19.0, cloudsave 3.4.2, cloudcode 2.10.4.
    public static class UgsErrors
    {
        public static OnlineStatus Map(Exception e) => e switch
        {
            // Lỗi write lock: khoá save đã bị ghi sau lần ta đọc (spec 5.4); CloudSync loại ca lượt trước của ta ghi xong mà mất trả lời.
            CloudSaveException { Reason: CloudSaveExceptionReason.Conflict } => OnlineStatus.OtherDevice,
            // Module ném ClientTooOldException: Cloud Code gói message trong lỗi nên dò chuỗi (spec con 3.3).
            CloudCodeException c when c.Message != null && c.Message.Contains(ServerProtocol.ClientTooOldTag) => OnlineStatus.ClientTooOld,
            RequestFailedException r when IsNetwork(r.ErrorCode) => OnlineStatus.NetworkError,
            TimeoutException or HttpRequestException => OnlineStatus.NetworkError,
            _ => OnlineStatus.ServerError,
        };

        // Lỗi liên kết/đăng nhập (kiểm theo authentication 3.8.1). Server trả ENTITY_EXISTS (mã AccountAlreadyLinked) cho cả
        // "tên đã dùng" (user + pass) lẫn "danh tính đã thuộc người khác" (Google); INVALID_PARAMETERS gộp cả mật khẩu yếu lẫn sai thông tin.
        public static AccountStatus MapAccount(Exception e, AccountMethod method, bool signIn)
        {
            if (e is OperationCanceledException) return AccountStatus.Cancelled;
            if (e is not RequestFailedException r) return e is TimeoutException or HttpRequestException ? AccountStatus.Offline : AccountStatus.Unavailable;
            if (IsNetwork(r.ErrorCode)) return AccountStatus.Offline;

            var code = r.ErrorCode;
            // Phương thức chưa bật trên Dashboard (môi trường đó): server trả PERMISSION_DENIED "... provider is not available",
            // SDK gộp vào InvalidParameters nên phải dò chuỗi, không thì báo nhầm là mật khẩu yếu hay sai thông tin.
            if (r.Message != null && r.Message.Contains("provider is not available")) return AccountStatus.Unavailable;
            // Playtest thật: sai tên hay mật khẩu (kể cả tài khoản đã xoá) về mã Unknown với chữ "Invalid username or password".
            if (signIn && r.Message != null && r.Message.Contains("Invalid username or password")) return AccountStatus.WrongCredentials;
            if (code == AuthenticationErrorCodes.AccountAlreadyLinked)
                return method == AccountMethod.Google ? AccountStatus.Conflict : AccountStatus.UsernameTaken;
            if (signIn && (code == AuthenticationErrorCodes.InvalidParameters || code == CommonErrorCodes.InvalidToken
                           || code == CommonErrorCodes.NotFound || code == CommonErrorCodes.Forbidden))
                return AccountStatus.WrongCredentials;
            if (!signIn && code == AuthenticationErrorCodes.InvalidParameters && method == AccountMethod.UserPass)
                return AccountStatus.WeakPassword;
            return AccountStatus.Unavailable;
        }

        // SDK đặt TransportError khi không có mạng (Cloud Save, Cloud Code, Authentication đều vậy).
        private static bool IsNetwork(int errorCode) =>
            errorCode == CommonErrorCodes.TransportError || errorCode == CommonErrorCodes.Timeout ||
            errorCode == CommonErrorCodes.ServiceUnavailable;
    }
}
