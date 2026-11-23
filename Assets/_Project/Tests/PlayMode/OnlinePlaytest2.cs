using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BunDauPeo.Boot;
using BunDauPeo.Core;
using BunDauPeo.Online;
using BunDauPeo.Playtest;
using BunDauPeo.UI;
using NUnit.Framework;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BunDauPeo.PlayModeTests
{
    // Playtest 2 của Online đợt 3 trên UGS THẬT (môi trường development): tạo, dùng rồi xoá một tài khoản thử.
    // Explicit: không chạy trong bộ test thường (kế hoạch: test không chạm mạng). Chạy tay:
    // unity test . --mode PlayMode --filter OnlinePlaytest2 ... Kết quả từng mục ghi ra log với tiền tố [PT2].
    // Dùng hồ sơ SDK riêng "bdp_playtest2" để không đụng phiên ẩn danh của người dùng trên máy này.
    [Explicit("Chạm UGS development, tạo tài khoản thật")]
    public class OnlinePlaytest2
    {
        private const string Env = "development";
        private const string Profile = "bdp_playtest2";
        private const string Pass = "Abcdef1!xyz";
        private const float SceneTimeout = 60f;

        private string _dir;
        private bool _refused;

        private static void Log(string item, string result) => Debug.Log($"[PT2] {item}: {result}");

        private static IEnumerator Wait(Task task, float seconds = 60f)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup > deadline) Assert.Fail($"{seconds}s chờ tác vụ mạng");
                yield return null;
            }

            if (task.IsFaulted) throw task.Exception.GetBaseException();
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _refused = Object.FindFirstObjectByType<GameBootstrap>() != null;
            Assert.IsFalse(_refused, "Đã có GameBootstrap chạy. Chạy bằng `unity test` (Editor đóng).");
            _dir = Path.Combine(Path.GetTempPath(), "bdp-pt2-" + Path.GetRandomFileName());
            yield return null;
            yield return Wait(UgsGameServer.EnsureInitializedAsync(Env));
            // Lần chạy trước (hay test trước) có thể để SDK đang đăng nhập: SwitchProfile chỉ chạy khi đã đăng xuất.
            if (AuthenticationService.Instance.IsSignedIn) AuthenticationService.Instance.SignOut(false);
            AuthenticationService.Instance.SwitchProfile(Profile);
            AuthenticationService.Instance.ClearSessionToken();
            GameBootstrap.SavePathOverride = Path.Combine(_dir, "save.json");
            GameBootstrap.ServerFactory = () => new UgsGameServer(Env);
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
            foreach (var name in new[] { "DevOverlay", "OnlineOverlay", "WaitOverlay", "AccountBoxes" })
            {
                var overlay = GameObject.Find(name);
                if (overlay != null) Object.Destroy(overlay);
            }

            ServiceLocator.Clear();
            yield return null;
            var game = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Pt2Empty" + Guid.NewGuid().ToString("N").Substring(0, 6)));
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
            var inner = UiDriver.WaitForButton(name, 20f);
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

        private static IEnumerator Shot(string name)
        {
            yield return null;
            yield return null;
            ScreenCapture1080.Capture($"pt2-{name}.png");
        }

        private static InputField Input(string name) => GameObject.Find(name).GetComponent<InputField>();

        [UnityTest, Timeout(900000)]
        public IEnumerator Items_1_2_3_7_8()
        {
            var user = "bdp" + Guid.NewGuid().ToString("N").Substring(0, 8);

            // --- vào game bằng phiên ẩn danh thật ---
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(TitleView.TitleScene);
            yield return ClickWhenReady("TapArea");
            yield return WaitForScene("Restaurant");
            var server = ServiceLocator.Get<IGameServer>();
            var playerId = server.PlayerId;
            Assert.IsNotEmpty(playerId);
            var account = ServiceLocator.Get<IAccountService>();
            Assert.IsFalse(account.IsLinked);
            Log("setup", $"ẩn danh trên {Env}, PlayerId={playerId}, tài khoản thử={user}");

            // --- mục 7a: thư ghim + chấm đỏ trước khi liên kết ---
            var dot = GameObject.Find("HubRoundBtnMail_PH_110x110").transform.Find("MailDot_PH_28x28").gameObject;
            Assert.IsTrue(dot.activeSelf, "chấm đỏ khi có thư ghim chưa đọc");
            yield return ClickWhenReady("HubRoundBtnMail_PH_110x110");
            yield return null;
            Assert.IsFalse(dot.activeSelf, "mở thư thì mất chấm đỏ");
            Assert.AreEqual(UiText.Get("mail.link_title"), GameObject.Find("DetailTitle").GetComponent<Text>().text);
            Assert.IsFalse(GameObject.Find("MailDeleteRead_PH_280x80").GetComponent<Button>().interactable, "Xoá đã đọc không xoá thư ghim");
            yield return Shot("7a-mail");
            Log("mục 7 (trước liên kết)", "ĐẠT: thư ghim hiện, mở thì mất chấm đỏ, Xoá đã đọc mờ");
            yield return ClickWhenReady("CloseButton");

            // --- mục 1: Hồ sơ → hộp chọn cách (không có Google ngoài Android) ---
            yield return ClickWhenReady("Avatar_PH_140x140");
            var accountText = GameObject.Find("ProfileAccountText").GetComponent<Text>();
            StringAssert.Contains(UiText.Get("account.not_linked"), accountText.text);
            yield return ClickWhenReady("ProfileAccountBtn_PH_160x55");
            Assert.IsNotNull(GameObject.Find("AccountUserPassBtn_PH_700x90"));
            Assert.IsFalse(Resources.FindObjectsOfTypeAll<Button>().Any(b => b.name == "AccountGoogleBtn_PH_700x90" && b.gameObject.activeInHierarchy), "Editor: không có nút Google");
            yield return Shot("1-choose");
            Log("mục 1", "ĐẠT: dòng Tài khoản 'Chưa liên kết', hộp chọn có 'Tên đăng nhập và mật khẩu', không có Google (Editor)");

            // --- mục 2: tạo tài khoản thật ---
            yield return ClickWhenReady("AccountUserPassBtn_PH_700x90");
            Input("CredentialUser_PH_700x80").text = user;
            Input("CredentialPass_PH_700x80").text = Pass;
            yield return ClickWhenReady("CredentialActionBtn_PH_380x90");
            var deadline = Time.realtimeSinceStartup + 40f;
            while (!account.IsLinked && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            Assert.IsTrue(account.IsLinked, "liên kết thật không xong sau 40s");
            StringAssert.Contains(user, accountText.text);
            Assert.IsFalse(AccountBoxes.IsShown);
            yield return Shot("2-linked");
            Log("mục 2 (liên kết)", $"ĐẠT: Hồ sơ hiện '{accountText.text}'");
            yield return ClickWhenReady("CloseButton");

            // thư ghim biến mất
            yield return ClickWhenReady("HubRoundBtnMail_PH_110x110");
            yield return null;
            Assert.IsTrue(GameObject.Find("EmptyText").activeInHierarchy, "hộp thư trống sau khi liên kết");
            yield return Shot("7b-mail-empty");
            Log("mục 7 (sau liên kết)", "ĐẠT: thư ghim mất, hiện 'Chưa có thư'");
            yield return ClickWhenReady("CloseButton");

            // việc tân thủ 'Liên kết tài khoản' nhận +100 Gem từ server
            var wallet = ServiceLocator.Get<Wallet>();
            var gemBefore = wallet.Get(CurrencyType.Gem);
            yield return ClickWhenReady("HubRoundBtnMissions_PH_110x110");
            var tabs = UiDriver.FindButtons("MissionTab_PH_300x80").OrderBy(b => b.transform.GetSiblingIndex()).ToList();
            Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click(tabs[1]).Status);
            yield return null;
            var row = Object.FindObjectsByType<MissionRowView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Single(x => x.Id == "new_link");
            Assert.AreEqual(MissionButtonState.Claim, row.State, "new_link phải nhận được ngay sau khi liên kết");
            Assert.AreEqual(ClickStatus.Clicked, UiDriver.Click(row.Button).Status);
            var claimDeadline = Time.realtimeSinceStartup + 30f;
            while ((WaitOverlay.IsBlocking || wallet.Get(CurrencyType.Gem) == gemBefore) && Time.realtimeSinceStartup < claimDeadline) yield return null;
            yield return null;
            var gemAfter = wallet.Get(CurrencyType.Gem);
            Assert.AreEqual(gemBefore + 100, gemAfter, "+100 Gem do server trao");
            yield return Shot("2-new-link-claimed");
            Log("mục 2 (thưởng)", $"ĐẠT: new_link nhận +100 Gem ({gemBefore} → {gemAfter}) qua server");
            yield return ClickWhenReady("CloseButton");

            // --- mục 3: xoá dữ liệu app rồi đăng nhập lại bằng tài khoản ---
            Assert.IsTrue(ServiceLocator.Get<PlayerProfile>().SetName("Bé Peo"));
            yield return Wait(ServiceLocator.Get<CloudSync>().FlushAsync());
            var gemKept = wallet.Get(CurrencyType.Gem);
            ServiceLocator.Get<IGameStarter>().Shutdown();
            AuthenticationService.Instance.SignOut(true);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Log("mục 3 (chuẩn bị)", "giả lập xoá dữ liệu app: Shutdown, xoá token SDK và bản đệm save");
            SceneManager.LoadScene(TitleView.TitleScene);
            yield return WaitForScene(TitleView.TitleScene);
            yield return ClickWhenReady("TitleAccountBtn_PH_480x90");
            yield return ClickWhenReady("AccountUserPassBtn_PH_700x90");
            Input("CredentialUser_PH_700x80").text = user;
            Input("CredentialPass_PH_700x80").text = Pass;
            yield return Shot("3-signin-box");
            yield return ClickWhenReady("CredentialActionBtn_PH_380x90");
            yield return WaitForScene("Restaurant");
            var server2 = ServiceLocator.Get<IGameServer>();
            Assert.AreEqual(playerId, server2.PlayerId, "đăng nhập lại phải vào đúng người chơi cũ");
            Assert.AreEqual("Bé Peo", ServiceLocator.Get<PlayerProfile>().Name);
            Assert.AreEqual(gemKept, ServiceLocator.Get<Wallet>().Get(CurrencyType.Gem), "Gem giữ nguyên");
            Assert.IsTrue(ServiceLocator.Get<IAccountService>().IsLinked);
            yield return Shot("3-back-in-lobby");
            Log("mục 3", $"ĐẠT: cùng PlayerId {playerId}, tên 'Bé Peo', Gem {gemKept}, vẫn 'Đã liên kết'");

            // --- mục 8: Reset giữ liên kết; Xoá tài khoản chạy với tài khoản đã liên kết ---
            var reset = ServiceLocator.Get<GameResetter>().ResetAsync();
            yield return Wait(reset);
            Assert.AreEqual(OnlineStatus.Ok, reset.Result);
            Assert.IsTrue(ServiceLocator.Get<IAccountService>().IsLinked, "Reset dữ liệu giữ liên kết");
            Assert.AreEqual(playerId, ServiceLocator.Get<IGameServer>().PlayerId);
            Log("mục 8 (Reset)", "ĐẠT: sau Reset vẫn 'Đã liên kết', cùng PlayerId");
            var delete = ServiceLocator.Get<AccountDeleter>().DeleteAsync();
            yield return Wait(delete);
            Assert.AreEqual(OnlineStatus.Ok, delete.Result);
            var signIn = ServiceLocator.Get<IAccountService>().SignInUserPassAsync(user, Pass);
            yield return Wait(signIn);
            Assert.AreNotEqual(AccountStatus.Ok, signIn.Result.Status, "tài khoản đã xoá thì không đăng nhập lại được");
            Log("mục 8 (Xoá tài khoản)", $"ĐẠT: xoá được tài khoản đã liên kết, đăng nhập lại báo {signIn.Result.Status}");
        }

        // Tên đã dùng: hai người chơi thật liên kết cùng tên (userpass không có Conflict; Conflict chỉ có với Google).
        [UnityTest, Timeout(300000)]
        public IEnumerator UsernameTaken_SecondPlayer()
        {
            var user = "bdp" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var auth = AuthenticationService.Instance;
            var first = new UgsAccountService(Env);

            AuthenticationService.Instance.SwitchProfile(Profile + "a");
            auth.ClearSessionToken();
            yield return Wait(auth.SignInAnonymouslyAsync());
            var a = first.LinkUserPassAsync(user, Pass);
            yield return Wait(a);
            Assert.AreEqual(AccountStatus.Ok, a.Result.Status);

            auth.SignOut();
            auth.SwitchProfile(Profile + "b");
            auth.ClearSessionToken();
            yield return Wait(auth.SignInAnonymouslyAsync());
            var b = new UgsAccountService(Env).LinkUserPassAsync(user, Pass);
            yield return Wait(b);
            Log("tên đã dùng", $"người chơi thứ hai liên kết cùng tên → {b.Result.Status}");
            Assert.AreEqual(AccountStatus.UsernameTaken, b.Result.Status);
            yield return Wait(auth.DeleteAccountAsync());

            auth.SwitchProfile(Profile + "a");
            yield return Wait(auth.SignInAnonymouslyAsync());
            yield return Wait(auth.DeleteAccountAsync());
        }
    }
}
