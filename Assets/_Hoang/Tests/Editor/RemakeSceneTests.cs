using Hoang.Remake.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hoang.Remake.Tests
{
    public sealed class RemakeSceneTests
    {
        [Test]
        public void GeneratedPrefabHasOneOfEachControllerAndAllSerializedUIReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RemakeSceneBuilder.PrefabPath);
            Assert.That(prefab, Is.Not.Null, "Run RemakeSceneBuilder.Build first.");
            Assert.That(prefab.GetComponentsInChildren<RemakeShell>(true).Length, Is.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<SkillTreeUI>(true).Length, Is.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<FacilityGridUI>(true).Length, Is.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<ShopDailyRewardsUI>(true).Length, Is.EqualTo(1));
            Assert.That(prefab.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(prefab.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1920,1080)));
            Assert.That(prefab.GetComponentInChildren<SkillTreeUI>(true).Slots.Count, Is.EqualTo(9));
            var shellState = new SerializedObject(prefab.GetComponent<RemakeShell>());
            var pages = shellState.FindProperty("_pages");
            var navigation = shellState.FindProperty("_navigation");
            Assert.That(pages.arraySize, Is.EqualTo(3));
            Assert.That(navigation.arraySize, Is.EqualTo(3));
            var expectedTabs = new[] { "Skill tree", "Kitchen grid", "Shop & daily" };
            for (var i = 0; i < expectedTabs.Length; i++)
            {
                var page = (GameObject)pages.GetArrayElementAtIndex(i).objectReferenceValue;
                var button = (Button)navigation.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(page.name, Is.EqualTo(expectedTabs[i]));
                Assert.That(page.activeSelf, Is.EqualTo(i == 0));
                Assert.That(button.GetComponentInChildren<Text>(true).text, Does.StartWith(expectedTabs[i]));
            }
            foreach (var component in new Component[]
            {
                prefab.GetComponent<RemakeShell>(), prefab.GetComponentInChildren<ShopDailyRewardsUI>(true),
                prefab.GetComponentInChildren<FacilityGridUI>(true), prefab.GetComponentInChildren<SkillTreeUI>(true),
            }) AssertObjectReferences(component);
        }

        [Test]
        public void GeneratedSceneHasIsolatedBootstrapInputSystemAndNoMissingScripts()
        {
            var scene = SceneManager.GetSceneByPath(RemakeSceneBuilder.ScenePath);
            var wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(RemakeSceneBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var bootstrapCount = 0;
                var eventSystems = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    bootstrapCount += root.GetComponentsInChildren<RemakeBootstrap>(true).Length;
                    Assert.That(root.GetComponentsInChildren<BunDauPeo.Boot.GameBootstrap>(true), Is.Empty);
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, transform.name);
                    foreach (var system in root.GetComponentsInChildren<EventSystem>(true))
                    {
                        eventSystems++;
                        Assert.That(system.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
                    }
                    var bootstrap = root.GetComponent<RemakeBootstrap>();
                    if (bootstrap != null) AssertObjectReferences(bootstrap);
                }
                Assert.That(bootstrapCount, Is.EqualTo(1));
                Assert.That(eventSystems, Is.EqualTo(1));
            }
            finally { if (!wasLoaded) EditorSceneManager.CloseScene(scene, true); }
        }

        private static void AssertObjectReferences(Component component)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            while (property.NextVisible(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                    Assert.That(property.objectReferenceValue, Is.Not.Null, component.GetType().Name + "." + property.propertyPath);
        }
    }
}
