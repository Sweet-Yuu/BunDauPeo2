using System;

namespace BunDauPeo.Core
{
    [Serializable]
    public struct DailyPresenceEntry
    {
        // Số ngày từ 2000-01-01 theo giờ máy.
        public int DayIndex;
        public double Seconds;
    }
}
