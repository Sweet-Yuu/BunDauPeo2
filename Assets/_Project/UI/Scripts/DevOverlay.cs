#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Dải DEV để thử trong Editor và Development Build; tự dựng bằng code nên không có gì của nó trong bản phát hành.
    // Hub sau này thay dải này. Không [SerializeField] trong #if (lệch bố cục serialize), không OnGUI (bản chỉ dùng Input System không nhận IMGUI).
    public static class DevOverlay
    {
        public const string RestaurantScene = "Restaurant";
        public const string GachaScene = "Gacha";
        public const long MaterialGrant = 50;

        public static readonly string[] SceneNames = { RestaurantScene, GachaScene };

        private static GameObject _root;

        // Tắt domain reload thì static còn nguyên sang lần Play sau.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _root = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Build()
        {
            if (_root != null) return;

            try
            {
                ServiceLocator.Get<RestaurantService>();
            }
            catch (InvalidOperationException)
            {
                // Boot lỗi hoặc không phải Boot: không có service thì không có gì để thử.
                return;
            }

            _root = new GameObject("DevOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(_root);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var strip = NewRect("Strip", _root.transform, new Vector2(16, 16), new Vector2(608, 76));
            var stripImage = strip.gameObject.AddComponent<Image>();
            stripImage.color = new Color(0f, 0f, 0f, 0.6f);
            stripImage.raycastTarget = false;
            AddButton(strip, font, "Quán", new Vector2(8, 8), 168, () => SceneManager.LoadScene(RestaurantScene));
            AddButton(strip, font, "Gacha", new Vector2(184, 8), 168, () => SceneManager.LoadScene(GachaScene));
            // Qua ví như mọi nguồn thu khác (không chạm file save), nên Changed bắn và Reset Save Data xoá sạch như thường.
            AddButton(strip, font, $"+{MaterialGrant} Material", new Vector2(360, 8), 240,
                () => ServiceLocator.Get<Wallet>().Add(CurrencyType.Material, MaterialGrant));
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void AddButton(RectTransform parent, Font font, string label, Vector2 position, float width, Action onClick)
        {
            var rect = NewRect(label + "Button", parent, position, new Vector2(width, 60));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.30f, 0.50f, 0.90f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            var textRect = NewRect("Label", rect, Vector2.zero, rect.sizeDelta);
            var text = textRect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
        }
    }
}
#endif
