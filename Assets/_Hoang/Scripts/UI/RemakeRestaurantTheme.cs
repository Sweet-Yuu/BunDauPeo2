using BunDauPeo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Hoang.Remake
{
    // Applied to authored views and runtime fallbacks without replacing their controllers or bindings.
    public static class RemakeRestaurantTheme
    {
        public static void Apply(RemakeShell shell)
        {
            var safe = shell.transform.Find("SafeArea") as RectTransform;
            if (safe == null) return;
            safe.GetComponent<Image>().color = RestaurantArt.Cream;
            var garden = Art(safe, "RestaurantGardenArtwork", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            garden.sprite = RestaurantArt.Garden;
            garden.preserveAspect = false;
            garden.transform.SetAsFirstSibling();
            Surface(safe, "HeaderSurface", new Vector2(0, .90f), Vector2.one, new Vector2(12, -2), new Vector2(-12, -8));
            garden.transform.SetAsFirstSibling();
            Art(safe, "RestaurantEmblem", "platter", new Vector2(0, .90f), new Vector2(.065f, 1), new Vector2(12, 8), new Vector2(-4, -8));
            Place(safe, "Title", new Vector2(.067f, .91f), new Vector2(.36f, 1), Vector2.zero, new Vector2(0, -7));
            var title = safe.Find("Title").GetComponent<Text>();
            title.fontStyle = FontStyle.Bold;
            title.resizeTextMaxSize = 29;
            Place(safe, "Balances", new Vector2(.39f, .91f), new Vector2(.89f, 1), new Vector2(20, 0), new Vector2(-10, -8));
            safe.Find("Balances").GetComponent<Text>().resizeTextMaxSize = 22;
            ApplyResourcePills(shell, safe);
            for (var i = 0; i < 3; i++)
            {
                var tab = safe.Find("Tab" + i);
                if (tab == null) continue;
                RestaurantArt.Card(tab.GetComponent<Image>(), i == 0 ? RestaurantArt.Jade : RestaurantArt.Cream);
                var label = tab.GetComponentInChildren<Text>();
                label.color = i == 0 ? Color.white : RestaurantArt.Ink;
                label.fontStyle = FontStyle.Bold;
                RemakeUI.Layout(label.rectTransform, new Vector2(.18f, 0), Vector2.one, new Vector2(6, 3), new Vector2(-12, -3));
                Art(tab, "TabArtwork", i == 0 ? "cooking" : i == 1 ? "stove" : "gift",
                    new Vector2(.05f, .12f), new Vector2(.21f, .88f), Vector2.zero, Vector2.zero);
            }

            foreach (var text in safe.GetComponentsInChildren<Text>(true))
            {
                text.raycastTarget = false;
                if (text.transform.parent.GetComponent<Button>() == null) text.color = RestaurantArt.Ink;
            }
            foreach (var button in safe.GetComponentsInChildren<Button>(true))
            {
                if (button.name.StartsWith("Skill_") || button.name.StartsWith("Tab")) continue;
                RestaurantArt.Card(button.GetComponent<Image>(), button.name.Contains("Reset") ? RestaurantArt.Purple : RestaurantArt.Jade);
                var label = button.GetComponentInChildren<Text>(true);
                if (label != null) { label.color = Color.white; label.fontStyle = FontStyle.Bold; }
            }
            ApplySkills(shell.SkillTree);
            ApplyKitchen(shell.FacilityGrid);
            ApplyShop(shell.Shop);
        }

        private static void ApplyResourcePills(RemakeShell shell, RectTransform safe)
        {
            var root = safe.Find("ResourcePills") as RectTransform;
            if (root == null) root = RemakeUI.Panel(safe, "ResourcePills", Color.clear,
                new Vector2(.375f, .913f), new Vector2(.895f, .99f), Vector2.zero, Vector2.zero);
            var ids = new[] { "coin", "diamond", "ingredient_basket" };
            var names = new[] { "Gold\n1,000", "Diamond\n5,000", "Ingredients\n0" };
            var labels = new Text[3];
            for (var i = 0; i < ids.Length; i++)
            {
                var pill = root.Find("Resource" + i) as RectTransform;
                if (pill == null) pill = RemakeUI.Panel(root, "Resource" + i, RestaurantArt.SoftGreen,
                    new Vector2(i / 3f, 0), new Vector2((i + 1) / 3f, 1), new Vector2(6, 4), new Vector2(-6, -4));
                Art(pill, "ResourceArtwork", ids[i], new Vector2(0, .08f), new Vector2(.28f, .92f), new Vector2(7, 0), Vector2.zero);
                var label = pill.Find("ResourceLabel");
                labels[i] = label == null ? RemakeUI.Label(pill, "ResourceLabel", names[i], 22, TextAnchor.MiddleCenter,
                    new Vector2(.28f, .04f), new Vector2(.97f, .96f), Vector2.zero, Vector2.zero) : label.GetComponent<Text>();
                labels[i].fontStyle = FontStyle.Bold;
            }
            shell.ConfigureBalanceLabels(labels);
        }

        public static void ApplySkills(SkillTreeUI tree)
        {
            if (tree == null) return;
            RestaurantArt.Card(tree.GetComponent<Image>(), RestaurantArt.Cream);
            var title = tree.transform.Find("Title").GetComponent<Text>();
            title.text = "Restaurant skills";
            title.fontStyle = FontStyle.Bold;
            title.resizeTextMaxSize = 34;
            var graph = tree.transform.Find("EditableSkillGraph");
            RestaurantArt.Card(graph.GetComponent<Image>(), new Color(.96f, .92f, .79f));
            var lines = graph.GetComponentInChildren<SkillTreeConnections>();
            if (lines != null) lines.color = new Color(.62f, .46f, .18f, .8f);
            Surface((RectTransform)tree.transform, "SkillSummaryCard", new Vector2(.74f, .12f), new Vector2(.98f, .78f), Vector2.zero, Vector2.zero);
            Place(tree.transform, "SkillBuffSummary", new Vector2(.765f, .40f), new Vector2(.96f, .745f), Vector2.zero, Vector2.zero);
            tree.transform.Find("SkillBuffSummary").GetComponent<Text>().resizeTextMaxSize = 21;
            Art(tree.transform, "SkillPlatterArtwork", "platter", new Vector2(.755f, .14f), new Vector2(.965f, .39f), Vector2.zero, Vector2.zero);
            foreach (var slot in tree.Slots) if (slot != null) slot.ApplyAppearance();
        }

        public static void ApplyKitchen(FacilityGridUI kitchen)
        {
            if (kitchen == null) return;
            RestaurantArt.Card(kitchen.GetComponent<Image>(), RestaurantArt.Cream);
            var root = kitchen.transform;
            var title = root.Find("Title").GetComponent<Text>();
            title.fontStyle = FontStyle.Bold;
            title.resizeTextMaxSize = 32;
            Surface((RectTransform)root, "InventorySurface", new Vector2(.01f, .045f), new Vector2(.31f, .92f), new Vector2(-6, -2), new Vector2(6, 2));
            root.Find("InventoryTitle").GetComponent<Text>().color = RestaurantArt.Jade;
            var grid = kitchen.GridRoot;
            grid.GetComponent<Image>().color = RestaurantArt.Jade;
            foreach (Transform cell in grid)
            {
                if (!cell.name.StartsWith("Cell_")) continue;
                var image = cell.GetComponent<Image>();
                var parts = cell.name.Split('_');
                var alternate = (int.Parse(parts[1]) + int.Parse(parts[2])) % 2 == 0;
                image.color = alternate ? new Color(.94f, .91f, .79f) : new Color(.85f, .87f, .73f);
            }
            kitchen.ApplyInventoryArtwork();
        }

        public static void ApplyShop(ShopDailyRewardsUI shop)
        {
            if (shop == null) return;
            var root = shop.transform;
            RestaurantArt.Card(root.GetComponent<Image>(), RestaurantArt.Cream);
            root.Find("Title").GetComponent<Text>().fontStyle = FontStyle.Bold;
            var daily = root.Find("DailyRewardPanel");
            RestaurantArt.Card(daily.GetComponent<Image>(), RestaurantArt.SoftGreen);
            Art(daily, "DailyGiftArtwork", "gift", new Vector2(.015f, .06f), new Vector2(.13f, .94f), Vector2.zero, Vector2.zero);
            Place(daily, "DailyStatus", new Vector2(.15f, .64f), new Vector2(.68f, .98f), Vector2.zero, Vector2.zero);
            Place(daily, "DailyReward", new Vector2(.15f, .30f), new Vector2(.68f, .68f), Vector2.zero, Vector2.zero);
            Place(daily, "Countdown", new Vector2(.15f, .06f), new Vector2(.68f, .29f), Vector2.zero, Vector2.zero);
            var ids = new[] { "diamond", "pouch", "chest", "vault" };
            for (var i = 0; i < ids.Length; i++)
            {
                var card = root.Find(i == 0 ? "FreeAdCard" : "GoldOffer" + i);
                RestaurantArt.Card(card.GetComponent<Image>(), new Color(1f, .98f, .91f));
                Place(card, "Name", new Vector2(0, .83f), Vector2.one, new Vector2(12, 0), new Vector2(-12, -2));
                card.Find("Name").GetComponent<Text>().fontStyle = FontStyle.Bold;
                Art(card, "OfferArtwork", ids[i], new Vector2(.17f, .37f), new Vector2(.83f, .82f), Vector2.zero, Vector2.zero);
                Place(card, "Amount", new Vector2(0, .29f), new Vector2(1, .43f), new Vector2(10, 0), new Vector2(-10, 0));
                var amount = card.Find("Amount").GetComponent<Text>();
                amount.fontStyle = FontStyle.Bold;
                amount.resizeTextMaxSize = 25;
                Place(card, "Details", new Vector2(0, .21f), new Vector2(1, .30f), new Vector2(8, 0), new Vector2(-8, 0));
                var button = card.GetComponentInChildren<Button>(true);
                RemakeUI.Layout((RectTransform)button.transform, Vector2.zero, new Vector2(1, .205f), new Vector2(14, 10), new Vector2(-14, -2));
                RestaurantArt.Card(button.GetComponent<Image>(), i == 0 ? RestaurantArt.Purple : RestaurantArt.Jade);
            }
        }

        public static Image Art(Transform parent, string name, string id, Vector2 min, Vector2 max, Vector2 lower, Vector2 upper)
        {
            var child = parent.Find(name);
            var image = child == null
                ? RestaurantArt.Illustration(parent, name, id, min, max, lower, upper)
                : child.GetComponent<Image>();
            RemakeUI.Layout(image.rectTransform, min, max, lower, upper);
            if (id != null) image.sprite = RestaurantArt.Icon(id);
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static void Surface(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 lower, Vector2 upper)
        {
            var child = parent.Find(name) as RectTransform;
            if (child == null) child = RemakeUI.Panel(parent, name, RestaurantArt.Cream, min, max, lower, upper);
            RemakeUI.Layout(child, min, max, lower, upper);
            RestaurantArt.Card(child.GetComponent<Image>(), RestaurantArt.Cream);
            child.GetComponent<Image>().raycastTarget = false;
            child.SetAsFirstSibling();
        }

        private static void Place(Transform parent, string name, Vector2 min, Vector2 max, Vector2 lower, Vector2 upper)
        {
            var child = parent.Find(name) as RectTransform;
            if (child != null) RemakeUI.Layout(child, min, max, lower, upper);
        }
    }
}
