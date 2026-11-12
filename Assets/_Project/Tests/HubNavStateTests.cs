using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class HubNavStateTests
    {
        [SetUp]
        public void SetUp() => HubPending.TryTake(out _, out _);

        [TearDown]
        public void TearDown() => HubPending.TryTake(out _, out _);

        [Test]
        public void Default_IsRestaurant_MenuClosed()
        {
            var state = new HubNavState();

            Assert.AreEqual(HubTab.Restaurant, state.Current);
            Assert.IsFalse(state.MenuOpen);
            Assert.AreEqual(HubTab.Restaurant, state.Highlighted);
        }

        [Test]
        public void Select_Tab_ChangesCurrent_ClosesMenu()
        {
            var state = new HubNavState();
            state.Select(HubTab.Menu);

            Assert.AreEqual(HubNavResult.Changed, state.Select(HubTab.Gacha));
            Assert.AreEqual(HubTab.Gacha, state.Current);
            Assert.IsFalse(state.MenuOpen);
            Assert.AreEqual(HubNavResult.Changed, state.Select(HubTab.Gacha), "chọn lại ô đang đứng vẫn là Changed");
        }

        [Test]
        public void Select_Story_ComingSoon_NoChange()
        {
            var state = new HubNavState();
            state.Select(HubTab.Characters);

            Assert.AreEqual(HubNavResult.ComingSoon, state.Select(HubTab.Story));
            Assert.AreEqual(HubTab.Characters, state.Current);
            Assert.IsFalse(state.MenuOpen);
        }

        [Test]
        public void Select_Menu_KeepsCurrent_HighlightsMenu()
        {
            var state = new HubNavState();
            state.Select(HubTab.Stages);

            Assert.AreEqual(HubNavResult.MenuOpened, state.Select(HubTab.Menu));
            Assert.AreEqual(HubTab.Stages, state.Current);
            Assert.AreEqual(HubTab.Menu, state.Highlighted);

            state.CloseMenu();

            Assert.IsFalse(state.MenuOpen);
            Assert.AreEqual(HubTab.Stages, state.Highlighted);
        }

        [Test]
        public void Pending_TakenOnce()
        {
            HubPending.Set(HubTab.Stages, true);

            Assert.IsTrue(HubPending.TryTake(out var tab, out var menu));
            Assert.AreEqual(HubTab.Stages, tab);
            Assert.IsTrue(menu);
            Assert.IsFalse(HubPending.TryTake(out _, out _));
        }

        [Test]
        public void Pending_StageMap_TakenOnce()
        {
            HubPending.Set(HubTab.Stages, false, stageMap: true);

            Assert.IsTrue(HubPending.TryTake(out var tab, out var menu, out var stageMap));
            Assert.AreEqual(HubTab.Stages, tab);
            Assert.IsFalse(menu);
            Assert.IsTrue(stageMap);
            Assert.IsFalse(HubPending.TryTake(out _, out _, out stageMap));
            Assert.IsFalse(stageMap, "the flag is cleared with the rest");

            // Bản hai tham số vẫn lấy và xoá cả cờ bản đồ.
            HubPending.Set(HubTab.Stages, false, true);
            Assert.IsTrue(HubPending.TryTake(out _, out _));
            Assert.IsFalse(HubPending.TryTake(out _, out _, out stageMap));
            Assert.IsFalse(stageMap);

            // Không truyền cờ thì mặc định false.
            HubPending.Set(HubTab.Gacha, true);
            Assert.IsTrue(HubPending.TryTake(out _, out _, out stageMap));
            Assert.IsFalse(stageMap, "default is off");
        }

        [Test]
        public void FromPending_InvalidTab_IsRestaurant()
        {
            Assert.AreEqual(HubTab.Restaurant, HubNavState.FromPending(HubTab.Story, false).Current);
            Assert.AreEqual(HubTab.Restaurant, HubNavState.FromPending(HubTab.Menu, false).Current);

            var invalid = HubNavState.FromPending((HubTab)99, true);
            Assert.AreEqual(HubTab.Restaurant, invalid.Current);
            Assert.IsTrue(invalid.MenuOpen);

            var valid = HubNavState.FromPending(HubTab.Gacha, false);
            Assert.AreEqual(HubTab.Gacha, valid.Current);
            Assert.IsFalse(valid.MenuOpen);
        }
    }
}
