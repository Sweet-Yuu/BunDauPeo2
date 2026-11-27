using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Quà qua LocalGameServer (spec đợt 4 mục 4.3); cùng CommandRunner + ServerHandlers với module.
    public class LocalGameServerGiftTests
    {
        static readonly DateTime Start = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        static readonly DateTime Now = Start.AddDays(1);

        static GiftData Gift(string id, long gem, long coin = 0) => new GiftData
        {
            Id = id, TitleVi = "Quà", TitleEn = "Gift", StartUtcTicks = Start.Ticks, EndUtcTicks = Start.AddDays(14).Ticks,
            Rewards = new List<GiftReward>
            {
                new GiftReward { Type = "Gem", Amount = gem },
                new GiftReward { Type = "Coin", Amount = Math.Max(coin, 1) },
            },
        };

        static async Task<LocalGameServer> BootedAsync()
        {
            var server = new LocalGameServer(() => Now, GachaRulesTests.Data());
            await server.SignInAsync();
            await server.BootstrapAsync();
            server.Gifts = new List<GiftData> { Gift("a", 300, 5000), Gift("b", 100) };
            return server;
        }

        static GiftListPayload Payload(OnlineResult<CommandResponse> r) => JsonUtility.FromJson<GiftListPayload>(r.Value.ResultJson);

        [Test]
        public async Task Local_ListGifts_ReturnsInWindowWithClaimedFlag()
        {
            var server = await BootedAsync();
            await server.ClaimGiftAsync("r1", "a");
            var before = server.State.Revision;

            var r = await server.ListGiftsAsync("en");

            Assert.IsTrue(r.Ok);
            Assert.AreEqual((int)CommandCode.Ok, r.Value.Code);
            var rows = Payload(r).Gifts;
            Assert.AreEqual(2, rows.Count);
            Assert.IsTrue(rows[0].Claimed);
            Assert.IsFalse(rows[1].Claimed);
            Assert.AreEqual("Gift", rows[0].Title);
            Assert.AreEqual(before, server.State.Revision, "listing writes nothing");
        }

        [Test]
        public async Task Local_ListGifts_OtherSession_OtherDevice()
        {
            var server = await BootedAsync();
            server.SimulateOtherDeviceBootstrap();
            Assert.AreEqual((int)CommandCode.OtherDevice, (await server.ListGiftsAsync("vi")).Value.Code);
        }

        [Test]
        public async Task Local_ClaimGift_AppliesAndPersistsState()
        {
            var server = await BootedAsync();
            var gem = server.State.Gem;

            var r = await server.ClaimGiftAsync("r1", "a");

            Assert.AreEqual((int)CommandCode.Ok, r.Value.Code);
            Assert.AreEqual(gem + 300, server.State.Gem);
            CollectionAssert.AreEqual(new[] { "a" }, server.State.ClaimedGiftIds);
            Assert.AreEqual(5000, JsonUtility.FromJson<GiftClaimPayload>(r.Value.ResultJson).Coin);
            Assert.AreEqual((int)CommandCode.AlreadyClaimed, (await server.ClaimGiftAsync("r2", "a")).Value.Code);
            Assert.AreEqual(gem + 300, server.State.Gem);
        }

        [Test]
        public async Task Local_ClaimAllGifts_SumsAll()
        {
            var server = await BootedAsync();
            var gem = server.State.Gem;
            var r = await server.ClaimAllGiftsAsync("r1");
            Assert.AreEqual((int)CommandCode.Ok, r.Value.Code);
            Assert.AreEqual(gem + 400, server.State.Gem);
            Assert.AreEqual((int)CommandCode.NothingToClaim, (await server.ClaimAllGiftsAsync("r2")).Value.Code);
        }

        [Test]
        public async Task Local_LostReply_RetrySameRequest_AppliesOnce()
        {
            var server = await BootedAsync();
            var gem = server.State.Gem;
            server.LoseNextReply();

            var lost = await server.ClaimGiftAsync("r1", "a");
            Assert.AreEqual(OnlineStatus.NetworkError, lost.Status);
            Assert.AreEqual(gem + 300, server.State.Gem, "server wrote before the reply was lost");

            var retry = await server.ClaimGiftAsync("r1", "a");
            Assert.IsTrue(retry.Ok);
            Assert.AreEqual((int)CommandCode.Ok, retry.Value.Code, "the recorded result is returned");
            Assert.AreEqual(5000, JsonUtility.FromJson<GiftClaimPayload>(retry.Value.ResultJson).Coin);
            Assert.AreEqual(gem + 300, server.State.Gem, "no second grant");
        }

        [Test]
        public async Task Local_ClaimGift_OtherSession_OtherDevice()
        {
            var server = await BootedAsync();
            var gem = server.State.Gem;
            server.SimulateOtherDeviceBootstrap();
            Assert.AreEqual((int)CommandCode.OtherDevice, (await server.ClaimGiftAsync("r1", "a")).Value.Code);
            Assert.AreEqual(gem, server.State.Gem);
        }

        [Test]
        public async Task Local_Gifts_MissingFile_Empty()
        {
            var server = await BootedAsync();
            server.Gifts = null;
            server.GiftsPath = Path.Combine(Path.GetTempPath(), "bdp_no_such_gifts_" + Guid.NewGuid().ToString("N") + ".json");
            var r = await server.ListGiftsAsync("vi");
            Assert.IsTrue(r.Ok);
            Assert.AreEqual(0, Payload(r).Gifts.Count);
        }

        [Test]
        public async Task Local_Gifts_File_ReadsDashboardShape_DropsBadGiftOnly()
        {
            var path = Path.Combine(Path.GetTempPath(), "bdp_gifts_" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path,
                "[{\"id\":\"ok\",\"titleVi\":\"Quà\",\"titleEn\":\"Gift\",\"rewards\":[{\"type\":\"Gem\",\"amount\":300}],"
                + "\"startUtc\":\"2026-12-31T00:00:00Z\",\"endUtc\":\"2027-01-14T00:00:00Z\"},"
                + "{\"id\":\"BAD\",\"titleVi\":\"x\",\"rewards\":[{\"type\":\"Gem\",\"amount\":1}],"
                + "\"startUtc\":\"2026-12-31T00:00:00Z\",\"endUtc\":\"2027-01-14T00:00:00Z\"}]");
            try
            {
                var server = await BootedAsync();
                server.Gifts = null;
                server.GiftsPath = path;
                var rows = Payload(await server.ListGiftsAsync("vi")).Gifts;
                Assert.AreEqual(1, rows.Count);
                Assert.AreEqual("ok", rows[0].Id);
                Assert.AreEqual(new DateTime(2027, 1, 14, 0, 0, 0, DateTimeKind.Utc).Ticks, rows[0].EndUtcTicks);
            }
            finally { File.Delete(path); }
        }

        // Nút DEV "Quà thử" ghi đúng hình Dashboard: quà mẫu hợp lệ, trong hạn, nhận được.
        [Test]
        public async Task DevSampleGift_File_IsValidAndClaimable()
        {
            var path = Path.Combine(Path.GetTempPath(), "bdp_gifts_dev_" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, BunDauPeo.UI.DevOverlay.SampleGiftJson(Now));
            try
            {
                var server = await BootedAsync();
                server.Gifts = null;
                server.GiftsPath = path;
                var rows = Payload(await server.ListGiftsAsync("vi")).Gifts;
                Assert.AreEqual(1, rows.Count);
                Assert.AreEqual(Now.AddDays(7).Ticks, rows[0].EndUtcTicks);
                var gem = server.State.Gem;
                Assert.AreEqual((int)CommandCode.Ok, (await server.ClaimGiftAsync("r1", rows[0].Id)).Value.Code);
                Assert.AreEqual(gem + 300, server.State.Gem);
            }
            finally { File.Delete(path); }
        }

        [Test]
        public async Task Local_Gifts_CorruptFile_ReturnsError_NotEmpty()
        {
            var path = Path.Combine(Path.GetTempPath(), "bdp_gifts_bad_" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, "[{\"id\": oops");
            try
            {
                var server = await BootedAsync();
                server.Gifts = null;
                server.GiftsPath = path;
                var gem = server.State.Gem;
                Assert.AreEqual(OnlineStatus.ServerError, (await server.ListGiftsAsync("vi")).Status);
                Assert.AreEqual(OnlineStatus.ServerError, (await server.ClaimGiftAsync("r1", "a")).Status);
                Assert.AreEqual(OnlineStatus.ServerError, (await server.ClaimAllGiftsAsync("r2")).Status);
                Assert.AreEqual(gem, server.State.Gem);
            }
            finally { File.Delete(path); }
        }

        [Test]
        public async Task ClientTooOld_StillRefusesGiftCommands()
        {
            var server = await BootedAsync();
            server.ProtocolVersion = ServerProtocol.MinProtocolVersion - 1;
            Assert.AreEqual(OnlineStatus.ClientTooOld, (await server.ListGiftsAsync("vi")).Status);
            Assert.AreEqual(OnlineStatus.ClientTooOld, (await server.ClaimGiftAsync("r1", "a")).Status);
        }
    }
}
