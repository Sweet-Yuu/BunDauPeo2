using System;
using System.Linq;
using System.Reflection;
using BunDauPeo.Characters;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class StarConfigTests
    {
        private static void SetCosts(StarConfig config, params int[] costs) => TestData.Edit(config, so =>
        {
            var array = so.FindProperty("_costs");
            array.arraySize = costs.Length;
            for (var i = 0; i < costs.Length; i++) array.GetArrayElementAtIndex(i).intValue = costs[i];
        });

        [Test]
        public void Defaults_AreValidAndPinned()
        {
            var config = ScriptableObject.CreateInstance<StarConfig>();
            Assert.IsEmpty(config.Validate());
            Assert.AreEqual(2, config.CostToReach(2));
            Assert.AreEqual(4, config.CostToReach(3));
            Assert.AreEqual(6, config.CostToReach(4));
            Assert.AreEqual(8, config.CostToReach(5));
            Assert.AreEqual(10, config.StatPercentPerStar);
            Assert.AreEqual(5, StarConfig.MaxStars);
        }

        [Test]
        public void Validate_ReportsBadCosts()
        {
            var cases = new[] { new int[0], new[] { 2, 4, 6 }, new[] { 2, 4, 6, 8, 10 }, new[] { 2, 0, 6, 8 }, new[] { 2, 4, 6, -1 } };
            foreach (var costs in cases)
            {
                var config = ScriptableObject.CreateInstance<StarConfig>();
                SetCosts(config, costs);
                var errors = config.Validate();
                Assert.IsNotEmpty(errors, $"costs = [{string.Join(",", costs)}]");
                Assert.IsTrue(errors.Any(e => e.Contains("Costs")), $"costs = [{string.Join(",", costs)}]");
            }

            // SerializedObject không gán được null, nên ca null (asset cũ thiếu field) đặt bằng reflection; Validate không được ném.
            var nullCosts = ScriptableObject.CreateInstance<StarConfig>();
            typeof(StarConfig).GetField("_costs", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(nullCosts, null);
            Assert.IsTrue(nullCosts.Validate().Any(e => e.Contains("Costs")), "costs = null");
        }

        [Test]
        public void Validate_ReportsNegativePercent()
        {
            var config = ScriptableObject.CreateInstance<StarConfig>();
            TestData.Edit(config, so => so.FindProperty("_statPercentPerStar").intValue = -1);
            Assert.IsTrue(config.Validate().Any(e => e.Contains("StatPercentPerStar")));

            TestData.Edit(config, so => so.FindProperty("_statPercentPerStar").intValue = 0);
            Assert.IsEmpty(config.Validate());
        }

        [Test]
        public void CostToReach_OutsideTwoToFive_Throws()
        {
            var config = ScriptableObject.CreateInstance<StarConfig>();
            Assert.Throws<ArgumentOutOfRangeException>(() => config.CostToReach(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => config.CostToReach(6));
        }
    }
}
