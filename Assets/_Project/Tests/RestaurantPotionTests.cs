using System;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Khách đầu trả 20 Coin ở t = 27,5 (cùng cấu hình RestaurantServiceTests); xúc xắc thuốc là Next(100) nên giả riêng.
    public class RestaurantPotionTests
    {
        // NextDouble giữ kịch bản của ScriptedRandom(0.99, 0, 0, 0): ba lần đầu 0, sau đó 0,99; Next(int) trả `roll`.
        private sealed class PotionRandom : System.Random
        {
            private int _doubles;
            public int Roll;
            public override double NextDouble() => _doubles++ < 3 ? 0.0 : 0.99;
            public override int Next(int maxValue) => Roll;
        }

        private static (RestaurantService service, Wallet wallet) Make(int roll, Action<LevelConfig> edit = null)
        {
            var save = new PlayerSave();
            var catalog = TestData.Catalog(
                RestaurantTestData.Worker("cook0", 0, 0, 0),
                RestaurantTestData.Worker("serve0", 0, 0, 0),
                RestaurantTestData.Worker("cashier0", 0, 0, 0));
            var roster = new Roster(save, catalog);
            roster.Add("cook0");
            roster.Add("serve0");
            roster.Add("cashier0");
            roster.AssignStaff("cook0", StaffRole.Cook);
            roster.AssignStaff("serve0", StaffRole.Server);
            roster.AssignStaff("cashier0", StaffRole.Cashier);
            var levels = ScriptableObject.CreateInstance<LevelConfig>();
            edit?.Invoke(levels);
            var wallet = new Wallet(save);
            var service = new RestaurantService(RestaurantTestData.Config(), save, wallet, roster,
                new PotionRandom { Roll = roll }, new FakeClock(), levels, () => true);
            for (var i = 0; i < 300; i++) service.Update(0.1f);
            return (service, wallet);
        }

        [Test]
        public void CustomerPaid_ChanceHit_AddsOnePotion()
        {
            var (service, wallet) = Make(roll: 2);   // 2 < 3%
            Assert.AreEqual(1, service.Simulation.Served);
            Assert.AreEqual(1, wallet.Get(CurrencyType.ExpPotion));
        }

        [Test]
        public void CustomerPaid_ChanceMiss_AddsNone()
        {
            var (_, wallet) = Make(roll: 3);
            Assert.AreEqual(0, wallet.Get(CurrencyType.ExpPotion));
            Assert.AreEqual(20, wallet.Get(CurrencyType.Coin));
        }

        [Test]
        public void ChanceZero_NeverDrops()
        {
            var (_, wallet) = Make(roll: 0, so => TestData.Edit(so, s => s.FindProperty("_restaurantPotionChancePercent").intValue = 0));
            Assert.AreEqual(0, wallet.Get(CurrencyType.ExpPotion));
        }

        [Test]
        public void ChanceHundred_AlwaysDrops()
        {
            var (_, wallet) = Make(roll: 99, so => TestData.Edit(so, s => s.FindProperty("_restaurantPotionChancePercent").intValue = 100));
            Assert.AreEqual(1, wallet.Get(CurrencyType.ExpPotion));
        }

        [Test]
        public void InvalidLevelConfig_NoPotionAndCoinStillPaid()
        {
            var (_, wallet) = Make(roll: 0, so => TestData.Edit(so, s => s.FindProperty("_afkCoinPerPotion").intValue = 0));
            Assert.AreEqual(0, wallet.Get(CurrencyType.ExpPotion));
            Assert.AreEqual(20, wallet.Get(CurrencyType.Coin));
        }
    }
}
