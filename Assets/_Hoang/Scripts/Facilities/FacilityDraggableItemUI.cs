using UnityEngine;
using UnityEngine.EventSystems;

namespace Hoang.Remake
{
    public sealed class FacilityDraggableItemUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private FacilityGridUI _owner;
        [SerializeField] private string _itemId;
        [SerializeField] private string _instanceId;
        private bool _dragging;

        public void Configure(FacilityGridUI owner, string itemId, string instanceId = null)
        {
            _owner = owner;
            _itemId = itemId;
            _instanceId = instanceId;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_dragging || _owner == null) return;
            _owner.Select(_itemId, _instanceId);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _owner?.Select(_itemId, _instanceId);
            _owner?.PreviewAt(eventData.position, eventData.pressEventCamera);
        }

        public void OnDrag(PointerEventData eventData) => _owner?.PreviewAt(eventData.position, eventData.pressEventCamera);
        public void OnPointerMove(PointerEventData eventData) => _owner?.PreviewAt(eventData.position, eventData.enterEventCamera);

        public void OnEndDrag(PointerEventData eventData)
        {
            _owner?.FinishDrag(eventData.position, eventData.pressEventCamera);
            _dragging = false;
        }

        public void OnPointerEnter(PointerEventData eventData) => _owner?.ShowItemTooltip(_itemId, _instanceId);
        public void OnPointerExit(PointerEventData eventData) => UITooltipSystem.HideTooltip();
    }
}
