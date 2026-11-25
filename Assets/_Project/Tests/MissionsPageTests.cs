using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BunDauPeo.Missions;
using BunDauPeo.Rules;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Luật hiển thị và luồng nhận của trang Nhiệm vụ (spec đợt 3 8.1, 8.5); phần bấm thật nằm ở MissionsPlayTests.
    public class MissionsPageTests
    {
        private static MissionRow Row(bool locked = false, bool claimed = false, bool claimable = false, int window = 0, int progress = 0, int target = 1) =>
            new MissionRow { Id = "x", Locked = locked, Claimed = claimed, Claimable = claimable, WindowState = window, Progress = progress, Target = target };

        [Test]
        public void ButtonState_ByRow()
        {
            Assert.AreEqual(MissionButtonState.Goto, MissionRowRule.State(Row(progress: 3, target: 5)));
            Assert.AreEqual(MissionButtonState.Claim, MissionRowRule.State(Row(claimable: true, progress: 5, target: 5)));
            Assert.AreEqual(MissionButtonState.Claimed, MissionRowRule.State(Row(claimed: true, progress: 5, target: 5)));
            Assert.AreEqual(MissionButtonState.NotYet, MissionRowRule.State(Row(window: 2, progress: 1)));
            Assert.AreEqual(MissionButtonState.Missed, MissionRowRule.State(Row(window: 3, progress: 1)));
            Assert.AreEqual(MissionButtonState.Locked, MissionRowRule.State(Row(locked: true)));
            // Đã nhận thắng khung giờ đã qua; khoá thắng mọi thứ.
            Assert.AreEqual(MissionButtonState.Claimed, MissionRowRule.State(Row(claimed: true, window: 3, progress: 1)));
            Assert.AreEqual(MissionButtonState.Locked, MissionRowRule.State(Row(locked: true, claimed: true, claimable: true)));
        }

        [Test]
        public async Task Badge_CountsClaimableOnly()
        {
            var rig = await MissionServiceTests.MakeAsync();
            // Khung sáng nhận được; khung chiều chưa tới giờ, Thắng 5 trận chưa đủ: chỉ một dòng tính.
            Assert.AreEqual(1, rig.Service.ClaimableCount(MissionService.TabDaily));
            Assert.AreEqual("1", MissionRowRule.Badge(rig.Service.ClaimableCount(MissionService.TabDaily)));
            Assert.AreEqual("", MissionRowRule.Badge(rig.Service.ClaimableCount(MissionService.TabNewbie)), "no number at zero");
            rig.Done.Add("n1");
            Assert.AreEqual("1", MissionRowRule.Badge(rig.Service.ClaimableCount(MissionService.TabNewbie)));
        }

        [Test]
        public async Task ClaimAll_Disabled_WhenNothingReady()
        {
            var rig = await MissionServiceTests.MakeAsync();
            Assert.IsFalse(MissionRowRule.ClaimAllEnabled(rig.Service.ClaimableCount(MissionService.TabNewbie)));
            Assert.IsTrue(MissionRowRule.ClaimAllEnabled(rig.Service.ClaimableCount(MissionService.TabDaily)));
            Assert.IsFalse(MissionRowRule.ClaimAllEnabled(0));
        }

        [Test]
        public void Goto_Destinations_PerSpecTable()
        {
            foreach (var id in new[] { "daily_win_5", "new_first_win", "new_win_10" })
                Assert.AreEqual(HubGoto.Stages, MissionGoto.Destination(id), id);
            foreach (var id in new[] { "daily_pull_1", "new_first_pull" })
                Assert.AreEqual(HubGoto.Gacha, MissionGoto.Destination(id), id);
            Assert.AreEqual(HubGoto.Characters, MissionGoto.Destination("new_star_up"));
            Assert.AreEqual(HubGoto.Afk, MissionGoto.Destination("new_afk"));
            Assert.AreEqual(HubGoto.Staff, MissionGoto.Destination("new_staff"));
            Assert.AreEqual(HubGoto.Upgrade, MissionGoto.Destination("new_upgrade"));
            Assert.AreEqual(HubGoto.Profile, MissionGoto.Destination("new_name"));
            Assert.AreEqual(HubGoto.Profile, MissionGoto.Destination("new_link"));
            // Thể lực theo khung giờ và việc khoá không có Đi tới.
            foreach (var id in new[] { "daily_login_am", "daily_login_pm", "daily_level_1", "unknown" })
                Assert.AreEqual(HubGoto.None, MissionGoto.Destination(id), id);
        }

        [Test]
        public async Task ClaimWithWaste_AsksConfirmFirst_CancelSendsNothing()
        {
            var rig = await MissionServiceTests.MakeAsync(stamina: 950);
            var asked = new List<StaminaPreview>();
            var flow = new MissionClaimFlow(rig.Service, p => { asked.Add(p); return Task.FromResult(false); });
            var calls = rig.Server.CommandCalls;
            var before = UnityEngine.JsonUtility.ToJson(rig.Save);

            var result = await flow.ClaimAsync(MissionService.TabDaily, "a");

            Assert.IsNull(result, "cancel means no result");
            Assert.AreEqual(1, asked.Count);
            Assert.AreEqual(950, asked[0].Have);
            Assert.AreEqual(49, asked[0].Granted);
            Assert.AreEqual(51, asked[0].Wasted);
            Assert.AreEqual(999, asked[0].Cap);
            Assert.AreEqual(calls, rig.Server.CommandCalls, "Cancel must not send the command");
            Assert.AreEqual(before, UnityEngine.JsonUtility.ToJson(rig.Save));
        }

        [Test]
        public async Task ClaimWithWaste_Accept_ClaimsAndCapsAt999()
        {
            var rig = await MissionServiceTests.MakeAsync(stamina: 950);
            var flow = new MissionClaimFlow(rig.Service, _ => Task.FromResult(true));

            var result = await flow.ClaimAsync(MissionService.TabDaily, "a");

            Assert.AreEqual(MissionClaimStatus.Ok, result.Value.Status);
            Assert.AreEqual(51, result.Value.StaminaWasted);
            Assert.AreEqual(999, rig.Save.Stamina.Current);
        }

        [Test]
        public async Task ClaimAll_Asks_Once_ForTotalWaste()
        {
            var rig = await MissionServiceTests.MakeAsync(stamina: 900);
            rig.Done.Add("s");
            var asked = new List<StaminaPreview>();
            var flow = new MissionClaimFlow(rig.Service, p => { asked.Add(p); return Task.FromResult(true); });

            var result = await flow.ClaimAsync(MissionService.TabDaily, null);

            Assert.AreEqual(1, asked.Count, "one box for the whole claim-all");
            Assert.AreEqual(200, asked[0].Reward, "a and s, 100 each");
            Assert.AreEqual(101, asked[0].Wasted);
            Assert.AreEqual(99, asked[0].Granted);
            Assert.AreEqual(MissionClaimStatus.Ok, result.Value.Status);
        }

        [Test]
        public async Task ClaimNoWaste_NoConfirm()
        {
            var rig = await MissionServiceTests.MakeAsync(stamina: 50);
            var flow = new MissionClaimFlow(rig.Service, _ => throw new InvalidOperationException("no box expected"));

            var result = await flow.ClaimAsync(MissionService.TabDaily, "a");

            Assert.AreEqual(MissionClaimStatus.Ok, result.Value.Status);
        }

        [Test]
        public void DailyTab_ShowsRemaining_NewbieTab_Hides()
        {
            Assert.IsTrue(MissionRowRule.ShowRemaining(MissionService.TabDaily));
            Assert.IsFalse(MissionRowRule.ShowRemaining(MissionService.TabNewbie));
            Assert.AreEqual("19:05", MissionRowRule.FormatRemaining(new TimeSpan(19, 5, 59)));
            Assert.AreEqual("00:00", MissionRowRule.FormatRemaining(TimeSpan.FromSeconds(-5)));
            Assert.AreEqual("23:59", MissionRowRule.FormatRemaining(new TimeSpan(23, 59, 0)));
        }

        [Test]
        public void Completed_SkipsLockedRows()
        {
            var rows = new List<MissionRow> { Row(claimed: true), Row(), Row(locked: true) };
            Assert.AreEqual((1, 2), MissionRowRule.Completed(rows));
        }
    }
}
