#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Dòng trạng thái của mục Thời tiết trong panel DEV: vẽ lại khi panel mở (OnEnable) và mỗi giây khi còn mở. Không [SerializeField] (xem DevOverlay).
    public class DevWeatherStatus : MonoBehaviour
    {
        private Text _text;
        private Func<string> _source;
        private float _next;

        public void Init(Text text, Func<string> source)
        {
            _text = text;
            _source = source;
        }

        private void OnEnable() => Refresh();

        private void Update()
        {
            if (Time.unscaledTime >= _next) Refresh();
        }

        public void Refresh()
        {
            _next = Time.unscaledTime + 1f;
            if (_text == null) return;

            try
            {
                _text.text = _source();
            }
            catch (InvalidOperationException)
            {
                // Chưa có service (Boot lỗi / vừa Về Tiêu đề): giữ dòng cũ.
            }
        }
    }
}
#endif
