using System;
using System.IO;
using System.Text.RegularExpressions;
using BunDauPeo.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hoang.Remake.Tests
{
    public sealed class RemakeContextTests
    {
        [Test]
        public void CommitWritesCandidateBeforePublishingAndFailureLeavesLiveUntouched()
        {
            var save = new HoangSaveData();
            save.Balances.Add(new CurrencyAmount(CurrencyType.Coin, 10));
            var allow = false;
            var context = new RemakeContext(save, candidate =>
            {
                Assert.That(save.Skills.AvailablePoints, Is.EqualTo(20));
                Assert.That(candidate.Skills.AvailablePoints, Is.EqualTo(19));
                return allow;
            });
            Assert.That(context.Commit(s => s.Skills.AvailablePoints--), Is.False);
            Assert.That(save.Skills.AvailablePoints, Is.EqualTo(20));
            allow = true;
            Assert.That(context.Commit(s => s.Skills.AvailablePoints--), Is.True);
            Assert.That(save.Skills.AvailablePoints, Is.EqualTo(19));
            Assert.That(context.Wallet.Get(CurrencyType.Coin), Is.EqualTo(10));
        }

        [Test]
        public void ThrowingSubscriberDoesNotUndoTransactionOrSkipOtherSubscribers()
        {
            var context = new RemakeContext(new HoangSaveData(), _ => true);
            var called = false;
            context.Changed += () => throw new InvalidOperationException("subscriber-test");
            context.Changed += () => called = true;
            LogAssert.Expect(LogType.Exception, new Regex("subscriber-test"));
            Assert.That(context.Commit(s => s.Skills.AvailablePoints = 1), Is.True);
            Assert.That(called, Is.True);
            Assert.That(context.Save.Skills.AvailablePoints, Is.EqualTo(1));
        }

        [Test]
        public void TypedSaveRoundTripPreservesBaseAndEveryFeatureAndResetDeletesBackup()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HoangRemake-" + Guid.NewGuid());
            var path = System.IO.Path.Combine(directory, "save.json");
            try
            {
                var store = new RemakeSaveStore(path);
                var save = new HoangSaveData();
                save.Balances.Add(new CurrencyAmount(CurrencyType.Coin, long.MaxValue - 8));
                save.OwnedCharacterIds.Add("known-base-field");
                save.ShopDaily.DailyRewardDay = 20000;
                save.Skills.AvailablePoints = 7;
                save.Facilities.InventoryItemIds.Add("generator");
                Assert.That(store.Save(save), Is.True);
                Assert.That(store.Save(save), Is.True);
                var loaded = store.Load();
                Assert.That(new Wallet(loaded).Get(CurrencyType.Coin), Is.EqualTo(long.MaxValue - 8));
                Assert.That(loaded.OwnedCharacterIds, Does.Contain("known-base-field"));
                Assert.That(loaded.ShopDaily.DailyRewardDay, Is.EqualTo(20000));
                Assert.That(loaded.Skills.AvailablePoints, Is.EqualTo(7));
                Assert.That(loaded.Facilities.InventoryItemIds, Does.Contain("generator"));
                var context = new RemakeContext(loaded, store.Save, null, store.Reset);
                Assert.That(context.Reset(), Is.True);
                Assert.That(loaded.ShopDaily.DailyRewardDay, Is.Zero);
                Assert.That(loaded.Skills.AvailablePoints, Is.EqualTo(20));
                Assert.That(loaded.Facilities.InventoryItemIds, Is.Empty);
                Assert.That(File.Exists(path + ".bak"), Is.False);
                Assert.That(store.Load().ShopDaily.DailyRewardDay, Is.Zero);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void LegacyBaseSaveLoadsWithFreshFeatureState()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HoangLegacy-" + Guid.NewGuid());
            var path = System.IO.Path.Combine(directory, "save.json");
            try
            {
                var save = new PlayerSave();
                save.Balances.Add(new CurrencyAmount(CurrencyType.Gem, 45));
                Assert.That(new SaveService(path).Save(save), Is.True);
                var loaded = new RemakeSaveStore(path).Load();
                Assert.That(new Wallet(loaded).Get(CurrencyType.Gem), Is.EqualTo(45));
                Assert.That(loaded.ShopDaily, Is.Not.Null);
                Assert.That(loaded.Skills.Slots, Is.Not.Null);
                Assert.That(loaded.Facilities.PlacedItems, Is.Not.Null);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void FailedResetKeepsLiveProgressAndDurableMainSave()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HoangResetLocked-" + Guid.NewGuid());
            var path = System.IO.Path.Combine(directory, "save.json");
            try
            {
                var store = new RemakeSaveStore(path);
                var save = new HoangSaveData();
                save.ShopDaily.DailyRewardDay = 20300;
                Assert.That(store.Save(save), Is.True);
                var context = new RemakeContext(save, store.Save, null, store.Reset);
                using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    LogAssert.Expect(LogType.Error, new Regex("Reset failed:"));
                    Assert.That(context.Reset(), Is.False);
                    Assert.That(save.ShopDaily.DailyRewardDay, Is.EqualTo(20300));
                }
                Assert.That(store.Load().ShopDaily.DailyRewardDay, Is.EqualTo(20300));
                Assert.That(context.Reset(), Is.True);
                Assert.That(store.Load().ShopDaily.DailyRewardDay, Is.Zero);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void LockedSaveNeverPublishesCandidateAndCanRetryAfterUnlock()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HoangLocked-" + Guid.NewGuid());
            var path = System.IO.Path.Combine(directory, "save.json");
            try
            {
                var store = new RemakeSaveStore(path);
                var save = new HoangSaveData();
                Assert.That(store.Save(save), Is.True);
                var context = new RemakeContext(save, store.Save);
                using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    for (var i = 0; i < 4; i++) LogAssert.Expect(LogType.Error, new Regex("Save failed:"));
                    Assert.That(context.Commit(s => s.Skills.AvailablePoints = 2), Is.False);
                    Assert.That(save.Skills.AvailablePoints, Is.EqualTo(20));
                }
                Assert.That(context.Commit(s => s.Skills.AvailablePoints = 2), Is.True);
                Assert.That(store.Load().Skills.AvailablePoints, Is.EqualTo(2));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void FailedResetRetainsRecoveryBackupWhenMainWasCorrupt()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HoangResetRecovery-" + Guid.NewGuid());
            var path = System.IO.Path.Combine(directory, "save.json");
            try
            {
                var store = new RemakeSaveStore(path);
                var save = new HoangSaveData();
                save.ShopDaily.DailyRewardDay = 20400;
                Assert.That(store.Save(save), Is.True);
                Assert.That(store.Save(save), Is.True);
                File.WriteAllText(path, "{ broken json");
                LogAssert.Expect(LogType.Warning, new Regex("Main save unreadable, restored from backup:"));
                var recovered = store.Load();
                var context = new RemakeContext(recovered, store.Save, null, store.Reset);
                using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    LogAssert.Expect(LogType.Error, new Regex("Reset failed:"));
                    Assert.That(context.Reset(), Is.False);
                }
                Assert.That(File.Exists(path + ".bak"), Is.True);
                LogAssert.Expect(LogType.Warning, new Regex("Main save unreadable, restored from backup:"));
                Assert.That(store.Load().ShopDaily.DailyRewardDay, Is.EqualTo(20400));
                Assert.That(context.Save.ShopDaily.DailyRewardDay, Is.EqualTo(20400));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void InterruptedResetRecoversCommittedProgressRatherThanUncommittedDefaults()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HoangResetCrash-" + Guid.NewGuid());
            var path = System.IO.Path.Combine(directory, "save.json");
            try
            {
                var store = new RemakeSaveStore(path);
                var save = new HoangSaveData();
                save.ShopDaily.DailyRewardDay = 20500;
                Assert.That(store.Save(save), Is.True);
                Assert.That(store.Save(save), Is.True);
                File.WriteAllText(path, "{ broken json");
                Assert.That(new SaveService(path + ".reset-candidate").Save(new HoangSaveData()), Is.True);
                File.Move(path + ".bak", path + ".reset-backup-interrupted");
                LogAssert.Expect(LogType.Warning, new Regex("Save unreadable, copy kept at:"));
                LogAssert.Expect(LogType.Warning, "Recovered progress from an interrupted restaurant reset.");
                Assert.That(store.Load().ShopDaily.DailyRewardDay, Is.EqualTo(20500));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}
