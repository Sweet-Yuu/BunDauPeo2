using System.Collections.Generic;
using BunDauPeo.Rules;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class GiftRulesTests
    {
        const long Start = 1_000_000, End = 2_000_000;

        static GiftData Gift(string id = "g1", long amount = 10, string type = "Gem") => new GiftData
        {
            Id = id, TitleVi = "Quà", TitleEn = "Gift", StartUtcTicks = Start, EndUtcTicks = End,
            Rewards = new List<GiftReward> { new GiftReward { Type = type, Amount = amount } },
        };

        static void AssertDropped(GiftData bad, string why)
        {
            var good = Gift("ok");
            var r = GiftRules.Validate(new List<GiftData> { bad, good });
            Assert.AreEqual(1, r.Count, why);
            Assert.AreEqual("ok", r[0].Id, why);
        }

        [Test]
        public void Window_StartInclusive_EndExclusive()
        {
            var g = Gift();
            Assert.IsFalse(GiftRules.InWindow(g, Start - 1));
            Assert.IsTrue(GiftRules.InWindow(g, Start));
            Assert.IsTrue(GiftRules.InWindow(g, End - 1));
            Assert.IsFalse(GiftRules.InWindow(g, End));
        }

        [Test]
        public void Validate_KeepsGoodGift()
        {
            var r = GiftRules.Validate(new List<GiftData> { Gift("a_1", 1_000_000, "Material") });
            Assert.AreEqual(1, r.Count);
        }

        [Test]
        public void Validate_DropsBadGiftOnly()
        {
            AssertDropped(Gift(""), "id rỗng");
            AssertDropped(Gift("Abc"), "id chữ hoa");
            AssertDropped(Gift("a b"), "id có khoảng trắng");
            AssertDropped(Gift(new string('a', 41)), "id 41 ký tự");
            AssertDropped(Gift(type: "Diamond"), "type lạ");
            AssertDropped(Gift(amount: 0), "amount 0");
            AssertDropped(Gift(amount: -5), "amount âm");
            AssertDropped(Gift(amount: 1_000_001), "amount quá trần");

            var inverted = Gift(); inverted.EndUtcTicks = inverted.StartUtcTicks;
            AssertDropped(inverted, "endUtc <= startUtc");

            var many = Gift();
            for (var i = 0; i < 10; i++) many.Rewards.Add(new GiftReward { Type = "Coin", Amount = 1 });
            AssertDropped(many, "11 dòng thưởng");

            var noRewards = Gift(); noRewards.Rewards.Clear();
            AssertDropped(noRewards, "không có thưởng");

            var noTitle = Gift(); noTitle.TitleVi = ""; noTitle.TitleEn = null;
            AssertDropped(noTitle, "thiếu cả hai tên");

            var nullReward = Gift(); nullReward.Rewards = new List<GiftReward> { null };
            AssertDropped(nullReward, "dòng thưởng null");

            AssertDropped(null, "quà null");
        }

        [Test]
        public void Validate_NullOrEmpty_ReturnsEmpty()
        {
            Assert.AreEqual(0, GiftRules.Validate(null).Count);
            Assert.AreEqual(0, GiftRules.Validate(new List<GiftData>()).Count);
        }

        [Test]
        public void Validate_DuplicateId_KeepsFirst()
        {
            var first = Gift("dup", 5); var second = Gift("dup", 99);
            var r = GiftRules.Validate(new List<GiftData> { first, second });
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual(5, r[0].Rewards[0].Amount);
        }

        [Test]
        public void Validate_Caps200Gifts()
        {
            var raw = new List<GiftData>();
            for (var i = 0; i < 201; i++) raw.Add(Gift("g" + i));
            var r = GiftRules.Validate(raw);
            Assert.AreEqual(200, r.Count);
            Assert.AreEqual("g199", r[199].Id);
        }

        [Test]
        public void View_MarksClaimed_AndHidesOutOfWindow()
        {
            var late = Gift("late"); late.StartUtcTicks = End; late.EndUtcTicks = End + 10;
            var expired = Gift("old"); expired.StartUtcTicks = 1; expired.EndUtcTicks = Start;
            var valid = new List<GiftData> { Gift("a"), Gift("b"), late, expired };
            var s = new ServerState { ClaimedGiftIds = { "b" } };

            var rows = GiftRules.View(s, valid, Start, "vi");

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("a", rows[0].Id); Assert.IsFalse(rows[0].Claimed);
            Assert.AreEqual("b", rows[1].Id); Assert.IsTrue(rows[1].Claimed);
            Assert.AreEqual(End, rows[0].EndUtcTicks);
            Assert.AreEqual(10, rows[0].Rewards[0].Amount);
        }

        [Test]
        public void View_TitleFollowsLanguage_AndFallsBack()
        {
            var both = Gift("both");
            var onlyEn = Gift("en"); onlyEn.TitleVi = "";
            var onlyVi = Gift("vi"); onlyVi.TitleEn = null;
            var rows = GiftRules.View(new ServerState(), new List<GiftData> { both, onlyEn, onlyVi }, Start, "vi");
            Assert.AreEqual("Quà", rows[0].Title);
            Assert.AreEqual("Gift", rows[1].Title);
            Assert.AreEqual("Quà", rows[2].Title);

            rows = GiftRules.View(new ServerState(), new List<GiftData> { both, onlyEn, onlyVi }, Start, "en");
            Assert.AreEqual("Gift", rows[0].Title);
            Assert.AreEqual("Gift", rows[1].Title);
            Assert.AreEqual("Quà", rows[2].Title);
        }
    }
}
