using System;
using System.Collections.Generic;
using BunDauPeo.Characters;

namespace BunDauPeo.Gacha
{
    // Thuần logic: không biết ví hay save. Số lần gọi NextDouble là hợp đồng: một lần chọn độ hiếm
    // (bỏ qua khi pity ép SSR) rồi một lần chọn nhân vật; test kịch bản dựa vào thứ tự này.
    public sealed class GachaRoller
    {
        private static readonly Rarity[] Rarities = { Rarity.R, Rarity.SR, Rarity.SSR };

        private readonly Random _random;

        public GachaRoller(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        // pulls: số lượt kể từ SSR gần nhất (vào) và bộ đếm mới (ra). Ném lỗi thì pulls có thể đã đổi một phần,
        // nên người gọi phải truyền bản sao.
        public CharacterDefinition RollOne(GachaBannerDefinition banner, ref int pulls, Rarity? minRarity = null)
        {
            if (banner == null) throw new ArgumentNullException(nameof(banner));

            var threshold = banner.SsrPityThreshold;
            var forced = threshold > 0 && pulls >= threshold - 1;
            var rarity = forced ? Rarity.SSR : PickRarity(banner, minRarity ?? Rarity.R);
            var character = PickCharacter(banner, rarity);

            if (rarity == Rarity.SSR) pulls = 0;
            else if (pulls < int.MaxValue) pulls++;
            return character;
        }

        public IReadOnlyList<CharacterDefinition> RollTen(GachaBannerDefinition banner, ref int pulls)
        {
            if (banner == null) throw new ArgumentNullException(nameof(banner));

            var results = new List<CharacterDefinition>(10);
            var hasSrOrBetter = false;
            for (var i = 0; i < 10; i++)
            {
                // Nâng SR diễn ra trong chính lượt 10 để bộ đếm đúng khi lượt đó ra SSR.
                var needSr = i == 9 && banner.GuaranteeSrInTen && !hasSrOrBetter;
                var character = RollOne(banner, ref pulls, needSr ? Rarity.SR : null);
                if (character.Rarity >= Rarity.SR) hasSrOrBetter = true;
                results.Add(character);
            }

            return results;
        }

        private Rarity PickRarity(GachaBannerDefinition banner, Rarity min)
        {
            var weights = banner.RarityWeights;
            var total = 0.0;
            foreach (var rarity in Rarities)
                if (rarity >= min && weights.Get(rarity) > 0f) total += weights.Get(rarity);
            if (total <= 0.0) throw new InvalidOperationException($"Banner '{banner.Id}' has no rarity to roll at or above {min}.");

            var u = _random.NextDouble() * total;
            var last = min;
            foreach (var rarity in Rarities)
            {
                // Độ hiếm trọng số 0 hoặc thấp hơn min không bao giờ trúng và không bị trừ vào u.
                if (rarity < min || weights.Get(rarity) <= 0f) continue;
                last = rarity;
                if (u < weights.Get(rarity)) return rarity;
                u -= weights.Get(rarity);
            }

            // Sai số làm tròn (hoặc NextDouble = 1.0) làm vòng duyệt chạy hết: lấy độ hiếm hợp lệ cuối cùng.
            return last;
        }

        private CharacterDefinition PickCharacter(GachaBannerDefinition banner, Rarity rarity)
        {
            var characters = banner.Characters;
            var count = 0;
            foreach (var character in characters)
                if (character != null && character.Rarity == rarity) count++;
            if (count == 0) throw new InvalidOperationException($"Banner '{banner.Id}' has no {rarity} character.");

            var index = Math.Min((int)(_random.NextDouble() * count), count - 1);
            foreach (var character in characters)
            {
                if (character == null || character.Rarity != rarity) continue;
                if (index-- == 0) return character;
            }

            throw new InvalidOperationException("unreachable");
        }
    }
}
