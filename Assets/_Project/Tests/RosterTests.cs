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

        [Test]
        public void AddFragments_Owned_AddsAndRaisesChanged()
        {
            var raised = 0;
            _roster.Changed += () => raised++;
            Assert.IsTrue(_roster.AddFragments("c0", 2));
            Assert.AreEqual(2, _roster.GetFragments("c0"));
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void AddFragments_Accumulates()
        {
            _roster.AddFragments("c0", 2);
            _roster.AddFragments("c0", 3);
            Assert.AreEqual(5, _roster.GetFragments("c0"));
            Assert.AreEqual(1, _save.Fragments.Count);
        }

        [Test]
        public void AddFragments_NotOwned_ReturnsFalse_AndChangesNothing()
        {
            var roster = new Roster(new PlayerSave(), _catalog);
            var raised = 0;
            roster.Changed += () => raised++;
            Assert.IsFalse(roster.AddFragments("c0", 1));
            Assert.AreEqual(0, roster.GetFragments("c0"));
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void AddFragments_UnknownId_ReturnsFalse()
        {
            var save = new PlayerSave();
            save.OwnedCharacterIds.Add("ghost");
            LogAssert.Expect(LogType.Warning, new Regex("ghost"));
            var roster = new Roster(save, _catalog);
            Assert.IsFalse(roster.AddFragments("ghost", 1));
            Assert.IsEmpty(save.Fragments);
        }

        [Test]
        public void AddFragments_NonPositive_ReturnsFalse_WithoutChanged()
        {
            var raised = 0;
            _roster.Changed += () => raised++;
            Assert.IsFalse(_roster.AddFragments("c0", 0));
            Assert.IsFalse(_roster.AddFragments("c0", -1));
            Assert.AreEqual(0, _roster.GetFragments("c0"));
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void AddFragments_Overflow_ClampsAtMax()
        {
            _roster.AddFragments("c0", long.MaxValue);
            _roster.AddFragments("c0", 5);
            Assert.AreEqual(long.MaxValue, _roster.GetFragments("c0"));
        }

        [Test]
        public void SpendFragments_Enough_SubtractsAndRaisesChanged()
        {
            _roster.AddFragments("c0", 5);
            var raised = 0;
            _roster.Changed += () => raised++;
            Assert.IsTrue(_roster.SpendFragments("c0", 2));
            Assert.AreEqual(3, _roster.GetFragments("c0"));
            Assert.AreEqual(1, raised);

            // Biên: tiêu đúng hết số đang có vẫn được.
            Assert.IsTrue(_roster.SpendFragments("c0", 3));
            Assert.AreEqual(0, _roster.GetFragments("c0"));
        }

        [Test]
        public void SpendFragments_InvalidOrShort_ChangesNothing()
        {
            _roster.AddFragments("c0", 5);
            var raised = 0;
            _roster.Changed += () => raised++;
            Assert.IsFalse(_roster.SpendFragments("c0", 0));
            Assert.IsFalse(_roster.SpendFragments("c0", -1));
            Assert.IsFalse(_roster.SpendFragments(null, 1));
            Assert.IsFalse(_roster.SpendFragments("c0", 6));
            Assert.AreEqual(5, _roster.GetFragments("c0"));
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void GetFragments_NoEntry_ReturnsZero()
        {
            Assert.AreEqual(0, _roster.GetFragments("c0"));
            Assert.AreEqual(0, _roster.GetFragments(null));
        }

        [Test]
        public void Fragments_AreStoredInSave()
        {
            _roster.AddFragments("c0", 4);
            Assert.AreEqual(4, new Roster(_save, _catalog).GetFragments("c0"));
        }

        // N1 (D-005): ảnh chụp đội chụp trước Reset; sau Reset save là game mới, nên Hủy không được ghi lại id không còn sở hữu.
        [Test]
        public void RestoreTeam_AfterSaveOverwritten_DropsIdsNoLongerOwned()
        {
            _roster.AddToTeam("c0");
            _roster.AddToTeam("c1");
            _roster.AssignStaff("c2", StaffRole.Cook);
            _roster.AssignStaff("c3", StaffRole.Cook);
            var snapshot = _roster.SnapshotTeam();

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new PlayerSave()), _save);
            _roster.Add("c0");
            _roster.Add("c2");
            _roster.RestoreTeam(snapshot);

            CollectionAssert.AreEqual(new[] { "c0" }, _save.BattleTeam, "c1 is no longer owned");
            CollectionAssert.AreEqual(new[] { "c2" }, _save.Staff.Select(s => s.CharacterId).ToArray(), "c3 is no longer owned");
            Assert.IsFalse(_roster.IsOwned("c1"));
        }

        [Test]
        public void RestoreTeam_NothingChanged_DoesNotRaiseChanged()
        {
            _roster.AddToTeam("c0");
            _roster.AssignStaff("c1", StaffRole.Cook);
            var snapshot = _roster.SnapshotTeam();
            var raised = 0;
            _roster.Changed += () => raised++;

            _roster.RestoreTeam(snapshot);
            Assert.AreEqual(0, raised, "same team and staff as the snapshot");

            _roster.AddToTeam("c2");
            raised = 0;
            _roster.RestoreTeam(snapshot);
            Assert.AreEqual(1, raised, "a real change is announced exactly once");
            CollectionAssert.AreEqual(new[] { "c0" }, _save.BattleTeam);
        }
    }
}
