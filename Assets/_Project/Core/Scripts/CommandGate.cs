using System;
using System.Threading.Tasks;
using BunDauPeo.Rules;
using UnityEngine;

namespace BunDauPeo.Core
{
    // Cổng một lệnh mỗi lúc cho mọi lệnh server do người chơi bấm (spec con 6.3, 7.1): giữ mã RequestId qua các lần Thử lại,
    // hẹn 10 giây, bật/tắt màn chờ. Lệnh lỗi mạng chờ người chơi bấm Thử lại; kết quả cuối mới trả về cho service.
    public sealed class CommandGate
    {
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private readonly Func<TimeSpan, Task> _delay;
        private bool _waiting;
        private TaskCompletionSource<bool> _abort;

        public CommandGate(Func<TimeSpan, Task> delay) => _delay = delay ?? throw new ArgumentNullException(nameof(delay));

        public bool Busy { get; private set; }

        // true khi bắt đầu gửi, false khi xong hoặc khi mở hộp Mất kết nối, true lại khi Thử lại.
        public event Action<bool> WaitingChanged;

        // Mất kết nối: người nghe mở hộp và gọi retry khi bấm Thử lại (gửi lại cùng RequestId).
        public event Action<Action> Lost;
        public event Action OtherDevice;
        public event Action NoState;
        public event Action TooOld;

        // Đồng hồ thật, không phụ thuộc timeScale; chờ theo khung hình nên chạy cả WebGL (Task.Delay cần luồng thời gian).
        public static async Task RealtimeDelay(TimeSpan time)
        {
            var end = Time.realtimeSinceStartup + (float)time.TotalSeconds;
            while (Time.realtimeSinceStartup < end) await Awaitable.NextFrameAsync();
        }

        public async Task<OnlineResult<CommandResponse>> RunAsync(Func<string, Task<OnlineResult<CommandResponse>>> call)
        {
            if (call == null) throw new ArgumentNullException(nameof(call));
            // Nút bị gọi hai lần: lần hai không gửi gì.
            if (Busy) return OnlineResult<CommandResponse>.Fail(OnlineStatus.ServerError);

            Busy = true;
            var requestId = Guid.NewGuid().ToString("N");
            var abort = _abort = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            SetWaiting(true);
            try
            {
                while (true)
                {
                    var result = await AttemptAsync(call, requestId, abort.Task);
                    if (abort.Task.IsCompleted) return OnlineResult<CommandResponse>.Fail(OnlineStatus.OtherDevice);
                    if (result.Status == OnlineStatus.NetworkError || result.Status == OnlineStatus.ServerError)
                    {
                        // Không ai nghe thì không có ai bấm Thử lại: trả lỗi luôn thay vì treo mãi.
                        if (Lost == null) return result;
                        await WaitForRetryAsync(abort.Task);
                        if (abort.Task.IsCompleted) return OnlineResult<CommandResponse>.Fail(OnlineStatus.OtherDevice);
                        continue;
                    }

                    return Resolve(result);
                }
            }
            finally
            {
                if (_abort == abort) _abort = null;
                Busy = false;
                SetWaiting(false);
            }
        }

        // Phiên bị bỏ (Về Tiêu đề, Shutdown) khi lệnh còn chờ: nhả cổng, nếu không Busy kẹt và mọi lệnh sau bị bỏ im lặng.
        public void Abort() => _abort?.TrySetResult(true);

        private async Task<OnlineResult<CommandResponse>> AttemptAsync(Func<string, Task<OnlineResult<CommandResponse>>> call, string requestId, Task abort)
        {
            var sending = SafeCall(call, requestId);
            var timer = _delay(Timeout);
            // Hết giờ thì bỏ câu trả lời muộn của lượt này; server vẫn có thể đã ghi, Thử lại cùng RequestId lấy lại kết quả đó.
            var first = await Task.WhenAny(sending, timer, abort);
            return first == sending ? await sending : OnlineResult<CommandResponse>.Fail(OnlineStatus.NetworkError);
        }

        // IGameServer hứa không ném; nếu vẫn ném thì coi như lỗi server, không để Task lỗi không ai đọc.
        private static async Task<OnlineResult<CommandResponse>> SafeCall(Func<string, Task<OnlineResult<CommandResponse>>> call, string requestId)
        {
            try
            {
                return await call(requestId);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return OnlineResult<CommandResponse>.Fail(OnlineStatus.ServerError);
            }
        }

        private async Task WaitForRetryAsync(Task abort)
        {
            var retried = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            SetWaiting(false);
            var used = false;
            Lost.SafeInvoke(() =>
            {
                if (used || abort.IsCompleted) return;
                used = true;
                SetWaiting(true);
                retried.TrySetResult(true);
            });
            await Task.WhenAny(retried.Task, abort);
        }

        // Mã từ chối theo luật (không đủ Gem…) trả Success để service đổi sang trạng thái của nó; chỉ ba mã hỏng phiên được xử lý ở đây.
        private OnlineResult<CommandResponse> Resolve(OnlineResult<CommandResponse> result)
        {
            if (result.Status == OnlineStatus.ClientTooOld)
            {
                TooOld.SafeInvoke();
                return result;
            }

            if (!result.Ok) return result;
            switch ((CommandCode)result.Value.Code)
            {
                case CommandCode.OtherDevice:
                    OtherDevice.SafeInvoke();
                    return OnlineResult<CommandResponse>.Fail(OnlineStatus.OtherDevice);
                case CommandCode.NoState:
                    NoState.SafeInvoke();
                    return OnlineResult<CommandResponse>.Fail(OnlineStatus.ServerError);
                default:
                    return result;
            }
        }

        private void SetWaiting(bool waiting)
        {
            if (_waiting == waiting) return;
            _waiting = waiting;
            WaitingChanged.SafeInvoke(waiting);
        }
    }
}
