using BunDauPeo.Restaurant;
using System;
using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Core;

namespace Hoang.Remake.RestaurantRuntime
{
    public readonly struct WorkerInfo
    {
        public WorkerInfo(string characterId, int stat)
        {
            CharacterId = characterId;
            Stat = stat;
        }

        public string CharacterId { get; }
        public int Stat { get; }
    }

    // Điểm duy nhất mô phỏng và công thức thu nhập đọc: nguồn chỉ số mới chỉ cần đưa thêm giá trị vào đây.
    public readonly struct RestaurantProfile
    {
        private static readonly IReadOnlyList<WorkerInfo> None = Array.Empty<WorkerInfo>();

        // Chỉ số theo StaffRole; null khi là default(RestaurantProfile).
        private readonly IReadOnlyList<WorkerInfo>[] _workers;

        private RestaurantProfile(int tables, int cookSlots, int serveSlots, int cashierSlots,
            float arrivalIntervalMultiplier, float priceMultiplier, IReadOnlyList<WorkerInfo>[] workers,
            float cookingSpeed = 1, float patience = 1, float ingredientReduction = 0, int cleanliness = 0)
        {
            Tables = tables;
            CookSlots = cookSlots;
            ServeSlots = serveSlots;
            CashierSlots = cashierSlots;
            ArrivalIntervalMultiplier = arrivalIntervalMultiplier;
            PriceMultiplier = priceMultiplier;
            CookingSpeedMultiplier = cookingSpeed;
            PatienceMultiplier = patience;
            IngredientCostReduction = ingredientReduction;
            CleanlinessBonus = cleanliness;
            _workers = workers;
        }

        public int Tables { get; }
        public int CookSlots { get; }
        public int ServeSlots { get; }
        public int CashierSlots { get; }
        public float ArrivalIntervalMultiplier { get; }
        public float PriceMultiplier { get; }
        public float CookingSpeedMultiplier { get; }
        public float PatienceMultiplier { get; }
        public float IngredientCostReduction { get; }
        public int CleanlinessBonus { get; }

        public RestaurantProfile WithBuffs(float cooking, float revenue, float patience, float reduction, int cleanliness) =>
            new RestaurantProfile(Tables, CookSlots, ServeSlots, CashierSlots, ArrivalIntervalMultiplier,
                (float)Math.Min(float.MaxValue, (double)PriceMultiplier * Math.Max(1, revenue)), _workers, Math.Max(1, cooking), Math.Max(1, patience),
                Math.Clamp(reduction, 0, 0.8f), Math.Max(0, cleanliness));

        public IReadOnlyList<WorkerInfo> Workers(StaffRole role) => _workers?[(int)role] ?? None;

        public int Slots(StaffRole role) => role switch
        {
            StaffRole.Cook => CookSlots,
            StaffRole.Server => ServeSlots,
            _ => CashierSlots,
        };

        public static RestaurantProfile Build(RestaurantConfig config, PlayerSave save, Roster roster)
        {
            var profile = new RestaurantProfile(
                (int)ValueOf(config, save, UpgradeEffect.Tables, config.BaseTables),
                (int)ValueOf(config, save, UpgradeEffect.CookSlots, config.BaseCookSlots),
                (int)ValueOf(config, save, UpgradeEffect.ServeSlots, config.BaseServeSlots),
                (int)ValueOf(config, save, UpgradeEffect.CashierSlots, config.BaseCashierSlots),
                ValueOf(config, save, UpgradeEffect.ArrivalInterval, 1f),
                ValueOf(config, save, UpgradeEffect.DishPrice, 1f),
                null);

            var workers = new IReadOnlyList<WorkerInfo>[3];
            foreach (var role in new[] { StaffRole.Cook, StaffRole.Server, StaffRole.Cashier })
            {
                var list = new List<WorkerInfo>();
                var slots = profile.Slots(role);
                // Người thừa bị bỏ qua chứ không bị gỡ khỏi save: nâng cấp ô sau này sẽ dùng lại họ.
                foreach (var character in roster.GetStaff(role))
                {
                    if (list.Count >= slots) break;
                    list.Add(new WorkerInfo(character.Id, StatOf(character, role)));
                }

                workers[(int)role] = list;
            }

            return profile.WithWorkersArray(workers);
        }

        // Bản giả định (UI tính "→ ±X Coin/giây" khi chọn nhân viên); không cắt theo số ô.
        public RestaurantProfile WithWorkers(StaffRole role, IReadOnlyList<WorkerInfo> workers)
        {
            var copy = new IReadOnlyList<WorkerInfo>[3];
            for (var i = 0; i < copy.Length; i++) copy[i] = _workers?[i] ?? None;
            copy[(int)role] = workers ?? None;
            return WithWorkersArray(copy);
        }

        private RestaurantProfile WithWorkersArray(IReadOnlyList<WorkerInfo>[] workers) =>
            new RestaurantProfile(Tables, CookSlots, ServeSlots, CashierSlots, ArrivalIntervalMultiplier, PriceMultiplier,
                workers, CookingSpeedMultiplier, PatienceMultiplier, IngredientCostReduction, CleanlinessBonus);

        public static int StatOf(CharacterDefinition character, StaffRole role) => role switch
        {
            StaffRole.Cook => character.RestaurantStats.Cooking,
            StaffRole.Server => character.RestaurantStats.Serving,
            _ => character.RestaurantStats.Cashier,
        };

        // Cấp 0 hoặc không có dòng: giá trị gốc. Cấp vượt số cấp của SO: lấy cấp cao nhất, không đổi save.
        private static float ValueOf(RestaurantConfig config, PlayerSave save, UpgradeEffect effect, float baseValue)
        {
            if (config.Upgrades == null) return baseValue;
            foreach (var upgrade in config.Upgrades.Items)
            {
                if (upgrade == null || upgrade.Effect != effect) continue;
                var level = 0;
                foreach (var entry in save.Upgrades)
                    if (entry.UpgradeId == upgrade.Id && entry.Level > level) level = entry.Level;
                if (level <= 0 || upgrade.MaxLevel == 0) return baseValue;
                return upgrade.Levels[Math.Min(level, upgrade.MaxLevel) - 1].Value;
            }

            return baseValue;
        }
    }
}

