using BunDauPeo.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.EditorTools
{
    // Dựng scene Nhân vật bằng code, theo khuôn GachaSceneBuilder: Image với sprite có sẵn của Unity, chữ là Text uGUI.
    // Danh sách thẻ phủ cả panel; bấm thẻ mở DetailRoot (phủ lên danh sách) có tab Cấp / Sao, mũi tên ‹ ›, nút Quay lại (spec cấp nhân vật mục 8).
    internal static class CharactersSceneBuilder
    {
        // shortcut: 12 thẻ cố định (4 cột × 3 hàng), đủ cho 6 nhân vật hiện có; nhiều hơn thì thêm cuộn.
        private const int CardCount = 12;
        private const int Columns = 4;

        private static readonly Color Background = new(0.10f, 0.11f, 0.15f);
        private static readonly Color Panel = new(0.18f, 0.20f, 0.27f);
        private static readonly Color ButtonColor = new(0.30f, 0.50f, 0.90f);
        private static readonly Color TabColor = new(0.30f, 0.34f, 0.46f);
        private static readonly Color Gold = new(1f, 0.85f, 0.4f);
        private static readonly Color ExpFill = new(0.35f, 0.75f, 0.40f);
        private static readonly Color Dim = new(0f, 0f, 0f, 0.65f);

        private static readonly Vector2 TopLeft = new(0, 1);
        private static readonly Vector2 TopRight = new(1, 1);
        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 BottomCenter = new(0.5f, 0);

        // Panel Nhân vật của Lobby (spec UI hub 5.4): kín màn, nền đặc chặn bấm, lúc đầu tắt. Lưới và panel chi tiết đã nằm trên vạch 120 px.
        public static RectTransform BuildPanel(RectTransform canvas, Sprite sprite, Font font)
        {
            var root = GachaSceneBuilder.NewRect("CharactersPanel_PH_1920x1080", canvas);
            GachaSceneBuilder.Stretch(root);
            GachaSceneBuilder.AddImage(root, sprite, Background, raycast: true);

            var title = GachaSceneBuilder.NewLabel("TitleText", root, font, "characters.title", 56, TextAnchor.MiddleLeft, Color.white);
            GachaSceneBuilder.Place(title.rectTransform, TopLeft, TopLeft, new Vector2(40, -30), new Vector2(800, 80));
            NewLevelBar("PlayerLevelBar_PH_420x80", root, sprite, font, 480);

            // Lưới thẻ.
            var cards = new CharacterCardView[CardCount];
            const float cardW = 430, cardH = 190, gap = 30;
            for (var i = 0; i < CardCount; i++)
            {
                var position = new Vector2(40 + (i % Columns) * (cardW + gap), -140 - (i / Columns) * (cardH + 20));
                cards[i] = NewCard(i, root, sprite, font, position, new Vector2(cardW, cardH));
            }

            // Panel chi tiết phủ lên danh sách.
            var detail = GachaSceneBuilder.NewRect("DetailRoot", root);
            GachaSceneBuilder.Stretch(detail);
            GachaSceneBuilder.AddImage(detail, sprite, Background, raycast: true);

            var back = Labeled(GachaSceneBuilder.NewButton("BackButton", detail, sprite, font, "", TabColor, TopLeft, new Vector2(40, -30), new Vector2(260, 90)),
                "characters.back");
            var detailTitle = GachaSceneBuilder.NewLabel("DetailTitleText", detail, font, "characters.detail_title", 52, TextAnchor.MiddleLeft, Color.white);
            GachaSceneBuilder.Place(detailTitle.rectTransform, TopLeft, TopLeft, new Vector2(340, -30), new Vector2(600, 90));
            NewLevelBar("PlayerLevelBar_PH_420x80", detail, sprite, font, 960);

            // Trái: ảnh nhân vật (hình tạm), tên, độ hiếm, sao, cấp và chỉ số hiện tại.
            var portrait = GachaSceneBuilder.NewRect("DetailPortrait_PH_640x440", detail);
            GachaSceneBuilder.Place(portrait, TopLeft, TopLeft, new Vector2(40, -150), new Vector2(640, 440));
            GachaSceneBuilder.AddImage(portrait, sprite, Panel, raycast: false);
            var prev = Labeled(GachaSceneBuilder.NewButton("PrevButton", detail, sprite, font, "", TabColor, TopLeft, new Vector2(50, -290), new Vector2(90, 140)),
                "characters.prev");
            var next = Labeled(GachaSceneBuilder.NewButton("NextButton", detail, sprite, font, "", TabColor, TopLeft, new Vector2(580, -290), new Vector2(90, 140)),
                "characters.next");
            var name = Line("NameText", detail, font, 52, TextAnchor.MiddleLeft, Color.white, 40, -610, 440, 70);
            var rarity = Line("RarityText", detail, font, 38, TextAnchor.MiddleLeft, Gold, 40, -610, 200, 70);
            rarity.rectTransform.anchoredPosition = new Vector2(500, -610);
            var stars = Line("StarsText", detail, font, 44, TextAnchor.MiddleLeft, Gold, 40, -685, 640, 60);
            var levelLine = Line("LevelLineText", detail, font, 38, TextAnchor.MiddleLeft, Color.white, 40, -750, 300, 50);
            var hp = Line("HpText", detail, font, 34, TextAnchor.MiddleLeft, Color.white, 340, -750, 340, 50);
            var attack = Line("AttackText", detail, font, 34, TextAnchor.MiddleLeft, Color.white, 40, -800, 300, 50);
            var defense = Line("DefenseText", detail, font, 34, TextAnchor.MiddleLeft, Color.white, 340, -800, 340, 50);
            var speed = Line("SpeedText", detail, font, 34, TextAnchor.MiddleLeft, Color.white, 40, -850, 300, 50);

            // Phải: hai tab và nội dung của tab.
            var levelTab = Labeled(GachaSceneBuilder.NewButton("LevelTabButton", detail, sprite, font, "", ButtonColor, TopLeft, new Vector2(760, -150), new Vector2(320, 90)),
                "characters.tab_level");
            var starTab = Labeled(GachaSceneBuilder.NewButton("StarTabButton", detail, sprite, font, "", TabColor, TopLeft, new Vector2(1100, -150), new Vector2(320, 90)),
                "characters.tab_star");

            var levelGroup = GachaSceneBuilder.NewRect("LevelGroup", detail);
            GachaSceneBuilder.Place(levelGroup, TopLeft, TopLeft, new Vector2(760, -270), new Vector2(1120, 560));
            var levelChange = Line("LevelChangeText", levelGroup, font, 44, TextAnchor.MiddleLeft, Color.white, 0, 0, 1120, 70);
            var levelPreview = Line("LevelPreviewText", levelGroup, font, 38, TextAnchor.MiddleLeft, Color.white, 0, -80, 1120, 70);
            var levelCap = Line("LevelCapText", levelGroup, font, 36, TextAnchor.MiddleLeft, Gold, 0, -170, 1120, 60);
            var potion = Line("PotionText", levelGroup, font, 38, TextAnchor.MiddleLeft, Color.white, 0, -250, 1120, 60);
            var coin = Line("CoinText", levelGroup, font, 38, TextAnchor.MiddleLeft, Color.white, 0, -320, 1120, 60);
            var levelUp = GachaSceneBuilder.NewButton("LevelUpButton", levelGroup, sprite, font, "", ButtonColor, TopLeft, new Vector2(0, -420), new Vector2(760, 110));
            var levelUpLabel = levelUp.GetComponentInChildren<Text>();

            var starGroup = GachaSceneBuilder.NewRect("StarGroup", detail);
            GachaSceneBuilder.Place(starGroup, TopLeft, TopLeft, new Vector2(760, -270), new Vector2(1120, 560));
            var starPreview = Line("StarPreviewText", starGroup, font, 38, TextAnchor.MiddleLeft, Color.white, 0, 0, 1120, 70);
            var fragments = Line("FragmentsText", starGroup, font, 40, TextAnchor.MiddleLeft, Gold, 0, -90, 1120, 60);
            var starUp = GachaSceneBuilder.NewButton("StarUpButton", starGroup, sprite, font, "", ButtonColor, TopLeft, new Vector2(0, -190), new Vector2(760, 110));
            var starUpLabel = starUp.GetComponentInChildren<Text>();

            var status = Line("StarUpStatus", detail, font, 36, TextAnchor.MiddleLeft, Gold, 760, -790, 1120, 80);

            var viewObject = GachaSceneBuilder.NewRect("CharactersView", root);
            var view = viewObject.gameObject.AddComponent<CharactersView>();
            var so = new SerializedObject(view);
            var cardsProp = so.FindProperty("_cards");
            cardsProp.arraySize = CardCount;
            for (var i = 0; i < CardCount; i++) cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            Set(so, "_detailRoot", detail.gameObject);
            Set(so, "_backButton", back);
            Set(so, "_prevButton", prev);
            Set(so, "_nextButton", next);
            Set(so, "_levelTabButton", levelTab);
            Set(so, "_starTabButton", starTab);
            Set(so, "_levelGroup", levelGroup.gameObject);
            Set(so, "_starGroup", starGroup.gameObject);
            Set(so, "_nameText", name);
            Set(so, "_rarityText", rarity);
            Set(so, "_starsText", stars);
            Set(so, "_levelLineText", levelLine);
            Set(so, "_hpText", hp);
            Set(so, "_attackText", attack);
            Set(so, "_defenseText", defense);
            Set(so, "_speedText", speed);
            Set(so, "_levelChangeText", levelChange);
            Set(so, "_levelPreviewText", levelPreview);
            Set(so, "_levelCapText", levelCap);
            Set(so, "_potionText", potion);
            Set(so, "_coinText", coin);
            Set(so, "_levelUpButton", levelUp);
            Set(so, "_levelUpLabel", levelUpLabel);
            Set(so, "_starPreviewText", starPreview);
            Set(so, "_fragmentsText", fragments);
            Set(so, "_starUpButton", starUp);
            Set(so, "_starUpLabel", starUpLabel);
            Set(so, "_statusText", status);
            so.ApplyModifiedPropertiesWithoutUndo();

            detail.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            return root;
        }

        // Popup lên cấp người chơi (spec mục 8): hộp giữa màn trên nền mờ, vẽ đè lên thanh dưới; lúc đầu tắt.
        public static LevelUpPopupView BuildLevelUpPopup(RectTransform canvas, Sprite sprite, Font font)
        {
            var root = GachaSceneBuilder.NewRect("LevelUpPopupRoot", canvas);
            GachaSceneBuilder.Stretch(root);
            GachaSceneBuilder.AddImage(root, null, Dim, raycast: true);
            var box = GachaSceneBuilder.NewRect("LevelUpPopupBox", root);
            GachaSceneBuilder.Place(box, Center, Center, Vector2.zero, new Vector2(900, 560));
            GachaSceneBuilder.AddImage(box, sprite, Panel, raycast: false);
            var title = GachaSceneBuilder.NewLabel("TitleText", box, font, "level.up_title", 64, TextAnchor.MiddleCenter, Gold);
            GachaSceneBuilder.Place(title.rectTransform, TopLeft, TopLeft, new Vector2(50, -40), new Vector2(800, 90));
            var body = GachaSceneBuilder.NewText("BodyText", box, font, "", 40, TextAnchor.UpperCenter, Color.white);
            GachaSceneBuilder.Place(body.rectTransform, TopLeft, TopLeft, new Vector2(50, -160), new Vector2(800, 220));
            var ok = Labeled(GachaSceneBuilder.NewButton("LevelUpOkButton", box, sprite, font, "", ButtonColor, BottomCenter, new Vector2(0, 40), new Vector2(320, 100)),
                "level.up_ok");
            NavBarBuilder.OverBar(root);
            root.GetComponent<Canvas>().sortingOrder = 20; // trên WorldScroll và các hộp khác của Lobby (cùng bậc 10)
            root.gameObject.SetActive(false);

            var popup = GachaSceneBuilder.NewRect("LevelUpPopup", canvas).gameObject.AddComponent<LevelUpPopupView>();
            var so = new SerializedObject(popup);
            Set(so, "_root", root.gameObject);
            Set(so, "_bodyText", body);
            Set(so, "_okButton", ok);
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        private static void Set(SerializedObject so, string field, Object value) => so.FindProperty(field).objectReferenceValue = value;

        // Chữ nhãn của nút lấy từ UiText và gắn LocalizedText để đổi theo ngôn ngữ.
        private static Button Labeled(Button button, string key)
        {
            var label = button.GetComponentInChildren<Text>();
            label.text = UiText.Get(key);
            label.gameObject.AddComponent<LocalizedText>().SetKey(key);
            return button;
        }

        // Thanh Lv + EXP người chơi ở trên thanh tiêu đề, tránh thanh tài nguyên bên phải: nền tối, lớp xanh kéo từ trái, "Lv n" bên trái và "exp/cần" bên phải.
        private static void NewLevelBar(string name, RectTransform parent, Sprite sprite, Font font, float x)
        {
            var bar = GachaSceneBuilder.NewRect(name, parent);
            GachaSceneBuilder.Place(bar, TopLeft, TopLeft, new Vector2(x, -30), new Vector2(420, 80));
            GachaSceneBuilder.AddImage(bar, sprite, Panel, raycast: false);
            var fill = GachaSceneBuilder.NewRect("Fill", bar);
            GachaSceneBuilder.Stretch(fill);
            GachaSceneBuilder.AddImage(fill, sprite, ExpFill, raycast: false);
            var level = GachaSceneBuilder.NewText("LevelText", bar, font, "", 40, TextAnchor.MiddleLeft, Color.white);
            GachaSceneBuilder.Place(level.rectTransform, TopLeft, TopLeft, new Vector2(20, 0), new Vector2(200, 80));
            var exp = GachaSceneBuilder.NewText("ExpText", bar, font, "", 32, TextAnchor.MiddleRight, Color.white);
            GachaSceneBuilder.Place(exp.rectTransform, TopRight, TopRight, new Vector2(-20, 0), new Vector2(240, 80));

            var view = bar.gameObject.AddComponent<PlayerLevelBarView>();
            var so = new SerializedObject(view);
            Set(so, "_levelText", level);
            Set(so, "_expText", exp);
            Set(so, "_fill", fill);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Dòng chữ ghi lúc chạy (không LocalizedText).
        private static Text Line(string name, RectTransform parent, Font font, int size, TextAnchor align, Color color, float x, float y, float width, float height)
        {
            var text = GachaSceneBuilder.NewText(name, parent, font, "", size, align, color);
            GachaSceneBuilder.Place(text.rectTransform, TopLeft, TopLeft, new Vector2(x, y), new Vector2(width, height));
            return text;
        }

        // Khung (viền chọn) là Image của thẻ; chân dung màu độ hiếm lùi 8 px nên viền lộ ra.
        private static CharacterCardView NewCard(int index, RectTransform parent, Sprite sprite, Font font, Vector2 position, Vector2 size)
        {
            var root = GachaSceneBuilder.NewRect("CharacterCard" + index, parent);
            GachaSceneBuilder.Place(root, TopLeft, TopLeft, position, size);
            var frame = GachaSceneBuilder.AddImage(root, sprite, Color.white, raycast: true);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;

            var portraitRect = GachaSceneBuilder.NewRect("Portrait", root);
            GachaSceneBuilder.Stretch(portraitRect, 8);
            var portrait = GachaSceneBuilder.AddImage(portraitRect, sprite, Color.white, raycast: false);

            var textWidth = size.x - 48;
            var name = GachaSceneBuilder.NewText("NameText", root, font, "", 36, TextAnchor.MiddleLeft, Color.white);
            GachaSceneBuilder.Place(name.rectTransform, TopLeft, TopLeft, new Vector2(24, -16), new Vector2(textWidth, 50));
            var info = GachaSceneBuilder.NewText("InfoText", root, font, "", 34, TextAnchor.MiddleLeft, Color.white);
            GachaSceneBuilder.Place(info.rectTransform, TopLeft, TopLeft, new Vector2(24, -72), new Vector2(textWidth, 46));
            var fragments = GachaSceneBuilder.NewText("FragmentsText", root, font, "", 34, TextAnchor.MiddleLeft, Color.white);
            GachaSceneBuilder.Place(fragments.rectTransform, TopLeft, TopLeft, new Vector2(24, -124), new Vector2(textWidth, 46));

            var view = root.gameObject.AddComponent<CharacterCardView>();
            var so = new SerializedObject(view);
            Set(so, "_button", button);
            Set(so, "_frame", frame);
            Set(so, "_portrait", portrait);
            Set(so, "_name", name);
            Set(so, "_info", info);
            Set(so, "_fragments", fragments);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
    }
}
