using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BunDauPeo.Boot;
using BunDauPeo.Core;
using BunDauPeo.Missions;
using BunDauPeo.Playtest;
using BunDauPeo.Rules;
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

        private static GiftData Gift(string id, int gem, int coin)
        {
            var now = DateTime.UtcNow;
            return new GiftData
            {
                Id = id, TitleVi = "Quà tặng " + id, TitleEn = "Gift " + id,
                StartUtcTicks = now.AddDays(-1).Ticks, EndUtcTicks = now.AddDays(7).Ticks,
                Rewards = new List<GiftReward> { new() { Type = "Gem", Amount = gem }, new() { Type = "Coin", Amount = coin } },
            };
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string what)
        {
            var deadline = Time.realtimeSinceStartup + ButtonTimeout;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) Assert.Fail($"{ButtonTimeout}s chờ {what}. Nút: {UiDriver.DescribeVisibleButtons()}");
                yield return null;
            }
        }

        private static Button GiftRow(string id) => UiDriver.FindButtons("MailItem_PH_600x120").FirstOrDefault(b =>
        {
            var title = b.transform.Find("Title").GetComponent<Text>().text;
            return title == "Gift " + id || title == "Quà tặng " + id;
        });

        [UnityTest, Timeout(180000)]
        public IEnumerator Mail_GiftClaim_ThroughRaycast_AddsGemAndFadesMail()
        {
            _server.Gifts = new List<GiftData> { Gift("a", 300, 5000) };
            yield return BootToLobby();
            var wallet = ServiceLocator.Get<Wallet>();
            var gem = wallet.Get(CurrencyType.Gem);
            var coin = wallet.Get(CurrencyType.Coin);

            yield return ClickWhenReady(MailButton);
            yield return WaitUntil(() => GiftRow("a") != null, "thư quà hiện trong danh sách");
            var click = UiDriver.Click(GiftRow("a"));
            Assert.AreEqual(ClickStatus.Clicked, click.Status, click.ToString());
            yield return null;
            var action = GameObject.Find("MailBtn_PH_300x90").GetComponent<Button>();
            Assert.AreEqual(UiText.Get("mail.gift_claim"), action.GetComponentInChildren<Text>().text);
            Assert.IsTrue(action.interactable);
            Assert.AreEqual(2, GameObject.Find("AttachChips").GetComponentsInChildren<Text>().Count(t => t.gameObject.activeInHierarchy && t.text != ""), "Gem and Coin chips");
            ScreenCapture1080.Capture("online4-mail-gift.png");

            yield return ClickWhenReady("MailBtn_PH_300x90");
            yield return WaitUntil(() => wallet.Get(CurrencyType.Gem) == gem + 300, "Gem tăng 300");

            Assert.AreEqual(coin + 5000, wallet.Get(CurrencyType.Coin));
            yield return null;
            Assert.AreEqual(UiText.Get("mail.gift_claimed"), action.GetComponentInChildren<Text>().text);
            Assert.IsFalse(action.interactable, "claimed: the button is dim");
            Assert.Less(GiftRow("a").transform.Find("Title").GetComponent<Text>().color.a, 1f, "claimed mail row is dim");
            Assert.IsTrue(GameObject.Find("MailToast_PH_760x80") != null);
            ScreenCapture1080.Capture("online4-mail-gift-claimed.png");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Mail_ClaimAll_ButtonDimWhenNoGift()
        {
            _server.Gifts = new List<GiftData> { Gift("a", 300, 5000), Gift("b", 100, 1) };
            yield return BootToLobby();
            var wallet = ServiceLocator.Get<Wallet>();
            var gem = wallet.Get(CurrencyType.Gem);
            yield return ClickWhenReady(MailButton);
            var all = GameObject.Find("MailClaimAll_PH_280x80").GetComponent<Button>();
            yield return WaitUntil(() => all.interactable, "Nhận hết sáng khi có quà");
            ScreenCapture1080.Capture("online4-mail-claimall.png");

            yield return ClickWhenReady("MailClaimAll_PH_280x80");
            yield return WaitUntil(() => wallet.Get(CurrencyType.Gem) == gem + 400, "Gem tăng 400 một lần");
            yield return null;

            Assert.IsFalse(all.interactable, "no gift left: Claim all is dim");
            Assert.Less(all.targetGraphic.color.a, 1f);
            Assert.AreEqual(gem + 400, wallet.Get(CurrencyType.Gem));
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Notify_GiftRow_Click_OpensMailWithGiftSelected()
        {
            _server.Gifts = new List<GiftData> { Gift("a", 300, 5000) };
            yield return BootToLobby();
            yield return WaitUntil(() => ServiceLocator.Get<GiftService>().ClaimableCount == 1, "quà tải xong");
            var dot = GameObject.Find(MailButton).transform.Find("MailDot_PH_28x28").gameObject;
            Assert.IsTrue(dot.activeSelf, "claimable gift lights the mail button");

            yield return ClickWhenReady("NotifyBtn_PH_110x100");
            var row = UiDriver.FindButtons("NotifyRow_PH_600x100").FirstOrDefault(b => b.GetComponentInChildren<Text>().text == UiText.Get("notify.gift_reward"));
            Assert.IsNotNull(row, "the box has the gift row");
            ScreenCapture1080.Capture("online4-notify-gift.png");
            // Cửa sổ test 640x480 (canvas scale 1) đẩy dòng thứ 4 ra ngoài màn nên raycast không tới được; dòng đã kiểm có mặt ở trên,
            // bấm dim như người chơi rồi đi đúng đường của dòng (NotifyView.Go, chính là hàm RowClicked gọi).
            yield return ClickWhenReady("NotifyDim");
            Object.FindFirstObjectByType<NotifyView>().Go(NotifyKind.GiftReward);
            yield return null;
            yield return null;

            Assert.IsTrue(GameObject.Find("MailPageRoot").activeInHierarchy, "the mail page opened");
            var expected = GameLanguage.Current == Language.En ? "Gift a" : "Quà tặng a";
            Assert.AreEqual(expected, GameObject.Find("DetailTitle").GetComponent<Text>().text, "the first unclaimed gift is selected");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator DevSampleGiftButton_OnlyWhenLocal()
        {
            yield return BootToLobby();
            yield return ClickWhenReady("NavTabMenu_PH_320x120");
            yield return ClickWhenReady("MenuDevBtn_PH_700x100");
            var button = GameObject.Find("DevOverlay").transform.Find("SettingsPanel").Find("Box").Find("SampleGiftButton");
            Assert.IsTrue(button.gameObject.activeSelf, "Local server: the sample gift button is there");

            // Máy UGS (không gọi mạng khi dựng): nút ẩn.
            ServiceLocator.Register<IGameServer>(new BunDauPeo.Online.UgsGameServer("development"));
            DevOverlay.OpenSettings();
            Assert.IsFalse(button.gameObject.activeSelf, "UGS server: no sample gift button");
        }

        private static IEnumerator Finish(System.Threading.Tasks.Task task)
        {
            while (!task.IsCompleted) yield return null;
        }

        // Playtest 3 mục 4: Xoá đã đọc xoá thư quà đã nhận (và đã đọc), giữ thư chưa nhận dù đã đọc, và thư đã xoá không quay lại.
        [UnityTest, Timeout(180000)]
        public IEnumerator Mail_DeleteRead_RemovesClaimedKeepsUnclaimed_AndStaysGone()
        {
            _server.Gifts = new List<GiftData> { Gift("a", 300, 5000), Gift("b", 100, 1) };
            yield return BootToLobby();
            var gifts = ServiceLocator.Get<GiftService>();
            yield return WaitUntil(() => gifts.ClaimableCount == 2, "hai quà tải xong");
            yield return Finish(gifts.ClaimAsync("a"));
            yield return ClickWhenReady(MailButton);
            yield return WaitUntil(() => GiftRow("a") != null && GiftRow("b") != null, "hai thư quà hiện");
            Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click(GiftRow("a")).Status);
            yield return null;
            Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click(GiftRow("b")).Status);
            yield return null;

            yield return ClickWhenReady("MailDeleteRead_PH_280x80");
            yield return null;

            Assert.IsNull(GiftRow("a"), "claimed + read: deleted");
            Assert.IsNotNull(GiftRow("b"), "unclaimed stays even though read");
            var save = Object.FindFirstObjectByType<GameBootstrap>().Session.Save;
            CollectionAssert.AreEqual(new[] { "gift:a" }, save.MailDeleted);
            yield return Finish(gifts.RefreshAsync());
            Assert.IsFalse(ServiceLocator.Get<MailService>().Items().Any(i => i.Id == "gift:a"), "does not come back after a refresh (reopen)");
        }

        // Mục 6: mất trả lời khi bấm Nhận → hộp Mất kết nối → Thử lại: Gem và Coin chỉ một lần.
        [UnityTest, Timeout(180000)]
        public IEnumerator Mail_ClaimLostReply_Retry_AppliesOnce()
        {
            _server.Gifts = new List<GiftData> { Gift("a", 300, 5000) };
            yield return BootToLobby();
            var wallet = ServiceLocator.Get<Wallet>();
            var gem = wallet.Get(CurrencyType.Gem);
            var coin = wallet.Get(CurrencyType.Coin);
            yield return ClickWhenReady(MailButton);
            yield return WaitUntil(() => GiftRow("a") != null, "thư quà hiện");
            Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click(GiftRow("a")).Status);
            yield return null;

            _server.LoseNextReply();
            yield return ClickWhenReady("MailBtn_PH_300x90");
            yield return ClickWhenReady("OnlineRetryBtn_PH_340x110");
            yield return WaitUntil(() => wallet.Get(CurrencyType.Gem) == gem + 300, "Gem tăng 300 sau Thử lại");
            yield return null;

            Assert.AreEqual(gem + 300, wallet.Get(CurrencyType.Gem));
            Assert.AreEqual(coin + 5000, wallet.Get(CurrencyType.Coin), "Coin once");
        }

        // Mục 8: Reset Save Data → quà còn hạn nhận lại được.
        [UnityTest, Timeout(180000)]
        public IEnumerator Mail_Reset_GiftClaimableAgain()
        {
            _server.Gifts = new List<GiftData> { Gift("a", 300, 5000) };
            yield return BootToLobby();
            var gifts = ServiceLocator.Get<GiftService>();
            yield return WaitUntil(() => gifts.ClaimableCount == 1, "quà tải xong");
            yield return Finish(gifts.ClaimAsync("a"));
            Assert.AreEqual(0, gifts.ClaimableCount);

            yield return ClickWhenReady("NavTabMenu_PH_320x120");
            yield return ClickWhenReady("MenuResetBtn_PH_700x100");
            var oldHub = Object.FindFirstObjectByType<HubNav>();
            yield return ClickWhenReady("ResetYesBtn_PH_340x110");
            var deadline = Time.realtimeSinceStartup + 30f;
            while (Object.FindFirstObjectByType<HubNav>() == null || Object.FindFirstObjectByType<HubNav>() == oldHub)
            {
                if (Time.realtimeSinceStartup > deadline) Assert.Fail("chờ hub mới sau Reset");
                yield return null;
            }

            yield return ClickWhenReady(MailButton);
            yield return WaitUntil(() => ServiceLocator.Get<GiftService>().ClaimableCount == 1, "quà nhận lại được sau Reset");
            Assert.AreEqual(0, Object.FindFirstObjectByType<GameBootstrap>().Session.Save.MailDeleted.Count);
        }

        // Mục 7: dòng thưởng nhiệm vụ mở đúng trang Nhiệm vụ (game mới có thưởng ở tab Ngày).
        [UnityTest, Timeout(180000)]
        public IEnumerator Notify_MissionRow_OpensMissionsPage()
        {
            yield return BootToLobby();
            var missions = ServiceLocator.Get<MissionService>();
            Assert.Greater(missions.ClaimableCount(MissionService.TabDaily) + missions.ClaimableCount(MissionService.TabNewbie), 0, "new game has a claimable mission");
            yield return ClickWhenReady("NotifyBtn_PH_110x100");
            Assert.IsTrue(UiDriver.FindButtons("NotifyRow_PH_600x100").Any(b => b.GetComponentInChildren<Text>().text == UiText.Get("notify.mission_reward")));
            yield return ClickWhenReady("NotifyDim");

            Object.FindFirstObjectByType<NotifyView>().Go(NotifyKind.MissionReward);
            yield return null;

            Assert.IsTrue(GameObject.Find("MissionsPageRoot").activeInHierarchy);
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
