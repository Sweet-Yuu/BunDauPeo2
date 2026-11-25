using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class CharacterDetailNavTests
    {
        private static readonly string[] Ids = { "a", "b", "c" };

        [Test]
        public void Next_WrapsAroundList() => Assert.AreEqual("a", CharacterDetailNav.Neighbor(Ids, "c", 1));

        [Test]
        public void Prev_WrapsAroundList() => Assert.AreEqual("c", CharacterDetailNav.Neighbor(Ids, "a", -1));

        [Test]
        public void SingleCharacter_StaysSame()
        {
            Assert.AreEqual("a", CharacterDetailNav.Neighbor(new[] { "a" }, "a", 1));
            Assert.AreEqual("", CharacterDetailNav.Neighbor(new string[0], "a", 1));
        }

        [Test]
        public void CurrentMissing_FallsBackToFirst() => Assert.AreEqual("a", CharacterDetailNav.Neighbor(Ids, "zzz", 1));
    }
}
