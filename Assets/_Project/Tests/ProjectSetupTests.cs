using System;
using System.Linq;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class ProjectSetupTests
    {
        private const string TempAsset = "Assets/_Project/Tests/_TempSetupTest.asset";

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(TempAsset);

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
            var scene = EditorSceneManager.OpenScene(ProjectSetup.BootScenePath, OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects().Select(g => g.GetComponent<GameBootstrap>()).Single(b => b != null);
                var so = new SerializedObject(bootstrap);
                Assert.IsNotNull(so.FindProperty("_catalog").objectReferenceValue);
                Assert.IsNotNull(so.FindProperty("_newGameConfig").objectReferenceValue);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
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
