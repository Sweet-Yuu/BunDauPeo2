using System.IO;
using BunDauPeo.Boot;
using BunDauPeo.EditorTools;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class PlaytestToolsTests
    {
        [Test]
        public void BackupAndRestore_FollowSavePathOverride()
        {
            var dir = Path.Combine(Path.GetTempPath(), "bdp-ptt-" + Path.GetRandomFileName());
            var realBackup = Path.Combine(Path.GetDirectoryName(GameBootstrap.SavePath), SaveBackup.FolderName);
            var realBackupExisted = Directory.Exists(realBackup);
            Directory.CreateDirectory(dir);
            var save = Path.Combine(dir, "save.json");
            File.WriteAllText(save, "original");
            try
            {
                GameBootstrap.SavePathOverride = save;

                Assert.That(PlaytestTools.BackupSave(), Does.Not.StartWith("ERROR:"));
                Assert.IsTrue(Directory.Exists(Path.Combine(dir, SaveBackup.FolderName)), "backup must sit next to the override file");

                File.WriteAllText(save, "dirtied by playtest");
                Assert.That(PlaytestTools.RestoreSave(), Does.Not.StartWith("ERROR:"));
                Assert.AreEqual("original", File.ReadAllText(save));
                Assert.IsFalse(Directory.Exists(Path.Combine(dir, SaveBackup.FolderName)));
                Assert.AreEqual(realBackupExisted, Directory.Exists(realBackup), "real save folder must not be touched");
            }
            finally
            {
                GameBootstrap.SavePathOverride = null;
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void ClickButton_UnknownName_ReportsNotFound()
        {
            Assert.That(PlaytestTools.ClickButton("NoSuchButton-bdp"), Does.Contain("NotFound"));
        }

        [Test]
        public void Capture1080_NotPlaying_ReturnsError()
        {
            Assert.That(PlaytestTools.Capture1080(), Does.StartWith("ERROR:"));
        }
    }
}
