using System.IO;
using BunDauPeo.Boot;
using BunDauPeo.Playtest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BunDauPeo.EditorTools
{
    // Mặt tiền công cụ thử, gọi được từ `unity command eval` và từ menu. Việc người dùng làm sai được trả về "ERROR: ...", không ném lỗi.
    public static class PlaytestTools
    {
        public static string OpenBoot()
        {
            if (EditorApplication.isPlaying) return Log("ERROR: Đang Play, thoát Play trước khi mở Boot.");
            if (SceneManager.GetActiveScene().isDirty) return Log("ERROR: Scene đang sửa dở, lưu hoặc bỏ thay đổi trước.");
            EditorSceneManager.OpenScene(ProjectSetup.BootScenePath, OpenSceneMode.Single);
            return Log("Đã mở " + ProjectSetup.BootScenePath);
        }

        public static string BackupSave() => Log(SaveBackup.Backup(SaveDirectory()));

        // OnApplicationQuit của game ghi lại save, nên khôi phục lúc đang Play sẽ bị ghi đè ngay sau đó.
        public static string RestoreSave() => Log(EditorApplication.isPlaying
            ? "ERROR: Đang Play, thoát Play trước khi khôi phục save."
            : SaveBackup.Restore(SaveDirectory()));

        public static string EnableBackgroundPlay() => Log(BackgroundPlay.Enable());

        public static string RestoreBackgroundPlay() => Log(BackgroundPlay.Restore());

        public static string Capture1080(string fileName = null)
        {
            if (!EditorApplication.isPlaying) return Log("ERROR: Chỉ chụp được khi đang Play.");
            throw new System.NotImplementedException(); // Task 7 nối ScreenCapture1080
        }

        public static string ClickButton(string name) => Log(UiDriver.Click(name).ToString());

        // Theo SavePathOverride để test dùng thư mục tạm mà không chạm save thật.
        private static string SaveDirectory() => Path.GetDirectoryName(GameBootstrap.SavePath);

        private static string Log(string message)
        {
            if (message.StartsWith("ERROR:")) Debug.LogWarning(message);
            else Debug.Log(message);
            return message;
        }

        [MenuItem("Tools/BunDauPeo/Playtest/Open Boot")]
        private static void OpenBootMenu() => OpenBoot();

        [MenuItem("Tools/BunDauPeo/Playtest/Backup Save")]
        private static void BackupSaveMenu() => BackupSave();

        [MenuItem("Tools/BunDauPeo/Playtest/Restore Save...")]
        private static void RestoreSaveMenu()
        {
            if (!EditorUtility.DisplayDialog("Restore Save",
                    "Xoá save hiện tại và chép lại bản sao lưu:\n\n" + SaveDirectory() + "\n\nKhông thể hoàn tác.", "Khôi phục", "Huỷ"))
                return;
            RestoreSave();
        }

        [MenuItem("Tools/BunDauPeo/Playtest/Capture 1920x1080")]
        private static void Capture1080Menu() => Capture1080();
    }
}
