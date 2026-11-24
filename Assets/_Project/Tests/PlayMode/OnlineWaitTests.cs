using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using BunDauPeo.Boot;
using BunDauPeo.Core;
using BunDauPeo.Playtest;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BunDauPeo.PlayModeTests
{
    // Màn chờ và hộp Cần cập nhật (spec con 7.1, 7.2) qua UI thật, trên save tạm và server giả (không chạm mạng, không chạm save thật).
    public class OnlineWaitTests
    {
        private const float SceneTimeout = 30f;
        private const float ButtonTimeout = 15f;

        private string _dir;
        private LocalGameServer _server;
        private bool _refused;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _refused = Object.FindFirstObjectByType<GameBootstrap>() != null;
            Assert.IsFalse(_refused, "Đã có GameBootstrap chạy với save thật. Chạy bằng `unity test --mode PlayMode` (Editor đóng).");
            _dir = Path.Combine(Path.GetTempPath(), "bdp-wait-" + Path.GetRandomFileName());
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
            // Các test sau (PlaytestKitPlayTests) cần màn trống: bỏ scene game còn lại, kẻo Canvas và camera của nó lẫn vào ảnh chụp.
            var game = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("OnlineWaitEmpty"));
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

            // Test đầu tiên của phiên: Canvas mới dựng có thể chưa raycast trúng gì ở khung đầu; thử lại tới 5 giây rồi mới báo lỗi.
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

        private static IEnumerator WaitUntil(Func<bool> condition, string what)
        {
            var deadline = Time.realtimeSinceStartup + SceneTimeout;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) Assert.Fail($"{SceneTimeout}s chờ {what}");
                yield return null;
            }
        }

        // Review Focus 4: chạm thanh dưới khi đang chờ lệnh server thì không đổi trang; chữ chỉ hiện sau 0,5 giây.
        [UnityTest, Timeout(180000)]
        public IEnumerator Wait_BlocksBottomBar()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            yield return ClickWhenReady("TapArea");
            yield return WaitForScene("Restaurant");

            var gate = ServiceLocator.Get<CommandGate>();
            var open = new TaskCompletionSource<bool>();
            _server.DelayNext(open);
            var run = gate.RunAsync(id => _server.DevAddGemAsync(id, 1));
            yield return null;

            Assert.IsTrue(WaitOverlay.IsBlocking, "lớp chờ bật ngay khi gửi");
            Assert.IsFalse(WaitOverlay.IsSpinnerShown, "chưa đủ 0,5 giây: chưa nháy gì");
            var blocked = UiDriver.Click("NavTabStages_PH_320x120");
            Assert.AreEqual(ClickStatus.Blocked, blocked.Status, blocked.ToString());
            StringAssert.Contains("WaitBlocker", blocked.Message);

            yield return new WaitForSecondsRealtime(WaitOverlay.ShowAfterSeconds + 0.3f);
            Assert.IsTrue(WaitOverlay.IsSpinnerShown, "sau 0,5 giây hiện ô xoay");
            var label = GameObject.Find("WaitLabel_PH_400x60");
            Assert.IsNotNull(label);
            Assert.AreEqual(UiText.Get("online.sending"), label.GetComponent<Text>().text);
            Assert.AreEqual("Sending…", label.GetComponent<Text>().text, "game mới: tiếng Anh");
            Assert.IsFalse(run.IsCompleted);
            ScreenCapture1080.Capture("online2-wait-en.png");

            open.SetResult(true);
            yield return WaitUntil(() => run.IsCompleted, "lệnh xong");
            yield return null;
            Assert.IsFalse(WaitOverlay.IsBlocking, "xong thì tắt lớp chờ");
            Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click("NavTabStages_PH_320x120").Status, "thanh dưới bấm lại được");
        }

        // Bấm nút thật (raycast) hai lần khi server giữ lệnh: lệnh tới server đúng một lần, lần bấm hai bị lớp chờ chặn; xong thì nút bấm lại được.
        private IEnumerator PressTwiceWhileHeld(string button, Func<bool> done)
        {
            var calls = _server.CommandCalls;
            var open = new TaskCompletionSource<bool>();
            _server.DelayNext(open);

            try
            {
                Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click(button).Status, button);
                yield return null;
                yield return null;
                Assert.IsTrue(WaitOverlay.IsBlocking, button + ": lớp chờ phải bật");
                Assert.AreNotEqual(ClickStatus.Clicked, UiDriver.Click(button).Status, button + ": lần bấm hai không được tới nút");
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.AreEqual(calls + 1, _server.CommandCalls, button + ": server nhận đúng một lệnh");
            }
            finally
            {
                // Test đỏ giữa chừng mà lệnh còn treo thì lớp chờ kẹt và kéo đỏ các test sau.
                open.TrySetResult(true);
            }

            yield return WaitUntil(() => !WaitOverlay.IsBlocking, button + ": lớp chờ tắt");
            Assert.IsTrue(done(), button + ": lệnh đã xong");
            Assert.AreEqual(calls + 1, _server.CommandCalls, button + ": vẫn đúng một lệnh sau khi xong");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator DoublePress_Gacha_OneCommand()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            yield return ClickWhenReady("TapArea");
            yield return WaitForScene("Restaurant");
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            _server.State.Gem += 5000;
            session.ApplyServerState(_server.State);
            var pulls = _server.State.GachaHistory.Count;
            yield return ClickWhenReady("NavTabGacha_PH_320x120");

            yield return PressTwiceWhileHeld("TenButton", () => _server.State.GachaHistory.Count == pulls + 1);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator DoublePress_StarUp_OneCommand()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            yield return ClickWhenReady("TapArea");
            yield return WaitForScene("Restaurant");
            var session = Object.FindFirstObjectByType<GameBootstrap>().Session;
            var stars = ServiceLocator.Get<BunDauPeo.Characters.StarService>();
            var id = session.Roster.GetOwned()[0].Id;
            var cost = stars.GetNextCost(id);
            Assert.Greater(cost, 0, "nhân vật đầu đã tối đa sao");
            // Đủ mảnh cho hai lần nâng: lần bấm hai mà lọt qua thì server nhận hai lệnh.
            _server.State.Fragments.Add(new FragmentCount { CharacterId = id, Count = cost * 2 });
            session.ApplyServerState(_server.State);
            var before = stars.GetStars(id);
            yield return ClickWhenReady("NavTabCharacters_PH_320x120");
            yield return ClickWhenReady("CharacterCard0");
            yield return ClickWhenReady("StarTabButton");

            yield return PressTwiceWhileHeld("StarUpButton", () => stars.GetStars(id) == before + 1);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator DoublePress_EnterStage_OneCommand()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            yield return ClickWhenReady("TapArea");
            yield return WaitForScene("Restaurant");
            var combat = ServiceLocator.Get<BunDauPeo.Combat.CombatService>();
            var stages = combat.Config.Stages.Items;
            var battleIndex = 0;
            while (stages[battleIndex].Kind != BunDauPeo.Combat.StageKind.Battle) battleIndex++;
            yield return ClickWhenReady("NavTabStages_PH_320x120");
            yield return ClickWhenReady("ModeMain_PH_820x560");
            for (var i = 0; i < battleIndex; i++)
            {
                yield return ClickWhenReady("Node" + i);
                yield return ClickWhenReady("ContinueButton");
            }

            yield return ClickWhenReady("Node" + battleIndex);
            yield return ClickWhenReady("ChallengeButton");
            yield return ClickWhenReady("TeamCard0");

            // Một thẻ đội có thể hỏi xác nhận trước khi gửi (NeedsConfirm): hộp đó không phải lệnh, nên chỉ đếm từ lúc gửi.
            var calls = _server.CommandCalls;
            var open = new TaskCompletionSource<bool>();
            _server.DelayNext(open);
            Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click("StartButton").Status);
            yield return null;
            if (GameObject.Find("SmallConfirmYesButton") != null) yield return ClickWhenReady("SmallConfirmYesButton");
            yield return WaitUntil(() => WaitOverlay.IsBlocking, "lớp chờ bật khi vào trận");
            try
            {
                Assert.AreNotEqual(ClickStatus.Clicked, UiDriver.Click("StartButton").Status, "lần bấm hai không được tới nút");
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.AreEqual(calls + 1, _server.CommandCalls, "server nhận đúng một lệnh vào màn");
            }
            finally
            {
                open.TrySetResult(true);
            }

            yield return WaitForScene(BunDauPeo.UI.BattleView.BattleScene);
            Assert.AreEqual(calls + 1, _server.CommandCalls);
        }

        // Hộp Cần cập nhật ở Tiêu đề (bước Bootstrap), không nút, bản tiếng Anh dài nhất vừa khung 900 px (L-039).
        [UnityTest, Timeout(180000)]
        public IEnumerator TooOld_AtTitle_ShowsBoxWithoutButton()
        {
            _server.ProtocolVersion = 1;
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            yield return ClickWhenReady("TapArea");
            yield return WaitUntil(() => OnlineOverlay.IsShown, "hộp Cần cập nhật");
            yield return null;

            Assert.AreEqual(TitleView.TitleScene, SceneManager.GetActiveScene().name, "không vào game");
            Assert.IsNotNull(GameObject.Find("OnlineBox_PH_900x420"));
            Assert.AreEqual("Update required", GameObject.Find("OnlineTitle").GetComponent<Text>().text);
            StringAssert.Contains("out of date", GameObject.Find("OnlineBody").GetComponent<Text>().text);
            Assert.IsNull(GameObject.Find("OnlineRetryBtn_PH_340x110"), "hộp này không có nút (GameObject.Find bỏ qua object tắt)");
            ScreenCapture1080.Capture("online2-too-old-en.png");
        }
    }
}
