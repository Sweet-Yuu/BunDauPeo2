using System;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using UnityEditor;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Số khởi điểm của CombatConfig lấy từ field initializer, đúng bảng spec 3.4; edit chỉ ghi đè cái test cần.
    internal static class CombatTestData
    {
        public static CombatConfig Config(Action<SerializedObject> edit = null)
        {
            var config = ScriptableObject.CreateInstance<CombatConfig>();
            TestData.Edit(config, so =>
            {
                so.FindProperty("_stages").objectReferenceValue = Catalog(Stage("st_a", new[] { Enemy() }));
                edit?.Invoke(so);
            });
            return config;
        }

        public static SkillDefinition Skill(SkillKind kind, SkillTarget target = SkillTarget.SingleEnemy,
            SkillEffect effect = SkillEffect.Damage, float power = 1f, int cooldown = 0, int energy = 0)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            TestData.Edit(skill, so =>
            {
                so.FindProperty("_kind").enumValueIndex = (int)kind;
                so.FindProperty("_target").enumValueIndex = (int)target;
                so.FindProperty("_effect").enumValueIndex = (int)effect;
                so.FindProperty("_power").floatValue = power;
                so.FindProperty("_cooldown").intValue = cooldown;
                so.FindProperty("_energyCost").intValue = energy;
            });
            return skill;
        }

        public static EnemyDefinition Enemy(string name = "e", int hp = 100, int attack = 10, int defense = 0, int speed = 50,
            SkillDefinition skill = null, bool boss = false)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            TestData.Edit(enemy, so =>
            {
                so.FindProperty("_displayName").stringValue = name;
                var stats = so.FindProperty("_stats");
                stats.FindPropertyRelative("HP").intValue = hp;
                stats.FindPropertyRelative("Attack").intValue = attack;
                stats.FindPropertyRelative("Defense").intValue = defense;
                stats.FindPropertyRelative("Speed").intValue = speed;
                so.FindProperty("_basicAttack").objectReferenceValue = Skill(SkillKind.Basic);
                so.FindProperty("_skill").objectReferenceValue = skill;
                so.FindProperty("_isBoss").boolValue = boss;
            });
            return enemy;
        }

        // Màn Battle: thể lực 10, lần đầu 30 Material + 150 Gem, đánh lại 30 Material + 30 Gem.
        public static StageDefinition Stage(string id, params EnemyDefinition[][] waves)
        {
            var stage = ScriptableObject.CreateInstance<StageDefinition>();
            TestData.Edit(stage, so =>
            {
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_displayName").stringValue = id;
                so.FindProperty("_kind").enumValueIndex = (int)StageKind.Battle;
                so.FindProperty("_staminaCost").intValue = 10;
                var array = so.FindProperty("_waves");
                array.arraySize = waves.Length;
                for (var i = 0; i < waves.Length; i++)
                {
                    var enemies = array.GetArrayElementAtIndex(i).FindPropertyRelative("_enemies");
                    enemies.arraySize = waves[i].Length;
                    for (var j = 0; j < waves[i].Length; j++)
                        enemies.GetArrayElementAtIndex(j).objectReferenceValue = waves[i][j];
                }

                SetRewards(so.FindProperty("_firstClearReward"),
                    new CurrencyAmount(CurrencyType.Material, 30), new CurrencyAmount(CurrencyType.Gem, 150));
                SetRewards(so.FindProperty("_repeatReward"),
                    new CurrencyAmount(CurrencyType.Material, 30), new CurrencyAmount(CurrencyType.Gem, 30));
            });
            return stage;
        }

        public static StageDefinition Story(string id)
        {
            var stage = ScriptableObject.CreateInstance<StageDefinition>();
            TestData.Edit(stage, so =>
            {
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_displayName").stringValue = id;
                so.FindProperty("_kind").enumValueIndex = (int)StageKind.Story;
            });
            return stage;
        }

        public static StageCatalog Catalog(params StageDefinition[] stages)
        {
            var catalog = ScriptableObject.CreateInstance<StageCatalog>();
            TestData.Edit(catalog, so =>
            {
                var array = so.FindProperty("_items");
                array.arraySize = stages.Length;
                for (var i = 0; i < stages.Length; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
            });
            return catalog;
        }

        public static void SetRewards(SerializedProperty array, params CurrencyAmount[] lines)
        {
            array.arraySize = lines.Length;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = array.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("Type").enumValueIndex = (int)lines[i].Type;
                line.FindPropertyRelative("Amount").longValue = lines[i].Amount;
            }
        }
    }
}
