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
    // Dựng scene Restaurant bằng code, cùng khuôn GachaSceneBuilder. Hình trong Canvas đều là Image: sàn và đồ vật là hình vuông đặc
    // (không gán sprite), người là Knob, nút là UISprite. Chữ là Text của uGUI (docs dự án thắng skill ui-ugui, xem GachaSceneBuilder).
    // Bản đầu không có tường: ghép hình bình hành rườm rà so với giá trị khi thử (spec 5.1 cho phép bỏ).
    internal static class RestaurantSceneBuilder
    {
        // Cạnh ô lưới trước khi xoay 45° và nén y một nửa: 60 × √2, để ô trên màn hình đúng 120×60.
        private const float CellSide = 84.8528f;
        private const int GridSize = 8;
        private const float RoomScale = 1.5f;

        private static readonly Color Background = new(0.10f, 0.11f, 0.15f);
        private static readonly Color FloorRed = new(0.85f, 0.42f, 0.45f);
        private static readonly Color FloorBlue = new(0.36f, 0.55f, 0.80f);
        private static readonly Color BarColor = new(0f, 0f, 0f, 0.55f);
        private static readonly Color TextColor = Color.white;
        private static readonly Color PaleText = new(0.85f, 0.88f, 0.95f);
        private static readonly Color Gold = new(1f, 0.85f, 0.4f);

        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new(0, 1);
        private static readonly Vector2 TopRight = new(1, 1);

        // Ô lưới (gx, gy) của điểm mốc; gx đi xuống bên phải, gy đi xuống bên trái.
        private static readonly Vector2[] TableCells =
        {
            new(3, 2), new(3, 4), new(3, 6), new(5, 2), new(5, 4), new(5, 6), new(7, 2), new(7, 4),
        };

        private static readonly Vector2[] CookCells = { new(1, 2), new(1, 3), new(1, 4) };
        private static readonly Vector2[] ServerCells = { new(1, 5), new(1, 6), new(1, 7) };
        private static readonly Vector2[] CashierCells = { new(2, 1), new(3, 1), new(4, 1) };
        private static readonly Vector2 DoorCell = new(7, 7);

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

            var back = NewRect("Background", canvas);
            Stretch(back);
            AddImage(back, null, Background, raycast: false);

            var room = NewRect("Room", canvas);
            Place(room, Center, Center, new Vector2(0, 280), Vector2.zero);
            room.localScale = Vector3.one * RoomScale;
            BuildFloor(room);

            // Đồ vật vẽ theo độ sâu: gần người xem hơn thì vẽ sau.
            var furniture = NewRect("Furniture", room);
            Place(furniture, Center, Center, Vector2.zero, Vector2.zero);
            var tables = new RectTransform[TableCells.Length];
            RectTransform counter = null;
            var pieces = new List<(float Depth, Action Build)>
            {
                (IsoProjection.DepthKey(0, 3), () => Box("Kitchen", furniture, new Vector2(0, 3), new Vector2(260, 80), new Color(0.55f, 0.35f, 0.25f))),
                (IsoProjection.DepthKey(3, 0), () => counter = Box("Counter", furniture, new Vector2(3, 0), new Vector2(260, 80), new Color(0.45f, 0.30f, 0.50f))),
            };
            for (var i = 0; i < TableCells.Length; i++)
            {
                var index = i;
                pieces.Add((IsoProjection.DepthKey(TableCells[i].x, TableCells[i].y),
                    () => tables[index] = Box($"Table{index}", furniture, TableCells[index], new Vector2(110, 60), new Color(0.80f, 0.65f, 0.40f))));
            }

            pieces.Sort((a, b) => a.Depth.CompareTo(b.Depth));
            foreach (var piece in pieces) piece.Build();

            var door = Box("Door", furniture, DoorCell, new Vector2(120, 30), new Color(0.25f, 0.20f, 0.18f));
            var landmarks = NewRect("Landmarks", room);
            Place(landmarks, Center, Center, Vector2.zero, Vector2.zero);
            var cookSpots = Spots("Cook", landmarks, CookCells);
            var serverSpots = Spots("Server", landmarks, ServerCells);
            var cashierSpots = Spots("Cashier", landmarks, CashierCells);

            // Mẫu để RestaurantView nhân bản; nằm dưới đối tượng tắt nên không hiện.
            var templates = NewRect("Templates", room);
            Place(templates, Center, Center, Vector2.zero, Vector2.zero);
            var customerTemplate = NewRect("CustomerTemplate", templates);
            Place(customerTemplate, Center, Center, Vector2.zero, new Vector2(44, 44));
            var customerImage = AddImage(customerTemplate, knob, Color.white, raycast: false);
            customerImage.type = Image.Type.Simple;
            var workerTemplate = NewRect("WorkerTemplate", templates);
            Place(workerTemplate, Center, Center, Vector2.zero, new Vector2(56, 56));
            var workerImage = AddImage(workerTemplate, knob, Color.white, raycast: false);
            workerImage.type = Image.Type.Simple;
            var workerLabel = NewText("NameText", workerTemplate, font, "", 22, TextAnchor.MiddleCenter, TextColor);
            Place(workerLabel.rectTransform, Center, Center, new Vector2(0, 46), new Vector2(200, 30));
            templates.gameObject.SetActive(false);

            var actors = NewRect("Actors", room);
            Place(actors, Center, Center, Vector2.zero, Vector2.zero);

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
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            BuildHud(canvas, font, knob, counter);

            EditorSceneManager.SaveScene(scene, scenePath);
            if (mode == NewSceneMode.Additive) EditorSceneManager.CloseScene(scene, true);
        }

        // Cha nén y 0,5 chứa cha xoay 45° chứa 64 ô vuông: không phải tính vị trí từng ô theo phép chiếu.
        private static void BuildFloor(RectTransform room)
        {
            var squash = NewRect("FloorSquash", room);
            Place(squash, Center, Center, Vector2.zero, Vector2.zero);
            squash.localScale = new Vector3(1f, 0.5f, 1f);
            var rotated = NewRect("FloorRotation", squash);
            Place(rotated, Center, Center, Vector2.zero, Vector2.zero);
            rotated.localRotation = Quaternion.Euler(0f, 0f, 45f);

            for (var gx = 0; gx < GridSize; gx++)
            {
                for (var gy = 0; gy < GridSize; gy++)
                {
                    var cell = NewRect($"Cell_{gx}_{gy}", rotated);
                    // Xoay +45° rồi nén y một nửa đưa (-gy, -gx) × cạnh ô về đúng IsoProjection.CellToLocal(gx, gy).
                    Place(cell, Center, Center, new Vector2(-CellSide * gy, -CellSide * gx), new Vector2(CellSide, CellSide));
                    AddImage(cell, null, (gx + gy) % 2 == 0 ? FloorRed : FloorBlue, raycast: false);
                }
            }
        }

        private static void BuildHud(RectTransform canvas, Font font, Sprite knob, RectTransform counter)
        {
            var bar = NewRect("TopBar", canvas);
            Place(bar, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 150));
            bar.anchorMin = new Vector2(0, 1);
            bar.anchorMax = new Vector2(1, 1);
            bar.sizeDelta = new Vector2(0, 150);
            AddImage(bar, null, BarColor, raycast: false);

            var coin = NewText("CoinText", canvas, font, "Coin 0", 44, TextAnchor.MiddleLeft, Gold);
            Place(coin.rectTransform, TopLeft, TopLeft, new Vector2(40, -15), new Vector2(420, 60));
            var material = NewText("MaterialText", canvas, font, "Material 0", 44, TextAnchor.MiddleLeft, TextColor);
            Place(material.rectTransform, TopLeft, TopLeft, new Vector2(480, -15), new Vector2(420, 60));
            var gem = NewText("GemText", canvas, font, "Gem 0", 44, TextAnchor.MiddleLeft, TextColor);
            Place(gem.rectTransform, TopLeft, TopLeft, new Vector2(920, -15), new Vector2(420, 60));

            var warning = NewRect("WarningIcon", canvas);
            Place(warning, TopLeft, TopLeft, new Vector2(40, -90), new Vector2(36, 36));
            var warningImage = AddImage(warning, null, new Color(1f, 0.45f, 0.35f), raycast: false);
            var income = NewText("IncomeText", canvas, font, "", 36, TextAnchor.MiddleLeft, PaleText);
            Place(income.rectTransform, TopLeft, TopLeft, new Vector2(92, -82), new Vector2(1000, 50));
            var counterText = NewText("CounterText", canvas, font, "", 36, TextAnchor.MiddleRight, PaleText);
            Place(counterText.rectTransform, TopRight, TopRight, new Vector2(-40, -82), new Vector2(700, 50));

            // Sao chất lượng: nền xám và lớp vàng Filled ngang chồng lên, vì font dựng sẵn có thể thiếu ký tự sao.
            var stars = new RectTransform[5];
            for (var i = 0; i < stars.Length; i++)
            {
                var slot = NewRect($"Star{i}", canvas);
                Place(slot, TopRight, TopRight, new Vector2(-40 - (4 - i) * 56, -20), new Vector2(48, 48));
                var empty = AddImage(slot, knob, new Color(0.35f, 0.37f, 0.45f), raycast: false);
                empty.type = Image.Type.Simple;
                var fill = NewRect("Fill", slot);
                Stretch(fill);
                var fillImage = AddImage(fill, knob, Gold, raycast: false);
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillAmount = 0f;
                stars[i] = fill;
            }

            var floats = new Text[8];
            for (var i = 0; i < floats.Length; i++)
            {
                floats[i] = NewText($"FloatText{i}", canvas, font, "", 44, TextAnchor.MiddleCenter, Gold);
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
        }

        private static RectTransform Box(string name, RectTransform parent, Vector2 cell, Vector2 size, Color color)
        {
            var rect = NewRect(name, parent);
            Place(rect, Center, Center, IsoProjection.CellToLocal(cell.x, cell.y), size);
            AddImage(rect, null, color, raycast: false);
            return rect;
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
