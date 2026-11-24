using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Missions;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Trang Nhiệm vụ kín màn, mở từ nút tròn ở Lobby (spec đợt 3 8.1). Server trao thưởng (MissionService); trang chỉ vẽ và gửi lệnh.
    public class MissionsPage : MonoBehaviour, IHubPage, IResourcePage
    {
        private static readonly Color TabOn = new(0.30f, 0.50f, 0.90f);
        private static readonly Color TabOff = new(0.30f, 0.33f, 0.42f);

        [SerializeField] private GameObject _root;
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button[] _tabButtons;
        [SerializeField] private GameObject[] _tabBadges;
        [SerializeField] private Text[] _tabBadgeTexts;
        [SerializeField] private RectTransform _content;
        [SerializeField] private MissionRowView _rowTemplate;
        [SerializeField] private Text _remainingText;
        [SerializeField] private Text _completedText;
        [SerializeField] private Button _claimAllButton;
        [SerializeField] private StaminaCapConfirm _confirm;
        // Đích của "Đi tới".
        [SerializeField] private HubNav _hub;
        [SerializeField] private StaffPanel _staffPanel;
        [SerializeField] private UpgradeTestPanel _upgradePanel;
        [SerializeField] private AfkPopup _afkPopup;
        [SerializeField] private ProfilePopup _profilePopup;

        private readonly List<MissionRowView> _rows = new();
        private MissionService _service;
        private MissionClaimFlow _flow;
        private RestaurantService _restaurant;
        private int _tab;
        private long _shownMinute = -1;

        public bool IsOpen => _root != null && _root.activeSelf;

        public ResourceSlots Slots => ResourceSlots.Stamina | ResourceSlots.Gem;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<MissionService>();
                _restaurant = ServiceLocator.Get<RestaurantService>();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(MissionsPage)}: {e.Message}", this);
                _service = null;
                return;
            }

            _flow = new MissionClaimFlow(_service, p => _confirm.AskAsync(p.Have, p.Granted, p.Cap, p.Wasted));
            _openButton.onClick.AddListener(Open);
            _closeButton.onClick.AddListener(Close);
            _claimAllButton.onClick.AddListener(ClaimAll);
            for (var i = 0; i < _tabButtons.Length; i++)
            {
                var tab = i;
                _tabButtons[i].onClick.AddListener(() => SelectTab(tab));
            }

            _service.Changed += Refresh;
            _root.SetActive(false);
        }

        private void OnDisable()
        {
            if (_service == null) return;
            _service.Changed -= Refresh;
            _restaurant?.Incidents.SetSpawnBlocked(this, false);
            _openButton.onClick.RemoveListener(Open);
            _closeButton.onClick.RemoveListener(Close);
            _claimAllButton.onClick.RemoveListener(ClaimAll);
            foreach (var tab in _tabButtons) tab.onClick.RemoveAllListeners();
            // Dòng là con của trang, gắn một lần lúc tạo: không gỡ ở đây (OnEnable không gắn lại).
            _service = null;
            _restaurant = null;
        }

        private bool HasAllReferences()
        {
            var ok = _root != null && _openButton != null && _closeButton != null && _tabButtons is { Length: 2 } && _tabBadges is { Length: 2 }
                     && _tabBadgeTexts is { Length: 2 } && _content != null && _rowTemplate != null && _remainingText != null
                     && _completedText != null && _claimAllButton != null && _confirm != null && _hub != null && _staffPanel != null
                     && _upgradePanel != null && _afkPopup != null && _profilePopup != null;
            if (!ok) Debug.LogError($"{nameof(MissionsPage)}: a serialized reference is not assigned.", this);
            return ok;
        }

        public void Open()
        {
            if (_service == null) return;
            _tab = MissionService.TabDaily;
            _restaurant.Incidents.SetSpawnBlocked(this, true);
            _root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            _confirm.Cancel();
            _restaurant?.Incidents.SetSpawnBlocked(this, false);
            _root.SetActive(false);
        }

        private void Update()
        {
            if (_service == null || !IsOpen) return;

            // Đồng hồ và khung giờ đổi theo phút: vẽ lại đúng lúc đó, không mỗi khung (L-044).
            var minute = (long)_service.UntilNextDay().TotalMinutes;
            if (minute != _shownMinute) Refresh();
        }

        private void SelectTab(int tab)
        {
            _tab = tab;
            Refresh();
        }

        private void Refresh()
        {
            if (_service == null || !IsOpen) return;

            var rows = _service.Rows(_tab);
            while (_rows.Count < rows.Count)
            {
                var view = Instantiate(_rowTemplate, _content);
                view.Clicked += OnRowClicked;
                _rows.Add(view);
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                var show = i < rows.Count;
                _rows[i].gameObject.SetActive(show);
                if (show) _rows[i].Bind(rows[i], _service.Definition(rows[i].Id));
            }

            for (var i = 0; i < _tabButtons.Length; i++)
            {
                var count = _service.ClaimableCount(i);
                _tabBadges[i].SetActive(count > 0);
                _tabBadgeTexts[i].text = MissionRowRule.Badge(count);
                _tabButtons[i].targetGraphic.color = i == _tab ? TabOn : TabOff;
            }

            var (done, total) = MissionRowRule.Completed(rows);
            _completedText.text = UiText.Format("missions.completed", done, total);
            _shownMinute = (long)_service.UntilNextDay().TotalMinutes;
            var showRemaining = MissionRowRule.ShowRemaining(_tab);
            _remainingText.gameObject.SetActive(showRemaining);
            if (showRemaining) _remainingText.text = UiText.Format("missions.remaining", MissionRowRule.FormatRemaining(_service.UntilNextDay()));
            _claimAllButton.interactable = MissionRowRule.ClaimAllEnabled(_service.ClaimableCount(_tab));
        }

        private void OnRowClicked(MissionRowView row)
        {
            if (row.State == MissionButtonState.Claim) RunClaim(row.Id);
            else if (row.State == MissionButtonState.Goto) Go(MissionGoto.Destination(row.Id));
        }

        private void ClaimAll() => RunClaim(null);

        private async void RunClaim(string idOrNull)
        {
            try
            {
                await _flow.ClaimAsync(_tab, idOrNull);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            Refresh();
        }

        // Đóng trang rồi mở đích (spec 8.1). Việc tới Lobby: về ô Quán rồi mở trang hoặc hộp tương ứng.
        private void Go(HubGoto destination)
        {
            Close();
            switch (destination)
            {
                case HubGoto.Stages: _hub.Select(HubTab.Stages); break;
                case HubGoto.Gacha: _hub.Select(HubTab.Gacha); break;
                case HubGoto.Characters: _hub.Select(HubTab.Characters); break;
                case HubGoto.Afk:
                    _hub.Select(HubTab.Restaurant);
                    // Không còn Coin chờ thì chỉ về Lobby.
                    if (_restaurant != null && _restaurant.Afk.PreviewCollect() > 0) _afkPopup.Show(_restaurant.Afk.AccumulatedSeconds);
                    break;
                case HubGoto.Staff: _hub.Select(HubTab.Restaurant); _staffPanel.Open(); break;
                case HubGoto.Upgrade: _hub.Select(HubTab.Restaurant); _upgradePanel.Open(); break;
                case HubGoto.Profile: _hub.Select(HubTab.Restaurant); _profilePopup.Open(); break;
            }
        }
    }
}
