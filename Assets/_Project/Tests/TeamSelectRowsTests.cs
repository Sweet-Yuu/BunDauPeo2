using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class TeamSelectRowsTests
    {
        private static Roster RosterWith(params CharacterDefinition[] owned)
        {
            var roster = new Roster(new PlayerSave(), TestData.Catalog(owned));
            foreach (var hero in owned) roster.Add(hero.Id);
            return roster;
        }

        [Test]
        public void Build_OwnedInOrder_MarksTeamMembers()
        {
            var roster = RosterWith(CombatTestData.Hero("a"), CombatTestData.Hero("b"), CombatTestData.Hero("c"));
            roster.AddToTeam("c");
            roster.AddToTeam("a");

            var rows = TeamSelectRows.Build(roster);

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, rows.Select(r => r.Character.Id).ToArray(), "ownership order, not team order");
            CollectionAssert.AreEqual(new[] { true, false, true }, rows.Select(r => r.InTeam).ToArray());
            Assert.IsTrue(rows.All(r => r.StaffRole == null));
        }

        [Test]
        public void Build_StaffMember_HasRole()
        {
            var roster = RosterWith(CombatTestData.Hero("a"), CombatTestData.Hero("b"), CombatTestData.Hero("c"));
            roster.AssignStaff("b", StaffRole.Cook);
            roster.AssignStaff("c", StaffRole.Cashier);
            roster.AddToTeam("a");

            var rows = TeamSelectRows.Build(roster);

            Assert.IsNull(rows[0].StaffRole);
            Assert.AreEqual(StaffRole.Cook, rows[1].StaffRole);
            Assert.AreEqual(StaffRole.Cashier, rows[2].StaffRole);
            Assert.IsFalse(rows[1].InTeam);

            roster.AddToTeam("b");
            var after = TeamSelectRows.Build(roster);
            Assert.IsNull(after[1].StaffRole, "joining the team takes the character out of the restaurant");
            Assert.IsTrue(after[1].InTeam);
        }
    }
}
