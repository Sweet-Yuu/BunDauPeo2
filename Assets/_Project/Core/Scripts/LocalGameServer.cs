using System;
using System.Threading.Tasks;
using BunDauPeo.Rules;

namespace BunDauPeo.Core
{
    // Server giả trong bộ nhớ cho mọi test và cho Editor chế độ Local (spec 5.12); không chạm mạng.
    public sealed class LocalGameServer : IGameServer
    {
        private readonly Func<DateTime> _utcNow;
        private OnlineStatus _failStatus;
        private int _failCount;
        private string _failOn;
        private OnlineStatus _failOnStatus;
        private bool _otherDeviceNext;
        private bool _loseNextReply;
        private bool _writeLockStale;

        public LocalGameServer() : this(() => DateTime.UtcNow) { }

        public LocalGameServer(Func<DateTime> utcNow) => _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));

        public string PlayerId { get; private set; } = "";
        public string StoredSave { get; private set; }
        // Đếm mọi lần gọi, kể cả lần lỗi, để test bắt được gọi thừa.
        public int BootstrapCalls { get; private set; }
        public int UploadCalls { get; private set; }

        // Khác null thì lượt đẩy treo tới khi cổng mở, để test "có thay đổi trong lúc đang đẩy".
        public TaskCompletionSource<bool> UploadGate { get; set; }

        // Khác null thì lượt tải save treo tới khi cổng mở, để test "chạm khi đang kết nối".
        public TaskCompletionSource<bool> LoadGate { get; set; }

        // count lệnh kế tiếp (bất kỳ lệnh nào) trả status và không đổi dữ liệu.
        public void FailNext(OnlineStatus status, int count = 1)
        {
            _failStatus = status;
            _failCount = count;
        }

        // Lần gọi kế tiếp của đúng lệnh này (nameof(IGameServer.XxxAsync)) trả status; các lệnh trước nó vẫn chạy.
        public void FailNextOn(string command, OnlineStatus status)
        {
            _failOn = command;
            _failOnStatus = status;
        }

        public void SimulateOtherDevice() => _otherDeviceNext = true;

        // Lượt đẩy kế tiếp ghi xong nhưng trả lời mất (hết giờ, NetworkError): máy giữ write lock cũ nên các lượt đẩy sau xung đột
        // (OtherDevice) tới khi tải lại save, như Cloud Save thật.
        public void LoseNextUploadReply() => _loseNextReply = true;

        // Editor chế độ Local nạp bản đệm làm dữ liệu server để tiến độ còn qua các lần Play.
        public void Seed(string json) => StoredSave = json;

        public Task<OnlineStatus> SignInAsync()
        {
            if (TryFail(nameof(SignInAsync), out var status)) return Task.FromResult(status);
            if (PlayerId == "") PlayerId = "local-" + Guid.NewGuid().ToString("N");
            return Task.FromResult(OnlineStatus.Ok);
        }

        public Task<OnlineResult<BootstrapResponse>> BootstrapAsync()
        {
            BootstrapCalls++;
            if (Refuse(nameof(BootstrapAsync), out var status)) return Task.FromResult(OnlineResult<BootstrapResponse>.Fail(status));
            return Task.FromResult(OnlineResult<BootstrapResponse>.Success(new BootstrapResponse { ServerUtcTicks = _utcNow().Ticks }));
        }

        public Task<OnlineResult<string>> LoadSaveAsync()
        {
            var gate = LoadGate;
            return gate == null ? Task.FromResult(Load()) : LoadAfter(gate.Task);
        }

        private async Task<OnlineResult<string>> LoadAfter(Task gate)
        {
            await gate;
            return Load();
        }

        private OnlineResult<string> Load()
        {
            if (Refuse(nameof(LoadSaveAsync), out var status)) return OnlineResult<string>.Fail(status);
            _writeLockStale = false;
            return OnlineResult<string>.Success(StoredSave);
        }

        public Task<OnlineStatus> UploadSaveAsync(string json)
        {
            UploadCalls++;
            var gate = UploadGate;
            return gate == null ? Task.FromResult(Upload(json)) : UploadAfter(gate.Task, json);
        }

        private async Task<OnlineStatus> UploadAfter(Task gate, string json)
        {
            await gate;
            return Upload(json);
        }

        private OnlineStatus Upload(string json)
        {
            if (Refuse(nameof(UploadSaveAsync), out var status)) return status;
            if (_otherDeviceNext)
            {
                _otherDeviceNext = false;
                return OnlineStatus.OtherDevice;
            }

            if (_writeLockStale) return OnlineStatus.OtherDevice;
            StoredSave = json;
            if (!_loseNextReply) return OnlineStatus.Ok;
            _loseNextReply = false;
            _writeLockStale = true;
            return OnlineStatus.NetworkError;
        }

        public Task<OnlineStatus> ResetPlayerAsync()
        {
            if (Refuse(nameof(ResetPlayerAsync), out var status)) return Task.FromResult(status);
            StoredSave = null;
            return Task.FromResult(OnlineStatus.Ok);
        }

        public Task<OnlineStatus> DeleteAccountAsync()
        {
            if (Refuse(nameof(DeleteAccountAsync), out var status)) return Task.FromResult(status);
            StoredSave = null;
            PlayerId = "";
            return Task.FromResult(OnlineStatus.Ok);
        }

        // Lệnh dữ liệu cần đăng nhập trước, như UGS thật; gọi sai thứ tự thì test thấy ServerError.
        private bool Refuse(string command, out OnlineStatus status)
        {
            if (TryFail(command, out status)) return true;
            if (PlayerId != "") return false;
            status = OnlineStatus.ServerError;
            return true;
        }

        private bool TryFail(string command, out OnlineStatus status)
        {
            status = OnlineStatus.Ok;
            if (_failOn == command)
            {
                _failOn = null;
                status = _failOnStatus;
                return true;
            }

            if (_failCount <= 0) return false;
            _failCount--;
            status = _failStatus;
            return true;
        }
    }
}
