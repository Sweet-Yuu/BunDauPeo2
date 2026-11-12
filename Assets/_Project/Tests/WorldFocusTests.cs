using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class WorldFocusTests
    {
        [Test]
        public void Center_MidEdgeAndNarrow()
        {
            var content = new Vector2(2800, 1400);
            var viewport = new Vector2(1920, 1080);

            var mid = WorldFocus.Center(content, viewport, new Vector2(0, 0));
            Assert.AreEqual(0.5f, mid.x, 0.001f);
            Assert.AreEqual(0.5f, mid.y, 0.001f);

            // Mép trái của viewport vừa chạm mép trái content: (2800 - 1920) / 2 = 440.
            Assert.AreEqual(0f, WorldFocus.Center(content, viewport, new Vector2(-440, 0)).x, 0.001f);
            Assert.AreEqual(1f, WorldFocus.Center(content, viewport, new Vector2(440, 0)).x, 0.001f);
            Assert.AreEqual(0.25f, WorldFocus.Center(content, viewport, new Vector2(-220, 0)).x, 0.001f, "quarter of the way");
            Assert.AreEqual(0f, WorldFocus.Center(content, viewport, new Vector2(-5000, 0)).x, 0.001f, "clamped low");
            Assert.AreEqual(1f, WorldFocus.Center(content, viewport, new Vector2(5000, 0)).x, 0.001f, "clamped high");

            // Trục dọc tính riêng: (1400 - 1080) / 2 = 160.
            Assert.AreEqual(0f, WorldFocus.Center(content, viewport, new Vector2(0, -160)).y, 0.001f);
            Assert.AreEqual(1f, WorldFocus.Center(content, viewport, new Vector2(0, 5000)).y, 0.001f);

            // Content không rộng hơn viewport theo trục nào thì trục đó là 0,5, dù target lệch.
            var narrow = WorldFocus.Center(new Vector2(1800, 1000), viewport, new Vector2(300, 200));
            Assert.AreEqual(0.5f, narrow.x, 0.001f);
            Assert.AreEqual(0.5f, narrow.y, 0.001f);

            var narrowX = WorldFocus.Center(new Vector2(1800, 1400), viewport, new Vector2(300, -160));
            Assert.AreEqual(0.5f, narrowX.x, 0.001f, "narrow axis");
            Assert.AreEqual(0f, narrowX.y, 0.001f, "wide axis still follows the target");
        }
    }
}
