using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.Tests
{
    // Roll, pity và lịch sử do server tính (HandlerGachaTests); ở đây là phần máy: kiểm trước khi gửi, áp state, ánh xạ mã.
    public class GachaServiceTests
    {
        private CharacterDefinition[] _chars;
        private CharacterCatalog _characters;
        private GachaBannerDefinition _banner;
        private GachaBannerCatalog _bannerCatalog;
        private PlayerSave _save;
        private Wallet _wallet;
        private Roster _roster;
        private bool _applyResult;
        private int _applyCalls;

        [SetUp]
        public void SetUp()
        {
            _chars = TestData.GachaCharacters();
            _characters = TestData.Catalog(_chars);
            _banner = TestData.Banner("b", _chars);
            _bannerCatalog = TestData.GachaCatalog(_banner);
            _save = new PlayerSave();
            _wallet = new Wallet(_save);
            _roster = new Roster(_save, _characters);
            _applyResult = true;
            _applyCalls = 0;
        }

        private static void SeedGem(PlayerSave save, long gem) => save.Balances.Add(new CurrencyAmount(CurrencyType.Gem, gem));

        // Server có đúng Gem đã seed; máy khớp server như lúc mở app.
        private async Task<(ServerRig Rig, GachaService Service)> MakeAsync(long gem = 2000, GameData data = null,
            GachaBannerCatalog banners = null, CharacterCatalog characters = null)
        {
            var rig = await ServerRig.CreateAsync(data ?? GachaRulesTests.Data(), s => SeedGem(s, gem));
            rig.ApplyTo(_save);
            var service = new GachaService(_save, _wallet, characters ?? _characters, banners ?? _bannerCatalog,
                rig.Server, rig.Gate, state =>
                {
                    _applyCalls++;
                    ServerStateApplier.Apply(_save, state);
                    _wallet.NotifyChanged();
                    _roster.NotifyChanged();
                    return _applyResult;
                });
            return (rig, service);
        }

        private long Gem => _wallet.Get(CurrencyType.Gem);

        [Test]
        public async Task Pull_NotEnoughGem_NoServerCall()
        {
            var (rig, service) = await MakeAsync(gem: 50);
            var calls = rig.Server.CommandCalls;

            var result = await service.PullSingleAsync("b");

            Assert.AreEqual(GachaPullStatus.NotEnoughCurrency, result.Status);
            Assert.IsEmpty(result.Entries);
            Assert.AreEqual(calls, rig.Server.CommandCalls);
            Assert.AreEqual(0, _applyCalls);
            Assert.AreEqual(50, Gem);
        }

        [Test]
        public async Task Pull_UnknownBanner_NoServerCall()
        {
            var (rig, service) = await MakeAsync();

            var result = await service.PullSingleAsync("nope");

            Assert.AreEqual(GachaPullStatus.UnknownBanner, result.Status);
            Assert.AreEqual(0, rig.Server.CommandCalls);
            Assert.AreEqual(2000, Gem);
        }

        [Test]
        public async Task Pull_InvalidBanner_ZeroCost_NoServerCall()
        {
            TestData.Edit(_banner, so =>
            {
                var cost = so.FindProperty("_singleCost");
                cost.arraySize = 1;
                var line = cost.GetArrayElementAtIndex(0);
                line.FindPropertyRelative("Type").enumValueIndex = (int)CurrencyType.Gem;
                line.FindPropertyRelative("Amount").longValue = 0;
            });
            var (rig, service) = await MakeAsync();

            var result = await service.PullSingleAsync("b");

            Assert.AreEqual(GachaPullStatus.InvalidBanner, result.Status);
            Assert.AreEqual(0, rig.Server.CommandCalls);
            Assert.AreEqual(2000, Gem);
        }

        [Test]
        public async Task Pull_BannerCharacterMissingFromCatalog_IsInvalidBanner_NoServerCall()
        {
            var (rig, service) = await MakeAsync(characters: TestData.Catalog(_chars.Take(5).ToArray()));

            var result = await service.PullSingleAsync("b");

            Assert.AreEqual(GachaPullStatus.InvalidBanner, result.Status);
            Assert.AreEqual(0, rig.Server.CommandCalls);
        }

        [Test]
        public async Task Pull_Ok_AppliesStateAndRaisesChanged()
        {
            var (rig, service) = await MakeAsync();
            var walletChanged = 0;
            var rosterChanged = 0;
            _wallet.Changed += () => walletChanged++;
            _roster.Changed += () => rosterChanged++;

            var result = await service.PullSingleAsync("b");

            Assert.AreEqual(GachaPullStatus.Success, result.Status);
            Assert.IsTrue(result.Saved);
            Assert.AreEqual(1, result.Entries.Count);
            var entry = result.Entries[0];
            Assert.IsTrue(entry.IsNew);
            Assert.IsTrue(_roster.IsOwned(entry.Character.Id), "the character from the server payload is now owned on the device");
            Assert.AreEqual(1900, Gem);
            Assert.AreEqual(rig.Server.State.Gem, Gem, "the device shows exactly what the server holds");
            Assert.AreEqual(1, service.History.Count);
            // Quay trúng SSR thì pity về 0: so với bản server, không đoán kết quả quay.
            Assert.AreEqual(rig.Server.State.GachaPity.Single(p => p.BannerId == "b").Pulls, service.GetPity("b"));
            Assert.AreEqual(1, _applyCalls);
            Assert.GreaterOrEqual(walletChanged, 1);
            Assert.GreaterOrEqual(rosterChanged, 1);
        }

        [Test]
        public async Task PullTen_Ok_TenEntriesFromPayload()
        {
            var (rig, service) = await MakeAsync();

            var result = await service.PullTenAsync("b");

            Assert.AreEqual(GachaPullStatus.Success, result.Status);
            Assert.AreEqual(10, result.Entries.Count);
            Assert.AreEqual(1100, Gem);
            Assert.AreEqual(1, service.History.Count);
            Assert.AreEqual(10, service.History[0].Count);
            // Mỗi dòng kết quả khớp state đã áp: nhân vật mới thì đang sở hữu, trùng thì mảnh nằm trong save.
            foreach (var entry in result.Entries)
                Assert.IsTrue(_roster.IsOwned(entry.Character.Id));
            Assert.AreEqual(rig.Server.State.OwnedCharacterIds.Count, _roster.GetOwned().Count);
        }

        // Review Focus 1: mất trả lời đúng lúc quay ×10, bấm Thử lại thì Gem trừ một lần và kết quả là bản server đã ghi.
        [Test]
        public async Task PullTen_LostReplyThenRetry_SpendsOnce()
        {
            var (rig, service) = await MakeAsync();
            var lost = 0;
            rig.Gate.Lost += retry =>
            {
                lost++;
                retry();
            };
            rig.Server.LoseNextReply();

            var result = await service.PullTenAsync("b");

            Assert.AreEqual(1, lost, "the player saw the Lost box once and pressed Retry");
            Assert.AreEqual(GachaPullStatus.Success, result.Status);
            Assert.AreEqual(10, result.Entries.Count);
            Assert.AreEqual(1100, rig.Server.State.Gem, "server charged 900 once");
            Assert.AreEqual(1100, Gem);
            Assert.AreEqual(1, service.History.Count, "one pull in the history, not two");
            Assert.AreEqual(1, _applyCalls, "the state is applied once, after the retry succeeded");
            var ids = result.Entries.Select(e => e.Character.Id).ToList();
            CollectionAssert.AreEqual(rig.Server.State.GachaHistory[0].CharacterIds, ids, "the result shown is what the server wrote");
        }

        [Test]
        public async Task Pull_ServerRejectsNotEnoughGem_MapsStatus_NoChange()
        {
            // Máy tưởng đủ Gem (2000) nhưng server chỉ có 50: server là bản chính.
            var (rig, service) = await MakeAsync(gem: 50);
            ServerStateApplier.Apply(_save, new ServerState { Gem = 2000 });

            var result = await service.PullSingleAsync("b");

            Assert.AreEqual(1, rig.Server.CommandCalls);
            Assert.AreEqual(GachaPullStatus.NotEnoughCurrency, result.Status);
            Assert.IsEmpty(result.Entries);
            Assert.AreEqual(0, _applyCalls);
            Assert.AreEqual(2000, Gem, "a rejected command changes nothing on the device");
            Assert.AreEqual(50, rig.Server.State.Gem);
        }

        [Test]
        public async Task Pull_ServerRejectsUnknownBanner_MapsToInvalidBanner_NoChange()
        {
            var data = GachaRulesTests.Data();
            data.Banners.Clear();
            var (rig, service) = await MakeAsync(data: data);

            var result = await service.PullSingleAsync("b");

            Assert.AreEqual(1, rig.Server.CommandCalls);
            Assert.AreEqual(GachaPullStatus.InvalidBanner, result.Status);
            Assert.AreEqual(0, _applyCalls);
            Assert.AreEqual(2000, Gem);
        }

        [Test]
        public async Task Pull_Offline_NoChange()
        {
            var (rig, service) = await MakeAsync();
            rig.Server.FailNext(OnlineStatus.NetworkError);

            var result = await service.PullSingleAsync("b");

            Assert.AreEqual(GachaPullStatus.Offline, result.Status);
            Assert.IsEmpty(result.Entries);
            Assert.AreEqual(0, _applyCalls);
            Assert.AreEqual(2000, Gem);
            Assert.IsEmpty(_roster.GetOwned());
            Assert.AreEqual(2000, rig.Server.State.Gem, "the failed command wrote nothing");
        }

        // L-003: handler của view ném lỗi thì state server đã trả vẫn phải vào bộ nhớ.
        [Test]
        public async Task Pull_ChangedHandlerThrows_StateStillApplied()
        {
            var (_, service) = await MakeAsync();
            _wallet.Changed += () => throw new InvalidOperationException("boom");
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            var result = await service.PullSingleAsync("b");

            Assert.AreEqual(GachaPullStatus.Success, result.Status);
            Assert.AreEqual(1900, Gem);
            Assert.AreEqual(1, _roster.GetOwned().Count);
            Assert.AreEqual(1, service.History.Count);
        }

        [Test]
        public async Task Pull_ApplyReportsNotSaved_ReturnsSavedFalse_AndKeepsState()
        {
            _applyResult = false;
            var (_, service) = await MakeAsync();

            var result = await service.PullSingleAsync("b");

            Assert.AreEqual(GachaPullStatus.Success, result.Status);
            Assert.IsFalse(result.Saved);
            Assert.AreEqual(1900, Gem);
        }

        [Test]
        public async Task GetPity_NoEntry_ReturnsZero()
        {
            var (_, service) = await MakeAsync();

            Assert.AreEqual(0, service.GetPity("b"));
            Assert.AreEqual(0, service.GetPity("other"));
            Assert.AreEqual(0, service.GetPity(null));
        }

        // Banner có cờ quay miễn phí ở cả server (GameData) lẫn SO của máy.
        private async Task<(ServerRig Rig, GachaService Service)> MakeFreeAsync(bool flag = true)
        {
            var data = GachaRulesTests.Data();
            data.Banners[0].FreePullDaily = flag;
            TestData.Edit(_banner, so => so.FindProperty("_freePullDaily").boolValue = flag);
            return await MakeAsync(gem: 1000, data: data);
        }

        private static long Today => MissionRules.DayIndex(DateTime.UtcNow);

        [Test]
        public async Task FreePull_Available_WhenFlagAndNotUsedToday()
        {
            var (_, service) = await MakeFreeAsync();
            Assert.IsTrue(service.FreePullAvailable("b"));
            Assert.IsFalse(service.FreePullAvailable("other"));
            Assert.IsFalse(service.FreePullAvailable(null));

            _save.Missions.FreePullDay = Today;
            Assert.IsFalse(service.FreePullAvailable("b"), "already used today");
            _save.Missions.FreePullDay = Today - 1;
            Assert.IsTrue(service.FreePullAvailable("b"), "yesterday's use does not count");

            var (_, noFlag) = await MakeFreeAsync(flag: false);
            _save.Missions.FreePullDay = 0;
            Assert.IsFalse(noFlag.FreePullAvailable("b"), "banner without the daily flag has no free pull");
        }

        [Test]
        public async Task FreePull_Ok_NoGemSpent_AppliesState()
        {
            var (_, service) = await MakeFreeAsync();
            var gemBefore = Gem;

            var result = await service.PullFreeAsync("b");

            Assert.AreEqual(GachaPullStatus.Success, result.Status);
            Assert.AreEqual(1, result.Entries.Count);
            Assert.AreEqual(gemBefore, Gem, "a free pull costs no Gem");
            Assert.AreEqual(Today, _save.Missions.FreePullDay);
            Assert.AreEqual(1, _save.GachaHistory.Count);
            Assert.AreEqual(1, _applyCalls);
            Assert.IsFalse(service.FreePullAvailable("b"));
        }

        [Test]
        public async Task FreePull_UsedToday_NoServerCall()
        {
            var (rig, service) = await MakeFreeAsync();
            Assert.AreEqual(GachaPullStatus.Success, (await service.PullFreeAsync("b")).Status);
            var calls = rig.Server.CommandCalls;
            var applies = _applyCalls;

            var again = await service.PullFreeAsync("b");

            Assert.AreEqual(GachaPullStatus.FreePullUsed, again.Status);
            Assert.IsEmpty(again.Entries);
            Assert.AreEqual(calls, rig.Server.CommandCalls);
            Assert.AreEqual(applies, _applyCalls);
        }

        [Test]
        public async Task FreePull_Offline_NoChange()
        {
            var (rig, service) = await MakeFreeAsync();
            var before = JsonUtility.ToJson(_save);
            rig.Server.FailNext(OnlineStatus.NetworkError);

            var result = await service.PullFreeAsync("b");

            Assert.AreEqual(GachaPullStatus.Offline, result.Status);
            Assert.AreEqual(before, JsonUtility.ToJson(_save));
            Assert.AreEqual(0, _applyCalls);
            Assert.IsTrue(service.FreePullAvailable("b"), "nothing was spent");
        }

        // Review Focus 2: mất trả lời rồi Thử lại (cùng requestId): lượt quay ghi một lần, Gem không đổi.
        [Test]
        public async Task FreePull_LostReplyThenRetry_OncePerDay()
        {
            var (rig, service) = await MakeFreeAsync();
            rig.Gate.Lost += retry => retry();
            rig.Server.LoseNextReply();

            var result = await service.PullFreeAsync("b");

            Assert.AreEqual(GachaPullStatus.Success, result.Status);
            Assert.AreEqual(1, _save.GachaHistory.Count, "recorded once");
            Assert.AreEqual(1000, Gem);
            Assert.AreEqual(GachaPullStatus.FreePullUsed, (await service.PullFreeAsync("b")).Status);
            Assert.AreEqual(1, rig.Server.State.GachaHistory.Count, "the retry did not roll a second time");
        }
    }
}
