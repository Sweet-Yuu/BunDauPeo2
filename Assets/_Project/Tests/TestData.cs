using BunDauPeo.Boot;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Gacha;
using UnityEditor;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Gán field private qua SerializedObject để không phải thêm setter vào code game chỉ vì test.
    internal static class TestData
    {
        public static SkillDefinition Skill(SkillKind kind)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var so = new SerializedObject(skill);
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.ApplyModifiedPropertiesWithoutUndo();
            return skill;
        }

        public static CharacterDefinition Character(string id) => Character(id, Rarity.R);

        public static CharacterDefinition Character(string id, Rarity rarity)
        {
            var character = ScriptableObject.CreateInstance<CharacterDefinition>();
            var so = new SerializedObject(character);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_rarity").enumValueIndex = (int)rarity;
            so.FindProperty("_stars").intValue = 1;
            so.FindProperty("_basicAttack").objectReferenceValue = Skill(SkillKind.Basic);
            so.FindProperty("_skill").objectReferenceValue = Skill(SkillKind.Skill);
            so.FindProperty("_ultimate").objectReferenceValue = Skill(SkillKind.Ultimate);
            so.ApplyModifiedPropertiesWithoutUndo();
            return character;
        }

        public static CharacterCatalog Catalog(params CharacterDefinition[] items)
        {
            var catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            var so = new SerializedObject(catalog);
            var array = so.FindProperty("_items");
            array.arraySize = items.Length;
            for (var i = 0; i < items.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        public static NewGameConfig Config(CurrencyAmount[] currency, params CharacterDefinition[] starters)
        {
            var config = ScriptableObject.CreateInstance<NewGameConfig>();
            var so = new SerializedObject(config);
            var money = so.FindProperty("_startingCurrency");
            money.arraySize = currency.Length;
            for (var i = 0; i < currency.Length; i++)
            {
                var element = money.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Type").enumValueIndex = (int)currency[i].Type;
                element.FindPropertyRelative("Amount").longValue = currency[i].Amount;
            }

            var list = so.FindProperty("_starterCharacters");
            list.arraySize = starters.Length;
            for (var i = 0; i < starters.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = starters[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        public static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Edit(Object target, System.Action<SerializedObject> set)
        {
            var so = new SerializedObject(target);
            set(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Giá trị khởi điểm của banner (chi phí, tỉ lệ, pity...) lấy từ field initializer, đúng bảng ở spec 3.3.
        public static GachaBannerDefinition Banner(string id, params CharacterDefinition[] characters)
        {
            var banner = ScriptableObject.CreateInstance<GachaBannerDefinition>();
            Edit(banner, so =>
            {
                so.FindProperty("_id").stringValue = id;
                var array = so.FindProperty("_characters");
                array.arraySize = characters.Length;
                for (var i = 0; i < characters.Length; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = characters[i];
            });
            return banner;
        }

        public static GachaBannerCatalog GachaCatalog(params GachaBannerDefinition[] banners)
        {
            var catalog = ScriptableObject.CreateInstance<GachaBannerCatalog>();
            Edit(catalog, so =>
            {
                var array = so.FindProperty("_items");
                array.arraySize = banners.Length;
                for (var i = 0; i < banners.Length; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = banners[i];
            });
            return catalog;
        }

        public static CharacterDefinition[] GachaCharacters() => new[]
        {
            Character("r0", Rarity.R), Character("r1", Rarity.R), Character("r2", Rarity.R),
            Character("s0", Rarity.SR), Character("s1", Rarity.SR),
            Character("x0", Rarity.SSR),
        };
    }
}
