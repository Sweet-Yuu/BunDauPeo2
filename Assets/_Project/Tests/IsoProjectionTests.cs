using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Ô 120×60 pixel, phép chiếu isometric 2:1 (spec Restaurant 5.1).
    public class IsoProjectionTests
    {
        [Test]
        public void CellToLocal_Origin_IsZero() =>
            Assert.AreEqual(Vector2.zero, IsoProjection.CellToLocal(0, 0));

        [Test]
        public void CellToLocal_StepInX_GoesRightAndDown() =>
            Assert.AreEqual(new Vector2(60, -30), IsoProjection.CellToLocal(1, 0));

        [Test]
        public void CellToLocal_StepInY_GoesLeftAndDown() =>
            Assert.AreEqual(new Vector2(-60, -30), IsoProjection.CellToLocal(0, 1));

        [Test]
        public void CellToLocal_Combined() =>
            Assert.AreEqual(new Vector2(-60, -150), IsoProjection.CellToLocal(2, 3));

        [Test]
        public void DepthKey_NearerCellIsLarger()
        {
            Assert.Greater(IsoProjection.DepthKey(2, 3), IsoProjection.DepthKey(1, 1));
            // Cùng gx + gy thì cùng độ sâu (cùng hàng chéo trên màn hình).
            Assert.AreEqual(IsoProjection.DepthKey(1, 2), IsoProjection.DepthKey(2, 1));
        }
    }
}
