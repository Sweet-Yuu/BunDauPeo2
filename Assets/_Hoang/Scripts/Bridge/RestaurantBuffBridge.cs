using System;
using System.Globalization;
using BunDauPeo.Core;
using UnityEngine;

namespace Hoang.Remake
{
    public static class RestaurantBuffBridge
    {
        private static SkillTreeManager _skills;
        private static FacilityManager _facilities;
        public static event Action OnBuffsChanged;

        public static void Initialize(SkillTreeManager skills, FacilityManager facilities)
        {
            Shutdown();
            _skills = skills;
            _facilities = facilities;
            if (_skills != null) _skills.Changed += NotifyChanged;
            if (_facilities != null) _facilities.Changed += NotifyChanged;
            NotifyChanged();
        }

        public static void Shutdown()
        {
            if (_skills != null) _skills.Changed -= NotifyChanged;
            if (_facilities != null) _facilities.Changed -= NotifyChanged;
            _skills = null;
            _facilities = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Shutdown();
            OnBuffsChanged = null;
        }

        public static float GetTotalCookingSpeedMultiplier() => Multiplier(
            _skills?.GetTotalProductionSpeedBonus() ?? 0, _facilities?.CurrentTotalStats.ProductionSpeedBonus ?? 0);
        public static float GetTotalRevenueMultiplier() => Multiplier(
            _skills?.GetTotalRevenueBonus() ?? 0, _facilities?.CurrentTotalStats.RevenueBonus ?? 0);
        public static float GetCustomerPatienceMultiplier() => Multiplier(
            _skills?.GetTotalCustomerPatienceBonus() ?? 0, _facilities?.CurrentTotalStats.CustomerPatienceBonus ?? 0);
        public static float GetTotalIngredientCostReduction() => (float)Math.Clamp(
            (double)(_skills?.GetTotalCostReduction() ?? 0) + (_facilities?.CurrentTotalStats.IngredientCostReduction ?? 0), 0, 0.8);
        public static int GetTotalCleanlinessBonus() => (int)Math.Min(int.MaxValue,
            (long)(_skills?.GetTotalCleanlinessBonus() ?? 0) + (_facilities?.CurrentTotalStats.CleanlinessBonus ?? 0));
        public static int GetTotalPowerCapacity() => _facilities?.TotalPowerCapacity ?? 0;
        public static int GetTotalPowerUsed() => _facilities?.CurrentPowerUsed ?? 0;
        public static int GetRemainingPower() => Math.Max(0, GetTotalPowerCapacity() - GetTotalPowerUsed());
        public static bool IsPowerSufficient() => _facilities?.IsPowerSufficient ?? true;

        public static string GetFormattedBuffSummary() => string.Format(CultureInfo.InvariantCulture,
            "Cooking speed: x{0:0.00}\nRevenue: x{1:0.00}\nCustomer patience: x{2:0.00}\nIngredient savings: {3:0}%\nCleanliness: +{4}\nPower: {5} / {6} W",
            GetTotalCookingSpeedMultiplier(), GetTotalRevenueMultiplier(), GetCustomerPatienceMultiplier(),
            GetTotalIngredientCostReduction() * 100, GetTotalCleanlinessBonus(), GetTotalPowerUsed(), GetTotalPowerCapacity());

        private static void NotifyChanged() => OnBuffsChanged.SafeInvoke();

        private static float Multiplier(float skillBonus, float facilityBonus) =>
            (float)Math.Clamp(1.0 + skillBonus + facilityBonus, 1, float.MaxValue);
    }
}
