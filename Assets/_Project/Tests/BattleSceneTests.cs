using System.Collections.Generic;
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

        // Thẻ chỉ còn trong popup tạm dừng (user bỏ hàng thẻ dưới màn 2026-11-06).
        private static BattleCardView[] BattleCards(UnityEngine.SceneManagement.Scene scene) =>
            RestaurantSceneTests.All<BattleCardView>(scene).Where(c => c.GetComponentInParent<BattlePausePopup>(true) == null).ToArray();

        // Hộp của mọi người và quái ở mọi tổ hợp, kéo dài xuống 110 px cho tên, số HP và hàng buff (hệ gốc trái trên).
        private static IEnumerable<(string Name, Rect Rect)> UnitZones()
        {
            for (var slot = 0; slot < 5; slot++)
            {
                yield return ($"Ally{slot}", WithLabels(BattleLayout.AllyRect(slot)));
                foreach (var boss in new[] { false, true }) yield return ($"Enemy{slot}(boss {boss})", WithLabels(BattleLayout.EnemyRect(slot, boss)));
            }
        }

        private static Rect WithLabels(Rect r) => new(r.xMin, r.yMin, r.width, r.height + 110f);

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
        public void BattleScene_ActionPanelWired_NoCardRowUnderTheBattle() =>
            WithBattleScene(scene =>
            {
                var panels = RestaurantSceneTests.All<BattleActionPanel>(scene);
                Assert.AreEqual(1, panels.Length);
                RestaurantSceneTests.AssertAllReferencesAssigned(panels[0]);
                Assert.AreEqual(3, panels[0].GetComponentsInChildren<Button>(true).Length, "Basic, Skill, Ultimate");
                Assert.IsEmpty(BattleCards(scene), "the card row under the battle is gone; cards live only in the pause popup");
            });

        // Ba nút chiêu ở góc phải dưới, không đè ai (kể cả vùng chữ dưới mỗi người); dòng "Lượt của X" ở góc trên trái.
        [Test]
        public void BattleScene_ActionButtonsClearOfUnits_TurnLineTopLeft() =>
            WithBattleScene(scene =>
            {
                var panel = RestaurantSceneTests.All<BattleActionPanel>(scene).Single();
                var canvas = new Rect(0, 0, 1920, 1080);
                var buttons = panel.GetComponentsInChildren<Button>(true)
                    .Select(b => (Name: b.name, Rect: RestaurantSceneTests.InReferenceCanvas((RectTransform)b.transform)))
                    .ToArray();
                Assert.AreEqual(3, buttons.Length);

                foreach (var (name, rect) in buttons)
                {
                    Assert.IsTrue(canvas.Contains(rect.min) && canvas.Contains(rect.max), $"{name} {rect} leaves the canvas");
                    Assert.GreaterOrEqual(Mathf.Min(rect.width, rect.height), 110f, $"{name} is too small to tap");
                    Assert.GreaterOrEqual(rect.xMin, 1400f, $"{name} is not in the bottom-right corner");
                    Assert.LessOrEqual(rect.yMax, 400f, $"{name} is not at the bottom");
                    var top = new Rect(rect.xMin, 1080f - rect.yMax, rect.width, rect.height);
                    foreach (var (zone, unit) in UnitZones()) Assert.IsFalse(Overlap(top, unit), $"{name} overlaps {zone}");
                }

                for (var i = 0; i < buttons.Length; i++)
                for (var j = i + 1; j < buttons.Length; j++) Assert.IsFalse(Overlap(buttons[i].Rect, buttons[j].Rect), $"{buttons[i].Name} overlaps {buttons[j].Name}");

                var turnLine = (Text)new SerializedObject(panel).FindProperty("_turnLine").objectReferenceValue;
                var turnRect = RestaurantSceneTests.InReferenceCanvas(turnLine.rectTransform);
                Assert.LessOrEqual(1080f - turnRect.yMin, 200f, "TurnLine reaches into the units");
                Assert.Less(turnRect.xMax, 700f, "TurnLine is not in the top-left corner");
                Assert.AreEqual(TextAnchor.MiddleLeft, turnLine.alignment);
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

        // B1 (user chốt 2026-11-06): Đợt và thanh lượt ở góc trên trái; góc trên phải một hàng x1, AUTO, Menu.
        [Test]
        public void BattleScene_TopBarWired_InsideCanvas() =>
            WithBattleScene(scene =>
            {
                var view = RestaurantSceneTests.All<BattleView>(scene).Single();
                var so = new SerializedObject(view);
                // Đổi sang hệ gốc trái trên như BattleLayout.
                static Rect TopBased(Rect r) => new(r.xMin, 1080f - r.yMax, r.width, r.height);
                Rect Of(Component c) => TopBased(RestaurantSceneTests.InReferenceCanvas((RectTransform)c.transform));

                var waveText = (Text)so.FindProperty("_waveText").objectReferenceValue;
                var menu = Of((Button)so.FindProperty("_menuButton").objectReferenceValue);
                var auto = Of((Button)so.FindProperty("_autoButton").objectReferenceValue);
                var speed = Of((Button)so.FindProperty("_speedButton").objectReferenceValue);
                var cellsProperty = so.FindProperty("_turnCells");
                Assert.AreEqual(10, cellsProperty.arraySize, "5 allies + 5 enemies");
                var cells = Enumerable.Range(0, 10)
                    .Select(i => (Name: $"cell {i}", Rect: Of((Image)cellsProperty.GetArrayElementAtIndex(i).objectReferenceValue)))
                    .ToArray();
                var named = cells.Concat(new[] { (Name: "WaveText", Rect: Of(waveText)), (Name: "MenuButton", Rect: menu), (Name: "AutoButton", Rect: auto), (Name: "SpeedButton", Rect: speed) }).ToArray();

                var canvas = new Rect(0, 0, 1920, 1080);
                foreach (var (name, rect) in named)
                {
                    Assert.IsTrue(canvas.Contains(rect.min) && canvas.Contains(rect.max - Vector2.one), $"{name} {rect} leaves the canvas");
                    Assert.LessOrEqual(rect.yMax, 180f, $"{name} reaches into the units");
                }

                // Góc trên trái: chữ Đợt và mọi ô lượt.
                foreach (var (name, rect) in cells.Append(("WaveText", Of(waveText))))
                    Assert.Less(rect.xMax, 700f, $"{name} is not in the top-left corner");

                // Góc trên phải: một hàng x1, AUTO, Menu (Menu ngoài cùng bên phải), cùng độ cao.
                foreach (var (name, rect) in new[] { ("MenuButton", menu), ("AutoButton", auto), ("SpeedButton", speed) })
                {
                    Assert.GreaterOrEqual(rect.xMin, 1300f, $"{name} is not in the top-right corner");
                    Assert.AreEqual(menu.center.y, rect.center.y, 1f, $"{name} shares the row with Menu");
                    Assert.GreaterOrEqual(Mathf.Min(rect.width, rect.height), 110f, $"{name} is too small to tap");
                }

                Assert.Less(speed.xMax, auto.xMin + 1f, "x1 sits left of AUTO");
                Assert.Less(auto.xMax, menu.xMin + 1f, "AUTO sits left of Menu");

                for (var i = 0; i < named.Length; i++)
                {
                    for (var j = i + 1; j < named.Length; j++) Assert.IsFalse(Overlap(named[i].Rect, named[j].Rect), $"{named[i].Name} overlaps {named[j].Name}");
                    foreach (var (zone, unit) in UnitZones()) Assert.IsFalse(Overlap(named[i].Rect, unit), $"{named[i].Name} overlaps {zone}");
                }
            });

        // Đè hơn 1 px cả hai chiều mới tính (khuôn BattleScene_CardsAndButtonsInCardArea).
        private static bool Overlap(Rect a, Rect b) =>
            Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > 1f && Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > 1f;

        [Test]
        public void BattleScene_PauseCards_InsideBox() =>
            WithBattleScene(scene =>
            {
                var popup = RestaurantSceneTests.All<BattlePausePopup>(scene).Single();
                var so = new SerializedObject(popup);
                var root = (GameObject)so.FindProperty("_root").objectReferenceValue;
                var box = RestaurantSceneTests.InReferenceCanvas((RectTransform)root.transform.Find("PauseBox"));
                Assert.AreEqual(new Vector2(1100, 760), box.size);

                var cards = Enumerable.Range(0, 5).Select(i => (BattleCardView)so.FindProperty("_cards").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                var rects = cards.Select((c, i) => (Name: $"PauseCard{i}", Rect: RestaurantSceneTests.InReferenceCanvas((RectTransform)c.transform))).ToArray();
                for (var i = 0; i < rects.Length; i++)
                {
                    Assert.AreEqual(rects[i].Name, cards[i].name);
                    Assert.IsTrue(cards[i].transform.IsChildOf(root.transform), $"{rects[i].Name} must sit inside the popup");
                    var rect = rects[i].Rect;
                    Assert.IsTrue(rect.xMin >= box.xMin && rect.xMax <= box.xMax && rect.yMin >= box.yMin && rect.yMax <= box.yMax, $"{rects[i].Name} leaves the box");
                    Assert.AreEqual(rects[0].Rect.center.y, rect.center.y, 1f, $"{rects[i].Name} shares the row");
                    for (var j = i + 1; j < rects.Length; j++) Assert.IsFalse(rect.Overlaps(rects[j].Rect), $"{rects[i].Name} overlaps {rects[j].Name}");
                }

                var stageName = (Text)so.FindProperty("_stageName").objectReferenceValue;
                Assert.IsNotNull(stageName, "the stage name is wired");
                var nameRect = RestaurantSceneTests.InReferenceCanvas(stageName.rectTransform);
                Assert.IsTrue(nameRect.yMin >= rects[0].Rect.yMax, "the stage name sits above the cards");
                Assert.IsTrue(nameRect.xMin >= box.xMin && nameRect.xMax <= box.xMax && nameRect.yMax <= box.yMax);
                Assert.IsFalse(root.activeSelf, "the popup starts closed");
            });

        [Test]
        public void BattlePausePopup_Open_ShowsStageNameAndLiveCards() =>
            WithBattleScene(scene =>
            {
                var popup = RestaurantSceneTests.All<BattlePausePopup>(scene).Single();
                var so = new SerializedObject(popup);
                var stage = CombatTestData.Stage("1-1", new[] { CombatTestData.Enemy("e", 100000, 160, 0, 500) });
                var session = CombatTestData.Battle(new[] { CombatTestData.Hero("Nem Chua", hp: 800, speed: 100) }, stage);
                session.EnemyAct();
                var cards = Enumerable.Range(0, 5).Select(i => (BattleCardView)so.FindProperty("_cards").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();

                popup.Open(session, 100, unit => unit.Hp);

                Assert.AreEqual("1-1", ((Text)so.FindProperty("_stageName").objectReferenceValue).text);
                Assert.IsTrue(cards[0].gameObject.activeSelf);
                for (var i = 1; i < cards.Length; i++) Assert.IsFalse(cards[i].gameObject.activeSelf, $"PauseCard{i} has nobody to show");
                Assert.AreEqual("640/800", cards[0].transform.Find("HpText").GetComponent<Text>().text);
                Assert.AreEqual("Nem Chua", cards[0].transform.Find("Name").GetComponent<Text>().text);
                Assert.IsFalse(cards[0].Button.interactable, "pause cards are read-only");
                popup.Close();
            });

        // Mở Menu giữa lúc đòn đánh còn chạy hiệu ứng: thẻ phải hiện máu đang hiển thị trên sân (ShownHp), không phải máu thật.
        [Test]
        public void BattlePausePopup_Open_CardsShowShownHpNotRealHp() =>
            WithBattleScene(scene =>
            {
                var popup = RestaurantSceneTests.All<BattlePausePopup>(scene).Single();
                var so = new SerializedObject(popup);
                var stage = CombatTestData.Stage("1-1", new[] { CombatTestData.Enemy("e", 100000, 160, 0, 500) });
                var session = CombatTestData.Battle(new[] { CombatTestData.Hero("Nem Chua", hp: 800, speed: 100) }, stage);
                session.EnemyAct();
                Assert.AreEqual(640, session.Allies[0].Hp, "real hp already dropped");
                var card = (BattleCardView)so.FindProperty("_cards").GetArrayElementAtIndex(0).objectReferenceValue;

                popup.Open(session, 100, unit => 800);

                Assert.AreEqual("800/800", card.transform.Find("HpText").GetComponent<Text>().text);
                popup.Close();
            });

        // Review Focus 2: UiDriver báo Ambiguous khi hai nút trùng tên (kể cả nút đang tắt).
        // B2 (user chốt 2026-11-06, bỏ hàng thẻ): số HP dưới tên ở cả hai phe, còn chỗ cho hàng buff bên dưới.
        [Test]
        public void BattleScene_EveryUnitHasHpTextUnderItsName() =>
            WithBattleScene(scene =>
            {
                var units = RestaurantSceneTests.All<BattleUnitView>(scene);
                Assert.AreEqual(10, units.Length);
                var canvas = new Rect(0, 0, 1920, 1080);
                foreach (var unit in units)
                {
                    var hp = (Text)new SerializedObject(unit).FindProperty("_hpText").objectReferenceValue;
                    Assert.IsNotNull(hp, $"{unit.name} needs HpText");
                    Assert.AreSame(unit.transform.Find("HpText").GetComponent<Text>(), hp);
                    var hpRect = RestaurantSceneTests.InReferenceCanvas(hp.rectTransform);
                    var nameRect = RestaurantSceneTests.InReferenceCanvas(unit.transform.Find("Name").GetComponent<RectTransform>());
                    var bodyRect = RestaurantSceneTests.InReferenceCanvas((RectTransform)unit.transform);
                    Assert.Less(hpRect.center.y, nameRect.center.y, $"{unit.name}: HpText sits under Name");
                    Assert.Less(hpRect.yMax, bodyRect.yMin, $"{unit.name}: HpText sits under the body");
                    Assert.IsTrue(canvas.Contains(hpRect.min) && canvas.Contains(hpRect.max), $"{unit.name} HpText {hpRect} leaves the canvas");
                    Assert.LessOrEqual(bodyRect.yMin - hpRect.yMin, 80f, $"{unit.name}: HpText hangs too far below the body");
                }
            });

        [Test]
        public void BattleUnitView_HpText_FollowsDelta() =>
            WithBattleScene(scene =>
            {
                var view = RestaurantSceneTests.All<BattleUnitView>(scene).Single(v => v.name == "Enemy0");
                var hp = (Text)new SerializedObject(view).FindProperty("_hpText").objectReferenceValue;
                var unit = BattleUnit.ForEnemy(CombatTestData.Enemy("e", hp: 1000), 0);

                view.Bind(unit, BattleLayout.EnemyRect(0, false));
                Assert.AreEqual(CombatViewText.UnitHp(1000, 1000), hp.text);

                view.ApplyDelta(-200);
                for (var i = 0; i < 20; i++) view.Tick(0.1f);
                Assert.AreEqual(CombatViewText.UnitHp(800, 1000), hp.text);
                Assert.AreEqual("800/1000", hp.text);

                view.ApplyDelta(+100);
                Assert.AreEqual("900/1000", hp.text, "healing updates the text too");
                view.ApplyDelta(-5000);
                Assert.AreEqual("0/1000", hp.text, "the text clamps at 0 like the bar");
            });

        [Test]
        public void BattleScene_ButtonNamesUnique() =>
            WithBattleScene(scene =>
            {
                var buttons = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Button>(true)).ToArray();
                Assert.Greater(buttons.Length, 20);
                var duplicate = buttons.GroupBy(b => b.name).FirstOrDefault(g => g.Count() > 1);
                Assert.IsNull(duplicate, duplicate == null ? "" : $"{duplicate.Count()} buttons are named {duplicate.Key}");
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
                Assert.AreEqual(5, so.FindProperty("_cards").arraySize);
                Assert.IsTrue(confirm.transform.IsChildOf(root.transform), "confirm opens inside the popup");

                // Nhánh đang tắt nên phải tìm cả đối tượng tắt (L-013). Mọi nút của popup đủ lớn để chạm và nằm trong Canvas.
                var canvas = new Rect(0, 0, 1920, 1080);
                // Năm thẻ chỉ xem (PauseCard{i}) cũng là Button nhưng không phải nút điều khiển.
                var buttons = root.GetComponentsInChildren<Button>(true).Where(b => b.GetComponent<BattleCardView>() == null).ToArray();
                Assert.AreEqual(5, buttons.Length, "Resume, Settings, Surrender, Yes, No");
                foreach (var button in buttons)
                {
                    var rect = RestaurantSceneTests.InReferenceCanvas((RectTransform)button.transform);
                    Assert.GreaterOrEqual(Mathf.Min(rect.width, rect.height), 110f, $"{button.name} is too small to tap");
                    Assert.IsTrue(canvas.Contains(rect.min) && canvas.Contains(rect.max), $"{button.name} {rect} leaves the canvas");
                }
            });

        [Test]
        public void BattleScene_LocalizedTexts_KeysExistAndNotWrittenByViews() =>
            WithBattleScene(SceneTextAsserts.AssertLocalizedTexts);
    }
}
