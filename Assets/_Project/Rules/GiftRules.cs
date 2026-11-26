#nullable disable
using System.Collections.Generic;

namespace BunDauPeo.Rules
{
    // Kiểm quà, hạn, view. Dùng chung máy và server; không sửa state.
    public static class GiftRules
    {
        public const int MaxGifts = 200, MaxRewards = 10, MaxIdLength = 40;
        public const long MaxAmount = 1_000_000;

        public const string Gem = "Gem", Coin = "Coin", Material = "Material";

        // startUtc <= now < endUtc.
        public static bool InWindow(GiftData g, long nowTicks) => nowTicks >= g.StartUtcTicks && nowTicks < g.EndUtcTicks;

        static bool IsIdChar(char c) => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';

        static bool IsValid(GiftData g)
        {
            if (g == null || string.IsNullOrEmpty(g.Id) || g.Id.Length > MaxIdLength) return false;
            foreach (var c in g.Id) if (!IsIdChar(c)) return false;
            if (g.EndUtcTicks <= g.StartUtcTicks) return false;
            if (string.IsNullOrEmpty(g.TitleVi) && string.IsNullOrEmpty(g.TitleEn)) return false;
            if (g.Rewards == null || g.Rewards.Count == 0 || g.Rewards.Count > MaxRewards) return false;
            foreach (var r in g.Rewards)
            {
                if (r == null || r.Amount < 1 || r.Amount > MaxAmount) return false;
                if (r.Type != Gem && r.Type != Coin && r.Type != Material) return false;
            }
            return true;
        }

        // Bỏ riêng quà vi phạm; trùng id giữ quà đầu; tối đa MaxGifts quà đầu. Không ném lỗi với null.
        public static List<GiftData> Validate(IReadOnlyList<GiftData> raw)
        {
            var result = new List<GiftData>();
            if (raw == null) return result;
            var seen = new HashSet<string>();
            for (var i = 0; i < raw.Count && i < MaxGifts; i++)
                if (IsValid(raw[i]) && seen.Add(raw[i].Id)) result.Add(raw[i]);
            return result;
        }

        // Chỉ quà trong hạn, giữ thứ tự. lang "en" ưu tiên TitleEn, còn lại TitleVi; thiếu thì dùng bản kia.
        public static List<GiftRow> View(ServerState s, IReadOnlyList<GiftData> valid, long nowTicks, string lang)
        {
            var rows = new List<GiftRow>();
            foreach (var g in valid)
            {
                if (!InWindow(g, nowTicks)) continue;
                var vi = !string.IsNullOrEmpty(g.TitleVi);
                var en = !string.IsNullOrEmpty(g.TitleEn);
                var title = lang == "en" ? (en ? g.TitleEn : g.TitleVi) : (vi ? g.TitleVi : g.TitleEn);
                rows.Add(new GiftRow
                {
                    Id = g.Id, Title = title, Rewards = new List<GiftReward>(g.Rewards),
                    EndUtcTicks = g.EndUtcTicks, Claimed = s.ClaimedGiftIds.Contains(g.Id),
                });
            }
            return rows;
        }
    }
}
