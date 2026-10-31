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
            // delayCall tránh load asset giữa lúc đang import.
            EditorApplication.delayCall += () =>
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectSetup.BootScenePath);
        }
    }
}
