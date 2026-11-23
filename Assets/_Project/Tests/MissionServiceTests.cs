using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BunDauPeo.Core;
using BunDauPeo.Missions;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.Tests
{
    // Thưởng do server tính (HandlerMissionTests); ở đây là phần máy: kiểm trước khi gửi, áp state, ánh xạ mã, xem trước thể lực mất.
    public class MissionServiceTests
    {
        // 22:00 UTC = 05:00 giờ Việt Nam: khung sáng (0–12) đang mở, khung chiều (12–24) chưa tới.
        private static readonly DateTime Morning = new DateTime(2026, 11, 20, 22, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Afternoon = Morning.AddHours(8);

        internal sealed class Rig
        {
            public LocalGameServer Server;
            public PlayerSave Save;
            public MissionService Service;
            public List<string> Done = new();
            public int Applies;
            public int ChangedCalls;
        }

        private static GameData Data()
        {
            var data = GachaRulesTests.Data();
            data.Stamina = new StaminaData { Max = 100, RegenSeconds = 300, RewardCap = 999 };
            data.Missions.Add(new MissionData { Id = "a", Tab = 0, Counter = MissionRules.CounterLoginWindow, Target = 1, RewardGem = 10, RewardStamina = 100, WindowStartHour = 0, WindowEndHour = 12 });
            data.Missions.Add(new MissionData { Id = "p", Tab = 0, Counter = MissionRules.CounterLoginWindow, Target = 1, RewardGem = 10, RewardStamina = 100, WindowStartHour = 12, WindowEndHour = 24 });
            data.Missions.Add(new MissionData { Id = "w", Tab = 0, Counter = MissionRules.CounterWins, Target = 5, RewardGem = 20 });
            data.Missions.Add(new MissionData { Id = "s", Tab = 0, Counter = MissionRules.CounterClientFlag, Target = 1, RewardStamina = 100 });
            data.Missions.Add(new MissionData { Id = "n1", Tab = 1, Counter = MissionRules.CounterClientFlag, Target = 1, RewardGem = 50 });
            return data;
        }

        // serverNow lệch với máy thì ca "máy tưởng được, server từ chối" có thật.
        internal static async Task<Rig> MakeAsync(int stamina = 50, DateTime? serverNow = null, DateTime? deviceNow = null)
        {
            var data = Data();
            var server = new LocalGameServer(() => serverNow ?? Morning, data);
            await server.SignInAsync();
            var seed = new PlayerSave();
            seed.Balances.Add(new CurrencyAmount(CurrencyType.Gem, 1000));
            seed.Stamina = new StaminaState { Current = stamina, AnchorUtcTicks = stamina < 100 ? Morning.Ticks : 0 }; // anchor 0 = đầy
            server.Seed(JsonUtility.ToJson(seed));
            var boot = await server.BootstrapAsync();
            var rig = new Rig { Server = server, Save = new PlayerSave() };
            ServerStateApplier.Apply(rig.Save, boot.Value.State);
            var gate = new CommandGate(_ => new TaskCompletionSource<bool>().Task);
            rig.Service = new MissionService(rig.Save, data, server, gate, state =>
            {
                rig.Applies++;
                ServerStateApplier.Apply(rig.Save, state);
                return true;
            }, () => deviceNow ?? Morning, () => rig.Done);
            rig.Service.Changed += () => rig.ChangedCalls++;
            return rig;
        }

        private static long Gem(PlayerSave save) => new Wallet(save).Get(CurrencyType.Gem);

        [Test]
        public async Task Rows_ReflectProgressAndDay()
        {
            var rig = await MakeAsync();
            var today = MissionRules.DayIndex(Morning);
            rig.Save.Missions = new MissionState { DailyDay = today, DailyWins = 3 };
            rig.Save.Missions.DailyClaimed.Add("a");

            var rows = rig.Service.Rows(MissionService.TabDaily);
            Assert.AreEqual(4, rows.Count, "only the daily tab");
            Assert.IsTrue(Row(rows, "a").Claimed);
            Assert.AreEqual(3, Row(rows, "w").Progress);
            Assert.IsFalse(Row(rows, "p").Claimable, "afternoon window has not started");
            Assert.AreEqual(2, Row(rows, "p").WindowState);

            // Sang ngày: bộ đếm và danh sách đã nhận của hôm qua không còn tính.
            rig.Save.Missions.DailyDay = today - 1;
            rows = rig.Service.Rows(MissionService.TabDaily);
            Assert.AreEqual(0, Row(rows, "w").Progress);
            Assert.IsFalse(Row(rows, "a").Claimed);
            Assert.IsTrue(Row(rows, "a").Claimable);
            Assert.AreEqual(1, rig.Service.ClaimableCount(MissionService.TabDaily));
        }

        [Test]
        public async Task Claim_Ok_AppliesStateAndRaisesChanged()
        {
            var rig = await MakeAsync(stamina: 50);

            var result = await rig.Service.ClaimAsync("a");

            Assert.AreEqual(MissionClaimStatus.Ok, result.Status);
            Assert.AreEqual(10, result.Gem);
            Assert.AreEqual(100, result.Stamina);
            Assert.AreEqual(0, result.StaminaWasted);
            Assert.AreEqual(1010, Gem(rig.Save));
            Assert.AreEqual(150, rig.Save.Stamina.Current);
            Assert.IsTrue(rig.Save.Missions.DailyClaimed.Contains("a"));
            Assert.AreEqual(1, rig.Applies);
            Assert.AreEqual(1, rig.ChangedCalls);
            Assert.IsTrue(Row(rig.Service.Rows(0), "a").Claimed);
        }

        [Test]
        public async Task Claim_Offline_NoChange()
        {
            var rig = await MakeAsync();
            var before = JsonUtility.ToJson(rig.Save);
            rig.Server.FailNext(OnlineStatus.NetworkError);

            var result = await rig.Service.ClaimAsync("a");

            Assert.AreEqual(MissionClaimStatus.Offline, result.Status);
            Assert.AreEqual(before, JsonUtility.ToJson(rig.Save));
            Assert.AreEqual(0, rig.Applies);
            Assert.AreEqual(0, rig.ChangedCalls);
        }

        // Máy thấy đúng giờ, server (giờ khác) từ chối: không áp gì.
        [Test]
        public async Task Claim_ServerRejects_MapsStatus()
        {
            var rig = await MakeAsync(serverNow: Afternoon, deviceNow: Morning);
            var before = JsonUtility.ToJson(rig.Save);

            var result = await rig.Service.ClaimAsync("a");

            Assert.AreEqual(MissionClaimStatus.NotInWindow, result.Status);
            Assert.AreEqual(before, JsonUtility.ToJson(rig.Save));
            Assert.AreEqual(0, rig.Applies);
        }

        [Test]
        public async Task Claim_LocalPrecheck_NoServerCall()
        {
            var rig = await MakeAsync();
            var calls = rig.Server.CommandCalls;

            Assert.AreEqual(MissionClaimStatus.Invalid, (await rig.Service.ClaimAsync("nope")).Status);
            Assert.AreEqual(MissionClaimStatus.NotReady, (await rig.Service.ClaimAsync("w")).Status);
            Assert.AreEqual(MissionClaimStatus.NotInWindow, (await rig.Service.ClaimAsync("p")).Status);
            Assert.AreEqual(MissionClaimStatus.NotReady, (await rig.Service.ClaimAsync("n1")).Status, "client flag is not done");
            Assert.AreEqual(calls, rig.Server.CommandCalls);

            Assert.AreEqual(MissionClaimStatus.Ok, (await rig.Service.ClaimAsync("a")).Status);
            calls = rig.Server.CommandCalls;
            Assert.AreEqual(MissionClaimStatus.AlreadyClaimed, (await rig.Service.ClaimAsync("a")).Status);
            Assert.AreEqual(calls, rig.Server.CommandCalls);
        }

        [Test]
        public async Task ClaimAll_ClaimsWholeTab_NothingLeftIsNoCall()
        {
            var rig = await MakeAsync(stamina: 50);
            rig.Done.Add("s");

            var result = await rig.Service.ClaimAllAsync(MissionService.TabDaily);

            Assert.AreEqual(MissionClaimStatus.Ok, result.Status);
            Assert.AreEqual(10, result.Gem);
            Assert.AreEqual(200, result.Stamina, "a (100) and s (100)");
            Assert.AreEqual(250, rig.Save.Stamina.Current);
            Assert.AreEqual(0, rig.Service.ClaimableCount(MissionService.TabDaily));

            var calls = rig.Server.CommandCalls;
            Assert.AreEqual(MissionClaimStatus.NothingToClaim, (await rig.Service.ClaimAllAsync(MissionService.TabDaily)).Status);
            Assert.AreEqual(calls, rig.Server.CommandCalls);
        }

        [Test]
        public async Task Claim_ChangedHandlerThrows_StateStillApplied()
        {
            var rig = await MakeAsync();
            rig.Service.Changed += () => throw new InvalidOperationException("boom");
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            var result = await rig.Service.ClaimAsync("a");

            Assert.AreEqual(MissionClaimStatus.Ok, result.Status);
            Assert.AreEqual(1010, Gem(rig.Save));
            Assert.AreEqual(1, rig.ChangedCalls, "handlers after the throwing one still run");
        }

        [Test]
        public void ClientDone_FromSave_NameAndStaff()
        {
            var save = new PlayerSave();
            CollectionAssert.IsEmpty(MissionService.ClientDoneFromSave(save), "a fresh save has done nothing");

            save.PlayerName = "Bun";
            Assert.AreEqual(new[] { "new_name" }, MissionService.ClientDoneFromSave(save));

            save.Staff.Add(new StaffAssignment { CharacterId = "r0", Role = StaffRole.Cook });
            CollectionAssert.AreEquivalent(new[] { "new_name", "new_staff" }, MissionService.ClientDoneFromSave(save));
        }

        [Test]
        public void ClientDone_FromSave_UpgradeAfkLink()
        {
            var save = new PlayerSave();
            save.Upgrades.Add(new UpgradeLevel { UpgradeId = "u", Level = 0 });
            CollectionAssert.IsEmpty(MissionService.ClientDoneFromSave(save), "level 0 is not bought");

            save.Upgrades.Add(new UpgradeLevel { UpgradeId = "v", Level = 1 });
            save.AfkCollected = true;
            CollectionAssert.AreEquivalent(new[] { "new_upgrade", "new_afk" }, MissionService.ClientDoneFromSave(save));
            CollectionAssert.Contains(MissionService.ClientDoneFromSave(save, linked: true), "new_link");
        }

        [Test]
        public async Task PreviewStaminaWaste_950Plus100_Is51()
        {
            var rig = await MakeAsync(stamina: 950);
            Assert.AreEqual(51, rig.Service.PreviewStaminaWaste(MissionService.TabDaily, "a"));
        }

        [Test]
        public async Task PreviewStaminaWaste_NoWaste_Zero_AndRewardWithoutStamina()
        {
            var rig = await MakeAsync(stamina: 50);
            Assert.AreEqual(0, rig.Service.PreviewStaminaWaste(MissionService.TabDaily, "a"));
            Assert.AreEqual(0, rig.Service.PreviewStaminaWaste(MissionService.TabDaily, "w"), "a gem-only mission wastes nothing");
        }

        [Test]
        public async Task PreviewStaminaWaste_ClaimAll_AddsWholeTab()
        {
            var rig = await MakeAsync(stamina: 900);
            rig.Done.Add("s");
            // a: 900 → 999 (mất 1); s: đã đầy trần nên mất cả 100.
            Assert.AreEqual(101, rig.Service.PreviewStaminaWaste(MissionService.TabDaily, null));
            Assert.AreEqual(900, rig.Save.Stamina.Current, "a preview changes nothing");
        }

        [Test]
        public async Task UntilNextDay_FromServerTime()
        {
            var rig = await MakeAsync();
            Assert.AreEqual(TimeSpan.FromHours(19), rig.Service.UntilNextDay());
        }

        private static MissionRow Row(IReadOnlyList<MissionRow> rows, string id)
        {
            foreach (var r in rows) if (r.Id == id) return r;
            Assert.Fail($"no row {id}");
            return null;
        }
    }
}
