using BunDauPeo.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static BunDauPeo.EditorTools.GachaSceneBuilder;

namespace BunDauPeo.EditorTools
{
    // Trang Hộp thư (spec đợt 3 8.2, ảnh hub-mailbox-en.png). Hình chỉ dùng hình có sẵn của Unity; tên object `_PH_<rộng>x<cao>` theo spec mục 8.
    internal static class MailPageBuilder
    {
        private static readonly Vector2 TopLeft = new(0, 1);
        private static readonly Vector2 TopRight = new(1, 1);
        private static readonly Vector2 BottomRight = new(1, 0);
        private static readonly Vector2 TopCenter = new(0.5f, 1);
        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Color PanelColor = new(0.18f, 0.20f, 0.27f);
        private static readonly Color BoxColor = new(0.13f, 0.15f, 0.21f);
        private static readonly Color RowColor = new(0.27f, 0.30f, 0.40f);
        private static readonly Color Gold = new(1f, 0.85f, 0.4f);
        private static readonly Color Soft = new(0.80f, 0.82f, 0.90f);
        private static readonly Color Grey = new(0.45f, 0.47f, 0.55f);
        private static readonly Color Green = new(0.20f, 0.60f, 0.35f);
        private static readonly Color Red = new(0.90f, 0.25f, 0.25f);

        internal static MailPage BuildPage(RectTransform canvas, Button open, GameObject dot, Font font, Sprite sprite, Sprite knob)
        {
            var (root, outer) = RestaurantSceneBuilder.PageFrame(canvas, "MailPageRoot", "Box", PanelColor);
            var box = NewRect("Frame", outer);
            box.anchorMin = box.anchorMax = box.pivot = TopLeft;
            box.anchoredPosition = Vector2.zero;
            box.sizeDelta = new Vector2(1920, 960);
            outer.gameObject.AddComponent<FitWidthScale>().Content = box;
            var title = NewLabel("TitleText", box, font, "mail.title", 52, TextAnchor.MiddleLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, TopLeft, TopLeft, new Vector2(250, -25), new Vector2(500, 70));
            var close = RestaurantSceneBuilder.BackButton(box, font, sprite);

            // Danh sách trái: khung cuộn + đếm + hai nút (xếp trong khung 620×720).
            var list = NewRect("MailList_PH_620x720", box);
            Place(list, TopLeft, TopLeft, new Vector2(60, -130), new Vector2(620, 720));
            AddImage(list, sprite, BoxColor, raycast: false);
            var scrollRoot = NewRect("ScrollView", list);
            Place(scrollRoot, TopLeft, TopLeft, new Vector2(10, -10), new Vector2(600, 560));
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
            layout.spacing = 8;
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
            var itemTemplate = BuildItem(box, font, sprite, knob);

            var count = NewText("CountText", list, font, "", 30, TextAnchor.MiddleLeft, Soft);
            Place(count.rectTransform, TopLeft, TopLeft, new Vector2(20, -580), new Vector2(580, 50));
            var claimAll = RestaurantSceneBuilder.LabelButton("MailClaimAll_PH_280x80", list, sprite, font, "mail.claim_all", Green, TopLeft, new Vector2(20, -635), new Vector2(280, 80));
            var deleteRead = RestaurantSceneBuilder.LabelButton("MailDeleteRead_PH_280x80", list, sprite, font, "mail.delete_read", Grey, TopLeft, new Vector2(320, -635), new Vector2(280, 80));
            foreach (var button in new[] { claimAll, deleteRead })
            {
                var text = button.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 20;
                text.resizeTextMaxSize = 34;
            }

            // Nội dung phải.
            var detail = NewRect("MailDetail_PH_1100x720", box);
            Place(detail, TopLeft, TopLeft, new Vector2(720, -130), new Vector2(1100, 720));
            AddImage(detail, sprite, BoxColor, raycast: false);
            var detailTitle = NewText("DetailTitle", detail, font, "", 44, TextAnchor.MiddleLeft, Gold);
            detailTitle.fontStyle = FontStyle.Bold;
            Place(detailTitle.rectTransform, TopLeft, TopLeft, new Vector2(30, -20), new Vector2(1040, 70));
            var sender = NewText("DetailSender", detail, font, "", 30, TextAnchor.MiddleLeft, Soft);
            Place(sender.rectTransform, TopLeft, TopLeft, new Vector2(30, -95), new Vector2(600, 40));
            var date = NewText("DetailDate", detail, font, "", 30, TextAnchor.MiddleRight, Soft);
            Place(date.rectTransform, TopRight, TopRight, new Vector2(-30, -95), new Vector2(300, 40));
            var body = NewText("DetailBody", detail, font, "", 34, TextAnchor.UpperLeft, Color.white);
            Place(body.rectTransform, TopLeft, TopLeft, new Vector2(30, -160), new Vector2(1040, 300));
            var attach = NewRect("MailAttach_PH_1000x120", detail);
            Place(attach, TopLeft, TopLeft, new Vector2(30, -490), new Vector2(1000, 120));
            AddImage(attach, sprite, RowColor, raycast: false);
            var attachText = NewLabel("AttachText", attach, font, "mail.attachments", 32, TextAnchor.MiddleLeft, Soft);
            Place(attachText.rectTransform, TopLeft, TopLeft, new Vector2(20, 0), new Vector2(200, 120));
            // Chip đính kèm của thư quà (spec đợt 4 mục 6): mẫu tắt + hàng chip xếp ngang.
            var chips = NewRect("AttachChips", attach);
            Place(chips, TopLeft, TopLeft, new Vector2(240, -25), new Vector2(740, 70));
            var chipRow = chips.gameObject.AddComponent<HorizontalLayoutGroup>();
            chipRow.spacing = 15;
            chipRow.childControlWidth = false;
            chipRow.childControlHeight = false;
            chipRow.childForceExpandWidth = false;
            chipRow.childForceExpandHeight = false;
            var chipTemplate = NewRect("MailAttachChip_PH_220x70", attach);
            Place(chipTemplate, TopLeft, TopLeft, Vector2.zero, new Vector2(220, 70));
            AddImage(chipTemplate, sprite, new Color(0.20f, 0.24f, 0.34f), raycast: false);
            var chipText = NewText("Label", chipTemplate, font, "", 30, TextAnchor.MiddleCenter, Gold);
            Stretch(chipText.rectTransform);
            chipText.resizeTextForBestFit = true;
            chipText.resizeTextMinSize = 20;
            chipText.resizeTextMaxSize = 30;
            chipTemplate.gameObject.SetActive(false);
            attach.gameObject.SetActive(false);
            var expiry = NewText("GiftExpiry", detail, font, "", 32, TextAnchor.MiddleLeft, Soft);
            Place(expiry.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 45), new Vector2(600, 60));
            var action = RestaurantSceneBuilder.LabelButton("MailBtn_PH_300x90", detail, sprite, font, "mail.link_btn", Green, BottomRight, new Vector2(-30, 30), new Vector2(300, 90));
            var empty = NewLabel("EmptyText", box, font, "mail.empty", 44, TextAnchor.MiddleCenter, Soft);
            Place(empty.rectTransform, TopLeft, TopLeft, new Vector2(720, -430), new Vector2(1100, 100));

            // Toast nhận quà: tự tắt vài giây (MailPage).
            var toast = NewRect("MailToast_PH_760x80", box);
            Place(toast, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(760, 80));
            AddImage(toast, sprite, new Color(0.05f, 0.06f, 0.10f, 0.92f), raycast: false);
            var toastText = NewText("Text", toast, font, "", 32, TextAnchor.MiddleCenter, Color.white);
            Stretch(toastText.rectTransform);
            toastText.resizeTextForBestFit = true;
            toastText.resizeTextMinSize = 20;
            toastText.resizeTextMaxSize = 32;
            toast.gameObject.SetActive(false);

            root.gameObject.SetActive(false);
            var pageObject = NewRect("MailPage", canvas);
            var page = pageObject.gameObject.AddComponent<MailPage>();
            var so = new SerializedObject(page);
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_openButton").objectReferenceValue = open;
            so.FindProperty("_openDot").objectReferenceValue = dot;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.FindProperty("_listContent").objectReferenceValue = content;
            so.FindProperty("_itemTemplate").objectReferenceValue = itemTemplate;
            so.FindProperty("_detail").objectReferenceValue = detail.gameObject;
            so.FindProperty("_detailTitle").objectReferenceValue = detailTitle;
            so.FindProperty("_detailSender").objectReferenceValue = sender;
            so.FindProperty("_detailDate").objectReferenceValue = date;
            so.FindProperty("_detailBody").objectReferenceValue = body;
            so.FindProperty("_attachBox").objectReferenceValue = attach.gameObject;
            so.FindProperty("_actionButton").objectReferenceValue = action;
            so.FindProperty("_emptyBox").objectReferenceValue = empty.gameObject;
            so.FindProperty("_countText").objectReferenceValue = count;
            so.FindProperty("_claimAllButton").objectReferenceValue = claimAll;
            so.FindProperty("_deleteReadButton").objectReferenceValue = deleteRead;
            so.FindProperty("_attachChips").objectReferenceValue = chips;
            so.FindProperty("_chipTemplate").objectReferenceValue = chipTemplate.gameObject;
            so.FindProperty("_expiryText").objectReferenceValue = expiry;
            so.FindProperty("_toast").objectReferenceValue = toast.gameObject;
            so.FindProperty("_toastText").objectReferenceValue = toastText;
            so.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        // Dòng thư mẫu (tắt): phong thư, tiêu đề, người gửi, ★ ghim, chấm đỏ chưa đọc.
        private static Button BuildItem(RectTransform box, Font font, Sprite sprite, Sprite knob)
        {
            var item = NewRect("MailItem_PH_600x120", box);
            Place(item, Center, Center, Vector2.zero, new Vector2(600, 120));
            var image = AddImage(item, sprite, RowColor, raycast: true);
            var button = item.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            var envelope = NewRect("Envelope_PH_56x40", item);
            Place(envelope, TopLeft, TopLeft, new Vector2(20, -40), new Vector2(56, 40));
            AddImage(envelope, null, Color.white, raycast: false);
            var flap = NewRect("Flap_PH_26x26", envelope);
            Place(flap, Center, Center, new Vector2(0, 8), new Vector2(26, 26));
            flap.localRotation = Quaternion.Euler(0, 0, 45);
            AddImage(flap, null, new Color(0.78f, 0.84f, 0.97f), raycast: false);

            var title = NewText("Title", item, font, "", 34, TextAnchor.MiddleLeft, Color.white);
            Place(title.rectTransform, TopLeft, TopLeft, new Vector2(100, -14), new Vector2(430, 50));
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 20;
            title.resizeTextMaxSize = 34;
            var sender = NewText("Sender", item, font, "", 26, TextAnchor.MiddleLeft, Soft);
            Place(sender.rectTransform, TopLeft, TopLeft, new Vector2(100, -68), new Vector2(430, 40));
            // ★ ghim: hình vuông xoay 45° (font mặc định không có ký tự ★).
            var star = NewRect("Star", item);
            Place(star, TopRight, Center, new Vector2(-24, -24), new Vector2(22, 22));
            star.localRotation = Quaternion.Euler(0, 0, 45);
            AddImage(star, null, Gold, raycast: false);
            var dot = NewRect("Dot", item);
            Place(dot, TopRight, Center, new Vector2(-24, -70), new Vector2(24, 24));
            AddImage(dot, knob, Red, raycast: false).type = Image.Type.Simple;
            var element = item.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 120;
            element.preferredHeight = 120;
            item.gameObject.SetActive(false);
            return button;
        }
    }
}
