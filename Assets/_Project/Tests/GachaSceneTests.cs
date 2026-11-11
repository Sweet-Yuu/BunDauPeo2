using System;
using System.Linq;
using BunDauPeo.Boot;
using BunDauPeo.EditorTools;
using BunDauPeo.Gacha;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    public class GachaSceneTests
    {
        // Mở scene từ đĩa (L-002) và chỉ đóng nếu test là bên mở nó.
        private static void WithScene(string path, Action<Scene> check)
        {
            var wasOpen = SceneManager.GetSceneByPath(path).isLoaded;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                check(scene);
            }
            finally
            {
                if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T[] All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        [Test]
        public void GachaScene_ViewIsWired_AndUsesOnlyImageAndText() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                Assert.AreEqual(1, All<Canvas>(scene).Count(c => !c.overrideSorting), "one main canvas; boxes drawn over the bar own a child Canvas");
                var view = All<GachaView>(scene).Single();
                var so = new SerializedObject(view);
                foreach (var field in new[]
                         {
                             "_banner", "_characters", "_singleButton", "_tenButton", "_skipButton", "_historyButton",
                             "_bannerNameText", "_pityText", "_messageText", "_revealView", "_totalView", "_rollAgainPopup", "_historyPopup",
                         })
                    Assert.IsNotNull(so.FindProperty(field).objectReferenceValue, field);

                var banner = (GachaBannerDefinition)so.FindProperty("_banner").objectReferenceValue;
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<GachaBannerDefinition>(ProjectSetup.BannerPath).Id, banner.Id);
                Assert.AreEqual(0, All<RawImage>(scene).Length);
            });

        [Test]
        public void GachaScene_HasRevealTotalAndPopupsWired() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var reveal = new SerializedObject(All<GachaRevealView>(scene).Single());
                foreach (var field in new[] { "_root", "_commonAnimator", "_legendaryAnimator", "_revealPanel", "_flash", "_portrait", "_nameText", "_starsText" })
                    Assert.IsNotNull(reveal.FindProperty(field).objectReferenceValue, "reveal " + field);

                var total = new SerializedObject(All<GachaTotalView>(scene).Single());
                foreach (var field in new[] { "_root", "_grid", "_iconTemplate", "_okButton" })
                    Assert.IsNotNull(total.FindProperty(field).objectReferenceValue, "total " + field);

                var again = new SerializedObject(All<GachaRollAgainPopup>(scene).Single());
                foreach (var field in new[] { "_root", "_warningText", "_rollAgainLabel", "_rollAgainButton", "_dismissButton" })
                    Assert.IsNotNull(again.FindProperty(field).objectReferenceValue, "popup " + field);

                var history = new SerializedObject(All<GachaHistoryPopup>(scene).Single());
                foreach (var field in new[] { "_root", "_listText", "_closeButton" })
                    Assert.IsNotNull(history.FindProperty(field).objectReferenceValue, "history " + field);
            });

        // Hộp Lịch sử quay nền tối như các hộp Gacha khác (user chốt 2026-11-12) nhưng có viền sáng để thấy rõ trên nền mờ.
        [Test]
        public void GachaScene_HistoryBox_DarkWithLightBorder() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var box = All<RectTransform>(scene).Single(r => r.name == "GachaHistoryBox_PH_1200x800");
                var fill = box.GetComponent<Image>().color;
                var outline = box.GetComponent<Outline>();
                Assert.IsNotNull(outline, "history box has a border");
                Assert.GreaterOrEqual(outline.effectColor.a, 0.8f, "border is visible");
                Assert.Greater(outline.effectColor.grayscale, fill.grayscale + 0.4f, "border is clearly lighter than the box");
                Assert.GreaterOrEqual(Mathf.Abs(outline.effectDistance.x), 3f, "border is thick enough to see");
            });

        [Test]
        public void GachaScene_CutscenePrefabsAreInstanced_WithTheirControllers() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var reveal = new SerializedObject(All<GachaRevealView>(scene).Single());
                var common = (Animator)reveal.FindProperty("_commonAnimator").objectReferenceValue;
                var legendary = (Animator)reveal.FindProperty("_legendaryAnimator").objectReferenceValue;
                Assert.AreEqual("Assets/_Project/Gacha/Animations/Summoning/CommonSummon.controller", AssetDatabase.GetAssetPath(common.runtimeAnimatorController));
                Assert.AreEqual("Assets/_Project/Gacha/Animations/Summoning/LegendarySummon.controller", AssetDatabase.GetAssetPath(legendary.runtimeAnimatorController));
            });

        [Test]
        public void GachaScene_TotalGridIsFiveColumns() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var total = new SerializedObject(All<GachaTotalView>(scene).Single());
                var grid = ((RectTransform)total.FindProperty("_grid").objectReferenceValue).GetComponent<GridLayoutGroup>();
                Assert.AreEqual(GridLayoutGroup.Constraint.FixedColumnCount, grid.constraint);
                Assert.AreEqual(5, grid.constraintCount);
            });

        // G4: nút Bỏ qua (chỉ hiện lúc mở thẻ) ở góc trên phải, xa nút Quay ở góc dưới phải.
        [Test]
        public void GachaScene_SkipButtonTopRight() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var skip = All<Button>(scene).Single(b => b.name == "SkipButton");
                var rect = RestaurantSceneTests.InReferenceCanvas((RectTransform)skip.transform);
                Assert.GreaterOrEqual(rect.xMin, 1500f, "SkipButton must sit in the right part of the screen");
                Assert.LessOrEqual(1080f - rect.yMax, 140f, "SkipButton must hug the top edge, clear of the pull buttons at the bottom right");
            });

        // MessageText... trùng tên với màn quán và bản đồ nên chỉ tìm trong panel Gacha.
        private static RectTransform In(Scene scene, string name) =>
            LobbySceneTests.In<RectTransform>(scene, LobbySceneTests.GachaPanel).Single(r => r.name == name);

        private static Rect Box(Scene scene, string name) => RestaurantSceneTests.InReferenceCanvas(In(scene, name));

        // G1: mọi khối trang banner nằm trong màn, không đè nhau và các nút/dòng báo nằm trên vạch 120 px của thanh dưới.
        [Test]
        public void GachaScene_BannerPageBlocksInCorners() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var screen = new Rect(0, 0, 1920, 1080);
                // Banner kín panel nằm dưới mọi khối khác (mục 5 index 2), nên không tính đè nhau với nó.
                var names = new[] { "BannerPanel", "GachaBanner_PH_1920x1080", "SingleButton", "TenButton", "HistoryButton", "MessageText" };
                var rects = names.Select(n => Box(scene, n)).ToArray();
                for (var i = 0; i < names.Length; i++)
                {
                    Assert.IsTrue(screen.Contains(rects[i].min) && screen.Contains(rects[i].max - Vector2.one), $"{names[i]} {rects[i]} leaves the screen");
                    for (var j = i + 1; j < names.Length; j++)
                        if (i != 1 && j != 1)
                            Assert.IsFalse(Overlap(rects[i], rects[j]), $"{names[i]} {rects[i]} overlaps {names[j]} {rects[j]}");
                }

                foreach (var i in new[] { 2, 3, 4, 5 })
                    Assert.GreaterOrEqual(rects[i].yMin, HubNav.BarHeight, $"{names[i]} reaches the bar strip");

                var panel = In(scene, "BannerPanel");
                foreach (var child in new[] { "BannerNameText", "PityText" })
                {
                    var rect = RestaurantSceneTests.InReferenceCanvas(In(scene, child));
                    Assert.AreSame(panel, In(scene, child).parent, child + " belongs to BannerPanel");
                    Assert.IsTrue(rects[0].Contains(rect.min) && rects[0].Contains(rect.max - Vector2.one), child + " leaves BannerPanel");
                }

                Assert.LessOrEqual(rects[0].xMax, 800f, "BannerPanel is top left");
                Assert.GreaterOrEqual(rects[0].yMin, 900f, "BannerPanel is at the top");
                Assert.IsFalse(All<Image>(scene).Single(i => i.name == "GachaBanner_PH_1920x1080").raycastTarget, "the banner must not block touches");
                Assert.AreEqual(new Vector2(300, 160), rects[2].size);
                Assert.AreEqual(new Vector2(300, 160), rects[3].size);
                Assert.Greater(rects[3].xMin, rects[2].xMin, "TenButton is right of SingleButton");
                Assert.GreaterOrEqual(rects[2].yMin, rects[3].yMin - 1f);
                Assert.Greater(rects[4].yMin, rects[2].yMax - 1f, "HistoryButton sits above the pull buttons");
                Assert.Greater(rects[3].xMin, 1500f, "pull buttons are bottom right");
            });

        // Spec UI hub 5.4: banner kéo kín panel (kể cả phần dưới thanh), nút quay trên vạch, GachaView biết HubNav để ẩn thanh lúc lộ.
        [Test]
        public void Banner_FillsPanel() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var banner = In(scene, "GachaBanner_PH_1920x1080");
                Assert.AreSame(LobbySceneTests.Named(scene, LobbySceneTests.GachaPanel), banner.parent, "the banner is a direct child of the Gacha panel");
                Assert.AreEqual(Vector2.zero, banner.anchorMin);
                Assert.AreEqual(Vector2.one, banner.anchorMax);
                Assert.AreEqual(Vector2.zero, banner.offsetMin);
                Assert.AreEqual(Vector2.zero, banner.offsetMax);
                Assert.AreEqual(0, banner.GetSiblingIndex(), "the banner draws under everything else in the panel");
                foreach (var name in new[] { "SingleButton", "TenButton" })
                    Assert.GreaterOrEqual(Box(scene, name).yMin, HubNav.BarHeight, $"{name} reaches the bar strip");

                var hub = new SerializedObject(All<GachaView>(scene).Single()).FindProperty("_hub").objectReferenceValue;
                Assert.IsNotNull(hub, "GachaView._hub");
                Assert.AreSame(All<HubNav>(scene).Single(), hub);
            });

        private static bool Overlap(Rect a, Rect b) => a.xMin < b.xMax - 1 && b.xMin < a.xMax - 1 && a.yMin < b.yMax - 1 && b.yMin < a.yMax - 1;

        // G2, G3: tên và sao ở góc dưới phải của panel mở thẻ, trang tổng gọn trong khung.
        [Test]
        public void GachaScene_RevealNameBottomRight_TotalInsidePanel() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var panel = Box(scene, "RevealPanel");
                Assert.AreEqual(new Vector2(1800, 1000), panel.size);
                var portrait = RestaurantSceneTests.InReferenceCanvas((RectTransform)All<RectTransform>(scene).Single(r => r.name == "RevealPanel").Find("Portrait"));
                Assert.AreEqual(new Vector2(760, 760), portrait.size);
                Assert.Less(portrait.center.x, panel.center.x, "Portrait is in the left half");
                Assert.IsTrue(panel.Contains(portrait.min) && panel.Contains(portrait.max - Vector2.one));
                foreach (var name in new[] { "NameText", "StarsText" })
                {
                    var rect = All<RectTransform>(scene).Where(r => r.name == name && r.GetComponentInParent<GachaRevealView>(true) != null)
                        .Select(RestaurantSceneTests.InReferenceCanvas).Single();
                    Assert.IsTrue(panel.Contains(rect.min) && panel.Contains(rect.max - Vector2.one), name + " leaves RevealPanel");
                    Assert.Greater(rect.xMin, panel.center.x, name + " is in the right half");
                    Assert.Less(rect.yMax, panel.y + panel.height / 4f, name + " is in the bottom quarter");
                    Assert.IsFalse(Overlap(rect, portrait), name + " overlaps Portrait");
                }

                var total = Box(scene, "TotalPanel");
                Assert.AreEqual(new Vector2(1300, 640), total.size);
                var grid = All<GridLayoutGroup>(scene).Single();
                var gridRect = RestaurantSceneTests.InReferenceCanvas((RectTransform)grid.transform);
                Assert.IsTrue(total.Contains(gridRect.min) && total.Contains(gridRect.max - Vector2.one), "Grid leaves TotalPanel");
                Assert.AreEqual(new Vector2(200, 220), grid.cellSize);
                // 10 ô = 5 cột × 2 hàng phải vừa khung lưới.
                Assert.LessOrEqual(2 * grid.cellSize.y + grid.spacing.y, gridRect.height);
                Assert.LessOrEqual(5 * grid.cellSize.x + 4 * grid.spacing.x, gridRect.width);
                var ok = Box(scene, "OkButton");
                Assert.IsFalse(Overlap(ok, total), "OkButton must sit below TotalPanel");
                Assert.Less(ok.yMax, total.yMin + 1f);
                Assert.GreaterOrEqual(ok.yMin, 0f);
                Assert.AreEqual("Kết quả quay", All<Text>(scene).Single(t => t.name == "TitleText" && t.GetComponentInParent<GachaTotalView>(true) != null).text);
            });

        [Test]
        public void BootScene_StartSceneIsRestaurant() =>
            WithScene(ProjectSetup.BootScenePath, scene =>
            {
                var bootstrap = All<GameBootstrap>(scene).Single();
                Assert.AreEqual("Restaurant",new SerializedObject(bootstrap).FindProperty("_startScene").stringValue);
            });

        [Test]
        public void GachaScene_LocalizedTexts_KeysExistAndNotWrittenByViews() =>
            WithScene(ProjectSetup.RestaurantScenePath, SceneTextAsserts.AssertLocalizedTexts);
    }
}
