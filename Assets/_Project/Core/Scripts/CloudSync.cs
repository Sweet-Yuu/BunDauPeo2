using System;
using System.Threading.Tasks;

namespace BunDauPeo.Core
{
    // Đẩy khoá save lên server khi bẩn (spec 5.5): gộp lượt, không bao giờ hai lượt cùng lúc, lỗi thì thử lại.
    // Thời gian đi qua Tick để test không phải chờ thật.
    public sealed class CloudSync
    {
        public const float DebounceSeconds = 5f;
        public const float RetrySeconds = 5f;
        public const float LostAfterSeconds = 30f;

        private readonly IGameServer _server;
        private readonly Func<string> _snapshotJson;
        private readonly Action _onUploaded;

        private bool _dirty;
        private double _dirtyAge;
        // Có thay đổi chưa lên server (kể cả lượt đang đẩy) và tuổi của thay đổi cũ nhất trong số đó.
        private bool _unsynced;
        private double _unsyncedAge;
        private int _failures;
        private double _retryIn;
        private bool _stopped;
        private Task _inFlight;

        public CloudSync(IGameServer server, Func<string> snapshotJson, Action onUploaded)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _snapshotJson = snapshotJson ?? throw new ArgumentNullException(nameof(snapshotJson));
            _onUploaded = onUploaded ?? throw new ArgumentNullException(nameof(onUploaded));
        }

        public bool IsLost { get; private set; }
        public event Action Lost;
        public event Action Restored;
        public event Action OtherDevice;

        private bool Uploading => _inFlight != null && !_inFlight.IsCompleted;

        public void MarkDirty()
        {
            if (_stopped) return;
            if (!_dirty) _dirtyAge = 0;
            _dirty = true;
            if (!_unsynced) _unsyncedAge = 0;
            _unsynced = true;
        }

        public void Tick(float unscaledDt)
        {
            if (_stopped) return;
            if (_dirty) _dirtyAge += unscaledDt;
            if (_unsynced) _unsyncedAge += unscaledDt;

            if (!IsLost && _unsynced && (_failures > 0 || Uploading) && _unsyncedAge >= LostAfterSeconds)
            {
                IsLost = true;
                Lost.SafeInvoke();
            }

            if (Uploading || !_dirty) return;
            if (_failures > 0)
            {
                _retryIn -= unscaledDt;
                if (_retryIn > 0) return;
            }
            else if (_dirtyAge < DebounceSeconds)
            {
                return;
            }

            StartUpload();
        }

        // Vào nền, thoát: chờ lượt đang chạy rồi đẩy ngay phần còn bẩn, không đợi gộp lượt.
        public async Task FlushAsync()
        {
            if (Uploading) await _inFlight;
            if (_stopped || !_dirty) return;
            StartUpload();
            await _inFlight;
        }

        // Reset, Xoá tài khoản, Về Tiêu đề: bỏ mọi lượt chờ; lượt đang chạy xong cũng không bắn gì nữa.
        public void Stop()
        {
            _stopped = true;
            _dirty = false;
        }

        private void StartUpload()
        {
            _dirty = false;
            _inFlight = UploadAsync(_snapshotJson());
        }

        private async Task UploadAsync(string json)
        {
            OnlineStatus status;
            try
            {
                status = await _server.UploadSaveAsync(json);
            }
            catch (Exception e)
            {
                // IGameServer hứa không ném; nếu vẫn ném thì coi như lỗi server và thử lại, không kẹt cờ đang đẩy.
                UnityEngine.Debug.LogException(e);
                status = OnlineStatus.ServerError;
            }

            if (_stopped) return;
            switch (status)
            {
                case OnlineStatus.Ok:
                    _failures = 0;
                    // Thay đổi đến trong lúc đẩy giờ là thay đổi cũ nhất chưa lên server.
                    _unsynced = _dirty;
                    _unsyncedAge = _dirtyAge;
                    _onUploaded();
                    if (IsLost)
                    {
                        IsLost = false;
                        Restored.SafeInvoke();
                    }
                    break;
                case OnlineStatus.OtherDevice:
                    Stop();
                    OtherDevice.SafeInvoke();
                    break;
                default:
                    // Bản lỗi chưa lên server: đánh dấu bẩn lại để lần thử sau chụp bản mới nhất.
                    _failures++;
                    _retryIn = RetrySeconds;
                    _dirty = true;
                    break;
            }
        }
    }
}
