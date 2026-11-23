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

        private static readonly string[] MissionKeys =
        {
            "missions.title", "missions.tab_daily", "missions.tab_newbie", "missions.progress", "missions.reward", "missions.goto", "missions.claim",
            "missions.claimed", "missions.claim_all", "missions.remaining", "missions.completed", "missions.not_yet", "missions.missed", "missions.locked",
            "missions.daily_win_5", "missions.daily_pull_1", "missions.daily_level_1", "missions.daily_login_am", "missions.daily_login_pm",
            "missions.new_link", "missions.new_name", "missions.new_staff", "missions.new_first_win", "missions.new_first_pull", "missions.new_upgrade",
            "missions.new_afk", "missions.new_star_up", "missions.new_win_10", "stamina.cap_title", "stamina.cap_body", "gacha.free_pull", "gacha.free_left",
        };

        [Test]
        public void MissionKeys_ExistInBothLanguages()
        {
            var keys = new HashSet<string>(UiText.AllEntries.Select(e => e.Key));
            foreach (var key in MissionKeys)
            {
                Assert.IsTrue(keys.Contains(key), $"thiếu khoá {key}");
                GameLanguage.Current = Language.Vi;
                var vi = UiText.Get(key);
                GameLanguage.Current = Language.En;
                Assert.AreNotEqual(vi, UiText.Get(key), $"{key}: bản Anh trùng bản Việt");
            }
        }

        // L-039: Arial ~0,5 em mỗi ký tự; chuỗi dài nhất bản Anh phải vừa ô đã dựng (số dòng × 1,2 em ≤ cao ô).
        [Test]
        public void EnglishLongest_FitsBoxes()
        {
            GameLanguage.Current = Language.En;
            Assert.LessOrEqual(Lines("stamina.cap_body", 34, 820) * 34 * 1.2f, 180f, "stamina.cap_body: hộp 820×180 cỡ 34");
            Assert.LessOrEqual(Lines("stamina.cap_title", 44, 820), 1, "stamina.cap_title: một dòng 820 cỡ 44");
            Assert.LessOrEqual(Lines("missions.daily_login_am", 34, 1000), 1, "missions.daily_login_am: một dòng 1000 cỡ 34");
            Assert.LessOrEqual(Lines("missions.daily_login_pm", 34, 1000), 1, "missions.daily_login_pm: một dòng 1000 cỡ 34");
            Assert.LessOrEqual(Lines("missions.new_first_win", 34, 1000), 1, "missions.new_first_win: một dòng 1000 cỡ 34");
        }

        private static int Lines(string key, int fontSize, float width) => (int)Math.Ceiling(UiText.Get(key).Length * 0.5f * fontSize / width);
    }
}
