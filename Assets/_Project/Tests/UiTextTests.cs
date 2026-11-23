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

        private static readonly string[] AccountKeys =
        {
            "account.row", "account.not_linked", "account.linked", "account.link_btn", "account.signin_btn", "account.choose_link",
            "account.choose_signin", "account.google", "account.userpass", "account.username", "account.password", "account.create",
            "account.signin", "account.err_taken", "account.err_weak", "account.err_wrong", "account.err_name", "account.err_other",
            "account.hint_pass", "account.warn_lose", "account.conflict_title", "account.conflict_body", "account.switch",
        };

        [Test]
        public void AccountKeys_ExistInBothLanguages()
        {
            var keys = new HashSet<string>(UiText.AllEntries.Select(e => e.Key));
            foreach (var key in AccountKeys)
            {
                Assert.IsTrue(keys.Contains(key), $"thiếu khoá {key}");
                GameLanguage.Current = Language.Vi;
                var vi = UiText.Get(key);
                GameLanguage.Current = Language.En;
                // "Google" giống nhau ở hai tiếng.
                if (key != "account.google") Assert.AreNotEqual(vi, UiText.Get(key), $"{key}: bản Anh trùng bản Việt");
            }
        }

        private static readonly string[] MailKeys =
        {
            "mail.title", "mail.count", "mail.claim_all", "mail.delete_read", "mail.empty", "mail.attachments", "mail.sender",
            "mail.link_title", "mail.link_body", "mail.link_btn",
        };

        [Test]
        public void MailKeys_ExistInBothLanguages()
        {
            var keys = new HashSet<string>(UiText.AllEntries.Select(e => e.Key));
            foreach (var key in MailKeys)
            {
                Assert.IsTrue(keys.Contains(key), $"thiếu khoá {key}");
                GameLanguage.Current = Language.Vi;
                var vi = UiText.Get(key);
                GameLanguage.Current = Language.En;
                Assert.AreNotEqual(vi, UiText.Get(key), $"{key}: bản Anh trùng bản Việt");
            }

            // Hộp: nội dung thư 1040×300 cỡ 34, tiêu đề dòng thư 430 cỡ 34 (co chữ), tiêu đề chi tiết 1040 cỡ 44.
            Assert.LessOrEqual(Lines("mail.link_body", 34, 1040) * 34 * 1.2f, 300f, "mail.link_body");
            Assert.LessOrEqual(Lines("mail.link_title", 44, 1040), 1, "mail.link_title");
        }

        [Test]
        public void AccountEnglishLongest_FitsBoxes()
        {
            GameLanguage.Current = Language.En;
            Assert.LessOrEqual(Lines("account.err_name", 32, 820), 1, "account.err_name: một dòng 820 cỡ 32");
            Assert.LessOrEqual(Lines("account.err_weak", 32, 820), 1, "account.err_weak: một dòng 820 cỡ 32");
            Assert.LessOrEqual(Lines("account.err_wrong", 32, 820), 1, "account.err_wrong: một dòng 820 cỡ 32");
            Assert.LessOrEqual(Lines("account.warn_lose", 30, 820), 2, "account.warn_lose: hai dòng 820 cỡ 30 (hộp 70)");
            Assert.LessOrEqual(Lines("account.hint_pass", 26, 820), 2, "account.hint_pass: hai dòng 820 cỡ 26 (hộp 70)");
            Assert.LessOrEqual(Lines("account.conflict_body", 34, 820) * 34 * 1.2f, 140f, "account.conflict_body: hộp 820×140 cỡ 34");
            Assert.LessOrEqual(Lines("account.conflict_title", 40, 840), 1, "account.conflict_title: một dòng 840 cỡ 40");
            Assert.LessOrEqual(Lines("account.choose_signin", 52, 840), 1, "account.choose_signin: một dòng 840 cỡ 52");
            Assert.LessOrEqual(Lines("account.userpass", 36, 700), 1, "account.userpass: một dòng 700 cỡ 36");
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
