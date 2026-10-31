using System;
using System.Linq;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.EditorTools;
using BunDauPeo.Gacha;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BunDauPeo.Tests
{
    public class ProjectSetupTests
    {
        private const string TempAsset = "Assets/_Project/Tests/_TempSetupTest.asset";
        private const string TempBanner = "Assets/_Project/Tests/_TempBanner.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempAsset);
            AssetDatabase.DeleteAsset(TempBanner);
        }

        [Test]
        public void GetOrCreateAsset_Existing_KeepsItAndSkipsInit()
        {
            var first = ProjectSetup.GetOrCreateAsset<CharacterCatalog>(TempAsset, _ => { });
            var called = false;
            var second = ProjectSetup.GetOrCreateAsset<CharacterCatalog>(TempAsset, _ => called = true);
            Assert.AreSame(first, second);
            Assert.IsFalse(called);
        }

        [Test]
        public void SampleCatalog_IsValid_AndCoversAllRarities()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(ProjectSetup.CatalogPath);
            Assert.IsNotNull(catalog);
            Assert.GreaterOrEqual(catalog.Items.Count, 6);
            CollectionAssert.IsEmpty(catalog.Validate());
            foreach (var c in catalog.Items)
            {
                CollectionAssert.IsEmpty(c.Validate());
                Assert.IsNotNull(c.Portrait);
            }

            foreach (Rarity r in Enum.GetValues(typeof(Rarity)))
                Assert.IsTrue(catalog.Items.Any(c => c.Rarity == r), r.ToString());
        }

        [Test]
        public void NewGameConfig_StartersAreInCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(ProjectSetup.CatalogPath);
            var config = AssetDatabase.LoadAssetAtPath<NewGameConfig>(ProjectSetup.NewGameConfigPath);
            Assert.IsNotEmpty(config.StarterCharacters);
            foreach (var c in config.StarterCharacters) Assert.IsTrue(catalog.TryGet(c.Id, out _), c.Id);
        }

        [Test]
        public void BootScene_IsFirstInBuild_AndBootstrapIsWired()
        {
            Assert.AreEqual(ProjectSetup.BootScenePath, EditorBuildSettings.scenes[0].path);
            // Scene đang mở sẵn trong Editor thì không được đóng nó khi test xong.
            var wasOpen = SceneManager.GetSceneByPath(ProjectSetup.BootScenePath).isLoaded;
            var scene = EditorSceneManager.OpenScene(ProjectSetup.BootScenePath, OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects().Select(g => g.GetComponent<GameBootstrap>()).Single(b => b != null);
                var so = new SerializedObject(bootstrap);
                Assert.IsNotNull(so.FindProperty("_catalog").objectReferenceValue);
                Assert.IsNotNull(so.FindProperty("_newGameConfig").objectReferenceValue);
                Assert.IsNotNull(so.FindProperty("_bannerCatalog").objectReferenceValue);
            }
            finally
            {
                if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void SampleBanner_IsValid_AndInSampleCharacterCatalog()
        {
            var banner = AssetDatabase.LoadAssetAtPath<GachaBannerDefinition>(ProjectSetup.BannerPath);
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(ProjectSetup.CatalogPath);
            Assert.IsNotNull(banner);
            CollectionAssert.IsEmpty(banner.Validate());
            Assert.IsNotEmpty(banner.Id);
            foreach (var c in banner.Characters) Assert.IsTrue(catalog.TryGet(c.Id, out _), c.Id);
            foreach (Rarity r in Enum.GetValues(typeof(Rarity)))
                Assert.IsTrue(banner.Characters.Any(c => c.Rarity == r), r.ToString());
        }

        [Test]
        public void GetOrCreateBanner_NewAsset_HasSpecDefaults()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(ProjectSetup.CatalogPath);
            var banner = ProjectSetup.GetOrCreateBanner(TempBanner, catalog.Items.ToArray());
            Assert.AreEqual("banner_standard", banner.Id);
            Assert.AreEqual(new CurrencyAmount(CurrencyType.Gem, 100), banner.SingleCost.Single());
            Assert.AreEqual(new CurrencyAmount(CurrencyType.Gem, 900), banner.TenCost.Single());
            Assert.AreEqual(70f, banner.RarityWeights.R);
            Assert.AreEqual(25f, banner.RarityWeights.SR);
            Assert.AreEqual(5f, banner.RarityWeights.SSR);
            Assert.AreEqual(50, banner.SsrPityThreshold);
            Assert.IsTrue(banner.GuaranteeSrInTen);
            Assert.AreEqual(1, banner.DuplicateFragments.R);
            Assert.AreEqual(1, banner.DuplicateFragments.SR);
            Assert.AreEqual(1, banner.DuplicateFragments.SSR);
        }

        [Test]
        public void BannerCatalog_ContainsSampleBanner()
        {
            var banner = AssetDatabase.LoadAssetAtPath<GachaBannerDefinition>(ProjectSetup.BannerPath);
            var catalog = AssetDatabase.LoadAssetAtPath<GachaBannerCatalog>(ProjectSetup.BannerCatalogPath);
            Assert.IsNotNull(catalog);
            CollectionAssert.IsEmpty(catalog.Validate());
            Assert.IsTrue(catalog.TryGet(banner.Id, out var found));
            Assert.AreSame(banner, found);
        }

        [Test]
        public void WireBootScene_FillsMissingBannerCatalog()
        {
            try
            {
                SetBootBannerCatalog(null);
                ProjectSetup.WireBootScene();
                Assert.IsNotNull(ReadBootBannerCatalog());
            }
            finally
            {
                ProjectSetup.WireBootScene();
            }
        }

        // Mở scene từ đĩa (L-002): nếu scene chưa mở thì giá trị đọc ra là giá trị đã lưu trong file.
        private static void SetBootBannerCatalog(Object value)
        {
            var wasOpen = SceneManager.GetSceneByPath(ProjectSetup.BootScenePath).isLoaded;
            var scene = EditorSceneManager.OpenScene(ProjectSetup.BootScenePath, OpenSceneMode.Additive);
            try
            {
                var so = new SerializedObject(BootstrapIn(scene));
                so.FindProperty("_bannerCatalog").objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Object ReadBootBannerCatalog()
        {
            var wasOpen = SceneManager.GetSceneByPath(ProjectSetup.BootScenePath).isLoaded;
            var scene = EditorSceneManager.OpenScene(ProjectSetup.BootScenePath, OpenSceneMode.Additive);
            try
            {
                return new SerializedObject(BootstrapIn(scene)).FindProperty("_bannerCatalog").objectReferenceValue;
            }
            finally
            {
                if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameBootstrap BootstrapIn(Scene scene) =>
            scene.GetRootGameObjects().Select(g => g.GetComponent<GameBootstrap>()).Single(b => b != null);

        [Test]
        public void SampleCharacters_StarsMatchRarity()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(ProjectSetup.CatalogPath);
            foreach (var c in catalog.Items) Assert.AreEqual((int)c.Rarity + 1, c.Stars, c.Id);
        }

        [Test]
        public void Run_FillsZeroStars_AndKeepsUserValues()
        {
            const string zeroPath = "Assets/_Project/Characters/Data/Characters/char_nem_chua.asset";
            const string keepPath = "Assets/_Project/Characters/Data/Characters/char_bun_la.asset";
            var zero = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(zeroPath);
            var keep = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(keepPath);
            SetStars(zero, 0);
            SetStars(keep, 4);
            try
            {
                ProjectSetup.Run();
                Assert.AreEqual(1, zero.Stars, "zero stars is filled from rarity");
                Assert.AreEqual(4, keep.Stars, "a user-set value is never overwritten");
            }
            finally
            {
                SetStars(zero, 1);
                SetStars(keep, 3);
            }
        }

        private static void SetStars(CharacterDefinition c, int stars)
        {
            var so = new SerializedObject(c);
            so.FindProperty("_stars").intValue = stars;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void Run_AfterSampleIdWasRenamed_DoesNotThrow()
        {
            const string path = "Assets/_Project/Characters/Data/Characters/char_nem_chua.asset";
            var asset = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            var original = asset.Id;
            SetId(asset, "renamed_by_test");
            try
            {
                Assert.DoesNotThrow(ProjectSetup.Run);
            }
            finally
            {
                SetId(asset, original);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
        }

        private static void SetId(UnityEngine.Object asset, string id)
        {
            var so = new SerializedObject(asset);
            so.FindProperty("_id").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void PlayerSettings_LandscapeOnly()
        {
            Assert.AreEqual(UIOrientation.AutoRotation, PlayerSettings.defaultInterfaceOrientation);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeRight);
        }

        [Test]
        public void PlayerSettings_Identity()
        {
            // Đường dẫn save phụ thuộc hai tên này; đổi sau khi có save thật thì save cũ bị bỏ lại.
            Assert.AreEqual("Sweet-Yuu", PlayerSettings.companyName);
            Assert.AreEqual("BunDauPeo", PlayerSettings.productName);
        }
    }
}
