using System;
using BunDauPeo.Core;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.Tests
{
    public class WalletTests
    {
        private PlayerSave _save;
        private Wallet _wallet;

        [SetUp]
        public void SetUp()
        {
            _save = new PlayerSave();
            _wallet = new Wallet(_save);
        }

        [Test]
        public void Add_IncreasesBalance_AndRaisesChanged()
        {
            var raised = 0;
            _wallet.Changed += () => raised++;
            _wallet.Add(CurrencyType.Coin, 100);
            Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Add_Negative_Throws_AndKeepsBalance()
        {
            _wallet.Add(CurrencyType.Coin, 100);
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.Add(CurrencyType.Coin, -1));
            Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
        }

        [Test]
        public void TrySpend_Enough_Deducts()
        {
            _wallet.Add(CurrencyType.Coin, 100);
            Assert.IsTrue(_wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 30)));
            Assert.AreEqual(70, _wallet.Get(CurrencyType.Coin));
        }

        [Test]
        public void TrySpend_MissingOneCurrency_DeductsNothing()
        {
            _wallet.Add(CurrencyType.Coin, 100);
            var raised = 0;
            _wallet.Changed += () => raised++;
            Assert.IsFalse(_wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 50), new CurrencyAmount(CurrencyType.Material, 1)));
            Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void TrySpend_SameCurrencyLines_AreSummed()
        {
            _wallet.Add(CurrencyType.Coin, 100);
            Assert.IsFalse(_wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 60), new CurrencyAmount(CurrencyType.Coin, 60)));
            Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
        }

        [Test]
        public void TrySpend_NegativeLine_ReturnsFalse_AndAddsNothing()
        {
            _wallet.Add(CurrencyType.Coin, 100);
            Assert.IsFalse(_wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, -50)));
            Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
        }

        [Test]
        public void TrySpend_NoCosts_ReturnsTrue_WithoutChanged()
        {
            var raised = 0;
            _wallet.Changed += () => raised++;
            Assert.IsTrue(_wallet.TrySpend());
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void Changed_HandlerThrows_SpendStillSucceeds_AndLaterHandlersRun()
        {
            _wallet.Add(CurrencyType.Coin, 100);
            var raised = 0;
            _wallet.Changed += () => throw new InvalidOperationException("boom");
            _wallet.Changed += () => raised++;
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            Assert.IsTrue(_wallet.TrySpend(new CurrencyAmount(CurrencyType.Coin, 30)));
            Assert.AreEqual(70, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Balance_IsStoredInSave()
        {
            _wallet.Add(CurrencyType.Gem, 5);
            Assert.AreEqual(5, new Wallet(_save).Get(CurrencyType.Gem));
        }
    }
}
