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
                foreach (var field in new[]
                         {
                             "_banner", "_characters", "_singleButton", "_tenButton", "_skipButton", "_historyButton", "_gemText",
                             "_bannerNameText", "_pityText", "_messageText", "_revealView", "_totalView", "_rollAgainPopup", "_historyPopup",
                         })
                    Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, field);

                var banner = (GachaBannerDefinition)so.FindProperty("_banner").objectReferenceValue;
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<GachaBannerDefinition>(ProjectSetup.BannerPath).Id, banner.Id);
                Assert.AreEqual(0, All<RawImage>(scene).Length);
            });

        [Test]
        public void GachaScene_HasRevealTotalAndPopupsWired() =>
            WithScene(ProjectSetup.GachaScenePath, scene =>
            {
                var reveal = new SerializedObject(All<GachaRevealView>(scene).Single());
                foreach (var field in new[] { "_root", "_commonAnimator", "_legendaryAnimator", "_revealPanel", "_flash", "_portrait", "_nameText", "_starsText" })
                    Assert.IsNotNull(reveal.FindProperty(field).objectReferenceValue, "reveal " + field);

                var total = new SerializedObject(All<GachaTotalView>(scene).Single());
                foreach (var field in new[] { "_root", "_grid", "_iconTemplate", "_okButton" })
                    Assert.IsNotNull(total.FindProperty(field).objectReferenceValue, "total " + field);

                var again = new SerializedObject(All<GachaRollAgainPopup>(scene).Single());
                foreach (var field in new[] { "_root", "_warningText", "_rollAgainLabel", "_rollAgainButton", "_dismissButton" })
                    Assert.IsNotNull(again.FindProperty(field).objectReferenceValue, "popup " + field);

                var history = new SerializedObject(All<GachaHistoryPopup>(scene).Single());
                foreach (var field in new[] { "_root", "_listText", "_closeButton" })
                    Assert.IsNotNull(history.FindProperty(field).objectReferenceValue, "history " + field);
            });

        [Test]
        public void GachaScene_CutscenePrefabsAreInstanced_WithTheirControllers() =>
            WithScene(ProjectSetup.GachaScenePath, scene =>
            {
                var reveal = new SerializedObject(All<GachaRevealView>(scene).Single());
                var common = (Animator)reveal.FindProperty("_commonAnimator").objectReferenceValue;
                var legendary = (Animator)reveal.FindProperty("_legendaryAnimator").objectReferenceValue;
                Assert.AreEqual("Assets/_Project/Gacha/Animations/Summoning/CommonSummon.controller", AssetDatabase.GetAssetPath(common.runtimeAnimatorController));
                Assert.AreEqual("Assets/_Project/Gacha/Animations/Summoning/LegendarySummon.controller", AssetDatabase.GetAssetPath(legendary.runtimeAnimatorController));
            });

        [Test]
        public void GachaScene_TotalGridIsFiveColumns() =>
            WithScene(ProjectSetup.GachaScenePath, scene =>
            {
                var total = new SerializedObject(All<GachaTotalView>(scene).Single());
                var grid = ((RectTransform)total.FindProperty("_grid").objectReferenceValue).GetComponent<GridLayoutGroup>();
                Assert.AreEqual(GridLayoutGroup.Constraint.FixedColumnCount, grid.constraint);
                Assert.AreEqual(5, grid.constraintCount);
            });

        // G4: Cài đặt DEV nằm góc dưới phải, nên nút Bỏ qua (chỉ hiện lúc mở thẻ) phải ở góc trên phải.
        [Test]
        public void GachaScene_SkipButtonTopRight() =>
            WithScene(ProjectSetup.GachaScenePath, scene =>
            {
                var skip = All<Button>(scene).Single(b => b.name == "SkipButton");
                var rect = RestaurantSceneTests.InReferenceCanvas((RectTransform)skip.transform);
                Assert.GreaterOrEqual(rect.xMin, 1500f, "SkipButton must sit in the right part of the screen");
                Assert.LessOrEqual(1080f - rect.yMax, 140f, "SkipButton must hug the top edge, clear of the DEV Settings button at the bottom right");
            });

        [Test]
        public void GachaScene_IsSecondInBuildSettings()
        {
            Assert.AreEqual(ProjectSetup.BootScenePath, EditorBuildSettings.scenes[0].path);
            Assert.AreEqual(ProjectSetup.GachaScenePath, EditorBuildSettings.scenes[1].path);
        }

        [Test]
        public void BootScene_StartSceneIsRestaurant() =>
            WithScene(ProjectSetup.BootScenePath, scene =>
            {
                var bootstrap = All<GameBootstrap>(scene).Single();
                Assert.AreEqual("Restaurant",new SerializedObject(bootstrap).FindProperty("_startScene").stringValue);
            });
    }
}
