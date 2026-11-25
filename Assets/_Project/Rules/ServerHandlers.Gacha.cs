#nullable disable
using System;
using System.Collections.Generic;
using BunDauPeo.Core;

namespace BunDauPeo.Rules
{
    [Serializable]
    public class PulledEntry
    {
        public string CharacterId = "";
        public bool IsNew;
        public long Fragments;
    }

    [Serializable]
    public class GachaPullPayload
    {
        public List<PulledEntry> Pulls = new();
    }

    [Serializable]
    public class StarUpPayload
    {
        public int NewStars;
    }

    // Handler nhận state ĐÃ là bản sao (CommandRunner.Run đã Clone): sửa thẳng rồi trả Ok(state, payload);
    // từ chối thì trả Reject(code) và người gọi bỏ bản sao. Từ chối luôn đứng trước bước đổi đầu tiên.
    public static partial class ServerHandlers
    {
        public const int MaxHistoryEntries = 50;
        public const long MaxDevGem = 100000;

        // Không qua CommandRunner (chưa có phiên): luôn cấp phiên mới và tăng Revision tại đây.
        public static HandlerOutcome Bootstrap(ServerState stateOrNull, GameData data, ServerContext ctx)
        {
            var state = stateOrNull == null ? ServerState.NewGame(data) : stateOrNull.Clone();
            state.ActiveSessionId = ctx.NewId();
            state.Revision++;
            return HandlerOutcome.Ok(state);
        }

        public static HandlerOutcome GachaPull(ServerState s, GameData data, ServerContext ctx, string bannerId, int count)
        {
            var banner = bannerId == null ? null : data.Banners.Find(b => b.Id == bannerId);
            if (banner == null || (count != 1 && count != 10)) return HandlerOutcome.Reject(CommandCode.Invalid);

            var cost = count == 10 ? banner.TenCostGem : banner.SingleCostGem;
            if (s.Gem < cost) return HandlerOutcome.Reject(CommandCode.NotEnoughGem);

            var payload = RollAndGrant(s, data, ctx, banner, count, cost);
            MissionRules.RollDay(s.Missions, ctx.NowUtc);
            AddDailyPulls(s, data, count);
            return HandlerOutcome.Ok(s, payload);
        }

        // Dùng chung cho quay trả Gem và quay miễn phí (cost = 0): cùng roll, pity, lịch sử.
        static GachaPullPayload RollAndGrant(ServerState s, GameData data, ServerContext ctx, BannerData banner, int count, long cost)
        {
            // Roll trên bản sao bộ đếm: RNG ném lỗi thì chưa có gì đổi để hoàn lại.
            var pulls = GetPity(s, banner.Id);
            var rules = new GachaRules(ctx.Random);
            List<string> drawn;
            if (count == 10) drawn = rules.RollTen(data, banner, ref pulls);
            else drawn = new List<string> { rules.RollOne(data, banner, ref pulls) };

            s.Gem -= cost;
            var payload = new GachaPullPayload();
            foreach (var id in drawn) payload.Pulls.Add(Grant(s, banner, id, GachaRules.RarityOf(data, id)));
            SetPity(s, banner.Id, pulls);
            AppendHistory(s, banner.Id, drawn, ctx.NowUtc.Ticks);
            return payload;
        }

        static void AddDailyPulls(ServerState s, GameData data, int count) =>
            s.Missions.DailyPulls = Math.Min(s.Missions.DailyPulls + count, MissionRules.CounterCap(data, MissionRules.CounterPulls));

        public static HandlerOutcome StarUp(ServerState s, GameData data, string characterId)
        {
            var character = characterId == null ? null : data.Characters.Find(c => c.Id == characterId);
            if (character == null) return HandlerOutcome.Reject(CommandCode.Invalid);
            if (!s.OwnedCharacterIds.Contains(characterId)) return HandlerOutcome.Reject(CommandCode.NotOwned);

            var starUpIndex = s.StarUps.FindIndex(e => e.CharacterId == characterId);
            var ups = starUpIndex < 0 ? 0 : Math.Max(s.StarUps[starUpIndex].Count, 0);
            var stars = character.BaseStars + ups;
            if (stars >= data.MaxStars) return HandlerOutcome.Reject(CommandCode.MaxStars);

            // Sao gốc < 1 cho chỉ số âm: không được nâng miễn phí (như InvalidConfig của StarService).
            var costIndex = stars + 1 - 2;
            if (costIndex < 0 || costIndex >= data.StarCosts.Count || data.StarCosts[costIndex] <= 0)
                return HandlerOutcome.Reject(CommandCode.Invalid);
            var cost = data.StarCosts[costIndex];

            var fragIndex = s.Fragments.FindIndex(f => f.CharacterId == characterId);
            if (fragIndex < 0 || s.Fragments[fragIndex].Count < cost) return HandlerOutcome.Reject(CommandCode.NotEnoughFragments);

            s.Fragments[fragIndex] = new FragmentCount { CharacterId = characterId, Count = s.Fragments[fragIndex].Count - cost };
            var entry = new StarUpEntry { CharacterId = characterId, Count = ups + 1 };
            if (starUpIndex < 0) s.StarUps.Add(entry);
            else s.StarUps[starUpIndex] = entry;
            return HandlerOutcome.Ok(s, new StarUpPayload { NewStars = stars + 1 });
        }

        public static HandlerOutcome DevAddGem(ServerState s, long amount)
        {
            if (amount < 1 || amount > MaxDevGem) return HandlerOutcome.Reject(CommandCode.Invalid);
            s.Gem += amount;
            return HandlerOutcome.Ok(s);
        }

        public static HandlerOutcome DevRefillStamina(ServerState s, GameData data)
        {
            s.Stamina.Current = data.Stamina.Max;
            s.Stamina.AnchorUtcTicks = 0;
            return HandlerOutcome.Ok(s);
        }

        static int GetPity(ServerState s, string bannerId)
        {
            var index = s.GachaPity.FindIndex(p => p.BannerId == bannerId);
            return index < 0 ? 0 : Math.Max(0, s.GachaPity[index].Pulls);
        }

        static void SetPity(ServerState s, string bannerId, int pulls)
        {
            var entry = new PityEntry { BannerId = bannerId, Pulls = pulls };
            var index = s.GachaPity.FindIndex(p => p.BannerId == bannerId);
            if (index < 0) s.GachaPity.Add(entry);
            else s.GachaPity[index] = entry;
        }

        // "Đã sở hữu" kiểm sau mỗi lần trao, nên bản thứ hai của cùng nhân vật trong lần quay 10 là trùng.
        static PulledEntry Grant(ServerState s, BannerData banner, string id, int rarity)
        {
            if (!s.OwnedCharacterIds.Contains(id))
            {
                s.OwnedCharacterIds.Add(id);
                return new PulledEntry { CharacterId = id, IsNew = true };
            }

            var fragments = rarity switch { 0 => banner.DupFragmentsR, 1 => banner.DupFragmentsSR, _ => banner.DupFragmentsSSR };
            if (fragments <= 0) return new PulledEntry { CharacterId = id };

            var index = s.Fragments.FindIndex(f => f.CharacterId == id);
            if (index < 0)
            {
                s.Fragments.Add(new FragmentCount { CharacterId = id, Count = fragments });
            }
            else
            {
                var current = s.Fragments[index].Count;
                var updated = current > long.MaxValue - fragments ? long.MaxValue : current + fragments;
                s.Fragments[index] = new FragmentCount { CharacterId = id, Count = updated };
            }

            return new PulledEntry { CharacterId = id, Fragments = fragments };
        }

        static void AppendHistory(ServerState s, string bannerId, List<string> drawn, long utcTicks)
        {
            s.GachaHistory.Add(new GachaHistoryEntry
            {
                Utc = utcTicks,
                BannerId = bannerId,
                Count = drawn.Count,
                CharacterIds = drawn.ToArray(),
            });
            if (s.GachaHistory.Count > MaxHistoryEntries)
                s.GachaHistory.RemoveRange(0, s.GachaHistory.Count - MaxHistoryEntries);
        }
    }
}
