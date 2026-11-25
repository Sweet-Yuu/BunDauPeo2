#nullable disable
using System;

namespace BunDauPeo.Rules
{
    // Quay miễn phí mỗi ngày, nhiệm vụ DEV (spec đợt 3 mục 6.1).
    public static partial class ServerHandlers
    {
        public const int MaxDevStamina = 1000;

        public static HandlerOutcome GachaFreePull(ServerState s, GameData data, ServerContext ctx, string bannerId)
        {
            var banner = bannerId == null ? null : data.Banners.Find(b => b.Id == bannerId);
            if (banner == null || !banner.FreePullDaily) return HandlerOutcome.Reject(CommandCode.Invalid);
            var today = MissionRules.DayIndex(ctx.NowUtc);
            if (s.Missions.FreePullDay == today) return HandlerOutcome.Reject(CommandCode.FreePullUsed);

            var payload = RollAndGrant(s, data, ctx, banner, 1, 0);
            MissionRules.RollDay(s.Missions, ctx.NowUtc);
            AddDailyPulls(s, data, 1);
            s.Missions.FreePullDay = today;
            return HandlerOutcome.Ok(s, payload);
        }

        public static HandlerOutcome DevResetMissions(ServerState s)
        {
            s.Missions = new MissionState();
            return HandlerOutcome.Ok(s);
        }

        // Chỉ để playtest hộp xác nhận thể lực (không có trong spec).
        public static HandlerOutcome DevAddStamina(ServerState s, GameData data, ServerContext ctx, int amount)
        {
            if (amount < 1 || amount > MaxDevStamina) return HandlerOutcome.Reject(CommandCode.Invalid);
            var regen = data.Stamina.RegenSeconds <= 0 ? 0 : data.Stamina.RegenSeconds * TimeSpan.TicksPerSecond;
            StaminaRules.Add(s.Stamina, amount, ctx.NowUtc.Ticks, data.Stamina.Max, regen, data.Stamina.RewardCap);
            return HandlerOutcome.Ok(s);
        }
    }
}
