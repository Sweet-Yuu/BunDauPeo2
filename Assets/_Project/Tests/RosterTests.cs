using System;
using System.Linq;
using System.Text.RegularExpressions;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.Tests
{
    public class RosterTests
    {
        private CharacterCatalog _catalog;
        private PlayerSave _save;
        private Roster _roster;

        [SetUp]
        public void SetUp()
        {
            _catalog = TestData.Catalog(Enumerable.Range(0, 7).Select(i => TestData.Character("c" + i)).ToArray());
            _save = new PlayerSave();
            _roster = new Roster(_save, _catalog);
            for (var i = 0; i < 7; i++) _roster.Add("c" + i);
        }

        [Test]
        public void Add_Duplicate_ReturnsFalse() => Assert.IsFalse(_roster.Add("c0"));

        [Test]
        public void Add_NotInCatalog_ReturnsFalse() => Assert.IsFalse(_roster.Add("ghost"));

        [Test]
        public void AddToTeam_NotOwned_ReturnsFalse()
        {
            var roster = new Roster(new PlayerSave(), _catalog);
            Assert.IsFalse(roster.AddToTeam("c0"));
        }

        [Test]
        public void AddToTeam_RemovesFromStaff()
        {
            _roster.AssignStaff("c0", StaffRole.Cook);
            Assert.IsTrue(_roster.AddToTeam("c0"));
            CollectionAssert.IsEmpty(_roster.GetStaff(StaffRole.Cook));
            Assert.AreEqual("c0", _roster.GetBattleTeam()[0].Id);
        }

        [Test]
        public void AssignStaff_RemovesFromTeam()
        {
            _roster.AddToTeam("c0");
            Assert.IsTrue(_roster.AssignStaff("c0", StaffRole.Cook));
            CollectionAssert.IsEmpty(_roster.GetBattleTeam());
        }

        [Test]
        public void AddToTeam_Full_RejectedAndStaysAtRestaurant()
        {
            for (var i = 1; i <= 5; i++) _roster.AddToTeam("c" + i);
            _roster.AssignStaff("c0", StaffRole.Cook);
            Assert.IsFalse(_roster.AddToTeam("c0"));
            Assert.AreEqual(5, _roster.GetBattleTeam().Count);
            Assert.AreEqual("c0", _roster.GetStaff(StaffRole.Cook)[0].Id);
        }

        [Test]
        public void AddToTeam_AlreadyInTeam_ReturnsFalse()
        {
            _roster.AddToTeam("c0");
            Assert.IsFalse(_roster.AddToTeam("c0"));
            Assert.AreEqual(1, _roster.GetBattleTeam().Count);
        }

        [Test]
        public void AssignStaff_NewRole_ReplacesOldRole()
        {
            _roster.AssignStaff("c0", StaffRole.Cook);
            Assert.IsTrue(_roster.AssignStaff("c0", StaffRole.Cashier));
            CollectionAssert.IsEmpty(_roster.GetStaff(StaffRole.Cook));
            Assert.AreEqual(1, _roster.GetStaff(StaffRole.Cashier).Count);
            Assert.AreEqual(1, _save.Staff.Count);
        }

        [Test]
        public void AssignStaff_SameRole_ReturnsFalse_WithoutChanged()
        {
            _roster.AssignStaff("c0", StaffRole.Cook);
            var raised = 0;
            _roster.Changed += () => raised++;
            Assert.IsFalse(_roster.AssignStaff("c0", StaffRole.Cook));
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void UnknownId_SkippedInLists_ButKeptInSave()
        {
            var save = new PlayerSave();
            save.OwnedCharacterIds.Add("ghost");
            save.BattleTeam.Add("ghost");
            var roster = new Roster(save, _catalog);
            CollectionAssert.IsEmpty(roster.GetOwned());
            CollectionAssert.IsEmpty(roster.GetBattleTeam());
            Assert.Contains("ghost", save.OwnedCharacterIds);
            Assert.Contains("ghost", save.BattleTeam);
        }

        [Test]
        public void OwnedGhost_CannotJoinTeam()
        {
            var save = new PlayerSave();
            save.OwnedCharacterIds.Add("ghost");
            Assert.IsFalse(new Roster(save, _catalog).AddToTeam("ghost"));
        }

        [Test]
        public void Changed_HandlerThrows_ChangeStillApplies_AndLaterHandlersRun()
        {
            var raised = 0;
            _roster.Changed += () => throw new InvalidOperationException("boom");
            _roster.Changed += () => raised++;
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            Assert.IsTrue(_roster.AddToTeam("c0"));
            Assert.AreEqual(1, _roster.GetBattleTeam().Count);
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Changed_RaisedOnRealChangeOnly()
        {
            var raised = 0;
            _roster.Changed += () => raised++;
            _roster.AddToTeam("c0");
            _roster.RemoveFromTeam("c0");
            _roster.RemoveFromTeam("c0");
            Assert.AreEqual(2, raised);
        }
    }
}
