using System;
using BunDauPeo.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Thông tin một màn Battle: các đợt quái (ô vuông, boss lớn hơn), thưởng, thể lực trước/sau và cảnh báo không hoàn.
    // Không tự vào trận: bấm Thách đấu thì bắn Challenge, bản đồ mở popup chọn đội.
    public class StageInfoPopup : MonoBehaviour, IHubPage
    {
        public const int WaveRows = 4, CellsPerRow = WaveDefinition.MaxEnemies;
        private const float NormalCell = 64f, BossCell = 84f;

        private static readonly Color EnemyColor = new(0.55f, 0.12f, 0.15f);
        private static readonly Color BossColor = new(0.80f, 0.20f, 0.20f);

        // _root chặn chạm; component nằm ở object luôn bật để còn nhận Open.
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _title;
        [SerializeField] private Text[] _waveLabels;
        [SerializeField] private Image[] _cells;
        [SerializeField] private RewardRow _rewardRow;
        [SerializeField] private Text _staminaText;
        [SerializeField] private Text _warningText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _challengeButton;

        private CombatService _service;
        private StageDefinition _stage;

        public event Action<StageDefinition> Challenge;

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Open(CombatService service, StageDefinition stage)
        {
            if (service == null || stage == null || !HasAllReferences()) return;

            _service = service;
            _stage = stage;
            _title.text = stage.DisplayName;
            // shortcut: bốn hàng đợt; màn nhiều đợt hơn chỉ hiện bốn đợt đầu, nới WaveRows nếu có màn dài hơn.
            for (var row = 0; row < WaveRows; row++)
            {
                var wave = row < stage.Waves.Count ? stage.Waves[row] : null;
                _waveLabels[row].gameObject.SetActive(wave != null);
                // View giữ nhãn này nên tự ghi chữ mỗi lần mở; chữ builder ghi sẵn là tiếng Việt.
                _waveLabels[row].text = UiText.Format("combat.wave_row", row + 1);
                for (var col = 0; col < CellsPerRow; col++)
                {
                    var cell = _cells[row * CellsPerRow + col];
                    var enemy = wave != null && col < wave.Enemies.Count ? wave.Enemies[col] : null;
                    cell.gameObject.SetActive(enemy != null);
                    if (enemy == null) continue;

                    var size = enemy.IsBoss ? BossCell : NormalCell;
                    cell.rectTransform.sizeDelta = new Vector2(size, size);
                    cell.color = enemy.IsBoss ? BossColor : EnemyColor;
                }
            }

            _root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            _stage = null;
            if (_root != null) _root.SetActive(false);
        }

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            _cancelButton.onClick.AddListener(Close);
            _challengeButton.onClick.AddListener(OnChallengeClicked);
        }

        private void OnDisable()
        {
            if (_cancelButton != null) _cancelButton.onClick.RemoveListener(Close);
            if (_challengeButton != null) _challengeButton.onClick.RemoveListener(OnChallengeClicked);
        }

        private void Update()
        {
            // Thể lực hồi theo giờ: thẻ "Thể lực a → b" và dòng chờ phải theo kịp khi popup đang mở.
            if (IsOpen && _stage != null) Refresh();
        }

        private bool HasAllReferences()
        {
            var ok = _root != null && _title != null && _rewardRow != null && _staminaText != null
                     && _warningText != null && _messageText != null && _cancelButton != null && _challengeButton != null
                     && _waveLabels != null && _waveLabels.Length == WaveRows && Array.TrueForAll(_waveLabels, t => t != null)
                     && _cells != null && _cells.Length == WaveRows * CellsPerRow && Array.TrueForAll(_cells, c => c != null);
            if (!ok) Debug.LogError($"{nameof(StageInfoPopup)}: references are not assigned.", this);
            return ok;
        }

        private void Refresh()
        {
            if (_service == null || _stage == null) return;

            var firstClear = _service.Progress.GetStatus(_stage.Id) != StageStatus.Cleared;
            _rewardRow.Show(firstClear ? _stage.FirstClearReward : _stage.RepeatReward, CombatViewText.RewardHeader(firstClear));

            var stamina = _service.Stamina;
            var current = stamina.Current;
            var cost = _stage.StaminaCost;
            _staminaText.text = CombatViewText.StaminaChange(current, cost);
            _warningText.text = CombatViewText.EnterWarning(cost);

            var affordable = stamina.CanAfford(cost);
            _challengeButton.interactable = affordable;
            _messageText.text = affordable
                ? ""
                : CombatViewText.WaitLine(CombatViewText.SecondsUntilAffordable(current, cost, stamina.SecondsToNext, _service.Config.StaminaRegenSeconds));
        }

        private void OnChallengeClicked()
        {
            if (_stage != null) Challenge?.Invoke(_stage);
        }
    }
}
