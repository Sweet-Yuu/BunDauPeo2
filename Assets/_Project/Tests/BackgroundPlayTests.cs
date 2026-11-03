using System;
using BunDauPeo.EditorTools;
using NUnit.Framework;
using UnityEditor;

namespace BunDauPeo.Tests
{
    public class BackgroundPlayTests
    {
        private const string Key = "BunDauPeo.Playtest.RunInBackgroundOriginal";

        // Test chạy trên PlayerSettings và SessionState thật của Editor, nên phải trả lại cả hai dù test lỗi.
        private static void WithRealState(Action body)
        {
            var savedSetting = PlayerSettings.runInBackground;
            var savedKey = SessionState.GetInt(Key, -1);
            try
            {
                SessionState.SetInt(Key, -1);
                body();
            }
            finally
            {
                PlayerSettings.runInBackground = savedSetting;
                SessionState.SetInt(Key, savedKey);
            }
        }

        [Test]
        public void EnableThenRestore_OriginalFalse_BackToFalse() => WithRealState(() =>
        {
            PlayerSettings.runInBackground = false;
            BackgroundPlay.Enable();
            // Bắt bản Enable không làm gì: giữa chừng phải thật sự là true.
            Assert.IsTrue(PlayerSettings.runInBackground);
            BackgroundPlay.Restore();
            // Bắt bản Restore luôn đặt true hoặc không trả lại.
            Assert.IsFalse(PlayerSettings.runInBackground);
        });

        [Test]
        public void EnableThenRestore_OriginalTrue_BackToTrue() => WithRealState(() =>
        {
            PlayerSettings.runInBackground = true;
            BackgroundPlay.Enable();
            BackgroundPlay.Restore();
            // Bắt bản Restore luôn đặt false (hoặc hiểu "bật" là "gốc = false").
            Assert.IsTrue(PlayerSettings.runInBackground);
        });

        [Test]
        public void Enable_Twice_RestoresValueBeforeFirstCall() => WithRealState(() =>
        {
            PlayerSettings.runInBackground = false;
            BackgroundPlay.Enable();
            BackgroundPlay.Enable();
            BackgroundPlay.Restore();
            // Bắt bản Enable ghi đè giá trị gốc bằng true ở lần gọi thứ hai.
            Assert.IsFalse(PlayerSettings.runInBackground);
        });

        [Test]
        public void Restore_WithoutEnable_ChangesNothing() => WithRealState(() =>
        {
            // Bắt bản Restore khi chưa bật mà vẫn ép về false (hoặc true).
            PlayerSettings.runInBackground = true;
            BackgroundPlay.Restore();
            Assert.IsTrue(PlayerSettings.runInBackground);

            PlayerSettings.runInBackground = false;
            BackgroundPlay.Restore();
            Assert.IsFalse(PlayerSettings.runInBackground);
        });

        [Test]
        public void HandlePlayModeState_EnteredEditMode_Restores() => WithRealState(() =>
        {
            PlayerSettings.runInBackground = false;
            BackgroundPlay.Enable();

            // Bắt bản khôi phục ở mọi trạng thái, làm game mất Run In Background ngay khi vào Play.
            BackgroundPlay.HandlePlayModeState(PlayModeStateChange.EnteredPlayMode);
            Assert.IsTrue(PlayerSettings.runInBackground);

            // Bắt bản không nối HandlePlayModeState với Restore.
            BackgroundPlay.HandlePlayModeState(PlayModeStateChange.EnteredEditMode);
            Assert.IsFalse(PlayerSettings.runInBackground);
        });
    }
}
