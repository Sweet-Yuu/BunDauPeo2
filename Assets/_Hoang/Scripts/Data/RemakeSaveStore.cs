using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BunDauPeo.Core;
using UnityEngine;

namespace Hoang.Remake
{
    // SaveService.Load trả base PlayerSave; đọc typed bổ sung giữ nguyên field remake.
    public sealed class RemakeSaveStore
    {
        private readonly string _path;
        private readonly SaveService _service;
        public string Path => _path;

        public RemakeSaveStore(string path)
        {
            _path = System.IO.Path.GetFullPath(path);
            _service = new SaveService(_path);
        }

        public HoangSaveData Load()
        {
            // Giữ nguyên recovery và phân biệt dữ liệu hỏng với lỗi IO của core.
            if (_service.Load() == null) return RecoverInterruptedReset();
            return ReadTyped(_path) ?? ReadTyped(_path + ".bak");
        }

        private HoangSaveData RecoverInterruptedReset()
        {
            var directory = System.IO.Path.GetDirectoryName(_path);
            if (!Directory.Exists(directory)) return null;
            var backups = Directory.GetFiles(directory, System.IO.Path.GetFileName(_path) + ".reset-backup-*");
            Array.Sort(backups, (left, right) => File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left)));
            foreach (var backup in backups)
            {
                var recovered = ReadTyped(backup);
                if (recovered == null) continue;
                Debug.LogWarning("Recovered progress from an interrupted restaurant reset.");
                return recovered;
            }
            return null;
        }

        private static HoangSaveData ReadTyped(string path)
        {
            if (!File.Exists(path)) return null;
            try { return JsonUtility.FromJson<HoangSaveData>(File.ReadAllText(path)); }
            catch (ArgumentException) { return null; }
        }

        public bool Save(HoangSaveData candidate)
        {
            // Public API core serialize lại cùng candidate khi retry; không chạy lại mutation/grant.
            for (var attempt = 0; attempt < 4; attempt++)
            {
                if (_service.Save(candidate)) return true;
                if (attempt < 3) Thread.Sleep(10);
            }
            return false;
        }

        public bool DeleteAll() => _service.DeleteAll();

        public bool Reset(HoangSaveData defaults)
        {
            var stagedPath = _path + ".reset-candidate";
            // Stage qua SaveService để lỗi ghi không xóa main đang dùng.
            if (!new SaveService(stagedPath).Save(defaults)) return false;
            var moved = new List<(string original, string staged)>();
            var committed = false;
            try
            {
                foreach (var artifact in Directory.GetFiles(System.IO.Path.GetDirectoryName(_path), System.IO.Path.GetFileName(_path) + ".*"))
                {
                    if (string.Equals(artifact, stagedPath, StringComparison.OrdinalIgnoreCase)) continue;
                    var isRecoveryBackup = string.Equals(artifact, _path + ".bak", StringComparison.OrdinalIgnoreCase)
                        || artifact.StartsWith(_path + ".reset-backup-", StringComparison.OrdinalIgnoreCase);
                    var temporary = _path + (isRecoveryBackup ? ".reset-backup-" : ".reset-previous-") + Guid.NewGuid().ToString("N");
                    File.Move(artifact, temporary);
                    moved.Add((artifact, temporary));
                }
                for (var attempt = 0; ; attempt++)
                {
                    try
                    {
                        if (File.Exists(_path)) File.Replace(stagedPath, _path, null);
                        else File.Move(stagedPath, _path);
                        committed = true;
                        return true;
                    }
                    catch (IOException) when (attempt < 3 && File.Exists(stagedPath)) { Thread.Sleep(10); }
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError("Reset failed: " + e.Message);
                return false;
            }
            finally
            {
                // Backup có thể là bản hợp lệ duy nhất khi main hỏng: chỉ xóa sau commit.
                foreach (var artifact in moved)
                {
                    try
                    {
                        if (committed) File.Delete(artifact.staged);
                        else File.Move(artifact.staged, artifact.original);
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    { Debug.LogWarning("Could not finish reset recovery cleanup: " + e.Message); }
                }
                try { if (File.Exists(stagedPath)) File.Delete(stagedPath); }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                { Debug.LogWarning("Could not clean reset candidate: " + e.Message); }
            }
        }
    }
}
