using System.Linq;
using BunDauPeo.EditorTools;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static BunDauPeo.Tests.LobbySceneTests;

namespace BunDauPeo.Tests
{
    // Trang của quán (Nhân viên, Chọn người cho ô, Nâng cấp, Chất lượng) kín màn dưới thanh, và các hộp vẽ trên thanh (spec UI hub 5.1, 5.5);
    // `FullScreenPage` dùng chung với PanelPagesTests (trang của Bản đồ và Gacha).
    // Đọc lại từ file Restaurant.unity (L-002).
    public class LobbyPagesTests
    {
        // Trang kín màn: root phủ 1920×1080, nền đặc chặn bấm, vẽ dưới thanh; nút Quay lại ở góc trên-trái nhãn hub.back;
        // mọi nút (kể cả nút đang tắt, L-025) nằm trên vạch; view của trang nằm trong HubNav._pages.
        internal static void FullScreenPage(Scene scene, string rootName, string backPath, System.Type viewType)
        {
            var root = Named(scene, rootName);
            Assert.AreEqual(new Rect(0, 0, 1920, 1080), RestaurantSceneTests.InReferenceCanvas(root), $"{rootName} is full screen");
            var image = root.GetComponent<Image>();
            Assert.IsTrue(image.raycastTarget, $"{rootName} must block clicks behind it");
            Assert.GreaterOrEqual(image.color.a, 0.99f, $"{rootName} must be an opaque page, not a dim");
            Assert.IsFalse(root.GetComponentsInParent<Canvas>(true).Any(c => c.overrideSorting), $"{rootName} must draw under the bar");

            var canvas = root.GetComponentInParent<Canvas>(true).transform;
            var top = root;
            while (top.parent != canvas) top = (RectTransform)top.parent;
            Assert.Less(top.GetSiblingIndex(), Named(scene, Bar).GetSiblingIndex(), $"{rootName} draws under the bar");

            var back = root.Find(backPath) as RectTransform;
            Assert.IsNotNull(back, $"{rootName}/{backPath}");
            Assert.IsNotNull(back.GetComponent<Button>(), $"{backPath} is a button");
            var rect = RestaurantSceneTests.InReferenceCanvas(back);
            Assert.LessOrEqual(rect.xMin, 60f, $"{backPath} {rect} is not at the left");
            Assert.GreaterOrEqual(rect.yMax, 1020f, $"{backPath} {rect} is not at the top");
            Assert.AreEqual("hub.back", back.GetComponentInChildren<LocalizedText>(true).Key, $"{backPath} label");

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var buttonRect = RestaurantSceneTests.InReferenceCanvas((RectTransform)button.transform);
                Assert.GreaterOrEqual(buttonRect.yMin, HubNav.BarHeight, $"{button.name} {buttonRect} reaches the bar strip");
            }

            var pages = new SerializedObject(RestaurantSceneTests.All<HubNav>(scene).Single()).FindProperty("_pages");
            var registered = Enumerable.Range(0, pages.arraySize).Select(i => pages.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            Assert.IsTrue(registered.Any(p => p != null && p.GetType() == viewType), $"HubNav._pages lacks {viewType.Name}");
        }

        [Test]
        public void StaffPage_FullScreen_BackTopLeft() =>
            WithLobby(scene => FullScreenPage(scene, "StaffPanelRoot", "Box/CloseButton", typeof(StaffPanel)));

        [Test]
        public void StaffListPage_FullScreen_BackTopLeft() =>
            WithLobby(scene => FullScreenPage(scene, "StaffListPopup", "Box/CloseButton", typeof(StaffPanel)));

        [Test]
        public void UpgradePage_FullScreen_BackTopLeft() =>
            WithLobby(scene => FullScreenPage(scene, "UpgradePanelRoot", "Box/CloseButton", typeof(UpgradeTestPanel)));

        // Spec UI hub B 5.7: ba cột × ba ô 500×220, mỗi ô có chân dung, tên, dòng phụ và nút Gỡ nằm trong ô; đáy hàng cuối y 930 (trên vạch).
        [Test]
        public void StaffPage_GridOfNine_AboveBar() =>
            WithLobby(scene =>
            {
                var centersX = new[] { 390f, 960f, 1530f };
                var tops = new[] { 210f, 460f, 710f };
                var panel = new SerializedObject(RestaurantSceneTests.All<StaffPanel>(scene).Single());
                var infoTexts = panel.FindProperty("_slotInfoTexts");
                var portraits = panel.FindProperty("_portraits");
                var texts = panel.FindProperty("_slotTexts");
                Assert.AreEqual(9, infoTexts.arraySize, "_slotInfoTexts");
                Assert.AreEqual(9, portraits.arraySize, "_portraits");
                for (var role = 0; role < 3; role++)
                {
                    for (var slot = 0; slot < 3; slot++)
                    {
                        var index = role * 3 + slot;
                        var name = $"Slot{role}_{slot}";
                        var cell = Named(scene, name);
                        Assert.IsNotNull(cell.GetComponent<Button>(), name + " is a Button");
                        AssertRect(centersX[role] - 250f, tops[slot], 500f, 220f, FromTop(scene, name), name);

                        var portrait = (Image)portraits.GetArrayElementAtIndex(index).objectReferenceValue;
                        Assert.IsNotNull(portrait, $"_portraits[{index}]");
                        Assert.AreEqual("Portrait_PH_140x140", portrait.name);
                        Assert.IsTrue(portrait.transform.IsChildOf(cell), $"{name} owns its portrait");
                        Assert.AreEqual(new Vector2(140, 140), RestaurantSceneTests.InReferenceCanvas((RectTransform)portrait.transform).size);

                        foreach (var (label, text) in new[] { ("_slotTexts", texts), ("_slotInfoTexts", infoTexts) })
                        {
                            var component = (Text)text.GetArrayElementAtIndex(index).objectReferenceValue;
                            Assert.IsNotNull(component, $"{label}[{index}]");
                            Assert.IsTrue(component.transform.IsChildOf(cell), $"{label}[{index}] belongs to {name}");
                        }

                        var remove = cell.Find("RemoveButton") as RectTransform;
                        Assert.IsNotNull(remove, name + "/RemoveButton");
                        var removeRect = RestaurantSceneTests.InReferenceCanvas(remove);
                        Assert.AreEqual(new Vector2(120, 60), removeRect.size);
                        var cellRect = RestaurantSceneTests.InReferenceCanvas(cell);
                        Assert.IsTrue(cellRect.Contains(removeRect.min) && cellRect.Contains(removeRect.max - Vector2.one), $"{name}: RemoveButton {removeRect} leaves its slot {cellRect}");
                        Assert.GreaterOrEqual(cellRect.yMin, HubNav.BarHeight, $"{name} reaches the bar strip");
                    }
                }
            });

        // Spec UI hub B 5.7: thẻ nâng cấp 540×360 lưới 3×2, thẻ mẫu không phải Button, nút Mua riêng.
        [Test]
        public void UpgradePage_CardsWithBuyButton() =>
            WithLobby(scene =>
            {
                var panel = new SerializedObject(RestaurantSceneTests.All<UpgradeTestPanel>(scene).Single());
                var template = (UpgradeCardView)panel.FindProperty("_cardTemplate").objectReferenceValue;
                Assert.IsNotNull(template, "_cardTemplate");
                Assert.AreEqual("UpgradeCard", template.name);
                Assert.IsNull(template.GetComponent<Button>(), "the card itself is not a Button");
                var card = new SerializedObject(template);
                foreach (var field in new[] { "_nameText", "_levelText", "_costText" })
                {
                    var text = (Text)card.FindProperty(field).objectReferenceValue;
                    Assert.IsNotNull(text, field);
                    Assert.IsTrue(text.transform.IsChildOf(template.transform), field);
                }

                var buy = (Button)card.FindProperty("_buyButton").objectReferenceValue;
                Assert.IsNotNull(buy, "_buyButton");
                Assert.AreEqual("BuyButton_PH_210x80", buy.name);
                Assert.IsTrue(buy.transform.IsChildOf(template.transform));
                Assert.AreEqual(new Vector2(210, 80), RestaurantSceneTests.InReferenceCanvas((RectTransform)buy.transform).size);
                Assert.AreEqual("restaurant.buy", buy.GetComponentInChildren<LocalizedText>(true).Key);

                var slots = panel.FindProperty("_cardSlots");
                Assert.AreEqual(6, slots.arraySize, "3 × 2 card slots");
                for (var i = 0; i < 6; i++)
                {
                    var slot = (RectTransform)slots.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.IsNotNull(slot, $"_cardSlots[{i}]");
                    var rect = FromTop(RestaurantSceneTests.InReferenceCanvas(slot));
                    AssertRect(new[] { 120f, 690f, 1260f }[i % 3], new[] { 150f, 550f }[i / 3], 540f, 360f, rect, $"card slot {i}");
                    Assert.GreaterOrEqual(1080f - rect.yMax, HubNav.BarHeight, $"card slot {i} reaches the bar strip");
                }

                var title = In<LocalizedText>(scene, "UpgradePanelRoot").Single(t => t.name == "TitleText");
                Assert.AreEqual("restaurant.upgrade_title", title.Key);
            });

        [Test]
        public void QualityPage_FullScreen_BackTopLeft() =>
            WithLobby(scene => FullScreenPage(scene, "QualityPopupRoot", "QualityPopupBox/CloseButton", typeof(QualityPopup)));

        // Hộp xác nhận và phần thưởng: nền mờ phủ kín màn và vẽ trên thanh (Canvas con overrideSorting), hộp giữ cỡ cũ.
        [Test]
        public void Boxes_DimFullScreen_DrawnOverBar() =>
            WithLobby(scene =>
            {
                var barOrder = Named(scene, Bar).GetComponentInParent<Canvas>().sortingOrder;
                foreach (var (rootName, boxName, size) in new[]
                         {
                             ("StaffConfirmPopup", "Box", RestaurantSceneBuilder.StaffConfirmSize),
                             ("AfkPopupRoot", "AfkPopupBox", RestaurantSceneBuilder.AfkPopupSize),
                             ("ResetConfirmRoot", "ResetConfirm_PH_800x420", NavBarBuilder.ResetConfirmSize),
                             ("SmallConfirmRoot", "SmallConfirmBox", StageSelectSceneBuilder.ConfirmSize),
                             ("StaffConfirmRoot", "StaffConfirmBox", StageSelectSceneBuilder.ConfirmSize),
                             ("RollAgainPopup", "Box", GachaSceneBuilder.RollAgainSize),
                             ("HistoryPopup", "GachaHistoryBox_PH_1200x800", GachaSceneBuilder.HistoryBoxSize),
                             ("NotifyPopup", "NotifyBox_PH_640x660", new Vector2(640, 660)),
                         })
                {
                    var root = Named(scene, rootName);
                    Assert.AreEqual(new Rect(0, 0, 1920, 1080), RestaurantSceneTests.InReferenceCanvas(root), $"{rootName} dim covers the screen");
                    // Hộp Thông báo để nền mờ ở con NotifyDim (là nút đóng hộp); các hộp khác để ngay trên root.
                    var dim = root.GetComponent<Image>() ?? root.Find("NotifyDim").GetComponent<Image>();
                    Assert.IsTrue(dim.raycastTarget, $"{rootName} dim must block the bar");
                    Assert.Less(dim.color.a, 0.99f, $"{rootName} is a dim, not an opaque page");

                    var canvas = root.GetComponent<Canvas>();
                    Assert.IsNotNull(canvas, $"{rootName} needs its own Canvas to draw over the bar");
                    Assert.IsTrue(canvas.overrideSorting, rootName);
                    Assert.Greater(canvas.sortingOrder, barOrder, $"{rootName} draws under the bar");
                    Assert.IsNotNull(root.GetComponent<GraphicRaycaster>(), $"{rootName} needs a GraphicRaycaster");

                    var box = (RectTransform)root.Find(boxName);
                    Assert.IsNotNull(box, $"{rootName}/{boxName}");
                    Assert.AreEqual(size, RestaurantSceneTests.InReferenceCanvas(box).size, $"{boxName} keeps its old size");
                }
            });
    }
}
