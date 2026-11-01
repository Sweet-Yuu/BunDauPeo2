using System;
using BunDauPeo.Core;

namespace BunDauPeo.Restaurant
{
    public readonly struct ActiveIncident
    {
        public ActiveIncident(IncidentDefinition definition, StaffRole role, float elapsed, bool canResolve)
        {
            Definition = definition;
            Role = role;
            Elapsed = elapsed;
            CanResolve = canResolve;
        }

        public IncidentDefinition Definition { get; }
        public StaffRole Role { get; }
        public float Elapsed { get; }

        // Vai của sự cố đang có người; khi false thì Resolve() từ chối.
        public bool CanResolve { get; }
    }

    // Sự cố loại A (spec 4.5). Chỉ chạy khi người chơi có mặt; thuần C#, RNG và đồng hồ do người gọi tiêm vào.
    public sealed class IncidentDirector
    {
        private readonly RestaurantConfig _config;
        private readonly Random _random;
        private readonly RestaurantSimulation _simulation;
        private readonly RestaurantQuality _quality;
        private readonly Func<StaffRole, bool> _roleHasStaff;

        private bool _present;
        private bool _spawnAllowed = true;
        private IncidentDefinition _active;
        private double _elapsed;
        // null = chưa đặt. Đếm ngược chạy cả khi không được sinh và dừng ở 0 để sinh ngay khi được mở lại.
        private double? _spawnTimer;

        public IncidentDirector(RestaurantConfig config, Random random, RestaurantSimulation simulation, RestaurantQuality quality,
            Func<StaffRole, bool> roleHasStaff)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _quality = quality ?? throw new ArgumentNullException(nameof(quality));
            _roleHasStaff = roleHasStaff ?? throw new ArgumentNullException(nameof(roleHasStaff));
        }

        public ActiveIncident? Active => _active == null
            ? null
            : new ActiveIncident(_active, _active.Role, (float)_elapsed, _roleHasStaff(_active.Role));

        public void SetPresent(bool present)
        {
            _present = present;
            if (!present) Cancel();
        }

        // Panel hoặc popup đang mở thì "!" bị che, bỏ lỡ là phạt oan.
        public void SetSpawnAllowed(bool allowed) => _spawnAllowed = allowed;

        public void Update(float dt)
        {
            if (!_present || dt < 0f) return;

            if (_active != null)
            {
                _elapsed += dt;
                if (_elapsed > _config.IncidentLateSeconds) Finish(IncidentOutcome.Missed);
                return;
            }

            _spawnTimer ??= _config.IncidentSpawnMin + _random.NextDouble() * (_config.IncidentSpawnMax - _config.IncidentSpawnMin);
            _spawnTimer -= dt;
            if (_spawnTimer > 0) return;

            _spawnTimer = 0;
            if (_spawnAllowed) Spawn();
        }

        // Không đổi gì khi không có sự cố hoặc vai của sự cố không có nhân vật.
        public bool Resolve()
        {
            if (_active == null || !_roleHasStaff(_active.Role)) return false;

            Finish(_elapsed <= _config.IncidentFastSeconds ? IncidentOutcome.Fast : IncidentOutcome.Late);
            return true;
        }

        // Huỷ không tính điểm; đồng hồ sinh mới được đặt lại khi quay lại.
        public void Cancel()
        {
            if (_active == null) return;

            _simulation.SetStationBlocked(_active.Role, false);
            _active = null;
            _elapsed = 0;
            _spawnTimer = null;
        }

        public void Reset()
        {
            Cancel();
            _spawnTimer = null;
        }

        private void Spawn()
        {
            _active = Pick();
            _elapsed = 0;
            _spawnTimer = null;
            _simulation.SetStationBlocked(_active.Role, true);
        }

        private IncidentDefinition Pick()
        {
            var incidents = _config.Incidents;
            var total = 0.0;
            foreach (var incident in incidents) total += incident.Weight;

            var roll = _random.NextDouble() * total;
            var cumulative = 0.0;
            foreach (var incident in incidents)
            {
                cumulative += incident.Weight;
                if (roll < cumulative) return incident;
            }

            return incidents[incidents.Count - 1];
        }

        private void Finish(IncidentOutcome outcome)
        {
            _simulation.SetStationBlocked(_active.Role, false);
            _active = null;
            _elapsed = 0;
            _spawnTimer = null;
            _quality.RecordIncident(outcome);
        }
    }
}
