using System;
using System.Collections.Generic;

namespace Hoang.Remake
{
    [Serializable]
    public sealed class FacilityPlacedItemSaveEntry
    {
        public string InstanceId;
        public string ItemId;
        public int OriginX;
        public int OriginY;
        public int RotationIndex;
    }

    [Serializable]
    public sealed class FacilitySaveState
    {
        public List<string> InventoryItemIds = new();
        public List<FacilityPlacedItemSaveEntry> PlacedItems = new();
    }
}
