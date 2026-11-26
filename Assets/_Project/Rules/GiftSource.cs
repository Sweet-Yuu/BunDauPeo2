#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace BunDauPeo.Rules
{
    // Nguồn danh sách quà thô (Custom data trên server, file gifts.json ở máy Local). Đọc lỗi thì ném lỗi, không trả rỗng.
    public interface IGiftSource
    {
        Task<IReadOnlyList<GiftData>> LoadAsync();
    }

    // Hình JSON của Dashboard (spec đợt 4 mục 4.1): tên field chữ thường, ngày ISO. Field công khai để JsonUtility và Newtonsoft đọc cùng.
    [Serializable]
    public class GiftJsonReward
    {
        public string type = "";
        public long amount;
    }

    [Serializable]
    public class GiftJson
    {
        public string id = "";
        public string titleVi = "";
        public string titleEn = "";
        public List<GiftJsonReward> rewards = new();
        public string startUtc = "";
        public string endUtc = "";

        // Ngày không đọc được thì để 0: GiftRules.Validate bỏ quà đó (endUtc <= startUtc).
        public GiftData ToGift()
        {
            var g = new GiftData { Id = id, TitleVi = titleVi, TitleEn = titleEn, StartUtcTicks = Ticks(startUtc), EndUtcTicks = Ticks(endUtc) };
            if (rewards != null)
                foreach (var r in rewards) g.Rewards.Add(r == null ? null : new GiftReward { Type = r.type, Amount = r.amount });
            return g;
        }

        static long Ticks(string iso) =>
            DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d)
                ? d.Ticks : 0;
    }
}
