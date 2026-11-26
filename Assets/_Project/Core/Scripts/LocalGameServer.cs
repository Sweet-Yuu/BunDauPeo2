using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BunDauPeo.Rules;
using UnityEngine;

namespace BunDauPeo.Core
{
    // Server giả trong bộ nhớ cho mọi test và cho Editor chế độ Local (spec 5.12); không chạm mạng.
    // Lệnh game chạy đúng CommandRunner + ServerHandlers như module Cloud Code, nên luật tiền/gacha không có bản thứ hai.
    public sealed class LocalGameServer : IGameServer
    {
        private readonly Func<DateTime> _utcNow;
        private GameData _data;
        private readonly bool _emptyData;
        private OnlineStatus _failStatus;
        private int _failCount;
        private string _failOn;
        private OnlineStatus _failOnStatus;
        private bool _otherDeviceNext;
        private bool _loseNextReply;
        private bool _loseNextCommandReply;
        private bool _loseNextResetReply;
        private bool _writeLockStale;
        private TaskCompletionSource<bool> _delayNext;
        private string _sessionId = "";

        // Không có dữ liệu cân bằng: Boot gắn bản của game vào (UseDataIfEmpty) trước khi dùng.
        public LocalGameServer() : this(() => DateTime.UtcNow, new GameData(), true) { }

        public LocalGameServer(GameData data) : this(() => DateTime.UtcNow, data) { }

        public LocalGameServer(Func<DateTime> utcNow) : this(utcNow, new GameData(), true) { }

        public LocalGameServer(Func<DateTime> utcNow, GameData data) : this(utcNow, data, false) { }

        private LocalGameServer(Func<DateTime> utcNow, GameData data, bool emptyData)
        {
            _emptyData = emptyData;
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            DataHash = Rules.DataHash.Of(JsonUtility.ToJson(data));
        }

        // Test dựng server không dữ liệu rồi để Boot gắn dữ liệu thật của game; đã có dữ liệu thì không đổi.
        public void UseDataIfEmpty(GameData data)
        {
            if (!_emptyData) return;
            _data = data ?? throw new ArgumentNullException(nameof(data));
            DataHash = Rules.DataHash.Of(JsonUtility.ToJson(data));
        }

        public string PlayerId { get; private set; } = "";
        public string StoredSave { get; private set; }
        // Bản "state" trên server; null = chưa có (người chơi mới hoặc sau Reset recreate=false).
        public ServerState State { get; private set; }
        // Đếm mọi lần gọi, kể cả lần lỗi, để test bắt được gọi thừa.
        public int BootstrapCalls { get; private set; }
        public int UploadCalls { get; private set; }
        public int CommandCalls { get; private set; }

        // Hook test: phiên bản giao thức máy "gửi đi" (đặt 1 để thấy ClientTooOld) và mã băm dữ liệu server "báo".
        public int ProtocolVersion { get; set; } = ServerProtocol.Version;
        public string DataHash { get; set; }

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

        // Lượt đẩy kế tiếp xung đột và máy khác đang giữ phiên (mã phiên đổi), như máy khác vừa Bootstrap rồi ghi.
        public void SimulateOtherDevice()
        {
            _otherDeviceNext = true;
            SimulateOtherDeviceBootstrap();
        }

        // Lượt đẩy kế tiếp ghi xong nhưng trả lời mất (hết giờ, NetworkError): máy giữ write lock cũ nên các lượt đẩy sau xung đột
        // (OtherDevice) tới khi tải lại save, như Cloud Save thật.
        public void LoseNextUploadReply() => _loseNextReply = true;

        // Lệnh game kế tiếp chạy và GHI xong rồi trả NetworkError (mất trả lời); gửi lại cùng requestId thì không chạy lần hai.
        public void LoseNextReply() => _loseNextCommandReply = true;

        // Lệnh game kế tiếp chờ cổng mở rồi mới chạy (test màn chờ).
        // Như server môi trường phát hành: lệnh DEV trả DevOnly và không đổi gì.
        public bool RejectDev { get; set; }

        public void DelayNext(TaskCompletionSource<bool> gate) => _delayNext = gate;

        // Reset kế tiếp xoá xong trên server rồi trả NetworkError (mất trả lời); Reset lại thì chạy lần nữa và thành công.
        public void LoseNextResetReply() => _loseNextResetReply = true;

        // Máy khác Bootstrap: phiên đang giữ đổi, lệnh kế tiếp của máy này nhận OtherDevice.
        public void SimulateOtherDeviceBootstrap()
        {
            if (State != null) State.ActiveSessionId = Guid.NewGuid().ToString("N");
        }

        // Editor chế độ Local nạp bản đệm làm dữ liệu server để tiến độ còn qua các lần Play: phần server giữ thành State.
        public void Seed(string saveJson)
        {
            StoredSave = saveJson;
            // null/rỗng = server trống (test xoá dữ liệu): cả state cũng không còn.
            var save = string.IsNullOrEmpty(saveJson) ? null : JsonUtility.FromJson<PlayerSave>(saveJson);
            State = save == null ? null : ServerStateApplier.FromSave(save);
        }

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
            if (ProtocolVersion < ServerProtocol.MinProtocolVersion)
                return Task.FromResult(OnlineResult<BootstrapResponse>.Fail(OnlineStatus.ClientTooOld));

            var outcome = ServerHandlers.Bootstrap(State, _data, NewContext());
            State = outcome.State;
            _sessionId = State.ActiveSessionId;
            return Task.FromResult(OnlineResult<BootstrapResponse>.Success(new BootstrapResponse
            {
                ServerUtcTicks = _utcNow().Ticks,
                SessionId = _sessionId,
                DataHash = DataHash,
                State = State.Clone(),
            }));
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

        public Task<OnlineResult<string>> LoadActiveSessionIdAsync()
        {
            if (Refuse(nameof(LoadActiveSessionIdAsync), out var status)) return Task.FromResult(OnlineResult<string>.Fail(status));
            return Task.FromResult(OnlineResult<string>.Success(State?.ActiveSessionId));
        }

        public Task<OnlineResult<ServerState>> ResetPlayerAsync() => Task.FromResult(Reset(true));

        // recreate = false chỉ để test "xoá hết, lệnh sau thấy NoState" (UGS gọi như vậy khi Xoá tài khoản).
        public Task<OnlineResult<ServerState>> ResetNoRecreateAsync() => Task.FromResult(Reset(false));

        private OnlineResult<ServerState> Reset(bool recreate)
        {
            if (Refuse(nameof(ResetPlayerAsync), out var status)) return OnlineResult<ServerState>.Fail(status);
            if (ProtocolVersion < ServerProtocol.MinProtocolVersion) return OnlineResult<ServerState>.Fail(OnlineStatus.ClientTooOld);
            if (State != null && _sessionId != State.ActiveSessionId) return OnlineResult<ServerState>.Fail(OnlineStatus.OtherDevice);

            StoredSave = null;
            State = recreate ? ServerHandlers.ResetState(_data, _sessionId) : null;
            if (_loseNextResetReply)
            {
                _loseNextResetReply = false;
                return OnlineResult<ServerState>.Fail(OnlineStatus.NetworkError);
            }

            return OnlineResult<ServerState>.Success(State?.Clone());
        }

        public Task<OnlineStatus> DeleteAccountAsync()
        {
            if (Refuse(nameof(DeleteAccountAsync), out var status)) return Task.FromResult(status);
            StoredSave = null;
            State = null;
            PlayerId = "";
            return Task.FromResult(OnlineStatus.Ok);
        }

        public Task<OnlineResult<CommandResponse>> GachaPullAsync(string requestId, string bannerId, int count) =>
            Command(nameof(GachaPullAsync), requestId, (s, c) => ServerHandlers.GachaPull(s, _data, c, bannerId, count));

        public Task<OnlineResult<CommandResponse>> EnterStageAsync(string requestId, string stageId, IReadOnlyList<string> team) =>
            Command(nameof(EnterStageAsync), requestId,
                (s, c) => ServerHandlers.EnterStage(s, _data, c, stageId, new List<string>(team ?? new List<string>())));

        public Task<OnlineResult<CommandResponse>> CompleteStageAsync(string requestId, string ticket, bool won) =>
            Command(nameof(CompleteStageAsync), requestId, (s, c) => ServerHandlers.CompleteStage(s, _data, c, ticket, won));

        public Task<OnlineResult<CommandResponse>> CompleteStoryAsync(string requestId, string stageId) =>
            Command(nameof(CompleteStoryAsync), requestId, (s, c) => ServerHandlers.CompleteStory(s, _data, stageId));

        public Task<OnlineResult<CommandResponse>> StarUpAsync(string requestId, string characterId) =>
            Command(nameof(StarUpAsync), requestId, (s, c) => ServerHandlers.StarUp(s, _data, characterId));

        public Task<OnlineResult<CommandResponse>> DevAddGemAsync(string requestId, long amount) =>
            Command(nameof(DevAddGemAsync), requestId,
                (s, c) => RejectDev ? HandlerOutcome.Reject(CommandCode.DevOnly) : ServerHandlers.DevAddGem(s, amount));

        public Task<OnlineResult<CommandResponse>> DevRefillStaminaAsync(string requestId) =>
            Command(nameof(DevRefillStaminaAsync), requestId,
                (s, c) => RejectDev ? HandlerOutcome.Reject(CommandCode.DevOnly) : ServerHandlers.DevRefillStamina(s, _data));

        public Task<OnlineResult<CommandResponse>> ClaimMissionAsync(string requestId, string missionId, IReadOnlyList<string> clientDone) =>
            Command(nameof(ClaimMissionAsync), requestId,
                (s, c) => ServerHandlers.ClaimMission(s, _data, c, missionId, new List<string>(clientDone ?? new List<string>())));

        public Task<OnlineResult<CommandResponse>> ClaimAllMissionsAsync(string requestId, int tab, IReadOnlyList<string> clientDone) =>
            Command(nameof(ClaimAllMissionsAsync), requestId,
                (s, c) => ServerHandlers.ClaimAllMissions(s, _data, c, tab, new List<string>(clientDone ?? new List<string>())));

        public Task<OnlineResult<CommandResponse>> GachaFreePullAsync(string requestId, string bannerId) =>
            Command(nameof(GachaFreePullAsync), requestId, (s, c) => ServerHandlers.GachaFreePull(s, _data, c, bannerId));

        public Task<OnlineResult<CommandResponse>> DevResetMissionsAsync(string requestId) =>
            Command(nameof(DevResetMissionsAsync), requestId,
                (s, c) => RejectDev ? HandlerOutcome.Reject(CommandCode.DevOnly) : ServerHandlers.DevResetMissions(s));

        public Task<OnlineResult<CommandResponse>> DevAddStaminaAsync(string requestId, int amount) =>
            Command(nameof(DevAddStaminaAsync), requestId,
                (s, c) => RejectDev ? HandlerOutcome.Reject(CommandCode.DevOnly) : ServerHandlers.DevAddStamina(s, _data, c, amount));

        // Quà thô: test gán Gifts; không thì đọc GiftsPath (gifts.json cạnh save, cùng hình Dashboard). Không có file = rỗng; file hỏng = lỗi.
        public List<GiftData> Gifts { get; set; }
        public string GiftsPath { get; set; }

        private bool LoadGifts(out List<GiftData> gifts)
        {
            gifts = Gifts ?? new List<GiftData>();
            if (Gifts != null || string.IsNullOrEmpty(GiftsPath) || !System.IO.File.Exists(GiftsPath)) return true;
            try
            {
                var wrapper = JsonUtility.FromJson<GiftJsonList>("{\"items\":" + System.IO.File.ReadAllText(GiftsPath) + "}");
                if (wrapper?.items == null) return false;
                gifts = new List<GiftData>();
                foreach (var j in wrapper.items) gifts.Add(j?.ToGift());
                return true;
            }
            catch (Exception e) when (e is ArgumentException || e is System.IO.IOException)
            {
                Debug.LogWarning("gifts.json unreadable: " + e.Message);
                return false;
            }
        }

        [Serializable]
        private class GiftJsonList
        {
            public List<GiftJson> items;
        }

        public Task<OnlineResult<CommandResponse>> ListGiftsAsync(string lang)
        {
            CommandCalls++;
            if (Refuse(nameof(ListGiftsAsync), out var status)) return Task.FromResult(OnlineResult<CommandResponse>.Fail(status));
            if (!LoadGifts(out var gifts)) return Task.FromResult(OnlineResult<CommandResponse>.Fail(OnlineStatus.ServerError));
            var ctx = NewContext();
            var request = new ServerRequest { ProtocolVersion = ProtocolVersion, SessionId = _sessionId };
            try
            {
                var response = CommandRunner.RunReadOnly(State, request, s => ServerHandlers.ListGifts(s, gifts, ctx, lang), o => JsonUtility.ToJson(o));
                response.ServerUtcTicks = _utcNow().Ticks;
                return Task.FromResult(OnlineResult<CommandResponse>.Success(response));
            }
            catch (ClientTooOldException)
            {
                return Task.FromResult(OnlineResult<CommandResponse>.Fail(OnlineStatus.ClientTooOld));
            }
        }

        public Task<OnlineResult<CommandResponse>> ClaimGiftAsync(string requestId, string giftId) =>
            !LoadGifts(out var gifts)
                ? Task.FromResult(OnlineResult<CommandResponse>.Fail(OnlineStatus.ServerError))
                : Command(nameof(ClaimGiftAsync), requestId, (s, c) => ServerHandlers.ClaimGift(s, gifts, c, giftId));

        public Task<OnlineResult<CommandResponse>> ClaimAllGiftsAsync(string requestId) =>
            !LoadGifts(out var gifts)
                ? Task.FromResult(OnlineResult<CommandResponse>.Fail(OnlineStatus.ServerError))
                : Command(nameof(ClaimAllGiftsAsync), requestId, (s, c) => ServerHandlers.ClaimAllGifts(s, gifts, c));

        private async Task<OnlineResult<CommandResponse>> Command(string name, string requestId,
            Func<ServerState, ServerContext, HandlerOutcome> handler)
        {
            CommandCalls++;
            var gate = _delayNext;
            _delayNext = null;
            if (gate != null) await gate.Task;

            if (Refuse(name, out var status)) return OnlineResult<CommandResponse>.Fail(status);
            var ctx = NewContext();
            var request = new ServerRequest { ProtocolVersion = ProtocolVersion, SessionId = _sessionId, RequestId = requestId ?? "" };
            CommandRun run;
            try
            {
                run = CommandRunner.Run(State, request, s => handler(s, ctx), o => JsonUtility.ToJson(o));
            }
            catch (ClientTooOldException)
            {
                return OnlineResult<CommandResponse>.Fail(OnlineStatus.ClientTooOld);
            }

            if (run.ToWrite != null) State = run.ToWrite;
            run.Response.ServerUtcTicks = _utcNow().Ticks;
            if (!_loseNextCommandReply) return OnlineResult<CommandResponse>.Success(run.Response);
            _loseNextCommandReply = false;
            return OnlineResult<CommandResponse>.Fail(OnlineStatus.NetworkError);
        }

        private ServerContext NewContext() => new()
        {
            NowUtc = _utcNow(),
            Random = new System.Random(),
            NewId = () => Guid.NewGuid().ToString("N"),
        };

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
