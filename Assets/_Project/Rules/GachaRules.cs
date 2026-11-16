#nullable disable
using System;
using System.Collections.Generic;

namespace BunDauPeo.Rules
{
    // Bản C# thuần của GachaRoller (chạy cả trên server). Số lần gọi NextDouble là hợp đồng: một lần chọn độ hiếm
    // (bỏ qua khi pity ép SSR) rồi một lần chọn nhân vật; test kịch bản dựa vào thứ tự này.
    // Độ hiếm là int: 0 R, 1 SR, 2 SSR (giá trị của enum Rarity).
    public sealed class GachaRules
    {
        const int R = 0, SR = 1, SSR = 2;

        readonly Random _random;

        public GachaRules(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        // pulls: số lượt kể từ SSR gần nhất (vào) và bộ đếm mới (ra). Ném lỗi thì pulls có thể đã đổi một phần,
        // nên người gọi phải truyền bản sao.
        public string RollOne(GameData data, BannerData banner, ref int pulls, int? minRarity = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (banner == null) throw new ArgumentNullException(nameof(banner));

            var threshold = banner.SsrPityThreshold;
            var forced = threshold > 0 && pulls >= threshold - 1;
            var rarity = forced ? SSR : PickRarity(banner, minRarity ?? R);
            var id = PickCharacter(data, banner, rarity);

            if (rarity == SSR) pulls = 0;
            else if (pulls < int.MaxValue) pulls++;
            return id;
        }

        public List<string> RollTen(GameData data, BannerData banner, ref int pulls)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (banner == null) throw new ArgumentNullException(nameof(banner));

            var results = new List<string>(10);
            var hasSrOrBetter = false;
            for (var i = 0; i < 10; i++)
            {
                // Nâng SR diễn ra trong chính lượt 10 để bộ đếm đúng khi lượt đó ra SSR.
                var needSr = i == 9 && banner.GuaranteeSrInTen && !hasSrOrBetter;
                var id = RollOne(data, banner, ref pulls, needSr ? SR : (int?)null);
                if (RarityOf(data, id) >= SR) hasSrOrBetter = true;
                results.Add(id);
            }

            return results;
        }

        // Id lạ trả -1 (không bao giờ trúng bất kỳ độ hiếm nào).
        public static int RarityOf(GameData data, string id)
        {
            foreach (var c in data.Characters)
                if (c.Id == id) return c.Rarity;
            return -1;
        }

        static float Weight(BannerData banner, int rarity) => rarity switch
        {
            R => banner.WeightR,
            SR => banner.WeightSR,
            SSR => banner.WeightSSR,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null),
        };

        int PickRarity(BannerData banner, int min)
        {
            var total = 0.0;
            for (var rarity = R; rarity <= SSR; rarity++)
                if (rarity >= min && Weight(banner, rarity) > 0f) total += Weight(banner, rarity);
            if (total <= 0.0) throw new InvalidOperationException($"Banner '{banner.Id}' has no rarity to roll at or above {min}.");

            var u = _random.NextDouble() * total;
            var last = min;
            for (var rarity = R; rarity <= SSR; rarity++)
            {
                // Độ hiếm trọng số 0 hoặc thấp hơn min không bao giờ trúng và không bị trừ vào u.
                if (rarity < min || Weight(banner, rarity) <= 0f) continue;
                last = rarity;
                if (u < Weight(banner, rarity)) return rarity;
                u -= Weight(banner, rarity);
            }

            // Sai số làm tròn (hoặc NextDouble = 1.0) làm vòng duyệt chạy hết: lấy độ hiếm hợp lệ cuối cùng.
            return last;
        }

        string PickCharacter(GameData data, BannerData banner, int rarity)
        {
            var count = 0;
            foreach (var id in banner.CharacterIds)
                if (RarityOf(data, id) == rarity) count++;
            if (count == 0) throw new InvalidOperationException($"Banner '{banner.Id}' has no character of rarity {rarity}.");

            var index = Math.Min((int)(_random.NextDouble() * count), count - 1);
            foreach (var id in banner.CharacterIds)
            {
                if (RarityOf(data, id) != rarity) continue;
                if (index-- == 0) return id;
            }

            throw new InvalidOperationException("unreachable");
        }
    }
}
