using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Cấu hình mẫu: món 20 Coin nấu 5 giây, bưng 4, ăn 8, thu 3; thời gian xử lý = gốc × 100 / (100 + chỉ số).
    public class IncomeCalculatorTests
    {
        private const double Delta = 0.001;

        private static RestaurantConfig Config(float arrival = 15f, int tables = 3)
        {
            var config = RestaurantTestData.Config();
            TestData.Edit(config, so =>
            {
                so.FindProperty("_baseArrivalInterval").floatValue = arrival;
                so.FindProperty("_baseTables").intValue = tables;
            });
            return config;
        }

        private static RestaurantProfile Staffed(RestaurantConfig config, int[] cook, int[] serve, int[] cashier, PlayerSave save = null)
        {
            save ??= new PlayerSave();
            var roster = new Roster(save, TestData.Catalog());
            return RestaurantProfile.Build(config, save, roster)
                .WithWorkers(StaffRole.Cook, Workers(cook))
                .WithWorkers(StaffRole.Server, Workers(serve))
                .WithWorkers(StaffRole.Cashier, Workers(cashier));
        }

        private static WorkerInfo[] Workers(int[] stats) => stats.Select((stat, i) => new WorkerInfo($"w{i}", stat)).ToArray();

        private static double Income(RestaurantConfig config, RestaurantProfile profile) =>
            IncomeCalculator.Estimate(config, profile).CoinsPerSecond;

        [Test]
        public void Arrival_IsBottleneck()
        {
            var config = Config();
            var profile = Staffed(config, new[] { 0 }, new[] { 0 }, new[] { 0 });
            var estimate = IncomeCalculator.Estimate(config, profile);
            Assert.AreEqual(20.0 / 15.0, estimate.CoinsPerSecond, Delta);
            Assert.IsNull(estimate.MissingRole);
        }

        [Test]
        public void Cook_IsBottleneck()
        {
            var config = Config(arrival: 1, tables: 100);
            var profile = Staffed(config, new[] { 0 }, new[] { 100 }, new[] { 100 });
            Assert.AreEqual(4.0, Income(config, profile), Delta);
        }

        [Test]
        public void Server_IsBottleneck()
        {
            var config = Config(arrival: 1, tables: 100);
            var profile = Staffed(config, new[] { 100 }, new[] { 0 }, new[] { 100 });
            Assert.AreEqual(5.0, Income(config, profile), Delta);
        }

        [Test]
        public void Cashier_IsBottleneck()
        {
            var config = Config(arrival: 1, tables: 100);
            var profile = Staffed(config, new[] { 100 }, new[] { 100 }, new[] { 0 });
            Assert.AreEqual(20.0 / 3.0, Income(config, profile), Delta);
        }

        [Test]
        public void Tables_IsBottleneck()
        {
            // Chu kỳ một bàn: nấu 2,5 + bưng 2 + ăn 8 + thu 1,5 = 14 giây.
            var config = Config(arrival: 1, tables: 3);
            var three = new[] { 100, 100, 100 };
            var profile = Staffed(config, three, three, three);
            Assert.AreEqual(20.0 * 3.0 / 14.0, Income(config, profile), Delta);
        }

        [TestCase(StaffRole.Cook)]
        [TestCase(StaffRole.Server)]
        [TestCase(StaffRole.Cashier)]
        public void MissingRole_GivesZeroAndNamesRole(StaffRole missing)
        {
            var config = Config();
            int[] Crew(StaffRole role) => role == missing ? new int[0] : new[] { 50 };
            var profile = Staffed(config, Crew(StaffRole.Cook), Crew(StaffRole.Server), Crew(StaffRole.Cashier));

            var estimate = IncomeCalculator.Estimate(config, profile);

            Assert.AreEqual(0.0, estimate.CoinsPerSecond);
            Assert.AreEqual(missing, estimate.MissingRole);
        }

        [Test]
        public void TwoWorkersInOneRole_AddCapacities()
        {
            var config = Config(arrival: 1, tables: 100);

            // Nấu: 0,2 + 0,4 = 0,6; Server (0,5) là cổ chai nên thêm người nấu không đổi kết quả.
            Assert.AreEqual(10.0, Income(config, Staffed(config, new[] { 0, 100 }, new[] { 100 }, new[] { 100 })), Delta);
            Assert.AreEqual(10.0, Income(config, Staffed(config, new[] { 0, 100, 100 }, new[] { 100 }, new[] { 100 })), Delta);
            // Nếu chỉ lấy người giỏi nhất thay vì cộng, người nấu 0 + 100 sẽ cho 0,4 → 8; người 100 đứng một mình đúng là 8.
            Assert.AreEqual(8.0, Income(config, Staffed(config, new[] { 100 }, new[] { 100 }, new[] { 100 })), Delta);
            // Bưng chỉ số 0 (0,25) thành cổ chai bất kể số người nấu.
            Assert.AreEqual(5.0, Income(config, Staffed(config, new[] { 0, 100 }, new[] { 0 }, new[] { 100 })), Delta);
        }

        [Test]
        public void StatDoublesSpeed()
        {
            var config = Config(arrival: 1, tables: 100);
            var slow = Income(config, Staffed(config, new[] { 0 }, new[] { 100 }, new[] { 100 }));
            var fast = Income(config, Staffed(config, new[] { 100 }, new[] { 100 }, new[] { 100 }));
            Assert.AreEqual(slow * 2, fast, Delta);
        }

        [Test]
        public void MoneyPerCustomer_UsesWeightedMeans()
        {
            var config = RestaurantTestData.Config(RestaurantTestData.UpgradeCatalog(
                RestaurantTestData.Upgrade("up_prices", UpgradeEffect.DishPrice, RestaurantTestData.Level(1.1f, 1, 1))));
            RestaurantTestData.SetList(config, "_dishes", RestaurantTestData.Dish(20, 5), RestaurantTestData.Dish(40, 5));
            RestaurantTestData.SetList(config, "_customers", RestaurantTestData.Customer(1, 40, 1.0f), RestaurantTestData.Customer(3, 40, 2.0f));
            TestData.Edit(config, so =>
            {
                so.FindProperty("_baseArrivalInterval").floatValue = 10f;
                so.FindProperty("_baseTables").intValue = 100;
            });
            var save = new PlayerSave();
            save.Upgrades.Add(new UpgradeLevel { UpgradeId = "up_prices", Level = 1 });
            var profile = Staffed(config, new[] { 100 }, new[] { 100 }, new[] { 100 }, save);

            // Khách đến mỗi 10 giây là cổ chai (0,1 khách/giây); tiền/khách = 30 × 1,75 × 1,1 = 57,75.
            Assert.AreEqual(0.1 * 57.75, Income(config, profile), Delta);
        }

        [Test]
        public void ArrivalMultiplier_ScalesArrival()
        {
            var config = RestaurantTestData.Config(RestaurantTestData.UpgradeCatalog(
                RestaurantTestData.Upgrade("up_arrivals", UpgradeEffect.ArrivalInterval, RestaurantTestData.Level(0.5f, 1, 1))));
            TestData.Edit(config, so => so.FindProperty("_baseTables").intValue = 100);
            var save = new PlayerSave();
            var before = Income(config, Staffed(config, new[] { 100 }, new[] { 100 }, new[] { 100 }, save));
            save.Upgrades.Add(new UpgradeLevel { UpgradeId = "up_arrivals", Level = 1 });
            var after = Income(config, Staffed(config, new[] { 100 }, new[] { 100 }, new[] { 100 }, save));

            Assert.AreEqual(20.0 / 15.0, before, Delta);
            Assert.AreEqual(before * 2, after, Delta);
        }

        [Test]
        public void CookTime_IsAverageOfDishes()
        {
            var twoDishes = Config(arrival: 1, tables: 100);
            RestaurantTestData.SetList(twoDishes, "_dishes", RestaurantTestData.Dish(20, 4), RestaurantTestData.Dish(20, 6));
            var oneDish = Config(arrival: 1, tables: 100);

            var crew = new[] { 0 };
            var fast = new[] { 100 };
            Assert.AreEqual(Income(oneDish, Staffed(oneDish, crew, fast, fast)), Income(twoDishes, Staffed(twoDishes, crew, fast, fast)), Delta);
        }
    }
}
