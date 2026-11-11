using System;
using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Điều phối: bấm quay, giao kết quả cho reveal, trang tổng, popup quay tiếp và lịch sử. Không giữ luật nào.
    public class GachaView : MonoBehaviour
    {
        [SerializeField] private GachaBannerDefinition _banner;
        [SerializeField] private CharacterCatalog _characters;
        [SerializeField] private Text _bannerNameText;
        [SerializeField] private Text _pityText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _singleButton;
        [SerializeField] private Button _tenButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _historyButton;
        [SerializeField] private GachaRevealView _revealView;
        [SerializeField] private GachaTotalView _totalView;
        [SerializeField] private GachaRollAgainPopup _rollAgainPopup;
        [SerializeField] private GachaHistoryPopup _historyPopup;
        // Thanh dưới ẩn từ lúc bấm quay (cutscene, lộ, tổng) tới lúc về trang roll (spec UI hub 5.3).
        [SerializeField] private HubNav _hub;

        private Wallet _wallet;
        private Roster _roster;
        private GachaService _gacha;
        private StarService _stars;
        private bool _subscribed;
        // Từ lúc bấm quay đến lúc bấm Thôi: khoá nút quay trên trang roll.
        private bool _busy;
        private bool _lastIsTen;
        private bool _lastSaved = true;
        private IReadOnlyList<GachaPullEntry> _entries;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _wallet = ServiceLocator.Get<Wallet>();
                _roster = ServiceLocator.Get<Roster>();
                _gacha = ServiceLocator.Get<GachaService>();
                _stars = ServiceLocator.Get<StarService>();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(GachaView)}: {e.Message}", this);
                return;
            }

            _wallet.Changed += Redraw;
            _roster.Changed += Redraw;
            _singleButton.onClick.AddListener(PullSingle);
            _tenButton.onClick.AddListener(PullTen);
            _skipButton.onClick.AddListener(_revealView.SkipToSsrOrTotal);
            _historyButton.onClick.AddListener(OpenHistory);
            _revealView.Finished += ShowTotal;
            _totalView.OkClicked += OpenRollAgain;
            _rollAgainPopup.RollAgainClicked += RollAgain;
            _rollAgainPopup.DismissClicked += Dismiss;
            _historyPopup.CloseClicked += CloseHistory;
            _subscribed = true;
            Redraw();
        }

        private void OnDisable()
        {
            if (!_subscribed) return;
            _subscribed = false;
            // Panel tắt giữa lượt quay (nạp lại scene...) thì trả chủ ẩn thanh, để thanh không kẹt.
            _hub.SetBarHidden(this, false);
            _wallet.Changed -= Redraw;
            _roster.Changed -= Redraw;
            _singleButton.onClick.RemoveListener(PullSingle);
            _tenButton.onClick.RemoveListener(PullTen);
            _skipButton.onClick.RemoveListener(_revealView.SkipToSsrOrTotal);
            _historyButton.onClick.RemoveListener(OpenHistory);
            _revealView.Finished -= ShowTotal;
            _totalView.OkClicked -= OpenRollAgain;
            _rollAgainPopup.RollAgainClicked -= RollAgain;
            _rollAgainPopup.DismissClicked -= Dismiss;
            _historyPopup.CloseClicked -= CloseHistory;
        }

        // Nút Bỏ qua chỉ hiện khi reveal cho phép (Quay 10, không ở cutscene SSR).
        private void Update()
        {
            if (_subscribed) _skipButton.gameObject.SetActive(_busy && _revealView.CanSkipAll);
        }

        private bool HasAllReferences()
        {
            var ok = _banner != null && _characters != null && _bannerNameText != null
                     && _pityText != null && _messageText != null && _singleButton != null && _tenButton != null
                     && _skipButton != null && _historyButton != null && _revealView != null && _totalView != null
                     && _rollAgainPopup != null && _historyPopup != null && _hub != null;
            if (!ok) Debug.LogError($"{nameof(GachaView)}: a serialized reference is not assigned.", this);
            return ok;
        }

        private void PullSingle() => TryPull(() => _gacha.PullSingle(_banner.Id), isTen: false);

        private void PullTen() => TryPull(() => _gacha.PullTen(_banner.Id), isTen: true);

        private void TryPull(Func<GachaPullResult> pull, bool isTen)
        {
            GachaPullResult result;
            try
            {
                result = pull();
            }
            catch (Exception e)
            {
                // Roll chạy trước khi trừ tiền nên chưa có gì đổi; mở lại nút thay vì khoá vĩnh viễn.
                Debug.LogException(e, this);
                _busy = false;
                Redraw();
                return;
            }

            StartPull(result, isTen);
        }

        private void StartPull(GachaPullResult result, bool isTen)
        {
            _messageText.text = GachaViewText.StatusMessage(result.Status, result.Saved);
            if (result.Status != GachaPullStatus.Success)
            {
                // Lỗi (thiếu Gem, banner lỗi) thì không có lượt nào để xem: mở lại nút.
                _busy = false;
                Redraw();
                return;
            }

            _lastIsTen = isTen;
            _lastSaved = result.Saved;
            _entries = result.Entries;
            _busy = true;
            _revealView.Play(result.Entries, isTen, _stars.GetStars);
            Redraw();
        }

        private void ShowTotal() => _totalView.Show(_entries);

        private void OpenRollAgain()
        {
            _totalView.Hide();
            var cost = CurrentCost();
            _rollAgainPopup.Open(GachaViewText.RollAgainLabel(cost), GachaViewText.CanRollAgain(_wallet, cost), TotalGem(cost));
        }

        private void RollAgain()
        {
            _rollAgainPopup.Close();
            if (_lastIsTen) PullTen();
            else PullSingle();
        }

        private void Dismiss()
        {
            _rollAgainPopup.Close();
            _busy = false;
            // Cảnh báo chưa lưu được nằm trên trang roll, bị reveal che; giữ lại tới lúc người chơi về trang này.
            _messageText.text = GachaViewText.StatusMessage(GachaPullStatus.Success, _lastSaved);
            Redraw();
        }

        private void OpenHistory() => _historyPopup.Open(_gacha.History, NameOf);

        private void CloseHistory() => _historyPopup.Close();

        private CurrencyAmount[] CurrentCost() => _lastIsTen ? _banner.TenCost : _banner.SingleCost;

        private static long TotalGem(IReadOnlyList<CurrencyAmount> cost)
        {
            long total = 0;
            foreach (var line in cost)
                if (line.Type == CurrencyType.Gem) total += line.Amount;
            return total;
        }

        private string NameOf(string id) =>
            _characters.TryGet(id, out var character) && !string.IsNullOrEmpty(character.DisplayName) ? character.DisplayName : id;

        private void Redraw()
        {
            _bannerNameText.text = _banner.DisplayName;
            _pityText.text = GachaViewText.PityLine(_banner.SsrPityThreshold, _gacha.GetPity(_banner.Id));
            SetButton(_singleButton, 1, _banner.SingleCost);
            SetButton(_tenButton, 10, _banner.TenCost);
            _historyButton.interactable = !_busy;
            _hub.SetBarHidden(this, _busy);
        }

        private void SetButton(Button button, int count, CurrencyAmount[] cost)
        {
            button.interactable = !_busy && _wallet.CanAfford(cost);
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = GachaViewText.PullLabel(count, cost);
        }
    }
}
