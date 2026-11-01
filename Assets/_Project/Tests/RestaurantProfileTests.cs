using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class RestaurantProfileTests
    {
        private static RestaurantConfig ConfigWith(params UpgradeDefinition[] upgrades) =>
            RestaurantTestData.Config(RestaurantTestData.UpgradeCatalog(upgrades));

        private static Roster RosterOf(PlayerSave save, params CharacterDefinition[] owned)
        {
            var roster = new Roster(save, TestData.Catalog(owned));
            foreach (var character in owned) roster.Add(character.Id);
            return roster;
        }

        private static void SetLevel(PlayerSave save, string id, int level) =>
            save.Upgrades.Add(new UpgradeLevel { UpgradeId = id, Level = level });

        [Test]
        public void Build_NoUpgrades_UsesConfigBases()
        {
            var save = new PlayerSave();
            var profile = RestaurantProfile.Build(ConfigWith(), save, RosterOf(save));

            Assert.AreEqual(3, profile.Tables);
            Assert.AreEqual(1, profile.CookSlots);
            Assert.AreEqual(1, profile.ServeSlots);
            Assert.AreEqual(1, profile.CashierSlots);
            Assert.AreEqual(1f, profile.ArrivalIntervalMultiplier);
            Assert.AreEqual(1f, profile.PriceMultiplier);
            Assert.AreEqual(0, profile.Workers(StaffRole.Cook).Count);
        }

        [Test]
        public void Build_UpgradeLevels_UseLevelValues()
        {
            var config = ConfigWith(
                RestaurantTestData.Upgrade("up_tables", UpgradeEffect.Tables,
                    RestaurantTestData.Level(4, 1, 1), RestaurantTestData.Level(5, 1, 1), RestaurantTestData.Level(6, 1, 1)),
                RestaurantTestData.Upgrade("up_arrivals", UpgradeEffect.ArrivalInterval,
                    RestaurantTestData.Level(0.9f, 1, 1), RestaurantTestData.Level(0.81f, 1, 1)),
                RestaurantTestData.Upgrade("up_prices", UpgradeEffect.DishPrice,
                    RestaurantTestData.Level(1.1f, 1, 1), RestaurantTestData.Level(1.2f, 1, 1), RestaurantTestData.Level(1.3f, 1, 1)),
                RestaurantTestData.Upgrade("up_cook_slots", UpgradeEffect.CookSlots,
                    RestaurantTestData.Level(2, 1, 1), RestaurantTestData.Level(3, 1, 1)));
            var save = new PlayerSave();
            SetLevel(save, "up_tables", 2);
            SetLevel(save, "up_arrivals", 1);
            SetLevel(save, "up_prices", 3);
            SetLevel(save, "up_cook_slots", 0);

            var profile = RestaurantProfile.Build(config, save, RosterOf(save));

            Assert.AreEqual(5, profile.Tables);
            Assert.AreEqual(0.9f, profile.ArrivalIntervalMultiplier);
            Assert.AreEqual(1.3f, profile.PriceMultiplier);
            Assert.AreEqual(1, profile.CookSlots);
        }

        [Test]
        public void Build_LevelBeyondSo_UsesHighestLevel_KeepsSave()
        {
            var config = ConfigWith(RestaurantTestData.Upgrade("up_tables", UpgradeEffect.Tables,
                RestaurantTestData.Level(4, 1, 1), RestaurantTestData.Level(5, 1, 1), RestaurantTestData.Level(6, 1, 1)));
            var save = new PlayerSave();
            SetLevel(save, "up_tables", 9);

            var profile = RestaurantProfile.Build(config, save, RosterOf(save));

            Assert.AreEqual(6, profile.Tables);
            Assert.AreEqual(9, save.Upgrades[0].Level);
        }

        [Test]
        public void Build_UnknownUpgradeId_Ignored()
        {
            var save = new PlayerSave();
            SetLevel(save, "ghost", 3);

            var profile = RestaurantProfile.Build(ConfigWith(), save, RosterOf(save));

            Assert.AreEqual(3, profile.Tables);
            Assert.AreEqual(1f, profile.PriceMultiplier);
        }

        [Test]
        public void Build_TrimsStaffToSlots_WithoutRemovingFromSave()
        {
            var cooks = new[] { RestaurantTestData.Worker("c1", 10, 0, 0), RestaurantTestData.Worker("c2", 20, 0, 0), RestaurantTestData.Worker("c3", 30, 0, 0) };
            var save = new PlayerSave();
            var roster = RosterOf(save, cooks);
            foreach (var cook in cooks) roster.AssignStaff(cook.Id, StaffRole.Cook);

            var profile = RestaurantProfile.Build(ConfigWith(), save, roster);

            Assert.AreEqual(1, profile.Workers(StaffRole.Cook).Count);
            Assert.AreEqual("c1", profile.Workers(StaffRole.Cook)[0].CharacterId);
            Assert.AreEqual(3, roster.GetStaff(StaffRole.Cook).Count);

            // Hai ô thì giữ hai người đầu, không phải luôn một.
            var wider = ConfigWith(RestaurantTestData.Upgrade("up_cook_slots", UpgradeEffect.CookSlots, RestaurantTestData.Level(2, 1, 1)));
            SetLevel(save, "up_cook_slots", 1);
            var widerProfile = RestaurantProfile.Build(wider, save, roster);
            Assert.AreEqual(2, widerProfile.Workers(StaffRole.Cook).Count);
            Assert.AreEqual("c2", widerProfile.Workers(StaffRole.Cook)[1].CharacterId);
        }

        [Test]
        public void Build_StaffStatsComeFromRestaurantStats()
        {
            // Cùng bộ chỉ số 30/20/10 ở ba người, nên gán nhầm vai sẽ ra số khác.
            var cook = RestaurantTestData.Worker("c", 30, 20, 10);
            var server = RestaurantTestData.Worker("s", 30, 20, 10);
            var cashier = RestaurantTestData.Worker("h", 30, 20, 10);
            var save = new PlayerSave();
            var roster = RosterOf(save, cook, server, cashier);
            roster.AssignStaff("c", StaffRole.Cook);
            roster.AssignStaff("s", StaffRole.Server);
            roster.AssignStaff("h", StaffRole.Cashier);

            var profile = RestaurantProfile.Build(ConfigWith(), save, roster);

            Assert.AreEqual(30, profile.Workers(StaffRole.Cook)[0].Stat);
            Assert.AreEqual(20, profile.Workers(StaffRole.Server)[0].Stat);
            Assert.AreEqual(10, profile.Workers(StaffRole.Cashier)[0].Stat);
        }

        [Test]
        public void Build_UnknownCharacterIdSkipped()
        {
            var cook = RestaurantTestData.Worker("c", 30, 0, 0);
            var save = new PlayerSave();
            var roster = RosterOf(save, cook);
            // "ghost" đứng trước nhưng không trong catalog: không được chiếm ô duy nhất.
            save.Staff.Add(new StaffAssignment { CharacterId = "ghost", Role = StaffRole.Cook });
            roster.AssignStaff("c", StaffRole.Cook);

            var profile = RestaurantProfile.Build(ConfigWith(), save, roster);

            Assert.AreEqual(1, profile.Workers(StaffRole.Cook).Count);
            Assert.AreEqual("c", profile.Workers(StaffRole.Cook)[0].CharacterId);
        }

        [Test]
        public void WithWorkers_ReplacesOnlyThatRole()
        {
            var cook = RestaurantTestData.Worker("c", 30, 0, 0);
            var server = RestaurantTestData.Worker("s", 0, 20, 0);
            var save = new PlayerSave();
            var roster = RosterOf(save, cook, server);
            roster.AssignStaff("c", StaffRole.Cook);
            roster.AssignStaff("s", StaffRole.Server);
            var profile = RestaurantProfile.Build(ConfigWith(), save, roster);

            var changed = profile.WithWorkers(StaffRole.Server, new[] { new WorkerInfo("x", 99), new WorkerInfo("y", 98) });

            Assert.AreEqual(2, changed.Workers(StaffRole.Server).Count);
            Assert.AreEqual(99, changed.Workers(StaffRole.Server)[0].Stat);
            Assert.AreEqual("c", changed.Workers(StaffRole.Cook)[0].CharacterId);
            Assert.AreEqual(0, changed.Workers(StaffRole.Cashier).Count);
            Assert.AreEqual(profile.Tables, changed.Tables);
            Assert.AreEqual("s", profile.Workers(StaffRole.Server)[0].CharacterId, "bản gốc không bị đổi");
        }
    }
}
