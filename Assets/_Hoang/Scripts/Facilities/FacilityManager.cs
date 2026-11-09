using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using UnityEngine;

namespace Hoang.Remake
{
    public sealed class FacilityManager : IDisposable
    {
        private readonly RemakeContext _context;
        private readonly Dictionary<string, FacilityItemSO> _definitions = new(StringComparer.Ordinal);
        private readonly List<FacilityItemSO> _catalog = new();
        private bool _disposed;

        public FacilityManager(RemakeContext context, FacilityItemSO[] definitions)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            foreach (var definition in definitions)
            {
                if (definition == null || definition.Validate().Count > 0) throw new ArgumentException("Invalid facility definition.", nameof(definitions));
                if (!_definitions.TryAdd(definition.Id, definition)) throw new ArgumentException($"Duplicate facility Id: {definition.Id}", nameof(definitions));
                _catalog.Add(definition);
            }

            Rebuild();
            _context.Changed += OnContextChanged;
        }

        public FacilityGrid Grid { get; private set; }
        public IReadOnlyList<string> InventoryItemIds => _context.Save.Facilities.InventoryItemIds;
        public IReadOnlyList<FacilityItemSO> Definitions => _catalog;
        public FacilityStats CurrentTotalStats { get; private set; }
        public int TotalPowerCapacity => Grid.TotalPowerCapacity;
        public int CurrentPowerUsed => Grid.CurrentPowerUsed;
        public int RemainingPower => TotalPowerCapacity - CurrentPowerUsed;
        public bool IsPowerSufficient
        {
            get
            {
                foreach (var item in Grid.PlacedItems)
                    if (!item.IsPowered) return false;
                return true;
            }
        }

        public event Action Changed;
        public FacilityItemSO GetDefinition(string itemId) => itemId != null && _definitions.TryGetValue(itemId, out var definition) ? definition : null;

        public bool TryPlaceFromInventory(string itemId, Vector2Int origin, int rotation)
        {
            if (_disposed || !_context.Save.Facilities.InventoryItemIds.Contains(itemId) || !Grid.CanPlaceItem(GetDefinition(itemId), origin, rotation)) return false;
            var instanceId = Guid.NewGuid().ToString("N");
            return _context.Commit(save =>
            {
                save.Facilities.InventoryItemIds.Remove(itemId);
                save.Facilities.PlacedItems.Add(new FacilityPlacedItemSaveEntry
                {
                    InstanceId = instanceId, ItemId = itemId, OriginX = origin.x, OriginY = origin.y, RotationIndex = (rotation % 4 + 4) % 4,
                });
            });
        }

        public bool TryRepositionPlacedItem(string instanceId, Vector2Int origin, int rotation)
        {
            var item = Grid.GetPlacedItem(instanceId);
            if (_disposed || item == null || !Grid.CanPlaceItem(item.ItemSO, origin, rotation, item)) return false;
            return _context.Commit(save =>
            {
                var entry = save.Facilities.PlacedItems.Find(row => row.InstanceId == instanceId);
                entry.OriginX = origin.x;
                entry.OriginY = origin.y;
                entry.RotationIndex = (rotation % 4 + 4) % 4;
            });
        }

        public bool TryRemoveToInventory(string instanceId)
        {
            if (_disposed || Grid.GetPlacedItem(instanceId) == null) return false;
            return _context.Commit(save =>
            {
                var entry = save.Facilities.PlacedItems.Find(row => row.InstanceId == instanceId);
                save.Facilities.InventoryItemIds.Add(entry.ItemId);
                save.Facilities.PlacedItems.Remove(entry);
            });
        }

        public bool ClearAllToInventory()
        {
            if (_disposed) return false;
            if (_context.Save.Facilities.PlacedItems.Count == 0) return true;
            return _context.Commit(save =>
            {
                foreach (var entry in save.Facilities.PlacedItems)
                    if (entry != null && !string.IsNullOrEmpty(entry.ItemId)) save.Facilities.InventoryItemIds.Add(entry.ItemId);
                save.Facilities.PlacedItems.Clear();
            });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _context.Changed -= OnContextChanged;
        }

        private void OnContextChanged()
        {
            Rebuild();
            Changed.SafeInvoke();
        }

        private void Rebuild()
        {
            var grid = new FacilityGrid();
            var state = _context.Save.Facilities;
            if (state != null && state.PlacedItems != null)
            {
                foreach (var entry in state.PlacedItems)
                {
                    if (entry == null || GetDefinition(entry.ItemId) == null || string.IsNullOrWhiteSpace(entry.InstanceId)) continue;
                    grid.PlaceItem(new FacilityPlacedItem(entry.InstanceId, GetDefinition(entry.ItemId), new Vector2Int(entry.OriginX, entry.OriginY), entry.RotationIndex));
                }
            }

            Grid = grid;
            var stats = new FacilityStats();
            foreach (var item in grid.PlacedItems)
                if (item.IsPowered) stats += item.ItemSO.Stats;
            CurrentTotalStats = stats;
        }
    }
}
