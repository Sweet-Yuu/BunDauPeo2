using BunDauPeo.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.EditorTools
{
    // Dựng scene Nhân vật bằng code, theo khuôn GachaSceneBuilder: Image với sprite có sẵn của Unity, chữ là Text uGUI.
    internal static class CharactersSceneBuilder
    {
        // shortcut: 12 thẻ cố định (3 cột × 4 hàng), đủ cho 6 nhân vật hiện có; nhiều hơn thì thêm cuộn.
        private const int CardCount = 12;
        private const int Columns = 3;

        private static readonly Color Background = new(0.10f, 0.11f, 0.15f);
        private static readonly Color Panel = new(0.18f, 0.20f, 0.27f);
        private static readonly Color ButtonColor = new(0.30f, 0.50f, 0.90f);
        private static readonly Color Gold = new(1f, 0.85f, 0.4f);

        private static readonly Vector2 TopLeft = new(0, 1);
        private static readonly Vector2 TopRight = new(1, 1);

        public static void Create(string scenePath)
        {
            var mode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);

            // Nạp sau NewScene (L-002): Single dỡ asset không được scene nào dùng.
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GachaSceneBuilder.CreateCamera(scene);
            GachaSceneBuilder.CreateEventSystem(scene);
            var canvas = GachaSceneBuilder.CreateCanvas(scene);

            var back = GachaSceneBuilder.NewRect("Background", canvas);
            GachaSceneBuilder.Stretch(back);
            GachaSceneBuilder.AddImage(back, sprite, Background, raycast: false);

            var title = GachaSceneBuilder.NewLabel("TitleText", canvas, font, "characters.title", 56, TextAnchor.MiddleLeft, Color.white);
            GachaSceneBuilder.Place(title.rectTransform, TopLeft, TopLeft, new Vector2(40, -30), new Vector2(800, 80));

            // Lưới thẻ bên trái.
            var cards = new CharacterCardView[CardCount];
            const float cardW = 320, cardH = 190, gap = 20;
            for (var i = 0; i < CardCount; i++)
            {
                var position = new Vector2(40 + (i % Columns) * (cardW + gap), -140 - (i / Columns) * (cardH + gap));
                cards[i] = NewCard(i, canvas, sprite, font, position, new Vector2(cardW, cardH));
            }

            // Panel phải.
            var panel = GachaSceneBuilder.NewRect("DetailPanel", canvas);
            GachaSceneBuilder.Place(panel, TopRight, TopRight, new Vector2(-40, -140), new Vector2(760, 820));
            GachaSceneBuilder.AddImage(panel, sprite, Panel, raycast: false);

            var name = Line("NameText", panel, font, 56, TextAnchor.MiddleLeft, Color.white, -20, 80);
            var rarity = Line("RarityText", panel, font, 40, TextAnchor.MiddleLeft, Gold, -100, 60);
            var stars = Line("StarsText", panel, font, 48, TextAnchor.MiddleRight, Gold, -100, 60);
            var hp = Line("HpText", panel, font, 42, TextAnchor.MiddleLeft, Color.white, -190, 60);
            var attack = Line("AttackText", panel, font, 42, TextAnchor.MiddleLeft, Color.white, -255, 60);
            var defense = Line("DefenseText", panel, font, 42, TextAnchor.MiddleLeft, Color.white, -320, 60);
            var speed = Line("SpeedText", panel, font, 42, TextAnchor.MiddleLeft, Color.white, -385, 60);
            var fragments = Line("FragmentsText", panel, font, 40, TextAnchor.MiddleLeft, Gold, -470, 60);

            var button = GachaSceneBuilder.NewButton("StarUpButton", panel, sprite, font, "", ButtonColor,
                TopLeft, new Vector2(40, -560), new Vector2(680, 110));
            var buttonLabel = button.GetComponentInChildren<Text>();
            var status = Line("StarUpStatus", panel, font, 34, TextAnchor.MiddleCenter, Gold, -700, 80);

            var viewObject = GachaSceneBuilder.NewRect("CharactersView", canvas);
            var view = viewObject.gameObject.AddComponent<CharactersView>();
            var so = new SerializedObject(view);
            var cardsProp = so.FindProperty("_cards");
            cardsProp.arraySize = CardCount;
            for (var i = 0; i < CardCount; i++) cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            so.FindProperty("_nameText").objectReferenceValue = name;
            so.FindProperty("_rarityText").objectReferenceValue = rarity;
            so.FindProperty("_starsText").objectReferenceValue = stars;
            so.FindProperty("_hpText").objectReferenceValue = hp;
            so.FindProperty("_attackText").objectReferenceValue = attack;
            so.FindProperty("_defenseText").objectReferenceValue = defense;
            so.FindProperty("_speedText").objectReferenceValue = speed;
            so.FindProperty("_fragmentsText").objectReferenceValue = fragments;
            so.FindProperty("_starUpButton").objectReferenceValue = button;
            so.FindProperty("_starUpLabel").objectReferenceValue = buttonLabel;
            so.FindProperty("_statusText").objectReferenceValue = status;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, scenePath);
            if (mode == NewSceneMode.Additive) EditorSceneManager.CloseScene(scene, true);
        }

        // Dòng chữ ghi lúc chạy (không LocalizedText): rộng hết panel trừ lề 40.
        private static Text Line(string name, RectTransform panel, Font font, int size, TextAnchor align, Color color, float y, float height)
        {
            var text = GachaSceneBuilder.NewText(name, panel, font, "", size, align, color);
            GachaSceneBuilder.Place(text.rectTransform, TopLeft, TopLeft, new Vector2(40, y), new Vector2(680, height));
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

            var name = GachaSceneBuilder.NewText("NameText", root, font, "", 36, TextAnchor.MiddleLeft, Color.white);
            GachaSceneBuilder.Place(name.rectTransform, TopLeft, TopLeft, new Vector2(24, -16), new Vector2(272, 50));
            var info = GachaSceneBuilder.NewText("InfoText", root, font, "", 34, TextAnchor.MiddleLeft, Color.white);
            GachaSceneBuilder.Place(info.rectTransform, TopLeft, TopLeft, new Vector2(24, -72), new Vector2(272, 46));
            var fragments = GachaSceneBuilder.NewText("FragmentsText", root, font, "", 34, TextAnchor.MiddleLeft, Color.white);
            GachaSceneBuilder.Place(fragments.rectTransform, TopLeft, TopLeft, new Vector2(24, -124), new Vector2(272, 46));

            var view = root.gameObject.AddComponent<CharacterCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_frame").objectReferenceValue = frame;
            so.FindProperty("_portrait").objectReferenceValue = portrait;
            so.FindProperty("_name").objectReferenceValue = name;
            so.FindProperty("_info").objectReferenceValue = info;
            so.FindProperty("_fragments").objectReferenceValue = fragments;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
    }
}
