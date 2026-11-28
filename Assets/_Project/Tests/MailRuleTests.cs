using System.Linq;
using BunDauPeo.Core;
using BunDauPeo.Missions;
using NUnit.Framework;

namespace BunDauPeo.Tests
{
    // Review Focus 5 của kế hoạch đợt 3: thư ghim không bị Xoá đã đọc xoá, và mất khi đã liên kết.
    public class MailRuleTests
    {
        private PlayerSave _save;
        private LocalAccountService _account;
        private MailService _mail;
        private int _persists;

        [SetUp]
        public void SetUp()
        {
            _save = new PlayerSave();
            _account = new LocalAccountService("a");
            _persists = 0;
            _mail = new MailService(_save, () => { _persists++; return true; }, new IMailSource[] { new LinkAccountMailSource(_account) }, _account);
        }

        private static MailItem Plain(string id) => new() { Id = id, Pinned = false };

        [Test]
        public void Pinned_ShownWhenNotLinked_HiddenWhenLinked()
        {
            Assert.AreEqual(new[] { LinkAccountMailSource.LinkId }, _mail.Items().Select(i => i.Id).ToArray());
            Assert.IsTrue(_mail.Items()[0].Pinned);
            Assert.IsFalse(_mail.Items()[0].HasAttachments);

            _account.LinkUserPassAsync("abc", "Abcdef1!").GetAwaiter().GetResult();

            Assert.IsEmpty(_mail.Items());
        }

        [Test]
        public void Pinned_Hidden_WhenNoLinkMethodAvailable()
        {
            var none = new NoMethodAccount();
            Assert.IsEmpty(new LinkAccountMailSource(none).Items());
        }

        [Test]
        public void DeleteRead_SkipsPinned()
        {
            var item = _mail.Items()[0];
            _mail.MarkRead(item);
            _persists = 0;

            _mail.DeleteRead();

            Assert.AreEqual(1, _mail.Items().Count, "the pinned mail stays");
            CollectionAssert.Contains(_save.MailRead, item.Id);
            Assert.AreEqual(0, _persists, "nothing was deleted so nothing is saved");
        }

        [Test]
        public void CanDeleteRead_False_WhenOnlyPinned()
        {
            var item = _mail.Items()[0];
            Assert.IsFalse(_mail.CanDeleteRead());
            _mail.MarkRead(item);
            Assert.IsFalse(_mail.CanDeleteRead(), "read but pinned");

            // Thư thường đã đọc thì xoá được (đợt 4 thêm nguồn thư quà).
            _save.MailRead.Add("gift_1");
            Assert.IsTrue(MailRule.CanDeleteRead(new[] { Plain("gift_1") }, _save));
            Assert.IsFalse(MailRule.CanDeleteRead(new[] { Plain("gift_2") }, _save), "unread plain mail is not deletable");
        }

        [Test]
        public void Unread_UntilOpened_ThenRead()
        {
            var item = _mail.Items()[0];
            Assert.IsTrue(_mail.Unread(item));
            Assert.AreEqual(1, _mail.UnreadCount());
            var changed = 0;
            _mail.Changed += () => changed++;

            _mail.MarkRead(item);
            _mail.MarkRead(item);

            Assert.IsFalse(_mail.Unread(item));
            Assert.AreEqual(0, _mail.UnreadCount());
            Assert.AreEqual(1, _persists, "saved once, the second open changes nothing");
            Assert.AreEqual(1, changed);
            Assert.AreEqual(1, _save.MailRead.Count(id => id == item.Id), "no duplicate ids");
        }

        [Test]
        public void Empty_WhenLinked_ShowsEmptyState()
        {
            _account.LinkUserPassAsync("abc", "Abcdef1!").GetAwaiter().GetResult();

            Assert.IsEmpty(_mail.Items());
            Assert.AreEqual(0, _mail.UnreadCount());
            Assert.IsFalse(_mail.CanDeleteRead());
        }

        [Test]
        public void Capacity_500()
        {
            Assert.AreEqual(500, MailRule.Capacity);
            var many = new ManySource(600);
            var visible = MailRule.Visible(new IMailSource[] { many });
            Assert.AreEqual(500, visible.Count);
        }

        [Test]
        public void PinnedComeFirst()
        {
            var order = MailRule.Visible(new IMailSource[] { new ManySource(2), new LinkAccountMailSource(_account) });
            Assert.AreEqual(LinkAccountMailSource.LinkId, order[0].Id);
            Assert.AreEqual("m0", order[1].Id);
        }

        [Test]
        public void Linking_RemovesMailImmediately()
        {
            var changed = 0;
            _mail.Changed += () => changed++;

            _account.LinkUserPassAsync("abc", "Abcdef1!").GetAwaiter().GetResult();

            Assert.AreEqual(1, changed, "the page redraws when the account changes");
            Assert.IsEmpty(_mail.Items());
        }

        [Test]
        public void MailRead_SurvivesLinkedThenLost()
        {
            // Đã đọc rồi mới liên kết: id vẫn nằm trong save (không phải trạng thái thư), không ảnh hưởng thư khác.
            _mail.MarkRead(_mail.Items()[0]);
            _account.LinkUserPassAsync("abc", "Abcdef1!").GetAwaiter().GetResult();
            Assert.AreEqual(1, _save.MailRead.Count);
        }

        private static MailItem GiftMail(string id, bool claimed, long expires = 100) => new()
        {
            Id = "gift:" + id, Action = MailAction.ClaimGift, HasAttachments = true, Claimed = claimed, ExpiresUtcTicks = expires,
        };

        [Test]
        public void GiftMail_Unclaimed_NotDeletable_EvenWhenRead()
        {
            var item = GiftMail("a", false);
            _save.MailRead.Add(item.Id);

            Assert.IsFalse(MailRule.CanDeleteRead(new[] { item }, _save));
            Assert.AreEqual(0, MailRule.DeleteRead(new[] { item }, _save));
            Assert.IsEmpty(_save.MailDeleted);
        }

        [Test]
        public void GiftMail_ClaimedAndRead_DeletedIntoMailDeleted()
        {
            var read = GiftMail("a", true);
            var unread = GiftMail("b", true);
            _save.MailRead.Add(read.Id);

            Assert.IsTrue(MailRule.CanDeleteRead(new[] { read, unread }, _save));
            Assert.AreEqual(1, MailRule.DeleteRead(new[] { read, unread }, _save));

            CollectionAssert.AreEqual(new[] { "gift:a" }, _save.MailDeleted);
        }

        [Test]
        public void DeleteRead_KeepsPinned()
        {
            var pinned = _mail.Items()[0];
            _mail.MarkRead(pinned);

            _mail.DeleteRead();

            Assert.IsEmpty(_save.MailDeleted);
            Assert.AreEqual(1, _mail.Items().Count);
        }

        [Test]
        public void RedDot_UnreadOrClaimable()
        {
            var read = Plain("p");
            _save.MailRead.Add("p");
            var items = new[] { read };

            Assert.IsFalse(MailRule.HasRedDot(items, _save, 0));
            Assert.IsTrue(MailRule.HasRedDot(items, _save, 1), "a claimable gift alone lights the dot");
            Assert.IsTrue(MailRule.HasRedDot(new[] { read, Plain("q") }, _save, 0), "an unread mail alone lights the dot");
        }

        [Test]
        public void GiftMail_InMailDeleted_NotRebuilt()
        {
            var service = GiftsWith("a", "b");
            _save.MailDeleted.Add("gift:a");

            var ids = new GiftMailSource(service, _save).Items().Select(i => i.Id).ToArray();

            Assert.AreEqual(new[] { "gift:b" }, ids);
        }

        [Test]
        public void GiftMail_Expired_NotShown_AndRewardsKept()
        {
            var service = GiftsWith("a");
            var item = new GiftMailSource(service, _save).Items().Single();
            Assert.IsFalse(item.Pinned);
            Assert.IsTrue(item.HasAttachments);
            Assert.AreEqual(MailAction.ClaimGift, item.Action);
            Assert.AreEqual(1, item.Rewards.Count);

            _nowTicks = long.MaxValue; // quá mọi hạn

            Assert.IsEmpty(new GiftMailSource(service, _save).Items());
        }

        [Test]
        public void Capacity500_StillCuts()
        {
            var visible = MailRule.Visible(new IMailSource[] { new ManySource(300), new ManySource(300) });
            Assert.AreEqual(500, visible.Count);
        }

        [Test]
        public void GiftMails_SortedByExpiry_BelowPinned()
        {
            var late = GiftMail("late", false, 300);
            var soon = GiftMail("soon", false, 100);
            var order = MailRule.Visible(new IMailSource[] { new ListSource(late, soon), new LinkAccountMailSource(_account) });

            CollectionAssert.AreEqual(new[] { LinkAccountMailSource.LinkId, "gift:soon", "gift:late" }, order.Select(i => i.Id).ToArray());
        }

        private long _nowTicks;

        // GiftService với danh sách dựng tay qua server giả: chỉ cần Gifts và ServerNowTicks.
        private GiftService GiftsWith(params string[] ids)
        {
            _nowTicks = 0;
            var server = new LocalGameServer(() => new System.DateTime(2026, 12, 31, 0, 0, 0, System.DateTimeKind.Utc), GachaRulesTests.Data());
            server.SignInAsync().GetAwaiter().GetResult();
            server.BootstrapAsync().GetAwaiter().GetResult();
            var start = new System.DateTime(2026, 12, 30, 0, 0, 0, System.DateTimeKind.Utc);
            server.Gifts = ids.Select(id => new BunDauPeo.Rules.GiftData
            {
                Id = id, TitleEn = id, TitleVi = id, StartUtcTicks = start.Ticks, EndUtcTicks = start.AddDays(14).Ticks,
                Rewards = { new BunDauPeo.Rules.GiftReward { Type = "Gem", Amount = 5 } },
            }).ToList();
            var service = new GiftService(server, new CommandGate(_ => new System.Threading.Tasks.TaskCompletionSource<bool>().Task),
                new Wallet(_save), _ => true, () => "en", () => _nowTicks == 0 ? new System.DateTime(2026, 12, 31, 0, 0, 0, System.DateTimeKind.Utc).Ticks : _nowTicks);
            service.RefreshAsync().GetAwaiter().GetResult();
            return service;
        }

        private sealed class ListSource : IMailSource
        {
            private readonly MailItem[] _items;
            public ListSource(params MailItem[] items) => _items = items;
            public System.Collections.Generic.IEnumerable<MailItem> Items() => _items;
        }

        private sealed class ManySource : IMailSource
        {
            private readonly int _count;
            public ManySource(int count) => _count = count;

            public System.Collections.Generic.IEnumerable<MailItem> Items()
            {
                for (var i = 0; i < _count; i++) yield return Plain("m" + i);
            }
        }

        private sealed class NoMethodAccount : IAccountService
        {
            public bool IsLinked => false;
            public System.Collections.Generic.IReadOnlyList<string> LinkedNames => System.Array.Empty<string>();
            public System.Collections.Generic.IReadOnlyList<AccountMethod> Available => System.Array.Empty<AccountMethod>();
            public event System.Action Changed { add { } remove { } }
            public System.Threading.Tasks.Task RefreshAsync() => System.Threading.Tasks.Task.CompletedTask;
            public System.Threading.Tasks.Task<AccountResult> LinkUserPassAsync(string user, string pass) => null;
            public System.Threading.Tasks.Task<AccountResult> LinkGoogleAsync() => null;
            public System.Threading.Tasks.Task<AccountResult> SignInUserPassAsync(string user, string pass) => null;
            public System.Threading.Tasks.Task<AccountResult> SignInGoogleAsync() => null;
        }
    }
}
