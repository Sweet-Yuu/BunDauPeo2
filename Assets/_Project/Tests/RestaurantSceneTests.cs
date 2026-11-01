using System;
using System.IO;
using System.Linq;
using BunDauPeo.EditorTools;
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
    public class RestaurantSceneTests
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

        // Mọi tham chiếu [SerializeField] của component phải có giá trị; mảng không rỗng và không phần tử null.
        private static void AssertAllReferencesAssigned(Component component)
        {
            var so = new SerializedObject(component);
            var property = so.GetIterator();
            var checkedAny = false;
            for (var enter = true; property.NextVisible(enter); enter = false)
            {
                if (property.propertyPath == "m_Script") continue;
                checkedAny = true;
                if (property.isArray)
                {
                    Assert.Greater(property.arraySize, 0, $"{component.GetType().Name}.{property.propertyPath} is empty");
                    for (var i = 0; i < property.arraySize; i++)
                        Assert.IsNotNull(property.GetArrayElementAtIndex(i).objectReferenceValue, $"{component.GetType().Name}.{property.propertyPath}[{i}]");
                }
                else
                {
                    Assert.AreEqual(SerializedPropertyType.ObjectReference, property.propertyType, property.propertyPath);
                    Assert.IsNotNull(property.objectReferenceValue, $"{component.GetType().Name}.{property.propertyPath}");
                }
            }

            Assert.IsTrue(checkedAny, component.GetType().Name);
        }

        [Test]
        public void RestaurantScene_HasCameraAndEventSystemWithInputSystemModule() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                Assert.AreEqual(1, All<Camera>(scene).Length);
                Assert.AreEqual(1, All<EventSystem>(scene).Length);
                Assert.AreEqual(1, All<InputSystemUIInputModule>(scene).Length);
                Assert.AreEqual(0, All<StandaloneInputModule>(scene).Length);
            });

        [Test]
        public void RestaurantScene_ViewAndHudAreWired_AndUseOnlyImageAndText() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                Assert.AreEqual(1, All<Canvas>(scene).Length);
                AssertAllReferencesAssigned(All<RestaurantView>(scene).Single());
                AssertAllReferencesAssigned(All<RestaurantHud>(scene).Single());
                Assert.AreEqual(0, All<RawImage>(scene).Length);
            });

        [Test]
        public void RestaurantScene_StaffPanelWired() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene => AssertAllReferencesAssigned(All<StaffPanel>(scene).Single()));

        [Test]
        public void RestaurantScene_IsThirdInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.GreaterOrEqual(scenes.Length, 3);
            Assert.AreEqual(ProjectSetup.BootScenePath, scenes[0].path);
            Assert.AreEqual(ProjectSetup.GachaScenePath, scenes[1].path);
            Assert.AreEqual(ProjectSetup.RestaurantScenePath, scenes[2].path);
            Assert.IsTrue(scenes[2].enabled);
        }

        [Test]
        public void DevOverlay_SceneTargetsAreInBuildSettings()
        {
            var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray();
            Assert.IsNotEmpty(DevOverlay.SceneNames);
            foreach (var name in DevOverlay.SceneNames) CollectionAssert.Contains(enabled, name);
        }
    }
}
