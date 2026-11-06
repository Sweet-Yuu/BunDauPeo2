using System.Linq;
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
    public class StageSelectSceneTests
    {
        private static void WithStageSelect(System.Action<UnityEngine.SceneManagement.Scene> check) =>
            RestaurantSceneTests.WithScene(ProjectSetup.StageSelectScenePath, check);

        private static GameObject RootOf(Component popup) =>
            (GameObject)new SerializedObject(popup).FindProperty("_root").objectReferenceValue;

        [Test]
        public void StageSelectScene_HasCameraAndEventSystemWithInputSystemModule() =>
            WithStageSelect(scene =>
            {
                Assert.AreEqual(1, RestaurantSceneTests.All<Camera>(scene).Length);
                var systems = RestaurantSceneTests.All<EventSystem>(scene);
                Assert.AreEqual(1, systems.Length);
                Assert.IsNotNull(systems[0].GetComponent<InputSystemUIInputModule>());
                Assert.IsNull(systems[0].GetComponent<StandaloneInputModule>());
            });

        [Test]
        public void StageSelectScene_ViewAndPopupsWired_NoRawImage() =>
            WithStageSelect(scene =>
            {
                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<StageMapView>(scene).Single());
                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<StageInfoPopup>(scene).Single());
                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<StoryPlaceholderPopup>(scene).Single());
                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<StageNodeView>(scene).Single());

                Assert.AreEqual(0, RestaurantSceneTests.All<RawImage>(scene).Length);
                var canvas = RestaurantSceneTests.All<CanvasScaler>(scene).Single();
                Assert.AreEqual(1f, canvas.matchWidthOrHeight);
                Assert.AreEqual(new Vector2(1920, 1080), canvas.referenceResolution);
            });

        // S1: hai cột. Trái: tên, quái, thưởng; phải dưới: thể lực, cảnh báo và hai nút cạnh nhau.
        [Test]
        public void StageSelectScene_InfoPopup_TwoColumns_InsideBox() =>
            WithStageSelect(scene =>
            {
                var popup = RestaurantSceneTests.All<StageInfoPopup>(scene).Single();
                var so = new SerializedObject(popup);
                var box = (RectTransform)RootOf(popup).transform.Find("InfoBox");
                Assert.IsNotNull(box);
                var boxRect = RestaurantSceneTests.InReferenceCanvas(box);
                Assert.AreEqual(new Vector2(1600, 820), boxRect.size);

                var row = (RewardRow)so.FindProperty("_rewardRow").objectReferenceValue;
                Assert.IsNotNull(row);
                var members = box.GetComponentsInChildren<Text>(true).Select(t => (Component)t)
                    .Concat(box.GetComponentsInChildren<Button>(true))
                    .Append(row)
                    .Concat(ReadArray<Image>(so.FindProperty("_cells")).Select(c => (Component)c))
                    .ToArray();
                foreach (var member in members)
                {
                    var rect = RestaurantSceneTests.InReferenceCanvas((RectTransform)member.transform);
                    Assert.IsTrue(rect.xMin >= boxRect.xMin - 1f && rect.xMax <= boxRect.xMax + 1f
                                  && rect.yMin >= boxRect.yMin - 1f && rect.yMax <= boxRect.yMax + 1f, $"{member.name} {rect} leaves the box {boxRect}");
                }

                var third = boxRect.width / 3f;
                var twoThirds = boxRect.xMin + third * 2f;
                var title = (Text)so.FindProperty("_title").objectReferenceValue;
                Assert.AreEqual(TextAnchor.MiddleLeft, title.alignment, "the stage name is left-aligned");
                Assert.Less(RestaurantSceneTests.InReferenceCanvas(title.rectTransform).center.x, boxRect.xMin + third, "Title sits in the left third");

                foreach (var image in ReadArray<Image>(so.FindProperty("_cells")))
                    Assert.LessOrEqual(RestaurantSceneTests.InReferenceCanvas(image.rectTransform).xMax, twoThirds, $"{image.name} enemy cell crosses into the right third");
                Assert.LessOrEqual(RestaurantSceneTests.InReferenceCanvas((RectTransform)row.transform).xMax, twoThirds, "RewardRow stays in the left two thirds");

                var rightThird = boxRect.xMin + third * 2f;
                foreach (var name in new[] { "_staminaText", "_warningText", "_messageText" })
                {
                    var text = (Text)so.FindProperty(name).objectReferenceValue;
                    Assert.GreaterOrEqual(RestaurantSceneTests.InReferenceCanvas(text.rectTransform).xMin, rightThird - 1f, $"{name} sits in the right third");
                }

                var cancel = RestaurantSceneTests.InReferenceCanvas((RectTransform)((Button)so.FindProperty("_cancelButton").objectReferenceValue).transform);
                var challenge = RestaurantSceneTests.InReferenceCanvas((RectTransform)((Button)so.FindProperty("_challengeButton").objectReferenceValue).transform);
                Assert.GreaterOrEqual(cancel.xMin, rightThird - 1f, "Cancel sits in the right third");
                Assert.GreaterOrEqual(challenge.xMin, rightThird - 1f, "Challenge sits in the right third");
                Assert.IsFalse(cancel.Overlaps(challenge), "Cancel and Challenge overlap");
                Assert.AreEqual(cancel.center.y, challenge.center.y, 5f, "the two buttons share a row");
                Assert.Less(cancel.center.y, boxRect.center.y, "the buttons sit at the bottom");
            });

        private static T[] ReadArray<T>(SerializedProperty array) where T : Object
        {
            var items = new T[array.arraySize];
            for (var i = 0; i < items.Length; i++) items[i] = (T)array.GetArrayElementAtIndex(i).objectReferenceValue;
            return items;
        }

        [Test]
        public void StageSelectScene_PopupsStartClosed_BlockClicks() =>
            WithStageSelect(scene =>
            {
                foreach (Component popup in new Component[]
                         {
                             RestaurantSceneTests.All<StageInfoPopup>(scene).Single(),
                             RestaurantSceneTests.All<StoryPlaceholderPopup>(scene).Single(),
                         })
                {
                    var root = RootOf(popup);
                    Assert.IsFalse(root.activeSelf, $"{popup.GetType().Name} starts open");
                    Assert.IsTrue(root.GetComponent<Image>().raycastTarget, $"{popup.GetType().Name} must block clicks behind it");
                    Assert.IsTrue(popup.gameObject.activeSelf, $"{popup.GetType().Name} component must stay on a live object to receive Open");
                }
            });

        [Test]
        public void TeamSelectPopupWired_StartsClosed_BlocksClicks() =>
            WithStageSelect(scene =>
            {
                var popup = RestaurantSceneTests.All<TeamSelectPopup>(scene).Single();
                RestaurantSceneTests.AssertAllReferencesAssigned(popup);
                var root = RootOf(popup);
                Assert.IsFalse(root.activeSelf);
                Assert.IsTrue(root.GetComponent<Image>().raycastTarget, "must block clicks behind it");
                Assert.IsTrue(popup.gameObject.activeSelf);
                Assert.AreSame(popup, new SerializedObject(RestaurantSceneTests.All<StageMapView>(scene).Single()).FindProperty("_team").objectReferenceValue);
            });

        private static Rect RectOf(Component part) => RestaurantSceneTests.InReferenceCanvas((RectTransform)part.transform);

        // T1: 16 thẻ 8×2 không đè, 5 chip cùng hàng, Hủy / Vào trận cùng hàng chip và bên phải chip cuối.
        [Test]
        public void StageSelectScene_TeamPopup_SixteenCards_NoOverlap_ButtonsBesideChips() =>
            WithStageSelect(scene =>
            {
                var popup = RestaurantSceneTests.All<TeamSelectPopup>(scene).Single();
                var so = new SerializedObject(popup);
                var box = (RectTransform)RootOf(popup).transform.Find("TeamBox");
                var boxRect = RestaurantSceneTests.InReferenceCanvas(box);
                var cards = ReadArray<TeamCardView>(so.FindProperty("_cards"));
                Assert.AreEqual(16, cards.Length);
                Assert.AreEqual(16, TeamSelectPopup.CardsPerPage);

                var rects = cards.Select(RectOf).ToArray();
                for (var i = 0; i < rects.Length; i++)
                {
                    Assert.AreEqual(new Vector2(150, 190), rects[i].size, $"TeamCard{i}");
                    Assert.AreEqual($"TeamCard{i}", cards[i].name);
                    Assert.IsTrue(rects[i].xMin >= boxRect.xMin && rects[i].xMax <= boxRect.xMax && rects[i].yMin >= boxRect.yMin && rects[i].yMax <= boxRect.yMax, $"TeamCard{i} leaves the box");
                    Assert.AreEqual(rects[i % 8].center.x, rects[i].center.x, 1f, $"TeamCard{i} column");
                    Assert.AreEqual(rects[i / 8 * 8].center.y, rects[i].center.y, 1f, $"TeamCard{i} row");
                    for (var j = i + 1; j < rects.Length; j++) Assert.IsFalse(rects[i].Overlaps(rects[j]), $"TeamCard{i} overlaps TeamCard{j}");
                }

                Assert.Greater(rects[0].center.y, rects[8].center.y, "two rows");
                Assert.Less(rects[0].center.x, rects[7].center.x, "eight columns");

                var chips = ReadArray<Image>(so.FindProperty("_chips")).Select(RectOf).ToArray();
                var cancel = RectOf(ReadButton(so, "_cancelButton"));
                var start = RectOf(ReadButton(so, "_startButton"));
                var lowest = rects.Min(r => r.yMin);
                for (var i = 0; i < chips.Length; i++)
                {
                    Assert.AreEqual(new Vector2(150, 120), chips[i].size, $"Chip{i}");
                    Assert.AreEqual(chips[0].center.y, chips[i].center.y, 1f, $"Chip{i} shares the row");
                    Assert.Less(chips[i].yMax, lowest, $"Chip{i} sits under the card grid");
                    for (var j = i + 1; j < chips.Length; j++) Assert.IsFalse(chips[i].Overlaps(chips[j]), $"Chip{i} overlaps Chip{j}");
                    Assert.IsFalse(chips[i].Overlaps(cancel) || chips[i].Overlaps(start), $"Chip{i} overlaps a button");
                }

                var lastChip = chips.Max(c => c.xMax);
                Assert.GreaterOrEqual(cancel.xMin, lastChip, "Cancel sits right of the last chip");
                Assert.GreaterOrEqual(start.xMin, cancel.xMax, "Start sits right of Cancel");
                Assert.LessOrEqual(start.xMax, boxRect.xMax, "Start stays in the box");
                Assert.IsFalse(cancel.Overlaps(start));
                Assert.AreEqual(chips[0].center.y, cancel.center.y, 5f, "Cancel shares the chip row");
                Assert.AreEqual(chips[0].center.y, start.center.y, 5f, "Start shares the chip row");
            });

        private static Button ReadButton(SerializedObject so, string field) => (Button)so.FindProperty(field).objectReferenceValue;

        // Review Focus 5: chữ nào cũng nằm trong ô của nó và tự thu nhỏ khi tên dài.
        [Test]
        public void TeamSelect_CardTexts_FitInsideCard() =>
            WithStageSelect(scene =>
            {
                var so = new SerializedObject(RestaurantSceneTests.All<TeamSelectPopup>(scene).Single());
                var holders = ReadArray<TeamCardView>(so.FindProperty("_cards")).Select(c => (Component)c)
                    .Concat(ReadArray<Image>(so.FindProperty("_chips"))).ToArray();
                foreach (var holder in holders)
                {
                    var holderRect = RectOf(holder);
                    var texts = holder.GetComponentsInChildren<Text>(true);
                    Assert.IsNotEmpty(texts, holder.name);
                    foreach (var text in texts)
                    {
                        var rect = RectOf(text);
                        Assert.IsTrue(rect.xMin >= holderRect.xMin - 1f && rect.xMax <= holderRect.xMax + 1f
                                      && rect.yMin >= holderRect.yMin - 1f && rect.yMax <= holderRect.yMax + 1f, $"{holder.name}/{text.name} {rect} leaves {holderRect}");
                        Assert.IsTrue(text.resizeTextForBestFit, $"{holder.name}/{text.name} must shrink long names");
                        Assert.GreaterOrEqual(text.resizeTextMinSize, 16, $"{holder.name}/{text.name}");
                        Assert.AreEqual(HorizontalWrapMode.Wrap, text.horizontalOverflow, $"{holder.name}/{text.name}");
                    }
                }
            });

        // Review Focus 3: người thứ 17 là thẻ đầu của trang 2 và nút > bật; thẻ thừa tắt.
        [Test]
        public void TeamSelect_SeventeenCharacters_SecondPageShowsSeventeenth() =>
            WithStageSelect(scene =>
            {
                var heroes = Enumerable.Range(0, 17).Select(i => CombatTestData.Hero($"h{i:00}")).ToArray();
                var stage = CombatTestData.Stage("s1", new[] { CombatTestData.Enemy() });
                var rig = new CombatRig(new[] { stage }, heroes);
                var popup = RestaurantSceneTests.All<TeamSelectPopup>(scene).Single();
                var so = new SerializedObject(popup);
                var cards = ReadArray<TeamCardView>(so.FindProperty("_cards"));

                popup.Open(rig.Service, rig.Roster, stage);

                Assert.IsTrue(ReadButton(so, "_nextButton").interactable, "page 1 of 2 has a next page");
                Assert.IsFalse(ReadButton(so, "_prevButton").interactable);
                Assert.AreEqual("1/2", ((Text)so.FindProperty("_pageText").objectReferenceValue).text);
                Assert.AreEqual("h15", cards[15].Row.Character.Id, "page 1 ends with the 16th");

                // Awake/OnEnable không chạy ở EditMode nên onClick chưa nối: gọi thẳng hàm của nút >.
                typeof(TeamSelectPopup).GetMethod("NextPage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(popup, null);

                Assert.AreEqual("2/2", ((Text)so.FindProperty("_pageText").objectReferenceValue).text);
                Assert.IsTrue(cards[0].gameObject.activeSelf);
                Assert.AreEqual("h16", cards[0].Row.Character.Id, "the 17th character leads page 2");
                for (var i = 1; i < cards.Length; i++) Assert.IsFalse(cards[i].gameObject.activeSelf, $"TeamCard{i} is spare on page 2");
                Assert.IsFalse(ReadButton(so, "_nextButton").interactable);
                Assert.IsTrue(ReadButton(so, "_prevButton").interactable);
                popup.Close();
            });

        [Test]
        public void TeamSelect_ConfirmBoxesStartClosed() =>
            WithStageSelect(scene =>
            {
                var so = new SerializedObject(RestaurantSceneTests.All<TeamSelectPopup>(scene).Single());
                foreach (var field in new[] { "_smallConfirmRoot", "_staffConfirmRoot" })
                {
                    var root = (GameObject)so.FindProperty(field).objectReferenceValue;
                    Assert.IsNotNull(root, field);
                    Assert.IsFalse(root.activeSelf, $"{field} starts open (the stamina must not be spent before a confirm)");
                    Assert.IsTrue(root.GetComponent<Image>().raycastTarget, $"{field} must block clicks behind it");
                }
            });

        [Test]
        public void NodeCenters_SevenNodes_InsideMapArea_NoOverlap()
        {
            const float size = 120f;
            var rects = Enumerable.Range(0, 7).Select(i => new Rect(StageMapView.NodeCenter(i) - Vector2.one * size / 2f, Vector2.one * size)).ToArray();

            foreach (var rect in rects)
            {
                Assert.GreaterOrEqual(rect.yMin, 180f, $"{rect} reaches the top bar");
                Assert.LessOrEqual(rect.yMax, 980f, $"{rect} reaches the DEV strip");
                Assert.GreaterOrEqual(rect.xMin, 0f, $"{rect}");
                Assert.LessOrEqual(rect.xMax, 1920f, $"{rect}");
            }

            for (var i = 0; i < rects.Length; i++)
            for (var j = i + 1; j < rects.Length; j++)
                Assert.IsFalse(rects[i].Overlaps(rects[j]), $"node {i} overlaps node {j}");

            Assert.AreEqual(new Vector2(240, 640), StageMapView.NodeCenter(0));
            Assert.AreEqual(new Vector2(480, 440), StageMapView.NodeCenter(1));
        }

        [Test]
        public void BuildSettings_Order_IsBootGachaRestaurantStageSelectBattle()
        {
            var paths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            CollectionAssert.AreEqual(
                new[] { ProjectSetup.BootScenePath, ProjectSetup.GachaScenePath, ProjectSetup.RestaurantScenePath,
                        ProjectSetup.StageSelectScenePath, ProjectSetup.BattleScenePath },
                paths.Take(5).ToArray());
        }
    }
}
