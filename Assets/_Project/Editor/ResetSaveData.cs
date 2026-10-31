using System;
using BunDauPeo.Boot;
using BunDauPeo.Core;
using UnityEditor;
using UnityEngine;

namespace BunDauPeo.EditorTools
{
    // Nút reset dữ liệu: xoá TOÀN BỘ những gì đã lưu (xem D-005). Không gì được xoá nếu người dùng chưa xác nhận.
    public static class ResetSaveData
    {
        [MenuItem("Tools/BunDauPeo/Reset Save Data...")]
        private static void Menu()
        {
            var live = Application.isPlaying ? UnityEngine.Object.FindFirstObjectByType<GameBootstrap>()?.Session : null;
            var done = Run(Confirm, new SaveService(GameBootstrap.SavePath), live);
            if (done) Debug.Log("Reset Save Data: done.");
        }

        // live != null khi đang Play: phải reset cả session đang chạy, nếu không OnApplicationQuit sẽ ghi lại dữ liệu cũ.
        public static bool Run(Func<bool> confirm, SaveService service, GameSession live)
        {
            if (!confirm()) return false;
            return live != null ? live.ResetToDefaults() : service.DeleteAll();
        }

        private static bool Confirm() => EditorUtility.DisplayDialog(
            "Reset Save Data",
            "Xoá TOÀN BỘ dữ liệu đã lưu và đưa game về như mới:\n\n" +
            "- tiền (Coin, Gem, Material)\n" +
            "- nhân vật đã có, đội chiến đấu, nhân viên quán\n" +
            "- mọi bản sao lưu (.bak, .corrupt)\n\n" +
            GameBootstrap.SavePath + "\n\nKhông thể hoàn tác.",
            "Reset", "Huỷ");
    }
}
