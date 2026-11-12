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
        // Gốc của phòng (góc sau-trái của sàn) tính từ góc trái trên của Canvas 1920×1080; sàn chiếm khoảng 1184 × 736.
        // Dời phải 155 px so với chỗ cũ (310) để banner Gacha không che "!" Thu ngân ở 16:10 (user chốt 2026-11-11); xa hơn thì tường chạm thanh quán mở (test Room_ClearOfOpenRestaurantBar).
        private const float RoomShiftX = 155f;
        private static readonly Vector2 RoomOrigin = new(310 + RoomShiftX, 220);
        private const float ReferenceWidth = 1920f;
        // Khung kéo (spec UI hub B 5.2): 2800 rộng hơn màn 20:9 (2400 đơn vị) để vẫn kéo ngang được; WorldInset là phần thừa mỗi bên so với canvas 1920×1080.
        private static readonly Vector2 WorldSize = new(2800, 1400);
        private static readonly Vector2 WorldInset = (WorldSize - new Vector2(ReferenceWidth, 1080f)) / 2f;
        private const int GridColumns = 12;
        private const int GridRows = 8;

        private static readonly Color Background = new(0.74f, 0.89f, 0.66f);
        private static readonly Color FloorColor = new(1f, 0.93f, 0.78f);
        // Chiều cao khối (px) theo spec UI hub B 5.6: mặt trước dời xuống đúng chừng này.
        private const float WallHeight = 90f, KitchenHeight = 44f, CounterHeight = 40f, TableHeight = 22f, BedHeight = 12f;
        private static readonly Color PersonSkin = new(1f, 0.87f, 0.72f);
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
        private static readonly Color AvatarColor = new(0.45f, 0.55f, 0.85f);
        private static readonly Color AvatarFace = new(0.78f, 0.84f, 0.97f);
        private static readonly Color BannerColor = new(0.55f, 0.38f, 0.82f);
        // Popup sáng theo màn quán: thẻ trắng, chữ tối; hai màu này đủ tối để đọc trên thẻ (RestaurantSceneTests kiểm độ tương phản).
        private static readonly Color SoftText = new(0.36f, 0.38f, 0.46f);
        private static readonly Color FullText = new(0.78f, 0.35f, 0f);

        // Hộp vẽ trên thanh giữ cỡ cũ (spec UI hub 5.5); test đọc lại hai hằng này.
        internal static readonly Vector2 StaffConfirmSize = new(900, 420);
        internal static readonly Vector2 AfkPopupSize = new(1100, 700);

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

            // Lớp dưới cùng: vuốt trên WorldScroll kéo khung thế giới; Image trong suốt nhận vuốt, "!" của sự cố là con nên uGUI chuyển kéo lên đây.
            var worldRoot = NewRect("WorldScroll", canvas);
            Stretch(worldRoot);
            AddImage(worldRoot, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            // Mặc định Unity bỏ qua mesh trong suốt cả khi raycast: tắt để Image alpha 0 vẫn nhận vuốt.
            worldRoot.GetComponent<CanvasRenderer>().cullTransparentMesh = false;
            var world = NewRect("World_PH_2800x1400", worldRoot);
            Place(world, Center, Center, Vector2.zero, WorldSize);
            var scroll = worldRoot.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = worldRoot;
            scroll.content = world;
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 0f;

            var back = NewRect("Background", world);
            Stretch(back);
            AddImage(back, null, Background, raycast: false);
            BuildGarden(world, knob);

            // Phòng: RectTransform không kích thước đặt ở gốc lưới; con của nó đặt bằng IsoProjection.CellToLocal.
            var room = NewRect("Room", world);
            // Neo góc trên-trái của World; cộng WorldInset để lúc World ở giữa, phòng nằm đúng chỗ cũ trên màn 16:9.
            Place(room, TopLeft, TopLeft, new Vector2(RoomOrigin.x + WorldInset.x, -(RoomOrigin.y + WorldInset.y)), Vector2.zero);
            BuildCrops(room, knob);

            // Sàn: một chữ nhật một màu, pivot ở góc sau-trái nên xoay quanh gốc lưới.
            var floor = NewRect("Floor", room);
            Place(floor, Center, TopLeft, Vector2.zero, new Vector2(GridColumns * IsoProjection.CellWidth, GridRows * IsoProjection.CellHeight));
            floor.localRotation = Rotation();
            AddImage(floor, null, FloorColor, raycast: false);

            // Tường ở cạnh sau và cạnh trái: mặt trên sáng, mặt trước tối hơn dời xuống 90 px (Block lo phần đó).
            Wall(room, "WallBack", -0.5f, GridColumns + 0.3f, -0.55f, 0f);
            Wall(room, "WallLeft", -0.55f, 0f, -0.55f, GridRows + 0.1f);

            // Đồ vật vẽ theo độ sâu: gần người xem hơn thì vẽ sau.
            var furniture = NewRect("Furniture", room);
            Place(furniture, Center, Center, Vector2.zero, Vector2.zero);
            var tables = new RectTransform[TableCells.Length];
            RectTransform counter = null, kitchen = null;
            var pieces = new List<(float Depth, Action Build)>
            {
                (IsoProjection.DepthKey(8.5f, 0.75f), () => kitchen = Block("Kitchen", furniture, 5.5f, 11.5f, 0.15f, 1.35f, WoodDark, KitchenHeight)),
                (IsoProjection.DepthKey(1.9f, 3.5f), () => counter = Block("Counter", furniture, 0.6f, 3.2f, 3.0f, 4.0f, Wood, CounterHeight)),
            };
            for (var i = 0; i < TableCells.Length; i++)
            {
                var index = i;
                var cell = TableCells[i];
                pieces.Add((IsoProjection.DepthKey(cell.x, cell.y),
                    () => tables[index] = Block($"Table{index}", furniture, cell.x - 0.6f, cell.x + 0.6f, cell.y - 0.5f, cell.y + 0.5f, TableWood, TableHeight)));
            }

            pieces.Sort((a, b) => a.Depth.CompareTo(b.Depth));
            foreach (var piece in pieces) piece.Build();

            // Chỉ để nhìn: 3 nồi trên mặt bếp, máy tính tiền trên quầy, và mỗi bàn một đĩa (bật khi có khách đang ăn).
            for (var i = 0; i < 3; i++)
            {
                var pot = NewRect($"Pot{i}", kitchen);
                Place(pot, Center, Center, new Vector2((i - 1) * 170f, 0f), new Vector2(40, 40));
                AddImage(pot, knob, new Color(0.30f, 0.31f, 0.35f), raycast: false).type = Image.Type.Simple;
            }

            var register = NewRect("Register", counter);
            Place(register, Center, Center, Vector2.zero, new Vector2(44, 30));
            AddImage(register, null, new Color(0.25f, 0.27f, 0.32f), raycast: false);
            var dishes = new GameObject[tables.Length];
            for (var i = 0; i < tables.Length; i++)
            {
                var dish = NewRect("Dish_PH_40x24", tables[i]);
                Place(dish, Center, Center, Vector2.zero, new Vector2(40, 24));
                AddImage(dish, knob, new Color(0.95f, 0.95f, 0.95f), raycast: false).type = Image.Type.Simple;
                dish.gameObject.SetActive(false);
                dishes[i] = dish.gameObject;
            }

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
            var customerTemplate = BuildPerson("CustomerTemplate", templates, 54f, sprite, knob);
            BuildStatusBubble(customerTemplate, font, sprite, knob);
            var workerTemplate = BuildPerson("WorkerTemplate", templates, 64f, sprite, knob);
            // Tên nhân viên nằm dưới chân.
            var workerLabel = NewText("NameText", workerTemplate, font, "", 24, TextAnchor.UpperCenter, DarkText);
            Place(workerLabel.rectTransform, BottomCenter, TopCenter, new Vector2(0, -4), new Vector2(200, 30));
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
            viewSo.FindProperty("_customerTemplate").objectReferenceValue = customerTemplate;
            viewSo.FindProperty("_workerTemplate").objectReferenceValue = workerTemplate;
            SetRefs(viewSo.FindProperty("_dishes"), dishes);
            viewSo.FindProperty("_incidentButton").objectReferenceValue = bubble;
            viewSo.FindProperty("_incidentLabel").objectReferenceValue = incidentLabel;
            viewSo.FindProperty("_incidentHint").objectReferenceValue = incidentHint;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            // HUD Lobby (spec UI hub B 5.1, 5.4): gom trong LobbyHud để HubNav bật/tắt cả nhóm. RestaurantView và RestaurantHud nằm ngoài nhóm nên quán vẫn chạy.
            var lobbyHud = NewRect("LobbyHud", canvas);
            Stretch(lobbyHud);
            var hud = BuildHud(world, lobbyHud, font, sprite, knob, counter, out var statsButton);
            var (avatar, banner, shop) = BuildLobbyButtons(lobbyHud, font, sprite, knob);
            var restaurantBar = BuildRestaurantBar(lobbyHud, font, sprite, knob);
            var staffPanel = BuildStaffPanel(canvas, restaurantBar, font, sprite, knob);
            var upgradePanel = BuildUpgradePanel(canvas, restaurantBar, font, sprite, knob);
            BuildAfkButton(restaurantBar, font, knob, hud);
            var qualityPopup = BuildQualityPopup(canvas, font, sprite, statsButton);

            // Lớp vẽ của Lobby (spec UI hub 5.1), từ dưới lên: quán, HUD Lobby, trang của quán (ở trên), ba panel của thanh, thanh tài nguyên, Menu, thanh dưới, rồi hộp phần thưởng.
            var charactersPanel = CharactersSceneBuilder.BuildPanel(canvas, sprite, font);
            var stagesPanel = StageSelectSceneBuilder.BuildModePanel(canvas, sprite, font);
            var stageMapPanel = StageSelectSceneBuilder.BuildPanel(canvas, sprite, knob, font);
            var gachaPanel = GachaSceneBuilder.BuildPanel(canvas, sprite, font);
            var resourceBar = NavBarBuilder.BuildResourceBar(canvas, font, sprite, knob);
            var menuPanel = NavBarBuilder.BuildMenu(canvas, font, sprite, out var menuView);
            var notifyView = NavBarBuilder.BuildNotifyButton(canvas, font, sprite, knob);
            var hub = NavBarBuilder.Build(canvas, font, sprite);
            var notifyPopup = NavBarBuilder.BuildNotifyPopup(canvas, font, sprite);
            NavBarBuilder.WireLobby(hub, resourceBar, lobbyHud.gameObject, new[] { avatar, shop }, banner);
            // Trang kín màn: bấm ô khác thì HubNav đóng từng trang đang mở qua IHubPage.Close() (Chọn đội trả đội như lúc mở).
            NavBarBuilder.Wire(hub, menuView, charactersPanel, stagesPanel, gachaPanel, menuPanel, new Object[]
            {
                staffPanel, upgradePanel, qualityPopup, notifyPopup, stageMapPanel.GetComponentInChildren<StageMapView>(true),
                stageMapPanel.GetComponentInChildren<StageInfoPopup>(true), stageMapPanel.GetComponentInChildren<StoryPlaceholderPopup>(true),
                stageMapPanel.GetComponentInChildren<TeamSelectPopup>(true), gachaPanel.GetComponentInChildren<GachaHistoryPopup>(true),
            });
            var stageMap = stageMapPanel.GetComponentInChildren<StageMapView>(true);
            StageSelectSceneBuilder.WireModePanel(stagesPanel, hub, stageMap);
            var hubSo = new SerializedObject(hub);
            hubSo.FindProperty("_stageMap").objectReferenceValue = stageMap;
            hubSo.ApplyModifiedPropertiesWithoutUndo();

            // Hộp Thu AFK dựng sau thanh và có Canvas con (OverBar) nên vẽ đè lên thanh.
            var afkPopup = BuildAfkPopup(canvas, font, sprite);
            viewSo.FindProperty("_afkPopup").objectReferenceValue = afkPopup;
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            NavBarBuilder.WireNotify(notifyView, hub, notifyPopup, scroll, (RectTransform)bubble.transform, staffPanel, afkPopup,
                charactersPanel.GetComponentInChildren<CharactersView>(true));

            EditorSceneManager.SaveScene(scene, scenePath);
            if (mode == NewSceneMode.Additive) EditorSceneManager.CloseScene(scene, true);
        }

        private static Quaternion Rotation() => Quaternion.Euler(0f, 0f, IsoProjection.RotationDegrees);

        // Chữ nhật theo ô lưới [gx0, gx1] × [gy0, gy1], cùng góc xoay với sàn.
        // height > 0: vẽ thêm <tên>Shadow và <tên>Side trước mặt trên (spec UI hub B 5.6). Side là mặt trước, tối hơn 30%, dời xuống đúng height; bóng đổ lệch phải-dưới.
        private static RectTransform Block(string name, RectTransform parent, float gx0, float gx1, float gy0, float gy1, Color color, float height = 0f)
        {
            var center = IsoProjection.CellToLocal((gx0 + gx1) / 2f, (gy0 + gy1) / 2f);
            var size = new Vector2((gx1 - gx0) * IsoProjection.CellWidth, (gy1 - gy0) * IsoProjection.CellHeight);
            if (height > 0f)
            {
                Slab(name + "Shadow", parent, center + new Vector2(10f, -height - 8f), size, new Color(0f, 0f, 0f, 0.18f));
                Slab(name + "Side", parent, center + Vector2.down * height, size, new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, color.a));
            }

            return Slab(name, parent, center, size, color);
        }

        private static RectTransform Slab(string name, RectTransform parent, Vector2 center, Vector2 size, Color color)
        {
            var rect = NewRect(name, parent);
            Place(rect, Center, Center, center, size);
            rect.localRotation = Rotation();
            AddImage(rect, null, color, raycast: false);
            return rect;
        }

        private static void Wall(RectTransform room, string name, float gx0, float gx1, float gy0, float gy1) =>
            Block(name, room, gx0, gx1, gy0, gy1, WallTop, WallHeight);

        // Người (spec 5.6): gốc ở chân; Shadow cố định, Figure (thân + đầu) nhún và nghiêng quanh chân khi đi.
        private static RectTransform BuildPerson(string name, RectTransform parent, float size, Sprite sprite, Sprite knob)
        {
            var root = NewRect(name, parent);
            Place(root, Center, BottomCenter, Vector2.zero, new Vector2(size, size));
            var shadow = NewRect("Shadow", root);
            Place(shadow, BottomCenter, Center, new Vector2(0, 2), new Vector2(size, size / 3f));
            AddImage(shadow, knob, new Color(0f, 0f, 0f, 0.25f), raycast: false).type = Image.Type.Simple;
            var figure = NewRect("Figure", root);
            Place(figure, BottomCenter, BottomCenter, Vector2.zero, new Vector2(size, size * 1.6f));
            var body = NewRect("Body", figure);
            Place(body, BottomCenter, BottomCenter, new Vector2(0, 4), new Vector2(size * 0.8f, size));
            AddImage(body, sprite, Color.white, raycast: false);
            var head = NewRect("Head", figure);
            Place(head, BottomCenter, BottomCenter, new Vector2(0, size * 0.78f), new Vector2(size * 0.75f, size * 0.75f));
            AddImage(head, knob, PersonSkin, raycast: false).type = Image.Type.Simple;
            return root;
        }

        // Bóng trạng thái trên đầu khách: "..." khi chờ món, đồng xu khi chờ trả tiền; RestaurantView bật đúng một trong hai.
        private static void BuildStatusBubble(RectTransform customer, Font font, Sprite sprite, Sprite knob)
        {
            var bubble = NewRect("StatusBubble_PH_64x44", customer);
            Place(bubble, BottomCenter, BottomCenter, new Vector2(0, customer.sizeDelta.y * 1.7f), new Vector2(64, 44));
            AddImage(bubble, sprite, new Color(1f, 1f, 1f, 0.95f), raycast: false);
            var dots = NewText("FoodDots", bubble, font, "...", 32, TextAnchor.MiddleCenter, DarkText);
            Stretch(dots.rectTransform);
            dots.fontStyle = FontStyle.Bold;
            dots.gameObject.SetActive(false);
            var coin = NewRect("PayCoin", bubble);
            Place(coin, Center, Center, Vector2.zero, new Vector2(28, 28));
            AddImage(coin, knob, Gold, raycast: false).type = Image.Type.Simple;
            coin.gameObject.SetActive(false);
            bubble.gameObject.SetActive(false);
        }

        // Cảnh quanh phòng, chỉ để nhìn (raycast tắt, không có luật): cây phía sau và bụi hoa bên phải, tọa độ Canvas từ góc trái trên.
        private static void BuildGarden(RectTransform world, Sprite knob)
        {
            var garden = NewRect("Garden", world);
            // Gốc ở góc trên-trái của canvas lúc World ở giữa; cây ngoài màn có toạ độ âm hoặc vượt 1920×1080.
            Place(garden, TopLeft, TopLeft, new Vector2(WorldInset.x, -WorldInset.y), Vector2.zero);
            var dark = new Color(0.30f, 0.55f, 0.35f);
            var light = new Color(0.40f, 0.66f, 0.42f);
            var trees = new (float X, float Y, float Size, Color Color)[]
            {
                (110, 360, 210, dark), (170, 520, 150, light),
                // Cây và bụi hoa bên phải dời cùng phòng.
                (1500 + RoomShiftX, 330, 240, dark), (1610 + RoomShiftX, 500, 160, light),
                (1510 + RoomShiftX, 660, 120, new Color(0.55f, 0.65f, 0.95f)), (1630 + RoomShiftX, 720, 120, new Color(0.75f, 0.60f, 0.90f)),
                (1530 + RoomShiftX, 850, 130, new Color(0.95f, 0.65f, 0.80f)), (1650 + RoomShiftX, 920, 120, new Color(0.55f, 0.65f, 0.95f)),
                // Vùng World ngoài màn (kéo mới thấy): 5 bên trái, 5 bên phải.
                (-300, 200, 200, dark), (-150, 450, 160, light), (-330, 700, 220, dark), (-120, 950, 150, light), (-250, 1150, 180, dark),
                (2050, 260, 200, dark), (2250, 500, 170, light), (2100, 780, 210, dark), (2300, 1000, 160, light), (2000, 1150, 180, dark),
            };
            for (var i = 0; i < trees.Length; i++)
            {
                // Bóng elip dưới gốc, thân cây (cây lớn) rồi tán; tán giữ tên Plant{i}.
                var shadow = NewRect($"TreeShadow{i}", garden);
                Place(shadow, TopLeft, Center, new Vector2(trees[i].X + 8f, -(trees[i].Y + trees[i].Size * 0.5f)), new Vector2(trees[i].Size, trees[i].Size / 3f));
                AddImage(shadow, knob, new Color(0f, 0f, 0f, 0.18f), raycast: false).type = Image.Type.Simple;
                if (trees[i].Size >= 150f)
                {
                    var trunk = NewRect($"Trunk{i}", garden);
                    Place(trunk, TopLeft, Center, new Vector2(trees[i].X, -(trees[i].Y + trees[i].Size * 0.4f)), new Vector2(trees[i].Size * 0.14f, trees[i].Size * 0.45f));
                    AddImage(trunk, null, WoodDark, raycast: false);
                }

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
                    Block($"Soil{row}_{gx0}", crops, gx0, gx1, gy0, gy1, new Color(0.62f, 0.45f, 0.30f), BedHeight);
                    if (row != 0) continue;
                    var sprout = NewRect($"Sprout{gx0}", crops);
                    Place(sprout, Center, Center, IsoProjection.CellToLocal((gx0 + gx1) / 2f, (gy0 + gy1) / 2f), new Vector2(34, 34));
                    AddImage(sprout, knob, new Color(0.35f, 0.70f, 0.38f), raycast: false).type = Image.Type.Simple;
                }
            }
        }

        // Ô hẹp: chữ co lại thay vì tràn ra ngoài ô.
        private static void FitText(Text text, int min, int max)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = min;
            text.resizeTextMaxSize = max;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        // Thanh chất lượng một hàng (spec UI hub B 5.4), neo trên-trái trong LobbyHud: sao, cảnh báo + nhãn thu nhập, bộ đếm. `lobbyHud` giữ HUD cố định; chữ bay nằm trong `world` để bay đúng chỗ khi quán đã kéo.
        private static RestaurantHud BuildHud(RectTransform world, RectTransform lobbyHud, Font font, Sprite sprite, Sprite knob, RectTransform counter, out Button statsButton)
        {
            var bar = NewRect("InfoBar", lobbyHud);
            Place(bar, TopLeft, TopLeft, new Vector2(180, -108), new Vector2(800, 68));
            // Cả thanh là nút mở QualityPopup (spec 5.4: bấm thẻ sao); con của nó đều tắt raycast nên bấm đâu cũng trúng thanh.
            var barImage = AddImage(bar, sprite, BarColor, raycast: true);
            statsButton = bar.gameObject.AddComponent<Button>();
            statsButton.targetGraphic = barImage;

            // Sao chất lượng: nền xám và lớp vàng Filled ngang chồng lên, vì font dựng sẵn có thể thiếu ký tự sao.
            var stars = new RectTransform[5];
            for (var i = 0; i < stars.Length; i++)
            {
                var slot = NewRect($"Star{i}", bar);
                Place(slot, TopLeft, TopLeft, new Vector2(14 + i * 38, -16), new Vector2(36, 36));
                AddImage(slot, knob, StarOff, raycast: false).type = Image.Type.Simple;
                var fill = NewRect("Fill", slot);
                Stretch(fill);
                var fillImage = AddImage(fill, knob, StarOn, raycast: false);
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillAmount = 0f;
                stars[i] = fill;
            }

            // Bộ đếm dài ("Phục vụ 12 · Bỏ về 2 · Chất lượng 37/100"): ô 290×48 cho chữ co lại và xuống hai dòng.
            var counterText = NewText("CounterText", bar, font, "", 26, TextAnchor.MiddleRight, DarkText);
            Place(counterText.rectTransform, TopRight, TopRight, new Vector2(-14, -10), new Vector2(290, 48));
            FitText(counterText, 14, 26);
            var warning = NewRect("WarningIcon", bar);
            Place(warning, TopLeft, TopLeft, new Vector2(212, -19), new Vector2(30, 30));
            var warningImage = AddImage(warning, null, new Color(0.95f, 0.40f, 0.30f), raycast: false);
            var income = NewText("IncomeText", bar, font, "", 30, TextAnchor.MiddleLeft, DarkText);
            Place(income.rectTransform, TopLeft, TopLeft, new Vector2(250, -10), new Vector2(240, 48));
            FitText(income, 16, 30);

            // Thanh báo lỗi lưu khi thu AFK, ngay dưới thanh chất lượng; RestaurantHud bật 6 giây rồi tắt.
            var messageRoot = NewRect("AfkMessage", lobbyHud);
            Place(messageRoot, TopLeft, TopLeft, new Vector2(180, -186), new Vector2(800, 56));
            AddImage(messageRoot, sprite, BarColor, raycast: false);
            var messageText = NewText("AfkMessageText", messageRoot, font, "", 30, TextAnchor.MiddleCenter, IncidentHintText);
            Stretch(messageText.rectTransform);
            messageRoot.gameObject.SetActive(false);

            var floats = new Text[8];
            for (var i = 0; i < floats.Length; i++)
            {
                floats[i] = NewText($"FloatText{i}", world, font, "", 48, TextAnchor.MiddleCenter, FloatText);
                Place(floats[i].rectTransform, Center, Center, Vector2.zero, new Vector2(200, 60));
                floats[i].gameObject.SetActive(false);
            }

            var hudObject = NewRect("RestaurantHud", (RectTransform)lobbyHud.parent);
            var hud = hudObject.gameObject.AddComponent<RestaurantHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("_incomeText").objectReferenceValue = income;
            so.FindProperty("_counterText").objectReferenceValue = counterText;
            so.FindProperty("_warningIcon").objectReferenceValue = warningImage;
            so.FindProperty("_afkMessageRoot").objectReferenceValue = messageRoot.gameObject;
            so.FindProperty("_afkMessageText").objectReferenceValue = messageText;
            so.FindProperty("_floatOrigin").objectReferenceValue = counter;
            var starProperty = so.FindProperty("_stars");
            starProperty.arraySize = stars.Length;
            for (var i = 0; i < stars.Length; i++) starProperty.GetArrayElementAtIndex(i).objectReferenceValue = stars[i].GetComponent<Image>();
            SetRefs(so.FindProperty("_floatTexts"), floats);
            so.ApplyModifiedPropertiesWithoutUndo();
            return hud;
        }

        // Avatar, banner Gacha và nút tròn Shop khoá (spec UI hub B 5.4). Chỉ dựng nút; HubNav nối cú bấm ở NavBarBuilder.WireLobby.
        private static (Button Avatar, Button Banner, Button Shop) BuildLobbyButtons(RectTransform hud, Font font, Sprite sprite, Sprite knob)
        {
            // Avatar: vòng tròn ngoài và vòng trong nhạt hơn, không chữ (Hồ sơ là đợt C).
            var avatar = NewButton("Avatar_PH_140x140", hud, knob, font, "", AvatarColor, TopLeft, new Vector2(20, -20), new Vector2(140, 140));
            ((Image)avatar.targetGraphic).type = Image.Type.Simple;
            var face = NewRect("AvatarFace_PH_100x100", (RectTransform)avatar.transform);
            Place(face, Center, Center, Vector2.zero, new Vector2(100, 100));
            AddImage(face, knob, AvatarFace, raycast: false).type = Image.Type.Simple;

            var banner = LabelButton("BannerGacha_PH_360x200", hud, sprite, font, "hub.banner_gacha", BannerColor, TopLeft, new Vector2(20, -340), new Vector2(360, 200));
            var bannerText = banner.GetComponentInChildren<Text>();
            bannerText.fontSize = 44;
            bannerText.fontStyle = FontStyle.Bold;
            FitText(bannerText, 24, 44);

            // Shop khoá: nút tròn xám, ổ khoá ở nửa trên, chữ ở nửa dưới.
            var shop = LabelButton("HubRoundBtnShop_PH_110x110", hud, knob, font, "hub.shop", GreyButton, new Vector2(1, 0), new Vector2(-20, 140), new Vector2(110, 110));
            ((Image)shop.targetGraphic).type = Image.Type.Simple;
            var shopText = shop.GetComponentInChildren<Text>();
            shopText.fontSize = 28;
            shopText.rectTransform.offsetMax = new Vector2(0, -60);
            NavBarBuilder.BuildLock((RectTransform)shop.transform, "ShopLock_PH_40x48", new Vector2(0, 20));
            return (avatar, banner, shop);
        }

        // Thanh quán thu gọn (spec UI hub B 5.5): nút mép phải và hộp trắng bo góc chứa ba nút của quán; lúc đầu tắt, RestaurantBarView áp trạng thái đã lưu.
        private static RectTransform BuildRestaurantBar(RectTransform hud, Font font, Sprite sprite, Sprite knob)
        {
            var right = new Vector2(1, 0.5f);
            var toggle = NewButton("RestaurantBarToggle_PH_60x160", hud, sprite, font, "<", ButtonColor, right, Vector2.zero, new Vector2(60, 160));
            var toggleText = toggle.GetComponentInChildren<Text>();
            toggleText.fontSize = 56;
            toggleText.fontStyle = FontStyle.Bold;
            var dot = NewRect("RestaurantBarDot_PH_24x24", (RectTransform)toggle.transform);
            Place(dot, TopLeft, TopLeft, new Vector2(4, -4), new Vector2(24, 24));
            AddImage(dot, knob, IncidentRed, raycast: false).type = Image.Type.Simple;
            dot.gameObject.SetActive(false);

            var bar = NewRect("RestaurantBar_PH_180x460", hud);
            Place(bar, right, right, new Vector2(-60, 0), new Vector2(180, 460));
            AddImage(bar, sprite, CardColor, raycast: false);

            var view = NewRect("RestaurantBarView", hud).gameObject.AddComponent<RestaurantBarView>();
            var so = new SerializedObject(view);
            so.FindProperty("_toggle").objectReferenceValue = toggle;
            so.FindProperty("_toggleText").objectReferenceValue = toggleText;
            so.FindProperty("_bar").objectReferenceValue = bar.gameObject;
            so.FindProperty("_dot").objectReferenceValue = dot.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            bar.gameObject.SetActive(false);
            return bar;
        }

        // Ô thứ ba của thanh quán, cùng cỡ với hai nút trên; RestaurantHud ẩn/hiện theo Coin AFK đang chờ.
        private static void BuildAfkButton(RectTransform column, Font font, Sprite knob, RestaurantHud hud)
        {
            var button = NewButton("AfkButton", column, knob, font, "Thu AFK", AfkGreen, TopCenter, new Vector2(0, -320), new Vector2(130, 130));
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

        // Trang kín màn (spec UI hub 5.5): root phủ 1920×1080 với nền đặc chặn bấm, dưới thanh dưới; hộp nội dung chỉ phủ phần trên thanh
        // (offsetMin.y = BarHeight) nên con của nó neo theo mép trên/dưới không lọt xuống dải 0–120.
        private static (RectTransform Root, RectTransform Box) PageFrame(RectTransform parent, string rootName, string boxName, Color color)
        {
            var root = NewRect(rootName, parent);
            Stretch(root);
            AddImage(root, null, color, raycast: true);
            var box = NewRect(boxName, root);
            Stretch(box);
            box.offsetMin = new Vector2(0, HubNav.BarHeight);
            AddImage(box, null, color, raycast: false);
            return (root, box);
        }

        // Nút Quay lại của trang: góc trên-trái, nhãn hub.back, giữ tên cũ (CloseButton).
        private static Button BackButton(RectTransform box, Font font, Sprite sprite) =>
            LabelButton("CloseButton", box, sprite, font, "hub.back", GreyButton, TopLeft, new Vector2(30, -25), new Vector2(200, 80));

        // Trang Nhân viên: ba cột ô và danh sách nhân vật mở khi bấm ô (trang con, Quay lại về trang Nhân viên).
        private static StaffPanel BuildStaffPanel(RectTransform canvas, RectTransform column, Font font, Sprite sprite, Sprite knob)
        {
            const int slotsPerRole = 3;
            // Nút tròn trong ô đầu của cột phải, cỡ chạm 130.
            var open = LabelButton("StaffButton", column, knob, font, "restaurant.staff", ButtonColor, TopCenter, new Vector2(0, -20), new Vector2(130, 130));
            ((Image)open.targetGraphic).type = Image.Type.Simple;
            open.GetComponentInChildren<Text>().fontSize = 24;

            var (root, box) = PageFrame(canvas, "StaffPanelRoot", "Box", PanelColor);
            var title = NewLabel("TitleText", box, font, "restaurant.staff", 52, TextAnchor.MiddleCenter, TextColor);
            Place(title.rectTransform, TopCenter, TopCenter, new Vector2(0, -25), new Vector2(800, 70));
            var close = BackButton(box, font, sprite);
            // Dải 930–960 (từ trên) giữa hàng ô cuối và vạch 120 px: chỉ đủ một dòng nhỏ.
            var message = NewText("MessageText", box, font, "", 26, TextAnchor.MiddleCenter, Warning);
            Place(message.rectTransform, BottomCenter, BottomCenter, new Vector2(0, 0), new Vector2(1300, 30));

            // Spec UI hub B 5.7: ba cột × ba ô 500×220, tâm cột x 390 / 960 / 1530, hàng từ y 210 / 460 / 710 (tính từ trên).
            var columnTitles = new Text[3];
            var slotButtons = new Button[3 * slotsPerRole];
            var slotTexts = new Text[3 * slotsPerRole];
            var slotInfoTexts = new Text[3 * slotsPerRole];
            var portraits = new Image[3 * slotsPerRole];
            var removeButtons = new Button[3 * slotsPerRole];
            for (var role = 0; role < 3; role++)
            {
                var x = (role - 1) * 570f;
                columnTitles[role] = NewText($"ColumnTitle{role}", box, font, "", 44, TextAnchor.MiddleCenter, Gold);
                Place(columnTitles[role].rectTransform, TopCenter, TopCenter, new Vector2(x, -130), new Vector2(500, 60));
                for (var slot = 0; slot < slotsPerRole; slot++)
                {
                    var index = role * slotsPerRole + slot;
                    var button = NewButton($"Slot{role}_{slot}", box, sprite, font, "", ButtonColor, TopCenter, new Vector2(x, -210 - slot * 250), new Vector2(500, 220));
                    // Viền sáng chỉ bật ở ô trống (StaffPanel.Redraw).
                    var outline = button.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.85f, 0.93f, 1f);
                    outline.effectDistance = new Vector2(4, -4);
                    outline.enabled = false;

                    var portrait = NewRect("Portrait_PH_140x140", button.transform);
                    Place(portrait, TopLeft, TopLeft, new Vector2(20, -40), new Vector2(140, 140));
                    portraits[index] = AddImage(portrait, knob, AvatarColor, raycast: false);
                    portraits[index].type = Image.Type.Simple;

                    slotTexts[index] = button.GetComponentInChildren<Text>();
                    var label = slotTexts[index].rectTransform;
                    Place(label, TopLeft, TopLeft, new Vector2(180, -20), new Vector2(300, 110));
                    slotTexts[index].alignment = TextAnchor.MiddleLeft;
                    slotTexts[index].fontSize = 40;
                    // Tên dài và chữ gợi ý ô khoá co lại cho vừa ô.
                    slotTexts[index].resizeTextForBestFit = true;
                    slotTexts[index].resizeTextMinSize = 20;
                    slotTexts[index].resizeTextMaxSize = 40;
                    slotInfoTexts[index] = NewText("InfoText", button.transform as RectTransform, font, "", 28, TextAnchor.MiddleLeft, Gold);
                    Place(slotInfoTexts[index].rectTransform, TopLeft, TopLeft, new Vector2(180, -130), new Vector2(180, 50));
                    slotButtons[index] = button;
                    removeButtons[index] = LabelButton("RemoveButton", button.transform as RectTransform, sprite, font, "restaurant.remove", GreyButton,
                        new Vector2(1, 0), new Vector2(-15, 15), new Vector2(120, 60));
                    // Bản Anh "Remove" tràn nút 120 px ở cỡ chữ mặc định.
                    removeButtons[index].GetComponentInChildren<Text>().fontSize = 26;
                }
            }

            // Danh sách nhân vật: ScrollRect với Content tự co giãn theo số hàng; hàng mẫu nằm ngoài Content và tắt.
            var (listRoot, listBox) = PageFrame(root, "StaffListPopup", "Box", PanelColor);
            var listTitle = NewText("TitleText", listBox, font, "", 48, TextAnchor.MiddleCenter, TextColor);
            Place(listTitle.rectTransform, TopCenter, TopCenter, new Vector2(0, -25), new Vector2(1100, 70));
            var scrollRoot = NewRect("ScrollView", listBox);
            Place(scrollRoot, Center, Center, new Vector2(0, -30), new Vector2(1500, 720));
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
            var listClose = BackButton(listBox, font, sprite);
            listRoot.gameObject.SetActive(false);

            var confirmRoot = NewRect("StaffConfirmPopup", root);
            Stretch(confirmRoot);
            AddImage(confirmRoot, null, Dim, raycast: true);
            NavBarBuilder.OverBar(confirmRoot);
            var confirmBox = NewRect("Box", confirmRoot);
            Place(confirmBox, Center, Center, Vector2.zero, StaffConfirmSize);
            AddImage(confirmBox, sprite, PanelColor, raycast: false);
            var confirmText = NewText("ConfirmText", confirmBox, font, "", 40, TextAnchor.MiddleCenter, TextColor);
            Place(confirmText.rectTransform, TopCenter, TopCenter, new Vector2(0, -40), new Vector2(820, 190));
            var yes = LabelButton("YesButton", confirmBox, sprite, font, "common.move", ButtonColor, BottomCenter, new Vector2(-200, 40), new Vector2(340, 90));
            var no = LabelButton("NoButton", confirmBox, sprite, font, "common.not_now", GreyButton, BottomCenter, new Vector2(200, 40), new Vector2(340, 90));
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
            SetRefs(so.FindProperty("_slotInfoTexts"), slotInfoTexts);
            SetRefs(so.FindProperty("_portraits"), portraits);
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
            return panel;
        }

        // Trang Nâng cấp (spec UI hub B 5.7): mỗi nâng cấp một thẻ do UpgradeTestPanel dựng từ thẻ mẫu vào lưới 3×2.
        private static UpgradeTestPanel BuildUpgradePanel(RectTransform canvas, RectTransform column, Font font, Sprite sprite, Sprite knob)
        {
            // Ô thứ hai của thanh quán, cùng cỡ với nút Nhân viên.
            var open = LabelButton("UpgradeButton", column, knob, font, "restaurant.upgrade", ButtonColor, TopCenter, new Vector2(0, -170), new Vector2(130, 130));
            ((Image)open.targetGraphic).type = Image.Type.Simple;
            open.GetComponentInChildren<Text>().fontSize = 24;

            var (root, box) = PageFrame(canvas, "UpgradePanelRoot", "Box", PanelColor);
            var title = NewLabel("TitleText", box, font, "restaurant.upgrade_title", 52, TextAnchor.MiddleCenter, TextColor);
            Place(title.rectTransform, TopCenter, TopCenter, new Vector2(0, -25), new Vector2(700, 70));
            var close = BackButton(box, font, sprite);
            // Cạnh dưới lưới là y 910 (từ trên), còn 50 px tới vạch cho dòng báo.
            var message = NewText("MessageText", box, font, "", 30, TextAnchor.MiddleCenter, Warning);
            Place(message.rectTransform, BottomCenter, BottomCenter, new Vector2(0, 5), new Vector2(1400, 40));

            // Spec UI hub B 5.7: lưới 3 cột × 2 hàng thẻ 540×360 (cột trái x 120 / 690 / 1260, hàng y 150 / 550 từ trên).
            // shortcut: lưới 3×2 cố định cho 6 nâng cấp, thêm cái thứ 7 thì làm cuộn.
            var cardSlots = new RectTransform[6];
            for (var i = 0; i < cardSlots.Length; i++)
            {
                cardSlots[i] = NewRect($"CardSlot{i}", box);
                Place(cardSlots[i], TopLeft, TopLeft, new Vector2(120 + i % 3 * 570, -(150 + i / 3 * 400)), new Vector2(540, 360));
            }

            // Thẻ mẫu nằm ngoài lưới và tắt; UpgradeTestPanel nhân bản vào từng chỗ. Thẻ không phải Button, chỉ nút Mua bấm được.
            var template = NewRect("UpgradeCard", box);
            Place(template, Center, Center, Vector2.zero, new Vector2(540, 360));
            AddImage(template, sprite, new Color(0.22f, 0.36f, 0.62f), raycast: false);
            var nameText = NewText("NameText", template, font, "", 42, TextAnchor.MiddleLeft, Color.white);
            Place(nameText.rectTransform, TopLeft, TopLeft, new Vector2(20, -20), new Vector2(500, 60));
            var levelText = NewText("LevelText", template, font, "", 32, TextAnchor.MiddleLeft, Gold);
            Place(levelText.rectTransform, TopLeft, TopLeft, new Vector2(20, -90), new Vector2(500, 44));
            var costText = NewText("CostText", template, font, "", 30, TextAnchor.UpperLeft, Color.white);
            Place(costText.rectTransform, TopLeft, TopLeft, new Vector2(20, -145), new Vector2(500, 100));
            var buy = LabelButton("BuyButton_PH_210x80", template, sprite, font, "restaurant.buy", AfkGreen, new Vector2(1, 0), new Vector2(-20, 20), new Vector2(210, 80));
            var templateView = template.gameObject.AddComponent<UpgradeCardView>();
            var cardSo = new SerializedObject(templateView);
            cardSo.FindProperty("_nameText").objectReferenceValue = nameText;
            cardSo.FindProperty("_levelText").objectReferenceValue = levelText;
            cardSo.FindProperty("_costText").objectReferenceValue = costText;
            cardSo.FindProperty("_buyButton").objectReferenceValue = buy;
            cardSo.ApplyModifiedPropertiesWithoutUndo();
            template.gameObject.SetActive(false);

            root.gameObject.SetActive(false);

            var panelObject = NewRect("UpgradeTestPanel", canvas);
            var panel = panelObject.gameObject.AddComponent<UpgradeTestPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_openButton").objectReferenceValue = open;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_messageText").objectReferenceValue = message;
            SetRefs(so.FindProperty("_cardSlots"), cardSlots);
            so.FindProperty("_cardTemplate").objectReferenceValue = templateView;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        // Nút có nhãn cố định theo key: gắn LocalizedText vào Text con của nút.
        private static Button LabelButton(string name, RectTransform parent, Sprite sprite, Font font, string key, Color color,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            var button = NewButton(name, parent, sprite, font, UiText.Get(key), color, anchor, position, size);
            button.GetComponentInChildren<Text>().gameObject.AddComponent<LocalizedText>().SetKey(key);
            return button;
        }

        // Một dòng chữ căn giữa của popup, đặt theo mép trên của hộp.
        private static Text PopupLine(RectTransform box, Font font, string name, string text, int size, Color color, float y, float width, float height) =>
            PlaceLine(NewText(name, box, font, text, size, TextAnchor.MiddleCenter, color), y, width, height);

        // Như PopupLine nhưng là nhãn cố định: chữ theo key và đổi theo ngôn ngữ.
        private static Text PopupLabel(RectTransform box, Font font, string name, string key, int size, Color color, float y, float width, float height) =>
            PlaceLine(NewLabel(name, box, font, key, size, TextAnchor.MiddleCenter, color), y, width, height);

        private static Text PlaceLine(Text line, float y, float width, float height)
        {
            Place(line.rectTransform, TopCenter, TopCenter, new Vector2(0, y), new Vector2(width, height));
            return line;
        }

        // Thẻ trắng giữa màn trên nền tối mờ, chữ tối (bố cục sáng của màn quán); trả về gốc và hộp.
        private static (RectTransform Root, RectTransform Box) PopupFrame(RectTransform canvas, Sprite sprite, string rootName, string boxName, Vector2 size)
        {
            var root = NewRect(rootName, canvas);
            Stretch(root);
            AddImage(root, null, Dim, raycast: true);
            var box = NewRect(boxName, root);
            Place(box, Center, Center, Vector2.zero, size);
            AddImage(box, sprite, CardColor, raycast: false);
            return (root, box);
        }

        // Popup Thu AFK (spec 5.4): vắng bao lâu, Coin chờ, chất lượng, hệ số, Coin nhận, mẹo, Thu thập / Để sau.
        private static AfkPopup BuildAfkPopup(RectTransform canvas, Font font, Sprite sprite)
        {
            var (root, box) = PopupFrame(canvas, sprite, "AfkPopupRoot", "AfkPopupBox", AfkPopupSize);
            var title = PopupLabel(box, font, "TitleText", "restaurant.afk_title", 54, DarkText, -30, 800, 70);
            title.fontStyle = FontStyle.Bold;
            var summary = PopupLine(box, font, "SummaryText", "", 36, DarkText, -125, 960, 260);
            summary.alignment = TextAnchor.UpperLeft;
            var tip = PopupLine(box, font, "TipText", "", 30, SoftText, -395, 1000, 50);
            var full = PopupLine(box, font, "FullText", "", 34, FullText, -450, 1000, 48);
            var message = PopupLine(box, font, "MessageText", "", 30, IncidentHintText, -505, 1000, 48);
            var collect = LabelButton("CollectButton", box, sprite, font, "restaurant.collect", AfkGreen, BottomCenter, new Vector2(-200, 40), new Vector2(340, 100));
            var later = NewButton("LaterButton", box, sprite, font, UiText.Get("restaurant.later"), GreyButton, BottomCenter, new Vector2(200, 40), new Vector2(340, 100));
            NavBarBuilder.OverBar(root);
            root.gameObject.SetActive(false);

            var popup = NewRect("AfkPopup", canvas).gameObject.AddComponent<AfkPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_summaryText").objectReferenceValue = summary;
            so.FindProperty("_tipText").objectReferenceValue = tip;
            so.FindProperty("_fullText").objectReferenceValue = full;
            so.FindProperty("_messageText").objectReferenceValue = message;
            so.FindProperty("_collectButton").objectReferenceValue = collect;
            so.FindProperty("_laterButton").objectReferenceValue = later;
            so.FindProperty("_laterLabel").objectReferenceValue = later.GetComponentInChildren<Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
        }

        // Trang Chất lượng (spec 5.4), mở bằng thanh sao ở HUD: Chuyên cần với 7 ô ngày, dòng hôm nay, Xử lý sự cố, hệ số AFK. Giữ nền sáng của màn quán.
        private static QualityPopup BuildQualityPopup(RectTransform canvas, Font font, Sprite sprite, Button open)
        {
            const int days = 7;
            var (root, box) = PageFrame(canvas, "QualityPopupRoot", "QualityPopupBox", Color.white);
            var title = PopupLabel(box, font, "TitleText", "restaurant.quality_title", 54, DarkText, -28, 800, 70);
            title.fontStyle = FontStyle.Bold;
            var headline = PopupLine(box, font, "HeadlineText", "", 40, DarkText, -100, 1100, 56);
            var attendance = PopupLine(box, font, "AttendanceText", "", 38, DarkText, -170, 1100, 56);

            // Ô ngày: nền xám và lớp vàng Filled ngang chồng lên, như sao ở HUD.
            var fills = new Image[days];
            var labels = new Text[days];
            for (var i = 0; i < days; i++)
            {
                var cell = NewRect($"DayCell{i}", box);
                Place(cell, TopCenter, TopCenter, new Vector2((i - 3) * 160, -240), new Vector2(140, 44));
                AddImage(cell, sprite, StarOff, raycast: false);
                var fillRect = NewRect("Fill", cell);
                Stretch(fillRect);
                fills[i] = AddImage(fillRect, sprite, StarOn, raycast: false);
                fills[i].type = Image.Type.Filled;
                fills[i].fillMethod = Image.FillMethod.Horizontal;
                fills[i].fillAmount = 0f;
                labels[i] = NewText($"DayLabel{i}", box, font, "", 24, TextAnchor.MiddleCenter, SoftText);
                Place(labels[i].rectTransform, TopCenter, TopCenter, new Vector2((i - 3) * 160, -290), new Vector2(150, 34));
            }

            var today = PopupLine(box, font, "TodayText", "", 30, SoftText, -335, 1100, 44);
            var incident = PopupLine(box, font, "IncidentText", "", 38, DarkText, -405, 1100, 56);
            var breakdown = PopupLine(box, font, "BreakdownText", "", 32, SoftText, -465, 1100, 44);
            var factor = PopupLine(box, font, "FactorText", "", 38, DarkText, -540, 1100, 56);
            var close = BackButton(box, font, sprite);
            root.gameObject.SetActive(false);

            var popup = NewRect("QualityPopup", canvas).gameObject.AddComponent<QualityPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("_openButton").objectReferenceValue = open;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_headlineText").objectReferenceValue = headline;
            so.FindProperty("_attendanceText").objectReferenceValue = attendance;
            SetRefs(so.FindProperty("_dayFills"), fills);
            SetRefs(so.FindProperty("_dayLabels"), labels);
            so.FindProperty("_todayText").objectReferenceValue = today;
            so.FindProperty("_incidentText").objectReferenceValue = incident;
            so.FindProperty("_breakdownText").objectReferenceValue = breakdown;
            so.FindProperty("_factorText").objectReferenceValue = factor;
            so.ApplyModifiedPropertiesWithoutUndo();
            return popup;
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
