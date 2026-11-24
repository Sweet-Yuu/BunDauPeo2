using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class PlayerLevelBarRuleTests
    {
        [Test]
        public void Fill_IsExpOverToNext() => Assert.AreEqual(0.5f, PlayerLevelBarRule.Fill(50, 100, false), 0.0001f);

        [Test]
        public void Fill_AtMaxLevel_IsOne() => Assert.AreEqual(1f, PlayerLevelBarRule.Fill(0, 0, true));
    }
}
