using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hoang.Remake
{
    public sealed class SkillTreeUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _graph;
        [SerializeField] private Text _points;
        [SerializeField] private Text _summary;
        [SerializeField] private Text _feedback;
        [SerializeField] private Button _reset;
        [SerializeField] private SkillSlot[] _slots = Array.Empty<SkillSlot>();
        [SerializeField] private SkillTreeConnections _connections;
        private SkillTreeManager _manager;
        public IReadOnlyList<SkillSlot> Slots => _slots;

        public static SkillTreeUI Create(Transform parent)
        {
            var root = RemakeUI.Panel(parent, "SkillTree", RemakeUI.Paper, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var ui = root.gameObject.AddComponent<SkillTreeUI>();
            RemakeUI.Label(root, "Title", "RESTAURANT SKILL TREE", 32, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.88f), new Vector2(0.75f, 0.98f), Vector2.zero, Vector2.zero);
            ui._points = RemakeUI.Label(root, "Points", "Points: 20   Special points: 3", 23, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.79f), new Vector2(0.72f, 0.88f), Vector2.zero, Vector2.zero);
            ui._reset = RemakeUI.Button(root, "ResetSkillsButton", "Refund points", ui.ResetSkills,
                new Vector2(0.76f, 0.87f), new Vector2(0.97f, 0.96f), Vector2.zero, Vector2.zero);
            ui._graph = RemakeUI.Panel(root, "EditableSkillGraph", new Color(0.90f, 0.91f, 0.84f),
                new Vector2(0.025f, 0.12f), new Vector2(0.72f, 0.78f), Vector2.zero, Vector2.zero);
            var lines = new GameObject("PrerequisiteConnections", typeof(RectTransform), typeof(CanvasRenderer), typeof(SkillTreeConnections));
            lines.transform.SetParent(ui._graph, false);
            RemakeUI.Layout((RectTransform)lines.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ui._connections = lines.GetComponent<SkillTreeConnections>();
            ui._connections.color = new Color(0.38f, 0.54f, 0.46f, 0.75f);
            ui._connections.raycastTarget = false;
            ui._summary = RemakeUI.Label(root, "SkillBuffSummary", "Choose a skill to grow your restaurant.", 23, TextAnchor.UpperLeft,
                new Vector2(0.75f, 0.2f), new Vector2(0.97f, 0.75f), Vector2.zero, Vector2.zero);
            ui._feedback = RemakeUI.Label(root, "Feedback", "Unlock connected skills. Each upgrade is saved automatically.", 20, TextAnchor.MiddleLeft,
                new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.1f), Vector2.zero, Vector2.zero);
            return ui;
        }

        public void Configure(SkillSO[] definitions)
        {
            if (_slots.Length > 0) return;
            definitions ??= Array.Empty<SkillSO>();
            _slots = new SkillSlot[definitions.Length];
            for (var i = 0; i < definitions.Length; i++)
            {
                var skill = definitions[i];
                var point = skill.GraphPosition;
                if (point == Vector2.zero)
                    point = new Vector2(0.18f + (i % 3) * 0.32f, 0.75f - (i / 3) * 0.45f);
                point = new Vector2(Mathf.Clamp(point.x, 0.12f, 0.88f), Mathf.Clamp(point.y, 0.12f, 0.88f));
                var button = RemakeUI.Button(_graph, "Skill_" + skill.Id, skill.SkillName, null, point, point,
                    new Vector2(-88, -50), new Vector2(88, 50));
                var slot = button.gameObject.AddComponent<SkillSlot>();
                slot.Configure(skill, button);
                _slots[i] = slot;
            }
            _connections.Configure(_slots);
        }

        public void Bind(SkillTreeManager manager)
        {
            if (_manager != null) _manager.Changed -= Refresh;
            _manager = manager;
            if (_manager == null) return;
            var definitions = new SkillSO[_manager.Skills.Count];
            for (var i = 0; i < definitions.Length; i++) definitions[i] = _manager.Skills[i];
            Configure(definitions);
            foreach (var slot in _slots) slot.Bind(_manager, this);
            if (isActiveAndEnabled) _manager.Changed += Refresh;
            Refresh();
        }

        public void ArrangeGraph()
        {
            var depths = new Dictionary<SkillSO, int>();
            foreach (var slot in _slots)
                if (slot != null && slot.Definition != null) DepthOf(slot.Definition, depths, new HashSet<SkillSO>());
            var maxDepth = 0;
            foreach (var depth in depths.Values) maxDepth = Math.Max(depth, maxDepth);
            for (var depth = 0; depth <= maxDepth; depth++)
            {
                var layer = new List<SkillSlot>();
                foreach (var slot in _slots)
                    if (slot != null && slot.Definition != null && depths[slot.Definition] == depth) layer.Add(slot);
                for (var row = 0; row < layer.Count; row++)
                {
                    var point = new Vector2((depth + 0.5f) / (maxDepth + 1), (row + 1f) / (layer.Count + 1));
                    var rect = (RectTransform)layer[row].transform;
                    rect.anchorMin = point;
                    rect.anchorMax = point;
                    rect.anchoredPosition = Vector2.zero;
                }
            }
            _connections.SetVerticesDirty();
        }

        public void ShowResult(SkillUpgradeStatus status)
        {
            _feedback.text = status switch
            {
                SkillUpgradeStatus.Success => "Skill upgraded and saved.",
                SkillUpgradeStatus.Locked => "Upgrade every prerequisite to its required level first.",
                SkillUpgradeStatus.MaxLevel => "This skill is already at its maximum level.",
                SkillUpgradeStatus.NotEnoughPoints => "Not enough skill points.",
                SkillUpgradeStatus.BalanceLimit => "The point balance limit has been reached.",
                SkillUpgradeStatus.SaveFailed => "Could not save this upgrade. Please try again.",
                _ => "This skill is unavailable.",
            };
            Refresh();
        }

        private void Refresh()
        {
            if (_manager == null) return;
            _points.text = $"Points: {_manager.AvailablePoints}   Special points: {_manager.AvailableSpecialPoints}";
            _summary.text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "SKILL BONUSES\n\nCooking: +{0:0.#}%\nRevenue: +{1:0.#}%\nPatience: +{2:0.#}%\nSavings: +{3:0.#}%\nCleanliness: +{4}\n\nPurple skills use special points.",
                _manager.GetTotalProductionSpeedBonus() * 100, _manager.GetTotalRevenueBonus() * 100,
                _manager.GetTotalCustomerPatienceBonus() * 100, _manager.GetTotalCostReduction() * 100,
                _manager.GetTotalCleanlinessBonus());
            foreach (var slot in _slots) slot.Refresh();
            _connections.SetVerticesDirty();
        }

        private void ResetSkills()
        {
            if (_manager == null) return;
            _feedback.text = _manager.ResetSkills() ? "All spent points refunded. Skills reset and saved."
                : "Could not save the reset. Your skills and points are unchanged.";
            Refresh();
        }

        private void OnEnable()
        {
            if (_reset != null)
            {
                _reset.onClick.RemoveListener(ResetSkills);
                _reset.onClick.AddListener(ResetSkills);
            }
            if (_manager == null) return;
            _manager.Changed -= Refresh;
            _manager.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_reset != null) _reset.onClick.RemoveListener(ResetSkills);
            if (_manager != null) _manager.Changed -= Refresh;
            UITooltipSystem.HideTooltip();
        }

        private static int DepthOf(SkillSO skill, Dictionary<SkillSO, int> depths, HashSet<SkillSO> visiting)
        {
            if (depths.TryGetValue(skill, out var known)) return known;
            if (!visiting.Add(skill)) throw new ArgumentException("Skill prerequisites contain a cycle.");
            var depth = 0;
            foreach (var prerequisite in skill.Prerequisites ?? Array.Empty<SkillPrerequisite>())
                if (prerequisite?.Skill != null) depth = Math.Max(depth, 1 + DepthOf(prerequisite.Skill, depths, visiting));
            visiting.Remove(skill);
            depths[skill] = depth;
            return depth;
        }
    }
}
