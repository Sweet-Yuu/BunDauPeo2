#nullable disable
using System;
using System.Collections.Generic;

namespace BunDauPeo.Rules
{
    // Dữ liệu cân bằng server dùng (spec con mục 5). Rarity: 0 R, 1 SR, 2 SSR; Kind: giá trị StageKind (0 Battle, 1 Story).
    [Serializable]
    public class CharacterData
    {
        public string Id = "";
        public int Rarity;
        public int BaseStars;
    }

    [Serializable]
    public class BannerData
    {
        public string Id = "";
        public long SingleCostGem;
        public long TenCostGem;
        public float WeightR;
        public float WeightSR;
        public float WeightSSR;
        public int SsrPityThreshold;
        public bool GuaranteeSrInTen;
        public long DupFragmentsR;
        public long DupFragmentsSR;
        public long DupFragmentsSSR;
        public List<string> CharacterIds = new();
    }

    [Serializable]
    public class RewardData
    {
        public long Gem;
        public long Material;
    }

    [Serializable]
    public class StageData
    {
        public string Id = "";
        public int Kind;
        public int StaminaCost;
        public RewardData FirstClear = new();
        public RewardData Repeat = new();
    }

    [Serializable]
    public class StaminaData
    {
        public int Max;
        public int RegenSeconds;
    }

    [Serializable]
    public class NewGameData
    {
        public long StartingGem;
        public List<string> StarterCharacterIds = new();
    }

    [Serializable]
    public class GameData
    {
        public int DataVersion = 1;
        public List<CharacterData> Characters = new();
        public List<BannerData> Banners = new();
        public List<int> StarCosts = new();
        public int MaxStars;
        public StaminaData Stamina = new();
        public int MaxTeamSize;
        public List<StageData> Stages = new();
        public NewGameData NewGame = new();
    }
}
