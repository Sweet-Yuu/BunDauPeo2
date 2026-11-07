using System;
using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Gacha
{
    // Số khởi điểm theo spec Gacha 3.3; chỉnh trong Inspector, không cần sửa code.
    [CreateAssetMenu(menuName = "BunDauPeo/Gacha/Banner")]
    public class GachaBannerDefinition : GameDefinition
    {
        [SerializeField] private string _displayName = "Banner Thường";
        [SerializeField] private string _displayNameEn = "Standard Banner";
        [SerializeField] private CurrencyAmount[] _singleCost = { new(CurrencyType.Gem, 100) };
        [SerializeField] private CurrencyAmount[] _tenCost = { new(CurrencyType.Gem, 900) };
        [SerializeField] private RarityWeights _rarityWeights = new() { R = 70f, SR = 25f, SSR = 5f };
        [SerializeField] private int _ssrPityThreshold = 50;
        [SerializeField] private bool _guaranteeSrInTen = true;
        [SerializeField] private RarityCounts _duplicateFragments = new() { R = 1, SR = 1, SSR = 1 };
        [SerializeField] private CharacterDefinition[] _characters = Array.Empty<CharacterDefinition>();

        public string DisplayName => GameLanguage.Pick(_displayName, _displayNameEn);
        public CurrencyAmount[] SingleCost => _singleCost;
        public CurrencyAmount[] TenCost => _tenCost;
        public RarityWeights RarityWeights => _rarityWeights;
        public int SsrPityThreshold => _ssrPityThreshold;
        public bool GuaranteeSrInTen => _guaranteeSrInTen;
        public RarityCounts DuplicateFragments => _duplicateFragments;
        public IReadOnlyList<CharacterDefinition> Characters => _characters;

        private static readonly Rarity[] AllRarities = { Rarity.R, Rarity.SR, Rarity.SSR };

        // Mỗi lỗi bắt đầu bằng tên field ("SingleCost: ..."); lỗi thiếu nhân vật là "Characters (SR): ...".
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Id)) errors.Add("Id: must not be blank");

            CheckCost(errors, "SingleCost", _singleCost);
            CheckCost(errors, "TenCost", _tenCost);

            var weightsValid = true;
            foreach (var rarity in AllRarities)
            {
                if (IsValidWeight(_rarityWeights.Get(rarity))) continue;
                weightsValid = false;
                errors.Add($"RarityWeights: {rarity} weight must be a finite number >= 0");
            }

            if (weightsValid && !Array.Exists(AllRarities, r => _rarityWeights.Get(r) > 0f))
                errors.Add("RarityWeights: at least one weight must be > 0");

            foreach (var rarity in AllRarities)
                if (_duplicateFragments.Get(rarity) < 0) errors.Add($"DuplicateFragments: {rarity} must be >= 0");

            for (var i = 0; i < _characters.Length; i++)
                if (_characters[i] == null) errors.Add($"Characters: element {i} is missing");

            foreach (var rarity in AllRarities)
                if (_rarityWeights.Get(rarity) > 0f && !HasCharacter(rarity))
                    errors.Add($"Characters ({rarity}): {rarity} has weight > 0 but the banner has no {rarity} character");

            if (_ssrPityThreshold < 0) errors.Add("SsrPityThreshold: must be >= 0 (0 turns pity off)");
            if (_ssrPityThreshold > 0 && !HasCharacter(Rarity.SSR))
                errors.Add("SsrPityThreshold: pity is on but the banner has no SSR character");

            if (_guaranteeSrInTen && !CanRoll(Rarity.SR) && !CanRoll(Rarity.SSR))
                errors.Add("GuaranteeSrInTen: no SR or SSR can be rolled (weight 0 or no character)");

            return errors;
        }

        private static bool IsValidWeight(float weight) => weight >= 0f && !float.IsInfinity(weight);

        private static void CheckCost(List<string> errors, string field, CurrencyAmount[] cost)
        {
            if (cost == null || cost.Length == 0)
            {
                errors.Add($"{field}: must not be empty");
                return;
            }

            var anyPositive = false;
            foreach (var line in cost)
            {
                if (line.Amount < 0) errors.Add($"{field}: {line.Type} amount must not be negative");
                else if (line.Amount > 0) anyPositive = true;
            }

            // Tổng 0 làm TrySpend thành công mà không trừ gì, tức lượt quay miễn phí.
            if (!anyPositive) errors.Add($"{field}: total cost is 0");
        }

        private bool HasCharacter(Rarity rarity)
        {
            foreach (var character in _characters)
                if (character != null && character.Rarity == rarity) return true;
            return false;
        }

        private bool CanRoll(Rarity rarity) => _rarityWeights.Get(rarity) > 0f && HasCharacter(rarity);

        private void OnValidate()
        {
            foreach (var error in Validate()) Debug.LogWarning($"{name} - {error}", this);
        }
    }
}
