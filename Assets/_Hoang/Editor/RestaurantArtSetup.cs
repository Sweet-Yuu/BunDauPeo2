using System;
using System.IO;
using System.Linq;
using BunDauPeo.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace Hoang.Remake.Editor
{
    public static class RestaurantArtSetup
    {
        private const string Root = "Assets/_Project/UI/Resources/RestaurantArt/";
        public static readonly string[] SkillFacilityIds =
        {
            "cooking", "revenue", "patience", "ingredients", "cleanliness", "hospitality", "master_chef", "master_owner",
            "master_host", "generator", "stove", "freezer", "purifier", "wire", "relay", "register"
        };
        public static readonly string[] RewardIds = { "coin", "diamond", "ingredient_basket", "gift", "pouch", "chest", "vault", "platter" };

        [MenuItem("Hoang/Restaurant Art/Import and apply approved style")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ImportArt();
            foreach (var guid in AssetDatabase.FindAssets("t:SkillSO", new[] { "Assets/_Hoang/Data/Skills" }))
            {
                var skill = AssetDatabase.LoadAssetAtPath<SkillSO>(AssetDatabase.GUIDToAssetPath(guid));
                skill.SkillIcon = RestaurantArt.Icon(skill.Id);
                EditorUtility.SetDirty(skill);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:FacilityItemSO", new[] { "Assets/_Hoang/Data/Facilities" }))
            {
                var facility = AssetDatabase.LoadAssetAtPath<FacilityItemSO>(AssetDatabase.GUIDToAssetPath(guid));
                facility.Icon = RestaurantArt.Icon(facility.Id);
                EditorUtility.SetDirty(facility);
            }
            AssetDatabase.SaveAssets();

            var prefab = PrefabUtility.LoadPrefabContents(RemakeSceneBuilder.PrefabPath);
            try
            {
                RemakeRestaurantSkin.Apply(prefab.GetComponent<RemakeShell>());
                PrefabUtility.SaveAsPrefabAsset(prefab, RemakeSceneBuilder.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var scene = EditorSceneManager.OpenScene(RemakeSceneBuilder.ScenePath);
            var shell = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RemakeShell>(true)).Single();
            RemakeRestaurantSkin.Apply(shell);
            EditorSceneManager.SaveScene(scene);

            var lobby = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Restaurant.unity");
            var canvas = lobby.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).First(c => c.isRootCanvas);
            BunDauPeo.EditorTools.RestaurantLobbySkin.Apply((RectTransform)canvas.transform);
            EditorSceneManager.SaveScene(lobby);
            AssetDatabase.SaveAssets();
            Debug.Log("RestaurantArtSetup OK: imported 24 icons, garden and frame; updated original Lobby plus remake scene/prefab.");
        }

        public static void ImportArt()
        {
            AssetDatabase.Refresh();
            ImportSheet("skills-facilities.png", SkillFacilityIds, 4, 4);
            ImportSheet("rewards.png", RewardIds, 4, 2);
            ImportSingle("garden.png");
            CreateFrame();
            RestaurantArt.ClearCache();
        }

        private static void ImportSheet(string file, string[] names, int columns, int rows)
        {
            var path = Root + file;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            importer.GetSourceTextureWidthAndHeight(out var width, out var height);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Sprite importer has no data provider: " + path);
            provider.InitSpriteEditorDataProvider();
            // Installed Sprite package predates ISpriteFrameEditCapability; check its supported provider contract.
            if (!provider.HasDataProvider(typeof(ISpriteNameFileIdDataProvider)))
                throw new InvalidOperationException("Sprite importer cannot preserve sprite identities: " + path);
            var existing = provider.GetSpriteRects();
            var rects = names.Select((name, i) => new SpriteRect
            {
                name = name,
                rect = new Rect(i % columns * width / (float)columns, (rows - 1 - i / columns) * height / (float)rows,
                    width / (float)columns, height / (float)rows),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(.5f, .5f),
                spriteID = existing.FirstOrDefault(rect => rect.name == name)?.spriteID ?? GUID.Generate()
            }).ToArray();
            provider.SetSpriteRects(rects);
            var nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameProvider.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static void ImportSingle(string file)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Root + file);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        // A simple native UI primitive, not a painted asset: 9-sliced rounded white card.
        private static void CreateFrame()
        {
            var path = Root + "frame.png";
            if (!File.Exists(path))
            {
                const int size = 64;
                const float radius = 14;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color[size * size];
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var delta = new Vector2(Mathf.Max(Mathf.Abs(x - 31.5f) - (32 - radius), 0), Mathf.Max(Mathf.Abs(y - 31.5f) - (32 - radius), 0));
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius - delta.magnitude));
                }
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            ImportSingle("frame.png");
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.spriteBorder = new Vector4(16, 16, 16, 16);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
