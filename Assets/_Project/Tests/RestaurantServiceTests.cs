using System;
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

            var fixture = new Fixture { Save = save, Wallet = new Wallet(save), Roster = roster };
            fixture.Service = new RestaurantService(config ?? RestaurantTestData.Config(), save, fixture.Wallet, roster,
                random ?? new ScriptedRandom(0.99, 0.0, 0.0, 0.0), persist ?? (() => { fixture.Persists++; return true; }));
            return fixture;
        }

        private static void Run(RestaurantService service, float seconds, float dt = 0.1f)
        {
            var steps = Mathf.RoundToInt(seconds / dt);
            for (var i = 0; i < steps; i++) service.Update(dt);
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
        public void Clamp_CapsAtHalfSecond()
        {
            Assert.AreEqual(0.1f, RestaurantRunner.Clamp(0.1f));
            Assert.AreEqual(0.5f, RestaurantRunner.Clamp(5f));
        }
    }
}
