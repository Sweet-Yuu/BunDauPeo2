using System;
using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;

namespace BunDauPeo.UI
{
    public readonly struct CandidateRow
    {
        public CandidateRow(CharacterDefinition character, int stat, double incomeDelta, bool needsConfirm, StaffRole? currentRole)
        {
            Character = character;
            Stat = stat;
            IncomeDelta = incomeDelta;
            NeedsConfirm = needsConfirm;
            CurrentRole = currentRole;
        }

        public CharacterDefinition Character { get; }
        // Chỉ số của vai đang chọn.
        public int Stat { get; }
        // Thu nhập lý thuyết/giây nếu chọn người này, trừ thu nhập hiện tại.
        public double IncomeDelta { get; }
        // Đang ở đội chiến đấu: Roster.AssignStaff sẽ tự gỡ khỏi đội, nên phải hỏi trước.
        public bool NeedsConfirm { get; }
        public StaffRole? CurrentRole { get; }
    }

    // Hàm thuần của panel Nhân viên: view chỉ vẽ kết quả.
    public static class StaffPanelRows
    {
        private static readonly StaffRole[] Roles = { StaffRole.Cook, StaffRole.Server, StaffRole.Cashier };

        // replacingId là người đang ở ô được bấm (null nếu ô trống).
        public static IReadOnlyList<CandidateRow> Candidates(StaffRole role, RestaurantConfig config, RestaurantProfile profile,
            Roster roster, string replacingId)
        {
            var baseline = IncomeCalculator.Estimate(config, profile).CoinsPerSecond;
            var battleTeam = new HashSet<string>(roster.GetBattleTeam().Select(c => c.Id));
            var rows = new List<CandidateRow>();
            foreach (var character in roster.GetOwned())
            {
                var stat = RestaurantProfile.StatOf(character, role);
                var income = IncomeCalculator.Estimate(config, Hypothetical(profile, role, character.Id, stat, replacingId)).CoinsPerSecond;
                rows.Add(new CandidateRow(character, stat, income - baseline, battleTeam.Contains(character.Id), CurrentRoleOf(roster, character.Id)));
            }

            // OrderByDescending ổn định: cùng chỉ số giữ thứ tự sở hữu.
            return rows.OrderByDescending(r => r.Stat).ToList();
        }

        // Đặt người này vào vai, bỏ người bị thay, và bỏ người này khỏi vai cũ nếu đang ở vai khác.
        private static RestaurantProfile Hypothetical(RestaurantProfile profile, StaffRole role, string id, int stat, string replacingId)
        {
            var result = profile;
            foreach (var r in Roles)
            {
                var list = profile.Workers(r)
                    .Where(w => w.CharacterId != id && !(r == role && w.CharacterId == replacingId))
                    .ToList();
                if (r == role) list.Add(new WorkerInfo(id, stat));
                result = result.WithWorkers(r, list);
            }

            return result;
        }

        private static StaffRole? CurrentRoleOf(Roster roster, string id)
        {
            foreach (var role in Roles)
                if (roster.GetStaff(role).Any(c => c.Id == id)) return role;
            return null;
        }

        public static string RoleName(StaffRole role) => UiText.Get(role switch
        {
            StaffRole.Cook => "common.role_cook",
            StaffRole.Server => "common.role_server",
            _ => "common.role_cashier",
        });

        public static string RowLabel(CandidateRow row, StaffRole role)
        {
            var name = string.IsNullOrEmpty(row.Character.DisplayName) ? row.Character.Id : row.Character.DisplayName;
            var label = $"{name} [{row.Character.Rarity}] · {RoleName(role)} {row.Stat} · {DeltaLine(row.IncomeDelta)}";
            if (row.NeedsConfirm) label += " · " + UiText.Get("restaurant.row_battle_team");
            if (row.CurrentRole.HasValue) label += " · " + UiText.Format("restaurant.row_current_role", RoleName(row.CurrentRole.Value));
            return label;
        }

        // Ô chưa mở: gợi ý nâng cấp mở ô đó, lấy tên từ catalog nên đổi tên nâng cấp thì chữ đổi theo.
        public static string LockedHint(RestaurantConfig config, StaffRole role)
        {
            var effect = role switch
            {
                StaffRole.Cook => UpgradeEffect.CookSlots,
                StaffRole.Server => UpgradeEffect.ServeSlots,
                _ => UpgradeEffect.CashierSlots,
            };
            var upgrade = config.Upgrades == null ? null : config.Upgrades.Items.FirstOrDefault(u => u != null && u.Effect == effect);
            return upgrade == null ? UiText.Get("restaurant.locked") : UiText.Format("restaurant.locked_upgrade", upgrade.DisplayName);
        }

        private static string DeltaLine(double delta)
        {
            var rounded = Math.Round(delta, 1, MidpointRounding.AwayFromZero);
            var sign = rounded > 0 ? "+" : rounded < 0 ? "-" : "";
            return UiText.Format("restaurant.delta_line", sign, UiText.Decimal(Math.Abs(rounded), "0.0"));
        }
    }
}
