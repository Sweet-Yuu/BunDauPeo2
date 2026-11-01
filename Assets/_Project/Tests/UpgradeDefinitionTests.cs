using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class UpgradeDefinitionTests
    {
        private static UpgradeLevelDef Raw(float value, params CurrencyAmount[] cost) =>
            new UpgradeLevelDef { Value = value, Cost = cost };

        private static void AssertReports(List<string> errors, string fragment) =>
            Assert.IsTrue(errors.Exists(e => e.Contains(fragment)), $"expected an error containing '{fragment}', got: {string.Join(" | ", errors)}");

        [Test]
        public void Validate_Valid_NoErrors() =>
            CollectionAssert.IsEmpty(RestaurantTestData.Upgrade("up_tables", UpgradeEffect.Tables,
                RestaurantTestData.Level(4, 300, 5), RestaurantTestData.Level(5, 600, 10)).Validate());

        [Test]
        public void Validate_BlankId_Reported() =>
            AssertReports(RestaurantTestData.Upgrade("", UpgradeEffect.Tables, RestaurantTestData.Level(4, 300, 5)).Validate(), "Id");

        [Test]
        public void Validate_NoLevels_Reported() =>
            AssertReports(RestaurantTestData.Upgrade("u", UpgradeEffect.Tables).Validate(), "Levels");

        [Test]
        public void Validate_LevelWithEmptyCost_Reported() =>
            AssertReports(RestaurantTestData.Upgrade("u", UpgradeEffect.Tables, Raw(4)).Validate(), "Levels[0].Cost: must not be empty");

        [Test]
        public void Validate_LevelWithZeroTotalCost_Reported() =>
            AssertReports(RestaurantTestData.Upgrade("u", UpgradeEffect.Tables, RestaurantTestData.Level(4, 0, 0)).Validate(), "total cost is 0");

        [Test]
        public void Validate_LevelWithNegativeCostLine_Reported()
        {
            // Tổng là 4 (> 0), nên chỉ lỗi "dòng âm" mới bắt được ca này, không phải lỗi "tổng bằng 0".
            var upgrade = RestaurantTestData.Upgrade("u", UpgradeEffect.Tables,
                Raw(4, new CurrencyAmount(CurrencyType.Coin, 5), new CurrencyAmount(CurrencyType.Material, -1)));
            var errors = upgrade.Validate();
            AssertReports(errors, "negative");
            Assert.IsFalse(errors.Exists(e => e.Contains("total cost is 0")));
        }

        [Test]
        public void Validate_NonPositiveValue_Reported() =>
            AssertReports(RestaurantTestData.Upgrade("u", UpgradeEffect.Tables, RestaurantTestData.Level(0, 300, 5)).Validate(), "Levels[0].Value");

        [Test]
        public void Validate_CountEffectWithFractionalValue_Reported()
        {
            AssertReports(RestaurantTestData.Upgrade("u", UpgradeEffect.Tables, RestaurantTestData.Level(4.5f, 300, 5)).Validate(), "whole number");
            CollectionAssert.IsEmpty(RestaurantTestData.Upgrade("u", UpgradeEffect.ArrivalInterval, RestaurantTestData.Level(0.9f, 300, 5)).Validate());
        }

        [Test]
        public void MaxLevel_IsLevelsLength()
        {
            var upgrade = RestaurantTestData.Upgrade("u", UpgradeEffect.DishPrice,
                RestaurantTestData.Level(1.1f, 1, 1), RestaurantTestData.Level(1.2f, 2, 2), RestaurantTestData.Level(1.3f, 3, 3));
            Assert.AreEqual(3, upgrade.MaxLevel);
            Assert.AreEqual(3, upgrade.Levels.Count);
            Assert.AreEqual(1.2f, upgrade.Levels[1].Value);
        }
    }
}
