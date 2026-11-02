using System;
using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Bảng chiêu kiểu FGO: ba nút tròn của người đang tới lượt. Không chứa luật: gọi PlayerAct rồi đưa kết quả cho BattleView phát.
    public class BattleActionPanel : MonoBehaviour
    {
        [SerializeField] private BattleView _view;
        [SerializeField] private Button _basicButton;
        [SerializeField] private Button _skillButton;
        [SerializeField] private Button _ultimateButton;
        [SerializeField] private Text _skillLabel;
        // Vòng năng lượng của ultimate (Filled, radial).
        [SerializeField] private Image _ultimateRing;
        [SerializeField] private Text _turnLine;

        private CombatService _service;
        private BattleUnit _chosenEnemy;
        private BattleUnit _chosenAlly;
        // Nút và vòng vẫn hiện người phe ta gần nhất khi tới lượt quái.
        private BattleUnit _lastAlly;

        // chosen*: người chơi đã chạm, hoặc null. Chiêu Damage một mục tiêu: quái đã chọn nếu còn sống và thuộc đợt này, không thì mặc định;
        // chiêu Heal một mục tiêu: tương tự với người phe ta. Chiêu không cần chọn thì null.
        public static BattleUnit ResolveTarget(BattleSession session, BattleActionKind kind, BattleUnit chosenEnemy, BattleUnit chosenAlly)
        {
            var actor = session.Current;
            if (actor == null) return null;

            var skill = kind == BattleActionKind.Basic ? actor.BasicAttack : kind == BattleActionKind.Skill ? actor.Skill : actor.Ultimate;
            if (skill == null) return null;

            switch (skill.Target)
            {
                case SkillTarget.SingleEnemy: return Usable(chosenEnemy, session.Enemies) ?? session.DefaultTarget(kind);
                case SkillTarget.SingleAlly: return Usable(chosenAlly, session.Allies) ?? session.DefaultTarget(kind);
                default: return null;
            }
        }

        private static BattleUnit Usable(BattleUnit unit, IReadOnlyList<BattleUnit> side)
        {
            if (unit == null || !unit.IsAlive) return null;
            foreach (var candidate in side)
                if (ReferenceEquals(candidate, unit)) return unit;
            return null;
        }

        public void ChooseEnemy(BattleUnit unit) => _chosenEnemy = unit;

        public void ChooseAlly(BattleUnit unit) => _chosenAlly = unit;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<CombatService>();
            }
            catch (InvalidOperationException)
            {
                _service = null;
            }

            _basicButton.onClick.AddListener(OnBasic);
            _skillButton.onClick.AddListener(OnSkill);
            _ultimateButton.onClick.AddListener(OnUltimate);
        }

        private void OnDisable()
        {
            if (_basicButton != null) _basicButton.onClick.RemoveListener(OnBasic);
            if (_skillButton != null) _skillButton.onClick.RemoveListener(OnSkill);
            if (_ultimateButton != null) _ultimateButton.onClick.RemoveListener(OnUltimate);
        }

        private bool HasAllReferences()
        {
            var ok = _view != null && _basicButton != null && _skillButton != null && _ultimateButton != null
                     && _skillLabel != null && _ultimateRing != null && _turnLine != null;
            if (!ok) Debug.LogError($"{nameof(BattleActionPanel)}: references are not assigned.", this);
            return ok;
        }

        private void OnBasic() => Act(BattleActionKind.Basic);

        private void OnSkill() => Act(BattleActionKind.Skill);

        private void OnUltimate() => Act(BattleActionKind.Ultimate);

        private void Act(BattleActionKind kind)
        {
            var session = _service?.Current;
            // Nút đã khoá khi đang phát, nhưng hai lần bấm trong một khung thì vẫn có thể lọt qua.
            if (session == null || _view.IsBusy || _view.IsPaused) return;

            var result = session.PlayerAct(kind, ResolveTarget(session, kind, _chosenEnemy, _chosenAlly));
            _view.Enqueue(result);
        }

        private void Update()
        {
            var session = _service?.Current;
            if (session == null) return;

            var playerTurn = session.State == BattleState.PlayerTurn;
            if (playerTurn) _lastAlly = session.Current;
            var locked = !playerTurn || _view.IsBusy || _view.IsPaused;

            _basicButton.interactable = !locked && session.GetAvailability(BattleActionKind.Basic) == ActionAvailability.Available;
            _skillButton.interactable = !locked && session.GetAvailability(BattleActionKind.Skill) == ActionAvailability.Available;
            _ultimateButton.interactable = !locked && session.GetAvailability(BattleActionKind.Ultimate) == ActionAvailability.Available;

            var unit = _lastAlly;
            _skillLabel.text = CombatViewText.SkillButton(unit != null ? unit.SkillCooldown : 0);
            _ultimateRing.fillAmount = unit != null && unit.Ultimate != null
                ? CombatViewText.UltimateFill(unit.Energy, unit.Ultimate.EnergyCost, _service.Config.MaxEnergy)
                : 0f;

            var acting = _view.ActingUnit ?? session.Current;
            _turnLine.text = acting != null ? CombatViewText.TurnLine(acting.DisplayName) : "";

            MarkTargets(session);
        }

        // Mũi tên trên quái đang bị nhắm (đã chọn hoặc mặc định) và trên người phe ta đã chọn.
        private void MarkTargets(BattleSession session)
        {
            // Trận hết thì không còn mục tiêu để chỉ.
            var ended = session.State == BattleState.Won || session.State == BattleState.Lost;
            var enemy = ended ? null : ResolveTarget(session, BattleActionKind.Basic, _chosenEnemy, null);
            foreach (var view in _view.EnemyViews) view.SetTargeted(view.Unit != null && ReferenceEquals(view.Unit, enemy));

            var ally = ended ? null : Usable(_chosenAlly, session.Allies);
            foreach (var view in _view.AllyViews) view.SetTargeted(view.Unit != null && ReferenceEquals(view.Unit, ally));
        }
    }
}
