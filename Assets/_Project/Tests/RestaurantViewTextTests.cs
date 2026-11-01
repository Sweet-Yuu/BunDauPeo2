using System.Globalization;
using System.Linq;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class RestaurantViewTextTests
    {
        private static ActiveIncident Incident(StaffRole role, string name, bool canResolve) =>
            new ActiveIncident(RestaurantTestData.Incident(role, 1, name), role, 0f, canResolve);

        [Test]
        public void IncidentLabel_NamesDefinition()
        {
            Assert.AreEqual("Bếp tắc ga", RestaurantViewText.IncidentLabel(Incident(StaffRole.Cook, "Bếp tắc ga", true)));
            Assert.AreEqual("Máy tính tiền treo", RestaurantViewText.IncidentLabel(Incident(StaffRole.Cashier, "Máy tính tiền treo", false)));
        }

        [Test]
        public void IncidentHint_AsksForStaff_OnlyWhenRoleEmpty()
        {
            var hints = new[] { StaffRole.Cook, StaffRole.Server, StaffRole.Cashier }
                .Select(role => RestaurantViewText.IncidentHint(Incident(role, "x", false))).ToArray();

            StringAssert.Contains("đầu bếp", hints[0].ToLowerInvariant());
            StringAssert.Contains("phục vụ", hints[1].ToLowerInvariant());
            StringAssert.Contains("thu ngân", hints[2].ToLowerInvariant());
            Assert.AreEqual(3, hints.Distinct().Count());
            foreach (var role in new[] { StaffRole.Cook, StaffRole.Server, StaffRole.Cashier })
                Assert.AreEqual("", RestaurantViewText.IncidentHint(Incident(role, "x", true)));
        }

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

        private static UpgradeDefinition TablesUpgrade()
        {
            var upgrade = RestaurantTestData.Upgrade("up_tables", UpgradeEffect.Tables,
                RestaurantTestData.Level(4, 300, 5), RestaurantTestData.Level(5, 600, 10));
            TestData.Edit(upgrade, so => so.FindProperty("_displayName").stringValue = "Bàn");
            return upgrade;
        }

        [Test]
        public void UpgradeButton_ShowsNameLevelAndCost()
        {
            var upgrade = TablesUpgrade();

            // Giá là của cấp kế tiếp: cấp 0 thấy 300 + 5, cấp 1 thấy 600 + 10.
            var fresh = RestaurantViewText.UpgradeButton(upgrade, 0);
            StringAssert.Contains("Bàn", fresh);
            StringAssert.Contains("0/2", fresh);
            StringAssert.Contains("300 Coin", fresh);
            StringAssert.Contains("5 Material", fresh);

            var next = RestaurantViewText.UpgradeButton(upgrade, 1);
            StringAssert.Contains("1/2", next);
            StringAssert.Contains("600 Coin", next);
            StringAssert.Contains("10 Material", next);
            StringAssert.DoesNotContain("300", next);
        }

        [Test]
        public void UpgradeButton_AtMax_ShowsMax()
        {
            var text = RestaurantViewText.UpgradeButton(TablesUpgrade(), 2);

            StringAssert.Contains("2/2", text);
            StringAssert.Contains("Tối đa", text);
            StringAssert.DoesNotContain("Coin", text);
            StringAssert.DoesNotContain("Material", text);
        }

        [Test]
        public void UpgradeStatus_EveryFailureHasDistinctText_SuccessSavedIsEmpty_SuccessUnsavedWarns()
        {
            Assert.AreEqual("", RestaurantViewText.UpgradeStatus(UpgradeBuyStatus.Success, true));
            Assert.IsNotEmpty(RestaurantViewText.UpgradeStatus(UpgradeBuyStatus.Success, false));

            var texts = new System.Collections.Generic.HashSet<string>
            {
                RestaurantViewText.UpgradeStatus(UpgradeBuyStatus.Success, false),
            };
            foreach (var status in new[] { UpgradeBuyStatus.UnknownUpgrade, UpgradeBuyStatus.InvalidUpgrade, UpgradeBuyStatus.MaxLevel, UpgradeBuyStatus.NotEnoughCurrency })
            {
                var text = RestaurantViewText.UpgradeStatus(status, false);
                Assert.IsNotEmpty(text, status.ToString());
                Assert.IsTrue(texts.Add(text), $"{status} shares its text with another status");
                // Thất bại không lưu gì nên cờ Saved không được đổi chữ.
                Assert.AreEqual(text, RestaurantViewText.UpgradeStatus(status, true), status.ToString());
            }
        }

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
