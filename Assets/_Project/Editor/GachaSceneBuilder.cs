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
        private static readonly Color BannerColor = new(0.28f, 0.31f, 0.43f);

        // Hộp quay tiếp giữ cỡ cũ; test đọc lại hằng này.
        internal static readonly Vector2 RollAgainSize = new(900, 540);
        internal static readonly Vector2 HistoryBoxSize = new(1200, 800);

        private static readonly Vector2 TopLeft = new(0, 1);
        private static readonly Vector2 TopRight = new(1, 1);
        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 BottomCenter = new(0.5f, 0);
        private static readonly Vector2 BottomRight = new(1, 0);
        private static readonly Vector2 TopCenter = new(0.5f, 1);

        // Panel Gacha của Lobby (spec UI hub 5.4): kín màn, nền đặc chặn bấm, lúc đầu tắt. Gọi sau NewScene của RestaurantSceneBuilder (L-002).
        public static RectTransform BuildPanel(RectTransform canvas, Sprite sprite, Font font)
        {
            var banner = AssetDatabase.LoadAssetAtPath<GachaBannerDefinition>(ProjectSetup.BannerPath);
            var characters = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CharacterCatalogPath);
            var commonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CutsceneDir + "CommonSummon.prefab");
            var legendaryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CutsceneDir + "LegendarySummon.prefab");
            if (banner == null || characters == null || commonPrefab == null || legendaryPrefab == null)
                throw new InvalidOperationException("Gacha scene inputs are missing (banner, character catalog or cutscene prefabs); run ProjectSetup after copying the cutscene assets.");

            var panel = NewRect("GachaPanel_PH_1920x1080", canvas);
            Stretch(panel);
            AddImage(panel, sprite, Background, raycast: true);

            // Trang roll (G1): banner kín panel (kể cả phần dưới thanh, spec UI hub 5.4); khối tên/Gem ở hai góc trên và nút quay
            // ở góc dưới phải (trên vạch 120 px của thanh dưới) nằm trên banner. Banner là ô giữ chỗ cho tới khi có ảnh.
            var art = NewRect("GachaBanner_PH_1920x1080", panel);
            Stretch(art);
            AddImage(art, sprite, BannerColor, raycast: false);
            var bannerPanel = NewRect("BannerPanel", panel);
            Place(bannerPanel, TopLeft, TopLeft, new Vector2(40, -30), new Vector2(760, 150));
            AddImage(bannerPanel, sprite, Panel, raycast: false);
            var bannerName = NewText("BannerNameText", bannerPanel, font, banner.DisplayName, 48, TextAnchor.MiddleLeft, Color.white);
            Place(bannerName.rectTransform, TopLeft, TopLeft, new Vector2(30, -10), new Vector2(700, 70));
            var pity = NewText("PityText", bannerPanel, font, "", 32, TextAnchor.MiddleLeft, Color.white);
            Place(pity.rectTransform, TopLeft, TopLeft, new Vector2(30, -85), new Vector2(700, 50));
            var message = NewText("MessageText", panel, font, "", 32, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.4f));
            Place(message.rectTransform, BottomRight, BottomRight, new Vector2(-40, 420), new Vector2(560, 50));

            var single = NewButton("SingleButton", panel, sprite, font, GachaViewText.PullLabel(1, banner.SingleCost), ButtonColor,
                BottomRight, new Vector2(-360, 140), new Vector2(300, 160));
            var ten = NewButton("TenButton", panel, sprite, font, GachaViewText.PullLabel(10, banner.TenCost), ButtonColor,
                BottomRight, new Vector2(-40, 140), new Vector2(300, 160));
            var history = NewButton("HistoryButton", panel, sprite, font, UiText.Get("gacha.history"), GreyButton,
                BottomRight, new Vector2(-40, 320), new Vector2(300, 80));
            Localize(history.GetComponentInChildren<Text>(), "gacha.history");

            // Reveal: hai cutscene (prefab của user) và thẻ nhân vật. Ẩn mặc định; GachaRevealView bật khi quay.
            var revealRoot = NewRect("RevealRoot", panel);
            Stretch(revealRoot);
            AddImage(revealRoot, sprite, Background, raycast: true);
            var common = InstantiateCutscene(commonPrefab, revealRoot);
            var legendary = InstantiateCutscene(legendaryPrefab, revealRoot);
            var revealPanel = NewRect("RevealPanel", revealRoot);
            Place(revealPanel, Center, Center, Vector2.zero, new Vector2(1800, 1000));
            AddImage(revealPanel, sprite, Panel, raycast: false);
            var portrait = NewRect("Portrait", revealPanel);
            Place(portrait, TopLeft, Center, new Vector2(640, -500), new Vector2(760, 760));
            var portraitImage = AddImage(portrait, sprite, Color.white, raycast: false);
            var revealName = NewText("NameText", revealPanel, font, "", 64, TextAnchor.MiddleRight, Color.white);
            Place(revealName.rectTransform, BottomRight, BottomRight, new Vector2(-60, 130), new Vector2(700, 90));
            var revealStars = NewText("StarsText", revealPanel, font, "", 48, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.4f));
            Place(revealStars.rectTransform, BottomRight, BottomRight, new Vector2(-60, 50), new Vector2(700, 70));
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
            var totalRoot = NewRect("TotalRoot", panel);
            Stretch(totalRoot);
            AddImage(totalRoot, sprite, Background, raycast: true);
            var totalPanel = NewRect("TotalPanel", totalRoot);
            Place(totalPanel, Center, Center, Vector2.zero, new Vector2(1300, 640));
            AddImage(totalPanel, sprite, Panel, raycast: false);
            var totalTitle = NewLabel("TitleText", totalPanel, font, "gacha.total_title", 48, TextAnchor.MiddleLeft, Color.white);
            Place(totalTitle.rectTransform, TopLeft, TopLeft, new Vector2(60, -25), new Vector2(800, 70));
            var grid = NewRect("Grid", totalPanel);
            Place(grid, TopCenter, TopCenter, new Vector2(0, -120), new Vector2(1120, 470));
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(200, 220);
            layout.spacing = new Vector2(30, 30);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            layout.childAlignment = TextAnchor.UpperCenter;
            var icon = NewRect("IconTemplate", grid);
            AddImage(icon, sprite, Color.white, raycast: false);
            var iconPortrait = NewRect("Portrait", icon);
            Place(iconPortrait, TopCenter, TopCenter, new Vector2(0, -12), new Vector2(160, 140));
            AddImage(iconPortrait, sprite, Color.white, raycast: false);
            var iconLabel = NewRect("Label", icon);
            Place(iconLabel, BottomCenter, BottomCenter, new Vector2(0, 8), new Vector2(190, 56));
            AddText(iconLabel, font, "", 24, TextAnchor.MiddleCenter, Color.white);
            icon.gameObject.SetActive(false);
            var ok = NewButton("OkButton", totalRoot, sprite, font, "OK", ButtonColor, Center, new Vector2(0, -395), new Vector2(360, 90));
            var totalView = totalRoot.gameObject.AddComponent<GachaTotalView>();
            var totalSo = new SerializedObject(totalView);
            totalSo.FindProperty("_root").objectReferenceValue = totalRoot.gameObject;
            totalSo.FindProperty("_grid").objectReferenceValue = grid;
            totalSo.FindProperty("_iconTemplate").objectReferenceValue = icon.gameObject;
            totalSo.FindProperty("_okButton").objectReferenceValue = ok;
            totalSo.ApplyModifiedPropertiesWithoutUndo();
            totalRoot.gameObject.SetActive(false);

            // Nút Bỏ qua: đặt trên reveal để bấm được; GachaView bật tắt theo luật của GachaRevealSequence.
            // Góc trên phải, xa hai nút quay ở góc dưới phải (G4).
            var skip = NewButton("SkipButton", panel, sprite, font, UiText.Get("gacha.skip"), GreyButton, TopRight, new Vector2(-40, -40), new Vector2(300, 90));
            Localize(skip.GetComponentInChildren<Text>(), "gacha.skip");
            skip.gameObject.SetActive(false);

            // Popup quay tiếp.
            var againRoot = NewRect("RollAgainPopup", panel);
            Stretch(againRoot);
            AddImage(againRoot, sprite, Dim, raycast: true);
            // Hộp giữ cỡ cũ nhưng nền mờ kín màn và vẽ trên thanh (spec 5.5).
            NavBarBuilder.OverBar(againRoot);
            var againBox = NewRect("Box", againRoot);
            Place(againBox, Center, Center, Vector2.zero, RollAgainSize);
            AddImage(againBox, sprite, Panel, raycast: false);
            var againTitle = NewLabel("TitleText", againBox, font, "gacha.roll_again_title", 52, TextAnchor.MiddleCenter, Color.white);
            Place(againTitle.rectTransform, TopCenter, TopCenter, new Vector2(0, -50), new Vector2(800, 70));
            var againWarning = NewText("WarningText", againBox, font, "", 34, TextAnchor.MiddleCenter, Warning);
            Place(againWarning.rectTransform, TopCenter, TopCenter, new Vector2(0, -140), new Vector2(800, 60));
            var rollAgain = NewButton("RollAgainButton", againBox, sprite, font, UiText.Get("gacha.roll_again"), ButtonColor, BottomCenter, new Vector2(0, 170), new Vector2(640, 100));
            var dismiss = NewButton("DismissButton", againBox, sprite, font, UiText.Get("common.not_now"), GreyButton, BottomCenter, new Vector2(0, 50), new Vector2(640, 100));
            Localize(dismiss.GetComponentInChildren<Text>(), "common.not_now");
            var againPopup = againRoot.gameObject.AddComponent<GachaRollAgainPopup>();
            var againSo = new SerializedObject(againPopup);
            againSo.FindProperty("_root").objectReferenceValue = againRoot.gameObject;
            againSo.FindProperty("_warningText").objectReferenceValue = againWarning;
            againSo.FindProperty("_rollAgainLabel").objectReferenceValue = rollAgain.GetComponentInChildren<Text>();
            againSo.FindProperty("_rollAgainButton").objectReferenceValue = rollAgain;
            againSo.FindProperty("_dismissButton").objectReferenceValue = dismiss;
            againSo.ApplyModifiedPropertiesWithoutUndo();
            againRoot.gameObject.SetActive(false);

            // Hộp lịch sử (spec UI hub B 5.7): nền mờ kín màn vẽ trên thanh, hộp giữa màn; danh sách nằm trong ScrollRect để 50 mục vẫn cuộn được.
            var historyRoot = NewRect("HistoryPopup", panel);
            Stretch(historyRoot);
            AddImage(historyRoot, sprite, new Color(0f, 0f, 0f, 0.55f), raycast: true);
            NavBarBuilder.OverBar(historyRoot);
            var historyBox = NewRect("GachaHistoryBox_PH_1200x800", historyRoot);
            Place(historyBox, Center, Center, Vector2.zero, HistoryBoxSize);
            AddImage(historyBox, sprite, Panel, raycast: false);
            var historyTitle = NewLabel("TitleText", historyBox, font, "gacha.history_title", 48, TextAnchor.MiddleCenter, Color.white);
            Place(historyTitle.rectTransform, TopCenter, TopCenter, new Vector2(0, -25), new Vector2(1000, 70));
            var scrollRect = NewRect("ScrollView", historyBox);
            Place(scrollRect, Center, Center, new Vector2(0, -10), new Vector2(1100, 520));
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
            var close = BattleSceneBuilder.LabelButton("CloseButton", historyBox, sprite, font, "common.close", GreyButton, BottomCenter, new Vector2(0, 25), new Vector2(300, 100));
            var historyPopup = historyRoot.gameObject.AddComponent<GachaHistoryPopup>();
            var historySo = new SerializedObject(historyPopup);
            historySo.FindProperty("_root").objectReferenceValue = historyRoot.gameObject;
            historySo.FindProperty("_listText").objectReferenceValue = listText;
            historySo.FindProperty("_closeButton").objectReferenceValue = close;
            historySo.ApplyModifiedPropertiesWithoutUndo();
            historyRoot.gameObject.SetActive(false);

            var viewObject = NewRect("GachaView", panel);
            var view = viewObject.gameObject.AddComponent<GachaView>();
            var so = new SerializedObject(view);
            so.FindProperty("_banner").objectReferenceValue = banner;
            so.FindProperty("_characters").objectReferenceValue = characters;
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

            panel.gameObject.SetActive(false);
            return panel;
        }

        // Trang kín màn (spec UI hub 5.5): root nền đặc phủ 1920×1080 do người gọi dựng; hộp nội dung chỉ phủ phần trên thanh
        // (offsetMin.y = BarHeight), nên con neo theo mép trên/dưới của hộp không lọt xuống dải 0–120.
        internal static RectTransform PageBox(RectTransform root, string name)
        {
            var box = NewRect(name, root);
            Stretch(box);
            box.offsetMin = new Vector2(0, HubNav.BarHeight);
            return box;
        }

        // Nút Quay lại của trang: góc trên-trái, nhãn hub.back, giữ tên cũ (CloseButton, CancelButton).
        internal static Button BackButton(string name, RectTransform box, Sprite sprite, Font font) =>
            BattleSceneBuilder.LabelButton(name, box, sprite, font, "hub.back", GreyButton, TopLeft, new Vector2(30, -25), new Vector2(200, 80));

        internal static void CreateCamera(Scene scene)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            SceneManager.MoveGameObjectToScene(go, scene);
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            // Thiếu AudioListener thì Play báo cảnh báo mỗi lần nạp scene.
            go.AddComponent<AudioListener>();
        }

        internal static void CreateEventSystem(Scene scene)
        {
            // Project chỉ dùng Input System (activeInputHandler: 1); StandaloneInputModule sẽ không nhận chuột/chạm.
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        internal static RectTransform CreateCanvas(Scene scene)
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

        internal static Button NewButton(string name, RectTransform parent, Sprite sprite, Font font, string text, Color color,
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

        // Hàng ô thưởng: nhãn 200 px bên trái, ô 96×96 cách 24, số 28 px dưới ô. Vị trí là góc trên trái của hàng.
        internal static RewardRow BuildRewardRow(RectTransform parent, string name, Font font, Vector2 anchor, Vector2 position)
        {
            const float cell = 96, gap = 24, labelWidth = 200;
            var root = NewRect(name, parent);
            Place(root, anchor, TopLeft, position, new Vector2(labelWidth + RewardRow.MaxCells * (cell + gap), cell + 44));
            var label = NewText("Label", root, font, "", 32, TextAnchor.MiddleLeft, Color.white);
            Place(label.rectTransform, TopLeft, TopLeft, Vector2.zero, new Vector2(labelWidth, cell));
            var cells = new Image[RewardRow.MaxCells];
            var amounts = new Text[RewardRow.MaxCells];
            for (var i = 0; i < RewardRow.MaxCells; i++)
            {
                var rect = NewRect($"Cell{i}", root);
                Place(rect, TopLeft, TopLeft, new Vector2(labelWidth + i * (cell + gap), 0), new Vector2(cell, cell));
                cells[i] = AddImage(rect, null, Color.white, raycast: false);
                amounts[i] = NewText("Amount", rect, font, "", 24, TextAnchor.UpperCenter, Color.white);
                // Ô chỉ rộng 120: "30 Material" bị cắt mất chữ Material nếu để ngắt dòng.
                amounts[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                Place(amounts[i].rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(cell + gap, 40));
            }
            var row = root.gameObject.AddComponent<RewardRow>();
            var so = new SerializedObject(row);
            so.FindProperty("_label").objectReferenceValue = label;
            Fill(so.FindProperty("_cells"), cells);
            Fill(so.FindProperty("_amounts"), amounts);
            so.ApplyModifiedPropertiesWithoutUndo();
            return row;
        }

        private static void Fill(SerializedProperty array, UnityEngine.Object[] items)
        {
            array.arraySize = items.Length;
            for (var i = 0; i < items.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        internal static Text NewText(string name, RectTransform parent, Font font, string text, int size, TextAnchor align, Color color) =>
            AddText(NewRect(name, parent), font, text, size, align, color);

        internal static Text AddText(RectTransform rect, Font font, string text, int size, TextAnchor align, Color color)
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

        // Nhãn cố định: chữ lấy từ UiText và gắn LocalizedText để scene đổi chữ theo ngôn ngữ. Chữ view ghi lúc chạy dùng NewText.
        internal static Text NewLabel(string name, RectTransform parent, Font font, string key, int size, TextAnchor align, Color color) =>
            Localize(NewText(name, parent, font, UiText.Get(key), size, align, color), key);

        internal static Text AddLabel(RectTransform rect, Font font, string key, int size, TextAnchor align, Color color) =>
            Localize(AddText(rect, font, UiText.Get(key), size, align, color), key);

        private static Text Localize(Text text, string key)
        {
            text.gameObject.AddComponent<LocalizedText>().SetKey(key);
            return text;
        }

        internal static Image AddImage(RectTransform rect, Sprite sprite, Color color, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        internal static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        // Đặt anchor và pivot trùng nhau trước, rồi mới đặt vị trí và kích thước.
        internal static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        internal static void Stretch(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
