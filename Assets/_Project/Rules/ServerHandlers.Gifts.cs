#nullable disable
using System.Collections.Generic;

namespace BunDauPeo.Rules
{
    // Quà từ Dashboard (spec đợt 4 mục 4.3). `gifts` là danh sách thô từ nguồn; handler tự Validate.
    // Từ chối luôn đứng trước bước đổi đầu tiên: Gem và ClaimedGiftIds chỉ ghi sau khi đã tính xong.
    public static partial class ServerHandlers
    {
        public static HandlerOutcome ListGifts(ServerState s, IReadOnlyList<GiftData> gifts, ServerContext ctx, string lang)
        {
            var rows = GiftRules.View(s, GiftRules.Validate(gifts), ctx.NowUtc.Ticks, lang);
            return HandlerOutcome.Ok(s, new GiftListPayload { Gifts = rows });
        }

        public static HandlerOutcome ClaimGift(ServerState s, IReadOnlyList<GiftData> gifts, ServerContext ctx, string giftId)
        {
            if (string.IsNullOrEmpty(giftId)) return HandlerOutcome.Reject(CommandCode.Invalid);
            var gift = GiftRules.Validate(gifts).Find(g => g.Id == giftId);
            if (gift == null) return HandlerOutcome.Reject(CommandCode.Invalid);
            if (!GiftRules.InWindow(gift, ctx.NowUtc.Ticks)) return HandlerOutcome.Reject(CommandCode.NotInWindow);
            if (s.ClaimedGiftIds.Contains(gift.Id)) return HandlerOutcome.Reject(CommandCode.AlreadyClaimed);
            return ApplyGifts(s, new List<GiftData> { gift });
        }

        public static HandlerOutcome ClaimAllGifts(ServerState s, IReadOnlyList<GiftData> gifts, ServerContext ctx)
        {
            var pending = new List<GiftData>();
            foreach (var g in GiftRules.Validate(gifts))
                if (GiftRules.InWindow(g, ctx.NowUtc.Ticks) && !s.ClaimedGiftIds.Contains(g.Id)) pending.Add(g);
            if (pending.Count == 0) return HandlerOutcome.Reject(CommandCode.NothingToClaim);
            return ApplyGifts(s, pending);
        }

        static HandlerOutcome ApplyGifts(ServerState s, List<GiftData> claims)
        {
            var payload = new GiftClaimPayload();
            foreach (var g in claims)
                foreach (var r in g.Rewards)
                {
                    if (r.Type == GiftRules.Gem) payload.Gem += r.Amount;
                    else if (r.Type == GiftRules.Coin) payload.Coin += r.Amount;
                    else payload.Material += r.Amount;
                }
            // Tổng tối đa 200 quà x 10 dòng x 1.000.000 nên payload không tràn; Gem của state thì kiểm.
            if (payload.Gem > long.MaxValue - s.Gem) return HandlerOutcome.Reject(CommandCode.Invalid);

            s.Gem += payload.Gem;
            foreach (var g in claims) s.ClaimedGiftIds.Add(g.Id);
            return HandlerOutcome.Ok(s, payload);
        }
    }
}
