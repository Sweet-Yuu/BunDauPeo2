using UnityEditor;
using UnityEditor.SceneManagement;

namespace BunDauPeo.EditorTools
{
    // Bấm Play ở scene nào cũng khởi động từ Boot, để GameBootstrap luôn chạy trước.
    public static class BootScenePlayMode
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            // Chạy test bằng dòng lệnh: Unity sẽ Play Boot thay vì scene khởi tạo của test, test không bao giờ bắt đầu.
            var runningTests = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-runTests") >= 0;

            // delayCall tránh load asset giữa lúc đang import.
            EditorApplication.delayCall += () =>
                EditorSceneManager.playModeStartScene = runningTests
                    ? null
                    : AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectSetup.BootScenePath);
        }
    }
}
