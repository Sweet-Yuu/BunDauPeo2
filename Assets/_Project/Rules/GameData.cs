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
        public bool FreePullDaily;
    }

    [Serializable]
    public class RewardData
    {
        public long Gem;
        public long Material;
        // Thuốc EXP: server chỉ trả số lượng, máy cộng vào ví như Material. Dữ liệu cũ thiếu field = 0.
        public long ExpPotion;
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
        public int RewardCap = 999;   // trần khi nhận thể lực thưởng (vượt Max được)
    }

    // Counter: 0 Wins, 1 Pulls, 2 LoginWindow, 3 BattleStagesCleared, 4 GachaEver, 5 StarUpEver, 6 ClientFlag, 7 None (spec đợt 3 mục 6.3). Tab: 0 ngày, 1 tân thủ.
    [Serializable]
    public class MissionData
    {
        public string Id = "";
        public int Tab;
        public int Counter;
        public int Target;
        public bool Locked;
        public long RewardGem;
        public int RewardStamina;
        public int WindowStartHour;
        public int WindowEndHour;
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
        public List<MissionData> Missions = new();
    }
}
