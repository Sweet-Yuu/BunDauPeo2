using System.Linq;
using BunDauPeo.EditorTools;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    // Lobby = Restaurant.unity có thanh dưới, 3 panel kín màn và Menu (spec UI hub 5.1–5.6). Đọc lại từ file scene (L-002).
    public class LobbySceneTests
    {
        internal const string Bar = "NavBar_PH_1920x120";
        internal const string CharactersPanel = "CharactersPanel_PH_1920x1080";
        internal const string StagesPanel = "StageMapPanel_PH_1920x1080";
        internal const string GachaPanel = "GachaPanel_PH_1920x1080";
        internal const string MenuPanel = "MenuPanel_PH_1920x1080";

        private static readonly string[] TabNames =
        {
            "NavTabRestaurant_PH_320x120", "NavTabCharacters_PH_320x120", "NavTabStory_PH_320x120",
            "NavTabStages_PH_320x120", "NavTabGacha_PH_320x120", "NavTabMenu_PH_320x120",
        };

        private static readonly string[] TabKeys =
        {
            "hub.tab_restaurant", "hub.tab_characters", "hub.tab_story", "hub.tab_stages", "hub.tab_gacha", "hub.tab_menu",
        };

        internal static void WithLobby(System.Action<Scene> check) => RestaurantSceneTests.WithScene(ProjectSetup.RestaurantScenePath, check);

        internal static RectTransform Named(Scene scene, string name) =>
            RestaurantSceneTests.All<RectTransform>(scene).Single(r => r.name == name);

        // Trong một panel: tên như GemCard, MessageText, CloseButton có ở nhiều chỗ của Restaurant.unity.
        internal static T[] In<T>(Scene scene, string panel) where T : Component => Named(scene, panel).GetComponentsInChildren<T>(true);

        [Test]
        public void NavBar_SixTabs_BottomAnchored_EqualWidths() =>
            WithLobby(scene =>
            {
                var bar = Named(scene, Bar);
                Assert.AreEqual(Vector2.zero, bar.anchorMin);
                Assert.AreEqual(new Vector2(1, 0), bar.anchorMax);
                Assert.AreEqual(HubNav.BarHeight, bar.sizeDelta.y);
                Assert.AreEqual(new Rect(0, 0, 1920, 120), RestaurantSceneTests.InReferenceCanvas(bar));

                var nav = new SerializedObject(RestaurantSceneTests.All<HubNav>(scene).Single());
                var buttons = nav.FindProperty("_tabButtons");
                Assert.AreEqual(TabNames.Length, buttons.arraySize);
                for (var i = 0; i < TabNames.Length; i++)
                {
                    var tab = bar.Find(TabNames[i]) as RectTransform;
                    Assert.IsNotNull(tab, TabNames[i]);
                    Assert.AreEqual(i / 6f, tab.anchorMin.x, 1e-4f, TabNames[i]);
                    Assert.AreEqual((i + 1) / 6f, tab.anchorMax.x, 1e-4f, TabNames[i]);
                    Assert.AreEqual(0f, tab.anchorMin.y, TabNames[i]);
                    Assert.AreEqual(1f, tab.anchorMax.y, TabNames[i]);
                    Assert.AreSame(tab.GetComponent<Button>(), buttons.GetArrayElementAtIndex(i).objectReferenceValue, $"HubTab {i} wires {TabNames[i]}");
                    Assert.AreEqual(i == (int)HubTab.Story, tab.Find("Lock_PH_40x48") != null, $"{TabNames[i]} lock");
                }
            });

        [Test]
        public void Panels_StartInactive_HubNavWired() =>
            WithLobby(scene =>
            {
                var navComponent = RestaurantSceneTests.All<HubNav>(scene).Single();
                var nav = new SerializedObject(navComponent);
                foreach (var (field, name) in new[]
                         {
                             ("_charactersPanel", CharactersPanel), ("_stagesPanel", StagesPanel), ("_gachaPanel", GachaPanel), ("_menuPanel", MenuPanel),
                         })
                {
                    var panel = (GameObject)nav.FindProperty(field).objectReferenceValue;
                    Assert.IsNotNull(panel, field);
                    Assert.AreEqual(name, panel.name, field);
                    Assert.IsFalse(panel.activeSelf, $"{name} starts open");
                    Assert.AreEqual(new Rect(0, 0, 1920, 1080), RestaurantSceneTests.InReferenceCanvas((RectTransform)panel.transform), $"{name} is full screen");
                    Assert.IsTrue(panel.GetComponent<Image>().raycastTarget, $"{name} must block clicks behind it");
                }

                Assert.IsNotNull(nav.FindProperty("_bar").objectReferenceValue, "_bar");
                Assert.IsNotNull(nav.FindProperty("_comingSoonToast").objectReferenceValue, "_comingSoonToast");
                var buttons = nav.FindProperty("_tabButtons");
                for (var i = 0; i < buttons.arraySize; i++) Assert.IsNotNull(buttons.GetArrayElementAtIndex(i).objectReferenceValue, $"_tabButtons[{i}]");
                var pages = nav.FindProperty("_pages");
                for (var i = 0; i < pages.arraySize; i++)
                    Assert.IsTrue(pages.GetArrayElementAtIndex(i).objectReferenceValue is IHubPage, $"_pages[{i}] must implement IHubPage");

                // Mỗi view nằm trong panel của nó và đủ tham chiếu (phần "view có đủ tham chiếu" của ba scene cũ).
                Assert.IsTrue(RestaurantSceneTests.All<CharactersView>(scene).Single().transform.IsChildOf(Named(scene, CharactersPanel)));
                Assert.IsTrue(RestaurantSceneTests.All<StageMapView>(scene).Single().transform.IsChildOf(Named(scene, StagesPanel)));
                Assert.IsTrue(RestaurantSceneTests.All<GachaView>(scene).Single().transform.IsChildOf(Named(scene, GachaPanel)));
                Assert.IsTrue(navComponent.transform.parent.GetComponent<Canvas>() != null, "HubNav sits on the one canvas");

                var characters = new SerializedObject(RestaurantSceneTests.All<CharactersView>(scene).Single());
                var it = characters.GetIterator();
                it.NextVisible(true);
                while (it.NextVisible(false))
                    if (it.propertyType == SerializedPropertyType.ObjectReference) Assert.IsNotNull(it.objectReferenceValue, "CharactersView." + it.name);
                Assert.Greater(characters.FindProperty("_cards").arraySize, 0);
                foreach (var name in new[] { "CharacterCard0", "StarUpButton", "StarUpStatus" })
                    Assert.IsTrue(In<Transform>(scene, CharactersPanel).Any(t => t.name == name), name);

                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<StageMapView>(scene).Single());
                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<MenuView>(scene).Single());
                Assert.AreSame(Named(scene, MenuPanel).gameObject, RestaurantSceneTests.All<MenuView>(scene).Single().gameObject, "MenuView sits on the Menu panel");
            });

        [Test]
        public void LocalizedTexts_KeysExist() =>
            WithLobby(scene =>
            {
                var bar = Named(scene, Bar);
                for (var i = 0; i < TabNames.Length; i++)
                {
                    var label = bar.Find(TabNames[i]).GetComponentInChildren<LocalizedText>(true);
                    Assert.IsNotNull(label, TabNames[i]);
                    Assert.AreEqual(TabKeys[i], label.Key, TabNames[i]);
                }

                var menu = Named(scene, MenuPanel);
                var keys = menu.GetComponentsInChildren<LocalizedText>(true).Select(l => l.Key).ToArray();
                foreach (var key in new[] { "hub.menu_title", "hub.reset", "hub.dev", "common.close", "hub.reset_confirm", "hub.reset_yes", "common.cancel" })
                    CollectionAssert.Contains(keys, key, "MenuPanel labels");
                Assert.IsNull(Named(scene, "MenuLanguageBtn_PH_700x100").GetComponentInChildren<LocalizedText>(true), "MenuView writes the language label at runtime");
                Assert.IsNull(Named(scene, "MenuStatus").GetComponent<LocalizedText>(), "MenuView writes the status line at runtime");
                Assert.IsNotNull(Named(scene, "ComingSoonToast").GetComponentInChildren<LocalizedText>(true), "toast text is a fixed label");
                SceneTextAsserts.AssertLocalizedTexts(scene);
            });

        // Nút của chính thanh và hộp vẽ trên thanh (Canvas con overrideSorting) được miễn; trang Gacha lúc thanh ẩn (cutscene, lộ, tổng) cũng vậy.
        [Test]
        public void NoButtonUnderBar() =>
            WithLobby(scene =>
            {
                var bar = Named(scene, Bar);
                foreach (var button in RestaurantSceneTests.All<Button>(scene))
                {
                    if (button.transform.IsChildOf(bar)) continue;
                    if (button.GetComponentsInParent<Canvas>(true).Any(c => c.overrideSorting)) continue;
                    if (button.GetComponentsInParent<Transform>(true).Any(t => t.name == "RevealRoot" || t.name == "TotalRoot")) continue;

                    var rect = RestaurantSceneTests.InReferenceCanvas((RectTransform)button.transform);
                    Assert.GreaterOrEqual(rect.yMin, HubNav.BarHeight, $"{Path(button.transform)} {rect} reaches the bar strip");
                }
            });

        [Test]
        public void DrawOrder_PanelsMenuBar() =>
            WithLobby(scene =>
            {
                var canvas = RestaurantSceneTests.All<Canvas>(scene).Single().transform;
                int Index(string name)
                {
                    var rect = Named(scene, name);
                    Assert.AreSame(canvas, rect.parent, $"{name} is a direct child of the canvas");
                    return rect.GetSiblingIndex();
                }

                var layers = new[]
                {
                    new[] { "Room", "RightColumn" },
                    new[] { "StaffPanelRoot", "UpgradePanelRoot", "QualityPopupRoot" },
                    new[] { CharactersPanel, StagesPanel, GachaPanel },
                    new[] { MenuPanel },
                    new[] { Bar },
                };
                for (var i = 0; i + 1 < layers.Length; i++)
                    Assert.Less(layers[i].Max(Index), layers[i + 1].Min(Index), $"{string.Join("/", layers[i])} must draw under {string.Join("/", layers[i + 1])}");

                Assert.Less(Index(Bar), Index("AfkPopupRoot"), "the reward box draws over the bar");
            });

        [Test]
        public void Stations_AboveBar() =>
            WithLobby(scene =>
            {
                var view = new SerializedObject(RestaurantSceneTests.All<RestaurantView>(scene).Single());
                foreach (var field in new[] { "_cookSpots", "_serverSpots", "_cashierSpots" })
                {
                    // RestaurantView đặt bong bóng ở điểm đầu tiên cộng 120 px lên (kích thước 90).
                    var spot = (RectTransform)view.FindProperty(field).GetArrayElementAtIndex(0).objectReferenceValue;
                    var bubble = RestaurantSceneTests.InReferenceCanvas(spot).center + new Vector2(0, 120) - new Vector2(45, 45);
                    Assert.Greater(bubble.y, HubNav.BarHeight, $"{field} bubble bottom {bubble.y} reaches the bar strip");
                }
            });

        private static string Path(Transform t) => string.Join("/", t.GetComponentsInParent<Transform>(true).Reverse().Select(x => x.name));
    }
}
