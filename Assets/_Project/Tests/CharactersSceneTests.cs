using System;
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

namespace BunDauPeo.Tests
{
    public class CharactersSceneTests
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
        public void Scene_HasCameraEventSystemAndView() =>
            WithScene(ProjectSetup.CharactersScenePath, scene =>
            {
                Assert.AreEqual(1, All<Camera>(scene).Length);
                Assert.AreEqual(1, All<EventSystem>(scene).Length);
                Assert.AreEqual(1, All<InputSystemUIInputModule>(scene).Length);
                Assert.AreEqual(0, All<StandaloneInputModule>(scene).Length);
                Assert.AreEqual(0, All<UnityEngine.UI.RawImage>(scene).Length);

                var so = new SerializedObject(All<CharactersView>(scene).Single());
                var it = so.GetIterator();
                it.NextVisible(true);
                while (it.NextVisible(false))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference)
                        Assert.IsNotNull(it.objectReferenceValue, it.name);
                }
                Assert.IsNotNull(so.FindProperty("_cards"), "_cards");
                Assert.Greater(so.FindProperty("_cards").arraySize, 0);

                var names = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Select(t => t.name).ToArray();
                foreach (var n in new[] { "CharacterCard0", "StarUpButton", "StarUpStatus" }) CollectionAssert.Contains(names, n);
            });

        [Test]
        public void Scene_LocalizedTexts_KeysExistAndNotWrittenByViews() =>
            WithScene(ProjectSetup.CharactersScenePath, SceneTextAsserts.AssertLocalizedTexts);
    }
}
