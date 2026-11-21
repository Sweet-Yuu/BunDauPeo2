using System.Collections.Generic;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Màn chờ của mọi lệnh server do người chơi bấm (spec con 7.1): lớp trong suốt kín màn chặn mọi chạm kể cả thanh dưới;
    // sau 0,5 giây (đồng hồ thật) mới hiện ô xoay và chữ, nên lệnh trả lời nhanh không nháy gì.
    // Dựng bằng code như OnlineOverlay để có trên mọi scene; vẽ dưới OnlineOverlay để hộp mạng bấm được.
    public static class WaitOverlay
    {
        public const int SortingOrder = OnlineOverlay.SortingOrder - 1;
        public const float ShowAfterSeconds = 0.5f;
        private static readonly Vector2 SpinnerSize = new(64, 64);
        private static readonly Vector2 LabelSize = new(400, 60);

        // Bật/tắt theo chủ (code-style L-015): nhiều nguồn cùng chờ thì lớp chỉ tắt khi mọi chủ đã nhả.
        private static readonly HashSet<object> Owners = new();
        private static GameObject _root;
        private static GameObject _blocker;
        private static GameObject _spinner;
        private static GameObject _label;
        private static CommandGate _watched;

        // Tắt domain reload thì static còn nguyên sang lần Play sau.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Owners.Clear();
            _root = null;
            _blocker = null;
            _spinner = null;
            _label = null;
            _watched = null;
        }

        public static bool IsWaiting => Owners.Count > 0;
        public static bool IsBlocking => _blocker != null && _blocker.activeSelf;
        public static bool IsSpinnerShown => _spinner != null && _spinner.activeSelf;

        // Cổng sống suốt app (OnlineStartup tạo một lần); gọi lại với cùng cổng thì bỏ qua.
        public static void Watch(CommandGate gate)
        {
            if (gate == null || gate == _watched) return;
            if (_watched != null) _watched.WaitingChanged -= OnWaiting;
            _watched = gate;
            gate.WaitingChanged += OnWaiting;
        }

        public static void SetWaiting(object owner, bool waiting)
        {
            if (waiting) Owners.Add(owner);
            else Owners.Remove(owner);
            Build();
            _blocker.SetActive(IsWaiting);
            _spinner.SetActive(false);
            _label.SetActive(false);
            var driver = _root.GetComponent<WaitDriver>();
            driver.enabled = IsWaiting;
            if (IsWaiting) driver.Restart();
        }

        private static void OnWaiting(bool waiting) => SetWaiting(_watched, waiting);

        private static void Build()
        {
            if (_root != null) return;

            _root = new GameObject("WaitOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(WaitDriver));
            Object.DontDestroyOnLoad(_root);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var blocker = NewRect("WaitBlocker", _root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var blockerImage = blocker.gameObject.AddComponent<Image>();
            blockerImage.color = Color.clear;
            blockerImage.raycastTarget = true;
            _blocker = blocker.gameObject;

            var center = new Vector2(0.5f, 0.5f);
            var spinner = NewRect("WaitSpinner_PH_64x64", blocker, center, center, new Vector2(0, 20), SpinnerSize);
            var spinnerImage = spinner.gameObject.AddComponent<Image>();
            spinnerImage.color = Color.white;
            spinnerImage.raycastTarget = false;
            _spinner = spinner.gameObject;

            var label = NewRect("WaitLabel_PH_400x60", blocker, center, center, new Vector2(0, -50), LabelSize);
            var text = label.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 36;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.9f);
            outline.effectDistance = new Vector2(2, -2);
            _label = label.gameObject;

            var driver = _root.GetComponent<WaitDriver>();
            driver.Init(_spinner, _label, text);
            driver.enabled = false;
            _blocker.SetActive(false);
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // Đếm 0,5 giây thật và xoay ô; chỉ chạy khi đang chờ.
        private sealed class WaitDriver : MonoBehaviour
        {
            private GameObject _spinner;
            private GameObject _label;
            private Text _text;
            private float _start;

            public void Init(GameObject spinner, GameObject label, Text text)
            {
                _spinner = spinner;
                _label = label;
                _text = text;
            }

            public void Restart() => _start = Time.unscaledTime;

            private void Update()
            {
                if (_spinner == null) return;
                if (Time.unscaledTime - _start < ShowAfterSeconds) return;
                if (!_spinner.activeSelf)
                {
                    // Chữ đọc lúc hiện, không lúc dựng: đổi ngôn ngữ giữa game vẫn đúng.
                    _text.text = UiText.Get("online.sending");
                    _spinner.SetActive(true);
                    _label.SetActive(true);
                }

                _spinner.transform.Rotate(0f, 0f, -360f * Time.unscaledDeltaTime);
            }
        }
    }
}
