using System;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Hoang.Remake
{
    public sealed class RemakeShell : MonoBehaviour
    {
        [SerializeField] private GameObject[] _pages;
        [SerializeField] private Button[] _navigation;
        [SerializeField] private Text _balance;
        [SerializeField] private Text _shopTabLabel;
        [SerializeField] private Text _feedback;
        [SerializeField] private SkillTreeUI _skillTree;
        [SerializeField] private FacilityGridUI _facilityGrid;
        [SerializeField] private ShopDailyRewardsUI _shop;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private GameObject _settings;
        [SerializeField] private GameObject _resetConfirmation;
        [SerializeField] private Button _closeSettings;
        [SerializeField] private Button _requestReset;
        [SerializeField] private Button _confirmReset;
        [SerializeField] private Button _cancelReset;
        private RemakeBootstrap _bootstrap;
        private UnityEngine.Events.UnityAction[] _navActions;
        private float _refreshAt;
        public SkillTreeUI SkillTree => _skillTree;
        public FacilityGridUI FacilityGrid => _facilityGrid;
        public ShopDailyRewardsUI Shop => _shop;

        public void Bind(RemakeBootstrap bootstrap)
        {
            if (_bootstrap != null) return;
            _bootstrap = bootstrap;
            _skillTree.Bind(bootstrap.Skills);
            _facilityGrid.Bind(bootstrap.Facilities);
            _shop.Bind(bootstrap.Context);
            _navActions = new UnityEngine.Events.UnityAction[_navigation.Length];
            for (var index = 0; index < _navigation.Length; index++)
            {
                var page = index;
                _navActions[index] = () => ShowPage(page);
                _navigation[index].onClick.AddListener(_navActions[index]);
            }
            _settingsButton.onClick.AddListener(OpenSettings);
            _closeSettings.onClick.AddListener(CloseSettings);
            _requestReset.onClick.AddListener(RequestReset);
            _cancelReset.onClick.AddListener(CancelReset);
            _confirmReset.onClick.AddListener(ResetAll);
            bootstrap.Context.Wallet.Changed += Refresh;
            ShowPage(0);
            Refresh();
        }

        public void ShowPage(int index)
        {
            for (var i = 0; i < _pages.Length; i++)
            {
                _pages[i].SetActive(i == index);
                _navigation[i].GetComponent<Image>().color = i == index ? RemakeUI.Green : new Color(0.39f, 0.42f, 0.45f);
            }
            UITooltipSystem.HideTooltip();
        }

        private void Update()
        {
            if (Time.unscaledTime < _refreshAt) return;
            _refreshAt = Time.unscaledTime + 0.25f;
            Refresh();
        }

        private void Refresh()
        {
            if (_bootstrap?.Context == null) return;
            var wallet = _bootstrap.Context.Wallet;
            var available = ShopDailyRewardsService.VietnamDay(DateTimeOffset.UtcNow) > _bootstrap.Context.Save.ShopDaily.DailyRewardDay;
            _shopTabLabel.text = available ? "Shop & daily  [READY]" : "Shop & daily";
            _balance.text = $"Gold  {wallet.Get(CurrencyType.Coin):N0}     Diamond  {wallet.Get(CurrencyType.Gem):N0}     Ingredients  {wallet.Get(CurrencyType.Material):N0}";
        }
        private void OpenSettings() { _settings.SetActive(true); _resetConfirmation.SetActive(false); }
        private void CloseSettings() => _settings.SetActive(false);
        private void RequestReset() => _resetConfirmation.SetActive(true);
        private void CancelReset() => _resetConfirmation.SetActive(false);
        private void ResetAll()
        {
            if (!_bootstrap.Context.Reset()) { _feedback.text = "Could not reset saved progress. Please try again."; return; }
            CloseSettings();
            ShowPage(0);
            _feedback.text = "All progress has been reset.";
            Refresh();
        }

        private void OnDestroy()
        {
            if (_bootstrap?.Context != null) _bootstrap.Context.Wallet.Changed -= Refresh;
            if (_navActions != null)
                for (var i = 0; i < _navigation.Length; i++)
                    if (_navigation[i] != null) _navigation[i].onClick.RemoveListener(_navActions[i]);
        }

        public static RemakeShell Create(SkillSO[] skills)
        {
            var go = new GameObject("HoangRestaurantSystems", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(RemakeShell));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var shell = go.GetComponent<RemakeShell>();
            var safe = RemakeUI.Panel(go.transform, "SafeArea", RemakeUI.Paper, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.gameObject.AddComponent<RemakeSafeArea>();
            RemakeUI.Label(safe, "Title", "BUN DAU PEO  /  RESTAURANT", 32, TextAnchor.MiddleLeft,
                new Vector2(0, 0.90f), Vector2.one, new Vector2(32, 0), new Vector2(-520, -8));
            shell._balance = RemakeUI.Label(safe, "Balances", "Gold  1,000     Diamond  5,000     Ingredients  0", 24, TextAnchor.MiddleRight,
                new Vector2(0.40f, 0.90f), Vector2.one, Vector2.zero, new Vector2(-215, -8));
            shell._settingsButton = RemakeUI.Button(safe, "SettingsButton", "Settings", null,
                new Vector2(1, 0.92f), Vector2.one, new Vector2(-185, 5), new Vector2(-32, -18));
            var names = new[] { "Skill tree", "Kitchen grid", "Shop & daily" };
            shell._pages = new GameObject[names.Length];
            shell._navigation = new Button[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                shell._navigation[i] = RemakeUI.Button(safe, "Tab" + i, names[i], null,
                    new Vector2((float)i / names.Length, 0.81f), new Vector2((float)(i + 1) / names.Length, 0.90f), new Vector2(32, 10), new Vector2(-32, -10));
                shell._pages[i] = RemakeUI.Panel(safe, names[i], Color.clear,
                    new Vector2(0, 0.035f), new Vector2(1, 0.80f), new Vector2(32, 18), new Vector2(-32, -10)).gameObject;
            }
            shell._shopTabLabel = shell._navigation[2].GetComponentInChildren<Text>();
            shell._shopTabLabel.text = "Shop & daily  [READY]";
            shell._skillTree = SkillTreeUI.Create(shell._pages[0].transform);
            shell._skillTree.Configure(skills);
            shell._facilityGrid = FacilityGridUI.Create(shell._pages[1].transform);
            shell._shop = ShopDailyRewardsUI.Create(shell._pages[2].transform);
            shell._feedback = RemakeUI.Label(safe, "Feedback", "", 20, TextAnchor.MiddleLeft,
                Vector2.zero, new Vector2(1, 0.035f), new Vector2(32, 0), new Vector2(-32, 0));
            shell.BuildSettings(safe);
            for (var i = 1; i < shell._pages.Length; i++) shell._pages[i].SetActive(false);
            return shell;
        }

        private void BuildSettings(Transform parent)
        {
            _settings = RemakeUI.Panel(parent, "SettingsModal", new Color(0.06f, 0.09f, 0.10f, 0.88f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var card = RemakeUI.Panel(_settings.transform, "SettingsCard", RemakeUI.Paper,
                new Vector2(0.27f, 0.27f), new Vector2(0.73f, 0.73f), Vector2.zero, Vector2.zero);
            RemakeUI.Label(card, "Title", "Settings", 40, TextAnchor.MiddleCenter,
                new Vector2(0, 0.70f), Vector2.one, Vector2.zero, Vector2.zero);
            RemakeUI.Label(card, "Details", "Progress saves automatically.\nReset clears currency, skills, facilities and daily rewards.", 25, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.70f), Vector2.zero, Vector2.zero);
            _requestReset = RemakeUI.Button(card, "RequestReset", "Reset all progress", null,
                new Vector2(0.08f, 0.12f), new Vector2(0.58f, 0.32f), Vector2.zero, Vector2.zero);
            _closeSettings = RemakeUI.Button(card, "CloseSettings", "Close", null,
                new Vector2(0.63f, 0.12f), new Vector2(0.92f, 0.32f), Vector2.zero, Vector2.zero);
            _resetConfirmation = RemakeUI.Panel(_settings.transform, "ResetConfirmation", RemakeUI.Paper,
                new Vector2(0.25f, 0.27f), new Vector2(0.75f, 0.73f), Vector2.zero, Vector2.zero).gameObject;
            RemakeUI.Label(_resetConfirmation.transform, "ConfirmText", "Reset ALL progress?\nThis cannot be undone.", 36, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.94f), Vector2.zero, Vector2.zero);
            _confirmReset = RemakeUI.Button(_resetConfirmation.transform, "ConfirmReset", "Reset everything", null,
                new Vector2(0.08f, 0.12f), new Vector2(0.53f, 0.32f), Vector2.zero, Vector2.zero);
            _cancelReset = RemakeUI.Button(_resetConfirmation.transform, "CancelReset", "Cancel", null,
                new Vector2(0.58f, 0.12f), new Vector2(0.92f, 0.32f), Vector2.zero, Vector2.zero);
            _resetConfirmation.SetActive(false);
            _settings.SetActive(false);
        }
    }
}
