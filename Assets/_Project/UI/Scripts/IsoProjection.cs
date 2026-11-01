using UnityEngine;

namespace BunDauPeo.UI
{
    // Lưới của màn quán theo ảnh tham khảo (spec Restaurant 5.1): sàn là một chữ nhật xoay RotationDegrees quanh góc sau-trái,
    // gx đi sang phải và hơi xuống, gy đi xuống và hơi sang trái. Gốc (0, 0) là góc sau-trái của sàn.
    public static class IsoProjection
    {
        private const float StepXx = 90f;
        private const float StepXy = -18f;
        private const float StepYx = -13f;
        private const float StepYy = -65f;

        // Cạnh một ô trong hệ của sàn (trước khi xoay); hai trục vuông góc nên khớp đúng phép xoay.
        public static readonly float CellWidth = Mathf.Sqrt(StepXx * StepXx + StepXy * StepXy);
        public static readonly float CellHeight = Mathf.Sqrt(StepYx * StepYx + StepYy * StepYy);

        // Độ xoay (z) của sàn và mọi đồ vật cùng hệ với sàn: khoảng −11,3° để hai cạnh dài dốc nhẹ xuống bên phải.
        public static readonly float RotationDegrees = Mathf.Atan2(StepXy, StepXx) * Mathf.Rad2Deg;

        public static Vector2 CellToLocal(float gx, float gy) => new(StepXx * gx + StepYx * gy, StepXy * gx + StepYy * gy);

        // Lớn hơn = gần người xem hơn = vẽ sau. Theo toạ độ y của màn hình nên một hàng gy xuống thấp hơn nhiều cột gx.
        public static float DepthKey(float gx, float gy) => -(StepXy * gx + StepYy * gy);
    }
}
