using BunDauPeo.Core;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class PlayerNameRuleTests
    {
        [TearDown]
        public void ResetLanguage() => GameLanguage.Current = Language.Vi;

        [Test]
        public void Normalize_AndDisplay()
        {
            Assert.AreEqual(12, PlayerNameRule.MaxLength);
            Assert.AreEqual("", PlayerNameRule.Normalize(null));
            Assert.AreEqual("", PlayerNameRule.Normalize("   "));
            Assert.AreEqual("An", PlayerNameRule.Normalize("  An  "));
            Assert.AreEqual("An", PlayerNameRule.Normalize("\n An \t"), "controls go before the trim");
            Assert.AreEqual("ABC", PlayerNameRule.Normalize("A\nB\tC"));
            Assert.AreEqual("A B", PlayerNameRule.Normalize("A B"), "inner spaces stay");
            Assert.AreEqual("abcdefghijkl", PlayerNameRule.Normalize("abcdefghijkl"), "12 stays");
            Assert.AreEqual("abcdefghijkl", PlayerNameRule.Normalize("abcdefghijklm"), "13 is cut to 12");

            // 11 ký tự + "e" kèm dấu ghép = 12 text element (13 char): giữ cả cụm.
            var combined = "abcdefghijk" + "é";
            Assert.AreEqual(combined, PlayerNameRule.Normalize(combined));

            // 12 ký tự + cụm thứ 13: cắt ở ranh giới cụm, không để lại dấu ghép mồ côi.
            Assert.AreEqual("abcdefghijkl", PlayerNameRule.Normalize("abcdefghijkl" + "é"));

            GameLanguage.Current = Language.Vi;
            Assert.AreEqual("Người chơi", PlayerNameRule.Display(""));
            GameLanguage.Current = Language.En;
            Assert.AreEqual("Player", PlayerNameRule.Display(""));
            Assert.AreEqual("An", PlayerNameRule.Display("An"));
        }
        // InputField đếm 12 char UTF-16 và trên Android cắt bằng Substring: có thể để lại nửa cặp surrogate (emoji), không được vào save.
        [Test]
        public void Normalize_DropsLoneSurrogates_KeepsWholePairs()
        {
            const string emoji = "😀";
            Assert.AreEqual("abcdefghijk", PlayerNameRule.Normalize("abcdefghijk\uD83D"), "emoji cut in half at 12 chars: the high half goes");
            Assert.AreEqual("abc", PlayerNameRule.Normalize("\uDE00abc"), "a lone low surrogate goes");
            Assert.AreEqual("ab", PlayerNameRule.Normalize("a\uD83Db"), "a high surrogate not followed by a low one goes");
            Assert.AreEqual("a" + emoji + "b", PlayerNameRule.Normalize("a" + emoji + "b"), "a whole emoji stays");
            Assert.AreEqual("abcdefghijk" + emoji, PlayerNameRule.Normalize("abcdefghijk" + emoji), "11 + 1 emoji = 12 elements stays whole");
            Assert.AreEqual("abcdefghijkl", PlayerNameRule.Normalize("abcdefghijkl" + emoji), "the 13th element is cut");
        }
    }
}
