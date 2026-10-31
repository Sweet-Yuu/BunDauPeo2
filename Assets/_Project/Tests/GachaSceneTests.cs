using System;
using System.Linq;
using BunDauPeo.Boot;
using BunDauPeo.EditorTools;
using BunDauPeo.Gacha;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    public class GachaSceneTests
    {
        // Mở scene từ đĩa (L-002) và chỉ đóng nếu test là bên mở nó.
        private static void WithScene(string path, Action<Scene> check)
        {
            var wasOpen = SceneManager.GetSceneByPath(path).isLoaded;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                check(scene);
            }
            finally
            {
                if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T[] All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void GachaScene_HasCameraAndEventSystemWithInputSystemModule() =>
            WithScene(ProjectSetup.GachaScenePath, scene =>
            {
                Assert.AreEqual(1, All<Camera>(scene).Length);
                Assert.AreEqual(1, All<EventSystem>(scene).Length);
                Assert.AreEqual(1, All<InputSystemUIInputModule>(scene).Length);
                Assert.AreEqual(0, All<StandaloneInputModule>(scene).Length);
            });

        [Test]
        public void GachaScene_ViewIsWired_AndUsesOnlyImageAndText() =>
            WithScene(ProjectSetup.GachaScenePath, scene =>
            {
                Assert.AreEqual(1, All<Canvas>(scene).Length);
                var view = All<GachaView>(scene).Single();
                var so = new SerializedObject(view);
                foreach (var field in new[] { "_banner", "_singleButton", "_tenButton", "_gemText", "_pityText", "_messageText" })
                    Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, field);
                Assert.AreEqual(10, so.FindProperty("_slots").arraySize);

                var banner = (GachaBannerDefinition)so.FindProperty("_banner").objectReferenceValue;
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<GachaBannerDefinition>(ProjectSetup.BannerPath).Id, banner.Id);
                Assert.AreEqual(0, All<RawImage>(scene).Length);
            });

        [Test]
        public void GachaScene_IsSecondInBuildSettings()
        {
            Assert.AreEqual(ProjectSetup.BootScenePath, EditorBuildSettings.scenes[0].path);
            Assert.AreEqual(ProjectSetup.GachaScenePath, EditorBuildSettings.scenes[1].path);
        }

        [Test]
        public void BootScene_StartSceneIsGacha() =>
            WithScene(ProjectSetup.BootScenePath, scene =>
            {
                var bootstrap = All<GameBootstrap>(scene).Single();
                Assert.AreEqual("Gacha", new SerializedObject(bootstrap).FindProperty("_startScene").stringValue);
            });
    }
}
