using System;
using BunDauPeo.Characters;
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
    // Dựng scene Gacha bằng code. Mọi hình là Image với sprite dựng sẵn (cộng bộ cutscene user cung cấp), chữ là Text của uGUI
    // (docs dự án thắng skill ui-ugui, vốn khuyên TextMeshPro: font TMP mặc định có thể thiếu dấu tiếng Việt).
    internal static class GachaSceneBuilder
    {
        private const string CutsceneDir = "Assets/_Project/Gacha/Prefabs/Cutscenes/";
        private const string CharacterCatalogPath = "Assets/_Project/Characters/Data/CharacterCatalog.asset";

        private static readonly Color Background = new(0.10f, 0.11f, 0.15f);
        private static readonly Color Panel = new(0.18f, 0.20f, 0.27f);
        private static readonly Color Dim = new(0f, 0f, 0f, 0.65f);
        private static readonly Color ButtonColor = new(0.30f, 0.50f, 0.90f);
        private static readonly Color GreyButton = new(0.45f, 0.47f, 0.55f);
        private static readonly Color Warning = new(1f, 0.55f, 0.45f);

        private static readonly Vector2 TopLeft = new(0, 1);
        private static readonly Vector2 TopRight = new(1, 1);
        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 BottomCenter = new(0.5f, 0);
        private static readonly Vector2 BottomRight = new(1, 0);
        private static readonly Vector2 TopCenter = new(0.5f, 1);

        public static void Create(string scenePath)
        {
            // Giống CreateBootScene: batch mode chỉ có scene Untitled chưa lưu nên phải dùng Single.
            var mode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);

            // Nạp sau NewScene (L-002): Single dỡ asset không được scene nào dùng.
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var banner = AssetDatabase.LoadAssetAtPath<GachaBannerDefinition>(ProjectSetup.BannerPath);
            var characters = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CharacterCatalogPath);
            var commonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CutsceneDir + "CommonSummon.prefab");
            var legendaryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CutsceneDir + "LegendarySummon.prefab");
            if (banner == null || characters == null || commonPrefab == null || legendaryPrefab == null)
                throw new InvalidOperationException("Gacha scene inputs are missing (banner, character catalog or cutscene prefabs); run ProjectSetup after copying the cutscene assets.");

            CreateCamera(scene);
            CreateEventSystem(scene);
            var canvas = CreateCanvas(scene);

            var back = NewRect("Background", canvas);
            Stretch(back);
            AddImage(back, sprite, Background, raycast: false);

            // Trang roll
            var gem = NewText("GemText", canvas, font, "Gem: 0", 48, TextAnchor.MiddleLeft, Color.white);
            Place(gem.rectTransform, TopLeft, TopLeft, new Vector2(40, -30), new Vector2(500, 70));
            var bannerName = NewText("BannerNameText", canvas, font, banner.DisplayName, 48, TextAnchor.MiddleRight, Color.white);
            Place(bannerName.rectTransform, TopRight, TopRight, new Vector2(-40, -30), new Vector2(700, 70));
            var pity = NewText("PityText", canvas, font, "", 32, TextAnchor.MiddleRight, Color.white);
            Place(pity.rectTransform, TopRight, TopRight, new Vector2(-40, -100), new Vector2(700, 50));
            var message = NewText("MessageText", canvas, font, "", 32, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f));
            Place(message.rectTransform, BottomCenter, BottomCenter, new Vector2(0, 150), new Vector2(1400, 50));

            var single = NewButton("SingleButton", canvas, sprite, font, GachaViewText.PullLabel(1, banner.SingleCost), ButtonColor,
                BottomCenter, new Vector2(-260, 40), new Vector2(440, 100));
            var ten = NewButton("TenButton", canvas, sprite, font, GachaViewText.PullLabel(10, banner.TenCost), ButtonColor,
                BottomCenter, new Vector2(260, 40), new Vector2(440, 100));
            var history = NewButton("HistoryButton", canvas, sprite, font, "Lịch sử", GreyButton,
                TopLeft, new Vector2(40, -110), new Vector2(300, 80));

            // Reveal: hai cutscene (prefab của user) và thẻ nhân vật. Ẩn mặc định; GachaRevealView bật khi quay.
            var revealRoot = NewRect("RevealRoot", canvas);
            Stretch(revealRoot);
            AddImage(revealRoot, sprite, Background, raycast: true);
            var common = InstantiateCutscene(commonPrefab, revealRoot);
            var legendary = InstantiateCutscene(legendaryPrefab, revealRoot);
            var revealPanel = NewRect("RevealPanel", revealRoot);
            Place(revealPanel, Center, Center, Vector2.zero, new Vector2(900, 760));
            AddImage(revealPanel, sprite, Panel, raycast: false);
            var portrait = NewRect("Portrait", revealPanel);
            Place(portrait, TopCenter, TopCenter, new Vector2(0, -40), new Vector2(460, 460));
            var portraitImage = AddImage(portrait, sprite, Color.white, raycast: false);
            var revealName = NewText("NameText", revealPanel, font, "", 52, TextAnchor.MiddleCenter, Color.white);
            Place(revealName.rectTransform, BottomCenter, BottomCenter, new Vector2(0, 150), new Vector2(840, 70));
            var revealStars = NewText("StarsText", revealPanel, font, "", 38, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f));
            Place(revealStars.rectTransform, BottomCenter, BottomCenter, new Vector2(0, 80), new Vector2(840, 60));
            var flash = NewRect("Flash", revealRoot);
            Stretch(flash);
            var flashImage = AddImage(flash, sprite, new Color(1f, 1f, 1f, 0f), raycast: false);
            var revealView = revealRoot.gameObject.AddComponent<GachaRevealView>();
            var revealSo = new SerializedObject(revealView);
            revealSo.FindProperty("_root").objectReferenceValue = revealRoot.gameObject;
            revealSo.FindProperty("_commonAnimator").objectReferenceValue = common.GetComponent<Animator>();
            revealSo.FindProperty("_legendaryAnimator").objectReferenceValue = legendary.GetComponent<Animator>();
            revealSo.FindProperty("_revealPanel").objectReferenceValue = revealPanel.gameObject;
            revealSo.FindProperty("_flash").objectReferenceValue = flashImage;
            revealSo.FindProperty("_portrait").objectReferenceValue = portraitImage;
            revealSo.FindProperty("_nameText").objectReferenceValue = revealName;
            revealSo.FindProperty("_starsText").objectReferenceValue = revealStars;
            revealSo.ApplyModifiedPropertiesWithoutUndo();
            common.SetActive(false);
            legendary.SetActive(false);
            revealRoot.gameObject.SetActive(false);

            // Trang tổng: lưới 5 cột (10 icon thành 5 trên + 5 dưới), icon mẫu ẩn để GachaTotalView nhân bản.
            var totalRoot = NewRect("TotalRoot", canvas);
            Stretch(totalRoot);
            AddImage(totalRoot, sprite, Background, raycast: true);
            var grid = NewRect("Grid", totalRoot);
            Place(grid, Center, Center, new Vector2(0, 60), new Vector2(1770, 730));
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(330, 350);
            layout.spacing = new Vector2(30, 30);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            layout.childAlignment = TextAnchor.UpperCenter;
            var icon = NewRect("IconTemplate", grid);
            AddImage(icon, sprite, Color.white, raycast: false);
            var iconPortrait = NewRect("Portrait", icon);
            Place(iconPortrait, Center, Center, new Vector2(0, 30), new Vector2(270, 270));
            AddImage(iconPortrait, sprite, Color.white, raycast: false);
            var iconLabel = NewRect("Label", icon);
            Place(iconLabel, BottomCenter, BottomCenter, new Vector2(0, 30), new Vector2(310, 70));
            AddText(iconLabel, font, "", 28, TextAnchor.MiddleCenter, Color.white);
            icon.gameObject.SetActive(false);
            var ok = NewButton("OkButton", totalRoot, sprite, font, "OK", ButtonColor, BottomCenter, new Vector2(0, 90), new Vector2(360, 100));
            var totalView = totalRoot.gameObject.AddComponent<GachaTotalView>();
            var totalSo = new SerializedObject(totalView);
            totalSo.FindProperty("_root").objectReferenceValue = totalRoot.gameObject;
            totalSo.FindProperty("_grid").objectReferenceValue = grid;
            totalSo.FindProperty("_iconTemplate").objectReferenceValue = icon.gameObject;
            totalSo.FindProperty("_okButton").objectReferenceValue = ok;
            totalSo.ApplyModifiedPropertiesWithoutUndo();
            totalRoot.gameObject.SetActive(false);

            // Nút Bỏ qua: đặt trên reveal để bấm được; GachaView bật tắt theo luật của GachaRevealSequence.
            var skip = NewButton("SkipButton", canvas, sprite, font, "Bỏ qua", GreyButton, BottomRight, new Vector2(-40, 40), new Vector2(300, 90));
            skip.gameObject.SetActive(false);

            // Popup quay tiếp.
            var againRoot = NewRect("RollAgainPopup", canvas);
            Stretch(againRoot);
            AddImage(againRoot, sprite, Dim, raycast: true);
            var againBox = NewRect("Box", againRoot);
            Place(againBox, Center, Center, Vector2.zero, new Vector2(900, 540));
            AddImage(againBox, sprite, Panel, raycast: false);
            var againTitle = NewText("TitleText", againBox, font, "Quay tiếp?", 52, TextAnchor.MiddleCenter, Color.white);
            Place(againTitle.rectTransform, TopCenter, TopCenter, new Vector2(0, -50), new Vector2(800, 70));
            var againWarning = NewText("WarningText", againBox, font, "", 34, TextAnchor.MiddleCenter, Warning);
            Place(againWarning.rectTransform, TopCenter, TopCenter, new Vector2(0, -140), new Vector2(800, 60));
            var rollAgain = NewButton("RollAgainButton", againBox, sprite, font, "Quay tiếp", ButtonColor, BottomCenter, new Vector2(0, 170), new Vector2(640, 100));
            var dismiss = NewButton("DismissButton", againBox, sprite, font, "Thôi", GreyButton, BottomCenter, new Vector2(0, 50), new Vector2(640, 100));
            var againPopup = againRoot.gameObject.AddComponent<GachaRollAgainPopup>();
            var againSo = new SerializedObject(againPopup);
            againSo.FindProperty("_root").objectReferenceValue = againRoot.gameObject;
            againSo.FindProperty("_warningText").objectReferenceValue = againWarning;
            againSo.FindProperty("_rollAgainLabel").objectReferenceValue = rollAgain.GetComponentInChildren<Text>();
            againSo.FindProperty("_rollAgainButton").objectReferenceValue = rollAgain;
            againSo.FindProperty("_dismissButton").objectReferenceValue = dismiss;
            againSo.ApplyModifiedPropertiesWithoutUndo();
            againRoot.gameObject.SetActive(false);

            // Popup lịch sử: danh sách nằm trong ScrollRect để 50 mục vẫn cuộn được.
            var historyRoot = NewRect("HistoryPopup", canvas);
            Stretch(historyRoot);
            AddImage(historyRoot, sprite, Dim, raycast: true);
            var historyBox = NewRect("Box", historyRoot);
            Place(historyBox, Center, Center, Vector2.zero, new Vector2(1200, 900));
            AddImage(historyBox, sprite, Panel, raycast: false);
            var historyTitle = NewText("TitleText", historyBox, font, "Lịch sử quay", 48, TextAnchor.MiddleCenter, Color.white);
            Place(historyTitle.rectTransform, TopCenter, TopCenter, new Vector2(0, -30), new Vector2(1000, 70));
            var scrollRect = NewRect("ScrollView", historyBox);
            Place(scrollRect, Center, Center, new Vector2(0, -10), new Vector2(1100, 640));
            var viewport = NewRect("Viewport", scrollRect);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var listText = AddText(content, font, "", 28, TextAnchor.UpperLeft, Color.white);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var close = NewButton("CloseButton", historyBox, sprite, font, "Đóng", GreyButton, BottomCenter, new Vector2(0, 40), new Vector2(360, 90));
            var historyPopup = historyRoot.gameObject.AddComponent<GachaHistoryPopup>();
            var historySo = new SerializedObject(historyPopup);
            historySo.FindProperty("_root").objectReferenceValue = historyRoot.gameObject;
            historySo.FindProperty("_listText").objectReferenceValue = listText;
            historySo.FindProperty("_closeButton").objectReferenceValue = close;
            historySo.ApplyModifiedPropertiesWithoutUndo();
            historyRoot.gameObject.SetActive(false);

            var viewObject = NewRect("GachaView", canvas);
            var view = viewObject.gameObject.AddComponent<GachaView>();
            var so = new SerializedObject(view);
            so.FindProperty("_banner").objectReferenceValue = banner;
            so.FindProperty("_characters").objectReferenceValue = characters;
            so.FindProperty("_gemText").objectReferenceValue = gem;
            so.FindProperty("_bannerNameText").objectReferenceValue = bannerName;
            so.FindProperty("_pityText").objectReferenceValue = pity;
            so.FindProperty("_messageText").objectReferenceValue = message;
            so.FindProperty("_singleButton").objectReferenceValue = single;
            so.FindProperty("_tenButton").objectReferenceValue = ten;
            so.FindProperty("_skipButton").objectReferenceValue = skip;
            so.FindProperty("_historyButton").objectReferenceValue = history;
            so.FindProperty("_revealView").objectReferenceValue = revealView;
            so.FindProperty("_totalView").objectReferenceValue = totalView;
            so.FindProperty("_rollAgainPopup").objectReferenceValue = againPopup;
            so.FindProperty("_historyPopup").objectReferenceValue = historyPopup;
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

        // Prefab của user trải đầy reveal; Animator nằm ở object gốc của prefab.
        private static GameObject InstantiateCutscene(GameObject prefab, RectTransform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Stretch((RectTransform)go.transform);
            return go;
        }

        private static Button NewButton(string name, RectTransform parent, Sprite sprite, Font font, string text, Color color,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = NewRect(name, parent);
            Place(rect, anchor, anchor, position, size);
            var image = AddImage(rect, sprite, color, raycast: true);
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
