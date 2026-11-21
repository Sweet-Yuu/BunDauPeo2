using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class StarServiceTests
    {
        private CharacterDefinition _a;     // R, 1★, HP 800 / Atk 80 / Def 40 / Spd 100
        private CharacterDefinition _b;     // SR, 2★, HP 1000 / Atk 110 / Def 50 / Spd 110
        private CharacterDefinition _c;     // trong catalog nhưng chưa sở hữu
        private CharacterCatalog _catalog;
        private PlayerSave _save;
        private Roster _roster;
        private StarConfig _config;
        private int _changed;
        private int _applyCalls;
        private bool _applyResult;

        [SetUp]
        public void SetUp()
        {
            _a = Stats(TestData.Character("a", Rarity.R), 800, 80, 40, 100);
            _b = Stats(TestData.Character("b", Rarity.SR), 1000, 110, 50, 110);
            TestData.Edit(_b, so => so.FindProperty("_stars").intValue = 2);
            _c = TestData.Character("c", Rarity.R);
            _catalog = TestData.Catalog(_a, _b, _c);
            _save = new PlayerSave();
            _roster = new Roster(_save, _catalog);
            _roster.Add("a");
            _roster.Add("b");
            _config = ScriptableObject.CreateInstance<StarConfig>();
            _applyResult = true;
            _applyCalls = 0;
            _changed = 0;
            _roster.Changed += () => _changed++;
        }

        private static CharacterDefinition Stats(CharacterDefinition character, int hp, int attack, int defense, int speed)
        {
            TestData.Edit(character, so =>
            {
                var stats = so.FindProperty("_combatStats");
                stats.FindPropertyRelative("HP").intValue = hp;
                stats.FindPropertyRelative("Attack").intValue = attack;
                stats.FindPropertyRelative("Defense").intValue = defense;
                stats.FindPropertyRelative("Speed").intValue = speed;
            });
            return character;
        }

        // Tạo sau khi test đã sửa config: IsConfigValid chỉ tính một lần ở constructor. Chỉ cho ca đọc: lệnh ghi dùng MakeAsync.
        private StarService Service() => ServerRig.IdleStars(_save, _roster, _catalog, _config);

        private bool Apply(ServerState state)
        {
            _applyCalls++;
            ServerStateApplier.Apply(_save, state);
            _roster.NotifyChanged();
            return _applyResult;
        }

        private void Give(string id, long fragments)
        {
            _roster.AddFragments(id, fragments);
            _changed = 0;
        }

        private void SetStarUps(string id, int count) => _save.StarUps.Add(new StarUpEntry { CharacterId = id, Count = count });

        private void BreakCosts() => typeof(StarConfig)
            .GetField("_costs", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_config, null);


        [Test]
        public void GetStars_NoEntry_IsStartingStars()
        {
            var service = Service();
            Assert.AreEqual(1, service.GetStars("a"));
            Assert.AreEqual(2, service.GetStars("b"));
            Assert.AreEqual(0, service.GetStarUps("a"));
        }

        [Test]
        public void GetStars_NegativeCount_TreatedAsZero()
        {
            SetStarUps("a", -3);
            var service = Service();
            Assert.AreEqual(1, service.GetStars("a"));
            Assert.AreEqual(0, service.GetStarUps("a"));
        }

        [Test]
        public void GetStars_CountBeyondMax_ClampedAtFive()
        {
            SetStarUps("a", 9);
            var service = Service();
            Assert.AreEqual(5, service.GetStars("a"));
            Assert.AreEqual(4, service.GetStarUps("a"));
        }

        [Test]
        public void GetStars_UnknownId_IsZero()
        {
            var service = Service();
            Assert.AreEqual(0, service.GetStars("ghost"));
            Assert.AreEqual(0, service.GetStars(null));
            Assert.AreEqual(0, service.GetStarUps("ghost"));
            Assert.AreEqual(0, service.GetStarUps(null));
        }

        [Test]
        public void GetNextCost_ByTargetStar()
        {
            var service = Service();
            Assert.AreEqual(2, service.GetNextCost("a"));

            _save.StarUps.Clear();
            SetStarUps("a", 3);
            Assert.AreEqual(8, service.GetNextCost("a"));

            _save.StarUps.Clear();
            SetStarUps("a", 4);
            Assert.AreEqual(0, service.GetNextCost("a"));
            Assert.AreEqual(0, service.GetNextCost("ghost"));
        }

        private static GameData Data()
        {
            var data = new GameData { MaxStars = 5, MaxTeamSize = 5, StarCosts = new List<int> { 2, 4, 6, 8 } };
            data.Characters.Add(new CharacterData { Id = "a", Rarity = 0, BaseStars = 1 });
            data.Characters.Add(new CharacterData { Id = "b", Rarity = 1, BaseStars = 2 });
            data.Characters.Add(new CharacterData { Id = "c", Rarity = 0, BaseStars = 1 });
            return data;
        }

        // Server giữ a, b (+ mảnh/sao đã seed); máy khớp server như lúc mở app. Tạo service sau khi test sửa config.
        private async Task<(ServerRig Rig, StarService Service)> MakeAsync(long fragmentsOfA = 0, int starUpsOfA = 0,
            params string[] extraOwned)
        {
            var rig = await ServerRig.CreateAsync(Data(), s =>
            {
                s.OwnedCharacterIds.AddRange(new[] { "a", "b" });
                s.OwnedCharacterIds.AddRange(extraOwned);
                if (fragmentsOfA > 0) s.Fragments.Add(new FragmentCount { CharacterId = "a", Count = fragmentsOfA });
                if (starUpsOfA > 0) s.StarUps.Add(new StarUpEntry { CharacterId = "a", Count = starUpsOfA });
            });
            rig.ApplyTo(_save);
            _changed = 0;
            return (rig, new StarService(_save, _roster, _catalog, _config, rig.Server, rig.Gate, Apply));
        }

        // Từ chối ở máy thì không gửi lệnh nào, save và Changed không đổi.
        private async Task AssertRejectedLocallyAsync(ServerRig rig, StarService service, string id, StarUpStatus expected, int expectedStars)
        {
            var before = JsonUtility.ToJson(_save);
            var result = await service.TryStarUpAsync(id);
            Assert.AreEqual(expected, result.Status);
            Assert.AreEqual(expectedStars, result.StarsAfter);
            Assert.AreEqual(before, JsonUtility.ToJson(_save));
            Assert.AreEqual(0, _changed);
            Assert.AreEqual(0, _applyCalls);
            Assert.AreEqual(0, rig.Server.CommandCalls, "nothing was sent to the server");
        }

        [Test]
        public async Task StarUpAsync_Ok_Applies()
        {
            var (rig, service) = await MakeAsync(fragmentsOfA: 3);

            var result = await service.TryStarUpAsync("a");

            Assert.AreEqual(StarUpStatus.Success, result.Status);
            Assert.AreEqual(2, result.StarsAfter);
            Assert.IsTrue(result.Saved);
            Assert.AreEqual(1, _roster.GetFragments("a"));
            Assert.AreEqual(1, _save.StarUps.Count);
            Assert.AreEqual(1, _save.StarUps[0].Count);
            Assert.GreaterOrEqual(_changed, 1);
            Assert.AreEqual(1, _applyCalls);
            Assert.AreEqual(1, rig.Server.State.StarUps[0].Count, "device and server agree");
        }

        [Test]
        public async Task StarUpAsync_NotEnoughFragments_NoServerCall()
        {
            var (rig, service) = await MakeAsync(fragmentsOfA: 1);
            await AssertRejectedLocallyAsync(rig, service, "a", StarUpStatus.NotEnoughFragments, 1);
        }

        [Test]
        public async Task StarUpAsync_InvalidConfig_NoServerCall()
        {
            BreakCosts();
            var (rig, service) = await MakeAsync(fragmentsOfA: 5);
            await AssertRejectedLocallyAsync(rig, service, "a", StarUpStatus.InvalidConfig, 1);
            Assert.AreEqual(0, service.GetNextCost("a"));
        }

        [Test]
        public async Task StarUpAsync_StartingStarsBelowOne_RejectedNotFree()
        {
            // SO chưa chạy Setup Project có _stars = 0: giá bậc kế bằng 0, không được nâng miễn phí.
            TestData.Edit(_a, so => so.FindProperty("_stars").intValue = 0);
            var (rig, service) = await MakeAsync(fragmentsOfA: 5);
            await AssertRejectedLocallyAsync(rig, service, "a", StarUpStatus.InvalidConfig, 0);
            Assert.AreEqual(5, _roster.GetFragments("a"));
        }

        [Test]
        public async Task StarUpAsync_UnknownId_NoServerCall()
        {
            var (rig, service) = await MakeAsync(fragmentsOfA: 5);
            await AssertRejectedLocallyAsync(rig, service, "ghost", StarUpStatus.Unknown, 0);
        }

        [Test]
        public async Task StarUpAsync_NotOwned_NoServerCall()
        {
            var (rig, service) = await MakeAsync();
            // Mảnh được ghi thẳng vào save để chỉ còn bước kiểm sở hữu chặn lại.
            _save.Fragments.Add(new FragmentCount { CharacterId = "c", Count = 10 });
            await AssertRejectedLocallyAsync(rig, service, "c", StarUpStatus.NotOwned, 1);
        }

        [Test]
        public async Task StarUpAsync_AtFiveStars_NoServerCall()
        {
            var (rig, service) = await MakeAsync(fragmentsOfA: 20, starUpsOfA: 4);
            await AssertRejectedLocallyAsync(rig, service, "a", StarUpStatus.MaxStars, 5);
        }

        [Test]
        public async Task StarUpAsync_ServerRejects_MapsStatus_NoChange()
        {
            // Máy tưởng có 3 mảnh nhưng server không có: server là bản chính.
            var (rig, service) = await MakeAsync();
            _save.Fragments.Add(new FragmentCount { CharacterId = "a", Count = 3 });
            var before = JsonUtility.ToJson(_save);

            var result = await service.TryStarUpAsync("a");

            Assert.AreEqual(1, rig.Server.CommandCalls);
            Assert.AreEqual(StarUpStatus.NotEnoughFragments, result.Status);
            Assert.AreEqual(before, JsonUtility.ToJson(_save));
            Assert.AreEqual(0, _applyCalls);
        }

        [Test]
        public async Task StarUpAsync_Offline_NoChange()
        {
            var (rig, service) = await MakeAsync(fragmentsOfA: 3);
            rig.Server.FailNext(OnlineStatus.NetworkError);
            var before = JsonUtility.ToJson(_save);

            var result = await service.TryStarUpAsync("a");

            Assert.AreEqual(StarUpStatus.Offline, result.Status);
            Assert.AreEqual(before, JsonUtility.ToJson(_save));
            Assert.AreEqual(0, _applyCalls);
            Assert.AreEqual(3, rig.Server.State.Fragments[0].Count, "the failed command wrote nothing");
        }

        [Test]
        public async Task StarUpAsync_LostReplyThenRetry_SpendsOnce()
        {
            var (rig, service) = await MakeAsync(fragmentsOfA: 5);
            rig.Gate.Lost += retry => retry();
            rig.Server.LoseNextReply();

            var result = await service.TryStarUpAsync("a");

            Assert.AreEqual(StarUpStatus.Success, result.Status);
            Assert.AreEqual(2, result.StarsAfter);
            Assert.AreEqual(3, _roster.GetFragments("a"), "charged 2 once, not 4");
            Assert.AreEqual(3, rig.Server.State.Fragments[0].Count);
        }

        [Test]
        public async Task StarUpAsync_FromOneToFive_CostsTwentyThenMaxStars()
        {
            var (_, service) = await MakeAsync(fragmentsOfA: 20);
            for (var expected = 2; expected <= 5; expected++)
            {
                var result = await service.TryStarUpAsync("a");
                Assert.AreEqual(StarUpStatus.Success, result.Status, $"to {expected}★");
                Assert.AreEqual(expected, result.StarsAfter);
            }

            Assert.AreEqual(StarUpStatus.MaxStars, (await service.TryStarUpAsync("a")).Status);
            Assert.AreEqual(0, _roster.GetFragments("a"));
            Assert.AreEqual(4, _save.StarUps[0].Count);
        }

        [Test]
        public async Task StarUpAsync_ApplyReportsNotSaved_SavedFalseButApplied()
        {
            _applyResult = false;
            var (_, service) = await MakeAsync(fragmentsOfA: 3);

            var result = await service.TryStarUpAsync("a");

            Assert.AreEqual(StarUpStatus.Success, result.Status);
            Assert.IsFalse(result.Saved);
            Assert.AreEqual(2, result.StarsAfter);
            Assert.AreEqual(1, _roster.GetFragments("a"));
        }


        [Test]
        public void GetCombatStats_ScalesHpAttackDefenseNotSpeed()
        {
            SetStarUps("a", 4);
            SetStarUps("b", 1);
            var service = Service();

            var a = service.GetCombatStats(_a);
            Assert.AreEqual(1120, a.HP);
            Assert.AreEqual(112, a.Attack);
            Assert.AreEqual(56, a.Defense);
            Assert.AreEqual(100, a.Speed);

            var b = service.GetCombatStats(_b);
            Assert.AreEqual(1100, b.HP);
            Assert.AreEqual(121, b.Attack);
            Assert.AreEqual(55, b.Defense);
            Assert.AreEqual(110, b.Speed);
        }

        [Test]
        public void GetCombatStats_RoundsHalfUp()
        {
            Stats(_a, 805, 15, 5, 100);
            SetStarUps("a", 1);
            var stats = Service().GetCombatStats(_a);
            Assert.AreEqual(886, stats.HP);
            Assert.AreEqual(17, stats.Attack);
            Assert.AreEqual(6, stats.Defense);
        }

        [Test]
        public void GetCombatStats_InvalidConfig_ReturnsBase()
        {
            BreakCosts();
            SetStarUps("a", 2);
            var stats = Service().GetCombatStats(_a);
            Assert.AreEqual(800, stats.HP);
            Assert.AreEqual(80, stats.Attack);
            Assert.AreEqual(40, stats.Defense);
            Assert.AreEqual(100, stats.Speed);
        }

        [Test]
        public void GetCombatStats_StartRaisedAfterStarUps_UsesEffectiveStarUps()
        {
            SetStarUps("a", 3);
            TestData.Edit(_a, so => so.FindProperty("_stars").intValue = 3);
            var service = Service();

            Assert.AreEqual(5, service.GetStars("a"));
            Assert.AreEqual(2, service.GetStarUps("a"));
            Assert.AreEqual(960, service.GetCombatStats(_a).HP);
        }
    }
}
