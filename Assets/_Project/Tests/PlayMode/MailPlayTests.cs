using System;
using System.Collections;
using System.IO;
using BunDauPeo.Boot;
using BunDauPeo.Core;
using BunDauPeo.Missions;
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
    // Hộp thư qua UI thật (spec đợt 3 8.2) trên save tạm, LocalGameServer và LocalAccountService: không chạm mạng, không chạm save thật.
    public class MailPlayTests
    {
        private const float SceneTimeout = 30f;
        private const float ButtonTimeout = 15f;
        private const string MailButton = "HubRoundBtnMail_PH_110x110";
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
            _dir = Path.Combine(Path.GetTempPath(), "bdp-mail-" + Path.GetRandomFileName());
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
            SceneManager.SetActiveScene(SceneManager.CreateScene("MailPlayEmpty"));
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

        [UnityTest, Timeout(180000)]
        public IEnumerator MailButton_OpensPage_PinnedMailVisible()
        {
            yield return BootToLobby();
            var dot = GameObject.Find(MailButton).transform.Find("MailDot_PH_28x28").gameObject;
            Assert.IsTrue(dot.activeSelf, "unread pinned mail → red dot on the lobby button");

            yield return ClickWhenReady(MailButton);
            yield return null;

            var root = GameObject.Find("MailPageRoot");
            Assert.IsTrue(root.activeInHierarchy);
            Assert.IsFalse(dot.activeSelf, "opening the mail reads it → no red dot");
            Assert.AreEqual(UiText.Get("mail.link_title"), GameObject.Find("DetailTitle").GetComponent<Text>().text);
            Assert.AreEqual(UiText.Format("mail.count", 1, MailRule.Capacity), GameObject.Find("CountText").GetComponent<Text>().text);
            Assert.IsFalse(GameObject.Find("MailClaimAll_PH_280x80").GetComponent<Button>().interactable, "no gift mail in phase 3");
            Assert.IsFalse(GameObject.Find("MailDeleteRead_PH_280x80").GetComponent<Button>().interactable, "the pinned mail is not deletable");
            ScreenCapture1080.Capture("o3c-mail-en.png");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator LinkFromMail_Success_MailGone()
        {
            yield return BootToLobby();
            yield return ClickWhenReady(MailButton);
            yield return ClickWhenReady("MailBtn_PH_300x90");
            yield return ClickWhenReady("AccountUserPassBtn_PH_700x90");
            GameObject.Find("CredentialUser_PH_700x80").GetComponent<InputField>().text = "abc";
            GameObject.Find("CredentialPass_PH_700x80").GetComponent<InputField>().text = Strong;
            yield return ClickWhenReady("CredentialActionBtn_PH_380x90");

            var deadline = Time.realtimeSinceStartup + 5f;
            while (!_account.IsLinked && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;

            Assert.IsTrue(_account.IsLinked);
            Assert.IsTrue(GameObject.Find("EmptyText") != null && GameObject.Find("EmptyText").activeInHierarchy, "empty state after linking");
            Assert.AreEqual(UiText.Format("mail.count", 0, MailRule.Capacity), GameObject.Find("CountText").GetComponent<Text>().text);
        }
    }
}
