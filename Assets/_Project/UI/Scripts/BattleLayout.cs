using UnityEngine;

namespace BunDauPeo.UI
{
    // Toạ độ màn trận theo spec Combat 5.4: vùng 1920×1080 gốc trái trên, y hướng xuống. Hàm thuần để test không đè nhau.
    // Mỗi phe là lưới 3 cột (Front, Mid, Back) × 3 hàng; hàng cách nhau 240 để dưới mỗi người còn chỗ cho tên, số HP và một hàng
    // buff/debuff sau này (130 thân + ~70 chữ + ~40 hàng buff).
    public static class BattleLayout
    {
        public const float RowTop = 280f, RowMid = 520f, RowBottom = 760f;

        public static readonly Vector2 UnitSize = new(130, 130), BossSize = new(280, 320);

        // Theo SlotIndex: Front 0-1 (hàng trên và dưới), Mid 2-3 (hàng trên và dưới), Back 4 (hàng giữa).
        private static readonly Vector2[] AllyCenters = { new(760, RowTop), new(760, RowBottom), new(540, RowTop), new(540, RowBottom), new(320, RowMid) };
        private static readonly Vector2[] EnemyCenters = { new(1180, RowTop), new(1180, RowBottom), new(1400, RowTop), new(1400, RowBottom), new(1620, RowMid) };
        // Boss cao hơn hàng nên lùi xuống để không chạm thanh trên; đáy boss (560) còn cách quái ô 1 (đỉnh 695) đủ chỗ cho chữ.
        private static readonly Vector2 BossCenter = new(1180, 400);

        public static Rect AllyRect(int slotIndex) => Box(AllyCenters[slotIndex], UnitSize);

        // Chỉ ô 0 đủ chỗ cho cỡ boss; boss ở ô khác vẽ cỡ thường để không đè quái bên cạnh.
        public static Rect EnemyRect(int slotIndex, bool isBoss) =>
            isBoss && slotIndex == 0 ? Box(BossCenter, BossSize) : Box(EnemyCenters[slotIndex], UnitSize);

        // RectTransform neo giữa Canvas: màn dài hơn 16:9 không dồn cả trận sang trái.
        public static Vector2 ToAnchored(Vector2 topLeftPoint) => new(topLeftPoint.x - 960f, 540f - topLeftPoint.y);

        private static Rect Box(Vector2 center, Vector2 size) => new(center - size / 2f, size);
    }
}
