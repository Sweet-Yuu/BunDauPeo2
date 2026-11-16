using System;
using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace BunDauPeo.Tests
{
    public class HandlerGachaTests
    {
        GameData _data;
        int _ids;

        [SetUp]
        public void SetUp()
        {
            _data = GachaRulesTests.Data();
            _ids = 0;
        }

        ServerContext Ctx(Random random) => new ServerContext
        {
            NowUtc = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc),
            Random = random,
            NewId = () => "id" + (++_ids),
        };

        static ServerState State(long gem = 2000) => new ServerState { Gem = gem };

        static string Json(ServerState s) => JsonUtility.ToJson(s);

        static long FragmentsOf(ServerState s, string id) => s.Fragments.Where(f => f.CharacterId == id).Sum(f => f.Count);

        static int PityOf(ServerState s, string bannerId) => s.GachaPity.Single(p => p.BannerId == bannerId).Pulls;

        static GachaPullPayload Payload(HandlerOutcome outcome)
        {
            Assert.AreEqual(CommandCode.Ok, outcome.Code);
            return (GachaPullPayload)outcome.Payload;
        }

        static void AssertRejected(HandlerOutcome outcome, CommandCode code, string message)
        {
            Assert.AreEqual(code, outcome.Code, message);
            Assert.IsNull(outcome.State, message);
        }

        [Test]
        public void Pull_Single_SpendsGem_GrantsNewCharacter()
        {
            var s = State();
            var ctx = Ctx(new ScriptedRandom());

            var outcome = ServerHandlers.GachaPull(s, _data, ctx, "b", 1);

            var pulls = Payload(outcome).Pulls;
            Assert.AreEqual(1, pulls.Count);
            Assert.AreEqual("r0", pulls[0].CharacterId);
            Assert.IsTrue(pulls[0].IsNew);
            Assert.AreEqual(0, pulls[0].Fragments);
            Assert.AreSame(s, outcome.State);
            Assert.AreEqual(1900, s.Gem);
            CollectionAssert.AreEqual(new[] { "r0" }, s.OwnedCharacterIds);
            Assert.AreEqual(1, PityOf(s, "b"));
            Assert.AreEqual(1, s.GachaHistory.Count);
            Assert.AreEqual("b", s.GachaHistory[0].BannerId);
            Assert.AreEqual(1, s.GachaHistory[0].Count);
            CollectionAssert.AreEqual(new[] { "r0" }, s.GachaHistory[0].CharacterIds);
            Assert.AreEqual(ctx.NowUtc.Ticks, s.GachaHistory[0].Utc);
        }

        [Test]
        public void Pull_Ten_DuplicatesGiveFragments()
        {
            var s = State();

            var pulls = Payload(ServerHandlers.GachaPull(s, _data, Ctx(new ScriptedRandom()), "b", 10)).Pulls;

            // Chín R đầu đều là r0 (RNG toàn 0), lượt 10 bị nâng thành SR đầu tiên s0.
            Assert.AreEqual(10, pulls.Count);
            Assert.IsTrue(pulls[0].IsNew);
            Assert.IsTrue(pulls.Skip(1).Take(8).All(p => !p.IsNew && p.CharacterId == "r0" && p.Fragments == 1));
            Assert.AreEqual("s0", pulls[9].CharacterId);
            Assert.IsTrue(pulls[9].IsNew);
            Assert.AreEqual(8, FragmentsOf(s, "r0"));
            Assert.AreEqual(0, FragmentsOf(s, "s0"));
            CollectionAssert.AreEquivalent(new[] { "r0", "s0" }, s.OwnedCharacterIds);
            Assert.AreEqual(1100, s.Gem);
            Assert.AreEqual(10, s.GachaHistory[0].Count);
            Assert.AreEqual("s0", s.GachaHistory[0].CharacterIds[9]);

            // Mảnh trùng theo độ hiếm của banner: SSR đã sở hữu cho 4 mảnh.
            var banner = _data.Banners[0];
            banner.DupFragmentsR = 2;
            banner.DupFragmentsSR = 3;
            banner.DupFragmentsSSR = 4;
            var owned = State();
            owned.OwnedCharacterIds.Add("x0");
            var ssr = Payload(ServerHandlers.GachaPull(owned, _data, Ctx(new ScriptedRandom(0.0, 0.99, 0.0)), "b", 1)).Pulls[0];
            Assert.AreEqual("x0", ssr.CharacterId);
            Assert.IsFalse(ssr.IsNew);
            Assert.AreEqual(4, ssr.Fragments);
            Assert.AreEqual(4, FragmentsOf(owned, "x0"));
        }

        [Test]
        public void Pull_PityCarriesAcrossCalls()
        {
            var s = State(10000);
            var ctx = Ctx(new ScriptedRandom());
            for (var i = 0; i < 4; i++) Assert.AreEqual(CommandCode.Ok, ServerHandlers.GachaPull(s, _data, ctx, "b", 10).Code);
            Assert.AreEqual(40, PityOf(s, "b"), "bộ đếm nằm trong state, không trong RNG hay GachaRules");

            var fifth = Payload(ServerHandlers.GachaPull(s, _data, ctx, "b", 10)).Pulls;
            Assert.AreEqual("x0", fifth[9].CharacterId);
            Assert.AreEqual(0, PityOf(s, "b"));
            Assert.AreEqual(1, s.GachaPity.Count, "một dòng cho mỗi banner, không thêm dòng mới");
        }

        [Test]
        public void Pull_InvalidCountOrBanner_Rejected()
        {
            foreach (var count in new[] { 0, 2, 11, -1 })
            {
                var s = State();
                var before = Json(s);
                var rng = new ScriptedRandom();
                AssertRejected(ServerHandlers.GachaPull(s, _data, Ctx(rng), "b", count), CommandCode.Invalid, "count " + count);
                Assert.AreEqual(before, Json(s), "count " + count);
                Assert.AreEqual(0, rng.Calls, "count " + count);
            }

            foreach (var bannerId in new[] { "nope", "", null })
            {
                var s = State();
                var before = Json(s);
                AssertRejected(ServerHandlers.GachaPull(s, _data, Ctx(new ScriptedRandom()), bannerId, 1), CommandCode.Invalid, "banner " + bannerId);
                Assert.AreEqual(before, Json(s), "banner " + bannerId);
            }
        }

        [Test]
        public void Pull_NotEnoughGem_StateUnchanged()
        {
            // Mỗi cặp: một Gem dưới giá bị từ chối, đúng giá thì quay được (mốc ±1).
            foreach (var (count, cost) in new[] { (1, 100L), (10, 900L) })
            {
                var poor = State(cost - 1);
                var before = Json(poor);
                var rng = new ScriptedRandom();
                AssertRejected(ServerHandlers.GachaPull(poor, _data, Ctx(rng), "b", count), CommandCode.NotEnoughGem, "count " + count);
                Assert.AreEqual(before, Json(poor), "count " + count);
                Assert.AreEqual(0, rng.Calls, "chưa đủ Gem thì chưa roll: count " + count);

                var exact = State(cost);
                Assert.AreEqual(CommandCode.Ok, ServerHandlers.GachaPull(exact, _data, Ctx(new ScriptedRandom()), "b", count).Code, "count " + count);
                Assert.AreEqual(0, exact.Gem, "count " + count);
            }
        }

        [Test]
        public void Pull_RngThrows_StateUnchanged()
        {
            // Lần gọi RNG thứ 7 là giữa lượt 4 của quay 10: các lượt trước đã roll xong mà chưa được phép đổi gì.
            var s = State();
            s.OwnedCharacterIds.Add("r0");
            var before = Json(s);

            Assert.Throws<InvalidOperationException>(() =>
                ServerHandlers.GachaPull(s, _data, Ctx(new ThrowingRandom(throwOnCall: 7)), "b", 10));

            Assert.AreEqual(before, Json(s));
        }

        [Test]
        public void Pull_SameSeed_SameResultAsOldGachaService()
        {
            // Chuỗi sinh bằng GachaRoller cũ (seed 42, banner 70/25/5, pity 50, bảo đảm SR): 5 lần quay 10 rồi 5 lần quay 1.
            // Lượt thứ 50 (cuối lần quay 10 thứ năm) là SSR do pity ép. Chuỗi này không được đổi khi sửa tỉ lệ.
            var expected = ("r0,r1,r0,s1,r2,r0,r0,r0,r0,s1,r0,r2,s1,r2,r2,r1,r0,r1,s0,r2,s0,r0,r2,r0,r2,r1,r0,r1,r1,s1,"
                + "r0,r0,r2,r1,r2,r1,r0,s0,r0,r0,s1,r1,r0,r0,r2,r1,r2,r2,r2,x0,r2,r0,r0,s0,r0").Split(',');
            var s = State(10000);
            var ctx = Ctx(new Random(42));
            var got = new List<string>();
            for (var i = 0; i < 5; i++) got.AddRange(Payload(ServerHandlers.GachaPull(s, _data, ctx, "b", 10)).Pulls.Select(p => p.CharacterId));
            for (var i = 0; i < 5; i++) got.AddRange(Payload(ServerHandlers.GachaPull(s, _data, ctx, "b", 1)).Pulls.Select(p => p.CharacterId));

            CollectionAssert.AreEqual(expected, got);
            Assert.AreEqual(5, PityOf(s, "b"));
            Assert.AreEqual(10000 - 5 * 900 - 5 * 100, s.Gem);
        }

        [Test]
        public void Pull_HistoryCappedAt50()
        {
            var s = State(10000);
            var ctx = Ctx(new ScriptedRandom());
            for (var i = 0; i < 51; i++) ServerHandlers.GachaPull(s, _data, ctx, "b", 1);

            // Lượt thứ 50 ép SSR (x0). Nếu không bỏ mục cũ nhất thì x0 nằm ở chỉ số 49, không phải 48.
            Assert.AreEqual(50, s.GachaHistory.Count);
            Assert.AreEqual("x0", s.GachaHistory[48].CharacterIds[0]);
            Assert.AreEqual("r0", s.GachaHistory[49].CharacterIds[0]);
        }

        // Từ chối thì state truyền vào không đổi một byte (L-003).
        void AssertStarRejected(ServerState s, string id, CommandCode code, string message)
        {
            var before = Json(s);
            var outcome = ServerHandlers.StarUp(s, _data, id);
            AssertRejected(outcome, code, message);
            Assert.AreEqual(before, Json(s), message);
        }

        static ServerState StarState(string id, long fragments, int starUps = 0)
        {
            var s = State();
            s.OwnedCharacterIds.Add(id);
            s.Fragments.Add(new FragmentCount { CharacterId = id, Count = fragments });
            if (starUps > 0) s.StarUps.Add(new StarUpEntry { CharacterId = id, Count = starUps });
            return s;
        }

        [Test]
        public void StarUp_CheckOrder_AndCost()
        {
            // 1. id lạ hoặc null: Invalid, kể cả khi mảnh và sở hữu đều có.
            var ghost = StarState("ghost", 10);
            AssertStarRejected(ghost, "ghost", CommandCode.Invalid, "unknown");
            AssertStarRejected(StarState("r0", 10), null, CommandCode.Invalid, "null id");

            // 2. chưa sở hữu: NotOwned dù đủ mảnh.
            var notOwned = State();
            notOwned.Fragments.Add(new FragmentCount { CharacterId = "r1", Count = 10 });
            AssertStarRejected(notOwned, "r1", CommandCode.NotOwned, "not owned");

            // 3. đã 5★: MaxStars dù đủ mảnh (1★ + 4 lần nâng).
            AssertStarRejected(StarState("r0", 20, starUps: 4), "r0", CommandCode.MaxStars, "max stars");
            // Sao gốc cao hơn cũng tính vào trần: 2★ gốc + 3 lần = 5★.
            _data.Characters.Single(c => c.Id == "s1").BaseStars = 2;
            AssertStarRejected(StarState("s1", 20, starUps: 3), "s1", CommandCode.MaxStars, "base 2 + 3 ups");

            // 4. thiếu mảnh: NotEnoughFragments; đủ đúng giá (mốc ±1) thì được. Giá lên 2★ là 2.
            AssertStarRejected(StarState("r0", 1), "r0", CommandCode.NotEnoughFragments, "1 of 2");
            var noRow = State();
            noRow.OwnedCharacterIds.Add("r0");
            AssertStarRejected(noRow, "r0", CommandCode.NotEnoughFragments, "no fragment row");
            var exact = StarState("r0", 2);
            Assert.AreEqual(CommandCode.Ok, ServerHandlers.StarUp(exact, _data, "r0").Code);
            Assert.AreEqual(0, FragmentsOf(exact, "r0"));

            // Giá theo bậc: 1★ trả 2, 4, 6, 8 (tổng 20) rồi MaxStars; sao gốc 2★ lên 3★ trả 4 (không phải 2).
            var all = StarState("r0", 20);
            var costs = new List<long>();
            for (var target = 2; target <= 5; target++)
            {
                var left = FragmentsOf(all, "r0");
                var payload = (StarUpPayload)ServerHandlers.StarUp(all, _data, "r0").Payload;
                Assert.AreEqual(target, payload.NewStars);
                costs.Add(left - FragmentsOf(all, "r0"));
            }
            CollectionAssert.AreEqual(new long[] { 2, 4, 6, 8 }, costs);
            Assert.AreEqual(0, FragmentsOf(all, "r0"));
            Assert.AreEqual(CommandCode.MaxStars, ServerHandlers.StarUp(all, _data, "r0").Code);

            var base2 = StarState("s1", 4);
            Assert.AreEqual(3, ((StarUpPayload)ServerHandlers.StarUp(base2, _data, "s1").Payload).NewStars);
            Assert.AreEqual(0, FragmentsOf(base2, "s1"));

            // Sao gốc < 1 cho giá âm/0: không được nâng miễn phí.
            _data.Characters.Single(c => c.Id == "s0").BaseStars = 0;
            AssertStarRejected(StarState("s0", 5), "s0", CommandCode.Invalid, "base stars 0");
        }

        [Test]
        public void StarUp_SpendsFragments_IncrementsCount()
        {
            var s = StarState("r0", 3);
            s.Fragments.Add(new FragmentCount { CharacterId = "r1", Count = 9 });
            s.StarUps.Add(new StarUpEntry { CharacterId = "r1", Count = 2 });

            var first = ServerHandlers.StarUp(s, _data, "r0");
            Assert.AreEqual(CommandCode.Ok, first.Code);
            Assert.AreSame(s, first.State);
            Assert.AreEqual(2, ((StarUpPayload)first.Payload).NewStars);
            Assert.AreEqual(1, FragmentsOf(s, "r0"));
            Assert.AreEqual(1, s.StarUps.Single(e => e.CharacterId == "r0").Count);

            // Lần hai: dòng cũ tăng lên, không thêm dòng mới.
            s.Fragments[s.Fragments.FindIndex(f => f.CharacterId == "r0")] = new FragmentCount { CharacterId = "r0", Count = 4 };
            var second = ServerHandlers.StarUp(s, _data, "r0");
            Assert.AreEqual(3, ((StarUpPayload)second.Payload).NewStars);
            Assert.AreEqual(0, FragmentsOf(s, "r0"));
            Assert.AreEqual(2, s.StarUps.Single(e => e.CharacterId == "r0").Count);
            Assert.AreEqual(2, s.StarUps.Count);

            // Nhân vật khác không bị đụng, Gem không đổi.
            Assert.AreEqual(9, FragmentsOf(s, "r1"));
            Assert.AreEqual(2, s.StarUps.Single(e => e.CharacterId == "r1").Count);
            Assert.AreEqual(2000, s.Gem);
        }

        [Test]
        public void Bootstrap_NewWhenNull_KeepsExisting_NewSessionEachTime()
        {
            var ctx = Ctx(new ScriptedRandom());

            var created = ServerHandlers.Bootstrap(null, _data, ctx);
            Assert.AreEqual(CommandCode.Ok, created.Code);
            Assert.IsNull(created.Payload);
            Assert.AreEqual(1500, created.State.Gem, "game mới theo NewGame");
            CollectionAssert.AreEqual(new[] { "r0" }, created.State.OwnedCharacterIds);
            Assert.AreEqual(100, created.State.Stamina.Current);
            Assert.AreEqual("id1", created.State.ActiveSessionId);
            Assert.AreEqual(1, created.State.Revision);

            var existing = State(77);
            existing.Revision = 5;
            existing.ActiveSessionId = "old";
            existing.OwnedCharacterIds.Add("s0");
            var kept = ServerHandlers.Bootstrap(existing, _data, ctx);
            Assert.AreEqual(77, kept.State.Gem, "state có sẵn không bị tạo lại");
            CollectionAssert.AreEqual(new[] { "s0" }, kept.State.OwnedCharacterIds);
            Assert.AreEqual("id2", kept.State.ActiveSessionId);
            Assert.AreEqual(6, kept.State.Revision);

            var again = ServerHandlers.Bootstrap(kept.State, _data, ctx);
            Assert.AreEqual("id3", again.State.ActiveSessionId);
            Assert.AreNotEqual(kept.State.ActiveSessionId, again.State.ActiveSessionId);
            Assert.AreEqual(77, again.State.Gem);
        }

        [Test]
        public void DevAddGem_Bounds()
        {
            foreach (var bad in new long[] { 0, -1, 100001, long.MaxValue, long.MinValue })
            {
                var s = State(50);
                var before = Json(s);
                AssertRejected(ServerHandlers.DevAddGem(s, bad), CommandCode.Invalid, "amount " + bad);
                Assert.AreEqual(before, Json(s), "amount " + bad);
            }

            var low = State(50);
            Assert.AreEqual(CommandCode.Ok, ServerHandlers.DevAddGem(low, 1).Code);
            Assert.AreEqual(51, low.Gem);

            var high = State(50);
            Assert.AreEqual(CommandCode.Ok, ServerHandlers.DevAddGem(high, 100000).Code);
            Assert.AreEqual(100050, high.Gem);
        }

        [Test]
        public void DevRefillStamina_Full()
        {
            var s = State(42);
            s.Stamina = new StaminaState { Current = 3, AnchorUtcTicks = 638_000_000_000_000_000 };

            var outcome = ServerHandlers.DevRefillStamina(s, _data);

            Assert.AreEqual(CommandCode.Ok, outcome.Code);
            Assert.AreEqual(100, s.Stamina.Current);
            Assert.AreEqual(0, s.Stamina.AnchorUtcTicks, "anchor 0 = đầy");
            Assert.AreEqual(42, s.Gem);
        }
    }
}
