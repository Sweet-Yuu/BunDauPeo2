using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.Tests
{
    public class CombatServiceTests
    {
        // GameSessionReset_ThroughResetCompleted_ClearsCurrent: ResetToDefaults đưa GameLanguage.Current về En.
        [TearDown]
        public void TearDown() => GameLanguage.Current = Language.Vi;

        private static CharacterDefinition Hero(string id, CharacterClass cls = CharacterClass.Fighter) =>
            CombatTestData.Hero(id, cls);

        private static Roster RosterWith(params CharacterDefinition[] owned)
        {
            var roster = new Roster(new PlayerSave(), TestData.Catalog(owned));
            foreach (var hero in owned) roster.Add(hero.Id);
            return roster;
        }

        private static Roster TeamOf(params CharacterDefinition[] heroes)
        {
            var roster = RosterWith(heroes);
            foreach (var hero in heroes) roster.AddToTeam(hero.Id);
            return roster;
        }

        private static string[] TeamIds(Roster roster) => roster.GetBattleTeam().Select(c => c.Id).ToArray();

        private static CombatConfig ConfigWith(params StageDefinition[] stages) =>
            CombatTestData.Config(so => so.FindProperty("_stages").objectReferenceValue = CombatTestData.Catalog(stages));

        private static StageDefinition Battle(string id) => CombatTestData.Stage(id, new[] { CombatTestData.Enemy() });

        [Test]
        public void StartBattle_Success_BuildsSessionFromBattleTeam()
        {
            var service = CombatTestData.Service(roster: TeamOf(Hero("a"), Hero("m", CharacterClass.Mage)));

            var result = service.StartBattle("st_a");

            Assert.AreEqual(StartBattleStatus.Success, result.Status);
            Assert.AreSame(result.Session, service.Current);
            CollectionAssert.AreEqual(new[] { "a", "m" }, result.Session.Allies.Select(u => u.DisplayName).ToArray());
            Assert.AreEqual("st_a", result.Session.Stage.Id);
            Assert.AreEqual(1, result.Session.Enemies.Count);
        }

        [Test]
        public void StartBattle_UnknownStage()
        {
            var service = CombatTestData.Service(roster: TeamOf(Hero("a")));

            foreach (var id in new[] { "nope", "", null })
            {
                var result = service.StartBattle(id);

                Assert.AreEqual(StartBattleStatus.UnknownStage, result.Status, $"id = '{id}'");
                Assert.IsNull(result.Session, $"id = '{id}'");
            }

            Assert.IsNull(service.Current);
        }

        [Test]
        public void StartBattle_StoryStage_NotBattle()
        {
            var service = CombatTestData.Service(ConfigWith(CombatTestData.Story("story"), Battle("st_a")), TeamOf(Hero("a")));

            var result = service.StartBattle("story");

            Assert.AreEqual(StartBattleStatus.NotBattle, result.Status);
            Assert.IsNull(service.Current);
        }

        [Test]
        public void StartBattle_EmptyTeam()
        {
            var service = CombatTestData.Service(roster: RosterWith(Hero("a")));

            Assert.AreEqual(StartBattleStatus.EmptyTeam, service.StartBattle("st_a").Status);
            Assert.IsNull(service.Current);
        }

        [Test]
        public void StartBattle_CharacterFailsValidate_InvalidCharacter_CurrentUnchanged()
        {
            var good = Hero("good");
            var broken = Hero("broken");
            TestData.Edit(broken, so => so.FindProperty("_stars").intValue = 9);
            var roster = RosterWith(good, broken);
            roster.AddToTeam("good");
            var service = CombatTestData.Service(roster: roster);
            var first = service.StartBattle("st_a").Session;
            Assert.IsNotNull(first);

            roster.AddToTeam("broken");
            var result = service.StartBattle("st_a");

            Assert.AreEqual(StartBattleStatus.InvalidCharacter, result.Status);
            Assert.IsNull(result.Session);
            Assert.AreSame(first, service.Current, "a failed start leaves the running battle alone");
        }

        [Test]
        public void StartBattle_SkillBreaksCombatRules_InvalidCharacter()
        {
            // TestData.Character qua được Validate() nhưng skill hồi chiêu 0 và ultimate giá 0 vi phạm CombatSkillRules.
            var service = CombatTestData.Service(roster: TeamOf(TestData.Character("plain")));

            var result = service.StartBattle("st_a");

            Assert.AreEqual(StartBattleStatus.InvalidCharacter, result.Status);
            Assert.IsNull(service.Current);
        }

        [Test]
        public void InvalidConfig_NotRunning_StartBattleInvalidConfig()
        {
            var config = CombatTestData.Config(so => so.FindProperty("_maxEnergy").intValue = 0);
            LogAssert.Expect(LogType.Error, new Regex("invalid config"));

            var service = CombatTestData.Service(config, TeamOf(Hero("a")));

            Assert.IsFalse(service.IsRunning);
            Assert.AreSame(config, service.Config);
            Assert.AreEqual(StartBattleStatus.InvalidConfig, service.StartBattle("st_a").Status);
            Assert.IsNull(service.Current);
        }

        [Test]
        public void InvalidConfig_NullStages_ConstructorDoesNotThrow()
        {
            var config = CombatTestData.Config(so => so.FindProperty("_stages").objectReferenceValue = null);
            LogAssert.Expect(LogType.Error, new Regex("invalid config"));

            CombatService service = null;
            Assert.DoesNotThrow(() => service = CombatTestData.Service(config, TeamOf(Hero("a"))));

            Assert.IsFalse(service.IsRunning);
            Assert.AreEqual(StartBattleStatus.InvalidConfig, service.StartBattle("st_a").Status);
        }

        [Test]
        public void EndBattle_ClearsCurrent()
        {
            var service = CombatTestData.Service(roster: TeamOf(Hero("a")));
            service.StartBattle("st_a");

            service.EndBattle();

            Assert.IsNull(service.Current);
        }

        [Test]
        public void ResetRuntime_ClearsCurrentAutoAndFast()
        {
            var service = CombatTestData.Service(roster: TeamOf(Hero("a")));
            service.StartBattle("st_a");
            service.AutoEnabled = true;
            service.FastEnabled = true;

            service.ResetRuntime();

            Assert.IsNull(service.Current);
            Assert.IsFalse(service.AutoEnabled);
            Assert.IsFalse(service.FastEnabled);
        }

        [Test]
        public void GameSessionReset_ThroughResetCompleted_ClearsCurrent()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            try
            {
                var a = Hero("a");
                var session = new GameSession(new SaveService(Path.Combine(dir, "save.json")), TestData.Catalog(a),
                    TestData.Config(new[] { new CurrencyAmount(CurrencyType.Coin, 500) }, a));
                session.Roster.Add("a"); // nhân vật khởi đầu do server cấp, không phải session
                session.Roster.AddToTeam("a");
                var service = CombatTestData.Service(roster: session.Roster, persist: session.Persist);
                session.ResetCompleted += service.ResetRuntime;
                Assert.AreEqual(StartBattleStatus.Success, service.StartBattle("st_a").Status);
                service.AutoEnabled = true;

                Assert.IsTrue(session.ResetToDefaults());

                Assert.IsNull(service.Current, "Reset Save Data cancels the running battle (D-005)");
                Assert.IsFalse(service.AutoEnabled);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void AutoAndFast_DefaultOff_KeptAcrossBattles()
        {
            var service = CombatTestData.Service(roster: TeamOf(Hero("a")));
            Assert.IsFalse(service.AutoEnabled);
            Assert.IsFalse(service.FastEnabled);
            service.StartBattle("st_a");
            service.AutoEnabled = true;
            service.FastEnabled = true;

            service.EndBattle();
            service.StartBattle("st_a");

            Assert.IsTrue(service.AutoEnabled);
            Assert.IsTrue(service.FastEnabled);
        }

        [Test]
        public void Persist_ReturnsPersistResult()
        {
            foreach (var result in new[] { true, false })
            {
                var calls = 0;
                var service = CombatTestData.Service(persist: () =>
                {
                    calls++;
                    return result;
                });

                Assert.AreEqual(result, service.Persist(), $"persist returns {result}");
                Assert.AreEqual(1, calls, $"persist returns {result}");
            }
        }

        [Test]
        public void DevFillTeam_EmptyTeam_AddsUpToFiveOwned()
        {
            var three = RosterWith(Hero("a"), Hero("b"), Hero("c"));
            three.AssignStaff("c", StaffRole.Cook);
            Assert.AreEqual(3, DevOverlay.FillTeamIfEmpty(three));
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, TeamIds(three));
            CollectionAssert.IsEmpty(three.GetStaff(StaffRole.Cook), "c left the restaurant for the team");

            var seven = RosterWith(Enumerable.Range(1, 7).Select(i => Hero($"h{i}")).ToArray());
            Assert.AreEqual(5, DevOverlay.FillTeamIfEmpty(seven));
            CollectionAssert.AreEqual(new[] { "h1", "h2", "h3", "h4", "h5" }, TeamIds(seven), "the first five in ownership order");
        }

        [Test]
        public void DevFillTeam_NonEmptyTeam_Unchanged()
        {
            var roster = RosterWith(Hero("a"), Hero("b"), Hero("c"));
            roster.AddToTeam("b");

            Assert.AreEqual(0, DevOverlay.FillTeamIfEmpty(roster));

            CollectionAssert.AreEqual(new[] { "b" }, TeamIds(roster));
        }

        [Test]
        public void DevNextBattleStage_SkipsStory_Wraps()
        {
            var catalog = CombatTestData.Catalog(Battle("s1"), CombatTestData.Story("story"), Battle("s2"));

            Assert.AreEqual("s1", DevOverlay.NextBattleStageId(catalog, null), "null: first battle stage");
            Assert.AreEqual("s2", DevOverlay.NextBattleStageId(catalog, "s1"), "the story between them is skipped");
            Assert.AreEqual("s1", DevOverlay.NextBattleStageId(catalog, "s2"), "wraps around");
            Assert.AreEqual("s1", DevOverlay.NextBattleStageId(catalog, "story"), "not a battle stage: first battle stage");
            Assert.AreEqual("s1", DevOverlay.NextBattleStageId(catalog, "missing"), "unknown id: first battle stage");

            var single = CombatTestData.Catalog(Battle("only"));
            Assert.AreEqual("only", DevOverlay.NextBattleStageId(single, "only"));
            Assert.IsNull(DevOverlay.NextBattleStageId(CombatTestData.Catalog(CombatTestData.Story("story")), null), "no battle stage at all");
            Assert.IsNull(DevOverlay.NextBattleStageId(null, null), "invalid config leaves no catalog");
        }

        [Test]
        public void Stamina_IsExposedByService()
        {
            var stamina = new StaminaService(new PlayerSave(), CombatTestData.Config(), () => DateTime.UtcNow);

            Assert.AreSame(stamina, CombatTestData.Service(stamina: stamina).Stamina);
            Assert.IsNotNull(CombatTestData.Service().Stamina, "the helper supplies one when a test does not care");
        }

        [Test]
        public void Progress_IsExposedByService()
        {
            var progress = new StageProgressService(new PlayerSave(), new Wallet(new PlayerSave()), null,
                new StaminaService(new PlayerSave(), CombatTestData.Config(), () => DateTime.UtcNow), new LocalGameServer(),
                new CommandGate(_ => new System.Threading.Tasks.TaskCompletionSource<bool>().Task), _ => true);

            Assert.AreSame(progress, CombatTestData.Service(progress: progress).Progress);
            Assert.IsNotNull(CombatTestData.Service().Progress, "the helper supplies one when a test does not care");
        }

        private static CombatRig EnterRig(Action<UnityEditor.SerializedObject> configEdit = null, CharacterDefinition[] team = null) =>
            new CombatRig(new[] { Battle1Hp("b1"), CombatTestData.Story("story"), Battle1Hp("b2") }, team, configEdit);

        private static StageDefinition Battle1Hp(string id) => CombatTestData.Stage(id, new[] { CombatTestData.Enemy(hp: 1) });

        [Test]
        public void EnterStage_Success_SpendsStageCost_PersistsOnce_BuildsPaidSession()
        {
            var rig = new CombatRig(new[] { Battle1Hp("b1") });

            var result = rig.Service.EnterStageAsync("b1").Sync();

            Assert.AreEqual(EnterStageStatus.Entered, result.Status);
            Assert.AreSame(result.Session, rig.Service.Current);
            Assert.IsTrue(result.Session.EntryPaid);
            Assert.IsTrue(result.Saved);
            Assert.AreEqual(10, result.StaminaSpent);
            Assert.AreEqual(90, rig.Stamina.Current);
            Assert.AreEqual(1, rig.PersistCalls);
            Assert.AreEqual(90, rig.Snapshots[0].StaminaCurrent, "stamina was already spent when persist ran");
            Assert.IsFalse(rig.Snapshots[0].HasBattle, "no battle exists yet when persist runs");
            Assert.AreEqual("b1", result.Session.Stage.Id);
        }

        [Test]
        public void EnterStageAsync_Ok_TicketSet_StaminaFromServer()
        {
            var rig = new CombatRig(new[] { Battle1Hp("b1") });

            var result = rig.Service.EnterStageAsync("b1").Sync();

            Assert.AreEqual(EnterStageStatus.Entered, result.Status);
            Assert.IsNotEmpty(result.Session.Ticket);
            Assert.AreEqual(rig.Server.State.PendingTicket, result.Session.Ticket, "the ticket is the one the server holds");
            Assert.AreEqual(90, rig.Server.State.Stamina.Current, "the server charged the stamina");
            Assert.AreEqual(90, rig.Save.Stamina.Current, "the device shows what the server returned");
            Assert.AreEqual(1, rig.Server.CommandCalls);
        }

        [Test]
        public void EnterStageAsync_NotEnoughStamina_NoServerCall()
        {
            var rig = new CombatRig(new[] { Battle1Hp("b1") });
            rig.SetStamina(9);

            var result = rig.Service.EnterStageAsync("b1").Sync();

            Assert.AreEqual(EnterStageStatus.NotEnoughStamina, result.Status);
            Assert.AreEqual(0, rig.Server.CommandCalls);
            Assert.IsNull(rig.Service.Current);
        }

        [Test]
        public void EnterStageAsync_BattleInProgress_NoServerCall()
        {
            var rig = new CombatRig(new[] { CombatTestData.Stage("b1", new[] { CombatTestData.Enemy(hp: 100000) }) });
            rig.Service.EnterStageAsync("b1").Sync();
            var calls = rig.Server.CommandCalls;

            Assert.AreEqual(EnterStageStatus.BattleInProgress, rig.Service.EnterStageAsync("b1").Sync().Status);

            Assert.AreEqual(calls, rig.Server.CommandCalls);
        }

        [Test]
        public void EnterStageAsync_ServerRejectsNotOwned_MapsStatus_NoSession()
        {
            // Máy tưởng đã có nhân vật, server không: server là bản chính.
            var rig = new CombatRig(new[] { Battle1Hp("b1") });
            rig.Server.State.OwnedCharacterIds.Clear();
            var before = rig.State();

            var result = rig.Service.EnterStageAsync("b1").Sync();

            Assert.AreEqual(1, rig.Server.CommandCalls);
            Assert.AreEqual(EnterStageStatus.InvalidCharacter, result.Status);
            Assert.IsNull(result.Session);
            Assert.IsNull(rig.Service.Current);
            Assert.AreEqual(before, rig.State());
            Assert.AreEqual(0L, rig.Server.State.Stamina.AnchorUtcTicks, "nothing was charged: the bar is still full");
        }

        [Test]
        public void EnterStageAsync_Offline_NoSession_NoChange()
        {
            var rig = new CombatRig(new[] { Battle1Hp("b1") });
            var before = rig.State();
            rig.Server.FailNext(OnlineStatus.NetworkError);

            var result = rig.Service.EnterStageAsync("b1").Sync();

            Assert.AreEqual(EnterStageStatus.Offline, result.Status);
            Assert.IsNull(result.Session);
            Assert.IsNull(rig.Service.Current);
            Assert.AreEqual(before, rig.State());
            Assert.AreEqual(0, rig.PersistCalls);
        }

        // Mất trả lời EnterStage, bấm Thử lại: thể lực trừ một lần, vé là vé server giữ, server nhận hai lệnh cùng RequestId.
        [Test]
        public void EnterStage_LostReplyThenRetry_StaminaOnce_SameTicket()
        {
            var rig = new CombatRig(new[] { Battle1Hp("b1") });
            var lost = 0;
            rig.Gate.Lost += retry =>
            {
                lost++;
                retry();
            };
            rig.Server.LoseNextReply();

            var result = rig.Service.EnterStageAsync("b1").Sync();

            Assert.AreEqual(1, lost, "the player saw the Lost box once and pressed Retry");
            Assert.AreEqual(EnterStageStatus.Entered, result.Status);
            Assert.AreEqual(90, rig.Server.State.Stamina.Current, "charged once, not twice");
            Assert.AreEqual(90, rig.Save.Stamina.Current);
            Assert.AreEqual(rig.Server.State.PendingTicket, result.Session.Ticket, "the retry got the first ticket back");
            Assert.AreEqual(2, rig.Server.CommandCalls, "the lost send plus the retry");
        }

        [Test]
        public void EnterStage_Success_AddsExpEqualToStaminaCost()
        {
            var rig = new CombatRig(new[] { Battle1Hp("b1") });

            rig.Service.EnterStageAsync("b1").Sync();

            Assert.AreEqual((1, 10L), (rig.PlayerLevel.Level, rig.PlayerLevel.Exp));
            Assert.AreEqual(10, rig.Save.PlayerExp);
            Assert.AreEqual(1, rig.PersistCalls, "the EXP is saved by the same persist as the entry");
        }

        [Test]
        public void EnterStage_Rejected_AddsNoExp()
        {
            var lowStamina = new CombatRig(new[] { Battle1Hp("b1") });
            lowStamina.SetStamina(9);
            Assert.AreEqual(EnterStageStatus.NotEnoughStamina, lowStamina.Service.EnterStageAsync("b1").Sync().Status);
            Assert.AreEqual(0, lowStamina.Save.PlayerExp, "not enough stamina");

            var locked = EnterRig();
            Assert.AreEqual(EnterStageStatus.Locked, locked.Service.EnterStageAsync("b2").Sync().Status);
            Assert.AreEqual(0, locked.Save.PlayerExp, "locked stage");

            var noTeam = new CombatRig(new[] { Battle1Hp("b1") }, new CharacterDefinition[0]);
            Assert.AreEqual(EnterStageStatus.EmptyTeam, noTeam.Service.EnterStageAsync("b1").Sync().Status);
            Assert.AreEqual(0, noTeam.Save.PlayerExp, "empty team");
        }

        [Test]
        public void EnterStage_Offline_AddsNoExp()
        {
            var rig = new CombatRig(new[] { Battle1Hp("b1") });
            rig.Server.FailNext(OnlineStatus.NetworkError);

            Assert.AreEqual(EnterStageStatus.Offline, rig.Service.EnterStageAsync("b1").Sync().Status);

            Assert.AreEqual(0, rig.Save.PlayerExp);
        }

        [Test]
        public void EnterStage_LostReplyRetrySameRequest_AddsExpOnce()
        {
            var rig = new CombatRig(new[] { Battle1Hp("b1") });
            rig.Gate.Lost += retry => retry();
            rig.Server.LoseNextReply();

            Assert.AreEqual(EnterStageStatus.Entered, rig.Service.EnterStageAsync("b1").Sync().Status);

            Assert.AreEqual(10, rig.Save.PlayerExp, "10 stamina = 10 EXP once, not twice");
        }

        [Test]
        public void StartBattle_Dev_AddsNoExp()
        {
            var rig = new CombatRig(new[] { Battle1Hp("b1") });

            Assert.AreEqual(StartBattleStatus.Success, rig.Service.StartBattle("b1").Status);

            Assert.AreEqual(0, rig.Save.PlayerExp);
        }

        [Test]
        public void BattleAllyStats_IncludeStarAndLevel()
        {
            var rig = EnterRig();
            rig.StarUp("a", 1);
            // Người chơi Lv5 (tổng EXP lên tới cấp 5) và nhân vật a đã lưu Lv5.
            long exp = 0;
            for (var l = 1; l < 5; l++) exp += rig.LevelConfig.ExpToNext(l);
            rig.Save.PlayerExp = exp;
            rig.Save.CharacterLevels.Add(new CharacterLevelEntry { CharacterId = "a", Level = 5 });

            var ally = rig.Service.EnterStageAsync("b1").Sync().Session.Allies[0];

            // Hero mặc định HP 1000, Atk 100: +10% sao +8% cấp = 118%.
            Assert.AreEqual((1180, 118), (ally.MaxHp, ally.Attack));
        }

        [Test]
        public void EnterStage_StarredCharacter_HasScaledStats()
        {
            var rig = EnterRig();
            rig.StarUp("a", 1);

            var ally = rig.Service.EnterStageAsync("b1").Sync().Session.Allies[0];
            Assert.AreEqual((1100, 1100, 110, 0, 100), (ally.MaxHp, ally.Hp, ally.Attack, ally.Defense, ally.Speed));

            // Dải DEV (StartBattle) là chỗ dựng trận thứ hai, cũng phải dùng chỉ số theo sao.
            var dev = rig.Service.StartBattle("b1").Session.Allies[0];
            Assert.AreEqual((1100, 110), (dev.MaxHp, dev.Attack));
        }

        [Test]
        public void StarUp_DuringBattle_DoesNotChangeCurrentUnits()
        {
            var rig = new CombatRig(new[] { Battle1Hp("b1") });
            var first = rig.Service.EnterStageAsync("b1").Sync().Session;
            Assert.AreEqual(1000, first.Allies[0].MaxHp);

            // Nâng sao do server làm rồi áp vào save (StarServiceTests); ở đây chỉ cần save đổi giữa trận.
            rig.StarUp("a", 1);

            Assert.AreSame(first, rig.Service.Current);
            Assert.AreEqual(1000, first.Allies[0].MaxHp);
            Assert.AreEqual(1000, first.Allies[0].Hp);

            first.Surrender();
            var next = rig.Service.EnterStageAsync("b1").Sync().Session;
            Assert.AreEqual(1100, next.Allies[0].MaxHp);
        }

        [Test]
        public void EnterStage_EachStageRejection_SpendsNothing_PersistsNothing_KeepsCurrent()
        {
            var bad = Battle1Hp("bad");
            var rig = new CombatRig(new[] { Battle1Hp("b1"), CombatTestData.Story("story"), bad, Battle1Hp("b2") });
            var trial = rig.Service.StartBattle("b1").Session;
            CombatTestData.PlayToWin(trial);
            var before = rig.State();

            void Check(string name, string id, EnterStageStatus expected)
            {
                var result = rig.Service.EnterStageAsync(id).Sync();
                Assert.AreEqual(expected, result.Status, name);
                Assert.IsNull(result.Session, name);
                Assert.IsFalse(result.Saved, name);
                Assert.AreEqual(before, rig.State(), name);
                Assert.AreEqual(0, rig.PersistCalls, name);
                Assert.AreSame(trial, rig.Service.Current, name);
            }

            Check("unknown id", "nope", EnterStageStatus.Unknown);
            rig.Clear("b1");
            before = rig.State();
            Check("story stage", "story", EnterStageStatus.WrongKind);
            rig.Clear("story");
            // Màn hỏng sau khi dịch vụ đã dựng (config hỏng ngay từ đầu thì IsRunning = false và EnterStage dừng ở InvalidConfig).
            TestData.Edit(bad, so => so.FindProperty("_staminaCost").intValue = 0);
            before = rig.State();
            Check("broken stage", "bad", EnterStageStatus.Invalid);
            rig.Save.ClearedStages.Clear();
            rig.Server.State.ClearedStages.Clear();
            before = rig.State();
            Check("locked stage", "b2", EnterStageStatus.Locked);

            rig.Clear("b1", "story", "bad");
            rig.SetStamina(9);
            before = rig.State();
            Check("stamina 9", "b2", EnterStageStatus.NotEnoughStamina);

            foreach (var hero in rig.Roster.GetBattleTeam().ToArray()) rig.Roster.RemoveFromTeam(hero.Id);
            Check("empty team (stamina is also short, team is checked first)", "b2", EnterStageStatus.EmptyTeam);
        }

        [Test]
        public void EnterStage_InvalidCharacterInTeam_WithEnoughStamina_SpendsNothing()
        {
            // Qua được Validate() nhưng skill hồi chiêu 0 vi phạm CombatSkillRules (như Task 6).
            var rig = EnterRig(team: new[] { Hero("good"), TestData.Character("plain") });
            var before = rig.State();

            var result = rig.Service.EnterStageAsync("b1").Sync();

            Assert.AreEqual(EnterStageStatus.InvalidCharacter, result.Status);
            Assert.IsNull(result.Session);
            Assert.AreEqual(before, rig.State());
            Assert.AreEqual(100, rig.Stamina.Current);
            Assert.AreEqual(0, rig.PersistCalls);
            Assert.IsNull(rig.Service.Current);
        }

        [Test]
        public void EnterStage_InvalidConfig_SpendsNothing()
        {
            LogAssert.Expect(LogType.Error, new Regex("invalid config"));
            var rig = EnterRig(so => so.FindProperty("_maxEnergy").intValue = 0);
            var before = rig.State();

            var result = rig.Service.EnterStageAsync("b1").Sync();

            Assert.IsFalse(rig.Service.IsRunning);
            Assert.AreEqual(EnterStageStatus.InvalidConfig, result.Status);
            Assert.AreEqual(before, rig.State());
            Assert.AreEqual(0, rig.PersistCalls);
            Assert.IsNull(rig.Service.Current);
        }

        [Test]
        public void EnterStage_BrokenStaminaConfig_InvalidConfig_NoThrow()
        {
            foreach (var field in new[] { "_staminaRegenSeconds", "_maxStamina" })
            {
                LogAssert.Expect(LogType.Error, new Regex("invalid config"));
                CombatRig rig = null;
                Assert.DoesNotThrow(() => rig = EnterRig(so => so.FindProperty(field).intValue = 0), $"{field} = 0");

                Assert.IsFalse(rig.Service.IsRunning, $"{field} = 0");
                Assert.AreEqual(EnterStageStatus.InvalidConfig, rig.Service.EnterStageAsync("b1").Sync().Status, $"{field} = 0");
                Assert.AreEqual(0, rig.PersistCalls, $"{field} = 0");
            }
        }

        [Test]
        public void EnterStage_WhileBattleInProgress_Rejected_NoSecondCharge()
        {
            var rig = new CombatRig(new[] { CombatTestData.Stage("b1", new[] { CombatTestData.Enemy(hp: 100000) }) });
            var first = rig.Service.EnterStageAsync("b1").Sync().Session;
            Assert.AreEqual(BattleState.PlayerTurn, first.State);

            var second = rig.Service.EnterStageAsync("b1").Sync();
            Assert.AreEqual(EnterStageStatus.BattleInProgress, second.Status, "PlayerTurn");
            Assert.IsNull(second.Session);

            first.PlayerAct(BattleActionKind.Basic, first.DefaultTarget(BattleActionKind.Basic));
            Assert.AreEqual(BattleState.EnemyTurn, first.State);
            Assert.AreEqual(EnterStageStatus.BattleInProgress, rig.Service.EnterStageAsync("b1").Sync().Status, "EnemyTurn");

            Assert.AreEqual(90, rig.Stamina.Current);
            Assert.AreEqual(1, rig.PersistCalls);
            Assert.AreSame(first, rig.Service.Current);
        }

        [Test]
        public void EnterStage_AfterLostBattle_ChargesAgain_NewSession()
        {
            var rig = new CombatRig(new[] { Battle1Hp("b1") });
            var first = rig.Service.EnterStageAsync("b1").Sync().Session;
            first.Surrender();

            var again = rig.Service.EnterStageAsync("b1").Sync();

            Assert.AreEqual(EnterStageStatus.Entered, again.Status);
            Assert.AreNotSame(first, again.Session);
            Assert.AreEqual(80, rig.Stamina.Current);
            Assert.AreEqual(2, rig.PersistCalls);

            CombatTestData.PlayToWin(again.Session);
            rig.Service.EndBattle();
            var third = rig.Service.EnterStageAsync("b1").Sync();

            Assert.AreEqual(EnterStageStatus.Entered, third.Status, "after Won and EndBattle");
            Assert.AreEqual(70, rig.Stamina.Current);
            Assert.AreEqual(3, rig.PersistCalls);
        }

        [Test]
        public void EnterStage_SpendsTheStagesOwnCost()
        {
            var stage = Battle1Hp("big");
            TestData.Edit(stage, so => so.FindProperty("_staminaCost").intValue = 25);
            var rig = new CombatRig(new[] { stage });

            var result = rig.Service.EnterStageAsync("big").Sync();

            Assert.AreEqual(EnterStageStatus.Entered, result.Status);
            Assert.AreEqual(25, result.StaminaSpent);
            Assert.AreEqual(75, rig.Stamina.Current);
        }

        [Test]
        public void StartBattle_Dev_NeverSpendsNorPersists_SessionNotPaid()
        {
            var rig = EnterRig();

            var open = rig.Service.StartBattle("b1");
            Assert.AreEqual(StartBattleStatus.Success, open.Status);
            Assert.IsFalse(open.Session.EntryPaid);
            Assert.AreEqual(100, rig.Stamina.Current);

            rig.SetStamina(0);
            var broke = rig.Service.StartBattle("b2");
            Assert.AreEqual(StartBattleStatus.Success, broke.Status, "a locked stage with no stamina still opens from the DEV strip");
            Assert.IsFalse(broke.Session.EntryPaid);

            Assert.AreEqual(0, rig.PersistCalls);
            Assert.AreEqual(0, rig.Stamina.Current, "untouched at the value the test set");
            Assert.AreEqual(0, rig.Snapshots.Count);
        }
    }
}
