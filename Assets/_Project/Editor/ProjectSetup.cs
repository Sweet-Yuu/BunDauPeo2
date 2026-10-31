using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Core;
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

        private static readonly (string Id, string Name, Rarity Rarity, int Hp, int Atk, int Def, int Spd, int Cook, int Serve, int Cashier, string Skill)[] CharacterTable =
        {
            ("char_nem_chua", "Nem Chua", Rarity.R, 800, 80, 40, 100, 30, 20, 10, "PowerStrike"),
            ("char_cha_com", "Chả Cốm", Rarity.R, 900, 70, 50, 95, 20, 30, 10, "PowerStrike"),
            ("char_dau_phu", "Đậu Phụ", Rarity.R, 1000, 60, 60, 90, 10, 20, 30, "Heal"),
            ("char_mam_tom", "Mắm Tôm", Rarity.SR, 1000, 110, 50, 110, 50, 30, 20, "PowerStrike"),
            ("char_long_doi", "Lòng Dồi", Rarity.SR, 1200, 90, 70, 100, 30, 50, 20, "Heal"),
            ("char_bun_la", "Bún Lá", Rarity.SSR, 1400, 130, 80, 120, 60, 50, 50, "PowerStrike"),
        };

        private static readonly string[] StarterIds = { "char_nem_chua", "char_cha_com", "char_dau_phu" };

        [MenuItem("Tools/BunDauPeo/Setup Project")]
        public static void Run()
        {
            EnsureFolder(CharacterData, "Skills");
            EnsureFolder(CharacterData, "Characters");

            var skills = SkillTable.ToDictionary(row => row.File, CreateSkill);
            var characters = CharacterTable.Select(row => CreateCharacter(row, skills)).ToArray();

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

            CreateBootScene();
            PutBootSceneFirst();
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("ProjectSetup: done.");
        }

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

        private static SkillDefinition CreateSkill(
            (string File, string Display, SkillKind Kind, SkillTarget Target, SkillEffect Effect, float Power, int Cooldown, int Energy) row) =>
            GetOrCreateAsset<SkillDefinition>($"{SkillsFolder}/Skill_{row.File}.asset", asset => Apply(asset, so =>
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
            (string Id, string Name, Rarity Rarity, int Hp, int Atk, int Def, int Spd, int Cook, int Serve, int Cashier, string Skill) row,
            Dictionary<string, SkillDefinition> skills) =>
            GetOrCreateAsset<CharacterDefinition>($"{CharactersFolder}/{row.Id}.asset", asset => Apply(asset, so =>
            {
                so.FindProperty("_id").stringValue = row.Id;
                so.FindProperty("_displayName").stringValue = row.Name;
                so.FindProperty("_rarity").enumValueIndex = (int)row.Rarity;
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

        private static void PutBootSceneFirst()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length > 0 && scenes[0].path == BootScenePath && scenes[0].enabled) return;

            var rest = scenes.Where(s => s.path != BootScenePath);
            EditorBuildSettings.scenes = rest.Prepend(new EditorBuildSettingsScene(BootScenePath, true)).ToArray();
        }

        private static void ApplyPlayerSettings()
        {
            // Hai tên này quyết định thư mục persistentDataPath chứa save; không đụng Bundle Identifier.
            PlayerSettings.companyName = "Sweet-Yuu";
            PlayerSettings.productName = "BunDauPeo";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
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
