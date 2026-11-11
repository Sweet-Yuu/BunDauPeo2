using System.Linq;
using BunDauPeo.Core;
using BunDauPeo.EditorTools;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    public class StageSelectSceneTests
    {
        private static void WithStageSelect(System.Action<UnityEngine.SceneManagement.Scene> check) =>
            RestaurantSceneTests.WithScene(ProjectSetup.RestaurantScenePath, check);

        private static GameObject RootOf(Component popup) =>
            (GameObject)new SerializedObject(popup).FindProperty("_root").objectReferenceValue;

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

        // Đợt B: thẻ tiền của Bản đồ và khối Gem của Gacha nhường chỗ cho thanh tài nguyên chung; hai view vẫn đủ tham chiếu.
        [Test]
        public void OldCurrencyCards_Gone_PanelStillWired() =>
            WithStageSelect(scene =>
            {
                var stages = LobbySceneTests.In<Transform>(scene, LobbySceneTests.StagesPanel).Select(t => t.name).ToArray();
                foreach (var gone in new[] { "StaminaCard", "MaterialCard", "GemCard" })
                    CollectionAssert.DoesNotContain(stages, gone, "stage map panel still has " + gone);
                var gacha = LobbySceneTests.In<Transform>(scene, LobbySceneTests.GachaPanel).Select(t => t.name).ToArray();
                foreach (var gone in new[] { "GemCard", "GemText" })
                    CollectionAssert.DoesNotContain(gacha, gone, "Gacha panel still has " + gone);

                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<StageMapView>(scene).Single());
                RestaurantSceneTests.AssertAllReferencesAssigned(RestaurantSceneTests.All<GachaView>(scene).Single());
            });

        // S1 (trang kín màn): hai cột trong vùng trên vạch. Trái: tên, quái. Phải: thưởng, thể lực, cảnh báo, nút Thách đấu dưới-phải.
        // Nút Hủy là nút Quay lại ở góc trên-trái.
        [Test]
        public void StageSelectScene_InfoPopup_TwoColumns_InsideBox() =>
            WithStageSelect(scene =>
            {
                var popup = RestaurantSceneTests.All<StageInfoPopup>(scene).Single();
                var so = new SerializedObject(popup);
                var box = (RectTransform)RootOf(popup).transform.Find("InfoBox");
                Assert.IsNotNull(box);
                var boxRect = RestaurantSceneTests.InReferenceCanvas(box);
                Assert.AreEqual(new Rect(0, HubNav.BarHeight, 1920, 1080 - HubNav.BarHeight), boxRect, "the page box covers the screen above the bar");

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

                var middle = boxRect.center.x;
                var title = (Text)so.FindProperty("_title").objectReferenceValue;
                Assert.AreEqual(TextAnchor.MiddleLeft, title.alignment, "the stage name is left-aligned");
                var titleRect = RestaurantSceneTests.InReferenceCanvas(title.rectTransform);
                Assert.Greater(titleRect.center.y, boxRect.center.y + boxRect.height / 4f, "the stage name sits at the top");

                foreach (var image in ReadArray<Image>(so.FindProperty("_cells")))
                    Assert.LessOrEqual(RestaurantSceneTests.InReferenceCanvas(image.rectTransform).xMax, middle, $"{image.name} enemy cell crosses into the right column");
                Assert.GreaterOrEqual(RestaurantSceneTests.InReferenceCanvas((RectTransform)row.transform).xMin, middle, "RewardRow sits in the right column");

                foreach (var name in new[] { "_staminaText", "_warningText", "_messageText" })
                {
                    var text = (Text)so.FindProperty(name).objectReferenceValue;
                    Assert.GreaterOrEqual(RestaurantSceneTests.InReferenceCanvas(text.rectTransform).xMin, middle, $"{name} sits in the right column");
                }

                var cancel = RestaurantSceneTests.InReferenceCanvas((RectTransform)((Button)so.FindProperty("_cancelButton").objectReferenceValue).transform);
                var challenge = RestaurantSceneTests.InReferenceCanvas((RectTransform)((Button)so.FindProperty("_challengeButton").objectReferenceValue).transform);
                Assert.Greater(challenge.xMin, middle, "Challenge sits in the right column");
                Assert.Less(challenge.center.y, boxRect.center.y, "Challenge sits at the bottom");
                Assert.Greater(challenge.center.x, boxRect.center.x + boxRect.width / 4f, "Challenge sits at the bottom right");
                Assert.IsFalse(cancel.Overlaps(challenge), "Cancel and Challenge overlap");
                Assert.IsFalse(cancel.Overlaps(titleRect), "the Back button overlaps the stage name");
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

        // T1 (trang kín màn): 16 thẻ 8×2 không đè, 5 chip cùng hàng, Vào trận cùng hàng chip và bên phải chip cuối; Hủy là nút Quay lại trên-trái.
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
                var start = RectOf(ReadButton(so, "_startButton"));
                var lowest = rects.Min(r => r.yMin);
                for (var i = 0; i < chips.Length; i++)
                {
                    Assert.AreEqual(new Vector2(150, 120), chips[i].size, $"Chip{i}");
                    Assert.AreEqual(chips[0].center.y, chips[i].center.y, 1f, $"Chip{i} shares the row");
                    Assert.Less(chips[i].yMax, lowest, $"Chip{i} sits under the card grid");
                    for (var j = i + 1; j < chips.Length; j++) Assert.IsFalse(chips[i].Overlaps(chips[j]), $"Chip{i} overlaps Chip{j}");
                    Assert.IsFalse(chips[i].Overlaps(start), $"Chip{i} overlaps Start");
                }

                var lastChip = chips.Max(c => c.xMax);
                Assert.GreaterOrEqual(start.xMin, lastChip, "Start sits right of the last chip");
                Assert.LessOrEqual(start.xMax, boxRect.xMax, "Start stays in the box");
                Assert.AreEqual(chips[0].center.y, start.center.y, 5f, "Start shares the chip row");
                Assert.AreEqual(new Rect(0, HubNav.BarHeight, 1920, 1080 - HubNav.BarHeight), boxRect, "the page box covers the screen above the bar");
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

        // Review Focus 2: Hủy (cũng là Close() của IHubPage) trả đội và vai ở quán về như lúc mở popup. AddToTeam gỡ người khỏi quán,
        // nên phải trả cả hai danh sách; chỉ trả đội thì người đó mất chỗ ở quán.
        [Test]
        public void TeamSelect_Cancel_RestoresTeamAndStaffAsOpened() =>
            WithStageSelect(scene =>
            {
                var heroes = Enumerable.Range(0, 4).Select(i => CombatTestData.Hero($"h{i:00}")).ToArray();
                var stage = CombatTestData.Stage("s1", new[] { CombatTestData.Enemy() });
                var rig = new CombatRig(new[] { stage }, heroes);
                rig.Roster.RemoveFromTeam("h02");
                rig.Roster.RemoveFromTeam("h03");
                Assert.IsTrue(rig.Roster.AssignStaff("h02", StaffRole.Cook));
                var teamBefore = rig.Roster.GetBattleTeam().Select(c => c.Id).ToArray();
                var staffBefore = rig.Roster.GetStaff(StaffRole.Cook).Select(c => c.Id).ToArray();
                var persistsBefore = rig.PersistCalls;
                var popup = RestaurantSceneTests.All<TeamSelectPopup>(scene).Single();
                popup.Open(rig.Service, rig.Roster, stage);

                // Giữa chừng: bỏ h00, thêm h03 và kéo h02 từ quán vào đội.
                Assert.IsTrue(rig.Roster.RemoveFromTeam("h00"));
                Assert.IsTrue(rig.Roster.AddToTeam("h03"));
                Assert.IsTrue(rig.Roster.AddToTeam("h02"));
                Assert.IsEmpty(rig.Roster.GetStaff(StaffRole.Cook), "h02 left the restaurant");

                popup.Cancel();

                Assert.IsFalse(popup.IsOpen);
                CollectionAssert.AreEqual(teamBefore, rig.Roster.GetBattleTeam().Select(c => c.Id).ToArray(), "team after Cancel");
                CollectionAssert.AreEqual(staffBefore, rig.Roster.GetStaff(StaffRole.Cook).Select(c => c.Id).ToArray(), "h02 must be back in the kitchen");
                // Autosave của quán có thể đã ghi đội chọn dở xuống đĩa lúc popup mở: Hủy lưu lại đội đã trả về (một lần).
                Assert.AreEqual(persistsBefore + 1, rig.PersistCalls, "Cancel saves the restored team once, never the half-made one");
            });

        // Bấm ô khác của thanh gọi Close() qua IHubPage: phải là đường Hủy chứ không chỉ ẩn popup.
        [Test]
        public void TeamSelect_Close_IsCancel() =>
            WithStageSelect(scene =>
            {
                var heroes = Enumerable.Range(0, 2).Select(i => CombatTestData.Hero($"h{i:00}")).ToArray();
                var stage = CombatTestData.Stage("s1", new[] { CombatTestData.Enemy() });
                var rig = new CombatRig(new[] { stage }, heroes);
                var popup = RestaurantSceneTests.All<TeamSelectPopup>(scene).Single();
                popup.Open(rig.Service, rig.Roster, stage);
                rig.Roster.RemoveFromTeam("h01");

                ((IHubPage)popup).Close();

                CollectionAssert.AreEqual(new[] { "h00", "h01" }, rig.Roster.GetBattleTeam().Select(c => c.Id).ToArray());
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

            Assert.AreEqual(new Vector2(240, 720), StageMapView.NodeCenter(0));
            Assert.AreEqual(new Vector2(480, 400), StageMapView.NodeCenter(1));
        }

        // M1: ổ khoá ở đỉnh giữa nút; ở mọi vị trí nút nó nằm trong màn, dưới thanh trên và không chạm nút khác.
        [Test]
        public void StageSelectScene_LockIconOnTopOfNode() =>
            WithStageSelect(scene =>
            {
                var node = (RectTransform)RestaurantSceneTests.All<StageNodeView>(scene).Single().transform;
                var lockIcon = (RectTransform)node.Find("LockIcon");
                var nodeRect = RestaurantSceneTests.InReferenceCanvas(node);
                var lockRect = RestaurantSceneTests.InReferenceCanvas(lockIcon);
                Assert.Less((lockRect.center - (nodeRect.center + new Vector2(0, 60))).magnitude, 4f, "LockIcon sits 60 px above the node centre");
                var body = lockIcon.Find("LockBody").GetComponent<Image>();
                Assert.AreEqual(new Color(1f, 0.85f, 0.4f), body.color, "lock body is Gold");

                // NodeCenter dùng gốc trái trên; đổi sang y hướng lên như InReferenceCanvas.
                var offset = lockRect.center - nodeRect.center;
                var nodes = Enumerable.Range(0, 7).Select(i => new Vector2(StageMapView.NodeCenter(i).x, 1080f - StageMapView.NodeCenter(i).y)).ToArray();
                for (var i = 0; i < nodes.Length; i++)
                {
                    var rect = new Rect(nodes[i] + offset - lockRect.size / 2f, lockRect.size);
                    Assert.IsTrue(new Rect(0, 0, 1920, 1080).Contains(rect.min) && rect.yMax <= 1080f - 180f, $"lock {i} {rect} leaves the map area");
                    for (var j = 0; j < nodes.Length; j++)
                    {
                        if (j == i) continue;
                        Assert.IsFalse(rect.Overlaps(new Rect(nodes[j] - nodeRect.size / 2f, nodeRect.size)), $"lock {i} overlaps node {j}");
                    }
                }
            });

        // B1: nhãn đợt do view giữ nên view phải tự ghi chữ lúc Open, không dựa vào chữ builder đã ghi sẵn (builder chạy ở tiếng Việt).
        [Test]
        public void InfoPopup_Open_WaveLabelsFollowLanguage() =>
            WithStageSelect(scene =>
            {
                var stage = CombatTestData.Stage("s1", new[] { CombatTestData.Enemy() }, new[] { CombatTestData.Enemy() });
                var rig = new CombatRig(new[] { stage });
                var popup = RestaurantSceneTests.All<StageInfoPopup>(scene).Single();
                var labels = ReadArray<Text>(new SerializedObject(popup).FindProperty("_waveLabels"));
                try
                {
                    GameLanguage.Current = Language.En;
                    popup.Open(rig.Service, stage);
                    Assert.AreEqual("Wave 1", labels[0].text);
                    Assert.AreEqual("Wave 2", labels[1].text);

                    GameLanguage.Current = Language.Vi;
                    popup.Open(rig.Service, stage);
                    Assert.AreEqual("Đợt 1", labels[0].text);
                    Assert.AreEqual("Đợt 2", labels[1].text);
                }
                finally
                {
                    GameLanguage.Current = Language.Vi;
                    popup.Close();
                }
            });

        [Test]
        public void StageSelectScene_LocalizedTexts_KeysExistAndNotWrittenByViews() =>
            WithStageSelect(SceneTextAsserts.AssertLocalizedTexts);
    }
}
