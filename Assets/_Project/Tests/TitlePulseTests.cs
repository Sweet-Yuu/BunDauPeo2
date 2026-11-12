using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class TitlePulseTests
    {
        [Test]
        public void Alpha_StartsFull_StaysInRange_DipsAtHalf()
        {
            Assert.AreEqual(1f, TitlePulse.Alpha(0f), 1e-5f);

            for (var i = 0; i <= 80; i++)
            {
                var t = i * 0.05f;
                var alpha = TitlePulse.Alpha(t);
                Assert.That(alpha, Is.InRange(TitlePulse.MinAlpha - 1e-5f, 1f + 1e-5f), $"t = {t}");
            }

            // Đáy ở nửa chu kỳ, đỉnh lại ở cả chu kỳ, và giữa chừng nằm giữa hai mức (không phải hằng số).
            Assert.AreEqual(TitlePulse.MinAlpha, TitlePulse.Alpha(0.8f), 0.001f);
            Assert.AreEqual(1f, TitlePulse.Alpha(TitlePulse.PeriodSeconds), 0.001f);
            Assert.AreEqual(0.8096f, TitlePulse.Alpha(0.4f), 0.001f);
        }
    }
}
