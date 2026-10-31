using System;
using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using NUnit.Framework;
using UnityEditor;

namespace BunDauPeo.Tests
{
    public class GachaBannerTests
    {
        private static GachaBannerDefinition Default() => TestData.Banner("b", TestData.GachaCharacters());

        private static GachaBannerDefinition Edited(Action<SerializedObject> set)
        {
            var banner = Default();
            TestData.Edit(banner, set);
            return banner;
        }

        private static void SetCost(SerializedObject so, string field, params CurrencyAmount[] lines)
        {
            var array = so.FindProperty(field);
            array.arraySize = lines.Length;
            for (var i = 0; i < lines.Length; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Type").enumValueIndex = (int)lines[i].Type;
                element.FindPropertyRelative("Amount").longValue = lines[i].Amount;
            }
        }

        private static void SetWeights(SerializedObject so, float r, float sr, float ssr)
        {
            var weights = so.FindProperty("_rarityWeights");
            weights.FindPropertyRelative("R").floatValue = r;
            weights.FindPropertyRelative("SR").floatValue = sr;
            weights.FindPropertyRelative("SSR").floatValue = ssr;
        }

        private static void AssertReports(List<string> errors, string fragment) =>
            Assert.IsTrue(errors.Exists(e => e.Contains(fragment)), $"expected an error containing '{fragment}', got: {string.Join(" | ", errors)}");

        [Test]
        public void Validate_Default_NoErrors() => CollectionAssert.IsEmpty(Default().Validate());

        [Test]
        public void Validate_BlankId_Reported() =>
            AssertReports(TestData.Banner("", TestData.GachaCharacters()).Validate(), "Id");

        [Test]
        public void Validate_EmptyCost_Reported() =>
            AssertReports(Edited(so => SetCost(so, "_singleCost")).Validate(), "SingleCost");

        [Test]
        public void Validate_ZeroTotalCost_Reported() =>
            AssertReports(Edited(so => SetCost(so, "_tenCost", new CurrencyAmount(CurrencyType.Gem, 0))).Validate(), "TenCost");

        [Test]
        public void Validate_NegativeCostLine_Reported() =>
            // Có một dòng dương để lỗi "tổng bằng 0" không che việc kiểm dòng âm.
            AssertReports(Edited(so => SetCost(so, "_singleCost",
                new CurrencyAmount(CurrencyType.Gem, 100), new CurrencyAmount(CurrencyType.Coin, -1))).Validate(), "negative");

        [Test]
        public void Validate_NegativeRarityWeight_Reported() =>
            AssertReports(Edited(so => SetWeights(so, 70, -1, 5)).Validate(), "RarityWeights");

        [Test]
        public void Validate_AllZeroRarityWeights_Reported() =>
            AssertReports(Edited(so => SetWeights(so, 0, 0, 0)).Validate(), "RarityWeights");

        [Test]
        public void Validate_NegativeFragments_Reported() =>
            AssertReports(Edited(so => so.FindProperty("_duplicateFragments").FindPropertyRelative("R").intValue = -1).Validate(),
                "DuplicateFragments");

        [Test]
        public void Validate_NullCharacter_Reported() =>
            AssertReports(Edited(so =>
            {
                var array = so.FindProperty("_characters");
                array.arraySize++;
                array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = null;
            }).Validate(), "Characters");

        [Test]
        public void Validate_RarityWithWeightButNoCharacter_Reported()
        {
            var chars = TestData.GachaCharacters();
            var banner = TestData.Banner("b", chars[0], chars[1], chars[2]);
            var errors = banner.Validate();
            AssertReports(errors, "Characters (SR)");
            AssertReports(errors, "Characters (SSR)");
        }

        [Test]
        public void Validate_PityWithoutSsrCharacter_Reported()
        {
            var chars = TestData.GachaCharacters();
            var banner = TestData.Banner("b", chars[0], chars[3]);
            TestData.Edit(banner, so => SetWeights(so, 70, 25, 0));
            AssertReports(banner.Validate(), "SsrPityThreshold");
        }

        [Test]
        public void Validate_GuaranteeSrInTen_WithSrAndSsrWeightsZero_Reported() =>
            AssertReports(Edited(so =>
            {
                SetWeights(so, 100, 0, 0);
                so.FindProperty("_ssrPityThreshold").intValue = 0;
            }).Validate(), "GuaranteeSrInTen");

        [Test]
        public void Properties_ExposeSerializedValues()
        {
            var banner = Default();
            Assert.AreEqual(CurrencyType.Gem, banner.SingleCost[0].Type);
            Assert.AreEqual(100, banner.SingleCost[0].Amount);
            Assert.AreEqual(900, banner.TenCost[0].Amount);
            Assert.AreEqual(25f, banner.RarityWeights.SR);
            Assert.AreEqual(5f, banner.RarityWeights.Get(Rarity.SSR));
            Assert.AreEqual(50, banner.SsrPityThreshold);
            Assert.IsTrue(banner.GuaranteeSrInTen);
            Assert.AreEqual(1, banner.DuplicateFragments.Get(Rarity.R));
            Assert.AreEqual(6, banner.Characters.Count);
            Assert.AreEqual("Banner Thường", banner.DisplayName);
        }
    }
}
