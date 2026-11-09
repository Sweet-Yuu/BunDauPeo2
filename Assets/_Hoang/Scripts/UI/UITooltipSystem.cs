using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hoang.Remake
{
    public sealed class UITooltipSystem : MonoBehaviour
    {
        private static UITooltipSystem _instance;
        private RectTransform _canvasRect;
        private RectTransform _panel;
        private Text _category;
        private Text _title;
        private Text _body;
        private Text _status;
        private Text _footer;
        private Vector2 _lastPosition;
        private bool _layoutDirty;

        public static void ShowTooltip(string category, string title, string body, string status, string footer,
            Vector2? screenPosition = null)
        {
            EnsureInstance();
            _instance._category.text = category ?? "";
            _instance._title.text = title ?? "";
            _instance._body.text = body ?? "";
            _instance._status.text = status ?? "";
            _instance._footer.text = footer ?? "";
            _instance._status.color = (status ?? "").Contains("LOCKED") || (status ?? "").Contains("NO POWER")
                ? new Color(1, 0.62f, 0.42f) : new Color(0.64f, 0.94f, 0.71f);
            _instance._lastPosition = screenPosition ?? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            _instance._layoutDirty = true;
            _instance._panel.gameObject.SetActive(true);
            _instance.Reposition();
        }

        public static void ShowTooltip(string title, string body) => ShowTooltip("RESTAURANT", title, body, "", "");

        public static void HideTooltip()
        {
            if (_instance != null && _instance._panel != null) _instance._panel.gameObject.SetActive(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        private static void EnsureInstance()
        {
            if (_instance != null) return;
            var go = new GameObject("RestaurantTooltipCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1200;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _instance = go.AddComponent<UITooltipSystem>();
            _instance._canvasRect = (RectTransform)go.transform;
            _instance._panel = RemakeUI.Panel(go.transform, "Tooltip", new Color(0.08f, 0.11f, 0.16f, 0.98f),
                Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(420, 100));
            _instance._panel.GetComponent<Image>().raycastTarget = false;
            var layout = _instance._panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 14, 14);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = _instance._panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _instance._category = _instance.AddLine("Category", 16, new Color(0.55f, 0.85f, 0.95f), FontStyle.Bold);
            _instance._title = _instance.AddLine("Title", 24, Color.white, FontStyle.Bold);
            _instance._body = _instance.AddLine("Body", 19, new Color(0.9f, 0.92f, 0.95f), FontStyle.Normal);
            _instance._status = _instance.AddLine("Status", 18, Color.white, FontStyle.Bold);
            _instance._footer = _instance.AddLine("Footer", 16, new Color(0.71f, 0.75f, 0.81f), FontStyle.Italic);
            _instance._panel.gameObject.SetActive(false);
        }

        private Text AddLine(string name, int size, Color color, FontStyle style)
        {
            var line = RemakeUI.Label(_panel, name, "", size, TextAnchor.UpperLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            line.color = color;
            line.fontStyle = style;
            line.resizeTextForBestFit = false;
            line.verticalOverflow = VerticalWrapMode.Overflow;
            return line;
        }

        private void LateUpdate()
        {
            if (_panel == null || !_panel.gameObject.activeSelf) return;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                _lastPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            else if (Mouse.current != null) _lastPosition = Mouse.current.position.ReadValue();
            Reposition();
        }

        private void Reposition()
        {
            var safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, safe.min, null, out var lower);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, safe.max, null, out var upper);
            var width = Mathf.Min(420, Mathf.Max(1, upper.x - lower.x - 24));
            if (_layoutDirty || !Mathf.Approximately(_panel.rect.width, width))
            {
                _panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);
                _layoutDirty = false;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, _lastPosition, null, out var point);
            var placed = CalculatePlacement(Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y), point,
                _panel.rect.size, out var pivot, out var scale);
            _panel.localScale = Vector3.one * scale;
            _panel.pivot = pivot;
            _panel.anchoredPosition = placed.min + Vector2.Scale(placed.size, pivot);
        }

        public static Rect CalculatePlacement(Rect safeBounds, Vector2 pointer, Vector2 contentSize,
            out Vector2 pivot, out float scale)
        {
            pivot = new Vector2(pointer.x > safeBounds.xMin + safeBounds.width * 0.72f ? 1 : 0,
                pointer.y < safeBounds.yMin + safeBounds.height * 0.28f ? 0 : 1);
            if (safeBounds.width <= 0 || safeBounds.height <= 0 || contentSize.x <= 0 || contentSize.y <= 0)
            {
                scale = 0;
                return new Rect(safeBounds.center, Vector2.zero);
            }
            // The margin also shrinks on tiny safe areas; no minimum scale can guarantee containment.
            var margin = new Vector2(Mathf.Min(10, safeBounds.width * 0.1f), Mathf.Min(10, safeBounds.height * 0.1f));
            var available = safeBounds.size - margin * 2;
            scale = (float)System.Math.Min(1, System.Math.Min((double)available.x / contentSize.x, (double)available.y / contentSize.y));
            var size = contentSize * scale;
            var position = pointer + new Vector2(pivot.x == 0 ? 20 : -20, pivot.y == 0 ? 20 : -20);
            var minimum = safeBounds.min + margin + Vector2.Scale(size, pivot);
            var maximum = safeBounds.max - margin - Vector2.Scale(size, Vector2.one - pivot);
            position.x = Mathf.Clamp(position.x, minimum.x, Mathf.Max(minimum.x, maximum.x));
            position.y = Mathf.Clamp(position.y, minimum.y, Mathf.Max(minimum.y, maximum.y));
            return new Rect(position - Vector2.Scale(size, pivot), size);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
