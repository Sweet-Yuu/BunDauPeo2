#nullable disable
using System;
using System.Collections.Generic;
using BunDauPeo.Core;

namespace BunDauPeo.Rules
{
    [Serializable]
    public class MissionClaimPayload
    {
        public long Gem;
        public int Stamina;
        public int StaminaWasted;
    }

    // Nhận thưởng nhiệm vụ (spec đợt 3 mục 6.1). Từ chối luôn đứng trước bước đổi đầu tiên: kiểm bằng MissionRules.View (không sửa state).
    public static partial class ServerHandlers
    {
        public static HandlerOutcome ClaimMission(ServerState s, GameData data, ServerContext ctx, string missionId, List<string> clientDone)
        {
            var def = string.IsNullOrEmpty(missionId) ? null : data.Missions.Find(d => d.Id == missionId);
            if (def == null) return HandlerOutcome.Reject(CommandCode.Invalid);

            var row = MissionRules.View(s, data, ctx.NowUtc, clientDone).Find(r => r.Id == missionId);
            if (row.Locked) return HandlerOutcome.Reject(CommandCode.Locked);
            if (row.Claimed) return HandlerOutcome.Reject(CommandCode.AlreadyClaimed);
            if (row.Progress < row.Target) return HandlerOutcome.Reject(CommandCode.NotReady);
            if (row.WindowState == 2 || row.WindowState == 3) return HandlerOutcome.Reject(CommandCode.NotInWindow);

            return ApplyClaims(s, data, ctx, new List<MissionData> { def });
        }

        public static HandlerOutcome ClaimAllMissions(ServerState s, GameData data, ServerContext ctx, int tab, List<string> clientDone)
        {
            var ready = new List<MissionData>();
            foreach (var row in MissionRules.View(s, data, ctx.NowUtc, clientDone))
            {
                if (!row.Claimable) continue;
                var def = data.Missions.Find(d => d.Id == row.Id);
                if (def.Tab == tab) ready.Add(def);
            }
            if (ready.Count == 0) return HandlerOutcome.Reject(CommandCode.NothingToClaim);
            return ApplyClaims(s, data, ctx, ready);
        }

        static HandlerOutcome ApplyClaims(ServerState s, GameData data, ServerContext ctx, List<MissionData> claims)
        {
            // Thể lực chốt trên bản nháp như EnterStage; Gem và danh sách đã nhận chỉ ghi khi mọi thứ đã tính xong.
            var stamina = new StaminaState { Current = s.Stamina.Current, AnchorUtcTicks = s.Stamina.AnchorUtcTicks };
            var regen = data.Stamina.RegenSeconds <= 0 ? 0 : data.Stamina.RegenSeconds * TimeSpan.TicksPerSecond;
            var payload = new MissionClaimPayload();
            foreach (var d in claims)
            {
                payload.Gem += d.RewardGem;
                if (d.RewardStamina <= 0) continue;
                var granted = StaminaRules.Add(stamina, d.RewardStamina, ctx.NowUtc.Ticks, data.Stamina.Max, regen, data.Stamina.RewardCap);
                payload.Stamina += granted;
                payload.StaminaWasted += d.RewardStamina - granted;
            }

            MissionRules.RollDay(s.Missions, ctx.NowUtc);
            s.Gem += payload.Gem;
            s.Stamina = stamina;
            foreach (var d in claims) (d.Tab == 0 ? s.Missions.DailyClaimed : s.Missions.NewbieClaimed).Add(d.Id);
            return HandlerOutcome.Ok(s, payload);
        }
    }
}
