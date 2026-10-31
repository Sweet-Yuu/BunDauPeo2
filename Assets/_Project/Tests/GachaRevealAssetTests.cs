using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace BunDauPeo.Tests
{
    // GUID lấy từ .meta của Project_04; đổi GUID nghĩa là clip và prefab không còn tham chiếu được nhau.
    public class GachaRevealAssetTests
    {
        private const string Summoning = "Assets/_Project/Gacha/Animations/Summoning/";
        private const string Cutscenes = "Assets/_Project/Gacha/Prefabs/Cutscenes/";
        private const string Art = "Assets/_Project/Gacha/Art/Summoning/";

        private static readonly (string Path, string Guid)[] Copied =
        {
            (Summoning + "CommonSummon.anim", "ad3d68231e9fbe34296679c68a094c80"),
            (Summoning + "LegendarySummon.anim", "268b08577020b744e9f70366c7517a2f"),
            (Summoning + "CommonSummon.controller", "eb4df6125cf84fd439f1179c823ef1e7"),
            (Summoning + "LegendarySummon.controller", "30b38c69ead93714aa223ead3ad9cd8b"),
            (Art + "Spark.png", "da69432c42035ea49bf93e416432139e"),
            (Art + "Ring.png", "829805906caf53643a6c3fdfe23fa60b"),
            (Art + "Glow.png", "a208deb31132cca4d980f5c1cd2e7649"),
            (Cutscenes + "CommonSummon.prefab", null),
            (Cutscenes + "LegendarySummon.prefab", null),
        };

        [Test]
        public void CopiedAssets_KeepSourceGuids()
        {
            foreach (var (path, guid) in Copied)
            {
                if (guid != null) Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path), path);
                else Assert.IsNotEmpty(AssetDatabase.AssetPathToGUID(path), path);
            }
        }

        [Test]
        public void CutscenePrefabs_ReferencesResolve()
        {
            foreach (var prefab in new[] { Cutscenes + "CommonSummon.prefab", Cutscenes + "LegendarySummon.prefab" })
            {
                var text = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), prefab));
                var guids = new HashSet<string>();
                foreach (Match m in Regex.Matches(text, "guid: ([0-9a-f]{32})")) guids.Add(m.Groups[1].Value);
                Assert.IsNotEmpty(guids, prefab);
                foreach (var guid in guids)
                    Assert.IsNotEmpty(AssetDatabase.GUIDToAssetPath(guid), $"{prefab} references missing guid {guid}");
            }
        }
    }
}
