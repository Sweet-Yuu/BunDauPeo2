using System;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Hộp mạng (spec 5.4, ảnh mô phỏng bản 1 ở mục 10): Không kết nối được, Mất kết nối, Tài khoản đang dùng ở máy khác.
    // Một hộp giữ cỡ, đổi chữ; nền mờ kín màn chặn mọi chạm, vẽ trên mọi scene, thanh dưới, nút Thông báo và panel DEV.
    // Dựng bằng code như DevOverlay để có trên mọi scene mà không builder nào phải nhớ.
    public static class OnlineOverlay
    {
        public const int SortingOrder = 1100;
        private static readonly Vector2 BoxSize = new(900, 420);
        private static readonly Vector2 ButtonSize = new(340, 110);
        private static readonly Color Dim = new(0, 0, 0, 0.6f);
        private static readonly Color Panel = new(0.13f, 0.16f, 0.22f);
        private static readonly Color Gold = new(1f, 0.85f, 0.4f);
        private static readonly Color ButtonColor = new(0.22f, 0.55f, 0.32f);

        private static GameObject _root;
        private static GameObject _dim;
        private static Text _title;
        private static Text _body;
        private static Text _buttonLabel;
        private static GameObject _button;
        private static Action _action;
        private static IGameStarter _watched;
        private static CommandGate _gate;
        // Hộp đang chờ Thử lại một lệnh (Reset, Xoá tài khoản): sự kiện đẩy save chung không được đổi nút hay đóng hộp.
        private static bool _forCommand;

        // Tắt domain reload thì static còn nguyên sang lần Play sau.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _root = null;
            _dim = null;
            _action = null;
            _watched = null;
            _gate = null;
            _forCommand = false;
            _button = null;
        }

        public static bool IsShown => _dim != null && _dim.activeSelf;

        public static void ShowConnectFailed(Action retry) => Show("online.connect_failed_title", "online.connect_failed_body", "online.retry", retry);

        // Thử lại một lệnh đang dở (Reset, Xoá tài khoản); Mất kết nối của đẩy save dùng OnProblem.
        public static void ShowLost(Action retry)
        {
            Show("online.lost_title", "online.lost_body", "online.retry", retry);
            _forCommand = true;
        }

        // Máy dưới MinProtocolVersion: không có nút vì không có đường đi tiếp (spec con 7.2).
        public static void ShowTooOld() => Show("online.too_old_title", "online.too_old_body", null, null);

        public static void ShowOtherDevice(Action toTitle) => Show("online.other_device_title", "online.other_device_body", "online.to_title", toTitle);

        public static void Hide()
        {
            _action = null;
            _forCommand = false;
            if (_dim != null) _dim.SetActive(false);
        }

        // Hộp giữa game: starter sống suốt app (qua Shutdown), nên nghe nó một lần; Tiêu đề gọi trước khi dựng session.
        public static void Watch(IGameStarter starter)
        {
            if (starter == null || starter == _watched) return;
            if (_watched != null)
            {
                _watched.ConnectionProblem -= OnProblem;
                _watched.ConnectionRestored -= OnRestored;
                _watched.NoState -= ToTitle;
            }

            _watched = starter;
            starter.ConnectionProblem += OnProblem;
            starter.ConnectionRestored += OnRestored;
            starter.NoState += ToTitle;
        }

        // Cổng lệnh sống suốt app: lệnh mất kết nối, máy khác, bản cũ đều hiện hộp từ đây.
        public static void Watch(CommandGate gate)
        {
            if (gate == null || gate == _gate) return;
            if (_gate != null)
            {
                _gate.Lost -= OnCommandLost;
                _gate.OtherDevice -= OnCommandOtherDevice;
                _gate.TooOld -= OnCommandTooOld;
            }

            _gate = gate;
            gate.Lost += OnCommandLost;
            gate.OtherDevice += OnCommandOtherDevice;
            gate.TooOld += OnCommandTooOld;
        }

        private static void OnCommandLost(Action retry) => ShowLost(() =>
        {
            Hide();
            retry();
        });

        private static void OnCommandOtherDevice() => ShowOtherDevice(ToTitle);

        private static void OnCommandTooOld()
        {
            // Đẩy save tiếp cũng chỉ bị từ chối; dừng cho khỏi gửi vô ích.
            try
            {
                ServiceLocator.Get<CloudSync>().Stop();
            }
            catch (InvalidOperationException)
            {
                // Chưa có session (hộp hiện ở Tiêu đề): không có gì để dừng.
            }

            ShowTooOld();
        }

        private static void OnProblem(OnlineStatus status)
        {
            if (status == OnlineStatus.OtherDevice) ShowOtherDevice(ToTitle);
            // Hộp đóng khi lượt đẩy thành công (ConnectionRestored); lỗi nữa thì hộp ở lại để bấm tiếp.
            else if (!_forCommand) Show("online.lost_title", "online.lost_body", "online.retry", () => _ = ServiceLocator.Get<CloudSync>().FlushAsync());
        }

        private static void OnRestored()
        {
            if (!_forCommand) Hide();
        }

        // Bỏ thay đổi chưa đẩy: Tiêu đề đăng nhập lại và tải bản trên server (spec 5.4, máy đăng nhập sau thắng).
        private static void ToTitle()
        {
            Hide();
            _watched?.Shutdown();
            SceneManager.LoadScene(TitleView.TitleScene);
        }

        private static void Show(string titleKey, string bodyKey, string buttonKey, Action action)
        {
            Build();
            _title.text = UiText.Get(titleKey);
            _body.text = UiText.Get(bodyKey);
            // Hộp không nút (Cần cập nhật) dùng cả phần dưới cho chữ: bản tiếng Anh dài hơn hai dòng.
            _button.SetActive(buttonKey != null);
            if (buttonKey != null) _buttonLabel.text = UiText.Get(buttonKey);
            _body.rectTransform.sizeDelta = new Vector2(820, buttonKey != null ? 140 : 240);
            _body.rectTransform.anchoredPosition = new Vector2(0, buttonKey != null ? -195 : -245);
            _action = action;
            _forCommand = false;
            _dim.SetActive(true);
        }

        private static void OnButton()
        {
            var action = _action;
            action?.Invoke();
        }

        private static void Build()
        {
            if (_root != null) return;

            _root = new GameObject("OnlineOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(_root);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var dim = NewRect("OnlineDim", _root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddImage(dim, Dim, raycast: true);
            _dim = dim.gameObject;

            var center = new Vector2(0.5f, 0.5f);
            var box = NewRect("OnlineBox_PH_900x420", dim, center, center, Vector2.zero, BoxSize);
            AddImage(box, Panel, raycast: true);

            var top = new Vector2(0.5f, 1f);
            _title = NewText("OnlineTitle", box, font, 52, Gold, top, new Vector2(0, -75), new Vector2(840, 90));
            _title.fontStyle = FontStyle.Bold;
            _body = NewText("OnlineBody", box, font, 36, Color.white, top, new Vector2(0, -195), new Vector2(820, 140));

            var bottom = new Vector2(0.5f, 0f);
            var button = NewRect("OnlineRetryBtn_PH_340x110", box, bottom, bottom, new Vector2(0, 40 + ButtonSize.y / 2), ButtonSize);
            var image = AddImage(button, ButtonColor, raycast: true);
            var click = button.gameObject.AddComponent<Button>();
            click.targetGraphic = image;
            click.onClick.AddListener(OnButton);
            _button = button.gameObject;
            _buttonLabel = NewText("Label", button, font, 40, Color.white, center, Vector2.zero, ButtonSize);

            _dim.SetActive(false);
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

        private static Image AddImage(RectTransform rect, Color color, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static Text NewText(string name, Transform parent, Font font, int size, Color color, Vector2 anchor, Vector2 position, Vector2 box)
        {
            var text = NewRect(name, parent, anchor, anchor, position, box).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }
    }
}
