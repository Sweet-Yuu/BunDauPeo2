using UnityEditor;

namespace BunDauPeo.EditorTools
{
    // Bật Run In Background trong lúc playtest tự động để game không đứng khi cửa sổ Unity mất focus,
    // rồi trả đúng giá trị gốc khi thoát Play Mode.
    public static class BackgroundPlay
    {
        // -1 = chưa bật; 0/1 = giá trị gốc của runInBackground. SessionState sống qua domain reload nhưng mất khi tắt Editor.
        private const string OriginalKey = "BunDauPeo.Playtest.RunInBackgroundOriginal";

        [InitializeOnLoadMethod]
        private static void Init()
        {
            // Domain reload gọi lại hàm này; -= trước để không đăng ký hai lần.
            EditorApplication.playModeStateChanged -= HandlePlayModeState;
            EditorApplication.playModeStateChanged += HandlePlayModeState;
        }

        public static string Enable()
        {
            // Chỉ lưu giá trị gốc ở lần đầu; gọi lần hai mà ghi đè sẽ làm mất giá trị thật.
            if (SessionState.GetInt(OriginalKey, -1) == -1)
                SessionState.SetInt(OriginalKey, PlayerSettings.runInBackground ? 1 : 0);

            // Không SaveAssets: đây chỉ là thay đổi tạm, không được ghi vào ProjectSettings.
            PlayerSettings.runInBackground = true;
            return "Đã bật Run In Background (tự khôi phục khi thoát Play Mode).";
        }

        public static string Restore()
        {
            var original = SessionState.GetInt(OriginalKey, -1);
            if (original == -1) return "Run In Background chưa được bật, không đổi gì.";

            PlayerSettings.runInBackground = original == 1;
            SessionState.SetInt(OriginalKey, -1);
            return "Đã khôi phục Run In Background về giá trị ban đầu.";
        }

        public static void HandlePlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) Restore();
        }
    }
}
