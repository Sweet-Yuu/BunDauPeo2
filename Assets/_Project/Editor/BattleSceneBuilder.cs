using BunDauPeo.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static BunDauPeo.EditorTools.GachaSceneBuilder;

namespace BunDauPeo.EditorTools
{
    // Dựng scene Battle bằng code theo spec Combat 5.4, cùng khuôn RestaurantSceneBuilder. Hình đều là Image: phe ta Knob, quái hình vuông đặc,
    // thanh HP là UISprite dạng Filled. Chữ là Text của uGUI (docs dự án thắng skill ui-ugui, xem GachaSceneBuilder).
    internal static class BattleSceneBuilder
    {
        private static readonly Color Background = new(0.13f, 0.15f, 0.20f);
        private static readonly Color EnemyColor = new(0.55f, 0.12f, 0.15f);
        private static readonly Color HpBack = new(0.05f, 0.05f, 0.08f, 0.85f);
        private static readonly Color HpGreen = new(0.35f, 0.85f, 0.40f);
        private static readonly Color Marker = new(1f, 0.85f, 0.2f);
        private static readonly Color ButtonColor = new(0.30f, 0.50f, 0.90f);

        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 TopCenter = new(0.5f, 1);

        public static void Create(string scenePath)
        {
            // Giống CreateBootScene: batch mode chỉ có scene Untitled chưa lưu nên phải dùng Single.
            var mode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);

            // Nạp sau NewScene (L-002): Single dỡ asset không được scene nào dùng.
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateCamera(scene);
            CreateEventSystem(scene);
            var canvas = CreateCanvas(scene);
            // Bố cục cao đúng 1080 nên khớp theo chiều cao.
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 1f;

            var back = NewRect("Background", canvas);
            Stretch(back);
            AddImage(back, null, Background, raycast: false);

            var units = NewRect("Units", canvas);
            Stretch(units);
            var allies = new BattleUnitView[5];
            var enemies = new BattleUnitView[5];
            for (var slot = 0; slot < 5; slot++)
            {
                allies[slot] = BuildUnit(units, $"Ally{slot}", BattleLayout.AllyRect(slot), knob, sprite, font, true);
                enemies[slot] = BuildUnit(units, $"Enemy{slot}", BattleLayout.EnemyRect(slot, false), null, sprite, font, false);
            }

            // Mẫu số bay lên; view nhân bản rồi bật. Nằm sau đơn vị nên vẽ trên chúng.
            var floatTemplate = NewText("FloatTemplate", canvas, font, "0", 48, TextAnchor.MiddleCenter, Color.white);
            floatTemplate.fontStyle = FontStyle.Bold;
            Place(floatTemplate.rectTransform, Center, Center, Vector2.zero, new Vector2(200, 60));
            floatTemplate.gameObject.SetActive(false);

            var empty = NewRect("EmptyRoot", canvas);
            Stretch(empty);
            var emptyText = NewText("EmptyText", empty, font, "Chưa có trận", 56, TextAnchor.MiddleCenter, Color.white);
            Place(emptyText.rectTransform, Center, Center, new Vector2(0, 40), new Vector2(800, 90));
            var emptyBack = NewButton("EmptyBackButton", empty, sprite, font, "Về", ButtonColor, Center, new Vector2(0, -80), new Vector2(260, 90));

            var viewObject = NewRect("BattleView", canvas);
            var view = viewObject.gameObject.AddComponent<BattleView>();
            var so = new SerializedObject(view);
            SetRefs(so.FindProperty("_allies"), allies);
            SetRefs(so.FindProperty("_enemies"), enemies);
            so.FindProperty("_floatTemplate").objectReferenceValue = floatTemplate;
            so.FindProperty("_emptyRoot").objectReferenceValue = empty.gameObject;
            so.FindProperty("_emptyBackButton").objectReferenceValue = emptyBack;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, scenePath);
            if (mode == NewSceneMode.Additive) EditorSceneManager.CloseScene(scene, true);
        }

        // Hộp tắt sẵn: view bật và đặt lại vị trí, cỡ, màu khi có trận (bảng màu class nằm ở BattleUnitView).
        private static BattleUnitView BuildUnit(RectTransform parent, string name, Rect box, Sprite bodySprite, Sprite barSprite, Font font, bool ally)
        {
            var root = NewRect(name, parent);
            Place(root, Center, Center, BattleLayout.ToAnchored(box.center), box.size);
            var body = AddImage(root, bodySprite, ally ? Color.white : EnemyColor, raycast: true);
            body.type = Image.Type.Simple;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;

            // Tên dưới thân, chữ trắng: Knob là hình tròn nên chữ trong thân tràn ra ngoài rìa và khó đọc.
            var label = NewText("Name", root, font, "", 26, TextAnchor.UpperCenter, Color.white);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = new Vector2(1, 0);
            label.rectTransform.pivot = new Vector2(0.5f, 1);
            label.rectTransform.anchoredPosition = new Vector2(0, -4);
            label.rectTransform.sizeDelta = new Vector2(0, 40);

            var hpBack = NewRect("HpBack", root);
            hpBack.anchorMin = TopCenter - new Vector2(0.5f, 0);
            hpBack.anchorMax = TopCenter + new Vector2(0.5f, 0);
            hpBack.pivot = TopCenter;
            hpBack.anchoredPosition = new Vector2(0, -8);
            hpBack.sizeDelta = new Vector2(-20, 14);
            AddImage(hpBack, barSprite, HpBack, raycast: false);
            var fillRect = NewRect("HpFill", hpBack);
            Stretch(fillRect, 2);
            var fill = AddImage(fillRect, barSprite, HpGreen, raycast: false);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;

            // Không có sprite tam giác có sẵn: mũi tên mục tiêu là hình vuông xoay 45° (hình thoi) trên đầu.
            var marker = NewRect("TargetMarker", root);
            Place(marker, TopCenter, Center, new Vector2(0, 32), new Vector2(32, 32));
            marker.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var markerImage = AddImage(marker, null, Marker, raycast: false);
            markerImage.enabled = false;

            var view = root.gameObject.AddComponent<BattleUnitView>();
            var so = new SerializedObject(view);
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_name").objectReferenceValue = label;
            so.FindProperty("_hpFill").objectReferenceValue = fill;
            so.FindProperty("_targetMarker").objectReferenceValue = markerImage;
            so.FindProperty("_button").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            return view;
        }

        private static void SetRefs(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void CreateCamera(Scene scene)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            SceneManager.MoveGameObjectToScene(go, scene);
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            go.AddComponent<AudioListener>();
        }
    }
}
