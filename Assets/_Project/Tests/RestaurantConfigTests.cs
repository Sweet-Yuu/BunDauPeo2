using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;
using UnityEditor;

namespace BunDauPeo.Tests
{
    public class RestaurantConfigTests
    {
        private static RestaurantConfig Edited(System.Action<SerializedObject> set)
        {
            var config = RestaurantTestData.Config();
            TestData.Edit(config, set);
            return config;
        }

        private static void SetNumber(SerializedObject so, string field, double value)
        {
            var property = so.FindProperty(field);
            Assert.IsNotNull(property, field);
            if (property.propertyType == SerializedPropertyType.Integer) property.intValue = (int)value;
            else property.floatValue = (float)value;
        }

        // "_baseTables" -> "BaseTables": lỗi bắt đầu bằng tên property.
        private static string Pascal(string field) => char.ToUpperInvariant(field[1]) + field.Substring(2);

        private static void AssertReports(List<string> errors, string fragment) =>
            Assert.IsTrue(errors.Exists(e => e.Contains(fragment)), $"expected an error containing '{fragment}', got: {string.Join(" | ", errors)}");

        [Test]
        public void Validate_SampleConfig_NoErrors() => CollectionAssert.IsEmpty(RestaurantTestData.Config().Validate());

        [TestCase("_dishes", false, "Dishes: must not be empty")]
        [TestCase("_dishes", true, "Dishes[0]: missing reference")]
        [TestCase("_customers", false, "Customers: must not be empty")]
        [TestCase("_incidents", false, "Incidents: must not be empty")]
        public void Validate_EmptyOrNullList_Reported(string field, bool nullElement, string expected)
        {
            var config = RestaurantTestData.Config();
            RestaurantTestData.SetList(config, field, nullElement ? new UnityEngine.Object[] { null } : new UnityEngine.Object[0]);
            AssertReports(config.Validate(), expected);
        }

        [TestCase("_basePrice", "BasePrice")]
        [TestCase("_cookSeconds", "CookSeconds")]
        public void Validate_NonPositiveDishValue_Reported(string field, string expected)
        {
            var dish = RestaurantTestData.Dish();
            TestData.Edit(dish, so => SetNumber(so, field, 0));
            var config = RestaurantTestData.Config();
            RestaurantTestData.SetList(config, "_dishes", dish);
            AssertReports(config.Validate(), $"Dishes[0].{expected}");
        }

        [TestCase("_spawnWeight", "SpawnWeight")]
        [TestCase("_patienceSeconds", "PatienceSeconds")]
        [TestCase("_payMultiplier", "PayMultiplier")]
        public void Validate_NonPositiveCustomerValue_Reported(string field, string expected)
        {
            var customer = RestaurantTestData.Customer();
            TestData.Edit(customer, so => SetNumber(so, field, 0));
            var config = RestaurantTestData.Config();
            RestaurantTestData.SetList(config, "_customers", customer);
            AssertReports(config.Validate(), $"Customers[0].{expected}");
        }

        [TestCase("_baseArrivalInterval")]
        [TestCase("_serveSeconds")]
        [TestCase("_eatSeconds")]
        [TestCase("_cashierSeconds")]
        [TestCase("_baseTables")]
        [TestCase("_baseCookSlots")]
        public void Validate_NonPositiveBaseValue_Reported(string field) =>
            AssertReports(Edited(so => SetNumber(so, field, 0)).Validate(), $"{Pascal(field)}: must be > 0");

        [Test]
        public void Validate_QualityMaxNotSumTo100_Reported() =>
            AssertReports(Edited(so => SetNumber(so, "_incidentMax", 40)).Validate(), "AttendanceMax + IncidentMax");

        [TestCase(-0.1, 0.5, "AfkFactorMin: must be >= 0")]
        [TestCase(0.6, 0.5, "AfkFactorMin: must be <= AfkFactorMax")]
        [TestCase(0.1, 1.0, "AfkFactorMax: must be < 1")]
        public void Validate_AfkFactor_Reported(double min, double max, string expected) =>
            AssertReports(Edited(so =>
            {
                SetNumber(so, "_afkFactorMin", min);
                SetNumber(so, "_afkFactorMax", max);
            }).Validate(), expected);

        [Test]
        public void Validate_AfkMaxHoursNonPositive_Reported() =>
            AssertReports(Edited(so => SetNumber(so, "_afkMaxHours", 0)).Validate(), "AfkMaxHours");

        [TestCase("_incidentSpawnMin", 130, "IncidentSpawnMin: must be <= IncidentSpawnMax")]
        [TestCase("_incidentFastSeconds", 20, "IncidentFastSeconds: must be < IncidentLateSeconds")]
        public void Validate_IncidentTimes_Reported(string field, double value, string expected) =>
            AssertReports(Edited(so => SetNumber(so, field, value)).Validate(), expected);

        [Test]
        public void Validate_AutosaveNonPositive_Reported() =>
            AssertReports(Edited(so => SetNumber(so, "_autosaveSeconds", 0)).Validate(), "AutosaveSeconds");

        [Test]
        public void Validate_Upgrades_Reported_WhenCatalogIsNull()
        {
            var config = RestaurantTestData.Config();
            TestData.SetRef(config, "_upgrades", null);
            AssertReports(config.Validate(), "Upgrades: missing catalog");
        }

        [Test]
        public void Validate_Upgrades_Reported_WhenTwoShareAnEffect()
        {
            var catalog = RestaurantTestData.UpgradeCatalog(
                RestaurantTestData.Upgrade("a", UpgradeEffect.Tables, RestaurantTestData.Level(4, 1, 1)),
                RestaurantTestData.Upgrade("b", UpgradeEffect.Tables, RestaurantTestData.Level(5, 1, 1)));
            AssertReports(RestaurantTestData.Config(catalog).Validate(), "Upgrades: more than one upgrade has Effect Tables");
        }

        [Test]
        public void Validate_Upgrades_Reported_WhenAnUpgradeIsInvalid()
        {
            var catalog = RestaurantTestData.UpgradeCatalog(RestaurantTestData.Upgrade("up_bad", UpgradeEffect.Tables));
            AssertReports(RestaurantTestData.Config(catalog).Validate(), "Upgrades[0] (up_bad)");
        }
    }
}
