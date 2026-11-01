using System.Globalization;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class RestaurantViewTextTests
    {
        [Test]
        public void IncomeLine_FormatsRateWithCommaDecimal()
        {
            var saved = CultureInfo.CurrentCulture;
            try
            {
                // Culture dùng dấu chấm (en-US) sẽ cho "1.6" nếu cài đặt không cố định dấu phẩy.
                foreach (var name in new[] { "en-US", "de-DE", "vi-VN" })
                {
                    CultureInfo.CurrentCulture = new CultureInfo(name);
                    var line = RestaurantViewText.IncomeLine(new IncomeEstimate(1.56, null));
                    StringAssert.Contains("1,6", line, name);
                    StringAssert.Contains("Coin/giây", line, name);
                    StringAssert.DoesNotContain("1.6", line, name);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = saved;
            }
        }

        [TestCase(StaffRole.Cook, "Thiếu đầu bếp, quán không kiếm tiền")]
        [TestCase(StaffRole.Server, "Thiếu nhân viên phục vụ, quán không kiếm tiền")]
        [TestCase(StaffRole.Cashier, "Thiếu thu ngân, quán không kiếm tiền")]
        public void IncomeLine_MissingRole_NamesRole(StaffRole role, string expected) =>
            Assert.AreEqual(expected, RestaurantViewText.IncomeLine(new IncomeEstimate(0, role)));

        [Test]
        public void CounterLine_ShowsServedAndAbandoned() =>
            Assert.AreEqual("Phục vụ 12 · Bỏ về 2", RestaurantViewText.CounterLine(12, 2));

        [Test]
        public void InvalidConfigLine_NotEmpty() =>
            Assert.IsNotEmpty(RestaurantViewText.InvalidConfigLine());

        [Test]
        public void StarFill_PartialStar()
        {
            // 3,1 sao: ba ngôi đầy, ngôi thứ tư đầy một phần mười, ngôi cuối trống.
            var expected = new[] { 1f, 1f, 1f, 0.1f, 0f };
            for (var i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], RestaurantViewText.StarFill(3.1, i), 1e-5f, $"star {i}");
        }
    }
}
