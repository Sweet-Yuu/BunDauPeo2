using BunDauPeo.Characters;

namespace BunDauPeo.UI
{
    // Chỉ số trước → sau khi nâng một cấp nhân vật (tab Cấp).
    public readonly struct CharacterUpgradePreview
    {
        public bool HasNext { get; }
        public CombatStats Before { get; }
        public CombatStats After { get; }

        private CharacterUpgradePreview(bool hasNext, CombatStats before, CombatStats after)
        {
            HasNext = hasNext;
            Before = before;
            After = after;
        }

        // maxLevel = trần hiện tại (cấp người chơi); level >= maxLevel thì không còn bước kế.
        public static CharacterUpgradePreview Build(CharacterDefinition c, int starUps, int level, int maxLevel, LevelConfig lc, int statPercentPerStar)
        {
            // Config lỗi: chỉ số theo sao như CombatService, không có bước kế.
            if (lc.Validate().Count != 0)
            {
                var plain = GrowthStats.Get(c, starUps, 1, statPercentPerStar, 0);
                return new CharacterUpgradePreview(false, plain, plain);
            }

            var before = GrowthStats.Get(c, starUps, level, statPercentPerStar, lc.StatPercentPerLevel);
            if (level >= maxLevel) return new CharacterUpgradePreview(false, before, before);
            return new CharacterUpgradePreview(true, before, GrowthStats.Get(c, starUps, level + 1, statPercentPerStar, lc.StatPercentPerLevel));
        }
    }
}
