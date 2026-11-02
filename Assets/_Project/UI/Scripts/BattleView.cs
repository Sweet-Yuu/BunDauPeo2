using System;
using System.Collections.Generic;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Chỉ vẽ: không chứa luật. Mỗi khung so CombatService.Current với trận đang vẽ; khác thì vẽ lại hoặc về "Chưa có trận".
    // Hoạt hình phát từng BattleActionResult theo Time.unscaledDeltaTime (không dùng Time.timeScale, để quán không bị ảnh hưởng).
    public class BattleView : MonoBehaviour
    {
        public const string BattleScene = "Battle", StageSelectScene = "StageSelect", RestaurantScene = "Restaurant";

        private const int SlotCount = 5;
        private const float StrikeDistance = 60f, FloatRise = 90f;

        private static readonly Color DamageOnEnemy = Color.white;
        private static readonly Color DamageOnAlly = new(1f, 0.55f, 0.45f);
        private static readonly Color HealColor = new(0.55f, 0.95f, 0.55f);

        [SerializeField] private BattleUnitView[] _allies;
        [SerializeField] private BattleUnitView[] _enemies;
        [SerializeField] private Text _floatTemplate;
        [SerializeField] private GameObject _emptyRoot;
        [SerializeField] private Button _emptyBackButton;

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
        private BattleActionResult _playing;
        private BattleUnitView _actorView;
        private float _elapsed, _duration;
        private bool _impacted;

        public static float ActionSeconds(bool fast) => fast ? 0.3f : 0.6f;

        // Task sau (bảng chiêu) đưa kết quả đòn của người chơi vào đây.
        public bool IsBusy => _playing != null || _queue.Count > 0;

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
            _hasDrawn = false;
        }

        private void OnDisable()
        {
            if (_emptyBackButton != null) _emptyBackButton.onClick.RemoveListener(Back);
        }

        private bool HasAllReferences()
        {
            var ok = _floatTemplate != null && _emptyRoot != null && _emptyBackButton != null
                     && Complete(_allies) && Complete(_enemies);
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

        private static void Back() => SceneManager.LoadScene(RestaurantScene);

        private void Update()
        {
            if (_allies == null || _emptyRoot == null) return;

            var dt = Time.unscaledDeltaTime;
            var session = _service?.Current;
            if (!_hasDrawn || !ReferenceEquals(session, _drawn)) Redraw(session);

            if (session != null) Animate(session, dt);
            foreach (var view in _allies) view.Tick(dt);
            foreach (var view in _enemies) view.Tick(dt);
            TickFloats(dt);
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

            foreach (var view in _allies) view.Hide();
            foreach (var view in _enemies) view.Hide();
            if (session == null) return;

            foreach (var ally in session.Allies) _allies[ally.SlotIndex].Bind(ally, BattleLayout.AllyRect(ally.SlotIndex));
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
            if (_playing.NextWaveStarted) BindEnemies(_drawn);
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
