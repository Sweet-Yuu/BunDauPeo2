using System.Collections.Generic;
using System.IO;
using System.Text;
using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Gacha;
using BunDauPeo.Rules;
using UnityEditor;
using UnityEngine;

namespace BunDauPeo.EditorTools
{
    // Xuất dữ liệu cân bằng cho server (GameData.json). Test CommittedJson_* so file đã commit với SO thật.
    public static class ServerDataExport
    {
        public const string OutputPath = "ServerModule/BunDauPeoServer/GameData.json";

        public static string FullOutputPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputPath));

        // Nạp đúng các asset mà ProjectSetup.WireBootScene gán vào GameBootstrap (cùng đường dẫn, không mở scene nên không làm bẩn scene đang mở).
        public static GameData BuildFromProject(out List<string> errors)
        {
            var characters = Load<CharacterCatalog>(ProjectSetup.CatalogPath);
            var banners = Load<GachaBannerCatalog>(ProjectSetup.BannerCatalogPath);
            var stars = Load<StarConfig>(ProjectSetup.StarConfigPath);
            var combat = Load<CombatConfig>(ProjectSetup.CombatConfigPath);
            var newGame = Load<NewGameConfig>(ProjectSetup.NewGameConfigPath);
            errors = GameDataBuilder.Validate(characters, banners, stars, combat, newGame);
            return GameDataBuilder.Build(characters, banners, stars, combat, newGame);
        }

        [MenuItem("Tools/BunDauPeo/Export Server Data")]
        public static void Export()
        {
            var data = BuildFromProject(out var errors);
            if (errors.Count > 0)
            {
                foreach (var error in errors) Debug.LogError($"ServerDataExport: {error}");
                return;
            }

            // UTF-8 không BOM, không xuống dòng cuối: test so từng ký tự.
            File.WriteAllText(FullOutputPath, GameDataBuilder.ToJson(data), new UTF8Encoding(false));
            Debug.Log($"ServerDataExport: wrote {OutputPath}");
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new FileNotFoundException($"ServerDataExport: missing {typeof(T).Name} at {path}");
            return asset;
        }
    }
}
