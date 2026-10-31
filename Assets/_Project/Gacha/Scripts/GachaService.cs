using System;
using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Core;

namespace BunDauPeo.Gacha
{
    public sealed class GachaService
    {
        private readonly PlayerSave _save;
        private readonly Wallet _wallet;
        private readonly Roster _roster;
        private readonly CharacterCatalog _characters;
        private readonly GachaBannerCatalog _banners;
        private readonly GachaRoller _roller;
        private readonly Func<bool> _persist;

        public GachaService(PlayerSave save, Wallet wallet, Roster roster, CharacterCatalog characters,
            GachaBannerCatalog banners, Random random, Func<bool> persist)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _banners = banners ?? throw new ArgumentNullException(nameof(banners));
            _roller = new GachaRoller(random);
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
        }

        public GachaPullResult PullSingle(string bannerId) => Pull(bannerId, ten: false);

        public GachaPullResult PullTen(string bannerId) => Pull(bannerId, ten: true);

        public int GetPity(string bannerId)
        {
            var index = PityIndex(bannerId);
            return index < 0 ? 0 : Math.Max(0, _save.GachaPity[index].Pulls);
        }

        // Thứ tự là hợp đồng: kiểm banner, kiểm tiền, roll, trừ tiền, áp, ghi pity, lưu. Mọi bước có thể ném hoặc
        // thất bại (banner, tiền, RNG) đứng trước bước đầu tiên đổi trạng thái (trừ tiền).
        private GachaPullResult Pull(string bannerId, bool ten)
        {
            if (!_banners.TryGet(bannerId, out var banner)) return Failure(GachaPullStatus.UnknownBanner);
            if (!IsPlayable(banner)) return Failure(GachaPullStatus.InvalidBanner);

            var cost = ten ? banner.TenCost : banner.SingleCost;
            if (!_wallet.CanAfford(cost)) return Failure(GachaPullStatus.NotEnoughCurrency);

            // Roll trên bản sao bộ đếm: RNG ném lỗi thì chưa có gì đổi để hoàn lại.
            var pulls = GetPity(banner.Id);
            IReadOnlyList<CharacterDefinition> drawn;
            if (ten)
            {
                drawn = _roller.RollTen(banner, ref pulls);
            }
            else
            {
                drawn = new[] { _roller.RollOne(banner, ref pulls) };
            }

            if (!_wallet.TrySpend(cost)) return Failure(GachaPullStatus.NotEnoughCurrency);

            var entries = new List<GachaPullEntry>(drawn.Count);
            foreach (var character in drawn) entries.Add(Apply(banner, character));
            SetPity(banner.Id, pulls);

            return new GachaPullResult { Status = GachaPullStatus.Success, Saved = _persist(), Entries = entries };
        }

        // "Đã sở hữu" kiểm sau mỗi lần áp, nên bản thứ hai của cùng nhân vật trong lần quay 10 là trùng.
        private GachaPullEntry Apply(GachaBannerDefinition banner, CharacterDefinition character)
        {
            if (!_roster.IsOwned(character.Id)) return new GachaPullEntry(character, _roster.Add(character.Id), 0);

            var fragments = banner.DuplicateFragments.Get(character.Rarity);
            return new GachaPullEntry(character, false, _roster.AddFragments(character.Id, fragments) ? fragments : 0);
        }

        private bool IsPlayable(GachaBannerDefinition banner)
        {
            if (banner.Validate().Count > 0) return false;
            foreach (var character in banner.Characters)
                if (!_characters.TryGet(character.Id, out _)) return false;
            return true;
        }

        private int PityIndex(string bannerId) =>
            bannerId == null ? -1 : _save.GachaPity.FindIndex(p => p.BannerId == bannerId);

        private void SetPity(string bannerId, int pulls)
        {
            var entry = new PityEntry { BannerId = bannerId, Pulls = pulls };
            var index = PityIndex(bannerId);
            if (index < 0) _save.GachaPity.Add(entry);
            else _save.GachaPity[index] = entry;
        }

        private static GachaPullResult Failure(GachaPullStatus status) =>
            new() { Status = status, Saved = false, Entries = Array.Empty<GachaPullEntry>() };
    }
}
