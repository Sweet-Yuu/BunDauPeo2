using BunDauPeo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.EditorTools
{
    // Chỉ đổi lớp nhìn; giữ nguyên transform làm mốc cho simulation và binding của các view.
    public static class RestaurantLobbySkin
    {
        private static readonly Color Wood = new(0.65f, 0.39f, 0.20f);
        private static readonly Color WoodLight = new(0.86f, 0.66f, 0.38f);
        private static readonly Color Danger = new(0.66f, 0.22f, 0.15f);

        public static void Apply(RectTransform canvas)
        {
            var background = canvas.Find("Background")?.GetComponent<Image>();
            if (background != null && RestaurantArt.Garden != null)
            {
                background.sprite = RestaurantArt.Garden;
                background.type = Image.Type.Simple;
                background.color = Color.white;
                background.raycastTarget = false;
            }

            // Các cây tròn tạm được thay bằng cảnh vườn trong artwork.
            var garden = canvas.Find("Garden");
            if (garden != null) garden.gameObject.SetActive(false);
            SkinRoom(canvas.Find("Room") as RectTransform);
            ResourceCard(canvas.Find("CoinCard") as RectTransform, "coin");
            ResourceCard(canvas.Find("MaterialCard") as RectTransform, "ingredient_basket");
            ResourceCard(canvas.Find("GemCard") as RectTransform, "diamond");
            LightCard(canvas.Find("InfoBar") as RectTransform);
            LightCard(canvas.Find("AfkMessage") as RectTransform);

            var column = canvas.Find("RightColumn") as RectTransform;
            LightCard(column);
            if (column != null)
            {
                IconButton(column.Find("StaffButton") as RectTransform, "hospitality");
                IconButton(column.Find("UpgradeButton") as RectTransform, "master_chef");
                IconButton(column.Find("AfkButton") as RectTransform, "gift");
            }

            foreach (var pageName in new[] { "StaffPanelRoot", "UpgradePanelRoot", "QualityPopupRoot", "MenuPanel_PH_1920x1080" })
            {
                var page = canvas.Find(pageName) as RectTransform;
                if (page != null) SkinPage(page);
            }

            var afk = canvas.Find("AfkPopupRoot") as RectTransform;
            if (afk != null) SkinBox(afk.Find("AfkPopupBox") as RectTransform);
            SkinNavigation(canvas.Find("NavBar_PH_1920x120") as RectTransform);

            var menu = canvas.Find("MenuPanel_PH_1920x1080") as RectTransform;
            if (menu != null)
                Art(menu, "MenuPlatterArt_PH_270x270", "platter", new Vector2(.80f, .22f), new Vector2(.97f, .48f));
        }

        private static void SkinRoom(RectTransform room)
        {
            if (room == null) return;
            var floor = room.Find("Floor") as RectTransform;
            if (floor != null)
            {
                SetColor(floor, new Color(.96f, .86f, .65f));
                Border(floor.GetComponent<Image>(), WoodLight, new Vector2(5, -5));
                for (var x = 1; x < 12; x++)
                    TileLine(floor, "FloorColumn" + x, new Vector2(x / 12f, 0), new Vector2(x / 12f, 1), true);
                for (var y = 1; y < 8; y++)
                    TileLine(floor, "FloorRow" + y, new Vector2(0, y / 8f), new Vector2(1, y / 8f), false);
            }

            foreach (var wall in new[] { "WallBack", "WallLeft" }) SetColor(room.Find(wall), RestaurantArt.Jade);
            foreach (var trim in new[] { "WallBackTop", "WallLeftTop" }) SetColor(room.Find(trim), WoodLight);
            var furniture = room.Find("Furniture");
            if (furniture == null) return;
            var kitchen = furniture.Find("Kitchen") as RectTransform;
            if (kitchen != null)
            {
                RestaurantArt.Card(kitchen.GetComponent<Image>(), Wood);
                for (var i = 0; i < 3; i++)
                    Art(kitchen, "CookingStationArt" + i, "stove", new Vector2(i / 3f, .02f), new Vector2((i + 1) / 3f, 1.35f), upright: true);
            }

            var counter = furniture.Find("Counter") as RectTransform;
            if (counter != null)
            {
                RestaurantArt.Card(counter.GetComponent<Image>(), WoodLight);
                Art(counter, "RegisterArt_PH_130x90", "register", new Vector2(.18f, 0), new Vector2(.82f, 1.45f), upright: true);
            }

            for (var i = 0; i < 8; i++)
            {
                var table = furniture.Find("Table" + i) as RectTransform;
                if (table == null) continue;
                RestaurantArt.Card(table.GetComponent<Image>(), WoodLight);
                Border(table.GetComponent<Image>(), Wood, new Vector2(2, -2));
                Art(table, "MealArt_PH_90x70", "platter", new Vector2(.10f, .06f), new Vector2(.90f, 1.03f), upright: true);
            }

            SetColor(furniture.Find("Door"), new Color(.66f, .30f, .20f));
        }

        private static void TileLine(RectTransform floor, string name, Vector2 min, Vector2 max, bool vertical)
        {
            var child = floor.Find(name) as RectTransform;
            if (child == null)
            {
                child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
                child.SetParent(floor, false);
            }
            child.anchorMin = min;
            child.anchorMax = max;
            child.offsetMin = vertical ? new Vector2(-.7f, 0) : new Vector2(0, -.7f);
            child.offsetMax = vertical ? new Vector2(.7f, 0) : new Vector2(0, .7f);
            var image = child.GetComponent<Image>();
            image.color = new Color(.52f, .33f, .17f, .13f);
            image.raycastTarget = false;
        }

        private static void ResourceCard(RectTransform card, string icon)
        {
            if (card == null) return;
            LightCard(card);
            var label = card.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.rectTransform.offsetMin = new Vector2(72, 0);
                label.fontStyle = FontStyle.Bold;
                label.resizeTextMinSize = 18;
                label.resizeTextMaxSize = 31;
            }
            Art(card, "ResourceArt_PH_58x58", icon, new Vector2(0, .10f), new Vector2(0, .90f), new Vector2(8, 0), new Vector2(67, 0));
        }

        private static void IconButton(RectTransform rect, string icon)
        {
            if (rect == null) return;
            SkinButton(rect.GetComponent<Button>());
            Art(rect, "ActionArt_PH_70x70", icon, new Vector2(.19f, .41f), new Vector2(.81f, .98f));
            var label = rect.GetComponentInChildren<Text>(true);
            if (label == null) return;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = new Vector2(1, .43f);
            label.rectTransform.offsetMin = new Vector2(7, 3);
            label.rectTransform.offsetMax = new Vector2(-7, 0);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 23;
        }

        private static void SkinPage(RectTransform page)
        {
            var image = page.GetComponent<Image>();
            if (image != null) image.color = RestaurantArt.Cream;
            foreach (var child in page.GetComponentsInChildren<Image>(true))
            {
                // Dim roots keep blocking the whole screen; filled progress images keep their shape and state.
                if (child.type == Image.Type.Filled || child == image) continue;
                if (child.name == "Box" || child.name == "QualityPopupBox" || child.name == "ResetConfirm_PH_800x420")
                    LightCard(child.rectTransform);
            }
            foreach (var label in page.GetComponentsInChildren<Text>(true))
            {
                if (label.GetComponentInParent<Button>(true) == null)
                    label.color = label.name.Contains("Message") || label.name.Contains("Status") ? Danger : RestaurantArt.Ink;
                if (label.name.Contains("Title")) label.fontStyle = FontStyle.Bold;
            }
            foreach (var button in page.GetComponentsInChildren<Button>(true)) SkinButton(button);
        }

        private static void SkinBox(RectTransform box)
        {
            if (box == null) return;
            LightCard(box);
            foreach (var button in box.GetComponentsInChildren<Button>(true)) SkinButton(button);
        }

        private static void SkinNavigation(RectTransform bar)
        {
            if (bar == null) return;
            SetColor(bar, new Color(.15f, .25f, .15f));
            var ids = new[] { "master_owner", "master_host", "revenue", "hospitality", "gift", "register" };
            var index = 0;
            foreach (Transform child in bar)
            {
                var button = child.GetComponent<Button>();
                if (button == null) continue;
                SkinButton(button);
                var rect = (RectTransform)child;
                Art(rect, "TabArt_PH_70x70", ids[Mathf.Min(index++, ids.Length - 1)], new Vector2(0, .16f), new Vector2(0, .84f), new Vector2(18, 0), new Vector2(88, 0));
                var label = rect.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.rectTransform.offsetMin = new Vector2(88, 4);
                    label.rectTransform.offsetMax = new Vector2(-12, -4);
                    label.fontStyle = FontStyle.Bold;
                    label.resizeTextMaxSize = 30;
                }
                var lockRoot = rect.Find("Lock_PH_40x48") as RectTransform;
                if (lockRoot != null)
                {
                    lockRoot.anchoredPosition = new Vector2(136, 37);
                    lockRoot.localScale = Vector3.one * .55f;
                }
            }
        }

        private static void SkinButton(Button button)
        {
            if (button == null) return;
            var image = button.targetGraphic as Image;
            if (image != null)
            {
                var danger = button.name.Contains("Reset") || button.name == "YesButton";
                RestaurantArt.Card(image, danger ? Danger : RestaurantArt.Jade);
                Border(image, new Color(.73f, .57f, .30f), new Vector2(1, -1));
            }
            foreach (var label in button.GetComponentsInChildren<Text>(true)) label.color = RestaurantArt.Cream;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            colors.pressedColor = new Color(.82f, .82f, .82f);
            colors.disabledColor = new Color(.65f, .65f, .65f, .8f);
            button.colors = colors;
        }

        private static void LightCard(RectTransform rect)
        {
            if (rect == null) return;
            var image = rect.GetComponent<Image>();
            if (image != null)
            {
                RestaurantArt.Card(image, RestaurantArt.Cream);
                Border(image, new Color(.77f, .62f, .36f), new Vector2(2, -2));
            }
            foreach (var label in rect.GetComponentsInChildren<Text>(true))
                if (label.GetComponentInParent<Button>(true) == null || label.transform.parent == rect)
                    label.color = RestaurantArt.Ink;
        }

        private static void Border(Image image, Color color, Vector2 distance)
        {
            if (image == null) return;
            var outline = image.GetComponent<Outline>();
            if (outline == null) outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void SetColor(Transform transform, Color color)
        {
            if (transform == null) return;
            var image = transform.GetComponent<Image>();
            if (image != null) image.color = color;
        }

        private static void Art(RectTransform parent, string name, string icon, Vector2 min, Vector2 max,
            Vector2 lower = default, Vector2 upper = default, bool upright = false)
        {
            var existing = parent.Find(name)?.GetComponent<Image>();
            var image = existing != null ? existing : RestaurantArt.Illustration(parent, name, icon, min, max, lower, upper);
            image.sprite = RestaurantArt.Icon(icon);
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;
            if (upright) image.rectTransform.localRotation = Quaternion.Inverse(parent.localRotation);
        }
    }
}
