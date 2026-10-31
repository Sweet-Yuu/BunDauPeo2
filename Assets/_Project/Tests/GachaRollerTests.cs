using System;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Gacha;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class GachaRollerTests
    {
        private CharacterDefinition[] _chars;
        private GachaBannerDefinition _banner;

        [SetUp]
        public void SetUp()
        {
            _chars = TestData.GachaCharacters();
            _banner = TestData.Banner("b", _chars);
        }

        private static void SetWeights(GachaBannerDefinition banner, float r, float sr, float ssr) =>
            TestData.Edit(banner, so =>
            {
                var weights = so.FindProperty("_rarityWeights");
                weights.FindPropertyRelative("R").floatValue = r;
                weights.FindPropertyRelative("SR").floatValue = sr;
                weights.FindPropertyRelative("SSR").floatValue = ssr;
            });

        private static void SetPity(GachaBannerDefinition banner, int threshold) =>
            TestData.Edit(banner, so => so.FindProperty("_ssrPityThreshold").intValue = threshold);

        private static string[] Ids(System.Collections.Generic.IReadOnlyList<CharacterDefinition> list) => list.Select(c => c.Id).ToArray();

        [Test]
        public void ScriptedRandom_NextDouble_IsOverridable()
        {
            Random random = new ScriptedRandom(0.0, 0.25, 0.75);
            Assert.AreEqual(0.25, random.NextDouble());
            Assert.AreEqual(0.75, random.NextDouble());
            Assert.AreEqual(0.0, random.NextDouble());
            Assert.AreEqual(3, ((ScriptedRandom)random).Calls);
        }

        [Test]
        public void RollOne_AlwaysZero_GivesFirstR_AndIncrementsPulls()
        {
            var rng = new ScriptedRandom();
            var pulls = 0;
            var result = new GachaRoller(rng).RollOne(_banner, ref pulls);
            Assert.AreEqual("r0", result.Id);
            Assert.AreEqual(1, pulls);
            Assert.AreEqual(2, rng.Calls);
        }

        [Test]
        public void RollOne_PityThreshold_ForcesSsrAtPull50()
        {
            var rng = new ScriptedRandom();
            var roller = new GachaRoller(rng);
            var pulls = 0;
            for (var i = 0; i < 49; i++) Assert.AreEqual("r0", roller.RollOne(_banner, ref pulls).Id);
            Assert.AreEqual(49, pulls);

            var callsBefore = rng.Calls;
            var forced = roller.RollOne(_banner, ref pulls);
            Assert.AreEqual("x0", forced.Id);
            Assert.AreEqual(0, pulls);
            Assert.AreEqual(1, rng.Calls - callsBefore, "forced SSR must skip the rarity roll");
        }

        [Test]
        public void RollOne_NaturalSsr_ResetsPulls()
        {
            var pulls = 10;
            var result = new GachaRoller(new ScriptedRandom(0.0, 0.99, 0.0)).RollOne(_banner, ref pulls);
            Assert.AreEqual("x0", result.Id);
            Assert.AreEqual(0, pulls);
        }

        [Test]
        public void RollOne_ThresholdZero_NeverForces()
        {
            SetPity(_banner, 0);
            var pulls = 1000;
            var result = new GachaRoller(new ScriptedRandom()).RollOne(_banner, ref pulls);
            Assert.AreEqual("r0", result.Id);
            Assert.AreEqual(1001, pulls);
        }

        [Test]
        public void RollOne_ZeroWeightRarity_NeverChosen()
        {
            SetWeights(_banner, 0, 25, 5);
            var pulls = 0;
            var result = new GachaRoller(new ScriptedRandom()).RollOne(_banner, ref pulls);
            Assert.AreEqual(Rarity.SR, result.Rarity);
            Assert.AreEqual("s0", result.Id);
        }

        [Test]
        public void RollOne_MinRaritySr_SkipsLowerRarity()
        {
            var roller = new GachaRoller(new ScriptedRandom());
            var pulls = 0;
            Assert.AreEqual("s0", roller.RollOne(_banner, ref pulls, Rarity.SR).Id);
            Assert.AreEqual(1, pulls);
            Assert.AreEqual("x0", roller.RollOne(_banner, ref pulls, Rarity.SSR).Id);
            Assert.AreEqual(0, pulls);
        }

        [Test]
        public void RollOne_NextDoubleOne_ReturnsLastValidRarity_WithoutThrowing()
        {
            var pulls = 0;
            SetWeights(_banner, 70, 25, 0);
            Assert.AreEqual("s1", new GachaRoller(new ScriptedRandom(1.0)).RollOne(_banner, ref pulls).Id);

            SetWeights(_banner, 70, 0, 0);
            Assert.AreEqual("r2", new GachaRoller(new ScriptedRandom(1.0)).RollOne(_banner, ref pulls).Id);
        }

        [Test]
        public void RollOne_PicksCharacterByIndex()
        {
            var pulls = 0;
            Assert.AreEqual("r1", new GachaRoller(new ScriptedRandom(0.0, 0.0, 0.34)).RollOne(_banner, ref pulls).Id);
            Assert.AreEqual("r2", new GachaRoller(new ScriptedRandom(0.0, 0.0, 0.999)).RollOne(_banner, ref pulls).Id);
            Assert.AreEqual("r2", new GachaRoller(new ScriptedRandom(0.0, 0.0, 1.0)).RollOne(_banner, ref pulls).Id);
        }

        [Test]
        public void RollTen_PityAcrossBoundary()
        {
            var pulls = 45;
            var results = new GachaRoller(new ScriptedRandom()).RollTen(_banner, ref pulls);
            CollectionAssert.AreEqual(
                new[] { "r0", "r0", "r0", "r0", "x0", "r0", "r0", "r0", "r0", "r0" }, Ids(results));
            Assert.AreEqual(5, pulls);
        }

        [Test]
        public void RollTen_NoSrInFirstNine_UpgradesTenthToSr()
        {
            var pulls = 0;
            var results = new GachaRoller(new ScriptedRandom()).RollTen(_banner, ref pulls);
            Assert.AreEqual(10, results.Count);
            Assert.IsTrue(results.Take(9).All(c => c.Rarity == Rarity.R));
            Assert.AreEqual("s0", results[9].Id);
            Assert.AreEqual(10, pulls, "an SR does not reset the SSR counter");
        }

        [Test]
        public void RollTen_UpgradedTenthCanBeSsr()
        {
            // 0.9 là SR theo bảng đầy đủ (u = 90) nhưng SSR khi lượt 10 bị nâng (u = 27 trên SR+SSR): chỉ đúng khi có nâng.
            var values = Enumerable.Repeat(0.0, 18).Append(0.9).ToArray();
            var pulls = 0;
            var results = new GachaRoller(new ScriptedRandom(0.0, values)).RollTen(_banner, ref pulls);
            Assert.IsTrue(results.Take(9).All(c => c.Rarity == Rarity.R));
            Assert.AreEqual("x0", results[9].Id);
            Assert.AreEqual(0, pulls);
        }

        [Test]
        public void RollTen_SrAlreadyPresent_DoesNotUpgrade()
        {
            var pulls = 0;
            var rng = new ScriptedRandom(0.0, 0.0, 0.0, 0.0, 0.0, 0.8, 0.0);
            var results = new GachaRoller(rng).RollTen(_banner, ref pulls);
            Assert.AreEqual("s0", results[2].Id);
            Assert.AreEqual(Rarity.R, results[9].Rarity);
        }

        [Test]
        public void RollOne_TenSingles_HaveNoSrGuarantee()
        {
            var roller = new GachaRoller(new ScriptedRandom());
            var pulls = 0;
            for (var i = 0; i < 10; i++) Assert.AreEqual(Rarity.R, roller.RollOne(_banner, ref pulls).Rarity);
            Assert.AreEqual(10, pulls);
        }

        [Test]
        public void RollTen_GuaranteeDisabled_AllowsTenRs()
        {
            TestData.Edit(_banner, so => so.FindProperty("_guaranteeSrInTen").boolValue = false);
            var pulls = 0;
            var results = new GachaRoller(new ScriptedRandom()).RollTen(_banner, ref pulls);
            Assert.IsTrue(results.All(c => c.Rarity == Rarity.R));
        }

        [Test]
        public void RollTen_PityForcedSsrInTenthSlot()
        {
            var pulls = 40;
            var results = new GachaRoller(new ScriptedRandom()).RollTen(_banner, ref pulls);
            Assert.IsTrue(results.Take(9).All(c => c.Rarity == Rarity.R));
            Assert.AreEqual("x0", results[9].Id);
            Assert.AreEqual(0, pulls);
        }

        [Test]
        public void Distribution_Rarity_WithinFiveSigma()
        {
            SetPity(_banner, 0);
            const int n = 100000;
            var roller = new GachaRoller(new Random(12345));
            var counts = new int[3];
            var pulls = 0;
            for (var i = 0; i < n; i++) counts[(int)roller.RollOne(_banner, ref pulls).Rarity]++;

            var expected = new[] { 0.70, 0.25, 0.05 };
            for (var r = 0; r < 3; r++)
            {
                var p = expected[r];
                var tolerance = 5 * Math.Sqrt(p * (1 - p) / n);
                Assert.AreEqual(p, counts[r] / (double)n, tolerance, $"rarity {(Rarity)r}");
            }
        }

        [Test]
        public void Distribution_Characters_AreUniformWithinRarity()
        {
            SetPity(_banner, 0);
            const int n = 100000;
            var roller = new GachaRoller(new Random(54321));
            var byId = _chars.ToDictionary(c => c.Id, _ => 0);
            var pulls = 0;
            for (var i = 0; i < n; i++) byId[roller.RollOne(_banner, ref pulls).Id]++;

            foreach (var rarity in new[] { Rarity.R, Rarity.SR })
            {
                var group = _chars.Where(c => c.Rarity == rarity).ToArray();
                var total = group.Sum(c => byId[c.Id]);
                var p = 1.0 / group.Length;
                var tolerance = 5 * Math.Sqrt(p * (1 - p) / total);
                foreach (var c in group)
                    Assert.AreEqual(p, byId[c.Id] / (double)total, tolerance, c.Id);
            }
        }
    }
}
