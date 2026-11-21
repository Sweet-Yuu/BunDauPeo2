using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Màn trận s1, s2, s3 và cốt truyện st (thứ tự s1, s2, st, s3). Thể lực 100, hồi 300 giây/điểm, đội tối đa 3.
    public class HandlerStageTests
    {
        const long Regen = 300L * TimeSpan.TicksPerSecond;
        static readonly DateTime T0 = new DateTime(2026, 11, 16, 9, 0, 0, DateTimeKind.Utc);

        int _ids;

        [SetUp]
        public void SetUp() => _ids = 0;

        static StageData Stage(string id, int kind, int cost, long firstGem, long firstMat, long repeatGem, long repeatMat) => new StageData
        {
            Id = id,
            Kind = kind,
            StaminaCost = cost,
            FirstClear = new RewardData { Gem = firstGem, Material = firstMat },
            Repeat = new RewardData { Gem = repeatGem, Material = repeatMat },
        };

        static GameData Data() => new GameData
        {
            Stamina = new StaminaData { Max = 100, RegenSeconds = 300 },
            MaxTeamSize = 3,
            Stages = new List<StageData>
            {
                Stage("s1", 0, 10, 100, 5, 10, 1),
                Stage("s2", 0, 20, 200, 7, 20, 2),
                Stage("st", 1, 0, 0, 0, 0, 0),
                Stage("s3", 0, 30, 300, 9, 30, 3),
            },
            NewGame = new NewGameData { StartingGem = 1500, StarterCharacterIds = new List<string> { "a", "b" } },
        };

        static ServerState State(params string[] cleared)
        {
            var s = ServerState.NewGame(Data());
            s.OwnedCharacterIds = new List<string> { "a", "b", "c", "d" };
            s.ClearedStages.AddRange(cleared);
            return s;
        }

        ServerContext Ctx(DateTime? now = null) => new ServerContext { NowUtc = now ?? T0, NewId = () => "t" + (++_ids) };

        static List<string> Team(params string[] ids) => new List<string>(ids);

        // Mọi field của state, để so "từ chối thì không đổi gì" (Clone phòng handler sửa tại chỗ rồi mới từ chối).
        static string Snap(ServerState s) =>
            $"{s.Revision}|{s.Gem}|{string.Join(",", s.OwnedCharacterIds)}|{s.Fragments.Count}|{s.StarUps.Count}|{s.GachaPity.Count}|{s.GachaHistory.Count}" +
            $"|{s.Stamina.Current}|{s.Stamina.AnchorUtcTicks}|{string.Join(",", s.ClearedStages)}|{s.PendingTicket}|{s.PendingStageId}|{s.ActiveSessionId}";

        static void AssertRejected(HandlerOutcome r, CommandCode code, ServerState original, string snap, string label)
        {
            Assert.AreEqual(code, r.Code, label);
            Assert.IsNull(r.State, label);
            Assert.AreEqual(snap, Snap(original), label + ": the stored state is untouched");
        }

        HandlerOutcome Enter(ServerState s, string stage, params string[] team) => ServerHandlers.EnterStage(s, Data(), Ctx(), stage, Team(team));

        [Test]
        public void Enter_CheckOrder()
        {
            var data = Data();

            // Mỗi ca vi phạm luật của nó và mọi luật đứng sau (thể lực 0): thứ tự đảo thì mã đổi.
            ServerState Drained(params string[] cleared)
            {
                var s = State(cleared);
                s.Stamina = new StaminaState { Current = 0, AnchorUtcTicks = T0.Ticks };
                return s;
            }

            void Case(string label, ServerState s, string stage, List<string> team, CommandCode expected)
            {
                var snap = Snap(s);
                var r = ServerHandlers.EnterStage(s, data, Ctx(), stage, team);
                AssertRejected(r, expected, s, snap, label);
            }

            Case("unknown stage", Drained(), "nope", Team(), CommandCode.Invalid);
            Case("null stage id", Drained(), null, Team(), CommandCode.Invalid);
            Case("story stage, locked, empty team", Drained(), "st", Team(), CommandCode.WrongKind);
            Case("locked battle stage", Drained(), "s2", Team(), CommandCode.Locked);
            Case("empty team", Drained(), "s1", Team(), CommandCode.EmptyTeam);
            Case("null team", Drained(), "s1", null, CommandCode.EmptyTeam);
            Case("over MaxTeamSize, unowned member", Drained(), "s1", Team("a", "b", "c", "zzz"), CommandCode.Invalid);
            Case("duplicate id, unowned member", Drained(), "s1", Team("a", "a", "zzz"), CommandCode.Invalid);
            Case("unowned member", Drained(), "s1", Team("a", "zzz"), CommandCode.NotOwned);
            Case("null member", Drained(), "s1", Team("a", null), CommandCode.NotOwned);
            Case("not enough stamina", Drained(), "s1", Team("a"), CommandCode.NotEnoughStamina);

            // Thiếu thể lực sau khi đã hồi một phần: mốc và số hồi của state gốc vẫn y nguyên.
            var partial = State();
            partial.Stamina = new StaminaState { Current = 5, AnchorUtcTicks = T0.Ticks - 650 * TimeSpan.TicksPerSecond };
            Case("not enough stamina after regen", partial, "s1", Team("a"), CommandCode.NotEnoughStamina);

            // Đối chứng: mở khoá đúng thì qua; chính màn đã qua luôn mở dù màn trước chưa qua.
            Assert.AreEqual(CommandCode.Ok, Enter(State("s1"), "s2", "a").Code, "s2 opens once s1 is cleared");
            Assert.AreEqual(CommandCode.Ok, Enter(State("s2"), "s2", "a").Code, "a cleared stage stays open even if the one before is not cleared");
            Assert.AreEqual(CommandCode.Ok, Enter(State("s1", "s2", "st"), "s3", "a", "b", "c").Code, "three members is the limit, and clearing the story opens s3");
            Assert.AreEqual(CommandCode.Locked, Enter(State("s1", "s2"), "s3", "a").Code, "s3 waits for the story stage");
        }

        [Test]
        public void Enter_TeamNotOwned_Rejected_NoStaminaSpent()
        {
            var s = State();
            s.Stamina = new StaminaState { Current = 50, AnchorUtcTicks = T0.Ticks - 100 * TimeSpan.TicksPerSecond };
            var snap = Snap(s);

            var r = ServerHandlers.EnterStage(s, Data(), Ctx(T0.AddSeconds(1000)), "s1", Team("a", "zzz"));

            AssertRejected(r, CommandCode.NotOwned, s, snap, "unowned member");
            Assert.AreEqual("", s.PendingTicket);

            var ok = ServerHandlers.EnterStage(s, Data(), Ctx(T0.AddSeconds(1000)), "s1", Team("a", "b"));
            Assert.AreEqual(CommandCode.Ok, ok.Code, "the same state accepts a valid team afterwards");
        }

        [Test]
        public void Enter_SpendsStamina_ByServerTime_IssuesTicket()
        {
            // 2,5 chặng đã trôi tới giờ server: +2 điểm, mốc giữ nửa chặng dở.
            var s = State();
            s.Stamina = new StaminaState { Current = 50, AnchorUtcTicks = T0.Ticks - 750 * TimeSpan.TicksPerSecond };

            var r = Enter(s, "s1", "a", "b");

            Assert.AreEqual(CommandCode.Ok, r.Code);
            Assert.AreSame(s, r.State);
            Assert.AreEqual(42, s.Stamina.Current, "50 + 2 regen points - 10");
            Assert.AreEqual(T0.Ticks - 150 * TimeSpan.TicksPerSecond, s.Stamina.AnchorUtcTicks, "half an interval of progress is kept");
            Assert.AreEqual("t1", s.PendingTicket);
            Assert.AreEqual("s1", s.PendingStageId);
            var payload = (EnterStagePayload)r.Payload;
            Assert.AreEqual(("t1", 10), (payload.Ticket, payload.StaminaCost));

            // Từ đầy: mốc hồi bắt đầu đúng giờ server.
            var full = State();
            Assert.AreEqual(CommandCode.Ok, Enter(full, "s1", "a").Code);
            Assert.AreEqual((90, T0.Ticks), (full.Stamina.Current, full.Stamina.AnchorUtcTicks));

            // Cùng state, giờ server khác thì kết quả khác: thể lực đo theo ctx, không theo máy.
            var later = State();
            later.Stamina = new StaminaState { Current = 50, AnchorUtcTicks = T0.Ticks };
            ServerHandlers.EnterStage(later, Data(), Ctx(T0.AddSeconds(3 * 300)), "s1", Team("a"));
            Assert.AreEqual(43, later.Stamina.Current, "50 + 3 - 10");
        }

        [Test]
        public void Enter_Again_ReplacesTicket()
        {
            var data = Data();
            var s = State("s1");
            var ctx = Ctx();

            var first = ServerHandlers.EnterStage(s, data, ctx, "s1", Team("a"));
            var oldTicket = ((EnterStagePayload)first.Payload).Ticket;
            var second = ServerHandlers.EnterStage(first.State, data, ctx, "s2", Team("a"));

            Assert.AreEqual(CommandCode.Ok, second.Code);
            var newTicket = ((EnterStagePayload)second.Payload).Ticket;
            Assert.AreNotEqual(oldTicket, newTicket);
            Assert.AreEqual((newTicket, "s2"), (s.PendingTicket, s.PendingStageId));
            Assert.AreEqual(100 - 10 - 20, s.Stamina.Current, "both entries paid");

            var snap = Snap(s);
            var stale = ServerHandlers.CompleteStage(s, data, oldTicket, true);
            AssertRejected(stale, CommandCode.TicketMismatch, s, snap, "the replaced ticket");
            Assert.AreEqual(CommandCode.Ok, ServerHandlers.CompleteStage(s, data, newTicket, true).Code);
        }

        [Test]
        public void Complete_WrongOrOldTicket_Mismatch()
        {
            var data = Data();

            // Không có ticket đang chờ: chuỗi rỗng không được khớp chuỗi rỗng.
            var none = State();
            foreach (var ticket in new[] { "", null, "t1" })
            {
                var snap = Snap(none);
                AssertRejected(ServerHandlers.CompleteStage(none, data, ticket, true), CommandCode.TicketMismatch, none, snap, $"no pending, ticket '{ticket}'");
            }

            var pending = State();
            pending.PendingTicket = "t5";
            pending.PendingStageId = "s1";
            foreach (var ticket in new[] { "", null, "t4", "T5", "t5 " })
            {
                var snap = Snap(pending);
                AssertRejected(ServerHandlers.CompleteStage(pending, data, ticket, true), CommandCode.TicketMismatch, pending, snap, $"ticket '{ticket}'");
                AssertRejected(ServerHandlers.CompleteStage(pending, data, ticket, false), CommandCode.TicketMismatch, pending, snap, $"lost, ticket '{ticket}'");
            }

            // Ticket đúng nhưng màn đã biến khỏi dữ liệu: từ chối, không ghi gì.
            pending.PendingStageId = "gone";
            var goneSnap = Snap(pending);
            AssertRejected(ServerHandlers.CompleteStage(pending, data, "t5", true), CommandCode.Invalid, pending, goneSnap, "stage missing from data");
        }

        [Test]
        public void Complete_Won_FirstThenRepeat()
        {
            var data = Data();
            var s = State();
            s.Gem = 1000;
            var ctx = Ctx();

            var enter = ServerHandlers.EnterStage(s, data, ctx, "s1", Team("a"));
            var first = ServerHandlers.CompleteStage(s, data, ((EnterStagePayload)enter.Payload).Ticket, true);

            Assert.AreEqual(CommandCode.Ok, first.Code);
            var p1 = (StageRewardPayload)first.Payload;
            Assert.AreEqual((true, 100L, 5L), (p1.FirstClear, p1.Gem, p1.Material));
            Assert.AreEqual(1100, s.Gem);
            CollectionAssert.AreEqual(new[] { "s1" }, s.ClearedStages);
            Assert.AreEqual(("", ""), (s.PendingTicket, s.PendingStageId), "the ticket is spent");

            var again = ServerHandlers.EnterStage(s, data, ctx, "s1", Team("a"));
            var repeat = ServerHandlers.CompleteStage(s, data, ((EnterStagePayload)again.Payload).Ticket, true);

            var p2 = (StageRewardPayload)repeat.Payload;
            Assert.AreEqual((false, 10L, 1L), (p2.FirstClear, p2.Gem, p2.Material));
            Assert.AreEqual(1110, s.Gem);
            CollectionAssert.AreEqual(new[] { "s1" }, s.ClearedStages, "cleared once, not twice");

            // Màn khác có số khác: thưởng lấy đúng màn đã vào, không phải màn đầu danh sách.
            var s2 = ServerHandlers.EnterStage(s, data, ctx, "s2", Team("a"));
            var p3 = (StageRewardPayload)ServerHandlers.CompleteStage(s, data, ((EnterStagePayload)s2.Payload).Ticket, true).Payload;
            Assert.AreEqual((true, 200L, 7L), (p3.FirstClear, p3.Gem, p3.Material));
            Assert.AreEqual(1310, s.Gem);
        }

        [Test]
        public void Complete_Lost_ClearsTicket_NoReward()
        {
            var data = Data();
            var s = State();
            s.Gem = 1000;
            var enter = ServerHandlers.EnterStage(s, data, Ctx(), "s1", Team("a"));
            var stamina = (s.Stamina.Current, s.Stamina.AnchorUtcTicks);

            var lost = ServerHandlers.CompleteStage(s, data, ((EnterStagePayload)enter.Payload).Ticket, false);

            Assert.AreEqual(CommandCode.Ok, lost.Code);
            var p = (StageRewardPayload)lost.Payload;
            Assert.AreEqual((false, 0L, 0L), (p.FirstClear, p.Gem, p.Material));
            Assert.AreEqual(1000, s.Gem);
            CollectionAssert.IsEmpty(s.ClearedStages);
            Assert.AreEqual(("", ""), (s.PendingTicket, s.PendingStageId));
            Assert.AreEqual(stamina, (s.Stamina.Current, s.Stamina.AnchorUtcTicks), "the entry cost is not refunded");
        }

        [Test]
        public void Complete_SameTicketTwice_SecondMismatch()
        {
            var data = Data();
            var s = State();
            s.Gem = 1000;
            var ticket = ((EnterStagePayload)ServerHandlers.EnterStage(s, data, Ctx(), "s1", Team("a")).Payload).Ticket;
            Assert.AreEqual(CommandCode.Ok, ServerHandlers.CompleteStage(s, data, ticket, true).Code);
            var snap = Snap(s);

            var second = ServerHandlers.CompleteStage(s, data, ticket, true);

            AssertRejected(second, CommandCode.TicketMismatch, s, snap, "the same ticket again");
            Assert.AreEqual(1100, s.Gem, "paid once");
        }

        [Test]
        public void Story_LockedOrCleared_Rejected()
        {
            var data = Data();

            void Case(string label, ServerState s, string stage, CommandCode expected)
            {
                var snap = Snap(s);
                AssertRejected(ServerHandlers.CompleteStory(s, data, stage), expected, s, snap, label);
            }

            Case("unknown stage", State("s2"), "nope", CommandCode.Invalid);
            Case("battle stage", State("s1", "s2"), "s2", CommandCode.WrongKind);
            Case("locked: s2 not cleared", State("s1"), "st", CommandCode.Locked);
            Case("locked: nothing cleared", State(), "st", CommandCode.Locked);
            Case("already cleared", State("s1", "s2", "st"), "st", CommandCode.AlreadyCleared);
        }

        [Test]
        public void Story_MarksCleared_NoReward()
        {
            var data = Data();
            var s = State("s1", "s2");
            var before = (s.Gem, s.Stamina.Current, s.Stamina.AnchorUtcTicks);
            Assert.AreEqual(CommandCode.Locked, Enter(s, "s3", "a").Code, "setup: s3 is locked behind the story");

            var r = ServerHandlers.CompleteStory(s, data, "st");

            Assert.AreEqual(CommandCode.Ok, r.Code);
            Assert.IsNull(r.Payload);
            CollectionAssert.AreEqual(new[] { "s1", "s2", "st" }, s.ClearedStages);
            Assert.AreEqual(before, (s.Gem, s.Stamina.Current, s.Stamina.AnchorUtcTicks), "no reward, no stamina");
            Assert.AreEqual(CommandCode.Ok, Enter(s, "s3", "a").Code, "clearing the story opens the next stage");
        }

        [Test]
        public void ResetState_NewGamePlusSession()
        {
            var data = Data();

            var s = ServerHandlers.ResetState(data, "keep-me");

            Assert.AreEqual("keep-me", s.ActiveSessionId);
            Assert.AreEqual(1500, s.Gem);
            CollectionAssert.AreEqual(new[] { "a", "b" }, s.OwnedCharacterIds);
            Assert.AreEqual((100, 0L), (s.Stamina.Current, s.Stamina.AnchorUtcTicks), "full stamina");
            Assert.AreEqual(0, s.Revision);
            CollectionAssert.IsEmpty(s.ClearedStages);
            Assert.AreEqual(("", "", "", ""), (s.PendingTicket, s.PendingStageId, s.LastRequestId, s.LastResultJson));

            s.OwnedCharacterIds.Add("x");
            var again = ServerHandlers.ResetState(data, "other");
            CollectionAssert.AreEqual(new[] { "a", "b" }, again.OwnedCharacterIds, "each reset is a fresh state");
            CollectionAssert.AreEqual(new[] { "a", "b" }, data.NewGame.StarterCharacterIds, "the data is not shared with the state");
        }
    }
}
