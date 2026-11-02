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
    }
}
