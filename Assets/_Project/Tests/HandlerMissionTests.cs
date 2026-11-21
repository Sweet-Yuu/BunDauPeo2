using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Nhiệm vụ ngày + tân thủ (spec đợt 3 mục 6.1, 7). 10:00 giờ VN = 03:00 UTC: khung sáng (am).
    public class HandlerMissionTests
    {
        static readonly DateTime Morning = new DateTime(2026, 11, 21, 3, 0, 0, DateTimeKind.Utc);

        static MissionData Mission(string id, int tab, int counter, int target, long gem = 0, int stamina = 0, int from = 0, int to = 0, bool locked = false) =>
            new MissionData { Id = id, Tab = tab, Counter = counter, Target = target, RewardGem = gem, RewardStamina = stamina, WindowStartHour = from, WindowEndHour = to, Locked = locked };

        // Mười màn trận b1..b10 và một màn cốt truyện st.
        static GameData Data()
        {
            var data = new GameData
            {
                Stamina = new StaminaData { Max = 100, RegenSeconds = 300, RewardCap = 999 },
                MaxTeamSize = 3,
                NewGame = new NewGameData { StartingGem = 0 },
            };
            for (var i = 1; i <= 10; i++) data.Stages.Add(new StageData { Id = "b" + i, Kind = 0 });
            data.Stages.Add(new StageData { Id = "st", Kind = 1 });
            data.Missions.AddRange(new[]
            {
                Mission("daily_win_5", 0, MissionRules.CounterWins, 5, gem: 20),
                Mission("daily_pull_1", 0, MissionRules.CounterPulls, 1, gem: 20),
                Mission("daily_level_1", 0, MissionRules.CounterNone, 0, locked: true),
                Mission("daily_login_am", 0, MissionRules.CounterLoginWindow, 1, stamina: 100, from: 0, to: 12),
                Mission("daily_login_pm", 0, MissionRules.CounterLoginWindow, 1, stamina: 100, from: 12, to: 24),
                Mission("new_link", 1, MissionRules.CounterClientFlag, 1, gem: 100),
                Mission("new_first_win", 1, MissionRules.CounterBattleStagesCleared, 1, gem: 100),
                Mission("new_first_pull", 1, MissionRules.CounterGachaEver, 1, gem: 50),
                Mission("new_star_up", 1, MissionRules.CounterStarUpEver, 1, gem: 100),
                Mission("new_win_10", 1, MissionRules.CounterBattleStagesCleared, 10, gem: 200),
            });
            return data;
        }

        static ServerState State() => new ServerState
        {
            Stamina = new StaminaState { Current = 100, AnchorUtcTicks = 0 },
            Missions = new MissionState { DailyDay = MissionRules.DayIndex(Morning) },
        };

        static ServerContext Ctx(DateTime now) => new ServerContext { NowUtc = now, NewId = () => "x" };

        static HandlerOutcome Claim(ServerState s, string id, DateTime? now = null, params string[] clientDone) =>
            ServerHandlers.ClaimMission(s, Data(), Ctx(now ?? Morning), id, new List<string>(clientDone));

        static HandlerOutcome ClaimAll(ServerState s, int tab, DateTime? now = null, params string[] clientDone) =>
            ServerHandlers.ClaimAllMissions(s, Data(), Ctx(now ?? Morning), tab, new List<string>(clientDone));

        static void AssertRejected(HandlerOutcome r, CommandCode code, ServerState s, string before, string label)
        {
            Assert.AreEqual(code, r.Code, label);
            Assert.IsNull(r.State, label);
            Assert.AreEqual(before, JsonUtility.ToJson(s), label + ": the state is untouched");
        }

        [Test]
        public void Claim_Daily_NotReady_Rejected()
        {
            var s = State();
            s.Missions.DailyWins = 4;
            var before = JsonUtility.ToJson(s);

            AssertRejected(Claim(s, "daily_win_5"), CommandCode.NotReady, s, before, "4 of 5 wins");
        }

        [Test]
        public void Claim_Daily_Ready_AddsGem_MarksClaimed()
        {
            var s = State();
            s.Gem = 10;
            s.Missions.DailyWins = 5;

            var r = Claim(s, "daily_win_5");

            Assert.AreEqual(CommandCode.Ok, r.Code);
            Assert.AreEqual(30, s.Gem);
            CollectionAssert.AreEqual(new[] { "daily_win_5" }, s.Missions.DailyClaimed);
            var p = (MissionClaimPayload)r.Payload;
            Assert.AreEqual((20L, 0, 0), (p.Gem, p.Stamina, p.StaminaWasted));
        }

        [Test]
        public void Claim_Twice_SameDay_AlreadyClaimed()
        {
            var s = State();
            s.Missions.DailyWins = 5;
            Assert.AreEqual(CommandCode.Ok, Claim(s, "daily_win_5").Code);
            var gem = s.Gem;
            var before = JsonUtility.ToJson(s);

            AssertRejected(Claim(s, "daily_win_5"), CommandCode.AlreadyClaimed, s, before, "second claim");
            Assert.AreEqual(gem, s.Gem);
        }

        [Test]
        public void Claim_NextDay_ClaimsAgain()
        {
            var s = State();
            s.Missions.DailyWins = 5;
            Assert.AreEqual(CommandCode.Ok, Claim(s, "daily_win_5").Code);

            // Qua 00:00 giờ VN: số thắng của hôm qua không tính; làm đủ 5 trận mới thì nhận lại được.
            var next = Morning.AddDays(1);
            Assert.AreEqual(CommandCode.NotReady, Claim(s, "daily_win_5", next).Code, "yesterday's wins do not count");

            MissionRules.RollDay(s.Missions, next);
            s.Missions.DailyWins = 5;
            Assert.AreEqual(CommandCode.Ok, Claim(s, "daily_win_5", next).Code);
            Assert.AreEqual(40, s.Gem);
        }

        [Test]
        public void Claim_NextDay_ClaimedListIsForgotten()
        {
            var s = State();
            s.Missions.DailyWins = 5;
            s.Missions.DailyClaimed.Add("daily_pull_1");
            s.Missions.DailyPulls = 1;
            var next = Morning.AddDays(1);

            // Hôm qua đã nhận quay: hôm nay (bộ đếm 0) phải NotReady chứ không phải AlreadyClaimed.
            Assert.AreEqual(CommandCode.NotReady, Claim(s, "daily_pull_1", next).Code);
            Assert.AreEqual(CommandCode.NotReady, Claim(s, "daily_win_5", next).Code);
        }

        [Test]
        public void Claim_Locked_Rejected()
        {
            var s = State();
            var before = JsonUtility.ToJson(s);
            AssertRejected(Claim(s, "daily_level_1"), CommandCode.Locked, s, before, "locked");
        }

        [Test]
        public void Claim_UnknownId_Invalid()
        {
            var s = State();
            var before = JsonUtility.ToJson(s);
            AssertRejected(Claim(s, "nope"), CommandCode.Invalid, s, before, "unknown");
            AssertRejected(Claim(s, null), CommandCode.Invalid, s, before, "null");
            AssertRejected(Claim(s, ""), CommandCode.Invalid, s, before, "empty");
        }

        [Test]
        public void Claim_LoginAm_OutsideWindow_NotInWindow()
        {
            var s = State();
            var before = JsonUtility.ToJson(s);
            var afternoon = new DateTime(2026, 11, 21, 8, 0, 0, DateTimeKind.Utc);   // 15:00 VN

            AssertRejected(Claim(s, "daily_login_am", afternoon), CommandCode.NotInWindow, s, before, "am at 15:00");
            Assert.AreEqual(CommandCode.Ok, Claim(s, "daily_login_pm", afternoon).Code);
        }

        [Test]
        public void Claim_LoginAm_AtBoundary_ClaimsUntilNoon()
        {
            var at1159 = new DateTime(2026, 11, 21, 4, 59, 59, DateTimeKind.Utc);
            var at1200 = new DateTime(2026, 11, 21, 5, 0, 0, DateTimeKind.Utc);

            var s = State();
            Assert.AreEqual(CommandCode.Ok, Claim(s, "daily_login_am", at1159).Code, "11:59:59 is still morning");

            var s2 = State();
            var before = JsonUtility.ToJson(s2);
            AssertRejected(Claim(s2, "daily_login_am", at1200), CommandCode.NotInWindow, s2, before, "12:00:00 is not");
            Assert.AreEqual(CommandCode.Ok, Claim(s2, "daily_login_pm", at1200).Code, "12:00:00 opens the pm window");
        }

        [Test]
        public void Claim_Login_AddsStamina_CapsAt999_ReportsWasted()
        {
            var s = State();
            s.Stamina = new StaminaState { Current = 950, AnchorUtcTicks = 0 };

            var r = Claim(s, "daily_login_am");

            Assert.AreEqual(CommandCode.Ok, r.Code);
            Assert.AreEqual(999, s.Stamina.Current);
            var p = (MissionClaimPayload)r.Payload;
            Assert.AreEqual((0L, 49, 51), (p.Gem, p.Stamina, p.StaminaWasted));
        }

        [Test]
        public void Claim_Login_NormalStamina_NoWaste()
        {
            var s = State();
            s.Stamina = new StaminaState { Current = 30, AnchorUtcTicks = Morning.Ticks };

            var p = (MissionClaimPayload)Claim(s, "daily_login_am").Payload;

            Assert.AreEqual((100, 0), (p.Stamina, p.StaminaWasted));
            Assert.AreEqual(130, s.Stamina.Current);
            Assert.AreEqual(0L, s.Stamina.AnchorUtcTicks, "over Max: not regenerating");
        }

        [Test]
        public void Claim_Newbie_ClientFlag_RequiresClientDone()
        {
            var s = State();
            var before = JsonUtility.ToJson(s);

            AssertRejected(Claim(s, "new_link"), CommandCode.NotReady, s, before, "client did not report");
            AssertRejected(Claim(s, "new_link", null, "new_name"), CommandCode.NotReady, s, before, "another id reported");

            Assert.AreEqual(CommandCode.Ok, Claim(s, "new_link", null, "new_link").Code);
            Assert.AreEqual(100, s.Gem);
            var after = JsonUtility.ToJson(s);
            // Hôm sau vẫn "đã nhận": tân thủ chỉ một lần.
            AssertRejected(Claim(s, "new_link", Morning.AddDays(3), "new_link"), CommandCode.AlreadyClaimed, s, after, "newbie is once ever");
        }

        [Test]
        public void Claim_Newbie_FirstWin_FromClearedStages()
        {
            var s = State();
            var before = JsonUtility.ToJson(s);
            AssertRejected(Claim(s, "new_first_win"), CommandCode.NotReady, s, before, "no stage cleared");

            s.ClearedStages.Add("b1");
            Assert.AreEqual(CommandCode.Ok, Claim(s, "new_first_win").Code);
        }

        [Test]
        public void Claim_Newbie_Win10_CountsBattleStagesOnly()
        {
            var s = State();
            for (var i = 1; i <= 9; i++) s.ClearedStages.Add("b" + i);
            s.ClearedStages.Add("st");   // màn cốt truyện: không tính
            var before = JsonUtility.ToJson(s);

            AssertRejected(Claim(s, "new_win_10"), CommandCode.NotReady, s, before, "9 battle + 1 story");

            s.ClearedStages.Add("b10");
            Assert.AreEqual(CommandCode.Ok, Claim(s, "new_win_10").Code);
            Assert.AreEqual(200, s.Gem);
        }

        [Test]
        public void Claim_Newbie_FirstPull_FromHistory()
        {
            var s = State();
            var before = JsonUtility.ToJson(s);
            AssertRejected(Claim(s, "new_first_pull"), CommandCode.NotReady, s, before, "no pull yet");

            s.GachaHistory.Add(new GachaHistoryEntry { BannerId = "b", Count = 1, CharacterIds = new[] { "a" } });
            Assert.AreEqual(CommandCode.Ok, Claim(s, "new_first_pull").Code);
        }

        [Test]
        public void Claim_Newbie_StarUp_FromStarUps()
        {
            var s = State();
            var before = JsonUtility.ToJson(s);
            AssertRejected(Claim(s, "new_star_up"), CommandCode.NotReady, s, before, "no star up");

            s.StarUps.Add(new StarUpEntry { CharacterId = "a", Count = 1 });
            Assert.AreEqual(CommandCode.Ok, Claim(s, "new_star_up").Code);
        }

        [Test]
        public void ClaimAll_Tab_ClaimsEveryReadyRow_Total()
        {
            var s = State();
            s.Missions.DailyWins = 5;
            s.Missions.DailyPulls = 1;
            s.Stamina = new StaminaState { Current = 950, AnchorUtcTicks = 0 };

            var r = ClaimAll(s, 0);

            Assert.AreEqual(CommandCode.Ok, r.Code);
            var p = (MissionClaimPayload)r.Payload;
            Assert.AreEqual(40L, p.Gem, "20 + 20; the pm window is closed in the morning");
            Assert.AreEqual((49, 51), (p.Stamina, p.StaminaWasted));
            Assert.AreEqual(40, s.Gem);
            CollectionAssert.AreEquivalent(new[] { "daily_win_5", "daily_pull_1", "daily_login_am" }, s.Missions.DailyClaimed);
        }

        [Test]
        public void ClaimAll_NothingReady_NothingToClaim()
        {
            var s = State();
            s.Missions.DailyClaimed.Add("daily_login_am");
            var before = JsonUtility.ToJson(s);

            AssertRejected(ClaimAll(s, 0), CommandCode.NothingToClaim, s, before, "nothing ready");
        }

        [Test]
        public void ClaimAll_OtherTab_Untouched()
        {
            var s = State();
            s.Missions.DailyWins = 5;
            s.ClearedStages.Add("b1");

            Assert.AreEqual(CommandCode.Ok, ClaimAll(s, 1).Code);

            Assert.AreEqual(100, s.Gem, "only the newbie row new_first_win");
            CollectionAssert.AreEqual(new[] { "new_first_win" }, s.Missions.NewbieClaimed);
            CollectionAssert.IsEmpty(s.Missions.DailyClaimed, "the daily tab was not claimed");
        }

        [Test]
        public void Claim_HandlerThrowsAfterMutation_RunnerWritesNothing()
        {
            var stored = State();
            stored.ActiveSessionId = "sess";
            stored.Missions.DailyWins = 5;
            var before = JsonUtility.ToJson(stored);
            var request = new ServerRequest { ProtocolVersion = ServerProtocol.Version, SessionId = "sess", RequestId = "r1" };

            Assert.Throws<InvalidOperationException>(() => CommandRunner.Run(stored, request, s =>
            {
                var claimed = ServerHandlers.ClaimMission(s, Data(), Ctx(Morning), "daily_win_5", new List<string>());
                Assert.AreEqual(CommandCode.Ok, claimed.Code);
                throw new InvalidOperationException("boom");
            }, JsonUtility.ToJson));

            Assert.AreEqual(before, JsonUtility.ToJson(stored), "the stored state keeps no claim and no Gem");
        }

        [Test]
        public void View_WindowStates_AndProgressCap()
        {
            var s = State();
            s.Missions.DailyWins = 99;
            var data = Data();

            var rows = MissionRules.View(s, data, Morning, new List<string>());
            var win = rows.Find(r => r.Id == "daily_win_5");
            Assert.AreEqual((5, 5, true), (win.Progress, win.Target, win.Claimable), "progress is capped at the target");
            Assert.AreEqual(0, win.WindowState);

            Assert.AreEqual(1, rows.Find(r => r.Id == "daily_login_am").WindowState, "10:00 is in the am window");
            Assert.AreEqual(2, rows.Find(r => r.Id == "daily_login_pm").WindowState, "pm not yet");
            Assert.IsFalse(rows.Find(r => r.Id == "daily_login_pm").Claimable);
            Assert.IsTrue(rows.Find(r => r.Id == "daily_level_1").Locked);

            var evening = new DateTime(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);   // 19:00 VN
            var late = MissionRules.View(s, data, evening, new List<string>());
            Assert.AreEqual(3, late.Find(r => r.Id == "daily_login_am").WindowState, "am missed");
            Assert.AreEqual(1, late.Find(r => r.Id == "daily_login_pm").WindowState);
        }

        [Test]
        public void View_DoesNotMutateState()
        {
            var s = State();
            s.Missions.DailyDay = 1;
            s.Missions.DailyWins = 3;
            var before = JsonUtility.ToJson(s);

            MissionRules.View(s, Data(), Morning, new List<string>());

            Assert.AreEqual(before, JsonUtility.ToJson(s));
        }
    }
}
