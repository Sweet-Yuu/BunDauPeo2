using UnityEngine;

namespace BunDauPeo.UI
{
    // Toạ độ màn trận theo spec Combat 5.4: vùng 1920×1080 gốc trái trên, y hướng xuống. Hàm thuần để test không đè nhau.
    public static class BattleLayout
    {
        // Thẻ nhân vật và bảng chiêu nằm trong dải này; dải DEV ở y >= 988 không che chúng.
        public const float CardAreaTop = 820f, CardAreaBottom = 980f;

        public static readonly Vector2 UnitSize = new(140, 170), BossSize = new(280, 320);

        // Theo SlotIndex: Front 0-1, Mid 2-3, Back 4.
        private static readonly Vector2[] AllyCenters = { new(760, 360), new(760, 600), new(540, 300), new(540, 540), new(320, 450) };
        private static readonly Vector2[] EnemyCenters = { new(1180, 360), new(1180, 600), new(1400, 300), new(1400, 540), new(1620, 450) };
        private static readonly Vector2 BossCenter = new(1180, 340);

        public static Rect AllyRect(int slotIndex) => Box(AllyCenters[slotIndex], UnitSize);

        // Chỉ ô 0 đủ chỗ cho cỡ boss; boss ở ô khác vẽ cỡ thường để không đè quái bên cạnh.
        public static Rect EnemyRect(int slotIndex, bool isBoss) =>
            isBoss && slotIndex == 0 ? Box(BossCenter, BossSize) : Box(EnemyCenters[slotIndex], UnitSize);

        // RectTransform neo giữa Canvas: màn dài hơn 16:9 không dồn cả trận sang trái.
        public static Vector2 ToAnchored(Vector2 topLeftPoint) => new(topLeftPoint.x - 960f, 540f - topLeftPoint.y);

        private static Rect Box(Vector2 center, Vector2 size) => new(center - size / 2f, size);
    }
}
