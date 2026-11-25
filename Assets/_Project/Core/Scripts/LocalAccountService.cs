using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BunDauPeo.Core
{
    // Kho danh tính giả dùng chung: hai LocalAccountService chung kho là hai người chơi trên cùng một "server".
    public sealed class LocalAccountDirectory
    {
        internal readonly Dictionary<string, (string Owner, string Pass)> Identities = new();
    }

    // IAccountService cho test và Editor Local: không mạng, không SDK. Danh tính chỉ sống trong bộ nhớ.
    // shortcut: không lưu qua lần Play sau, đủ cho thử luồng; cần bền thì ghi vào bản đệm save.
    public sealed class LocalAccountService : IAccountService
    {
        private readonly LocalAccountDirectory _directory;
        private readonly List<string> _linked = new();
        private readonly IReadOnlyList<AccountMethod> _available;

        public LocalAccountService(string playerId = "local-player", LocalAccountDirectory directory = null, bool google = false)
        {
            PlayerId = playerId;
            _directory = directory ?? new LocalAccountDirectory();
            _available = google ? new[] { AccountMethod.Google, AccountMethod.UserPass } : new[] { AccountMethod.UserPass };
        }

        public string PlayerId { get; private set; }

        // Danh tính Google của máy này (tên tài khoản Google giả).
        public string GoogleAccount { get; set; } = "google-user";

        // Giả lập mất mạng: mọi lệnh trả Offline và không đổi gì.
        public bool Offline { get; set; }

        // Giả lập mất trả lời: lệnh Liên kết tên + mật khẩu kế tiếp đã xong trên "server" nhưng máy nhận Offline (như UgsAccountService).
        public bool LoseNextLinkReply { get; set; }

        public bool IsLinked => _linked.Count > 0;
        public IReadOnlyList<string> LinkedNames => _linked;
        public IReadOnlyList<AccountMethod> Available => _available;
        public event Action Changed;

        public Task RefreshAsync() => Task.CompletedTask;

        public Task<AccountResult> LinkUserPassAsync(string user, string pass)
        {
            if (Offline) return Done(AccountStatus.Offline);
            if (!AccountRules.ValidName(user)) return Done(AccountStatus.WrongCredentials);
            if (!AccountRules.StrongPassword(pass)) return Done(AccountStatus.WeakPassword);
            var key = NameKey(user);
            // Lần liên kết trước đã xong mà mất trả lời: đọc lại thì tên đã là của người này, trả Ok (như UgsAccountService).
            var mine = _directory.Identities.TryGetValue(key, out var owned) && owned.Owner == PlayerId;
            if (_directory.Identities.ContainsKey(key) && !mine) return Done(AccountStatus.UsernameTaken);
            if (!mine && _linked.Exists(n => n != "Google")) return Done(AccountStatus.Unavailable);

            _directory.Identities[key] = (PlayerId, pass);
            if (LoseNextLinkReply)
            {
                LoseNextLinkReply = false;
                return Done(AccountStatus.Offline);
            }

            if (mine && AccountRules.HasLinkedName(_linked, user)) return Done(AccountStatus.Ok);
            _linked.Add(user);
            Changed?.Invoke();
            return Done(AccountStatus.Ok);
        }

        public Task<AccountResult> LinkGoogleAsync()
        {
            if (Offline) return Done(AccountStatus.Offline);
            if (!Has(AccountMethod.Google)) return Done(AccountStatus.Unavailable);
            var key = GoogleKey();
            if (_directory.Identities.TryGetValue(key, out var owner))
                return Done(owner.Owner == PlayerId ? AccountStatus.Ok : AccountStatus.Conflict);

            _directory.Identities[key] = (PlayerId, "");
            _linked.Add("Google");
            Changed?.Invoke();
            return Done(AccountStatus.Ok);
        }

        public Task<AccountResult> SignInUserPassAsync(string user, string pass)
        {
            if (Offline) return Done(AccountStatus.Offline);
            if (user == null || !_directory.Identities.TryGetValue(NameKey(user), out var id) || id.Pass != pass)
                return Done(AccountStatus.WrongCredentials);

            SwitchTo(id.Owner);
            return Done(AccountStatus.Ok);
        }

        public Task<AccountResult> SignInGoogleAsync()
        {
            if (Offline) return Done(AccountStatus.Offline);
            if (!Has(AccountMethod.Google)) return Done(AccountStatus.Unavailable);
            if (!_directory.Identities.TryGetValue(GoogleKey(), out var id)) return Done(AccountStatus.WrongCredentials);

            SwitchTo(id.Owner);
            return Done(AccountStatus.Ok);
        }

        // Người chơi mới = tên liên kết của người đó (đọc lại từ kho).
        private void SwitchTo(string owner)
        {
            PlayerId = owner;
            _linked.Clear();
            foreach (var pair in _directory.Identities)
                if (pair.Value.Owner == owner) _linked.Add(pair.Key.StartsWith("g:") ? "Google" : pair.Key.Substring(2));
            Changed?.Invoke();
        }

        private bool Has(AccountMethod method)
        {
            foreach (var m in _available)
                if (m == method) return true;
            return false;
        }

        private string GoogleKey() => "g:" + GoogleAccount;
        private static string NameKey(string user) => "u:" + user.ToLowerInvariant();
        private static Task<AccountResult> Done(AccountStatus status) => Task.FromResult(new AccountResult(status));
    }
}
