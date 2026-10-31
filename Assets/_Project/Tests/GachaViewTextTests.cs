using BunDauPeo.Gacha;
using BunDauPeo.Characters;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class GachaViewTextTests
    {
        [Test]
        public void Describe_NewCharacter_ShowsNew() =>
            StringAssert.Contains("MỚI", GachaViewText.Describe(new GachaPullEntry(TestData.Character("c"), true, 0)));

        [Test]
        public void Describe_Duplicate_ShowsFragments()
        {
            var text = GachaViewText.Describe(new GachaPullEntry(TestData.Character("c"), false, 3));
            StringAssert.Contains("TRÙNG", text);
            StringAssert.Contains("+3", text);
        }

        [Test]
        public void RarityColor_IsDistinctPerRarity()
        {
            var r = GachaViewText.RarityColor(Rarity.R);
            var sr = GachaViewText.RarityColor(Rarity.SR);
            var ssr = GachaViewText.RarityColor(Rarity.SSR);
            Assert.AreNotEqual(r, sr);
            Assert.AreNotEqual(r, ssr);
            Assert.AreNotEqual(sr, ssr);
        }

        [Test]
        public void PityLine_ShowsRemaining()
        {
            StringAssert.Contains("50", GachaViewText.PityLine(50, 0));
            var nearly = GachaViewText.PityLine(50, 49);
            StringAssert.Contains("1", nearly);
            StringAssert.DoesNotContain("50", nearly);
        }

        [Test]
        public void PityLine_CounterPastThreshold_ShowsOneNotZero()
        {
            // Hạ ngưỡng trong Inspector khi save đang cao hơn: lượt tới vẫn là lượt ép SSR, nên là "1" chứ không phải "0".
            var text = GachaViewText.PityLine(30, 40);
            StringAssert.Contains(" 1 ", text);
            StringAssert.DoesNotContain(" 0 ", text);
        }

        [Test]
        public void PityLine_ThresholdZero_ShowsOff() =>
            StringAssert.Contains("tắt", GachaViewText.PityLine(0, 0));

        [Test]
        public void StatusMessage_FailuresHaveText_SuccessSavedIsEmpty_SuccessUnsavedWarns()
        {
            Assert.AreEqual("", GachaViewText.StatusMessage(GachaPullStatus.Success, true));
            var unsaved = GachaViewText.StatusMessage(GachaPullStatus.Success, false);
            Assert.IsNotEmpty(unsaved);

            var unknown = GachaViewText.StatusMessage(GachaPullStatus.UnknownBanner, false);
            var invalid = GachaViewText.StatusMessage(GachaPullStatus.InvalidBanner, false);
            var poor = GachaViewText.StatusMessage(GachaPullStatus.NotEnoughCurrency, false);
            Assert.IsNotEmpty(unknown);
            Assert.IsNotEmpty(invalid);
            Assert.IsNotEmpty(poor);
            Assert.AreNotEqual(unknown, invalid);
            Assert.AreNotEqual(unknown, poor);
            Assert.AreNotEqual(invalid, poor);
        }
    }
}
