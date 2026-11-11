using System.Linq;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BunDauPeo.Tests.LobbyPagesTests;
using static BunDauPeo.Tests.LobbySceneTests;

namespace BunDauPeo.Tests
{
    // Trang con của Bản đồ và Gacha kín màn dưới thanh (spec UI hub 5.5), cùng khuôn với trang của quán (LobbyPagesTests.FullScreenPage).
    // Đọc lại từ file Restaurant.unity (L-002).
    public class PanelPagesTests
    {
        [Test]
        public void StageInfoPage_FullScreen_BackTopLeft() =>
            WithLobby(scene => FullScreenPage(scene, "InfoRoot", "InfoBox/CancelButton", typeof(StageInfoPopup)));

        [Test]
        public void TeamSelectPage_FullScreen_BackTopLeft() =>
            WithLobby(scene => FullScreenPage(scene, "TeamRoot", "TeamBox/CancelButton", typeof(TeamSelectPopup)));

        [Test]
        public void StoryPage_FullScreen_BackTopLeft() =>
            WithLobby(scene => FullScreenPage(scene, "StoryRoot", "StoryBox/CloseButton", typeof(StoryPlaceholderPopup)));

        // Lịch sử quay là hộp giữa màn vẽ trên thanh (spec UI hub B 5.7). Vẫn là IHubPage trong HubNav._pages để Reset và đổi ngôn ngữ đóng được (L-035).
        [Test]
        public void GachaHistory_IsBoxOverBar_AndHubPage() =>
            WithLobby(scene =>
            {
                var root = Named(scene, "HistoryPopup");
                Assert.AreEqual(new Rect(0, 0, 1920, 1080), RestaurantSceneTests.InReferenceCanvas(root), "dim covers the screen");
                var dim = root.GetComponent<Image>();
                Assert.IsTrue(dim.raycastTarget, "dim must block the bar");
                Assert.AreEqual(0.55f, dim.color.a, 0.02f);
                Assert.Less(dim.color.r + dim.color.g + dim.color.b, 0.05f, "dim is black");

                var canvas = root.GetComponent<Canvas>();
                Assert.IsNotNull(canvas, "needs its own Canvas to draw over the bar");
                Assert.IsTrue(canvas.overrideSorting);
                Assert.Greater(canvas.sortingOrder, Named(scene, Bar).GetComponentInParent<Canvas>().sortingOrder, "draws under the bar");
                Assert.IsNotNull(root.GetComponent<GraphicRaycaster>());

                var box = Named(scene, "GachaHistoryBox_PH_1200x800");
                Assert.AreSame(root, box.parent);
                var boxRect = RestaurantSceneTests.InReferenceCanvas(box);
                Assert.AreEqual(new Vector2(1200, 800), boxRect.size);
                Assert.AreEqual(new Vector2(960, 540), boxRect.center, "box is centred");

                var close = box.Find("CloseButton") as RectTransform;
                Assert.IsNotNull(close, "GachaHistoryBox_PH_1200x800/CloseButton");
                Assert.IsNotNull(close.GetComponent<Button>());
                var closeRect = RestaurantSceneTests.InReferenceCanvas(close);
                Assert.AreEqual(new Vector2(300, 100), closeRect.size);
                Assert.IsTrue(boxRect.Contains(closeRect.min) && boxRect.Contains(closeRect.max - Vector2.one), "Close stays in the box");
                Assert.AreEqual("common.close", close.GetComponentInChildren<LocalizedText>(true).Key);

                var pages = new SerializedObject(RestaurantSceneTests.All<HubNav>(scene).Single()).FindProperty("_pages");
                Assert.IsTrue(Enumerable.Range(0, pages.arraySize).Any(i => pages.GetArrayElementAtIndex(i).objectReferenceValue is GachaHistoryPopup),
                    "HubNav._pages lacks GachaHistoryPopup");
            });
    }
}
