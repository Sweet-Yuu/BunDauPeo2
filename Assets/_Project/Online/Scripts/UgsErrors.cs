using System;
using System.Net.Http;
using BunDauPeo.Core;
using Unity.Services.CloudSave;
using Unity.Services.Core;

namespace BunDauPeo.Online
{
    // Đổi ngoại lệ của SDK UGS thành OnlineStatus; kiểm theo core 1.19.0, cloudsave 3.4.2, cloudcode 2.10.4.
    public static class UgsErrors
    {
        public static OnlineStatus Map(Exception e) => e switch
        {
            // Lỗi write lock: máy khác đã ghi khoá save sau lần ta đọc (spec 5.4).
            CloudSaveException { Reason: CloudSaveExceptionReason.Conflict } => OnlineStatus.OtherDevice,
            RequestFailedException r when IsNetwork(r.ErrorCode) => OnlineStatus.NetworkError,
            TimeoutException or HttpRequestException => OnlineStatus.NetworkError,
            _ => OnlineStatus.ServerError,
        };

        // SDK đặt TransportError khi không có mạng (Cloud Save, Cloud Code, Authentication đều vậy).
        private static bool IsNetwork(int errorCode) =>
            errorCode == CommonErrorCodes.TransportError || errorCode == CommonErrorCodes.Timeout ||
            errorCode == CommonErrorCodes.ServiceUnavailable;
    }
}
