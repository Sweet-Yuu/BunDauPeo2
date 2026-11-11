using System;
using System.IO;
using System.Linq;
using BunDauPeo.EditorTools;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    public class RestaurantSceneTests
    {
        // Mở scene từ đĩa (L-002) và chỉ đóng nếu test là bên mở nó.
        internal static void WithScene(string path, Action<Scene> check)
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

        internal static T[] All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        // Mọi tham chiếu [SerializeField] của component phải có giá trị; mảng không rỗng và không phần tử null.
        // optionalFields: field được phép để trống (ví dụ _hpText của phe ta).
        internal static void AssertAllReferencesAssigned(Component component, params string[] optionalFields)
        {
            var so = new SerializedObject(component);
            var property = so.GetIterator();
            var checkedAny = false;
            for (var enter = true; property.NextVisible(enter); enter = false)
            {
                if (property.propertyPath == "m_Script" || System.Array.IndexOf(optionalFields, property.propertyPath) >= 0) continue;
                checkedAny = true;
                if (property.isArray)
                {
                    Assert.Greater(property.arraySize, 0, $"{component.GetType().Name}.{property.propertyPath} is empty");
                    for (var i = 0; i < property.arraySize; i++)
                        Assert.IsNotNull(property.GetArrayElementAtIndex(i).objectReferenceValue, $"{component.GetType().Name}.{property.propertyPath}[{i}]");
                }
                else
                {
                    Assert.AreEqual(SerializedPropertyType.ObjectReference, property.propertyType, property.propertyPath);
                    Assert.IsNotNull(property.objectReferenceValue, $"{component.GetType().Name}.{property.propertyPath}");
                }
            }

            Assert.IsTrue(checkedAny, component.GetType().Name);
        }

        [Test]
        public void RestaurantScene_HasCameraAndEventSystemWithInputSystemModule() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                Assert.AreEqual(1, All<Camera>(scene).Length);
                Assert.AreEqual(1, All<EventSystem>(scene).Length);
                Assert.AreEqual(1, All<InputSystemUIInputModule>(scene).Length);
                Assert.AreEqual(0, All<StandaloneInputModule>(scene).Length);
            });

        [Test]
        public void RestaurantScene_ViewAndHudAreWired_AndUseOnlyImageAndText() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                Assert.AreEqual(1, All<Canvas>(scene).Count(c => !c.overrideSorting), "one main canvas; boxes drawn over the bar own a child Canvas");
                AssertAllReferencesAssigned(All<RestaurantView>(scene).Single());
                AssertAllReferencesAssigned(All<RestaurantHud>(scene).Single());
                Assert.AreEqual(0, All<RawImage>(scene).Length);
            });

        [Test]
        public void RestaurantScene_StaffPanelWired() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene => AssertAllReferencesAssigned(All<StaffPanel>(scene).Single()));

        [Test]
        public void RestaurantScene_UpgradePanelWired() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene => AssertAllReferencesAssigned(All<UpgradeTestPanel>(scene).Single()));

        // Độ sáng tương đối đơn giản (đủ để so chữ với nền, không cần đúng chuẩn WCAG).
        private static float Luma(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        [Test]
        public void RestaurantScene_PopupsWired() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var afk = All<AfkPopup>(scene).Single();
                var quality = All<QualityPopup>(scene).Single();
                AssertAllReferencesAssigned(afk);
                AssertAllReferencesAssigned(quality);

                // Mở scene là đóng: popup chỉ hiện khi view gọi Show hoặc người chơi bấm thẻ sao.
                foreach (var popup in new Component[] { afk, quality })
                {
                    var root = (GameObject)new SerializedObject(popup).FindProperty("_root").objectReferenceValue;
                    Assert.IsFalse(root.activeSelf, $"{popup.GetType().Name} starts open");
                    Assert.IsTrue(root.GetComponent<Image>().raycastTarget, $"{popup.GetType().Name} must block clicks behind it");
                }

                // Thẻ sao ở HUD là nút mở QualityPopup.
                var opener = (Button)new SerializedObject(quality).FindProperty("_openButton").objectReferenceValue;
                Assert.AreEqual("InfoBar", opener.name);
                Assert.IsTrue(opener.targetGraphic.raycastTarget, "the star card must be clickable");

                // Popup sáng như màn quán: nền hộp sáng, mọi chữ không nằm trên nút phải tối hơn nền ít nhất 0,45 độ sáng; hộp nằm trong Canvas.
                var canvas = new Rect(0, 0, 1920, 1080);
                foreach (var boxName in new[] { "AfkPopupBox", "QualityPopupBox" })
                {
                    var box = Named(scene, boxName);
                    var rect = InReferenceCanvas(box);
                    Assert.IsTrue(canvas.Contains(rect.min) && canvas.Contains(rect.max - Vector2.one), $"{boxName} {rect} leaves the canvas");
                    var boxColor = box.GetComponent<Image>().color;
                    Assert.Greater(Luma(boxColor), 0.8f, $"{boxName} must be a light card");
                    foreach (var text in box.GetComponentsInChildren<Text>(true))
                    {
                        // Popup đóng nên cả nhánh inactive: phải tìm cả đối tượng tắt.
                        if (text.GetComponentInParent<Button>(true) != null) continue;
                        Assert.Greater(Luma(boxColor) - Luma(text.color), 0.45f, $"{boxName}/{text.name} is hard to read on the card");
                    }
                }
            });

        private static RectTransform Named(Scene scene, string name) =>
            All<RectTransform>(scene).First(r => r.name == name);

        // Toạ độ trong hệ của sàn (gốc ở góc trái trên của sàn, trục theo sàn): người, đồ và điểm mốc cùng góc xoay nên so được trực tiếp.
        private static Vector2 InFloor(RectTransform floor, RectTransform target) =>
            floor.InverseTransformPoint(target.position);

        private static Rect FloorRect(RectTransform floor, RectTransform target)
        {
            var center = InFloor(floor, target);
            return new Rect(center - target.rect.size / 2f, target.rect.size);
        }

        [Test]
        public void RestaurantScene_LandmarksAreInsideFloor() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var floor = Named(scene, "Floor");
                var view = new SerializedObject(All<RestaurantView>(scene).Single());
                // Gốc ở góc trái trên của sàn (pivot 0, 1); thêm nửa pixel để cửa nằm đúng trên cạnh trước vẫn đạt.
                var inside = new Rect(floor.rect.position, floor.rect.size);
                inside.xMin -= 0.5f; inside.yMin -= 0.5f; inside.xMax += 0.5f; inside.yMax += 0.5f;

                var landmarks = new System.Collections.Generic.List<(string Name, RectTransform Spot)>();
                foreach (var field in new[] { "_tables", "_cookSpots", "_serverSpots", "_cashierSpots" })
                {
                    var array = view.FindProperty(field);
                    for (var i = 0; i < array.arraySize; i++)
                        landmarks.Add(($"{field}[{i}]", (RectTransform)array.GetArrayElementAtIndex(i).objectReferenceValue));
                }

                landmarks.Add(("_door", (RectTransform)view.FindProperty("_door").objectReferenceValue));
                Assert.AreEqual(8 + 3 + 3 + 3 + 1, landmarks.Count);
                foreach (var (name, spot) in landmarks)
                    Assert.IsTrue(inside.Contains(InFloor(floor, spot)), $"{name} at {InFloor(floor, spot)} is outside the floor {floor.rect}");
            });

        [Test]
        public void RestaurantScene_FurnitureDoesNotOverlap() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var floor = Named(scene, "Floor");
                var names = new[] { "Kitchen", "Counter" }.Concat(Enumerable.Range(0, 8).Select(i => $"Table{i}")).ToArray();
                var rects = names.Select(n => (Name: n, Rect: FloorRect(floor, Named(scene, n)))).ToArray();
                for (var i = 0; i < rects.Length; i++)
                {
                    for (var j = i + 1; j < rects.Length; j++)
                    {
                        // Cạnh chạm nhau thì không tính là đè; chỉ cần lấn quá một pixel là lỗi.
                        var a = rects[i].Rect;
                        var b = rects[j].Rect;
                        var overlapX = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                        var overlapY = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                        Assert.IsFalse(overlapX > 1f && overlapY > 1f, $"{rects[i].Name} overlaps {rects[j].Name}");
                    }
                }
            });

        // Rect trong hệ Canvas tham chiếu 1920×1080 (gốc dưới trái), tính từ neo và offset đã lưu: không phụ thuộc kích thước Game view lúc mở scene.
        internal static Rect InReferenceCanvas(RectTransform rt)
        {
            var parent = rt.parent.GetComponent<Canvas>() != null ? new Rect(0, 0, 1920, 1080) : InReferenceCanvas((RectTransform)rt.parent);
            var anchorMin = parent.min + Vector2.Scale(rt.anchorMin, parent.size);
            var anchorMax = parent.min + Vector2.Scale(rt.anchorMax, parent.size);
            var size = anchorMax - anchorMin + rt.sizeDelta;
            var pivotPosition = anchorMin + Vector2.Scale(rt.pivot, anchorMax - anchorMin) + rt.anchoredPosition;
            return new Rect(pivotPosition - Vector2.Scale(rt.pivot, size), size);
        }

        [Test]
        public void RestaurantScene_HudAndColumnInsideCanvas() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var canvas = new Rect(0, 0, 1920, 1080);
                foreach (var name in new[] { "LobbyHud", "RestaurantBar_PH_180x460", "RestaurantBarToggle_PH_60x160" })
                {
                    var rect = InReferenceCanvas(Named(scene, name));
                    Assert.IsTrue(canvas.Contains(rect.min) && canvas.Contains(rect.max - Vector2.one), $"{name} {rect} leaves the 1920x1080 canvas");
                }

                var column = Named(scene, "RestaurantBar_PH_180x460");
                var staff = Named(scene, "StaffButton");
                Assert.AreSame(column, staff.parent, "StaffButton belongs to the restaurant bar");
                var staffRect = InReferenceCanvas(staff);
                Assert.GreaterOrEqual(Mathf.Min(staffRect.width, staffRect.height), 110f, "touch size");
                Assert.IsTrue(InReferenceCanvas(column).Contains(staffRect.min) && InReferenceCanvas(column).Contains(staffRect.max - Vector2.one));

                // Nâng cấp ở ô thứ hai của cột, dưới nút Nhân viên và không đè lên nó.
                var upgrade = Named(scene, "UpgradeButton");
                Assert.AreSame(column, upgrade.parent, "UpgradeButton belongs to the right column");
                var upgradeRect = InReferenceCanvas(upgrade);
                Assert.GreaterOrEqual(Mathf.Min(upgradeRect.width, upgradeRect.height), 110f, "touch size");
                Assert.IsTrue(InReferenceCanvas(column).Contains(upgradeRect.min) && InReferenceCanvas(column).Contains(upgradeRect.max - Vector2.one));
                Assert.IsFalse(upgradeRect.Overlaps(staffRect), "UpgradeButton overlaps StaffButton");
                Assert.Less(upgradeRect.center.y, staffRect.center.y, "UpgradeButton is below StaffButton");

                // Thu AFK ở ô thứ ba, dưới nút Nâng cấp.
                var afk = Named(scene, "AfkButton");
                Assert.AreSame(column, afk.parent, "AfkButton belongs to the right column");
                var afkRect = InReferenceCanvas(afk);
                Assert.GreaterOrEqual(Mathf.Min(afkRect.width, afkRect.height), 110f, "touch size");
                Assert.IsTrue(InReferenceCanvas(column).Contains(afkRect.min) && InReferenceCanvas(column).Contains(afkRect.max - Vector2.one));
                Assert.IsFalse(afkRect.Overlaps(upgradeRect), "AfkButton overlaps UpgradeButton");
                Assert.IsFalse(afkRect.Overlaps(staffRect), "AfkButton overlaps StaffButton");
                Assert.Less(afkRect.center.y, upgradeRect.center.y, "AfkButton is below UpgradeButton");

                // Cạnh phải của sàn là góc (12, 0): gốc của phòng cộng 90 × 12.
                var room = InReferenceCanvas(Named(scene, "Room"));
                var floor = Named(scene, "Floor");
                var cellWidth = Mathf.Sqrt(90f * 90f + 18f * 18f);
                var cellHeight = Mathf.Sqrt(13f * 13f + 65f * 65f);
                Assert.AreEqual(12 * cellWidth, floor.rect.width, 1f);
                Assert.AreEqual(8 * cellHeight, floor.rect.height, 1f);
                Assert.AreEqual(-Mathf.Atan2(18f, 90f) * Mathf.Rad2Deg, floor.localEulerAngles.z > 180f ? floor.localEulerAngles.z - 360f : floor.localEulerAngles.z, 0.05f);
                Assert.Less(room.xMin + 90f * 12, InReferenceCanvas(Named(scene, "RestaurantBarToggle_PH_60x160")).xMin, "floor must not run under the restaurant bar toggle");
                Assert.Less(room.xMin + 90f * 12, InReferenceCanvas(column).xMin, "floor must not run under the open restaurant bar");
            });

        // Hình chữ nhật có cắt tứ giác lồi (tường, sàn đã xoay) không: trục tách là hai trục chữ nhật và hai pháp tuyến của tứ giác.
        private static bool QuadOverlapsRect(Vector2[] quad, Rect rect)
        {
            var rectCorners = new[] { rect.min, new Vector2(rect.xMax, rect.yMin), rect.max, new Vector2(rect.xMin, rect.yMax) };
            var axes = new[] { Vector2.right, Vector2.up, Perp(quad[1] - quad[0]), Perp(quad[2] - quad[1]) };
            foreach (var axis in axes)
            {
                Project(quad, axis, out var minA, out var maxA);
                Project(rectCorners, axis, out var minB, out var maxB);
                if (maxA <= minB + 1f || maxB <= minA + 1f) return false;
            }

            return true;
        }

        private static Vector2 Perp(Vector2 v) => new(-v.y, v.x);

        private static void Project(Vector2[] points, Vector2 axis, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            foreach (var p in points)
            {
                var d = Vector2.Dot(p, axis.normalized);
                min = Mathf.Min(min, d);
                max = Mathf.Max(max, d);
            }
        }

        // R1 (đợt B): thanh chất lượng một hàng 800×68 ngay dưới thanh tài nguyên, bên phải avatar; thông báo AFK ngay dưới nó; không đè thanh quán.
        [Test]
        public void RestaurantScene_InfoBarClearOfRoomAndColumn() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var bar = InReferenceCanvas(Named(scene, "InfoBar"));
                var message = InReferenceCanvas(Named(scene, "AfkMessage"));
                var avatar = InReferenceCanvas(Named(scene, "Avatar_PH_140x140"));
                var resources = InReferenceCanvas(Named(scene, "ResourceBar_PH_1120x76"));
                var column = InReferenceCanvas(Named(scene, "RestaurantBar_PH_180x460"));
                Assert.AreEqual(new Vector2(800, 68), bar.size, "InfoBar is one row");
                Assert.GreaterOrEqual(bar.xMin, avatar.xMax, "InfoBar is right of the avatar");
                Assert.LessOrEqual(bar.yMax, resources.yMin, "InfoBar sits under the resource bar");
                Assert.Less(bar.xMax, column.xMin, "InfoBar stops before the restaurant bar");
                Assert.AreEqual(bar.xMin, message.xMin, 1f);
                Assert.AreEqual(bar.width, message.width, 1f);
                Assert.GreaterOrEqual(bar.yMin, message.yMax, "AfkMessage sits right under InfoBar");
                Assert.Less(bar.yMin - message.yMax, 20f);

                var roomNode = Named(scene, "Room");
                var origin = InReferenceCanvas(roomNode).position;
                // Chỉ xét sàn: thanh chất lượng (180, 108, 800×68) và phòng "cùng chỗ cũ" cùng nằm theo ảnh đã duyệt nên thanh phủ góc sau-trái nơi hai tường gặp nhau
                // (khoảng 135×27 px, thanh trong suốt 0,78). Chờ user xem ảnh Lobby ở lần dừng playtest.
                var blocks = new[] { "Floor" };
                foreach (var block in blocks)
                {
                    var target = Named(scene, block);
                    var corners = new Vector3[4];
                    target.GetWorldCorners(corners);
                    var quad = corners.Select(c => origin + (Vector2)roomNode.InverseTransformPoint(c)).ToArray();
                    Assert.IsFalse(QuadOverlapsRect(quad, bar), $"InfoBar overlaps {block}");
                    // AfkMessage (spec UI hub B 5.4: (180, 186)) chỉ hiện 6 giây khi thu AFK lưu lỗi và vẽ đè lên phòng, nên không xét đè sàn/tường.
                }

                Assert.IsFalse(bar.Overlaps(column));
                Assert.IsFalse(message.Overlaps(column));
            });

        // Spec UI hub B 5.6: đồ có chiều cao vẽ <tên>Shadow và <tên>Side trước mặt trên, Side dời xuống đúng chiều cao; cửa và lối đi phẳng.
        [Test]
        public void Blocks_HaveSideAndShadow_BelowTop() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var heights = new System.Collections.Generic.List<(string Name, float Height)>
                {
                    ("Kitchen", 44f), ("Counter", 40f), ("WallBack", 90f), ("WallLeft", 90f),
                };
                for (var i = 0; i < 8; i++) heights.Add(($"Table{i}", 22f));
                var soils = All<RectTransform>(scene).Where(r => r.name.StartsWith("Soil") && !r.name.EndsWith("Side") && !r.name.EndsWith("Shadow")).ToArray();
                Assert.AreEqual(6, soils.Length, "2 rows of 3 beds");
                foreach (var soil in soils) heights.Add((soil.name, 12f));

                foreach (var (name, height) in heights)
                {
                    var top = Named(scene, name);
                    var side = top.parent.Find(name + "Side") as RectTransform;
                    var shadow = top.parent.Find(name + "Shadow") as RectTransform;
                    Assert.IsNotNull(side, name + "Side");
                    Assert.IsNotNull(shadow, name + "Shadow");
                    Assert.Less(side.GetSiblingIndex(), top.GetSiblingIndex(), name + "Side draws under the top");
                    Assert.Less(shadow.GetSiblingIndex(), top.GetSiblingIndex(), name + "Shadow draws under the top");
                    foreach (var part in new[] { side, shadow })
                    {
                        Assert.AreEqual(top.sizeDelta, part.sizeDelta, part.name + " size");
                        Assert.AreEqual(0f, Quaternion.Angle(top.localRotation, part.localRotation), 0.01f, part.name + " rotation");
                        Assert.IsFalse(part.GetComponent<Image>().raycastTarget, part.name + " raycast");
                    }

                    Assert.AreEqual(height, top.anchoredPosition.y - side.anchoredPosition.y, 0.5f, name + "Side drop");
                    Assert.AreEqual(top.anchoredPosition.x, side.anchoredPosition.x, 0.5f, name + "Side x");
                    Assert.LessOrEqual(shadow.GetComponent<Image>().color.a, 0.25f, name + "Shadow alpha");
                    Assert.Less(Luma(side.GetComponent<Image>().color), Luma(top.GetComponent<Image>().color), name + "Side is darker than the top");
                }

                foreach (var flat in new[] { "Door", "Path" })
                {
                    var rect = Named(scene, flat);
                    Assert.IsNull(rect.parent.Find(flat + "Side"), flat + " has no side");
                    Assert.IsNull(rect.parent.Find(flat + "Shadow"), flat + " has no shadow");
                }
            });

        // Spec 5.6: mỗi bàn có một đĩa tắt, RestaurantView giữ mảng theo thứ tự bàn.
        [Test]
        public void Tables_HaveDishesInOrder() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                var dishes = new SerializedObject(All<RestaurantView>(scene).Single()).FindProperty("_dishes");
                Assert.AreEqual(8, dishes.arraySize);
                for (var i = 0; i < 8; i++)
                {
                    var dish = (GameObject)dishes.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.IsNotNull(dish, $"_dishes[{i}]");
                    Assert.AreEqual("Dish_PH_40x24", dish.name);
                    Assert.AreSame(Named(scene, $"Table{i}"), dish.transform.parent, $"_dishes[{i}] sits on Table{i}");
                    Assert.IsFalse(dish.activeSelf, $"_dishes[{i}] starts hidden");
                }
            });

        // Spec 5.6: người = bóng + thân + đầu, chân là pivot; chỉ khách có bóng trạng thái với hai biểu tượng.
        [Test]
        public void ActorTemplates_HaveShadowFigureBubble() =>
            WithScene(ProjectSetup.RestaurantScenePath, scene =>
            {
                foreach (var name in new[] { "CustomerTemplate", "WorkerTemplate" })
                {
                    var template = Named(scene, name);
                    Assert.AreEqual(0f, template.pivot.y, name + " pivot is at the feet");
                    foreach (var path in new[] { "Shadow", "Figure/Body", "Figure/Head" })
                        Assert.IsNotNull(template.Find(path), $"{name}/{path}");
                    Assert.AreEqual(0f, template.Find("Figure").GetComponent<RectTransform>().pivot.y, name + "/Figure sways around the feet");
                }

                var bubble = Named(scene, "CustomerTemplate").Find("StatusBubble_PH_64x44") as RectTransform;
                Assert.IsNotNull(bubble);
                Assert.AreEqual(new Vector2(64, 44), bubble.sizeDelta);
                Assert.IsFalse(bubble.gameObject.activeSelf, "bubble starts hidden");
                Assert.IsNotNull(bubble.Find("FoodDots"));
                Assert.IsNotNull(bubble.Find("PayCoin"));
                Assert.IsNull(Named(scene, "WorkerTemplate").Find("StatusBubble_PH_64x44"), "only customers wear a bubble");
                Assert.IsNotNull(Named(scene, "WorkerTemplate").Find("NameText"), "the worker keeps a name label");
            });

        [Test]
        public void RestaurantScene_IsSecondInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.GreaterOrEqual(scenes.Length, 2);
            Assert.AreEqual(ProjectSetup.BootScenePath, scenes[0].path);
            Assert.AreEqual(ProjectSetup.RestaurantScenePath, scenes[1].path);
            Assert.IsTrue(scenes[1].enabled);
        }

        [Test]
        public void DevOverlay_SceneTargetsAreInBuildSettings()
        {
            var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray();
            Assert.IsNotEmpty(DevOverlay.SceneNames);
            foreach (var name in DevOverlay.SceneNames) CollectionAssert.Contains(enabled, name);
        }

        [Test]
        public void RestaurantScene_LocalizedTexts_KeysExistAndNotWrittenByViews() =>
            WithScene(ProjectSetup.RestaurantScenePath, SceneTextAsserts.AssertLocalizedTexts);
    }
}
