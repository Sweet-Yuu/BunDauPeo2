using System;
using System.Collections.Generic;

namespace Hoang.Remake
{
    [Serializable]
    public sealed class SkillSlotSaveEntry
    {
        public string SkillId;
        public string SkillName;
        public int CurrentLevel;
        public bool IsUnlocked;
        public int NormalPointsSpent;
        public int SpecialPointsSpent;
    }

    [Serializable]
    public sealed class SkillTreeSaveState
    {
        public int AvailablePoints = 20;
        public int AvailableSpecialPoints = 3;
        public List<SkillSlotSaveEntry> Slots = new();
    }
}
