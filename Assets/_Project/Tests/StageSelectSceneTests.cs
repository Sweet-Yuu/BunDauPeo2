using System.Linq;
using BunDauPeo.EditorTools;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    public class StageSelectSceneTests
    {
        private static void WithStageSelect(System.Action<UnityEngine.SceneManagement.Scene> check) =>
            RestaurantSceneTests.WithScene(ProjectSetup.StageSelectScenePath, check);

        private static GameObject RootOf(Component popup) =>
            (GameObject)new SerializedObject(popup).FindProperty("_root").objectReferenceValue;

        [Test]
        public void StageSelectScene_HasCameraAndEventSystemWithInputSystemModule() =>
            WithStageSelect(scene =>
            {
                Assert.AreEqual(1, RestaurantSceneTests.All<Camera>(scene).Length);
                var systems = RestaurantSceneTests.All<EventSystem>(scene);
                Assert.AreEqual(1, systems.Length);
                Assert.IsNotNull(systems[0].GetComponent<InputSystemUIInputModule>());
                Assert.IsNull(systems[0].GetComponent<StandaloneInputModule>());
            });

        [Test]
        public void StageSelectScene_ViewAndPopupsWired_NoRawImage() =>
            WithStageSelect(scene =>
            {
                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<StageMapView>(scene).Single());
                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<StageInfoPopup>(scene).Single());
                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<StoryPlaceholderPopup>(scene).Single());
                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<StageNodeView>(scene).Single());

                Assert.AreEqual(0, RestaurantSceneTests.All<RawImage>(scene).Length);
                var canvas = RestaurantSceneTests.All<CanvasScaler>(scene).Single();
                Assert.AreEqual(1f, canvas.matchWidthOrHeight);
                Assert.AreEqual(new Vector2(1920, 1080), canvas.referenceResolution);
            });

        [Test]
        public void StageSelectScene_PopupsStartClosed_BlockClicks() =>
            WithStageSelect(scene =>
            {
                foreach (Component popup in new Component[]
                         {
                             RestaurantSceneTests.All<StageInfoPopup>(scene).Single(),
                             RestaurantSceneTests.All<StoryPlaceholderPopup>(scene).Single(),
                         })
                {
                    var root = RootOf(popup);
                    Assert.IsFalse(root.activeSelf, $"{popup.GetType().Name} starts open");
                    Assert.IsTrue(root.GetComponent<Image>().raycastTarget, $"{popup.GetType().Name} must block clicks behind it");
                    Assert.IsTrue(popup.gameObject.activeSelf, $"{popup.GetType().Name} component must stay on a live object to receive Open");
                }
            });

        [Test]
        public void NodeCenters_SevenNodes_InsideMapArea_NoOverlap()
        {
            const float size = 120f;
            var rects = Enumerable.Range(0, 7).Select(i => new Rect(StageMapView.NodeCenter(i) - Vector2.one * size / 2f, Vector2.one * size)).ToArray();

            foreach (var rect in rects)
            {
                Assert.GreaterOrEqual(rect.yMin, 180f, $"{rect} reaches the top bar");
                Assert.LessOrEqual(rect.yMax, 980f, $"{rect} reaches the DEV strip");
                Assert.GreaterOrEqual(rect.xMin, 0f, $"{rect}");
                Assert.LessOrEqual(rect.xMax, 1920f, $"{rect}");
            }

            for (var i = 0; i < rects.Length; i++)
            for (var j = i + 1; j < rects.Length; j++)
                Assert.IsFalse(rects[i].Overlaps(rects[j]), $"node {i} overlaps node {j}");

            Assert.AreEqual(new Vector2(240, 640), StageMapView.NodeCenter(0));
            Assert.AreEqual(new Vector2(480, 440), StageMapView.NodeCenter(1));
        }

        [Test]
        public void BuildSettings_Order_IsBootGachaRestaurantStageSelectBattle()
        {
            var paths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            CollectionAssert.AreEqual(
                new[] { ProjectSetup.BootScenePath, ProjectSetup.GachaScenePath, ProjectSetup.RestaurantScenePath,
                        ProjectSetup.StageSelectScenePath, ProjectSetup.BattleScenePath },
                paths.Take(5).ToArray());
        }
    }
}
