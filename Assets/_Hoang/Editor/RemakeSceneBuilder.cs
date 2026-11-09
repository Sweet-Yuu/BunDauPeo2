using System;
using System.Collections.Generic;
using System.IO;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Restaurant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Hoang.Remake.Editor
{
    public static class RemakeSceneBuilder
    {
        public const string ScenePath = "Assets/_Hoang/Scenes/RestaurantSystems.unity";
        public const string PrefabPath = "Assets/_Hoang/Resources/HoangRestaurantSystems.prefab";
        private const string DataRoot = "Assets/_Hoang/Data/";

        [MenuItem("Hoang/Restaurant Remake/Rebuild scene and prefab")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.DeleteAsset("Assets/_Hoang/Docs/remake-generate.log");
            AssetDatabase.DeleteAsset("Assets/_Hoang/Data/NewGameConfig.asset");
            Directory.CreateDirectory(DataRoot + "Skills");
            Directory.CreateDirectory(DataRoot + "Facilities");
            Directory.CreateDirectory("Assets/_Hoang/Resources");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.backgroundColor = RemakeUI.Paper;
            camera.clearFlags = CameraClearFlags.SolidColor;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var skills = CreateSkills();
            var facilities = CreateFacilities();
            var shell = RemakeShell.Create(skills);
            shell.FacilityGrid.Configure(facilities);
            PrefabUtility.SaveAsPrefabAsset(shell.gameObject, PrefabPath);
            var bootstrap = new GameObject("Restaurant Systems Bootstrap").AddComponent<RemakeBootstrap>();
            bootstrap.Configure(
                AssetDatabase.LoadAssetAtPath<CharacterCatalog>("Assets/_Project/Characters/Data/CharacterCatalog.asset"),
                CreateNewGameConfig(),
                AssetDatabase.LoadAssetAtPath<RestaurantConfig>("Assets/_Project/Restaurant/Data/RestaurantConfig.asset"),
                skills, facilities, shell);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Restaurant remake scene and editable UI prefab created: " + ScenePath);
        }

        [MenuItem("Hoang/Restaurant Remake/Open scene")]
        public static void Open() => TryOpen();

        private static bool TryOpen()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) Build();
            else EditorSceneManager.OpenScene(ScenePath);
            return SceneManager.GetActiveScene().path == ScenePath;
        }

        [MenuItem("Hoang/Restaurant Remake/Play remake")]
        public static void Play()
        {
            if (EditorApplication.isPlaying) return;
            if (!TryOpen()) return;
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Hoang/Restaurant Remake/Build Windows player")]
        public static void BuildWindows()
        {
            var folder = EditorUtility.SaveFolderPanel("Build restaurant remake", "", "BunDauPeo-Remake");
            if (string.IsNullOrEmpty(folder)) return;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, target = BuildTarget.StandaloneWindows64,
                locationPathName = Path.Combine(folder, "BunDauPeoRemake.exe"), options = BuildOptions.Development,
            });
            Debug.Log("Restaurant remake build: " + report.summary.result);
        }

        [MenuItem("Tools/Hoang/Save & Load/Save current game state")]
        public static void SaveCurrent()
        {
            if (RemakeBootstrap.Active?.Context == null) { Debug.Log("Enter Play in the remake scene first."); return; }
            Debug.Log(RemakeBootstrap.Active.Context.Persist() ? "Restaurant progress saved." : "Restaurant save failed.");
        }

        [MenuItem("Tools/Hoang/Save & Load/Open save directory")]
        public static void OpenSaveDirectory()
        {
            var directory = Path.GetDirectoryName(RemakeBootstrap.SavePath);
            Directory.CreateDirectory(directory);
            EditorUtility.RevealInFinder(directory);
        }

        [MenuItem("Tools/Hoang/Save & Load/Clear all save data")]
        public static void ClearSave()
        {
            if (!EditorUtility.DisplayDialog("Reset all restaurant progress?",
                "All currency, skills, facilities and daily progress in the remake will be reset.", "Reset everything", "Cancel")) return;
            var success = RemakeBootstrap.Active?.Context != null
                ? RemakeBootstrap.Active.Context.Reset()
                : new RemakeSaveStore(RemakeBootstrap.SavePath).DeleteAll();
            Debug.Log(success ? "Restaurant progress reset." : "Could not reset restaurant progress.");
        }

        private static SkillSO[] CreateSkills()
        {
            var cook = Skill("cooking", "Quick hands", "Cook meals faster.", new Vector2(0.17f, 0.76f), s => s.ProductionSpeedBonusPerLevel = 0.05f);
            var revenue = Skill("revenue", "Menu craft", "Earn more Gold for each meal.", new Vector2(0.50f, 0.76f), s => s.RevenueBonusPerLevel = 0.05f);
            var patience = Skill("patience", "Warm welcome", "Customers wait longer before leaving.", new Vector2(0.83f, 0.76f), s => s.CustomerPatienceBonusPerLevel = 0.08f);
            var cost = Skill("ingredients", "Smart sourcing", "Reduce ingredient restocking costs.", new Vector2(0.17f, 0.45f), s =>
            {
                s.IngredientCostReductionPerLevel = 0.04f;
                s.Prerequisites = new[] { Prerequisite(cook, 2) };
            });
            var clean = Skill("cleanliness", "Clean kitchen", "Improve your restaurant reputation.", new Vector2(0.50f, 0.45f), s =>
            {
                s.CleanlinessBonusPerLevel = 3;
                s.Prerequisites = new[] { Prerequisite(revenue, 2) };
            });
            var service = Skill("hospitality", "Thoughtful service", "Increase patience and meal revenue.", new Vector2(0.83f, 0.45f), s =>
            {
                s.CustomerPatienceBonusPerLevel = 0.05f; s.RevenueBonusPerLevel = 0.02f;
                s.Prerequisites = new[] { Prerequisite(patience, 2) };
            });
            var chef = Skill("master_chef", "Master chef", "Exclusive: cook much faster.", new Vector2(0.17f, 0.14f), s =>
            {
                s.IsExclusive = true; s.MaxLevel = 3; s.ProductionSpeedBonusPerLevel = 0.15f;
                s.Prerequisites = new[] { Prerequisite(cost, 2) };
            });
            var owner = Skill("master_owner", "Restaurant owner", "Exclusive: boost revenue and reputation.", new Vector2(0.50f, 0.14f), s =>
            {
                s.IsExclusive = true; s.MaxLevel = 3; s.RevenueBonusPerLevel = 0.15f; s.CleanlinessBonusPerLevel = 5;
                s.Prerequisites = new[] { Prerequisite(clean, 2), Prerequisite(cost, 1) };
            });
            var host = Skill("master_host", "Beloved host", "Exclusive: guests stay patient and ingredients cost less.", new Vector2(0.83f, 0.14f), s =>
            {
                s.IsExclusive = true; s.MaxLevel = 3; s.CustomerPatienceBonusPerLevel = 0.20f; s.IngredientCostReductionPerLevel = 0.05f;
                s.Prerequisites = new[] { Prerequisite(service, 2) };
            });
            return new[] { cook, revenue, patience, cost, clean, service, chef, owner, host };
        }

        private static RemakeNewGameConfig CreateNewGameConfig()
        {
            return GetOrCreate<RemakeNewGameConfig>(DataRoot + "RemakeDefaults.asset", config =>
                config.SetStarters(AssetDatabase.LoadAssetAtPath<NewGameConfig>("Assets/_Project/Boot/Data/NewGameConfig.asset").StarterCharacters));
        }

        private static SkillPrerequisite Prerequisite(SkillSO skill, int level) => new() { Skill = skill, MinimumLevel = level };

        private static SkillSO Skill(string id, string name, string description, Vector2 position, Action<SkillSO> configure) =>
            GetOrCreate<SkillSO>(DataRoot + "Skills/" + id + ".asset", s =>
            {
                s.Id = id; s.SkillName = name; s.Description = description; s.GraphPosition = position; configure(s);
            });

        private static FacilityItemSO[] CreateFacilities()
        {
            return new[]
            {
                Facility("generator", "Generator", "GEN", "Source: supplies 120 W to adjacent devices.", new[]{new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(0,1),new Vector2Int(1,1)}, s =>
                { s.IsPowerSource = true; s.PowerOutput = 120; s.ThemeColor = new Color(0.95f,0.75f,0.25f); }),
                Facility("wire", "Power cable", "WIRE", "Conducts power. No consumption.", new[]{Vector2Int.zero}, s =>
                { s.ThemeColor = new Color(0.62f,0.65f,0.70f); }),
                Facility("relay", "Power relay", "RELAY", "Conducts power and supplies sensitive high-load devices.", new[]{Vector2Int.zero}, s =>
                { s.IsRelay = true; s.PowerConsumption = 5; s.ThemeColor = new Color(0.65f,0.48f,0.85f); }),
                Facility("stove", "Fast stove", "STOVE", "Powered: cooking speed +20%. Needs 35 W.", new[]{new Vector2Int(0,0),new Vector2Int(1,0)}, s =>
                { s.PowerConsumption = 35; s.Stats = new FacilityStats { ProductionSpeedBonus = 0.20f }; s.ThemeColor = new Color(0.91f,0.49f,0.30f); }),
                Facility("freezer", "Cold storage", "COLD", "Requires source or powered relay next to it. Saves 15% on ingredients. Needs 60 W.", new[]{new Vector2Int(0,0),new Vector2Int(0,1),new Vector2Int(1,0)}, s =>
                { s.PowerConsumption = 60; s.RequiresRelay = true; s.Stats = new FacilityStats { IngredientCostReduction = 0.15f }; s.ThemeColor = new Color(0.33f,0.68f,0.86f); }),
                Facility("register", "Smart register", "TILL", "Powered: revenue +15%. Needs 20 W.", new[]{Vector2Int.zero}, s =>
                { s.PowerConsumption = 20; s.CanConductPower = false; s.Stats = new FacilityStats { RevenueBonus = 0.15f }; s.ThemeColor = new Color(0.42f,0.73f,0.50f); }),
                Facility("purifier", "Air purifier", "AIR", "Powered: patience +15%, cleanliness +10. Needs 25 W.", new[]{new Vector2Int(0,0),new Vector2Int(0,1)}, s =>
                { s.PowerConsumption = 25; s.Stats = new FacilityStats { CustomerPatienceBonus = 0.15f, CleanlinessBonus = 10 }; s.ThemeColor = new Color(0.52f,0.80f,0.76f); }),
            };
        }

        private static FacilityItemSO Facility(string id, string name, string shortName, string description, Vector2Int[] shape, Action<FacilityItemSO> configure) =>
            GetOrCreate<FacilityItemSO>(DataRoot + "Facilities/" + id + ".asset", s =>
            {
                s.Id = id; s.ItemName = name; s.ShortName = shortName; s.Description = description;
                s.ShapeCoords = new List<Vector2Int>(shape); configure(s);
            });

        private static T GetOrCreate<T>(string path, Action<T> configure) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
