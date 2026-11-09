using System;
using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Thanh dưới của Lobby (spec UI hub 5.3): 6 ô, 3 panel kín màn, Menu. Quán vẫn chạy bên dưới nên RestaurantView không tắt;
    // panel hay Menu mở thì đồng hồ sự cố dừng. Luật chọn ô nằm ở HubNavState.
    public class HubNav : MonoBehaviour
    {
        // Dùng chung cho builder và test: không nút nào của game được lọt vào dải 0–120 px của khung 1920×1080.
        public const float BarHeight = 120f;

        private const float ToastSeconds = 2f;
        private static readonly Color TabColor = new(0.22f, 0.33f, 0.25f);
        private static readonly Color TabHighlight = new(0.18f, 0.43f, 0.31f);

        // Thứ tự đúng HubTab.
        [SerializeField] private Button[] _tabButtons;
        [SerializeField] private GameObject _bar;
        [SerializeField] private GameObject _charactersPanel;
        [SerializeField] private GameObject _stagesPanel;
        [SerializeField] private GameObject _gachaPanel;
        [SerializeField] private GameObject _menuPanel;
        [SerializeField] private GameObject _comingSoonToast;
        // Mỗi phần tử cài IHubPage; bấm ô khác thì trang đang mở đóng qua Close().
        [SerializeField] private MonoBehaviour[] _pages;

        private readonly HashSet<object> _barHiders = new();
        private RestaurantService _service;
        private float _toastLeft;

        public HubNavState State { get; private set; } = new HubNavState();

        // Gọi trước LoadScene("Restaurant"): Awake của lần nạp sau mở đúng ô (và Menu) rồi xoá.
        public static void OpenOnNextLoad(HubTab tab, bool menu) => HubPending.Set(tab, menu);

        private void Awake()
        {
            if (!HasAllReferences()) return;

            if (HubPending.TryTake(out var tab, out var menu)) State = HubNavState.FromPending(tab, menu);
            ApplyPanels();
        }

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<RestaurantService>();
            }
            catch (InvalidOperationException e)
            {
                // Mở không qua Boot: thanh vẫn chạy, chỉ không dừng được đồng hồ sự cố.
                Debug.LogError($"{nameof(HubNav)}: {e.Message}", this);
                _service = null;
            }

            for (var i = 0; i < _tabButtons.Length; i++)
            {
                var tab = (HubTab)i;
                _tabButtons[i].onClick.AddListener(() => Select(tab));
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_tabButtons != null)
                foreach (var button in _tabButtons)
                    if (button != null) button.onClick.RemoveAllListeners();
            // Rời scene (vào trận) thì trả chủ dừng, để sự cố của lần sau không kẹt.
            _service?.Incidents.SetClockPaused(this, false);
            _service = null;
        }

        private void Update()
        {
            if (_toastLeft <= 0f) return;

            _toastLeft -= Time.unscaledDeltaTime;
            if (_toastLeft <= 0f) _comingSoonToast.SetActive(false);
        }

        private bool HasAllReferences()
        {
            var tabs = _tabButtons != null && _tabButtons.Length == Enum.GetValues(typeof(HubTab)).Length && Array.TrueForAll(_tabButtons, b => b != null);
            var pages = _pages != null && Array.TrueForAll(_pages, p => p is IHubPage);
            var ok = tabs && pages && _bar != null && _charactersPanel != null && _stagesPanel != null && _gachaPanel != null && _menuPanel != null
                     && _comingSoonToast != null;
            if (!ok)
                Debug.LogError($"{nameof(HubNav)}: references are not assigned (tabs {tabs}, pages implementing {nameof(IHubPage)} {pages}, " +
                               $"bar {_bar != null}, panels {_charactersPanel != null}/{_stagesPanel != null}/{_gachaPanel != null}/{_menuPanel != null}, toast {_comingSoonToast != null}).", this);
            return ok;
        }

        public void Select(HubTab tab)
        {
            switch (State.Select(tab))
            {
                case HubNavResult.Changed:
                    CloseOpenPages();
                    ApplyPanels();
                    break;
                case HubNavResult.MenuOpened:
                    ApplyPanels();
                    break;
                case HubNavResult.ComingSoon:
                    _comingSoonToast.SetActive(true);
                    _toastLeft = ToastSeconds;
                    return;
            }

            Refresh();
        }

        // Nút Đóng của Menu.
        public void CloseMenu()
        {
            State.CloseMenu();
            ApplyPanels();
            Refresh();
        }

        // Thanh ẩn khi còn ít nhất một chủ (Gacha ẩn lúc cutscene, lộ, tổng). Gọi lặp lại cùng chủ không lệch.
        public void SetBarHidden(object owner, bool hidden)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (hidden) _barHiders.Add(owner);
            else _barHiders.Remove(owner);
            // Rời scene thì _bar có thể đã bị huỷ trước OnDisable của người gọi.
            if (_bar != null) _bar.SetActive(_barHiders.Count == 0);
        }

        // Bấm ô khác: trang kín màn đang mở đóng qua đúng đường nút Quay lại của nó (trả SetSpawnBlocked...).
        public static void ClosePages(IEnumerable<IHubPage> pages)
        {
            foreach (var page in pages)
                if (page.IsOpen) page.Close();
        }

        public void CloseOpenPages() => ClosePages(_pages.OfType<IHubPage>());

        private void ApplyPanels()
        {
            _charactersPanel.SetActive(State.Current == HubTab.Characters);
            _stagesPanel.SetActive(State.Current == HubTab.Stages);
            _gachaPanel.SetActive(State.Current == HubTab.Gacha);
            _menuPanel.SetActive(State.MenuOpen);
        }

        private void Refresh()
        {
            for (var i = 0; i < _tabButtons.Length; i++)
                _tabButtons[i].targetGraphic.color = (HubTab)i == State.Highlighted ? TabHighlight : TabColor;
            _service?.Incidents.SetClockPaused(this, State.Current != HubTab.Restaurant || State.MenuOpen);
        }
    }
}
