using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class CharactersViewTextTests
    {
        [TearDown]
        public void TearDown() => GameLanguage.Current = Language.Vi;

        private static CharacterListRow Row(int stars, long fragments, int cost, bool max)
        {
            var now = new CombatStats { HP = 1000, Attack = 110, Defense = 50, Speed = 110 };
            var next = max ? now : new CombatStats { HP = 1100, Attack = 121, Defense = 55, Speed = 110 };
            return new CharacterListRow(TestData.Character("a"), stars, fragments, cost, max, !max && fragments >= cost, now, next);
        }

        [Test]
        public void CardAndLines_Vietnamese()
        {
            Assert.AreEqual("3/4", CharactersViewText.CardFragments(Row(3, 3, 4, false)));
            Assert.AreEqual("Tối đa", CharactersViewText.CardFragments(Row(5, 9, 0, true)));
            Assert.AreEqual("★★★☆☆", CharactersViewText.StarsLine(3));
            StringAssert.Contains("1000 → 1100", CharactersViewText.StatLine("hp", 1000, 1100, false));
            StringAssert.DoesNotContain("→", CharactersViewText.StatLine("hp", 1000, 1100, true));
            StringAssert.Contains("5 / 6", CharactersViewText.FragmentsLine(Row(3, 5, 6, false)));
            StringAssert.DoesNotContain("/", CharactersViewText.FragmentsLine(Row(5, 9, 0, true)));
        }

        [Test]
        public void ButtonLabel_Vietnamese()
        {
            Assert.AreEqual("Nâng lên 4★ (6 mảnh)", CharactersViewText.ButtonLabel(Row(3, 5, 6, false)));
            Assert.AreEqual("Đã tối đa", CharactersViewText.ButtonLabel(Row(5, 9, 0, true)));
        }

        [Test]
        public void StatusText_SavedFalse_SaysNotSaved()
        {
            StringAssert.Contains("Chưa lưu được", CharactersViewText.StatusText(new StarUpResult { Status = StarUpStatus.Success, StarsAfter = 3, Saved = false }));
            StringAssert.Contains("Đã lên 3★", CharactersViewText.StatusText(new StarUpResult { Status = StarUpStatus.Success, StarsAfter = 3, Saved = true }));
        }

        [Test]
        public void StatusText_Rejections_AreReadable()
        {
            foreach (StarUpStatus status in System.Enum.GetValues(typeof(StarUpStatus)))
            {
                var text = CharactersViewText.StatusText(new StarUpResult { Status = status, StarsAfter = 2, Saved = true });
                Assert.IsFalse(text.StartsWith("#"), status.ToString());
            }
            var notEnough = CharactersViewText.StatusText(new StarUpResult { Status = StarUpStatus.NotEnoughFragments });
            var error = CharactersViewText.StatusText(new StarUpResult { Status = StarUpStatus.InvalidConfig });
            Assert.AreNotEqual(notEnough, error);
        }

        [Test]
        public void ButtonLabel_English()
        {
            GameLanguage.Current = Language.En;
            var label = CharactersViewText.ButtonLabel(Row(3, 5, 6, false));
            StringAssert.Contains("★", label);
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(label, "[À-ỹ]"), label);
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(CharactersViewText.ButtonLabel(Row(5, 9, 0, true)), "[À-ỹ]"));
        }
    }
}
