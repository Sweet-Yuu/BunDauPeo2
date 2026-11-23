using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BunDauPeo.Core;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace BunDauPeo.Online
{
    // IAccountService trên UGS Authentication (3.8.1): tên đăng nhập + mật khẩu; Google ở Google qua IGoogleTokenProvider (Sign in with Google, không dùng Play Games).
    // Không bao giờ ép liên kết (cờ force của SDK): danh tính đã thuộc người khác thì trả Conflict để UI hỏi chuyển tài khoản.
    public sealed class UgsAccountService : IAccountService
    {
        private const string Log = "[Account]";
        private const string GoogleTypeId = "google-play-games";

        private readonly string _environment;
        private readonly List<string> _names = new();
        private readonly List<AccountMethod> _available = new() { AccountMethod.UserPass };
        private readonly IGoogleTokenProvider _google;

        // google = null: nền tảng chưa có SDK Google (Editor, WebGL hiện tại) nên không có nút Google.
        public UgsAccountService(string environment, IGoogleTokenProvider google = null)
        {
            _environment = string.IsNullOrEmpty(environment) ? throw new ArgumentException("environment is required", nameof(environment)) : environment;
            _google = google;
            if (google != null) _available.Insert(0, AccountMethod.Google);
        }

        public bool IsLinked => _names.Count > 0;
        public IReadOnlyList<string> LinkedNames => _names;
        public IReadOnlyList<AccountMethod> Available => _available;
        public event Action Changed;

        public async Task RefreshAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn) return;
                var info = await AuthenticationService.Instance.GetPlayerInfoAsync();
                _names.Clear();
                foreach (var identity in info.Identities)
                    if (identity.TypeId == GoogleTypeId) _names.Add("Google");
                if (!string.IsNullOrEmpty(info.Username)) _names.Add(info.Username);
            }
            catch (Exception e)
            {
                // Giữ danh sách cũ: lần vào Hồ sơ sau, hay lần đăng nhập sau, đọc lại.
                Debug.LogWarning($"{Log} refresh failed: {e.Message}");
            }
        }

        public Task<AccountResult> LinkUserPassAsync(string user, string pass) =>
            LinkAsync(AccountMethod.UserPass, () => AuthenticationService.Instance.AddUsernamePasswordAsync(user, pass));

        // Token lấy trước khi đụng tới UGS: đóng hộp chọn tài khoản Google không để lại gì dở.
        public async Task<AccountResult> LinkGoogleAsync()
        {
            var token = await TokenAsync(false);
            if (token.Status != AccountStatus.Ok) return new AccountResult(token.Status);
            return await LinkAsync(AccountMethod.Google, () => AuthenticationService.Instance.LinkWithGoogleAsync(token.Value));
        }

        public Task<AccountResult> SignInUserPassAsync(string user, string pass) =>
            SignInAsync(AccountMethod.UserPass, () => AuthenticationService.Instance.SignInWithUsernamePasswordAsync(user, pass));

        public async Task<AccountResult> SignInGoogleAsync()
        {
            var token = await TokenAsync(true);
            if (token.Status != AccountStatus.Ok) return new AccountResult(token.Status);
            return await SignInAsync(AccountMethod.Google, () => AuthenticationService.Instance.SignInWithGoogleAsync(token.Value));
        }

        private async Task<(AccountStatus Status, string Value)> TokenAsync(bool signIn)
        {
            if (_google == null) return (AccountStatus.Unavailable, null);
            try
            {
                var token = await _google.GetIdTokenAsync();
                return string.IsNullOrEmpty(token) ? (AccountStatus.Unavailable, null) : (AccountStatus.Ok, token);
            }
            catch (Exception e)
            {
                return (UgsErrors.MapAccount(e, AccountMethod.Google, signIn), null);
            }
        }

        private async Task<AccountResult> LinkAsync(AccountMethod method, Func<Task> link)
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
                    return new AccountResult(AccountStatus.Unavailable);
                await link();
            }
            catch (Exception e)
            {
                var status = UgsErrors.MapAccount(e, method, signIn: false);
                Debug.LogWarning($"{Log} link {method} failed ({status}): {e.Message}");
                return new AccountResult(status);
            }

            await RefreshAsync();
            Changed?.Invoke();
            return new AccountResult(AccountStatus.Ok);
        }

        // SDK chỉ cho đăng nhập khi đã đăng xuất: đăng xuất giữ token phiên ẩn danh, lỗi thì đăng nhập ẩn danh lại bằng token đó
        // để không để phiên nửa vời. Mất mạng đúng lúc đó thì Tiêu đề (UgsGameServer.SignInAsync) đăng nhập lại.
        private async Task<AccountResult> SignInAsync(AccountMethod method, Func<Task> signIn)
        {
            var auth = AuthenticationService.Instance;
            var wasSignedIn = false;
            try
            {
                await UgsGameServer.EnsureInitializedAsync(_environment);
                wasSignedIn = auth.IsSignedIn;
                if (wasSignedIn) auth.SignOut(false);
                await signIn();
            }
            catch (Exception e)
            {
                if (wasSignedIn && !auth.IsSignedIn)
                {
                    try
                    {
                        await auth.SignInAnonymouslyAsync();
                    }
                    catch (Exception restore)
                    {
                        Debug.LogWarning($"{Log} cannot restore previous session: {restore.Message}");
                    }
                }

                var status = UgsErrors.MapAccount(e, method, signIn: true);
                Debug.LogWarning($"{Log} sign in {method} failed ({status}): {e.Message}");
                return new AccountResult(status);
            }

            await RefreshAsync();
            Changed?.Invoke();
            return new AccountResult(AccountStatus.Ok);
        }
    }
}
