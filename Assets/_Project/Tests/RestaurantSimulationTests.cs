using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.Tests
{
    // Cấu hình mẫu: món 20 Coin nấu 5 giây, bưng 4, ăn 8, thu 3, khách đầu đến sau 0,5 × 15 = 7,5 giây khi RNG trả 0,0.
    // Với nhân viên chỉ số 0, một khách đi hết dây chuyền và trả tiền ở t = 7,5 + 5 + 4 + 8 + 3 = 27,5.
    public class RestaurantSimulationTests
    {
        private static readonly int[] One = { 0 };

        private static RestaurantSimulation Make(RestaurantConfig config, System.Random random, RestaurantProfile profile)
        {
            var sim = new RestaurantSimulation(config, random);
            sim.SetProfile(profile);
            return sim;
        }

        private static RestaurantSimulation Make(RestaurantConfig config, System.Random random, int[] cook = null, int[] serve = null, int[] cashier = null) =>
            Make(config, random, RestaurantTestData.Profile(config, cook ?? One, serve ?? One, cashier ?? One));

        // Ba giá trị đầu cho khách đầu (khoảng đến đầu, loại, món), rồi khoảng đến kế rất dài để chỉ có một khách.
        private static ScriptedRandom OneCustomerOnly() => new ScriptedRandom(0.99, 0.0, 0.0, 0.0);

        private static void Run(RestaurantSimulation sim, float seconds, float dt = 0.1f)
        {
            var steps = Mathf.RoundToInt(seconds / dt);
            for (var i = 0; i < steps; i++) sim.Tick(dt);
        }

        private static RestaurantConfig WithEdit(System.Action<UnityEditor.SerializedObject> set)
        {
            var config = RestaurantTestData.Config();
            TestData.Edit(config, set);
            return config;
        }

        [Test]
        public void OneCustomer_FullPipeline_PaysAtExpectedTimeAndAmount()
        {
            var sim = Make(RestaurantTestData.Config(), OneCustomerOnly());
            var paid = new List<(int id, long coins)>();
            sim.CustomerPaid += (id, coins) => paid.Add((id, coins));

            // Mỗi trạm đúng bước 0,1 giây: chưa trả ở 27,45, trả ở 27,55 (chặt hơn dung sai ±0,2 của kế hoạch, để bắt lệch một bước).
            sim.Tick(27.45f);
            Assert.AreEqual(0, sim.Served);
            Assert.AreEqual(1, sim.Customers.Count);

            sim.Tick(0.1f);

            Assert.AreEqual(1, sim.Served);
            Assert.AreEqual(1, paid.Count);
            Assert.AreEqual(20, paid[0].coins);
            Assert.AreEqual(0, sim.Customers.Count);
        }

        [Test]
        public void HigherStat_ShortensWork()
        {
            // Nấu 2,5 thay vì 5: trả tiền ở t = 25,0. Chỉ số 0 sẽ chưa trả ở t = 25,2.
            var sim = Make(RestaurantTestData.Config(), OneCustomerOnly(), cook: new[] { 100 });

            sim.Tick(24.8f);
            Assert.AreEqual(0, sim.Served);
            sim.Tick(0.4f);
            Assert.AreEqual(1, sim.Served);
        }

        [Test]
        public void TwoCustomers_AreCookedFifoByOneCook()
        {
            var config = RestaurantTestData.Config();
            RestaurantTestData.SetList(config, "_dishes", RestaurantTestData.Dish(20, 20));
            var sim = Make(config, new ScriptedRandom());

            // Khách 1 nấu từ 7,5 đến 27,5; khách 2 (t = 15) và 3 (t = 22,5) xếp hàng chờ nấu.
            sim.Tick(10f);
            var first = sim.Customers.Single(c => c.State == CustomerState.Cooking).Id;
            sim.Tick(15f);
            var waiting = sim.Customers.Where(c => c.State == CustomerState.WaitingForCook).Select(c => c.Id).ToList();
            Assert.AreEqual(2, waiting.Count);

            sim.Tick(3f);

            var cooking = sim.Customers.Single(c => c.State == CustomerState.Cooking).Id;
            Assert.AreEqual(waiting.Min(), cooking);
            Assert.AreNotEqual(first, cooking);
        }

        [Test]
        public void TableLimit_SecondCustomerWaitsForTable()
        {
            var sim = Make(WithEdit(so => so.FindProperty("_baseTables").intValue = 1), new ScriptedRandom());

            sim.Tick(20f);
            var second = sim.Customers.Single(c => c.State == CustomerState.WaitingForTable);
            Assert.AreEqual(-1, second.TableIndex);

            sim.Tick(8f);

            Assert.IsFalse(sim.Customers.Any(c => c.Id == second.Id && c.State == CustomerState.WaitingForTable));
            Assert.AreEqual(0, sim.Customers.Single(c => c.Id == second.Id).TableIndex);
        }

        [Test]
        public void WaitingLineFull_NewCustomerNotCreated_AbandonedIncrements_NoRngForTypeAndDish()
        {
            // Khách đến mỗi 1 giây, một bàn, nấu 100 giây: khách 1 ngồi bàn, khách 2 vào hàng chờ, khách 3 (t = 3) bị từ chối.
            var config = WithEdit(so =>
            {
                so.FindProperty("_baseArrivalInterval").floatValue = 2f;
                so.FindProperty("_baseTables").intValue = 1;
                so.FindProperty("_maxWaitingLine").intValue = 1;
            });
            RestaurantTestData.SetList(config, "_dishes", RestaurantTestData.Dish(20, 100));
            var random = new ScriptedRandom();
            var sim = Make(config, random);

            sim.Tick(2.5f);
            Assert.AreEqual(7, random.Calls);
            Assert.AreEqual(2, sim.Customers.Count);
            Assert.AreEqual(0, sim.Abandoned);

            sim.Tick(1f);

            Assert.AreEqual(1, sim.Abandoned);
            Assert.AreEqual(2, sim.Customers.Count);
            Assert.AreEqual(8, random.Calls);
        }

        [Test]
        public void Price_RoundsHalfAwayFromZero()
        {
            var config = RestaurantTestData.Config();
            RestaurantTestData.SetList(config, "_dishes", RestaurantTestData.Dish(15, 5));
            RestaurantTestData.SetList(config, "_customers", RestaurantTestData.Customer(1, 40, 1.5f));
            var sim = Make(config, OneCustomerOnly());
            var paid = new List<long>();
            sim.CustomerPaid += (_, coins) => paid.Add(coins);

            sim.Tick(27.7f);

            CollectionAssert.AreEqual(new long[] { 23 }, paid);
        }

        [Test]
        public void PriceMultiplier_Applies()
        {
            var config = RestaurantTestData.Config(RestaurantTestData.UpgradeCatalog(
                RestaurantTestData.Upgrade("up_prices", UpgradeEffect.DishPrice, RestaurantTestData.Level(1.5f, 1, 1))));
            var save = new PlayerSave();
            save.Upgrades.Add(new UpgradeLevel { UpgradeId = "up_prices", Level = 1 });
            var sim = Make(config, OneCustomerOnly(), RestaurantTestData.Profile(config, One, One, One, save));
            var paid = new List<long>();
            sim.CustomerPaid += (_, coins) => paid.Add(coins);

            sim.Tick(27.7f);

            CollectionAssert.AreEqual(new long[] { 30 }, paid);
        }

        [Test]
        public void TickLargeDt_EqualsManySmallTicks()
        {
            // Khách đến dày, hai loại khách và hai món, RNG có nhiều giá trị khác nhau để cài đặt sai đổi kết quả.
            var values = new[] { 0.3, 0.8, 0.1, 0.6, 0.45, 0.9, 0.2, 0.7, 0.55, 0.35, 0.05, 0.95 };
            RestaurantSimulation Build(out ScriptedRandom random)
            {
                var config = WithEdit(so => so.FindProperty("_baseArrivalInterval").floatValue = 3f);
                RestaurantTestData.SetList(config, "_dishes", RestaurantTestData.Dish(20, 5), RestaurantTestData.Dish(35, 7));
                RestaurantTestData.SetList(config, "_customers", RestaurantTestData.Customer(2, 40, 1), RestaurantTestData.Customer(1, 40, 1.5f));
                random = new ScriptedRandom(0.5, values);
                return Make(config, random, new[] { 20 }, new[] { 10 }, new[] { 30 });
            }

            var one = Build(out var oneRandom);
            var many = Build(out var manyRandom);

            one.Tick(120f);
            for (var i = 0; i < 1200; i++) many.Tick(0.1f);

            Assert.Greater(one.Served, 0);
            Assert.AreEqual(one.Served, many.Served);
            Assert.AreEqual(one.Abandoned, many.Abandoned);
            Assert.AreEqual(oneRandom.Calls, manyRandom.Calls);
            CollectionAssert.AreEqual(
                one.Customers.Select(c => (c.Id, c.State, c.TableIndex)).ToList(),
                many.Customers.Select(c => (c.Id, c.State, c.TableIndex)).ToList());
        }

        [Test]
        public void RngCallContract_ConstructorOne_PerArrivalThree()
        {
            var random = new ScriptedRandom();
            var sim = Make(RestaurantTestData.Config(), random);
            Assert.AreEqual(1, random.Calls);

            sim.Tick(7.3f);
            Assert.AreEqual(1, random.Calls);
            sim.Tick(0.4f);

            Assert.AreEqual(1, sim.Customers.Count);
            Assert.AreEqual(4, random.Calls);
        }

        [Test]
        public void Snapshots_ShowStatesInOrder()
        {
            var sim = Make(RestaurantTestData.Config(), OneCustomerOnly());
            var seen = new List<CustomerState>();
            for (var i = 0; i < 290; i++)
            {
                sim.Tick(0.1f);
                foreach (var customer in sim.Customers)
                    if (seen.Count == 0 || seen[seen.Count - 1] != customer.State) seen.Add(customer.State);
            }

            // Chỉ có một khách; trạng thái chỉ đi tới, và đi qua cả bốn việc.
            for (var i = 1; i < seen.Count; i++) Assert.Greater((int)seen[i], (int)seen[i - 1]);
            foreach (var state in new[] { CustomerState.Cooking, CustomerState.Serving, CustomerState.Eating, CustomerState.Paying })
                CollectionAssert.Contains(seen, state);
        }

        [Test]
        public void PaidHandlerThrows_DoesNotBreakSimulation()
        {
            var sim = Make(RestaurantTestData.Config(), new ScriptedRandom());
            sim.CustomerPaid += (_, _) => throw new System.InvalidOperationException("boom");

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            sim.Tick(27.7f);
            Assert.AreEqual(1, sim.Served);

            // Khách 2 đến lúc 15, trả lúc 35: mô phỏng vẫn chạy tiếp sau lần ném lỗi đầu.
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            sim.Tick(8f);
            Assert.AreEqual(2, sim.Served);
        }

        [Test]
        public void Workers_ReportCustomerWhileBusy()
        {
            var sim = Make(RestaurantTestData.Config(), OneCustomerOnly());

            sim.Tick(5f);
            Assert.AreEqual(3, sim.Workers.Count);
            Assert.IsTrue(sim.Workers.All(w => w.CustomerId == -1));

            sim.Tick(5f);
            var cooking = sim.Customers.Single(c => c.State == CustomerState.Cooking);
            Assert.AreEqual(cooking.Id, sim.Workers.Single(w => w.Role == StaffRole.Cook).CustomerId);
            Assert.AreEqual("cook0", sim.Workers.Single(w => w.Role == StaffRole.Cook).CharacterId);
            Assert.AreEqual(-1, sim.Workers.Single(w => w.Role == StaffRole.Server).CustomerId);
            Assert.AreEqual(-1, sim.Workers.Single(w => w.Role == StaffRole.Cashier).CustomerId);
        }
    }
}
