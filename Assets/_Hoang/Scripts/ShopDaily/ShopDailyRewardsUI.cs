using System;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Hoang.Remake
{
    public sealed class ShopDailyRewardsUI : MonoBehaviour
    {
        private static readonly Color Paper = new(0.96f, 0.94f, 0.87f);
        private static readonly Color Ink = new(0.12f, 0.18f, 0.19f);
        private static readonly Color Green = new(0.20f, 0.43f, 0.33f);
        private static readonly Color Purple = new(0.37f, 0.29f, 0.56f);

        [SerializeField] private Text _balance;
        [SerializeField] private Text _countdown;
        [SerializeField] private Text _dailyStatus;
        [SerializeField] private Text _adStatus;
        [SerializeField] private Text _feedback;
        [SerializeField] private Button _claim;
        [SerializeField] private Text _claimLabel;
        [SerializeField] private Button _ad;
        [SerializeField] private Button[] _buy = Array.Empty<Button>();
        [SerializeField] private GameObject _editorAdDialog;
        [SerializeField] private Button _completeAd;
        [SerializeField] private Button _cancelAd;
        [SerializeField] private Button _errorAd;

        private RemakeContext _context;
        private ShopDailyRewardsService _service;
        private AdTicket _ticket;
        private bool _listening;
        private bool _busy;
        private float _nextRefresh;
        private UnityAction[] _buyHandlers;

        public void Bind(RemakeContext context)
        {
            Detach();
            _context = context;
            if (isActiveAndEnabled) Attach();
            Refresh();
        }

        private void OnEnable()
        {
            Attach();
            Refresh();
        }

        private void OnDisable() => Detach();

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 1f;
            Refresh();
        }

        private void Attach()
        {
            if (_listening || _context == null || _claim == null) return;
            _service = new ShopDailyRewardsService(_context);
            _context.Changed += Refresh;
            _context.ResetCompleted += OnReset;
            _claim.onClick.AddListener(Claim);
            _ad.onClick.AddListener(StartAd);
            _buyHandlers = new UnityAction[_buy.Length];
            for (var i = 0; i < _buy.Length; i++)
            {
                var index = i;
                _buyHandlers[i] = () => Buy(index);
                _buy[i].onClick.AddListener(_buyHandlers[i]);
            }
#if UNITY_EDITOR
            _completeAd.onClick.AddListener(CompleteAd);
            _cancelAd.onClick.AddListener(CancelAd);
            _errorAd.onClick.AddListener(CancelAd);
#endif
            _listening = true;
            _nextRefresh = 0f;
        }

        private void Detach()
        {
            if (_context != null)
            {
                _context.Changed -= Refresh;
                _context.ResetCompleted -= OnReset;
            }
            if (_listening)
            {
                _claim.onClick.RemoveListener(Claim);
                _ad.onClick.RemoveListener(StartAd);
                for (var i = 0; i < _buy.Length; i++) _buy[i].onClick.RemoveListener(_buyHandlers[i]);
#if UNITY_EDITOR
                _completeAd.onClick.RemoveListener(CompleteAd);
                _cancelAd.onClick.RemoveListener(CancelAd);
                _errorAd.onClick.RemoveListener(CancelAd);
#endif
            }
            _listening = false;
            _service?.Dispose();
            _service = null;
            _ticket = null;
            _busy = false;
            if (_editorAdDialog != null) _editorAdDialog.SetActive(false);
        }

        private void OnReset()
        {
            _ticket = null;
            if (_editorAdDialog != null) _editorAdDialog.SetActive(false);
            _feedback.text = "Progress reset. Today's rewards are available again.";
            Refresh();
        }

        private void Claim() => Run(() => _service.ClaimDaily(), "Received 100 Gold + 100 Diamond!");
        private void Buy(int index) => Run(() => _service.BuyGold(index), "Gold exchange complete!");

        private void Run(Func<ShopRewardResult> action, string success)
        {
            if (_busy || _service == null) return;
            _busy = true;
            Refresh();
            try
            {
                var result = action();
                _feedback.text = result == ShopRewardResult.Success ? success : Message(result);
                _feedback.color = result == ShopRewardResult.Success ? Green : new Color(0.68f, 0.22f, 0.18f);
            }
            finally
            {
                _busy = false;
                Refresh();
            }
        }

        private void StartAd()
        {
            if (_busy || _service == null) return;
#if UNITY_EDITOR
            _ticket = _service.BeginAd();
            if (_ticket == null)
            {
                _feedback.text = "No rewarded ad claims are available today.";
                return;
            }
            _editorAdDialog.SetActive(true);
            Refresh();
#else
            _feedback.text = "Ads are currently unavailable. Please try again later.";
#endif
        }

#if UNITY_EDITOR
        private void CompleteAd() => FinishEditorAd(true);
        private void CancelAd() => FinishEditorAd(false);

        private void FinishEditorAd(bool completed)
        {
            if (_ticket == null) return;
            var ticket = _ticket;
            _ticket = null;
            _editorAdDialog.SetActive(false);
            Run(() => _service.FinishAd(ticket, completed), "Received 50 Diamond!");
        }
#endif

        private void Refresh()
        {
            if (_balance == null) return;
            var ready = _service != null && _service.IsReady;
            var now = DateTimeOffset.UtcNow;
            var allowed = ready && !_busy && _ticket == null;
            var canClaim = ready && _service.CanClaimDaily(now);
            _balance.text = _context == null ? "Gold: --    Diamond: --" :
                $"Gold: {_context.Wallet.Get(CurrencyType.Coin):N0}    Diamond: {_context.Wallet.Get(CurrencyType.Gem):N0}";
            var until = ShopDailyRewardsService.UntilVietnamMidnight(now);
            _countdown.text = $"New day in {(int)until.TotalHours:00}:{until.Minutes:00}:{until.Seconds:00} | 00:00 UTC+7";
            _dailyStatus.text = !ready ? "Waiting for save..." : canClaim ? "TODAY'S GIFT" : "Claimed today";
            _claimLabel.text = canClaim ? "Claim rewards" : ready ? "Claimed today" : "Please wait";
            _claim.interactable = allowed && canClaim;
            var ads = ready ? _service.RemainingAds(now) : 0;
            _adStatus.text = $"{ads}/3 claims left today";
            _ad.interactable = allowed && ads > 0;
            for (var i = 0; i < _buy.Length; i++)
                _buy[i].interactable = allowed && _context.Wallet.Get(CurrencyType.Gem) >= ShopDailyRewardsService.Offers[i].DiamondCost;
        }

        private static string Message(ShopRewardResult result) => result switch
        {
            ShopRewardResult.AlreadyClaimed => "Rewards have already been claimed today.",
            ShopRewardResult.InsufficientFunds => "Not enough Diamond.",
            ShopRewardResult.DailyLimit => "All rewarded ad claims have been used today.",
            ShopRewardResult.Cancelled => "Ad not completed. No reward claim was used.",
            ShopRewardResult.SaveFailed => "Could not save this transaction. Please try again.",
            ShopRewardResult.BalanceLimit => "Balance limit reached. No transaction was made.",
            _ => "This action is currently unavailable.",
        };

        public static ShopDailyRewardsUI Create(Transform parent)
        {
            var root = RemakeUI.Panel(parent, "ShopDaily", Paper, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));
            var ui = root.gameObject.AddComponent<ShopDailyRewardsUI>();
            Label(root, "Title", "Shop & Daily rewards", 32, TextAnchor.MiddleLeft, new Vector2(0, .91f), new Vector2(.46f, 1), new Vector2(24, 0), new Vector2(-8, 0));
            ui._balance = Label(root, "Balance", "Gold: --    Diamond: --", 22, TextAnchor.MiddleRight, new Vector2(.46f, .91f), Vector2.one, new Vector2(0, 0), new Vector2(-24, 0));
            Label(root, "Subtitle", "Exchange Diamond. Keep your restaurant growing.", 19, TextAnchor.MiddleLeft, new Vector2(0, .85f), new Vector2(1, .91f), new Vector2(24, 0), new Vector2(-24, 0));

            var daily = RemakeUI.Panel(root, "DailyRewardPanel", new Color(.87f, .91f, .83f), new Vector2(0, .61f), new Vector2(1, .84f), new Vector2(24, 0), new Vector2(-24, 0));
            ui._dailyStatus = Label(daily, "DailyStatus", "TODAY'S GIFT", 22, TextAnchor.MiddleLeft, new Vector2(0, .60f), new Vector2(.65f, 1), new Vector2(20, 0), new Vector2(0, 0));
            Label(daily, "DailyReward", "+100 Gold   +100 Diamond", 28, TextAnchor.MiddleLeft, new Vector2(0, .25f), new Vector2(.65f, .68f), new Vector2(20, 0), Vector2.zero);
            ui._countdown = Label(daily, "Countdown", "New day in --:--:-- | 00:00 UTC+7", 17, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(.68f, .28f), new Vector2(20, 0), Vector2.zero);
            ui._claim = MakeButton(daily, "ClaimRewards", "Claim rewards", Green, new Vector2(.71f, .22f), new Vector2(.98f, .78f), Vector2.zero, Vector2.zero);
            ui._claimLabel = ui._claim.GetComponentInChildren<Text>();

            var names = new[] { "Free daily", "Gold pouch", "Gold chest", "Gold vault" };
            ui._buy = new Button[3];
            for (var i = 0; i < 4; i++)
            {
                var card = RemakeUI.Panel(root, i == 0 ? "FreeAdCard" : $"GoldOffer{i}", Color.white, new Vector2(i * .25f, .13f), new Vector2((i + 1) * .25f, .57f), new Vector2(24, 0), new Vector2(-12, 0));
                Label(card, "Name", names[i], 24, TextAnchor.MiddleCenter, new Vector2(0, .75f), Vector2.one, new Vector2(8, 0), new Vector2(-8, 0));
                var amount = i == 0 ? "+50 Diamond" : $"+{ShopDailyRewardsService.Offers[i - 1].GoldAmount:N0} Gold";
                Label(card, "Amount", amount, 28, TextAnchor.MiddleCenter, new Vector2(0, .45f), new Vector2(1, .75f), new Vector2(8, 0), new Vector2(-8, 0));
                var status = Label(card, "Details", i == 0 ? "3/3 claims left today" : "Added instantly", 17, TextAnchor.MiddleCenter, new Vector2(0, .30f), new Vector2(1, .45f), new Vector2(6, 0), new Vector2(-6, 0));
                var buy = MakeButton(card, i == 0 ? "WatchAd" : "BuyGold", i == 0 ? "Watch ad" : $"{ShopDailyRewardsService.Offers[i - 1].DiamondCost} Diamond", i == 0 ? Purple : Green, new Vector2(0, 0), new Vector2(1, .28f), new Vector2(12, 12), new Vector2(-12, -6));
                if (i == 0) { ui._ad = buy; ui._adStatus = status; }
                else ui._buy[i - 1] = buy;
            }

            ui._feedback = Label(root, "Feedback", "Daily rewards reset at midnight in Vietnam.", 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1, .10f), new Vector2(24, 0), new Vector2(-24, 0));
            var overlay = RemakeUI.Panel(root, "EditorAdSimulation", new Color(0, 0, 0, .76f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dialog = RemakeUI.Panel(overlay, "Dialog", Paper, new Vector2(.24f, .22f), new Vector2(.76f, .78f), Vector2.zero, Vector2.zero);
            Label(dialog, "Title", "TEST AD | EDITOR", 28, TextAnchor.MiddleCenter, new Vector2(0, .72f), Vector2.one, new Vector2(20, 0), new Vector2(-20, 0));
            Label(dialog, "Explanation", "Editor simulation only. Complete grants 50 Diamond.\nCancel and error do not use a reward claim.", 20, TextAnchor.MiddleCenter, new Vector2(0, .43f), new Vector2(1, .74f), new Vector2(20, 0), new Vector2(-20, 0));
            ui._completeAd = MakeButton(dialog, "CompleteAd", "Complete", Green, new Vector2(.08f, .23f), new Vector2(.92f, .40f), Vector2.zero, Vector2.zero);
            ui._cancelAd = MakeButton(dialog, "CancelAd", "Cancel", Purple, new Vector2(.08f, .04f), new Vector2(.48f, .20f), Vector2.zero, Vector2.zero);
            ui._errorAd = MakeButton(dialog, "ErrorAd", "Simulate error", Purple, new Vector2(.52f, .04f), new Vector2(.92f, .20f), Vector2.zero, Vector2.zero);
            ui._editorAdDialog = overlay.gameObject;
            overlay.gameObject.SetActive(false);
            RemakeRestaurantTheme.ApplyShop(ui);
            return ui;
        }

        private static Text Label(Transform parent, string name, string text, int size, TextAnchor alignment, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var label = RemakeUI.Label(parent, name, text, size, alignment, min, max, offsetMin, offsetMax);
            label.color = Ink;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Math.Min(size, 12);
            label.resizeTextMaxSize = size;
            label.raycastTarget = false;
            return label;
        }

        private static Button MakeButton(Transform parent, string name, string text, Color color, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var button = RemakeUI.Button(parent, name, text, null, min, max, offsetMin, offsetMax);
            button.GetComponent<Image>().color = color;
            var label = button.GetComponentInChildren<Text>();
            label.color = Color.white;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 23;
            return button;
        }
    }
}
