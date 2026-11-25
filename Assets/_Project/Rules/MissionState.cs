#nullable disable
using System;
using System.Collections.Generic;

namespace BunDauPeo.Rules
{
    // Tiến độ nhiệm vụ server giữ (spec đợt 3 mục 5.1). Chỉ số ngày tính theo UTC+7, xem MissionRules.
    [Serializable]
    public class MissionState
    {
        public long DailyDay;                       // 0 = chưa có
        public int DailyWins;
        public int DailyPulls;
        public List<string> DailyClaimed = new();
        public long FreePullDay;                    // chỉ số ngày đã dùng lượt quay miễn phí; 0 = chưa
        public List<string> NewbieClaimed = new();

        public MissionState Clone()
        {
            var c = (MissionState)MemberwiseClone();
            c.DailyClaimed = new List<string>(DailyClaimed);
            c.NewbieClaimed = new List<string>(NewbieClaimed);
            return c;
        }
    }
}
