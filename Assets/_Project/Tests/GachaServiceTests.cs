using System;
using System.Linq;
using System.Text.RegularExpressions;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Random = System.Random;

namespace BunDauPeo.Tests
{
    public class GachaServiceTests
    {
        private CharacterDefinition[] _chars;
        private CharacterCatalog _characters;
        private GachaBannerDefinition _banner;
        private GachaBannerCatalog _bannerCatalog;
        private PlayerSave _save;
        private Wallet _wallet;
        private Roster _roster;
        private int _persistCalls;
        private bool _persistResult;

        [SetUp]
        public void SetUp()
        {
            _chars = TestData.GachaCharacters();
            _characters = TestData.Catalog(_chars);
            _banner = TestData.Banner("b", _chars);
            _bannerCatalog = TestData.GachaCatalog(_banner);
            _save = new PlayerSave();
            _wallet = new Wallet(_save);
            _wallet.Add(CurrencyType.Gem, 2000);
            _roster = new Roster(_save, _characters);
            _persistCalls = 0;
            _persistResult = true;
        }

        private GachaService Make(Random random, GachaBannerCatalog banners = null, CharacterCatalog characters = null) =>
            new(_save, _wallet, _roster, characters ?? _characters, banners ?? _bannerCatalog, random, () =>
            {
                _persistCalls++;
                return _persistResult;
            });

        private long Gem => _wallet.Get(CurrencyType.Gem);

        [Test]
        public void PullSingle_NewCharacter_SpendsGemAndAddsToRoster()
        {
            var rosterChanged = 0;
            _roster.Changed += () => rosterChanged++;

            var result = Make(new ScriptedRandom()).PullSingle("b");

            Assert.AreEqual(GachaPullStatus.Success, result.Status);
            Assert.IsTrue(result.Saved);
            Assert.AreEqual(1, result.Entries.Count);
            Assert.AreEqual("r0", result.Entries[0].Character.Id);
            Assert.IsTrue(result.Entries[0].IsNew);
            Assert.AreEqual(1900, Gem);
            Assert.IsTrue(_roster.IsOwned("r0"));
            Assert.AreEqual(1, _persistCalls);
            Assert.GreaterOrEqual(rosterChanged, 1);
        }

        [Test]
        public void PullSingle_Duplicate_AddsFragmentsNotCharacter()
        {
            _roster.Add("r0");

            var result = Make(new ScriptedRandom()).PullSingle("b");

            Assert.IsFalse(result.Entries[0].IsNew);
            Assert.AreEqual(1, result.Entries[0].FragmentsGained);
            Assert.AreEqual(1, _roster.GetFragments("r0"));
            Assert.AreEqual(1, _roster.GetOwned().Count);
            Assert.AreEqual(1900, Gem);
        }

        [Test]
        public void PullSingle_DuplicateFragmentsFollowRarity()
        {
            TestData.Edit(_banner, so =>
            {
                var counts = so.FindProperty("_duplicateFragments");
                counts.FindPropertyRelative("R").intValue = 2;
                counts.FindPropertyRelative("SR").intValue = 3;
                counts.FindPropertyRelative("SSR").intValue = 4;
            });
            _roster.Add("x0");

            var result = Make(new ScriptedRandom(0.0, 0.99, 0.0)).PullSingle("b");

            Assert.AreEqual("x0", result.Entries[0].Character.Id);
            Assert.AreEqual(4, _roster.GetFragments("x0"));
            Assert.AreEqual(4, result.Entries[0].FragmentsGained);
        }

        [Test]
        public void PullTen_SameCharacterTwice_SecondIsDuplicate()
        {
            var result = Make(new ScriptedRandom()).PullTen("b");

            Assert.AreEqual(GachaPullStatus.Success, result.Status);
            Assert.AreEqual(10, result.Entries.Count);
            Assert.IsTrue(result.Entries[0].IsNew);
            Assert.IsTrue(result.Entries.Skip(1).Take(8).All(e => !e.IsNew && e.Character.Id == "r0"));
            Assert.AreEqual(8, _roster.GetFragments("r0"));
            Assert.AreEqual("s0", result.Entries[9].Character.Id);
            Assert.IsTrue(result.Entries[9].IsNew);
            Assert.AreEqual(1100, Gem);
        }

        [Test]
        public void PullTen_PersistsOnce()
        {
            Make(new ScriptedRandom()).PullTen("b");
            Assert.AreEqual(1, _persistCalls);
        }

        [Test]
        public void PullTen_PityContinuesAcrossCalls_AndIsStoredInSave()
        {
            _wallet.Add(CurrencyType.Gem, 3000);
            var service = Make(new ScriptedRandom());
            for (var i = 0; i < 4; i++) Assert.AreEqual(GachaPullStatus.Success, service.PullTen("b").Status);
            Assert.AreEqual(40, service.GetPity("b"));
            Assert.AreEqual(40, Make(new ScriptedRandom()).GetPity("b"), "a fresh service on the same save must read the stored counter");

            var fifth = service.PullTen("b");
            Assert.AreEqual("x0", fifth.Entries[9].Character.Id);
            Assert.AreEqual(0, service.GetPity("b"));
        }

        [Test]
        public void GetPity_NoEntry_ReturnsZero()
        {
            var service = Make(new ScriptedRandom());
            Assert.AreEqual(0, service.GetPity("b"));
            Assert.AreEqual(0, service.GetPity("zzz"));
        }

        [Test]
        public void Pity_IsPerBanner()
        {
            var other = TestData.Banner("c", _chars);
            var service = Make(new ScriptedRandom(), TestData.GachaCatalog(_banner, other));
            for (var i = 0; i < 3; i++) service.PullSingle("b");
            Assert.AreEqual(3, service.GetPity("b"));
            Assert.AreEqual(0, service.GetPity("c"));
        }

        [Test]
        public void NotEnoughCurrency_ChangesNothing()
        {
            Assert.IsTrue(_wallet.TrySpend(new CurrencyAmount(CurrencyType.Gem, 1950)));
            var walletChanged = 0;
            var rosterChanged = 0;
            _wallet.Changed += () => walletChanged++;
            _roster.Changed += () => rosterChanged++;
            var rng = new ScriptedRandom();

            var result = Make(rng).PullSingle("b");

            Assert.AreEqual(GachaPullStatus.NotEnoughCurrency, result.Status);
            Assert.IsNotNull(result.Entries);
            Assert.IsEmpty(result.Entries);
            Assert.AreEqual(50, Gem);
            Assert.IsEmpty(_roster.GetOwned());
            Assert.AreEqual(0, _roster.GetFragments("r0"));
            Assert.IsEmpty(_save.GachaPity);
            Assert.AreEqual(0, rng.Calls);
            Assert.AreEqual(0, _persistCalls);
            Assert.AreEqual(0, walletChanged);
            Assert.AreEqual(0, rosterChanged);
        }

        [Test]
        public void UnknownBanner_ChangesNothing()
        {
            var rng = new ScriptedRandom();

            var result = Make(rng).PullSingle("nope");

            Assert.AreEqual(GachaPullStatus.UnknownBanner, result.Status);
            Assert.IsEmpty(result.Entries);
            Assert.AreEqual(2000, Gem);
            Assert.AreEqual(0, rng.Calls);
            Assert.AreEqual(0, _persistCalls);
        }

        [Test]
        public void InvalidBanner_ZeroCost_ChangesNothing()
        {
            TestData.Edit(_banner, so =>
            {
                var cost = so.FindProperty("_singleCost");
                cost.arraySize = 1;
                var line = cost.GetArrayElementAtIndex(0);
                line.FindPropertyRelative("Type").enumValueIndex = (int)CurrencyType.Gem;
                line.FindPropertyRelative("Amount").longValue = 0;
            });
            var rng = new ScriptedRandom();

            var result = Make(rng).PullSingle("b");

            Assert.AreEqual(GachaPullStatus.InvalidBanner, result.Status);
            Assert.AreEqual(2000, Gem);
            Assert.AreEqual(0, rng.Calls);
            Assert.IsEmpty(_roster.GetOwned());
        }

        [Test]
        public void BannerCharacterMissingFromCharacterCatalog_IsInvalidBanner()
        {
            var withoutSsr = TestData.Catalog(_chars.Take(5).ToArray());

            var result = Make(new ScriptedRandom(), characters: withoutSsr).PullSingle("b");

            Assert.AreEqual(GachaPullStatus.InvalidBanner, result.Status);
            Assert.AreEqual(2000, Gem);
        }

        [Test]
        public void RngThrows_ChangesNothing()
        {
            // Lần gọi RNG thứ 7 là giữa lượt 4 của PullTen: các lượt trước đã roll xong mà chưa được phép đổi gì.
            var service = Make(new ThrowingRandom(throwOnCall: 7));
            var walletChanged = 0;
            var rosterChanged = 0;
            _wallet.Changed += () => walletChanged++;
            _roster.Changed += () => rosterChanged++;

            Assert.Throws<InvalidOperationException>(() => service.PullTen("b"));

            Assert.AreEqual(2000, Gem);
            Assert.IsEmpty(_roster.GetOwned());
            Assert.IsEmpty(_save.Fragments);
            Assert.IsEmpty(_save.GachaPity);
            Assert.AreEqual(0, _persistCalls);
            Assert.AreEqual(0, walletChanged);
            Assert.AreEqual(0, rosterChanged);
        }

        [Test]
        public void ChangedHandlerThrows_PullStillCompletes()
        {
            _wallet.Changed += () => throw new InvalidOperationException("boom");
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            var result = Make(new ScriptedRandom()).PullSingle("b");

            Assert.AreEqual(GachaPullStatus.Success, result.Status);
            Assert.AreEqual(1900, Gem);
            Assert.IsTrue(_roster.IsOwned("r0"));
            Assert.AreEqual(1, Make(new ScriptedRandom()).GetPity("b"));
            Assert.AreEqual(1, _persistCalls);
        }

        [Test]
        public void PersistFails_ReturnsSavedFalse_AndKeepsState()
        {
            _persistResult = false;

            var result = Make(new ScriptedRandom()).PullSingle("b");

            Assert.AreEqual(GachaPullStatus.Success, result.Status);
            Assert.IsFalse(result.Saved);
            Assert.AreEqual(1900, Gem);
            Assert.IsTrue(_roster.IsOwned("r0"));
        }

        [Test]
        public void PullSingle_AppendsOneHistoryEntry()
        {
            Make(new ScriptedRandom()).PullSingle("b");

            Assert.AreEqual(1, _save.GachaHistory.Count);
            var entry = _save.GachaHistory[0];
            Assert.AreEqual("b", entry.BannerId);
            Assert.AreEqual(1, entry.Count);
            CollectionAssert.AreEqual(new[] { "r0" }, entry.CharacterIds);
        }

        [Test]
        public void PullTen_AppendsOneEntryWithTenIdsInRollOrder()
        {
            Make(new ScriptedRandom()).PullTen("b");

            Assert.AreEqual(1, _save.GachaHistory.Count);
            var entry = _save.GachaHistory[0];
            Assert.AreEqual(10, entry.Count);
            Assert.AreEqual(10, entry.CharacterIds.Length);
            Assert.AreEqual("s0", entry.CharacterIds[9]);
        }

        [Test]
        public void RejectedPull_AddsNoHistory()
        {
            Make(new ScriptedRandom()).PullSingle("nope");
            Assert.IsEmpty(_save.GachaHistory);

            Assert.IsTrue(_wallet.TrySpend(new CurrencyAmount(CurrencyType.Gem, 1950)));
            Make(new ScriptedRandom()).PullSingle("b");
            Assert.IsEmpty(_save.GachaHistory);

            _wallet.Add(CurrencyType.Gem, 1950);
            Assert.Throws<InvalidOperationException>(() => Make(new ThrowingRandom()).PullSingle("b"));
            Assert.IsEmpty(_save.GachaHistory);
        }

        [Test]
        public void History_KeepsLast50()
        {
            _wallet.Add(CurrencyType.Gem, 5000);
            var service = Make(new ScriptedRandom());
            for (var i = 0; i < 51; i++) service.PullSingle("b");

            // Lượt thứ 50 ép SSR (x0). Nếu không bỏ mục cũ nhất thì x0 nằm ở chỉ số 49, không phải 48.
            Assert.AreEqual(GachaService.MaxHistoryEntries, _save.GachaHistory.Count);
            Assert.AreEqual(50, GachaService.MaxHistoryEntries);
            Assert.AreEqual("x0", _save.GachaHistory[48].CharacterIds[0]);
            Assert.AreEqual(1, service.History[49].Count);
        }
    }
}
