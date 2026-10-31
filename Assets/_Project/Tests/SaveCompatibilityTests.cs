using BunDauPeo.Core;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // JsonUtility lưu enum dưới dạng số, nên chèn một giá trị vào giữa sẽ làm save cũ lệch sang giá trị khác.
    public class SaveCompatibilityTests
    {
        [Test]
        public void CurrencyType_Values_ArePinned()
        {
            Assert.AreEqual(0, (int)CurrencyType.Coin);
            Assert.AreEqual(1, (int)CurrencyType.Gem);
            Assert.AreEqual(2, (int)CurrencyType.Material);
        }

        [Test]
        public void StaffRole_Values_ArePinned()
        {
            Assert.AreEqual(0, (int)StaffRole.Cook);
            Assert.AreEqual(1, (int)StaffRole.Server);
            Assert.AreEqual(2, (int)StaffRole.Cashier);
        }
    }
}
