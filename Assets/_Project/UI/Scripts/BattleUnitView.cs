using BunDauPeo.Characters;
using BunDauPeo.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Hình của một người trong trận. HP hiển thị đi theo từng đòn (ApplyDelta lúc đòn chạm), không đọc BattleUnit.Hp,
    // vì trận chạy đồng bộ nên Hp thật đã là giá trị cuối trước khi hoạt hình phát tới.
    [RequireComponent(typeof(CanvasGroup))]
    public class BattleUnitView : MonoBehaviour
    {
        private const float FlashSeconds = 0.25f, FadeSeconds = 0.4f, FillPerSecond = 2.5f;

        private static readonly Color EnemyColor = new(0.55f, 0.12f, 0.15f);
        private static readonly Color BossColor = new(0.40f, 0.06f, 0.10f);

        [SerializeField] private Image _body;
        [SerializeField] private Text _name;
        [SerializeField] private Image _hpFill;
        [SerializeField] private Text _hpText;
        [SerializeField] private Image _targetMarker;
        [SerializeField] private Button _button;

        private CanvasGroup _group;
        private Color _baseColor;
        private float _fill;
        private float _flash;

        public BattleUnit Unit { get; private set; }
        public int ShownHp { get; private set; }
        // Vị trí nghỉ (toạ độ neo); đòn nhích khỏi đây rồi về.
        public Vector2 Home { get; private set; }
        public Button Button => _button;

        private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

        public static Color ClassColor(CharacterClass cls) => cls switch
        {
            CharacterClass.Tank => new Color(0.45f, 0.55f, 0.68f),
            CharacterClass.Fighter => new Color(0.92f, 0.42f, 0.27f),
            CharacterClass.Support => new Color(0.47f, 0.78f, 0.47f),
            CharacterClass.Marksman => new Color(0.95f, 0.80f, 0.25f),
            CharacterClass.Mage => new Color(0.66f, 0.46f, 0.86f),
            _ => new Color(0.7f, 0.7f, 0.7f),
        };

        // box: hộp theo BattleLayout (gốc trái trên).
        public void Bind(BattleUnit unit, Rect box)
        {
            gameObject.SetActive(true);
            Unit = unit;
            ShownHp = unit.Hp;
            _fill = unit.HpRatio;
            _flash = 0f;

            var rect = (RectTransform)transform;
            rect.anchoredPosition = Home = BattleLayout.ToAnchored(box.center);
            rect.sizeDelta = box.size;

            _baseColor = unit.IsAlly ? ClassColor(unit.Character.Class) : unit.IsBoss ? BossColor : EnemyColor;
            _body.color = _baseColor;
            _name.text = unit.DisplayName;
            _hpFill.fillAmount = _fill;
            ShowHpText();
            _targetMarker.enabled = false;
            Group.alpha = unit.IsAlive ? 1f : 0f;
            Group.blocksRaycasts = unit.IsAlive;
        }

        public void Hide()
        {
            Unit = null;
            gameObject.SetActive(false);
        }

        public void SetTargeted(bool on) => _targetMarker.enabled = on;

        // Âm là sát thương, dương là hồi máu.
        public void ApplyDelta(int delta)
        {
            ShownHp = Mathf.Clamp(ShownHp + delta, 0, Unit.MaxHp);
            if (delta < 0) _flash = FlashSeconds;
            ShowHpText();
        }

        private void ShowHpText()
        {
            if (_hpText != null) _hpText.text = CombatViewText.UnitHp(ShownHp, Unit.MaxHp);
        }

        public void Nudge(Vector2 offset) => ((RectTransform)transform).anchoredPosition = Home + offset;

        public void Tick(float dt)
        {
            if (Unit == null) return;

            _fill = Mathf.MoveTowards(_fill, (float)ShownHp / Unit.MaxHp, dt * FillPerSecond);
            _hpFill.fillAmount = _fill;

            _flash = Mathf.Max(0f, _flash - dt);
            _body.color = Color.Lerp(_baseColor, Color.white, _flash / FlashSeconds);

            // Chết thì mờ dần rồi ẩn; hồi máu không hồi sinh nên không cần chiều ngược lại.
            if (ShownHp > 0) return;
            Group.alpha = Mathf.MoveTowards(Group.alpha, 0f, dt / FadeSeconds);
            if (Group.alpha <= 0f) Group.blocksRaycasts = false;
        }
    }
}
