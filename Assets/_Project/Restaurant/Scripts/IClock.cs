using System;

namespace BunDauPeo.Restaurant
{
    // Tiêm vào để test dựng đồng hồ giả. LocalNow cho mốc ngày của Chuyên cần, UtcNow cho AFK.
    public interface IClock
    {
        DateTime UtcNow { get; }
        DateTime LocalNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime LocalNow => DateTime.Now;
    }
}
