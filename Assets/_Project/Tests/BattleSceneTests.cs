using System.Linq;
using BunDauPeo.Combat;
using BunDauPeo.EditorTools;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    public class BattleSceneTests
    {
        private static void WithBattleScene(System.Action<UnityEngine.SceneManagement.Scene> check) =>
            RestaurantSceneTests.WithScene(ProjectSetup.BattleScenePath, check);

        [Test]
        public void BattleScene_HasCameraAndEventSystemWithInputSystemModule() =>
            WithBattleScene(scene =>
            {
                Assert.AreEqual(1, RestaurantSceneTests.All<Camera>(scene).Length);
                var systems = RestaurantSceneTests.All<EventSystem>(scene);
                Assert.AreEqual(1, systems.Length);
                Assert.IsNotNull(systems[0].GetComponent<InputSystemUIInputModule>());
                Assert.IsNull(systems[0].GetComponent<StandaloneInputModule>());
            });

        [Test]
        public void BattleScene_ViewWired_OnlyImageAndText_ScalerMatchesHeight() =>
            WithBattleScene(scene =>
            {
                var views = RestaurantSceneTests.All<BattleView>(scene);
                Assert.AreEqual(1, views.Length);
                RestaurantSceneTests.AssertAllReferencesAssigned(views[0]);

                var units = RestaurantSceneTests.All<BattleUnitView>(scene);
                Assert.AreEqual(10, units.Length, "5 allies + 5 enemies");
                foreach (var unit in units) RestaurantSceneTests.AssertAllReferencesAssigned(unit);

                Assert.AreEqual(0, RestaurantSceneTests.All<RawImage>(scene).Length);
                var canvas = RestaurantSceneTests.All<CanvasScaler>(scene).Single();
                Assert.AreEqual(1f, canvas.matchWidthOrHeight);
                Assert.AreEqual(new Vector2(1920, 1080), canvas.referenceResolution);
            });

        [Test]
        public void BattleScene_ActionPanelAndCardsWired() =>
            WithBattleScene(scene =>
            {
                var panels = RestaurantSceneTests.All<BattleActionPanel>(scene);
                Assert.AreEqual(1, panels.Length);
                RestaurantSceneTests.AssertAllReferencesAssigned(panels[0]);
                Assert.AreEqual(3, panels[0].GetComponentsInChildren<Button>(true).Length, "Basic, Skill, Ultimate");

                var cards = RestaurantSceneTests.All<BattleCardView>(scene);
                Assert.AreEqual(5, cards.Length);
                foreach (var card in cards) RestaurantSceneTests.AssertAllReferencesAssigned(card);
            });

        [Test]
        public void BattleScene_CardsAndButtonsInCardArea() =>
            WithBattleScene(scene =>
            {
                var rects = RestaurantSceneTests.All<BattleCardView>(scene)
                    .Select(c => (c.name, (RectTransform)c.transform))
                    .Concat(RestaurantSceneTests.All<BattleActionPanel>(scene).Single().GetComponentsInChildren<Button>(true)
                        .Select(b => (b.name, (RectTransform)b.transform)))
                    .Select(x => (Name: x.Item1, Rect: RestaurantSceneTests.InReferenceCanvas(x.Item2)))
                    .ToArray();
                Assert.AreEqual(8, rects.Length);

                var canvas = new Rect(0, 0, 1920, 1080);
                foreach (var (name, rect) in rects)
                {
                    Assert.IsTrue(canvas.Contains(rect.min) && canvas.Contains(rect.max), $"{name} {rect} leaves the canvas");
                    // InReferenceCanvas có gốc dưới trái; kế hoạch tính y từ trên.
                    Assert.GreaterOrEqual(1080f - rect.yMax, BattleLayout.CardAreaTop, $"{name} starts above the card area");
                    Assert.LessOrEqual(1080f - rect.yMin, BattleLayout.CardAreaBottom, $"{name} ends below the card area");
                }

                for (var i = 0; i < rects.Length; i++)
                {
                    for (var j = i + 1; j < rects.Length; j++)
                    {
                        var a = rects[i].Rect;
                        var b = rects[j].Rect;
                        var overlapX = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                        var overlapY = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                        Assert.IsFalse(overlapX > 1f && overlapY > 1f, $"{rects[i].Name} overlaps {rects[j].Name}");
                    }
                }

                foreach (var button in RestaurantSceneTests.All<BattleActionPanel>(scene).Single().GetComponentsInChildren<Button>(true))
                {
                    var size = RestaurantSceneTests.InReferenceCanvas((RectTransform)button.transform).size;
                    Assert.GreaterOrEqual(Mathf.Min(size.x, size.y), 110f, $"{button.name} is too small to tap");
                }
            });

        [Test]
        public void BattleScene_ResultContentInsideBox_ButtonsApart() =>
            WithBattleScene(scene =>
            {
                var view = RestaurantSceneTests.All<BattleResultView>(scene).Single();
                var so = new SerializedObject(view);
                var root = (GameObject)so.FindProperty("_root").objectReferenceValue;
                var box = (RectTransform)root.transform.Find("ResultBox");
                Assert.IsNotNull(box, "ResultBox holds every text and button of the result");
                var boxRect = RestaurantSceneTests.InReferenceCanvas(box);
                Assert.AreEqual(new Vector2(1400, 760), boxRect.size);
                Assert.AreEqual(1f, box.GetComponent<Image>().color.a, "the box is solid so nothing behind shows through the text");

                var row = (RewardRow)so.FindProperty("_rewardRow").objectReferenceValue;
                Assert.IsNotNull(row);
                var members = root.GetComponentsInChildren<Text>(true).Select(t => (Component)t)
                    .Concat(root.GetComponentsInChildren<Button>(true))
                    .Append(row)
                    .ToArray();
                foreach (var member in members)
                {
                    Assert.IsTrue(member.transform.IsChildOf(box), $"{member.name} is outside ResultBox in the hierarchy");
                    var rect = RestaurantSceneTests.InReferenceCanvas((RectTransform)member.transform);
                    Assert.IsTrue(rect.xMin >= boxRect.xMin - 1f && rect.xMax <= boxRect.xMax + 1f
                                  && rect.yMin >= boxRect.yMin - 1f && rect.yMax <= boxRect.yMax + 1f, $"{member.name} {rect} leaves the box {boxRect}");
                }

                var confirm = RestaurantSceneTests.InReferenceCanvas((RectTransform)root.transform.Find("ResultBox/ConfirmButton"));
                var retry = RestaurantSceneTests.InReferenceCanvas((RectTransform)root.transform.Find("ResultBox/RetryButton"));
                var back = RestaurantSceneTests.InReferenceCanvas((RectTransform)root.transform.Find("ResultBox/BackButton"));
                Assert.IsFalse(retry.Overlaps(back), "Retry and Back overlap");
                Assert.LessOrEqual(back.xMax, boxRect.center.x, "Back sits in the left half");
                Assert.GreaterOrEqual(retry.xMin, boxRect.center.x, "Retry sits in the right half");
                Assert.AreEqual(retry.center.y, back.center.y, 1f, "Back and Retry share a row");
                Assert.Less(confirm.yMax, boxRect.center.y, "Confirm is in the lower half");
                Assert.AreEqual(boxRect.center.x, confirm.center.x, 1f, "Confirm is centred");
            });

        private static CombatRig OneStageRig() =>
            new CombatRig(new[] { CombatTestData.Stage("b1", new[] { CombatTestData.Enemy(hp: 1) }) });

        [Test]
        public void BattleResult_NotPaid_HidesRewardRow() =>
            WithBattleScene(scene =>
            {
                var rig = OneStageRig();
                var trial = rig.Service.StartBattle("b1").Session;
                CombatTestData.PlayToWin(trial);
                var view = RestaurantSceneTests.All<BattleResultView>(scene).Single();

                view.Show(rig.Service, trial);

                var so = new SerializedObject(view);
                var row = (RewardRow)so.FindProperty("_rewardRow").objectReferenceValue;
                Assert.IsFalse(row.gameObject.activeSelf, "a DEV battle has no reward cells, not even empty ones");
                Assert.AreEqual(CombatViewText.TrialLine(), ((Text)so.FindProperty("_rewardText").objectReferenceValue).text);
            });

        [Test]
        public void BattleResult_Success_ShowsRewardCellsWithFirstClearLabel() =>
            WithBattleScene(scene =>
            {
                var rig = OneStageRig();
                var won = CombatTestData.PaidWin(rig.Service, "b1");
                var view = RestaurantSceneTests.All<BattleResultView>(scene).Single();

                view.Show(rig.Service, won);

                var so = new SerializedObject(view);
                var row = (RewardRow)so.FindProperty("_rewardRow").objectReferenceValue;
                Assert.IsTrue(row.gameObject.activeSelf);
                Assert.AreEqual("30 Material", row.transform.Find("Cell0/Amount").GetComponent<Text>().text);
                Assert.AreEqual("150 Gem", row.transform.Find("Cell1/Amount").GetComponent<Text>().text);
                Assert.IsFalse(row.transform.Find("Cell2").gameObject.activeSelf);
                Assert.AreEqual("Lần đầu", row.transform.Find("Label").GetComponent<Text>().text);
                Assert.AreEqual("", ((Text)so.FindProperty("_rewardText").objectReferenceValue).text, "the cells replace the reward sentence");
            });

        [Test]
        public void BattleScene_ResultViewWired_StartsClosed() =>
            WithBattleScene(scene =>
            {
                var result = RestaurantSceneTests.All<BattleResultView>(scene).Single();
                RestaurantSceneTests.AssertAllReferencesAssigned(result);
                var root = (GameObject)new SerializedObject(result).FindProperty("_root").objectReferenceValue;
                Assert.IsFalse(root.activeSelf, "the result screen appears only when the battle ends");
                Assert.IsTrue(root.GetComponent<Image>().raycastTarget, "must block clicks behind it");
                Assert.IsTrue(result.gameObject.activeSelf);
                Assert.AreSame(result, new SerializedObject(RestaurantSceneTests.All<BattleView>(scene).Single()).FindProperty("_result").objectReferenceValue);
            });

        [Test]
        public void BattleScene_InBuildSettings_AfterRestaurant()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.GreaterOrEqual(scenes.Length, 5);
            Assert.AreEqual(ProjectSetup.RestaurantScenePath, scenes[2].path);
            Assert.AreEqual(ProjectSetup.StageSelectScenePath, scenes[3].path, "the map sits between the restaurant and the battle");
            Assert.AreEqual(ProjectSetup.BattleScenePath, scenes[4].path);
            Assert.IsTrue(scenes[4].enabled);
        }

        [Test]
        public void BattleScene_TopBarWired_InsideCanvas() =>
            WithBattleScene(scene =>
            {
                var view = RestaurantSceneTests.All<BattleView>(scene).Single();
                var so = new SerializedObject(view);
                var buttons = new[] { "_menuButton", "_autoButton", "_speedButton" }
                    .Select(f => (Name: f, Rect: RestaurantSceneTests.InReferenceCanvas((RectTransform)((Button)so.FindProperty(f).objectReferenceValue).transform)))
                    .ToArray();
                var cellsProperty = so.FindProperty("_turnCells");
                Assert.AreEqual(10, cellsProperty.arraySize, "5 allies + 5 enemies");
                var cells = Enumerable.Range(0, 10)
                    .Select(i => (Name: $"cell {i}", Rect: RestaurantSceneTests.InReferenceCanvas((RectTransform)((Image)cellsProperty.GetArrayElementAtIndex(i).objectReferenceValue).transform)))
                    .ToArray();

                var canvas = new Rect(0, 0, 1920, 1080);
                var all = buttons.Concat(cells).ToArray();
                foreach (var (name, rect) in all)
                {
                    Assert.IsTrue(canvas.Contains(rect.min) && canvas.Contains(rect.max), $"{name} {rect} leaves the canvas");
                    // Gốc dưới trái: thanh trên cùng phải nằm trên quái (đỉnh quái thường ở y 215, boss 180 từ trên xuống).
                    Assert.LessOrEqual(1080f - rect.yMin, 180f, $"{name} reaches into the units");
                }

                foreach (var (name, rect) in buttons)
                    Assert.GreaterOrEqual(Mathf.Min(rect.width, rect.height), 110f, $"{name} is too small to tap");

                for (var i = 0; i < all.Length; i++)
                for (var j = i + 1; j < all.Length; j++)
                {
                    var a = all[i].Rect;
                    var b = all[j].Rect;
                    var overlapX = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                    var overlapY = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                    Assert.IsFalse(overlapX > 1f && overlapY > 1f, $"{all[i].Name} overlaps {all[j].Name}");
                }
            });

        [Test]
        public void BattleScene_PausePopupStartsClosed_BlocksClicks() =>
            WithBattleScene(scene =>
            {
                var popup = RestaurantSceneTests.All<BattlePausePopup>(scene).Single();
                RestaurantSceneTests.AssertAllReferencesAssigned(popup);

                var so = new SerializedObject(popup);
                var root = (GameObject)so.FindProperty("_root").objectReferenceValue;
                var confirm = (GameObject)so.FindProperty("_confirmRoot").objectReferenceValue;
                Assert.IsFalse(root.activeSelf, "the popup starts closed");
                Assert.IsFalse(confirm.activeSelf, "the confirm box starts closed");
                Assert.IsTrue(root.GetComponent<Image>().raycastTarget, "must block clicks behind it");
                Assert.IsTrue(confirm.GetComponent<Image>().raycastTarget, "must block the pause buttons behind it");
                Assert.AreEqual(5, so.FindProperty("_hpLines").arraySize);
                Assert.IsTrue(confirm.transform.IsChildOf(root.transform), "confirm opens inside the popup");

                // Nhánh đang tắt nên phải tìm cả đối tượng tắt (L-013). Mọi nút của popup đủ lớn để chạm và nằm trong Canvas.
                var canvas = new Rect(0, 0, 1920, 1080);
                var buttons = root.GetComponentsInChildren<Button>(true);
                Assert.AreEqual(4, buttons.Length, "Resume, Surrender, Yes, No");
                foreach (var button in buttons)
                {
                    var rect = RestaurantSceneTests.InReferenceCanvas((RectTransform)button.transform);
                    Assert.GreaterOrEqual(Mathf.Min(rect.width, rect.height), 110f, $"{button.name} is too small to tap");
                    Assert.IsTrue(canvas.Contains(rect.min) && canvas.Contains(rect.max), $"{button.name} {rect} leaves the canvas");
                }
            });
    }
}
