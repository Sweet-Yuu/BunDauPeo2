using System;
using BunDauPeo.Core;

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

    // Giờ server (spec 5.6); mốc ngày vẫn theo múi giờ của máy.
    public sealed class ServerClock : IClock
    {
        private readonly ServerTime _time;

        public ServerClock(ServerTime time) => _time = time ?? throw new ArgumentNullException(nameof(time));

        public DateTime UtcNow => _time.UtcNow;
        public DateTime LocalNow => _time.UtcNow.ToLocalTime();
    }
}
