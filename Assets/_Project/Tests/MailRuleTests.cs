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
