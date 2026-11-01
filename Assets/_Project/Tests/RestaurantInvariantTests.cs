using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Bất biến nền của AFK (spec 4.2): thu nhập thật không vượt công thức lý thuyết, và không thấp hơn lý thuyết × hệ số AFK tối đa.
    // Đỏ nghĩa là thuật toán mô phỏng hoặc công thức đã đổi; không đổi seed hay dung sai để xanh.
    public class RestaurantInvariantTests
    {
        private const int Seconds = 7200;
        private const float TickStep = 0.5f;
        private static readonly int[] Seeds = { 1, 2, 3 };

        // Số của spec 3.2: ba món, ba loại khách.
        private static RestaurantConfig Sample(float arrivalInterval = 15f, UpgradeCatalog upgrades = null)
        {
            var config = RestaurantTestData.Config(upgrades);
            RestaurantTestData.SetList(config, "_dishes", RestaurantTestData.Dish(20, 6), RestaurantTestData.Dish(15, 4), RestaurantTestData.Dish(25, 5));
            RestaurantTestData.SetList(config, "_customers",
                RestaurantTestData.Customer(6, 40, 1.0f), RestaurantTestData.Customer(3, 30, 1.5f), RestaurantTestData.Customer(1, 20, 1.2f));
            TestData.Edit(config, so => so.FindProperty("_baseArrivalInterval").floatValue = arrivalInterval);
            return config;
        }

        // perRole người mỗi vai, cùng một chỉ số; hồ sơ cắt theo số ô của save.
        private static RestaurantProfile Staffed(RestaurantConfig config, PlayerSave save, int perRole, int stat)
        {
            var characters = new CharacterDefinition[perRole * 3];
            for (var i = 0; i < characters.Length; i++) characters[i] = RestaurantTestData.Worker($"c{i}", stat, stat, stat);
            var roster = new Roster(save, TestData.Catalog(characters));
            for (var i = 0; i < characters.Length; i++)
            {
                roster.Add(characters[i].Id);
                roster.AssignStaff(characters[i].Id, (StaffRole)(i / perRole));
            }

            return RestaurantProfile.Build(config, save, roster);
        }

        private static void AssertRealIncomeWithinBounds(RestaurantConfig config, RestaurantProfile profile)
        {
            var theory = IncomeCalculator.Estimate(config, profile).CoinsPerSecond;
            Assert.Greater(theory, 0, "state must have all three roles staffed");

            var sum = 0.0;
            foreach (var seed in Seeds)
            {
                var sim = new RestaurantSimulation(config, new System.Random(seed));
                var coins = 0L;
                sim.CustomerPaid += (_, paid) => coins += paid;
                sim.SetProfile(profile);
                for (var t = 0f; t < Seconds; t += TickStep) sim.Tick(TickStep);
                sum += coins / (double)Seconds;
            }

            var real = sum / Seeds.Length;
            Assert.LessOrEqual(real, theory * 1.10, $"real {real:F4}/s must not exceed theory {theory:F4}/s by more than 10%");
            Assert.GreaterOrEqual(real, theory * config.AfkFactorMax,
                $"real {real:F4}/s must be at least theory {theory:F4}/s x {config.AfkFactorMax}");
        }

        [Test]
        public void FreshRestaurant_RealIncomeIsWithinTheoryBounds()
        {
            var config = Sample();
            AssertRealIncomeWithinBounds(config, Staffed(config, new PlayerSave(), 1, 30));
        }

        [Test]
        public void FullyStaffedHighStats_RealIncomeIsWithinTheoryBounds()
        {
            var config = Sample();
            AssertRealIncomeWithinBounds(config, Staffed(config, new PlayerSave(), 1, 100));
        }

        [Test]
        public void AfterUpgrades_RealIncomeIsWithinTheoryBounds()
        {
            var catalog = RestaurantTestData.UpgradeCatalog(
                RestaurantTestData.Upgrade("up_tables", UpgradeEffect.Tables, RestaurantTestData.Level(8, 1, 1)),
                RestaurantTestData.Upgrade("up_cook_slots", UpgradeEffect.CookSlots, RestaurantTestData.Level(2, 1, 1)),
                RestaurantTestData.Upgrade("up_serve_slots", UpgradeEffect.ServeSlots, RestaurantTestData.Level(2, 1, 1)),
                RestaurantTestData.Upgrade("up_cashier_slots", UpgradeEffect.CashierSlots, RestaurantTestData.Level(2, 1, 1)),
                RestaurantTestData.Upgrade("up_arrivals", UpgradeEffect.ArrivalInterval, RestaurantTestData.Level(0.59f, 1, 1)),
                RestaurantTestData.Upgrade("up_prices", UpgradeEffect.DishPrice, RestaurantTestData.Level(1.5f, 1, 1)));
            var config = Sample(upgrades: catalog);
            var save = new PlayerSave();
            foreach (var upgrade in catalog.Items) save.Upgrades.Add(new UpgradeLevel { UpgradeId = upgrade.Id, Level = 1 });

            var profile = Staffed(config, save, 2, 50);
            Assert.AreEqual(8, profile.Tables);
            Assert.AreEqual(2, profile.Workers(StaffRole.Cook).Count);
            AssertRealIncomeWithinBounds(config, profile);
        }

        [Test]
        public void Overloaded_RealIncomeIsWithinTheoryBounds()
        {
            // Khách đến mỗi 3 giây trên 3 bàn: hàng chờ luôn đầy, một phần khách bỏ về.
            var config = Sample(arrivalInterval: 3f);
            AssertRealIncomeWithinBounds(config, Staffed(config, new PlayerSave(), 1, 30));
        }
    }
}
