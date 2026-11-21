namespace BunDauPeo.Core
{
    // Kết quả mọi lệnh server: lệnh không ném ra ngoài, lỗi nào cũng về một trong các giá trị này.
    // ClientTooOld ở cuối enum: máy dưới MinProtocolVersion của server (hàng rào bản cũ, spec con 3.3).
    public enum OnlineStatus { Ok, NetworkError, OtherDevice, ServerError, ClientTooOld }

    public readonly struct OnlineResult<T>
    {
        public OnlineStatus Status { get; }
        public T Value { get; }
        public bool Ok => Status == OnlineStatus.Ok;

        private OnlineResult(OnlineStatus status, T value)
        {
            Status = status;
            Value = value;
        }

        public static OnlineResult<T> Success(T value) => new(OnlineStatus.Ok, value);
        public static OnlineResult<T> Fail(OnlineStatus status) => new(status, default);
    }
}
