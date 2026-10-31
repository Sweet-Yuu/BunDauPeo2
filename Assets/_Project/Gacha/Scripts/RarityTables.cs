using System;
using BunDauPeo.Characters;

namespace BunDauPeo.Gacha
{
    [Serializable]
    public struct RarityWeights
    {
        public float R, SR, SSR;

        public float Get(Rarity rarity) => rarity switch
        {
            Rarity.R => R,
            Rarity.SR => SR,
            Rarity.SSR => SSR,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null),
        };
    }

    [Serializable]
    public struct RarityCounts
    {
        public int R, SR, SSR;

        public int Get(Rarity rarity) => rarity switch
        {
            Rarity.R => R,
            Rarity.SR => SR,
            Rarity.SSR => SSR,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null),
        };
    }
}
