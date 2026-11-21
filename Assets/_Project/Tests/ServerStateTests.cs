using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class ServerStateTests
    {
        static ServerState Filled() => new ServerState
        {
            Version = 1,
            Revision = 7,
            Gem = 123,
            OwnedCharacterIds = new List<string> { "a", "b" },
            Fragments = new List<FragmentCount> { new FragmentCount { CharacterId = "a", Count = 5 } },
            StarUps = new List<StarUpEntry> { new StarUpEntry { CharacterId = "a", Count = 2 } },
            GachaPity = new List<PityEntry> { new PityEntry { BannerId = "x", Pulls = 9 } },
            GachaHistory = new List<GachaHistoryEntry>
            {
                new GachaHistoryEntry { Utc = 1, BannerId = "x", Count = 2, CharacterIds = new[] { "a", "b" } },
            },
            Stamina = new StaminaState { Current = 40, AnchorUtcTicks = 99 },
            ClearedStages = new List<string> { "s1" },
            Missions = new MissionState
            {
                DailyDay = 740_000, DailyWins = 3, DailyPulls = 2, DailyClaimed = { "d1" },
                FreePullDay = 740_000, NewbieClaimed = { "n1" },
            },
            PendingTicket = "t",
            PendingStageId = "s2",
            ActiveSessionId = "sess",
            LastRequestId = "r",
            LastResultJson = "{}",
        };

        [Test]
        public void Defaults_MatchSpec()
        {
            var s = new ServerState();
            Assert.AreEqual(1, s.Version);
            Assert.AreEqual(0, s.Revision);
            Assert.AreEqual(0, s.Gem);
            Assert.AreEqual("", s.PendingTicket);
            Assert.AreEqual("", s.PendingStageId);
            Assert.AreEqual("", s.ActiveSessionId);
            Assert.AreEqual("", s.LastRequestId);
            Assert.AreEqual("", s.LastResultJson);
            Assert.IsNotNull(s.Stamina);
            // Mọi field List phải khác null, rỗng (thêm field List mới thì ca này tự bắt).
            foreach (var f in typeof(ServerState).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!f.FieldType.IsGenericType || f.FieldType.GetGenericTypeDefinition() != typeof(List<>)) continue;
                var list = (IList)f.GetValue(s);
                Assert.IsNotNull(list, f.Name);
                Assert.AreEqual(0, list.Count, f.Name);
            }
        }

        [Test]
        public void Defaults_HaveEmptyMissions()
        {
            var m = new ServerState().Missions;
            Assert.IsNotNull(m);
            Assert.AreEqual(0, m.DailyDay);
            Assert.AreEqual(0, m.FreePullDay);
            Assert.AreEqual(0, m.DailyClaimed.Count);
            Assert.AreEqual(0, m.NewbieClaimed.Count);
        }

        [Test]
        public void JsonUtility_RoundTrip_KeepsMissionDays()
        {
            var orig = Filled();
            var back = JsonUtility.FromJson<ServerState>(JsonUtility.ToJson(orig));
            Assert.AreEqual(740_000, back.Missions.DailyDay);
            Assert.AreEqual(740_000, back.Missions.FreePullDay);
            CollectionAssert.AreEqual(new[] { "d1" }, back.Missions.DailyClaimed);
            CollectionAssert.AreEqual(new[] { "n1" }, back.Missions.NewbieClaimed);
        }

        [Test]
        public void Clone_IsDeep_ForEveryListField()
        {
            var orig = Filled();
            var clone = orig.Clone();
            Assert.AreEqual(JsonUtility.ToJson(orig), JsonUtility.ToJson(clone));

            var checkedFields = 0;
            foreach (var f in typeof(ServerState).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var isList = f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(List<>);
                if (!isList && f.FieldType != typeof(StaminaState)) continue;
                Assert.IsFalse(ReferenceEquals(f.GetValue(orig), f.GetValue(clone)), f.Name + " dùng chung tham chiếu");
                checkedFields++;
            }
            Assert.AreEqual(7, checkedFields, "đổi số field List/StaminaState thì cập nhật Clone và số này");
            Assert.IsFalse(ReferenceEquals(orig.Missions, clone.Missions));
            Assert.IsFalse(ReferenceEquals(orig.Missions.DailyClaimed, clone.Missions.DailyClaimed));
            Assert.IsFalse(ReferenceEquals(orig.Missions.NewbieClaimed, clone.Missions.NewbieClaimed));

            // Mảng trong mục lịch sử cũng phải là bản riêng.
            clone.GachaHistory[0].CharacterIds[0] = "changed";
            Assert.AreEqual("a", orig.GachaHistory[0].CharacterIds[0]);
        }

        [Test]
        public void JsonUtility_RoundTrip_KeepsLongAndLists()
        {
            var orig = Filled();
            orig.Gem = 9_000_000_000;
            orig.Stamina.AnchorUtcTicks = 638_700_000_000_000_000;
            var back = JsonUtility.FromJson<ServerState>(JsonUtility.ToJson(orig));
            Assert.AreEqual(9_000_000_000, back.Gem);
            Assert.AreEqual(638_700_000_000_000_000, back.Stamina.AnchorUtcTicks);
            CollectionAssert.AreEqual(orig.OwnedCharacterIds, back.OwnedCharacterIds);
            Assert.AreEqual(JsonUtility.ToJson(orig), JsonUtility.ToJson(back));
        }

        // Chuỗi này cũng nằm ở ServerModule/BunDauPeoServer.Tests (ServerStateJsonSample): đổi một nơi phải đổi cả hai.
        [Test]
        public void Json_Shape_MatchesNewtonsoftSample()
        {
            var state = new ServerState
            {
                Revision = 2,
                Gem = 9_000_000_000,
                OwnedCharacterIds = { "a" },
                Fragments = { new FragmentCount { CharacterId = "a", Count = 3 } },
                StarUps = { new StarUpEntry { CharacterId = "a", Count = 1 } },
                GachaPity = { new PityEntry { BannerId = "b", Pulls = 4 } },
                GachaHistory = { new GachaHistoryEntry { Utc = 5, BannerId = "b", Count = 1, CharacterIds = new[] { "a" } } },
                Stamina = new StaminaState { Current = 40, AnchorUtcTicks = 638_700_000_000_000_123 },
                ClearedStages = { "s1" },
                Missions = { DailyDay = 740_000, DailyWins = 2, DailyPulls = 1, DailyClaimed = { "d" }, FreePullDay = 739_999, NewbieClaimed = { "n" } },
                PendingTicket = "t",
                PendingStageId = "s2",
                ActiveSessionId = "sess",
                LastRequestId = "r",
                LastResultJson = "{}",
            };
            const string expected =
                "{\"Version\":1,\"Revision\":2,\"Gem\":9000000000,\"OwnedCharacterIds\":[\"a\"],"
                + "\"Fragments\":[{\"CharacterId\":\"a\",\"Count\":3}],\"StarUps\":[{\"CharacterId\":\"a\",\"Count\":1}],"
                + "\"GachaPity\":[{\"BannerId\":\"b\",\"Pulls\":4}],"
                + "\"GachaHistory\":[{\"Utc\":5,\"BannerId\":\"b\",\"Count\":1,\"CharacterIds\":[\"a\"]}],"
                + "\"Stamina\":{\"Current\":40,\"AnchorUtcTicks\":638700000000000123},\"ClearedStages\":[\"s1\"],"
                + "\"Missions\":{\"DailyDay\":740000,\"DailyWins\":2,\"DailyPulls\":1,\"DailyClaimed\":[\"d\"],\"FreePullDay\":739999,\"NewbieClaimed\":[\"n\"]},"
                + "\"PendingTicket\":\"t\",\"PendingStageId\":\"s2\",\"ActiveSessionId\":\"sess\",\"LastRequestId\":\"r\",\"LastResultJson\":\"{}\"}";
            Assert.AreEqual(expected, JsonUtility.ToJson(state));
        }

        [Test]
        public void NewGame_FromData()
        {
            var data = new GameData
            {
                Stamina = new StaminaData { Max = 100, RegenSeconds = 300 },
                NewGame = new NewGameData { StartingGem = 1500, StarterCharacterIds = new List<string> { "s1", "s2" } },
            };
            var s = ServerState.NewGame(data);
            Assert.AreEqual(1500, s.Gem);
            CollectionAssert.AreEqual(new[] { "s1", "s2" }, s.OwnedCharacterIds);
            Assert.AreEqual(100, s.Stamina.Current);
            Assert.AreEqual(0, s.Stamina.AnchorUtcTicks);
            Assert.AreEqual(0, s.Revision);
            // Danh sách starter của state phải là bản riêng, không dùng chung với GameData.
            s.OwnedCharacterIds.Add("x");
            Assert.AreEqual(2, data.NewGame.StarterCharacterIds.Count);
        }
    }
}
