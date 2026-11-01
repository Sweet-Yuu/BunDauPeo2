using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEditor;
using UnityEngine;

namespace BunDauPeo.Tests
{
    // Dựng SO của Restaurant qua SerializedObject, giống TestData; giá trị mặc định là số khởi điểm của spec.
    internal static class RestaurantTestData
    {
        public static DishDefinition Dish(long price = 20, float cook = 5)
        {
            var dish = ScriptableObject.CreateInstance<DishDefinition>();
            TestData.Edit(dish, so =>
            {
                so.FindProperty("_basePrice").longValue = price;
                so.FindProperty("_cookSeconds").floatValue = cook;
            });
            return dish;
        }

        public static CustomerDefinition Customer(float weight = 1, float patience = 40, float pay = 1)
        {
            var customer = ScriptableObject.CreateInstance<CustomerDefinition>();
            TestData.Edit(customer, so =>
            {
                so.FindProperty("_spawnWeight").floatValue = weight;
                so.FindProperty("_patienceSeconds").floatValue = patience;
                so.FindProperty("_payMultiplier").floatValue = pay;
            });
            return customer;
        }

        public static IncidentDefinition Incident(StaffRole role, float weight = 1)
        {
            var incident = ScriptableObject.CreateInstance<IncidentDefinition>();
            TestData.Edit(incident, so =>
            {
                so.FindProperty("_role").enumValueIndex = (int)role;
                so.FindProperty("_weight").floatValue = weight;
            });
            return incident;
        }

        public static UpgradeLevelDef Level(float value, long coin, long material) => new UpgradeLevelDef
        {
            Value = value,
            Cost = new[]
            {
                new CurrencyAmount(CurrencyType.Coin, coin),
                new CurrencyAmount(CurrencyType.Material, material),
            },
        };

        public static UpgradeDefinition Upgrade(string id, UpgradeEffect effect, params UpgradeLevelDef[] levels)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            TestData.Edit(upgrade, so =>
            {
                so.FindProperty("_id").stringValue = id;
                so.FindProperty("_effect").enumValueIndex = (int)effect;
                var array = so.FindProperty("_levels");
                array.arraySize = levels.Length;
                for (var i = 0; i < levels.Length; i++)
                {
                    var element = array.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("Value").floatValue = levels[i].Value;
                    var cost = element.FindPropertyRelative("Cost");
                    var lines = levels[i].Cost ?? new CurrencyAmount[0];
                    cost.arraySize = lines.Length;
                    for (var j = 0; j < lines.Length; j++)
                    {
                        var line = cost.GetArrayElementAtIndex(j);
                        line.FindPropertyRelative("Type").enumValueIndex = (int)lines[j].Type;
                        line.FindPropertyRelative("Amount").longValue = lines[j].Amount;
                    }
                }
            });
            return upgrade;
        }

        public static UpgradeCatalog UpgradeCatalog(params UpgradeDefinition[] items)
        {
            var catalog = ScriptableObject.CreateInstance<UpgradeCatalog>();
            SetList(catalog, "_items", items);
            return catalog;
        }

        // Một món 20 Coin nấu 5 giây, một khách ×1, ba sự cố mỗi vai một cái, catalog nâng cấp rỗng nếu không truyền; còn lại là số của spec.
        public static RestaurantConfig Config(UpgradeCatalog upgrades = null)
        {
            var config = ScriptableObject.CreateInstance<RestaurantConfig>();
            SetList(config, "_dishes", Dish());
            SetList(config, "_customers", Customer());
            SetList(config, "_incidents", Incident(StaffRole.Cook), Incident(StaffRole.Server), Incident(StaffRole.Cashier));
            TestData.SetRef(config, "_upgrades", upgrades != null ? upgrades : UpgradeCatalog());
            return config;
        }

        public static CharacterDefinition Worker(string id, int cook, int serve, int cashier)
        {
            var worker = TestData.Character(id);
            TestData.Edit(worker, so =>
            {
                var stats = so.FindProperty("_restaurantStats");
                stats.FindPropertyRelative("Cooking").intValue = cook;
                stats.FindPropertyRelative("Serving").intValue = serve;
                stats.FindPropertyRelative("Cashier").intValue = cashier;
            });
            return worker;
        }

        // Hồ sơ với nhân viên giả (id "cook0", "serve0"...), chỉ số theo từng vai; số ô không bị cắt.
        public static RestaurantProfile Profile(RestaurantConfig config, int[] cook, int[] serve, int[] cashier, PlayerSave save = null)
        {
            save ??= new PlayerSave();
            var roster = new Roster(save, TestData.Catalog());
            return RestaurantProfile.Build(config, save, roster)
                .WithWorkers(StaffRole.Cook, Crew("cook", cook))
                .WithWorkers(StaffRole.Server, Crew("serve", serve))
                .WithWorkers(StaffRole.Cashier, Crew("cashier", cashier));
        }

        private static WorkerInfo[] Crew(string prefix, int[] stats)
        {
            var crew = new WorkerInfo[stats.Length];
            for (var i = 0; i < stats.Length; i++) crew[i] = new WorkerInfo($"{prefix}{i}", stats[i]);
            return crew;
        }

        public static void SetList(Object target, string field, params Object[] items)
        {
            TestData.Edit(target, so =>
            {
                var array = so.FindProperty(field);
                array.arraySize = items.Length;
                for (var i = 0; i < items.Length; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            });
        }
    }
}
