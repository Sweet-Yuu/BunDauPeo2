using System;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Nút Thông báo (spec UI hub C 5.3): chấm đỏ khi có việc, bấm mở hộp, bấm dòng thì tới đúng chỗ. Đọc service mỗi 0,5 giây
    // (thể lực hồi theo đồng hồ, không có event); luật nằm ở NotifyRule.
    public class NotifyView : MonoBehaviour
    {
        private const float PollSeconds = 0.5f;

        [SerializeField] private HubNav _hub;
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _dot;
        [SerializeField] private NotifyPopup _popup;
        // Các đích của dòng.
        [SerializeField] private ScrollRect _worldScroll;
        [SerializeField] private RectTransform _incidentBubble;
        [SerializeField] private StaffPanel _staffPanel;
        [SerializeField] private AfkPopup _afkPopup;
        [SerializeField] private CharactersView _characters;

        private RestaurantService _restaurant;
        private CombatService _combat;
        private StarService _stars;
        private Roster _roster;
        private NotifySnapshot _shown;
        private float _pollLeft;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _restaurant = ServiceLocator.Get<RestaurantService>();
                _combat = ServiceLocator.Get<CombatService>();
                _stars = ServiceLocator.Get<StarService>();
                _roster = ServiceLocator.Get<Roster>();
            }
            catch (InvalidOperationException e)
            {
                // Mở không qua Boot: chấm tắt, hộp mở ra trống.
                Debug.LogError($"{nameof(NotifyView)}: {e.Message}", this);
                _restaurant = null;
            }

            _button.onClick.AddListener(OpenBox);
            _popup.RowClicked += Go;
            Refresh();
        }

        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(OpenBox);
            if (_popup != null) _popup.RowClicked -= Go;
            _restaurant = null;
        }

        private bool HasAllReferences()
        {
            var ok = _hub != null && _button != null && _dot != null && _popup != null
                     && _worldScroll != null && _incidentBubble != null && _staffPanel != null && _afkPopup != null && _characters != null;
            if (!ok) Debug.LogError($"{nameof(NotifyView)}: references are not assigned.", this);
            return ok;
        }

        private void Update()
        {
            if (_restaurant == null) return;

            _pollLeft -= Time.unscaledDeltaTime;
            if (_pollLeft <= 0f) Refresh();
        }

        private void Refresh()
        {
            _pollLeft = PollSeconds;
            _shown = Capture();
            var any = NotifyRule.HasAny(_shown);
            if (_dot.activeSelf != any) _dot.SetActive(any);
        }

        private NotifySnapshot Capture()
        {
            if (_restaurant == null) return default;

            var stamina = _combat.Stamina;
            var ready = NotifyRule.StarUpReady(CharacterListRows.Build(_roster, _stars).Select(r => r.Character.Id), _stars.GetNextCost,
                id => (int)Math.Min(int.MaxValue, _roster.GetFragments(id)));
            return new NotifySnapshot(_restaurant.Incidents.Active.HasValue, _restaurant.Estimate.MissingRole, _restaurant.Afk.PreviewCollect(),
                stamina.Current, stamina.Max, ready);
        }

        private void OpenBox()
        {
            Refresh();
            _popup.Open(_shown);
        }

        // Hộp đã đóng. Việc đã hết ngay trước khi bấm (sự cố tự huỷ, AFK vừa thu) vẫn tới đích, không báo gì.
        public void Go(NotifyKind kind)
        {
            _hub.Select(kind switch
            {
                NotifyKind.StaminaFull => HubTab.Stages,
                NotifyKind.StarUp => HubTab.Characters,
                _ => HubTab.Restaurant,
            });

            switch (kind)
            {
                case NotifyKind.Incident:
                    FocusIncident();
                    break;
                case NotifyKind.StaffMissing:
                    _staffPanel.Open();
                    break;
                case NotifyKind.AfkCoins:
                    if (_restaurant != null) _afkPopup.Show(_restaurant.Afk.AccumulatedSeconds);
                    break;
                case NotifyKind.StarUp:
                    if (_shown.StarUpReadyIds != null && _shown.StarUpReadyIds.Count > 0) _characters.SelectById(_shown.StarUpReadyIds[0]);
                    break;
            }
        }

        // Kéo khung thế giới cho "!" ra giữa màn.
        private void FocusIncident()
        {
            var content = _worldScroll.content;
            var viewport = _worldScroll.viewport != null ? _worldScroll.viewport : (RectTransform)_worldScroll.transform;
            var target = (Vector2)content.InverseTransformPoint(_incidentBubble.position);
            _worldScroll.StopMovement();
            _worldScroll.normalizedPosition = WorldFocus.Center(content.rect.size, viewport.rect.size, target);
        }
    }
}
