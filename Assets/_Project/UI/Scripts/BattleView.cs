using System;
using System.Collections.Generic;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Chỉ vẽ: không chứa luật. Mỗi khung so CombatService.Current với trận đang vẽ; khác thì vẽ lại hoặc về "Chưa có trận".
    // Hoạt hình phát từng BattleActionResult theo Time.unscaledDeltaTime (không dùng Time.timeScale, để quán không bị ảnh hưởng).
    public class BattleView : MonoBehaviour
    {
        public const string BattleScene = "Battle", StageSelectScene = "StageSelect", RestaurantScene = "Restaurant";

        private const int SlotCount = 5, TurnCellCount = 10;
        private const float StrikeDistance = 60f, FloatRise = 90f;

        private static readonly Color DamageOnEnemy = Color.white;
        private static readonly Color DamageOnAlly = new(1f, 0.55f, 0.45f);
        private static readonly Color HealColor = new(0.55f, 0.95f, 0.55f);
        private static readonly Color AutoOn = new(0.95f, 0.75f, 0.20f);
        private static readonly Color AutoOff = new(0.30f, 0.33f, 0.45f);
        private static readonly Color AllyCell = new(0.30f, 0.50f, 0.90f);
        private static readonly Color EnemyCell = new(0.70f, 0.22f, 0.25f);
        private const float WaveBannerSeconds = 1f;

        [SerializeField] private BattleUnitView[] _allies;
        [SerializeField] private BattleUnitView[] _enemies;
        [SerializeField] private Text _floatTemplate;
        [SerializeField] private GameObject _emptyRoot;
        [SerializeField] private Button _emptyBackButton;
        [SerializeField] private BattleActionPanel _panel;
        [SerializeField] private BattleCardView[] _cards;
        // Banner tạm: Task kết quả thay bằng BattleResultView.
        [SerializeField] private GameObject _endRoot;
        [SerializeField] private Text _endText;
        [SerializeField] private Button _endButton;
        // Thanh trên: chữ đợt, thanh lượt 10 ô, Menu / AUTO / tốc độ, banner đợt mới, popup tạm dừng.
        [SerializeField] private GameObject _topBar;
        [SerializeField] private Text _waveText;
        [SerializeField] private Image[] _turnCells;
        [SerializeField] private Button _menuButton;
        [SerializeField] private Button _autoButton;
        [SerializeField] private Button _speedButton;
        [SerializeField] private GameObject _waveBanner;
        [SerializeField] private Text _waveBannerText;
        [SerializeField] private BattlePausePopup _pause;

        private sealed class FloatText
        {
            public Text Text;
            public float Age;
            public float Life;
            public Vector2 Start;
        }

        private readonly Queue<BattleActionResult> _queue = new();
        private readonly List<FloatText> _floats = new();

        private CombatService _service;
        private BattleSession _drawn;
        private bool _hasDrawn;
        private bool _ready;
        private readonly List<(Button Button, UnityAction Action)> _listeners = new();
        private BattleActionResult _playing;
        private BattleUnitView _actorView;
        private float _elapsed, _duration;
        private bool _impacted;
        // Đợt đang hiện trên màn: chỉ tăng khi đòn kết liễu phát xong, cùng lúc quái mới hiện.
        private int _shownWave;
        private float _bannerLeft;
        private Text _speedLabel;
        private Text[] _cellLabels;

        public static float ActionSeconds(bool fast) => fast ? 0.3f : 0.6f;

        // Task sau (bảng chiêu) đưa kết quả đòn của người chơi vào đây.
        public bool IsBusy => _playing != null || _queue.Count > 0;

        // Người đang ra đòn trong hoạt hình; null khi rảnh.
        public BattleUnit ActingUnit => _playing?.Actor;

        // Popup tạm dừng đang mở: hoạt hình, quái và AUTO đứng yên.
        public bool IsPaused => _pause != null && _pause.IsOpen;

        public IReadOnlyList<BattleUnitView> AllyViews => _allies;

        public IReadOnlyList<BattleUnitView> EnemyViews => _enemies;

        public void Enqueue(BattleActionResult result)
        {
            if (result != null && result.Status == BattleActionStatus.Success) _queue.Enqueue(result);
        }

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<CombatService>();
            }
            catch (InvalidOperationException)
            {
                // Mở Battle không qua Boot: không có dịch vụ thì chỉ hiện "Chưa có trận".
                _service = null;
            }

            _emptyBackButton.onClick.AddListener(Back);
            _endButton.onClick.AddListener(OnEndClicked);
            Listen(_menuButton, OnMenuClicked);
            Listen(_autoButton, OnAutoClicked);
            Listen(_speedButton, OnSpeedClicked);
            _speedLabel = _speedButton.GetComponentInChildren<Text>(true);
            _cellLabels = System.Array.ConvertAll(_turnCells, c => c.GetComponentInChildren<Text>(true));
            // Chạm quái hoặc người phe ta đặt mục tiêu; thẻ chỉ chọn đồng minh. Đọc Unit lúc chạm vì hình được dùng lại giữa các đợt.
            foreach (var view in _enemies) Listen(view.Button, () => _panel.ChooseEnemy(view.Unit));
            foreach (var view in _allies) Listen(view.Button, () => _panel.ChooseAlly(view.Unit));
            foreach (var card in _cards) Listen(card.Button, () => _panel.ChooseAlly(card.Unit));
            _hasDrawn = false;
            _ready = true;
        }

        private void OnDisable()
        {
            _ready = false;
            if (_emptyBackButton != null) _emptyBackButton.onClick.RemoveListener(Back);
            if (_endButton != null) _endButton.onClick.RemoveListener(OnEndClicked);
            foreach (var (button, action) in _listeners)
                if (button != null) button.onClick.RemoveListener(action);
            _listeners.Clear();
        }

        private void Listen(Button button, UnityAction action)
        {
            button.onClick.AddListener(action);
            _listeners.Add((button, action));
        }

        private bool HasAllReferences()
        {
            var ok = _floatTemplate != null && _emptyRoot != null && _emptyBackButton != null
                     && _panel != null && _endRoot != null && _endText != null && _endButton != null
                     && _topBar != null && _waveText != null && _menuButton != null && _autoButton != null && _speedButton != null
                     && _waveBanner != null && _waveBannerText != null && _pause != null
                     && _turnCells != null && _turnCells.Length == TurnCellCount && System.Array.TrueForAll(_turnCells, c => c != null)
                     && Complete(_allies) && Complete(_enemies) && _cards != null && _cards.Length == SlotCount
                     && System.Array.TrueForAll(_cards, c => c != null);
            if (!ok) Debug.LogError($"{nameof(BattleView)}: references are not assigned.", this);
            return ok;
        }

        private static bool Complete(BattleUnitView[] views)
        {
            if (views == null || views.Length != SlotCount) return false;
            foreach (var view in views)
                if (view == null) return false;
            return true;
        }

        private static void Back() => SceneManager.LoadScene(StageSelectScene);

        // Banner tạm: về bản đồ màn (Task kết quả thay bằng BattleResultView).
        private void OnEndClicked()
        {
            _service?.EndBattle();
            SceneManager.LoadScene(StageSelectScene);
        }

        private void OnMenuClicked()
        {
            var session = _service?.Current;
            if (session != null && session.State != BattleState.Won && session.State != BattleState.Lost) _pause.Open(session);
        }

        private void OnAutoClicked()
        {
            if (_service != null) _service.AutoEnabled = !_service.AutoEnabled;
        }

        private void OnSpeedClicked()
        {
            if (_service != null) _service.FastEnabled = !_service.FastEnabled;
        }

        private void Update()
        {
            if (!_ready) return;

            var dt = Time.unscaledDeltaTime;
            var session = _service?.Current;
            if (!_hasDrawn || !ReferenceEquals(session, _drawn)) Redraw(session);

            // Tạm dừng: không phát tiếp, không gọi EnemyAct hay AUTO, và mọi bộ đếm thời gian đứng yên.
            var paused = IsPaused;
            if (paused) dt = 0f;
            if (session != null && !paused) Animate(session, dt);
            foreach (var view in _allies) view.Tick(dt);
            foreach (var view in _enemies) view.Tick(dt);
            TickFloats(dt);
            if (session != null)
            {
                RefreshHud(session);
                RefreshTopBar(session, dt);
            }
        }

        private void RefreshTopBar(BattleSession session, float dt)
        {
            _waveText.text = CombatViewText.WaveLabel(_shownWave + 1, session.WaveCount);

            var order = session.RemainingOrder;
            for (var i = 0; i < _turnCells.Length; i++)
            {
                var cell = _turnCells[i];
                cell.gameObject.SetActive(i < order.Count);
                if (i >= order.Count) continue;

                var baseColor = order[i].IsAlly ? AllyCell : EnemyCell;
                cell.color = i == 0 ? Color.Lerp(baseColor, Color.white, 0.6f) : new Color(baseColor.r, baseColor.g, baseColor.b, 0.7f);
                var name = order[i].DisplayName;
                _cellLabels[i].text = string.IsNullOrEmpty(name) ? "" : name.Substring(0, 1);
            }

            ((Image)_autoButton.targetGraphic).color = _service.AutoEnabled ? AutoOn : AutoOff;
            _speedLabel.text = CombatViewText.SpeedLabel(_service.FastEnabled);

            if (_bannerLeft > 0f)
            {
                _bannerLeft -= dt;
                if (_bannerLeft <= 0f) _waveBanner.SetActive(false);
            }
        }

        // Thẻ theo máu hoạt hình; banner chỉ hiện khi đòn cuối đã phát xong.
        private void RefreshHud(BattleSession session)
        {
            var current = session.State == BattleState.PlayerTurn && !IsBusy ? session.Current : null;
            foreach (var card in _cards)
            {
                if (card.Unit == null) continue;
                card.Refresh(_allies[card.Unit.SlotIndex].ShownHp, _service.Config.MaxEnergy, ReferenceEquals(card.Unit, current));
            }

            var ended = (session.State == BattleState.Won || session.State == BattleState.Lost) && !IsBusy;
            _endRoot.SetActive(ended);
            if (ended) _endText.text = CombatViewText.EndBanner(session.State);
        }

        private void Redraw(BattleSession session)
        {
            _drawn = session;
            _hasDrawn = true;
            _queue.Clear();
            _playing = null;
            _actorView = null;
            foreach (var text in _floats) text.Text.gameObject.SetActive(false);
            _emptyRoot.SetActive(session == null);
            _pause.Close();
            _waveBanner.SetActive(false);
            _bannerLeft = 0f;
            _shownWave = session?.WaveIndex ?? 0;
            _topBar.SetActive(session != null);

            _endRoot.SetActive(false);
            _panel.gameObject.SetActive(session != null);
            foreach (var view in _allies) view.Hide();
            foreach (var view in _enemies) view.Hide();
            foreach (var card in _cards) card.Hide();
            if (session == null) return;

            foreach (var ally in session.Allies) _allies[ally.SlotIndex].Bind(ally, BattleLayout.AllyRect(ally.SlotIndex));
            for (var i = 0; i < session.Allies.Count; i++) _cards[i].Bind(session.Allies[i]);
            BindEnemies(session);
        }

        private void BindEnemies(BattleSession session)
        {
            foreach (var view in _enemies) view.Hide();
            foreach (var enemy in session.Enemies)
                _enemies[enemy.SlotIndex].Bind(enemy, BattleLayout.EnemyRect(enemy.SlotIndex, enemy.IsBoss));
        }

        private void Animate(BattleSession session, float dt)
        {
            if (_playing == null)
            {
                if (_queue.Count > 0) Begin(_queue.Dequeue());
                // Chỉ khi hàng đợi trống: hết đòn người chơi cũ thì mới tới quái, đúng thứ tự trận.
                else if (session.State == BattleState.EnemyTurn) Enqueue(session.EnemyAct());
                else if (session.State == BattleState.PlayerTurn && _service.AutoEnabled)
                {
                    var choice = AutoPolicy.Choose(session);
                    Enqueue(session.PlayerAct(choice.Kind, choice.Target));
                }
                if (_playing == null && _queue.Count > 0) Begin(_queue.Dequeue());
            }

            if (_playing == null) return;

            _elapsed += dt;
            var t = Mathf.Clamp01(_elapsed / _duration);
            if (_actorView != null) _actorView.Nudge(StrikeOffset(_playing) * Mathf.Sin(t * Mathf.PI));

            if (!_impacted && t >= 0.5f) Impact(_playing);
            if (_elapsed < _duration) return;

            if (!_impacted) Impact(_playing);
            if (_actorView != null) _actorView.Nudge(Vector2.zero);
            // Đợt mới chỉ hiện sau khi đòn kết liễu phát xong.
            if (_playing.NextWaveStarted)
            {
                BindEnemies(_drawn);
                _shownWave++;
                _waveBannerText.text = CombatViewText.WaveLabel(_shownWave + 1, _drawn.WaveCount);
                _waveBanner.SetActive(true);
                _bannerLeft = WaveBannerSeconds;
            }

            _playing = null;
            _actorView = null;
        }

        private void Begin(BattleActionResult result)
        {
            _playing = result;
            _actorView = ViewOf(result.Actor);
            _elapsed = 0f;
            _duration = ActionSeconds(_service != null && _service.FastEnabled);
            _impacted = false;
        }

        // Hướng nhích về phía mục tiêu đầu tiên; đòn không có mục tiêu (không xảy ra ở trận thường) thì nhích về phía đối diện.
        private Vector2 StrikeOffset(BattleActionResult result)
        {
            if (_actorView == null) return Vector2.zero;
            var target = result.Hits.Count > 0 ? ViewOf(result.Hits[0].Target) : null;
            var direction = target != null
                ? target.Home - _actorView.Home
                : (result.Actor.IsAlly ? Vector2.right : Vector2.left);
            return direction.normalized * StrikeDistance;
        }

        private void Impact(BattleActionResult result)
        {
            _impacted = true;
            foreach (var hit in result.Hits)
            {
                var view = ViewOf(hit.Target);
                if (view == null) continue;

                view.ApplyDelta(hit.IsHeal ? hit.Amount : -hit.Amount);
                var color = hit.IsHeal ? HealColor : hit.Target.IsAlly ? DamageOnAlly : DamageOnEnemy;
                SpawnFloat(view.Home, (hit.IsHeal ? "+" : "-") + hit.Amount, color);
            }
        }

        // Tìm hình của một người theo ô; so tham chiếu để hình của đợt cũ không nhận đòn của đợt mới.
        private BattleUnitView ViewOf(BattleUnit unit)
        {
            if (unit == null) return null;
            var views = unit.IsAlly ? _allies : _enemies;
            return unit.SlotIndex >= 0 && unit.SlotIndex < views.Length && ReferenceEquals(views[unit.SlotIndex].Unit, unit)
                ? views[unit.SlotIndex]
                : null;
        }

        private void SpawnFloat(Vector2 anchoredPosition, string text, Color color)
        {
            var entry = _floats.Find(f => !f.Text.gameObject.activeSelf);
            if (entry == null)
            {
                var clone = Instantiate(_floatTemplate, _floatTemplate.transform.parent);
                entry = new FloatText { Text = clone };
                _floats.Add(entry);
            }

            entry.Age = 0f;
            entry.Life = ActionSeconds(_service != null && _service.FastEnabled) * 1.5f;
            entry.Start = anchoredPosition;
            entry.Text.text = text;
            entry.Text.color = color;
            entry.Text.rectTransform.anchoredPosition = anchoredPosition;
            entry.Text.gameObject.SetActive(true);
        }

        private void TickFloats(float dt)
        {
            foreach (var entry in _floats)
            {
                if (!entry.Text.gameObject.activeSelf) continue;

                entry.Age += dt;
                var t = entry.Age / entry.Life;
                if (t >= 1f)
                {
                    entry.Text.gameObject.SetActive(false);
                    continue;
                }

                entry.Text.rectTransform.anchoredPosition = entry.Start + Vector2.up * (FloatRise * t);
                var color = entry.Text.color;
                color.a = 1f - t * t;
                entry.Text.color = color;
            }
        }
    }
}
