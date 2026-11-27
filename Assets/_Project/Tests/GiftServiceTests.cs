using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BunDauPeo.Core;
using BunDauPeo.Missions;
using BunDauPeo.Rules;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Máy: giữ danh sách, gửi đúng giftId, áp state server, cộng Coin/Material một lần (Review Focus 2). Số lượng do server tính (HandlerGiftTests).
    public class GiftServiceTests
    {
        static readonly DateTime Start = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        sealed class Rig
        {
            public LocalGameServer Server;
            public PlayerSave Save = new();
            public Wallet Wallet;
            public CommandGate Gate;
            public GiftService Service;
            public DateTime Now = Start.AddDays(1);
            public int Applies;
        }

        static GiftData Gift(string id, int gem, int coin, int material = 0, int days = 14)
        {
            var g = new GiftData
            {
                Id = id, TitleVi = "Quà " + id, TitleEn = "Gift " + id, StartUtcTicks = Start.Ticks, EndUtcTicks = Start.AddDays(days).Ticks,
                Rewards = new List<GiftReward>(),
            };
            if (gem > 0) g.Rewards.Add(new GiftReward { Type = "Gem", Amount = gem });
            if (coin > 0) g.Rewards.Add(new GiftReward { Type = "Coin", Amount = coin });
            if (material > 0) g.Rewards.Add(new GiftReward { Type = "Material", Amount = material });
            return g;
        }

        static async Task<Rig> MakeAsync(params GiftData[] gifts)
        {
            var rig = new Rig();
            rig.Server = new LocalGameServer(() => rig.Now, GachaRulesTests.Data());
            await rig.Server.SignInAsync();
            var boot = await rig.Server.BootstrapAsync();
            ServerStateApplier.Apply(rig.Save, boot.Value.State);
            rig.Server.Gifts = new List<GiftData>(gifts);
            rig.Wallet = new Wallet(rig.Save);
            rig.Gate = new CommandGate(_ => new TaskCompletionSource<bool>().Task);
            rig.Service = new GiftService(rig.Server, rig.Gate, rig.Wallet, state =>
            {
                rig.Applies++;
                ServerStateApplier.Apply(rig.Save, state);
                return true;
            }, () => "en", () => rig.Now.Ticks);
            return rig;
        }

        static long Gem(Rig rig) => rig.Wallet.Get(CurrencyType.Gem);

        [Test]
        public async Task Refresh_Failure_KeepsOldList()
        {
            var rig = await MakeAsync(Gift("a", 10, 0));
            Assert.IsTrue(await rig.Service.RefreshAsync());
            Assert.AreEqual(1, rig.Service.Gifts.Count);

            rig.Server.FailNextOn(nameof(IGameServer.ListGiftsAsync), OnlineStatus.NetworkError);
            var ok = await rig.Service.RefreshAsync();

            Assert.IsFalse(ok);
            Assert.AreEqual(1, rig.Service.Gifts.Count, "old list kept");
            Assert.AreEqual("Gift a", rig.Service.Gifts[0].Title);
        }

        [Test]
        public async Task Claim_AppliesGemFromState_AndCoinMaterialToWallet()
        {
            var rig = await MakeAsync(Gift("a", 300, 5000, 7));
            await rig.Service.RefreshAsync();
            var gem = Gem(rig);

            var r = await rig.Service.ClaimAsync("a");

            Assert.AreEqual(GiftClaimStatus.Ok, r.Status);
            Assert.AreEqual(gem + 300, Gem(rig));
            Assert.AreEqual(gem + 300, rig.Server.State.Gem, "Gem equals the server state");
            Assert.AreEqual(5000, rig.Wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(7, rig.Wallet.Get(CurrencyType.Material));
            Assert.AreEqual(1, rig.Applies);
            Assert.IsTrue(rig.Service.Gifts[0].Claimed, "the list shows it claimed");
        }

        [Test]
        public async Task Claim_CoinMaterialAppliedOnce_OnRetrySameRequest()
        {
            var rig = await MakeAsync(Gift("a", 300, 5000, 7));
            await rig.Service.RefreshAsync();
            var gem = Gem(rig);
            var lost = 0;
            rig.Gate.Lost += retry =>
            {
                lost++;
                retry();
            };
            rig.Server.LoseNextReply();

            var r = await rig.Service.ClaimAsync("a");

            Assert.AreEqual(1, lost, "the player saw the Lost box and pressed Retry");
            Assert.AreEqual(GiftClaimStatus.Ok, r.Status);
            Assert.AreEqual(gem + 300, Gem(rig), "Gem once");
            Assert.AreEqual(5000, rig.Wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(7, rig.Wallet.Get(CurrencyType.Material));
            Assert.AreEqual(1, rig.Applies);
        }

        [Test]
        public async Task Claim_OutOfWindow_RefreshesList_ReturnsNotInWindow()
        {
            var rig = await MakeAsync(Gift("a", 300, 5000, 0, 2), Gift("b", 5, 0, 0, 14));
            await rig.Service.RefreshAsync();
            Assert.AreEqual(2, rig.Service.Gifts.Count);
            var gem = Gem(rig);
            rig.Now = Start.AddDays(3);

            var r = await rig.Service.ClaimAsync("a");

            Assert.AreEqual(GiftClaimStatus.NotInWindow, r.Status);
            Assert.AreEqual(1, rig.Service.Gifts.Count, "the expired gift left the list");
            Assert.AreEqual("b", rig.Service.Gifts[0].Id);
            Assert.AreEqual(gem, Gem(rig));
            Assert.AreEqual(0, rig.Wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(0, rig.Applies);
        }

        [Test]
        public async Task Claim_ServerError_ChangesNothing()
        {
            var rig = await MakeAsync(Gift("a", 300, 5000, 7));
            await rig.Service.RefreshAsync();
            var gem = Gem(rig);
            var before = UnityEngine.JsonUtility.ToJson(rig.Save);
            rig.Server.FailNextOn(nameof(IGameServer.ClaimGiftAsync), OnlineStatus.ServerError);

            var r = await rig.Service.ClaimAsync("a");

            Assert.AreEqual(GiftClaimStatus.Offline, r.Status);
            Assert.AreEqual(gem, Gem(rig));
            Assert.AreEqual(before, UnityEngine.JsonUtility.ToJson(rig.Save), "wallet and save untouched");
            Assert.AreEqual(0, rig.Applies);
            Assert.IsFalse(rig.Service.Gifts[0].Claimed);
        }

        [Test]
        public async Task ClaimAll_TotalsOnce()
        {
            var rig = await MakeAsync(Gift("a", 300, 5000), Gift("b", 100, 0, 3));
            await rig.Service.RefreshAsync();
            var gem = Gem(rig);

            var r = await rig.Service.ClaimAllAsync();

            Assert.AreEqual(GiftClaimStatus.Ok, r.Status);
            Assert.AreEqual(400, r.Gem);
            Assert.AreEqual(5000, r.Coin);
            Assert.AreEqual(3, r.Material);
            Assert.AreEqual(gem + 400, Gem(rig));
            Assert.AreEqual(5000, rig.Wallet.Get(CurrencyType.Coin));
            Assert.AreEqual(0, rig.Service.ClaimableCount);

            var again = await rig.Service.ClaimAllAsync();
            Assert.AreEqual(GiftClaimStatus.NothingToClaim, again.Status);
            Assert.AreEqual(gem + 400, Gem(rig));
        }

        [Test]
        public async Task ClaimableCount_IgnoresClaimedAndExpired()
        {
            var rig = await MakeAsync(Gift("a", 1, 0, 0, 14), Gift("b", 1, 0, 0, 2), Gift("c", 1, 0, 0, 14));
            await rig.Service.RefreshAsync();
            await rig.Service.ClaimAsync("a");
            Assert.AreEqual(2, rig.Service.ClaimableCount);

            rig.Now = Start.AddDays(3); // b hết hạn theo giờ server, danh sách chưa làm mới

            Assert.AreEqual(1, rig.Service.ClaimableCount, "only c: a is claimed, b is expired");
        }
    }
}
