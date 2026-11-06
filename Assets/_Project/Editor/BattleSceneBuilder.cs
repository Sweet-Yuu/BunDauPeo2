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

        private static readonly Color Gold = new(1f, 0.85f, 0.4f);
        private static readonly Color CardFrame = new(0.30f, 0.33f, 0.45f);
        private static readonly Color CardColor = new(0.16f, 0.18f, 0.26f);
        private static readonly Color EnergyBlue = new(0.35f, 0.70f, 0.95f);

        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 TopCenter = new(0.5f, 1);
        private static readonly Vector2 TopLeft = new(0, 1);
        private static readonly Vector2 BottomLeft = new(0, 0);
        private static readonly Vector2 BottomRight = new(1, 0);
        private static readonly Vector2 TopRight = new(1, 1);

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

            var cards = new BattleCardView[5];
            for (var i = 0; i < cards.Length; i++) cards[i] = BuildCard(canvas, i, sprite, font);

            var panel = BuildActionPanel(canvas, knob, font);

            var topBar = BuildTopBar(canvas, sprite, font, out var waveText, out var turnCells, out var menu, out var auto, out var speed);

            // Banner "Đợt 2/3": chữ lớn giữa màn, bật 1 giây khi sang đợt mới; không chặn chạm.
            var waveBanner = NewText("WaveBanner", canvas, font, "", 120, TextAnchor.MiddleCenter, Gold);
            waveBanner.fontStyle = FontStyle.Bold;
            Place(waveBanner.rectTransform, Center, Center, new Vector2(0, 200), new Vector2(900, 160));
            waveBanner.gameObject.SetActive(false);

            var empty = NewRect("EmptyRoot", canvas);
            Stretch(empty);
            var emptyText = NewText("EmptyText", empty, font, "Chưa có trận", 56, TextAnchor.MiddleCenter, Color.white);
            Place(emptyText.rectTransform, Center, Center, new Vector2(0, 40), new Vector2(800, 90));
            var emptyBack = NewButton("EmptyBackButton", empty, sprite, font, "Về", ButtonColor, Center, new Vector2(0, -80), new Vector2(260, 90));

            // Popup tạm dừng dựng sau thanh trên nên chặn chạm xuống nó; banner kết thúc vẫn nằm trên.
            var pause = BuildPausePopup(canvas, sprite, font);

            // Màn kết quả dựng sau cùng nên vẽ trên mọi thứ (và chặn chạm xuống dưới).
            var result = BuildResultView(canvas, sprite, font);

            var viewObject = NewRect("BattleView", canvas);
            var view = viewObject.gameObject.AddComponent<BattleView>();
            var so = new SerializedObject(view);
            SetRefs(so.FindProperty("_allies"), allies);
            SetRefs(so.FindProperty("_enemies"), enemies);
            so.FindProperty("_floatTemplate").objectReferenceValue = floatTemplate;
            so.FindProperty("_emptyRoot").objectReferenceValue = empty.gameObject;
            so.FindProperty("_emptyBackButton").objectReferenceValue = emptyBack;
            so.FindProperty("_panel").objectReferenceValue = panel;
            SetRefs(so.FindProperty("_cards"), cards);
            so.FindProperty("_result").objectReferenceValue = result;
            so.FindProperty("_topBar").objectReferenceValue = topBar.gameObject;
            so.FindProperty("_waveText").objectReferenceValue = waveText;
            SetRefs(so.FindProperty("_turnCells"), turnCells);
            so.FindProperty("_menuButton").objectReferenceValue = menu;
            so.FindProperty("_autoButton").objectReferenceValue = auto;
            so.FindProperty("_speedButton").objectReferenceValue = speed;
            so.FindProperty("_waveBanner").objectReferenceValue = waveBanner.gameObject;
            so.FindProperty("_waveBannerText").objectReferenceValue = waveBanner;
            so.FindProperty("_pause").objectReferenceValue = pause;
            so.ApplyModifiedPropertiesWithoutUndo();

            var panelSo = new SerializedObject(panel);
            panelSo.FindProperty("_view").objectReferenceValue = view;
            panelSo.ApplyModifiedPropertiesWithoutUndo();

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

        // Thẻ dưới trái trong dải y 820-980 (cao 150, đáy cách mép dưới 105): viền là nền thẻ, thân thẻ thụt vào 5 px.
        private static BattleCardView BuildCard(RectTransform canvas, int index, Sprite sprite, Font font)
        {
            var root = NewRect($"Card{index}", canvas);
            Place(root, BottomLeft, BottomLeft, new Vector2(40 + index * 192, 105), new Vector2(180, 150));
            var frame = AddImage(root, sprite, CardFrame, raycast: true);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;

            var body = NewRect("Body", root);
            Stretch(body, 5);
            AddImage(body, sprite, CardColor, raycast: false);

            var portraitRect = NewRect("Portrait", root);
            Place(portraitRect, TopLeft, TopLeft, new Vector2(12, -12), new Vector2(64, 64));
            var portrait = AddImage(portraitRect, null, Color.white, raycast: false);

            var name = NewText("Name", root, font, "", 20, TextAnchor.MiddleLeft, Color.white);
            Place(name.rectTransform, TopLeft, TopLeft, new Vector2(84, -10), new Vector2(92, 30));
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            var ready = NewText("ReadyTag", root, font, "", 26, TextAnchor.MiddleLeft, Gold);
            ready.fontStyle = FontStyle.Bold;
            Place(ready.rectTransform, TopLeft, TopLeft, new Vector2(84, -44), new Vector2(92, 32));
            var hp = NewText("HpText", root, font, "", 22, TextAnchor.MiddleCenter, Color.white);
            Place(hp.rectTransform, BottomLeft, BottomLeft, new Vector2(10, 46), new Vector2(160, 28));

            var hpFill = Bar(root, "Hp", new Vector2(10, 30), HpGreen, sprite);
            var energyFill = Bar(root, "Energy", new Vector2(10, 12), EnergyBlue, sprite);

            var view = root.gameObject.AddComponent<BattleCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("_portrait").objectReferenceValue = portrait;
            so.FindProperty("_name").objectReferenceValue = name;
            so.FindProperty("_hp").objectReferenceValue = hp;
            so.FindProperty("_hpFill").objectReferenceValue = hpFill;
            so.FindProperty("_energyFill").objectReferenceValue = energyFill;
            so.FindProperty("_frame").objectReferenceValue = frame;
            so.FindProperty("_readyTag").objectReferenceValue = ready;
            so.FindProperty("_button").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            return view;
        }

        // Thanh nằm ngang 160×12: nền tối và lớp Filled; trả lớp Filled.
        private static Image Bar(RectTransform parent, string name, Vector2 position, Color color, Sprite sprite)
        {
            var back = NewRect(name + "Back", parent);
            Place(back, BottomLeft, BottomLeft, position, new Vector2(160, 12));
            AddImage(back, sprite, HpBack, raycast: false);
            var fillRect = NewRect(name + "Fill", back);
            Stretch(fillRect, 2);
            var fill = AddImage(fillRect, sprite, color, raycast: false);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            return fill;
        }

        // Ba nút tròn 120 px góc phải dưới, trong dải y 820-980; vòng năng lượng của ultimate là quầng 130 px nằm sau nút.
        private static BattleActionPanel BuildActionPanel(RectTransform canvas, Sprite knob, Font font)
        {
            var root = NewRect("ActionPanel", canvas);
            Stretch(root);

            var turnLine = NewText("TurnLine", root, font, "", 30, TextAnchor.MiddleRight, Color.white);
            Place(turnLine.rectTransform, BottomRight, BottomRight, new Vector2(-472, 105), new Vector2(380, 120));

            var basic = ActionButton(root, "BasicButton", "Đánh", knob, font, new Color(0.30f, 0.55f, 0.95f), -328);
            var skill = ActionButton(root, "SkillButton", "Skill", knob, font, new Color(0.30f, 0.70f, 0.45f), -184);

            var track = NewRect("UltimateTrack", root);
            Place(track, BottomRight, BottomRight, new Vector2(-35, 100), new Vector2(130, 130));
            AddImage(track, knob, HpBack, raycast: false).type = Image.Type.Simple;
            var ringRect = NewRect("UltimateRing", root);
            Place(ringRect, BottomRight, BottomRight, new Vector2(-35, 100), new Vector2(130, 130));
            var ring = AddImage(ringRect, knob, Gold, raycast: false);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillAmount = 0f;
            var ultimate = ActionButton(root, "UltimateButton", "Ultimate", knob, font, new Color(0.62f, 0.40f, 0.85f), -40);

            var panel = root.gameObject.AddComponent<BattleActionPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_basicButton").objectReferenceValue = basic;
            so.FindProperty("_skillButton").objectReferenceValue = skill;
            so.FindProperty("_ultimateButton").objectReferenceValue = ultimate;
            so.FindProperty("_skillLabel").objectReferenceValue = skill.GetComponentInChildren<Text>();
            so.FindProperty("_ultimateRing").objectReferenceValue = ring;
            so.FindProperty("_turnLine").objectReferenceValue = turnLine;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        // Thanh trên trong dải y 0-150 (quái bắt đầu từ y 180): chữ đợt và thanh lượt giữa, ba nút 150×110 góc phải.
        private static RectTransform BuildTopBar(RectTransform canvas, Sprite sprite, Font font, out Text waveText, out Image[] turnCells,
            out Button menu, out Button auto, out Button speed)
        {
            var bar = NewRect("TopBar", canvas);
            Stretch(bar);

            waveText = NewText("WaveText", bar, font, "", 44, TextAnchor.MiddleCenter, Color.white);
            waveText.fontStyle = FontStyle.Bold;
            Place(waveText.rectTransform, TopCenter, TopCenter, new Vector2(0, -20), new Vector2(400, 60));

            var strip = NewRect("TurnBar", bar);
            Place(strip, TopCenter, TopCenter, new Vector2(0, -90), new Vector2(632, 56));
            turnCells = new Image[10];
            for (var i = 0; i < turnCells.Length; i++)
            {
                var cell = NewRect($"TurnCell{i}", strip);
                Place(cell, TopLeft, TopLeft, new Vector2(i * 64, 0), new Vector2(56, 56));
                turnCells[i] = AddImage(cell, sprite, Color.white, raycast: false);
                var letter = NewText("Letter", cell, font, "", 30, TextAnchor.MiddleCenter, Color.white);
                Stretch(letter.rectTransform);
                letter.fontStyle = FontStyle.Bold;
                cell.gameObject.SetActive(false);
            }

            menu = NewButton("MenuButton", bar, sprite, font, "Menu", ButtonColor, TopRight, new Vector2(-20, -20), new Vector2(150, 110));
            auto = NewButton("AutoButton", bar, sprite, font, "AUTO", CardFrame, TopRight, new Vector2(-190, -20), new Vector2(150, 110));
            speed = NewButton("SpeedButton", bar, sprite, font, "x1", ButtonColor, TopRight, new Vector2(-360, -20), new Vector2(150, 110));
            return bar;
        }

        // Nền tối chặn chạm; hộp giữa có 5 dòng HP, Tiếp tục, Bỏ cuộc; hộp xác nhận nằm trong cùng nền.
        private static BattlePausePopup BuildPausePopup(RectTransform canvas, Sprite sprite, Font font)
        {
            var holder = NewRect("BattlePausePopup", canvas);
            Stretch(holder);

            var root = NewRect("PauseRoot", holder);
            Stretch(root);
            AddImage(root, null, new Color(0f, 0f, 0f, 0.65f), raycast: true);

            var box = NewRect("PauseBox", root);
            Place(box, Center, Center, Vector2.zero, new Vector2(800, 760));
            AddImage(box, sprite, CardColor, raycast: false);
            var title = NewText("Title", box, font, "Tạm dừng", 52, TextAnchor.MiddleCenter, Gold);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, Center, Center, new Vector2(0, 320), new Vector2(700, 70));

            var lines = new Text[5];
            for (var i = 0; i < lines.Length; i++)
            {
                lines[i] = NewText($"HpLine{i}", box, font, "", 36, TextAnchor.MiddleCenter, Color.white);
                Place(lines[i].rectTransform, Center, Center, new Vector2(0, 230 - i * 56), new Vector2(700, 50));
            }

            var resume = NewButton("ResumeButton", box, sprite, font, "Tiếp tục", ButtonColor, Center, new Vector2(-200, -250), new Vector2(340, 110));
            var surrender = NewButton("SurrenderButton", box, sprite, font, "Bỏ cuộc", new Color(0.70f, 0.22f, 0.25f), Center, new Vector2(200, -250), new Vector2(340, 110));

            var confirm = NewRect("ConfirmRoot", root);
            Stretch(confirm);
            AddImage(confirm, null, new Color(0f, 0f, 0f, 0.5f), raycast: true);
            var confirmBox = NewRect("ConfirmBox", confirm);
            Place(confirmBox, Center, Center, Vector2.zero, new Vector2(760, 380));
            AddImage(confirmBox, sprite, CardFrame, raycast: false);
            var confirmText = NewText("ConfirmText", confirmBox, font, "", 40, TextAnchor.MiddleCenter, Color.white);
            Place(confirmText.rectTransform, Center, Center, new Vector2(0, 70), new Vector2(680, 170));
            var yes = NewButton("ConfirmYesButton", confirmBox, sprite, font, "Bỏ cuộc", new Color(0.70f, 0.22f, 0.25f), Center, new Vector2(-170, -100), new Vector2(300, 110));
            var no = NewButton("ConfirmNoButton", confirmBox, sprite, font, "Tiếp tục", ButtonColor, Center, new Vector2(170, -100), new Vector2(300, 110));
            confirm.gameObject.SetActive(false);
            root.gameObject.SetActive(false);

            var popup = holder.gameObject.AddComponent<BattlePausePopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            SetRefs(so.FindProperty("_hpLines"), lines);
            so.FindProperty("_resumeButton").objectReferenceValue = resume;
            so.FindProperty("_surrenderButton").objectReferenceValue = surrender;
            so.FindProperty("_confirmRoot").objectReferenceValue = confirm.gameObject;
            so.FindProperty("_confirmText").objectReferenceValue = confirmText;
            so.FindProperty("_confirmYesButton").objectReferenceValue = yes;
            so.FindProperty("_confirmNoButton").objectReferenceValue = no;
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        // Nền tối chặn chạm; chữ giữa màn: banner CLEAR/Thất bại, thưởng hoặc gợi ý, thể lực, cảnh báo lưu, lý do Thử lại; nút Xác nhận
        // (thắng) hoặc Thử lại và Về bản đồ (thua) chung một hàng, view bật tắt theo kết quả.
        private static BattleResultView BuildResultView(RectTransform canvas, Sprite sprite, Font font)
        {
            var holder = NewRect("BattleResultView", canvas);
            Stretch(holder);
            var root = NewRect("ResultRoot", holder);
            Stretch(root);
            // Đậm hơn nền popup khác: chữ kết quả nằm đè lên quái và người phe ta phía sau.
            AddImage(root, null, new Color(0f, 0f, 0f, 0.85f), raycast: true);

            // Hộp đặc: mọi chữ và nút nằm trong hộp, không còn đè lên quái và người phía sau.
            var box = NewRect("ResultBox", root);
            Place(box, Center, Center, Vector2.zero, new Vector2(1400, 760));
            AddImage(box, sprite, CardColor, raycast: false);

            var banner = NewText("Banner", box, font, "", 130, TextAnchor.MiddleCenter, Gold);
            banner.fontStyle = FontStyle.Bold;
            Place(banner.rectTransform, Center, Center, new Vector2(0, 270), new Vector2(1200, 170));
            var rewardRow = BuildRewardRow(box, "RewardRow", font, Center, new Vector2(-340, 130));
            var reward = NewText("RewardText", box, font, "", 44, TextAnchor.MiddleCenter, Color.white);
            Place(reward.rectTransform, Center, Center, new Vector2(0, 60), new Vector2(1280, 100));
            var stamina = NewText("StaminaText", box, font, "", 40, TextAnchor.MiddleCenter, Color.white);
            Place(stamina.rectTransform, Center, Center, new Vector2(0, -40), new Vector2(1280, 70));
            var warning = NewText("WarningText", box, font, "", 32, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.45f));
            Place(warning.rectTransform, Center, Center, new Vector2(0, -110), new Vector2(1280, 50));
            // Lý do Thử lại không được: nằm ngay trên nút Thử lại (dưới-phải).
            var message = NewText("MessageText", box, font, "", 32, TextAnchor.MiddleRight, new Color(1f, 0.65f, 0.45f));
            Place(message.rectTransform, Center, Center, new Vector2(300, -200), new Vector2(640, 50));

            var confirm = NewButton("ConfirmButton", box, sprite, font, "Xác nhận", ButtonColor, Center, new Vector2(0, -290), new Vector2(380, 110));
            var retry = NewButton("RetryButton", box, sprite, font, "Thử lại", ButtonColor, Center, new Vector2(300, -290), new Vector2(380, 110));
            var back = NewButton("BackButton", box, sprite, font, "Về bản đồ", CardFrame, Center, new Vector2(-300, -290), new Vector2(380, 110));
            root.gameObject.SetActive(false);

            var view = holder.gameObject.AddComponent<BattleResultView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_banner").objectReferenceValue = banner;
            so.FindProperty("_rewardRow").objectReferenceValue = rewardRow;
            so.FindProperty("_rewardText").objectReferenceValue = reward;
            so.FindProperty("_staminaText").objectReferenceValue = stamina;
            so.FindProperty("_warningText").objectReferenceValue = warning;
            so.FindProperty("_messageText").objectReferenceValue = message;
            so.FindProperty("_confirmButton").objectReferenceValue = confirm;
            so.FindProperty("_retryButton").objectReferenceValue = retry;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static Button ActionButton(RectTransform parent, string name, string label, Sprite knob, Font font, Color color, float x)
        {
            var button = NewButton(name, parent, knob, font, label, color, BottomRight, new Vector2(x, 105), new Vector2(120, 120));
            ((Image)button.targetGraphic).type = Image.Type.Simple;
            button.GetComponentInChildren<Text>().fontSize = 26;
            return button;
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
