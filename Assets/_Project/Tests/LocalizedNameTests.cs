using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using BunDauPeo.Restaurant;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BunDauPeo.Tests
{
    public class LocalizedNameTests
    {
        private static readonly Type[] NamedTypes =
        {
            typeof(CharacterDefinition), typeof(SkillDefinition), typeof(GachaBannerDefinition), typeof(DishDefinition),
            typeof(CustomerDefinition), typeof(IncidentDefinition), typeof(UpgradeDefinition), typeof(EnemyDefinition), typeof(StageDefinition),
        };

        [TearDown]
        public void TearDown() => GameLanguage.Current = Language.Vi;

        private static IEnumerable<Object> Assets(Type type) =>
            AssetDatabase.FindAssets($"t:{type.Name}", new[] { "Assets/_Project" })
                .Select(guid => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type));

        private static string English(Object asset)
        {
            var property = new SerializedObject(asset).FindProperty("_displayNameEn");
            return property == null ? null : property.stringValue;
        }

        [Test]
        public void LocalizedNames_AllCatalogAssetsHaveEnglish()
        {
            foreach (var type in NamedTypes)
            {
                var assets = Assets(type).ToList();
                Assert.IsNotEmpty(assets, $"no {type.Name} asset found (test would pass for nothing)");
                foreach (var asset in assets)
                    Assert.IsFalse(string.IsNullOrWhiteSpace(English(asset)), $"{type.Name} {asset.name}: _displayNameEn is empty");
            }
        }

        [Test]
        public void LocalizedNames_CharactersAndDishesHaveNoDiacritics()
        {
            var assets = Assets(typeof(CharacterDefinition)).Concat(Assets(typeof(DishDefinition))).ToList();
            Assert.IsNotEmpty(assets);
            foreach (var asset in assets)
            {
                var english = English(asset);
                Assert.IsFalse(string.IsNullOrWhiteSpace(english), $"{asset.name}: _displayNameEn is empty");
                Assert.IsFalse(Regex.IsMatch(english, "[À-ỹĐđ]"), $"{asset.name}: '{english}' has diacritics");
            }
        }

        [Test]
        public void DisplayName_FollowsLanguage_AndFallsBack()
        {
            var dish = ScriptableObject.CreateInstance<DishDefinition>();
            try
            {
                var so = new SerializedObject(dish);
                so.FindProperty("_displayName").stringValue = "Món";
                so.FindProperty("_displayNameEn").stringValue = "Dish";
                so.ApplyModifiedPropertiesWithoutUndo();

                GameLanguage.Current = Language.Vi;
                Assert.AreEqual("Món", dish.DisplayName);
                GameLanguage.Current = Language.En;
                Assert.AreEqual("Dish", dish.DisplayName);

                so.FindProperty("_displayNameEn").stringValue = "";
                so.ApplyModifiedPropertiesWithoutUndo();
                Assert.AreEqual("Món", dish.DisplayName);
            }
            finally
            {
                Object.DestroyImmediate(dish);
            }
        }
    }
}
