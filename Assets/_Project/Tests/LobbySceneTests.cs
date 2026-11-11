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
        internal const string ResourceBar = "ResourceBar_PH_1120x76";

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
                Assert.AreSame(Named(scene, "LobbyHud").gameObject, nav.FindProperty("_lobbyHud").objectReferenceValue, "_lobbyHud");
                var soon = nav.FindProperty("_comingSoonButtons");
                Assert.AreEqual(2, soon.arraySize, "_comingSoonButtons: avatar and shop");
                Assert.AreSame(Named(scene, "Avatar_PH_140x140").GetComponent<Button>(), soon.GetArrayElementAtIndex(0).objectReferenceValue);
                Assert.AreSame(Named(scene, "HubRoundBtnShop_PH_110x110").GetComponent<Button>(), soon.GetArrayElementAtIndex(1).objectReferenceValue);
                Assert.AreSame(Named(scene, "BannerGacha_PH_360x200").GetComponent<Button>(), nav.FindProperty("_bannerButton").objectReferenceValue, "_bannerButton");
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
                var canvas = RestaurantSceneTests.All<Canvas>(scene).Single(c => !c.overrideSorting).transform;
                int Index(string name)
                {
                    var rect = Named(scene, name);
                    Assert.AreSame(canvas, rect.parent, $"{name} is a direct child of the canvas");
                    return rect.GetSiblingIndex();
                }

                var layers = new[]
                {
                    new[] { "WorldScroll", "LobbyHud" },
                    new[] { "StaffPanelRoot", "UpgradePanelRoot", "QualityPopupRoot" },
                    new[] { CharactersPanel, StagesPanel, GachaPanel },
                    new[] { ResourceBar },
                    new[] { MenuPanel },
                    new[] { Bar },
                };
                for (var i = 0; i + 1 < layers.Length; i++)
                    Assert.Less(layers[i].Max(Index), layers[i + 1].Min(Index), $"{string.Join("/", layers[i])} must draw under {string.Join("/", layers[i + 1])}");

                Assert.Less(Index(Bar), Index("AfkPopupRoot"), "the reward box draws over the bar");
                Assert.Less(Index(ResourceBar), Index("ComingSoonToast"), "the toast draws over the resource bar");
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

        // Rect theo góc trên-trái (y đi xuống) như spec 5.4; InReferenceCanvas trả gốc dưới-trái.
        internal static Rect FromTop(Scene scene, string name) => FromTop(RestaurantSceneTests.InReferenceCanvas(Named(scene, name)));

        internal static Rect FromTop(Rect r) => new(r.xMin, 1080f - r.yMax, r.width, r.height);

        internal static void AssertRect(float x, float y, float w, float h, Rect actual, string name)
        {
            Assert.AreEqual(x, actual.x, 0.01f, name + " x");
            Assert.AreEqual(y, actual.y, 0.01f, name + " y (from top)");
            Assert.AreEqual(w, actual.width, 0.01f, name + " width");
            Assert.AreEqual(h, actual.height, 0.01f, name + " height");
        }

        // Spec 5.4: HUD Lobby gom trong LobbyHud; object mới đúng tên, cỡ, chỗ; khối tiền cũ và cột phải cũ không còn.
        [Test]
        public void LobbyHud_ObjectsNamedSizedPlaced() =>
            WithLobby(scene =>
            {
                var canvas = RestaurantSceneTests.All<Canvas>(scene).Single(c => !c.overrideSorting).transform;
                var hud = Named(scene, "LobbyHud");
                Assert.AreSame(canvas, hud.parent);
                Assert.AreEqual(new Rect(0, 0, 1920, 1080), RestaurantSceneTests.InReferenceCanvas(hud), "LobbyHud covers the canvas");
                Assert.IsTrue(hud.gameObject.activeSelf, "LobbyHud starts on");

                foreach (var (name, x, y, w, h) in new[]
                         {
                             ("Avatar_PH_140x140", 20f, 20f, 140f, 140f), ("InfoBar", 180f, 108f, 800f, 68f), ("BannerGacha_PH_360x200", 20f, 200f, 360f, 200f),
                         })
                {
                    Assert.IsTrue(Named(scene, name).IsChildOf(hud), name + " belongs to LobbyHud");
                    AssertRect(x, y, w, h, FromTop(scene, name), name);
                    Assert.IsNotNull(Named(scene, name).GetComponent<Button>(), name + " is a Button");
                }

                var message = FromTop(scene, "AfkMessage");
                Assert.IsTrue(Named(scene, "AfkMessage").IsChildOf(hud));
                Assert.AreEqual(180f, message.x, 0.01f);
                Assert.AreEqual(186f, message.y, 0.01f);
                Assert.AreEqual(800f, message.width, 0.01f);

                var shop = RestaurantSceneTests.InReferenceCanvas(Named(scene, "HubRoundBtnShop_PH_110x110"));
                Assert.IsTrue(Named(scene, "HubRoundBtnShop_PH_110x110").IsChildOf(hud));
                Assert.IsNotNull(Named(scene, "HubRoundBtnShop_PH_110x110").GetComponent<Button>());
                Assert.AreEqual(new Vector2(110, 110), shop.size);
                Assert.AreEqual(1900f, shop.xMax, 0.01f, "20 from the right edge");
                Assert.AreEqual(140f, shop.yMin, 0.01f, "140 above the bottom edge");
                Assert.IsTrue(Named(scene, "ShopLock_PH_40x48").IsChildOf(Named(scene, "HubRoundBtnShop_PH_110x110")));

                var toggle = RestaurantSceneTests.InReferenceCanvas(Named(scene, "RestaurantBarToggle_PH_60x160"));
                var bar = RestaurantSceneTests.InReferenceCanvas(Named(scene, "RestaurantBar_PH_180x460"));
                Assert.AreEqual(new Vector2(60, 160), toggle.size);
                Assert.AreEqual(new Vector2(180, 460), bar.size);
                Assert.AreEqual(1920f, toggle.xMax, 0.01f, "toggle hugs the right edge");
                Assert.AreEqual(540f, toggle.center.y, 0.01f);
                Assert.AreEqual(540f, bar.center.y, 0.01f);
                Assert.AreEqual(toggle.xMin, bar.xMax, 0.01f, "the bar sits right against the toggle");
                Assert.IsNotNull(Named(scene, "RestaurantBarToggle_PH_60x160").GetComponent<Button>());

                var screen = new Rect(0, 0, 1920, 1080);
                foreach (var rect in hud.GetComponentsInChildren<RectTransform>(true))
                {
                    var r = RestaurantSceneTests.InReferenceCanvas(rect);
                    Assert.IsTrue(screen.Contains(r.min) && screen.Contains(r.max - Vector2.one), $"{rect.name} {r} leaves the canvas");
                }

                var all = RestaurantSceneTests.All<RectTransform>(scene).Select(r => r.name).ToArray();
                foreach (var gone in new[] { "CoinCard", "MaterialCard", "GemCard", "StaminaCard", "RightColumn" })
                    CollectionAssert.DoesNotContain(all, gone, gone + " is replaced by the shared resource bar / restaurant bar");

                // Chỗ nút Thông báo của đợt C: không nút nào của HUD Lobby hay thanh tài nguyên (đang tắt cũng tính, L-025).
                var notice = new Rect(1790, 960, 110, 100);
                var hudButtons = hud.GetComponentsInChildren<Button>(true).Concat(Named(scene, ResourceBar).GetComponentsInChildren<Button>(true));
                foreach (var button in hudButtons)
                    Assert.IsFalse(RestaurantSceneTests.InReferenceCanvas((RectTransform)button.transform).Overlaps(notice), $"{button.name} sits on the reserved notification spot");
            });

        // Spec 5.3: một ResourceBar 1120×76 với 4 ô 280 theo thứ tự Thể lực, Coin, Material, Gem; Material không có nút +.
        [Test]
        public void ResourceBar_SlotsWired() =>
            WithLobby(scene =>
            {
                var canvas = RestaurantSceneTests.All<Canvas>(scene).Single(c => !c.overrideSorting).transform;
                var bar = Named(scene, ResourceBar);
                Assert.AreSame(canvas, bar.parent);
                AssertRect(180, 20, 1120, 76, FromTop(scene, ResourceBar), ResourceBar);

                var slotNames = new[] { "ResStamina_PH_280x76", "ResCoin_PH_280x76", "ResMaterial_PH_280x76", "ResGem_PH_280x76" };
                for (var i = 0; i < slotNames.Length; i++)
                {
                    var slot = bar.Find(slotNames[i]) as RectTransform;
                    Assert.IsNotNull(slot, slotNames[i]);
                    Assert.AreEqual(i, slot.GetSiblingIndex(), slotNames[i] + " order");
                    Assert.AreEqual(280f, RestaurantSceneTests.InReferenceCanvas(slot).width, 0.01f, slotNames[i]);
                    Assert.AreEqual(i != 2, slot.Find("ResPlus_PH_56x56") != null, slotNames[i] + " plus button (Material has none)");
                }

                var view = RestaurantSceneTests.All<ResourceBarView>(scene).Single();
                RestaurantSceneTests.AssertAllReferencesAssigned(view);
                var so = new SerializedObject(view);
                Assert.AreSame(RestaurantSceneTests.All<HubNav>(scene).Single(), so.FindProperty("_hub").objectReferenceValue);
                Assert.AreSame(bar, so.FindProperty("_bar").objectReferenceValue);
                var slots = so.FindProperty("_slots");
                var texts = so.FindProperty("_slotTexts");
                Assert.AreEqual(4, slots.arraySize);
                Assert.AreEqual(4, texts.arraySize);
                for (var i = 0; i < 4; i++)
                {
                    var slot = (GameObject)slots.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.AreEqual(slotNames[i], slot.name, "_slots order");
                    Assert.IsTrue(((Text)texts.GetArrayElementAtIndex(i).objectReferenceValue).transform.IsChildOf(slot.transform), $"_slotTexts[{i}] belongs to its slot");
                }

                Assert.IsTrue(((Text)so.FindProperty("_staminaNextText").objectReferenceValue).transform.IsChildOf(bar.Find(slotNames[0])), "the countdown lives in the Stamina slot");
                var plus = so.FindProperty("_plusButtons");
                Assert.AreEqual(3, plus.arraySize, "plus buttons: Stamina, Coin, Gem");
                foreach (var (index, slotIndex) in new[] { (0, 0), (1, 1), (2, 3) })
                {
                    var button = (Button)plus.GetArrayElementAtIndex(index).objectReferenceValue;
                    Assert.AreEqual("ResPlus_PH_56x56", button.name);
                    Assert.AreSame(bar.Find(slotNames[slotIndex]), button.transform.parent, $"_plusButtons[{index}] sits in {slotNames[slotIndex]}");
                }
            });

        // Spec 5.5: thanh quán chứa ba nút cũ, lúc đầu tắt; chấm là con của nút toggle; trang Nhân viên và Nâng cấp vẫn mở bằng nút cũ.
        [Test]
        public void RestaurantBar_WiredAndHoldsOldButtons() =>
            WithLobby(scene =>
            {
                var bar = Named(scene, "RestaurantBar_PH_180x460");
                var toggle = Named(scene, "RestaurantBarToggle_PH_60x160");
                Assert.IsFalse(bar.gameObject.activeSelf, "the restaurant bar starts closed");

                var barRect = RestaurantSceneTests.InReferenceCanvas(bar);
                var previous = default(Rect);
                foreach (var name in new[] { "StaffButton", "UpgradeButton", "AfkButton" })
                {
                    var button = Named(scene, name);
                    Assert.AreSame(bar, button.parent, name + " lives in the restaurant bar");
                    var rect = RestaurantSceneTests.InReferenceCanvas(button);
                    Assert.AreEqual(new Vector2(130, 130), rect.size, name);
                    Assert.IsTrue(barRect.Contains(rect.min) && barRect.Contains(rect.max - Vector2.one), name + " leaves the bar");
                    if (previous.size != Vector2.zero)
                    {
                        Assert.IsFalse(rect.Overlaps(previous), name + " overlaps the button above");
                        Assert.Less(rect.center.y, previous.center.y, name + " is below the previous button");
                    }

                    previous = rect;
                }

                var dot = Named(scene, "RestaurantBarDot_PH_24x24");
                Assert.AreSame(toggle, dot.parent, "the dot is a child of the toggle");

                var view = RestaurantSceneTests.All<RestaurantBarView>(scene).Single();
                RestaurantSceneTests.AssertAllReferencesAssigned(view);
                var so = new SerializedObject(view);
                Assert.AreSame(toggle.GetComponent<Button>(), so.FindProperty("_toggle").objectReferenceValue);
                Assert.AreSame(bar.gameObject, so.FindProperty("_bar").objectReferenceValue);
                Assert.AreSame(dot.gameObject, so.FindProperty("_dot").objectReferenceValue);
                Assert.IsTrue(((Text)so.FindProperty("_toggleText").objectReferenceValue).transform.IsChildOf(toggle));

                Assert.AreSame(Named(scene, "StaffButton").GetComponent<Button>(),
                    new SerializedObject(RestaurantSceneTests.All<StaffPanel>(scene).Single()).FindProperty("_openButton").objectReferenceValue);
                Assert.AreSame(Named(scene, "UpgradeButton").GetComponent<Button>(),
                    new SerializedObject(RestaurantSceneTests.All<UpgradeTestPanel>(scene).Single()).FindProperty("_openButton").objectReferenceValue);
            });

        // Spec 5.2 + Review Focus 3: khung kéo 2800×1400 kẹp mép, không zoom; rộng hơn màn 20:9 (2400 đơn vị) nên vẫn kéo ngang được.
        [Test]
        public void WorldScroll_ClampedNoZoom_WiderThan20by9() =>
            WithLobby(scene =>
            {
                var canvas = RestaurantSceneTests.All<Canvas>(scene).Single(c => !c.overrideSorting).transform;
                var root = Named(scene, "WorldScroll");
                Assert.AreSame(canvas, root.parent);
                Assert.AreEqual(0, root.GetSiblingIndex(), "WorldScroll is the first (bottom) layer");
                Assert.AreEqual(new Rect(0, 0, 1920, 1080), RestaurantSceneTests.InReferenceCanvas(root), "WorldScroll fills the canvas");
                var image = root.GetComponent<Image>();
                Assert.IsTrue(image.raycastTarget, "the transparent image catches swipes");
                Assert.AreEqual(0f, image.color.a, "WorldScroll is invisible");
                Assert.IsFalse(root.GetComponent<CanvasRenderer>().cullTransparentMesh, "a culled transparent mesh gets no raycast, so no swipe");

                var scroll = root.GetComponent<ScrollRect>();
                Assert.IsTrue(scroll.horizontal && scroll.vertical);
                Assert.AreEqual(ScrollRect.MovementType.Clamped, scroll.movementType);
                Assert.IsTrue(scroll.inertia);
                Assert.AreEqual(0f, scroll.scrollSensitivity, "no zoom/wheel");
                Assert.AreSame(root, scroll.viewport);
                var world = scroll.content;
                Assert.AreEqual("World_PH_2800x1400", world.name);
                Assert.AreSame(root, world.parent);
                Assert.AreEqual(new Vector2(2800, 1400), world.sizeDelta);
                Assert.GreaterOrEqual(world.sizeDelta.x, 2400 + 200, "pans sideways on 20:9");
                Assert.GreaterOrEqual(world.sizeDelta.y, 1080 + 200, "pans up/down");
                Assert.AreEqual(new Vector2(0.5f, 0.5f), world.pivot);
                Assert.AreEqual(Vector2.zero, world.anchoredPosition, "the lobby opens centred");

                foreach (var name in new[] { "Background", "Garden", "Room" })
                    Assert.IsTrue(Named(scene, name).IsChildOf(world), name + " lives in the world");
                var floats = RestaurantSceneTests.All<RectTransform>(scene).Where(r => r.name.StartsWith("FloatText")).ToArray();
                Assert.AreEqual(8, floats.Length);
                foreach (var f in floats) Assert.IsTrue(f.IsChildOf(world), f.name + " floats inside the world");
                var origin = (RectTransform)new SerializedObject(RestaurantSceneTests.All<RestaurantHud>(scene).Single()).FindProperty("_floatOrigin").objectReferenceValue;
                Assert.IsTrue(origin.IsChildOf(world), "_floatOrigin");

                // Chỉ "!" của sự cố được nhận bấm trong thế giới; trang trí không chặn vuốt hay bấm.
                foreach (var graphic in world.GetComponentsInChildren<Graphic>(true).Where(g => g.raycastTarget))
                    Assert.IsTrue(graphic.GetComponentInParent<Button>(true)?.name == "IncidentBubble", $"{Path(graphic.transform)} raycasts");
                Assert.AreEqual(8 + 10, Named(scene, "Garden").Cast<Transform>().Count(t => t.name.StartsWith("Plant")), "Garden: 8 old plants + 10 outside the screen");
            });

        private static string Path(Transform t) => string.Join("/", t.GetComponentsInParent<Transform>(true).Reverse().Select(x => x.name));
    }
}
