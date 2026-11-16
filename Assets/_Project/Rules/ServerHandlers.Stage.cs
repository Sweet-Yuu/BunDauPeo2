#nullable disable
using System;
using System.Collections.Generic;
using BunDauPeo.Core;

namespace BunDauPeo.Rules
{
    [Serializable]
    public class EnterStagePayload
    {
        public string Ticket = "";
        public int StaminaCost;
    }

    [Serializable]
    public class StageRewardPayload
    {
        public bool FirstClear;
        public long Gem;
        public long Material;
    }

    // Màn, thể lực, reset. Handler nhận state đã là bản sao (CommandRunner); từ chối thì không để lại thay đổi nào trên state.
    public static partial class ServerHandlers
    {
        const int StageKindBattle = 0;
        const int StageKindStory = 1;

        public static HandlerOutcome EnterStage(ServerState s, GameData data, ServerContext ctx, string stageId, List<string> team)
        {
            var index = StageIndex(data, stageId);
            if (index < 0) return HandlerOutcome.Reject(CommandCode.Invalid);
            var stage = data.Stages[index];
            if (stage.Kind != StageKindBattle) return HandlerOutcome.Reject(CommandCode.WrongKind);
            if (!IsStageOpen(s, data, index)) return HandlerOutcome.Reject(CommandCode.Locked);
            if (team == null || team.Count == 0) return HandlerOutcome.Reject(CommandCode.EmptyTeam);
            if (team.Count > data.MaxTeamSize || new HashSet<string>(team).Count != team.Count)
                return HandlerOutcome.Reject(CommandCode.Invalid);
            foreach (var id in team)
                if (id == null || !s.OwnedCharacterIds.Contains(id)) return HandlerOutcome.Reject(CommandCode.NotOwned);

            // Chốt và trừ trên bản nháp: bị từ chối thì s không dính phần chốt hồi.
            var stamina = new StaminaState { Current = s.Stamina.Current, AnchorUtcTicks = s.Stamina.AnchorUtcTicks };
            var regen = data.Stamina.RegenSeconds <= 0 ? 0 : data.Stamina.RegenSeconds * TimeSpan.TicksPerSecond;
            if (!StaminaRules.TrySpend(stamina, stage.StaminaCost, ctx.NowUtc.Ticks, data.Stamina.Max, regen))
                return HandlerOutcome.Reject(CommandCode.NotEnoughStamina);

            s.Stamina = stamina;
            s.PendingTicket = ctx.NewId();
            s.PendingStageId = stageId;
            return HandlerOutcome.Ok(s, new EnterStagePayload { Ticket = s.PendingTicket, StaminaCost = stage.StaminaCost });
        }

        public static HandlerOutcome CompleteStage(ServerState s, GameData data, string ticket, bool won)
        {
            // Hai chuỗi rỗng không được coi là khớp.
            if (string.IsNullOrEmpty(ticket) || ticket != s.PendingTicket) return HandlerOutcome.Reject(CommandCode.TicketMismatch);
            var index = StageIndex(data, s.PendingStageId);
            if (index < 0) return HandlerOutcome.Reject(CommandCode.Invalid);

            s.PendingTicket = "";
            s.PendingStageId = "";
            if (!won) return HandlerOutcome.Ok(s, new StageRewardPayload());

            var stage = data.Stages[index];
            var first = !s.ClearedStages.Contains(stage.Id);
            var reward = first ? stage.FirstClear : stage.Repeat;
            s.Gem += reward.Gem;
            if (first) s.ClearedStages.Add(stage.Id);
            return HandlerOutcome.Ok(s, new StageRewardPayload { FirstClear = first, Gem = reward.Gem, Material = reward.Material });
        }

        public static HandlerOutcome CompleteStory(ServerState s, GameData data, string stageId)
        {
            var index = StageIndex(data, stageId);
            if (index < 0) return HandlerOutcome.Reject(CommandCode.Invalid);
            if (data.Stages[index].Kind != StageKindStory) return HandlerOutcome.Reject(CommandCode.WrongKind);
            if (!IsStageOpen(s, data, index)) return HandlerOutcome.Reject(CommandCode.Locked);
            if (s.ClearedStages.Contains(stageId)) return HandlerOutcome.Reject(CommandCode.AlreadyCleared);

            s.ClearedStages.Add(stageId);
            return HandlerOutcome.Ok(s, null);
        }

        public static ServerState ResetState(GameData data, string keepSessionId)
        {
            var state = ServerState.NewGame(data);
            state.ActiveSessionId = keepSessionId;
            return state;
        }

        static int StageIndex(GameData data, string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            for (var i = 0; i < data.Stages.Count; i++)
                if (data.Stages[i].Id == id) return i;
            return -1;
        }

        // Màn thứ i mở khi i = 0, màn i-1 đã qua, hoặc chính nó đã qua (chèn màn vào giữa không khoá lại người đã đi qua).
        static bool IsStageOpen(ServerState s, GameData data, int index)
        {
            if (index == 0 || s.ClearedStages.Contains(data.Stages[index].Id)) return true;
            return s.ClearedStages.Contains(data.Stages[index - 1].Id);
        }
    }
}
