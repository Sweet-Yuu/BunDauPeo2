using System.Threading.Tasks;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class LocalGameServerTests
    {
        private static async Task<LocalGameServer> BootedAsync()
        {
            var server = new LocalGameServer(GachaRulesTests.Data());
            await server.SignInAsync();
            await server.BootstrapAsync();
            return server;
        }

        [Test]
        public async Task SignIn_StableId_UntilDeleteAccount()
        {
            var server = new LocalGameServer();
            Assert.AreEqual("", server.PlayerId);

            Assert.AreEqual(OnlineStatus.Ok, await server.SignInAsync());
            var first = server.PlayerId;
            Assert.AreEqual(OnlineStatus.Ok, await server.SignInAsync());
            Assert.AreNotEqual("", first);
            Assert.AreEqual(first, server.PlayerId, "signing in again must not create a new player");

            Assert.AreEqual(OnlineStatus.Ok, await server.DeleteAccountAsync());
            Assert.AreEqual("", server.PlayerId);
            await server.SignInAsync();
            Assert.AreNotEqual("", server.PlayerId);
            Assert.AreNotEqual(first, server.PlayerId);
        }

        [Test]
        public async Task LoadUpload_RoundTrip_AndFailNextChangesNothing()
        {
            var server = new LocalGameServer();
            await server.SignInAsync();

            var empty = await server.LoadSaveAsync();
            Assert.IsTrue(empty.Ok);
            Assert.IsNull(empty.Value);

            Assert.AreEqual(OnlineStatus.Ok, await server.UploadSaveAsync("a"));
            Assert.AreEqual("a", (await server.LoadSaveAsync()).Value);

            server.FailNext(OnlineStatus.NetworkError);
            Assert.AreEqual(OnlineStatus.NetworkError, await server.UploadSaveAsync("b"));
            Assert.AreEqual("a", (await server.LoadSaveAsync()).Value, "failed upload must not change data");

            // Lỗi chỉ cho đúng số lệnh đã hẹn; lệnh sau chạy bình thường.
            server.FailNext(OnlineStatus.ServerError, 2);
            Assert.AreEqual(OnlineStatus.ServerError, (await server.LoadSaveAsync()).Status);
            Assert.AreEqual(OnlineStatus.ServerError, (await server.BootstrapAsync()).Status);
            Assert.IsTrue((await server.LoadSaveAsync()).Ok);

            server.SimulateOtherDevice();
            Assert.AreEqual(OnlineStatus.OtherDevice, await server.UploadSaveAsync("c"));
            Assert.AreEqual("a", server.StoredSave);
        }

        [Test]
        public async Task ResetPlayer_ClearsSave_KeepsId_ReturnsFreshState()
        {
            var server = await BootedAsync();
            var id = server.PlayerId;
            await server.UploadSaveAsync("{\"Version\":1}");
            await server.GachaPullAsync("r1", "b", 1);

            var reset = await server.ResetPlayerAsync();

            Assert.IsTrue(reset.Ok);
            Assert.AreEqual(1500, reset.Value.Gem, "new game state, not the spent one");
            Assert.AreEqual(server.State.ActiveSessionId, reset.Value.ActiveSessionId, "same session keeps working");
            Assert.IsNull((await server.LoadSaveAsync()).Value);
            Assert.AreEqual(id, server.PlayerId);
        }

        [Test]
        public async Task Commands_RunHandlers_AndKeepState()
        {
            var server = new LocalGameServer(GachaRulesTests.Data());
            await server.SignInAsync();
            var boot = await server.BootstrapAsync();
            Assert.AreEqual(1500, boot.Value.State.Gem);
            Assert.AreEqual(1500, server.State.Gem);

            var pull = await server.GachaPullAsync("r1", "b", 1);

            Assert.IsTrue(pull.Ok);
            Assert.AreEqual((int)CommandCode.Ok, pull.Value.Code);
            Assert.AreEqual(1400, server.State.Gem);
            Assert.AreEqual(1400, pull.Value.State.Gem);
            Assert.AreEqual(1, server.CommandCalls);

            var rejected = await server.GachaPullAsync("r2", "nope", 1);
            Assert.AreEqual((int)CommandCode.Invalid, rejected.Value.Code, "a rule rejection is Ok status with a code");
            Assert.AreEqual(1400, server.State.Gem);
        }

        [Test]
        public async Task Local_LostReply_RetrySameRequest_AppliesOnce()
        {
            var server = await BootedAsync();
            server.LoseNextReply();

            var lost = await server.GachaPullAsync("r1", "b", 10);
            Assert.AreEqual(OnlineStatus.NetworkError, lost.Status);
            Assert.AreEqual(600, server.State.Gem, "server already took 900");
            var written = server.State.LastResultJson;
            var owned = server.State.OwnedCharacterIds.Count;

            var retry = await server.GachaPullAsync("r1", "b", 10);

            Assert.IsTrue(retry.Ok);
            Assert.AreEqual((int)CommandCode.Ok, retry.Value.Code);
            Assert.AreEqual(written, retry.Value.ResultJson, "the retry returns the result already written");
            Assert.AreEqual(600, server.State.Gem, "charged once");
            Assert.AreEqual(owned, server.State.OwnedCharacterIds.Count);
            Assert.AreEqual(10, JsonUtility.FromJson<GachaPullPayload>(retry.Value.ResultJson).Pulls.Count);

            // Mã khác thì là lệnh mới: không bị nuốt nhầm làm lệnh cũ.
            await server.GachaPullAsync("r2", "b", 1);
            Assert.AreEqual(500, server.State.Gem);
        }

        [Test]
        public async Task FailNext_ChangesNothing()
        {
            var server = await BootedAsync();
            server.FailNext(OnlineStatus.ServerError);

            var failed = await server.GachaPullAsync("r1", "b", 10);

            Assert.AreEqual(OnlineStatus.ServerError, failed.Status);
            Assert.AreEqual(1500, server.State.Gem);
            CollectionAssert.AreEqual(new[] { "r0" }, server.State.OwnedCharacterIds, "only the starter");
            Assert.AreEqual("", server.State.LastRequestId);
        }

        [Test]
        public async Task OtherDeviceBootstrap_NextCommandOtherDevice()
        {
            var server = await BootedAsync();
            server.SimulateOtherDeviceBootstrap();

            var result = await server.GachaPullAsync("r1", "b", 1);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual((int)CommandCode.OtherDevice, result.Value.Code);
            Assert.AreEqual(1500, server.State.Gem);
            Assert.AreEqual(OnlineStatus.OtherDevice, (await server.ResetPlayerAsync()).Status, "reset is refused too");
        }

        [Test]
        public async Task OldVersion_ClientTooOld()
        {
            var server = await BootedAsync();
            server.ProtocolVersion = 1;

            Assert.AreEqual(OnlineStatus.ClientTooOld, (await server.GachaPullAsync("r1", "b", 1)).Status);
            Assert.AreEqual(OnlineStatus.ClientTooOld, (await server.BootstrapAsync()).Status);
            Assert.AreEqual(1500, server.State.Gem);
        }

        [Test]
        public async Task Seed_FullCache_BecomesState()
        {
            var save = new PlayerSave();
            save.Balances.Add(new CurrencyAmount(CurrencyType.Coin, 50));
            save.Balances.Add(new CurrencyAmount(CurrencyType.Gem, 777));
            save.OwnedCharacterIds.AddRange(new[] { "r1", "s0" });
            save.ClearedStages.Add("s1");
            var server = new LocalGameServer(GachaRulesTests.Data());
            server.Seed(JsonUtility.ToJson(save));
            await server.SignInAsync();

            var boot = await server.BootstrapAsync();

            Assert.AreEqual(777, boot.Value.State.Gem, "seeded state, not a new game");
            CollectionAssert.AreEqual(new[] { "r1", "s0" }, boot.Value.State.OwnedCharacterIds);
            CollectionAssert.AreEqual(new[] { "s1" }, boot.Value.State.ClearedStages);
            Assert.AreNotEqual("", boot.Value.SessionId);
        }

        [Test]
        public async Task Reset_RecreateFalse_ThenCommand_NoState()
        {
            var server = await BootedAsync();

            var reset = await server.ResetNoRecreateAsync();

            Assert.IsTrue(reset.Ok);
            Assert.IsNull(reset.Value);
            Assert.IsNull((await server.LoadActiveSessionIdAsync()).Value);
            var result = await server.GachaPullAsync("r1", "b", 1);
            Assert.AreEqual((int)CommandCode.NoState, result.Value.Code);
        }

        [Test]
        public async Task DelayNext_HoldsCommandUntilGateOpens()
        {
            var server = await BootedAsync();
            var gate = new TaskCompletionSource<bool>();
            server.DelayNext(gate);

            var pending = server.DevAddGemAsync("r1", 5);
            await Task.Yield();
            Assert.IsFalse(pending.IsCompleted);
            Assert.AreEqual(1500, server.State.Gem);

            gate.SetResult(true);
            Assert.IsTrue((await pending).Ok);
            Assert.AreEqual(1505, server.State.Gem);
            Assert.AreEqual(server.State.ActiveSessionId, (await server.LoadActiveSessionIdAsync()).Value);
        }
    }
}
