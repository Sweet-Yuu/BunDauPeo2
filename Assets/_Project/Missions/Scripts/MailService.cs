using System;
using System.Collections.Generic;
using BunDauPeo.Core;

namespace BunDauPeo.Missions
{
    // Hộp thư của một phiên: nguồn thư + trạng thái đã đọc (PlayerSave.MailRead, D-005). Không gọi server.
    public sealed class MailService : IDisposable
    {
        private readonly PlayerSave _save;
        private readonly Func<bool> _persist;
        private readonly IReadOnlyList<IMailSource> _sources;
        private readonly IAccountService _account;
        private readonly GiftService _gifts;

        public MailService(PlayerSave save, Func<bool> persist, IReadOnlyList<IMailSource> sources, IAccountService account, GiftService gifts = null)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
            // Liên kết xong thì thư ghim mất ngay; trang đang mở vẽ lại.
            _account = account;
            if (account != null) account.Changed += OnAccountChanged;
            // Quà đổi (làm mới, nhận) thì trang và chấm đỏ vẽ lại.
            _gifts = gifts;
            if (gifts != null) gifts.Changed += OnAccountChanged;
        }

        public event Action Changed;

        // Service của phiên cũ phải gỡ khỏi account (sống qua Shutdown) kẻo handler cũ cộng dồn mỗi phiên.
        public void Dispose()
        {
            if (_account != null) _account.Changed -= OnAccountChanged;
            if (_gifts != null) _gifts.Changed -= OnAccountChanged;
        }

        private void OnAccountChanged() => Changed.SafeInvoke();

        public IReadOnlyList<MailItem> Items() => MailRule.Visible(_sources);

        public int UnreadCount() => MailRule.UnreadCount(Items(), _save);

        public bool Unread(MailItem item) => MailRule.Unread(item, _save);

        // Chấm đỏ nút Hộp thư: thư chưa đọc hoặc quà chưa nhận.
        public bool HasRedDot() => MailRule.HasRedDot(Items(), _save, _gifts?.ClaimableCount ?? 0);

        public bool CanDeleteRead() => MailRule.CanDeleteRead(Items(), _save);

        public void DeleteRead()
        {
            if (MailRule.DeleteRead(Items(), _save) > 0) _persist();
            Changed.SafeInvoke();
        }

        // Lưu lỗi thì vẫn đã đọc trong bộ nhớ (như tên Hồ sơ); lần Persist sau ghi lại.
        public void MarkRead(MailItem item)
        {
            if (!MailRule.MarkRead(item, _save)) return;
            _persist();
            Changed.SafeInvoke();
        }
    }
}
