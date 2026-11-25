using System.Collections.Generic;
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
    

        // Dữ liệu có hai việc tân thủ do máy báo, một việc ngày thắng 1 trận (không đạt được trong test), và banner quay miễn phí.
        private static async Task<LocalGameServer> BootedWithMissionsAsync()
        {
            var data = GachaRulesTests.Data();
            data.Banners[0].FreePullDaily = true;
            data.Missions.Add(new MissionData { Id = "new_link", Tab = 1, Counter = MissionRules.CounterClientFlag, Target = 1, RewardGem = 100 });
            data.Missions.Add(new MissionData { Id = "new_name", Tab = 1, Counter = MissionRules.CounterClientFlag, Target = 1, RewardGem = 50 });
            var server = new LocalGameServer(data);
            await server.SignInAsync();
            await server.BootstrapAsync();
            return server;
        }

        private static readonly string[] Both = { "new_link", "new_name" };

        [Test]
        public async Task ClaimMission_Ok_AppliesGem()
        {
            var server = await BootedWithMissionsAsync();

            var r = await server.ClaimMissionAsync("r1", "new_link", new List<string> { "new_link" });

            Assert.IsTrue(r.Ok);
            Assert.AreEqual((int)CommandCode.Ok, r.Value.Code);
            Assert.AreEqual(1600, server.State.Gem);
            Assert.AreEqual(1600, r.Value.State.Gem);
            Assert.AreEqual(100, JsonUtility.FromJson<MissionClaimPayload>(r.Value.ResultJson).Gem);

            var again = await server.ClaimMissionAsync("r2", "new_link", new List<string> { "new_link" });
            Assert.AreEqual((int)CommandCode.AlreadyClaimed, again.Value.Code);
            Assert.AreEqual(1600, server.State.Gem);
        }

        [Test]
        public async Task Local_LostReply_ClaimMission_RetrySameRequest_AppliesOnce()
        {
            var server = await BootedWithMissionsAsync();
            server.LoseNextReply();

            var lost = await server.ClaimMissionAsync("r1", "new_link", Both);
            Assert.AreEqual(OnlineStatus.NetworkError, lost.Status);
            Assert.AreEqual(1600, server.State.Gem, "the server already paid");
            var written = server.State.LastResultJson;

            var retry = await server.ClaimMissionAsync("r1", "new_link", Both);

            Assert.IsTrue(retry.Ok);
            Assert.AreEqual((int)CommandCode.Ok, retry.Value.Code, "not AlreadyClaimed: the stored result is returned");
            Assert.AreEqual(written, retry.Value.ResultJson);
            Assert.AreEqual(1600, server.State.Gem, "paid once");
        }

        [Test]
        public async Task Local_LostReply_ClaimAllMissions_RetrySameRequest_AppliesOnce()
        {
            var server = await BootedWithMissionsAsync();
            server.LoseNextReply();

            Assert.AreEqual(OnlineStatus.NetworkError, (await server.ClaimAllMissionsAsync("r1", 1, Both)).Status);
            Assert.AreEqual(1650, server.State.Gem);
            var written = server.State.LastResultJson;

            var retry = await server.ClaimAllMissionsAsync("r1", 1, Both);

            Assert.AreEqual((int)CommandCode.Ok, retry.Value.Code);
            Assert.AreEqual(written, retry.Value.ResultJson);
            Assert.AreEqual(150, JsonUtility.FromJson<MissionClaimPayload>(retry.Value.ResultJson).Gem);
            Assert.AreEqual(1650, server.State.Gem);
            Assert.AreEqual(2, server.State.Missions.NewbieClaimed.Count);
        }

        [Test]
        public async Task Local_LostReply_GachaFreePull_RetrySameRequest_AppliesOnce()
        {
            var server = await BootedWithMissionsAsync();
            server.LoseNextReply();

            Assert.AreEqual(OnlineStatus.NetworkError, (await server.GachaFreePullAsync("r1", "b")).Status);
            var written = server.State.LastResultJson;
            var history = server.State.GachaHistory.Count;
            Assert.AreEqual(1, history);

            var retry = await server.GachaFreePullAsync("r1", "b");

            Assert.AreEqual((int)CommandCode.Ok, retry.Value.Code, "not FreePullUsed");
            Assert.AreEqual(written, retry.Value.ResultJson, "the result the server wrote, not a new roll");
            Assert.AreEqual(1, server.State.GachaHistory.Count);
            Assert.AreEqual(1500, server.State.Gem);
            Assert.AreEqual(1, server.State.Missions.DailyPulls);
        }

        [Test]
        public async Task ClaimMission_OtherSession_OtherDevice()
        {
            var server = await BootedWithMissionsAsync();
            server.SimulateOtherDeviceBootstrap();

            var r = await server.ClaimMissionAsync("r1", "new_link", Both);

            Assert.IsTrue(r.Ok);
            Assert.AreEqual((int)CommandCode.OtherDevice, r.Value.Code);
            Assert.AreEqual(1500, server.State.Gem);
            Assert.AreEqual((int)CommandCode.OtherDevice, (await server.GachaFreePullAsync("r2", "b")).Value.Code);
        }

        [Test]
        public async Task FreePull_ThenPaidPull_Independent()
        {
            var server = await BootedWithMissionsAsync();

            Assert.AreEqual((int)CommandCode.Ok, (await server.GachaFreePullAsync("r1", "b")).Value.Code);
            Assert.AreEqual((int)CommandCode.FreePullUsed, (await server.GachaFreePullAsync("r2", "b")).Value.Code);
            Assert.AreEqual((int)CommandCode.Ok, (await server.GachaPullAsync("r3", "b", 1)).Value.Code, "the paid pull is unaffected");

            Assert.AreEqual(1400, server.State.Gem, "only the paid pull cost Gem");
            Assert.AreEqual(2, server.State.Missions.DailyPulls);
            Assert.AreEqual(2, server.State.GachaHistory.Count);
        }

        [Test]
        public async Task DevMissionCommands_RejectDev_DevOnly_NoChange()
        {
            var server = await BootedWithMissionsAsync();
            await server.ClaimMissionAsync("r1", "new_link", Both);
            server.RejectDev = true;
            var before = JsonUtility.ToJson(server.State);

            Assert.AreEqual((int)CommandCode.DevOnly, (await server.DevResetMissionsAsync("r2")).Value.Code);
            Assert.AreEqual((int)CommandCode.DevOnly, (await server.DevAddStaminaAsync("r3", 50)).Value.Code);
            Assert.AreEqual(before, JsonUtility.ToJson(server.State));

            server.RejectDev = false;
            Assert.AreEqual((int)CommandCode.Ok, (await server.DevAddStaminaAsync("r4", 50)).Value.Code);
            Assert.AreEqual(150, server.State.Stamina.Current, "100 + 50, past Max");
            Assert.AreEqual((int)CommandCode.Ok, (await server.DevResetMissionsAsync("r5")).Value.Code);
            CollectionAssert.IsEmpty(server.State.Missions.NewbieClaimed);
        }

        [Test]
        public async Task ClaimMission_NullClientDone_NotReady()
        {
            var server = await BootedWithMissionsAsync();

            var r = await server.ClaimMissionAsync("r1", "new_link", null);

            Assert.AreEqual((int)CommandCode.NotReady, r.Value.Code);
            Assert.AreEqual(1500, server.State.Gem);
        }
    }
}
