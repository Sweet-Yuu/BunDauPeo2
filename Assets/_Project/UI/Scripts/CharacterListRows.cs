using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Characters;

namespace BunDauPeo.UI
{
    public readonly struct CharacterListRow
    {
        public CharacterListRow(CharacterDefinition character, int stars, long fragments, int nextCost, bool isMax, bool canStarUp, CombatStats now, CombatStats next,
            int level = 1)
        {
            Character = character;
            Stars = stars;
            Fragments = fragments;
            NextCost = nextCost;
            IsMax = isMax;
            CanStarUp = canStarUp;
            Now = now;
            Next = next;
            Level = level;
        }

        public CharacterDefinition Character { get; }
        public int Stars { get; }
        public long Fragments { get; }
        public int NextCost { get; }
        public bool IsMax { get; }
        public bool CanStarUp { get; }
        // Chỉ số hiện tại, đã gồm cấp nhân vật khi Build có truyền dịch vụ cấp.
        public CombatStats Now { get; }
        // Chỉ số sau khi nâng sao (cùng cấp); bằng Now khi đã tối đa.
        public CombatStats Next { get; }
        // Cấp hiệu lực (min cấp đã lưu, cấp người chơi).
        public int Level { get; }
    }

    // Hàm thuần của màn Nhân vật: view chỉ vẽ kết quả.
    public static class CharacterListRows
    {
        // Theo thứ tự sở hữu. Không truyền levels/config thì mọi nhân vật Lv1 và chỉ số chỉ theo sao (như trước khi có cấp).
        public static IReadOnlyList<CharacterListRow> Build(Roster roster, StarService stars, CharacterLevelService levels = null, LevelConfig config = null, StarConfig starConfig = null) =>
            roster.GetOwned().Select(c => RowOf(c, roster, stars, levels, config, starConfig)).ToList();

        private static CharacterListRow RowOf(CharacterDefinition character, Roster roster, StarService stars, CharacterLevelService levels, LevelConfig config, StarConfig starConfig)
        {
            var id = character.Id;
            var starUps = stars.GetStarUps(id);
            var level = levels?.GetLevel(id) ?? 1;
            CombatStats StatsAt(int ups) => levels != null && config != null && starConfig != null
                ? GrowthStats.Get(character, ups, level, starConfig.StatPercentPerStar, config.StatPercentPerLevel)
                : stars.GetCombatStats(character, ups);
            var now = StatsAt(starUps);
            var isMax = stars.GetStars(id) >= StarConfig.MaxStars;
            var cost = stars.GetNextCost(id);
            var fragments = roster.GetFragments(id);
            var next = isMax ? now : StatsAt(starUps + 1);
            return new CharacterListRow(character, stars.GetStars(id), fragments, cost, isMax, !isMax && cost > 0 && fragments >= cost, now, next, level);
        }
    }
}
