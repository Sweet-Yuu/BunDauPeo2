using System;
using System.Collections;
using System.IO;
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
    // Tài khoản qua UI thật (spec đợt 3 8.3) trên save tạm, LocalGameServer và LocalAccountService: không chạm mạng, không chạm save thật.
    public class AccountPlayTests
    {
        private const float SceneTimeout = 30f;
        private const float ButtonTimeout = 15f;
        private const string Strong = "Abcdef1!";

        private string _dir;
        private LocalGameServer _server;
        private LocalAccountService _account;
        private bool _refused;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _refused = Object.FindFirstObjectByType<GameBootstrap>() != null;
            Assert.IsFalse(_refused, "Đã có GameBootstrap chạy với save thật. Chạy bằng `unity test --mode PlayMode` (Editor đóng).");
            _dir = Path.Combine(Path.GetTempPath(), "bdp-account-" + Path.GetRandomFileName());
            yield return null;
            GameBootstrap.SavePathOverride = Path.Combine(_dir, "save.json");
            _server = new LocalGameServer();
            _account = new LocalAccountService("local-player");
            GameBootstrap.ServerFactory = () => _server;
            GameBootstrap.AccountFactory = () => _account;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_refused) yield break;
            GameBootstrap.SavePathOverride = null;
            GameBootstrap.ServerFactory = null;
            GameBootstrap.AccountFactory = null;
            GameLanguage.Current = Language.Vi;
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null) Object.Destroy(bootstrap.gameObject);
            foreach (var name in new[] { "DevOverlay", "OnlineOverlay", "WaitOverlay", "AccountBoxes" })
            {
                var overlay = GameObject.Find(name);
                if (overlay != null) Object.Destroy(overlay);
            }

            ServiceLocator.Clear();
            yield return null;
            var game = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("AccountPlayEmpty" + Guid.NewGuid().ToString("N")));
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

        private static InputField Input(string name) => GameObject.Find(name).GetComponent<InputField>();

        [UnityTest, Timeout(180000)]
        public IEnumerator Profile_LinkUserPass_ShowsLinked()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            yield return ClickWhenReady("TapArea");
            yield return WaitForScene("Restaurant");

            yield return ClickWhenReady("Avatar_PH_140x140");
            var accountText = GameObject.Find("ProfileAccountText").GetComponent<Text>();
            StringAssert.Contains(UiText.Get("account.not_linked"), accountText.text);

            var linkButton = GameObject.Find("ProfileAccountBtn_PH_160x55");
            yield return ClickWhenReady("ProfileAccountBtn_PH_160x55");
            yield return ClickWhenReady("AccountUserPassBtn_PH_700x90");
            Input("CredentialUser_PH_700x80").text = "abc";
            Input("CredentialPass_PH_700x80").text = Strong;
            yield return ClickWhenReady("CredentialActionBtn_PH_380x90");

            var deadline = Time.realtimeSinceStartup + 5f;
            while (!_account.IsLinked && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;

            Assert.IsTrue(_account.IsLinked);
            StringAssert.Contains("abc", accountText.text);
            Assert.IsFalse(AccountBoxes.IsShown, "the boxes close after a successful link");
            Assert.IsFalse(linkButton.activeInHierarchy, "no link button once linked");
        }

        // Đường UI thật của hàng rào mất tiến độ (Major 2 review đợt 3): hộp xác nhận chặn trước SignIn, Huỷ không đổi gì, Đăng nhập mới đổi.
        [UnityTest, Timeout(60000)]
        public IEnumerator SignIn_OverAnonymousProgress_ConfirmBoxGatesSignIn()
        {
            var directory = new LocalAccountDirectory();
            var owner = new LocalAccountService("b", directory, google: true);
            owner.LinkGoogleAsync().GetAwaiter().GetResult();
            var anonymous = new LocalAccountService("a", directory, google: true);
            var signedIn = false;
            var flow = new AccountFlow(anonymous, new AccountSwitcher(async signIn =>
            {
                var result = await signIn();
                signedIn |= result.Ok;
                return result;
            }), () => { }, () => true);

            // Không có scene game nên tự dựng EventSystem (UiDriver cần); TearDown dọn qua scene rỗng.
            var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            AccountBoxes.ShowSignIn(flow);
            yield return ClickWhenReady("AccountGoogleBtn_PH_700x90");
            Assert.IsNotNull(GameObject.Find("ConfirmOkBtn_PH_340x100"), "the confirm box shows before SignIn");
            Assert.AreEqual("a", anonymous.PlayerId);
            Assert.IsFalse(signedIn, "nothing is called before the confirmation");

            yield return ClickWhenReady("ConfirmCancelBtn_PH_340x100");
            Assert.IsFalse(AccountBoxes.IsShown);
            Assert.AreEqual("a", anonymous.PlayerId, "Cancel keeps the anonymous progress");

            AccountBoxes.ShowSignIn(flow);
            yield return ClickWhenReady("AccountGoogleBtn_PH_700x90");
            yield return ClickWhenReady("ConfirmOkBtn_PH_340x100");
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!signedIn && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(signedIn, "Confirm runs the sign-in");
            Assert.AreEqual("b", anonymous.PlayerId);
            Object.Destroy(eventSystem);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Title_AccountButton_Visible_HiddenWhileConnecting()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            yield return null;
            var button = GameObject.Find("TitleAccountBtn_PH_480x90");
            Assert.IsNotNull(button);
            Assert.IsTrue(button.activeInHierarchy, "visible at 'Tap to start'");

            ScreenCapture1080.Capture("o3c-title-account-en.png");
            yield return ClickWhenReady("TapArea");
            yield return null;
            // Local nhanh nên có thể đã vào Restaurant: chỉ cần nút không còn nhận chạm ở Tiêu đề.
            var after = GameObject.Find("TitleAccountBtn_PH_480x90");
            Assert.IsTrue(after == null || !after.activeInHierarchy, "hidden while connecting / entering");
        }
    }
}
