using System.Collections.Generic;
using BunDauPeo.Core;

namespace BunDauPeo.Missions
{
    public enum MailAction { None, LinkAccount }

    // Một thư trong Hộp thư (spec đợt 3 8.2). Id ổn định: MailRead của save lưu các Id đã mở.
    public sealed class MailItem
    {
        public string Id = "";
        public string TitleKey = "";
        public string BodyKey = "";
        public bool Pinned;
        public bool HasAttachments;
        public MailAction Action;
    }

    // Nguồn thư: đợt 3 chỉ có LinkAccountMailSource; đợt 4 thêm nguồn thư quà từ server mà không sửa trang.
    public interface IMailSource
    {
        IEnumerable<MailItem> Items();
    }

    // Thư ghim nhắc liên kết: chỉ khi chưa liên kết và còn cách để liên kết. Liên kết xong là mất ngay (không cần Xoá).
    public sealed class LinkAccountMailSource : IMailSource
    {
        public const string LinkId = "link_account";
        private readonly IAccountService _account;

        public LinkAccountMailSource(IAccountService account) => _account = account ?? throw new System.ArgumentNullException(nameof(account));

        public IEnumerable<MailItem> Items()
        {
            if (_account.IsLinked || _account.Available.Count == 0) yield break;
            yield return new MailItem
            {
                Id = LinkId, TitleKey = "mail.link_title", BodyKey = "mail.link_body", Pinned = true, Action = MailAction.LinkAccount,
            };
        }
    }

    public static class MailRule
    {
        public const int Capacity = 500;

        // Thư ghim lên đầu, giữ thứ tự của nguồn trong mỗi nhóm; quá sức chứa thì cắt cuối.
        public static IReadOnlyList<MailItem> Visible(IEnumerable<IMailSource> sources)
        {
            var pinned = new List<MailItem>();
            var rest = new List<MailItem>();
            foreach (var source in sources)
                foreach (var item in source.Items())
                    (item.Pinned ? pinned : rest).Add(item);

            pinned.AddRange(rest);
            if (pinned.Count > Capacity) pinned.RemoveRange(Capacity, pinned.Count - Capacity);
            return pinned;
        }

        public static bool Unread(MailItem item, PlayerSave save) => !save.MailRead.Contains(item.Id);

        public static int UnreadCount(IEnumerable<MailItem> items, PlayerSave save)
        {
            var count = 0;
            foreach (var item in items)
                if (Unread(item, save)) count++;
            return count;
        }

        // Thư ghim không xoá được; chỉ thư thường đã đọc mới xoá được.
        public static bool CanDeleteRead(IEnumerable<MailItem> items, PlayerSave save)
        {
            foreach (var item in items)
                if (!item.Pinned && !Unread(item, save)) return true;
            return false;
        }

        // Đợt 3 chưa có thư thường nào (nguồn duy nhất là thư ghim) nên không xoá gì; đợt 4 nối kho thư quà vào đây.
        // shortcut: trả số thư xoá được chứ chưa xoá, vì chưa có kho thư; nâng khi nguồn thư quà có chỗ lưu.
        public static int DeleteRead(IEnumerable<MailItem> items, PlayerSave save)
        {
            var count = 0;
            foreach (var item in items)
                if (!item.Pinned && !Unread(item, save)) count++;
            return count;
        }

        // Mở thư = đã đọc. Trả true khi vừa đổi (người gọi lưu save).
        public static bool MarkRead(MailItem item, PlayerSave save)
        {
            if (!Unread(item, save)) return false;
            save.MailRead.Add(item.Id);
            return true;
        }
    }
}
