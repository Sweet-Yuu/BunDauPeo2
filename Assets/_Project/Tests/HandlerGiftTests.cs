using System;
using System.Collections.Generic;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class HandlerGiftTests
    {
        static readonly DateTime Start = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        static readonly DateTime End = Start.AddDays(14);
        static readonly DateTime Now = Start.AddDays(1);

        static GiftData Gift(string id, params (string type, long amount)[] rewards)
        {
            var g = new GiftData { Id = id, TitleVi = "Quà", TitleEn = "Gift", StartUtcTicks = Start.Ticks, EndUtcTicks = End.Ticks };
            foreach (var r in rewards) g.Rewards.Add(new GiftReward { Type = r.type, Amount = r.amount });
            return g;
        }

        static List<GiftData> Gifts() => new List<GiftData>
        {
            Gift("a", ("Gem", 300), ("Coin", 5000)),
            Gift("b", ("Gem", 100), ("Material", 7)),
        };

        static ServerContext Ctx(DateTime? now = null) => new ServerContext { NowUtc = now ?? Now, NewId = () => "x" };

        static void AssertRejected(HandlerOutcome r, CommandCode code, ServerState s, string before, string label)
        {
            Assert.AreEqual(code, r.Code, label);
            Assert.IsNull(r.State, label);
            Assert.AreEqual(before, JsonUtility.ToJson(s), label + ": the state is untouched");
        }

        [Test]
        public void List_DoesNotChangeState()
        {
            var s = new ServerState { Gem = 5 };
            var before = JsonUtility.ToJson(s);
            var r = ServerHandlers.ListGifts(s, Gifts(), Ctx(), "vi");
            Assert.AreEqual(CommandCode.Ok, r.Code);
            Assert.AreEqual(before, JsonUtility.ToJson(r.State));
            Assert.AreEqual(2, ((GiftListPayload)r.Payload).Gifts.Count);
        }

        [Test]
        public void Claim_AddsGemAndRecordsId()
        {
            var s = new ServerState { Gem = 10 };
            var r = ServerHandlers.ClaimGift(s, Gifts(), Ctx(), "a");
            Assert.AreEqual(CommandCode.Ok, r.Code);
            Assert.AreEqual(310, r.State.Gem);
            CollectionAssert.AreEqual(new[] { "a" }, r.State.ClaimedGiftIds);
        }

        [Test]
        public void Claim_CoinAndMaterialInPayloadOnly()
        {
            var r = ServerHandlers.ClaimGift(new ServerState(), Gifts(), Ctx(), "a");
            var p = (GiftClaimPayload)r.Payload;
            Assert.AreEqual(300, p.Gem);
            Assert.AreEqual(5000, p.Coin);
            Assert.AreEqual(0, p.Material);
            Assert.AreEqual(300, r.State.Gem, "state chỉ tăng phần Gem");
        }

        [Test]
        public void Claim_Twice_AlreadyClaimed_StateUnchanged()
        {
            var s = new ServerState { ClaimedGiftIds = { "a" }, Gem = 1 };
            var before = JsonUtility.ToJson(s);
            AssertRejected(ServerHandlers.ClaimGift(s, Gifts(), Ctx(), "a"), CommandCode.AlreadyClaimed, s, before, "again");
        }

        [Test]
        public void Claim_UnknownOrEmptyId_Invalid()
        {
            var s = new ServerState();
            var before = JsonUtility.ToJson(s);
            AssertRejected(ServerHandlers.ClaimGift(s, Gifts(), Ctx(), "zzz"), CommandCode.Invalid, s, before, "unknown");
            AssertRejected(ServerHandlers.ClaimGift(s, Gifts(), Ctx(), ""), CommandCode.Invalid, s, before, "empty");
            AssertRejected(ServerHandlers.ClaimGift(s, Gifts(), Ctx(), null), CommandCode.Invalid, s, before, "null");
        }

        [Test]
        public void Claim_OutOfWindow_NotInWindow()
        {
            var s = new ServerState();
            var before = JsonUtility.ToJson(s);
            AssertRejected(ServerHandlers.ClaimGift(s, Gifts(), Ctx(Start.AddTicks(-1)), "a"), CommandCode.NotInWindow, s, before, "before start");
            AssertRejected(ServerHandlers.ClaimGift(s, Gifts(), Ctx(End), "a"), CommandCode.NotInWindow, s, before, "at end");
            Assert.AreEqual(CommandCode.Ok, ServerHandlers.ClaimGift(s, Gifts(), Ctx(Start), "a").Code, "start is inside");
        }

        [Test]
        public void Claim_InvalidGift_Invalid()
        {
            var gifts = Gifts();
            gifts.Add(Gift("bad", ("Gem", 0)));
            var s = new ServerState();
            var before = JsonUtility.ToJson(s);
            AssertRejected(ServerHandlers.ClaimGift(s, gifts, Ctx(), "bad"), CommandCode.Invalid, s, before, "invalid gift is treated as missing");
        }

        [Test]
        public void ClaimAll_SumsOnlyPendingInWindow()
        {
            var gifts = Gifts();
            var late = Gift("late", ("Gem", 999)); late.StartUtcTicks = End.Ticks; late.EndUtcTicks = End.Ticks + 10;
            gifts.Add(late);
            var r = ServerHandlers.ClaimAllGifts(new ServerState { Gem = 1 }, gifts, Ctx());
            var p = (GiftClaimPayload)r.Payload;
            Assert.AreEqual(400, p.Gem);
            Assert.AreEqual(5000, p.Coin);
            Assert.AreEqual(7, p.Material);
            Assert.AreEqual(401, r.State.Gem);
            CollectionAssert.AreEqual(new[] { "a", "b" }, r.State.ClaimedGiftIds);
        }

        [Test]
        public void ClaimAll_Nothing_NothingToClaim()
        {
            var s = new ServerState();
            var before = JsonUtility.ToJson(s);
            AssertRejected(ServerHandlers.ClaimAllGifts(s, new List<GiftData>(), Ctx()), CommandCode.NothingToClaim, s, before, "empty list");
            AssertRejected(ServerHandlers.ClaimAllGifts(s, Gifts(), Ctx(End)), CommandCode.NothingToClaim, s, before, "all expired");
        }

        [Test]
        public void ClaimAll_AfterClaim_OnlyRemainder()
        {
            var first = ServerHandlers.ClaimGift(new ServerState(), Gifts(), Ctx(), "a");
            var r = ServerHandlers.ClaimAllGifts(first.State, Gifts(), Ctx());
            var p = (GiftClaimPayload)r.Payload;
            Assert.AreEqual(100, p.Gem);
            Assert.AreEqual(0, p.Coin);
            Assert.AreEqual(7, p.Material);
            CollectionAssert.AreEqual(new[] { "a", "b" }, r.State.ClaimedGiftIds);
        }

        [Test]
        public void ClaimAll_AfterClaimAll_NothingToClaim()
        {
            var first = ServerHandlers.ClaimAllGifts(new ServerState(), Gifts(), Ctx());
            Assert.AreEqual(CommandCode.NothingToClaim, ServerHandlers.ClaimAllGifts(first.State, Gifts(), Ctx()).Code);
        }

        [Test]
        public void Claim_RejectLeavesStateUntouched()
        {
            var s = new ServerState { Gem = 3, ClaimedGiftIds = { "b" }, OwnedCharacterIds = { "c" } };
            var before = JsonUtility.ToJson(s);
            ServerHandlers.ClaimGift(s, Gifts(), Ctx(), "b");
            ServerHandlers.ClaimGift(s, Gifts(), Ctx(), "nope");
            ServerHandlers.ClaimGift(s, Gifts(), Ctx(End), "a");
            Assert.AreEqual(before, JsonUtility.ToJson(s));
        }

        [Test]
        public void Reset_ClearsClaimedGiftIds_AndGiftClaimableAgain()
        {
            var claimed = ServerHandlers.ClaimGift(new ServerState(), Gifts(), Ctx(), "a").State;
            Assert.AreEqual(1, claimed.ClaimedGiftIds.Count);
            var fresh = ServerState.NewGame(new GameData { Stamina = new StaminaData(), NewGame = new NewGameData() });
            Assert.AreEqual(0, fresh.ClaimedGiftIds.Count);
            Assert.AreEqual(CommandCode.Ok, ServerHandlers.ClaimGift(fresh, Gifts(), Ctx(), "a").Code);
        }

        [Test]
        public void Claim_RewardOverflow_RejectsOrClamps()
        {
            var s = new ServerState { Gem = long.MaxValue - 100 };
            var before = JsonUtility.ToJson(s);
            AssertRejected(ServerHandlers.ClaimGift(s, Gifts(), Ctx(), "a"), CommandCode.Invalid, s, before, "Gem would overflow");
        }

        // L-003: handler ném lỗi giữa chừng thì CommandRunner không ghi gì (handler chạy trên bản sao).
        [Test]
        public void Claim_HandlerThrows_StoredStateUntouched()
        {
            var stored = new ServerState { ActiveSessionId = "s", Gem = 5 };
            var before = JsonUtility.ToJson(stored);
            var req = new ServerRequest { ProtocolVersion = ServerProtocol.Version, SessionId = "s", RequestId = "r1" };
            Assert.Throws<InvalidOperationException>(() => CommandRunner.Run(stored, req, st =>
            {
                ServerHandlers.ClaimGift(st, Gifts(), Ctx(), "a");
                throw new InvalidOperationException("boom");
            }, o => JsonUtility.ToJson(o)));
            Assert.AreEqual(before, JsonUtility.ToJson(stored));
        }
    }
}
