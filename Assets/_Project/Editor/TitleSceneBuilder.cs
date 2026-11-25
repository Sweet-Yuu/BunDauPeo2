using BunDauPeo.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static BunDauPeo.EditorTools.GachaSceneBuilder;

namespace BunDauPeo.EditorTools
{
    // Dựng scene Title bằng code theo spec UI hub C 5.1: nền, ô logo giữ chỗ, dòng "Chạm để bắt đầu", số phiên bản, và một lớp chạm kín màn
    // nằm trên cùng. Chỉ Image và Text (hình có sẵn của Unity); Logo_PH_900x300 để sau thay logo thật.
    internal static class TitleSceneBuilder
    {
        private static readonly Color Background = new(0.10f, 0.28f, 0.18f);
        private static readonly Color LogoColor = new(1f, 0.85f, 0.4f);
        private static readonly Color LogoText = new(0.25f, 0.15f, 0.05f);

        private static readonly Vector2 TopCenter = new(0.5f, 1);
        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 BottomRight = new(1, 0);

        public static void Create(string scenePath)
        {
            // Giống BattleSceneBuilder: batch mode chỉ có scene Untitled chưa lưu nên phải dùng Single.
            var mode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);

            // Nạp sau NewScene (L-002): Single dỡ asset không được scene nào dùng.
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateCamera(scene);
            CreateEventSystem(scene);
            var canvas = CreateCanvas(scene);
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 1f;

            var back = NewRect("TitleBackground", canvas);
            Stretch(back);
            AddImage(back, null, Background, raycast: false);

            var logo = NewRect("Logo_PH_900x300", canvas);
            Place(logo, TopCenter, TopCenter, new Vector2(0, -260), new Vector2(900, 300));
            AddImage(logo, sprite, LogoColor, raycast: false);
            var name = NewLabel("GameNameText", logo, font, "title.game_name", 110, TextAnchor.MiddleCenter, LogoText);
            name.fontStyle = FontStyle.Bold;
            Stretch(name.rectTransform);

            // TitleView ghi chữ lúc chạy ("Chạm để bắt đầu", rồi "Đang kết nối…") nên không gắn LocalizedText.
            var hint = NewText("TapHint", canvas, font, UiText.Get("title.tap_to_start"), 64, TextAnchor.MiddleCenter, Color.white);
            Place(hint.rectTransform, TopCenter, Center, new Vector2(0, -830), new Vector2(1200, 100));

            var version = NewText("VersionText", canvas, font, UiText.Format("title.version", Application.version), 36, TextAnchor.LowerRight, Color.white);
            Place(version.rectTransform, BottomRight, BottomRight, new Vector2(-30, 30), new Vector2(300, 60));

            // Anh em cuối = vẽ trên cùng và nhận chạm trước; trong suốt nhưng có raycast.
            var tap = NewRect("TapArea", canvas);
            Stretch(tap);
            var tapImage = AddImage(tap, null, new Color(0, 0, 0, 0), raycast: true);
            var tapButton = tap.gameObject.AddComponent<Button>();
            tapButton.targetGraphic = tapImage;
            tapButton.transition = Selectable.Transition.None;

            // Nằm sau TapArea trong hệ thống phân cấp = vẽ và nhận chạm trước TapArea (Online đợt 3, spec 8.3).
            var accountRect = NewRect("TitleAccountBtn_PH_480x90", canvas);
            Place(accountRect, TopCenter, Center, new Vector2(0, -960), new Vector2(480, 90));
            var accountImage = AddImage(accountRect, sprite, new Color(0.22f, 0.55f, 0.32f), raycast: true);
            var accountButton = accountRect.gameObject.AddComponent<Button>();
            accountButton.targetGraphic = accountImage;
            var accountLabel = NewLabel("Label", accountRect, font, "account.signin_btn", 36, TextAnchor.MiddleCenter, Color.white);
            Stretch(accountLabel.rectTransform);
            accountLabel.resizeTextForBestFit = true;
            accountLabel.resizeTextMinSize = 20;
            accountLabel.resizeTextMaxSize = 36;

            var view = canvas.gameObject.AddComponent<TitleView>();
            var so = new SerializedObject(view);
            so.FindProperty("_tapArea").objectReferenceValue = tapButton;
            so.FindProperty("_tapHint").objectReferenceValue = hint;
            so.FindProperty("_versionText").objectReferenceValue = version;
            so.FindProperty("_accountButton").objectReferenceValue = accountButton;
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
            go.AddComponent<AudioListener>();
        }
    }
}
