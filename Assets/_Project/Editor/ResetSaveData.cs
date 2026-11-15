using System;
using BunDauPeo.Boot;
using BunDauPeo.Core;
using BunDauPeo.UI;
using UnityEditor;
using UnityEngine;

namespace BunDauPeo.EditorTools
{
    // Nút reset dữ liệu: xoá TOÀN BỘ những gì đã lưu (xem D-005). Không gì được xoá nếu người dùng chưa xác nhận.
    public static class ResetSaveData
    {
        [MenuItem("Tools/BunDauPeo/Reset Save Data...")]
        private static async void Menu()
        {
            var live = Application.isPlaying ? UnityEngine.Object.FindFirstObjectByType<GameBootstrap>()?.Session : null;
            if (live == null)
            {
                if (Run(Confirm, new SaveService(GameBootstrap.SavePath), null)) Debug.Log("Reset Save Data: done.");
                return;
            }

            // Đang Play: đi qua session đang chạy, server trước (spec 5.8, D-005 luật 4); đóng trang/hộp đang mở của hub trước, kẻo Hồ sơ gõ dở
            // ghi vào save mới (L-035).
            if (!Confirm() || !CloseHubPages(live)) return;
            var status = await ServiceLocator.Get<GameResetter>().ResetAsync();
            if (status == OnlineStatus.Ok) Debug.Log("Reset Save Data: done.");
            else Debug.LogError($"Reset Save Data: FAILED ({status}).");
        }

        // live != null khi đang Play: phải reset cả session đang chạy, nếu không OnApplicationQuit sẽ ghi lại dữ liệu cũ.
        public static bool Run(Func<bool> confirm, SaveService service, GameSession live)
        {
            if (!confirm()) return false;
            return live != null ? live.ResetToDefaults() : service.DeleteAll();
        }

        private static bool CloseHubPages(GameSession live)
        {
            if (live != null) UnityEngine.Object.FindFirstObjectByType<HubNav>()?.CloseOpenPages();
            return true;
        }

        private static bool Confirm() => EditorUtility.DisplayDialog(
            "Reset Save Data",
            "Xoá TOÀN BỘ dữ liệu đã lưu và đưa game về như mới:\n\n" +
            "- tiền (Coin, Gem, Material)\n" +
            "- nhân vật đã có, đội chiến đấu, nhân viên quán\n" +
            "- mọi bản sao lưu (.bak, .corrupt)\n" +
            "- đang Play: cả save trên server (ID người chơi giữ nguyên)\n\n" +
            GameBootstrap.SavePath + "\n\nKhông thể hoàn tác.",
            "Reset", "Huỷ");
    }
}
