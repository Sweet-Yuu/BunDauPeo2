using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Restaurant
{
    // Giá trị số được lưu trong PlayerSave.IncidentScores: chỉ thêm ở cuối.
    public enum IncidentOutcome { Missed = 0, Late = 1, Fast = 2 }

    // Chuyên cần + Xử lý sự cố (spec 4.4). Chỉ đọc và ghi PlayerSave; không giữ trạng thái riêng.
    public sealed class RestaurantQuality
    {
        private const int WindowDays = 7;
        private const int MaxRows = 8;
        private const int MaxIncidents = 10;
        private static readonly DateTime Epoch = new DateTime(2000, 1, 1);

        private readonly PlayerSave _save;
        private readonly RestaurantConfig _config;
        private readonly IClock _clock;

        public RestaurantQuality(PlayerSave save, RestaurantConfig config, IClock clock)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public double Attendance
        {
            get
            {
                var sum = 0.0;
                foreach (var credit in DayCredits) sum += credit;
                return _config.AttendanceMax * sum / WindowDays;
            }
        }

        public double IncidentPoints
        {
            get
            {
                var sum = 0.0;
                var first = Math.Max(0, _save.IncidentScores.Count - MaxIncidents);
                for (var i = first; i < _save.IncidentScores.Count; i++)
                    sum += Math.Clamp(_save.IncidentScores[i], 0, 2) / 2.0;
                return _config.IncidentMax * sum / MaxIncidents;
            }
        }

        public double Total => Attendance + IncidentPoints;
        public double Stars => Total / 20.0;
        public double AfkFactor => Mathf.Lerp(_config.AfkFactorMin, _config.AfkFactorMax, (float)(Total / 100.0));

        // [0] = hôm qua … [6] = 7 ngày trước. Hôm nay không bao giờ nằm trong cửa sổ.
        public IReadOnlyList<float> DayCredits
        {
            get
            {
                var today = DayIndex(_clock.LocalNow);
                var credits = new float[WindowDays];
                for (var i = 0; i < WindowDays; i++)
                    credits[i] = (float)Math.Min(1.0, SecondsOf(today - 1 - i) / _config.DailyGoalSeconds);
                return credits;
            }
        }

        public double TodaySeconds => SecondsOf(DayIndex(_clock.LocalNow));

        public void RecordPresence(double seconds)
        {
            if (!(seconds > 0)) return;

            var today = DayIndex(_clock.LocalNow);
            var index = _save.DailyPresence.FindIndex(e => e.DayIndex == today);
            if (index < 0)
            {
                _save.DailyPresence.Add(new DailyPresenceEntry { DayIndex = today, Seconds = seconds });
            }
            else
            {
                var row = _save.DailyPresence[index];
                row.Seconds += seconds;
                _save.DailyPresence[index] = row;
            }

            // Dòng tương lai (đồng hồ lùi) chỉ bị bỏ khi quá 8 dòng, như dòng cũ.
            while (_save.DailyPresence.Count > MaxRows)
            {
                var oldest = 0;
                for (var i = 1; i < _save.DailyPresence.Count; i++)
                    if (_save.DailyPresence[i].DayIndex < _save.DailyPresence[oldest].DayIndex) oldest = i;
                _save.DailyPresence.RemoveAt(oldest);
            }
        }

        public void RecordIncident(IncidentOutcome outcome)
        {
            _save.IncidentScores.Add((int)outcome);
            while (_save.IncidentScores.Count > MaxIncidents) _save.IncidentScores.RemoveAt(0);
        }

        private double SecondsOf(int dayIndex)
        {
            var sum = 0.0;
            foreach (var row in _save.DailyPresence)
                if (row.DayIndex == dayIndex) sum += row.Seconds;
            return sum;
        }

        private static int DayIndex(DateTime local) => (int)(local.Date - Epoch).TotalDays;
    }
}
