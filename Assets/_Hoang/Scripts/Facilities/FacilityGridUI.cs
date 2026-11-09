using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hoang.Remake
{
    public sealed class FacilityGridUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _gridRoot;
        [SerializeField] private RectTransform _itemLayer;
        [SerializeField] private RectTransform _previewLayer;
        [SerializeField] private RectTransform _inventoryRoot;
        [SerializeField] private UnityEngine.UI.Text _stats;
        [SerializeField] private UnityEngine.UI.Text _selection;
        [SerializeField] private UnityEngine.UI.Text _feedback;
        [SerializeField] private UnityEngine.UI.Button _rotateButton;
        [SerializeField] private UnityEngine.UI.Button _returnButton;
        [SerializeField] private UnityEngine.UI.Button _cancelButton;
        [SerializeField] private UnityEngine.UI.Button _clearButton;
        [Serializable]
        private sealed class InventoryRow
        {
            public string ItemId;
            public RectTransform Root;
            public UnityEngine.UI.Text Label;
            public FacilityDraggableItemUI Handle;
        }

        [SerializeField] private FacilityItemSO[] _configuredDefinitions = Array.Empty<FacilityItemSO>();
        [SerializeField] private List<InventoryRow> _authoredInventoryRows = new();
        private FacilityManager _manager;
        private readonly Dictionary<string, FacilityPlacedItemUI> _placedItemMap = new();
        private readonly Dictionary<string, InventoryRow> _inventoryRows = new();
        private readonly List<UnityEngine.UI.Image> _previewCells = new();
        private string _selectedItemId;
        private string _selectedInstanceId;
        private int _rotation;
        private Vector2 _lastPointer;
        private Camera _lastCamera;
        private bool _hasPointer;

        public FacilityManager Manager => _manager;
        public RectTransform GridRoot => _gridRoot;
        public int PlacedViewCount => _placedItemMap.Count;

        public static FacilityGridUI Create(Transform parent)
        {
            var root = NewRect(parent, "Facilities");
            SetRect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.96f, 0.95f, 0.89f, 1f);
            var view = root.gameObject.AddComponent<FacilityGridUI>();
            var title = NewText(root, "Title", "Kitchen facilities", 29, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0.02f, 0.92f), new Vector2(0.65f, 1f), Vector2.zero, Vector2.zero);
            view._stats = NewText(root, "PowerAndBuffs", "Power 0 / 0 W | Place a power source, then connect devices.", 18, TextAnchor.UpperLeft);
            SetRect(view._stats.rectTransform, new Vector2(0.33f, 0.87f), new Vector2(0.98f, 0.94f), Vector2.zero, Vector2.zero);

            var inventoryTitle = NewText(root, "InventoryTitle", "INVENTORY", 20, TextAnchor.MiddleLeft);
            SetRect(inventoryTitle.rectTransform, new Vector2(0.02f, 0.84f), new Vector2(0.30f, 0.92f), Vector2.zero, Vector2.zero);
            view._inventoryRoot = NewRect(root, "Inventory");
            SetRect(view._inventoryRoot, new Vector2(0.02f, 0.28f), new Vector2(0.30f, 0.84f), Vector2.zero, Vector2.zero);
            var layout = view._inventoryRoot.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            var hints = NewText(root, "Instructions", "Drag an item or select it, then click a grid cell.\nR / Rotate: turn clockwise.\nDrag a placed item to move it.\nReturn selected: put it in inventory.", 17, TextAnchor.UpperLeft);
            SetRect(hints.rectTransform, new Vector2(0.02f, 0.06f), new Vector2(0.30f, 0.26f), Vector2.zero, Vector2.zero);

            var gridArea = NewRect(root, "GridArea");
            SetRect(gridArea, new Vector2(0.33f, 0.17f), new Vector2(0.98f, 0.86f), Vector2.zero, Vector2.zero);
            view._gridRoot = NewRect(gridArea, "Grid11x11");
            view._gridRoot.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.13f, 0.18f, 0.21f);
            var fitter = view._gridRoot.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1;
            view._gridRoot.gameObject.AddComponent<FacilityGridSurface>().Configure(view);
            for (var y = 0; y < FacilityGrid.GridSize; y++)
            for (var x = 0; x < FacilityGrid.GridSize; x++)
            {
                var cell = NewRect(view._gridRoot, $"Cell_{x}_{y}");
                SetRect(cell, new Vector2((float)x / FacilityGrid.GridSize, (float)y / FacilityGrid.GridSize),
                    new Vector2((float)(x + 1) / FacilityGrid.GridSize, (float)(y + 1) / FacilityGrid.GridSize), new Vector2(1, 1), new Vector2(-1, -1));
                var image = cell.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.color = (x + y) % 2 == 0 ? new Color(0.84f, 0.87f, 0.82f) : new Color(0.79f, 0.83f, 0.78f);
                image.raycastTarget = false;
            }

            view._itemLayer = NewRect(view._gridRoot, "PlacedItems");
            SetRect(view._itemLayer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            view._previewLayer = NewRect(view._gridRoot, "PlacementPreview");
            SetRect(view._previewLayer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            view._selection = NewText(root, "Selection", "Select an item to begin.", 17, TextAnchor.MiddleLeft);
            SetRect(view._selection.rectTransform, new Vector2(0.33f, 0.11f), new Vector2(0.98f, 0.17f), Vector2.zero, Vector2.zero);
            view._rotateButton = NewButton(root, "Rotate", "Rotate (R)", new Vector2(0.33f, 0.04f), new Vector2(0.48f, 0.11f));
            view._returnButton = NewButton(root, "ReturnSelected", "Return selected", new Vector2(0.49f, 0.04f), new Vector2(0.68f, 0.11f));
            view._cancelButton = NewButton(root, "CancelSelection", "Cancel", new Vector2(0.69f, 0.04f), new Vector2(0.81f, 0.11f));
            view._clearButton = NewButton(root, "ReturnAll", "Return all", new Vector2(0.82f, 0.04f), new Vector2(0.98f, 0.11f));
            view._feedback = NewText(root, "Feedback", "Only powered devices grant bonuses.", 16, TextAnchor.LowerLeft);
            SetRect(view._feedback.rectTransform, new Vector2(0.02f, 0), new Vector2(0.98f, 0.04f), Vector2.zero, Vector2.zero);
            view.WireButtons();
            return view;
        }

        public void Configure(FacilityItemSO[] definitions)
        {
            _configuredDefinitions = definitions == null ? Array.Empty<FacilityItemSO>() : (FacilityItemSO[])definitions.Clone();
            RefreshInventory();
        }

        public void Bind(FacilityManager manager)
        {
            if (_manager != null) _manager.Changed -= Refresh;
            _manager = manager;
            if (_manager != null && isActiveAndEnabled) _manager.Changed += Refresh;
            WireButtons();
            Refresh();
        }

        private void OnEnable()
        {
            if (_manager != null) _manager.Changed += Refresh;
            WireButtons();
            Refresh();
        }

        private void OnDisable()
        {
            if (_manager != null) _manager.Changed -= Refresh;
            UnwireButtons();
            CancelSelection();
            UITooltipSystem.HideTooltip();
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(_selectedItemId)) return;
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Rotate();
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CancelSelection();
        }

        public void Select(string itemId, string instanceId = null)
        {
            if (_manager == null || _manager.GetDefinition(itemId) == null) return;
            var placed = string.IsNullOrEmpty(instanceId) ? null : _manager.Grid.GetPlacedItem(instanceId);
            if (string.IsNullOrEmpty(instanceId) ? InventoryCount(itemId) == 0 : placed == null || placed.ItemSO.Id != itemId)
            {
                CancelSelection();
                if (_feedback != null) _feedback.text = "No copies of this device are available in inventory.";
                return;
            }
            _selectedItemId = itemId;
            _selectedInstanceId = instanceId;
            _rotation = placed?.RotationIndex ?? 0;
            RefreshSelection();
            RefreshPlacedItems();
        }

        public void PreviewAt(Vector2 screenPoint, Camera camera)
        {
            _lastPointer = screenPoint;
            _lastCamera = camera;
            _hasPointer = true;
            if (_manager == null || string.IsNullOrEmpty(_selectedItemId) || !TryCell(screenPoint, camera, out var cell))
            {
                HidePreview();
                return;
            }

            var definition = _manager.GetDefinition(_selectedItemId);
            var ignored = _manager.Grid.GetPlacedItem(_selectedInstanceId);
            var available = ignored != null || InventoryCount(_selectedItemId) > 0;
            var valid = available && _manager.Grid.CanPlaceItem(definition, cell, _rotation, ignored);
            var shape = definition.GetRotatedCoords(_rotation);
            EnsurePreviewCells(shape.Count);
            for (var i = 0; i < _previewCells.Count; i++)
            {
                var image = _previewCells[i];
                image.gameObject.SetActive(i < shape.Count);
                if (i >= shape.Count) continue;
                var offset = cell + shape[i];
                // The edge cell stays red if a shape extends beyond the grid.
                offset.x = Mathf.Clamp(offset.x, 0, FacilityGrid.GridSize - 1);
                offset.y = Mathf.Clamp(offset.y, 0, FacilityGrid.GridSize - 1);
                SetRect(image.rectTransform,
                    new Vector2((float)offset.x / FacilityGrid.GridSize, (float)offset.y / FacilityGrid.GridSize),
                    new Vector2((float)(offset.x + 1) / FacilityGrid.GridSize, (float)(offset.y + 1) / FacilityGrid.GridSize),
                    new Vector2(3, 3), new Vector2(-3, -3));
                image.color = valid ? new Color(0.15f, 0.85f, 0.40f, 0.65f) : new Color(0.95f, 0.20f, 0.15f, 0.65f);
            }
        }

        public void PlaceAt(Vector2 screenPoint, Camera camera)
        {
            if (_manager == null || string.IsNullOrEmpty(_selectedItemId) || !TryCell(screenPoint, camera, out var cell)) return;
            var success = string.IsNullOrEmpty(_selectedInstanceId)
                ? _manager.TryPlaceFromInventory(_selectedItemId, cell, _rotation)
                : _manager.TryRepositionPlacedItem(_selectedInstanceId, cell, _rotation);
            _feedback.text = success ? "Layout saved." : "Cannot place here, or the layout could not be saved.";
            if (success) CancelSelection();
        }

        public void FinishDrag(Vector2 screenPoint, Camera camera)
        {
            if (!string.IsNullOrEmpty(_selectedInstanceId) && RectTransformUtility.RectangleContainsScreenPoint(_inventoryRoot, screenPoint, camera))
                ReturnSelected();
            else PlaceAt(screenPoint, camera);
            HidePreview();
        }

        public void HidePreview()
        {
            foreach (var image in _previewCells) image.gameObject.SetActive(false);
        }

        public void ShowItemTooltip(string itemId, string instanceId)
        {
            var definition = _manager?.GetDefinition(itemId);
            if (definition == null) return;
            var item = _manager.Grid.GetPlacedItem(instanceId);
            var power = definition.IsPowerSource ? $"Power output: {definition.PowerOutput} W" : $"Power required: {definition.PowerConsumption} W";
            var relay = definition.RequiresRelay ? "\nRequires an adjacent powered relay or source." : definition.IsRelay ? "\nPowers relay-required devices." : "";
            var stats = definition.Stats;
            var body = $"{definition.Description}\n{power}{relay}\nCooking +{stats.ProductionSpeedBonus:P0} | Revenue +{stats.RevenueBonus:P0}\nPatience +{stats.CustomerPatienceBonus:P0} | Cost -{stats.IngredientCostReduction:P0}\nCleanliness +{stats.CleanlinessBonus}";
            UITooltipSystem.ShowTooltip("FACILITY", definition.ItemName, body,
                item == null ? "STATUS: IN INVENTORY" : item.IsPowered ? "STATUS: ACTIVE" : "STATUS: NO POWER", "Drag to place. R to rotate.");
        }

        private bool TryCell(Vector2 point, Camera camera, out Vector2Int cell)
        {
            cell = default;
            if (_gridRoot == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_gridRoot, point, camera, out var local)) return false;
            var rect = _gridRoot.rect;
            if (!rect.Contains(local) || rect.width <= 0 || rect.height <= 0) return false;
            cell = new Vector2Int(Mathf.FloorToInt((local.x - rect.xMin) / rect.width * FacilityGrid.GridSize),
                Mathf.FloorToInt((local.y - rect.yMin) / rect.height * FacilityGrid.GridSize));
            return _manager.Grid.IsInBounds(cell);
        }

        private void Rotate()
        {
            if (string.IsNullOrEmpty(_selectedItemId)) return;
            _rotation = (_rotation + 1) % 4;
            RefreshSelection();
            if (_hasPointer) PreviewAt(_lastPointer, _lastCamera);
        }

        private void ReturnSelected()
        {
            if (_manager == null || string.IsNullOrEmpty(_selectedInstanceId)) return;
            var result = _manager.TryRemoveToInventory(_selectedInstanceId);
            _feedback.text = result ? "Returned to inventory. Layout saved." : "Could not save the layout. Please try again.";
            if (result) CancelSelection();
        }

        private void ReturnAll()
        {
            if (_manager == null) return;
            var result = _manager.ClearAllToInventory();
            _feedback.text = result ? "All devices returned to inventory." : "Could not save the layout. Please try again.";
            if (result) CancelSelection();
        }

        private void CancelSelection()
        {
            _selectedItemId = null;
            _selectedInstanceId = null;
            _hasPointer = false;
            HidePreview();
            RefreshSelection();
            if (_manager != null && isActiveAndEnabled) RefreshPlacedItems();
        }

        private void Refresh()
        {
            if (_manager == null || _gridRoot == null || _inventoryRoot == null) return;
            RefreshInventory();
            RefreshPlacedItems();
            var stats = _manager.CurrentTotalStats;
            _stats.text = $"Power {_manager.CurrentPowerUsed} / {_manager.TotalPowerCapacity} W | Cooking +{stats.ProductionSpeedBonus:P0} | Revenue +{stats.RevenueBonus:P0}\nPatience +{stats.CustomerPatienceBonus:P0} | Cost -{stats.IngredientCostReduction:P0} | Cleanliness +{stats.CleanlinessBonus}";
            if (!string.IsNullOrEmpty(_selectedInstanceId) && _manager.Grid.GetPlacedItem(_selectedInstanceId) == null) CancelSelection();
            else if (!string.IsNullOrEmpty(_selectedItemId) && string.IsNullOrEmpty(_selectedInstanceId) && InventoryCount(_selectedItemId) == 0) CancelSelection();
            RefreshSelection();
        }

        private void RefreshInventory()
        {
            if (_inventoryRoot == null) return;
            // The references survive scene/prefab serialization; rebuild only the lookup, never authored rows.
            _inventoryRows.Clear();
            _authoredInventoryRows ??= new List<InventoryRow>();
            foreach (var row in _authoredInventoryRows)
            {
                if (row == null || row.Root == null || row.Label == null || row.Handle == null || string.IsNullOrEmpty(row.ItemId)) continue;
                _inventoryRows.TryAdd(row.ItemId, row);
            }
            IReadOnlyList<FacilityItemSO> definitions = _manager == null ? _configuredDefinitions ?? Array.Empty<FacilityItemSO>() : _manager.Definitions;
            var activeIds = new HashSet<string>();
            foreach (var definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id)) continue;
                activeIds.Add(definition.Id);
                if (!_inventoryRows.TryGetValue(definition.Id, out var descriptor))
                {
                    var row = NewRect(_inventoryRoot, "Inventory_" + definition.Id);
                    var image = row.gameObject.AddComponent<UnityEngine.UI.Image>();
                    image.color = Color.Lerp(definition.ThemeColor, Color.white, 0.72f);
                    var label = NewText(row, "ItemName", "", 18, TextAnchor.MiddleLeft);
                    SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 3), new Vector2(-6, -3));
                    var handle = row.gameObject.AddComponent<FacilityDraggableItemUI>();
                    var element = row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                    element.minHeight = 38;
                    element.preferredHeight = 60;
                    descriptor = new InventoryRow { ItemId = definition.Id, Root = row, Label = label, Handle = handle };
                    _authoredInventoryRows.Add(descriptor);
                    _inventoryRows.Add(definition.Id, descriptor);
                }

                if (!descriptor.Root.gameObject.activeSelf) descriptor.Root.gameObject.SetActive(true);
                descriptor.Handle.Configure(this, definition.Id);
                var count = _manager == null ? "" : $" x{InventoryCount(definition.Id)}";
                descriptor.Label.text = $"{definition.ItemName}{count}\n{(definition.IsPowerSource ? "+" + definition.PowerOutput : "-" + definition.PowerConsumption)} W";
                descriptor.Label.color = _manager == null || InventoryCount(definition.Id) > 0
                    ? new Color(0.12f, 0.18f, 0.20f) : new Color(0.43f, 0.47f, 0.46f);
            }
            foreach (var descriptor in _inventoryRows.Values)
                if (!activeIds.Contains(descriptor.ItemId) && descriptor.Root.gameObject.activeSelf) descriptor.Root.gameObject.SetActive(false);
        }

        private int InventoryCount(string id)
        {
            var count = 0;
            foreach (var itemId in _manager.InventoryItemIds)
                if (itemId == id) count++;
            return count;
        }

        private void RefreshPlacedItems()
        {
            if (_itemLayer == null) return;
            var seen = new HashSet<string>();
            foreach (var item in _manager.Grid.PlacedItems)
            {
                seen.Add(item.InstanceId);
                if (!_placedItemMap.TryGetValue(item.InstanceId, out var view))
                {
                    view = FacilityPlacedItemUI.Create(_itemLayer, this, item);
                    _placedItemMap.Add(item.InstanceId, view);
                }

                view.Refresh(item, item.InstanceId == _selectedInstanceId);
            }

            var removed = new List<string>();
            foreach (var pair in _placedItemMap)
                if (!seen.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (var id in removed)
            {
                var view = _placedItemMap[id];
                view.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(view.gameObject);
                else DestroyImmediate(view.gameObject);
                _placedItemMap.Remove(id);
            }
        }

        private void RefreshSelection()
        {
            if (_selection != null)
            {
                var definition = _manager?.GetDefinition(_selectedItemId);
                _selection.text = definition == null ? "Select an item to begin." : $"Selected: {definition.ItemName} | Rotation: {_rotation * 90} degrees";
            }

            if (_rotateButton != null) _rotateButton.interactable = !string.IsNullOrEmpty(_selectedItemId);
            if (_returnButton != null) _returnButton.interactable = !string.IsNullOrEmpty(_selectedInstanceId);
            if (_cancelButton != null) _cancelButton.interactable = !string.IsNullOrEmpty(_selectedItemId);
        }

        private void EnsurePreviewCells(int count)
        {
            while (_previewCells.Count < count)
            {
                var rect = NewRect(_previewLayer, "PreviewCell" + _previewCells.Count);
                var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.raycastTarget = false;
                _previewCells.Add(image);
            }
        }

        private void WireButtons()
        {
            UnwireButtons();
            if (_rotateButton != null) _rotateButton.onClick.AddListener(Rotate);
            if (_returnButton != null) _returnButton.onClick.AddListener(ReturnSelected);
            if (_cancelButton != null) _cancelButton.onClick.AddListener(CancelSelection);
            if (_clearButton != null) _clearButton.onClick.AddListener(ReturnAll);
        }

        private void UnwireButtons()
        {
            if (_rotateButton != null) _rotateButton.onClick.RemoveListener(Rotate);
            if (_returnButton != null) _returnButton.onClick.RemoveListener(ReturnSelected);
            if (_cancelButton != null) _cancelButton.onClick.RemoveListener(CancelSelection);
            if (_clearButton != null) _clearButton.onClick.RemoveListener(ReturnAll);
        }

        internal static RectTransform NewRect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        internal static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        internal static UnityEngine.UI.Text NewText(Transform parent, string name, string text, int size, TextAnchor alignment)
        {
            var rect = NewRect(parent, name);
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var label = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.alignment = alignment;
            label.text = text;
            label.color = new Color(0.16f, 0.21f, 0.23f);
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static UnityEngine.UI.Button NewButton(Transform parent, string name, string text, Vector2 min, Vector2 max)
        {
            var rect = NewRect(parent, name);
            SetRect(rect, min, max, Vector2.zero, Vector2.zero);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.20f, 0.43f, 0.33f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var label = NewText(rect, "Label", text, 17, TextAnchor.MiddleCenter);
            label.color = Color.white;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 17;
            return button;
        }
    }
}
