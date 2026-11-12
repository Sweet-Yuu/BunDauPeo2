using System;
using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Core;

namespace BunDauPeo.UI
{
    // Thứ tự hiện trong hộp Thông báo = thứ tự enum.
    public enum NotifyKind { Incident, StaffMissing, AfkCoins, StaminaFull, StarUp }

    public readonly struct NotifySnapshot
    {
        public NotifySnapshot(bool hasIncident, StaffRole? missingRole, long afkCoins, int stamina, int staminaMax, IReadOnlyList<string> starUpReadyIds)
        {
            HasIncident = hasIncident;
            MissingRole = missingRole;
            AfkCoins = afkCoins;
            Stamina = stamina;
            StaminaMax = staminaMax;
            StarUpReadyIds = starUpReadyIds;
        }

        public bool HasIncident { get; }
        public StaffRole? MissingRole { get; }
        public long AfkCoins { get; }
        public int Stamina { get; }
        public int StaminaMax { get; }
        // Đúng thứ tự màn Nhân vật; null = rỗng.
        public IReadOnlyList<string> StarUpReadyIds { get; }
    }

    public static class NotifyRule
    {
        public static IReadOnlyList<NotifyKind> Rows(NotifySnapshot s)
        {
            var rows = new List<NotifyKind>();
            if (s.HasIncident) rows.Add(NotifyKind.Incident);
            if (s.MissingRole.HasValue) rows.Add(NotifyKind.StaffMissing);
            if (s.AfkCoins > 0) rows.Add(NotifyKind.AfkCoins);
            if (s.StaminaMax > 0 && s.Stamina >= s.StaminaMax) rows.Add(NotifyKind.StaminaFull);
            if (s.StarUpReadyIds != null && s.StarUpReadyIds.Count > 0) rows.Add(NotifyKind.StarUp);
            return rows;
        }

        public static bool HasAny(NotifySnapshot s) => Rows(s).Count > 0;

        public static string Text(NotifyKind kind, NotifySnapshot s) => kind switch
        {
            NotifyKind.Incident => UiText.Get("notify.incident"),
            NotifyKind.StaffMissing => UiText.Format("notify.staff_missing", StaffPanelRows.RoleName(s.MissingRole.GetValueOrDefault())),
            NotifyKind.AfkCoins => UiText.Format("notify.afk", s.AfkCoins),
            NotifyKind.StaminaFull => UiText.Get("notify.stamina_full"),
            NotifyKind.StarUp => UiText.Format("notify.star_up", s.StarUpReadyIds?.Count ?? 0),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        // Giữ id có cost > 0 (chưa tối đa sao) và đủ mảnh, theo thứ tự đưa vào.
        public static IReadOnlyList<string> StarUpReady(IEnumerable<string> orderedIds, Func<string, int> nextCost, Func<string, int> fragments) =>
            orderedIds.Where(id => nextCost(id) > 0 && fragments(id) >= nextCost(id)).ToList();
    }
}
