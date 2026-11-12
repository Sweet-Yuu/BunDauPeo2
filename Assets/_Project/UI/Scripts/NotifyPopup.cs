using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Hộp thả xuống của nút Thông báo (spec UI hub C 5.3): tối đa 5 dòng theo ảnh chụp lúc mở. Component nằm trên root của hộp nên
    // IsOpen = root bật; chỉ có Open/Close, không có Awake phụ thuộc scene. Là hộp nổi (IHubOverlay): không đổi bố cục Lobby.
    public class NotifyPopup : MonoBehaviour, IHubOverlay
    {
        [SerializeField] private Button _dim;
        // 5 dòng theo thứ tự NotifyKind; dòng thừa tắt.
        [SerializeField] private Button[] _rows;
        [SerializeField] private Text[] _rowTexts;
        [SerializeField] private GameObject _emptyText;

        private readonly List<NotifyKind> _kinds = new();
        private RestaurantService _service;
        private bool _wired;

        // Bấm một dòng: hộp đã đóng trước khi phát.
        public event Action<NotifyKind> RowClicked;

        public bool IsOpen => gameObject.activeSelf;

        public void Open(NotifySnapshot snapshot)
        {
            if (!HasAllReferences()) return;

            Wire();
            _kinds.Clear();
            _kinds.AddRange(NotifyRule.Rows(snapshot));
            gameObject.SetActive(true);
            for (var i = 0; i < _rows.Length; i++)
            {
                var on = i < _kinds.Count;
                _rows[i].gameObject.SetActive(on);
                if (on) _rowTexts[i].text = NotifyRule.Text(_kinds[i], snapshot);
            }

            _emptyText.SetActive(_kinds.Count == 0);
        }

        public void Close() => gameObject.SetActive(false);

        // Panel che quán nên không sinh sự cố mới trong lúc hộp mở (như trang của quán; cờ nhiều chủ, L-015).
        private void OnEnable()
        {
            try
            {
                _service = ServiceLocator.Get<RestaurantService>();
                _service.Incidents.SetSpawnBlocked(this, true);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(NotifyPopup)}: {e.Message}", this);
                _service = null;
            }
        }

        private void OnDisable()
        {
            _service?.Incidents.SetSpawnBlocked(this, false);
            _service = null;
        }

        private bool HasAllReferences()
        {
            var ok = _dim != null && _emptyText != null && _rows != null && _rows.Length == Enum.GetValues(typeof(NotifyKind)).Length
                     && Array.TrueForAll(_rows, r => r != null) && _rowTexts != null && _rowTexts.Length == _rows.Length && Array.TrueForAll(_rowTexts, t => t != null);
            if (!ok) Debug.LogError($"{nameof(NotifyPopup)}: references are not assigned.", this);
            return ok;
        }

        // Một lần, lúc mở lần đầu (root còn tắt tới lúc đó).
        private void Wire()
        {
            if (_wired) return;

            _wired = true;
            _dim.onClick.AddListener(Close);
            for (var i = 0; i < _rows.Length; i++)
            {
                var index = i;
                _rows[i].onClick.AddListener(() => OnRowClicked(index));
            }
        }

        private void OnRowClicked(int index)
        {
            if (index >= _kinds.Count) return;

            var kind = _kinds[index];
            Close();
            RowClicked?.Invoke(kind);
        }
    }
}
