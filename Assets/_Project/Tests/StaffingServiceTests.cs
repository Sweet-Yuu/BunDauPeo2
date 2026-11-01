using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class StaffingServiceTests
    {
        private PlayerSave _save;
        private Roster _roster;
        private RestaurantConfig _config;
        private StaffingService _staffing;

        [SetUp]
        public void SetUp()
        {
            _save = new PlayerSave();
            // "x" có trong catalog nhưng người chơi chưa sở hữu.
            var characters = new[] { TestData.Character("c1"), TestData.Character("c2"), TestData.Character("c3"), TestData.Character("x") };
            _roster = new Roster(_save, TestData.Catalog(characters));
            _roster.Add("c1");
            _roster.Add("c2");
            _roster.Add("c3");
            _config = RestaurantTestData.Config(RestaurantTestData.UpgradeCatalog(
                RestaurantTestData.Upgrade("up_cook_slots", UpgradeEffect.CookSlots, RestaurantTestData.Level(2, 1, 1))));
            // Hồ sơ dựng lại mỗi lần gọi, nên mua ô mới thì lần TryAssign kế tiếp thấy ngay.
            _staffing = new StaffingService(_roster, () => RestaurantProfile.Build(_config, _save, _roster));
        }

        private static string[] Ids(System.Collections.Generic.IReadOnlyList<CharacterDefinition> list)
        {
            var ids = new string[list.Count];
            for (var i = 0; i < ids.Length; i++) ids[i] = list[i].Id;
            return ids;
        }

        [Test]
        public void TryAssign_FreeRole_Success()
        {
            Assert.AreEqual(StaffAssignResult.Success, _staffing.TryAssign("c1", StaffRole.Cook));
            CollectionAssert.AreEqual(new[] { "c1" }, Ids(_roster.GetStaff(StaffRole.Cook)));

            Assert.IsTrue(_staffing.Remove("c1"));
            CollectionAssert.IsEmpty(_roster.GetStaff(StaffRole.Cook));
            Assert.IsFalse(_staffing.Remove("c1"));
        }

        [Test]
        public void TryAssign_RoleFull_ReturnsRoleFull_AndChangesNothing()
        {
            _staffing.TryAssign("c1", StaffRole.Cook);

            Assert.AreEqual(StaffAssignResult.RoleFull, _staffing.TryAssign("c2", StaffRole.Cook));

            CollectionAssert.AreEqual(new[] { "c1" }, Ids(_roster.GetStaff(StaffRole.Cook)));
        }

        [Test]
        public void TryAssign_AfterSlotsIncrease_Succeeds()
        {
            _staffing.TryAssign("c1", StaffRole.Cook);
            Assert.AreEqual(StaffAssignResult.RoleFull, _staffing.TryAssign("c2", StaffRole.Cook));

            _save.Upgrades.Add(new UpgradeLevel { UpgradeId = "up_cook_slots", Level = 1 });

            Assert.AreEqual(StaffAssignResult.Success, _staffing.TryAssign("c2", StaffRole.Cook));
            CollectionAssert.AreEqual(new[] { "c1", "c2" }, Ids(_roster.GetStaff(StaffRole.Cook)));
            Assert.AreEqual(StaffAssignResult.RoleFull, _staffing.TryAssign("c3", StaffRole.Cook));
        }

        [Test]
        public void TryAssign_NotOwned()
        {
            Assert.AreEqual(StaffAssignResult.NotOwned, _staffing.TryAssign("x", StaffRole.Cook));
            CollectionAssert.IsEmpty(_roster.GetStaff(StaffRole.Cook));
        }

        [Test]
        public void TryAssign_CharacterInBattleTeam_LeavesTeam()
        {
            _roster.AddToTeam("c1");

            Assert.AreEqual(StaffAssignResult.Success, _staffing.TryAssign("c1", StaffRole.Server));

            CollectionAssert.IsEmpty(_roster.GetBattleTeam());
            CollectionAssert.AreEqual(new[] { "c1" }, Ids(_roster.GetStaff(StaffRole.Server)));
        }

        [Test]
        public void TryAssign_CharacterAtOtherRole_MovesRole()
        {
            _staffing.TryAssign("c1", StaffRole.Server);

            Assert.AreEqual(StaffAssignResult.Success, _staffing.TryAssign("c1", StaffRole.Cook));

            CollectionAssert.IsEmpty(_roster.GetStaff(StaffRole.Server));
            CollectionAssert.AreEqual(new[] { "c1" }, Ids(_roster.GetStaff(StaffRole.Cook)));
        }

        [Test]
        public void TryAssign_SameRoleSameCharacter_NotRoleFull()
        {
            _staffing.TryAssign("c1", StaffRole.Cook);

            Assert.AreEqual(StaffAssignResult.Success, _staffing.TryAssign("c1", StaffRole.Cook));
            CollectionAssert.AreEqual(new[] { "c1" }, Ids(_roster.GetStaff(StaffRole.Cook)));
        }

        [Test]
        public void TryReplace_SwapsCharacter_KeepsRoleCount()
        {
            _staffing.TryAssign("c1", StaffRole.Cook);

            Assert.AreEqual(StaffAssignResult.Success, _staffing.TryReplace("c1", "c2", StaffRole.Cook));

            CollectionAssert.AreEqual(new[] { "c2" }, Ids(_roster.GetStaff(StaffRole.Cook)));
        }

        [Test]
        public void TryReplace_NewNotOwned_ChangesNothing()
        {
            _staffing.TryAssign("c1", StaffRole.Cook);

            Assert.AreEqual(StaffAssignResult.NotOwned, _staffing.TryReplace("c1", "x", StaffRole.Cook));

            CollectionAssert.AreEqual(new[] { "c1" }, Ids(_roster.GetStaff(StaffRole.Cook)));
        }
    }
}
