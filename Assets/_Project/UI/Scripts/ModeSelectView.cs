using System;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Màn Chọn chế độ (spec UI hub C 5.2): thẻ Chương chính mở Bản đồ, bốn ô khoá chỉ hiện "Sắp có".
    public class ModeSelectView : MonoBehaviour
    {
        [SerializeField] private HubNav _hub;
        [SerializeField] private StageMapView _map;
        [SerializeField] private Button _mainButton;
        [SerializeField] private Text _mainSubText;
        // Thứ tự: Khó, Sự kiện, Thử thách, Đấu trường.
        [SerializeField] private Button[] _lockedButtons;

        private CombatService _service;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<CombatService>();
            }
            catch (InvalidOperationException)
            {
                // Mở không qua Boot: chữ phụ để trống.
                Debug.LogError($"{nameof(ModeSelectView)}: {nameof(CombatService)} is not registered.", this);
                _service = null;
            }

            _mainButton.onClick.AddListener(_map.Open);
            foreach (var button in _lockedButtons) button.onClick.AddListener(_hub.ShowComingSoon);
            // Màn Truyện clear ngay trong Bản đồ nên "Màn tiếp" đổi khi Bản đồ còn mở.
            if (_service != null) _service.Progress.Changed += RefreshSub;
            RefreshSub();
        }

        private void OnDisable()
        {
            if (_service != null) _service.Progress.Changed -= RefreshSub;
            _service = null;
            if (_mainButton != null && _map != null) _mainButton.onClick.RemoveListener(_map.Open);
            if (_lockedButtons == null || _hub == null) return;
            foreach (var button in _lockedButtons)
                if (button != null) button.onClick.RemoveListener(_hub.ShowComingSoon);
        }

        private bool HasAllReferences()
        {
            var ok = _hub != null && _map != null && _mainButton != null && _mainSubText != null
                     && _lockedButtons != null && Array.TrueForAll(_lockedButtons, b => b != null);
            if (!ok) Debug.LogError($"{nameof(ModeSelectView)}: references are not assigned.", this);
            return ok;
        }

        private void RefreshSub()
        {
            _mainSubText.text = SubText(_service);
        }

        private static string SubText(CombatService service)
        {
            if (service == null || !service.IsRunning) return "";

            var stages = service.Config.Stages;
            var next = StageMapView.NextStageIndex(service.Progress, stages);
            return next < 0 ? UiText.Get("stages.mode_main_done") : UiText.Format("stages.mode_main_next", CombatViewText.NodeLabel(stages.Items[next]));
        }
    }
}
