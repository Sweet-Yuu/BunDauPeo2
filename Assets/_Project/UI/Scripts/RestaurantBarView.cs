using System;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Thanh quán thu gọn của Lobby (spec UI hub B 5.5): nút ở mép phải mở/đóng hộp Nhân viên, Nâng cấp, Thu AFK.
    // Trạng thái nhớ qua PlayerSave (RestaurantBarPref); chấm đỏ ở nút báo có Coin AFK chờ khi thanh đóng.
    public class RestaurantBarView : MonoBehaviour
    {
        [SerializeField] private Button _toggle;
        [SerializeField] private Text _toggleText;
        [SerializeField] private GameObject _bar;
        [SerializeField] private GameObject _dot;

        private RestaurantBarPref _pref;
        private RestaurantService _service;
        private bool _open;
        private bool _ready;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            _pref = null;
            _service = null;
            try
            {
                _pref = ServiceLocator.Get<RestaurantBarPref>();
                _service = ServiceLocator.Get<RestaurantService>();
            }
            catch (InvalidOperationException e)
            {
                // Mở không qua Boot: thanh vẫn bấm được, chỉ không nhớ và không có chấm.
                Debug.LogError($"{nameof(RestaurantBarView)}: {e.Message}", this);
            }

            _open = _pref != null && _pref.IsOpen;
            Apply();
            _toggle.onClick.AddListener(Toggle);
            _ready = true;
        }

        private void OnDisable()
        {
            if (!_ready) return;

            _ready = false;
            _toggle.onClick.RemoveListener(Toggle);
        }

        private bool HasAllReferences()
        {
            var ok = _toggle != null && _toggleText != null && _bar != null && _dot != null;
            if (!ok) Debug.LogError($"{nameof(RestaurantBarView)}: a serialized reference is not assigned.", this);
            return ok;
        }

        private void Update()
        {
            if (!_ready) return;

            var afk = _service?.Afk;
            var show = !_open && afk != null && afk.PendingTheoretical > 0 && afk.PreviewCollect() > 0;
            if (_dot.activeSelf != show) _dot.SetActive(show);
        }

        private void Toggle()
        {
            _open = !_open;
            Apply();
            // Cờ giao diện: lưu lỗi thì thanh vẫn đổi trên màn, lần lưu sau ghi lại (spec 5.5).
            if (_pref != null && !_pref.Set(_open))
                Debug.LogWarning($"{nameof(RestaurantBarView)}: the bar state was not saved; it will be retried on the next save.", this);
        }

        private void Apply()
        {
            _bar.SetActive(_open);
            _toggleText.text = _open ? ">" : "<";
            if (_open) _dot.SetActive(false);
        }
    }
}
