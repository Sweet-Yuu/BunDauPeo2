using System;
using BunDauPeo.Core;
using BunDauPeo.UI;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    public class ResourceBarRuleTests
    {
        // Trang giả cài cả IHubPage lẫn IResourcePage; thuần C# vì assembly Tests chỉ có ở Editor.
        private sealed class FakeResourcePage : IHubPage, IResourcePage
        {
            public bool Open = true;
            public ResourceSlots PageSlots;

            public bool IsOpen => Open;
            public ResourceSlots Slots => PageSlots;

            public void Close() => Open = false;
        }

        [TearDown]
        public void ResetLanguage() => GameLanguage.Current = Language.Vi;

        [Test]
        public void NoPage_FollowsTab()
        {
            var none = Array.Empty<IHubPage>();

            Assert.AreEqual(ResourceSlots.All, ResourceBarRule.For(HubTab.Restaurant, false, none));
            Assert.AreEqual(ResourceSlots.Coin, ResourceBarRule.For(HubTab.Characters, false, none));
            Assert.AreEqual(ResourceSlots.Stamina, ResourceBarRule.For(HubTab.Stages, false, none));
            Assert.AreEqual(ResourceSlots.Gem, ResourceBarRule.For(HubTab.Gacha, false, none));
            Assert.AreEqual(ResourceSlots.None, ResourceBarRule.For(HubTab.Story, false, none));
        }

        [Test]
        public void MenuOpen_IsNone()
        {
            var materialPage = new FakeResourcePage { PageSlots = ResourceSlots.Material };

            foreach (HubTab tab in Enum.GetValues(typeof(HubTab)))
            {
                Assert.AreEqual(ResourceSlots.None, ResourceBarRule.For(tab, true, Array.Empty<IHubPage>()), $"{tab} without page");
                Assert.AreEqual(ResourceSlots.None, ResourceBarRule.For(tab, true, new IHubPage[] { materialPage }), $"{tab} with open page");
            }
        }

        [Test]
        public void OpenPages_OverrideTab_AndUnion()
        {
            var material = new FakeResourcePage { PageSlots = ResourceSlots.Material };
            Assert.AreEqual(ResourceSlots.Material, ResourceBarRule.For(HubTab.Restaurant, false, new IHubPage[] { material }), "page beats the tab");

            var plain = new FakeHubPage { Open = true };
            Assert.AreEqual(ResourceSlots.None, ResourceBarRule.For(HubTab.Restaurant, false, new IHubPage[] { plain }), "open page without IResourcePage counts as None");

            var stamina = new FakeResourcePage { PageSlots = ResourceSlots.Stamina };
            var coin = new FakeResourcePage { PageSlots = ResourceSlots.Coin };
            Assert.AreEqual(ResourceSlots.Stamina | ResourceSlots.Coin, ResourceBarRule.For(HubTab.Stages, false, new IHubPage[] { stamina, coin }), "two open pages union");

            var closed = new FakeResourcePage { Open = false, PageSlots = ResourceSlots.Gem };
            Assert.AreEqual(ResourceSlots.Stamina, ResourceBarRule.For(HubTab.Stages, false, new IHubPage[] { stamina, closed }), "closed page is ignored");
            Assert.AreEqual(ResourceSlots.Stamina, ResourceBarRule.For(HubTab.Stages, false, new IHubPage[] { closed }), "only closed pages: back to the tab rule");

            Assert.IsTrue(ResourceBarRule.IsLobbyLayout(HubTab.Restaurant, false, false));
            Assert.IsFalse(ResourceBarRule.IsLobbyLayout(HubTab.Restaurant, true, false), "menu open");
            Assert.IsFalse(ResourceBarRule.IsLobbyLayout(HubTab.Restaurant, false, true), "page open");
            Assert.IsFalse(ResourceBarRule.IsLobbyLayout(HubTab.Gacha, false, false), "other tab");
        }

        [Test]
        public void StaminaNext_Full_IsEmpty()
        {
            Assert.AreEqual("", ResourceBarRule.StaminaNext(100, 100, 0));
            Assert.AreEqual("", ResourceBarRule.StaminaNext(100, 100, 12));

            Assert.AreEqual("+1 sau 5:00", ResourceBarRule.StaminaNext(80, 100, 299.2), "seconds round up");

            GameLanguage.Current = Language.En;
            Assert.AreEqual("+1 in 5:00", ResourceBarRule.StaminaNext(80, 100, 299.2));
            Assert.AreEqual("", ResourceBarRule.StaminaNext(100, 100, 12), "full stays empty in English");
        }
    }
}
