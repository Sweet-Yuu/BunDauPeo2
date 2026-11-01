using System;
using System.IO;
using BunDauPeo.Core;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // JsonUtility lưu enum dưới dạng số, nên chèn một giá trị vào giữa sẽ làm save cũ lệch sang giá trị khác.
    public class SaveCompatibilityTests
    {
        [Test]
        public void CurrencyType_Values_ArePinned()
        {
            Assert.AreEqual(0, (int)CurrencyType.Coin);
            Assert.AreEqual(1, (int)CurrencyType.Gem);
            Assert.AreEqual(2, (int)CurrencyType.Material);
        }

        [Test]
        public void StaffRole_Values_ArePinned()
        {
            Assert.AreEqual(0, (int)StaffRole.Cook);
            Assert.AreEqual(1, (int)StaffRole.Server);
            Assert.AreEqual(2, (int)StaffRole.Cashier);
        }

        [Test]
        public void OldSave_WithoutFragmentsAndPity_LoadsWithEmptyLists()
        {
            var loaded = JsonUtility.FromJson<PlayerSave>(
                "{\"Version\":1,\"Balances\":[{\"Type\":1,\"Amount\":5}],\"OwnedCharacterIds\":[\"a\"]}");
            Assert.AreEqual(5, loaded.Balances[0].Amount);
            Assert.AreEqual("a", loaded.OwnedCharacterIds[0]);
            Assert.IsNotNull(loaded.Fragments);
            Assert.IsNotNull(loaded.GachaPity);
            Assert.IsEmpty(loaded.Fragments);
            Assert.IsEmpty(loaded.GachaPity);
        }

        [Test]
        public void FragmentsAndPity_RoundTripThroughSaveService()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            try
            {
                var service = new SaveService(Path.Combine(dir, "save.json"));
                var save = new PlayerSave();
                save.Fragments.Add(new FragmentCount { CharacterId = "a", Count = 3 });
                save.GachaPity.Add(new PityEntry { BannerId = "banner", Pulls = 7 });
                Assert.IsTrue(service.Save(save));
                var loaded = service.Load();
                Assert.AreEqual(3, loaded.Fragments[0].Count);
                Assert.AreEqual(7, loaded.GachaPity[0].Pulls);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void OldSave_WithoutGachaHistory_LoadsWithEmptyList()
        {
            var loaded = JsonUtility.FromJson<PlayerSave>("{\"Version\":1,\"Balances\":[],\"OwnedCharacterIds\":[]}");
            Assert.IsNotNull(loaded.GachaHistory);
            Assert.IsEmpty(loaded.GachaHistory);
        }

        [Test]
        public void OldSave_WithoutRestaurantFields_LoadsWithDefaults()
        {
            var loaded = JsonUtility.FromJson<PlayerSave>(
                "{\"Version\":1,\"Balances\":[{\"Type\":1,\"Amount\":5}],\"OwnedCharacterIds\":[\"a\"]}");
            Assert.AreEqual(5, loaded.Balances[0].Amount);
            Assert.IsNotNull(loaded.Upgrades);
            Assert.IsNotNull(loaded.DailyPresence);
            Assert.IsNotNull(loaded.IncidentScores);
            Assert.IsEmpty(loaded.Upgrades);
            Assert.IsEmpty(loaded.DailyPresence);
            Assert.IsEmpty(loaded.IncidentScores);
            Assert.IsNotNull(loaded.Afk);
            Assert.AreEqual(0, loaded.Afk.LastActiveUtcTicks);
            Assert.AreEqual(0, loaded.Afk.PendingTheoretical);
        }

        [Test]
        public void RestaurantFields_RoundTripThroughSaveService()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            try
            {
                var service = new SaveService(Path.Combine(dir, "save.json"));
                var save = new PlayerSave();
                save.Upgrades.Add(new UpgradeLevel { UpgradeId = "up_tables", Level = 2 });
                save.DailyPresence.Add(new DailyPresenceEntry { DayIndex = 7300, Seconds = 120.5 });
                save.IncidentScores.AddRange(new[] { 2, 0, 1 });
                save.Afk = new AfkState
                {
                    LastActiveUtcTicks = 123456789012345L,
                    RatePerSecond = 1.56,
                    AccumulatedSeconds = 3600.25,
                    PendingTheoretical = 5614.5,
                };
                Assert.IsTrue(service.Save(save));

                var loaded = service.Load();

                Assert.AreEqual("up_tables", loaded.Upgrades[0].UpgradeId);
                Assert.AreEqual(2, loaded.Upgrades[0].Level);
                Assert.AreEqual(7300, loaded.DailyPresence[0].DayIndex);
                Assert.AreEqual(120.5, loaded.DailyPresence[0].Seconds);
                CollectionAssert.AreEqual(new[] { 2, 0, 1 }, loaded.IncidentScores);
                Assert.AreEqual(123456789012345L, loaded.Afk.LastActiveUtcTicks);
                Assert.AreEqual(1.56, loaded.Afk.RatePerSecond);
                Assert.AreEqual(3600.25, loaded.Afk.AccumulatedSeconds);
                Assert.AreEqual(5614.5, loaded.Afk.PendingTheoretical);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void GachaHistory_RoundTripsThroughSaveService()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            try
            {
                var service = new SaveService(Path.Combine(dir, "save.json"));
                var save = new PlayerSave();
                save.GachaHistory.Add(new GachaHistoryEntry { Utc = 1, BannerId = "b", Count = 2, CharacterIds = new[] { "a", "b" } });
                Assert.IsTrue(service.Save(save));
                var loaded = service.Load();
                Assert.AreEqual(2, loaded.GachaHistory[0].Count);
                Assert.AreEqual("b", loaded.GachaHistory[0].CharacterIds[1]);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
