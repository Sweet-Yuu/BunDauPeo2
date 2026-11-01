using System;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Restaurant
{
    // Nối mô phỏng với ví, Roster và save. Thuần C#: RestaurantRunner chỉ gọi Update mỗi khung.
    public sealed class RestaurantService
    {
        private readonly RestaurantConfig _config;
        private readonly PlayerSave _save;
        private readonly Wallet _wallet;
        private readonly Roster _roster;
        private readonly Func<bool> _persist;
        // double: cộng 0,1f nhiều lần bằng float sẽ lệch khỏi mốc 60 giây.
        private double _autosaveTimer;

        public RestaurantService(RestaurantConfig config, PlayerSave save, Wallet wallet, Roster roster, System.Random random, Func<bool> persist)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));

            var errors = config.Validate();
            IsRunning = errors.Count == 0;
            if (!IsRunning) Debug.LogError($"{nameof(RestaurantService)}: invalid config, restaurant not running:\n- {string.Join("\n- ", errors)}");

            Simulation = new RestaurantSimulation(config, random);
            Staffing = new StaffingService(roster, () => Profile);
            RebuildProfile();

            Simulation.CustomerPaid += OnCustomerPaid;
            roster.Changed += OnRosterChanged;
        }

        public bool IsRunning { get; }
        public RestaurantSimulation Simulation { get; }
        public StaffingService Staffing { get; }
        public RestaurantProfile Profile { get; private set; }
        public IncomeEstimate Estimate { get; private set; }
        public bool IsPresent { get; private set; }

        public event Action ProfileChanged;

        public void SetPresent(bool present) => IsPresent = present;

        public void Update(float unscaledDt)
        {
            if (!IsRunning) return;

            Simulation.Tick(unscaledDt);
            _autosaveTimer += unscaledDt;
            if (_autosaveTimer < _config.AutosaveSeconds) return;

            // Lưu lỗi thì thử lại ở chu kỳ sau thay vì mỗi khung.
            _autosaveTimer -= _config.AutosaveSeconds;
            try
            {
                if (!_persist()) Debug.LogWarning($"{nameof(RestaurantService)}: autosave did not complete.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{nameof(RestaurantService)}: autosave failed: {e.Message}");
            }
        }

        // Dữ liệu trong PlayerSave đã bị ghi đè trước khi gọi; ở đây chỉ xoá trạng thái chạy.
        public void ResetRuntime()
        {
            Simulation.Reset();
            _autosaveTimer = 0;
        }

        private void OnRosterChanged()
        {
            RebuildProfile();
            ProfileChanged.SafeInvoke();
        }

        private void RebuildProfile()
        {
            Profile = RestaurantProfile.Build(_config, _save, _roster);
            Simulation.SetProfile(Profile);
            // Cấu hình lỗi có thể làm công thức chia cho 0 hoặc duyệt danh sách rỗng.
            Estimate = IsRunning ? IncomeCalculator.Estimate(_config, Profile) : default;
        }

        private void OnCustomerPaid(int customerId, long coins) => _wallet.Add(CurrencyType.Coin, coins);
    }
}
