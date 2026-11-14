using System;

namespace BunDauPeo.Core
{
    // Giờ máy cộng độ lệch đo từ giờ server, nên chỉnh giờ máy không đổi thể lực, AFK, quán (spec 5.6).
    public sealed class ServerTime
    {
        private readonly Func<DateTime> _deviceUtcNow;
        private TimeSpan _offset;

        public ServerTime() : this(() => DateTime.UtcNow) { }

        public ServerTime(Func<DateTime> deviceUtcNow) =>
            _deviceUtcNow = deviceUtcNow ?? throw new ArgumentNullException(nameof(deviceUtcNow));

        public DateTime UtcNow => DateTime.SpecifyKind(_deviceUtcNow() + _offset, DateTimeKind.Utc);

        public void Sync(long serverUtcTicks) => _offset = new DateTime(serverUtcTicks, DateTimeKind.Utc) - _deviceUtcNow();
    }
}
