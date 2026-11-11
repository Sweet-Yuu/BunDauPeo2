using System;
using System.IO;
using BunDauPeo.Boot;
using BunDauPeo.Core;
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
            // GrantGemsThenResetViaResetter...: ResetToDefaults đưa GameLanguage.Current về En.
            GameLanguage.Current = Language.Vi;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void GrantGems_AddsGemAmount_AndNothingElse()
        {
            var wallet = new Wallet(new PlayerSave());
            DevOverlay.GrantGems(wallet);
            DevOverlay.GrantGems(wallet);
            Assert.AreEqual(2 * DevOverlay.GemGrant, wallet.Get(CurrencyType.Gem), "cộng dồn, không đặt lại");
            Assert.AreEqual(0, wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(0, wallet.Get(CurrencyType.Material));
        }

        [Test]
        public void GameResetter_Reset_ReturnsDelegateResult()
        {
            var calls = 0;
            Assert.IsTrue(new GameResetter(() => { calls++; return true; }).Reset());
            Assert.IsFalse(new GameResetter(() => { calls++; return false; }).Reset(), "reset thất bại phải báo false");
            Assert.AreEqual(2, calls);
            Assert.Throws<ArgumentNullException>(() => new GameResetter(null));
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
        public void GrantGemsThenResetViaResetter_GemsBackToStarting_AndFileHasNoGrant()
        {
            var path = Path.Combine(_dir, "save.json");
            var service = new SaveService(path);
            var a = TestData.Character("a");
            var config = TestData.Config(new[] { new CurrencyAmount(CurrencyType.Gem, 30) }, a);
            var session = new GameSession(service, TestData.Catalog(a), config);

            DevOverlay.GrantGems(session.Wallet);
            session.Persist();
            Assert.AreEqual(30 + DevOverlay.GemGrant, session.Wallet.Get(CurrencyType.Gem));

            Assert.IsTrue(new GameResetter(session.ResetToDefaults).Reset());

            Assert.AreEqual(30, session.Wallet.Get(CurrencyType.Gem));
            var reopened = new GameSession(new SaveService(path), TestData.Catalog(a), config);
            Assert.AreEqual(30, reopened.Wallet.Get(CurrencyType.Gem), "file trên đĩa cũng đã reset");
        }
    }
}
