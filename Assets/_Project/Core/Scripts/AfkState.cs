using System;

namespace BunDauPeo.Core
{
    [Serializable]
    public class AfkState
    {
        // 0 = chưa có mốc.
        public long LastActiveUtcTicks;
        // Coin lý thuyết/giây tại lần Heartbeat gần nhất.
        public double RatePerSecond;
        // Đã tích, tối đa bằng trần.
        public double AccumulatedSeconds;
        // Coin lý thuyết đang chờ, chưa nhân hệ số chất lượng.
        public double PendingTheoretical;
    }
}
