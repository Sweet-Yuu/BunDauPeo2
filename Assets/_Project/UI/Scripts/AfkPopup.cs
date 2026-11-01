using System;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Hiện khi vào màn quán sau một khoảng vắng đủ dài (RestaurantView gọi Show). Số liệu vẽ lại mỗi khung từ
    // AfkService, nên nút Thu AFK ở HUD hay Reset giữa chừng đều không để lại số cũ trên popup.
    public class AfkPopup : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _summaryText;
        [SerializeField] private Text _tipText;
        [SerializeField] private Text _fullText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _collectButton;
        [SerializeField] private Button _laterButton;
        [SerializeField] private Text _laterLabel;

        private RestaurantService _service;
        private double _absenceSeconds;
        // Thu xong mà chưa lưu được: popup ở lại để báo, nên không tự đóng dù Coin chờ đã về 0.
        private bool _collectedUnsaved;
        private long _collectedCoins;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<RestaurantService>();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(AfkPopup)}: {e.Message}", this);
                _service = null;
                return;
            }

            _collectButton.onClick.AddListener(Collect);
            _laterButton.onClick.AddListener(Close);
            _root.SetActive(false);
        }

        private void OnDisable()
        {
            if (_service == null) return;
            if (IsOpen) _service.Incidents.SetSpawnAllowed(true);
            _collectButton.onClick.RemoveListener(Collect);
            _laterButton.onClick.RemoveListener(Close);
            _service = null;
        }

        private bool HasAllReferences()
        {
            var ok = _root != null && _summaryText != null && _tipText != null && _fullText != null && _messageText != null
                     && _collectButton != null && _laterButton != null && _laterLabel != null;
            if (!ok) Debug.LogError($"{nameof(AfkPopup)}: a serialized reference is not assigned.", this);
            return ok;
        }

        public void Show(double absenceSeconds)
        {
            if (_service == null) return;

            _absenceSeconds = absenceSeconds;
            _collectedUnsaved = false;
            _tipText.text = RestaurantViewText.TipLine(_service.Config.DailyGoalSeconds);
            // "Chưa lưu được" của lần mở trước không được dính sang lần này.
            _messageText.text = "";
            _service.Incidents.SetSpawnAllowed(false);
            _root.SetActive(true);
            Refresh();
        }

        private void Update()
        {
            if (_service == null || !IsOpen) return;

            // Hết Coin chờ (thu ở nơi khác, hoặc Reset save) thì popup không còn gì để nói.
            if (!_collectedUnsaved && _service.Afk.PendingTheoretical <= 0)
            {
                Close();
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            var afk = _service.Afk;
            var quality = _service.Quality;
            _summaryText.text = _collectedUnsaved
                ? RestaurantViewText.AfkCollectedLine(_collectedCoins)
                : RestaurantViewText.AfkSummary(_absenceSeconds, afk.PendingTheoretical, quality.Total, quality.AfkFactor, afk.PreviewCollect());
            _fullText.text = RestaurantViewText.FullCapLine(afk.IsFull);
            _collectButton.gameObject.SetActive(!_collectedUnsaved);
            _laterLabel.text = _collectedUnsaved ? "Đóng" : "Để sau";
        }

        private void Collect()
        {
            var result = _service.Afk.Collect();
            if (result.Saved)
            {
                Close();
                return;
            }

            // Coin đã vào ví và còn trong bộ nhớ; lần lưu định kỳ kế tiếp sẽ thử lại.
            _collectedUnsaved = true;
            _collectedCoins = result.Coins;
            _messageText.text = RestaurantViewText.SaveWarning(false);
            Refresh();
        }

        private void Close()
        {
            _service.Incidents.SetSpawnAllowed(true);
            _root.SetActive(false);
        }
    }
}
