using System;
using System.Collections.Generic;
using BunDauPeo.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static BunDauPeo.EditorTools.GachaSceneBuilder;
using Object = UnityEngine.Object;

namespace BunDauPeo.EditorTools
{
    // Dựng scene Restaurant bằng code theo spec Restaurant 5.1 (bố cục từ ba ảnh Mota/restaurant-layout-*), cùng khuôn GachaSceneBuilder.
    // Hình trong Canvas đều là Image: sàn, tường, đồ vật là hình vuông đặc (không gán sprite) cùng một góc xoay; người và cây là Knob;
    // thẻ và nút là UISprite/Knob. Chữ là Text của uGUI (docs dự án thắng skill ui-ugui, xem GachaSceneBuilder).
    internal static class RestaurantSceneBuilder
    {
        // Gốc của phòng (góc sau-trái của sàn) tính từ góc trái trên của Canvas 1920×1080; sàn chiếm khoảng 1184 × 736, lệch trái để chừa cột nút.
        private static readonly Vector2 RoomOrigin = new(310, 220);
        private const float ReferenceWidth = 1920f;
        private const int GridColumns = 12;
        private const int GridRows = 8;

        private static readonly Color Background = new(0.74f, 0.89f, 0.66f);
        private static readonly Color FloorColor = new(1f, 0.93f, 0.78f);
        private static readonly Color WallFace = new(0.46f, 0.64f, 0.52f);
        private static readonly Color WallTop = new(0.68f, 0.84f, 0.74f);
        private static readonly Color Wood = new(0.66f, 0.46f, 0.30f);
        private static readonly Color WoodDark = new(0.52f, 0.34f, 0.22f);
        private static readonly Color TableWood = new(0.83f, 0.66f, 0.42f);
        private static readonly Color Carpet = new(0.78f, 0.35f, 0.33f);
        private static readonly Color CardColor = new(1f, 1f, 1f, 0.95f);
        private static readonly Color BarColor = new(1f, 1f, 1f, 0.78f);
        private static readonly Color DarkText = new(0.20f, 0.22f, 0.30f);
        private static readonly Color StarOn = new(1f, 0.74f, 0.15f);
        private static readonly Color StarOff = new(0.78f, 0.79f, 0.84f);
        private static readonly Color FloatText = new(0.92f, 0.50f, 0.05f);
        private static readonly Color IncidentRed = new(0.90f, 0.25f, 0.25f);
        private static readonly Color IncidentHintText = new(0.75f, 0.10f, 0.10f);
        private static readonly Color TextColor = Color.white;
        private static readonly Color Gold = new(1f, 0.85f, 0.4f);
        private static readonly Color PanelColor = new(0.18f, 0.20f, 0.27f);
        private static readonly Color Dim = new(0f, 0f, 0f, 0.65f);
        private static readonly Color ButtonColor = new(0.30f, 0.50f, 0.90f);
        private static readonly Color AfkGreen = new(0.20f, 0.60f, 0.35f);
        private static readonly Color GreyButton = new(0.45f, 0.47f, 0.55f);
        private static readonly Color Warning = new(1f, 0.55f, 0.45f);

        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new(0, 1);
        private static readonly Vector2 TopRight = new(1, 1);
        private static readonly Vector2 TopCenter = new(0.5f, 1);
        private static readonly Vector2 BottomCenter = new(0.5f, 0);

        // Điểm mốc theo ô lưới (gx, gy). Bàn: hai hàng bốn, hàng gần bếp trước. Điểm mốc của người đứng giữa các lối đi.
        private static readonly Vector2[] TableCells =
        {
            new(5.2f, 3.4f), new(7.0f, 3.4f), new(8.8f, 3.4f), new(10.6f, 3.4f),
            new(5.2f, 5.8f), new(7.0f, 5.8f), new(8.8f, 5.8f), new(10.6f, 5.8f),
        };

        private static readonly Vector2[] CookCells = { new(6.5f, 2.3f), new(8.5f, 2.3f), new(10.5f, 2.3f) };
        private static readonly Vector2[] ServerCells = { new(4.4f, 1.6f), new(4.4f, 2.3f), new(3.6f, 1.9f) };
        private static readonly Vector2[] CashierCells = { new(1.0f, 2.4f), new(2.0f, 2.4f), new(3.0f, 2.4f) };
        private static readonly Vector2 DoorCell = new(3.0f, 8.0f);

        public static void Create(string scenePath)
        {
            // Giống CreateBootScene: batch mode chỉ có scene Untitled chưa lưu nên phải dùng Single.
            var mode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);

            // Nạp sau NewScene (L-002): Single dỡ asset không được scene nào dùng.
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateCamera(scene);
            CreateEventSystem(scene);
            var canvas = CreateCanvas(scene);
            // Bố cục cao đúng 1080 nên khớp theo chiều cao: Game view rộng (vd. 2658×960) không cắt phía dưới như match 0,5.
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 1f;

            var back = NewRect("Background", canvas);
            Stretch(back);
            AddImage(back, null, Background, raycast: false);
            BuildGarden(canvas, knob);

            // Phòng: RectTransform không kích thước đặt ở gốc lưới; con của nó đặt bằng IsoProjection.CellToLocal.
            var room = NewRect("Room", canvas);
            // Neo ở giữa-trên: màn rộng hơn 16:9 thì phòng và vườn ở lại gần giữa, không trôi sát mép trái.
            Place(room, TopCenter, TopLeft, new Vector2(RoomOrigin.x - ReferenceWidth / 2f, -RoomOrigin.y), Vector2.zero);
            BuildCrops(room, knob);

            // Sàn: một chữ nhật một màu, pivot ở góc sau-trái nên xoay quanh gốc lưới.
            var floor = NewRect("Floor", room);
            Place(floor, Center, TopLeft, Vector2.zero, new Vector2(GridColumns * IsoProjection.CellWidth, GridRows * IsoProjection.CellHeight));
            floor.localRotation = Rotation();
            AddImage(floor, null, FloorColor, raycast: false);

            // Tường thấp ở cạnh sau và cạnh trái: mặt tường và một dải sáng nhích lên làm "mép trên".
            Wall(room, "WallBackTop", -0.5f, GridColumns + 0.3f, -0.55f, 0f, WallTop, lift: 26);
            Wall(room, "WallBack", -0.5f, GridColumns + 0.3f, -0.55f, 0f, WallFace, lift: 0);
            Wall(room, "WallLeftTop", -0.55f, 0f, -0.55f, GridRows + 0.1f, WallTop, lift: 26);
            Wall(room, "WallLeft", -0.55f, 0f, -0.55f, GridRows + 0.1f, WallFace, lift: 0);

            // Đồ vật vẽ theo độ sâu: gần người xem hơn thì vẽ sau.
            var furniture = NewRect("Furniture", room);
            Place(furniture, Center, Center, Vector2.zero, Vector2.zero);
            var tables = new RectTransform[TableCells.Length];
            RectTransform counter = null;
            var pieces = new List<(float Depth, Action Build)>
            {
                (IsoProjection.DepthKey(8.5f, 0.75f), () => Block("Kitchen", furniture, 5.5f, 11.5f, 0.15f, 1.35f, WoodDark)),
                (IsoProjection.DepthKey(1.9f, 3.5f), () => counter = Block("Counter", furniture, 0.6f, 3.2f, 3.0f, 4.0f, Wood)),
            };
            for (var i = 0; i < TableCells.Length; i++)
            {
                var index = i;
                var cell = TableCells[i];
                pieces.Add((IsoProjection.DepthKey(cell.x, cell.y),
                    () => tables[index] = Block($"Table{index}", furniture, cell.x - 0.6f, cell.x + 0.6f, cell.y - 0.5f, cell.y + 0.5f, TableWood)));
            }

            pieces.Sort((a, b) => a.Depth.CompareTo(b.Depth));
            foreach (var piece in pieces) piece.Build();

            // Cửa: thảm ở cạnh trước, phía trái, gần quầy. Điểm mốc nằm đúng trên cạnh trước.
            var door = Block("Door", furniture, DoorCell.x - 1f, DoorCell.x + 1f, DoorCell.y - 0.3f, DoorCell.y + 0.3f, Carpet);
            var landmarks = NewRect("Landmarks", room);
            Place(landmarks, Center, Center, Vector2.zero, Vector2.zero);
            var cookSpots = Spots("Cook", landmarks, CookCells);
            var serverSpots = Spots("Server", landmarks, ServerCells);
            var cashierSpots = Spots("Cashier", landmarks, CashierCells);

            // Mẫu để RestaurantView nhân bản; nằm dưới đối tượng tắt nên không hiện. Người đứng thẳng, không xoay theo sàn.
            var templates = NewRect("Templates", room);
            Place(templates, Center, Center, Vector2.zero, Vector2.zero);
            var customerTemplate = NewRect("CustomerTemplate", templates);
            Place(customerTemplate, Center, Center, Vector2.zero, new Vector2(54, 54));
            var customerImage = AddImage(customerTemplate, knob, Color.white, raycast: false);
            customerImage.type = Image.Type.Simple;
            var workerTemplate = NewRect("WorkerTemplate", templates);
            Place(workerTemplate, Center, Center, Vector2.zero, new Vector2(64, 64));
            var workerImage = AddImage(workerTemplate, knob, Color.white, raycast: false);
            workerImage.type = Image.Type.Simple;
            var workerLabel = NewText("NameText", workerTemplate, font, "", 24, TextAnchor.MiddleCenter, DarkText);
            Place(workerLabel.rectTransform, Center, Center, new Vector2(0, 52), new Vector2(200, 30));
            templates.gameObject.SetActive(false);

            var actors = NewRect("Actors", room);
            Place(actors, Center, Center, Vector2.zero, Vector2.zero);

            // Bong bóng sự cố nằm trên lớp người; RestaurantView đặt nó lên trạm của sự cố và bật/tắt.
            var bubble = NewButton("IncidentBubble", room, knob, font, "!", IncidentRed, Center, Vector2.zero, new Vector2(90, 90));
            ((Image)bubble.targetGraphic).type = Image.Type.Simple;
            var bubbleMark = bubble.GetComponentInChildren<Text>();
            bubbleMark.fontSize = 64;
            bubbleMark.fontStyle = FontStyle.Bold;
            // Chữ nằm bên phải bong bóng: phía trên là thanh HUD, vẽ sau nên che chữ.
            var incidentLabel = NewText("IncidentLabel", (RectTransform)bubble.transform, font, "", 28, TextAnchor.MiddleLeft, DarkText);
            Place(incidentLabel.rectTransform, Center, new Vector2(0f, 0.5f), new Vector2(56, 20), new Vector2(460, 34));
            var incidentHint = NewText("IncidentHint", (RectTransform)bubble.transform, font, "", 26, TextAnchor.MiddleLeft, IncidentHintText);
            Place(incidentHint.rectTransform, Center, new Vector2(0f, 0.5f), new Vector2(56, -18), new Vector2(460, 34));
            bubble.gameObject.SetActive(false);

            var viewObject = NewRect("RestaurantView", canvas);
            var view = viewObject.gameObject.AddComponent<RestaurantView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_actors").objectReferenceValue = actors;
            viewSo.FindProperty("_door").objectReferenceValue = door;
            SetRefs(viewSo.FindProperty("_tables"), tables);
            SetRefs(viewSo.FindProperty("_cookSpots"), cookSpots);
            SetRefs(viewSo.FindProperty("_serverSpots"), serverSpots);
            SetRefs(viewSo.FindProperty("_cashierSpots"), cashierSpots);
            viewSo.FindProperty("_customerTemplate").objectReferenceValue = customerImage;
            viewSo.FindProperty("_workerTemplate").objectReferenceValue = workerImage;
            viewSo.FindProperty("_incidentButton").objectReferenceValue = bubble;
            viewSo.FindProperty("_incidentLabel").objectReferenceValue = incidentLabel;
            viewSo.FindProperty("_incidentHint").objectReferenceValue = incidentHint;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var hud = BuildHud(canvas, font, sprite, knob, counter);

            // Cột nút phải: dải trắng bo góc sát mép phải, ba ô; ô chưa có nút để trống (Thu AFK ở ô thứ ba).
            var column = NewRect("RightColumn", canvas);
            Place(column, TopRight, TopRight, new Vector2(-20, -110), new Vector2(170, 520));
            AddImage(column, sprite, CardColor, raycast: false);
            BuildStaffPanel(canvas, column, font, sprite, knob);
            BuildUpgradePanel(canvas, column, font, sprite, knob);
            BuildAfkButton(column, font, knob, hud);

            EditorSceneManager.SaveScene(scene, scenePath);
            if (mode == NewSceneMode.Additive) EditorSceneManager.CloseScene(scene, true);
        }

        private static Quaternion Rotation() => Quaternion.Euler(0f, 0f, IsoProjection.RotationDegrees);

        // Chữ nhật theo ô lưới [gx0, gx1] × [gy0, gy1], cùng góc xoay với sàn.
        private static RectTransform Block(string name, RectTransform parent, float gx0, float gx1, float gy0, float gy1, Color color, float lift = 0f)
        {
            var rect = NewRect(name, parent);
            var center = IsoProjection.CellToLocal((gx0 + gx1) / 2f, (gy0 + gy1) / 2f);
            Place(rect, Center, Center, center + Vector2.up * lift, new Vector2((gx1 - gx0) * IsoProjection.CellWidth, (gy1 - gy0) * IsoProjection.CellHeight));
            rect.localRotation = Rotation();
            AddImage(rect, null, color, raycast: false);
            return rect;
        }

        private static void Wall(RectTransform room, string name, float gx0, float gx1, float gy0, float gy1, Color color, float lift) =>
            Block(name, room, gx0, gx1, gy0, gy1, color, lift);

        // Cảnh quanh phòng, chỉ để nhìn (raycast tắt, không có luật): cây phía sau và bụi hoa bên phải, tọa độ Canvas từ góc trái trên.
        private static void BuildGarden(RectTransform canvas, Sprite knob)
        {
            var garden = NewRect("Garden", canvas);
            Place(garden, TopCenter, TopLeft, new Vector2(-ReferenceWidth / 2f, 0), Vector2.zero);
            var trees = new (float X, float Y, float Size, Color Color)[]
            {
                (110, 360, 210, new Color(0.30f, 0.55f, 0.35f)), (170, 520, 150, new Color(0.40f, 0.66f, 0.42f)),
                (1500, 330, 240, new Color(0.30f, 0.55f, 0.35f)), (1610, 500, 160, new Color(0.40f, 0.66f, 0.42f)),
                (1510, 660, 120, new Color(0.55f, 0.65f, 0.95f)), (1630, 720, 120, new Color(0.75f, 0.60f, 0.90f)),
                (1530, 850, 130, new Color(0.95f, 0.65f, 0.80f)), (1650, 920, 120, new Color(0.55f, 0.65f, 0.95f)),
            };
            for (var i = 0; i < trees.Length; i++)
            {
                var rect = NewRect($"Plant{i}", garden);
                Place(rect, TopLeft, Center, new Vector2(trees[i].X, -trees[i].Y), new Vector2(trees[i].Size, trees[i].Size));
                AddImage(rect, knob, trees[i].Color, raycast: false).type = Image.Type.Simple;
            }
        }

        // Lối từ cửa và hai hàng luống phía trước phòng, cùng góc xoay với sàn (nằm trong hệ lưới nên đặt bằng ô lưới).
        private static void BuildCrops(RectTransform room, Sprite knob)
        {
            var crops = NewRect("Crops", room);
            Place(crops, Center, Center, Vector2.zero, Vector2.zero);
            Block("Path", crops, DoorCell.x - 1f, DoorCell.x + 1f, 8.3f, 11f, new Color(0.93f, 0.85f, 0.62f));
            foreach (var (gy0, gy1, row) in new[] { (8.5f, 9.1f, 0), (9.6f, 10.2f, 1) })
            {
                foreach (var (gx0, gx1) in new[] { (0.2f, 1.8f), (4.3f, 6.5f), (7.0f, 9.2f) })
                {
                    Block($"Soil{row}_{gx0}", crops, gx0, gx1, gy0, gy1, new Color(0.62f, 0.45f, 0.30f));
                    if (row != 0) continue;
                    var sprout = NewRect($"Sprout{gx0}", crops);
                    Place(sprout, Center, Center, IsoProjection.CellToLocal((gx0 + gx1) / 2f, (gy0 + gy1) / 2f), new Vector2(34, 34));
                    AddImage(sprout, knob, new Color(0.35f, 0.70f, 0.38f), raycast: false).type = Image.Type.Simple;
                }
            }
        }

        private static Text Card(RectTransform canvas, Font font, Sprite sprite, string name, string textName, string text, Vector2 position)
        {
            var card = NewRect(name, canvas);
            Place(card, TopLeft, TopLeft, position, new Vector2(340, 76));
            AddImage(card, sprite, CardColor, raycast: false);
            var label = NewText(textName, card, font, text, 40, TextAnchor.MiddleLeft, DarkText);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(26, 0);
            label.rectTransform.offsetMax = new Vector2(-12, 0);
            return label;
        }

        private static RestaurantHud BuildHud(RectTransform canvas, Font font, Sprite sprite, Sprite knob, RectTransform counter)
        {
            // Ba thẻ tiền trên-trái.
            var coin = Card(canvas, font, sprite, "CoinCard", "CoinText", "Coin 0", new Vector2(30, -20));
            var material = Card(canvas, font, sprite, "MaterialCard", "MaterialText", "Material 0", new Vector2(390, -20));
            var gem = Card(canvas, font, sprite, "GemCard", "GemText", "Gem 0", new Vector2(750, -20));

            // Thanh giữa-trên dưới hàng thẻ: sao và bộ đếm ở hàng trên, cảnh báo và nhãn thu nhập ở hàng dưới; được phép đè lên tường sau.
            var bar = NewRect("InfoBar", canvas);
            Place(bar, TopCenter, TopLeft, new Vector2(240 - ReferenceWidth / 2f, -108), new Vector2(1120, 120));
            AddImage(bar, sprite, BarColor, raycast: false);

            // Sao chất lượng: nền xám và lớp vàng Filled ngang chồng lên, vì font dựng sẵn có thể thiếu ký tự sao.
            var stars = new RectTransform[5];
            for (var i = 0; i < stars.Length; i++)
            {
                var slot = NewRect($"Star{i}", bar);
                Place(slot, TopLeft, TopLeft, new Vector2(24 + i * 56, -10), new Vector2(48, 48));
                AddImage(slot, knob, StarOff, raycast: false).type = Image.Type.Simple;
                var fill = NewRect("Fill", slot);
                Stretch(fill);
                var fillImage = AddImage(fill, knob, StarOn, raycast: false);
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillAmount = 0f;
                stars[i] = fill;
            }

            var counterText = NewText("CounterText", bar, font, "", 34, TextAnchor.MiddleRight, DarkText);
            Place(counterText.rectTransform, TopRight, TopRight, new Vector2(-24, -10), new Vector2(760, 48));
            var warning = NewRect("WarningIcon", bar);
            Place(warning, TopLeft, TopLeft, new Vector2(24, -70), new Vector2(36, 36));
            var warningImage = AddImage(warning, null, new Color(0.95f, 0.40f, 0.30f), raycast: false);
            var income = NewText("IncomeText", bar, font, "", 34, TextAnchor.MiddleLeft, DarkText);
            Place(income.rectTransform, TopLeft, TopLeft, new Vector2(74, -64), new Vector2(1020, 48));

            var floats = new Text[8];
            for (var i = 0; i < floats.Length; i++)
            {
                floats[i] = NewText($"FloatText{i}", canvas, font, "", 48, TextAnchor.MiddleCenter, FloatText);
                Place(floats[i].rectTransform, Center, Center, Vector2.zero, new Vector2(200, 60));
                floats[i].gameObject.SetActive(false);
            }

            var hudObject = NewRect("RestaurantHud", canvas);
            var hud = hudObject.gameObject.AddComponent<RestaurantHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("_coinText").objectReferenceValue = coin;
            so.FindProperty("_materialText").objectReferenceValue = material;
            so.FindProperty("_gemText").objectReferenceValue = gem;
            so.FindProperty("_incomeText").objectReferenceValue = income;
            so.FindProperty("_counterText").objectReferenceValue = counterText;
            so.FindProperty("_warningIcon").objectReferenceValue = warningImage;
            so.FindProperty("_floatOrigin").objectReferenceValue = counter;
            var starProperty = so.FindProperty("_stars");
            starProperty.arraySize = stars.Length;
            for (var i = 0; i < stars.Length; i++) starProperty.GetArrayElementAtIndex(i).objectReferenceValue = stars[i].GetComponent<Image>();
            SetRefs(so.FindProperty("_floatTexts"), floats);
            so.ApplyModifiedPropertiesWithoutUndo();
            return hud;
        }

        // Ô thứ ba của cột phải, cùng cỡ với hai nút trên; RestaurantHud ẩn/hiện theo Coin AFK đang chờ.
        private static void BuildAfkButton(RectTransform column, Font font, Sprite knob, RestaurantHud hud)
        {
            var button = NewButton("AfkButton", column, knob, font, "Thu AFK", AfkGreen, TopCenter, new Vector2(0, -350), new Vector2(130, 130));
            ((Image)button.targetGraphic).type = Image.Type.Simple;
            var text = button.GetComponentInChildren<Text>();
            text.fontSize = 22;
            // Số Coin dài (5 chữ số trở lên) phải co lại cho vừa vòng tròn.
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 22;
            // Chừa lề trong vòng tròn để chữ co lại thay vì tràn ra ngoài.
            text.rectTransform.offsetMin = new Vector2(16, 0);
            text.rectTransform.offsetMax = new Vector2(-16, 0);

            var so = new SerializedObject(hud);
            so.FindProperty("_afkButton").objectReferenceValue = button;
            so.FindProperty("_afkText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Theo mẫu quest-team-select-popup: hộp giữa màn, danh sách ở trên, ô hiện tại ở dưới; ở đây là ba cột ô và danh sách mở khi bấm ô.
        private static void BuildStaffPanel(RectTransform canvas, RectTransform column, Font font, Sprite sprite, Sprite knob)
        {
            const int slotsPerRole = 3;
            // Nút tròn trong ô đầu của cột phải, cỡ chạm 130.
            var open = NewButton("StaffButton", column, knob, font, "Nhân viên", ButtonColor, TopCenter, new Vector2(0, -20), new Vector2(130, 130));
            ((Image)open.targetGraphic).type = Image.Type.Simple;
            open.GetComponentInChildren<Text>().fontSize = 24;

            var root = NewRect("StaffPanelRoot", canvas);
            Stretch(root);
            AddImage(root, null, Dim, raycast: true);
            var box = NewRect("Box", root);
            Place(box, Center, Center, Vector2.zero, new Vector2(1500, 860));
            AddImage(box, sprite, PanelColor, raycast: false);
            var title = NewText("TitleText", box, font, "Nhân viên", 52, TextAnchor.MiddleCenter, TextColor);
            Place(title.rectTransform, TopCenter, TopCenter, new Vector2(0, -25), new Vector2(800, 70));
            var close = NewButton("CloseButton", box, sprite, font, "Đóng", GreyButton, TopRight, new Vector2(-30, -25), new Vector2(200, 80));
            var message = NewText("MessageText", box, font, "", 32, TextAnchor.MiddleCenter, Warning);
            Place(message.rectTransform, BottomCenter, BottomCenter, new Vector2(0, 25), new Vector2(1300, 50));

            var columnTitles = new Text[3];
            var slotButtons = new Button[3 * slotsPerRole];
            var slotTexts = new Text[3 * slotsPerRole];
            var removeButtons = new Button[3 * slotsPerRole];
            for (var role = 0; role < 3; role++)
            {
                var x = (role - 1) * 480f;
                columnTitles[role] = NewText($"ColumnTitle{role}", box, font, "", 44, TextAnchor.MiddleCenter, Gold);
                Place(columnTitles[role].rectTransform, TopCenter, TopCenter, new Vector2(x, -120), new Vector2(440, 60));
                for (var slot = 0; slot < slotsPerRole; slot++)
                {
                    var index = role * slotsPerRole + slot;
                    var button = NewButton($"Slot{role}_{slot}", box, sprite, font, "", ButtonColor, TopCenter, new Vector2(x, -190 - slot * 140), new Vector2(440, 110));
                    var label = (RectTransform)button.transform.GetChild(0);
                    // Chừa chỗ cho nút Gỡ bên phải.
                    label.offsetMin = new Vector2(14, 0);
                    label.offsetMax = new Vector2(-130, 0);
                    slotTexts[index] = label.GetComponent<Text>();
                    slotTexts[index].alignment = TextAnchor.MiddleLeft;
                    slotTexts[index].fontSize = 30;
                    slotButtons[index] = button;
                    removeButtons[index] = NewButton("RemoveButton", button.transform as RectTransform, sprite, font, "Gỡ", GreyButton,
                        new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(110, 70));
                }
            }

            // Danh sách nhân vật: ScrollRect với Content tự co giãn theo số hàng; hàng mẫu nằm ngoài Content và tắt.
            var listRoot = NewRect("StaffListPopup", root);
            Stretch(listRoot);
            AddImage(listRoot, null, Dim, raycast: true);
            var listBox = NewRect("Box", listRoot);
            Place(listBox, Center, Center, Vector2.zero, new Vector2(1250, 780));
            AddImage(listBox, sprite, PanelColor, raycast: false);
            var listTitle = NewText("TitleText", listBox, font, "", 48, TextAnchor.MiddleCenter, TextColor);
            Place(listTitle.rectTransform, TopCenter, TopCenter, new Vector2(0, -25), new Vector2(1100, 70));
            var scrollRoot = NewRect("ScrollView", listBox);
            Place(scrollRoot, Center, Center, new Vector2(0, -10), new Vector2(1150, 540));
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
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var rowTemplate = NewButton("RowTemplate", listBox, sprite, font, "", ButtonColor, Center, Vector2.zero, new Vector2(1150, 90));
            var rowLabel = (RectTransform)rowTemplate.transform.GetChild(0);
            rowLabel.offsetMin = new Vector2(16, 0);
            rowLabel.offsetMax = new Vector2(-16, 0);
            var rowText = rowLabel.GetComponent<Text>();
            rowText.alignment = TextAnchor.MiddleLeft;
            rowText.fontSize = 28;
            var rowSize = rowTemplate.gameObject.AddComponent<LayoutElement>();
            rowSize.minHeight = 90;
            rowSize.preferredHeight = 90;
            rowTemplate.gameObject.SetActive(false);
            var listClose = NewButton("CloseButton", listBox, sprite, font, "Đóng", GreyButton, BottomCenter, new Vector2(0, 25), new Vector2(320, 80));
            listRoot.gameObject.SetActive(false);

            var confirmRoot = NewRect("StaffConfirmPopup", root);
            Stretch(confirmRoot);
            AddImage(confirmRoot, null, Dim, raycast: true);
            var confirmBox = NewRect("Box", confirmRoot);
            Place(confirmBox, Center, Center, Vector2.zero, new Vector2(900, 420));
            AddImage(confirmBox, sprite, PanelColor, raycast: false);
            var confirmText = NewText("ConfirmText", confirmBox, font, "", 40, TextAnchor.MiddleCenter, TextColor);
            Place(confirmText.rectTransform, TopCenter, TopCenter, new Vector2(0, -40), new Vector2(820, 190));
            var yes = NewButton("YesButton", confirmBox, sprite, font, "Chuyển", ButtonColor, BottomCenter, new Vector2(-200, 40), new Vector2(340, 90));
            var no = NewButton("NoButton", confirmBox, sprite, font, "Thôi", GreyButton, BottomCenter, new Vector2(200, 40), new Vector2(340, 90));
            confirmRoot.gameObject.SetActive(false);

            root.gameObject.SetActive(false);

            var panelObject = NewRect("StaffPanel", canvas);
            var panel = panelObject.gameObject.AddComponent<StaffPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_openButton").objectReferenceValue = open;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_messageText").objectReferenceValue = message;
            SetRefs(so.FindProperty("_columnTitles"), columnTitles);
            SetRefs(so.FindProperty("_slotButtons"), slotButtons);
            SetRefs(so.FindProperty("_slotTexts"), slotTexts);
            SetRefs(so.FindProperty("_removeButtons"), removeButtons);
            so.FindProperty("_listRoot").objectReferenceValue = listRoot.gameObject;
            so.FindProperty("_listTitle").objectReferenceValue = listTitle;
            so.FindProperty("_listContent").objectReferenceValue = content;
            so.FindProperty("_rowTemplate").objectReferenceValue = rowTemplate;
            so.FindProperty("_listCloseButton").objectReferenceValue = listClose;
            so.FindProperty("_confirmRoot").objectReferenceValue = confirmRoot.gameObject;
            so.FindProperty("_confirmText").objectReferenceValue = confirmText;
            so.FindProperty("_confirmYesButton").objectReferenceValue = yes;
            so.FindProperty("_confirmNoButton").objectReferenceValue = no;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Bản thử của nâng cấp (spec 5.3): hộp giữa màn, mỗi nâng cấp một hàng nút do UpgradeTestPanel dựng từ hàng mẫu.
        private static void BuildUpgradePanel(RectTransform canvas, RectTransform column, Font font, Sprite sprite, Sprite knob)
        {
            // Ô thứ hai của cột phải, cùng cỡ với nút Nhân viên.
            var open = NewButton("UpgradeButton", column, knob, font, "Nâng cấp", ButtonColor, TopCenter, new Vector2(0, -185), new Vector2(130, 130));
            ((Image)open.targetGraphic).type = Image.Type.Simple;
            open.GetComponentInChildren<Text>().fontSize = 24;

            var root = NewRect("UpgradePanelRoot", canvas);
            Stretch(root);
            AddImage(root, null, Dim, raycast: true);
            var box = NewRect("Box", root);
            Place(box, Center, Center, Vector2.zero, new Vector2(1100, 880));
            AddImage(box, sprite, PanelColor, raycast: false);
            var title = NewText("TitleText", box, font, "Nâng cấp (bản thử)", 52, TextAnchor.MiddleCenter, TextColor);
            Place(title.rectTransform, TopCenter, TopCenter, new Vector2(0, -25), new Vector2(700, 70));
            var close = NewButton("CloseButton", box, sprite, font, "Đóng", GreyButton, TopRight, new Vector2(-30, -25), new Vector2(200, 80));
            var message = NewText("MessageText", box, font, "", 32, TextAnchor.MiddleCenter, Warning);
            Place(message.rectTransform, BottomCenter, BottomCenter, new Vector2(0, 25), new Vector2(1000, 50));

            // shortcut: cột không cuộn, vừa sáu nâng cấp mẫu; thêm ScrollRect như danh sách của StaffPanel nếu catalog dài hơn.
            var content = NewRect("Content", box);
            Place(content, TopCenter, TopCenter, new Vector2(0, -120), new Vector2(1000, 660));
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var rowTemplate = NewButton("RowTemplate", box, sprite, font, "", ButtonColor, Center, Vector2.zero, new Vector2(1000, 100));
            var rowLabel = (RectTransform)rowTemplate.transform.GetChild(0);
            rowLabel.offsetMin = new Vector2(20, 0);
            rowLabel.offsetMax = new Vector2(-20, 0);
            var rowText = rowLabel.GetComponent<Text>();
            rowText.alignment = TextAnchor.MiddleLeft;
            rowText.fontSize = 30;
            var rowSize = rowTemplate.gameObject.AddComponent<LayoutElement>();
            rowSize.minHeight = 100;
            rowSize.preferredHeight = 100;
            rowTemplate.gameObject.SetActive(false);

            root.gameObject.SetActive(false);

            var panelObject = NewRect("UpgradeTestPanel", canvas);
            var panel = panelObject.gameObject.AddComponent<UpgradeTestPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_openButton").objectReferenceValue = open;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_messageText").objectReferenceValue = message;
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_rowTemplate").objectReferenceValue = rowTemplate;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform[] Spots(string prefix, RectTransform parent, Vector2[] cells)
        {
            var spots = new RectTransform[cells.Length];
            for (var i = 0; i < cells.Length; i++)
            {
                spots[i] = NewRect($"{prefix}Spot{i}", parent);
                Place(spots[i], Center, Center, IsoProjection.CellToLocal(cells[i].x, cells[i].y), Vector2.zero);
            }

            return spots;
        }

        private static void SetRefs(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void CreateCamera(Scene scene)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            SceneManager.MoveGameObjectToScene(go, scene);
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            go.AddComponent<AudioListener>();
        }
    }
}
