using System;
using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Core;
using BunDauPeo.Missions;

namespace BunDauPeo.UI
{
    // Thứ tự hiện trong hộp Thông báo = thứ tự enum.
    public enum NotifyKind { Incident, StaffMissing, AfkCoins, StaminaFull, StarUp, MissionReward, GiftReward }

    public readonly struct NotifySnapshot
    {
        public NotifySnapshot(bool hasIncident, StaffRole? missingRole, long afkCoins, int stamina, int staminaMax, IReadOnlyList<string> starUpReadyIds,
            bool dailyMissionReward = false, bool newbieMissionReward = false, bool hasGiftReward = false)
        {
            HasIncident = hasIncident;
            MissingRole = missingRole;
            AfkCoins = afkCoins;
            Stamina = stamina;
            StaminaMax = staminaMax;
            StarUpReadyIds = starUpReadyIds;
            DailyMissionReward = dailyMissionReward;
            NewbieMissionReward = newbieMissionReward;
            HasGiftReward = hasGiftReward;
        }

        public bool HasIncident { get; }
        public StaffRole? MissingRole { get; }
        public long AfkCoins { get; }
        public int Stamina { get; }
        public int StaminaMax { get; }
        // Đúng thứ tự màn Nhân vật; null = rỗng.
        public IReadOnlyList<string> StarUpReadyIds { get; }
        // Thưởng nhiệm vụ nhận được ở từng tab; quà chưa nhận trong hạn (đợt 4).
        public bool DailyMissionReward { get; }
        public bool NewbieMissionReward { get; }
        public bool HasGiftReward { get; }
        public bool HasMissionReward => DailyMissionReward || NewbieMissionReward;
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
            if (s.HasMissionReward) rows.Add(NotifyKind.MissionReward);
            if (s.HasGiftReward) rows.Add(NotifyKind.GiftReward);
            return rows;
        }

        // Bấm dòng thưởng nhiệm vụ: tab Ngày nếu tab đó có thưởng, không thì Tân thủ .
        public static int MissionTab(NotifySnapshot s) => s.DailyMissionReward ? MissionService.TabDaily : MissionService.TabNewbie;

        public static bool HasAny(NotifySnapshot s) => Rows(s).Count > 0;

        public static string Text(NotifyKind kind, NotifySnapshot s) => kind switch
        {
            NotifyKind.Incident => UiText.Get("notify.incident"),
            NotifyKind.StaffMissing => UiText.Format("notify.staff_missing", StaffPanelRows.RoleName(s.MissingRole.GetValueOrDefault())),
            NotifyKind.AfkCoins => UiText.Format("notify.afk", s.AfkCoins),
            NotifyKind.StaminaFull => UiText.Get("notify.stamina_full"),
            NotifyKind.StarUp => UiText.Format("notify.star_up", s.StarUpReadyIds?.Count ?? 0),
            NotifyKind.MissionReward => UiText.Get("notify.mission_reward"),
            NotifyKind.GiftReward => UiText.Get("notify.gift_reward"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        // Giữ id có cost > 0 (chưa tối đa sao) và đủ mảnh, theo thứ tự đưa vào.
        public static IReadOnlyList<string> StarUpReady(IEnumerable<string> orderedIds, Func<string, int> nextCost, Func<string, int> fragments) =>
            orderedIds.Where(id => nextCost(id) > 0 && fragments(id) >= nextCost(id)).ToList();
    }
}
