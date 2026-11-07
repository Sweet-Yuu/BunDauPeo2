using BunDauPeo.Core;
using BunDauPeo.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class GameLanguageTests
    {
        private static readonly SystemLanguage[] Devices =
            { SystemLanguage.Vietnamese, SystemLanguage.English, SystemLanguage.Japanese, SystemLanguage.Unknown };

        [TearDown]
        public void TearDown() => GameLanguage.Current = Language.Vi;

        [Test]
        public void Resolve_SavedCodeWins_RegardlessOfDevice()
        {
            foreach (var saved in new[] { "vi", "en" })
            foreach (var system in Devices)
            {
                var expected = saved == "vi" ? Language.Vi : Language.En;
                Assert.AreEqual(expected, GameLanguage.Resolve(saved, system), $"saved={saved}, system={system}");
            }
        }

        [Test]
        public void Resolve_EmptyOrUnknownCode_FollowsDevice()
        {
            // "VI" chứng minh mã phân biệt hoa thường: code chỉ ghi chữ thường.
            foreach (var saved in new[] { "", null, "fr", "VI" })
            foreach (var system in Devices)
            {
                var expected = system == SystemLanguage.Vietnamese ? Language.Vi : Language.En;
                Assert.AreEqual(expected, GameLanguage.Resolve(saved, system), $"saved={saved ?? "null"}, system={system}");
            }
        }

        [Test]
        public void Code_RoundTripsThroughTryParse()
        {
            foreach (Language language in System.Enum.GetValues(typeof(Language)))
            {
                Assert.IsTrue(GameLanguage.TryParse(GameLanguage.Code(language), out var parsed), $"language={language}");
                Assert.AreEqual(language, parsed);
            }
            Assert.AreEqual("en", GameLanguage.Code(Language.En));
            Assert.AreEqual("vi", GameLanguage.Code(Language.Vi));
        }

        // N2: thoát Play thì trả tiếng Việt để builder và test EditMode không thấy tiếng Anh còn sót từ lượt Play.
        [Test]
        public void LanguageReset_EnteredEditMode_ResetsToVietnamese_OtherStatesDoNot()
        {
            GameLanguage.Current = Language.En;
            LanguageReset.HandlePlayModeState(PlayModeStateChange.EnteredPlayMode);
            LanguageReset.HandlePlayModeState(PlayModeStateChange.ExitingPlayMode);
            Assert.AreEqual(Language.En, GameLanguage.Current, "chỉ EnteredEditMode mới trả lại");

            LanguageReset.HandlePlayModeState(PlayModeStateChange.EnteredEditMode);
            Assert.AreEqual(Language.Vi, GameLanguage.Current);
        }

        [Test]
        public void Pick_EnglishEmpty_FallsBackToVietnamese()
        {
            GameLanguage.Current = Language.En;
            Assert.AreEqual("B", GameLanguage.Pick("A", "B"));
            Assert.AreEqual("A", GameLanguage.Pick("A", ""));
            Assert.AreEqual("A", GameLanguage.Pick("A", null));
            GameLanguage.Current = Language.Vi;
            Assert.AreEqual("A", GameLanguage.Pick("A", "B"));
        }
    }
}
