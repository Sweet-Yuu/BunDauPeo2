using System;
using System.Globalization;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Thanh chất lượng của màn quán: nhãn thu nhập, bộ đếm, sao chất lượng và chữ "+N" bay lên từ quầy. Số dư nằm ở ResourceBarView.
    public class RestaurantHud : MonoBehaviour
    {
        private const float FloatSeconds = 1f;
        private const float FloatRise = 90f;
        private const float MessageSeconds = 6f;

        [SerializeField] private Text _incomeText;
        [SerializeField] private Text _counterText;
        [SerializeField] private Image _warningIcon;
        [SerializeField] private Image[] _stars;
        // Nút Thu AFK ở ô thứ ba của cột phải; chỉ hiện khi có Coin AFK đang chờ.
        [SerializeField] private Button _afkButton;
        [SerializeField] private Text _afkText;
        // Thanh báo dưới thanh thông tin: nút Thu AFK biến mất ngay sau khi thu, nên lỗi lưu phải có chỗ riêng để hiện.
        [SerializeField] private GameObject _afkMessageRoot;
        [SerializeField] private Text _afkMessageText;
        [SerializeField] private RectTransform _floatOrigin;
        // Bể 8 đối tượng, dùng vòng tròn: chữ cũ nhất bị ghi đè khi có quá nhiều khoản thu cùng lúc.
        [SerializeField] private Text[] _floatTexts;

        private RestaurantService _service;
        private float[] _floatAge;
        private Vector3[] _floatStart;
        private int _nextFloat;
        private int _shownServed = -1;
        private int _shownAbandoned = -1;
        private double _shownTotal = -1;
        private long _shownAfkCoins = -1;
        private float _messageLeft;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<RestaurantService>();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(RestaurantHud)}: {e.Message}", this);
                _service = null;
                return;
            }

            _floatAge = new float[_floatTexts.Length];
            _floatStart = new Vector3[_floatTexts.Length];
            for (var i = 0; i < _floatTexts.Length; i++)
            {
                _floatAge[i] = FloatSeconds;
                _floatTexts[i].gameObject.SetActive(false);
            }

            _service.ProfileChanged += RedrawIncome;
            _service.Simulation.CustomerPaid += OnCustomerPaid;
            _afkButton.onClick.AddListener(CollectAfk);
            _shownAfkCoins = -1;
            _shownServed = -1;
            _shownAbandoned = -1;
            _shownTotal = -1;
            _messageLeft = 0f;
            _afkMessageRoot.SetActive(false);
            RedrawIncome();
            RedrawStars();
        }

        private void RedrawStars()
        {
            var stars = _service.Quality.Stars;
            for (var i = 0; i < _stars.Length; i++) _stars[i].fillAmount = RestaurantViewText.StarFill(stars, i);
        }

        private void OnDisable()
        {
            if (_service == null) return;
            _service.ProfileChanged -= RedrawIncome;
            _service.Simulation.CustomerPaid -= OnCustomerPaid;
            _afkButton.onClick.RemoveListener(CollectAfk);
            _service = null;
        }

        private void Update()
        {
            if (_service == null) return;

            var simulation = _service.Simulation;
            var total = _service.Quality.Total;
            // Chất lượng đổi khi qua nửa đêm hoặc sau mỗi sự cố, không có event, nên so mỗi khung (rẻ).
            if (simulation.Served != _shownServed || simulation.Abandoned != _shownAbandoned || total != _shownTotal)
            {
                _shownServed = simulation.Served;
                _shownAbandoned = simulation.Abandoned;
                _shownTotal = total;
                _counterText.text = $"{RestaurantViewText.CounterLine(_shownServed, _shownAbandoned)} · {RestaurantViewText.QualityLine(total)}";
                RedrawStars();
            }

            RefreshAfkButton();
            AnimateFloats(Time.unscaledDeltaTime);
            if (_messageLeft > 0f)
            {
                _messageLeft -= Time.unscaledDeltaTime;
                if (_messageLeft <= 0f) _afkMessageRoot.SetActive(false);
            }
        }

        // Hệ số chất lượng đổi theo ngày và sự cố nên số Coin xem trước cũng đổi; so mỗi khung (rẻ).
        private void RefreshAfkButton()
        {
            var afk = _service.Afk;
            var show = afk.PendingTheoretical > 0;
            if (_afkButton.gameObject.activeSelf != show) _afkButton.gameObject.SetActive(show);
            if (!show) return;

            var coins = afk.PreviewCollect();
            if (coins == _shownAfkCoins) return;
            _shownAfkCoins = coins;
            _afkText.text = RestaurantViewText.AfkButton(coins);
        }

        private void CollectAfk()
        {
            var result = _service.Afk.Collect();
            if (result.Saved) return;

            // Coin đã vào ví; lần lưu định kỳ kế tiếp thử lại. Người chơi phải biết là chưa lưu.
            Debug.LogWarning($"{nameof(RestaurantHud)}: AFK collect was not saved; it will be retried on the next save.", this);
            _afkMessageText.text = RestaurantViewText.SaveWarning(false);
            _afkMessageRoot.SetActive(true);
            _messageLeft = MessageSeconds;
        }

        private bool HasAllReferences()
        {
            var ok = _incomeText != null
                     && _counterText != null && _warningIcon != null && _floatOrigin != null
                     && _afkButton != null && _afkText != null && _afkMessageRoot != null && _afkMessageText != null
                     && _stars != null && _stars.Length > 0 && _floatTexts != null && _floatTexts.Length > 0;
            if (!ok) Debug.LogError($"{nameof(RestaurantHud)}: a serialized reference is not assigned.", this);
            return ok;
        }

        private void RedrawIncome()
        {
            var missing = _service.Estimate.MissingRole.HasValue;
            _incomeText.text = _service.IsRunning ? RestaurantViewText.IncomeLine(_service.Estimate) : RestaurantViewText.InvalidConfigLine();
            _warningIcon.enabled = !_service.IsRunning || missing;
        }

        private void OnCustomerPaid(int customerId, long coins)
        {
            var i = _nextFloat;
            _nextFloat = (_nextFloat + 1) % _floatTexts.Length;
            _floatAge[i] = 0f;
            _floatStart[i] = _floatOrigin.position;
            var text = _floatTexts[i];
            text.text = "+" + coins.ToString(CultureInfo.InvariantCulture);
            text.rectTransform.position = _floatStart[i];
            // Nền phòng sáng nên dùng màu cam đậm.
            text.color = new Color(0.92f, 0.50f, 0.05f, 1f);
            text.gameObject.SetActive(true);
        }

        private void AnimateFloats(float dt)
        {
            var rise = FloatRise * transform.lossyScale.x;
            for (var i = 0; i < _floatTexts.Length; i++)
            {
                if (_floatAge[i] >= FloatSeconds) continue;
                _floatAge[i] += dt;
                var t = Mathf.Clamp01(_floatAge[i] / FloatSeconds);
                var text = _floatTexts[i];
                text.rectTransform.position = _floatStart[i] + Vector3.up * (rise * t);
                var color = text.color;
                color.a = 1f - t;
                text.color = color;
                if (t >= 1f) text.gameObject.SetActive(false);
            }
        }
    }
}
