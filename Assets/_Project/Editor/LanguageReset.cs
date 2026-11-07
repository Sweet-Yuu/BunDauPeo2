using BunDauPeo.Core;
using UnityEditor;

namespace BunDauPeo.EditorTools
{
    // GameLanguage.Current là static và Unity không nạp lại domain khi thoát Play, nên lượt Play đã chọn English
    // sẽ làm scene builder và test EditMode thấy tiếng Anh. Về Edit Mode thì trả lại tiếng Việt.
    public static class LanguageReset
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            // Domain reload gọi lại hàm này; -= trước để không đăng ký hai lần.
            EditorApplication.playModeStateChanged -= HandlePlayModeState;
            EditorApplication.playModeStateChanged += HandlePlayModeState;
        }

        public static void HandlePlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) GameLanguage.Current = Language.Vi;
        }
    }
}
