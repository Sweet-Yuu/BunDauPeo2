using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Characters;

namespace BunDauPeo.UI
{
    public readonly struct CharacterListRow
    {
        public CharacterListRow(CharacterDefinition character, int stars, long fragments, int nextCost, bool isMax, bool canStarUp, CombatStats now, CombatStats next)
        {
            Character = character;
            Stars = stars;
            Fragments = fragments;
            NextCost = nextCost;
            IsMax = isMax;
            CanStarUp = canStarUp;
            Now = now;
            Next = next;
        }

        public CharacterDefinition Character { get; }
        public int Stars { get; }
        public long Fragments { get; }
        public int NextCost { get; }
        public bool IsMax { get; }
        public bool CanStarUp { get; }
        public CombatStats Now { get; }
        // Chỉ số sau khi nâng; bằng Now khi đã tối đa.
        public CombatStats Next { get; }
    }

    // Hàm thuần của màn Nhân vật: view chỉ vẽ kết quả.
    public static class CharacterListRows
    {
        // Theo thứ tự sở hữu.
        public static IReadOnlyList<CharacterListRow> Build(Roster roster, StarService stars) =>
            roster.GetOwned().Select(c => RowOf(c, roster, stars)).ToList();

        private static CharacterListRow RowOf(CharacterDefinition character, Roster roster, StarService stars)
        {
            var id = character.Id;
            var starUps = stars.GetStarUps(id);
            var now = stars.GetCombatStats(character, starUps);
            var isMax = stars.GetStars(id) >= StarConfig.MaxStars;
            var cost = stars.GetNextCost(id);
            var fragments = roster.GetFragments(id);
            var next = isMax ? now : stars.GetCombatStats(character, starUps + 1);
            return new CharacterListRow(character, stars.GetStars(id), fragments, cost, isMax, !isMax && cost > 0 && fragments >= cost, now, next);
        }
    }
}
