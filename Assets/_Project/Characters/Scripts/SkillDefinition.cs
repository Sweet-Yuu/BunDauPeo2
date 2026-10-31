using UnityEngine;

namespace BunDauPeo.Characters
{
    [CreateAssetMenu(menuName = "BunDauPeo/Characters/Skill")]
    public class SkillDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private SkillKind _kind;
        [SerializeField] private SkillTarget _target;
        [SerializeField] private SkillEffect _effect;
        [SerializeField] private float _power = 1f;
        [Tooltip("Dùng khi Kind = Skill.")]
        [SerializeField] private int _cooldown;
        [Tooltip("Dùng khi Kind = Ultimate.")]
        [SerializeField] private int _energyCost;
        [SerializeField] private string _animationTrigger;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public SkillKind Kind => _kind;
        public SkillTarget Target => _target;
        public SkillEffect Effect => _effect;
        public float Power => _power;
        public int Cooldown => _cooldown;
        public int EnergyCost => _energyCost;
        public string AnimationTrigger => _animationTrigger;
    }
}
