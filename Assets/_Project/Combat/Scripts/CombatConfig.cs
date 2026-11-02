using System;
using System.Collections.Generic;
using UnityEngine;

namespace BunDauPeo.Combat
{
    // Giá trị số được dùng làm chỉ số mảng và lưu: chỉ thêm ở cuối.
    public enum BattlePosition { Front = 0, Mid = 1, Back = 2 }

    // Số khởi điểm theo spec Combat 3.4; chỉnh trong Inspector, không cần sửa code.
    [CreateAssetMenu(menuName = "BunDauPeo/Combat/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        [Header("Nhắm mục tiêu")]
        [SerializeField] private float _frontWeight = 3f;
        [SerializeField] private float _midWeight = 2f;
        [SerializeField] private float _backWeight = 1f;

        [Header("Năng lượng")]
        [SerializeField] private int _maxEnergy = 100;
        [SerializeField] private int _energyOnBasic = 25;
        [SerializeField] private int _energyOnSkill = 10;
        [SerializeField] private int _energyOnHit = 15;

        [Header("Giới hạn")]
        // Cả trận; quá thì thua, chặn trận không bao giờ kết thúc vì hồi máu liên tục.
        [SerializeField] private int _maxRounds = 60;

        [Header("AUTO")]
        [SerializeField] private float _autoHealBelow = 0.7f;

        [Header("Thể lực")]
        [SerializeField] private int _maxStamina = 60;
        // Một điểm mỗi khoảng này; là mẫu số của StaminaService.
        [SerializeField] private int _staminaRegenSeconds = 300;

        public float FrontWeight => _frontWeight;
        public float MidWeight => _midWeight;
        public float BackWeight => _backWeight;
        public int MaxEnergy => _maxEnergy;
        public int EnergyOnBasic => _energyOnBasic;
        public int EnergyOnSkill => _energyOnSkill;
        public int EnergyOnHit => _energyOnHit;
        public int MaxRounds => _maxRounds;
        public float AutoHealBelow => _autoHealBelow;
        public int MaxStamina => _maxStamina;
        public int StaminaRegenSeconds => _staminaRegenSeconds;

        public float Weight(BattlePosition position) => position switch
        {
            BattlePosition.Front => _frontWeight,
            BattlePosition.Mid => _midWeight,
            BattlePosition.Back => _backWeight,
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, null),
        };

        // Mỗi lỗi bắt đầu bằng tên field ("FrontWeight: ...", "MaxStamina: ...").
        public List<string> Validate()
        {
            var errors = new List<string>();

            // Viết dạng !(x > 0) để NaN cũng bị bắt.
            if (!(_frontWeight > 0f)) errors.Add("FrontWeight: must be > 0");
            if (!(_midWeight > 0f)) errors.Add("MidWeight: must be > 0");
            if (!(_backWeight > 0f)) errors.Add("BackWeight: must be > 0");

            if (_maxEnergy < 1) errors.Add("MaxEnergy: must be >= 1");
            if (_energyOnBasic < 0) errors.Add("EnergyOnBasic: must be >= 0");
            if (_energyOnSkill < 0) errors.Add("EnergyOnSkill: must be >= 0");
            if (_energyOnHit < 0) errors.Add("EnergyOnHit: must be >= 0");

            if (_maxRounds < 1) errors.Add("MaxRounds: must be >= 1");
            if (!(_autoHealBelow > 0f && _autoHealBelow <= 1f)) errors.Add("AutoHealBelow: must be in (0, 1]");

            if (_maxStamina < 1) errors.Add("MaxStamina: must be >= 1");
            if (_staminaRegenSeconds <= 0) errors.Add("StaminaRegenSeconds: must be > 0");

            return errors;
        }

        private void OnValidate()
        {
            foreach (var error in Validate()) Debug.LogWarning($"{name} - {error}", this);
        }
    }
}
