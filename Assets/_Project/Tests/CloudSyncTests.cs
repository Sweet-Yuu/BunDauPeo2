using System.Threading.Tasks;
using BunDauPeo.Core;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class CloudSyncTests
    {
        private LocalGameServer _server;
        private string _session;
        private string _snapshot;
        private int _uploaded;
        private CloudSync _sync;

        [SetUp]
        public void SetUp()
        {
            _server = new LocalGameServer(GachaRulesTests.Data());
            // Task của LocalGameServer đã xong ngay nên đọc Result không chặn.
            Assert.AreEqual(OnlineStatus.Ok, _server.SignInAsync().Result);
            _session = _server.BootstrapAsync().Result.Value.SessionId;
            _snapshot = "v1";
            _uploaded = 0;
            _sync = new CloudSync(_server, () => _snapshot, () => _uploaded++, () => _session);
        }

        private void Run(float seconds, float step = 1f)
        {
            for (var t = 0f; t < seconds - 0.0001f; t += step) _sync.Tick(step);
        }

        [Test]
        public void MarkDirty_UploadsOnceAfterDebounce()
        {
            _sync.MarkDirty();
            _sync.Tick(1f);
            _snapshot = "v2";
            _sync.MarkDirty();
            _sync.Tick(1f);
            _snapshot = "v3";
            _sync.MarkDirty();

            _sync.Tick(2.9f);
            Assert.AreEqual(0, _server.UploadCalls, "debounce counts from the first mark: nothing at 4.9 s");
            _sync.Tick(0.1f);
            Assert.AreEqual(1, _server.UploadCalls, "one upload at 5 s for three marks");
            Assert.AreEqual("v3", _server.StoredSave, "the upload carries the latest snapshot");
            Assert.AreEqual(1, _uploaded);

            Run(20f);
            Assert.AreEqual(1, _server.UploadCalls, "nothing new to push");
        }

        [Test]
        public async Task ChangeDuringUpload_UploadsAgain_LatestWins()
        {
            var gate = new TaskCompletionSource<bool>();
            _server.UploadGate = gate;
            _sync.MarkDirty();
            _sync.Tick(5f);
            Assert.AreEqual(1, _server.UploadCalls);

            _snapshot = "v2";
            _sync.MarkDirty();
            Run(10f);
            Assert.AreEqual(1, _server.UploadCalls, "never two uploads at once");

            _server.UploadGate = null;
            gate.SetResult(true);
            await _sync.FlushAsync();

            Assert.AreEqual(2, _server.UploadCalls);
            Assert.AreEqual("v2", _server.StoredSave);
            Assert.AreEqual(2, _uploaded);
        }

        [Test]
        public async Task Pause_FlushesPendingChange()
        {
            _sync.MarkDirty();
            _sync.Tick(1f);

            await _sync.FlushAsync();
            Assert.AreEqual(1, _server.UploadCalls, "flush does not wait for the debounce");
            Assert.AreEqual("v1", _server.StoredSave);

            await _sync.FlushAsync();
            Run(10f);
            Assert.AreEqual(1, _server.UploadCalls, "a clean flush uploads nothing and leaves nothing pending");
        }

        [Test]
        public void NetworkError_RetriesThenLostAt30s_ThenRestored()
        {
            var lost = 0;
            var restored = 0;
            _sync.Lost += () => lost++;
            _sync.Restored += () => restored++;
            _server.FailNext(OnlineStatus.NetworkError, 100);
            _sync.MarkDirty();

            Run(25f);
            Assert.AreEqual(0, lost, "not lost before 25 s");
            Assert.IsFalse(_sync.IsLost);
            Assert.GreaterOrEqual(_server.UploadCalls, 4, "retries every 5 s");
            Run(4f);
            Assert.AreEqual(0, lost, "not lost at 29 s");
            Run(1f);
            Assert.AreEqual(1, lost, "lost at 30 s");
            Assert.IsTrue(_sync.IsLost);
            Run(30f);
            Assert.AreEqual(1, lost, "Lost fires once");
            Assert.AreEqual(0, restored);
            Assert.AreEqual(0, _uploaded);

            _server.FailNext(OnlineStatus.Ok, 0);
            _snapshot = "v2";
            Run(5f);
            Assert.AreEqual(1, restored);
            Assert.IsFalse(_sync.IsLost);
            Assert.AreEqual("v2", _server.StoredSave);
            Assert.AreEqual(1, _uploaded);
        }

        [Test]
        public void OtherDevice_RaisesAndStops()
        {
            var other = 0;
            _sync.OtherDevice += () => other++;
            _server.SimulateOtherDevice();
            _sync.MarkDirty();
            _sync.Tick(5f);
            Assert.AreEqual(1, other);
            Assert.AreEqual(1, _server.UploadCalls);

            _sync.MarkDirty();
            Run(60f);
            Assert.AreEqual(1, _server.UploadCalls, "no more uploads after another device took over");
            Assert.AreEqual(1, other);
            Assert.IsNull(_server.StoredSave);
        }

        // Review O1 mục 2, Review Focus 5: lượt đẩy hết giờ mà server đã ghi → máy giữ write lock cũ → lượt sau xung đột. State vẫn giữ
        // mã phiên của máy này nên không phải máy khác: nhận lock mới rồi đẩy lại, không báo OtherDevice, không mất thay đổi mới.
        [Test]
        public void Conflict_SameSession_Reuploads()
        {
            var save = new PlayerSave();
            var other = 0;
            var uploaded = 0;
            var sync = new CloudSync(_server, () => JsonUtility.ToJson(save), () => { save.SaveRevision++; uploaded++; }, () => _session);
            sync.OtherDevice += () => other++;

            _server.LoseNextUploadReply();
            save.PlayerName = "A";
            sync.MarkDirty();
            sync.Tick(CloudSync.DebounceSeconds);
            Assert.AreEqual("A", JsonUtility.FromJson<PlayerSave>(_server.StoredSave).PlayerName, "the server wrote it");
            Assert.AreEqual(0, uploaded, "the reply was lost, so the device does not know");

            save.PlayerName = "B";
            sync.MarkDirty();
            sync.Tick(CloudSync.RetrySeconds);

            Assert.AreEqual(0, other, "not another device");
            Assert.AreEqual("B", JsonUtility.FromJson<PlayerSave>(_server.StoredSave).PlayerName, "the newer change reached the server");
            Assert.AreEqual(1, uploaded);
            Assert.AreEqual(1, save.SaveRevision);
        }

        // Không đọc được mã phiên: chưa biết ai ghi, nên coi như lỗi mạng và thử lại, không báo nhầm OtherDevice.
        [Test]
        public void Conflict_SessionReadFails_RetriesLater_NotOtherDevice()
        {
            var save = new PlayerSave();
            var other = 0;
            var noState = 0;
            var sync = new CloudSync(_server, () => JsonUtility.ToJson(save), () => save.SaveRevision++, () => _session);
            sync.OtherDevice += () => other++;
            sync.NoState += () => noState++;

            _server.LoseNextUploadReply();
            sync.MarkDirty();
            sync.Tick(CloudSync.DebounceSeconds);
            save.PlayerName = "B";
            sync.MarkDirty();
            _server.FailNextOn(nameof(IGameServer.LoadActiveSessionIdAsync), OnlineStatus.NetworkError);
            sync.Tick(CloudSync.RetrySeconds);
            Assert.AreEqual(0, other, "unknown writer is not another device");
            Assert.AreEqual(0, noState);

            sync.Tick(CloudSync.RetrySeconds);
            Assert.AreEqual(0, other);
            Assert.AreEqual("B", JsonUtility.FromJson<PlayerSave>(_server.StoredSave).PlayerName, "the next retry gets through");
        }

        // Máy khác đã Bootstrap (mã phiên khác) → OtherDevice, không ghi đè.
        [Test]
        public void Conflict_OtherSession_OtherDevice()
        {
            var save = new PlayerSave();
            var other = 0;
            var sync = new CloudSync(_server, () => JsonUtility.ToJson(save), () => save.SaveRevision++, () => _session);
            sync.OtherDevice += () => other++;
            var theirs = JsonUtility.ToJson(new PlayerSave { SaveRevision = 7, PlayerName = "other" });

            _server.FailNext(OnlineStatus.NetworkError);
            sync.MarkDirty();
            sync.Tick(CloudSync.DebounceSeconds);
            _server.Seed(theirs);
            _server.SimulateOtherDevice();
            sync.Tick(CloudSync.RetrySeconds);

            Assert.AreEqual(1, other);
            Assert.AreEqual(theirs, _server.StoredSave, "their save is not overwritten");
        }

        // Review Focus 5: Reset mất trả lời rồi xoá hết mà chưa tạo lại → không có state; không báo "máy khác", về Tiêu đề.
        [Test]
        public void Conflict_NoState_GoesToTitle()
        {
            var save = new PlayerSave();
            var other = 0;
            var noState = 0;
            var sync = new CloudSync(_server, () => JsonUtility.ToJson(save), () => save.SaveRevision++, () => _session);
            sync.OtherDevice += () => other++;
            sync.NoState += () => noState++;

            _server.LoseNextUploadReply();
            sync.MarkDirty();
            sync.Tick(CloudSync.DebounceSeconds);
            Assert.IsTrue(_server.ResetNoRecreateAsync().Result.Ok);
            sync.Tick(CloudSync.RetrySeconds);

            Assert.AreEqual(1, noState);
            Assert.AreEqual(0, other);
            var calls = _server.UploadCalls;
            sync.MarkDirty();
            sync.Tick(60f);
            Assert.AreEqual(calls, _server.UploadCalls, "stopped after NoState");
        }

        // Review O1 mục 6: thay đổi đến trong lúc dừng (chờ lệnh Reset/Xoá tài khoản) mà lệnh lỗi → Resume phải đẩy bù.
        [Test]
        public async Task ChangeWhileStopped_ResumeUploadsIt()
        {
            await _sync.StopAsync();
            _snapshot = "v2";
            _sync.MarkDirty();
            _sync.Resume();
            Run(CloudSync.DebounceSeconds);

            Assert.AreEqual(1, _server.UploadCalls);
            Assert.AreEqual("v2", _server.StoredSave);
        }
    }
}
