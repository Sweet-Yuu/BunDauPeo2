using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Lưới của màn quán (spec Restaurant 5.1): sàn chữ nhật xoay khoảng −11,3°; gx đi sang phải và hơi xuống, gy đi xuống và hơi sang trái.
    public class IsoProjectionTests
    {
        [Test]
        public void CellToLocal_Origin_IsZero() =>
            Assert.AreEqual(Vector2.zero, IsoProjection.CellToLocal(0, 0));

        [Test]
        public void CellToLocal_StepInX_GoesRightAndSlightlyDown() =>
            Assert.AreEqual(new Vector2(90, -18), IsoProjection.CellToLocal(1, 0));

        [Test]
        public void CellToLocal_StepInY_GoesDownAndSlightlyLeft() =>
            Assert.AreEqual(new Vector2(-13, -65), IsoProjection.CellToLocal(0, 1));

        [Test]
        public void CellToLocal_Combined()
        {
            Assert.AreEqual(new Vector2(141, -231), IsoProjection.CellToLocal(2, 3));
            // Hai trục vuông góc: nếu không thì sàn xoay một góc không còn khớp với lưới.
            var dot = Vector2.Dot(IsoProjection.CellToLocal(1, 0), IsoProjection.CellToLocal(0, 1));
            Assert.AreEqual(0f, dot, 1e-3f);
        }

        [Test]
        public void DepthKey_NearerCellIsLarger()
        {
            Assert.Greater(IsoProjection.DepthKey(2, 3), IsoProjection.DepthKey(1, 1));
            // Một hàng xuống thấp hơn trên màn hình hơn hai cột sang phải, nên gần hơn: DepthKey bám theo toạ độ y, không theo gx + gy.
            Assert.Greater(IsoProjection.DepthKey(0, 1), IsoProjection.DepthKey(2, 0));
            Assert.AreEqual(18f * 3 + 65f * 2, IsoProjection.DepthKey(3, 2), 1e-4f);
        }
    }
}
