using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Core;

namespace BunDauPeo.UI
{
    public readonly struct TeamSelectRow
    {
        public TeamSelectRow(CharacterDefinition character, bool inTeam, StaffRole? staffRole, int stars)
        {
            Stars = stars;
            Character = character;
            InTeam = inTeam;
            StaffRole = staffRole;
        }

        public CharacterDefinition Character { get; }
        // Sao hiện tại (đã tính lần nâng), không phải sao khởi điểm của SO.
        public int Stars { get; }
        public bool InTeam { get; }
        // Vai đang làm ở quán; đã vào đội thì null (Roster.AddToTeam gỡ khỏi quán).
        public StaffRole? StaffRole { get; }
    }

    // Hàm thuần của popup chọn đội: view chỉ vẽ kết quả.
    public static class TeamSelectRows
    {
        private static readonly StaffRole[] Roles = { StaffRole.Cook, StaffRole.Server, StaffRole.Cashier };

        // Theo thứ tự sở hữu.
        public static IReadOnlyList<TeamSelectRow> Build(Roster roster, StarService stars)
        {
            var team = new HashSet<string>(roster.GetBattleTeam().Select(c => c.Id));
            return roster.GetOwned().Select(c => new TeamSelectRow(c, team.Contains(c.Id), RoleOf(roster, c.Id), stars.GetStars(c.Id))).ToList();
        }

        private static StaffRole? RoleOf(Roster roster, string id)
        {
            foreach (var role in Roles)
                if (roster.GetStaff(role).Any(c => c.Id == id)) return role;
            return null;
        }
    }
}
