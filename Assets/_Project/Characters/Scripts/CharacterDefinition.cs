using System.Collections.Generic;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Characters
{
    [CreateAssetMenu(menuName = "BunDauPeo/Characters/Character")]
    public class CharacterDefinition : GameDefinition
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Rarity _rarity;
        [SerializeField] private Sprite _portrait;
        [SerializeField] private Sprite _battleSprite;
        [SerializeField] private CombatStats _combatStats;
        [SerializeField] private RestaurantStats _restaurantStats;
        [SerializeField] private SkillDefinition _basicAttack;
        [SerializeField] private SkillDefinition _skill;
        [SerializeField] private SkillDefinition _ultimate;

        public string DisplayName => _displayName;
        public Rarity Rarity => _rarity;
        public Sprite Portrait => _portrait;
        public Sprite BattleSprite => _battleSprite;
        public CombatStats CombatStats => _combatStats;
        public RestaurantStats RestaurantStats => _restaurantStats;
        public SkillDefinition BasicAttack => _basicAttack;
        public SkillDefinition Skill => _skill;
        public SkillDefinition Ultimate => _ultimate;

        public List<string> Validate()
        {
            var errors = new List<string>();
            CheckSkill(errors, "BasicAttack", _basicAttack, SkillKind.Basic);
            CheckSkill(errors, "Skill", _skill, SkillKind.Skill);
            CheckSkill(errors, "Ultimate", _ultimate, SkillKind.Ultimate);
            return errors;
        }

        private static void CheckSkill(List<string> errors, string slot, SkillDefinition skill, SkillKind expected)
        {
            if (skill == null) errors.Add($"{slot}: missing skill");
            else if (skill.Kind != expected) errors.Add($"{slot}: must be Kind {expected}, found {skill.Kind}");
        }

        private void OnValidate()
        {
            foreach (var error in Validate()) Debug.LogWarning($"{name} - {error}", this);
        }
    }
}
