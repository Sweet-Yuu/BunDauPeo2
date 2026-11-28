using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using UnityEngine;

namespace BunDauPeo.Missions
{
    public enum GiftClaimStatus { Ok, Offline, NotInWindow, AlreadyClaimed, NothingToClaim, Invalid }

    public struct GiftClaimResult
    {
        public GiftClaimStatus Status;
        public long Gem;
        public long Coin;
        public long Material;
    }

    // Quà phát từ server (spec đợt 4 5.1): giữ bản sao ListGifts, nhận qua lệnh server rồi áp state. Máy chỉ gửi giftId.
    public sealed class GiftService
    {
        private readonly IGameServer _server;
        private readonly CommandGate _gate;
        private readonly Wallet _wallet;
        private readonly Func<ServerState, bool> _apply;
        private readonly Func<string> _lang;
        private readonly Func<long> _serverNowTicks;
        private List<GiftRow> _gifts = new();

        // apply chép state vào save, lưu và báo view (như MissionService); Coin/Material cộng vào ví trước nên lần lưu đó ghi cả hai.
        public GiftService(IGameServer server, CommandGate gate, Wallet wallet, Func<ServerState, bool> apply,
            Func<string> lang, Func<long> serverNowTicks)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            _lang = lang ?? throw new ArgumentNullException(nameof(lang));
            _serverNowTicks = serverNowTicks ?? throw new ArgumentNullException(nameof(serverNowTicks));
        }

        public event Action Changed;

        public IReadOnlyList<GiftRow> Gifts => _gifts;

        public long ServerNowTicks => _serverNowTicks();

        public int ClaimableCount
        {
            get
            {
                var now = _serverNowTicks();
                var n = 0;
                foreach (var g in _gifts)
                    if (!g.Claimed && g.EndUtcTicks > now) n++;
                return n;
            }
        }

        // Lỗi (mạng, phiên, JSON): giữ danh sách cũ, trả false. Không qua CommandGate: không đổi gì, không cần hộp Mất kết nối.
        public async Task<bool> RefreshAsync()
        {
            var result = await _server.ListGiftsAsync(_lang());
            if (!result.Ok || result.Value == null || (CommandCode)result.Value.Code != CommandCode.Ok) return false;
            GiftListPayload payload;
            try
            {
                payload = JsonUtility.FromJson<GiftListPayload>(result.Value.ResultJson);
            }
            catch (ArgumentException e)
            {
                Debug.LogException(e);
                return false;
            }

            if (payload?.Gifts == null) return false;
            _gifts = payload.Gifts;
            Changed.SafeInvoke();
            return true;
        }

        public Task<GiftClaimResult> ClaimAsync(string giftId)
        {
            var row = string.IsNullOrEmpty(giftId) ? null : _gifts.Find(g => g.Id == giftId);
            if (row == null) return Task.FromResult(Fail(GiftClaimStatus.Invalid));
            if (row.Claimed) return Task.FromResult(Fail(GiftClaimStatus.AlreadyClaimed));
            return SendAsync(requestId => _server.ClaimGiftAsync(requestId, giftId));
        }

        public Task<GiftClaimResult> ClaimAllAsync()
        {
            if (ClaimableCount == 0) return Task.FromResult(Fail(GiftClaimStatus.NothingToClaim));
            return SendAsync(requestId => _server.ClaimAllGiftsAsync(requestId));
        }

        private async Task<GiftClaimResult> SendAsync(Func<string, Task<OnlineResult<CommandResponse>>> call)
        {
            var result = await _gate.RunAsync(call);
            if (!result.Ok) return Fail(GiftClaimStatus.Offline);

            var response = result.Value;
            var status = Map((CommandCode)response.Code);
            if (status != GiftClaimStatus.Ok)
            {
                // Server đã khác máy (hết hạn, đã nhận ở máy khác): làm mới để trang thấy đúng.
                await RefreshAsync();
                return Fail(status);
            }

            GiftClaimPayload payload;
            try
            {
                payload = JsonUtility.FromJson<GiftClaimPayload>(response.ResultJson);
            }
            catch (ArgumentException e)
            {
                Debug.LogException(e);
                return Fail(GiftClaimStatus.Offline);
            }

            if (payload == null || response.State == null) return Fail(GiftClaimStatus.Offline);

            if (payload.Coin > 0) _wallet.Add(CurrencyType.Coin, payload.Coin);
            if (payload.Material > 0) _wallet.Add(CurrencyType.Material, payload.Material);
            _apply(response.State);
            // Làm mới thất bại thì ít nhất đánh dấu đã nhận theo state server (ClaimedGiftIds) để nút không gửi lại.
            foreach (var g in _gifts)
                if (response.State.ClaimedGiftIds.Contains(g.Id)) g.Claimed = true;
            await RefreshAsync();
            Changed.SafeInvoke();
            return new GiftClaimResult { Status = GiftClaimStatus.Ok, Gem = payload.Gem, Coin = payload.Coin, Material = payload.Material };
        }

        private static GiftClaimStatus Map(CommandCode code)
        {
            switch (code)
            {
                case CommandCode.Ok: return GiftClaimStatus.Ok;
                case CommandCode.NotInWindow: return GiftClaimStatus.NotInWindow;
                case CommandCode.AlreadyClaimed: return GiftClaimStatus.AlreadyClaimed;
                case CommandCode.NothingToClaim: return GiftClaimStatus.NothingToClaim;
                default: return GiftClaimStatus.Invalid;
            }
        }

        private static GiftClaimResult Fail(GiftClaimStatus status) => new GiftClaimResult { Status = status };
    }

    // Thư quà trong khung đợt 3 (spec đợt 4 5.2): không ghim, hạn gần nhất lên trước (MailRule.Visible sắp), bỏ thư đã xoá và đã hết hạn.
    public sealed class GiftMailSource : IMailSource
    {
        public const string Prefix = "gift:";
        private readonly GiftService _gifts;
        private readonly PlayerSave _save;

        public GiftMailSource(GiftService gifts, PlayerSave save)
        {
            _gifts = gifts ?? throw new ArgumentNullException(nameof(gifts));
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        public IEnumerable<MailItem> Items()
        {
            var now = _gifts.ServerNowTicks;
            foreach (var g in _gifts.Gifts)
            {
                var id = Prefix + g.Id;
                if (g.EndUtcTicks <= now || _save.MailDeleted.Contains(id)) continue;
                yield return new MailItem
                {
                    Id = id, Title = g.Title, TitleKey = "mail.gift_title", BodyKey = "mail.gift_body", Pinned = false,
                    HasAttachments = true, Action = MailAction.ClaimGift, Rewards = g.Rewards,
                    ExpiresUtcTicks = g.EndUtcTicks, Claimed = g.Claimed,
                };
            }
        }
    }
}
