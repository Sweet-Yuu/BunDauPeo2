using UnityEngine;

namespace BunDauPeo.UI
{
    // Phép chiếu isometric 2:1, ô 120×60 pixel. Gốc (0, 0) là góc sau của phòng; gx đi xuống bên phải, gy đi xuống bên trái.
    public static class IsoProjection
    {
        private const float HalfWidth = 60f;
        private const float HalfHeight = 30f;

        public static Vector2 CellToLocal(float gx, float gy) => new((gx - gy) * HalfWidth, -(gx + gy) * HalfHeight);

        // Lớn hơn = gần người xem hơn = vẽ sau.
        public static float DepthKey(float gx, float gy) => gx + gy;
    }
}
