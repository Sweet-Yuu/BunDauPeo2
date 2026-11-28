using System;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class WeatherServiceTests
    {
        private sealed class Fixture
        {
            public PlayerSave Save = new();
            public FakeClock Clock = new();
            public ScriptedRandom Random;
            public int Persists;
            public int Changes;
            public WeatherService Service;

            public Fixture Build()
            {
                Service = new WeatherService(RestaurantTestData.Config(), Save, Clock, Random, () => { Persists++; return true; });
                Service.Changed += () => Changes++;
                return this;
            }

            // Mỗi bước 1 giây: đồng hồ tiến rồi Update, như một khung hình.
            public void Run(int seconds)
            {
                for (var i = 0; i < seconds; i++)
                {
                    Clock.Advance(TimeSpan.FromSeconds(1));
                    Service.Update(1f);
                }
            }
        }

        private static Fixture Make(double roll = 0.99) => new Fixture { Random = new ScriptedRandom(roll) }.Build();

        [Test]
        public void NewGame_IsClear()
        {
            var f = Make();

            Assert.IsFalse(f.Service.IsRaining);
            Assert.AreEqual(1, f.Service.ArrivalSlowdown);
        }

        [Test]
        public void Update_Before15Min_NeverStartsRain()
        {
            var f = Make(0.0);

            f.Run(14 * 60 + 59);

            Assert.IsFalse(f.Service.IsRaining);
            Assert.AreEqual(0, f.Random.Calls);
        }

        [Test]
        public void Update_RollsEveryRollInterval()
        {
            var f = Make(0.99);

            f.Run(20 * 60);

            Assert.AreEqual(2, f.Random.Calls);
        }

        [Test]
        public void Roll_Hit_StartsRainFor10Min()
        {
            var f = Make(0.01);

            f.Run(15 * 60);

            Assert.IsTrue(f.Service.IsRaining);
            Assert.AreEqual(600, f.Service.RainRemainingSeconds, 0.5);
            Assert.AreEqual(f.Clock.UtcNow.Ticks + TimeSpan.FromMinutes(10).Ticks, f.Save.Weather.RainEndsUtcTicks);
            Assert.AreEqual(2, f.Service.ArrivalSlowdown);
        }

        [Test]
        public void Roll_Miss_StaysClear()
        {
            var f = Make(0.99);

            f.Run(15 * 60);

            Assert.IsFalse(f.Service.IsRaining);
        }

        [Test]
        public void Rain_EndsAfter10Min_ResetsCounters()
        {
            var f = Make(0.01);
            f.Run(15 * 60);

            f.Clock.Advance(TimeSpan.FromMinutes(10));
            f.Service.Update(1f);

            Assert.IsFalse(f.Service.IsRaining);
            Assert.AreEqual(0, f.Save.Weather.ContinuousSeconds);
            Assert.AreEqual(0, f.Save.Weather.SecondsSinceRoll);
            Assert.AreEqual(0, f.Save.Weather.RainEndsUtcTicks);
            Assert.AreEqual(2, f.Changes);
        }

        [Test]
        public void NoRolls_WhileRaining()
        {
            var f = Make(0.0);
            f.Service.ForceRain();
            f.Save.Weather.ContinuousSeconds = 12000;

            for (var i = 0; i < 100; i++) f.Service.Update(10f);

            Assert.AreEqual(0, f.Random.Calls);
            Assert.AreEqual(12000, f.Save.Weather.ContinuousSeconds);
        }

        [Test]
        public void Rain_ContinuesThroughBackground()
        {
            var f = Make();
            f.Service.ForceRain();

            f.Service.Heartbeat();
            f.Clock.Advance(TimeSpan.FromMinutes(3));
            f.Service.CatchUp();

            Assert.IsTrue(f.Service.IsRaining);
            Assert.AreEqual(420, f.Service.RainRemainingSeconds, 0.5);
        }

        [Test]
        public void CatchUp_RainEndedWhileAway_ResetsCounters()
        {
            var f = Make();
            f.Service.ForceRain();
            f.Save.Weather.ContinuousSeconds = 1000;
            f.Service.Heartbeat();

            f.Clock.Advance(TimeSpan.FromMinutes(11));
            f.Service.CatchUp();

            Assert.IsFalse(f.Service.IsRaining);
            Assert.AreEqual(0, f.Save.Weather.ContinuousSeconds);
            Assert.AreEqual(0, f.Save.Weather.SecondsSinceRoll);
        }

        [Test]
        public void CatchUp_GapBoundaries()
        {
            var f = Make();
            f.Save.Weather.ContinuousSeconds = 40 * 60;
            f.Save.Weather.SecondsSinceRoll = 120;
            f.Service.Heartbeat();

            f.Clock.Advance(TimeSpan.FromMinutes(9));
            f.Service.CatchUp();
            Assert.AreEqual(40 * 60, f.Save.Weather.ContinuousSeconds);
            Assert.AreEqual(120, f.Save.Weather.SecondsSinceRoll);

            f.Service.Heartbeat();
            f.Clock.Advance(TimeSpan.FromMinutes(10));
            f.Service.CatchUp();
            Assert.AreEqual(0, f.Save.Weather.ContinuousSeconds);
            Assert.AreEqual(0, f.Save.Weather.SecondsSinceRoll);
        }

        [Test]
        public void CatchUp_NegativeGap_TreatedAsZero()
        {
            var f = Make();
            f.Save.Weather.ContinuousSeconds = 600;
            f.Service.Heartbeat();

            f.Clock.Advance(TimeSpan.FromMinutes(-5));
            f.Service.CatchUp();

            Assert.AreEqual(600, f.Save.Weather.ContinuousSeconds);
            Assert.AreEqual(f.Clock.UtcNow.Ticks, f.Save.Weather.LastActiveUtcTicks);
        }

        [Test]
        public void Ctor_LoadsRainFromSave()
        {
            var active = new Fixture { Random = new ScriptedRandom() };
            active.Save.Weather.RainEndsUtcTicks = active.Clock.UtcNow.Ticks + TimeSpan.FromMinutes(5).Ticks;
            active.Build();
            Assert.IsTrue(active.Service.IsRaining);
            Assert.AreEqual(300, active.Service.RainRemainingSeconds, 0.5);

            var expired = new Fixture { Random = new ScriptedRandom() };
            expired.Save.Weather.RainEndsUtcTicks = expired.Clock.UtcNow.Ticks - 1;
            expired.Build();
            Assert.IsFalse(expired.Service.IsRaining);
        }

        [Test]
        public void StartRain_PersistsImmediately()
        {
            var miss = Make(0.99);
            miss.Run(20 * 60);
            Assert.AreEqual(0, miss.Persists);

            var f = Make(0.01);
            f.Run(15 * 60);
            Assert.AreEqual(1, f.Persists);

            f.Clock.Advance(TimeSpan.FromMinutes(10));
            f.Service.Update(1f);
            Assert.AreEqual(1, f.Persists);
        }

        [Test]
        public void Heartbeat_WritesLastActive()
        {
            var f = Make();
            f.Clock.Advance(TimeSpan.FromMinutes(7));

            f.Service.Heartbeat();

            Assert.AreEqual(f.Clock.UtcNow.Ticks, f.Save.Weather.LastActiveUtcTicks);
        }

        [Test]
        public void DevForceRain_ForceClear()
        {
            var f = Make();
            f.Save.Weather.ContinuousSeconds = 999;

            f.Service.ForceRain();
            Assert.IsTrue(f.Service.IsRaining);
            Assert.AreEqual(600, f.Service.RainRemainingSeconds, 0.5);

            f.Service.ForceClear();
            Assert.IsFalse(f.Service.IsRaining);
            Assert.AreEqual(0, f.Save.Weather.ContinuousSeconds);
            Assert.AreEqual(0, f.Save.Weather.SecondsSinceRoll);
        }

        [Test]
        public void DevAddContinuous_And_RollNow()
        {
            var f = Make();
            f.Service.AddContinuous(1800);
            Assert.AreEqual(30, f.Service.ContinuousMinutes, 1e-9);
            Assert.AreEqual(0, f.Random.Calls);

            // Ở phút 200 xác suất là 0,75.
            var hit = new Fixture { Random = new ScriptedRandom(0.5) }.Build();
            hit.Service.AddContinuous(200 * 60);
            hit.Service.RollNow();
            Assert.IsTrue(hit.Service.IsRaining);

            var miss = new Fixture { Random = new ScriptedRandom(0.9) }.Build();
            miss.Service.AddContinuous(200 * 60);
            miss.Service.RollNow();
            Assert.IsFalse(miss.Service.IsRaining);
        }

        [Test]
        public void CurrentChance_And_SecondsToNextRoll()
        {
            var f = Make();
            f.Save.Weather.ContinuousSeconds = 135 * 60;
            f.Save.Weather.SecondsSinceRoll = 100;

            Assert.AreEqual(0.10, f.Service.CurrentChance, 1e-6);
            Assert.AreEqual(200, f.Service.SecondsToNextRoll, 1e-9);
        }
    }
}
