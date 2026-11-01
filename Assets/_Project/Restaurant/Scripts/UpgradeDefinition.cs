using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Restaurant
{
    // Giá trị số được ghim vì SO lưu enum dưới dạng số; chỉ thêm ở cuối.
    public enum UpgradeEffect
    {
        Tables = 0,
        CookSlots = 1,
        ServeSlots = 2,
        CashierSlots = 3,
        ArrivalInterval = 4,
        DishPrice = 5,
    }

    // Value là giá trị tuyệt đối tại cấp đó: số bàn/ô cho loại đếm, hệ số nhân cho ArrivalInterval và DishPrice.
    [Serializable]
    public struct UpgradeLevelDef
    {
        public CurrencyAmount[] Cost;
        public float Value;
    }

    [CreateAssetMenu(menuName = "BunDauPeo/Restaurant/Upgrade")]
    public class UpgradeDefinition : GameDefinition
    {
        [SerializeField] private string _displayName = "Nâng cấp";
        [SerializeField] private Sprite _icon;
        [SerializeField] private UpgradeEffect _effect;
        [SerializeField] private UpgradeLevelDef[] _levels = Array.Empty<UpgradeLevelDef>();

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public UpgradeEffect Effect => _effect;
        public IReadOnlyList<UpgradeLevelDef> Levels => _levels;
        public int MaxLevel => _levels.Length;

        // Mỗi lỗi bắt đầu bằng tên field ("Levels[0].Cost: ...").
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) errors.Add("Id: must not be blank");
            if (_levels.Length == 0)
            {
                errors.Add("Levels: must have at least one level");
                return errors;
            }

            var isCount = _effect is UpgradeEffect.Tables or UpgradeEffect.CookSlots
                or UpgradeEffect.ServeSlots or UpgradeEffect.CashierSlots;
            for (var i = 0; i < _levels.Length; i++)
            {
                CheckCost(errors, $"Levels[{i}].Cost", _levels[i].Cost);
                var value = _levels[i].Value;
                if (!(value > 0f)) errors.Add($"Levels[{i}].Value: must be > 0");
                else if (isCount && value != Mathf.Floor(value)) errors.Add($"Levels[{i}].Value: {_effect} needs a whole number");
            }

            return errors;
        }

        private static void CheckCost(List<string> errors, string field, CurrencyAmount[] cost)
        {
            if (cost == null || cost.Length == 0)
            {
                errors.Add($"{field}: must not be empty");
                return;
            }

            var anyPositive = false;
            foreach (var line in cost)
            {
                if (line.Amount < 0) errors.Add($"{field}: {line.Type} amount must not be negative");
                else if (line.Amount > 0) anyPositive = true;
            }

            // Tổng 0 làm TrySpend thành công mà không trừ gì, tức nâng cấp miễn phí.
            if (!anyPositive) errors.Add($"{field}: total cost is 0");
        }

        private void OnValidate()
        {
            foreach (var error in Validate()) Debug.LogWarning($"{name} - {error}", this);
        }
    }
}
