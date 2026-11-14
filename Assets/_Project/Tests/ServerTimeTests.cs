using System;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class ServerTimeTests
    {
        private static DateTime Utc(int h, int m) => new DateTime(2026, 11, 13, h, m, 0, DateTimeKind.Utc);

        [Test]
        public void Sync_AppliesOffset_AndResync()
        {
            var device = Utc(10, 0);
            var time = new ServerTime(() => device);
            Assert.AreEqual(Utc(10, 0), time.UtcNow, "before the first Sync the offset is zero");

            time.Sync(Utc(10, 5).Ticks);
            Assert.AreEqual(Utc(10, 5), time.UtcNow);

            device = Utc(10, 1);
            Assert.AreEqual(Utc(10, 6), time.UtcNow, "device time keeps running under the offset");

            time.Sync(Utc(9, 0).Ticks);
            Assert.AreEqual(Utc(9, 0), time.UtcNow, "a new Sync replaces the offset instead of adding to it");
            device = Utc(10, 2);
            Assert.AreEqual(Utc(9, 1), time.UtcNow);
            Assert.AreEqual(DateTimeKind.Utc, time.UtcNow.Kind);
        }

        [Test]
        public void ServerClock_LocalNowIsServerUtcToLocal()
        {
            var time = new ServerTime(() => Utc(10, 0));
            time.Sync(Utc(22, 30).Ticks);
            var clock = new ServerClock(time);

            Assert.AreEqual(Utc(22, 30), clock.UtcNow);
            Assert.AreEqual(Utc(22, 30).ToLocalTime(), clock.LocalNow);
        }
    }
}
