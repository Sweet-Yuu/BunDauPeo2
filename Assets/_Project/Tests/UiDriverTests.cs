using System.Collections.Generic;
using BunDauPeo.Playtest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    public class UiDriverTests
    {
        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _made) if (go != null) Object.DestroyImmediate(go);
            _made.Clear();
        }

        private Button MakeButton(string name, string label = null, Transform parent = null)
        {
            var go = new GameObject(name);
            _made.Add(go);
            if (parent != null) go.transform.SetParent(parent, false);
            var button = go.AddComponent<Button>();
            if (label != null)
            {
                var child = new GameObject("Label");
                child.transform.SetParent(go.transform, false);
                child.AddComponent<Text>().text = label;
            }
            return button;
        }

        [Test]
        public void FindButtons_ByGameObjectName()
        {
            var a = MakeButton("UdtStartButton");
            MakeButton("UdtOther");
            var found = UiDriver.FindButtons("UdtStartButton");
            Assert.AreEqual(1, found.Count);
            Assert.AreSame(a, found[0]);
        }

        [Test]
        public void FindButtons_NoNameMatch_FallsBackToChildTextLabel()
        {
            var b = MakeButton("UdtNodeX", "Màn chơi UDT");
            MakeButton("UdtNodeY", "Khác");
            var found = UiDriver.FindButtons("Màn chơi UDT");
            Assert.AreEqual(1, found.Count);
            Assert.AreSame(b, found[0]);
        }

        [Test]
        public void FindButtons_IgnoresInactiveAndNonInteractable()
        {
            var off = MakeButton("UdtGone");
            off.gameObject.SetActive(false);
            var locked = MakeButton("UdtGone");
            locked.interactable = false;
            var parent = new GameObject("UdtHiddenParent");
            _made.Add(parent);
            MakeButton("UdtGone", null, parent.transform);
            parent.SetActive(false);
            Assert.AreEqual(0, UiDriver.FindButtons("UdtGone").Count);

            var ok = MakeButton("UdtGone");
            var found = UiDriver.FindButtons("UdtGone");
            Assert.AreEqual(1, found.Count);
            Assert.AreSame(ok, found[0]);
        }

        [Test]
        public void FindButtons_NameMatchWinsOverLabelMatch()
        {
            var a = MakeButton("UdtX");
            MakeButton("UdtB", "UdtX");
            var found = UiDriver.FindButtons("UdtX");
            Assert.AreEqual(1, found.Count);
            Assert.AreSame(a, found[0]);
        }

        [Test]
        public void Click_TwoMatches_ReturnsAmbiguousAndClicksNeither()
        {
            var root1 = new GameObject("UdtPanel1");
            var root2 = new GameObject("UdtPanel2");
            _made.Add(root1);
            _made.Add(root2);
            var a = MakeButton("UdtTwin", null, root1.transform);
            var b = MakeButton("UdtTwin", null, root2.transform);
            var clicks = 0;
            a.onClick.AddListener(() => clicks++);
            b.onClick.AddListener(() => clicks++);

            var result = UiDriver.Click("UdtTwin");

            Assert.AreEqual(ClickStatus.Ambiguous, result.Status);
            StringAssert.Contains("UdtPanel1/UdtTwin", result.Message);
            StringAssert.Contains("UdtPanel2/UdtTwin", result.Message);
            Assert.AreEqual(0, clicks);
        }

        [Test]
        public void Click_NoMatch_ReturnsNotFound_AndDisabledReturnsNotInteractable()
        {
            var missing = UiDriver.Click("UdtNoSuchButton");
            Assert.AreEqual(ClickStatus.NotFound, missing.Status);

            var locked = MakeButton("UdtLocked");
            locked.interactable = false;
            var clicks = 0;
            locked.onClick.AddListener(() => clicks++);
            var result = UiDriver.Click("UdtLocked");
            Assert.AreEqual(ClickStatus.NotInteractable, result.Status);
            Assert.AreEqual(0, clicks);
        }
    }
}
