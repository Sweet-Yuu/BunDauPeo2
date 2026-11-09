using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hoang.Remake
{
    public sealed class SkillTreeConnections : Graphic
    {
        [SerializeField] private SkillSlot[] _slots = System.Array.Empty<SkillSlot>();

        public void Configure(SkillSlot[] slots)
        {
            _slots = slots;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var bySkill = new Dictionary<SkillSO, SkillSlot>();
            foreach (var slot in _slots)
                if (slot != null && slot.Definition != null) bySkill[slot.Definition] = slot;
            foreach (var slot in _slots)
            {
                if (slot == null || slot.Definition == null) continue;
                foreach (var prerequisite in slot.Definition.Prerequisites ?? System.Array.Empty<SkillPrerequisite>())
                {
                    if (prerequisite?.Skill == null || !bySkill.TryGetValue(prerequisite.Skill, out var parent)) continue;
                    Vector2 from = rectTransform.InverseTransformPoint(parent.transform.position);
                    Vector2 to = rectTransform.InverseTransformPoint(slot.transform.position);
                    var direction = to - from;
                    if (direction.sqrMagnitude < 1) continue;
                    var edge = new Vector2(-direction.y, direction.x).normalized * 2.5f;
                    var first = vertices.currentVertCount;
                    vertices.AddVert(from - edge, color, Vector2.zero);
                    vertices.AddVert(from + edge, color, Vector2.zero);
                    vertices.AddVert(to + edge, color, Vector2.zero);
                    vertices.AddVert(to - edge, color, Vector2.zero);
                    vertices.AddTriangle(first, first + 1, first + 2);
                    vertices.AddTriangle(first, first + 2, first + 3);
                }
            }
        }
    }
}
