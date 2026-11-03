using System;
using System.IO;
using System.Linq;

namespace BunDauPeo.EditorTools
{
    // Sao lưu / khôi phục các file save quanh một lượt playtest. Không bao giờ ghi đè bản sao lưu cũ (sẽ mất save gốc).
    public static class SaveBackup
    {
        public const string FolderName = "playtest-backup";
        public const string NoSaveMarker = "NO_SAVE";

        private const string SaveName = "save.json";

        public static string Backup(string saveDirectory)
        {
            var backup = Path.Combine(saveDirectory, FolderName);
            var tmp = backup + ".tmp";
            if (Directory.Exists(backup))
                return "ERROR: Đã có bản sao lưu (" + FolderName + "). Chạy Restore trước, nếu không save gốc sẽ bị mất.";
            try
            {
                if (Directory.Exists(tmp)) Directory.Delete(tmp, true); // rác từ lần crash trước
                Directory.CreateDirectory(tmp);
                var files = SaveFiles(saveDirectory);
                foreach (var file in files) File.Copy(file, Path.Combine(tmp, Path.GetFileName(file)));
                if (files.Length == 0) File.WriteAllBytes(Path.Combine(tmp, NoSaveMarker), new byte[0]);
                // Move cả thư mục: hoặc có đủ bản sao lưu, hoặc không có gì; không bao giờ có bản dở dang.
                Directory.Move(tmp, backup);
                return files.Length == 0 ? "Chưa có save, đã đánh dấu NO_SAVE." : "Đã sao lưu " + files.Length + " file save.";
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                try { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); } catch (Exception) { /* dọn không được thì thôi, lỗi gốc mới quan trọng */ }
                return "ERROR: " + e.Message;
            }
        }

        public static string Restore(string saveDirectory)
        {
            var backup = Path.Combine(saveDirectory, FolderName);
            if (!Directory.Exists(backup)) return "ERROR: Không có bản sao lưu để khôi phục.";
            try
            {
                // Thay thế, không trộn: file hiện có mà bản sao lưu không có (vd .bak mới) phải biến mất.
                foreach (var file in SaveFiles(saveDirectory)) File.Delete(file);
                foreach (var file in Directory.GetFiles(backup))
                {
                    var name = Path.GetFileName(file);
                    if (name != NoSaveMarker) File.Copy(file, Path.Combine(saveDirectory, name), true);
                }
                // Xoá bản sao lưu SAU CÙNG: lỗi giữa chừng thì vẫn còn bản gốc để thử lại.
                Directory.Delete(backup, true);
                return "Đã khôi phục save.";
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return "ERROR: " + e.Message + " (giữ nguyên bản sao lưu)";
            }
        }

        // Chỉ save.json và save.json.* nằm trực tiếp trong thư mục; thư mục sao lưu không tính.
        private static string[] SaveFiles(string dir) => !Directory.Exists(dir)
            ? new string[0]
            : Directory.GetFiles(dir).Where(f =>
            {
                var name = Path.GetFileName(f);
                return name == SaveName || name.StartsWith(SaveName + ".", StringComparison.Ordinal);
            }).ToArray();
    }
}
