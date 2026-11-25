using System;
using System.Collections.Generic;
using BunDauPeo.Missions;
using BunDauPeo.Rules;

namespace BunDauPeo.UI
{
    public enum MissionButtonState { Goto, Claim, Claimed, NotYet, Missed, Locked }

    // Luật hiển thị của trang Nhiệm vụ (spec đợt 3 8.1), thuần để test.
    public static class MissionRowRule
    {
        // Khoá thắng mọi thứ; đã nhận thắng khung giờ (nhận xong thì khung đã qua không còn là "Đã lỡ").
        public static MissionButtonState State(MissionRow row)
        {
            if (row.Locked) return MissionButtonState.Locked;
            if (row.Claimed) return MissionButtonState.Claimed;
            if (row.Claimable) return MissionButtonState.Claim;
            if (row.WindowState == 2) return MissionButtonState.NotYet;
            if (row.WindowState == 3) return MissionButtonState.Missed;
            return MissionButtonState.Goto;
        }

        // Số đỏ trên tab: rỗng khi không có dòng nhận được.
        public static string Badge(int claimable) => claimable > 0 ? claimable.ToString() : "";

        // Nhận hết mờ khi không dòng nào nhận được.
        public static bool ClaimAllEnabled(int claimable) => claimable > 0;

        // Chỉ tab Ngày có đồng hồ tới 00:00.
        public static bool ShowRemaining(int tab) => tab == MissionService.TabDaily;

        public static string FormatRemaining(TimeSpan left)
        {
            if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            return $"{(int)left.TotalHours:00}:{left.Minutes:00}";
        }

        // x/y của chân trang: việc khoá chưa tính vào y.
        public static (int Done, int Total) Completed(IReadOnlyList<MissionRow> rows)
        {
            var done = 0;
            var total = 0;
            foreach (var row in rows)
            {
                if (row.Locked) continue;
                total++;
                if (row.Claimed) done++;
            }

            return (done, total);
        }
    }
}
