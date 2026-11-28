using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class WeatherCurveTests
    {
        private static readonly RestaurantConfig Config = ScriptableObject.CreateInstance<RestaurantConfig>();

        [Test]
        public void Chance_Before15Min_IsZero()
        {
            foreach (var minutes in new[] { 0.0, 14.99 }) Assert.AreEqual(0, WeatherCurve.Chance(minutes, Config), $"{minutes}");
        }

        [Test]
        public void Chance_15To90_IsBase()
        {
            foreach (var minutes in new[] { 15.0, 60.0, 89.99 }) Assert.AreEqual(0.05, WeatherCurve.Chance(minutes, Config), 1e-6, $"{minutes}");
        }

        [Test]
        public void Chance_90To180_RisesLinearly()
        {
            Assert.AreEqual(0.05, WeatherCurve.Chance(90, Config), 1e-6);
            Assert.AreEqual(0.10, WeatherCurve.Chance(135, Config), 1e-6);
            Assert.AreEqual(0.15, WeatherCurve.Chance(179.99, Config), 0.001);
        }

        [Test]
        public void Chance_From180_StepsEveryRollInterval()
        {
            Assert.AreEqual(0.15, WeatherCurve.Chance(180, Config), 1e-6);
            Assert.AreEqual(0.15, WeatherCurve.Chance(184.99, Config), 1e-6);
            Assert.AreEqual(0.30, WeatherCurve.Chance(185, Config), 1e-6);
            Assert.AreEqual(0.45, WeatherCurve.Chance(190, Config), 1e-6);
            Assert.AreEqual(0.60, WeatherCurve.Chance(195, Config), 1e-6);
            Assert.AreEqual(0.75, WeatherCurve.Chance(200, Config), 1e-6);
            Assert.AreEqual(0.90, WeatherCurve.Chance(205, Config), 1e-6);
        }

        [Test]
        public void Chance_CappedAtMax()
        {
            foreach (var minutes in new[] { 205.0, 400.0, 100000.0 }) Assert.AreEqual(0.9, WeatherCurve.Chance(minutes, Config), 1e-6, $"{minutes}");
        }

        [Test]
        public void Chance_NegativeMinutes_IsZero() => Assert.AreEqual(0, WeatherCurve.Chance(-5, Config));

        // Validate() còn báo thiếu món/khách/catalog của config trống; chỉ xét lỗi thời tiết.
        private static List<string> WeatherErrors(RestaurantConfig config) =>
            config.Validate().FindAll(e => !e.StartsWith("Dishes") && !e.StartsWith("Customers") && !e.StartsWith("Incidents") && !e.StartsWith("Upgrades"));

        [Test]
        public void Config_Defaults_AreValid() => Assert.IsEmpty(WeatherErrors(RestaurantTestData.Config()));

        [Test]
        public void Config_Validate_RejectsBadWeather()
        {
            var cases = new (string field, float value, string name)[]
            {
                ("_graceMinutes", 100f, "GraceMinutes"),
                ("_maxChance", 1.5f, "MaxChance"),
                ("_baseChance", 0.5f, "BaseChance"),
                ("_rainMinutes", 0f, "RainMinutes"),
                ("_rainArrivalMultiplier", 0.5f, "RainArrivalMultiplier"),
            };
            foreach (var (field, value, name) in cases)
            {
                var config = RestaurantTestData.Config();
                TestData.Edit(config, so => so.FindProperty(field).floatValue = value);

                Assert.IsTrue(WeatherErrors(config).Exists(e => e.StartsWith(name)), $"{name}: {string.Join(" | ", WeatherErrors(config))}");
            }
        }

        [Test]
        public void PlayerSave_NewGame_WeatherIsClear()
        {
            var w = new PlayerSave().Weather;

            Assert.AreEqual(0, w.RainEndsUtcTicks);
            Assert.AreEqual(0, w.ContinuousSeconds);
            Assert.AreEqual(0, w.SecondsSinceRoll);
            Assert.AreEqual(0, w.LastActiveUtcTicks);
        }

        [Test]
        public void PlayerSave_JsonRoundTrip_KeepsWeather()
        {
            var save = new PlayerSave();
            save.Weather.RainEndsUtcTicks = 123456789012L;
            save.Weather.ContinuousSeconds = 456.5;
            save.Weather.SecondsSinceRoll = 78.25;
            save.Weather.LastActiveUtcTicks = 987654321098L;

            var w = JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(save)).Weather;

            Assert.AreEqual(123456789012L, w.RainEndsUtcTicks);
            Assert.AreEqual(456.5, w.ContinuousSeconds);
            Assert.AreEqual(78.25, w.SecondsSinceRoll);
            Assert.AreEqual(987654321098L, w.LastActiveUtcTicks);

            var old = JsonUtility.FromJson<PlayerSave>("{\"Version\":1}").Weather;
            Assert.IsNotNull(old);
            Assert.AreEqual(0, old.RainEndsUtcTicks);
            Assert.AreEqual(0, old.ContinuousSeconds);
        }
    }
}
