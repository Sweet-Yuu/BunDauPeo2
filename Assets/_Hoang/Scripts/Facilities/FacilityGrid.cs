using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hoang.Remake
{
    public sealed class FacilityPlacedItem
    {
        public FacilityPlacedItem(string instanceId, FacilityItemSO itemSO, Vector2Int origin, int rotationIndex)
        {
            InstanceId = instanceId ?? throw new ArgumentNullException(nameof(instanceId));
            ItemSO = itemSO != null ? itemSO : throw new ArgumentNullException(nameof(itemSO));
            OriginCoord = origin;
            RotationIndex = (rotationIndex % 4 + 4) % 4;
        }

        public string InstanceId { get; }
        public FacilityItemSO ItemSO { get; }
        public Vector2Int OriginCoord { get; internal set; }
        public int RotationIndex { get; internal set; }
        public bool IsPowered { get; internal set; }

        public IEnumerable<Vector2Int> GetOccupiedCells()
        {
            foreach (var offset in ItemSO.GetRotatedCoords(RotationIndex)) yield return OriginCoord + offset;
        }
    }

    public sealed class FacilityGrid
    {
        public const int GridSize = 11;
        private readonly FacilityPlacedItem[,] _cells = new FacilityPlacedItem[GridSize, GridSize];
        private readonly List<FacilityPlacedItem> _placedItems = new();
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        public IReadOnlyList<FacilityPlacedItem> PlacedItems => _placedItems;
        public int TotalPowerCapacity { get; private set; }
        public int CurrentPowerUsed { get; private set; }
        public bool IsInBounds(Vector2Int cell) => cell.x >= 0 && cell.x < GridSize && cell.y >= 0 && cell.y < GridSize;
        public FacilityPlacedItem GetItemAt(Vector2Int cell) => IsInBounds(cell) ? _cells[cell.x, cell.y] : null;
        public bool IsCellPowered(Vector2Int cell) => GetItemAt(cell)?.IsPowered == true;
        public FacilityPlacedItem GetPlacedItem(string instanceId) => _placedItems.Find(item => item.InstanceId == instanceId);

        public bool CanPlaceItem(FacilityItemSO definition, Vector2Int origin, int rotation, FacilityPlacedItem ignoreItem = null)
        {
            if (definition == null || definition.Validate().Count > 0) return false;
            var shape = definition.GetRotatedCoords(rotation);
            if (shape.Count == 0) return false;
            foreach (var offset in shape)
            {
                // Bounds are checked before adding to avoid overflow with corrupt origins.
                if (origin.x < 0 || origin.y < 0 || origin.x >= GridSize || origin.y >= GridSize
                    || offset.x < 0 || offset.y < 0 || offset.x >= GridSize - origin.x || offset.y >= GridSize - origin.y) return false;
                var cell = origin + offset;
                if (_cells[cell.x, cell.y] != null && _cells[cell.x, cell.y] != ignoreItem) return false;
            }

            return true;
        }

        public bool PlaceItem(FacilityPlacedItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.InstanceId) || GetPlacedItem(item.InstanceId) != null
                || !CanPlaceItem(item.ItemSO, item.OriginCoord, item.RotationIndex)) return false;
            _placedItems.Add(item);
            SetCells(item, item);
            UpdatePowerTransmission();
            return true;
        }

        public bool TryReposition(string instanceId, Vector2Int origin, int rotation)
        {
            var item = GetPlacedItem(instanceId);
            if (item == null || !CanPlaceItem(item.ItemSO, origin, rotation, item)) return false;
            SetCells(item, null);
            item.OriginCoord = origin;
            item.RotationIndex = (rotation % 4 + 4) % 4;
            SetCells(item, item);
            UpdatePowerTransmission();
            return true;
        }

        public bool RemoveItem(FacilityPlacedItem item)
        {
            if (item == null || !_placedItems.Remove(item)) return false;
            SetCells(item, null);
            UpdatePowerTransmission();
            return true;
        }

        public void Clear()
        {
            Array.Clear(_cells, 0, _cells.Length);
            _placedItems.Clear();
            UpdatePowerTransmission();
        }

        public void UpdatePowerTransmission()
        {
            long totalCapacity = 0;
            CurrentPowerUsed = 0;
            var sources = new List<FacilityPlacedItem>();
            foreach (var item in _placedItems)
            {
                item.IsPowered = item.ItemSO.IsPowerSource;
                if (!item.IsPowered) continue;
                totalCapacity += item.ItemSO.PowerOutput;
                sources.Add(item);
            }

            TotalPowerCapacity = (int)Math.Min(int.MaxValue, totalCapacity);
            // Source ordering follows the grid, so loading a differently ordered save does not change load priority.
            sources.Sort((a, b) => a.OriginCoord.y != b.OriginCoord.y
                ? a.OriginCoord.y.CompareTo(b.OriginCoord.y) : a.OriginCoord.x.CompareTo(b.OriginCoord.x));
            var queue = new Queue<Vector2Int>();
            var visited = new bool[GridSize, GridSize];
            foreach (var source in sources) EnqueueCells(source, queue, visited);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var supplier = GetItemAt(current);
                foreach (var direction in Directions)
                {
                    var cell = current + direction;
                    if (!IsInBounds(cell) || visited[cell.x, cell.y]) continue;
                    var item = GetItemAt(cell);
                    if (item == null) continue;
                    if (!item.IsPowered)
                    {
                        if (item.ItemSO.RequiresRelay && !supplier.ItemSO.IsPowerSource && !supplier.ItemSO.IsRelay) continue;
                        if (item.ItemSO.PowerConsumption > TotalPowerCapacity - CurrentPowerUsed) continue;
                        item.IsPowered = true;
                        CurrentPowerUsed += item.ItemSO.PowerConsumption;
                    }

                    if (item.ItemSO.CanConductPower || item.ItemSO.IsPowerSource) EnqueueCells(item, queue, visited);
                }
            }
        }

        private void SetCells(FacilityPlacedItem item, FacilityPlacedItem value)
        {
            foreach (var cell in item.GetOccupiedCells()) _cells[cell.x, cell.y] = value;
        }

        private static void EnqueueCells(FacilityPlacedItem item, Queue<Vector2Int> queue, bool[,] visited)
        {
            foreach (var cell in item.GetOccupiedCells())
            {
                if (visited[cell.x, cell.y]) continue;
                visited[cell.x, cell.y] = true;
                queue.Enqueue(cell);
            }
        }
    }
}
