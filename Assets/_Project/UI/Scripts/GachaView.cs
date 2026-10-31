using System;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Màn tối thiểu để thử vòng quay; hub thật ở spec UI hub. Giữ mỏng: chọn chữ/màu nằm ở GachaViewText.
    public class GachaView : MonoBehaviour
    {
        [Serializable]
        private struct Slot
        {
            public Image Background;
            public Text Label;
        }

        [SerializeField] private GachaBannerDefinition _banner;
        [SerializeField] private Text _gemText;
        [SerializeField] private Text _bannerNameText;
        [SerializeField] private Text _pityText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _singleButton;
        [SerializeField] private Button _tenButton;
        [SerializeField] private Slot[] _slots;

        private Wallet _wallet;
        private Roster _roster;
        private GachaService _gacha;
        private bool _active;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _wallet = ServiceLocator.Get<Wallet>();
                _roster = ServiceLocator.Get<Roster>();
                _gacha = ServiceLocator.Get<GachaService>();
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
            _active = true;
            Redraw();
        }

        private void OnDisable()
        {
            if (!_active) return;
            _active = false;
            _wallet.Changed -= Redraw;
            _roster.Changed -= Redraw;
            _singleButton.onClick.RemoveListener(PullSingle);
            _tenButton.onClick.RemoveListener(PullTen);
        }

        private bool HasAllReferences()
        {
            var ok = _banner != null && _gemText != null && _bannerNameText != null && _pityText != null
                     && _messageText != null && _singleButton != null && _tenButton != null && _slots != null;
            if (ok)
            {
                foreach (var slot in _slots)
                    if (slot.Background == null || slot.Label == null) ok = false;
            }

            if (!ok) Debug.LogError($"{nameof(GachaView)}: a serialized reference is not assigned.", this);
            return ok;
        }

        private void PullSingle() => Show(_gacha.PullSingle(_banner.Id));

        private void PullTen() => Show(_gacha.PullTen(_banner.Id));

        private void Show(GachaPullResult result)
        {
            _messageText.text = GachaViewText.StatusMessage(result.Status, result.Saved);
            if (result.Status == GachaPullStatus.Success)
            {
                for (var i = 0; i < _slots.Length; i++)
                {
                    var used = i < result.Entries.Count;
                    _slots[i].Background.gameObject.SetActive(used);
                    if (!used) continue;

                    var entry = result.Entries[i];
                    var character = entry.Character;
                    var name = string.IsNullOrEmpty(character.DisplayName) ? character.Id : character.DisplayName;
                    _slots[i].Background.color = GachaViewText.RarityColor(character.Rarity);
                    _slots[i].Label.text = $"{character.Rarity}  {name}\n{GachaViewText.Describe(entry)}";
                }
            }

            // Vẽ lại từ trạng thái sau khi lượt quay trả về: sự kiện giữa chừng thấy ví đã trừ nhưng pity chưa ghi.
            Redraw();
        }

        private void Redraw()
        {
            _gemText.text = $"Gem: {_wallet.Get(CurrencyType.Gem)}";
            _bannerNameText.text = _banner.DisplayName;
            _pityText.text = GachaViewText.PityLine(_banner.SsrPityThreshold, _gacha.GetPity(_banner.Id));
            SetButton(_singleButton, 1, _banner.SingleCost);
            SetButton(_tenButton, 10, _banner.TenCost);
        }

        private void SetButton(Button button, int count, CurrencyAmount[] cost)
        {
            button.interactable = _wallet.CanAfford(cost);
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = GachaViewText.PullLabel(count, cost);
        }
    }
}
