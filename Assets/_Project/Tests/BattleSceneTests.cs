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
    public class BattleSceneTests
    {
        private static void WithBattleScene(System.Action<UnityEngine.SceneManagement.Scene> check) =>
            RestaurantSceneTests.WithScene(ProjectSetup.BattleScenePath, check);

        [Test]
        public void BattleScene_HasCameraAndEventSystemWithInputSystemModule() =>
            WithBattleScene(scene =>
            {
                Assert.AreEqual(1, RestaurantSceneTests.All<Camera>(scene).Length);
                var systems = RestaurantSceneTests.All<EventSystem>(scene);
                Assert.AreEqual(1, systems.Length);
                Assert.IsNotNull(systems[0].GetComponent<InputSystemUIInputModule>());
                Assert.IsNull(systems[0].GetComponent<StandaloneInputModule>());
            });

        [Test]
        public void BattleScene_ViewWired_OnlyImageAndText_ScalerMatchesHeight() =>
            WithBattleScene(scene =>
            {
                var views = RestaurantSceneTests.All<BattleView>(scene);
                Assert.AreEqual(1, views.Length);
                RestaurantSceneTests.AssertAllReferencesAssigned(views[0]);

                var units = RestaurantSceneTests.All<BattleUnitView>(scene);
                Assert.AreEqual(10, units.Length, "5 allies + 5 enemies");
                foreach (var unit in units) RestaurantSceneTests.AssertAllReferencesAssigned(unit);

                Assert.AreEqual(0, RestaurantSceneTests.All<RawImage>(scene).Length);
                var canvas = RestaurantSceneTests.All<CanvasScaler>(scene).Single();
                Assert.AreEqual(1f, canvas.matchWidthOrHeight);
                Assert.AreEqual(new Vector2(1920, 1080), canvas.referenceResolution);
            });

        [Test]
        public void BattleScene_ActionPanelAndCardsWired() =>
            WithBattleScene(scene =>
            {
                var panels = RestaurantSceneTests.All<BattleActionPanel>(scene);
                Assert.AreEqual(1, panels.Length);
                RestaurantSceneTests.AssertAllReferencesAssigned(panels[0]);
                Assert.AreEqual(3, panels[0].GetComponentsInChildren<Button>(true).Length, "Basic, Skill, Ultimate");

                var cards = RestaurantSceneTests.All<BattleCardView>(scene);
                Assert.AreEqual(5, cards.Length);
                foreach (var card in cards) RestaurantSceneTests.AssertAllReferencesAssigned(card);
            });

        [Test]
        public void BattleScene_CardsAndButtonsInCardArea() =>
            WithBattleScene(scene =>
            {
                var rects = RestaurantSceneTests.All<BattleCardView>(scene)
                    .Select(c => (c.name, (RectTransform)c.transform))
                    .Concat(RestaurantSceneTests.All<BattleActionPanel>(scene).Single().GetComponentsInChildren<Button>(true)
                        .Select(b => (b.name, (RectTransform)b.transform)))
                    .Select(x => (Name: x.Item1, Rect: RestaurantSceneTests.InReferenceCanvas(x.Item2)))
                    .ToArray();
                Assert.AreEqual(8, rects.Length);

                var canvas = new Rect(0, 0, 1920, 1080);
                foreach (var (name, rect) in rects)
                {
                    Assert.IsTrue(canvas.Contains(rect.min) && canvas.Contains(rect.max), $"{name} {rect} leaves the canvas");
                    // InReferenceCanvas có gốc dưới trái; kế hoạch tính y từ trên.
                    Assert.GreaterOrEqual(1080f - rect.yMax, BattleLayout.CardAreaTop, $"{name} starts above the card area");
                    Assert.LessOrEqual(1080f - rect.yMin, BattleLayout.CardAreaBottom, $"{name} ends below the card area");
                }

                for (var i = 0; i < rects.Length; i++)
                {
                    for (var j = i + 1; j < rects.Length; j++)
                    {
                        var a = rects[i].Rect;
                        var b = rects[j].Rect;
                        var overlapX = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                        var overlapY = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                        Assert.IsFalse(overlapX > 1f && overlapY > 1f, $"{rects[i].Name} overlaps {rects[j].Name}");
                    }
                }

                foreach (var button in RestaurantSceneTests.All<BattleActionPanel>(scene).Single().GetComponentsInChildren<Button>(true))
                {
                    var size = RestaurantSceneTests.InReferenceCanvas((RectTransform)button.transform).size;
                    Assert.GreaterOrEqual(Mathf.Min(size.x, size.y), 110f, $"{button.name} is too small to tap");
                }
            });

        [Test]
        public void BattleScene_InBuildSettings_AfterRestaurant()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.GreaterOrEqual(scenes.Length, 4);
            Assert.AreEqual(ProjectSetup.RestaurantScenePath, scenes[2].path);
            Assert.AreEqual(ProjectSetup.BattleScenePath, scenes[3].path);
            Assert.IsTrue(scenes[3].enabled);
        }
    }
}
