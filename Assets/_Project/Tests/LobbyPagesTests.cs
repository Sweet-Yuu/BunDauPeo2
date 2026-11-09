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
    // Trang của quán (Nhân viên, Chọn người cho ô, Nâng cấp, Chất lượng) kín màn dưới thanh, và các hộp vẽ trên thanh (spec UI hub 5.1, 5.5).
    // Đọc lại từ file Restaurant.unity (L-002).
    public class LobbyPagesTests
    {
        // Trang kín màn: root phủ 1920×1080, nền đặc chặn bấm, vẽ dưới thanh; nút Quay lại ở góc trên-trái nhãn hub.back;
        // mọi nút (kể cả nút đang tắt, L-025) nằm trên vạch; view của trang nằm trong HubNav._pages.
        private static void FullScreenPage(Scene scene, string rootName, string backPath, System.Type viewType)
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
                         })
                {
                    var root = Named(scene, rootName);
                    Assert.AreEqual(new Rect(0, 0, 1920, 1080), RestaurantSceneTests.InReferenceCanvas(root), $"{rootName} dim covers the screen");
                    var dim = root.GetComponent<Image>();
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
