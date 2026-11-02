using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Combat
{
    // Giá trị số được lưu trong asset: chỉ thêm ở cuối.
    public enum StageKind { Battle = 0, Story = 1 }

    // Màn Story không có đợt, không tốn thể lực, không thưởng; tên là chữ giữ chỗ cho cốt truyện.
    [CreateAssetMenu(menuName = "BunDauPeo/Combat/Stage")]
    public class StageDefinition : GameDefinition
    {
        [SerializeField] private string _displayName;
        [SerializeField] private StageKind _kind;
        [SerializeField] private WaveDefinition[] _waves = Array.Empty<WaveDefinition>();
        [SerializeField] private int _staminaCost;
        [SerializeField] private CurrencyAmount[] _firstClearReward = Array.Empty<CurrencyAmount>();
        [SerializeField] private CurrencyAmount[] _repeatReward = Array.Empty<CurrencyAmount>();

        public string DisplayName => _displayName;
        public StageKind Kind => _kind;
        public IReadOnlyList<WaveDefinition> Waves => _waves;
        public int StaminaCost => _staminaCost;
        public IReadOnlyList<CurrencyAmount> FirstClearReward => _firstClearReward;
        public IReadOnlyList<CurrencyAmount> RepeatReward => _repeatReward;

        // Mỗi lỗi bắt đầu bằng tên field ("Waves[0].Enemies[1]: ...", "FirstClearReward[1]: ...").
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) errors.Add("Id: must not be blank");

            if (_kind == StageKind.Battle)
            {
                if (_waves.Length == 0) errors.Add("Waves: a Battle needs at least one wave");
                for (var i = 0; i < _waves.Length; i++) CheckWave(errors, i, _waves[i]);
                if (_staminaCost < 1) errors.Add("StaminaCost: a Battle needs >= 1");
                if (_firstClearReward.Length == 0) errors.Add("FirstClearReward: a Battle needs a first-clear reward");
            }
            else
            {
                if (_waves.Length != 0) errors.Add("Waves: a Story has no waves");
                if (_staminaCost != 0) errors.Add("StaminaCost: a Story must be 0");
                if (_firstClearReward.Length != 0) errors.Add("FirstClearReward: a Story has no reward");
                if (_repeatReward.Length != 0) errors.Add("RepeatReward: a Story has no reward");
            }

            CheckRewardLines(errors, "FirstClearReward", _firstClearReward);
            CheckRewardLines(errors, "RepeatReward", _repeatReward);
            return errors;
        }

        private static void CheckWave(List<string> errors, int index, WaveDefinition wave)
        {
            var count = wave.Enemies.Count;
            if (count < 1 || count > WaveDefinition.MaxEnemies)
                errors.Add($"Waves[{index}]: must have 1..{WaveDefinition.MaxEnemies} enemies, found {count}");

            for (var j = 0; j < count; j++)
            {
                var enemy = wave.Enemies[j];
                var prefix = $"Waves[{index}].Enemies[{j}]";
                if (enemy == null) errors.Add($"{prefix}: missing reference");
                else foreach (var error in enemy.Validate()) errors.Add($"{prefix}: {error}");
            }
        }

        // Coin chỉ đến từ quán (game-design, mục Tiền tệ).
        private static void CheckRewardLines(List<string> errors, string field, CurrencyAmount[] lines)
        {
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Amount <= 0) errors.Add($"{field}[{i}]: Amount must be > 0");
                if (lines[i].Type == CurrencyType.Coin) errors.Add($"{field}[{i}]: Coin is not allowed");
            }
        }

        private void OnValidate()
        {
            foreach (var error in Validate()) Debug.LogWarning($"{name} - {error}", this);
        }
    }
}
