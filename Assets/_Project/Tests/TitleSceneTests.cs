using System.Linq;
using BunDauPeo.EditorTools;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    public class TitleSceneTests
    {
        private static void WithTitle(System.Action<Scene> check) => RestaurantSceneTests.WithScene(ProjectSetup.TitleScenePath, check);

        private static LocalizedText LabelOf(Scene scene, string name) => LobbySceneTests.Named(scene, name).GetComponent<LocalizedText>();

        // Scene đọc lại từ file (L-002): bố cục spec UI hub C 5.1, một lớp chạm kín màn nằm trên cùng.
        [Test]
        public void Scene_ObjectsPlacedAndWired() =>
            WithTitle(scene =>
            {
                Assert.AreEqual(1, RestaurantSceneTests.All<Camera>(scene).Length);
                var systems = RestaurantSceneTests.All<EventSystem>(scene);
                Assert.AreEqual(1, systems.Length);
                Assert.IsNotNull(systems[0].GetComponent<InputSystemUIInputModule>());
                Assert.IsNull(systems[0].GetComponent<StandaloneInputModule>());

                var scaler = RestaurantSceneTests.All<CanvasScaler>(scene).Single();
                Assert.AreEqual(new Vector2(1920, 1080), scaler.referenceResolution);
                Assert.AreEqual(1f, scaler.matchWidthOrHeight, 0.0001f);

                var logo = LobbySceneTests.FromTop(scene, "Logo_PH_900x300");
                LobbySceneTests.AssertRect(510, 260, 900, 300, logo, "Logo_PH_900x300");
                Assert.AreEqual("title.game_name", LabelOf(scene, "GameNameText").Key);
                Assert.AreSame(LobbySceneTests.Named(scene, "Logo_PH_900x300"), LobbySceneTests.Named(scene, "GameNameText").parent, "GameNameText sits in the logo box");

                var hint = LobbySceneTests.FromTop(scene, "TapHint");
                Assert.AreEqual(960f, hint.center.x, 0.01f, "TapHint centre x");
                Assert.AreEqual(830f, hint.center.y, 0.01f, "TapHint centre y");
                Assert.AreEqual(1200f, hint.width, 0.01f, "TapHint width");
                Assert.AreEqual("title.tap_to_start", LabelOf(scene, "TapHint").Key);

                var version = LobbySceneTests.Named(scene, "VersionText");
                Assert.AreEqual(new Vector2(1, 0), version.anchorMin, "VersionText anchored bottom-right");
                Assert.AreEqual(new Vector2(1, 0), version.anchorMax);
                Assert.IsNull(version.GetComponent<LocalizedText>(), "the view writes the version at runtime");

                var tap = LobbySceneTests.Named(scene, "TapArea");
                Assert.AreEqual(tap.parent.childCount - 1, tap.GetSiblingIndex(), "TapArea is the last (top) sibling");
                Assert.AreEqual(Vector2.zero, tap.anchorMin);
                Assert.AreEqual(Vector2.one, tap.anchorMax);
                Assert.AreEqual(Vector2.zero, tap.offsetMin);
                Assert.AreEqual(Vector2.zero, tap.offsetMax);
                Assert.IsNotNull(tap.GetComponent<Button>());
                Assert.IsTrue(tap.GetComponent<Image>().raycastTarget, "TapArea catches every touch");
                foreach (var image in RestaurantSceneTests.All<Image>(scene).Where(i => i.transform != tap))
                    Assert.IsFalse(image.raycastTarget, $"{image.name} must not take touches");
                foreach (var text in RestaurantSceneTests.All<Text>(scene))
                    Assert.IsFalse(text.raycastTarget, $"{text.name} must not take touches");

                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<TitleView>(scene).Single());
                Assert.IsTrue(UiText.AllEntries.Any(e => e.Key == "title.game_name") && UiText.AllEntries.Any(e => e.Key == "title.tap_to_start"));
            });
    }
}
