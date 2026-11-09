using BunDauPeo.UI;
using NUnit.Framework;
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

        [Test]
        public void GachaHistoryPage_FullScreen_BackTopLeft() =>
            WithLobby(scene => FullScreenPage(scene, "HistoryPopup", "Box/CloseButton", typeof(GachaHistoryPopup)));
    }
}
