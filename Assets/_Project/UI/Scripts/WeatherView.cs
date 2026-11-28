using System;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Thẻ "Mưa · còn m:ss" và lớp phủ xanh của Lobby (spec Thời tiết 5). Chỉ hiện khi mưa; chữ chỉ ghi lại khi số giây đổi (L-044).
    public class WeatherView : MonoBehaviour
    {
        [SerializeField] private GameObject _badge;
        [SerializeField] private Text _badgeText;
        [SerializeField] private GameObject _tint;

        private WeatherService _weather;
        private long _shownSeconds = -1;

        private void OnEnable()
        {
            if (_weather != null) return;

            try
            {
                Init(ServiceLocator.Get<RestaurantService>().Weather);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(WeatherView)}: {e.Message}", this);
            }
        }

        public void Init(WeatherService weather)
        {
            _weather = weather;
            _shownSeconds = -1;
            Refresh();
        }

        private void Update() => Refresh();

        public void Refresh()
        {
            if (_weather == null) return;

            var raining = _weather.IsRaining;
            if (_badge.activeSelf != raining) _badge.SetActive(raining);
            if (_tint.activeSelf != raining) _tint.SetActive(raining);
            if (!raining)
            {
                _shownSeconds = -1;
                return;
            }

            var seconds = (long)Math.Ceiling(_weather.RainRemainingSeconds);
            if (seconds == _shownSeconds) return;

            _shownSeconds = seconds;
            _badgeText.text = UiText.Format("weather.rain", CombatViewText.Clock(seconds));
        }
    }
}
