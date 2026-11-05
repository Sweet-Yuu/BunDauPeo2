using System.IO;
using BunDauPeo.Boot;
using NUnit.Framework;
using UnityEngine;

namespace BunDauPeo.Tests
{
    public class GameBootstrapSavePathTests
    {
        [TearDown]
        public void TearDown() => GameBootstrap.SavePathOverride = null;

        [Test]
        public void SavePath_NoOverride_IsPersistentDataPathSaveJson()
        {
            GameBootstrap.SavePathOverride = null;
            Assert.AreEqual(Path.Combine(Application.persistentDataPath, "save.json"), GameBootstrap.SavePath);
        }

        [Test]
        public void SavePath_WithOverride_ReturnsOverride()
        {
            var path = Path.Combine(Path.GetTempPath(), "x", "save.json");
            try
            {
                GameBootstrap.SavePathOverride = path;
                Assert.AreEqual(path, GameBootstrap.SavePath);
            }
            finally { GameBootstrap.SavePathOverride = null; }
        }

        [Test]
        public void SavePath_OverrideSetBackToNull_IsRealPathAgain()
        {
            var real = Path.Combine(Application.persistentDataPath, "save.json");
            try
            {
                GameBootstrap.SavePathOverride = Path.Combine(Path.GetTempPath(), "y", "save.json");
                Assert.AreNotEqual(real, GameBootstrap.SavePath);
            }
            finally { GameBootstrap.SavePathOverride = null; }
            Assert.AreEqual(real, GameBootstrap.SavePath);
        }
    }
}
