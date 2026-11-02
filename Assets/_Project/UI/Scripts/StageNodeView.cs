using BunDauPeo.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Một nút tròn trên bản đồ màn: màu và biểu tượng theo trạng thái (khoá: ổ khoá, đã clear: dấu tích). Không chứa luật.
    public class StageNodeView : MonoBehaviour
    {
        private static readonly Color LockedColor = new(0.35f, 0.36f, 0.42f);
        private static readonly Color AvailableColor = new(0.30f, 0.50f, 0.90f);
        private static readonly Color ClearedColor = new(0.30f, 0.70f, 0.40f);

        [SerializeField] private Image _body;
        [SerializeField] private Text _label;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private GameObject _checkIcon;
        [SerializeField] private Button _button;

        public Button Button => _button;
        public StageDefinition Stage { get; private set; }

        public void Bind(StageDefinition stage, StageStatus status)
        {
            Stage = stage;
            _label.text = CombatViewText.NodeLabel(stage);
            _body.color = status switch
            {
                StageStatus.Cleared => ClearedColor,
                StageStatus.Available => AvailableColor,
                _ => LockedColor,
            };
            _lockIcon.SetActive(status != StageStatus.Cleared && status != StageStatus.Available);
            _checkIcon.SetActive(status == StageStatus.Cleared);
        }
    }
}
