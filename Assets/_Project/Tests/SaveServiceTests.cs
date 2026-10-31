using System;
using System.IO;
using System.Text.RegularExpressions;
using BunDauPeo.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BunDauPeo.Tests
{
    public class SaveServiceTests
    {
        private string _dir;
        private string _path;
        private SaveService _service;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_dir);
            _path = Path.Combine(_dir, "save.json");
            _service = new SaveService(_path);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private static PlayerSave Make(long coin)
        {
            var save = new PlayerSave();
            save.Balances.Add(new CurrencyAmount(CurrencyType.Coin, coin));
            return save;
        }

        [Test]
        public void SaveThenLoad_RoundTripsAllFields()
        {
            var save = new PlayerSave();
            save.Balances.Add(new CurrencyAmount(CurrencyType.Gem, 5));
            save.OwnedCharacterIds.Add("a");
            save.BattleTeam.Add("a");
            save.Staff.Add(new StaffAssignment { CharacterId = "b", Role = StaffRole.Cashier });
            Assert.IsTrue(_service.Save(save));
            var loaded = _service.Load();
            Assert.AreEqual(5, loaded.Balances[0].Amount);
            Assert.AreEqual(CurrencyType.Gem, loaded.Balances[0].Type);
            Assert.AreEqual("a", loaded.OwnedCharacterIds[0]);
            Assert.AreEqual("a", loaded.BattleTeam[0]);
            Assert.AreEqual(StaffRole.Cashier, loaded.Staff[0].Role);
        }

        [Test]
        public void Load_NoFile_ReturnsNull() => Assert.IsNull(_service.Load());

        [Test]
        public void Save_CreatesMissingDirectory() =>
            Assert.IsTrue(new SaveService(Path.Combine(_dir, "sub", "save.json")).Save(new PlayerSave()));

        [Test]
        public void Load_CorruptMain_FallsBackToPreviousSave()
        {
            _service.Save(Make(1));
            _service.Save(Make(2));
            File.WriteAllText(_path, "{broken");
            Assert.AreEqual(1, _service.Load().Balances[0].Amount);
        }

        [Test]
        public void Load_EmptyMain_FallsBackToPreviousSave()
        {
            _service.Save(Make(1));
            _service.Save(Make(2));
            File.WriteAllText(_path, "");
            Assert.AreEqual(1, _service.Load().Balances[0].Amount);
        }

        [Test]
        public void Load_BothCorrupt_ReturnsNull_AndKeepsCopy()
        {
            File.WriteAllText(_path, "{broken");
            File.WriteAllText(_path + ".bak", "{broken");
            Assert.IsNull(_service.Load());
            Assert.AreEqual("{broken", File.ReadAllText(_path + ".corrupt"));
        }

        [Test]
        public void Load_BothCorrupt_KeepsCopyOfBackupToo()
        {
            // Lần lưu của game mới ghi đè .bak, nên bản hỏng của nó phải được giữ riêng.
            File.WriteAllText(_path, "{broken");
            File.WriteAllText(_path + ".bak", "{bak-broken");
            Assert.IsNull(_service.Load());
            Assert.AreEqual("{bak-broken", File.ReadAllText(_path + ".bak.corrupt"));
        }

        [Test]
        public void LeftoverTmp_DoesNotAffectLoadOrSave()
        {
            _service.Save(Make(1));
            File.WriteAllText(_path + ".tmp", "{half-written");
            Assert.AreEqual(1, _service.Load().Balances[0].Amount);
            Assert.IsTrue(_service.Save(Make(2)));
            Assert.AreEqual(2, _service.Load().Balances[0].Amount);
        }

        [Test]
        public void Load_FileLockedByOtherProcess_Throws_InsteadOfLookingLikeNoSave()
        {
            // Trả null ở đây sẽ làm GameSession bắt đầu game mới rồi ghi đè save thật khi khoá nhả.
            _service.Save(Make(1));
            using (new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.Throws<IOException>(() => _service.Load());
        }

        [Test]
        public void Save_ThirdTime_ReplacesExistingBackup()
        {
            _service.Save(Make(1));
            _service.Save(Make(2));
            Assert.IsTrue(_service.Save(Make(3)));
            Assert.AreEqual(3, _service.Load().Balances[0].Amount);
            File.Delete(_path);
            Assert.AreEqual(2, _service.Load().Balances[0].Amount);
        }

        [Test]
        public void DeleteAll_RemovesMainAndEverySidecar_AndLeavesOtherFiles()
        {
            _service.Save(Make(1));
            _service.Save(Make(2));
            foreach (var extension in new[] { ".tmp", ".corrupt", ".bak.corrupt" })
                File.WriteAllText(_path + extension, "x");
            var other = Path.Combine(_dir, "other.json");
            File.WriteAllText(other, "keep");

            Assert.IsTrue(_service.DeleteAll());
            CollectionAssert.AreEqual(new[] { other }, Directory.GetFiles(_dir));
            Assert.IsNull(_service.Load());
        }

        [Test]
        public void DeleteAll_NothingToDelete_ReturnsTrue() => Assert.IsTrue(_service.DeleteAll());

        [Test]
        public void DeleteAll_FileLocked_ReturnsFalse_AndKeepsMainSave()
        {
            _service.Save(Make(1));
            LogAssert.Expect(LogType.Error, new Regex("Reset failed"));
            using (new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.IsFalse(_service.DeleteAll());
            Assert.AreEqual(1, _service.Load().Balances[0].Amount);
        }

        [Test]
        public void Save_WhenTargetUnwritable_ReturnsFalse()
        {
            Directory.CreateDirectory(_path); // đường dẫn file là một thư mục
            LogAssert.Expect(LogType.Error, new Regex("Save failed"));
            Assert.IsFalse(_service.Save(new PlayerSave()));
        }
    }
}
