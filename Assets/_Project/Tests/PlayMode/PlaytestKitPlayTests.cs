using System;
using System.Collections;
using System.IO;
using BunDauPeo.Playtest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BunDauPeo.PlayModeTests
{
    public class PlaytestKitPlayTests
    {
        private GameObject _canvasObject;
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "bdp-shot-" + Path.GetRandomFileName());
            _canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            _canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var image = new GameObject("Red", typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(_canvasObject.transform, false);
            image.color = Color.red;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.sizeDelta = Vector2.zero;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_canvasObject);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [UnityTest]
        public IEnumerator WaitForButton_Timeout_ListsVisibleButtons()
        {
            var button = new GameObject("Present", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(_canvasObject.transform, false);
            yield return null;

            var ex = Assert.Throws<TimeoutException>(() =>
            {
                var wait = UiDriver.WaitForButton("Missing", 0.5f);
                while (wait.MoveNext()) { } // hết giờ thì ném; MoveNext chạy thẳng vì không có gì để chờ khung
            }, "phải hết giờ khi nút không có");
            Assert.That(ex.Message, Does.Contain("Missing"));
            Assert.That(ex.Message, Does.Contain("Present"), "thông điệp phải liệt kê nút đang hiện");
        }

        [UnityTest]
        public IEnumerator Capture_Overlay_WritesPng1920x1080_AndRestoresOverlay()
        {
            yield return null; // để Canvas và Image có khung đầu
            var canvas = _canvasObject.GetComponent<Canvas>();

            var path = ScreenCapture1080.Capture(null, _dir);

            Assert.That(path, Does.EndWith(".png"), path);
            Assert.AreEqual(_dir, Path.GetDirectoryName(path));
            var tex = new Texture2D(2, 2);
            try
            {
                Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)));
                Assert.AreEqual(1920, tex.width);
                Assert.AreEqual(1080, tex.height);
                var centre = tex.GetPixel(960, 540);
                Assert.That(centre.r, Is.GreaterThan(0.8f), "ảnh phải có hình Canvas, không phải ảnh trống");
                Assert.That(centre.g, Is.LessThan(0.2f));
            }
            finally { Object.Destroy(tex); }
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
        }

        // L-026: tên không có đuôi thì tự thêm .png, có sẵn đuôi thì giữ nguyên (không thành .png.png).
        [UnityTest]
        public IEnumerator Capture_NameWithoutExtension_GetsPng_NameWithPngStaysSame()
        {
            yield return null;

            var bare = ScreenCapture1080.Capture("shot-bare", _dir);
            var named = ScreenCapture1080.Capture("shot-named.png", _dir);

            Assert.AreEqual("shot-bare.png", Path.GetFileName(bare), bare);
            Assert.IsTrue(File.Exists(bare), bare);
            Assert.AreEqual("shot-named.png", Path.GetFileName(named), named);
        }

        [UnityTest]
        public IEnumerator Capture_FailsAfterSwitchingCanvas_StillRestoresOverlay()
        {
            yield return null;
            var canvas = _canvasObject.GetComponent<Canvas>();
            // Đường dẫn thư mục trùng một file có thật: lỗi ghi xảy ra sau khi Canvas đã đổi chế độ.
            var blocker = Path.Combine(Path.GetTempPath(), "bdp-blocker-" + Path.GetRandomFileName());
            File.WriteAllText(blocker, "x");
            try
            {
                var result = ScreenCapture1080.Capture(null, blocker);

                Assert.That(result, Does.StartWith("ERROR:"), result);
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
                Assert.IsNull(Object.FindFirstObjectByType<Camera>(), "camera tạm phải bị huỷ");
            }
            finally { File.Delete(blocker); }
        }
    }
}
