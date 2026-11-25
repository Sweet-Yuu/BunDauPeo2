using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.Tests
{
    // Catalog: Story s1, Battle b1, Battle b2; thể lực 100, mỗi màn Battle tốn 10, quái 1 HP.
    public class StageProgressServiceTests
    {
        // AfterGameSessionReset_FirstStageAvailableAgain_StaminaFull: ResetToDefaults đưa GameLanguage.Current về En.
        [TearDown]
        public void TearDown() => GameLanguage.Current = Language.Vi;

        private static StageDefinition Battle(string id) => CombatTestData.Stage(id, new[] { CombatTestData.Enemy(hp: 1) });

        private static CombatRig Rig() =>
            new CombatRig(new[] { CombatTestData.Story("s1"), Battle("b1"), Battle("b2") });

        private static string Cleared(CombatRig rig) => string.Join(",", rig.Save.ClearedStages);

        [Test]
        public void GetStatus_NewSave_FirstAvailable_RestLocked()
        {
            var rig = Rig();

            Assert.AreEqual(StageStatus.Available, rig.Progress.GetStatus("s1"));
            Assert.AreEqual(StageStatus.Locked, rig.Progress.GetStatus("b1"));
            Assert.AreEqual(StageStatus.Locked, rig.Progress.GetStatus("b2"), "two ahead of the frontier is locked too");
        }

        [Test]
        public void GetStatus_ClearingOne_OpensNext()
        {
            var rig = Rig();

            Assert.AreEqual(StageCompleteStatus.Success, rig.Progress.CompleteStoryAsync("s1").Sync().Status);

            Assert.AreEqual(StageStatus.Cleared, rig.Progress.GetStatus("s1"));
            Assert.AreEqual(StageStatus.Available, rig.Progress.GetStatus("b1"));
            Assert.AreEqual(StageStatus.Locked, rig.Progress.GetStatus("b2"));

            CombatTestData.PaidWin(rig.Service, "b1");
            rig.Progress.CompleteBattleAsync(rig.Service.Current).Sync();

            Assert.AreEqual(StageStatus.Cleared, rig.Progress.GetStatus("b1"));
            Assert.AreEqual(StageStatus.Available, rig.Progress.GetStatus("b2"));
        }

        [Test]
        public void GetStatus_ClearedStage_StaysOpenWhenPreviousNotCleared()
        {
            // Chèn "mid" trước b2 sau khi người chơi đã clear b2: b2 không bị khoá lại.
            var rig = new CombatRig(new[] { CombatTestData.Story("s1"), Battle("b1"), Battle("mid"), Battle("b2") });
            rig.Clear("s1", "b1", "b2");

            Assert.AreEqual(StageStatus.Available, rig.Progress.GetStatus("mid"));
            Assert.AreEqual(StageStatus.Cleared, rig.Progress.GetStatus("b2"), "its own clear keeps it open");

            var onlyB2 = new CombatRig(new[] { CombatTestData.Story("s1"), Battle("b1"), Battle("b2") });
            onlyB2.Clear("b2");
            Assert.AreEqual(StageStatus.Cleared, onlyB2.Progress.GetStatus("b2"));
            Assert.AreEqual(StageStatus.Locked, onlyB2.Progress.GetStatus("b1"), "clearing b2 does not open what is before it");
        }

        [Test]
        public void GetStatus_UnknownId()
        {
            var rig = Rig();

            foreach (var id in new[] { "nope", "", null })
                Assert.AreEqual(StageStatus.Unknown, rig.Progress.GetStatus(id), $"id = '{id}'");
        }

        [Test]
        public void UnknownIdsInClearedStages_IgnoredAndKept()
        {
            var rig = Rig();
            rig.Clear("zzz", "s1");

            Assert.AreEqual(StageStatus.Unknown, rig.Progress.GetStatus("zzz"));
            Assert.AreEqual(StageStatus.Cleared, rig.Progress.GetStatus("s1"));
            Assert.AreEqual(StageStatus.Available, rig.Progress.GetStatus("b1"));
            Assert.AreEqual(StageStatus.Locked, rig.Progress.GetStatus("b2"));

            CombatTestData.PaidWin(rig.Service, "b1");
            rig.Progress.CompleteBattleAsync(rig.Service.Current).Sync();

            CollectionAssert.AreEqual(new[] { "zzz", "s1", "b1" }, rig.Save.ClearedStages, "the stale id survives for when the stage returns");
        }

        [Test]
        public void NullCatalog_EverythingUnknown_NoThrow()
        {
            var rig = Rig();
            rig.Clear("s1");
            var won = CombatTestData.PaidWin(rig.Service, "b1");
            var before = rig.State();
            StageProgressService progress = null;

            Assert.DoesNotThrow(() => progress = new StageProgressService(rig.Save, rig.Wallet, null, rig.Stamina, rig.Server, rig.Gate, rig.Apply));

            Assert.AreEqual(StageStatus.Unknown, progress.GetStatus("s1"));
            Assert.AreEqual(StageEnterStatus.Unknown, progress.CanEnter("b1", 1));
            Assert.AreEqual(StageCompleteStatus.Unknown, progress.CompleteStoryAsync("s1").Sync().Status);
            Assert.AreEqual(StageCompleteStatus.Unknown, progress.CompleteBattleAsync(won).Sync().Status);
            Assert.IsFalse(won.RewardsClaimed);
            Assert.AreEqual(before, rig.State());
        }

        [Test]
        public void CanEnter_Ready_ChangesNothing()
        {
            var rig = Rig();
            rig.Clear("s1");
            var before = rig.State();

            Assert.AreEqual(StageEnterStatus.Ready, rig.Progress.CanEnter("b1", 1));
            Assert.AreEqual(StageEnterStatus.Ready, rig.Progress.CanEnter("b1", 5));

            Assert.AreEqual(before, rig.State());
            Assert.AreEqual(0, rig.PersistCalls);
        }

        [Test]
        public void CanEnter_ReportsFirstFailingCheck_InOrder()
        {
            // Mọi màn dưới đây đều khoá (b0 chưa clear), đội 0 người, thể lực 5: chỉ thứ tự kiểm quyết định kết quả.
            var bad = Battle("bad");
            var rig = new CombatRig(new[] { Battle("b0"), CombatTestData.Story("story"), bad, Battle("b") });
            TestData.Edit(bad, so => so.FindProperty("_staminaCost").intValue = 0); // hỏng sau khi dựng, để config không bị từ chối
            rig.SetStamina(5);

            Assert.AreEqual(StageEnterStatus.Unknown, rig.Progress.CanEnter("nope", 0), "unknown id wins over everything");
            Assert.AreEqual(StageEnterStatus.Invalid, rig.Progress.CanEnter("bad", 0), "invalid wins over locked, empty team, stamina");
            Assert.AreEqual(StageEnterStatus.WrongKind, rig.Progress.CanEnter("story", 0), "story wins over locked, empty team, stamina");
            Assert.AreEqual(StageEnterStatus.Locked, rig.Progress.CanEnter("b", 0), "locked wins over empty team and stamina");

            rig.Clear("b0", "story", "bad");
            Assert.AreEqual(StageEnterStatus.EmptyTeam, rig.Progress.CanEnter("b", 0), "empty team wins over stamina");
            Assert.AreEqual(StageEnterStatus.NotEnoughStamina, rig.Progress.CanEnter("b", 1));

            rig.SetStamina(10);
            Assert.AreEqual(StageEnterStatus.Ready, rig.Progress.CanEnter("b", 1), "exactly the cost is enough");
            rig.SetStamina(9);
            Assert.AreEqual(StageEnterStatus.NotEnoughStamina, rig.Progress.CanEnter("b", 1));
        }

        [Test]
        public void CompleteBattle_FirstClear_PaysFirstReward_RecordsId_StaminaUntouched()
        {
            var rig = Rig();
            rig.Clear("s1");
            var won = CombatTestData.PaidWin(rig.Service, "b1");
            var staminaAfterEnter = (rig.Save.Stamina.Current, rig.Save.Stamina.AnchorUtcTicks);
            Assert.AreEqual(90, staminaAfterEnter.Current);

            var result = rig.Progress.CompleteBattleAsync(won).Sync();

            Assert.AreEqual(StageCompleteStatus.Success, result.Status);
            Assert.IsTrue(result.FirstClear);
            Assert.IsTrue(result.Saved);
            Assert.AreEqual(10, result.StaminaSpent);
            CollectionAssert.AreEqual(
                new[] { new CurrencyAmount(CurrencyType.Material, 30), new CurrencyAmount(CurrencyType.Gem, 150) }, result.Rewards.ToArray());
            Assert.AreEqual(30, rig.Wallet.Get(CurrencyType.Material));
            Assert.AreEqual(150, rig.Wallet.Get(CurrencyType.Gem));
            CollectionAssert.AreEqual(new[] { "s1", "b1" }, rig.Save.ClearedStages);
            Assert.IsTrue(won.RewardsClaimed);
            Assert.AreEqual(staminaAfterEnter, (rig.Save.Stamina.Current, rig.Save.Stamina.AnchorUtcTicks), "CompleteBattle never touches stamina");
            Assert.AreEqual(90, rig.Stamina.Current);
        }

        [Test]
        public void CompleteBattle_Repeat_PaysRepeatReward_NoDuplicateId()
        {
            var rig = Rig();
            rig.Clear("s1");
            rig.Progress.CompleteBattleAsync(CombatTestData.PaidWin(rig.Service, "b1")).Sync();

            var result = rig.Progress.CompleteBattleAsync(CombatTestData.PaidWin(rig.Service, "b1")).Sync();

            Assert.AreEqual(StageCompleteStatus.Success, result.Status);
            Assert.IsFalse(result.FirstClear);
            CollectionAssert.AreEqual(
                new[] { new CurrencyAmount(CurrencyType.Material, 30), new CurrencyAmount(CurrencyType.Gem, 30) }, result.Rewards.ToArray());
            Assert.AreEqual(60, rig.Wallet.Get(CurrencyType.Material));
            Assert.AreEqual(180, rig.Wallet.Get(CurrencyType.Gem), "150 first time, only +30 the second time");
            Assert.AreEqual(1, rig.Save.ClearedStages.Count(id => id == "b1"));
            Assert.AreEqual(80, rig.Stamina.Current, "each entry paid 10");
        }

        [Test]
        public void CompleteBattle_PersistOnce_AfterAllChanges()
        {
            var rig = Rig();
            rig.Clear("s1");
            var won = CombatTestData.PaidWin(rig.Service, "b1");
            rig.Watched = won;
            var persistBefore = rig.PersistCalls;
            var order = new List<string>();
            rig.Progress.Changed += () => order.Add($"changed:{rig.PersistCalls - persistBefore}");

            rig.Progress.CompleteBattleAsync(won).Sync();

            Assert.AreEqual(persistBefore + 1, rig.PersistCalls, "exactly one persist");
            CollectionAssert.AreEqual(new[] { "changed:1" }, order, "Changed fires once, after the single persist");
            var seen = rig.Snapshots.Last();
            Assert.AreEqual((30L, 150L, "s1,b1"), (seen.Material, seen.Gem, seen.Cleared), "everything was already changed when persist ran");
            Assert.AreEqual(true, seen.WatchedClaimed);
            Assert.AreEqual(90, seen.StaminaCurrent);
        }

        [Test]
        public void CompleteBattle_PersistFails_SavedFalse_MemoryKept()
        {
            var rig = Rig();
            rig.Clear("s1");
            var won = CombatTestData.PaidWin(rig.Service, "b1");
            rig.PersistResult = false;

            var result = rig.Progress.CompleteBattleAsync(won).Sync();

            Assert.AreEqual(StageCompleteStatus.Success, result.Status);
            Assert.IsFalse(result.Saved);
            Assert.AreEqual(30, rig.Wallet.Get(CurrencyType.Material), "memory keeps the reward; OnApplicationPause will save it again");
            Assert.AreEqual(150, rig.Wallet.Get(CurrencyType.Gem));
            CollectionAssert.AreEqual(new[] { "s1", "b1" }, rig.Save.ClearedStages);
            Assert.IsTrue(won.RewardsClaimed);
        }

        [Test]
        public void CompleteBattle_SameSessionTwice_AlreadyClaimed_ChangesNothing()
        {
            var rig = Rig();
            rig.Clear("s1");
            var won = CombatTestData.PaidWin(rig.Service, "b1");
            rig.Progress.CompleteBattleAsync(won).Sync();
            var before = rig.State();
            var changed = 0;
            rig.Progress.Changed += () => changed++;

            var result = rig.Progress.CompleteBattleAsync(won).Sync();

            Assert.AreEqual(StageCompleteStatus.AlreadyClaimed, result.Status);
            Assert.IsFalse(result.Saved);
            CollectionAssert.IsEmpty(result.Rewards);
            Assert.AreEqual(before, rig.State(), "no double reward, no extra persist");
            Assert.AreEqual(0, changed);
        }

        [Test]
        public void CompleteBattle_NotWon_ChangesNothing()
        {
            var rig = Rig();
            rig.Clear("s1");
            var playing = rig.Service.EnterStageAsync("b1").Sync().Session;
            var before = rig.State();

            Assert.AreEqual(StageCompleteStatus.NotWon, rig.Progress.CompleteBattleAsync(playing).Sync().Status, "still being fought");
            playing.Surrender();
            Assert.AreEqual(BattleState.Lost, playing.State);
            var result = rig.Progress.CompleteBattleAsync(playing).Sync();

            Assert.AreEqual(StageCompleteStatus.NotWon, result.Status, "lost");
            CollectionAssert.IsEmpty(result.Rewards);
            Assert.AreEqual(before, rig.State());
            Assert.IsFalse(playing.RewardsClaimed);
        }

        [Test]
        public void CompleteBattle_LockedStage_ChangesNothing()
        {
            var rig = Rig();
            rig.Clear("s1");
            var won = CombatTestData.PaidWin(rig.Service, "b1");
            rig.Save.ClearedStages.Remove("s1");
            var before = rig.State();

            var result = rig.Progress.CompleteBattleAsync(won).Sync();

            Assert.AreEqual(StageCompleteStatus.Locked, result.Status);
            Assert.AreEqual(before, rig.State());
            Assert.IsFalse(won.RewardsClaimed);
        }

        [Test]
        public void CompleteBattle_NotPaid_ChangesNothing()
        {
            // Trận vào thẳng từ dải DEV (StartBattle): thắng cũng không có thưởng, kể cả màn đang khoá.
            var rig = Rig();
            var trial = rig.Service.StartBattle("b2").Session;
            CombatTestData.PlayToWin(trial);
            var before = rig.State();
            var changed = 0;
            rig.Progress.Changed += () => changed++;

            var result = rig.Progress.CompleteBattleAsync(trial).Sync();

            Assert.AreEqual(StageCompleteStatus.NotPaid, result.Status, "NotPaid is checked before the locked check");
            CollectionAssert.IsEmpty(result.Rewards);
            Assert.AreEqual(before, rig.State());
            Assert.AreEqual(0, rig.PersistCalls);
            Assert.AreEqual(0, rig.Server.CommandCalls, "a DEV battle never reaches the server");
            Assert.AreEqual(0, changed);
            Assert.IsFalse(trial.RewardsClaimed);
            Assert.AreEqual(0, rig.Wallet.Get(CurrencyType.Material));
        }

        [Test]
        public void CompleteBattle_InvalidStage_ChangesNothing()
        {
            var rig = Rig();
            rig.Clear("s1");
            var won = CombatTestData.PaidWin(rig.Service, "b1");
            rig.Service.Config.Stages.TryGet("b1", out var stage);
            TestData.Edit(stage, so => so.FindProperty("_staminaCost").intValue = 0);
            var before = rig.State();

            var result = rig.Progress.CompleteBattleAsync(won).Sync();

            Assert.AreEqual(StageCompleteStatus.Invalid, result.Status);
            Assert.AreEqual(before, rig.State());
            Assert.IsFalse(won.RewardsClaimed);
        }

        [Test]
        public void CompleteBattle_StageNotInCatalog_Unknown()
        {
            var rig = Rig();
            rig.Clear("s1");
            var won = CombatTestData.PaidWin(rig.Service, "b1");
            var otherCatalog = CombatTestData.Catalog(CombatTestData.Story("s1"), Battle("b2"));
            var progress = new StageProgressService(rig.Save, rig.Wallet, otherCatalog, rig.Stamina, rig.Server, rig.Gate, rig.Apply);
            var before = rig.State();

            var result = progress.CompleteBattleAsync(won).Sync();

            Assert.AreEqual(StageCompleteStatus.Unknown, result.Status);
            Assert.AreEqual(before, rig.State());
            Assert.IsFalse(won.RewardsClaimed);
        }

        [Test]
        public void CompleteBattle_ChangedHandlerThrows_StillPersists()
        {
            var rig = Rig();
            rig.Clear("s1");
            var won = CombatTestData.PaidWin(rig.Service, "b1");
            var persistBefore = rig.PersistCalls;
            var later = 0;
            rig.Progress.Changed += () => throw new InvalidOperationException("boom");
            rig.Progress.Changed += () => later++;
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            var result = rig.Progress.CompleteBattleAsync(won).Sync();

            Assert.AreEqual(StageCompleteStatus.Success, result.Status);
            Assert.IsTrue(result.Saved);
            Assert.AreEqual(persistBefore + 1, rig.PersistCalls);
            Assert.AreEqual(1, later, "a throwing handler does not stop the next one");
        }

        [Test]
        public void CompleteStory_FirstTime_AddsId_NoRewardNoStamina_PersistOnce()
        {
            var rig = Rig();
            var changed = 0;
            rig.Progress.Changed += () => changed++;

            var result = rig.Progress.CompleteStoryAsync("s1").Sync();

            Assert.AreEqual(StageCompleteStatus.Success, result.Status);
            Assert.IsTrue(result.Saved);
            CollectionAssert.IsEmpty(result.Rewards);
            Assert.AreEqual(0, result.StaminaSpent);
            CollectionAssert.AreEqual(new[] { "s1" }, rig.Save.ClearedStages);
            Assert.AreEqual(0, rig.Wallet.Get(CurrencyType.Material) + rig.Wallet.Get(CurrencyType.Gem));
            Assert.AreEqual(100, rig.Stamina.Current);
            Assert.AreEqual(0, rig.Save.Stamina.AnchorUtcTicks, "stamina bar is still full");
            Assert.AreEqual(1, rig.PersistCalls);
            Assert.AreEqual(1, changed);
            Assert.AreEqual("s1", rig.Snapshots[0].Cleared, "persist ran after the id was added");
        }

        [Test]
        public void CompleteStory_Again_AlreadyCleared_NoPersist()
        {
            var rig = Rig();
            rig.Progress.CompleteStoryAsync("s1").Sync();
            var before = rig.State();

            var result = rig.Progress.CompleteStoryAsync("s1").Sync();

            Assert.AreEqual(StageCompleteStatus.AlreadyCleared, result.Status);
            Assert.AreEqual(before, rig.State());
            Assert.AreEqual(1, rig.PersistCalls);
        }

        [Test]
        public void CompleteStory_BattleId_WrongKind()
        {
            var rig = Rig();
            rig.Clear("s1");
            var before = rig.State();

            Assert.AreEqual(StageCompleteStatus.WrongKind, rig.Progress.CompleteStoryAsync("b1").Sync().Status);
            Assert.AreEqual(StageCompleteStatus.Unknown, rig.Progress.CompleteStoryAsync("nope").Sync().Status);

            Assert.AreEqual(before, rig.State());
        }

        [Test]
        public void CompleteStory_Locked_ChangesNothing()
        {
            var rig = new CombatRig(new[] { Battle("b1"), CombatTestData.Story("s2") });
            var before = rig.State();

            var result = rig.Progress.CompleteStoryAsync("s2").Sync();

            Assert.AreEqual(StageCompleteStatus.Locked, result.Status);
            Assert.AreEqual(before, rig.State());
            Assert.AreEqual(0, rig.PersistCalls);
        }

        private static CombatRig PotionRig()
        {
            var b1 = Battle("b1");
            TestData.Edit(b1, so => CombatTestData.SetRewards(so.FindProperty("_firstClearReward"),
                new CurrencyAmount(CurrencyType.Material, 30), new CurrencyAmount(CurrencyType.Gem, 150), new CurrencyAmount(CurrencyType.ExpPotion, 10)));
            return new CombatRig(new[] { CombatTestData.Story("s1"), b1, Battle("b2") });
        }

        [Test]
        public void CompleteStage_AddsExpPotionToWallet()
        {
            var rig = PotionRig();
            rig.Clear("s1");
            rig.Progress.CompleteBattleAsync(CombatTestData.PaidWin(rig.Service, "b1")).Sync();
            Assert.AreEqual(10, rig.Wallet.Get(CurrencyType.ExpPotion));
        }

        [Test]
        public void CompleteStage_ResultListsExpPotionLine()
        {
            var rig = PotionRig();
            rig.Clear("s1");
            var result = rig.Progress.CompleteBattleAsync(CombatTestData.PaidWin(rig.Service, "b1")).Sync();
            CollectionAssert.Contains(result.Rewards.ToArray(), new CurrencyAmount(CurrencyType.ExpPotion, 10));
        }

        [Test]
        public void CompleteStage_ReplyWithoutExpPotionField_AddsNothing()
        {
            var old = JsonUtility.FromJson<StageRewardPayload>("{\"FirstClear\":true,\"Gem\":150,\"Material\":30}");
            Assert.AreEqual(0, old.ExpPotion);

            var rig = Rig();
            rig.Clear("s1");
            var result = rig.Progress.CompleteBattleAsync(CombatTestData.PaidWin(rig.Service, "b1")).Sync();
            Assert.AreEqual(0, rig.Wallet.Get(CurrencyType.ExpPotion));
            Assert.That(result.Rewards.Select(r => r.Type), Has.None.EqualTo(CurrencyType.ExpPotion));
        }

        // Review Focus 2: thắng, mất trả lời CompleteStage, bấm Thử lại: Gem vào một lần, Material cộng một lần, màn đã qua.
        [Test]
        public void CompleteBattle_LostReplyThenRetry_MaterialOnce()
        {
            var rig = Rig();
            rig.Clear("s1");
            var won = CombatTestData.PaidWin(rig.Service, "b1");
            var lost = 0;
            rig.Gate.Lost += retry =>
            {
                lost++;
                retry();
            };
            rig.Server.LoseNextReply();

            var result = rig.Progress.CompleteBattleAsync(won).Sync();

            Assert.AreEqual(1, lost, "the player saw the Lost box once and pressed Retry");
            Assert.AreEqual(StageCompleteStatus.Success, result.Status);
            Assert.IsTrue(result.FirstClear);
            Assert.AreEqual(30, rig.Wallet.Get(CurrencyType.Material), "Material added once, not twice");
            Assert.AreEqual(150, rig.Wallet.Get(CurrencyType.Gem), "Gem added once");
            Assert.AreEqual(150, rig.Server.State.Gem);
            CollectionAssert.AreEqual(new[] { "s1", "b1" }, rig.Save.ClearedStages);
            Assert.IsTrue(won.RewardsClaimed);
        }

        [Test]
        public void CompleteBattle_Offline_NoChange_CanTryAgain()
        {
            var rig = Rig();
            rig.Clear("s1");
            var won = CombatTestData.PaidWin(rig.Service, "b1");
            var before = rig.State();
            rig.Server.FailNext(OnlineStatus.NetworkError);

            var result = rig.Progress.CompleteBattleAsync(won).Sync();

            Assert.AreEqual(StageCompleteStatus.Offline, result.Status);
            Assert.AreEqual(before, rig.State());
            Assert.IsFalse(won.RewardsClaimed);
            Assert.AreEqual(StageCompleteStatus.Success, rig.Progress.CompleteBattleAsync(won).Sync().Status, "the ticket is still pending on the server");
            Assert.AreEqual(30, rig.Wallet.Get(CurrencyType.Material));
        }

        [Test]
        public void CompleteBattle_ServerTicketMismatch_NotPaid_NoChange()
        {
            var rig = Rig();
            rig.Clear("s1");
            var won = CombatTestData.PaidWin(rig.Service, "b1");
            rig.Server.State.PendingTicket = "someone-else";
            var before = rig.State();

            var result = rig.Progress.CompleteBattleAsync(won).Sync();

            Assert.AreEqual(StageCompleteStatus.NotPaid, result.Status);
            Assert.AreEqual(before, rig.State());
            Assert.IsFalse(won.RewardsClaimed);
            Assert.AreEqual(0, rig.Wallet.Get(CurrencyType.Material));
        }

        // Máy tưởng chưa qua nhưng server đã ghi (phiên trước mất trả lời): server là bản chính.
        [Test]
        public void CompleteStory_AlreadyCleared_FromServer()
        {
            var rig = Rig();
            rig.Server.State.ClearedStages.Add("s1");
            var before = rig.State();

            var result = rig.Progress.CompleteStoryAsync("s1").Sync();

            Assert.AreEqual(StageCompleteStatus.AlreadyCleared, result.Status);
            Assert.AreEqual(1, rig.Server.CommandCalls);
            Assert.AreEqual(before, rig.State());
        }

        [Test]
        public void CompleteStory_Offline_NoChange()
        {
            var rig = Rig();
            var before = rig.State();
            rig.Server.FailNext(OnlineStatus.NetworkError);

            var result = rig.Progress.CompleteStoryAsync("s1").Sync();

            Assert.AreEqual(StageCompleteStatus.Offline, result.Status);
            Assert.AreEqual(before, rig.State());
            Assert.AreEqual(0, rig.PersistCalls);
        }

        [Test]
        public void AfterGameSessionReset_FirstStageAvailableAgain_StaminaFull()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            try
            {
                var a = CombatTestData.Hero("a");
                var session = new GameSession(new SaveService(Path.Combine(dir, "save.json")), TestData.Catalog(a),
                    TestData.Config(new[] { new CurrencyAmount(CurrencyType.Coin, 500) }, a));
                var config = CombatTestData.Config();
                var now = CombatRig.T0;
                var stamina = new StaminaService(session.Save, config, () => now);
                var catalog = CombatTestData.Catalog(CombatTestData.Story("s1"), Battle("b1"));
                var progress = new StageProgressService(session.Save, session.Wallet, catalog, stamina, new LocalGameServer(),
                    new CommandGate(_ => new System.Threading.Tasks.TaskCompletionSource<bool>().Task), _ => true);
                // Server giữ cả hai; ở đây chỉ cần save đang có tiến độ và thể lực dở dang để Reset xoá.
                session.Save.ClearedStages.Add("s1");
                session.Save.Stamina = new StaminaState { Current = 70, AnchorUtcTicks = now.Ticks };
                Assert.AreEqual(StageStatus.Available, progress.GetStatus("b1"));
                Assert.AreEqual(70, stamina.Current);

                Assert.IsTrue(session.ResetToDefaults());

                Assert.AreEqual(StageStatus.Available, progress.GetStatus("s1"), "the service reads the overwritten save, no stale cache");
                Assert.AreEqual(StageStatus.Locked, progress.GetStatus("b1"));
                Assert.AreEqual(100, stamina.Current);
                Assert.AreEqual(0d, stamina.SecondsToNext);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
