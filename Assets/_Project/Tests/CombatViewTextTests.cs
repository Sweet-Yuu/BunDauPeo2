using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class CombatViewTextTests
    {
        [Test]
        public void UnitHp_CurrentOverMax()
        {
            Assert.AreEqual("640/800", CombatViewText.UnitHp(640, 800));
            Assert.AreEqual("0/800", CombatViewText.UnitHp(0, 800));
        }

        [Test]
        public void ActionSeconds_FastIsHalf()
        {
            Assert.AreEqual(0.6f, BattleView.ActionSeconds(false), 1e-6f);
            Assert.AreEqual(0.3f, BattleView.ActionSeconds(true), 1e-6f);
            Assert.AreEqual(BattleView.ActionSeconds(false) / 2f, BattleView.ActionSeconds(true), 1e-6f);
        }
    }
}
