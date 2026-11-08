using System;
using System.Collections.Generic;
using UnityEngine;

namespace BunDauPeo.Characters
{
    // Số khởi điểm theo spec nâng sao 5.2; chỉnh trong Inspector, không cần sửa code.
    [CreateAssetMenu(menuName = "BunDauPeo/Characters/Star Config")]
    public class StarConfig : ScriptableObject
    {
        public const int MaxStars = 5;
        private const int CostCount = MaxStars - 1;

        // Giá lên 2★, 3★, 4★, 5★: phần tử i là giá để lên (i + 2)★.
        [SerializeField] private int[] _costs = { 2, 4, 6, 8 };
        // % chỉ số gốc cộng thêm cho mỗi sao đã nâng (HP, Attack, Defense).
        [SerializeField] private int _statPercentPerStar = 10;

        public int StatPercentPerStar => _statPercentPerStar;

        public int CostToReach(int targetStars)
        {
            if (targetStars < 2 || targetStars > MaxStars)
                throw new ArgumentOutOfRangeException(nameof(targetStars), targetStars, "Target stars must be 2..5");
            return _costs[targetStars - 2];
        }

        // Mỗi lỗi bắt đầu bằng tên field ("Costs: ...").
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (_costs == null || _costs.Length != CostCount)
            {
                errors.Add($"Costs: must have {CostCount} elements");
            }
            else
            {
                for (var i = 0; i < _costs.Length; i++)
                    if (_costs[i] < 1) errors.Add($"Costs: element {i} (to {i + 2} stars) must be >= 1");
            }

            if (_statPercentPerStar < 0) errors.Add("StatPercentPerStar: must be >= 0");
            return errors;
        }

        private void OnValidate()
        {
            foreach (var error in Validate()) Debug.LogWarning($"{name} - {error}", this);
        }
    }
}
