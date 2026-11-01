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

        // ---- Task 6: kiên nhẫn, thiếu người, trạm bị chặn, đổi hồ sơ, Reset ----

        private static RestaurantConfig Cfg(float patience = 40f, float cook = 5f, float serve = 4f, float cashier = 3f, float arrival = 15f, int tables = 3, int line = 4)
        {
            var config = RestaurantTestData.Config();
            RestaurantTestData.SetList(config, "_dishes", RestaurantTestData.Dish(20, cook));
            RestaurantTestData.SetList(config, "_customers", RestaurantTestData.Customer(1, patience, 1));
            TestData.Edit(config, so =>
            {
                so.FindProperty("_serveSeconds").floatValue = serve;
                so.FindProperty("_cashierSeconds").floatValue = cashier;
                so.FindProperty("_baseArrivalInterval").floatValue = arrival;
                so.FindProperty("_baseTables").intValue = tables;
                so.FindProperty("_maxWaitingLine").intValue = line;
            });
            return config;
        }

        private static RestaurantProfile ProfileOf(RestaurantConfig config, int[] cook = null, int[] serve = null, int[] cashier = null) =>
            RestaurantTestData.Profile(config, cook ?? One, serve ?? One, cashier ?? One);

        // Khách 1 đến lúc 7,5 và hết kiên nhẫn lúc 17,5 (patience 10); khách 2 đến lúc 18,0, sau khi khách 1 đã đi.
        [TestCase(CustomerState.WaitingForTable, 5f, 4f, -1, 0)]
        [TestCase(CustomerState.WaitingForCook, 5f, 4f, (int)StaffRole.Cook, 1)]
        [TestCase(CustomerState.Cooking, 100f, 4f, -1, 1)]
        [TestCase(CustomerState.WaitingForServer, 5f, 4f, (int)StaffRole.Server, 1)]
        [TestCase(CustomerState.Serving, 5f, 100f, -1, 1)]
        public void Patience1_ExpiresInEachState_NoPayment_FreesResources(CustomerState state, float cook, float serve, int blockedRole, int tablesAtStart)
        {
            var config = Cfg(patience: 10f, cook: cook, serve: serve, tables: tablesAtStart);
            var oneTable = ProfileOf(Cfg(patience: 10f, cook: cook, serve: serve, tables: 1));
            var sim = Make(config, new ScriptedRandom(0.99, 0.0, 0.0, 0.0, 0.2), ProfileOf(config));
            if (blockedRole >= 0) sim.SetStationBlocked((StaffRole)blockedRole, true);
            var paid = 0;
            sim.CustomerPaid += (_, _) => paid++;

            sim.Tick(17.3f);
            Assert.AreEqual(state, sim.Customers.Single().State);

            sim.Tick(0.4f);
            Assert.AreEqual(0, sim.Customers.Count);
            Assert.AreEqual(1, sim.Abandoned);
            Assert.AreEqual(0, sim.Served);
            Assert.AreEqual(0, paid);
            Assert.IsTrue(sim.Workers.All(w => w.CustomerId == -1));

            // Bàn đã nhả và khỏi hàng chờ bàn: khách 2 (t = 18,0) ngồi ngay vào bàn 0.
            sim.SetProfile(oneTable);
            sim.Tick(0.6f);
            var second = sim.Customers.Single();
            Assert.AreEqual(0, second.TableIndex);
            Assert.AreNotEqual(CustomerState.WaitingForTable, second.State);
        }

        // Khoảng đến 40 giây: khách 1 đến lúc 20, nấu xong 25, bưng xong 29 (bắt đầu ăn), ăn xong 37, rồi còn 10 giây kiên nhẫn tới 47.
        [Test]
        public void Patience2_ExpiresWhileWaitingForCashier_NoPayment()
        {
            var config = Cfg(patience: 10f, arrival: 40f);
            var sim = Make(config, new ScriptedRandom(0.99, 0.0, 0.0, 0.0), ProfileOf(config));
            sim.SetStationBlocked(StaffRole.Cashier, true);
            var paid = 0;
            sim.CustomerPaid += (_, _) => paid++;

            sim.Tick(46.8f);
            Assert.AreEqual(CustomerState.WaitingForCashier, sim.Customers.Single().State);

            sim.Tick(0.4f);

            Assert.AreEqual(0, sim.Customers.Count);
            Assert.AreEqual(1, sim.Abandoned);
            Assert.AreEqual(0, sim.Served);
            Assert.AreEqual(0, paid);
        }

        [Test]
        public void Patience2_ExpiresWhilePaying_NoPayment()
        {
            var config = Cfg(patience: 10f, cashier: 100f, arrival: 40f);
            var sim = Make(config, new ScriptedRandom(0.99, 0.0, 0.0, 0.0), ProfileOf(config));
            var paid = 0;
            sim.CustomerPaid += (_, _) => paid++;

            sim.Tick(46.8f);
            Assert.AreEqual(CustomerState.Paying, sim.Customers.Single().State);
            Assert.AreEqual(sim.Customers.Single().Id, sim.Workers.Single(w => w.Role == StaffRole.Cashier).CustomerId);

            sim.Tick(0.4f);

            Assert.AreEqual(0, sim.Customers.Count);
            Assert.AreEqual(1, sim.Abandoned);
            Assert.AreEqual(0, paid);
            Assert.AreEqual(-1, sim.Workers.Single(w => w.Role == StaffRole.Cashier).CustomerId);
        }

        [Test]
        public void Eating_DoesNotRunPatience()
        {
            // Kiên nhẫn 5 giây, ăn 8 giây; các trạm nhanh (nấu 2,5 + bưng 2 = 4,5 giây tới lúc ăn, thu 1,5).
            var config = Cfg(patience: 5f);
            var fast = new[] { 100 };
            var sim = Make(config, OneCustomerOnly(), ProfileOf(config, fast, fast, fast));

            sim.Tick(22f);

            Assert.AreEqual(1, sim.Served);
            Assert.AreEqual(0, sim.Abandoned);
        }

        [TestCase(StaffRole.Cook)]
        [TestCase(StaffRole.Server)]
        [TestCase(StaffRole.Cashier)]
        public void MissingRole_CustomersLeaveUnpaid(StaffRole missing)
        {
            var config = RestaurantTestData.Config();
            int[] Crew(StaffRole role) => role == missing ? new int[0] : One;
            var sim = Make(config, new ScriptedRandom(), ProfileOf(config, Crew(StaffRole.Cook), Crew(StaffRole.Server), Crew(StaffRole.Cashier)));

            sim.Tick(90f);

            Assert.AreEqual(0, sim.Served);
            Assert.GreaterOrEqual(sim.Abandoned, 1);
        }

        [Test]
        public void StationBlocked_FreezesProgress_ThenResumes()
        {
            // Nấu 5 giây từ 7,5; chặn lúc 10 (còn 2,5 giây), mở lúc 20: xong lúc 22,5. Làm lại từ đầu thì xong 25,0; không đóng băng thì xong 12,5.
            var sim = Make(RestaurantTestData.Config(), OneCustomerOnly());
            sim.Tick(10f);
            var id = sim.Customers.Single().Id;
            sim.SetStationBlocked(StaffRole.Cook, true);

            sim.Tick(10f);
            Assert.AreEqual(CustomerState.Cooking, sim.Customers.Single(c => c.Id == id).State);
            sim.SetStationBlocked(StaffRole.Cook, false);

            sim.Tick(2.2f);
            Assert.AreEqual(CustomerState.Cooking, sim.Customers.Single(c => c.Id == id).State);
            sim.Tick(0.6f);
            Assert.AreNotEqual(CustomerState.Cooking, sim.Customers.Single(c => c.Id == id).State);
        }

        [Test]
        public void StationBlocked_NoNewJobsStarted()
        {
            var sim = Make(RestaurantTestData.Config(), OneCustomerOnly());
            sim.SetStationBlocked(StaffRole.Server, true);

            // Khách thứ hai của OneCustomerOnly đến lúc 29,85, nên dừng ở 29.
            sim.Tick(29f);
            Assert.AreEqual(CustomerState.WaitingForServer, sim.Customers.Single().State);
            Assert.AreEqual(-1, sim.Workers.Single(w => w.Role == StaffRole.Server).CustomerId);

            sim.SetStationBlocked(StaffRole.Server, false);
            sim.Tick(0.2f);

            Assert.AreEqual(CustomerState.Serving, sim.Customers.Single().State);
        }

        [Test]
        public void SetProfile_MoreTables_AppliesImmediately()
        {
            var oneTable = Cfg(tables: 1);
            var sim = Make(oneTable, new ScriptedRandom(), ProfileOf(oneTable));
            sim.Tick(16f);
            Assert.AreEqual(1, sim.Customers.Count(c => c.State == CustomerState.WaitingForTable));

            sim.SetProfile(ProfileOf(Cfg(tables: 3)));
            sim.Tick(0.1f);

            Assert.AreEqual(0, sim.Customers.Count(c => c.State == CustomerState.WaitingForTable));
        }

        [Test]
        public void SetProfile_NewWorker_AppliesImmediately()
        {
            var config = Cfg(cook: 20f);
            var sim = Make(config, new ScriptedRandom(), ProfileOf(config));
            sim.Tick(16f);
            Assert.AreEqual(1, sim.Customers.Count(c => c.State == CustomerState.WaitingForCook));

            sim.SetProfile(ProfileOf(config, cook: new[] { 0, 0 }));
            sim.Tick(0.1f);

            Assert.AreEqual(0, sim.Customers.Count(c => c.State == CustomerState.WaitingForCook));
            Assert.AreEqual(2, sim.Customers.Count(c => c.State == CustomerState.Cooking));
        }

        [Test]
        public void SetProfile_RemovedWorker_JobReturnsToFrontOfQueue_ProgressLost()
        {
            // Khách 1 nấu 20 giây từ 7,5; lúc 16 người nấu bị thay (mất 8,5 giây tiến độ); khách 2 (t = 15) đang chờ phía sau.
            var config = Cfg(cook: 20f);
            var sim = Make(config, new ScriptedRandom(), ProfileOf(config));
            sim.Tick(10f);
            var first = sim.Customers.Single().Id;
            sim.Tick(6f);
            Assert.AreEqual(1, sim.Customers.Count(c => c.State == CustomerState.WaitingForCook));

            sim.SetProfile(ProfileOf(config).WithWorkers(StaffRole.Cook, new[] { new WorkerInfo("replacement", 0) }));
            Assert.AreEqual(CustomerState.WaitingForCook, sim.Customers.Single(c => c.Id == first).State);

            sim.Tick(0.2f);
            Assert.AreEqual(first, sim.Customers.Single(c => c.State == CustomerState.Cooking).Id);
            Assert.AreEqual("replacement", sim.Workers.Single(w => w.Role == StaffRole.Cook).CharacterId);

            // Làm lại đủ 20 giây từ lúc 16,1 (xong 36,1), không phải 11,5 giây còn lại (xong 27,5).
            sim.Tick(19f);
            Assert.AreEqual(CustomerState.Cooking, sim.Customers.Single(c => c.Id == first).State);
            sim.Tick(1.5f);
            Assert.AreNotEqual(CustomerState.Cooking, sim.Customers.Single(c => c.Id == first).State);
        }

        [Test]
        public void Reset_ClearsCustomersCountersAndQueues()
        {
            // Khách đến mỗi 1 giây, một bàn, hàng chờ 1: có khách đã trả tiền, có khách bị bỏ về, có khách đang ngồi và chờ.
            var config = Cfg(arrival: 2f, tables: 1, line: 1);
            var random = new ScriptedRandom();
            var sim = Make(config, random, ProfileOf(config));
            sim.Tick(22f);
            Assert.Greater(sim.Served, 0);
            Assert.Greater(sim.Abandoned, 0);
            Assert.Greater(sim.Customers.Count, 0);
            var callsBefore = random.Calls;

            sim.Reset();

            Assert.AreEqual(callsBefore + 1, random.Calls);
            Assert.AreEqual(0, sim.Customers.Count);
            Assert.AreEqual(0, sim.Served);
            Assert.AreEqual(0, sim.Abandoned);
            Assert.IsTrue(sim.Workers.All(w => w.CustomerId == -1));

            // Khoảng đến đầu mới là 0,5 × 2 = 1,0 giây.
            sim.Tick(0.8f);
            Assert.AreEqual(0, sim.Customers.Count);
            sim.Tick(0.4f);
            Assert.AreEqual(1, sim.Customers.Count);
            Assert.AreEqual(CustomerState.Cooking, sim.Customers.Single().State);
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
