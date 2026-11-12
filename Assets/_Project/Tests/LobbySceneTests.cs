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
        internal const string StagesPanel = "ModeSelectPanel_PH_1920x1080";
        internal const string StageMapPanel = "StageMapPanel_PH_1920x1080";
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
                Assert.AreEqual(1, soon.arraySize, "_comingSoonButtons: only the shop (the avatar opens the profile)");
                Assert.AreSame(Named(scene, "HubRoundBtnShop_PH_110x110").GetComponent<Button>(), soon.GetArrayElementAtIndex(0).objectReferenceValue);
                Assert.AreSame(Named(scene, "Avatar_PH_140x140").GetComponent<Button>(), nav.FindProperty("_profileButton").objectReferenceValue, "_profileButton");
                Assert.AreSame(Named(scene, "BannerGacha_PH_360x200").GetComponent<Button>(), nav.FindProperty("_bannerButton").objectReferenceValue, "_bannerButton");
                Assert.IsNotNull(nav.FindProperty("_comingSoonToast").objectReferenceValue, "_comingSoonToast");
                var buttons = nav.FindProperty("_tabButtons");
                for (var i = 0; i < buttons.arraySize; i++) Assert.IsNotNull(buttons.GetArrayElementAtIndex(i).objectReferenceValue, $"_tabButtons[{i}]");
                var pages = nav.FindProperty("_pages");
                for (var i = 0; i < pages.arraySize; i++)
                    Assert.IsTrue(pages.GetArrayElementAtIndex(i).objectReferenceValue is IHubPage, $"_pages[{i}] must implement IHubPage");

                // Mỗi view nằm trong panel của nó và đủ tham chiếu (phần "view có đủ tham chiếu" của ba scene cũ).
                Assert.IsTrue(RestaurantSceneTests.All<CharactersView>(scene).Single().transform.IsChildOf(Named(scene, CharactersPanel)));
                Assert.IsTrue(RestaurantSceneTests.All<StageMapView>(scene).Single().transform.IsChildOf(Named(scene, StageMapPanel)));
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
                    new[] { CharactersPanel, StagesPanel, StageMapPanel, GachaPanel },
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

        // Tỉ lệ màn thật. Canvas khớp chiều cao nên rộng = 1080 × tỉ lệ; World ở giữa nên lệch (1920 − rộng) / 2 so với màn, HUD neo trái đứng yên, HUD neo phải lệch cả (1920 − rộng).
        private static readonly (string Name, float Aspect)[] Aspects = { ("16:9", 16f / 9f), ("18:9", 2f), ("20:9", 20f / 9f), ("16:10", 1.6f), ("4:3", 4f / 3f) };

        // N1 (review cuối đợt B): "!" của mỗi vai (điểm đầu + 120 px lên, nút 90) không nằm dưới nút nào của HUD Lobby hay thanh tài nguyên, kể cả nút đang tắt (L-025).
        [Test]
        public void IncidentBubbles_ClearOfHudButtons_AtEveryAspect() =>
            WithLobby(scene =>
            {
                var view = new SerializedObject(RestaurantSceneTests.All<RestaurantView>(scene).Single());
                var buttons = Named(scene, "LobbyHud").GetComponentsInChildren<Button>(true).Concat(Named(scene, ResourceBar).GetComponentsInChildren<Button>(true)).ToArray();
                foreach (var (aspectName, aspect) in Aspects)
                {
                    var shift = 1920f - 1080f * aspect;
                    foreach (var field in new[] { "_cookSpots", "_serverSpots", "_cashierSpots" })
                    {
                        var spot = (RectTransform)view.FindProperty(field).GetArrayElementAtIndex(0).objectReferenceValue;
                        var centre = RestaurantSceneTests.InReferenceCanvas(spot).center + new Vector2(0, 120);
                        var bubble = new Rect(centre.x - shift / 2f - 45f, centre.y - 45f, 90f, 90f);
                        foreach (var button in buttons)
                        {
                            var rect = RestaurantSceneTests.InReferenceCanvas((RectTransform)button.transform);
                            // Nút nằm nửa phải của canvas tham chiếu thì neo phải.
                            if (rect.center.x > 960f) rect.x -= shift;
                            Assert.IsFalse(bubble.Overlaps(rect), $"{aspectName}: {field} '!' {bubble} is under {button.name} {rect}");
                        }
                    }
                }
            });

        // Mép phải phòng (sàn và tường sau) không chạm thanh quán đang mở. 4:3 bỏ qua: phòng lệch phải để nhường chỗ "!" nên mở thanh thì chạm tường, người chơi kéo quán được (user chốt 2026-11-11).
        [Test]
        public void Room_ClearOfOpenRestaurantBar_AtWideAspects() =>
            WithLobby(scene =>
            {
                var roomNode = Named(scene, "Room");
                var origin = RestaurantSceneTests.InReferenceCanvas(roomNode).position;
                var right = new[] { "Floor", "WallBack" }.Max(name =>
                {
                    var corners = new Vector3[4];
                    Named(scene, name).GetWorldCorners(corners);
                    return corners.Max(c => origin.x + roomNode.InverseTransformPoint(c).x);
                });
                var column = RestaurantSceneTests.InReferenceCanvas(Named(scene, "RestaurantBar_PH_180x460"));
                foreach (var (aspectName, aspect) in Aspects.Where(a => a.Aspect >= 1.6f))
                {
                    var shift = 1920f - 1080f * aspect;
                    Assert.Less(right - shift / 2f, column.xMin - shift, $"{aspectName}: room edge {right - shift / 2f} runs under the open restaurant bar {column.xMin - shift}");
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
                             ("Avatar_PH_140x140", 20f, 20f, 140f, 140f), ("InfoBar", 180f, 108f, 800f, 68f), ("BannerGacha_PH_360x200", 20f, 340f, 360f, 200f),
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

                // Hình mới có tên theo luật _PH_ (review cuối đợt B): mặt avatar 100×100.
                Assert.IsNotNull(Named(scene, "Avatar_PH_140x140").Find("AvatarFace_PH_100x100"), "avatar face is named AvatarFace_PH_100x100");

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

        internal const string NotifyButton = "NotifyBtn_PH_110x100";

        // Rect (gốc dưới-trái) của rt khi canvas rộng `width` × 1080 (canvas khớp chiều cao), tính cả localScale của tổ tiên (FitWidthScale).
        internal static Rect RectAtWidth(RectTransform rt, float width) => Frame(rt, new Vector2(width, 1080f)).Rect;

        private static (Rect Rect, Vector2 LocalSize, float Ratio) Frame(RectTransform rt, Vector2 canvas)
        {
            Vector2 parentMin, parentLocal;
            float parentRatio;
            if (rt.parent.GetComponent<Canvas>() != null)
            {
                parentMin = Vector2.zero;
                parentLocal = canvas;
                parentRatio = 1f;
            }
            else
            {
                var parent = Frame((RectTransform)rt.parent, canvas);
                parentMin = parent.Rect.min;
                parentLocal = parent.LocalSize;
                parentRatio = parent.Ratio;
            }

            var anchorMin = Vector2.Scale(rt.anchorMin, parentLocal);
            var anchorMax = Vector2.Scale(rt.anchorMax, parentLocal);
            var localSize = anchorMax - anchorMin + rt.sizeDelta;
            var pivotLocal = anchorMin + Vector2.Scale(rt.pivot, anchorMax - anchorMin) + rt.anchoredPosition;
            var ratio = parentRatio * rt.localScale.x;
            var size = localSize * ratio;
            var pivot = parentMin + pivotLocal * parentRatio;
            return (new Rect(pivot - Vector2.Scale(rt.pivot, size), size), localSize, ratio);
        }

        // Nút Thông báo (đợt C, spec 5.3): trên-phải (1790, 20), sau Menu và trước thanh dưới. Ở mọi tỉ lệ màn, nút và thanh tài nguyên đã dời (ngoài Lobby,
        // một ô 280) không đè Button nào, kể cả nút đang tắt (L-025, L-027) và không tính con của hộp OverBar (hộp nổi che cả thanh).
        [Test]
        public void NotifyButton_PlacedAboveMenu_ClearOfEveryButton() =>
            WithLobby(scene =>
            {
                var canvas = RestaurantSceneTests.All<Canvas>(scene).Single(c => !c.overrideSorting).transform;
                var notify = Named(scene, NotifyButton);
                Assert.AreSame(canvas, notify.parent);
                Assert.AreEqual(Vector2.one, notify.anchorMin, "top-right anchor");
                Assert.AreEqual(Vector2.one, notify.anchorMax, "top-right anchor");
                AssertRect(1790, 20, 110, 100, FromTop(scene, NotifyButton), NotifyButton);
                Assert.IsFalse(notify.IsChildOf(Named(scene, "LobbyHud")), "the button stays on every page, not only the Lobby");
                Assert.Less(Named(scene, MenuPanel).GetSiblingIndex(), notify.GetSiblingIndex(), "the button draws over the Menu");
                Assert.Less(notify.GetSiblingIndex(), Named(scene, Bar).GetSiblingIndex(), "the button draws under the bar");

                var dot = notify.Find("NotifyDot_PH_28x28");
                Assert.IsNotNull(dot);
                Assert.IsFalse(dot.gameObject.activeSelf, "the dot starts off");
                Assert.IsNotNull(notify.GetComponent<Button>());

                var view = RestaurantSceneTests.All<NotifyView>(scene).Single();
                Assert.AreSame(notify.gameObject, view.gameObject, "NotifyView sits on the button");
                RestaurantSceneTests.AssertAllReferencesAssigned(view);
                var so = new SerializedObject(view);
                Assert.AreSame(notify.GetComponent<Button>(), so.FindProperty("_button").objectReferenceValue);
                Assert.AreSame(dot.gameObject, so.FindProperty("_dot").objectReferenceValue);
                Assert.AreSame(RestaurantSceneTests.All<NotifyPopup>(scene).Single(), so.FindProperty("_popup").objectReferenceValue);
                Assert.AreSame(Named(scene, NotifyButton).gameObject, new SerializedObject(RestaurantSceneTests.All<HubNav>(scene).Single()).FindProperty("_notifyButton").objectReferenceValue);

                Assert.GreaterOrEqual(ResourceBarView.RightMargin, 20f + 110f + 10f, "the moved resource bar keeps clear of the button");
                // Bỏ qua của Gacha chỉ bật khi đang quay, lúc thanh dưới và nút Thông báo đều ẩn (HubNav.SetBarHidden); điều kiện này có assert ở PlayMode Hub_ResourceBar_FollowsTabsAndPages.
                var skip = new SerializedObject(RestaurantSceneTests.All<GachaView>(scene).Single()).FindProperty("_skipButton").objectReferenceValue;
                var buttons = RestaurantSceneTests.All<Button>(scene)
                    .Where(b => b.transform != notify && b.GetComponentsInParent<Canvas>(true).All(c => !c.overrideSorting)
                                && b != skip).ToArray();
                Assert.Greater(buttons.Length, 10, "the scene has buttons to check against");
                foreach (var (aspectName, aspect) in Aspects)
                {
                    var width = 1080f * aspect;
                    var spot = RectAtWidth(notify, width);
                    var resources = new Rect(width - ResourceBarView.RightMargin - 280f, 1080f - 20f - 76f, 280f, 76f);
                    Assert.IsFalse(spot.Overlaps(resources), $"{aspectName}: the moved resource bar runs under the notification button");
                    foreach (var button in buttons)
                    {
                        var rect = RectAtWidth((RectTransform)button.transform, width);
                        Assert.IsFalse(rect.Overlaps(spot), $"{aspectName}: {button.name} {rect} sits under the notification button {spot}");
                        if (button.transform.IsChildOf(Named(scene, ResourceBar)) || button.transform.IsChildOf(Named(scene, "LobbyHud"))) continue;
                        Assert.IsFalse(rect.Overlaps(resources), $"{aspectName}: {button.name} {rect} sits under the moved resource bar {resources}");
                    }
                }
            });

        // Hộp thả xuống (spec 5.3): dim bấm được kín màn, hộp 640×660 dưới nút, 5 dòng 600×100.
        [Test]
        public void OverlayBoxes_NotifyAndProfile() =>
            WithLobby(scene =>
            {
                AssertProfileBox(scene);
                var root = Named(scene, "NotifyPopup");
                Assert.IsFalse(root.gameObject.activeSelf, "the box starts closed");
                Assert.AreEqual(new Rect(0, 0, 1920, 1080), RestaurantSceneTests.InReferenceCanvas(root));
                var canvas = root.GetComponent<Canvas>();
                Assert.IsNotNull(canvas);
                Assert.IsTrue(canvas.overrideSorting);
                Assert.Greater(canvas.sortingOrder, Named(scene, Bar).GetComponentInParent<Canvas>().sortingOrder, "drawn over the bar");
                Assert.IsNotNull(root.GetComponent<GraphicRaycaster>());

                var dim = Named(scene, "NotifyDim");
                Assert.IsTrue(dim.IsChildOf(root));
                Assert.AreEqual(new Rect(0, 0, 1920, 1080), RestaurantSceneTests.InReferenceCanvas(dim));
                Assert.IsNotNull(dim.GetComponent<Button>(), "tapping the dim closes the box");
                Assert.IsTrue(dim.GetComponent<Image>().raycastTarget);
                Assert.AreEqual(0.45f, dim.GetComponent<Image>().color.a, 0.01f);

                var box = Named(scene, "NotifyBox_PH_640x660");
                Assert.IsTrue(box.IsChildOf(root));
                AssertRect(1260, 180, 640, 660, FromTop(scene, "NotifyBox_PH_640x660"), "NotifyBox");
                Assert.AreEqual(Vector2.one, box.anchorMin, "top-right anchor");
                var boxTop = FromTop(scene, "NotifyBox_PH_640x660");
                var rows = box.GetComponentsInChildren<Button>(true).Where(b => b.name == "NotifyRow_PH_600x100").ToArray();
                Assert.AreEqual(5, rows.Length);
                for (var i = 0; i < rows.Length; i++)
                {
                    var row = FromTop(RestaurantSceneTests.InReferenceCanvas((RectTransform)rows[i].transform));
                    Assert.AreEqual(new Vector2(600, 100), row.size, $"row {i}");
                    Assert.AreEqual(boxTop.x + 20f, row.x, 0.01f, $"row {i} x");
                    Assert.AreEqual(boxTop.y + 90f + 110f * i, row.y, 0.01f, $"row {i} y");
                }

                Assert.AreEqual("notify.title", box.GetComponentsInChildren<LocalizedText>(true).First(l => l.name == "NotifyTitle").Key);
                Assert.AreEqual("notify.empty", Named(scene, "NotifyEmpty").GetComponent<LocalizedText>().Key);

                var popup = RestaurantSceneTests.All<NotifyPopup>(scene).Single();
                Assert.AreSame(root.gameObject, popup.gameObject, "NotifyPopup sits on its root");
                RestaurantSceneTests.AssertAllReferencesAssigned(popup);
                Assert.IsTrue((object)popup is IHubOverlay);
                var pages = new SerializedObject(RestaurantSceneTests.All<HubNav>(scene).Single()).FindProperty("_pages");
                Assert.IsTrue(Enumerable.Range(0, pages.arraySize).Any(i => ReferenceEquals(pages.GetArrayElementAtIndex(i).objectReferenceValue, popup)), "NotifyPopup is in HubNav._pages");
            });

        // Hộp Hồ sơ (spec 5.4): kín màn qua OverBar, nền không là Button (bấm nền không đóng), hộp 1200×800 giữa màn, ô tên 12 ký tự, lưới cuộn, ô mẫu tắt.
        private static void AssertProfileBox(Scene scene)
        {
            var root = Named(scene, "ProfilePopup");
            Assert.IsFalse(root.gameObject.activeSelf, "the profile box starts closed");
            Assert.AreEqual(new Rect(0, 0, 1920, 1080), RestaurantSceneTests.InReferenceCanvas(root));
            var canvas = root.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.IsTrue(canvas.overrideSorting);
            Assert.Greater(canvas.sortingOrder, Named(scene, Bar).GetComponentInParent<Canvas>().sortingOrder, "drawn over the bar");
            Assert.IsNotNull(root.GetComponent<GraphicRaycaster>());
            var dim = root.GetComponent<Image>();
            Assert.IsNotNull(dim, "the root draws the dim");
            Assert.IsTrue(dim.raycastTarget, "the dim blocks clicks behind it");
            Assert.AreEqual(0.5f, dim.color.a, 0.01f);
            Assert.AreEqual(0f, dim.color.r + dim.color.g + dim.color.b, "black dim");
            Assert.IsNull(root.GetComponent<Button>(), "tapping the dim must not close the box (typed text would be lost)");

            AssertRect(360, 140, 1200, 800, FromTop(scene, "ProfileBox_PH_1200x800"), "ProfileBox");
            Assert.IsTrue(Named(scene, "ProfileBox_PH_1200x800").IsChildOf(root));
            Assert.AreEqual("profile.title", Named(scene, "ProfileTitle").GetComponent<LocalizedText>().Key);
            AssertRect(440, 280, 240, 240, FromTop(scene, "ProfileAvatar_PH_240x240"), "ProfileAvatar");
            var avatar = Named(scene, "ProfileAvatar_PH_240x240");
            Assert.IsNotNull(avatar.GetComponent<AvatarView>());
            RestaurantSceneTests.AssertAllReferencesAssigned(avatar.GetComponent<AvatarView>(), "_followProfile");
            Assert.IsNotNull(avatar.Find("AvatarInitial"));

            var input = Named(scene, "NameInput_PH_400x90").GetComponent<InputField>();
            Assert.IsNotNull(input);
            AssertRect(400, 590, 400, 90, FromTop(scene, "NameInput_PH_400x90"), "NameInput");
            Assert.AreEqual(12, input.characterLimit);
            Assert.AreEqual(InputField.LineType.SingleLine, input.lineType);
            Assert.AreEqual(InputField.ContentType.Standard, input.contentType);
            Assert.IsTrue(input.placeholder is Text, "the placeholder is a Text the popup fills with the default name");
            Assert.IsNull(input.placeholder.GetComponent<LocalizedText>(), "InputField.placeholder is written at runtime, so no LocalizedText");
            Assert.AreEqual("profile.name_hint", Named(scene, "NameHint").GetComponent<LocalizedText>().Key);
            Assert.IsNotNull(Named(scene, "ProfileStatus").GetComponent<Text>());
            Assert.AreEqual("", Named(scene, "ProfileStatus").GetComponent<Text>().text, "no error line at first");
            Assert.AreEqual("profile.change_avatar", Named(scene, "AvatarLabel").GetComponent<LocalizedText>().Key);

            var grid = Named(scene, "AvatarGrid");
            var scroll = grid.GetComponent<ScrollRect>();
            Assert.IsNotNull(scroll);
            Assert.IsTrue(scroll.vertical && !scroll.horizontal, "the grid scrolls vertically only");
            Assert.AreEqual(ScrollRect.MovementType.Clamped, scroll.movementType);
            Assert.IsNotNull(grid.GetComponentInChildren<RectMask2D>(true), "cells are clipped to the grid");
            var template = grid.GetComponentsInChildren<Button>(true).Single(b => b.name == "AvatarCell_PH_120x120");
            Assert.IsFalse(template.gameObject.activeSelf, "the cell template is off");
            Assert.AreEqual(new Vector2(120, 120), ((RectTransform)template.transform).sizeDelta);
            Assert.IsNotNull(template.transform.Find("AvatarSelected_PH_136x136"), "gold ring under the circle");
            Assert.IsNotNull(template.GetComponent<AvatarView>());
            Assert.IsNotNull(template.transform.Find("AvatarInitial"), "a cell shows the character's initial");

            var close = Named(scene, "ProfilePopup").GetComponentsInChildren<Button>(true).Single(b => b.name == "CloseButton");
            Assert.AreEqual(new Vector2(300, 100), ((RectTransform)close.transform).sizeDelta);
            AssertRect(810, 810, 300, 100, FromTop(RestaurantSceneTests.InReferenceCanvas((RectTransform)close.transform)), "CloseButton");

            var popup = RestaurantSceneTests.All<ProfilePopup>(scene).Single();
            Assert.AreSame(root.gameObject, popup.gameObject, "ProfilePopup sits on its root");
            RestaurantSceneTests.AssertAllReferencesAssigned(popup);
            Assert.IsTrue((object)popup is IHubOverlay);
            var nav = new SerializedObject(RestaurantSceneTests.All<HubNav>(scene).Single());
            var pages = nav.FindProperty("_pages");
            Assert.IsTrue(Enumerable.Range(0, pages.arraySize).Any(i => ReferenceEquals(pages.GetArrayElementAtIndex(i).objectReferenceValue, popup)), "ProfilePopup is in HubNav._pages");

            var lobbyAvatar = Named(scene, "Avatar_PH_140x140");
            Assert.IsNotNull(lobbyAvatar.GetComponent<AvatarView>(), "the Lobby avatar shows the chosen character");
            RestaurantSceneTests.AssertAllReferencesAssigned(lobbyAvatar.GetComponent<AvatarView>(), "_followProfile");
            Assert.IsTrue(new SerializedObject(lobbyAvatar.GetComponent<AvatarView>()).FindProperty("_followProfile").boolValue, "the Lobby avatar follows PlayerProfile.Changed");
            Assert.IsFalse(new SerializedObject(avatar.GetComponent<AvatarView>()).FindProperty("_followProfile").boolValue, "the popup draws its own avatar");
            Assert.IsNotNull(lobbyAvatar.GetComponentsInChildren<Transform>(true).SingleOrDefault(t => t.name == "AvatarInitial"));
        }

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
