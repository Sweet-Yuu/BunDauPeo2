using System.Linq;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    // Kiểm LocalizedText của một scene đã dựng: có, key có trong bảng, và không đè chữ mà view ghi lúc chạy.
    internal static class SceneTextAsserts
    {
        internal static void AssertLocalizedTexts(Scene scene)
        {
            var localized = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LocalizedText>(true)).ToArray();
            Assert.Greater(localized.Length, 0, "scene has no LocalizedText");

            var keys = UiText.AllEntries.Select(e => e.Key).ToHashSet();
            foreach (var item in localized) Assert.IsTrue(keys.Contains(item.Key), $"{Path(item.gameObject)}: key '{item.Key}' not in UiText");

            var owned = localized.Select(l => l.GetComponent<Text>()).ToHashSet();
            foreach (var behaviour in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if (behaviour == null) continue;
                var property = new SerializedObject(behaviour).GetIterator();
                for (var enter = true; property.NextVisible(enter); enter = false)
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue is not Text text) continue;
                    Assert.IsFalse(owned.Contains(text), $"{Path(text.gameObject)} has LocalizedText but {behaviour.GetType().Name}.{property.propertyPath} writes it at runtime");
                }
            }
        }

        private static string Path(GameObject go) => string.Join("/", go.GetComponentsInParent<Transform>(true).Reverse().Select(t => t.name));
    }
}
