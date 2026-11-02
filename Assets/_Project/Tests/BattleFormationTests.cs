using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class BattleFormationTests
    {
        private static CharacterDefinition[] Team(params CharacterClass[] classes) =>
            classes.Select((c, i) => CombatTestData.Hero($"h{i}", c)).ToArray();

        private static CharacterDefinition[] Team(CharacterClass cls, int count) =>
            Team(Enumerable.Repeat(cls, count).ToArray());

        private static int[] Slots(IReadOnlyList<FormationSlot> slots) => slots.Select(s => s.SlotIndex).ToArray();

        [Test]
        public void PreferredPosition_EachClass()
        {
            Assert.AreEqual(BattlePosition.Front, BattleFormation.PreferredPosition(CharacterClass.Tank));
            Assert.AreEqual(BattlePosition.Front, BattleFormation.PreferredPosition(CharacterClass.Fighter));
            Assert.AreEqual(BattlePosition.Mid, BattleFormation.PreferredPosition(CharacterClass.Support));
            Assert.AreEqual(BattlePosition.Mid, BattleFormation.PreferredPosition(CharacterClass.Marksman));
            Assert.AreEqual(BattlePosition.Back, BattleFormation.PreferredPosition(CharacterClass.Mage));
        }

        [Test]
        public void PreferredPosition_Unset_IsFront() =>
            Assert.AreEqual(BattlePosition.Front, BattleFormation.PreferredPosition(CharacterClass.Unset));

        [Test]
        public void SlotConstants_SumToMaxTeamSize() =>
            Assert.AreEqual(Roster.MaxBattleTeamSize, BattleFormation.FrontSlots + BattleFormation.MidSlots + BattleFormation.BackSlots);

        [Test]
        public void Assign_FiveClasses_SlotIndexes()
        {
            var slots = BattleFormation.Assign(Team(CharacterClass.Tank, CharacterClass.Fighter, CharacterClass.Support,
                CharacterClass.Marksman, CharacterClass.Mage));

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, Slots(slots));
            CollectionAssert.AreEqual(
                new[] { BattlePosition.Front, BattlePosition.Front, BattlePosition.Mid, BattlePosition.Mid, BattlePosition.Back },
                slots.Select(s => s.Position).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1, 0, 1, 0 }, slots.Select(s => s.IndexInPosition).ToArray());
        }

        [Test]
        public void Assign_FrontFull_OverflowsToMidThenBack()
        {
            var slots = BattleFormation.Assign(Team(CharacterClass.Fighter, 5));
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, Slots(slots));
            CollectionAssert.AreEqual(
                new[] { BattlePosition.Front, BattlePosition.Front, BattlePosition.Mid, BattlePosition.Mid, BattlePosition.Back },
                slots.Select(s => s.Position).ToArray());
        }

        [Test]
        public void Assign_MidFull_OverflowsToFrontThenBack()
        {
            var withFrontTaken = BattleFormation.Assign(Team(CharacterClass.Tank, CharacterClass.Tank,
                CharacterClass.Support, CharacterClass.Support, CharacterClass.Support));
            Assert.AreEqual(4, withFrontTaken[4].SlotIndex, "third Support: Front is full, so Back");
            Assert.AreEqual(BattlePosition.Back, withFrontTaken[4].Position);

            var frontOpen = BattleFormation.Assign(Team(CharacterClass.Support, 3));
            Assert.AreEqual(0, frontOpen[2].SlotIndex, "third Support with Front open: Front before Back");
            Assert.AreEqual(BattlePosition.Front, frontOpen[2].Position);
        }

        [Test]
        public void Assign_BackFull_OverflowsToMidThenFront()
        {
            var slots = BattleFormation.Assign(Team(CharacterClass.Mage, 5));
            CollectionAssert.AreEqual(new[] { 4, 2, 3, 0, 1 }, Slots(slots));
        }

        [Test]
        public void Assign_FiveFighters_Is221()
        {
            var slots = BattleFormation.Assign(Team(CharacterClass.Fighter, 5));
            Assert.AreEqual(2, slots.Count(s => s.Position == BattlePosition.Front));
            Assert.AreEqual(2, slots.Count(s => s.Position == BattlePosition.Mid));
            Assert.AreEqual(1, slots.Count(s => s.Position == BattlePosition.Back));
        }

        [Test]
        public void Assign_TeamsOfOneToFour()
        {
            for (var size = 1; size <= 4; size++)
            {
                var slots = BattleFormation.Assign(Team(CharacterClass.Fighter, size));
                CollectionAssert.AreEqual(Enumerable.Range(0, size).ToArray(), Slots(slots), $"size {size}");
            }
        }

        [Test]
        public void Assign_KeepsTeamOrder()
        {
            var team = Team(CharacterClass.Mage, CharacterClass.Tank, CharacterClass.Support);
            var slots = BattleFormation.Assign(team);
            CollectionAssert.AreEqual(team, slots.Select(s => s.Character).ToArray());
            CollectionAssert.AreEqual(new[] { 4, 0, 2 }, Slots(slots));
        }

        [Test]
        public void Assign_MoreThanFive_KeepsFirstFive()
        {
            var team = Team(CharacterClass.Fighter, 6);
            var slots = BattleFormation.Assign(team);
            CollectionAssert.AreEqual(team.Take(5).ToArray(), slots.Select(s => s.Character).ToArray());
        }

        [Test]
        public void GetEntryStatus_Boundaries()
        {
            Assert.AreEqual(TeamEntryStatus.Empty, BattleFormation.GetEntryStatus(0));
            Assert.AreEqual(TeamEntryStatus.NeedsConfirm, BattleFormation.GetEntryStatus(1));
            Assert.AreEqual(TeamEntryStatus.NeedsConfirm, BattleFormation.GetEntryStatus(4));
            Assert.AreEqual(TeamEntryStatus.Ready, BattleFormation.GetEntryStatus(5));
            Assert.AreEqual(TeamEntryStatus.TooMany, BattleFormation.GetEntryStatus(6));
        }
    }
}
