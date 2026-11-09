using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hoang.Remake
{
    public sealed class SkillSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private SkillSO _skill;
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        private SkillTreeManager _manager;
        private SkillTreeUI _owner;

        public SkillSO Definition => _skill;

        public void Configure(SkillSO skill, Button button)
        {
            _skill = skill;
            _button = button;
            _label = button.GetComponentInChildren<Text>(true);
            _button.onClick.RemoveListener(Upgrade);
            _button.onClick.AddListener(Upgrade);
            Refresh();
        }

        public void Bind(SkillTreeManager manager, SkillTreeUI owner)
        {
            _manager = manager;
            _owner = owner;
            Refresh();
        }

        public void Refresh()
        {
            if (_skill == null || _button == null || _label == null) return;
            var level = _manager?.GetLevel(_skill.Id) ?? 0;
            var unlocked = _manager?.IsUnlocked(_skill) ?? (_skill.Prerequisites == null || _skill.Prerequisites.Length == 0);
            var cost = _skill.IsExclusive ? $"{_skill.SpecialPointCost} special" : $"{_skill.NormalPointCost} point(s)";
            _label.text = $"{_skill.SkillName}\nLevel {level} / {_skill.MaxLevel}\n{(level >= _skill.MaxLevel ? "MAX LEVEL" : !unlocked ? "LOCKED" : cost)}";
            _label.fontSize = 21;
            _button.interactable = _manager != null;
            _button.GetComponent<Image>().color = level >= _skill.MaxLevel ? new Color(0.64f, 0.47f, 0.19f)
                : !unlocked ? new Color(0.43f, 0.46f, 0.47f)
                : _skill.IsExclusive ? new Color(0.48f, 0.31f, 0.6f) : RemakeUI.Green;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_skill == null) return;
            var body = new StringBuilder(_skill.Description ?? "");
            AppendBonus(body, "Cooking speed", _skill.ProductionSpeedBonusPerLevel);
            AppendBonus(body, "Revenue", _skill.RevenueBonusPerLevel);
            AppendBonus(body, "Patience", _skill.CustomerPatienceBonusPerLevel);
            AppendBonus(body, "Ingredient savings", _skill.IngredientCostReductionPerLevel);
            if (_skill.CleanlinessBonusPerLevel > 0) body.Append($"\nCleanliness: +{_skill.CleanlinessBonusPerLevel} per level");
            foreach (var prerequisite in _skill.Prerequisites ?? System.Array.Empty<SkillPrerequisite>())
                if (prerequisite?.Skill != null) body.Append($"\nRequires {prerequisite.Skill.SkillName} level {prerequisite.MinimumLevel}");
            var unlocked = _manager?.IsUnlocked(_skill) ?? false;
            UITooltipSystem.ShowTooltip(_skill.IsExclusive ? "EXCLUSIVE SKILL" : "RESTAURANT SKILL", _skill.SkillName,
                body.ToString(), unlocked ? "STATUS: AVAILABLE" : "STATUS: LOCKED", "Click to upgrade. Reset refunds spent points.", eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData) => UITooltipSystem.HideTooltip();

        private void OnEnable()
        {
            if (_button == null) return;
            _button.onClick.RemoveListener(Upgrade);
            _button.onClick.AddListener(Upgrade);
        }

        private void OnDisable()
        {
            UITooltipSystem.HideTooltip();
            if (_button != null) _button.onClick.RemoveListener(Upgrade);
        }

        private void Upgrade()
        {
            if (_manager == null || _skill == null) return;
            var result = _manager.TryUpgrade(_skill.Id);
            _owner?.ShowResult(result);
        }

        private static void AppendBonus(StringBuilder text, string name, float bonus)
        {
            if (bonus > 0) text.AppendFormat(CultureInfo.InvariantCulture, "\n{0}: +{1:0.#}% per level", name, bonus * 100);
        }
    }
}
