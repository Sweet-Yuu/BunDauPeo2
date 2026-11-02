using System;
using System.Collections.Generic;
using BunDauPeo.Characters;

namespace BunDauPeo.Combat
{
    public readonly struct FormationSlot
    {
        public FormationSlot(CharacterDefinition character, BattlePosition position, int indexInPosition, int slotIndex)
        {
            Character = character;
            Position = position;
            IndexInPosition = indexInPosition;
            SlotIndex = slotIndex;
        }

        public CharacterDefinition Character { get; }
        public BattlePosition Position { get; }
        public int IndexInPosition { get; }
        // Số thứ tự chung của phe ta (Front 0-1, Mid 2-3, Back 4): thứ tự lượt và mục tiêu mặc định dựa vào số này.
        public int SlotIndex { get; }
    }

    public enum TeamEntryStatus { Empty, NeedsConfirm, Ready, TooMany }

    // Hàm thuần của đội: không lưu gì trong save (spec Combat 4.1).
    public static class BattleFormation
    {
        public const int FrontSlots = 2, MidSlots = 2, BackSlots = 1;

        // Thứ tự thử khi hàng ưu tiên đầy: hàng gần nhất trước.
        private static readonly BattlePosition[][] Fallback =
        {
            new[] { BattlePosition.Front, BattlePosition.Mid, BattlePosition.Back },
            new[] { BattlePosition.Mid, BattlePosition.Front, BattlePosition.Back },
            new[] { BattlePosition.Back, BattlePosition.Mid, BattlePosition.Front },
        };

        // Unset về Front: StartBattle đã chặn nhân vật lỗi, nhưng view hiển thị đội không được ném.
        public static BattlePosition PreferredPosition(CharacterClass c) => c switch
        {
            CharacterClass.Support or CharacterClass.Marksman => BattlePosition.Mid,
            CharacterClass.Mage => BattlePosition.Back,
            _ => BattlePosition.Front,
        };

        public static IReadOnlyList<FormationSlot> Assign(IReadOnlyList<CharacterDefinition> team)
        {
            var used = new int[3];
            var slots = new List<FormationSlot>();
            for (var i = 0; i < team.Count && i < Roster.MaxBattleTeamSize; i++)
            {
                foreach (var position in Fallback[(int)PreferredPosition(team[i].Class)])
                {
                    if (used[(int)position] >= Capacity(position)) continue;
                    slots.Add(new FormationSlot(team[i], position, used[(int)position], FirstSlotIndex(position) + used[(int)position]));
                    used[(int)position]++;
                    break;
                }
            }

            return slots;
        }

        public static TeamEntryStatus GetEntryStatus(int teamSize)
        {
            if (teamSize <= 0) return TeamEntryStatus.Empty;
            if (teamSize < Roster.MaxBattleTeamSize) return TeamEntryStatus.NeedsConfirm;
            return teamSize == Roster.MaxBattleTeamSize ? TeamEntryStatus.Ready : TeamEntryStatus.TooMany;
        }

        private static int Capacity(BattlePosition position) => position switch
        {
            BattlePosition.Front => FrontSlots,
            BattlePosition.Mid => MidSlots,
            _ => BackSlots,
        };

        private static int FirstSlotIndex(BattlePosition position) => position switch
        {
            BattlePosition.Front => 0,
            BattlePosition.Mid => FrontSlots,
            _ => FrontSlots + MidSlots,
        };
    }
}
