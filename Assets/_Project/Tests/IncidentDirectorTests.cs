using System.Linq;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Cấu hình mẫu: sự cố sinh sau 60–120 giây, kịp ≤ 8, trễ ≤ 20; ba sự cố trọng số 1 theo thứ tự Cook, Server, Cashier.
    // Bộ RNG của director tách khỏi bộ RNG của mô phỏng để đếm đúng số lần gọi của riêng nó.
    public class IncidentDirectorTests
    {
        private sealed class Fixture
        {
            public IncidentDirector Director;
            public RestaurantSimulation Simulation;
            public RestaurantQuality Quality;
            public PlayerSave Save;
            public ScriptedRandom Random;
            public readonly bool[] HasStaff = { true, true, true };
        }

        // Mô phỏng cho khách đầu đến lúc 7,5 giây và nấu 5 giây; chặn bếp thì khách ngồi bàn mà không được nấu.
        private static Fixture Make(ScriptedRandom random = null, RestaurantConfig config = null)
        {
            var f = new Fixture { Save = new PlayerSave(), Random = random ?? new ScriptedRandom(0.0) };
            config ??= RestaurantTestData.Config();
            f.Quality = new RestaurantQuality(f.Save, config, new FakeClock());
            f.Simulation = new RestaurantSimulation(config, new ScriptedRandom(0.99, 0.0, 0.0, 0.0));
            f.Simulation.SetProfile(RestaurantTestData.Profile(config, new[] { 0 }, new[] { 0 }, new[] { 0 }));
            f.Director = new IncidentDirector(config, f.Random, f.Simulation, f.Quality, role => f.HasStaff[(int)role]);
            return f;
        }

        // Có mặt, đồng hồ u = 0 (60 giây) rồi sinh sự cố đầu tiên (mặc định chọn Cook).
        private static Fixture Spawned(ScriptedRandom random = null)
        {
            var f = Make(random);
            f.Director.SetPresent(true);
            f.Director.Update(60f);
            Assert.IsTrue(f.Director.Active.HasValue, "sự cố phải sinh sau 60 giây");
            return f;
        }

        private static CustomerState Customer(Fixture f) => f.Simulation.Customers.Single().State;

        [Test]
        public void NoSpawn_WhenNotPresent()
        {
            var f = Make();

            f.Director.Update(300f);

            Assert.IsFalse(f.Director.Active.HasValue);
            Assert.AreEqual(0, f.Random.Calls);
        }

        [Test]
        public void Spawns_AfterRandomInterval()
        {
            foreach (var (u, seconds) in new[] { (0.0, 60f), (0.5, 90f), (1.0, 120f) })
            {
                var f = Make(new ScriptedRandom(0.0, u));
                f.Director.SetPresent(true);

                f.Director.Update(seconds - 0.5f);
                Assert.IsFalse(f.Director.Active.HasValue, $"u = {u}: chưa tới hạn");

                f.Director.Update(0.5f);
                Assert.IsTrue(f.Director.Active.HasValue, $"u = {u}: đúng {seconds} giây");
            }
        }

        [Test]
        public void EmptyRole_StillSpawns()
        {
            // u = 0 cho đồng hồ, 0,4 chọn sự cố thứ hai (Server): 0,4 × 3 = 1,2.
            var f = Make(new ScriptedRandom(0.0, 0.0, 0.4));
            f.HasStaff[(int)StaffRole.Server] = false;
            f.Director.SetPresent(true);

            f.Director.Update(60f);

            Assert.AreEqual(StaffRole.Server, f.Director.Active.Value.Role);
            Assert.IsFalse(f.Director.Active.Value.CanResolve);
        }

        [Test]
        public void OnlyOneIncidentAtATime()
        {
            var f = Spawned();
            var first = f.Director.Active.Value.Definition;
            Assert.AreEqual(2, f.Random.Calls);

            f.Director.Update(10f);
            f.Director.Update(9f);

            Assert.AreSame(first, f.Director.Active.Value.Definition);
            Assert.AreEqual(19f, f.Director.Active.Value.Elapsed, 1e-4f);
            // Đang có sự cố thì không đặt đồng hồ mới và không chọn thêm.
            Assert.AreEqual(2, f.Random.Calls);
        }

        [Test]
        public void SpawnNotAllowed_NoSpawn_ThenSpawnsImmediatelyWhenReallowed()
        {
            var f = Make();
            f.Director.SetPresent(true);
            f.Director.SetSpawnAllowed(false);

            f.Director.Update(200f);
            Assert.IsFalse(f.Director.Active.HasValue);

            f.Director.SetSpawnAllowed(true);
            f.Director.Update(0.1f);
            Assert.IsTrue(f.Director.Active.HasValue);
        }

        [Test]
        public void ActiveIncident_KeepsCounting_WhenSpawnNotAllowed()
        {
            var f = Spawned();
            f.Director.SetSpawnAllowed(false);

            f.Director.Update(5f);
            Assert.AreEqual(5f, f.Director.Active.Value.Elapsed, 1e-4f);

            f.Director.Update(16f);
            Assert.IsFalse(f.Director.Active.HasValue);
            CollectionAssert.AreEqual(new[] { 0 }, f.Save.IncidentScores);
        }

        [Test]
        public void Resolve_FastBoundary()
        {
            foreach (var (elapsed, expectedScore) in new[] { (7.99f, 2), (8f, 2), (8.01f, 1) })
            {
                var f = Spawned();
                f.Director.Update(elapsed);

                Assert.IsTrue(f.Director.Resolve(), $"{elapsed} giây");

                CollectionAssert.AreEqual(new[] { expectedScore }, f.Save.IncidentScores, $"{elapsed} giây");
                Assert.IsFalse(f.Director.Active.HasValue, $"{elapsed} giây");
            }
        }

        [Test]
        public void Resolve_LateBoundary()
        {
            var atLimit = Spawned();
            atLimit.Director.Update(20f);
            Assert.IsTrue(atLimit.Director.Active.HasValue, "đúng 20,0 giây vẫn còn xử lý được");
            Assert.IsTrue(atLimit.Director.Resolve());
            CollectionAssert.AreEqual(new[] { 1 }, atLimit.Save.IncidentScores);

            var pastLimit = Spawned();
            pastLimit.Director.Update(20.01f);
            Assert.IsFalse(pastLimit.Director.Active.HasValue, "quá 20 giây tự bỏ lỡ trước khi Resolve");
            Assert.IsFalse(pastLimit.Director.Resolve());
            CollectionAssert.AreEqual(new[] { 0 }, pastLimit.Save.IncidentScores);
        }

        [Test]
        public void Missed_AutoAtLateSeconds_RecordsZero()
        {
            var f = Spawned();

            f.Director.Update(21f);

            Assert.IsFalse(f.Director.Active.HasValue);
            CollectionAssert.AreEqual(new[] { 0 }, f.Save.IncidentScores);
        }

        [Test]
        public void Resolve_EmptyRole_ReturnsFalse_NoScoreNoStateChange()
        {
            var f = Make();
            f.HasStaff[(int)StaffRole.Cook] = false;
            f.Director.SetPresent(true);
            f.Director.Update(60f);
            f.Director.Update(3f);

            Assert.IsFalse(f.Director.Resolve());

            Assert.IsTrue(f.Director.Active.HasValue);
            Assert.AreEqual(3f, f.Director.Active.Value.Elapsed, 1e-4f);
            Assert.AreEqual(0, f.Save.IncidentScores.Count);
            // Trạm vẫn đóng: khách ngồi bàn mà bếp không nhận việc.
            f.Simulation.Tick(10f);
            Assert.AreEqual(CustomerState.WaitingForCook, Customer(f));
        }

        [Test]
        public void Resolve_AfterStaffAssignedInTime_ReturnsTrue()
        {
            var f = Make();
            f.HasStaff[(int)StaffRole.Cook] = false;
            f.Director.SetPresent(true);
            f.Director.Update(60f);
            f.Director.Update(5f);
            Assert.IsFalse(f.Director.Active.Value.CanResolve);

            f.HasStaff[(int)StaffRole.Cook] = true;

            Assert.IsTrue(f.Director.Active.Value.CanResolve);
            Assert.IsTrue(f.Director.Resolve());
            CollectionAssert.AreEqual(new[] { 2 }, f.Save.IncidentScores);
        }

        [Test]
        public void Resolve_StaffRemovedMidIncident_ReturnsFalse()
        {
            var f = Spawned();
            f.Director.Update(2f);
            f.HasStaff[(int)StaffRole.Cook] = false;

            Assert.IsFalse(f.Director.Active.Value.CanResolve);
            Assert.IsFalse(f.Director.Resolve());
            Assert.IsTrue(f.Director.Active.HasValue);
            Assert.AreEqual(0, f.Save.IncidentScores.Count);
        }

        [Test]
        public void StationBlocked_WhileActive_UnblockedAfterEnd()
        {
            var f = Spawned();

            f.Simulation.Tick(10f);
            Assert.AreEqual(CustomerState.WaitingForCook, Customer(f), "bếp đóng nên khách chưa được nấu");

            Assert.IsTrue(f.Director.Resolve());
            f.Simulation.Tick(0.2f);

            Assert.AreEqual(CustomerState.Cooking, Customer(f));
        }

        [Test]
        public void Cancel_OnLeavingOrBackground_NoScore_Unblocks()
        {
            var left = Spawned();
            left.Director.Update(3f);
            left.Director.SetPresent(false);
            Assert.IsFalse(left.Director.Active.HasValue);
            Assert.AreEqual(0, left.Save.IncidentScores.Count);
            left.Simulation.Tick(10f);
            Assert.AreEqual(CustomerState.Cooking, Customer(left), "rời màn thì mở lại trạm");

            var cancelled = Spawned();
            cancelled.Director.Cancel();
            Assert.IsFalse(cancelled.Director.Active.HasValue);
            Assert.AreEqual(0, cancelled.Save.IncidentScores.Count);
            cancelled.Simulation.Tick(10f);
            Assert.AreEqual(CustomerState.Cooking, Customer(cancelled));
        }

        // Tổng trọng số 5 (1, 3, 1): u × 5 rơi vào [0,1) Cook, [1,4) Server, [4,5) Cashier.
        [Test]
        public void Pick_IsWeighted()
        {
            var config = RestaurantTestData.Config(null,
                RestaurantTestData.Incident(StaffRole.Cook, 1), RestaurantTestData.Incident(StaffRole.Server, 3), RestaurantTestData.Incident(StaffRole.Cashier, 1));
            foreach (var (u, expected) in new[]
                     {
                         (0.1, StaffRole.Cook), (0.3, StaffRole.Server), (0.79, StaffRole.Server), (0.8, StaffRole.Cashier),
                     })
            {
                var f = Make(new ScriptedRandom(0.0, 0.0, u), config);
                f.Director.SetPresent(true);

                f.Director.Update(60f);

                Assert.AreEqual(expected, f.Director.Active.Value.Role, $"u = {u}");
            }
        }

        [Test]
        public void RngCallContract_OnePerTimerOnePerPick()
        {
            var f = Make();
            Assert.AreEqual(0, f.Random.Calls, "constructor không gọi RNG");

            f.Director.SetPresent(true);
            f.Director.Update(10f);
            Assert.AreEqual(1, f.Random.Calls, "một lần cho đồng hồ");

            f.Director.Update(50f);
            Assert.AreEqual(2, f.Random.Calls, "một lần chọn sự cố");

            f.Director.Update(3f);
            f.Director.Resolve();
            Assert.AreEqual(2, f.Random.Calls, "xử lý không gọi RNG");

            f.Director.Update(1f);
            Assert.AreEqual(3, f.Random.Calls, "sự cố xong thì đặt đồng hồ mới");
        }

        [Test]
        public void Outcome_IsRecordedInQuality()
        {
            var f = Make();
            f.Director.SetPresent(true);

            f.Director.Update(60f);
            f.Director.Resolve();
            f.Director.Update(60f);
            f.Director.Update(10f);
            f.Director.Resolve();
            f.Director.Update(60f);
            f.Director.Update(21f);

            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, f.Save.IncidentScores);
            Assert.AreEqual(50.0 * (2 + 1 + 0) / 2 / 10, f.Quality.IncidentPoints, 1e-9);
        }
    }
}
