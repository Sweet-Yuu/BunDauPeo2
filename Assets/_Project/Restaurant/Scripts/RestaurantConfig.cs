using System;
using System.Collections.Generic;
using UnityEngine;

namespace BunDauPeo.Restaurant
{
    // Số khởi điểm theo spec Restaurant 3.3; chỉnh trong Inspector, không cần sửa code.
    [CreateAssetMenu(menuName = "BunDauPeo/Restaurant/Config")]
    public class RestaurantConfig : ScriptableObject
    {
        [SerializeField] private DishDefinition[] _dishes = Array.Empty<DishDefinition>();
        [SerializeField] private CustomerDefinition[] _customers = Array.Empty<CustomerDefinition>();
        [SerializeField] private IncidentDefinition[] _incidents = Array.Empty<IncidentDefinition>();
        [SerializeField] private UpgradeCatalog _upgrades;

        [Header("Quán")]
        [SerializeField] private int _baseTables = 3;
        [SerializeField] private int _baseCookSlots = 1;
        [SerializeField] private int _baseServeSlots = 1;
        [SerializeField] private int _baseCashierSlots = 1;
        // Trung bình; khoảng cách thực tế = giá trị × ngẫu nhiên đều từ 0,5 đến 1,5.
        [SerializeField] private float _baseArrivalInterval = 15f;
        // Hàng chờ bàn tối đa; khách đến khi hàng đầy thì không vào và tính là bỏ về.
        [SerializeField] private int _maxWaitingLine = 4;

        [Header("Thời gian gốc")]
        [SerializeField] private float _serveSeconds = 4f;
        [SerializeField] private float _eatSeconds = 8f;
        [SerializeField] private float _cashierSeconds = 3f;

        [Header("Chất lượng")]
        [SerializeField] private float _attendanceMax = 50f;
        [SerializeField] private float _incidentMax = 50f;
        [SerializeField] private float _dailyGoalSeconds = 300f;

        [Header("Sự cố")]
        [SerializeField] private float _incidentSpawnMin = 60f;
        [SerializeField] private float _incidentSpawnMax = 120f;
        [SerializeField] private float _incidentFastSeconds = 8f;
        [SerializeField] private float _incidentLateSeconds = 20f;

        [Header("AFK")]
        [SerializeField] private float _afkMaxHours = 12f;
        [SerializeField] private float _afkFactorMin = 0.1f;
        [SerializeField] private float _afkFactorMax = 0.5f;
        [SerializeField] private float _afkPopupMinSeconds = 300f;

        [Header("Lưu")]
        [SerializeField] private float _autosaveSeconds = 60f;

        public IReadOnlyList<DishDefinition> Dishes => _dishes;
        public IReadOnlyList<CustomerDefinition> Customers => _customers;
        public IReadOnlyList<IncidentDefinition> Incidents => _incidents;
        public UpgradeCatalog Upgrades => _upgrades;
        public int BaseTables => _baseTables;
        public int BaseCookSlots => _baseCookSlots;
        public int BaseServeSlots => _baseServeSlots;
        public int BaseCashierSlots => _baseCashierSlots;
        public float BaseArrivalInterval => _baseArrivalInterval;
        public int MaxWaitingLine => _maxWaitingLine;
        public float ServeSeconds => _serveSeconds;
        public float EatSeconds => _eatSeconds;
        public float CashierSeconds => _cashierSeconds;
        public float AttendanceMax => _attendanceMax;
        public float IncidentMax => _incidentMax;
        public float DailyGoalSeconds => _dailyGoalSeconds;
        public float IncidentSpawnMin => _incidentSpawnMin;
        public float IncidentSpawnMax => _incidentSpawnMax;
        public float IncidentFastSeconds => _incidentFastSeconds;
        public float IncidentLateSeconds => _incidentLateSeconds;
        public float AfkMaxHours => _afkMaxHours;
        public float AfkFactorMin => _afkFactorMin;
        public float AfkFactorMax => _afkFactorMax;
        public float AfkPopupMinSeconds => _afkPopupMinSeconds;
        public float AutosaveSeconds => _autosaveSeconds;

        // Mỗi lỗi bắt đầu bằng tên field ("Dishes: ...", "BaseTables: ...").
        public List<string> Validate()
        {
            var errors = new List<string>();

            CheckList(errors, "Dishes", _dishes, (dish, prefix) =>
            {
                if (dish.BasePrice < 1) errors.Add($"{prefix}.BasePrice: must be >= 1");
                if (!(dish.CookSeconds > 0f)) errors.Add($"{prefix}.CookSeconds: must be > 0");
            });
            CheckList(errors, "Customers", _customers, (customer, prefix) =>
            {
                if (!(customer.SpawnWeight > 0f)) errors.Add($"{prefix}.SpawnWeight: must be > 0");
                if (!(customer.PatienceSeconds > 0f)) errors.Add($"{prefix}.PatienceSeconds: must be > 0");
                if (!(customer.PayMultiplier > 0f)) errors.Add($"{prefix}.PayMultiplier: must be > 0");
            });
            CheckList(errors, "Incidents", _incidents, null);

            Positive(errors, "BaseTables", _baseTables);
            Positive(errors, "BaseCookSlots", _baseCookSlots);
            Positive(errors, "BaseServeSlots", _baseServeSlots);
            Positive(errors, "BaseCashierSlots", _baseCashierSlots);
            Positive(errors, "BaseArrivalInterval", _baseArrivalInterval);
            Positive(errors, "ServeSeconds", _serveSeconds);
            Positive(errors, "EatSeconds", _eatSeconds);
            Positive(errors, "CashierSeconds", _cashierSeconds);

            if (Mathf.Abs(_attendanceMax + _incidentMax - 100f) > 0.0001f)
                errors.Add("AttendanceMax + IncidentMax: must add up to 100");

            if (_afkFactorMin < 0f) errors.Add("AfkFactorMin: must be >= 0");
            else if (_afkFactorMin > _afkFactorMax) errors.Add("AfkFactorMin: must be <= AfkFactorMax");
            if (_afkFactorMax >= 1f) errors.Add("AfkFactorMax: must be < 1");
            Positive(errors, "AfkMaxHours", _afkMaxHours);

            if (_incidentSpawnMin > _incidentSpawnMax) errors.Add("IncidentSpawnMin: must be <= IncidentSpawnMax");
            if (_incidentFastSeconds >= _incidentLateSeconds) errors.Add("IncidentFastSeconds: must be < IncidentLateSeconds");

            Positive(errors, "AutosaveSeconds", _autosaveSeconds);
            // 0 ở ba field này lọt qua mà làm chất lượng NaN, mọi khách bỏ về, hoặc popup mất số giây vắng.
            Positive(errors, "DailyGoalSeconds", _dailyGoalSeconds);
            Positive(errors, "AfkPopupMinSeconds", _afkPopupMinSeconds);
            if (_maxWaitingLine < 1) errors.Add("MaxWaitingLine: must be >= 1");

            CheckUpgrades(errors);
            return errors;
        }

        private static void Positive(List<string> errors, string field, float value)
        {
            if (!(value > 0f)) errors.Add($"{field}: must be > 0");
        }

        private static void CheckList<T>(List<string> errors, string field, T[] items, Action<T, string> checkItem)
            where T : UnityEngine.Object
        {
            if (items == null || items.Length == 0)
            {
                errors.Add($"{field}: must not be empty");
                return;
            }

            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] == null) errors.Add($"{field}[{i}]: missing reference");
                else checkItem?.Invoke(items[i], $"{field}[{i}]");
            }
        }

        private void CheckUpgrades(List<string> errors)
        {
            if (_upgrades == null)
            {
                errors.Add("Upgrades: missing catalog");
                return;
            }

            foreach (var error in _upgrades.Validate()) errors.Add($"Upgrades: {error}");

            var seen = new HashSet<UpgradeEffect>();
            for (var i = 0; i < _upgrades.Items.Count; i++)
            {
                var upgrade = _upgrades.Items[i];
                if (upgrade == null) continue;
                if (!seen.Add(upgrade.Effect)) errors.Add($"Upgrades: more than one upgrade has Effect {upgrade.Effect}");
                foreach (var error in upgrade.Validate()) errors.Add($"Upgrades[{i}] ({upgrade.Id}): {error}");
            }
        }

        private void OnValidate()
        {
            foreach (var error in Validate()) Debug.LogWarning($"{name} - {error}", this);
        }
    }
}
