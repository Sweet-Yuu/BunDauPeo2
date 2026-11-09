using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Hoang.Remake.Tests
{
    public sealed class SkillTreeRemakeTests
    {
        private readonly List<SkillSO> _assets = new();

        [TearDown]
        public void TearDown()
        {
            RestaurantBuffBridge.Shutdown();
            foreach (var asset in _assets) UnityEngine.Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        [Test]
        public void PrerequisitesRequireEveryMinimumLevelBeforeSpecialPointsCanBeSpent()
        {
            var cook = Skill("cook");
            var service = Skill("service");
            var master = Skill("master");
            master.IsExclusive = true;
            master.SpecialPointCost = 2;
            master.Prerequisites = new[]
            {
                new SkillPrerequisite { Skill = cook, MinimumLevel = 2 },
                new SkillPrerequisite { Skill = service, MinimumLevel = 1 },
            };
            var context = Context();
            using var tree = new SkillTreeManager(context, new[] { cook, service, master });
            Assert.That(tree.TryUpgrade("master"), Is.EqualTo(SkillUpgradeStatus.Locked));
            Assert.That(tree.TryUpgrade("cook"), Is.EqualTo(SkillUpgradeStatus.Success));
            Assert.That(tree.TryUpgrade("cook"), Is.EqualTo(SkillUpgradeStatus.Success));
            Assert.That(tree.TryUpgrade("master"), Is.EqualTo(SkillUpgradeStatus.Locked));
            Assert.That(tree.TryUpgrade("service"), Is.EqualTo(SkillUpgradeStatus.Success));
            Assert.That(tree.TryUpgrade("master"), Is.EqualTo(SkillUpgradeStatus.Success));
            Assert.That(tree.AvailablePoints, Is.EqualTo(17));
            Assert.That(tree.AvailableSpecialPoints, Is.EqualTo(1));
        }

        [Test]
        public void FailedPersistenceDoesNotSpendPointsUnlockOrGrantBonuses()
        {
            var cook = Skill("cook");
            cook.ProductionSpeedBonusPerLevel = 0.15f;
            var context = Context(_ => false);
            using var tree = new SkillTreeManager(context, new[] { cook });
            var changed = 0;
            tree.Changed += () => changed++;
            Assert.That(tree.TryUpgrade("cook"), Is.EqualTo(SkillUpgradeStatus.SaveFailed));
            Assert.That(tree.GetLevel("cook"), Is.Zero);
            Assert.That(tree.AvailablePoints, Is.EqualTo(20));
            Assert.That(tree.GetTotalProductionSpeedBonus(), Is.Zero);
            Assert.That(changed, Is.Zero);
        }

        [Test]
        public void ResetRefundsActualPaidCostsAfterConfigurationChangesAndPersistsOnlyOnce()
        {
            var skill = Skill("cook");
            skill.NormalPointCost = 3;
            var writes = 0;
            using var tree = new SkillTreeManager(Context(_ => { writes++; return true; }), new[] { skill });
            Assert.That(tree.TryUpgrade("cook"), Is.EqualTo(SkillUpgradeStatus.Success));
            Assert.That(tree.TryUpgrade("cook"), Is.EqualTo(SkillUpgradeStatus.Success));
            skill.NormalPointCost = 7;
            Assert.That(tree.ResetSkills(), Is.True);
            Assert.That(tree.AvailablePoints, Is.EqualTo(20));
            Assert.That(tree.GetLevel("cook"), Is.Zero);
            Assert.That(writes, Is.EqualTo(3));
            Assert.That(tree.ResetSkills(), Is.True);
            Assert.That(tree.AvailablePoints, Is.EqualTo(20));
        }

        [Test]
        public void MaxLevelAndInsufficientPointsNeverWriteSave()
        {
            var skill = Skill("cook");
            skill.MaxLevel = 1;
            skill.NormalPointCost = 20;
            var other = Skill("other");
            var writes = 0;
            using var tree = new SkillTreeManager(Context(_ => { writes++; return true; }), new[] { skill, other });
            Assert.That(tree.TryUpgrade("cook"), Is.EqualTo(SkillUpgradeStatus.Success));
            Assert.That(tree.TryUpgrade("cook"), Is.EqualTo(SkillUpgradeStatus.MaxLevel));
            Assert.That(tree.TryUpgrade("other"), Is.EqualTo(SkillUpgradeStatus.NotEnoughPoints));
            Assert.That(tree.TryUpgrade("missing"), Is.EqualTo(SkillUpgradeStatus.UnknownSkill));
            Assert.That(writes, Is.EqualTo(1));
        }

        [Test]
        public void SavedLevelsUseStableIdsAndSurviveReconstructingManager()
        {
            var skill = Skill("cook");
            skill.RevenueBonusPerLevel = 0.08f;
            var context = Context();
            using (var first = new SkillTreeManager(context, new[] { skill })) first.TryUpgrade("cook");
            skill.SkillName = "Renamed skill";
            using var reloaded = new SkillTreeManager(context, new[] { skill });
            Assert.That(reloaded.GetLevel("cook"), Is.EqualTo(1));
            Assert.That(reloaded.GetTotalRevenueBonus(), Is.EqualTo(0.08f).Within(0.00001));
            Assert.That(reloaded.AvailablePoints, Is.EqualTo(19));
        }

        [Test]
        public void ConfigurationRejectsCyclesDuplicateIdsAndImpossiblePrerequisites()
        {
            var a = Skill("a");
            var b = Skill("b");
            a.Prerequisites = new[] { new SkillPrerequisite { Skill = b, MinimumLevel = 1 } };
            b.Prerequisites = new[] { new SkillPrerequisite { Skill = a, MinimumLevel = 1 } };
            Assert.Throws<ArgumentException>(() => new SkillTreeManager(Context(), new[] { a, b }));
            a.Prerequisites = Array.Empty<SkillPrerequisite>();
            b.Prerequisites = Array.Empty<SkillPrerequisite>();
            b.Id = "a";
            Assert.Throws<ArgumentException>(() => new SkillTreeManager(Context(), new[] { a, b }));
            b.Id = "b";
            b.Prerequisites = new[] { new SkillPrerequisite { Skill = a, MinimumLevel = 6 } };
            Assert.Throws<ArgumentException>(() => new SkillTreeManager(Context(), new[] { a, b }));
        }

        [Test]
        public void BuffBridgeAggregatesSkillBonusesClampsReductionAndUnbindsOldTree()
        {
            var skill = Skill("cook");
            skill.ProductionSpeedBonusPerLevel = 0.1f;
            skill.IngredientCostReductionPerLevel = 0.6f;
            skill.CleanlinessBonusPerLevel = 2;
            using var tree = new SkillTreeManager(Context(), new[] { skill });
            RestaurantBuffBridge.Initialize(tree, null);
            tree.TryUpgrade("cook");
            tree.TryUpgrade("cook");
            Assert.That(RestaurantBuffBridge.GetTotalCookingSpeedMultiplier(), Is.EqualTo(1.2f).Within(0.00001));
            Assert.That(RestaurantBuffBridge.GetTotalIngredientCostReduction(), Is.EqualTo(0.8f));
            Assert.That(RestaurantBuffBridge.GetTotalCleanlinessBonus(), Is.EqualTo(4));
            RestaurantBuffBridge.Shutdown();
            Assert.That(RestaurantBuffBridge.GetTotalCookingSpeedMultiplier(), Is.EqualTo(1f));
        }

        [Test]
        public void BuffBridgeShutdownUnsubscribesFormerManagerEvents()
        {
            var skill = Skill("cook");
            using var tree = new SkillTreeManager(Context(), new[] { skill });
            RestaurantBuffBridge.Initialize(tree, null);
            var notifications = 0;
            Action listener = () => notifications++;
            RestaurantBuffBridge.OnBuffsChanged += listener;
            try
            {
                tree.TryUpgrade("cook");
                Assert.That(notifications, Is.GreaterThan(0));
                RestaurantBuffBridge.Shutdown();
                notifications = 0;
                Assert.That(tree.TryUpgrade("cook"), Is.EqualTo(SkillUpgradeStatus.Success));
                Assert.That(notifications, Is.Zero, "The former manager must no longer publish bridge events.");
            }
            finally { RestaurantBuffBridge.OnBuffsChanged -= listener; }
        }

        [Test]
        public void EnormousFiniteBonusesNeverOverflowBridgeMultipliersOrCookingDenominator()
        {
            var skill = Skill("extreme");
            skill.ProductionSpeedBonusPerLevel = float.MaxValue;
            skill.RevenueBonusPerLevel = float.MaxValue;
            skill.CustomerPatienceBonusPerLevel = float.MaxValue;
            skill.IngredientCostReductionPerLevel = float.MaxValue;
            using var tree = new SkillTreeManager(Context(), new[] { skill });
            Assert.That(tree.TryUpgrade("extreme"), Is.EqualTo(SkillUpgradeStatus.Success));
            Assert.That(tree.TryUpgrade("extreme"), Is.EqualTo(SkillUpgradeStatus.Success));
            var facility = ScriptableObject.CreateInstance<FacilityItemSO>();
            try
            {
                facility.Id = "extreme-source";
                facility.IsPowerSource = true;
                facility.Stats = new FacilityStats
                {
                    ProductionSpeedBonus = float.MaxValue,
                    RevenueBonus = float.MaxValue,
                    CustomerPatienceBonus = float.MaxValue,
                    IngredientCostReduction = float.MaxValue,
                };
                var context = Context();
                context.Save.Facilities.InventoryItemIds.Add(facility.Id);
                using var facilities = new FacilityManager(context, new[] { facility });
                Assert.That(facilities.TryPlaceFromInventory(facility.Id, Vector2Int.zero, 0), Is.True);
                RestaurantBuffBridge.Initialize(tree, facilities);
                foreach (var multiplier in new[] { RestaurantBuffBridge.GetTotalCookingSpeedMultiplier(),
                             RestaurantBuffBridge.GetTotalRevenueMultiplier(), RestaurantBuffBridge.GetCustomerPatienceMultiplier() })
                {
                    Assert.That(float.IsInfinity(multiplier) || float.IsNaN(multiplier), Is.False);
                    Assert.That(multiplier, Is.GreaterThanOrEqualTo(1f));
                    Assert.That(1.0 / multiplier, Is.GreaterThan(0));
                }
                Assert.That(tree.GetTotalProductionSpeedBonus(), Is.EqualTo(float.MaxValue));
                Assert.That(RestaurantBuffBridge.GetTotalIngredientCostReduction(), Is.EqualTo(0.8f));
            }
            finally { UnityEngine.Object.DestroyImmediate(facility); }
        }

        [Test]
        public void TooltipPlacementFitsNarrowSafeAreasAndExtremelyTallContentAtEveryEdge()
        {
            var safeAreas = new[] { new Rect(0, 0, 80, 40), new Rect(100, 50, 9, 6), new Rect(-200, -100, 1000, 650) };
            var content = new Vector2(420, 1000000);
            foreach (var safe in safeAreas)
            foreach (var pointer in new[] { safe.min, safe.max, safe.center, safe.min - Vector2.one * 1000, safe.max + Vector2.one * 1000 })
            {
                var placed = UITooltipSystem.CalculatePlacement(safe, pointer, content, out var pivot, out var scale);
                var input = $"safe={safe}, pointer={pointer}";
                Assert.That(placed.xMin, Is.GreaterThanOrEqualTo(safe.xMin - 0.001f), input);
                Assert.That(placed.yMin, Is.GreaterThanOrEqualTo(safe.yMin - 0.001f), input);
                Assert.That(placed.xMax, Is.LessThanOrEqualTo(safe.xMax + 0.001f), input);
                Assert.That(placed.yMax, Is.LessThanOrEqualTo(safe.yMax + 0.001f), input);
                Assert.That(scale, Is.GreaterThan(0).And.LessThan(0.1f), input);
                Assert.That(placed.width, Is.EqualTo(content.x * scale).Within(0.001f), input);
                Assert.That(placed.height, Is.EqualTo(content.y * scale).Within(0.001f), input);
                Assert.That(pivot.x, Is.InRange(0, 1));
                Assert.That(pivot.y, Is.InRange(0, 1));
            }
        }

        [Test]
        public void TooltipPlacementFlipsNearBottomRightWithoutShrinkingContentThatAlreadyFits()
        {
            var placed = UITooltipSystem.CalculatePlacement(new Rect(0, 0, 1920, 1080), new Vector2(1910, 10),
                new Vector2(420, 300), out var pivot, out var scale);
            Assert.That(pivot, Is.EqualTo(new Vector2(1, 0)));
            Assert.That(scale, Is.EqualTo(1));
            Assert.That(placed.xMax, Is.LessThan(1910));
            Assert.That(placed.yMin, Is.GreaterThan(10));
            Assert.That(placed.size, Is.EqualTo(new Vector2(420, 300)));
        }

        private SkillSO Skill(string id)
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            skill.Id = id;
            skill.SkillName = id;
            _assets.Add(skill);
            return skill;
        }

        private static RemakeContext Context(Func<HoangSaveData, bool> persist = null) =>
            new(new HoangSaveData(), persist ?? (_ => true));
    }
}
