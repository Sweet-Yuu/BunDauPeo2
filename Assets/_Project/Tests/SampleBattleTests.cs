using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.EditorTools;
using NUnit.Framework;
using UnityEditor;

namespace BunDauPeo.Tests
{
    // Chơi bằng asset thật: phe ta theo AutoPolicy, quái theo EnemyAct. Chạy 20 seed.
    public class SampleBattleTests
    {
        private const int Seeds = 20;
        private const int ActionCap = 10000;

        private static readonly string[] StarterIds = { "char_nem_chua", "char_cha_com", "char_dau_phu" };
        private static readonly string[] FiveClassIds = { "char_long_doi", "char_bun_la", "char_dau_phu", "char_cha_com", "char_mam_tom" };

        private static CombatConfig Config() => AssetDatabase.LoadAssetAtPath<CombatConfig>(ProjectSetup.CombatConfigPath);

        private static CharacterDefinition[] Team(params string[] ids)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(ProjectSetup.CatalogPath);
            return ids.Select(id =>
            {
                Assert.IsTrue(catalog.TryGet(id, out var c), id);
                return c;
            }).ToArray();
        }

        private static BattleSession Auto(CharacterDefinition[] team, StageDefinition stage, int seed)
        {
            var s = new BattleSession(team, stage, Config(), new System.Random(seed));
            for (var i = 0; i < ActionCap && s.State != BattleState.Won && s.State != BattleState.Lost; i++)
            {
                if (s.State == BattleState.PlayerTurn)
                {
                    var choice = AutoPolicy.Choose(s);
                    Assert.AreEqual(BattleActionStatus.Success, s.PlayerAct(choice.Kind, choice.Target).Status, $"{stage.Id} seed {seed}");
                }
                else
                {
                    s.EnemyAct();
                }
            }

            return s;
        }

        private static StageDefinition Stage(string id) => Config().Stages.Items.Single(st => st.Id == id);

        [Test]
        public void SampleStages_AutoBattle_EndWithinMaxRounds()
        {
            var config = Config();
            var teams = new[] { Team(StarterIds), Team(FiveClassIds) };
            var battles = config.Stages.Items.Where(st => st.Kind == StageKind.Battle).ToList();
            Assert.AreEqual(5, battles.Count);

            foreach (var stage in battles)
            foreach (var team in teams)
            for (var seed = 0; seed < Seeds; seed++)
            {
                var s = Auto(team, stage, seed);
                var label = $"{stage.Id} team {team.Length} seed {seed}";
                Assert.IsTrue(s.State == BattleState.Won || s.State == BattleState.Lost, $"{label}: still {s.State} after {ActionCap} actions");
                Assert.LessOrEqual(s.Round, config.MaxRounds, $"{label}: ended by the round limit");
            }
        }

        [Test]
        public void Starters_AutoWinStagesOneToThree()
        {
            var team = Team(StarterIds);
            foreach (var id in new[] { "st_1_1", "st_1_2", "st_1_3" })
            for (var seed = 0; seed < Seeds; seed++)
                Assert.AreEqual(BattleState.Won, Auto(team, Stage(id), seed).State, $"{id} seed {seed}");
        }

        [Test]
        public void FiveClassTeam_AutoBeatsBoss()
        {
            var team = Team(FiveClassIds);
            for (var seed = 0; seed < Seeds; seed++)
                Assert.AreEqual(BattleState.Won, Auto(team, Stage("st_1_5"), seed).State, $"st_1_5 seed {seed}");
        }
    }
}
