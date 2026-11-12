using BunDauPeo.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BunDauPeo.EditorTools.GachaSceneBuilder;

namespace BunDauPeo.EditorTools
{
    // Dựng scene StageSelect bằng code theo spec Combat 5.2, cùng khuôn BattleSceneBuilder. Hình đều là Image (nút tròn Knob, đoạn nối là
    // hình vuông đặc xoay, ổ khoá và dấu tích ghép từ hình vuông); chữ là Text của uGUI (docs dự án thắng skill ui-ugui).
    internal static class StageSelectSceneBuilder
    {
        private static readonly Color Background = new(0.10f, 0.12f, 0.18f);
        private static readonly Color Gold = new(1f, 0.85f, 0.4f);
        private static readonly Color CardColor = new(0.16f, 0.18f, 0.26f);
        private static readonly Color CardFrame = new(0.30f, 0.33f, 0.45f);
        private static readonly Color ButtonColor = new(0.30f, 0.50f, 0.90f);
        private static readonly Color Warning = new(1f, 0.65f, 0.45f);
        private static readonly Color LockedColor = new(0.35f, 0.36f, 0.42f);
        private static readonly Color Dark = new(0.10f, 0.10f, 0.14f);
        private static readonly Color EnemyColor = new(0.55f, 0.12f, 0.15f);

        // Hộp xác nhận của Chọn đội (SmallConfirm, StaffConfirm) giữ cỡ cũ; test đọc lại hằng này.
        internal static readonly Vector2 ConfirmSize = new(900, 380);

        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new(0, 1);

        private static readonly Color MainTileColor = new(0.15f, 0.52f, 0.55f);

        // Chọn chế độ (spec UI hub C 5.2): kín màn, nền đặc chặn bấm, lúc đầu tắt. Thẻ Chương chính ở trái, bốn ô khoá 2×2 ở phải;
        // đáy các ô y 760 nên nằm trên vạch 120 px. View nối HubNav và Bản đồ ở WireModePanel (hai thứ đó dựng sau).
        public static RectTransform BuildModePanel(RectTransform parent, Sprite sprite, Font font)
        {
            var panel = NewRect("ModeSelectPanel_PH_1920x1080", parent);
            Stretch(panel);
            AddImage(panel, null, Background, raycast: true);

            // Khung 1920×1080 chứa mọi ô; FitWidthScale (trên panel kín màn, nhận được sự kiện đổi cỡ canvas) co khung khi màn hẹp hơn 16:9 và canh giữa khi rộng hơn.
            var content = NewRect("ModeContent", panel);
            Place(content, TopLeft, TopLeft, Vector2.zero, new Vector2(1920, 1080));
            panel.gameObject.AddComponent<FitWidthScale>().Content = content;

            var title = NewLabel("ModeTitle", content, font, "hub.tab_stages", 56, TextAnchor.MiddleLeft, Gold);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, TopLeft, TopLeft, new Vector2(60, -30), new Vector2(800, 90));

            var main = BuildTile("ModeMain_PH_820x560", content, sprite, MainTileColor, new Vector2(100, 200), new Vector2(820, 560));
            var mainName = NewLabel("ModeMainName", (RectTransform)main.transform, font, "stages.mode_main", 80, TextAnchor.MiddleCenter, Color.white);
            mainName.fontStyle = FontStyle.Bold;
            Place(mainName.rectTransform, Center, Center, new Vector2(0, 50), new Vector2(760, 140));
            var mainSub = NewText("ModeMainSub", (RectTransform)main.transform, font, "", 40, TextAnchor.MiddleCenter, Gold);
            Place(mainSub.rectTransform, Center, Center, new Vector2(0, -90), new Vector2(760, 80));

            var locked = new Button[4];
            var lockedNames = new[] { "ModeLockedHard_PH_440x260", "ModeLockedEvent_PH_440x260", "ModeLockedChallenge_PH_440x260", "ModeLockedArena_PH_440x260" };
            var lockedKeys = new[] { "stages.mode_hard", "stages.mode_event", "stages.mode_challenge", "stages.mode_arena" };
            for (var i = 0; i < locked.Length; i++)
            {
                var position = new Vector2(1000 + 460 * (i % 2), 200 + 300 * (i / 2));
                locked[i] = BuildTile(lockedNames[i], content, sprite, LockedColor, position, new Vector2(440, 260));
                var tile = (RectTransform)locked[i].transform;
                var name = NewLabel("TileName", tile, font, lockedKeys[i], 52, TextAnchor.MiddleCenter, Color.white);
                Place(name.rectTransform, Center, Center, new Vector2(0, 30), new Vector2(400, 90));
                var soon = NewLabel("TileSoon", tile, font, "hub.coming_soon", 34, TextAnchor.MiddleCenter, new Color(0.80f, 0.82f, 0.88f));
                Place(soon.rectTransform, Center, Center, new Vector2(0, -50), new Vector2(400, 60));
                NavBarBuilder.BuildLock(tile, "Lock_PH_40x48", new Vector2(-180, 90));
            }

            var view = panel.gameObject.AddComponent<ModeSelectView>();
            var so = new SerializedObject(view);
            so.FindProperty("_mainButton").objectReferenceValue = main;
            so.FindProperty("_mainSubText").objectReferenceValue = mainSub;
            so.FindProperty("_lockedButtons").arraySize = locked.Length;
            for (var i = 0; i < locked.Length; i++) so.FindProperty("_lockedButtons").GetArrayElementAtIndex(i).objectReferenceValue = locked[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.gameObject.SetActive(false);
            return panel;
        }

        public static void WireModePanel(RectTransform modePanel, HubNav hub, StageMapView map)
        {
            var so = new SerializedObject(modePanel.GetComponent<ModeSelectView>());
            so.FindProperty("_hub").objectReferenceValue = hub;
            so.FindProperty("_map").objectReferenceValue = map;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Ô của Chọn chế độ: góc trên-trái ở (x, y) tính từ góc trên-trái màn.
        private static Button BuildTile(string name, RectTransform parent, Sprite sprite, Color color, Vector2 topLeft, Vector2 size)
        {
            var rect = NewRect(name, parent);
            Place(rect, TopLeft, TopLeft, new Vector2(topLeft.x, -topLeft.y), size);
            var image = AddImage(rect, sprite, color, raycast: true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        // Panel Bản đồ của Lobby (spec UI hub 5.4; đợt C: trang con của Chọn chế độ, có nút Quay lại): kín màn, nền đặc chặn bấm, lúc đầu tắt. Trong hàm này `canvas` là chính panel.
        // Nút Quay lại ở góc trên-trái, tiêu đề chương bên phải nút; dòng gợi ý trên vạch 120 px.
        public static RectTransform BuildPanel(RectTransform parent, Sprite sprite, Sprite knob, Font font)
        {
            var canvas = NewRect("StageMapPanel_PH_1920x1080", parent);
            Stretch(canvas);
            AddImage(canvas, null, Background, raycast: true);

            // Quay lại (về Chọn chế độ) chiếm góc trên-trái; tiêu đề chương nằm bên phải nút.
            var back = BackButton("BackButton", canvas, sprite, font);
            var chapter = NewLabel("ChapterText", canvas, font, "combat.chapter_1", 56, TextAnchor.MiddleLeft, Gold);
            chapter.fontStyle = FontStyle.Bold;
            Place(chapter.rectTransform, TopLeft, TopLeft, new Vector2(260, -30), new Vector2(500, 70));

            // Gốc của nút, đoạn nối và dấu NEXT; mẫu tắt sẵn, view nhân bản lúc chạy.
            var map = NewRect("Map", canvas);
            Stretch(map);
            var linkRect = NewRect("LinkTemplate", map);
            Place(linkRect, Center, Center, Vector2.zero, new Vector2(100, 14));
            var link = AddImage(linkRect, null, LockedColor, raycast: false);
            linkRect.gameObject.SetActive(false);
            var node = BuildNode(map, sprite, knob, font);
            node.gameObject.SetActive(false);
            var next = BuildNextMarker(map, font);

            var hint = NewText("HintText", canvas, font, "", 44, TextAnchor.MiddleCenter, Warning);
            Place(hint.rectTransform, Center, Center, new Vector2(0, -370), new Vector2(1000, 70));
            hint.gameObject.SetActive(false);
            var invalid = NewText("InvalidText", canvas, font, "", 56, TextAnchor.MiddleCenter, Warning);
            Place(invalid.rectTransform, Center, Center, Vector2.zero, new Vector2(1000, 100));
            invalid.gameObject.SetActive(false);

            var info = BuildInfoPopup(canvas, sprite, font);
            var story = BuildStoryPopup(canvas, sprite, font);
            var team = BuildTeamPopup(canvas, sprite, knob, font);

            var viewObject = NewRect("StageMapView", canvas);
            var view = viewObject.gameObject.AddComponent<StageMapView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = canvas.gameObject;
            so.FindProperty("_backButton").objectReferenceValue = back;
            so.FindProperty("_nodesRoot").objectReferenceValue = map;
            so.FindProperty("_nodeTemplate").objectReferenceValue = node;
            so.FindProperty("_linkTemplate").objectReferenceValue = link;
            so.FindProperty("_nextMarker").objectReferenceValue = next;
            so.FindProperty("_hintText").objectReferenceValue = hint;
            so.FindProperty("_invalidText").objectReferenceValue = invalid;
            so.FindProperty("_info").objectReferenceValue = info;
            so.FindProperty("_story").objectReferenceValue = story;
            so.FindProperty("_team").objectReferenceValue = team;
            so.ApplyModifiedPropertiesWithoutUndo();

            canvas.gameObject.SetActive(false);
            return canvas;
        }

        // Nút tròn 120 px: nhãn giữa, ổ khoá ở đỉnh giữa khi khoá, dấu tích trong huy hiệu trắng góc trên phải khi đã clear.
        private static StageNodeView BuildNode(RectTransform parent, Sprite sprite, Sprite knob, Font font)
        {
            var root = NewRect("NodeTemplate", parent);
            Place(root, Center, Center, Vector2.zero, new Vector2(120, 120));
            var body = AddImage(root, knob, ButtonColor, raycast: true);
            body.type = Image.Type.Simple;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = body;

            var label = NewText("Label", root, font, "1-1", 28, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            Stretch(label.rectTransform);

            var lockIcon = NewRect("LockIcon", root);
            Place(lockIcon, Center, Center, new Vector2(0, 60), new Vector2(44, 52));
            var shackle = NewRect("Shackle", lockIcon);
            Place(shackle, Center, Center, new Vector2(0, 14), new Vector2(24, 26));
            AddImage(shackle, null, Dark, raycast: false);
            var shackleHole = NewRect("ShackleHole", shackle);
            Place(shackleHole, Center, Center, new Vector2(0, -3), new Vector2(12, 16));
            AddImage(shackleHole, null, LockedColor, raycast: false);
            var lockBody = NewRect("LockBody", lockIcon);
            Place(lockBody, Center, Center, new Vector2(0, -8), new Vector2(44, 32));
            AddImage(lockBody, null, Gold, raycast: false);

            var check = BuildCheckBadge(root, knob, new Vector2(42, 42));

            var view = root.gameObject.AddComponent<StageNodeView>();
            var so = new SerializedObject(view);
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_lockIcon").objectReferenceValue = lockIcon.gameObject;
            so.FindProperty("_checkIcon").objectReferenceValue = check.gameObject;
            so.FindProperty("_button").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // Huy hiệu trắng 50×50 với dấu tích xanh ghép từ hai hình vuông xoay; dùng cho nút bản đồ và thẻ chọn đội.
        private static RectTransform BuildCheckBadge(RectTransform parent, Sprite knob, Vector2 position)
        {
            var check = NewRect("CheckIcon", parent);
            Place(check, Center, Center, position, new Vector2(50, 50));
            var badge = AddImage(check, knob, Color.white, raycast: false);
            badge.type = Image.Type.Simple;
            var shortStroke = NewRect("ShortStroke", check);
            Place(shortStroke, Center, Center, new Vector2(-7, -3), new Vector2(7, 16));
            shortStroke.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddImage(shortStroke, null, new Color(0.20f, 0.60f, 0.30f), raycast: false);
            var longStroke = NewRect("LongStroke", check);
            Place(longStroke, Center, Center, new Vector2(4, 1), new Vector2(7, 30));
            longStroke.localRotation = Quaternion.Euler(0f, 0f, -45f);
            AddImage(longStroke, null, new Color(0.20f, 0.60f, 0.30f), raycast: false);
            return check;
        }

        // Không có sprite tam giác có sẵn: mũi tên là hình thoi (vuông xoay 45°) trên chữ NEXT, nhấp nháy qua CanvasGroup.
        private static RectTransform BuildNextMarker(RectTransform parent, Font font)
        {
            var root = NewRect("NextMarker", parent);
            Place(root, Center, Center, Vector2.zero, new Vector2(120, 90));
            root.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            var text = NewLabel("NextText", root, font, "combat.next", 34, TextAnchor.MiddleCenter, Gold);
            text.fontStyle = FontStyle.Bold;
            Place(text.rectTransform, Center, Center, new Vector2(0, 22), new Vector2(120, 40));
            var arrow = NewRect("Arrow", root);
            Place(arrow, Center, Center, new Vector2(0, -22), new Vector2(26, 26));
            arrow.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddImage(arrow, null, Gold, raycast: false);
            root.gameObject.SetActive(false);
            return root;
        }

        // Trang kín màn (S1, spec UI hub 5.5), hai cột trong vùng trên vạch: tên màn ở trên; trái: tối đa 4 hàng đợt (5 ô vuông, boss
        // lớn hơn); phải: hàng ô thưởng, thể lực, cảnh báo, nút Thách đấu dưới-phải (neo mép phải hộp). Hủy là nút Quay lại trên-trái.
        // Toạ độ tính từ tâm hộp nội dung (1920×960 trên thanh), y hướng lên; nội dung nằm trong ±800 để màn 4:3 nhỏ của test vẫn thấy hết.
        private static StageInfoPopup BuildInfoPopup(RectTransform canvas, Sprite sprite, Font font)
        {
            var holder = NewRect("StageInfoPopup", canvas);
            Stretch(holder);
            var root = NewRect("InfoRoot", holder);
            Stretch(root);
            AddImage(root, null, Background, raycast: true);
            var box = PageBox(root, "InfoBox");

            var title = NewText("Title", box, font, "", 56, TextAnchor.MiddleLeft, Gold);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, Center, Center, new Vector2(-300, 410), new Vector2(800, 80));
            var enemyHeader = NewLabel("EnemyHeader", box, font, "combat.enemies", 36, TextAnchor.MiddleLeft, Gold);
            Place(enemyHeader.rectTransform, Center, Center, new Vector2(-700, 300), new Vector2(160, 50));

            var labels = new Text[StageInfoPopup.WaveRows];
            var cells = new Image[StageInfoPopup.WaveRows * StageInfoPopup.CellsPerRow];
            for (var row = 0; row < labels.Length; row++)
            {
                var y = 220 - row * 110;
                labels[row] = NewText($"WaveLabel{row}", box, font, UiText.Format("combat.wave_row", row + 1), 32, TextAnchor.MiddleLeft, Color.white);
                Place(labels[row].rectTransform, Center, Center, new Vector2(-700, y), new Vector2(160, 50));
                for (var col = 0; col < StageInfoPopup.CellsPerRow; col++)
                {
                    var cell = NewRect($"Cell{row}_{col}", box);
                    Place(cell, Center, Center, new Vector2(-520 + col * 100, y), new Vector2(64, 64));
                    cells[row * StageInfoPopup.CellsPerRow + col] = AddImage(cell, null, EnemyColor, raycast: false);
                }
            }

            var rewardRow = BuildRewardRow(box, "RewardRow", font, Center, new Vector2(100, 320));

            var stamina = RightLine(box, font, "StaminaText", 40, Color.white, 100, 60);
            var warning = RightLine(box, font, "WarningText", 26, Warning, 10, 90);
            var message = RightLine(box, font, "MessageText", 32, Warning, -80, 50);

            var cancel = BackButton("CancelButton", box, sprite, font);
            var challenge = BattleSceneBuilder.LabelButton("ChallengeButton", box, sprite, font, "combat.challenge", ButtonColor, new Vector2(1, 0), new Vector2(-60, 50), new Vector2(300, 100));
            root.gameObject.SetActive(false);

            var popup = holder.gameObject.AddComponent<StageInfoPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_title").objectReferenceValue = title;
            SetRefs(so.FindProperty("_waveLabels"), labels);
            SetRefs(so.FindProperty("_cells"), cells);
            so.FindProperty("_rewardRow").objectReferenceValue = rewardRow;
            so.FindProperty("_staminaText").objectReferenceValue = stamina;
            so.FindProperty("_warningText").objectReferenceValue = warning;
            so.FindProperty("_messageText").objectReferenceValue = message;
            so.FindProperty("_cancelButton").objectReferenceValue = cancel;
            so.FindProperty("_challengeButton").objectReferenceValue = challenge;
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        private static StoryPlaceholderPopup BuildStoryPopup(RectTransform canvas, Sprite sprite, Font font)
        {
            var holder = NewRect("StoryPlaceholderPopup", canvas);
            Stretch(holder);
            var root = NewRect("StoryRoot", holder);
            Stretch(root);
            AddImage(root, null, Background, raycast: true);

            // Trang kín màn: tên màn, "Chưa có nội dung", Tiếp tục giữa; Đóng là nút Quay lại trên-trái.
            var box = PageBox(root, "StoryBox");
            var title = NewText("Title", box, font, "", 56, TextAnchor.MiddleCenter, Gold);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, Center, Center, new Vector2(0, 150), new Vector2(800, 70));
            var body = Line(box, font, "BodyText", 42, Color.white, 20, 800);
            var message = Line(box, font, "MessageText", 32, Warning, -70, 800);
            var proceed = BattleSceneBuilder.LabelButton("ContinueButton", box, sprite, font, "common.continue", ButtonColor, Center, new Vector2(0, -200), new Vector2(340, 100));
            var close = BackButton("CloseButton", box, sprite, font);
            root.gameObject.SetActive(false);

            var popup = holder.gameObject.AddComponent<StoryPlaceholderPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_message").objectReferenceValue = message;
            so.FindProperty("_continueButton").objectReferenceValue = proceed;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        // Trang kín màn (T1, spec UI hub 5.5): lưới 16 thẻ chân dung 150×190 (8×2, phân trang); hàng dưới: 5 chip 150×120 chia theo
        // đội hình tự xếp bên trái, Vào trận bên phải; Hủy là nút Quay lại trên-trái. Toạ độ từ tâm hộp nội dung (1920×960 trên thanh).
        // Hai hộp xác nhận nằm trong cùng nền chặn chạm và tắt sẵn (thể lực chưa bị trừ khi hộp mở); chúng giữ cỡ cũ, vẽ trên thanh.
        private static TeamSelectPopup BuildTeamPopup(RectTransform canvas, Sprite sprite, Sprite knob, Font font)
        {
            var holder = NewRect("TeamSelectPopup", canvas);
            Stretch(holder);
            var root = NewRect("TeamRoot", holder);
            Stretch(root);
            AddImage(root, null, Background, raycast: true);
            var box = PageBox(root, "TeamBox");

            var title = NewText("Title", box, font, "", 48, TextAnchor.MiddleCenter, Gold);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, Center, Center, new Vector2(0, 395), new Vector2(1200, 60));

            var cards = new TeamCardView[TeamSelectPopup.CardsPerPage];
            for (var i = 0; i < cards.Length; i++)
                cards[i] = BuildTeamCard(box, i, sprite, knob, font);

            var prev = NewButton("PrevButton", box, sprite, font, "<", CardFrame, Center, new Vector2(-130, -75), new Vector2(100, 60));
            var next = NewButton("NextButton", box, sprite, font, ">", CardFrame, Center, new Vector2(130, -75), new Vector2(100, 60));
            var page = NewText("PageText", box, font, "1/1", 32, TextAnchor.MiddleCenter, Color.white);
            Place(page.rectTransform, Center, Center, new Vector2(0, -75), new Vector2(120, 50));

            var header = NewText("TeamHeader", box, font, "", 34, TextAnchor.MiddleCenter, Gold);
            Place(header.rectTransform, Center, Center, new Vector2(-405, -130), new Vector2(790, 46));
            // Ô 0-1 hàng trước, 2-3 hàng giữa, 4 hàng sau (BattleFormation): nhãn nằm giữa nhóm của nó.
            var positions = new[] { -645f, -325f, -85f };
            var widths = new[] { 310f, 310f, 150f };
            var labels = new Text[3];
            for (var i = 0; i < labels.Length; i++)
            {
                labels[i] = NewText($"PositionLabel{i}", box, font, "", 26, TextAnchor.MiddleCenter, Color.white);
                Place(labels[i].rectTransform, Center, Center, new Vector2(positions[i], -178), new Vector2(widths[i], 34));
            }

            var chips = new Image[5];
            var chipTexts = new Text[5];
            for (var i = 0; i < chips.Length; i++)
            {
                var chip = NewRect($"Chip{i}", box);
                Place(chip, Center, Center, new Vector2(-725 + i * 160, -255), new Vector2(150, 120));
                chips[i] = AddImage(chip, sprite, CardFrame, raycast: false);
                chipTexts[i] = NewText("Text", chip, font, "-", 24, TextAnchor.MiddleCenter, Color.white);
                Stretch(chipTexts[i].rectTransform, 4);
                FitText(chipTexts[i], 24);
            }

            var message = NewText("MessageText", box, font, "", 32, TextAnchor.MiddleCenter, Warning);
            Place(message.rectTransform, Center, Center, new Vector2(0, -385), new Vector2(1200, 40));
            var cancel = BackButton("CancelButton", box, sprite, font);
            var start = BattleSceneBuilder.LabelButton("StartButton", box, sprite, font, "combat.start", ButtonColor, Center, new Vector2(660, -255), new Vector2(300, 100));

            var small = BuildConfirm(root, sprite, font, "SmallConfirm", "combat.start", "common.no", out var smallText, out var smallYes, out var smallNo);
            var staff = BuildConfirm(root, sprite, font, "StaffConfirm", "common.move", "common.no", out var staffText, out var staffYes, out var staffNo);
            root.gameObject.SetActive(false);

            var popup = holder.gameObject.AddComponent<TeamSelectPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_title").objectReferenceValue = title;
            SetRefs(so.FindProperty("_cards"), cards);
            so.FindProperty("_prevButton").objectReferenceValue = prev;
            so.FindProperty("_nextButton").objectReferenceValue = next;
            so.FindProperty("_pageText").objectReferenceValue = page;
            so.FindProperty("_teamHeader").objectReferenceValue = header;
            SetRefs(so.FindProperty("_positionLabels"), labels);
            SetRefs(so.FindProperty("_chips"), chips);
            SetRefs(so.FindProperty("_chipTexts"), chipTexts);
            so.FindProperty("_messageText").objectReferenceValue = message;
            so.FindProperty("_cancelButton").objectReferenceValue = cancel;
            so.FindProperty("_startButton").objectReferenceValue = start;
            so.FindProperty("_smallConfirmRoot").objectReferenceValue = small.gameObject;
            so.FindProperty("_smallConfirmText").objectReferenceValue = smallText;
            so.FindProperty("_smallYesButton").objectReferenceValue = smallYes;
            so.FindProperty("_smallNoButton").objectReferenceValue = smallNo;
            so.FindProperty("_staffConfirmRoot").objectReferenceValue = staff.gameObject;
            so.FindProperty("_staffConfirmText").objectReferenceValue = staffText;
            so.FindProperty("_staffYesButton").objectReferenceValue = staffYes;
            so.FindProperty("_staffNoButton").objectReferenceValue = staffNo;
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        // Thẻ chân dung 150×190 ở lưới 8 cột (x -665..665, cách 190) × 2 hàng (y 260, 55): ô màu class 140×140 phía trên chứa
        // nhãn quán (trên) và dòng độ hiếm kèm sao (dưới); tên dưới ô; dấu tích góc trên phải khi đang trong đội.
        private static TeamCardView BuildTeamCard(RectTransform box, int index, Sprite sprite, Sprite knob, Font font)
        {
            var root = NewRect($"TeamCard{index}", box);
            Place(root, Center, Center, new Vector2(-665 + index % 8 * 190, 260 - index / 8 * 205), new Vector2(150, 190));
            var frame = AddImage(root, sprite, CardFrame, raycast: true);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;

            var body = NewRect("Body", root);
            Stretch(body, 5);
            AddImage(body, sprite, CardColor, raycast: false);
            var portraitRect = NewRect("Portrait", root);
            Place(portraitRect, Center, Center, new Vector2(0, 20), new Vector2(140, 140));
            var portrait = AddImage(portraitRect, null, Color.white, raycast: false);

            var tag = NewText("Tag", root, font, "", 22, TextAnchor.MiddleCenter, Gold);
            Place(tag.rectTransform, Center, Center, new Vector2(0, 62), new Vector2(130, 44));
            FitText(tag, 22);
            var info = NewText("Info", root, font, "", 22, TextAnchor.MiddleCenter, Color.white);
            Place(info.rectTransform, Center, Center, new Vector2(0, -30), new Vector2(130, 30));
            FitText(info, 22);
            var name = NewText("Name", root, font, "", 26, TextAnchor.MiddleCenter, Color.white);
            name.fontStyle = FontStyle.Bold;
            Place(name.rectTransform, Center, Center, new Vector2(0, -72), new Vector2(140, 36));
            FitText(name, 26);
            var check = BuildCheckBadge(root, knob, new Vector2(50, 70));

            var view = root.gameObject.AddComponent<TeamCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_frame").objectReferenceValue = frame;
            so.FindProperty("_portrait").objectReferenceValue = portrait;
            so.FindProperty("_name").objectReferenceValue = name;
            so.FindProperty("_info").objectReferenceValue = info;
            so.FindProperty("_tag").objectReferenceValue = tag;
            so.FindProperty("_check").objectReferenceValue = check.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
            return view;
        }

        // Tên dài không được tràn ô: thu chữ tới 16 rồi mới xuống dòng/cắt.
        private static void FitText(Text text, int maxSize)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = maxSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        // Hộp xác nhận Có/Không nằm trong nền popup, có nền tối riêng chặn chạm; tắt sẵn.
        private static RectTransform BuildConfirm(RectTransform parent, Sprite sprite, Font font, string name, string yesKey, string noKey,
            out Text text, out Button yes, out Button no)
        {
            var confirm = NewRect(name + "Root", parent);
            Stretch(confirm);
            AddImage(confirm, null, new Color(0f, 0f, 0f, 0.5f), raycast: true);
            NavBarBuilder.OverBar(confirm);
            var box = NewRect(name + "Box", confirm);
            Place(box, Center, Center, Vector2.zero, ConfirmSize);
            AddImage(box, sprite, CardFrame, raycast: false);
            text = NewText(name + "Text", box, font, "", 42, TextAnchor.MiddleCenter, Color.white);
            Place(text.rectTransform, Center, Center, new Vector2(0, 70), new Vector2(820, 170));
            yes = BattleSceneBuilder.LabelButton(name + "YesButton", box, sprite, font, yesKey, ButtonColor, Center, new Vector2(-190, -100), new Vector2(340, 110));
            no = BattleSceneBuilder.LabelButton(name + "NoButton", box, sprite, font, noKey, new Color(0.45f, 0.47f, 0.55f), Center, new Vector2(190, -100), new Vector2(340, 110));
            confirm.gameObject.SetActive(false);
            return confirm;
        }

        private static Text Line(RectTransform parent, Font font, string name, int size, Color color, float y, float width)
        {
            var text = NewText(name, parent, font, "", size, TextAnchor.MiddleCenter, color);
            Place(text.rectTransform, Center, Center, new Vector2(0, y), new Vector2(width, size + 16));
            return text;
        }

        // Dòng trong cột phải của trang màn: rộng 640, từ x 140 tới 780 tính từ tâm hộp, canh trái.
        private static Text RightLine(RectTransform parent, Font font, string name, int size, Color color, float y, float height)
        {
            var text = NewText(name, parent, font, "", size, TextAnchor.MiddleLeft, color);
            Place(text.rectTransform, Center, Center, new Vector2(460, y), new Vector2(640, height));
            return text;
        }

        private static void SetRefs(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
