#nullable disable
using System;
using System.Collections.Generic;

namespace BunDauPeo.Rules
{
    // Ngày nhiệm vụ = ngày giờ Việt Nam (UTC+7), tính từ giờ server truyền vào.
    public static class MissionRules
    {
        public const int DayOffsetHours = 7;
        const long OffsetTicks = DayOffsetHours * TimeSpan.TicksPerHour;

        public const int CounterWins = 0, CounterPulls = 1, CounterLoginWindow = 2, CounterBattleStagesCleared = 3,
            CounterGachaEver = 4, CounterStarUpEver = 5, CounterClientFlag = 6, CounterNone = 7;

        // Không có việc nào dùng bộ đếm này thì vẫn chặn trên để số không tăng vô hạn.
        const int FallbackCounterCap = 9999;

        public static long DayIndex(DateTime nowUtc) => (nowUtc.Ticks + OffsetTicks) / TimeSpan.TicksPerDay;

        // Sang ngày thì xoá bộ đếm ngày; FreePullDay và NewbieClaimed giữ nguyên.
        public static void RollDay(MissionState m, DateTime nowUtc)
        {
            var today = DayIndex(nowUtc);
            if (m.DailyDay == today) return;
            m.DailyDay = today;
            m.DailyWins = 0;
            m.DailyPulls = 0;
            m.DailyClaimed.Clear();
        }

        // Giờ VN trong [WindowStartHour, WindowEndHour).
        public static bool InWindow(MissionData d, DateTime nowUtc)
        {
            var hour = (int)((nowUtc.Ticks + OffsetTicks) / TimeSpan.TicksPerHour % 24);
            return hour >= d.WindowStartHour && hour < d.WindowEndHour;
        }

        public static TimeSpan UntilNextDay(DateTime nowUtc) =>
            new TimeSpan((DayIndex(nowUtc) + 1) * TimeSpan.TicksPerDay - OffsetTicks - nowUtc.Ticks);
            // Trần của bộ đếm ngày = mục tiêu lớn nhất trong các việc dùng nó.
        public static int CounterCap(GameData data, int counter)
        {
            var cap = 0;
            foreach (var d in data.Missions)
                if (d.Counter == counter && d.Target > cap) cap = d.Target;
            return cap > 0 ? cap : FallbackCounterCap;
        }

        // Hàng hiển thị/kiểm của một nhiệm vụ. Dùng chung máy và server; không sửa state.
        // LoginWindow không có bộ đếm: Progress luôn bằng Target, việc nhận được hay chưa do WindowState quyết định.
        public static List<MissionRow> View(ServerState s, GameData data, DateTime nowUtc, IReadOnlyCollection<string> clientDone)
        {
            var m = s.Missions;
            var sameDay = m.DailyDay == DayIndex(nowUtc);
            var rows = new List<MissionRow>(data.Missions.Count);
            foreach (var d in data.Missions)
            {
                var claimed = d.Tab == 0 ? sameDay && m.DailyClaimed.Contains(d.Id) : m.NewbieClaimed.Contains(d.Id);
                var row = new MissionRow { Id = d.Id, Target = d.Target, Locked = d.Locked, Claimed = claimed };
                if (!d.Locked)
                {
                    row.Progress = Math.Min(d.Target, Math.Max(0, RawProgress(s, data, d, sameDay, clientDone)));
                    row.WindowState = WindowStateOf(d, nowUtc);
                    row.Claimable = !claimed && row.Progress >= d.Target && (row.WindowState == 0 || row.WindowState == 1);
                }
                rows.Add(row);
            }
            return rows;
        }

        static int RawProgress(ServerState s, GameData data, MissionData d, bool sameDay, IReadOnlyCollection<string> clientDone)
        {
            switch (d.Counter)
            {
                case CounterWins: return sameDay ? s.Missions.DailyWins : 0;
                case CounterPulls: return sameDay ? s.Missions.DailyPulls : 0;
                case CounterLoginWindow: return d.Target;
                case CounterBattleStagesCleared:
                    var n = 0;
                    foreach (var id in s.ClearedStages)
                    {
                        var stage = data.Stages.Find(x => x.Id == id);
                        if (stage != null && stage.Kind == 0) n++;
                    }
                    return n;
                case CounterGachaEver: return s.GachaHistory.Count > 0 ? 1 : 0;
                case CounterStarUpEver: return s.StarUps.Exists(e => e.Count > 0) ? 1 : 0;
                case CounterClientFlag: return clientDone != null && Contains(clientDone, d.Id) ? 1 : 0;
                default: return 0;
            }
        }

        static bool Contains(IReadOnlyCollection<string> list, string id)
        {
            foreach (var x in list) if (x == id) return true;
            return false;
        }

        // 0 không có khung, 1 đang trong khung, 2 chưa tới giờ, 3 đã lỡ.
        static int WindowStateOf(MissionData d, DateTime nowUtc)
        {
            if (d.WindowEndHour <= d.WindowStartHour) return 0;
            if (InWindow(d, nowUtc)) return 1;
            var hour = (int)((nowUtc.Ticks + OffsetTicks) / TimeSpan.TicksPerHour % 24);
            return hour < d.WindowStartHour ? 2 : 3;
        }
    }

    public class MissionRow
    {
        public string Id = "";
        public int Progress;
        public int Target;
        public bool Claimable;
        public bool Claimed;
        public bool Locked;
        public int WindowState;
    }
}
