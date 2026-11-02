using BunDauPeo.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Thẻ một nhân vật phe ta dưới màn (kiểu Princess Connect): chân dung ô vuông màu class, HP, năng lượng, viền sáng khi tới lượt.
    // Chạm thẻ để chọn đồng minh cho chiêu hồi máu.
    public class BattleCardView : MonoBehaviour
    {
        private static readonly Color FrameOn = new(1f, 0.85f, 0.25f);
        private static readonly Color FrameOff = new(0.30f, 0.33f, 0.45f);

        [SerializeField] private Image _portrait;
        [SerializeField] private Text _name;
        [SerializeField] private Text _hp;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Image _energyFill;
        [SerializeField] private Image _frame;
        [SerializeField] private Text _readyTag;
        [SerializeField] private Button _button;

        public BattleUnit Unit { get; private set; }
        public Button Button => _button;

        public void Bind(BattleUnit unit)
        {
            gameObject.SetActive(true);
            Unit = unit;
            _portrait.color = BattleUnitView.ClassColor(unit.Character.Class);
            _name.text = unit.DisplayName;
        }

        public void Hide()
        {
            Unit = null;
            gameObject.SetActive(false);
        }

        // shownHp: máu theo hoạt hình (BattleUnitView.ShownHp), để thẻ và hình trên sân giảm cùng lúc.
        public void Refresh(int shownHp, int maxEnergy, bool isCurrent)
        {
            if (Unit == null) return;

            _hp.text = CombatViewText.UnitHp(shownHp, Unit.MaxHp);
            _hpFill.fillAmount = Unit.MaxHp > 0 ? (float)shownHp / Unit.MaxHp : 0f;
            _energyFill.fillAmount = Unit.Ultimate != null
                ? CombatViewText.UltimateFill(Unit.Energy, Unit.Ultimate.EnergyCost, maxEnergy)
                : 0f;
            // Đổi màu chứ không tắt Image: viền là nền của thẻ nên cũng là đích chạm của nút.
            _frame.color = isCurrent ? FrameOn : FrameOff;
            var ready = Unit.IsAlive && Unit.Ultimate != null && _energyFill.fillAmount >= 1f;
            _readyTag.text = CombatViewText.UltimateReadyTag(ready);
            // Người đã gục thì mờ đi nhưng thẻ vẫn ở đó để biết ai đã ngã.
            _portrait.canvasRenderer.SetAlpha(Unit.IsAlive ? 1f : 0.35f);
        }
    }
}
