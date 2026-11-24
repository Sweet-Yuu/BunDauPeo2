using System.IO;
using System.Linq;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.EditorTools;
using BunDauPeo.Gacha;
using BunDauPeo.Missions;
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
        public void Build_MapsExpPotionLine()
        {
            var r = MakeRig(firstReward: Lines((CurrencyType.Gem, 50), (CurrencyType.ExpPotion, 10)));
            var data = GameDataBuilder.Build(r.Characters, r.Banners, r.Stars, r.Combat, r.NewGame);
            Assert.AreEqual(10, data.Stages[0].FirstClear.ExpPotion);
            Assert.AreEqual(0, data.Stages[0].Repeat.ExpPotion);
        }

        [Test]
        public void Validate_AllowsExpPotionInStageReward()
        {
            var r = MakeRig(firstReward: Lines((CurrencyType.ExpPotion, 10)));
            Assert.IsEmpty(GameDataBuilder.Validate(r.Characters, r.Banners, r.Stars, r.Combat, r.NewGame));
        }

        [Test]
        public void Validate_StillRejectsCoinInStageReward()
        {
            var r = MakeRig(firstReward: Lines((CurrencyType.ExpPotion, 10), (CurrencyType.Coin, 10)));
            Assert.IsNotEmpty(GameDataBuilder.Validate(r.Characters, r.Banners, r.Stars, r.Combat, r.NewGame));
        }

        private static MissionDefinition Mission(string id, MissionTab tab = MissionTab.Daily, MissionCounter counter = MissionCounter.Wins,
            int target = 1, bool locked = false, long gem = 10, int stamina = 0, int from = 0, int to = 0)
        {
            var m = ScriptableObject.CreateInstance<MissionDefinition>();
            TestData.Edit(m, so =>
            {
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_tab").enumValueIndex = (int)tab;
                so.FindProperty("_counter").enumValueIndex = (int)counter;
                so.FindProperty("_target").intValue = target;
                so.FindProperty("_locked").boolValue = locked;
                so.FindProperty("_rewardGem").longValue = gem;
                so.FindProperty("_rewardStamina").intValue = stamina;
                so.FindProperty("_windowStartHour").intValue = from;
                so.FindProperty("_windowEndHour").intValue = to;
            });
            return m;
        }

        private static MissionCatalog MissionCat(params MissionDefinition[] missions)
        {
            var catalog = ScriptableObject.CreateInstance<MissionCatalog>();
            TestData.Edit(catalog, so =>
            {
                var array = so.FindProperty("_items");
                array.arraySize = missions.Length;
                for (var i = 0; i < missions.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = missions[i];
            });
            return catalog;
        }

        private static System.Collections.Generic.List<string> Validate(Rig r, MissionCatalog missions) =>
            GameDataBuilder.Validate(r.Characters, r.Banners, r.Stars, r.Combat, r.NewGame, missions);

        [Test]
        public void Build_IncludesMissions_InSpecOrder()
        {
            var r = MakeRig();
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<MissionCatalog>(ProjectSetup.MissionCatalogPath);
            Assert.IsNotNull(catalog, "chạy ProjectSetup.CreateMissionData để tạo MissionCatalog");

            var data = GameDataBuilder.Build(r.Characters, r.Banners, r.Stars, r.Combat, r.NewGame, catalog);

            CollectionAssert.AreEqual(new[]
            {
                "daily_win_5", "daily_pull_1", "daily_level_1", "daily_login_am", "daily_login_pm",
                "new_link", "new_name", "new_staff", "new_first_win", "new_first_pull", "new_upgrade", "new_afk", "new_star_up", "new_win_10",
            }, data.Missions.ConvertAll(m => m.Id));
            Assert.IsTrue(data.Missions.Find(m => m.Id == "daily_level_1").Locked);
            Assert.AreEqual(1, data.Missions.Find(m => m.Id == "new_link").Tab);
            Assert.AreEqual(5, data.Missions.FindIndex(m => m.Id == "new_link"), "new_link opens the newbie tab");
            var pm = data.Missions.Find(m => m.Id == "daily_login_pm");
            Assert.AreEqual((12, 24, 100), (pm.WindowStartHour, pm.WindowEndHour, pm.RewardStamina));
            Assert.AreEqual(750, data.Missions.FindAll(m => m.Tab == 1).ConvertAll(m => m.RewardGem).Sum(), "newbie total is 750 Gem");
            Assert.AreEqual(40, data.Missions.FindAll(m => m.Tab == 0).ConvertAll(m => m.RewardGem).Sum(), "daily Gem per day");
            Assert.IsEmpty(GameDataBuilder.Validate(r.Characters, r.Banners, r.Stars, r.Combat, r.NewGame, catalog));
        }

        [Test]
        public void Build_BannerFreePull_FlagCopied()
        {
            var r = MakeRig();
            Assert.IsFalse(GameDataBuilder.Build(r.Characters, r.Banners, r.Stars, r.Combat, r.NewGame).Banners[0].FreePullDaily);

            TestData.Edit(r.Banners.Items[0], so => so.FindProperty("_freePullDaily").boolValue = true);
            Assert.IsTrue(GameDataBuilder.Build(r.Characters, r.Banners, r.Stars, r.Combat, r.NewGame).Banners[0].FreePullDaily);

            var project = UnityEditor.AssetDatabase.LoadAssetAtPath<GachaBannerDefinition>(ProjectSetup.BannerPath);
            Assert.IsTrue(project.FreePullDaily, "the current banner is set to a daily free pull by CreateMissionData");
        }

        [Test]
        public void Build_StaminaRewardCap_999()
        {
            var r = MakeRig();

            var data = GameDataBuilder.Build(r.Characters, r.Banners, r.Stars, r.Combat, r.NewGame);

            Assert.AreEqual(999, data.Stamina.RewardCap);
            Assert.AreEqual(100, data.Stamina.Max);
        }

        [Test]
        public void Validate_Mission_BadRewardOrTarget()
        {
            var r = MakeRig();
            Assert.IsEmpty(Validate(r, MissionCat(Mission("a"), Mission("lk", locked: true, target: 0, gem: 0))));

            var negGem = Validate(r, MissionCat(Mission("neg", gem: -1)));
            Assert.AreEqual(1, negGem.Count);
            StringAssert.Contains("neg", negGem[0]);
            Assert.AreEqual(1, Validate(r, MissionCat(Mission("negs", stamina: -5))).Count);
            var zeroTarget = Validate(r, MissionCat(Mission("zero", target: 0)));
            Assert.AreEqual(1, zeroTarget.Count);
            StringAssert.Contains("Target", zeroTarget[0]);
        }

        [Test]
        public void Validate_Mission_DuplicateId()
        {
            var errors = Validate(MakeRig(), MissionCat(Mission("same"), Mission("same")));

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("duplicate", errors[0]);
        }

        [Test]
        public void Validate_Mission_BadWindow()
        {
            var r = MakeRig();
            Assert.IsEmpty(Validate(r, MissionCat(Mission("w", from: 12, to: 24))));
            Assert.AreEqual(1, Validate(r, MissionCat(Mission("rev", from: 12, to: 6))).Count, "reversed");
            Assert.AreEqual(1, Validate(r, MissionCat(Mission("hi", from: 0, to: 25))).Count, "past 24");
            Assert.AreEqual(1, Validate(r, MissionCat(Mission("lo", from: -1, to: 5))).Count, "before 0");
        }

        [Test]
        public void Validate_RewardCap_BelowMax_Rejected()
        {
            var r = MakeRig();
            TestData.Edit(r.Combat, so => so.FindProperty("_rewardStaminaCap").intValue = 50);

            var errors = Validate(r, MissionCat(Mission("login", stamina: 100, from: 0, to: 12)));

            Assert.AreEqual(1, errors.Count, "cap 50 is below Max 100");
            StringAssert.Contains("RewardStaminaCap", errors[0]);

            TestData.Edit(r.Combat, so => so.FindProperty("_rewardStaminaCap").intValue = 0);
            Assert.AreEqual(2, Validate(r, MissionCat(Mission("login", stamina: 100, from: 0, to: 12))).Count, "cap 0 also fails the stamina mission");
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
