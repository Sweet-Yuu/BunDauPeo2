using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.Tests
{
    // Catalog: up_tables 5 cấp (4 bàn 300 Coin + 5 Material, 5 bàn 600 + 10, ...), up_cook_slots 2 cấp, và một nâng cấp lỗi (cấp 1 tốn 0).
    public class UpgradeServiceTests
    {
        private sealed class Fixture
        {
            public UpgradeService Service;
            public PlayerSave Save;
            public Wallet Wallet;
            public int Persists;
            public int Changes;
        }

        private static Fixture Make(long coin = 5000, long material = 100, Func<bool> persist = null)
        {
            var save = new PlayerSave();
            var wallet = new Wallet(save);
            wallet.Add(CurrencyType.Coin, coin);
            wallet.Add(CurrencyType.Material, material);
            var catalog = RestaurantTestData.UpgradeCatalog(
                RestaurantTestData.Upgrade("up_tables", UpgradeEffect.Tables,
                    RestaurantTestData.Level(4, 300, 5), RestaurantTestData.Level(5, 600, 10), RestaurantTestData.Level(6, 1200, 15),
                    RestaurantTestData.Level(7, 2400, 20), RestaurantTestData.Level(8, 4800, 25)),
                RestaurantTestData.Upgrade("up_cook_slots", UpgradeEffect.CookSlots,
                    RestaurantTestData.Level(2, 500, 10), RestaurantTestData.Level(3, 1500, 30)),
                RestaurantTestData.Upgrade("up_broken", UpgradeEffect.DishPrice, RestaurantTestData.Level(1.1f, 0, 0)));
            var f = new Fixture { Save = save, Wallet = wallet };
            f.Service = new UpgradeService(save, wallet, catalog, persist ?? (() => { f.Persists++; return true; }));
            f.Service.Changed += () => f.Changes++;
            return f;
        }

        private static void AssertNothingChanged(Fixture f, long coin, long material, string id = "up_tables", int level = 0)
        {
            Assert.AreEqual(coin, f.Wallet.Get(CurrencyType.Coin), "coin");
            Assert.AreEqual(material, f.Wallet.Get(CurrencyType.Material), "material");
            Assert.AreEqual(level, f.Service.GetLevel(id), "level");
            Assert.AreEqual(0, f.Changes, "Changed");
            Assert.AreEqual(0, f.Persists, "persist");
        }

        [Test]
        public void Buy_SpendsBothCurrencies_RaisesLevel_PersistsOnce()
        {
            var f = Make();
            Assert.AreEqual(0, f.Service.GetLevel("up_tables"));

            var result = f.Service.TryBuy("up_tables");

            Assert.AreEqual(UpgradeBuyStatus.Success, result.Status);
            Assert.IsTrue(result.Saved);
            Assert.AreEqual(1, result.NewLevel);
            Assert.AreEqual(4700, f.Wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(95, f.Wallet.Get(CurrencyType.Material));
            Assert.AreEqual(1, f.Service.GetLevel("up_tables"));
            Assert.AreEqual(1, f.Persists);
        }

        [Test]
        public void Buy_MissingCoinOnly_ChangesNothing()
        {
            var f = Make(coin: 299);

            var result = f.Service.TryBuy("up_tables");

            Assert.AreEqual(UpgradeBuyStatus.NotEnoughCurrency, result.Status);
            Assert.IsFalse(result.Saved);
            AssertNothingChanged(f, 299, 100);
        }

        [Test]
        public void Buy_MissingMaterialOnly_ChangesNothing()
        {
            var f = Make(material: 4);

            var result = f.Service.TryBuy("up_tables");

            Assert.AreEqual(UpgradeBuyStatus.NotEnoughCurrency, result.Status);
            Assert.IsFalse(result.Saved);
            AssertNothingChanged(f, 5000, 4);
        }

        [Test]
        public void Buy_AtMaxLevel_ReturnsMaxLevel()
        {
            // Ví rỗng: MaxLevel phải được kiểm trước tiền, nếu không sẽ báo thiếu tiền.
            var f = Make(coin: 0, material: 0);
            f.Save.Upgrades.Add(new UpgradeLevel { UpgradeId = "up_cook_slots", Level = 2 });

            var result = f.Service.TryBuy("up_cook_slots");

            Assert.AreEqual(UpgradeBuyStatus.MaxLevel, result.Status);
            Assert.IsFalse(result.Saved);
            Assert.AreEqual(2, result.NewLevel);
            AssertNothingChanged(f, 0, 0, "up_cook_slots", 2);
        }

        [Test]
        public void Buy_UnknownId()
        {
            var f = Make();

            foreach (var id in new[] { "up_missing", "", null })
                Assert.AreEqual(UpgradeBuyStatus.UnknownUpgrade, f.Service.TryBuy(id).Status, id ?? "null");

            AssertNothingChanged(f, 5000, 100);
        }

        [Test]
        public void Buy_InvalidUpgrade_NothingSpent()
        {
            var f = Make();

            var result = f.Service.TryBuy("up_broken");

            // Chi phí tổng 0 làm TrySpend thành công mà không trừ gì: cài đặt thiếu kiểm Validate sẽ tăng cấp miễn phí.
            Assert.AreEqual(UpgradeBuyStatus.InvalidUpgrade, result.Status);
            Assert.IsFalse(result.Saved);
            AssertNothingChanged(f, 5000, 100, "up_broken");
        }

        [Test]
        public void Buy_PersistFails_SavedFalse_KeepsState()
        {
            var persists = 0;
            var f = Make(persist: () => { persists++; return false; });

            var result = f.Service.TryBuy("up_tables");

            Assert.AreEqual(UpgradeBuyStatus.Success, result.Status);
            Assert.IsFalse(result.Saved);
            Assert.AreEqual(1, result.NewLevel);
            Assert.AreEqual(1, persists);
            Assert.AreEqual(1, f.Service.GetLevel("up_tables"));
            Assert.AreEqual(4700, f.Wallet.Get(CurrencyType.Coin), "no refund");
            Assert.AreEqual(95, f.Wallet.Get(CurrencyType.Material), "no refund");
            Assert.AreEqual(1, f.Changes);
        }

        [Test]
        public void Buy_PersistedExactlyOnce()
        {
            // Lưu phải thấy trạng thái sau khi mua (ví đã trừ, cấp đã tăng, Changed đã bắn) để mất điện không mất hay nhân đôi tiền.
            var seen = new List<string>();
            Fixture f = null;
            f = Make(persist: () =>
            {
                seen.Add($"{f.Wallet.Get(CurrencyType.Coin)}/{f.Wallet.Get(CurrencyType.Material)}/{f.Service.GetLevel("up_tables")}/{f.Changes}");
                return true;
            });

            f.Service.TryBuy("up_tables");

            CollectionAssert.AreEqual(new[] { "4700/95/1/1" }, seen);
        }

        [Test]
        public void Buy_Consecutive_SpendsEachLevelCost()
        {
            var f = Make();

            var first = f.Service.TryBuy("up_tables");
            var second = f.Service.TryBuy("up_tables");

            Assert.AreEqual(1, first.NewLevel);
            Assert.AreEqual(2, second.NewLevel);
            Assert.AreEqual(5000 - 300 - 600, f.Wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(100 - 5 - 10, f.Wallet.Get(CurrencyType.Material));
            Assert.AreEqual(2, f.Service.GetLevel("up_tables"));
            Assert.AreEqual(1, f.Save.Upgrades.Count, "one row per upgrade");
            Assert.AreEqual(2, f.Persists);
        }

        [Test]
        public void Changed_RaisedOnSuccessOnly()
        {
            var f = Make(coin: 400);

            f.Service.TryBuy("up_missing");
            f.Service.TryBuy("up_broken");
            Assert.AreEqual(0, f.Changes, "unknown + invalid");

            f.Service.TryBuy("up_tables");
            Assert.AreEqual(1, f.Changes, "success");

            f.Service.TryBuy("up_tables");
            Assert.AreEqual(1, f.Changes, "not enough for level 2 (600)");
        }

        [Test]
        public void Buy_ChangedHandlerThrows_StillPersistsAndKeepsState()
        {
            var f = Make();
            var laterHandlerCalled = false;
            f.Service.Changed += () => throw new InvalidOperationException("boom");
            f.Service.Changed += () => laterHandlerCalled = true;
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            var result = f.Service.TryBuy("up_tables");

            Assert.AreEqual(UpgradeBuyStatus.Success, result.Status);
            Assert.IsTrue(result.Saved);
            Assert.AreEqual(1, f.Persists);
            Assert.AreEqual(1, f.Service.GetLevel("up_tables"));
            Assert.AreEqual(4700, f.Wallet.Get(CurrencyType.Coin));
            Assert.IsTrue(laterHandlerCalled);
        }
    }
}
