using System;

namespace BunDauPeo.Characters
{
    // Gộp % sao và % cấp vào chỉ số gốc (spec cấp nhân vật 3.1); Speed giữ gốc.
    public static class GrowthStats
    {
        public static CombatStats Get(CharacterDefinition character, int starUps, int level,
            int statPercentPerStar, int statPercentPerLevel)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            var stats = character.CombatStats;
            var percent = 100L + (long)statPercentPerStar * Math.Max(starUps, 0)
                               + (long)statPercentPerLevel * (Math.Max(level, 1) - 1);
            stats.HP = Scale(stats.HP, percent);
            stats.Attack = Scale(stats.Attack, percent);
            stats.Defense = Scale(stats.Defense, percent);
            return stats;
        }

        private static int Scale(int baseValue, long percent) => (int)((baseValue * percent + 50) / 100);
    }
}
