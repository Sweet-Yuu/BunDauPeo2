using BunDauPeo.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BunDauPeo.EditorTools.GachaSceneBuilder;

namespace BunDauPeo.EditorTools
{
    // Thanh dưới 6 ô, Menu và dòng "Sắp có" của Lobby (spec UI hub 5.3, 5.6). Dựng vào canvas của Restaurant.unity;
    // hình đều là Image với sprite có sẵn, chữ là Text uGUI gắn LocalizedText (D-012).
    internal static class NavBarBuilder
    {
        private static readonly string[] TabNames =
        {
            "NavTabRestaurant_PH_320x120", "NavTabCharacters_PH_320x120", "NavTabStory_PH_320x120",
            "NavTabStages_PH_320x120", "NavTabGacha_PH_320x120", "NavTabMenu_PH_320x120",
        };

        private static readonly string[] TabKeys =
        {
            "hub.tab_restaurant", "hub.tab_characters", "hub.tab_story", "hub.tab_stages", "hub.tab_gacha", "hub.tab_menu",
        };

        private static readonly Color Background = new(0.10f, 0.11f, 0.15f);
        private static readonly Color BarColor = new(0.07f, 0.08f, 0.11f);
        private static readonly Color TabColor = new(0.20f, 0.23f, 0.31f);
        private static readonly Color LockedTabColor = new(0.15f, 0.17f, 0.23f);
        private static readonly Color ButtonColor = new(0.30f, 0.50f, 0.90f);
        private static readonly Color GreyButton = new(0.45f, 0.47f, 0.55f);
        private static readonly Color DangerColor = new(0.85f, 0.20f, 0.20f);
        private static readonly Color DangerPanel = new(0.35f, 0.10f, 0.10f);
        private static readonly Color Dim = new(0f, 0f, 0f, 0.65f);
        private static readonly Color Warning = new(1f, 0.65f, 0.45f);
        private static readonly Color Gold = new(1f, 0.85f, 0.4f);
        private static readonly Color Dark = new(0.10f, 0.10f, 0.14f);
        private static readonly Color Muted = new(0.35f, 0.36f, 0.42f);

        private static readonly Color ResourceBarColor = new(1f, 1f, 1f, 0.92f);
        private static readonly Color ResourceTextColor = new(0.20f, 0.22f, 0.30f);
        private static readonly Color ResourceSubTextColor = new(0.36f, 0.38f, 0.46f);
        private const float ResourceSlotWidth = 280f;

        // Khớp tên ResetConfirm_PH_800x420; test đọc lại hằng này.
        internal static readonly Vector2 ResetConfirmSize = new(800, 420);

        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new(0, 1);
        private static readonly Vector2 TopCenter = new(0.5f, 1);
        private static readonly Vector2 BottomCenter = new(0.5f, 0);

        // Thanh, 6 ô, dòng "Sắp có" và HubNav (chưa nối panel: Wire làm sau khi panel đã dựng).
        public static HubNav Build(RectTransform canvas, Font font, Sprite sprite)
        {
            var hubObject = NewRect("HubNav", canvas);
            var hub = hubObject.gameObject.AddComponent<HubNav>();

            var bar = NewRect("NavBar_PH_1920x120", canvas);
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = new Vector2(1, 0);
            bar.pivot = new Vector2(0.5f, 0);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(0, HubNav.BarHeight);
            AddImage(bar, null, BarColor, raycast: true);

            var buttons = new Button[TabNames.Length];
            for (var i = 0; i < buttons.Length; i++) buttons[i] = BuildTab(bar, font, sprite, i);

            // Dòng báo nằm trên thanh, sau thanh trong thứ tự anh em nên vẽ đè lên panel.
            var toast = NewRect("ComingSoonToast", canvas);
            Place(toast, BottomCenter, BottomCenter, new Vector2(0, 150), new Vector2(420, 70));
            AddImage(toast, sprite, new Color(0f, 0f, 0f, 0.85f), raycast: false);
            var toastLabel = NewLabel("Label", toast, font, "hub.coming_soon", 36, TextAnchor.MiddleCenter, Color.white);
            Stretch(toastLabel.rectTransform);
            toast.gameObject.SetActive(false);

            var so = new SerializedObject(hub);
            SetRefs(so.FindProperty("_tabButtons"), buttons);
            so.FindProperty("_bar").objectReferenceValue = bar.gameObject;
            so.FindProperty("_comingSoonToast").objectReferenceValue = toast.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            return hub;
        }

        // Nối HubNav và MenuView với các panel đã dựng. pages: các view cài IHubPage (Task 5, 6 điền).
        public static void Wire(HubNav hub, MenuView menu, RectTransform characters, RectTransform stages, RectTransform gacha, RectTransform menuPanel,
            Object[] pages)
        {
            var so = new SerializedObject(hub);
            so.FindProperty("_charactersPanel").objectReferenceValue = characters.gameObject;
            so.FindProperty("_stagesPanel").objectReferenceValue = stages.gameObject;
            so.FindProperty("_gachaPanel").objectReferenceValue = gacha.gameObject;
            so.FindProperty("_menuPanel").objectReferenceValue = menuPanel.gameObject;
            SetRefs(so.FindProperty("_pages"), pages);
            so.ApplyModifiedPropertiesWithoutUndo();

            var menuSo = new SerializedObject(menu);
            menuSo.FindProperty("_hub").objectReferenceValue = hub;
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            // Gacha ẩn thanh lúc cutscene, lộ, tổng (spec 5.3): GachaView cần HubNav, mà thanh dựng sau panel.
            var gachaSo = new SerializedObject(gacha.GetComponentInChildren<GachaView>(true));
            gachaSo.FindProperty("_hub").objectReferenceValue = hub;
            gachaSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // Thanh tài nguyên chung (spec UI hub B 5.3, lớp 3b): 4 ô 280×76 liền nhau, bố cục Lobby (neo trái cạnh avatar); ResourceBarView đổi bố cục
        // và bật/tắt ô lúc chạy. Dựng sau ba panel, trước Menu. View nằm ngoài thanh để còn chạy khi thanh tắt; HubNav nối ở WireLobby.
        public static ResourceBarView BuildResourceBar(RectTransform canvas, Font font, Sprite sprite, Sprite knob)
        {
            var viewRect = NewRect("ResourceBarView", canvas);
            var view = viewRect.gameObject.AddComponent<ResourceBarView>();

            var bar = NewRect("ResourceBar_PH_1120x76", canvas);
            Place(bar, TopLeft, TopLeft, new Vector2(180, -20), new Vector2(4 * ResourceSlotWidth, 76));
            AddImage(bar, sprite, ResourceBarColor, raycast: false);

            var slotNames = new[] { "ResStamina_PH_280x76", "ResCoin_PH_280x76", "ResMaterial_PH_280x76", "ResGem_PH_280x76" };
            var slots = new GameObject[slotNames.Length];
            var texts = new Text[slotNames.Length];
            var plus = new System.Collections.Generic.List<Button>();
            Text next = null;
            for (var i = 0; i < slotNames.Length; i++)
            {
                var slot = NewRect(slotNames[i], bar);
                Place(slot, TopLeft, TopLeft, new Vector2(i * ResourceSlotWidth, 0), new Vector2(ResourceSlotWidth, 76));
                slots[i] = slot.gameObject;
                var hasPlus = i != 2;
                var value = NewText("ValueText", slot, font, "", 32, TextAnchor.MiddleLeft, ResourceTextColor);
                if (i == 0)
                {
                    // Thể lực: số ở dòng trên, "+1 sau m:ss" nhỏ ở dòng dưới.
                    Place(value.rectTransform, TopLeft, TopLeft, new Vector2(24, -4), new Vector2(190, 42));
                    next = NewText("NextText", slot, font, "", 22, TextAnchor.MiddleLeft, ResourceSubTextColor);
                    Place(next.rectTransform, TopLeft, TopLeft, new Vector2(24, -44), new Vector2(190, 28));
                }
                else
                {
                    Place(value.rectTransform, TopLeft, TopLeft, new Vector2(24, 0), new Vector2(hasPlus ? 190 : 244, 76));
                }

                value.resizeTextForBestFit = true;
                value.resizeTextMinSize = 18;
                value.resizeTextMaxSize = 32;
                value.horizontalOverflow = HorizontalWrapMode.Wrap;
                texts[i] = value;

                if (!hasPlus) continue;
                var button = NewButton("ResPlus_PH_56x56", slot, knob, font, "+", ButtonColor, new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(56, 56));
                ((Image)button.targetGraphic).type = Image.Type.Simple;
                var label = button.GetComponentInChildren<Text>();
                label.fontSize = 44;
                label.fontStyle = FontStyle.Bold;
                plus.Add(button);
            }

            var so = new SerializedObject(view);
            so.FindProperty("_bar").objectReferenceValue = bar;
            SetRefs(so.FindProperty("_slots"), slots);
            SetRefs(so.FindProperty("_slotTexts"), texts);
            so.FindProperty("_staminaNextText").objectReferenceValue = next;
            SetRefs(so.FindProperty("_plusButtons"), plus.ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // Nối phần HUD Lobby vào HubNav và ResourceBarView vào HubNav (HubNav dựng sau cả hai).
        public static void WireLobby(HubNav hub, ResourceBarView resources, GameObject lobbyHud, Button[] comingSoon, Button banner)
        {
            var so = new SerializedObject(hub);
            so.FindProperty("_lobbyHud").objectReferenceValue = lobbyHud;
            SetRefs(so.FindProperty("_comingSoonButtons"), comingSoon);
            so.FindProperty("_bannerButton").objectReferenceValue = banner;
            so.ApplyModifiedPropertiesWithoutUndo();

            var resourcesSo = new SerializedObject(resources);
            resourcesSo.FindProperty("_hub").objectReferenceValue = hub;
            resourcesSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // Hộp con vẽ trên thanh (spec 5.1 bước 6): Canvas con overrideSorting cao hơn canvas chính, kèm GraphicRaycaster.
        // Hộp nằm dưới view của nó nên tắt theo panel cha; Task 5, 6 gọi cho các hộp xác nhận và phần thưởng.
        public static void OverBar(RectTransform box)
        {
            var canvas = box.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10;
            box.gameObject.AddComponent<GraphicRaycaster>();
        }

        // Panel Menu: kín màn, nằm trên panel đang mở và dưới thanh. Bố cục theo spec 5.6; Task 4 nối Ngôn ngữ và Reset.
        public static RectTransform BuildMenu(RectTransform canvas, Font font, Sprite sprite, out MenuView view)
        {
            var panel = NewRect("MenuPanel_PH_1920x1080", canvas);
            Stretch(panel);
            AddImage(panel, null, Background, raycast: true);
            view = panel.gameObject.AddComponent<MenuView>();

            var title = NewLabel("MenuTitle", panel, font, "hub.menu_title", 64, TextAnchor.MiddleCenter, Gold);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, TopCenter, TopCenter, new Vector2(0, -40), new Vector2(800, 90));

            // Nhãn Ngôn ngữ do MenuView ghi lúc chạy (theo ngôn ngữ hiện tại), nên không gắn LocalizedText.
            var language = NewButton("MenuLanguageBtn_PH_700x100", panel, sprite, font, "", ButtonColor, Center, new Vector2(0, 170), new Vector2(700, 100));
            var reset = BattleSceneBuilder.LabelButton("MenuResetBtn_PH_700x100", panel, sprite, font, "hub.reset", DangerColor, Center, new Vector2(0, 40), new Vector2(700, 100));
            var dev = BattleSceneBuilder.LabelButton("MenuDevBtn_PH_700x100", panel, sprite, font, "hub.dev", GreyButton, Center, new Vector2(0, -90), new Vector2(700, 100));
            var status = NewText("MenuStatus", panel, font, "", 36, TextAnchor.MiddleCenter, Warning);
            Place(status.rectTransform, Center, Center, new Vector2(0, -190), new Vector2(1000, 60));
            var close = BattleSceneBuilder.LabelButton("MenuCloseBtn_PH_300x90", panel, sprite, font, "common.close", GreyButton, Center, new Vector2(0, -300), new Vector2(300, 90));

            // Hộp xác nhận Reset: nền mờ kín màn che cả thanh (OverBar) và hộp đỏ giữa màn.
            var confirmRoot = NewRect("ResetConfirmRoot", panel);
            Stretch(confirmRoot);
            AddImage(confirmRoot, null, Dim, raycast: true);
            OverBar(confirmRoot);
            var box = NewRect("ResetConfirm_PH_800x420", confirmRoot);
            Place(box, Center, Center, Vector2.zero, ResetConfirmSize);
            AddImage(box, sprite, DangerPanel, raycast: false);
            var prompt = NewLabel("PromptText", box, font, "hub.reset_confirm", 40, TextAnchor.MiddleCenter, Color.white);
            Place(prompt.rectTransform, TopCenter, TopCenter, new Vector2(0, -40), new Vector2(720, 200));
            var yes = BattleSceneBuilder.LabelButton("ResetYesBtn_PH_340x110", box, sprite, font, "hub.reset_yes", DangerColor, BottomCenter, new Vector2(-190, 50), new Vector2(340, 110));
            var no = BattleSceneBuilder.LabelButton("ResetNoBtn_PH_340x110", box, sprite, font, "common.cancel", GreyButton, BottomCenter, new Vector2(190, 50), new Vector2(340, 110));
            confirmRoot.gameObject.SetActive(false);

            var so = new SerializedObject(view);
            so.FindProperty("_languageButton").objectReferenceValue = language;
            so.FindProperty("_languageLabel").objectReferenceValue = language.GetComponentInChildren<Text>();
            so.FindProperty("_resetButton").objectReferenceValue = reset;
            so.FindProperty("_devButton").objectReferenceValue = dev;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.FindProperty("_statusText").objectReferenceValue = status;
            so.FindProperty("_resetConfirmRoot").objectReferenceValue = confirmRoot.gameObject;
            so.FindProperty("_resetYesButton").objectReferenceValue = yes;
            so.FindProperty("_resetNoButton").objectReferenceValue = no;
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.gameObject.SetActive(false);
            return panel;
        }

        private static Button BuildTab(RectTransform bar, Font font, Sprite sprite, int index)
        {
            var locked = index == (int)HubTab.Story;
            var tab = NewRect(TabNames[index], bar);
            tab.anchorMin = new Vector2(index / 6f, 0);
            tab.anchorMax = new Vector2((index + 1) / 6f, 1);
            tab.offsetMin = new Vector2(2, 4);
            tab.offsetMax = new Vector2(-2, -4);
            var image = AddImage(tab, sprite, locked ? LockedTabColor : TabColor, raycast: true);
            var button = tab.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var label = NewLabel("Label", tab, font, TabKeys[index], 36, TextAnchor.MiddleCenter, locked ? new Color(0.72f, 0.73f, 0.78f) : Color.white);
            Stretch(label.rectTransform);
            // Ô hẹp hoặc chữ Anh dài: chữ co lại thay vì tràn sang ô bên cạnh (Review Focus 5).
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 22;
            label.resizeTextMaxSize = 36;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            if (locked) BuildLock(tab, "Lock_PH_40x48", new Vector2(125, 0));
            return button;
        }

        // Ổ khoá ghép từ hình vuông, như ổ khoá ở nút bản đồ: quai, lỗ quai, thân vàng. Dùng cho ô Cốt truyện và nút Shop.
        internal static void BuildLock(RectTransform parent, string name, Vector2 position)
        {
            var root = NewRect(name, parent);
            Place(root, Center, Center, position, new Vector2(40, 48));
            var shackle = NewRect("Shackle", root);
            Place(shackle, Center, Center, new Vector2(0, 12), new Vector2(22, 24));
            AddImage(shackle, null, Dark, raycast: false);
            var hole = NewRect("ShackleHole", shackle);
            Place(hole, Center, Center, new Vector2(0, -3), new Vector2(10, 14));
            AddImage(hole, null, Muted, raycast: false);
            var body = NewRect("LockBody", root);
            Place(body, Center, Center, new Vector2(0, -9), new Vector2(40, 30));
            AddImage(body, null, Gold, raycast: false);
        }

        private static void SetRefs(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
