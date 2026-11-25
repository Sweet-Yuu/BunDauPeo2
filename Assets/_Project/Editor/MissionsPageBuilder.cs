using BunDauPeo.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BunDauPeo.EditorTools.GachaSceneBuilder;
using Object = UnityEngine.Object;

namespace BunDauPeo.EditorTools
{
    // Trang Nhiệm vụ (spec đợt 3 8.1) và hai nút tròn mới của Lobby (8.6). Hình chỉ dùng hình có sẵn của Unity; tên object `_PH_<rộng>x<cao>` theo spec mục 8.
    internal static class MissionsPageBuilder
    {
        private static readonly Vector2 TopLeft = new(0, 1);
        private static readonly Vector2 TopRight = new(1, 1);
        private static readonly Vector2 TopCenter = new(0.5f, 1);
        private static readonly Vector2 BottomCenter = new(0.5f, 0);
        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Color PanelColor = new(0.18f, 0.20f, 0.27f);
        private static readonly Color RowColor = new(0.27f, 0.30f, 0.40f);
        private static readonly Color BarBack = new(0.10f, 0.11f, 0.16f);
        private static readonly Color Gold = new(1f, 0.85f, 0.4f);
        private static readonly Color Soft = new(0.80f, 0.82f, 0.90f);
        private static readonly Color Grey = new(0.45f, 0.47f, 0.55f);
        private static readonly Color Blue = new(0.30f, 0.50f, 0.90f);
        private static readonly Color Green = new(0.20f, 0.60f, 0.35f);
        private static readonly Color Red = new(0.90f, 0.25f, 0.25f);
        private static readonly Color Tag = new(0.85f, 0.45f, 0.60f);
        private static readonly Color Dim = new(0f, 0f, 0f, 0.65f);
        private static readonly Vector2 ConfirmSize = new(900, 420);

        // Nút tròn xếp dọc trên Shop (hàng ngang đè điểm giữa màn 640×480 của test vuốt), cùng cỡ và kiểu với Shop: biểu tượng nửa trên, nhãn nửa dưới.
        internal static (Button Missions, Button Mail) BuildLobbyButtons(RectTransform hud, Font font, Sprite knob)
        {
            var missions = RoundButton("HubRoundBtnMissions_PH_110x110", hud, knob, font, "missions.title", Blue, 380);
            var clip = NewRect("MissionsIcon_PH_44x52", (RectTransform)missions.transform);
            Place(clip, Center, Center, new Vector2(0, 20), new Vector2(44, 52));
            AddImage(clip, null, Color.white, raycast: false);
            for (var i = 0; i < 3; i++)
            {
                var line = NewRect($"Line{i}", clip);
                Place(line, Center, Center, new Vector2(0, 14 - i * 14), new Vector2(30, 6));
                AddImage(line, null, Blue, raycast: false);
            }

            var mail = RoundButton("HubRoundBtnMail_PH_110x110", hud, knob, font, "mail.title", Blue, 260);
            // Phong thư: thân vuông, nắp là hình vuông xoay 45° (tam giác ghép).
            var body = NewRect("MailBody_PH_56x40", (RectTransform)mail.transform);
            Place(body, Center, Center, new Vector2(0, 20), new Vector2(56, 40));
            AddImage(body, null, Color.white, raycast: false);
            var flap = NewRect("MailFlap_PH_26x26", body);
            Place(flap, Center, Center, new Vector2(0, 8), new Vector2(26, 26));
            flap.localRotation = Quaternion.Euler(0, 0, 45);
            AddImage(flap, null, new Color(0.78f, 0.84f, 0.97f), raycast: false);
            // Chấm đỏ thư chưa đọc: nối ở Task 11.
            var dot = NewRect("MailDot_PH_28x28", (RectTransform)mail.transform);
            Place(dot, TopRight, Center, new Vector2(-8, -8), new Vector2(28, 28));
            AddImage(dot, knob, Red, raycast: false).type = Image.Type.Simple;
            dot.gameObject.SetActive(false);
            return (missions, mail);
        }

        private static Button RoundButton(string name, RectTransform hud, Sprite knob, Font font, string key, Color color, float y)
        {
            var button = RestaurantSceneBuilder.LabelButton(name, hud, knob, font, key, color, new Vector2(1, 0), new Vector2(-20, y), new Vector2(110, 110));
            ((Image)button.targetGraphic).type = Image.Type.Simple;
            var text = button.GetComponentInChildren<Text>();
            text.fontSize = 24;
            text.rectTransform.offsetMax = new Vector2(0, -60);
            return button;
        }

        internal static MissionsPage BuildPage(RectTransform canvas, Button open, Font font, Sprite sprite, Sprite knob)
        {
            var (root, outer) = RestaurantSceneBuilder.PageFrame(canvas, "MissionsPageRoot", "Box", PanelColor);
            // Khung 1920×960 neo trên-trái; FitWidthScale (trên Box) co đều khi màn hẹp hơn 16:9 (như panel Màn chơi).
            var box = NewRect("Frame", outer);
            box.anchorMin = box.anchorMax = box.pivot = TopLeft;
            box.anchoredPosition = Vector2.zero;
            box.sizeDelta = new Vector2(1920, 960);
            outer.gameObject.AddComponent<FitWidthScale>().Content = box;
            var title = NewLabel("TitleText", box, font, "missions.title", 52, TextAnchor.MiddleLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, TopLeft, TopLeft, new Vector2(250, -25), new Vector2(340, 70));
            var close = RestaurantSceneBuilder.BackButton(box, font, sprite);

            // Hai tab nằm hàng riêng dưới tiêu đề: thanh tài nguyên (Gem + Thể lực) chiếm góc phải trên, ở 4:3 lấn sang trái.
            var tabs = new Button[2];
            var badges = new GameObject[2];
            var badgeTexts = new Text[2];
            for (var i = 0; i < 2; i++)
            {
                tabs[i] = RestaurantSceneBuilder.LabelButton("MissionTab_PH_300x80", box, sprite, font, i == 0 ? "missions.tab_daily" : "missions.tab_newbie",
                    Blue, TopLeft, new Vector2(210 + i * 305, -120), new Vector2(300, 80));
                var badge = NewRect("TabBadge", (RectTransform)tabs[i].transform);
                Place(badge, TopRight, Center, new Vector2(-4, -4), new Vector2(44, 44));
                AddImage(badge, knob, Red, raycast: false).type = Image.Type.Simple;
                var count = NewText("Count", badge, font, "", 28, TextAnchor.MiddleCenter, Color.white);
                Stretch(count.rectTransform);
                badges[i] = badge.gameObject;
                badgeTexts[i] = count;
                badge.gameObject.SetActive(false);
            }

            // Danh sách cuộn 1500×700, hàng 1500×170.
            var scrollRoot = NewRect("ScrollView", box);
            Place(scrollRoot, TopCenter, TopCenter, new Vector2(0, -215), new Vector2(1500, 615));
            var viewport = NewRect("Viewport", scrollRoot);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            ConfigureScroll(scroll);
            var rowView = BuildRow(box, font, sprite);

            // Chân trang: còn lại, hoàn thành (trái) và Nhận hết (phải), đều trên vạch thanh dưới.
            var footer = NewRect("MissionFooter_PH_1500x80", box);
            Place(footer, BottomCenter, BottomCenter, new Vector2(0, 20), new Vector2(1500, 80));
            var remaining = NewText("RemainingText", footer, font, "", 36, TextAnchor.MiddleLeft, Gold);
            Place(remaining.rectTransform, TopLeft, TopLeft, new Vector2(20, 0), new Vector2(520, 80));
            var completed = NewText("CompletedText", footer, font, "", 36, TextAnchor.MiddleLeft, Color.white);
            Place(completed.rectTransform, TopLeft, TopLeft, new Vector2(600, 0), new Vector2(520, 80));
            var claimAll = RestaurantSceneBuilder.LabelButton("MissionClaimAll_PH_380x100", box, sprite, font, "missions.claim_all", Green,
                new Vector2(1, 0), new Vector2(-210, 10), new Vector2(380, 100));

            var confirm = BuildConfirm(root, font, sprite);
            root.gameObject.SetActive(false);

            var pageObject = NewRect("MissionsPage", canvas);
            var page = pageObject.gameObject.AddComponent<MissionsPage>();
            var so = new SerializedObject(page);
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_openButton").objectReferenceValue = open;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            SetRefs(so.FindProperty("_tabButtons"), tabs);
            SetRefs(so.FindProperty("_tabBadges"), badges);
            SetRefs(so.FindProperty("_tabBadgeTexts"), badgeTexts);
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_rowTemplate").objectReferenceValue = rowView;
            so.FindProperty("_remainingText").objectReferenceValue = remaining;
            so.FindProperty("_completedText").objectReferenceValue = completed;
            so.FindProperty("_claimAllButton").objectReferenceValue = claimAll;
            so.FindProperty("_confirm").objectReferenceValue = confirm;
            so.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        // Đích của "Đi tới": nối sau khi mọi trang đã dựng.
        internal static void WireTargets(MissionsPage page, HubNav hub, StaffPanel staff, UpgradeTestPanel upgrade, AfkPopup afk, ProfilePopup profile)
        {
            var so = new SerializedObject(page);
            so.FindProperty("_hub").objectReferenceValue = hub;
            so.FindProperty("_staffPanel").objectReferenceValue = staff;
            so.FindProperty("_upgradePanel").objectReferenceValue = upgrade;
            so.FindProperty("_afkPopup").objectReferenceValue = afk;
            so.FindProperty("_profilePopup").objectReferenceValue = profile;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static MissionRowView BuildRow(RectTransform box, Font font, Sprite sprite)
        {
            var row = NewRect("MissionRow_PH_1500x170", box);
            Place(row, Center, Center, Vector2.zero, new Vector2(1500, 170));
            AddImage(row, sprite, RowColor, raycast: false);

            var tag = NewRect("MissionTag_PH_150x44", row);
            Place(tag, TopLeft, TopLeft, new Vector2(20, -14), new Vector2(150, 44));
            AddImage(tag, sprite, Tag, raycast: false);
            var tagText = NewText("Text", tag, font, "", 26, TextAnchor.MiddleCenter, Color.white);
            Stretch(tagText.rectTransform);
            var rowTitle = NewText("TitleText", row, font, "", 34, TextAnchor.MiddleLeft, Color.white);
            Place(rowTitle.rectTransform, TopLeft, TopLeft, new Vector2(190, -10), new Vector2(1000, 54));

            var progressLabel = NewLabel("ProgressLabel", row, font, "missions.progress", 28, TextAnchor.MiddleLeft, Soft);
            Place(progressLabel.rectTransform, TopLeft, TopLeft, new Vector2(20, -88), new Vector2(130, 40));
            var bar = NewRect("MissionBar_PH_520x26", row);
            Place(bar, TopLeft, TopLeft, new Vector2(160, -95), new Vector2(520, 26));
            AddImage(bar, sprite, BarBack, raycast: false);
            var fill = NewRect("Fill", bar);
            Stretch(fill);
            AddImage(fill, sprite, Gold, raycast: false);
            var progress = NewText("ProgressText", row, font, "", 30, TextAnchor.MiddleLeft, Color.white);
            Place(progress.rectTransform, TopLeft, TopLeft, new Vector2(700, -88), new Vector2(120, 40));

            var rewardLabel = NewLabel("RewardLabel", row, font, "missions.reward", 28, TextAnchor.MiddleLeft, Soft);
            Place(rewardLabel.rectTransform, TopLeft, TopLeft, new Vector2(840, -88), new Vector2(120, 40));
            var rewardBox = NewRect("MissionReward_PH_150x80", row);
            Place(rewardBox, TopLeft, TopLeft, new Vector2(970, -68), new Vector2(150, 80));
            var reward = NewText("RewardText", rewardBox, font, "", 26, TextAnchor.MiddleLeft, Gold);
            Stretch(reward.rectTransform);
            reward.horizontalOverflow = HorizontalWrapMode.Overflow;

            var button = NewButton("MissionBtn_PH_220x90", row, sprite, font, "", Soft, new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(220, 90));
            var buttonText = button.GetComponentInChildren<Text>();
            buttonText.fontSize = 32;
            buttonText.resizeTextForBestFit = true;
            buttonText.resizeTextMinSize = 20;
            buttonText.resizeTextMaxSize = 32;
            NavBarBuilder.BuildLock(row, "MissionLock_PH_40x48", new Vector2(480, 0));
            var lockObject = row.Find("MissionLock_PH_40x48").gameObject;

            var view = row.gameObject.AddComponent<MissionRowView>();
            var so = new SerializedObject(view);
            so.FindProperty("_tagText").objectReferenceValue = tagText;
            so.FindProperty("_titleText").objectReferenceValue = rowTitle;
            so.FindProperty("_barFill").objectReferenceValue = fill;
            so.FindProperty("_progressText").objectReferenceValue = progress;
            so.FindProperty("_rewardText").objectReferenceValue = reward;
            so.FindProperty("_button").objectReferenceValue = button;
            so.FindProperty("_buttonText").objectReferenceValue = buttonText;
            so.FindProperty("_lock").objectReferenceValue = lockObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 170;
            element.preferredHeight = 170;
            row.gameObject.SetActive(false);
            return view;
        }

        // Hộp xác nhận thể lực (spec 8.5): nền mờ kín màn vẽ trên thanh, hộp giữ cỡ của hộp xác nhận Nhân viên.
        private static StaminaCapConfirm BuildConfirm(RectTransform pageRoot, Font font, Sprite sprite)
        {
            var host = NewRect("StaminaCapConfirm", pageRoot);
            var popup = NewRect("StaminaCapConfirmRoot", host);
            Stretch(popup);
            AddImage(popup, null, Dim, raycast: true);
            NavBarBuilder.OverBar(popup);
            var popupBox = NewRect("Box", popup);
            Place(popupBox, Center, Center, Vector2.zero, ConfirmSize);
            AddImage(popupBox, sprite, PanelColor, raycast: false);
            var head = NewLabel("TitleText", popupBox, font, "stamina.cap_title", 44, TextAnchor.MiddleCenter, Gold);
            Place(head.rectTransform, TopCenter, TopCenter, new Vector2(0, -30), new Vector2(820, 60));
            var body = NewText("BodyText", popupBox, font, "", 34, TextAnchor.MiddleCenter, Color.white);
            Place(body.rectTransform, TopCenter, TopCenter, new Vector2(0, -100), new Vector2(820, 180));
            var ok = RestaurantSceneBuilder.LabelButton("ConfirmOkButton", popupBox, sprite, font, "missions.claim", Green, BottomCenter, new Vector2(-200, 40), new Vector2(340, 90));
            var cancel = RestaurantSceneBuilder.LabelButton("ConfirmCancelButton", popupBox, sprite, font, "common.cancel", Grey, BottomCenter, new Vector2(200, 40), new Vector2(340, 90));
            popup.gameObject.SetActive(false);

            var confirm = host.gameObject.AddComponent<StaminaCapConfirm>();
            var so = new SerializedObject(confirm);
            so.FindProperty("_root").objectReferenceValue = popup.gameObject;
            so.FindProperty("_body").objectReferenceValue = body;
            so.FindProperty("_okButton").objectReferenceValue = ok;
            so.FindProperty("_cancelButton").objectReferenceValue = cancel;
            so.ApplyModifiedPropertiesWithoutUndo();
            return confirm;
        }

        private static void SetRefs(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
