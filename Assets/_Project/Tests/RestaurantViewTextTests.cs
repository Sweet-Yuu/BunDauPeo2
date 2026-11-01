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
        public void AfkButton_ShowsPreviewCoins()
        {
            var big = RestaurantViewText.AfkButton(10286);
            var small = RestaurantViewText.AfkButton(5);

            StringAssert.Contains("Thu AFK", big);
            StringAssert.Contains("10286 Coin", big);
            StringAssert.Contains("5 Coin", small);
            Assert.AreNotEqual(big, small);
        }

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
        public void AbsenceText_HoursAndMinutes()
        {
            Assert.AreEqual("5 giờ 12 phút", RestaurantViewText.AbsenceText(18720));
            // Phút lẻ không làm tròn lên: 3 giờ 7 phút 59 giây vẫn là 7 phút.
            Assert.AreEqual("3 giờ 7 phút", RestaurantViewText.AbsenceText(3 * 3600 + 7 * 60 + 59));
            Assert.AreEqual("12 giờ 0 phút", RestaurantViewText.AbsenceText(12 * 3600));
        }

        [Test]
        public void AbsenceText_UnderAnHour_MinutesOnly()
        {
            Assert.AreEqual("25 phút", RestaurantViewText.AbsenceText(25 * 60 + 40));
            StringAssert.DoesNotContain("giờ", RestaurantViewText.AbsenceText(59 * 60 + 59));
        }

        [Test]
        public void FullCapLine_OnlyWhenFull()
        {
            Assert.AreEqual("", RestaurantViewText.FullCapLine(false));
            StringAssert.Contains("Đã đầy", RestaurantViewText.FullCapLine(true));
            StringAssert.Contains("thu thập", RestaurantViewText.FullCapLine(true));
        }

        [Test]
        public void DayCellFill_IsCredit()
        {
            Assert.AreEqual(0.4f, RestaurantViewText.DayCellFill(0.4f), 1e-5f);
            Assert.AreEqual(0f, RestaurantViewText.DayCellFill(0f), 1e-5f);
            // Ngoài 0..1 thì kẹp lại, để Image.Filled không nhận số lạ.
            Assert.AreEqual(1f, RestaurantViewText.DayCellFill(1.7f), 1e-5f);
            Assert.AreEqual(0f, RestaurantViewText.DayCellFill(-0.3f), 1e-5f);
        }

        [Test]
        public void TodayLine_ShowsProgressAndNextDayNote()
        {
            // 170 giây là 2 phút 50 giây: phút đã đủ là 2, làm tròn sẽ ra 3.
            var line = RestaurantViewText.TodayLine(170, 300);
            StringAssert.Contains("Hôm nay", line);
            StringAssert.Contains("2/5 phút", line);
            StringAssert.Contains("tính vào ngày mai", line);
            StringAssert.Contains("0/10 phút", RestaurantViewText.TodayLine(0, 600));
        }

        [Test]
        public void IncidentBreakdown_CountsKindLateMissedAndEmpty() =>
            Assert.AreEqual("Kịp 2 · Trễ 1 · Bỏ lỡ 1 · Chưa có 6", RestaurantViewText.IncidentBreakdown(new[] { 2, 2, 1, 0 }));

        [Test]
        public void IncidentBreakdown_OnlyLatestTenCount()
        {
            // Mười hai mục: hai mục cũ nhất (Kịp) rơi ngoài cửa sổ 10, nên không còn "Chưa có".
            var scores = new[] { 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 };
            Assert.AreEqual("Kịp 0 · Trễ 2 · Bỏ lỡ 8 · Chưa có 0", RestaurantViewText.IncidentBreakdown(scores));
        }

        [Test]
        public void SaveWarning_OnlyWhenNotSaved()
        {
            Assert.AreEqual("", RestaurantViewText.SaveWarning(true));
            Assert.IsNotEmpty(RestaurantViewText.SaveWarning(false));
            // Mua nâng cấp không lưu được dùng cùng một câu.
            Assert.AreEqual(RestaurantViewText.SaveWarning(false), RestaurantViewText.UpgradeStatus(UpgradeBuyStatus.Success, false));
        }

        [Test]
        public void AfkSummary_ShowsAbsenceTheoreticalQualityFactorAndCoins()
        {
            var saved = CultureInfo.CurrentCulture;
            try
            {
                foreach (var name in new[] { "en-US", "de-DE" })
                {
                    CultureInfo.CurrentCulture = new CultureInfo(name);
                    // 29203,7 Coin lý thuyết: bỏ phần lẻ chứ không làm tròn lên 29204.
                    var text = RestaurantViewText.AfkSummary(18720, 29203.7, 62, 0.35, 10221);
                    StringAssert.Contains("5 giờ 12 phút", text, name);
                    StringAssert.Contains("29203 Coin", text, name);
                    StringAssert.Contains("62/100", text, name);
                    StringAssert.Contains("3,1 sao", text, name);
                    StringAssert.Contains("0,35", text, name);
                    StringAssert.Contains("10221 Coin", text, name);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = saved;
            }
        }

        [Test]
        public void AfkCollectedLine_ShowsCoinsReceived()
        {
            StringAssert.Contains("10221 Coin", RestaurantViewText.AfkCollectedLine(10221));
            StringAssert.Contains("5 Coin", RestaurantViewText.AfkCollectedLine(5));
            Assert.AreNotEqual(RestaurantViewText.AfkCollectedLine(10221), RestaurantViewText.AfkCollectedLine(5));
        }

        [Test]
        public void TipLine_UsesDailyGoalMinutes()
        {
            StringAssert.Contains("5 phút", RestaurantViewText.TipLine(300));
            StringAssert.Contains("10 phút", RestaurantViewText.TipLine(600));
        }

        [Test]
        public void QualityPopupLines_RoundPointsAndNameDays()
        {
            Assert.AreEqual("Chuyên cần 31/50", RestaurantViewText.AttendanceLine(31.4, 50));
            // 19,6 làm tròn lên 20; cắt bỏ phần lẻ sẽ ra 19.
            Assert.AreEqual("Xử lý sự cố 20/50", RestaurantViewText.IncidentPointsLine(19.6, 50));
            Assert.AreEqual("Hệ số AFK ×0,35", RestaurantViewText.FactorLine(0.35));
            Assert.AreEqual("Hệ số AFK ×0,50", RestaurantViewText.FactorLine(0.5));
            Assert.AreEqual("Hôm qua", RestaurantViewText.DayCellLabel(1));
            Assert.AreEqual("7 ngày", RestaurantViewText.DayCellLabel(7));
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
