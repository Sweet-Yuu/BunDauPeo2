using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class TeamSelectRowsTests
    {
        private PlayerSave _save;
        private StarService _stars;

        private Roster RosterWith(params CharacterDefinition[] owned)
        {
            _save = new PlayerSave();
            var catalog = TestData.Catalog(owned);
            var roster = new Roster(_save, catalog);
            foreach (var hero in owned) roster.Add(hero.Id);
            _stars = new StarService(_save, roster, catalog, ScriptableObject.CreateInstance<StarConfig>(), () => true);
            return roster;
        }

        [Test]
        public void Build_Stars_AreCurrentStars()
        {
            var roster = RosterWith(CombatTestData.Hero("a"));
            var start = TeamSelectRows.Build(roster, _stars)[0].Stars;
            _save.StarUps.Add(new StarUpEntry { CharacterId = "a", Count = 2 });

            Assert.AreEqual(start + 2, TeamSelectRows.Build(roster, _stars)[0].Stars);
        }

        [Test]
        public void Build_OwnedInOrder_MarksTeamMembers()
        {
            var roster = RosterWith(CombatTestData.Hero("a"), CombatTestData.Hero("b"), CombatTestData.Hero("c"));
            roster.AddToTeam("c");
            roster.AddToTeam("a");

            var rows = TeamSelectRows.Build(roster, _stars);

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

            var rows = TeamSelectRows.Build(roster, _stars);

            Assert.IsNull(rows[0].StaffRole);
            Assert.AreEqual(StaffRole.Cook, rows[1].StaffRole);
            Assert.AreEqual(StaffRole.Cashier, rows[2].StaffRole);
            Assert.IsFalse(rows[1].InTeam);

            roster.AddToTeam("b");
            var after = TeamSelectRows.Build(roster, _stars);
            Assert.IsNull(after[1].StaffRole, "joining the team takes the character out of the restaurant");
            Assert.IsTrue(after[1].InTeam);
        }
    }
}
