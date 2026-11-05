using System.Collections;
using BunDauPeo.Playtest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BunDauPeo.PlayModeTests
{
    // Nhánh từ chối của UiDriver: nút bị che, raycast rỗng, bị CanvasGroup khoá. Smoke test chỉ đi đường Clicked.
    public class UiDriverPlayTests
    {
        private GameObject _root;
        private int _clicks;

        [SetUp]
        public void SetUp()
        {
            _clicks = 0;
            _root = new GameObject("UiDriverTestRoot");
            _root.AddComponent<EventSystem>();
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.transform.SetParent(_root.transform, false);
        }

        [TearDown]
        public void TearDown() => Object.Destroy(_root);

        private Transform CanvasTransform => _root.transform.Find("Canvas");

        private Button MakeButton(string name, Transform parent = null, bool raycastTarget = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent != null ? parent : CanvasTransform, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(100, 100);
            go.GetComponent<Image>().raycastTarget = raycastTarget;
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => _clicks++);
            return button;
        }

        [UnityTest]
        public IEnumerator Click_FreeButton_Clicked_AndRunsOnClick()
        {
            MakeButton("Free");
            yield return null;

            var result = UiDriver.Click("Free");

            Assert.AreEqual(ClickStatus.Clicked, result.Status, result.ToString());
            Assert.AreEqual(1, _clicks);
        }

        [UnityTest]
        public IEnumerator Click_CoveredByImage_Blocked_NamesCover_AndDoesNotClick()
        {
            MakeButton("Under");
            var cover = new GameObject("CoverPopup", typeof(RectTransform), typeof(Image));
            cover.transform.SetParent(CanvasTransform, false); // anh em đứng sau nên vẽ lên trên
            ((RectTransform)cover.transform).sizeDelta = new Vector2(300, 300);
            yield return null;

            var result = UiDriver.Click("Under");

            Assert.AreEqual(ClickStatus.Blocked, result.Status, result.ToString());
            Assert.That(result.Message, Does.Contain("CoverPopup"), "phải nêu tên vật che");
            Assert.AreEqual(0, _clicks, "nút bị che không được bấm xuyên");
        }

        [UnityTest]
        public IEnumerator Click_NothingHitAtCenter_Blocked_AndDoesNotClick()
        {
            MakeButton("Ghost", raycastTarget: false);
            yield return null;

            var result = UiDriver.Click("Ghost");

            Assert.AreEqual(ClickStatus.Blocked, result.Status, result.ToString());
            Assert.AreEqual(0, _clicks);
        }
    }
}
