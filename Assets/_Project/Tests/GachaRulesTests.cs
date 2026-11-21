using System;
using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Rules;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Chép từng ca của GachaRollerTests sang GameData/BannerData (độ hiếm int: 0 R, 1 SR, 2 SSR).
    public class GachaRulesTests
    {
        const int R = 0, SR = 1, SSR = 2;

        GameData _data;
        BannerData _banner;

        // Cùng bộ nhân vật và banner với TestData.GachaCharacters/Banner (giá 100/900, 70/25/5, pity 50, bảo đảm SR).
        internal static GameData Data()
        {
            var data = new GameData
            {
                MaxStars = 5,
                MaxTeamSize = 5,
                StarCosts = new List<int> { 2, 4, 6, 8 },
                Stamina = new StaminaData { Max = 100, RegenSeconds = 300 },
                NewGame = new NewGameData { StartingGem = 1500, StarterCharacterIds = new List<string> { "r0" } },
            };
            foreach (var (id, rarity) in new[] { ("r0", R), ("r1", R), ("r2", R), ("s0", SR), ("s1", SR), ("x0", SSR) })
                data.Characters.Add(new CharacterData { Id = id, Rarity = rarity, BaseStars = 1 });

            data.Banners.Add(new BannerData
            {
                Id = "b",
                SingleCostGem = 100,
                TenCostGem = 900,
                WeightR = 70f,
                WeightSR = 25f,
                WeightSSR = 5f,
                SsrPityThreshold = 50,
                GuaranteeSrInTen = true,
                DupFragmentsR = 1,
                DupFragmentsSR = 1,
                DupFragmentsSSR = 1,
                CharacterIds = new List<string> { "r0", "r1", "r2", "s0", "s1", "x0" },
            });
            return data;
        }

        [SetUp]
        public void SetUp()
        {
            _data = Data();
            _banner = _data.Banners[0];
        }

        int RarityOf(string id) => GachaRules.RarityOf(_data, id);

        void SetWeights(float r, float sr, float ssr)
        {
            _banner.WeightR = r;
            _banner.WeightSR = sr;
            _banner.WeightSSR = ssr;
        }

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
            var result = new GachaRules(rng).RollOne(_data, _banner, ref pulls);
            Assert.AreEqual("r0", result);
            Assert.AreEqual(1, pulls);
            Assert.AreEqual(2, rng.Calls);
        }

        [Test]
        public void RollOne_PityThreshold_ForcesSsrAtPull50()
        {
            var rng = new ScriptedRandom();
            var roller = new GachaRules(rng);
            var pulls = 0;
            for (var i = 0; i < 49; i++) Assert.AreEqual("r0", roller.RollOne(_data, _banner, ref pulls));
            Assert.AreEqual(49, pulls);

            var callsBefore = rng.Calls;
            var forced = roller.RollOne(_data, _banner, ref pulls);
            Assert.AreEqual("x0", forced);
            Assert.AreEqual(0, pulls);
            Assert.AreEqual(1, rng.Calls - callsBefore, "forced SSR must skip the rarity roll");
        }

        [Test]
        public void RollOne_NaturalSsr_ResetsPulls()
        {
            var pulls = 10;
            var result = new GachaRules(new ScriptedRandom(0.0, 0.99, 0.0)).RollOne(_data, _banner, ref pulls);
            Assert.AreEqual("x0", result);
            Assert.AreEqual(0, pulls);
        }

        [Test]
        public void RollOne_ThresholdZero_NeverForces()
        {
            _banner.SsrPityThreshold = 0;
            var pulls = 1000;
            var result = new GachaRules(new ScriptedRandom()).RollOne(_data, _banner, ref pulls);
            Assert.AreEqual("r0", result);
            Assert.AreEqual(1001, pulls);
        }

        [Test]
        public void RollOne_ZeroWeightRarity_NeverChosen()
        {
            SetWeights(0, 25, 5);
            var pulls = 0;
            var result = new GachaRules(new ScriptedRandom()).RollOne(_data, _banner, ref pulls);
            Assert.AreEqual(SR, RarityOf(result));
            Assert.AreEqual("s0", result);
        }

        [Test]
        public void RollOne_MinRaritySr_SkipsLowerRarity()
        {
            var roller = new GachaRules(new ScriptedRandom());
            var pulls = 0;
            Assert.AreEqual("s0", roller.RollOne(_data, _banner, ref pulls, SR));
            Assert.AreEqual(1, pulls);
            Assert.AreEqual("x0", roller.RollOne(_data, _banner, ref pulls, SSR));
            Assert.AreEqual(0, pulls);
        }

        [Test]
        public void RollOne_NextDoubleOne_ReturnsLastValidRarity_WithoutThrowing()
        {
            var pulls = 0;
            SetWeights(70, 25, 0);
            Assert.AreEqual("s1", new GachaRules(new ScriptedRandom(1.0)).RollOne(_data, _banner, ref pulls));

            SetWeights(70, 0, 0);
            Assert.AreEqual("r2", new GachaRules(new ScriptedRandom(1.0)).RollOne(_data, _banner, ref pulls));
        }

        [Test]
        public void RollOne_PicksCharacterByIndex()
        {
            var pulls = 0;
            Assert.AreEqual("r1", new GachaRules(new ScriptedRandom(0.0, 0.0, 0.34)).RollOne(_data, _banner, ref pulls));
            Assert.AreEqual("r2", new GachaRules(new ScriptedRandom(0.0, 0.0, 0.999)).RollOne(_data, _banner, ref pulls));
            Assert.AreEqual("r2", new GachaRules(new ScriptedRandom(0.0, 0.0, 1.0)).RollOne(_data, _banner, ref pulls));
        }

        [Test]
        public void RollTen_PityAcrossBoundary()
        {
            var pulls = 45;
            var results = new GachaRules(new ScriptedRandom()).RollTen(_data, _banner, ref pulls);
            CollectionAssert.AreEqual(
                new[] { "r0", "r0", "r0", "r0", "x0", "r0", "r0", "r0", "r0", "r0" }, results);
            Assert.AreEqual(5, pulls);
        }

        [Test]
        public void RollTen_NoSrInFirstNine_UpgradesTenthToSr()
        {
            var pulls = 0;
            var results = new GachaRules(new ScriptedRandom()).RollTen(_data, _banner, ref pulls);
            Assert.AreEqual(10, results.Count);
            Assert.IsTrue(results.Take(9).All(id => RarityOf(id) == R));
            Assert.AreEqual("s0", results[9]);
            Assert.AreEqual(10, pulls, "an SR does not reset the SSR counter");
        }

        [Test]
        public void RollTen_UpgradedTenthCanBeSsr()
        {
            // 0.9 là SR theo bảng đầy đủ (u = 90) nhưng SSR khi lượt 10 bị nâng (u = 27 trên SR+SSR): chỉ đúng khi có nâng.
            var values = Enumerable.Repeat(0.0, 18).Append(0.9).ToArray();
            var pulls = 0;
            var results = new GachaRules(new ScriptedRandom(0.0, values)).RollTen(_data, _banner, ref pulls);
            Assert.IsTrue(results.Take(9).All(id => RarityOf(id) == R));
            Assert.AreEqual("x0", results[9]);
            Assert.AreEqual(0, pulls);
        }

        [Test]
        public void RollTen_SrAlreadyPresent_DoesNotUpgrade()
        {
            var pulls = 0;
            var rng = new ScriptedRandom(0.0, 0.0, 0.0, 0.0, 0.0, 0.8, 0.0);
            var results = new GachaRules(rng).RollTen(_data, _banner, ref pulls);
            Assert.AreEqual("s0", results[2]);
            Assert.AreEqual(R, RarityOf(results[9]));
        }

        [Test]
        public void RollOne_TenSingles_HaveNoSrGuarantee()
        {
            var roller = new GachaRules(new ScriptedRandom());
            var pulls = 0;
            for (var i = 0; i < 10; i++) Assert.AreEqual(R, RarityOf(roller.RollOne(_data, _banner, ref pulls)));
            Assert.AreEqual(10, pulls);
        }

        [Test]
        public void RollTen_GuaranteeDisabled_AllowsTenRs()
        {
            _banner.GuaranteeSrInTen = false;
            var pulls = 0;
            var results = new GachaRules(new ScriptedRandom()).RollTen(_data, _banner, ref pulls);
            Assert.IsTrue(results.All(id => RarityOf(id) == R));
        }

        [Test]
        public void RollTen_PityForcedSsrInTenthSlot()
        {
            var pulls = 40;
            var results = new GachaRules(new ScriptedRandom()).RollTen(_data, _banner, ref pulls);
            Assert.IsTrue(results.Take(9).All(id => RarityOf(id) == R));
            Assert.AreEqual("x0", results[9]);
            Assert.AreEqual(0, pulls);
        }

        [Test]
        public void Distribution_Rarity_WithinFiveSigma()
        {
            _banner.SsrPityThreshold = 0;
            const int n = 100000;
            var roller = new GachaRules(new Random(12345));
            var counts = new int[3];
            var pulls = 0;
            for (var i = 0; i < n; i++) counts[RarityOf(roller.RollOne(_data, _banner, ref pulls))]++;

            var expected = new[] { 0.70, 0.25, 0.05 };
            for (var r = 0; r < 3; r++)
            {
                var p = expected[r];
                var tolerance = 5 * Math.Sqrt(p * (1 - p) / n);
                Assert.AreEqual(p, counts[r] / (double)n, tolerance, $"rarity {r}");
            }
        }

        [Test]
        public void Distribution_Characters_AreUniformWithinRarity()
        {
            _banner.SsrPityThreshold = 0;
            const int n = 100000;
            var roller = new GachaRules(new Random(54321));
            var byId = _data.Characters.ToDictionary(c => c.Id, _ => 0);
            var pulls = 0;
            for (var i = 0; i < n; i++) byId[roller.RollOne(_data, _banner, ref pulls)]++;

            foreach (var rarity in new[] { R, SR })
            {
                var group = _data.Characters.Where(c => c.Rarity == rarity).ToArray();
                var total = group.Sum(c => byId[c.Id]);
                var p = 1.0 / group.Length;
                var tolerance = 5 * Math.Sqrt(p * (1 - p) / total);
                foreach (var c in group)
                    Assert.AreEqual(p, byId[c.Id] / (double)total, tolerance, c.Id);
            }
        }
    }
}
