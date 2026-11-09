using System;

namespace Hoang.Remake
{
    [Serializable]
    public struct FacilityStats
    {
        public float ProductionSpeedBonus;
        public float RevenueBonus;
        public float CustomerPatienceBonus;
        public float IngredientCostReduction;
        public int CleanlinessBonus;

        public static FacilityStats operator +(FacilityStats a, FacilityStats b) => new()
        {
            ProductionSpeedBonus = a.ProductionSpeedBonus + b.ProductionSpeedBonus,
            RevenueBonus = a.RevenueBonus + b.RevenueBonus,
            CustomerPatienceBonus = a.CustomerPatienceBonus + b.CustomerPatienceBonus,
            IngredientCostReduction = a.IngredientCostReduction + b.IngredientCostReduction,
            CleanlinessBonus = (int)Math.Min(int.MaxValue, (long)a.CleanlinessBonus + b.CleanlinessBonus),
        };
    }
}
