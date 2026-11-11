using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Lưới thẻ nâng cấp (spec UI hub B 5.7): mỗi nâng cấp một thẻ dựng từ thẻ mẫu vào chỗ _cardSlots, nút Mua riêng trên thẻ.
    // Nút Mua "mờ" vẫn bấm được để hiện lý do trong dòng thông báo.
    public class UpgradeTestPanel : MonoBehaviour, IHubPage, IResourcePage
    {
        private const float DimAlpha = 0.45f;

        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _messageText;
        // shortcut: lưới 3×2 cố định cho 6 nâng cấp, thêm cái thứ 7 thì làm cuộn.
        [SerializeField] private RectTransform[] _cardSlots;
        [SerializeField] private UpgradeCardView _cardTemplate;

        private sealed class Row
        {
            public UpgradeDefinition Definition;
            public UpgradeCardView Card;
            public CanvasGroup Group;
        }

        private readonly List<Row> _rows = new();
        private RestaurantService _service;
        private Wallet _wallet;

        public bool IsOpen => _root != null && _root.activeSelf;

        public ResourceSlots Slots => ResourceSlots.Material;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<RestaurantService>();
                _wallet = ServiceLocator.Get<Wallet>();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(UpgradeTestPanel)}: {e.Message}", this);
                _service = null;
                return;
            }

            _openButton.onClick.AddListener(Open);
            _closeButton.onClick.AddListener(Close);
            _wallet.Changed += Refresh;
            if (_service.Upgrades != null) _service.Upgrades.Changed += Refresh;
            _root.SetActive(false);
        }

        private void OnDisable()
        {
            if (_service == null) return;
            _service.Incidents.SetSpawnBlocked(this, false);
            _openButton.onClick.RemoveListener(Open);
            _closeButton.onClick.RemoveListener(Close);
            _wallet.Changed -= Refresh;
            if (_service.Upgrades != null) _service.Upgrades.Changed -= Refresh;
            _service = null;
        }

        private bool HasAllReferences()
        {
            var ok = _openButton != null && _closeButton != null && _root != null && _messageText != null
                     && _cardTemplate != null && _cardSlots != null && _cardSlots.Length > 0 && System.Array.TrueForAll(_cardSlots, s => s != null);
            if (!ok) Debug.LogError($"{nameof(UpgradeTestPanel)}: a serialized reference is not assigned.", this);
            return ok;
        }

        private void Open()
        {
            _service.Incidents.SetSpawnBlocked(this, true);
            _messageText.text = "";
            // Dọn hàng cũ trước khi dựng lại, nếu không mỗi lần mở lại cộng dồn thêm hàng.
            ClearRows();
            if (!_service.IsRunning || _service.Upgrades == null)
            {
                _messageText.text = RestaurantViewText.InvalidConfigLine();
                _root.SetActive(true);
                return;
            }

            foreach (var upgrade in _service.Config.Upgrades.Items)
            {
                if (upgrade == null) continue;
                if (_rows.Count >= _cardSlots.Length)
                {
                    Debug.LogWarning($"{nameof(UpgradeTestPanel)}: more upgrades than card slots, '{upgrade.Id}' is not shown.", this);
                    break;
                }

                var card = Instantiate(_cardTemplate, _cardSlots[_rows.Count], false);
                var cardRect = (RectTransform)card.transform;
                cardRect.anchorMin = Vector2.zero;
                cardRect.anchorMax = Vector2.one;
                cardRect.offsetMin = cardRect.offsetMax = Vector2.zero;
                card.gameObject.SetActive(true);
                var captured = upgrade;
                card.BuyButton.onClick.AddListener(() => Buy(captured));
                card.NameText.text = upgrade.DisplayName;
                _rows.Add(new Row
                {
                    Definition = upgrade,
                    Card = card,
                    Group = card.BuyButton.gameObject.AddComponent<CanvasGroup>(),
                });
            }

            _root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            _service.Incidents.SetSpawnBlocked(this, false);
            _root.SetActive(false);
        }

        private void ClearRows()
        {
            _rows.Clear();
            foreach (var slot in _cardSlots)
            {
                for (var i = slot.childCount - 1; i >= 0; i--)
                {
                    var old = slot.GetChild(i);
                    old.SetParent(null, false);
                    Destroy(old.gameObject);
                }
            }
        }

        private void Buy(UpgradeDefinition upgrade)
        {
            var result = _service.Upgrades.TryBuy(upgrade.Id);
            _messageText.text = RestaurantViewText.UpgradeStatus(result.Status, result.Saved);
            Refresh();
        }

        private void Refresh()
        {
            if (_service == null || !_root.activeSelf) return;

            foreach (var row in _rows)
            {
                var level = _service.Upgrades.GetLevel(row.Definition.Id);
                row.Card.LevelText.text = RestaurantViewText.UpgradeLevel(row.Definition, level);
                row.Card.CostText.text = RestaurantViewText.UpgradeCost(row.Definition, level);
                var cost = RestaurantViewText.NextCost(row.Definition, level);
                row.Group.alpha = cost == null || !_wallet.CanAfford(cost) ? DimAlpha : 1f;
            }
        }
    }
}
