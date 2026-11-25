using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using UnityEngine;

namespace BunDauPeo.Gacha
{
    // Server quay và ghi (spec con 6.2); máy chỉ kiểm banner và Gem để không gửi lệnh thừa, rồi áp state server trả về.
    public sealed class GachaService
    {
        private readonly PlayerSave _save;
        private readonly Wallet _wallet;
        private readonly CharacterCatalog _characters;
        private readonly GachaBannerCatalog _banners;
        private readonly IGameServer _server;
        private readonly CommandGate _gate;
        private readonly Func<ServerState, bool> _apply;
        private readonly Func<DateTime> _nowUtc;

        // apply chép state vào PlayerSave và báo view; trả false khi lưu bản đệm thất bại (bộ nhớ vẫn đúng).
        public GachaService(PlayerSave save, Wallet wallet, CharacterCatalog characters, GachaBannerCatalog banners,
            IGameServer server, CommandGate gate, Func<ServerState, bool> apply, Func<DateTime> nowUtc = null)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _banners = banners ?? throw new ArgumentNullException(nameof(banners));
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            // Ngày của lượt miễn phí tính theo giờ server (OnlineStartup truyền ServerTime); test không truyền thì giờ máy.
            _nowUtc = nowUtc ?? (() => DateTime.UtcNow);
        }

        public IReadOnlyList<GachaHistoryEntry> History => _save.GachaHistory;

        public Task<GachaPullResult> PullSingleAsync(string bannerId) => PullAsync(bannerId, 1);

        public Task<GachaPullResult> PullTenAsync(string bannerId) => PullAsync(bannerId, 10);

        public int GetPity(string bannerId)
        {
            var index = bannerId == null ? -1 : _save.GachaPity.FindIndex(p => p.BannerId == bannerId);
            return index < 0 ? 0 : Math.Max(0, _save.GachaPity[index].Pulls);
        }

        private async Task<GachaPullResult> PullAsync(string bannerId, int count)
        {
            if (!_banners.TryGet(bannerId, out var banner)) return Failure(GachaPullStatus.UnknownBanner);
            if (!IsPlayable(banner)) return Failure(GachaPullStatus.InvalidBanner);
            if (!_wallet.CanAfford(count == 10 ? banner.TenCost : banner.SingleCost)) return Failure(GachaPullStatus.NotEnoughCurrency);

            var result = await _gate.RunAsync(id => _server.GachaPullAsync(id, banner.Id, count));
            // Lỗi mạng cuối cùng (hộp mạng đã hiện) hoặc phiên hỏng: không đổi gì.
            if (!result.Ok) return Failure(GachaPullStatus.Offline);

            var response = result.Value;
            switch ((CommandCode)response.Code)
            {
                case CommandCode.Ok:
                    break;
                case CommandCode.NotEnoughGem:
                    return Failure(GachaPullStatus.NotEnoughCurrency);
                default:
                    return Failure(GachaPullStatus.InvalidBanner);
            }

            return Complete(response);
        }

        // Lượt miễn phí hôm nay còn không: banner có cờ FreePullDaily và ngày đã dùng (theo giờ server) khác hôm nay.
        public bool FreePullAvailable(string bannerId) =>
            bannerId != null && _banners.TryGet(bannerId, out var banner) && banner.FreePullDaily
            && _save.Missions.FreePullDay != MissionRules.DayIndex(_nowUtc());

        public async Task<GachaPullResult> PullFreeAsync(string bannerId)
        {
            if (!_banners.TryGet(bannerId, out var banner)) return Failure(GachaPullStatus.UnknownBanner);
            if (!IsPlayable(banner) || !banner.FreePullDaily) return Failure(GachaPullStatus.InvalidBanner);
            if (!FreePullAvailable(bannerId)) return Failure(GachaPullStatus.FreePullUsed);

            var result = await _gate.RunAsync(id => _server.GachaFreePullAsync(id, banner.Id));
            if (!result.Ok) return Failure(GachaPullStatus.Offline);

            var response = result.Value;
            switch ((CommandCode)response.Code)
            {
                case CommandCode.Ok:
                    return Complete(response);
                case CommandCode.FreePullUsed:
                    return Failure(GachaPullStatus.FreePullUsed);
                default:
                    return Failure(GachaPullStatus.InvalidBanner);
            }
        }

        private GachaPullResult Complete(CommandResponse response)
        {
            GachaPullPayload payload;
            try
            {
                payload = JsonUtility.FromJson<GachaPullPayload>(response.ResultJson);
            }
            catch (ArgumentException e)
            {
                Debug.LogException(e);
                return Failure(GachaPullStatus.Offline);
            }

            if (response.State == null || payload == null) return Failure(GachaPullStatus.Offline);

            var saved = _apply(response.State);
            var entries = new List<GachaPullEntry>(payload.Pulls.Count);
            foreach (var pulled in payload.Pulls)
                if (_characters.TryGet(pulled.CharacterId, out var character))
                    entries.Add(new GachaPullEntry(character, pulled.IsNew, pulled.Fragments));
            return new GachaPullResult { Status = GachaPullStatus.Success, Saved = saved, Entries = entries };
        }

        private bool IsPlayable(GachaBannerDefinition banner)
        {
            if (banner.Validate().Count > 0) return false;
            foreach (var character in banner.Characters)
                if (!_characters.TryGet(character.Id, out _)) return false;
            return true;
        }

        private static GachaPullResult Failure(GachaPullStatus status) =>
            new GachaPullResult { Status = status, Saved = true, Entries = Array.Empty<GachaPullEntry>() };
    }
}
