using BunDauPeo.Characters;
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

        public static CharacterDefinition Character(string id)
        {
            var character = ScriptableObject.CreateInstance<CharacterDefinition>();
            var so = new SerializedObject(character);
            so.FindProperty("_id").stringValue = id;
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

        public static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
