using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BunDauPeo.Boot;
using BunDauPeo.Core;
using BunDauPeo.Playtest;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BunDauPeo.PlayModeTests
{
    // Trang Nhiệm vụ qua UI thật (spec đợt 3 8.1) trên save tạm và LocalGameServer: không chạm mạng, không chạm save thật.
    public class MissionsPlayTests
    {
        private const float SceneTimeout = 30f;
        private const float ButtonTimeout = 15f;
        private const string OpenButton = "HubRoundBtnMissions_PH_110x110";

        private string _dir;
        private LocalGameServer _server;
        private bool _refused;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _refused = Object.FindFirstObjectByType<GameBootstrap>() != null;
            Assert.IsFalse(_refused, "Đã có GameBootstrap chạy với save thật. Chạy bằng `unity test --mode PlayMode` (Editor đóng).");
            _dir = Path.Combine(Path.GetTempPath(), "bdp-missions-" + Path.GetRandomFileName());
            yield return null;
            GameBootstrap.SavePathOverride = Path.Combine(_dir, "save.json");
            _server = new LocalGameServer();
            GameBootstrap.ServerFactory = () => _server;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_refused) yield break;
            GameBootstrap.SavePathOverride = null;
            GameBootstrap.ServerFactory = null;
            GameLanguage.Current = Language.Vi;
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null) Object.Destroy(bootstrap.gameObject);
            foreach (var name in new[] { "DevOverlay", "OnlineOverlay", "WaitOverlay" })
            {
                var overlay = GameObject.Find(name);
                if (overlay != null) Object.Destroy(overlay);
            }
            ServiceLocator.Clear();
            yield return null;
            // Các test sau cần màn trống (như OnlineWaitTests).
            var game = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("MissionsPlayEmpty"));
            if (game.isLoaded) yield return SceneManager.UnloadSceneAsync(game);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private static IEnumerator WaitForScene(string name)
        {
            var deadline = Time.realtimeSinceStartup + SceneTimeout;
            while (SceneManager.GetActiveScene().name != name)
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"{SceneTimeout}s chờ scene '{name}', đang ở '{SceneManager.GetActiveScene().name}'. Nút: {UiDriver.DescribeVisibleButtons()}");
                yield return null;
            }
            yield return null;
        }

        private static IEnumerator ClickWhenReady(string name)
        {
            var inner = UiDriver.WaitForButton(name, ButtonTimeout);
            while (true)
            {
                try
                {
                    if (!inner.MoveNext()) break;
                }
                catch (TimeoutException e)
                {
                    Assert.Fail(e.Message);
                }
                yield return inner.Current;
            }

            var deadline = Time.realtimeSinceStartup + 5f;
            var result = UiDriver.Click(name);
            while (result.Status == ClickStatus.Blocked && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                result = UiDriver.Click(name);
            }

            Assert.AreEqual(ClickStatus.Clicked, result.Status, result.ToString());
            yield return null;
        }

        private static IEnumerator BootToLobby()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            yield return ClickWhenReady("TapArea");
            yield return WaitForScene("Restaurant");
        }

        private static MissionRowView[] ShownRows() =>
            Object.FindObjectsByType<MissionRowView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        [UnityTest, Timeout(180000)]
        public IEnumerator OpenFromLobby_ShowsDailyAndNewbieRows()
        {
            yield return BootToLobby();
            yield return ClickWhenReady(OpenButton);
            yield return null;
            Assert.AreEqual(5, ShownRows().Length, "daily tab: 5 rows (one locked)");
            Assert.AreEqual(1, ShownRows().Count(r => r.State == MissionButtonState.Locked));
            ScreenCapture1080.Capture("o3b-missions-daily-en.png");

            var tabs = UiDriver.FindButtons("MissionTab_PH_300x80").OrderBy(b => b.transform.GetSiblingIndex()).ToList();
            Assert.AreEqual(2, tabs.Count);
            var tabClick = UiDriver.Click(tabs[1]);
            Assert.AreEqual(ClickStatus.Clicked, tabClick.Status, tabClick.ToString());
            yield return null;
            Assert.AreEqual(9, ShownRows().Length, "newbie tab: 9 rows");
            Assert.AreEqual(0, ShownRows().Count(r => r.State == MissionButtonState.Locked));
            ScreenCapture1080.Capture("o3b-missions-newbie-en.png");

            // Quay lại → Lobby.
            Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click(UiDriver.FindButtons("CloseButton").First(b => b.transform.IsChildOf(GameObject.Find("MissionsPageRoot").transform))).Status);
            yield return null;
            Assert.AreEqual(0, ShownRows().Length, "closing the page hides the rows");
        }

        // Đặt tên trong Hồ sơ là việc tân thủ do máy báo: đủ điều kiện mà không cần chờ giờ hay đánh trận.
        [UnityTest, Timeout(180000)]
        public IEnumerator Claim_AddsGem_ButtonBecomesClaimed()
        {
            yield return BootToLobby();
            var wallet = ServiceLocator.Get<Wallet>();
            ServiceLocator.Get<PlayerProfile>().SetName("Bun");
            var before = wallet.Get(CurrencyType.Gem);

            yield return ClickWhenReady(OpenButton);
            var tabs = UiDriver.FindButtons("MissionTab_PH_300x80").OrderBy(b => b.transform.GetSiblingIndex()).ToList();
            var tabClick = UiDriver.Click(tabs[1]);
            Assert.AreEqual(ClickStatus.Clicked, tabClick.Status, tabClick.ToString());
            yield return null;
            var row = ShownRows().Single(r => r.Id == "new_name");
            Assert.AreEqual(MissionButtonState.Claim, row.State);

            Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click(row.Button).Status);
            yield return null;
            yield return null;

            Assert.AreEqual(before + 50, wallet.Get(CurrencyType.Gem));
            Assert.AreEqual(MissionButtonState.Claimed, ShownRows().Single(r => r.Id == "new_name").State);
        }

        // Review Focus 4 (đợt 2) cho lệnh mới: lúc nhận thưởng thanh dưới bị chặn.
        [UnityTest, Timeout(180000)]
        public IEnumerator Wait_BlocksBottomBar_WhileClaiming()
        {
            yield return BootToLobby();
            ServiceLocator.Get<PlayerProfile>().SetName("Bun");
            yield return ClickWhenReady(OpenButton);
            var tabs = UiDriver.FindButtons("MissionTab_PH_300x80").OrderBy(b => b.transform.GetSiblingIndex()).ToList();
            UiDriver.Click(tabs[1]);
            yield return null;
            var row = ShownRows().Single(r => r.Id == "new_name");

            var open = new TaskCompletionSource<bool>();
            _server.DelayNext(open);
            Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click(row.Button).Status);
            yield return null;

            Assert.IsTrue(WaitOverlay.IsBlocking, "lớp chờ bật ngay khi gửi lệnh nhận");
            Assert.AreEqual(ClickStatus.Blocked, UiDriver.Click("NavTabStages_PH_320x120").Status);

            open.SetResult(true);
            var deadline = Time.realtimeSinceStartup + SceneTimeout;
            while (WaitOverlay.IsBlocking)
            {
                if (Time.realtimeSinceStartup > deadline) Assert.Fail($"{SceneTimeout}s chờ lệnh nhận xong");
                yield return null;
            }

            Assert.AreEqual(MissionButtonState.Claimed, ShownRows().Single(r => r.Id == "new_name").State);
        }

        // L-045: kéo và lăn chuột từ dòng, nút và khe giữa hai dòng đều tới ScrollRect (người chơi không phải bấm đúng nút rồi mới vuốt).
        [UnityTest, Timeout(180000)]
        public IEnumerator Scroll_DragAndWheel_ReachScrollRect_FromRowButtonAndGap()
        {
            yield return BootToLobby();
            yield return ClickWhenReady(OpenButton);
            var tabs = UiDriver.FindButtons("MissionTab_PH_300x80").OrderBy(b => b.transform.GetSiblingIndex()).ToList();
            UiDriver.Click(tabs[1]);
            yield return null;
            yield return null;

            var scroll = GameObject.Find("MissionsPageRoot").GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            var rows = ShownRows().OrderBy(r => r.transform.GetSiblingIndex()).ToList();
            Assert.Greater(rows.Count, 3);
            Vector2 Screen(Vector3 world) => RectTransformUtility.WorldToScreenPoint(null, world);
            var first = (RectTransform)rows[0].transform;
            var second = (RectTransform)rows[1].transform;
            var points = new (string Name, Vector2 Point)[]
            {
                ("row body", Screen(first.TransformPoint(new Vector3(first.rect.xMin + 400f, first.rect.center.y, 0f)))),
                ("row button", Screen(rows[0].Button.transform.position)),
                ("gap between rows", Screen((first.TransformPoint(new Vector3(0f, first.rect.yMin, 0f)) + second.TransformPoint(new Vector3(0f, second.rect.yMax, 0f))) / 2f)),
            };

            foreach (var (name, point) in points)
            {
                var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = point }, hits);
                Assert.IsNotEmpty(hits, $"{name} at {point}: nothing under the pointer");
                var target = hits[0].gameObject;
                Assert.AreSame(scroll.gameObject, UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IDragHandler>(target), $"{name}: drag does not reach the list ({target.name})");
                Assert.AreSame(scroll.gameObject, UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IScrollHandler>(target), $"{name}: mouse wheel does not reach the list ({target.name})");
            }
        }
    }
}
