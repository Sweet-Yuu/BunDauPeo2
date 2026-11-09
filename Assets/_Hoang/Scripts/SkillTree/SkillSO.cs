using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hoang.Remake
{
    [Serializable]
    public sealed class SkillPrerequisite
    {
        public SkillSO Skill;
        [Min(1)] public int MinimumLevel = 1;
    }

    [CreateAssetMenu(menuName = "Hoang/Restaurant/Skill")]
    public sealed class SkillSO : ScriptableObject
    {
        public string Id;
        public string SkillName;
        [TextArea] public string Description;
        public Sprite SkillIcon;
        [Min(1)] public int MaxLevel = 5;
        public bool IsExclusive;
        [Min(1)] public int NormalPointCost = 1;
        [Min(1)] public int SpecialPointCost = 1;
        public SkillPrerequisite[] Prerequisites = Array.Empty<SkillPrerequisite>();
        public Vector2 GraphPosition;
        [Min(0)] public float ProductionSpeedBonusPerLevel;
        [Min(0)] public float RevenueBonusPerLevel;
        [Min(0)] public float CustomerPatienceBonusPerLevel;
        [Min(0)] public float IngredientCostReductionPerLevel;
        [Min(0)] public int CleanlinessBonusPerLevel;

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) errors.Add("Id is required.");
            if (MaxLevel < 1) errors.Add("MaxLevel must be positive.");
            if (NormalPointCost < 1 || SpecialPointCost < 1) errors.Add("Point costs must be positive.");
            foreach (var bonus in new[] { ProductionSpeedBonusPerLevel, RevenueBonusPerLevel,
                         CustomerPatienceBonusPerLevel, IngredientCostReductionPerLevel })
                if (float.IsNaN(bonus) || float.IsInfinity(bonus) || bonus < 0)
                    errors.Add("Bonuses must be finite and non-negative.");
            if (CleanlinessBonusPerLevel < 0) errors.Add("Cleanliness bonus must be non-negative.");
            foreach (var prerequisite in Prerequisites ?? Array.Empty<SkillPrerequisite>())
                if (prerequisite == null || prerequisite.Skill == null || prerequisite.MinimumLevel < 1
                    || prerequisite.MinimumLevel > prerequisite.Skill.MaxLevel)
                    errors.Add("Prerequisites require a skill and a reachable minimum level.");
            return errors;
        }
    }
}
