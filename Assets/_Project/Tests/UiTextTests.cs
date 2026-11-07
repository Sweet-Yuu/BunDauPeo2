using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BunDauPeo.Core;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class UiTextTests
    {
        [TearDown]
        public void TearDown() => GameLanguage.Current = Language.Vi;

        [Test]
        public void AllEntries_HaveBothLanguages()
        {
            Assert.IsNotEmpty(UiText.AllEntries, "bảng không được rỗng (Common có sẵn)");
            foreach (var (key, vi, en) in UiText.AllEntries)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(key), "key rỗng");
                Assert.IsFalse(string.IsNullOrWhiteSpace(vi), $"Vi rỗng: {key}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(en), $"En rỗng: {key}");
            }
        }

        [Test]
        public void AllEntries_PlaceholdersAndLineBreaksMatch()
        {
            foreach (var (key, vi, en) in UiText.AllEntries)
            {
                CollectionAssert.AreEquivalent(Placeholders(vi), Placeholders(en), $"{{n}} lệch: {key}");
                Assert.AreEqual(vi.Count(c => c == '\n'), en.Count(c => c == '\n'), $"số \n lệch: {key}");
            }
        }

        [Test]
        public void AllEntries_KeysFollowFormat()
        {
            foreach (var (key, _, _) in UiText.AllEntries)
                Assert.IsTrue(Regex.IsMatch(key ?? "", @"^[a-z]+\.[a-z0-9_]+$"), $"key sai dạng: {key}");
        }

        [Test]
        public void BuildTable_DuplicateKey_Throws()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                UiText.BuildTable(new[] { ("x.a", "a", "a"), ("x.a", "b", "b") }));
            StringAssert.Contains("x.a", ex.Message);

            var table = UiText.BuildTable(new[] { ("x.a", "a", "A"), ("x.b", "b", "B") });
            Assert.AreEqual(2, table.Count);
        }

        [Test]
        public void Get_FollowsCurrentLanguage_AndUnknownKeyShowsHash()
        {
            GameLanguage.Current = Language.Vi;
            Assert.AreEqual("Đóng", UiText.Get("common.close"));
            GameLanguage.Current = Language.En;
            Assert.AreEqual("Close", UiText.Get("common.close"));
            Assert.AreEqual("#nope.key", UiText.Get("nope.key"));
        }

        [Test]
        public void Decimal_CommaInVietnamese_DotInEnglish()
        {
            GameLanguage.Current = Language.Vi;
            Assert.AreEqual("1,5", UiText.Decimal(1.5, "0.0"));
            GameLanguage.Current = Language.En;
            Assert.AreEqual("1.5", UiText.Decimal(1.5, "0.0"));
        }

        private static List<string> Placeholders(string text) =>
            Regex.Matches(text, @"\{(\d+)").Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
    }
}
