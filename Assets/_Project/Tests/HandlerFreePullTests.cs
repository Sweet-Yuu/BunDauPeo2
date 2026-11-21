using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace BunDauPeo.Tests
{
    public class HandlerFreePullTests
    {
        static readonly DateTime Morning = new DateTime(2026, 11, 21, 3, 0, 0, DateTimeKind.Utc);

        GameData _data;

        [SetUp]
        public void SetUp()
        {
            _data = GachaRulesTests.Data();
            _data.Banners[0].FreePullDaily = true;
        }

        static ServerContext Ctx(DateTime now, Random random = null) =>
            new ServerContext { NowUtc = now, Random = random ?? new ScriptedRandom(), NewId = () => "x" };

        static ServerState State(long gem = 2000) => new ServerState { Gem = gem, Stamina = new StaminaState { Current = 100 } };

        [Test]
        public void FreePull_Ok_NoGemSpent_PityAndHistoryUpdated()
        {
            var s = State();

            var r = ServerHandlers.GachaFreePull(s, _data, Ctx(Morning), "b");

            Assert.AreEqual(CommandCode.Ok, r.Code);
            var pulls = ((GachaPullPayload)r.Payload).Pulls;
            Assert.AreEqual(1, pulls.Count);
            Assert.AreEqual("r0", pulls[0].CharacterId);
            Assert.AreEqual(2000, s.Gem, "free");
            Assert.AreEqual(1, s.GachaPity.Find(p => p.BannerId == "b").Pulls);
            Assert.AreEqual(1, s.GachaHistory.Count);
            Assert.AreEqual(MissionRules.DayIndex(Morning), s.Missions.FreePullDay);
        }

        [Test]
        public void FreePull_Twice_SameDay_FreePullUsed()
        {
            var s = State();
            Assert.AreEqual(CommandCode.Ok, ServerHandlers.GachaFreePull(s, _data, Ctx(Morning), "b").Code);
            var before = JsonUtility.ToJson(s);

            var again = ServerHandlers.GachaFreePull(s, _data, Ctx(Morning.AddHours(10)), "b");   // 20:00 VN, cùng ngày

            Assert.AreEqual(CommandCode.FreePullUsed, again.Code);
            Assert.IsNull(again.State);
            Assert.AreEqual(before, JsonUtility.ToJson(s));
        }

        [Test]
        public void FreePull_NextDay_Again()
        {
            var s = State();
            ServerHandlers.GachaFreePull(s, _data, Ctx(Morning), "b");

            var r = ServerHandlers.GachaFreePull(s, _data, Ctx(Morning.AddDays(1)), "b");

            Assert.AreEqual(CommandCode.Ok, r.Code);
            Assert.AreEqual(MissionRules.DayIndex(Morning) + 1, s.Missions.FreePullDay);
        }

        [Test]
        public void FreePull_JustBeforeAndAfterMidnightVn()
        {
            var s = State();
            var last = new DateTime(2026, 11, 21, 16, 59, 59, DateTimeKind.Utc);   // 23:59:59 VN
            var first = new DateTime(2026, 11, 21, 17, 0, 0, DateTimeKind.Utc);    // 00:00:00 VN ngày sau

            Assert.AreEqual(CommandCode.Ok, ServerHandlers.GachaFreePull(s, _data, Ctx(last), "b").Code);
            Assert.AreEqual(CommandCode.FreePullUsed, ServerHandlers.GachaFreePull(s, _data, Ctx(last), "b").Code);
            Assert.AreEqual(CommandCode.Ok, ServerHandlers.GachaFreePull(s, _data, Ctx(first), "b").Code);
        }

        [Test]
        public void FreePull_BannerWithoutFlag_Invalid()
        {
            _data.Banners[0].FreePullDaily = false;
            var s = State();
            var before = JsonUtility.ToJson(s);

            Assert.AreEqual(CommandCode.Invalid, ServerHandlers.GachaFreePull(s, _data, Ctx(Morning), "b").Code);
            Assert.AreEqual(CommandCode.Invalid, ServerHandlers.GachaFreePull(s, _data, Ctx(Morning), "nope").Code);
            Assert.AreEqual(CommandCode.Invalid, ServerHandlers.GachaFreePull(s, _data, Ctx(Morning), null).Code);
            Assert.AreEqual(before, JsonUtility.ToJson(s));
        }

        [Test]
        public void FreePull_CountsDailyPulls()
        {
            var s = State();
            s.Missions.DailyDay = 1;
            s.Missions.DailyPulls = 4;

            ServerHandlers.GachaFreePull(s, _data, Ctx(Morning), "b");

            Assert.AreEqual(1, s.Missions.DailyPulls, "a new day: reset, then the free pull counts as 1");
            Assert.AreEqual(MissionRules.DayIndex(Morning), s.Missions.DailyDay);
        }

        [Test]
        public void FreePull_SameRolls_AsPaidSingle()
        {
            var free = State();
            var paid = State();
            // Cùng chuỗi random: cả hai phải ra cùng nhân vật và cùng pity (cùng hàm roll).
            var f = (GachaPullPayload)ServerHandlers.GachaFreePull(free, _data, Ctx(Morning, new Random(4242)), "b").Payload;
            var p = (GachaPullPayload)ServerHandlers.GachaPull(paid, _data, Ctx(Morning, new Random(4242)), "b", 1).Payload;

            Assert.AreEqual(p.Pulls[0].CharacterId, f.Pulls[0].CharacterId);
            Assert.AreEqual(paid.GachaPity[0].Pulls, free.GachaPity[0].Pulls);
            Assert.AreEqual(1900, paid.Gem);
            Assert.AreEqual(2000, free.Gem);
        }

        [Test]
        public void FreePull_RngThrows_NoStateChange()
        {
            var stored = State();
            stored.ActiveSessionId = "sess";
            var before = JsonUtility.ToJson(stored);
            var request = new ServerRequest { ProtocolVersion = ServerProtocol.Version, SessionId = "sess", RequestId = "r" };

            Assert.Throws<InvalidOperationException>(() => CommandRunner.Run(stored, request,
                s => ServerHandlers.GachaFreePull(s, _data, Ctx(Morning, new ThrowingRandom(1)), "b"), JsonUtility.ToJson));

            Assert.AreEqual(before, JsonUtility.ToJson(stored));
        }

        [Test]
        public void DevResetMissions_ClearsAll()
        {
            var s = State();
            s.Missions = new MissionState { DailyDay = 5, DailyWins = 3, DailyClaimed = { "a" }, FreePullDay = 5, NewbieClaimed = { "n" } };

            var r = ServerHandlers.DevResetMissions(s);

            Assert.AreEqual(CommandCode.Ok, r.Code);
            Assert.AreEqual(0, s.Missions.DailyDay);
            Assert.AreEqual(0, s.Missions.FreePullDay);
            CollectionAssert.IsEmpty(s.Missions.DailyClaimed);
            CollectionAssert.IsEmpty(s.Missions.NewbieClaimed);
        }

        [Test]
        public void DevAddStamina_CapsAt999()
        {
            var s = State();
            s.Stamina = new StaminaState { Current = 950, AnchorUtcTicks = 0 };
            Assert.AreEqual(CommandCode.Ok, ServerHandlers.DevAddStamina(s, _data, Ctx(Morning), 100).Code);
            Assert.AreEqual(999, s.Stamina.Current);

            var before = JsonUtility.ToJson(s);
            Assert.AreEqual(CommandCode.Invalid, ServerHandlers.DevAddStamina(s, _data, Ctx(Morning), 0).Code);
            Assert.AreEqual(CommandCode.Invalid, ServerHandlers.DevAddStamina(s, _data, Ctx(Morning), 1001).Code);
            Assert.AreEqual(before, JsonUtility.ToJson(s));
        }
    }
}
