using System;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Bấm thẻ sao ở HUD mở popup này (spec 5.4). Chỉ đọc RestaurantQuality; số vẽ lại mỗi khung khi đang mở.
    public class QualityPopup : MonoBehaviour, IHubPage
    {
        private const int Days = 7;

        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _headlineText;
        [SerializeField] private Text _attendanceText;
        // Ô ngày từ trái sang phải: 7 ngày trước … hôm qua.
        [SerializeField] private Image[] _dayFills;
        [SerializeField] private Text[] _dayLabels;
        [SerializeField] private Text _todayText;
        [SerializeField] private Text _incidentText;
        [SerializeField] private Text _breakdownText;
        [SerializeField] private Text _factorText;

        private RestaurantService _service;

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
                Debug.LogError($"{nameof(QualityPopup)}: {e.Message}", this);
                _service = null;
                return;
            }

            for (var i = 0; i < Days; i++) _dayLabels[i].text = RestaurantViewText.DayCellLabel(Days - i);
            _openButton.onClick.AddListener(Open);
            _closeButton.onClick.AddListener(Close);
            _root.SetActive(false);
        }

        private void OnDisable()
        {
            if (_service == null) return;
            _service.Incidents.SetSpawnBlocked(this, false);
            _openButton.onClick.RemoveListener(Open);
            _closeButton.onClick.RemoveListener(Close);
            _service = null;
        }

        private bool HasAllReferences()
        {
            var ok = _openButton != null && _closeButton != null && _root != null && _headlineText != null
                     && _attendanceText != null && _todayText != null && _incidentText != null
                     && _breakdownText != null && _factorText != null
                     && _dayFills != null && _dayFills.Length == Days && _dayLabels != null && _dayLabels.Length == Days;
            if (ok)
            {
                foreach (var fill in _dayFills) ok &= fill != null;
                foreach (var label in _dayLabels) ok &= label != null;
            }

            if (!ok) Debug.LogError($"{nameof(QualityPopup)}: a serialized reference is not assigned.", this);
            return ok;
        }

        private void Open()
        {
            _service.Incidents.SetSpawnBlocked(this, true);
            _root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            _service.Incidents.SetSpawnBlocked(this, false);
            _root.SetActive(false);
        }

        private void Update()
        {
            if (_service != null && _root.activeSelf) Refresh();
        }

        private void Refresh()
        {
            var quality = _service.Quality;
            var config = _service.Config;
            _headlineText.text = RestaurantViewText.QualityLine(quality.Total);
            _attendanceText.text = RestaurantViewText.AttendanceLine(quality.Attendance, config.AttendanceMax);

            // DayCredits[0] = hôm qua; ô cuối bên phải là hôm qua.
            var credits = quality.DayCredits;
            for (var i = 0; i < Days; i++) _dayFills[i].fillAmount = RestaurantViewText.DayCellFill(credits[Days - 1 - i]);

            _todayText.text = RestaurantViewText.TodayLine(quality.TodaySeconds, config.DailyGoalSeconds);
            _incidentText.text = RestaurantViewText.IncidentPointsLine(quality.IncidentPoints, config.IncidentMax);
            _breakdownText.text = RestaurantViewText.IncidentBreakdown(quality.IncidentScores);
            _factorText.text = RestaurantViewText.FactorLine(quality.AfkFactor);
        }
    }
}
