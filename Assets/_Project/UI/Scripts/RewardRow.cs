using System.Collections.Generic;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Hàng ô thưởng dùng chung (kết quả trận, popup màn): ô vuông tô màu theo loại tiền, số nằm dưới ô.
    public class RewardRow : MonoBehaviour
    {
        public const int MaxCells = 4;

        [SerializeField] private Text _label;
        [SerializeField] private Image[] _cells;
        [SerializeField] private Text[] _amounts;

        public bool HasAllReferences()
        {
            var ok = _label != null && _cells != null && _amounts != null && _cells.Length == MaxCells && _amounts.Length == MaxCells
                     && System.Array.TrueForAll(_cells, c => c != null) && System.Array.TrueForAll(_amounts, a => a != null);
            if (!ok) Debug.LogError($"{nameof(RewardRow)}: references are not assigned.", this);
            return ok;
        }

        // shortcut: quá MaxCells dòng thì chỉ hiện MaxCells ô đầu; nới khi có màn thưởng hơn 4 loại.
        public void Show(IReadOnlyList<CurrencyAmount> lines, string label)
        {
            if (!HasAllReferences()) return;

            gameObject.SetActive(true);
            _label.text = label ?? "";
            for (var i = 0; i < MaxCells; i++)
            {
                var visible = i < lines.Count;
                _cells[i].gameObject.SetActive(visible);
                if (!visible) continue;
                _cells[i].color = CombatViewText.CurrencyColor(lines[i].Type);
                _amounts[i].text = CombatViewText.CurrencyCell(lines[i]);
            }
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
