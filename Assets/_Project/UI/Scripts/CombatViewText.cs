using BunDauPeo.Combat;
using UnityEngine;

namespace BunDauPeo.UI
{
    // Chữ của màn trận: hàm tĩnh thuần để test không cần scene.
    public static class CombatViewText
    {
        public static string UnitHp(int hp, int maxHp) => $"{hp}/{maxHp}";

        public static string SkillButton(int cooldown) => cooldown > 0 ? $"Skill ({cooldown})" : "Skill";

        // Giá vượt năng lượng tối đa thì chỉ cần đầy thanh, nên chia cho giá hiệu dụng (cùng luật BattleSession).
        public static float UltimateFill(int energy, int energyCost, int maxEnergy)
        {
            var effective = Mathf.Min(energyCost, maxEnergy);
            return effective <= 0 ? 0f : Mathf.Clamp01((float)energy / effective);
        }

        public static string UltimateReadyTag(bool ready) => ready ? "OK!" : "";

        public static string TurnLine(string name) => $"Lượt của {name}";

        public static string EndBanner(BattleState state) => state switch
        {
            BattleState.Won => "CLEAR",
            BattleState.Lost => "Thất bại",
            _ => "",
        };
    }
}
