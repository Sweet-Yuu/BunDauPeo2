using UnityEngine;

namespace BunDauPeo.UI
{
    public static class WorldFocus
    {
        // normalizedPosition của ScrollRect (đáy = 0) để điểm target (toạ độ local trong content, pivot giữa) nằm giữa viewport; kẹp [0, 1].
        // Content không rộng hơn viewport theo trục nào thì trục đó trả 0,5.
        public static Vector2 Center(Vector2 contentSize, Vector2 viewportSize, Vector2 targetInContent) =>
            new(Axis(contentSize.x, viewportSize.x, targetInContent.x), Axis(contentSize.y, viewportSize.y, targetInContent.y));

        private static float Axis(float content, float viewport, float target)
        {
            var range = content - viewport;
            return range <= 0f ? 0.5f : Mathf.Clamp01((target + range / 2f) / range);
        }
    }
}
