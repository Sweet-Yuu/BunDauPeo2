using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Combat
{
    // Không có Id: quái không được save tham chiếu. Không có ultimate và không có năng lượng.
    [CreateAssetMenu(menuName = "BunDauPeo/Combat/Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private string _displayNameEn;
        [SerializeField] private CombatStats _stats;
        [SerializeField] private SkillDefinition _basicAttack;
        // Có thể trống: quái chỉ đánh thường.
        [SerializeField] private SkillDefinition _skill;
        [SerializeField] private bool _isBoss;

        public string DisplayName => GameLanguage.Pick(_displayName, _displayNameEn);
        public CombatStats Stats => _stats;
        public SkillDefinition BasicAttack => _basicAttack;
        public SkillDefinition Skill => _skill;
        public bool IsBoss => _isBoss;

        // Mỗi lỗi bắt đầu bằng tên field ("Stats.HP: ...", "Skill.Cooldown: ...").
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (_stats.HP <= 0) errors.Add("Stats.HP: must be > 0");
            if (_stats.Attack <= 0) errors.Add("Stats.Attack: must be > 0");
            if (_stats.Defense < 0) errors.Add("Stats.Defense: must be >= 0");
            if (_stats.Speed <= 0) errors.Add("Stats.Speed: must be > 0");

            if (_basicAttack == null) errors.Add("BasicAttack: missing skill");
            else foreach (var error in CombatSkillRules.Validate(_basicAttack, SkillKind.Basic)) errors.Add($"BasicAttack.{error}");

            if (_skill != null)
            {
                foreach (var error in CombatSkillRules.Validate(_skill, SkillKind.Skill)) errors.Add($"Skill.{error}");
                if (_skill.Effect != SkillEffect.Damage) errors.Add("Skill.Effect: enemy skills must be Damage");
            }

            return errors;
        }

        private void OnValidate()
        {
            foreach (var error in Validate()) Debug.LogWarning($"{name} - {error}", this);
        }
    }
}
