using System.Collections.Generic;
using UnityEngine;

namespace Hoang.Remake
{
    public sealed class FacilityPlacedItemUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private UnityEngine.UI.Text _label;
        [SerializeField] private UnityEngine.UI.Text _status;
        [SerializeField] private List<UnityEngine.UI.Image> _cells = new();
        [SerializeField] private List<FacilityDraggableItemUI> _handles = new();
        private Vector2Int _cachedOrigin = new(-1, -1);
        private int _cachedRotation = -1;

        public static FacilityPlacedItemUI Create(Transform parent, FacilityGridUI owner, FacilityPlacedItem item)
        {
            var rect = FacilityGridUI.NewRect(parent, "Placed_" + item.InstanceId);
            var view = rect.gameObject.AddComponent<FacilityPlacedItemUI>();
            view._rect = rect;
            for (var i = 0; i < item.ItemSO.ShapeCoords.Count; i++)
            {
                var cell = FacilityGridUI.NewRect(rect, "ShapeCell" + i);
                var image = cell.gameObject.AddComponent<UnityEngine.UI.Image>();
                var handle = cell.gameObject.AddComponent<FacilityDraggableItemUI>();
                handle.Configure(owner, item.ItemSO.Id, item.InstanceId);
                view._cells.Add(image);
                view._handles.Add(handle);
            }

            view._label = FacilityGridUI.NewText(rect, "Name", item.ItemSO.DisplayName, 15, TextAnchor.MiddleCenter);
            view._label.resizeTextForBestFit = true;
            view._label.resizeTextMinSize = 8;
            view._label.resizeTextMaxSize = 15;
            view._label.fontStyle = FontStyle.Bold;
            view._label.color = Color.white;
            view._status = FacilityGridUI.NewText(rect, "PowerStatus", "", 10, TextAnchor.LowerCenter);
            view._status.color = Color.white;
            view.Refresh(item, false);
            return view;
        }

        public void Refresh(FacilityPlacedItem item, bool selected)
        {
            var shape = item.ItemSO.GetRotatedCoords(item.RotationIndex);
            var dimensions = item.ItemSO.GetDimensions(item.RotationIndex);
            if (_cachedOrigin != item.OriginCoord || _cachedRotation != item.RotationIndex)
            {
                _rect.anchorMin = (Vector2)item.OriginCoord / FacilityGrid.GridSize;
                _rect.anchorMax = (Vector2)(item.OriginCoord + dimensions) / FacilityGrid.GridSize;
                _rect.offsetMin = Vector2.zero;
                _rect.offsetMax = Vector2.zero;
                for (var i = 0; i < _cells.Count; i++)
                {
                    var cell = _cells[i].rectTransform;
                    cell.anchorMin = new Vector2((float)shape[i].x / dimensions.x, (float)shape[i].y / dimensions.y);
                    cell.anchorMax = new Vector2((float)(shape[i].x + 1) / dimensions.x, (float)(shape[i].y + 1) / dimensions.y);
                    cell.offsetMin = new Vector2(2, 2);
                    cell.offsetMax = new Vector2(-2, -2);
                }

                // Text stays within one occupied cell, including nonrectangular shapes.
                var first = shape[0];
                FacilityGridUI.SetRect(_label.rectTransform,
                    new Vector2((float)first.x / dimensions.x, (float)first.y / dimensions.y),
                    new Vector2((float)(first.x + 1) / dimensions.x, (float)(first.y + 1) / dimensions.y),
                    new Vector2(2, 10), new Vector2(-2, -1));
                FacilityGridUI.SetRect(_status.rectTransform, _label.rectTransform.anchorMin, _label.rectTransform.anchorMax,
                    new Vector2(1, 1), new Vector2(-1, -16));
                _cachedOrigin = item.OriginCoord;
                _cachedRotation = item.RotationIndex;
            }

            var color = item.IsPowered ? item.ItemSO.ThemeColor : Color.Lerp(item.ItemSO.ThemeColor, new Color(0.16f, 0.18f, 0.23f), 0.6f);
            if (selected) color = Color.Lerp(color, Color.white, 0.3f);
            foreach (var cell in _cells) cell.color = color;
            _status.text = item.ItemSO.IsPowerSource ? "SOURCE" : item.IsPowered ? "ON" : "OFF";
        }
    }
}
