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
