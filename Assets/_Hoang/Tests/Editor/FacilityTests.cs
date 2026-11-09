using System;
using System.Collections.Generic;
using Hoang.Remake;
using NUnit.Framework;
using UnityEngine;

namespace Hoang.Remake.Tests
{
    public sealed class FacilityTests
    {
        private readonly List<FacilityItemSO> _items = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _items) UnityEngine.Object.DestroyImmediate(item);
            _items.Clear();
        }

        [Test]
        public void Rotation_NormalizesEveryOrientation_AndFourTurnsRestoreShape()
        {
            var item = Item("L");
            item.ShapeCoords = new List<Vector2Int> { new(2, 3), new(3, 3), new(2, 4) };
            var expected = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
            CollectionAssert.AreEquivalent(expected, item.GetRotatedCoords(0));
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 1) }, item.GetRotatedCoords(1));
            CollectionAssert.AreEquivalent(item.GetRotatedCoords(3), item.GetRotatedCoords(-1));
            CollectionAssert.AreEquivalent(expected, item.GetRotatedCoords(4));
        }

        [Test]
        public void Placement_RejectsOverlapBoundsAndEmptyShape_AndFailedMoveKeepsCells()
        {
            var item = Item("wide");
            item.ShapeCoords.Add(Vector2Int.right);
            var grid = new FacilityGrid();
            var placed = new FacilityPlacedItem("one", item, new Vector2Int(9, 0), 0);
            Assert.IsTrue(grid.PlaceItem(placed));
            Assert.IsFalse(grid.CanPlaceItem(item, new Vector2Int(10, 0), 0));
            Assert.IsFalse(grid.CanPlaceItem(item, new Vector2Int(8, 0), 0));
            Assert.IsFalse(grid.TryReposition("one", new Vector2Int(10, 0), 0));
            Assert.AreSame(placed, grid.GetItemAt(new Vector2Int(9, 0)));
            Assert.AreSame(placed, grid.GetItemAt(new Vector2Int(10, 0)));
            item.ShapeCoords.Clear();
            Assert.IsFalse(grid.CanPlaceItem(item, Vector2Int.zero, 0));
        }

        [Test]
        public void Power_RequiresConductivePathAndRelay_AndCountsEachItemOnce()
        {
            var source = Item("source", source: true, output: 100);
            var wire = Item("wire", consumption: 10);
            wire.ShapeCoords.Add(Vector2Int.right);
            var heavy = Item("heavy", consumption: 40);
            heavy.RequiresRelay = true;
            var relay = Item("relay", consumption: 5);
            relay.IsRelay = true;
            var grid = new FacilityGrid();
            Assert.IsTrue(grid.PlaceItem(new FacilityPlacedItem("s", source, Vector2Int.zero, 0)));
            Assert.IsTrue(grid.PlaceItem(new FacilityPlacedItem("w", wire, Vector2Int.right, 0)));
            var load = new FacilityPlacedItem("h", heavy, new Vector2Int(3, 0), 0);
            Assert.IsTrue(grid.PlaceItem(load));
            Assert.IsFalse(load.IsPowered, "A normal conductor cannot power a relay-required device.");
            Assert.AreEqual(10, grid.CurrentPowerUsed, "A multi-cell device consumes power once.");
            Assert.IsTrue(grid.PlaceItem(new FacilityPlacedItem("r", relay, new Vector2Int(3, 1), 0)));
            Assert.IsFalse(load.IsPowered, "The relay itself is not connected yet.");
            Assert.IsTrue(grid.PlaceItem(new FacilityPlacedItem("w2", wire, new Vector2Int(1, 1), 0)));
            Assert.IsTrue(load.IsPowered);
            Assert.AreEqual(65, grid.CurrentPowerUsed);
        }

        [Test]
        public void Power_SourceCanDirectlySupplyRelayRequiredDevice_ButNonConductorStopsChain()
        {
            var source = Item("source", source: true, output: 100);
            var end = Item("end", consumption: 20);
            end.RequiresRelay = true;
            end.CanConductPower = false;
            var grid = new FacilityGrid();
            grid.PlaceItem(new FacilityPlacedItem("s", source, Vector2Int.zero, 0));
            var powered = new FacilityPlacedItem("a", end, Vector2Int.right, 0);
            var disconnected = new FacilityPlacedItem("b", Item("consumer"), new Vector2Int(2, 0), 0);
            grid.PlaceItem(powered);
            grid.PlaceItem(disconnected);
            Assert.IsTrue(powered.IsPowered);
            Assert.IsFalse(disconnected.IsPowered);
            Assert.AreEqual(20, grid.CurrentPowerUsed);
        }

        [Test]
        public void Overload_IsDeterministic_AndRemovingLoadRestoresPower()
        {
            var source = Item("source", source: true, output: 30);
            var load = Item("load", consumption: 20);
            var grid = new FacilityGrid();
            var north = new FacilityPlacedItem("north", load, Vector2Int.up, 0);
            var east = new FacilityPlacedItem("east", load, Vector2Int.right, 0);
            grid.PlaceItem(east);
            grid.PlaceItem(north);
            grid.PlaceItem(new FacilityPlacedItem("source", source, Vector2Int.zero, 0));
            Assert.IsTrue(north.IsPowered, "BFS visits up before right, independent of placement order.");
            Assert.IsFalse(east.IsPowered);
            Assert.AreEqual(20, grid.CurrentPowerUsed);
            Assert.IsTrue(grid.RemoveItem(north));
            Assert.IsTrue(east.IsPowered);
            Assert.AreEqual(30, grid.TotalPowerCapacity);
        }

        [Test]
        public void Manager_PersistsDuplicatesWithStableInstances_AndFailedWritesChangeNothing()
        {
            var item = Item("oven");
            var save = new HoangSaveData();
            save.Facilities.InventoryItemIds.AddRange(new[] { "oven", "oven" });
            var canWrite = true;
            var writes = 0;
            var context = new RemakeContext(save, candidate => { writes++; return canWrite; });
            using var manager = new FacilityManager(context, new[] { item });
            Assert.IsTrue(manager.TryPlaceFromInventory("oven", Vector2Int.zero, 0));
            var id = manager.Grid.PlacedItems[0].InstanceId;
            Assert.IsTrue(manager.TryPlaceFromInventory("oven", new Vector2Int(2, 0), 0));
            Assert.AreNotEqual(id, manager.Grid.PlacedItems[1].InstanceId);
            Assert.AreEqual(0, manager.InventoryItemIds.Count);
            canWrite = false;
            Assert.IsFalse(manager.TryRemoveToInventory(id));
            Assert.IsFalse(manager.TryRepositionPlacedItem(id, new Vector2Int(5, 5), 1));
            Assert.IsFalse(manager.ClearAllToInventory());
            Assert.AreEqual(2, manager.Grid.PlacedItems.Count);
            Assert.AreEqual(Vector2Int.zero, manager.Grid.GetPlacedItem(id).OriginCoord);
            Assert.AreEqual(0, manager.InventoryItemIds.Count);
            canWrite = true;
            Assert.IsTrue(manager.TryRemoveToInventory(id));
            Assert.AreEqual(1, manager.InventoryItemIds.Count);
            Assert.IsTrue(manager.ClearAllToInventory());
            Assert.AreEqual(2, manager.InventoryItemIds.Count);
            Assert.AreEqual(0, manager.Grid.PlacedItems.Count);
            Assert.AreEqual(7, writes);
        }

        [Test]
        public void Manager_OnlyPoweredItemsGrantBuffs_AndStateRoundTrips()
        {
            var source = Item("source", source: true, output: 50);
            var oven = Item("oven", consumption: 20);
            oven.Stats = new FacilityStats { ProductionSpeedBonus = 0.2f, CleanlinessBonus = 3 };
            var save = new HoangSaveData();
            save.Facilities.InventoryItemIds.AddRange(new[] { "source", "oven", "oven" });
            var context = new RemakeContext(save, candidate => true);
            using var manager = new FacilityManager(context, new[] { source, oven });
            Assert.IsTrue(manager.TryPlaceFromInventory("source", Vector2Int.zero, 0));
            Assert.IsTrue(manager.TryPlaceFromInventory("oven", Vector2Int.right, 0));
            Assert.IsTrue(manager.TryPlaceFromInventory("oven", new Vector2Int(8, 8), 0));
            Assert.AreEqual(0.2f, manager.CurrentTotalStats.ProductionSpeedBonus, 0.0001f);
            Assert.AreEqual(3, manager.CurrentTotalStats.CleanlinessBonus);
            var restored = JsonUtility.FromJson<HoangSaveData>(JsonUtility.ToJson(context.Save));
            using var loaded = new FacilityManager(new RemakeContext(restored, candidate => true), new[] { source, oven });
            Assert.AreEqual(3, loaded.Grid.PlacedItems.Count);
            Assert.AreEqual(20, loaded.CurrentPowerUsed);
            Assert.AreEqual(manager.Grid.PlacedItems[1].InstanceId, loaded.Grid.PlacedItems[1].InstanceId);
        }

        [Test]
        public void DefinitionValidation_RejectsDuplicateCellsNegativePowerAndDuplicateIds()
        {
            var item = Item("bad");
            item.ShapeCoords.Add(Vector2Int.zero);
            Assert.IsNotEmpty(item.Validate());
            item.ShapeCoords.RemoveAt(1);
            item.PowerConsumption = -1;
            Assert.IsNotEmpty(item.Validate());
            item.PowerConsumption = 0;
            Assert.Throws<ArgumentException>(() => new FacilityManager(new RemakeContext(new HoangSaveData(), candidate => true), new[] { item, item }));
            item.ShapeCoords = new List<Vector2Int> { new(int.MinValue, 0), new(int.MaxValue, 0) };
            Assert.IsNotEmpty(item.Validate());
            Assert.IsFalse(new FacilityGrid().CanPlaceItem(item, Vector2Int.zero, 0));
        }

        [Test]
        public void FacilityView_ReusesPlacedObjectWhenLayoutChanges_AndCreatesAllGridCells()
        {
            var canvas = new GameObject("FacilityTestCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var item = Item("oven");
                var save = new HoangSaveData();
                save.Facilities.InventoryItemIds.Add("oven");
                var context = new RemakeContext(save, candidate => true);
                using var manager = new FacilityManager(context, new[] { item });
                var view = FacilityGridUI.Create(canvas.transform);
                view.Bind(manager);
                Assert.AreEqual(121, CountGridCells(view.GridRoot));
                Assert.IsTrue(manager.TryPlaceFromInventory("oven", Vector2Int.zero, 0));
                var before = view.GetComponentInChildren<FacilityPlacedItemUI>();
                Assert.IsNotNull(before);
                Assert.IsTrue(manager.TryRepositionPlacedItem(manager.Grid.PlacedItems[0].InstanceId, new Vector2Int(3, 4), 1));
                Assert.AreSame(before, view.GetComponentInChildren<FacilityPlacedItemUI>());
                Assert.AreEqual(1, view.PlacedViewCount);
                view.gameObject.SetActive(false);
                view.gameObject.SetActive(true);
                Assert.AreEqual(1, view.PlacedViewCount);
            }
            finally { UnityEngine.Object.DestroyImmediate(canvas); }
        }

        [Test]
        public void FacilityView_EmptyInventoryCannotBeSelectedOrShowValidPreview()
        {
            var canvas = new GameObject("FacilityTestCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var item = Item("oven");
                var context = new RemakeContext(new HoangSaveData(), candidate => true);
                using var manager = new FacilityManager(context, new[] { item });
                var view = FacilityGridUI.Create(canvas.transform);
                view.Bind(manager);
                view.Select("oven");
                Assert.IsFalse(view.transform.Find("Rotate").GetComponent<UnityEngine.UI.Button>().interactable);
                var preview = view.GridRoot.Find("PlacementPreview");
                Assert.AreEqual(0, preview.childCount, "Selecting unavailable inventory must not create a green placement preview.");
                Assert.IsTrue(context.Commit(save => save.Facilities.InventoryItemIds.Add("oven")));
                view.Select("oven");
                Assert.IsTrue(view.transform.Find("Rotate").GetComponent<UnityEngine.UI.Button>().interactable,
                    "The same row must become selectable after inventory is replenished.");
            }
            finally { UnityEngine.Object.DestroyImmediate(canvas); }
        }

        [Test]
        public void FacilityView_AuthoredInventoryRowsSurviveSerializationAndAreReusedOnBind()
        {
            var canvas = new GameObject("FacilityTestCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject cloned = null;
            try
            {
                var item = Item("oven");
                var view = FacilityGridUI.Create(canvas.transform);
                view.Configure(new[] { item });
                var originalRow = view.transform.Find("Inventory/Inventory_oven");
                Assert.IsNotNull(originalRow, "Configure must author inventory in Edit Mode.");
                view.Configure(new[] { item });
                Assert.AreEqual(1, view.transform.Find("Inventory").childCount);
                cloned = UnityEngine.Object.Instantiate(view.gameObject, canvas.transform);
                var restored = cloned.GetComponent<FacilityGridUI>();
                var authoredRow = restored.transform.Find("Inventory/Inventory_oven");
                var save = new HoangSaveData();
                save.Facilities.InventoryItemIds.Add("oven");
                using var manager = new FacilityManager(new RemakeContext(save, candidate => true), new[] { item });
                restored.Bind(manager);
                Assert.AreEqual(1, restored.transform.Find("Inventory").childCount, "Binding a deserialized view must not add duplicate rows.");
                Assert.AreSame(authoredRow, restored.transform.Find("Inventory/Inventory_oven"));
                StringAssert.Contains("x1", authoredRow.GetComponentInChildren<UnityEngine.UI.Text>().text);
            }
            finally
            {
                if (cloned != null) UnityEngine.Object.DestroyImmediate(cloned);
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        private static int CountGridCells(Transform parent)
        {
            var count = 0;
            for (var i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name.StartsWith("Cell_", StringComparison.Ordinal)) count++;
            return count;
        }

        private FacilityItemSO Item(string id, bool source = false, int output = 0, int consumption = 10)
        {
            var item = ScriptableObject.CreateInstance<FacilityItemSO>();
            item.Id = id;
            item.ItemName = id;
            item.IsPowerSource = source;
            item.PowerOutput = output;
            item.PowerConsumption = source ? 0 : consumption;
            _items.Add(item);
            return item;
        }
    }
}
