using System;
using System.Collections.Generic;
using UnityEngine;

namespace BunDauPeo.Characters
{
    // Số khởi điểm theo spec cấp nhân vật 5.3 (user chốt tính lại cuối tháng 12); chỉnh trong Inspector, không cần sửa code.
    [CreateAssetMenu(menuName = "BunDauPeo/Characters/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField] private int _maxPlayerLevel = 20;
        // EXP lên cấp L→L+1 = ExpBase + ExpPerLevel × L.
        [SerializeField] private int _expBase = 100;
        [SerializeField] private int _expPerLevel = 20;
        // % chỉ số gốc cộng thêm cho mỗi cấp trên cấp 1 (HP, Attack, Defense).
        [SerializeField] private int _statPercentPerLevel = 2;
        // Giá nâng nhân vật lên cấp đích T = T × mỗi khoản.
        [SerializeField] private int _potionPerTargetLevel = 2;
        [SerializeField] private int _coinPerTargetLevel = 100;
        // Thưởng lên cấp người chơi: Coin × cấp mới, và số thuốc cố định.
        [SerializeField] private int _levelUpRewardCoinPerLevel = 200;
        [SerializeField] private int _levelUpRewardPotion = 5;
        [SerializeField] private int _restaurantPotionChancePercent = 3;
        [SerializeField] private int _afkCoinPerPotion = 500;

        public int MaxPlayerLevel => _maxPlayerLevel;
        public int ExpBase => _expBase;
        public int ExpPerLevel => _expPerLevel;
        public int StatPercentPerLevel => _statPercentPerLevel;
        public int PotionPerTargetLevel => _potionPerTargetLevel;
        public int CoinPerTargetLevel => _coinPerTargetLevel;
        public int LevelUpRewardCoinPerLevel => _levelUpRewardCoinPerLevel;
        public int LevelUpRewardPotion => _levelUpRewardPotion;
        public int RestaurantPotionChancePercent => _restaurantPotionChancePercent;
        public int AfkCoinPerPotion => _afkCoinPerPotion;

        public long ExpToNext(int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level), level, "Level must be >= 1");
            return (long)_expBase + (long)_expPerLevel * level;
        }

        // Mỗi lỗi bắt đầu bằng tên field ("MaxPlayerLevel: ...").
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (_maxPlayerLevel < 1) errors.Add("MaxPlayerLevel: must be >= 1");
            if (_expBase < 1) errors.Add("ExpBase: must be >= 1");
            if (_expPerLevel < 0) errors.Add("ExpPerLevel: must be >= 0");
            if (_statPercentPerLevel < 0) errors.Add("StatPercentPerLevel: must be >= 0");
            if (_potionPerTargetLevel < 0) errors.Add("PotionPerTargetLevel: must be >= 0");
            if (_coinPerTargetLevel < 0) errors.Add("CoinPerTargetLevel: must be >= 0");
            if (_levelUpRewardCoinPerLevel < 0) errors.Add("LevelUpRewardCoinPerLevel: must be >= 0");
            if (_levelUpRewardPotion < 0) errors.Add("LevelUpRewardPotion: must be >= 0");
            if (_restaurantPotionChancePercent < 0 || _restaurantPotionChancePercent > 100)
                errors.Add("RestaurantPotionChancePercent: must be 0..100");
            if (_afkCoinPerPotion < 1) errors.Add("AfkCoinPerPotion: must be >= 1");
            return errors;
        }

        private void OnValidate()
        {
            foreach (var error in Validate()) Debug.LogWarning($"{name} - {error}", this);
        }
    }
}
