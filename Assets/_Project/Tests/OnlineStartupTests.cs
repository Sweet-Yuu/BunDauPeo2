using System;
using System.IO;
using System.Threading.Tasks;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;
using UnityEngine;

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
            _server = new LocalGameServer();
            var a = TestData.Character("a");
            _startup = new OnlineStartup(_server, new ServerTime(), Path.Combine(_dir, "save.json"),
                TestData.Catalog(a), TestData.Config(new[] { new CurrencyAmount(CurrencyType.Coin, 500) }, a),
                TestData.GachaCatalog(TestData.Banner("b", a)), RestaurantTestData.Config(), CombatTestData.Config(),
                ScriptableObject.CreateInstance<StarConfig>(), _host);
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
    }
}
