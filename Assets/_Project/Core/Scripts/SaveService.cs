using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BunDauPeo.Core
{
    public sealed class SaveService
    {
        private readonly string _path;

        public SaveService(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Save path is required.", nameof(path));
            _path = Path.GetFullPath(path);
        }

        private string TempPath => _path + ".tmp";
        private string BackupPath => _path + ".bak";
        private string CorruptPath => _path + ".corrupt";

        public bool Save(PlayerSave save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));

                // Ghi ra file tạm rồi thay file chính, để tắt app giữa chừng chỉ để lại .tmp chứ không làm hỏng save.
                var bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(save));
                using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(_path)) File.Replace(TempPath, _path, BackupPath);
                else File.Move(TempPath, _path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"Save failed: {e.Message}");
                return false;
            }
        }

        public PlayerSave Load()
        {
            var main = TryRead(_path);
            if (main != null) return main;

            var backup = TryRead(BackupPath);
            if (backup != null)
            {
                Debug.LogWarning($"Main save unreadable, restored from backup: {BackupPath}");
                return backup;
            }

            // Game mới sẽ ghi đè file chính, nên giữ lại một bản để còn cứu được bằng tay.
            if (File.Exists(_path))
            {
                try
                {
                    File.Copy(_path, CorruptPath, true);
                    Debug.LogWarning($"Save unreadable, copy kept at: {CorruptPath}");
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    Debug.LogWarning($"Save unreadable and could not keep a copy: {e.Message}");
                }
            }

            return null;
        }

        // JsonUtility ném ArgumentException khi JSON hỏng và trả null khi rỗng; cả hai đều là "hỏng".
        // Lỗi IO (file đang bị khoá) cố ý không bị nuốt: coi nó là "hỏng" sẽ làm game mới ghi đè save thật.
        private static PlayerSave TryRead(string path)
        {
            try
            {
                return File.Exists(path) ? JsonUtility.FromJson<PlayerSave>(File.ReadAllText(path)) : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
