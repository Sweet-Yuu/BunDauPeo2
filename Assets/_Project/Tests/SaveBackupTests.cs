using System;
using System.IO;
using BunDauPeo.EditorTools;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class SaveBackupTests
    {
        // Mỗi test một thư mục riêng, dọn trong finally để test hỏng cũng không để rác.
        private static void WithDir(Action<string> body)
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            try { body(dir); }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        private static void Write(string dir, string name, string text) => File.WriteAllText(Path.Combine(dir, name), text);
        private static string Read(string dir, string name) => File.ReadAllText(Path.Combine(dir, name));
        private static bool Has(string dir, string name) => File.Exists(Path.Combine(dir, name));
        private static bool HasFolder(string dir, string name) => Directory.Exists(Path.Combine(dir, name));

        [Test]
        public void BackupThenRestore_BringsBackOriginalFiles() => WithDir(dir =>
        {
            Write(dir, "save.json", "main");
            Write(dir, "save.json.bak", "previous");
            Write(dir, "save.json.corrupt", "broken");

            Assert.IsFalse(SaveBackup.Backup(dir).StartsWith("ERROR:"));
            Assert.IsTrue(HasFolder(dir, SaveBackup.FolderName));

            Write(dir, "save.json", "played");
            Write(dir, "save.json.tmp", "temp");
            Assert.IsFalse(SaveBackup.Restore(dir).StartsWith("ERROR:"));

            Assert.AreEqual("main", Read(dir, "save.json"));
            Assert.AreEqual("previous", Read(dir, "save.json.bak"));
            Assert.AreEqual("broken", Read(dir, "save.json.corrupt"));
            Assert.IsFalse(Has(dir, "save.json.tmp"));
            Assert.IsFalse(HasFolder(dir, SaveBackup.FolderName));
            Assert.AreEqual(3, Directory.GetFileSystemEntries(dir).Length, "không được thừa/thiếu gì trong thư mục save");
        });

        [Test]
        public void Backup_Twice_RefusesAndKeepsFirstBackup() => WithDir(dir =>
        {
            Write(dir, "save.json", "first");
            Assert.IsFalse(SaveBackup.Backup(dir).StartsWith("ERROR:"));

            Write(dir, "save.json", "second");
            var result = SaveBackup.Backup(dir);
            StringAssert.StartsWith("ERROR:", result);
            StringAssert.Contains("Restore", result);
            Assert.AreEqual("first", Read(Path.Combine(dir, SaveBackup.FolderName), "save.json"), "bản sao lưu đầu bị ghi đè");
            Assert.IsFalse(HasFolder(dir, SaveBackup.FolderName + ".tmp"));

            Assert.IsFalse(SaveBackup.Restore(dir).StartsWith("ERROR:"));
            Assert.AreEqual("first", Read(dir, "save.json"));
        });

        [Test]
        public void Backup_Twice_RefusalNamesBackupCreationTime() => WithDir(dir =>
        {
            Assert.IsNull(SaveBackup.BackupTime(dir), "chưa sao lưu thì không có giờ");
            Write(dir, "save.json", "first");
            Assert.IsFalse(SaveBackup.Backup(dir).StartsWith("ERROR:"));
            Directory.SetCreationTime(Path.Combine(dir, SaveBackup.FolderName), new DateTime(2026, 1, 2, 3, 4, 5));

            Assert.AreEqual("2026-01-02 03:04", SaveBackup.BackupTime(dir));
            StringAssert.Contains("2026-01-02 03:04", SaveBackup.Backup(dir));
        });

        // Ngoại lệ có chủ ý của D-005 (ghi ở D-009): Reset Save Data giữ bản sao lưu playtest làm lưới an toàn cho người thử.
        [Test]
        public void ResetDeleteAll_KeepsPlaytestBackup() => WithDir(dir =>
        {
            Write(dir, "save.json", "real");
            Assert.IsFalse(SaveBackup.Backup(dir).StartsWith("ERROR:"));
            Write(dir, "save.json", "played");

            Assert.IsTrue(new BunDauPeo.Core.SaveService(Path.Combine(dir, "save.json")).DeleteAll());

            Assert.IsFalse(Has(dir, "save.json"));
            Assert.AreEqual("real", Read(Path.Combine(dir, SaveBackup.FolderName), "save.json"), "Reset xoá nhầm bản sao lưu");
        });

        [Test]
        public void Backup_NoSaveYet_ThenRestore_DeletesSaveCreatedMeanwhile() => WithDir(dir =>
        {
            var saveDir = Path.Combine(dir, "not-created-yet");
            Assert.IsFalse(SaveBackup.Backup(saveDir).StartsWith("ERROR:"));
            Assert.IsTrue(Has(Path.Combine(saveDir, SaveBackup.FolderName), SaveBackup.NoSaveMarker));

            Write(saveDir, "save.json", "created while playtesting");
            Write(saveDir, "save.json.bak", "also created");
            Assert.IsFalse(SaveBackup.Restore(saveDir).StartsWith("ERROR:"));

            Assert.IsFalse(Has(saveDir, "save.json"));
            Assert.IsFalse(Has(saveDir, "save.json.bak"));
            Assert.IsFalse(Has(saveDir, SaveBackup.NoSaveMarker), "marker không được chép ra thư mục save");
            Assert.IsFalse(HasFolder(saveDir, SaveBackup.FolderName));
        });

        [Test]
        public void Restore_WithoutBackup_ReturnsError_AndTouchesNothing() => WithDir(dir =>
        {
            Write(dir, "save.json", "keep me");
            Write(dir, "save.json.bak", "keep me too");

            StringAssert.StartsWith("ERROR:", SaveBackup.Restore(dir));

            Assert.AreEqual("keep me", Read(dir, "save.json"));
            Assert.AreEqual("keep me too", Read(dir, "save.json.bak"));
            Assert.AreEqual(2, Directory.GetFileSystemEntries(dir).Length);
        });

        [Test]
        public void Restore_ReplacesFilesPresentNow_NotMerges() => WithDir(dir =>
        {
            Write(dir, "save.json", "original");
            Assert.IsFalse(SaveBackup.Backup(dir).StartsWith("ERROR:"));

            Write(dir, "save.json", "played");
            Write(dir, "save.json.bak", "created during play");
            Assert.IsFalse(SaveBackup.Restore(dir).StartsWith("ERROR:"));

            Assert.AreEqual("original", Read(dir, "save.json"));
            Assert.IsFalse(Has(dir, "save.json.bak"), "Restore đang trộn thay vì thay thế");
        });

        [Test]
        public void Backup_FileLocked_ReturnsError_AndLeavesNoBackupFolder() => WithDir(dir =>
        {
            Write(dir, "save.json", "locked");
            Write(dir, "save.json.bak", "free");
            var stream = new FileStream(Path.Combine(dir, "save.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            try
            {
                StringAssert.StartsWith("ERROR:", SaveBackup.Backup(dir));
                Assert.IsFalse(HasFolder(dir, SaveBackup.FolderName));
                Assert.IsFalse(HasFolder(dir, SaveBackup.FolderName + ".tmp"));
            }
            finally { stream.Dispose(); }

            // Mở khoá rồi thử lại phải chạy được: lần lỗi trước không để lại trạng thái dở dang.
            Assert.IsFalse(SaveBackup.Backup(dir).StartsWith("ERROR:"));
            Assert.AreEqual("locked", Read(Path.Combine(dir, SaveBackup.FolderName), "save.json"));
        });
    }
}
