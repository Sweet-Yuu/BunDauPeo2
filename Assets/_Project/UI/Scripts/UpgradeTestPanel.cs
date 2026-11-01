using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Bản thử: một cột nút, mỗi nâng cấp một nút. UI nâng cấp thật thay view này mà không đụng UpgradeService.
    // Nút "mờ" vẫn bấm được để hiện lý do trong dòng thông báo.
    public class UpgradeTestPanel : MonoBehaviour
    {
        private const float DimAlpha = 0.45f;

        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _messageText;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Button _rowTemplate;

        private sealed class Row
        {
            public UpgradeDefinition Definition;
            public Text Label;
            public CanvasGroup Group;
        }

        private readonly List<Row> _rows = new();
        private RestaurantService _service;
        private Wallet _wallet;

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
            _service.Incidents.SetSpawnAllowed(true);
            _openButton.onClick.RemoveListener(Open);
            _closeButton.onClick.RemoveListener(Close);
            _wallet.Changed -= Refresh;
            if (_service.Upgrades != null) _service.Upgrades.Changed -= Refresh;
            _service = null;
        }

        private bool HasAllReferences()
        {
            var ok = _openButton != null && _closeButton != null && _root != null && _messageText != null
                     && _content != null && _rowTemplate != null;
            if (!ok) Debug.LogError($"{nameof(UpgradeTestPanel)}: a serialized reference is not assigned.", this);
            return ok;
        }

        private void Open()
        {
            _service.Incidents.SetSpawnAllowed(false);
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
                var button = Instantiate(_rowTemplate, _content, false);
                button.gameObject.SetActive(true);
                var captured = upgrade;
                button.onClick.AddListener(() => Buy(captured));
                _rows.Add(new Row
                {
                    Definition = upgrade,
                    Label = button.GetComponentInChildren<Text>(),
                    Group = button.gameObject.AddComponent<CanvasGroup>(),
                });
            }

            _root.SetActive(true);
            Refresh();
        }

        private void Close()
        {
            _service.Incidents.SetSpawnAllowed(true);
            _root.SetActive(false);
        }

        private void ClearRows()
        {
            _rows.Clear();
            for (var i = _content.childCount - 1; i >= 0; i--)
            {
                var old = _content.GetChild(i);
                old.SetParent(null, false);
                Destroy(old.gameObject);
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
                row.Label.text = RestaurantViewText.UpgradeButton(row.Definition, level);
                var cost = RestaurantViewText.NextCost(row.Definition, level);
                row.Group.alpha = cost == null || !_wallet.CanAfford(cost) ? DimAlpha : 1f;
            }
        }
    }
}
