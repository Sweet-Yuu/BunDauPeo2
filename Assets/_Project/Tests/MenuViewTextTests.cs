using BunDauPeo.Core;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class MenuViewTextTests
    {
        [TearDown]
        public void TearDown() => GameLanguage.Current = Language.Vi;

        [Test]
        public void Labels_FollowLanguage()
        {
            GameLanguage.Current = Language.Vi;
            Assert.AreEqual("Ngôn ngữ: Tiếng Việt", MenuViewText.LanguageLabel());
            var viSoon = MenuViewText.ComingSoon();

            GameLanguage.Current = Language.En;
            Assert.AreEqual("Language: English", MenuViewText.LanguageLabel());
            Assert.AreNotEqual(viSoon, MenuViewText.ComingSoon());
        }

        [Test]
        public void ResetFailed_DoesNotPromiseKeptData()
        {
            foreach (var language in new[] { Language.Vi, Language.En })
            {
                GameLanguage.Current = language;
                var text = MenuViewText.ResetFailed();

                StringAssert.DoesNotContain("giữ nguyên", text, $"language = {language}");
                StringAssert.DoesNotContain("kept", text, $"language = {language}");
                Assert.IsFalse(text.StartsWith("#"), $"key thiếu: {text}");
            }
        }
    }
}
