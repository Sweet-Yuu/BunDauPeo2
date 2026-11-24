using System;
using BunDauPeo.Core;
using BunDauPeo.Rules;
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
        public void ExpPotion_AddAndGet_RoundTripsThroughSave()
        {
            _wallet.Add(CurrencyType.ExpPotion, 7);
            var loaded = JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(_save));
            Assert.AreEqual(7, new Wallet(loaded).Get(CurrencyType.ExpPotion));
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
        public void Add_WhenAlreadyAtMax_DoesNotRaiseChanged()
        {
            _wallet.Add(CurrencyType.Coin, long.MaxValue);
            var raised = 0;
            _wallet.Changed += () => raised++;
            _wallet.Add(CurrencyType.Coin, 1);
            Assert.AreEqual(long.MaxValue, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void Balance_IsStoredInSave()
        {
            _wallet.Add(CurrencyType.Material, 5);
            Assert.AreEqual(5, new Wallet(_save).Get(CurrencyType.Material));
        }

        // Gem do server giữ: chỉ ServerStateApplier ghi được; ví chỉ đọc và hỏi CanAfford.
        [Test]
        public void Gem_AddOrSpend_Throws()
        {
            ServerStateApplier.Apply(_save, new ServerState { Gem = 100 });
            var raised = 0;
            _wallet.Changed += () => raised++;

            Assert.Throws<ArgumentException>(() => _wallet.Add(CurrencyType.Gem, 1));
            Assert.Throws<ArgumentException>(() => _wallet.TrySpend(new CurrencyAmount(CurrencyType.Gem, 1)));
            Assert.Throws<ArgumentException>(() => _wallet.TrySpend(Coin(1), new CurrencyAmount(CurrencyType.Gem, 1)));
            Assert.Throws<ArgumentException>(() => _wallet.TrySpend(new CurrencyAmount(CurrencyType.Gem, 1000)), "also when it could not afford it");

            Assert.AreEqual(100, _wallet.Get(CurrencyType.Gem));
            Assert.AreEqual(0, raised);
            Assert.IsTrue(_wallet.CanAfford(new CurrencyAmount(CurrencyType.Gem, 100)), "reading and asking stay allowed");
            Assert.IsFalse(_wallet.CanAfford(new CurrencyAmount(CurrencyType.Gem, 101)));
        }

        [Test]
        public void Coin_Material_Unchanged()
        {
            _wallet.Add(CurrencyType.Coin, 10);
            _wallet.Add(CurrencyType.Material, 20);
            Assert.IsTrue(_wallet.TrySpend(Coin(4), new CurrencyAmount(CurrencyType.Material, 5)));
            Assert.AreEqual(6, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(15, _wallet.Get(CurrencyType.Material));
            Assert.IsTrue(_wallet.TrySpend(new CurrencyAmount(CurrencyType.Gem, 0)), "a zero Gem line spends nothing and is allowed");
        }

        private static CurrencyAmount Coin(long amount) => new(CurrencyType.Coin, amount);

        [Test]
        public void CanAfford_Enough_ReturnsTrue_WithoutChangingBalanceOrRaisingChanged()
        {
            _wallet.Add(CurrencyType.Coin, 100);
            var raised = 0;
            _wallet.Changed += () => raised++;
            Assert.IsTrue(_wallet.CanAfford(Coin(30)));
            Assert.AreEqual(100, _wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void CanAfford_MissingOneCurrency_ReturnsFalse()
        {
            _wallet.Add(CurrencyType.Coin, 100);
            Assert.IsFalse(_wallet.CanAfford(Coin(50), new CurrencyAmount(CurrencyType.Material, 1)));
        }

        [Test]
        public void CanAfford_SameCurrencyLines_AreSummed()
        {
            _wallet.Add(CurrencyType.Coin, 100);
            Assert.IsFalse(_wallet.CanAfford(Coin(60), Coin(60)));
            Assert.IsTrue(_wallet.CanAfford(Coin(40), Coin(60)));
        }

        [Test]
        public void CanAfford_NegativeLine_ReturnsFalse() => Assert.IsFalse(_wallet.CanAfford(Coin(-1)));

        [Test]
        public void CanAfford_NoCosts_ReturnsTrue() => Assert.IsTrue(_wallet.CanAfford());

        [Test]
        public void CanAfford_HugeLines_DoNotOverflow()
        {
            _wallet.Add(CurrencyType.Coin, long.MaxValue);
            Assert.IsTrue(_wallet.CanAfford(Coin(long.MaxValue)));
            Assert.IsFalse(_wallet.CanAfford(Coin(long.MaxValue), Coin(long.MaxValue)));
        }

        [Test]
        public void CanAfford_AgreesWithTrySpend()
        {
            var cases = new[]
            {
                new CurrencyAmount[0],
                new[] { Coin(100) },
                new[] { Coin(101) },
                new[] { Coin(60), Coin(60) },
                new[] { Coin(-5) },
                new[] { Coin(50), new CurrencyAmount(CurrencyType.Material, 1) },
            };
            foreach (var costs in cases)
            {
                var first = new PlayerSave();
                var second = new PlayerSave();
                new Wallet(first).Add(CurrencyType.Coin, 100);
                new Wallet(second).Add(CurrencyType.Coin, 100);
                Assert.AreEqual(new Wallet(second).TrySpend(costs), new Wallet(first).CanAfford(costs),
                    $"costs: {string.Join(", ", costs)}");
            }
        }
    }
}
