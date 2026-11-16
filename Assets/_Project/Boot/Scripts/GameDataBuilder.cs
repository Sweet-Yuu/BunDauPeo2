using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using BunDauPeo.Rules;
using UnityEngine;

namespace BunDauPeo.Boot
{
    // Dựng GameData (dữ liệu cân bằng server dùng) từ các SO của game; xuất ra GameData.json bằng ServerDataExport.
    public static class GameDataBuilder
    {
        public static GameData Build(CharacterCatalog characters, GachaBannerCatalog banners, StarConfig stars,
            CombatConfig combat, NewGameConfig newGame)
        {
            var data = new GameData { MaxStars = StarConfig.MaxStars, MaxTeamSize = Roster.MaxBattleTeamSize };

            foreach (var c in characters.Items)
                data.Characters.Add(new CharacterData { Id = c.Id, Rarity = (int)c.Rarity, BaseStars = c.Stars });

            foreach (var b in banners.Items)
            {
                var banner = new BannerData
                {
                    Id = b.Id,
                    SingleCostGem = Sum(b.SingleCost, CurrencyType.Gem),
                    TenCostGem = Sum(b.TenCost, CurrencyType.Gem),
                    WeightR = b.RarityWeights.R,
                    WeightSR = b.RarityWeights.SR,
                    WeightSSR = b.RarityWeights.SSR,
                    SsrPityThreshold = b.SsrPityThreshold,
                    GuaranteeSrInTen = b.GuaranteeSrInTen,
                    DupFragmentsR = b.DuplicateFragments.R,
                    DupFragmentsSR = b.DuplicateFragments.SR,
                    DupFragmentsSSR = b.DuplicateFragments.SSR,
                };
                foreach (var c in b.Characters) banner.CharacterIds.Add(c.Id);
                data.Banners.Add(banner);
            }

            // Phần tử i là giá để lên (i + 2) sao.
            for (var target = 2; target <= StarConfig.MaxStars; target++) data.StarCosts.Add(stars.CostToReach(target));

            data.Stamina = new StaminaData { Max = combat.MaxStamina, RegenSeconds = combat.StaminaRegenSeconds };

            foreach (var s in combat.Stages.Items)
            {
                data.Stages.Add(new StageData
                {
                    Id = s.Id,
                    Kind = (int)s.Kind,
                    StaminaCost = s.StaminaCost,
                    FirstClear = Reward(s.FirstClearReward),
                    Repeat = Reward(s.RepeatReward),
                });
            }

            data.NewGame.StartingGem = Sum(newGame.StartingCurrency, CurrencyType.Gem);
            foreach (var c in newGame.StarterCharacters) data.NewGame.StarterCharacterIds.Add(c.Id);
            return data;
        }

        // Server chỉ biết Gem và Material: Coin chỉ đến từ quán, giá gacha chỉ tính Gem.
        public static List<string> Validate(CharacterCatalog characters, GachaBannerCatalog banners, StarConfig stars,
            CombatConfig combat, NewGameConfig newGame)
        {
            var errors = new List<string>();

            foreach (var b in banners.Items)
            {
                CheckCost(errors, $"Banner {b.Id}: SingleCost", b.SingleCost);
                CheckCost(errors, $"Banner {b.Id}: TenCost", b.TenCost);
            }

            foreach (var s in combat.Stages.Items)
            {
                CheckReward(errors, $"Stage {s.Id}: FirstClearReward", s.FirstClearReward);
                CheckReward(errors, $"Stage {s.Id}: RepeatReward", s.RepeatReward);
            }

            if (newGame.StartingCurrency == null) errors.Add("NewGame: StartingCurrency is unreadable");
            else
                foreach (var line in newGame.StartingCurrency)
                    if (!System.Enum.IsDefined(typeof(CurrencyType), line.Type))
                        errors.Add($"NewGame: StartingCurrency has unknown type {(int)line.Type}");

            return errors;
        }

        public static string ToJson(GameData data) => JsonUtility.ToJson(data);

        private static long Sum(IReadOnlyList<CurrencyAmount> lines, CurrencyType type)
        {
            long total = 0;
            if (lines == null) return total;
            foreach (var line in lines)
                if (line.Type == type) total += line.Amount;
            return total;
        }

        private static RewardData Reward(IReadOnlyList<CurrencyAmount> lines) =>
            new() { Gem = Sum(lines, CurrencyType.Gem), Material = Sum(lines, CurrencyType.Material) };

        private static void CheckCost(List<string> errors, string label, IReadOnlyList<CurrencyAmount> lines)
        {
            foreach (var line in lines)
                if (line.Type != CurrencyType.Gem) errors.Add($"{label} must be Gem only, found {line.Type}");
        }

        private static void CheckReward(List<string> errors, string label, IReadOnlyList<CurrencyAmount> lines)
        {
            foreach (var line in lines)
                if (line.Type != CurrencyType.Gem && line.Type != CurrencyType.Material)
                    errors.Add($"{label} must be Gem/Material only, found {line.Type}");
        }
    }
}
