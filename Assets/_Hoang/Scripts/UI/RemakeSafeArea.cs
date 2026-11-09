using UnityEngine;

namespace Hoang.Remake
{
    public sealed class RemakeSafeArea : MonoBehaviour
    {
        private Rect _last;
        private Vector2Int _screen;
        private RectTransform _rect;
        private void Awake() => _rect = (RectTransform)transform;
        private void Update()
        {
            var area = Screen.safeArea;
            if (area == _last && _screen == new Vector2Int(Screen.width, Screen.height)) return;
            _last = area;
            _screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width < 1 || Screen.height < 1) return;
            _rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            _rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            _rect.offsetMin = _rect.offsetMax = Vector2.zero;
        }
    }
}
