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

        private static RectTransform Named(Scene scene, string name) =>
            All<RectTransform>(scene).First(r => r.name == name);

        // Toạ độ trong hệ của sàn (gốc ở góc trái trên của sàn, trục theo sàn): người, đồ và điểm mốc cùng góc xoay nên so được trực tiếp.
        private static Vector2 InFloor(RectTransform floor, RectTransform target) =>
            floor.InverseTransformPoint(target.position);

        private static Rect FloorRect(RectTransform floor, RectTransform target)
        {
            var center = InFloor(floor, target);
            return new Rect(center - target.rect.size / 2f, target.rect.size);
        }

        [Test]
        public void RestaurantScene_LandmarksAreInsideFloor() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var floor = Named(scene, "Floor");
                var view = new SerializedObject(All<RestaurantView>(scene).Single());
                // Gốc ở góc trái trên của sàn (pivot 0, 1); thêm nửa pixel để cửa nằm đúng trên cạnh trước vẫn đạt.
                var inside = new Rect(floor.rect.position, floor.rect.size);
                inside.xMin -= 0.5f; inside.yMin -= 0.5f; inside.xMax += 0.5f; inside.yMax += 0.5f;

                var landmarks = new System.Collections.Generic.List<(string Name, RectTransform Spot)>();
                foreach (var field in new[] { "_tables", "_cookSpots", "_serverSpots", "_cashierSpots" })
                {
                    var array = view.FindProperty(field);
                    for (var i = 0; i < array.arraySize; i++)
                        landmarks.Add(($"{field}[{i}]", (RectTransform)array.GetArrayElementAtIndex(i).objectReferenceValue));
                }

                landmarks.Add(("_door", (RectTransform)view.FindProperty("_door").objectReferenceValue));
                Assert.AreEqual(8 + 3 + 3 + 3 + 1, landmarks.Count);
                foreach (var (name, spot) in landmarks)
                    Assert.IsTrue(inside.Contains(InFloor(floor, spot)), $"{name} at {InFloor(floor, spot)} is outside the floor {floor.rect}");
            });

        [Test]
        public void RestaurantScene_FurnitureDoesNotOverlap() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var floor = Named(scene, "Floor");
                var names = new[] { "Kitchen", "Counter" }.Concat(Enumerable.Range(0, 8).Select(i => $"Table{i}")).ToArray();
                var rects = names.Select(n => (Name: n, Rect: FloorRect(floor, Named(scene, n)))).ToArray();
                for (var i = 0; i < rects.Length; i++)
                {
                    for (var j = i + 1; j < rects.Length; j++)
                    {
                        // Cạnh chạm nhau thì không tính là đè; chỉ cần lấn quá một pixel là lỗi.
                        var a = rects[i].Rect;
                        var b = rects[j].Rect;
                        var overlapX = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                        var overlapY = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                        Assert.IsFalse(overlapX > 1f && overlapY > 1f, $"{rects[i].Name} overlaps {rects[j].Name}");
                    }
                }
            });

        // Rect trong hệ Canvas tham chiếu 1920×1080 (gốc dưới trái), tính từ neo và offset đã lưu: không phụ thuộc kích thước Game view lúc mở scene.
        private static Rect InReferenceCanvas(RectTransform rt)
        {
            var parent = rt.parent.GetComponent<Canvas>() != null ? new Rect(0, 0, 1920, 1080) : InReferenceCanvas((RectTransform)rt.parent);
            var anchorMin = parent.min + Vector2.Scale(rt.anchorMin, parent.size);
            var anchorMax = parent.min + Vector2.Scale(rt.anchorMax, parent.size);
            var size = anchorMax - anchorMin + rt.sizeDelta;
            var pivotPosition = anchorMin + Vector2.Scale(rt.pivot, anchorMax - anchorMin) + rt.anchoredPosition;
            return new Rect(pivotPosition - Vector2.Scale(rt.pivot, size), size);
        }

        [Test]
        public void RestaurantScene_HudAndColumnInsideCanvas() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var canvas = new Rect(0, 0, 1920, 1080);
                foreach (var name in new[] { "CoinCard", "MaterialCard", "GemCard", "RightColumn" })
                {
                    var rect = InReferenceCanvas(Named(scene, name));
                    Assert.IsTrue(canvas.Contains(rect.min) && canvas.Contains(rect.max - Vector2.one), $"{name} {rect} leaves the 1920x1080 canvas");
                }

                var column = Named(scene, "RightColumn");
                var staff = Named(scene, "StaffButton");
                Assert.AreSame(column, staff.parent, "StaffButton belongs to the right column");
                var staffRect = InReferenceCanvas(staff);
                Assert.GreaterOrEqual(Mathf.Min(staffRect.width, staffRect.height), 110f, "touch size");
                Assert.IsTrue(InReferenceCanvas(column).Contains(staffRect.min) && InReferenceCanvas(column).Contains(staffRect.max - Vector2.one));

                // Cạnh phải của sàn là góc (12, 0): gốc của phòng cộng 90 × 12.
                var room = InReferenceCanvas(Named(scene, "Room"));
                var floor = Named(scene, "Floor");
                var cellWidth = Mathf.Sqrt(90f * 90f + 18f * 18f);
                var cellHeight = Mathf.Sqrt(13f * 13f + 65f * 65f);
                Assert.AreEqual(12 * cellWidth, floor.rect.width, 1f);
                Assert.AreEqual(8 * cellHeight, floor.rect.height, 1f);
                Assert.AreEqual(-Mathf.Atan2(18f, 90f) * Mathf.Rad2Deg, floor.localEulerAngles.z > 180f ? floor.localEulerAngles.z - 360f : floor.localEulerAngles.z, 0.05f);
                Assert.Less(room.xMin + 90f * 12, InReferenceCanvas(column).xMin, "floor must not run under the right column");
            });

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
