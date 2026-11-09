using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Hoang.Remake.Editor
{
    [InitializeOnLoad]
    public static class RemakePlayMode
    {
        private const string PreviousKey = "Hoang.Remake.PreviousPlayScene";
        private const string OverrideKey = "Hoang.Remake.PlaySceneOverridden";

        static RemakePlayMode() => EditorApplication.playModeStateChanged += OnPlayMode;

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-runTests") >= 0) return;
            if (state == PlayModeStateChange.ExitingEditMode
                && SceneManager.GetActiveScene().path == RemakeSceneBuilder.ScenePath)
            {
                SessionState.SetString(PreviousKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
                SessionState.SetBool(OverrideKey, true);
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(RemakeSceneBuilder.ScenePath);
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(OverrideKey, false))
            {
                var previous = SessionState.GetString(PreviousKey, "");
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                SessionState.EraseString(PreviousKey);
                SessionState.EraseBool(OverrideKey);
            }
        }
    }
}
