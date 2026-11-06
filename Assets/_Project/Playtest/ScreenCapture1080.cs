using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BunDauPeo.Playtest
{
    // Chụp màn đang Play ở đúng 1920×1080 (L-011): Game view thật có thể khác tỉ lệ và `capture game_view` luôn bóp thành 16:9.
    public static class ScreenCapture1080
    {
        private const int Width = 1920;
        private const int Height = 1080;

        public static string Capture(string fileName = null, string directory = null)
        {
            if (!Application.isPlaying) return "ERROR: Chỉ chụp được khi đang Play.";

            // Ngoài repo (cạnh thư mục project) nên không sinh .meta và không bị commit.
            directory ??= Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "bdp_BunDauPeo", "shots"));
            fileName = string.IsNullOrEmpty(fileName)
                ? $"shot-{DateTime.Now:yyyyMMdd-HHmmss}.png"
                : Path.GetFileName(fileName);

            var overlays = new List<(Canvas canvas, Camera previous)>();
            GameObject cameraObject = null;
            RenderTexture rt = null;
            Texture2D tex = null;
            try
            {
                var main = Camera.main;
                cameraObject = new GameObject("PlaytestCaptureCamera") { hideFlags = HideFlags.HideAndDontSave };
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = main != null ? main.backgroundColor : Color.black;
                rt = new RenderTexture(Width, Height, 24);
                camera.targetTexture = rt;

                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                             .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay))
                {
                    overlays.Add((canvas, canvas.worldCamera));
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                }

                Canvas.ForceUpdateCanvases();
                camera.Render();

                var previousActive = RenderTexture.active;
                RenderTexture.active = rt;
                tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                RenderTexture.active = previousActive;

                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, fileName);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                return path;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return "ERROR: " + e.Message;
            }
            finally
            {
                // Dù chụp lỗi ở bước nào, Canvas phải về Overlay, nếu không màn game còn nằm trên camera tạm đã huỷ.
                foreach (var (canvas, previous) in overlays)
                {
                    if (canvas == null) continue;
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.worldCamera = previous;
                }
                // DestroyImmediate: Destroy chỉ chạy cuối khung, nên camera tạm còn sống ngay sau khi hàm trả về.
                Object.DestroyImmediate(cameraObject);
                if (rt != null) rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }
        }
    }
}
