using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Shared by the original Lobby and Hoang's restaurant systems; assets stay in the build.
    public static class RestaurantArt
    {
        public const string ResourceRoot = "RestaurantArt/";
        public static readonly Color Ink = new(0.23f, 0.15f, 0.10f);
        public static readonly Color Cream = new(1f, 0.97f, 0.88f);
        public static readonly Color Jade = new(0.12f, 0.35f, 0.25f);
        public static readonly Color Muted = new(0.42f, 0.43f, 0.38f);
        public static readonly Color Gold = new(0.82f, 0.58f, 0.22f);
        public static readonly Color Purple = new(0.48f, 0.30f, 0.51f);
        public static readonly Color SoftGreen = new(0.90f, 0.93f, 0.82f);
        private static Dictionary<string, Sprite> _icons;
        private static Sprite _garden;
        private static Sprite _frame;

        public static void ClearCache()
        {
            _icons = null;
            _garden = null;
            _frame = null;
        }

        public static Sprite Garden => _garden != null ? _garden : _garden = Resources.Load<Sprite>(ResourceRoot + "garden");

        public static Sprite Icon(string id)
        {
            if (_icons == null)
            {
                _icons = new Dictionary<string, Sprite>();
                foreach (var sheet in new[] { "skills-facilities", "rewards" })
                    foreach (var sprite in Resources.LoadAll<Sprite>(ResourceRoot + sheet))
                        _icons[sprite.name] = sprite;
            }
            return id != null && _icons.TryGetValue(id, out var icon) ? icon : null;
        }

        public static void Card(Image image, Color color)
        {
            if (image == null) return;
            if (_frame == null) _frame = Resources.Load<Sprite>(ResourceRoot + "frame");
            if (_frame != null)
            {
                image.sprite = _frame;
                image.type = Image.Type.Sliced;
            }
            image.color = color;
            var outline = image.GetComponent<Outline>();
            if (outline == null)
            {
                outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, .55f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
                outline.useGraphicAlpha = true;
            }
        }

        public static Image Illustration(Transform parent, string name, string id,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var child = parent.Find(name);
            var image = child != null ? child.GetComponent<Image>() : null;
            if (image == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
            }
            var rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            image.sprite = Icon(id);
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
    }
}
