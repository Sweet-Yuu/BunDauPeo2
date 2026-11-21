using System.IO;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.EditorTools;
using BunDauPeo.Gacha;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class GameDataBuilderTests
    {
        private static CurrencyAmount[] Lines(params (CurrencyType type, long amount)[] lines)
        {
            var result = new CurrencyAmount[lines.Length];
            for (var i = 0; i < lines.Length; i++) result[i] = new CurrencyAmount(lines[i].type, lines[i].amount);
            return result;
        }

        private static StageDefinition Stage(string id, StageKind kind, int stamina, CurrencyAmount[] first, CurrencyAmount[] repeat)
        {
            var stage = ScriptableObject.CreateInstance<StageDefinition>();
            TestData.Edit(stage, so =>
            {
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_kind").enumValueIndex = (int)kind;
                so.FindProperty("_staminaCost").intValue = stamina;
                Fill(so.FindProperty("_firstClearReward"), first);
                Fill(so.FindProperty("_repeatReward"), repeat);
            });
            return stage;
        }

        private static void Fill(UnityEditor.SerializedProperty array, CurrencyAmount[] lines)
        {
            array.arraySize = lines.Length;
            for (var i = 0; i < lines.Length; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Type").enumValueIndex = (int)lines[i].Type;
                element.FindPropertyRelative("Amount").longValue = lines[i].Amount;
            }
        }

        private static CombatConfig MakeCombat(params StageDefinition[] stages)
        {
            var catalog = ScriptableObject.CreateInstance<StageCatalog>();
            TestData.Edit(catalog, so =>
            {
                var array = so.FindProperty("_items");
                array.arraySize = stages.Length;
                for (var i = 0; i < stages.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
            });
            var config = ScriptableObject.CreateInstance<CombatConfig>();
            TestData.SetRef(config, "_stages", catalog);
            return config;
        }

        private class Rig
        {
            public CharacterCatalog Characters;
            public GachaBannerCatalog Banners;
            public StarConfig Stars;
            public CombatConfig Combat;
            public NewGameConfig NewGame;
        }

        private static Rig MakeRig(CurrencyAmount[] singleCost = null, CurrencyAmount[] firstReward = null)
        {
            var chars = TestData.GachaCharacters();
            var banner = TestData.Banner("b1", chars);
            if (singleCost != null) TestData.Edit(banner, so => Fill(so.FindProperty("_singleCost"), singleCost));
            return new Rig
            {
                Characters = TestData.Catalog(chars),
                Banners = TestData.GachaCatalog(banner),
                Stars = ScriptableObject.CreateInstance<StarConfig>(),
                Combat = MakeCombat(
                    Stage("s1", StageKind.Battle, 10, firstReward ?? Lines((CurrencyType.Gem, 50), (CurrencyType.Material, 3)), Lines((CurrencyType.Gem, 5))),
                    Stage("s2", StageKind.Story, 0, Lines(), Lines())),
                NewGame = TestData.Config(Lines((CurrencyType.Gem, 1000), (CurrencyType.Gem, 500), (CurrencyType.Coin, 7)), chars[0], chars[3]),
            };
        }

        [Test]
        public void Build_CopiesBalanceData()
        {
            var r = MakeRig();
            var data = GameDataBuilder.Build(r.Characters, r.Banners, r.Stars, r.Combat, r.NewGame);

            Assert.AreEqual(6, data.Characters.Count);
            Assert.AreEqual("x0", data.Characters[5].Id);
            Assert.AreEqual(2, data.Characters[5].Rarity);
            Assert.AreEqual(1, data.Characters[3].Rarity);
            Assert.AreEqual(0, data.Characters[0].Rarity);
            Assert.AreEqual(1, data.Characters[0].BaseStars);

            var b = data.Banners[0];
            Assert.AreEqual("b1", b.Id);
            Assert.AreEqual(100, b.SingleCostGem);
            Assert.AreEqual(900, b.TenCostGem);
            Assert.AreEqual(70f, b.WeightR);
            Assert.AreEqual(25f, b.WeightSR);
            Assert.AreEqual(5f, b.WeightSSR);
            Assert.AreEqual(50, b.SsrPityThreshold);
            Assert.IsTrue(b.GuaranteeSrInTen);
            Assert.AreEqual(1, b.DupFragmentsSSR);
            CollectionAssert.AreEqual(new[] { "r0", "r1", "r2", "s0", "s1", "x0" }, b.CharacterIds);

            CollectionAssert.AreEqual(new[] { 2, 4, 6, 8 }, data.StarCosts);
            Assert.AreEqual(5, data.MaxStars);
            Assert.AreEqual(5, data.MaxTeamSize);
            Assert.AreEqual(100, data.Stamina.Max);
            Assert.AreEqual(300, data.Stamina.RegenSeconds);

            Assert.AreEqual(2, data.Stages.Count);
            Assert.AreEqual("s1", data.Stages[0].Id);
            Assert.AreEqual((int)StageKind.Battle, data.Stages[0].Kind);
            Assert.AreEqual(10, data.Stages[0].StaminaCost);
            Assert.AreEqual(50, data.Stages[0].FirstClear.Gem);
            Assert.AreEqual(3, data.Stages[0].FirstClear.Material);
            Assert.AreEqual(5, data.Stages[0].Repeat.Gem);
            Assert.AreEqual(0, data.Stages[0].Repeat.Material);
            Assert.AreEqual("s2", data.Stages[1].Id);
            Assert.AreEqual((int)StageKind.Story, data.Stages[1].Kind);
            Assert.AreEqual(0, data.Stages[1].FirstClear.Gem);

            // Chỉ cộng dòng Gem (không lẫn Coin).
            Assert.AreEqual(1500, data.NewGame.StartingGem);
            CollectionAssert.AreEqual(new[] { "r0", "s0" }, data.NewGame.StarterCharacterIds);
        }

        [Test]
        public void Validate_RejectsNonGemCost_AndCoinReward()
        {
            var ok = MakeRig();
            Assert.IsEmpty(GameDataBuilder.Validate(ok.Characters, ok.Banners, ok.Stars, ok.Combat, ok.NewGame));

            var badBanner = MakeRig(singleCost: Lines((CurrencyType.Coin, 100)));
            var errors = GameDataBuilder.Validate(badBanner.Characters, badBanner.Banners, badBanner.Stars, badBanner.Combat, badBanner.NewGame);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("b1", errors[0]);

            var badStage = MakeRig(firstReward: Lines((CurrencyType.Coin, 10)));
            errors = GameDataBuilder.Validate(badStage.Characters, badStage.Banners, badStage.Stars, badStage.Combat, badStage.NewGame);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("s1", errors[0]);
        }

        [Test]
        public void CommittedJson_MatchesProjectData()
        {
            var data = ServerDataExport.BuildFromProject(out var errors);
            Assert.IsEmpty(errors, "dữ liệu project không hợp lệ cho server");
            var committed = File.ReadAllText(ServerDataExport.FullOutputPath);
            Assert.AreEqual(GameDataBuilder.ToJson(data), committed,
                "GameData.json lệch dữ liệu project: chạy Tools/BunDauPeo/Export Server Data rồi commit file.");
        }

        [Test]
        public void CommittedJson_NoBomNoTrailingNewline()
        {
            var bytes = File.ReadAllBytes(ServerDataExport.FullOutputPath);
            Assert.Greater(bytes.Length, 3);
            Assert.IsFalse(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "GameData.json có BOM");
            Assert.AreEqual((byte)'}', bytes[bytes.Length - 1], "GameData.json phải kết thúc bằng '}', không xuống dòng cuối");
        }
    }
}
