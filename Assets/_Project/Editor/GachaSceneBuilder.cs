using System;
using BunDauPeo.Gacha;
using BunDauPeo.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.EditorTools
{
    // Dựng scene Gacha tối thiểu bằng code. Mọi hình là Image với sprite dựng sẵn, chữ là Text của uGUI (docs dự án thắng skill ui-ugui,
    // vốn khuyên TextMeshPro: font TMP mặc định có thể thiếu dấu tiếng Việt).
    internal static class GachaSceneBuilder
    {
        private static readonly Color Background = new(0.10f, 0.11f, 0.15f);
        private static readonly Color Panel = new(0.18f, 0.20f, 0.27f);
        private static readonly Color ButtonColor = new(0.30f, 0.50f, 0.90f);
        private static readonly Color Ink = new(0.08f, 0.08f, 0.10f);

        public static void Create(string scenePath)
        {
            // Giống CreateBootScene: batch mode chỉ có scene Untitled chưa lưu nên phải dùng Single.
            var mode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);

            // Nạp sau NewScene (L-002): Single dỡ asset không được scene nào dùng.
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var banner = AssetDatabase.LoadAssetAtPath<GachaBannerDefinition>(ProjectSetup.BannerPath);
            if (banner == null) throw new InvalidOperationException($"Banner not found at {ProjectSetup.BannerPath}; run ProjectSetup first.");

            CreateCamera(scene);
            CreateEventSystem(scene);
            var canvas = CreateCanvas(scene);

            var back = NewRect("Background", canvas);
            Stretch(back);
            AddImage(back, sprite, Background, raycast: false);

            var gem = NewText("GemText", canvas, font, "Gem: 0", 48, TextAnchor.MiddleLeft, Color.white);
            Place(gem.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(500, 70));
            var bannerName = NewText("BannerNameText", canvas, font, banner.DisplayName, 48, TextAnchor.MiddleRight, Color.white);
            Place(bannerName.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(700, 70));
            var pity = NewText("PityText", canvas, font, "", 32, TextAnchor.MiddleRight, Color.white);
            Place(pity.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -100), new Vector2(700, 50));

            var results = NewRect("Results", canvas);
            // Neo ở trên và gọn vừa 866 đơn vị cao (Game view rộng ~2.8:1) mà không đè dòng pity hay hai nút.
            Place(results, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -180), new Vector2(1760, 460));
            AddImage(results, sprite, Panel, raycast: false);
            var grid = results.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(330, 210);
            grid.spacing = new Vector2(20, 20);
            grid.padding = new RectOffset(15, 15, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            var slots = new (Image Background, Text Label)[10];
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = NewRect($"Slot{i:00}", results);
                var image = AddImage(slot, sprite, Color.white, raycast: false);
                var labelRect = NewRect("Label", slot);
                Stretch(labelRect, 10);
                var label = AddText(labelRect, font, "", 30, TextAnchor.MiddleCenter, Ink);
                slots[i] = (image, label);
                slot.gameObject.SetActive(false);
            }

            var message = NewText("MessageText", canvas, font, "", 32, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f));
            Place(message.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(1400, 50));

            var single = NewButton("SingleButton", canvas, sprite, font, GachaViewText.PullLabel(1, banner.SingleCost), new Vector2(-260, 40));
            var ten = NewButton("TenButton", canvas, sprite, font, GachaViewText.PullLabel(10, banner.TenCost), new Vector2(260, 40));

            var viewObject = NewRect("GachaView", canvas);
            var view = viewObject.gameObject.AddComponent<GachaView>();
            var so = new SerializedObject(view);
            so.FindProperty("_banner").objectReferenceValue = banner;
            so.FindProperty("_gemText").objectReferenceValue = gem;
            so.FindProperty("_bannerNameText").objectReferenceValue = bannerName;
            so.FindProperty("_pityText").objectReferenceValue = pity;
            so.FindProperty("_messageText").objectReferenceValue = message;
            so.FindProperty("_singleButton").objectReferenceValue = single;
            so.FindProperty("_tenButton").objectReferenceValue = ten;
            var array = so.FindProperty("_slots");
            array.arraySize = slots.Length;
            for (var i = 0; i < slots.Length; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Background").objectReferenceValue = slots[i].Background;
                element.FindPropertyRelative("Label").objectReferenceValue = slots[i].Label;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, scenePath);
            if (mode == NewSceneMode.Additive) EditorSceneManager.CloseScene(scene, true);
        }

        private static void CreateCamera(Scene scene)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            SceneManager.MoveGameObjectToScene(go, scene);
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            // Thiếu AudioListener thì Play báo cảnh báo mỗi lần nạp scene.
            go.AddComponent<AudioListener>();
        }

        private static void CreateEventSystem(Scene scene)
        {
            // Project chỉ dùng Input System (activeInputHandler: 1); StandaloneInputModule sẽ không nhận chuột/chạm.
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        private static RectTransform CreateCanvas(Scene scene)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(go, scene);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return (RectTransform)go.transform;
        }

        private static Button NewButton(string name, RectTransform parent, Sprite sprite, Font font, string text, Vector2 position)
        {
            var rect = NewRect(name, parent);
            Place(rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), position, new Vector2(440, 100));
            var image = AddImage(rect, sprite, ButtonColor, raycast: true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var labelRect = NewRect("Label", rect);
            Stretch(labelRect);
            AddText(labelRect, font, text, 36, TextAnchor.MiddleCenter, Color.white);
            return button;
        }

        private static Text NewText(string name, RectTransform parent, Font font, string text, int size, TextAnchor align, Color color) =>
            AddText(NewRect(name, parent), font, text, size, align, color);

        private static Text AddText(RectTransform rect, Font font, string text, int size, TextAnchor align, Color color)
        {
            var t = rect.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        private static Image AddImage(RectTransform rect, Sprite sprite, Color color, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        // Đặt anchor và pivot trùng nhau trước, rồi mới đặt vị trí và kích thước.
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
