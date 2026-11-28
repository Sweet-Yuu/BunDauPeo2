using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BunDauPeo.Boot;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using BunDauPeo.Rules;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class DevSettingsTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            // ServerGrantThenResetViaResetter...: ResetToDefaults đưa GameLanguage.Current về En.
            GameLanguage.Current = Language.Vi;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        // Server mới có Gem 100 và thể lực đã hao; save của máy khớp server, apply chép state vào save như GameSession.
        private async Task<(ServerRig Rig, PlayerSave Save, DevCommands Dev, Func<int> Applied)> DevRigAsync()
        {
            var rig = await ServerRig.CreateAsync(GachaRulesTests.Data(), s =>
            {
                s.Balances.Add(new CurrencyAmount(CurrencyType.Gem, 100));
                s.Stamina = new StaminaState { Current = 20, AnchorUtcTicks = DateTime.UtcNow.Ticks };
            });
            var save = new PlayerSave();
            rig.ApplyTo(save);
            var applied = 0;
            var dev = new DevCommands(rig.Server, rig.Gate, state =>
            {
                applied++;
                ServerStateApplier.Apply(save, state);
                return true;
            });
            return (rig, save, dev, () => applied);
        }

        // Mục Thời tiết dựng trên một hộp rỗng với dịch vụ đồng hồ giả; bấm nút bằng onClick.
        private static (UnityEngine.GameObject Box, WeatherService Weather) WeatherRig()
        {
            var weather = new WeatherService(RestaurantTestData.Config(), new PlayerSave(), new FakeClock(), new ScriptedRandom(0.99), () => true);
            var box = new UnityEngine.GameObject("Box", typeof(UnityEngine.RectTransform));
            DevOverlay.BuildWeatherSection((UnityEngine.RectTransform)box.transform, null, () => weather);
            return (box, weather);
        }

        private static void Click(UnityEngine.GameObject box, string label) =>
            box.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                .Single(b => b.GetComponentInChildren<UnityEngine.UI.Text>().text == label).onClick.Invoke();

        [Test]
        public void DevPanel_HasWeatherSection()
        {
            var (box, _) = WeatherRig();
            try
            {
                var labels = box.GetComponentsInChildren<UnityEngine.UI.Button>(true).Select(b => b.GetComponentInChildren<UnityEngine.UI.Text>().text).ToArray();
                CollectionAssert.AreEqual(new[] { "Gọi mưa", "Hết mưa", "Tua +30 phút", "Quay ngay" }, labels);
                StringAssert.Contains("Quang", box.GetComponentInChildren<DevWeatherStatus>(true).GetComponent<UnityEngine.UI.Text>().text);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(box);
            }
        }

        [Test]
        public void DevPanel_CallRain_StartsRain()
        {
            var (box, weather) = WeatherRig();
            try
            {
                Click(box, "Gọi mưa");

                Assert.IsTrue(weather.IsRaining);
                StringAssert.Contains("Mưa, còn 10:00", box.GetComponentInChildren<DevWeatherStatus>(true).GetComponent<UnityEngine.UI.Text>().text);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(box);
            }
        }

        [Test]
        public void DevPanel_ClearRain_AndAddContinuous()
        {
            var (box, weather) = WeatherRig();
            try
            {
                Click(box, "Gọi mưa");
                Click(box, "Hết mưa");
                Assert.IsFalse(weather.IsRaining);

                Click(box, "Tua +30 phút");
                Assert.AreEqual(30, weather.ContinuousMinutes, 1e-9);
                Assert.IsFalse(weather.IsRaining, "Tua không quay");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(box);
            }
        }

        [Test]
        public async Task AddGem_CallsServer_AppliesState()
        {
            var (rig, save, dev, applied) = await DevRigAsync();

            Assert.IsTrue(await dev.AddGemAsync(DevOverlay.GemGrant));
            Assert.IsTrue(await dev.AddGemAsync(DevOverlay.GemGrant));

            Assert.AreEqual(100 + 2 * DevOverlay.GemGrant, rig.Server.State.Gem, "the server holds the Gem");
            Assert.AreEqual(100 + 2 * DevOverlay.GemGrant, new Wallet(save).Get(CurrencyType.Gem), "the device shows what the server returned");
            Assert.AreEqual(2, rig.Server.CommandCalls);
            Assert.AreEqual(2, applied());
            Assert.AreEqual(0, new Wallet(save).Get(CurrencyType.Coin));
            Assert.AreEqual(0, new Wallet(save).Get(CurrencyType.Material));
        }

        [Test]
        public async Task RefillStamina_CallsServer()
        {
            var (rig, save, dev, applied) = await DevRigAsync();
            Assert.AreEqual(20, save.Stamina.Current, "setup: stamina is low");

            Assert.IsTrue(await dev.RefillStaminaAsync());

            Assert.AreEqual(1, rig.Server.CommandCalls);
            Assert.AreEqual(100, rig.Server.State.Stamina.Current);
            Assert.AreEqual((100, 0L), (save.Stamina.Current, save.Stamina.AnchorUtcTicks), "the device got the server's full bar");
            Assert.AreEqual(1, applied());
        }

        [Test]
        public async Task AddGem_DevOnly_NoChange()
        {
            var (rig, save, dev, applied) = await DevRigAsync();
            rig.Server.RejectDev = true;
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("DevOnly"));

            Assert.IsFalse(await dev.AddGemAsync(DevOverlay.GemGrant));

            Assert.AreEqual(100, rig.Server.State.Gem);
            Assert.AreEqual(100, new Wallet(save).Get(CurrencyType.Gem));
            Assert.AreEqual(0, applied());
        }

        [Test]
        public async Task AddGem_Offline_NoChange_NoWarning()
        {
            var (rig, save, dev, applied) = await DevRigAsync();
            rig.Server.FailNext(OnlineStatus.NetworkError);

            Assert.IsFalse(await dev.AddGemAsync(DevOverlay.GemGrant));

            Assert.AreEqual(100, new Wallet(save).Get(CurrencyType.Gem));
            Assert.AreEqual(0, applied());
        }

        [Test]
        public void GameResetter_ResetAsync_ReturnsDelegateResult()
        {
            var calls = 0;
            Assert.AreEqual(OnlineStatus.Ok, new GameResetter(() => { calls++; return Task.FromResult(OnlineStatus.Ok); }).ResetAsync().Result);
            Assert.AreEqual(OnlineStatus.NetworkError, new GameResetter(() => { calls++; return Task.FromResult(OnlineStatus.NetworkError); }).ResetAsync().Result,
                "reset thất bại phải báo đúng lỗi");
            Assert.AreEqual(2, calls);
            Assert.Throws<ArgumentNullException>(() => new GameResetter(null));
        }

        [Test]
        public void AccountDeleter_NullThrows_ReturnsDelegateResult()
        {
            var calls = 0;
            Assert.AreEqual(OnlineStatus.Ok, new AccountDeleter(() => { calls++; return Task.FromResult(OnlineStatus.Ok); }).DeleteAsync().Result);
            Assert.AreEqual(OnlineStatus.ServerError, new AccountDeleter(() => { calls++; return Task.FromResult(OnlineStatus.ServerError); }).DeleteAsync().Result);
            Assert.AreEqual(2, calls);
            Assert.Throws<ArgumentNullException>(() => new AccountDeleter(null));
        }

        [Test]
        public void LanguageSwitcher_Set_PassesLanguageAndReturnsResult()
        {
            Language? received = null;
            var switcher = new LanguageSwitcher(language => { received = language; return false; });
            Assert.IsFalse(switcher.Set(Language.En), "lưu thất bại phải báo false");
            Assert.AreEqual(Language.En, received);
            Assert.Throws<ArgumentNullException>(() => new LanguageSwitcher(null));
        }

        [Test]
        public void LanguageButtonLabel_EachLanguage()
        {
            Assert.AreEqual("Ngôn ngữ: Tiếng Việt", DevOverlay.LanguageButtonLabel(Language.Vi));
            Assert.AreEqual("Language: English", DevOverlay.LanguageButtonLabel(Language.En));
        }

        [Test]
        public void ShouldReloadScene_Battle_ReturnsFalse()
        {
            Assert.IsFalse(DevOverlay.ShouldReloadScene("Battle"), "nạp lại Battle làm hỏng trận đang chạy");
            foreach (var scene in new[] { "Restaurant" })
                Assert.IsTrue(DevOverlay.ShouldReloadScene(scene), scene);
        }

        [Test]
        public void ServerGrantThenResetViaResetter_GemsBackToStarting_AndFileHasNoGrant()
        {
            var path = Path.Combine(_dir, "save.json");
            var service = new SaveService(path);
            var a = TestData.Character("a");
            var config = TestData.Config(new[] { new CurrencyAmount(CurrencyType.Gem, 30) }, a);
            var session = new GameSession(service, TestData.Catalog(a), config);

            // Gem do server giữ: máy áp state server có thêm quà DEV, rồi ghi bản đệm.
            session.ApplyServerState(new ServerState { Gem = 30 + DevOverlay.GemGrant });
            Assert.AreEqual(30 + DevOverlay.GemGrant, session.Wallet.Get(CurrencyType.Gem), "the server grants the Gem");

            // Gem khởi đầu là của server: Reset trả về đúng số đó.
            var server = new LocalGameServer(new BunDauPeo.Rules.GameData { NewGame = { StartingGem = 30 } });
            Assert.AreEqual(OnlineStatus.Ok, server.SignInAsync().Result);
            var sync = new CloudSync(server, () => UnityEngine.JsonUtility.ToJson(session.Save), () => { }, () => "");
            Assert.AreEqual(OnlineStatus.Ok, new GameResetter(() => session.ResetToDefaultsAsync(server, sync)).ResetAsync().Result);

            Assert.AreEqual(30, session.Wallet.Get(CurrencyType.Gem));
            var reopened = new GameSession(new SaveService(path), TestData.Catalog(a), config);
            Assert.AreEqual(30, reopened.Wallet.Get(CurrencyType.Gem), "file trên đĩa cũng đã reset");
        }
    }
}
