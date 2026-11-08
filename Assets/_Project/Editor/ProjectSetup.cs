using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using BunDauPeo.Restaurant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BunDauPeo.EditorTools
{
    // Chạy lại được: asset đã có thì giữ nguyên, để không ghi đè số bạn đã chỉnh trong Inspector.
    public static class ProjectSetup
    {
        public const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";
        public const string CatalogPath = "Assets/_Project/Characters/Data/CharacterCatalog.asset";
        public const string NewGameConfigPath = "Assets/_Project/Boot/Data/NewGameConfig.asset";
        public const string GachaScenePath = "Assets/_Project/Scenes/Gacha.unity";
        public const string CharactersScenePath = "Assets/_Project/Scenes/Characters.unity";
        public const string RestaurantScenePath = "Assets/_Project/Scenes/Restaurant.unity";
        public const string BattleScenePath = "Assets/_Project/Scenes/Battle.unity";
        public const string StageSelectScenePath = "Assets/_Project/Scenes/StageSelect.unity";
        public const string BannerPath ="Assets/_Project/Gacha/Data/banner_standard.asset";
        public const string BannerCatalogPath = "Assets/_Project/Gacha/Data/GachaBannerCatalog.asset";
        public const string RestaurantConfigPath = "Assets/_Project/Restaurant/Data/RestaurantConfig.asset";
        public const string UpgradeCatalogPath = "Assets/_Project/Restaurant/Data/UpgradeCatalog.asset";
        public const string CombatConfigPath = "Assets/_Project/Combat/Data/CombatConfig.asset";
        public const string StageCatalogPath = "Assets/_Project/Combat/Data/StageCatalog.asset";
        public const string StarConfigPath = "Assets/_Project/Characters/Data/StarConfig.asset";

        private const string CharacterData = "Assets/_Project/Characters/Data";
        private const string SkillsFolder = CharacterData + "/Skills";
        private const string CharactersFolder = CharacterData + "/Characters";
        private const string PortraitSprite = "UI/Skin/UISprite.psd";
        private const string IconSprite = "UI/Skin/Knob.psd";

        private static readonly (string File, string Display, SkillKind Kind, SkillTarget Target, SkillEffect Effect, float Power, int Cooldown, int Energy)[] SkillTable =
        {
            ("BasicStrike", "Basic Strike", SkillKind.Basic, SkillTarget.SingleEnemy, SkillEffect.Damage, 1.0f, 0, 0),
            ("PowerStrike", "Power Strike", SkillKind.Skill, SkillTarget.SingleEnemy, SkillEffect.Damage, 1.8f, 3, 0),
            ("Heal", "Heal", SkillKind.Skill, SkillTarget.SingleAlly, SkillEffect.Heal, 1.2f, 4, 0),
            ("Finisher", "Finisher", SkillKind.Ultimate, SkillTarget.AllEnemies, SkillEffect.Damage, 2.5f, 0, 100),
        };

        private static readonly (string Id, string Name, Rarity Rarity, CharacterClass Class, int Hp, int Atk, int Def, int Spd, int Cook, int Serve, int Cashier, string Skill)[] CharacterTable =
        {
            ("char_nem_chua", "Nem Chua", Rarity.R, CharacterClass.Fighter, 800, 80, 40, 100, 30, 20, 10, "PowerStrike"),
            ("char_cha_com", "Chả Cốm", Rarity.R, CharacterClass.Marksman, 900, 70, 50, 95, 20, 30, 10, "PowerStrike"),
            ("char_dau_phu", "Đậu Phụ", Rarity.R, CharacterClass.Support, 1000, 60, 60, 90, 10, 20, 30, "Heal"),
            ("char_mam_tom", "Mắm Tôm", Rarity.SR, CharacterClass.Mage, 1000, 110, 50, 110, 50, 30, 20, "PowerStrike"),
            ("char_long_doi", "Lòng Dồi", Rarity.SR, CharacterClass.Tank, 1200, 90, 70, 100, 30, 50, 20, "Heal"),
            ("char_bun_la", "Bún Lá", Rarity.SSR, CharacterClass.Fighter, 1400, 130, 80, 120, 60, 50, 50, "PowerStrike"),
        };

        private const string RestaurantData = "Assets/_Project/Restaurant/Data";

        // Số mẫu của spec Restaurant 3.2 (món, khách, sự cố) và 3.4 (nâng cấp).
        private static readonly (string File, string Name, long Price, float CookSeconds)[] DishTable =
        {
            ("Dish_BunDauMamTom", "Bún đậu mắm tôm", 20, 6f),
            ("Dish_ChaCom", "Chả cốm", 15, 4f),
            ("Dish_NemChuaRan", "Nem chua rán", 25, 5f),
        };

        private static readonly (string File, string Name, float Weight, float Patience, float Pay)[] CustomerTable =
        {
            ("Customer_Normal", "Khách thường", 6f, 40f, 1.0f),
            ("Customer_Gourmet", "Khách sành ăn", 3f, 30f, 1.5f),
            ("Customer_Hurry", "Khách vội", 1f, 20f, 1.2f),
        };

        private static readonly (string File, string Name, StaffRole Role)[] IncidentTable =
        {
            ("Incident_GasBlocked", "Bếp tắc ga", StaffRole.Cook),
            ("Incident_SpilledFood", "Khách làm đổ đồ", StaffRole.Server),
            ("Incident_RegisterFrozen", "Máy tính tiền treo", StaffRole.Cashier),
        };

        private static readonly (string Id, string Name, UpgradeEffect Effect, float[] Values, long[] Coins, long[] Materials)[] UpgradeTable =
        {
            ("up_tables", "Bàn", UpgradeEffect.Tables, new[] { 4f, 5f, 6f, 7f, 8f }, new[] { 300L, 600, 1200, 2400, 4800 }, new[] { 5L, 10, 15, 20, 25 }),
            ("up_cook_slots", "Ô đầu bếp", UpgradeEffect.CookSlots, new[] { 2f, 3f }, new[] { 500L, 1500 }, new[] { 10L, 30 }),
            ("up_serve_slots", "Ô phục vụ", UpgradeEffect.ServeSlots, new[] { 2f, 3f }, new[] { 500L, 1500 }, new[] { 10L, 30 }),
            ("up_cashier_slots", "Ô thu ngân", UpgradeEffect.CashierSlots, new[] { 2f, 3f }, new[] { 500L, 1500 }, new[] { 10L, 30 }),
            ("up_arrivals", "Biển hiệu", UpgradeEffect.ArrivalInterval, new[] { 0.9f, 0.81f, 0.73f, 0.66f, 0.59f }, new[] { 300L, 600, 1200, 2400, 4800 }, new[] { 5L, 10, 15, 20, 25 }),
            ("up_prices", "Thực đơn", UpgradeEffect.DishPrice, new[] { 1.1f, 1.2f, 1.3f, 1.4f, 1.5f }, new[] { 400L, 800, 1600, 3200, 6400 }, new[] { 5L, 10, 15, 20, 25 }),
        };

        private const string CombatData = "Assets/_Project/Combat/Data";

        // Số mẫu của spec Combat 3.5. Đánh thường của quái dùng lại Skill_BasicStrike của nhân vật.
        private static readonly (string File, string Display, SkillKind Kind, SkillTarget Target, SkillEffect Effect, float Power, int Cooldown, int Energy)[] EnemySkillTable =
        {
            ("Claw", "Cào mạnh", SkillKind.Skill, SkillTarget.SingleEnemy, SkillEffect.Damage, 1.8f, 3, 0),
            ("Roar", "Gầm", SkillKind.Skill, SkillTarget.AllEnemies, SkillEffect.Damage, 1.0f, 3, 0),
        };

        private static readonly (string File, string Name, int Hp, int Atk, int Def, int Spd, string Skill, bool Boss)[] EnemyTable =
        {
            ("Enemy_Fly", "Ruồi Bàn", 500, 60, 30, 90, null, false),
            ("Enemy_Rat", "Chuột Bếp", 900, 85, 45, 100, null, false),
            ("Enemy_Roach", "Gián Kho", 1300, 110, 60, 85, "Claw", false),
            ("Enemy_RatKing", "Chuột Chúa", 7000, 135, 70, 105, "Roar", true),
        };

        // Đợt là danh sách file quái; thưởng theo thứ tự Material rồi Gem.
        private static readonly (string Id, string Name, StageKind Kind, string[][] Waves, int Stamina, long FirstMaterial, long FirstGem, long RepeatMaterial, long RepeatGem)[] StageTable =
        {
            ("st_1_story1", "Cốt truyện 1", StageKind.Story, new string[0][], 0, 0, 0, 0, 0),
            ("st_1_1", "1-1", StageKind.Battle, new[] { new[] { "Enemy_Fly", "Enemy_Fly" } }, 10, 30, 150, 30, 30),
            ("st_1_2", "1-2", StageKind.Battle, new[] { new[] { "Enemy_Fly", "Enemy_Fly" }, new[] { "Enemy_Fly", "Enemy_Fly", "Enemy_Fly" } }, 10, 40, 150, 40, 30),
            ("st_1_story2", "Cốt truyện 2", StageKind.Story, new string[0][], 0, 0, 0, 0, 0),
            ("st_1_3", "1-3", StageKind.Battle, new[] { new[] { "Enemy_Fly", "Enemy_Rat" }, new[] { "Enemy_Rat", "Enemy_Rat" }, new[] { "Enemy_Fly", "Enemy_Rat", "Enemy_Rat" } }, 10, 50, 200, 50, 40),
            ("st_1_4", "1-4", StageKind.Battle, new[] { new[] { "Enemy_Rat", "Enemy_Rat" }, new[] { "Enemy_Roach", "Enemy_Rat" }, new[] { "Enemy_Roach", "Enemy_Roach", "Enemy_Rat" } }, 10, 60, 200, 60, 40),
            ("st_1_5", "1-5", StageKind.Battle, new[] { new[] { "Enemy_Rat", "Enemy_Roach" }, new[] { "Enemy_Roach", "Enemy_Roach", "Enemy_Rat" }, new[] { "Enemy_RatKing" } }, 10, 100, 500, 100, 100),
        };

        private static readonly string[] StarterIds = { "char_nem_chua", "char_cha_com", "char_dau_phu" };

        [MenuItem("Tools/BunDauPeo/Setup Project")]
        public static void Run()
        {
            EnsureFolder(CharacterData, "Skills");
            EnsureFolder(CharacterData, "Characters");

            var skills = SkillTable.ToDictionary(row => row.File, row => CreateSkill(row, $"{SkillsFolder}/Skill_{row.File}.asset"));
            var characters = CharacterTable.Select(row => CreateCharacter(row, skills)).ToArray();
            // Asset đã có từ trước field _stars không được init lại, nên điền riêng; số user đã chỉnh (khác 0) giữ nguyên.
            FillMissingStars(characters);
            FillMissingClass(characters);

            GetOrCreateAsset<CharacterCatalog>(CatalogPath, asset =>
                Apply(asset, so => SetObjects(so.FindProperty("_items"), characters)));

            GetOrCreateAsset<NewGameConfig>(NewGameConfigPath, asset => Apply(asset, so =>
            {
                // Tính trong init để asset đã có không bị ảnh hưởng khi Id nhân vật mẫu đã được đổi.
                var starters = StarterIds.Select(id => characters.Single(c => c.Id == id)).ToArray();
                var money = so.FindProperty("_startingCurrency");
                var lines = new[] { new CurrencyAmount(CurrencyType.Coin, 500), new CurrencyAmount(CurrencyType.Gem, 1500) };
                money.arraySize = lines.Length;
                for (var i = 0; i < lines.Length; i++)
                {
                    var element = money.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("Type").enumValueIndex = (int)lines[i].Type;
                    element.FindPropertyRelative("Amount").longValue = lines[i].Amount;
                }

                SetObjects(so.FindProperty("_starterCharacters"), starters);
            }));

            EnsureFolder("Assets/_Project/Gacha", "Data");
            var banner = GetOrCreateBanner(BannerPath, characters);
            GetOrCreateAsset<GachaBannerCatalog>(BannerCatalogPath, asset =>
                Apply(asset, so => SetObjects(so.FindProperty("_items"), new Object[] { banner })));

            // Số khởi điểm nằm ở field initializer của StarConfig; asset đã có thì giữ số bạn đã chỉnh.
            GetOrCreateAsset<StarConfig>(StarConfigPath, null);

            CreateRestaurantData();
            CreateCombatData();
            FillEnglishNames();

            CreateBootScene();
            CreateGachaScene();
            CreateRestaurantScene();
            CreateStageSelectScene();
            CreateBattleScene();
            CreateCharactersScene();
            WireBootScene();
            ApplyBuildSettings();
            Debug.Log("ProjectSetup: done.");
        }

        // Run giữ scene Restaurant đã có; lệnh này dựng lại từ builder (ghi đè cùng đường dẫn nên GUID và Build Settings không đổi).
        [MenuItem("Tools/BunDauPeo/Rebuild Restaurant Scene")]
        public static void RebuildRestaurantScene() => RestaurantSceneBuilder.Create(RestaurantScenePath);

        [MenuItem("Tools/BunDauPeo/Rebuild Gacha Scene")]
        public static void RebuildGachaScene() => GachaSceneBuilder.Create(GachaScenePath);

        [MenuItem("Tools/BunDauPeo/Rebuild Characters Scene")]
        public static void RebuildCharactersScene() => CharactersSceneBuilder.Create(CharactersScenePath);

        [MenuItem("Tools/BunDauPeo/Rebuild StageSelect Scene")]
        public static void RebuildStageSelectScene() => StageSelectSceneBuilder.Create(StageSelectScenePath);

        [MenuItem("Tools/BunDauPeo/Rebuild Battle Scene")]
        public static void RebuildBattleScene() => BattleSceneBuilder.Create(BattleScenePath);

        public static T GetOrCreateAsset<T>(string path, Action<T> init) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            init?.Invoke(asset);
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // Số khởi điểm (chi phí, tỉ lệ, pity, mảnh) nằm ở field initializer của GachaBannerDefinition; ở đây chỉ gán Id và nhân vật.
        public static GachaBannerDefinition GetOrCreateBanner(string path, CharacterDefinition[] characters) =>
            GetOrCreateAsset<GachaBannerDefinition>(path, asset => Apply(asset, so =>
            {
                so.FindProperty("_id").stringValue = "banner_standard";
                SetObjects(so.FindProperty("_characters"), characters);
            }));

        // Boot.unity đã có sẵn nên phải vá chứ không tạo lại: chỉ gán field còn trống, không ghi đè lựa chọn của user.
        public static void WireBootScene()
        {
            var wasOpen = SceneManager.GetSceneByPath(BootScenePath).isLoaded;
            var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Additive);
            try
            {
                var bootstrap = scene.GetRootGameObjects().Select(g => g.GetComponent<GameBootstrap>()).FirstOrDefault(b => b != null);
                if (bootstrap == null)
                {
                    Debug.LogWarning($"ProjectSetup: no {nameof(GameBootstrap)} in {BootScenePath}; scene not wired.");
                    return;
                }

                var changed = false;
                Apply(bootstrap, so =>
                {
                    // Nạp sau khi mở scene (L-002): không giữ tham chiếu asset từ trước khi đổi scene.
                    var banners = so.FindProperty("_bannerCatalog");
                    if (banners.objectReferenceValue == null)
                    {
                        banners.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GachaBannerCatalog>(BannerCatalogPath);
                        changed = true;
                    }

                    var restaurant = so.FindProperty("_restaurantConfig");
                    if (restaurant.objectReferenceValue == null)
                    {
                        restaurant.objectReferenceValue = AssetDatabase.LoadAssetAtPath<RestaurantConfig>(RestaurantConfigPath);
                        changed = true;
                    }

                    var combat = so.FindProperty("_combatConfig");
                    if (combat.objectReferenceValue == null)
                    {
                        combat.objectReferenceValue = AssetDatabase.LoadAssetAtPath<CombatConfig>(CombatConfigPath);
                        changed = true;
                    }

                    var stars = so.FindProperty("_starConfig");
                    if (stars.objectReferenceValue == null)
                    {
                        stars.objectReferenceValue = AssetDatabase.LoadAssetAtPath<StarConfig>(StarConfigPath);
                        changed = true;
                    }

                    // Chỉ đổi giá trị trống hoặc "Gacha" (mặc định cũ): lựa chọn khác của user được giữ.
                    var start = so.FindProperty("_startScene");
                    if (string.IsNullOrEmpty(start.stringValue) || start.stringValue == Path.GetFileNameWithoutExtension(GachaScenePath))
                    {
                        start.stringValue = Path.GetFileNameWithoutExtension(RestaurantScenePath);
                        changed = true;
                    }
                });

                if (!changed) return;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        // Chỉ tạo asset còn thiếu; số bạn đã chỉnh trong Inspector được giữ nguyên.
        private static void CreateRestaurantData()
        {
            var icon = AssetDatabase.GetBuiltinExtraResource<Sprite>(IconSprite);

            var dishes = DishTable.Select(row => GetOrCreateAsset<DishDefinition>($"{RestaurantData}/{row.File}.asset", asset => Apply(asset, so =>
            {
                so.FindProperty("_displayName").stringValue = row.Name;
                so.FindProperty("_icon").objectReferenceValue = icon;
                so.FindProperty("_basePrice").longValue = row.Price;
                so.FindProperty("_cookSeconds").floatValue = row.CookSeconds;
            }))).ToArray();

            var customers = CustomerTable.Select(row => GetOrCreateAsset<CustomerDefinition>($"{RestaurantData}/{row.File}.asset", asset => Apply(asset, so =>
            {
                so.FindProperty("_displayName").stringValue = row.Name;
                so.FindProperty("_sprite").objectReferenceValue = icon;
                so.FindProperty("_spawnWeight").floatValue = row.Weight;
                so.FindProperty("_patienceSeconds").floatValue = row.Patience;
                so.FindProperty("_payMultiplier").floatValue = row.Pay;
            }))).ToArray();

            var incidents = IncidentTable.Select(row => GetOrCreateAsset<IncidentDefinition>($"{RestaurantData}/{row.File}.asset", asset => Apply(asset, so =>
            {
                so.FindProperty("_displayName").stringValue = row.Name;
                so.FindProperty("_role").enumValueIndex = (int)row.Role;
                so.FindProperty("_weight").floatValue = 1f;
            }))).ToArray();

            var upgrades = UpgradeTable.Select(row => GetOrCreateAsset<UpgradeDefinition>($"{RestaurantData}/Upgrade_{row.Id}.asset", asset => Apply(asset, so =>
            {
                so.FindProperty("_id").stringValue = row.Id;
                so.FindProperty("_displayName").stringValue = row.Name;
                so.FindProperty("_icon").objectReferenceValue = icon;
                so.FindProperty("_effect").enumValueIndex = (int)row.Effect;
                var levels = so.FindProperty("_levels");
                levels.arraySize = row.Values.Length;
                for (var i = 0; i < row.Values.Length; i++)
                {
                    var level = levels.GetArrayElementAtIndex(i);
                    level.FindPropertyRelative("Value").floatValue = row.Values[i];
                    var cost = level.FindPropertyRelative("Cost");
                    cost.arraySize = 2;
                    SetCost(cost.GetArrayElementAtIndex(0), CurrencyType.Coin, row.Coins[i]);
                    SetCost(cost.GetArrayElementAtIndex(1), CurrencyType.Material, row.Materials[i]);
                }
            }))).ToArray();

            var catalog = GetOrCreateAsset<UpgradeCatalog>(UpgradeCatalogPath, asset =>
                Apply(asset, so => SetObjects(so.FindProperty("_items"), upgrades)));

            GetOrCreateAsset<RestaurantConfig>(RestaurantConfigPath, asset => Apply(asset, so =>
            {
                SetObjects(so.FindProperty("_dishes"), dishes);
                SetObjects(so.FindProperty("_customers"), customers);
                SetObjects(so.FindProperty("_incidents"), incidents);
                so.FindProperty("_upgrades").objectReferenceValue = catalog;
            }));
        }

        // Chỉ tạo asset còn thiếu; số bạn đã chỉnh trong Inspector được giữ nguyên.
        private static void CreateCombatData()
        {
            EnsureFolder("Assets/_Project/Combat", "Data");
            foreach (var folder in new[] { "Skills", "Enemies", "Stages" }) EnsureFolder(CombatData, folder);

            var basic = AssetDatabase.LoadAssetAtPath<SkillDefinition>($"{SkillsFolder}/Skill_BasicStrike.asset");
            var enemySkills = EnemySkillTable.ToDictionary(row => row.File,
                row => CreateSkill(row, $"{CombatData}/Skills/EnemySkill_{row.File}.asset"));

            var enemies = EnemyTable.ToDictionary(row => row.File, row =>
                GetOrCreateAsset<EnemyDefinition>($"{CombatData}/Enemies/{row.File}.asset", asset => Apply(asset, so =>
                {
                    so.FindProperty("_displayName").stringValue = row.Name;
                    var stats = so.FindProperty("_stats");
                    stats.FindPropertyRelative("HP").intValue = row.Hp;
                    stats.FindPropertyRelative("Attack").intValue = row.Atk;
                    stats.FindPropertyRelative("Defense").intValue = row.Def;
                    stats.FindPropertyRelative("Speed").intValue = row.Spd;
                    so.FindProperty("_basicAttack").objectReferenceValue = basic;
                    so.FindProperty("_skill").objectReferenceValue = row.Skill == null ? null : enemySkills[row.Skill];
                    so.FindProperty("_isBoss").boolValue = row.Boss;
                })));

            var stages = StageTable.Select(row =>
                GetOrCreateAsset<StageDefinition>($"{CombatData}/Stages/{row.Id}.asset", asset => Apply(asset, so =>
                {
                    so.FindProperty("_id").stringValue = row.Id;
                    so.FindProperty("_displayName").stringValue = row.Name;
                    so.FindProperty("_kind").enumValueIndex = (int)row.Kind;
                    so.FindProperty("_staminaCost").intValue = row.Stamina;

                    var waves = so.FindProperty("_waves");
                    waves.arraySize = row.Waves.Length;
                    for (var i = 0; i < row.Waves.Length; i++)
                        SetObjects(waves.GetArrayElementAtIndex(i).FindPropertyRelative("_enemies"),
                            row.Waves[i].Select(file => (Object)enemies[file]).ToArray());

                    if (row.Kind == StageKind.Story) return;
                    SetRewards(so.FindProperty("_firstClearReward"), row.FirstMaterial, row.FirstGem);
                    SetRewards(so.FindProperty("_repeatReward"), row.RepeatMaterial, row.RepeatGem);
                }))).ToArray();

            var catalog = GetOrCreateAsset<StageCatalog>(StageCatalogPath, asset =>
                Apply(asset, so => SetObjects(so.FindProperty("_items"), stages)));

            GetOrCreateAsset<CombatConfig>(CombatConfigPath, asset =>
                Apply(asset, so => so.FindProperty("_stages").objectReferenceValue = catalog));

            MigrateStaminaRule();
        }

        // Tên tiếng Anh theo tên tiếng Việt của asset. Nhân vật và món: bỏ dấu, viết hoa chữ đầu; tên khác dịch nghĩa.
        private static readonly Dictionary<string, string> EnglishNames = new()
        {
            ["Nem Chua"] = "Nem Chua", ["Chả Cốm"] = "Cha Com", ["Đậu Phụ"] = "Dau Phu", ["Mắm Tôm"] = "Mam Tom",
            ["Lòng Dồi"] = "Long Doi", ["Bún Lá"] = "Bun La",
            ["Bún đậu mắm tôm"] = "Bun Dau Mam Tom", ["Chả cốm"] = "Cha Com", ["Nem chua rán"] = "Nem Chua Ran",
            ["Cào mạnh"] = "Savage Claw", ["Gầm"] = "Roar",
            ["Khách thường"] = "Regular Customer", ["Khách sành ăn"] = "Gourmet Customer", ["Khách vội"] = "Hurried Customer",
            ["Bếp tắc ga"] = "Blocked Stove", ["Khách làm đổ đồ"] = "Spilled Food", ["Máy tính tiền treo"] = "Frozen Register",
            ["Bàn"] = "Tables", ["Ô đầu bếp"] = "Cook Slots", ["Ô phục vụ"] = "Server Slots", ["Ô thu ngân"] = "Cashier Slots",
            ["Biển hiệu"] = "Signboard", ["Thực đơn"] = "Menu",
            ["Ruồi Bàn"] = "Table Fly", ["Chuột Bếp"] = "Kitchen Rat", ["Gián Kho"] = "Storeroom Roach", ["Chuột Chúa"] = "Rat King",
            ["Cốt truyện 1"] = "Story 1", ["Cốt truyện 2"] = "Story 2", ["Banner Thường"] = "Standard Banner",
        };

        private static readonly Type[] NamedTypes =
        {
            typeof(CharacterDefinition), typeof(SkillDefinition), typeof(GachaBannerDefinition), typeof(DishDefinition),
            typeof(CustomerDefinition), typeof(IncidentDefinition), typeof(UpgradeDefinition), typeof(EnemyDefinition), typeof(StageDefinition),
        };

        // Chỉ điền ô _displayNameEn đang rỗng, không đè bản user đã sửa. Tên không dấu sẵn (Basic Strike, 1-1) dùng lại nguyên văn.
        internal static void FillEnglishNames()
        {
            foreach (var type in NamedTypes)
            foreach (var guid in AssetDatabase.FindAssets($"t:{type.Name}", new[] { "Assets/_Project" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, type);
                var so = new SerializedObject(asset);
                var english = so.FindProperty("_displayNameEn");
                if (!string.IsNullOrWhiteSpace(english.stringValue)) continue;

                var vi = so.FindProperty("_displayName").stringValue;
                if (!EnglishNames.TryGetValue(vi, out var en))
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(vi, "[À-ỹĐđ]")) { Debug.LogWarning($"ProjectSetup: no English name for '{vi}' ({path})."); continue; }
                    en = vi;
                }

                english.stringValue = en;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
        }

        // Luật thể lực đổi 2026-11-03 (D-008): tối đa 60 → 100, màn 1-5 từ 15 → 10. Setup không ghi đè asset đã sinh nên đổi riêng;
        // chỉ đổi đúng giá trị cũ nguyên văn, số user đã sửa tay giữ nguyên.
        public static void MigrateStaminaRule()
        {
            MigrateInt(AssetDatabase.LoadAssetAtPath<CombatConfig>(CombatConfigPath), "_maxStamina", 60, 100);
            MigrateInt(AssetDatabase.LoadAssetAtPath<StageDefinition>($"{CombatData}/Stages/st_1_5.asset"), "_staminaCost", 15, 10);
        }

        private static void MigrateInt(Object asset, string field, int oldValue, int newValue)
        {
            if (asset == null) return;

            var so = new SerializedObject(asset);
            var property = so.FindProperty(field);
            if (property.intValue != oldValue) return;

            property.intValue = newValue;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static void SetRewards(SerializedProperty array, long material, long gem)
        {
            array.arraySize = 2;
            SetCost(array.GetArrayElementAtIndex(0), CurrencyType.Material, material);
            SetCost(array.GetArrayElementAtIndex(1), CurrencyType.Gem, gem);
        }

        private static void SetCost(SerializedProperty line, CurrencyType type, long amount)
        {
            line.FindPropertyRelative("Type").enumValueIndex = (int)type;
            line.FindPropertyRelative("Amount").longValue = amount;
        }

        private static SkillDefinition CreateSkill(
            (string File, string Display, SkillKind Kind, SkillTarget Target, SkillEffect Effect, float Power, int Cooldown, int Energy) row,
            string path) =>
            GetOrCreateAsset<SkillDefinition>(path, asset => Apply(asset, so =>
            {
                so.FindProperty("_displayName").stringValue = row.Display;
                so.FindProperty("_icon").objectReferenceValue = AssetDatabase.GetBuiltinExtraResource<Sprite>(IconSprite);
                so.FindProperty("_kind").enumValueIndex = (int)row.Kind;
                so.FindProperty("_target").enumValueIndex = (int)row.Target;
                so.FindProperty("_effect").enumValueIndex = (int)row.Effect;
                so.FindProperty("_power").floatValue = row.Power;
                so.FindProperty("_cooldown").intValue = row.Cooldown;
                so.FindProperty("_energyCost").intValue = row.Energy;
                so.FindProperty("_animationTrigger").stringValue = row.File;
            }));

        private static CharacterDefinition CreateCharacter(
            (string Id, string Name, Rarity Rarity, CharacterClass Class, int Hp, int Atk, int Def, int Spd, int Cook, int Serve, int Cashier, string Skill) row,
            Dictionary<string, SkillDefinition> skills) =>
            GetOrCreateAsset<CharacterDefinition>($"{CharactersFolder}/{row.Id}.asset", asset => Apply(asset, so =>
            {
                so.FindProperty("_id").stringValue = row.Id;
                so.FindProperty("_displayName").stringValue = row.Name;
                so.FindProperty("_rarity").enumValueIndex = (int)row.Rarity;
                so.FindProperty("_stars").intValue = StarsFor(row.Rarity);
                so.FindProperty("_class").enumValueIndex = (int)row.Class;
                so.FindProperty("_portrait").objectReferenceValue = AssetDatabase.GetBuiltinExtraResource<Sprite>(PortraitSprite);
                so.FindProperty("_battleSprite").objectReferenceValue = AssetDatabase.GetBuiltinExtraResource<Sprite>(IconSprite);

                var combat = so.FindProperty("_combatStats");
                combat.FindPropertyRelative("HP").intValue = row.Hp;
                combat.FindPropertyRelative("Attack").intValue = row.Atk;
                combat.FindPropertyRelative("Defense").intValue = row.Def;
                combat.FindPropertyRelative("Speed").intValue = row.Spd;

                var restaurant = so.FindProperty("_restaurantStats");
                restaurant.FindPropertyRelative("Cooking").intValue = row.Cook;
                restaurant.FindPropertyRelative("Serving").intValue = row.Serve;
                restaurant.FindPropertyRelative("Cashier").intValue = row.Cashier;

                so.FindProperty("_basicAttack").objectReferenceValue = skills["BasicStrike"];
                so.FindProperty("_skill").objectReferenceValue = skills[row.Skill];
                so.FindProperty("_ultimate").objectReferenceValue = skills["Finisher"];
            }));

        private static void CreateBootScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath) != null) return;

            // Additive để không đụng scene bạn đang mở. Batch mode chỉ có scene "Untitled" chưa lưu, và Unity từ chối
            // Additive cạnh nó; ở đó không có gì để mất nên dùng Single.
            var mode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            var go = new GameObject(nameof(GameBootstrap));
            SceneManager.MoveGameObjectToScene(go, scene);
            var bootstrap = go.AddComponent<GameBootstrap>();
            // Nạp asset sau NewScene: chế độ Single dỡ các asset đang không được scene nào dùng, nên tham chiếu giữ từ trước sẽ thành null.
            Apply(bootstrap, so =>
            {
                so.FindProperty("_catalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CatalogPath);
                so.FindProperty("_newGameConfig").objectReferenceValue = AssetDatabase.LoadAssetAtPath<NewGameConfig>(NewGameConfigPath);
            });

            EditorSceneManager.SaveScene(scene, BootScenePath);
            // Scene duy nhất đang mở thì không đóng được.
            if (mode == NewSceneMode.Additive) EditorSceneManager.CloseScene(scene, true);
        }

        private static void CreateGachaScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GachaScenePath) == null) GachaSceneBuilder.Create(GachaScenePath);
        }

        private static void CreateCharactersScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CharactersScenePath) == null) CharactersSceneBuilder.Create(CharactersScenePath);
        }

        private static void CreateRestaurantScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RestaurantScenePath) == null) RestaurantSceneBuilder.Create(RestaurantScenePath);
        }

        private static void CreateStageSelectScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StageSelectScenePath) == null) StageSelectSceneBuilder.Create(StageSelectScenePath);
        }

        private static void CreateBattleScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BattleScenePath) == null) BattleSceneBuilder.Create(BattleScenePath);
        }

        // Boot luôn đầu tiên (nạp trước mọi scene), rồi Gacha, Restaurant, StageSelect, Battle, Characters.
        private static void PutScenesInBuildOrder()
        {
            var ordered = new[] { BootScenePath, GachaScenePath, RestaurantScenePath, StageSelectScenePath, BattleScenePath, CharactersScenePath };
            // Scene ngoài Assets/_Project/Scenes/ (SampleScene) bị bỏ khỏi build; file scene giữ nguyên.
            var rest = EditorBuildSettings.scenes.Where(s => !ordered.Contains(s.path) && s.path.StartsWith("Assets/_Project/Scenes/"));
            EditorBuildSettings.scenes = ordered
                .Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(rest)
                .ToArray();
        }

        // Tách khỏi Run để chạy riêng ở batch mode mà không phải chạy cả Setup Project.
        public static void ApplyBuildSettings()
        {
            PutScenesInBuildOrder();
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, AndroidBuild.PackageName);

            // Hai tên này quyết định thư mục persistentDataPath chứa save; không đụng Bundle Identifier.
            PlayerSettings.companyName = "Sweet-Yuu";
            PlayerSettings.productName = "BunDauPeo";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        // Số sao khởi điểm: R = 1, SR = 2, SSR = 3. Chỉ là giá trị mẫu, user chỉnh riêng từng nhân vật.
        private static int StarsFor(Rarity rarity) => (int)rarity + 1;

        private static void FillMissingStars(CharacterDefinition[] characters)
        {
            foreach (var character in characters)
            {
                var so = new SerializedObject(character);
                var stars = so.FindProperty("_stars");
                if (stars.intValue != 0) continue;
                stars.intValue = StarsFor(character.Rarity);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // Cùng khuôn FillMissingStars: chỉ điền khi còn Unset; Id lạ (đã đổi tên) thì bỏ qua.
        private static void FillMissingClass(CharacterDefinition[] characters)
        {
            foreach (var character in characters)
            {
                var so = new SerializedObject(character);
                var cls = so.FindProperty("_class");
                if (cls.enumValueIndex != (int)CharacterClass.Unset) continue;
                var row = CharacterTable.FirstOrDefault(r => r.Id == character.Id);
                if (row.Id == null) continue;
                cls.enumValueIndex = (int)row.Class;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }

        private static void SetObjects(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        // Gán field private qua SerializedObject, cùng cách với TestData, để không thêm setter vào code game.
        private static void Apply(Object target, Action<SerializedObject> set)
        {
            var so = new SerializedObject(target);
            set(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
