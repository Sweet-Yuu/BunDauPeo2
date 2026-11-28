using System;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Restaurant
{
    // Mưa ngẫu nhiên theo thời gian chơi liên tục (spec Thời tiết 3, 7). Mọi trạng thái nằm ở PlayerSave.Weather và được đọc
    // lại mỗi lần (Reset ghi đè save tại chỗ), nên service không giữ bản sao.
    public sealed class WeatherService
    {
        private const long TicksPerMinute = TimeSpan.TicksPerMinute;

        private readonly RestaurantConfig _config;
        private readonly PlayerSave _save;
        private readonly IClock _clock;
        private readonly System.Random _random;
        private readonly Func<bool> _persist;

        public WeatherService(RestaurantConfig config, PlayerSave save, IClock clock, System.Random random, Func<bool> persist)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
            CatchUp();
        }

        public event Action Changed;

        private WeatherState State => _save.Weather;
        private long NowTicks => _clock.UtcNow.Ticks;

        public bool IsRaining => State.RainEndsUtcTicks > 0 && NowTicks < State.RainEndsUtcTicks;
        public double RainRemainingSeconds => IsRaining ? new TimeSpan(State.RainEndsUtcTicks - NowTicks).TotalSeconds : 0;
        public double ContinuousMinutes => State.ContinuousSeconds / 60.0;
        public double CurrentChance => WeatherCurve.Chance(ContinuousMinutes, _config);
        public double SecondsToNextRoll => Math.Max(0, _config.RollIntervalMinutes * 60.0 - State.SecondsSinceRoll);
        public double ArrivalSlowdown => IsRaining ? _config.RainArrivalMultiplier : 1;

        public void Update(float dt)
        {
            var state = State;
            if (state.RainEndsUtcTicks > 0)
            {
                if (NowTicks >= state.RainEndsUtcTicks) EndRain();
                return;
            }

            state.ContinuousSeconds += dt;
            state.SecondsSinceRoll += dt;
            var interval = _config.RollIntervalMinutes * 60.0;
            while (state.SecondsSinceRoll >= interval)
            {
                state.SecondsSinceRoll -= interval;
                if (Roll()) break;
            }
        }

        // Thứ tự theo spec mục 3 "Mốc hoạt động". Khoảng vắng âm (đồng hồ lùi) tính 0.
        public void CatchUp()
        {
            var state = State;
            var now = NowTicks;
            var away = state.LastActiveUtcTicks > 0 ? Math.Max(0, now - state.LastActiveUtcTicks) : 0;
            if (state.RainEndsUtcTicks > 0)
            {
                if (now >= state.RainEndsUtcTicks) EndRain();
            }
            else if (away >= (long)(_config.AwayResetMinutes * TicksPerMinute))
            {
                state.ContinuousSeconds = 0;
                state.SecondsSinceRoll = 0;
            }

            state.LastActiveUtcTicks = now;
        }

        public void Heartbeat() => State.LastActiveUtcTicks = NowTicks;

        public void ForceRain() => StartRain();

        public void ForceClear()
        {
            if (State.RainEndsUtcTicks > 0)
            {
                EndRain();
                return;
            }

            State.ContinuousSeconds = 0;
            State.SecondsSinceRoll = 0;
            Changed.SafeInvoke();
        }

        public void AddContinuous(double seconds) => State.ContinuousSeconds += seconds;

        public void RollNow() => Roll();

        // Save đã bị ghi đè trước khi gọi; chỉ báo view vẽ lại.
        public void ResetRuntime() => Changed.SafeInvoke();

        // Xác suất 0 thì không quay thật (không tiêu RNG).
        private bool Roll()
        {
            var chance = CurrentChance;
            if (chance <= 0 || _random.NextDouble() >= chance) return false;
            StartRain();
            return true;
        }

        private void StartRain()
        {
            State.RainEndsUtcTicks = NowTicks + (long)(_config.RainMinutes * TicksPerMinute);
            // Lưu ngay để app bị kill giữa cơn mưa không mất mưa.
            try
            {
                if (!_persist()) Debug.LogWarning($"{nameof(WeatherService)}: save did not complete.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{nameof(WeatherService)}: save failed: {e.Message}");
            }

            Changed.SafeInvoke();
        }

        private void EndRain()
        {
            var state = State;
            state.RainEndsUtcTicks = 0;
            state.ContinuousSeconds = 0;
            state.SecondsSinceRoll = 0;
            Changed.SafeInvoke();
        }
    }
}
