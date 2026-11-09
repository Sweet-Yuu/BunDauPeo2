using System.Linq;
using System.Reflection;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using BunDauPeo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.Tests
{
    // HubNav dựng bằng tay trong EditMode; Awake/OnEnable gọi bằng reflection (L-029).
    public class HubNavTests
    {
        private GameObject _root;
        private HubNav _hub;
        private RestaurantService _service;
        private GameObject _characters, _stages, _gacha, _menu, _bar, _toast;

        [SetUp]
        public void SetUp()
        {
            HubPending.TryTake(out _, out _);
            var save = new PlayerSave();
            var catalog = TestData.Catalog(RestaurantTestData.Worker("cook0", 0, 0, 0));
            _service = new RestaurantService(RestaurantTestData.Config(null, RestaurantTestData.Incident(StaffRole.Cook)), save, new Wallet(save),
                new Roster(save, catalog), new ScriptedRandom(0.0), new FakeClock(), () => true);
            ServiceLocator.Register(_service);

            _root = new GameObject("HubNavTest");
            GameObject Child(string name, bool active)
            {
                var go = new GameObject(name);
                go.transform.SetParent(_root.transform);
                go.SetActive(active);
                return go;
            }

            _characters = Child("c", false);
            _stages = Child("s", false);
            _gacha = Child("g", false);
            _menu = Child("m", false);
            _bar = Child("bar", true);
            _toast = Child("toast", false);
            var buttons = Enumerable.Range(0, 6).Select(i =>
            {
                var go = Child("tab" + i, true);
                var button = go.AddComponent<Button>();
                button.targetGraphic = go.AddComponent<Image>();
                return button;
            }).ToArray();

            _hub = Child("hub", false).AddComponent<HubNav>();
            var so = new SerializedObject(_hub);
            so.FindProperty("_charactersPanel").objectReferenceValue = _characters;
            so.FindProperty("_stagesPanel").objectReferenceValue = _stages;
            so.FindProperty("_gachaPanel").objectReferenceValue = _gacha;
            so.FindProperty("_menuPanel").objectReferenceValue = _menu;
            so.FindProperty("_bar").objectReferenceValue = _bar;
            so.FindProperty("_comingSoonToast").objectReferenceValue = _toast;
            so.FindProperty("_tabButtons").arraySize = buttons.Length;
            for (var i = 0; i < buttons.Length; i++) so.FindProperty("_tabButtons").GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            so.FindProperty("_pages").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();

            Call("Awake");
            Call("OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            ServiceLocator.Clear();
            HubPending.TryTake(out _, out _);
        }

        private void Call(string method) => typeof(HubNav).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_hub, null);

        private void AssertOnly(GameObject open, string message)
        {
            foreach (var panel in new[] { _characters, _stages, _gacha })
                Assert.AreEqual(panel == open, panel.activeSelf, $"{message}: {panel.name}");
        }

        [Test]
        public void Select_TogglesPanels_AndMenu()
        {
            AssertOnly(null, "start");

            _hub.Select(HubTab.Gacha);
            AssertOnly(_gacha, "Gacha");

            _hub.Select(HubTab.Stages);
            AssertOnly(_stages, "Stages");

            _hub.Select(HubTab.Menu);
            Assert.IsTrue(_menu.activeSelf);
            AssertOnly(_stages, "Menu keeps the panel below");

            _hub.Select(HubTab.Story);
            Assert.IsTrue(_toast.activeSelf, "Story says coming soon");
            AssertOnly(_stages, "Story changes nothing");
            Assert.IsTrue(_menu.activeSelf);

            _hub.Select(HubTab.Characters);
            AssertOnly(_characters, "Characters");
            Assert.IsFalse(_menu.activeSelf, "a tab closes the Menu");

            _hub.Select(HubTab.Restaurant);
            AssertOnly(null, "Restaurant");
        }

        // Bấm ô khác: chỉ trang đang mở bị đóng, đóng qua Close() của chính nó (Review Focus 1; PlayMode vòng ô kiểm lại với trang thật).
        [Test]
        public void ClosePages_ClosesOnlyOpenPages()
        {
            var open = new FakeHubPage { Open = true };
            var closed = new FakeHubPage();

            HubNav.ClosePages(new IHubPage[] { open, closed });

            Assert.AreEqual(1, open.Closed);
            Assert.IsFalse(open.IsOpen);
            Assert.AreEqual(0, closed.Closed, "a page that is not open is left alone");
        }

        [Test]
        public void ClockPaused_FollowsTabAndMenu_AndReleasedOnDisable()
        {
            var incidents = _service.Incidents;
            incidents.SetPresent(true);
            incidents.Update(60f);
            Assert.IsTrue(incidents.Active.HasValue, "setup: an incident is running");
            incidents.Update(3f);
            Assert.AreEqual(3f, incidents.Active.Value.Elapsed, 1e-4f);

            _hub.Select(HubTab.Gacha);
            incidents.Update(10f);
            Assert.AreEqual(3f, incidents.Active.Value.Elapsed, 1e-4f, "a panel is open");

            _hub.Select(HubTab.Restaurant);
            incidents.Update(1f);
            Assert.AreEqual(4f, incidents.Active.Value.Elapsed, 1e-4f, "the restaurant is showing");

            _hub.Select(HubTab.Menu);
            incidents.Update(5f);
            Assert.AreEqual(4f, incidents.Active.Value.Elapsed, 1e-4f, "the Menu covers the restaurant");

            _hub.CloseMenu();
            incidents.Update(1f);
            Assert.AreEqual(5f, incidents.Active.Value.Elapsed, 1e-4f, "the Menu closed");

            _hub.Select(HubTab.Characters);
            Call("OnDisable");
            incidents.Update(1f);
            Assert.AreEqual(6f, incidents.Active.Value.Elapsed, 1e-4f, "leaving the scene gives the clock back");
        }

        [Test]
        public void Awake_AppliesPendingTabAndMenu_Once()
        {
            Call("OnDisable");
            HubPending.Set(HubTab.Stages, true);

            Call("Awake");

            Assert.AreEqual(HubTab.Stages, _hub.State.Current);
            Assert.IsTrue(_hub.State.MenuOpen);
            AssertOnly(_stages, "pending Stages");
            Assert.IsTrue(_menu.activeSelf);
            Assert.IsFalse(HubPending.TryTake(out _, out _), "taken once");
        }

        [Test]
        public void SetBarHidden_NeedsEveryOwnerToRelease()
        {
            var a = new object();
            var b = new object();

            _hub.SetBarHidden(a, true);
            _hub.SetBarHidden(b, true);
            Assert.IsFalse(_bar.activeSelf);

            _hub.SetBarHidden(a, false);
            Assert.IsFalse(_bar.activeSelf, "the other owner still hides it");

            _hub.SetBarHidden(b, false);
            Assert.IsTrue(_bar.activeSelf);
            Assert.Throws<System.ArgumentNullException>(() => _hub.SetBarHidden(null, true));
        }
    }
}
