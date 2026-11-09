using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using BunDauPeo.UI;

namespace Hoang.Remake
{
    public static class RemakeUI
    {
        public static readonly Color Ink = RestaurantArt.Ink;
        public static readonly Color Paper = RestaurantArt.Cream;
        public static readonly Color Green = RestaurantArt.Jade;
        private static Font _font;
        public static Font Font => _font != null ? _font : _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform Panel(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Layout(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            var image = go.GetComponent<Image>();
            RestaurantArt.Card(image, color);
            image.raycastTarget = color.a > 0;
            return rect;
        }

        public static Text Label(Transform parent, string name, string text, int fontSize, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Layout(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            var label = go.GetComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = Ink;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Min(14, fontSize);
            label.resizeTextMaxSize = fontSize;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        public static Button Button(Transform parent, string name, string text, UnityAction click,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = Panel(parent, name, Green, anchorMin, anchorMax, offsetMin, offsetMax);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f);
            button.colors = colors;
            var label = Label(rect, "Label", text, 23, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -4));
            label.color = Color.white;
            if (click != null) button.onClick.AddListener(click);
            return button;
        }

        public static void Layout(RectTransform rect, Vector2 min, Vector2 max, Vector2 lower, Vector2 upper)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = lower;
            rect.offsetMax = upper;
            rect.localScale = Vector3.one;
        }
    }
}
