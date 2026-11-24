using System;
using System.IO;
using System.Threading.Tasks;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.Tests
{
    public class OnlineStartupTests
    {
        private string _dir;
        private GameObject _host;
        private LocalGameServer _server;
        private OnlineStartup _startup;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_dir);
            _host = new GameObject("OnlineStartupTests");
            var data = new GameData
            {
                NewGame = new NewGameData { StartingGem = 300, StarterCharacterIds = { "a" } },
                Stamina = new StaminaData { Max = 100, RegenSeconds = 300 },
            };
            _server = new LocalGameServer(data);
            var a = TestData.Character("a");
            _startup = new OnlineStartup(_server, new ServerTime(), Path.Combine(_dir, "save.json"),
                TestData.Catalog(a), TestData.Config(new[] { new CurrencyAmount(CurrencyType.Coin, 500) }, a),
                TestData.GachaCatalog(TestData.Banner("b", a)), RestaurantTestData.Config(), CombatTestData.Config(),
                ScriptableObject.CreateInstance<StarConfig>(), data, _host);
            ServiceLocator.Clear();
            _startup.RegisterBase();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            UnityEngine.Object.DestroyImmediate(_host);
            GameLanguage.Current = Language.Vi;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        // Review Focus 3: save đợt 1 thật (Gem 5000, nhân vật gacha x trong owned, đội, nhân viên, avatar) mở bằng bản đợt 2.
        [Test]
        public async Task Start_Phase1Save_ServerFieldsFromState_PrunesUnowned()
        {
            var old = new PlayerSave();
            old.Balances.Add(new CurrencyAmount(CurrencyType.Coin, 50));
            old.Balances.Add(new CurrencyAmount(CurrencyType.Gem, 5000));
            old.OwnedCharacterIds.AddRange(new[] { "a", "x" });
            old.BattleTeam.AddRange(new[] { "a", "x" });
            old.Staff.Add(new StaffAssignment { CharacterId = "x", Role = StaffRole.Cashier });
            old.AvatarCharacterId = "x";
            await _server.SignInAsync();
            await _server.UploadSaveAsync(JsonUtility.ToJson(old));

            Assert.AreEqual(OnlineStatus.Ok, await _startup.StartAsync());

            var save = _startup.Session.Save;
            Assert.AreEqual(300, ServiceLocator.Get<Wallet>().Get(CurrencyType.Gem), "Gem comes from state, not the old save");
            CollectionAssert.AreEqual(new[] { "a" }, save.OwnedCharacterIds, "x is not owned by the server");
            CollectionAssert.AreEqual(new[] { "a" }, save.BattleTeam);
            Assert.AreEqual(0, save.Staff.Count);
            Assert.AreEqual("", save.AvatarCharacterId);
            Assert.AreEqual(50, ServiceLocator.Get<Wallet>().Get(CurrencyType.Coin), "Coin kept");
        }

        [Test]
        public async Task Start_NewPlayer_GemFromServer_CoinFromConfig()
        {
            Assert.AreEqual(OnlineStatus.Ok, await _startup.StartAsync());

            Assert.AreEqual(300, ServiceLocator.Get<Wallet>().Get(CurrencyType.Gem), "granted once, by the server");
            Assert.AreEqual(500, ServiceLocator.Get<Wallet>().Get(CurrencyType.Coin));
            Assert.IsTrue(ServiceLocator.Get<Roster>().IsOwned("a"), "starter from server state");
        }

        [Test]
        public async Task Bootstrap_RegistersLevelServices()
        {
            Assert.AreEqual(OnlineStatus.Ok, await _startup.StartAsync());

            Assert.IsNotNull(ServiceLocator.Get<PlayerLevelService>());
            Assert.IsNotNull(ServiceLocator.Get<CharacterLevelService>());
            Assert.IsNotNull(ServiceLocator.Get<LevelConfig>());
        }

        [Test]
        public async Task Start_DataHashMismatch_LogsWarning_StillStarts()
        {
            _server.DataHash = "not-the-same";
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("differs"));

            Assert.AreEqual(OnlineStatus.Ok, await _startup.StartAsync());
            Assert.IsTrue(_startup.IsStarted);
        }

        [Test]
        public async Task Shutdown_ThenStart_RebuildsFromServer()
        {
            Assert.AreEqual(OnlineStatus.Ok, await _startup.StartAsync());
            Assert.IsTrue(_startup.IsStarted);
            Assert.AreEqual(500, ServiceLocator.Get<Wallet>().Get(CurrencyType.Coin), "empty server = new game");

            var session = _startup.Session;
            session.Wallet.Add(CurrencyType.Coin, 200);
            await _server.UploadSaveAsync(JsonUtility.ToJson(session.Save));
            // Đổi sau lần đẩy và chỉ ghi bản đệm: dựng lại phải lấy bản server, không lấy bộ nhớ hay bản đệm.
            session.Wallet.Add(CurrencyType.Coin, 1000);
            session.Persist();

            _startup.Shutdown();
            Assert.IsFalse(_startup.IsStarted);
            Assert.IsNull(_startup.Session);
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<Wallet>());
            Assert.AreSame(_startup, ServiceLocator.Get<IGameStarter>(), "base services stay registered");
            Assert.AreSame(_server, ServiceLocator.Get<IGameServer>());
            Assert.IsNotNull(ServiceLocator.Get<ServerTime>());

            Assert.AreEqual(OnlineStatus.Ok, await _startup.StartAsync());
            Assert.AreEqual(700, ServiceLocator.Get<Wallet>().Get(CurrencyType.Coin));
            Assert.AreNotSame(session, _startup.Session);
            Assert.AreEqual(1, _host.GetComponents<RestaurantRunner>().Length);
            Assert.AreEqual(2, _server.BootstrapCalls, "each start asks the server again");
        }

        // Review O1 mục 4: UGS xoá dữ liệu (ResetPlayer) rồi mới xoá tài khoản; bước sau lỗi thì server đã trống mà tài khoản còn.
        // Save trong bộ nhớ phải được đẩy lại ngay, kẻo tắt app lúc đó thì mở lại cùng ID là game mới.
        [Test]
        public async Task DeleteAccount_FailsAfterServerWiped_PushesSaveAgain()
        {
            Assert.AreEqual(OnlineStatus.Ok, await _startup.StartAsync());
            await _startup.Sync.FlushAsync();
            Assert.IsNotNull(_server.StoredSave, "the new game is on the server");

            _server.Seed(null);
            _server.FailNextOn(nameof(IGameServer.DeleteAccountAsync), OnlineStatus.NetworkError);
            Assert.AreEqual(OnlineStatus.NetworkError, await ServiceLocator.Get<AccountDeleter>().DeleteAsync());
            Assert.IsTrue(_startup.IsStarted, "the game keeps running");

            await _startup.Sync.FlushAsync();
            Assert.IsNotNull(_server.StoredSave, "the save is pushed back");
        }
    }
}
