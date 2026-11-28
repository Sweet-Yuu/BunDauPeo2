using System;

namespace BunDauPeo.Core
{
    // Trạng thái thời tiết của máy (spec Thời tiết 6.2); toàn 0 = trời quang, chưa có mốc.
    [Serializable]
    public class WeatherState
    {
        // 0 = không mưa.
        public long RainEndsUtcTicks;
        // Chơi liên tục, giây.
        public double ContinuousSeconds;
        // Tính từ lần quay trước, giây.
        public double SecondsSinceRoll;
        // 0 = chưa có mốc.
        public long LastActiveUtcTicks;
    }
}
