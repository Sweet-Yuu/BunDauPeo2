using System;
using System.Text.RegularExpressions;
using BunDauPeo.Core;
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

        private static CurrencyAmount[] Gem(long amount) => new[] { new CurrencyAmount(CurrencyType.Gem, amount) };

        [Test]
        public void InsufficientGemText_ContainsNeededAmount() => StringAssert.Contains("900", GachaViewText.InsufficientGemText(900));

        [Test]
        public void RollAgainLabel_ShowsCost()
        {
            var label = GachaViewText.RollAgainLabel(Gem(900));
            StringAssert.Contains("Quay tiếp", label);
            StringAssert.Contains("900", label);
        }

        [Test]
        public void CanRollAgain_FalseWhenShort_TrueWhenEnough()
        {
            var wallet = new Wallet(new PlayerSave());
            wallet.Add(CurrencyType.Gem, 899);
            Assert.IsFalse(GachaViewText.CanRollAgain(wallet, Gem(900)));
            wallet.Add(CurrencyType.Gem, 1);
            Assert.IsTrue(GachaViewText.CanRollAgain(wallet, Gem(900)));
        }

        [Test]
        public void HistoryLine_ListsCountAndNames()
        {
            var entry = new GachaHistoryEntry { Utc = 0, BannerId = "b", Count = 10, CharacterIds = new[] { "a", "b" } };
            var line = GachaViewText.HistoryLine(entry, id => "N" + id);
            StringAssert.Contains("Quay 10", line);
            StringAssert.Contains("Na", line);
            StringAssert.Contains("Nb", line);
        }

        [Test]
        public void HistoryLine_SingleRoll_SaysQuay1()
        {
            var entry = new GachaHistoryEntry { Utc = 0, BannerId = "b", Count = 1, CharacterIds = new[] { "a" } };
            StringAssert.Contains("Quay 1 ", GachaViewText.HistoryLine(entry, id => id) + " ");
        }

        [Test]
        public void StarsText_ShowsCount() => Assert.AreEqual("★★★", GachaViewText.StarsText(3));

        [Test]
        public void PullLabel_TwoLines() =>
            Assert.AreEqual("Quay 1\n100 Gem", GachaViewText.PullLabel(1, new[] { new CurrencyAmount(CurrencyType.Gem, 100) }));

        [TearDown]
        public void ResetLanguage() => GameLanguage.Current = Language.Vi;

        [Test]
        public void Describe_English()
        {
            GameLanguage.Current = Language.En;
            Assert.AreEqual("NEW", GachaViewText.Describe(new GachaPullEntry(TestData.Character("c"), true, 0)));
            var dup = GachaViewText.Describe(new GachaPullEntry(TestData.Character("c"), false, 3));
            StringAssert.Contains("+3", dup);
            Assert.IsFalse(Regex.IsMatch(dup, "[À-ỹ]"), dup);
        }

        [Test]
        public void PullLabel_English_KeepsTwoLines()
        {
            var cost = Gem(900);
            var vi = GachaViewText.PullLabel(10, cost);
            GameLanguage.Current = Language.En;
            var lines = GachaViewText.PullLabel(10, cost).Split('\n');
            Assert.AreEqual(2, lines.Length);
            StringAssert.Contains("10", lines[0]);
            Assert.AreEqual(vi.Split('\n')[1], lines[1]);
        }

        [Test]
        public void StatusMessage_English_EveryStatus()
        {
            foreach (GachaPullStatus status in Enum.GetValues(typeof(GachaPullStatus)))
            {
                foreach (var saved in new[] { true, false })
                {
                    GameLanguage.Current = Language.Vi;
                    var vi = GachaViewText.StatusMessage(status, saved);
                    GameLanguage.Current = Language.En;
                    var en = GachaViewText.StatusMessage(status, saved);
                    Assert.IsFalse(en.StartsWith("#"), $"{status}/{saved}: {en}");
                    Assert.IsFalse(Regex.IsMatch(en, "[À-ỹ]"), $"{status}/{saved}: {en}");
                    if (vi != "") Assert.AreNotEqual(vi, en, $"{status}/{saved}");
                }
            }
        }
    }
}
