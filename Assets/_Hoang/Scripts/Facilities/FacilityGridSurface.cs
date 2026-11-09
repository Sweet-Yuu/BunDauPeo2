using UnityEngine;
using UnityEngine.EventSystems;

namespace Hoang.Remake
{
    public sealed class FacilityGridSurface : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
    {
        [SerializeField] private FacilityGridUI _owner;

        public void Configure(FacilityGridUI owner) => _owner = owner;
        public void OnPointerClick(PointerEventData eventData) => _owner?.PlaceAt(eventData.position, eventData.pressEventCamera);
        public void OnPointerMove(PointerEventData eventData) => _owner?.PreviewAt(eventData.position, eventData.enterEventCamera);
        public void OnPointerExit(PointerEventData eventData) => _owner?.HidePreview();
    }
}
