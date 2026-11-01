using System;
using System.Linq;
using System.Text.RegularExpressions;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.Tests
{
    // Cấu hình mẫu như RestaurantSimulationTests: món 20 Coin nấu 5 giây, khách đầu trả tiền ở t = 27,5 khi nhân viên chỉ số 0.
    public class RestaurantServiceTests
    {
        private sealed class Fixture
        {
            public RestaurantService Service;
            public PlayerSave Save;
            public Wallet Wallet;
            public Roster Roster;
            public FakeClock Clock;
            public int Persists;
        }

        // Một chỗ dựng duy nhất: Task 10, 11, 13 chỉ phải sửa ở đây.
        private static Fixture Make(System.Random random = null, RestaurantConfig config = null, bool staffed = true, Func<bool> persist = null)
        {
            var save = new PlayerSave();
            var catalog = TestData.Catalog(
                RestaurantTestData.Worker("cook0", 0, 0, 0),
                RestaurantTestData.Worker("serve0", 0, 0, 0),
                RestaurantTestData.Worker("cashier0", 0, 0, 0));
            var roster = new Roster(save, catalog);
            foreach (var id in new[] { "cook0", "serve0", "cashier0" }) roster.Add(id);
            if (staffed)
            {
                roster.AssignStaff("cook0", StaffRole.Cook);
                roster.AssignStaff("serve0", StaffRole.Server);
                roster.AssignStaff("cashier0", StaffRole.Cashier);
            }

            var fixture = new Fixture { Save = save, Wallet = new Wallet(save), Roster = roster, Clock = new FakeClock() };
            fixture.Service = new RestaurantService(config ?? RestaurantTestData.Config(), save, fixture.Wallet, roster,
                random ?? new ScriptedRandom(0.99, 0.0, 0.0, 0.0), fixture.Clock, persist ?? (() => { fixture.Persists++; return true; }));
            return fixture;
        }

        private static void Run(RestaurantService service, float seconds, float dt = 0.1f)
        {
            var steps = Mathf.RoundToInt(seconds / dt);
            for (var i = 0; i < steps; i++) service.Update(dt);
        }

        // Sự cố sinh đúng lúc 66 giây (bếp rảnh: khách ở t = 60 nấu xong lúc 65, khách kế đến lúc 67,5); chọn Cook vì RNG luôn trả 0.
        private static Fixture MakeWithIncidentAt66()
        {
            var config = RestaurantTestData.Config(null, RestaurantTestData.Incident(StaffRole.Cook));
            TestData.Edit(config, so =>
            {
                so.FindProperty("_incidentSpawnMin").floatValue = 66f;
                so.FindProperty("_incidentSpawnMax").floatValue = 66f;
            });
            return Make(new ScriptedRandom(0.0), config);
        }

        [Test]
        public void Presence_SpawnsIncidentAndBlocksStation_ThroughService()
        {
            var f = MakeWithIncidentAt66();
            f.Service.SetPresent(true);

            Run(f.Service, 65f);
            Assert.IsFalse(f.Service.Incidents.Active.HasValue, "chưa tới 66 giây");
            Run(f.Service, 5f);

            Assert.AreEqual(StaffRole.Cook, f.Service.Incidents.Active.Value.Role);
            var customers = f.Service.Simulation.Customers;
            Assert.IsTrue(customers.Any(c => c.State == CustomerState.WaitingForCook), "khách đến sau 66 giây không được nấu");
            Assert.IsFalse(customers.Any(c => c.State == CustomerState.Cooking));
        }

        [Test]
        public void ResetRuntime_CancelsIncidentAndUnblocks()
        {
            var f = MakeWithIncidentAt66();
            f.Service.SetPresent(true);
            Run(f.Service, 70f);
            Assert.IsTrue(f.Service.Incidents.Active.HasValue);

            f.Service.ResetRuntime();

            Assert.IsFalse(f.Service.Incidents.Active.HasValue);
            Assert.AreEqual(0, f.Save.IncidentScores.Count, "huỷ không tính điểm");
            // Khách đầu sau Reset đến lúc 7,5 giây và nấu xong lúc 12,5: tới 14 giây phải đã qua bước nấu.
            Run(f.Service, 14f);
            Assert.IsTrue(f.Service.Simulation.Customers.Any(c => c.State != CustomerState.WaitingForCook && c.State != CustomerState.WaitingForTable),
                "bếp phải mở lại sau Reset");
        }

        [Test]
        public void RecordPresence_OnlyWhenPresent()
        {
            var f = Make();

            f.Service.Update(10f);
            Assert.AreEqual(0, f.Service.Quality.TodaySeconds, 1e-9);

            f.Service.SetPresent(true);
            f.Service.Update(10f);
            Assert.AreEqual(10, f.Service.Quality.TodaySeconds, 1e-9);

            f.Service.SetPresent(false);
            f.Service.Update(10f);
            Assert.AreEqual(10, f.Service.Quality.TodaySeconds, 1e-9);
        }

        [Test]
        public void CustomerPaid_AddsCoinToWallet_ExactlyOnce()
        {
            var f = Make();
            var changes = 0;
            f.Wallet.Changed += () => changes++;

            Run(f.Service, 30f);

            Assert.AreEqual(1, f.Service.Simulation.Served);
            Assert.AreEqual(20, f.Wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void StaffMovedToBattleTeam_StopsIncomeAndEstimate()
        {
            var f = Make();
            Assert.Greater(f.Service.Estimate.CoinsPerSecond, 0);

            // Khách đầu đang được nấu thì người nấu duy nhất bị chuyển sang đội chiến đấu.
            Run(f.Service, 10f);
            Assert.AreEqual(CustomerState.Cooking, f.Service.Simulation.Customers[0].State);
            var before = f.Wallet.Get(CurrencyType.Coin);
            Assert.IsTrue(f.Roster.AddToTeam("cook0"));

            Assert.AreEqual(0, f.Service.Estimate.CoinsPerSecond);
            Assert.AreEqual(StaffRole.Cook, f.Service.Estimate.MissingRole);
            Assert.AreEqual(CustomerState.WaitingForCook, f.Service.Simulation.Customers[0].State);

            Run(f.Service, 60f);
            Assert.AreEqual(0, f.Service.Simulation.Served);
            Assert.AreEqual(before, f.Wallet.Get(CurrencyType.Coin));
        }

        [Test]
        public void RosterChanged_RebuildsProfile_AndRaisesProfileChanged()
        {
            var f = Make(staffed: false);
            var raised = 0;
            f.Service.ProfileChanged += () => raised++;
            Assert.AreEqual(0, f.Service.Profile.Workers(StaffRole.Cook).Count);

            f.Roster.AssignStaff("cook0", StaffRole.Cook);

            Assert.AreEqual(1, raised);
            Assert.AreEqual(1, f.Service.Profile.Workers(StaffRole.Cook).Count);
            // Mô phỏng cũng nhận hồ sơ mới, không chỉ service.
            Assert.AreEqual(1, f.Service.Simulation.Workers.Count);
        }

        [Test]
        public void InvalidConfig_IsNotRunning_UpdateDoesNotThrow_EstimateZero()
        {
            var config = RestaurantTestData.Config();
            RestaurantTestData.SetList(config, "_dishes");

            LogAssert.Expect(LogType.Error, new Regex("RestaurantService: invalid config"));
            var f = Make(config: config);

            Assert.IsFalse(f.Service.IsRunning);
            Assert.DoesNotThrow(() => Run(f.Service, 120f));
            Assert.AreEqual(0, f.Service.Estimate.CoinsPerSecond);
            Assert.AreEqual(0, f.Service.Simulation.Customers.Count);
            Assert.AreEqual(0, f.Persists);
        }

        [Test]
        public void ResetRuntime_ClearsSimulationAndCounters()
        {
            var f = Make();
            Run(f.Service, 30f);
            Assert.AreEqual(1, f.Service.Simulation.Served);

            f.Service.ResetRuntime();

            Assert.AreEqual(0, f.Service.Simulation.Served);
            Assert.AreEqual(0, f.Service.Simulation.Abandoned);
            Assert.AreEqual(0, f.Service.Simulation.Customers.Count);
            // Đồng hồ lưu định kỳ cũng về 0: 30 + 59,9 giây mà không xoá sẽ đã lưu một lần.
            Run(f.Service, 59.9f);
            Assert.AreEqual(0, f.Persists);
        }

        [Test]
        public void Update_Autosaves_EveryAutosaveSeconds()
        {
            var f = Make();

            Run(f.Service, 59.9f);
            Assert.AreEqual(0, f.Persists);

            f.Service.Update(0.1f);
            Assert.AreEqual(1, f.Persists);

            // Lần kế tiếp cũng phải cách đủ 60 giây, không lưu mỗi khung sau lần đầu.
            Run(f.Service, 59.9f);
            Assert.AreEqual(1, f.Persists);
            f.Service.Update(0.1f);
            Assert.AreEqual(2, f.Persists);
        }

        [Test]
        public void Autosave_PersistFailure_DoesNotThrow_AndRetriesNextInterval()
        {
            var calls = 0;
            var f = Make(persist: () => { calls++; return calls > 1 ? true : throw new System.IO.IOException("disk locked"); });

            LogAssert.Expect(LogType.Warning, new Regex("RestaurantService: autosave"));
            Assert.DoesNotThrow(() => Run(f.Service, 60f));
            Assert.AreEqual(1, calls);

            Run(f.Service, 60f);
            Assert.AreEqual(2, calls);
        }

        [Test]
        public void Estimate_ReportsMissingRole()
        {
            var f = Make(staffed: false);
            Assert.AreEqual(0, f.Service.Estimate.CoinsPerSecond);
            Assert.AreEqual(StaffRole.Cook, f.Service.Estimate.MissingRole);

            f.Roster.AssignStaff("cook0", StaffRole.Cook);
            f.Roster.AssignStaff("serve0", StaffRole.Server);
            Assert.AreEqual(StaffRole.Cashier, f.Service.Estimate.MissingRole);

            f.Roster.AssignStaff("cashier0", StaffRole.Cashier);
            Assert.IsNull(f.Service.Estimate.MissingRole);
            Assert.Greater(f.Service.Estimate.CoinsPerSecond, 0);
        }

        [Test]
        public void BuyUpgrade_TakesEffectInSimulation_Immediately()
        {
            var tables = RestaurantTestData.Upgrade("up_tables", UpgradeEffect.Tables, RestaurantTestData.Level(4, 300, 5));
            var f = Make(config: RestaurantTestData.Config(RestaurantTestData.UpgradeCatalog(tables)));
            f.Wallet.Add(CurrencyType.Coin, 300);
            f.Wallet.Add(CurrencyType.Material, 5);
            var raised = 0;
            f.Service.ProfileChanged += () => raised++;
            Assert.AreEqual(3, f.Service.Profile.Tables);

            var result = f.Service.Upgrades.TryBuy("up_tables");

            // Không Update: hồ sơ phải đổi ngay khi mua, không đợi khung kế tiếp.
            Assert.AreEqual(UpgradeBuyStatus.Success, result.Status);
            Assert.AreEqual(4, f.Service.Profile.Tables);
            Assert.AreEqual(1, raised);
            Assert.AreEqual(1, f.Persists);
        }

        [Test]
        public void Clamp_CapsAtHalfSecond()
        {
            Assert.AreEqual(0.1f, RestaurantRunner.Clamp(0.1f));
            Assert.AreEqual(0.5f, RestaurantRunner.Clamp(5f));
        }
    }
}
