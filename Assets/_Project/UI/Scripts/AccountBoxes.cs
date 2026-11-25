using System;
using System.Threading.Tasks;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Ba hộp tài khoản (spec đợt 3 8.3): chọn cách (Liên kết / Đăng nhập), tên + mật khẩu, chuyển tài khoản.
    // Dựng bằng code như OnlineOverlay để có trên Tiêu đề và Hồ sơ/Hộp thư mà không builder nào phải nhớ. Vẽ dưới OnlineOverlay
    // (hộp Mất kết nối nằm trên), nên lỗi mạng giữa chừng hiện hộp đó và Thử lại chạy lại đúng bước.
    public static class AccountBoxes
    {
        public const int SortingOrder = 1090;
        private static readonly Color Dim = new(0, 0, 0, 0.6f);
        private static readonly Color Panel = new(0.13f, 0.16f, 0.22f);
        private static readonly Color Gold = new(1f, 0.85f, 0.4f);
        private static readonly Color Green = new(0.22f, 0.55f, 0.32f);
        private static readonly Color Grey = new(0.35f, 0.38f, 0.45f);
        private static readonly Color Field = new(0.07f, 0.09f, 0.13f);
        private static readonly Color Red = new(1f, 0.55f, 0.5f);

        private static GameObject _root;
        private static GameObject _dim;
        private static GameObject _choose, _credential, _conflict, _confirm;
        private static Text _chooseTitle, _chooseNote, _credTitle, _credError, _credHint, _credActionLabel;
        private static Button _googleButton, _userPassButton, _credAction;
        private static InputField _user, _pass;
        private static RectTransform _googleRect, _userPassRect;

        // Chữ cố định của hộp: gán lại mỗi lần mở để đổi ngôn ngữ không để lại chữ cũ.
        private static readonly System.Collections.Generic.List<(Text Text, string Key)> Labels = new();

        private static AccountFlow _flow;
        private static bool _signIn;
        private static bool _busy;
        private static string _lastUser = "", _lastPass = "";
        private static AccountMethod _lastMethod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _root = null;
            _dim = null;
            Labels.Clear();
            _flow = null;
            _busy = false;
            _lastUser = "";
            _lastPass = "";
        }

        public static bool IsShown => _dim != null && _dim.activeSelf;

        // Dựng luồng từ service đã đăng ký (Boot); afterSwitch: về Tiêu đề (giữa game) hay vào game (ở Tiêu đề).
        public static AccountFlow NewFlow(Action afterSwitch) => new(
            ServiceLocator.Get<IAccountService>(), ServiceLocator.Get<AccountSwitcher>(), afterSwitch,
            () => ServiceLocator.Get<LocalProgress>().Exists);

        // Hộp Liên kết tài khoản (Hồ sơ, Hộp thư).
        public static void ShowLink(AccountFlow flow) => ShowChoose(flow, signIn: false);

        // Hộp Đăng nhập bằng tài khoản (Tiêu đề).
        public static void ShowSignIn(AccountFlow flow) => ShowChoose(flow, signIn: true);

        public static void Hide()
        {
            // _busy do lệnh đang chạy tự hạ khi xong (finally), không hạ ở đây để Huỷ rồi mở lại không chạy hai lệnh cùng lúc.
            if (_dim != null) _dim.SetActive(false);
        }

        // Luồng giữ cờ chờ: hộp chọn đóng khi người chơi bấm Huỷ, không phải khi đang gửi.
        private static void ShowChoose(AccountFlow flow, bool signIn)
        {
            Build();
            _flow = flow;
            _signIn = signIn;
            _chooseTitle.text = UiText.Get(signIn ? "account.choose_signin" : "account.choose_link");
            var warn = signIn && flow.NeedsLoseWarning;
            _chooseNote.text = warn ? UiText.Get("account.warn_lose") : "";
            var google = HasMethod(flow, AccountMethod.Google);
            _googleButton.gameObject.SetActive(google);
            // Chỉ còn một nút thì nút đó lên chỗ nút đầu.
            _userPassRect.anchoredPosition = new Vector2(0, (google ? -355 : -250) - 45);
            _googleRect.anchoredPosition = new Vector2(0, -250 - 45);
            Show(_choose);
        }

        private static bool HasMethod(AccountFlow flow, AccountMethod method)
        {
            foreach (var m in flow.Account.Available)
                if (m == method) return true;
            return false;
        }

        private static void Show(GameObject box)
        {
            _choose.SetActive(box == _choose);
            _credential.SetActive(box == _credential);
            _conflict.SetActive(box == _conflict);
            _confirm.SetActive(box == _confirm);
            foreach (var (text, key) in Labels) text.text = UiText.Get(key);
            _dim.SetActive(true);
        }

        private static void OnGoogle() => _ = RunAsync(AccountMethod.Google, "", "");

        private static void OnUserPass()
        {
            if (_busy) return;
            _credTitle.text = UiText.Get(_signIn ? "account.choose_signin" : "account.choose_link");
            _credActionLabel.text = UiText.Get(_signIn ? "account.signin" : "account.create");
            _user.SetTextWithoutNotify("");
            _pass.SetTextWithoutNotify("");
            _credError.text = "";
            _credHint.text = _signIn ? "" : UiText.Get("account.hint_pass");
            Show(_credential);
        }

        private static void OnCredentialAction()
        {
            if (_busy) return;
            var user = _user.text.Trim();
            var pass = _pass.text;
            if (!_signIn)
            {
                // Báo nhanh phía máy theo luật UGS; server vẫn là bên quyết định.
                if (!AccountRules.ValidName(user))
                {
                    _credError.text = UiText.Get("account.err_name");
                    return;
                }

                if (!AccountRules.StrongPassword(pass))
                {
                    _credError.text = UiText.Get("account.err_weak");
                    return;
                }
            }

            _ = RunAsync(AccountMethod.UserPass, user, pass);
        }

        private static async Task RunAsync(AccountMethod method, string user, string pass, bool confirmed = false)
        {
            if (_busy || _flow == null) return;
            _lastMethod = method;
            _lastUser = user;
            _lastPass = pass;
            if (_signIn && !confirmed && _flow.NeedsLoseWarning)
            {
                Show(_confirm);
                return;
            }

            // Từ hộp xác nhận quay về hộp đang làm để dòng đang gửi / lỗi hiện đúng chỗ.
            if (_confirm.activeSelf) Show(method == AccountMethod.UserPass ? _credential : _choose);
            _busy = true;
            SetInteractable(false);
            _credError.text = UiText.Get("online.sending");
            var flow = _flow;
            AccountResult result;
            try
            {
                result = _signIn
                    ? await flow.SignInAsync(method, user, pass, confirmedLoss: confirmed)
                    : await flow.LinkAsync(method, user, pass);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Account] {method} failed: {e}");
                result = new AccountResult(AccountStatus.Unavailable);
            }
            finally
            {
                _busy = false;
                if (_dim != null) SetInteractable(true);
            }

            if (_dim == null) return;

            switch (result.Status)
            {
                case AccountStatus.Ok:
                    Hide();
                    break;
                case AccountStatus.Offline:
                    // Không đổi gì; Thử lại chạy lại đúng bước này.
                    _credError.text = "";
                    OnlineOverlay.ShowLost(() =>
                    {
                        OnlineOverlay.Hide();
                        _ = RunAsync(method, user, pass, confirmed);
                    });
                    break;
                case AccountStatus.Conflict:
                    Show(_conflict);
                    break;
                case AccountStatus.Cancelled:
                    _credError.text = "";
                    break;
                default:
                    var key = result.Status switch
                    {
                        AccountStatus.UsernameTaken => "account.err_taken",
                        AccountStatus.WeakPassword => "account.err_weak",
                        AccountStatus.WrongCredentials => "account.err_wrong",
                        _ => "account.err_other",
                    };
                    // Lỗi hiện ở chỗ người chơi đang nhìn: dòng lỗi của hộp tên + mật khẩu, hay dòng ghi chú của hộp chọn.
                    if (_credential.activeSelf) _credError.text = UiText.Get(key);
                    else _chooseNote.text = UiText.Get(key);
                    break;
            }
        }

        // Chuyển = đăng nhập bằng danh tính đã thuộc người khác (đã xác nhận mất tiến độ ở hộp này).
        private static async Task SwitchAsync()
        {
            if (_busy || _flow == null) return;
            _busy = true;
            SetInteractable(false);
            AccountResult result;
            try
            {
                result = await _flow.SignInAsync(_lastMethod, _lastUser, _lastPass, confirmedLoss: true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Account] switch failed: {e}");
                result = new AccountResult(AccountStatus.Unavailable);
            }
            finally
            {
                _busy = false;
                if (_dim != null) SetInteractable(true);
            }

            if (_dim == null) return;

            if (result.Ok) Hide();
            else if (result.Status == AccountStatus.Offline)
                OnlineOverlay.ShowLost(() =>
                {
                    OnlineOverlay.Hide();
                    _ = SwitchAsync();
                });
            else
            {
                Show(_choose);
                _chooseNote.text = UiText.Get("account.err_other");
            }
        }

        private static void SetInteractable(bool value)
        {
            _googleButton.interactable = value;
            _userPassButton.interactable = value;
            _credAction.interactable = value;
        }

        private static void Build()
        {
            if (_root != null) return;
            // Hộp cũ đã bị huỷ (đổi scene, test dọn): bỏ chữ của nó.
            Labels.Clear();

            _root = new GameObject("AccountBoxes", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(_root);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var dim = NewRect("AccountDim", _root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddImage(dim, Dim, raycast: true);
            _dim = dim.gameObject;

            _choose = BuildChoose(dim, font);
            _credential = BuildCredential(dim, font);
            _conflict = BuildConflict(dim, font);
            _confirm = BuildConfirm(dim, font);
            _dim.SetActive(false);
        }

        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 Top = new(0.5f, 1f);
        private static readonly Vector2 Bottom = new(0.5f, 0f);

        private static GameObject BuildChoose(RectTransform dim, Font font)
        {
            var box = NewRect("AccountBox_PH_900x520", dim, Center, Center, Vector2.zero, new Vector2(900, 520));
            AddImage(box, Panel, raycast: true);
            _chooseTitle = NewText("Title", box, font, 52, Gold, Top, new Vector2(0, -60), new Vector2(840, 80));
            _chooseTitle.fontStyle = FontStyle.Bold;
            _chooseNote = NewText("Note", box, font, 30, Red, Top, new Vector2(0, -150), new Vector2(820, 70));

            (_googleButton, _googleRect) = NewButton("AccountGoogleBtn_PH_700x90", box, font, "account.google", Green, Top, new Vector2(0, -250), new Vector2(700, 90), OnGoogle);
            (_userPassButton, _userPassRect) = NewButton("AccountUserPassBtn_PH_700x90", box, font, "account.userpass", Green, Top, new Vector2(0, -355), new Vector2(700, 90), OnUserPass);
            NewButton("AccountCancelBtn_PH_300x80", box, font, "common.cancel", Grey, Bottom, new Vector2(0, 55), new Vector2(300, 80), Hide);
            return box.gameObject;
        }

        private static GameObject BuildCredential(RectTransform dim, Font font)
        {
            var box = NewRect("CredentialBox_PH_900x620", dim, Center, Center, Vector2.zero, new Vector2(900, 620));
            AddImage(box, Panel, raycast: true);
            _credTitle = NewText("Title", box, font, 52, Gold, Top, new Vector2(0, -60), new Vector2(840, 80));
            _credTitle.fontStyle = FontStyle.Bold;
            _user = NewInput("CredentialUser_PH_700x80", box, font, "account.username", Top, new Vector2(0, -165), false);
            _pass = NewInput("CredentialPass_PH_700x80", box, font, "account.password", Top, new Vector2(0, -265), true);
            _credHint = NewText("Hint", box, font, 26, new Color(0.7f, 0.75f, 0.85f), Top, new Vector2(0, -390), new Vector2(820, 60));
            _credError = NewText("Error", box, font, 32, Red, Top, new Vector2(0, -445), new Vector2(820, 50));
            (_credAction, _) = NewButton("CredentialActionBtn_PH_380x90", box, font, null, Green, Bottom, new Vector2(-200, 55), new Vector2(380, 90), OnCredentialAction);
            _credActionLabel = _credAction.GetComponentInChildren<Text>();
            NewButton("CredentialCancelBtn_PH_300x90", box, font, "common.cancel", Grey, Bottom, new Vector2(200, 55), new Vector2(300, 90), Hide);
            return box.gameObject;
        }

        // Hộp xác nhận mất tiến độ ẩn danh, hiện ngay trước khi gọi SignIn (spec đợt 3 8.3).
        private static GameObject BuildConfirm(RectTransform dim, Font font)
        {
            var box = NewRect("ConfirmLoseBox_PH_900x420", dim, Center, Center, Vector2.zero, new Vector2(900, 420));
            AddImage(box, Panel, raycast: true);
            var title = NewText("Title", box, font, 40, Gold, Top, new Vector2(0, -70), new Vector2(840, 80));
            title.fontStyle = FontStyle.Bold;
            Labels.Add((title, "account.confirm_title"));
            var body = NewText("Body", box, font, 30, Color.white, Top, new Vector2(0, -200), new Vector2(820, 140));
            Labels.Add((body, "account.warn_lose"));
            NewButton("ConfirmOkBtn_PH_340x100", box, font, "account.signin", Green, Bottom, new Vector2(-200, 50), new Vector2(340, 100),
                () => _ = RunAsync(_lastMethod, _lastUser, _lastPass, confirmed: true));
            NewButton("ConfirmCancelBtn_PH_340x100", box, font, "common.cancel", Grey, Bottom, new Vector2(200, 50), new Vector2(340, 100), Hide);
            return box.gameObject;
        }

        private static GameObject BuildConflict(RectTransform dim, Font font)
        {
            var box = NewRect("ConflictBox_PH_900x420", dim, Center, Center, Vector2.zero, new Vector2(900, 420));
            AddImage(box, Panel, raycast: true);
            var title = NewText("Title", box, font, 40, Gold, Top, new Vector2(0, -70), new Vector2(840, 80));
            title.fontStyle = FontStyle.Bold;
            Labels.Add((title, "account.conflict_title"));
            var body = NewText("Body", box, font, 34, Color.white, Top, new Vector2(0, -200), new Vector2(820, 140));
            Labels.Add((body, "account.conflict_body"));
            NewButton("ConflictSwitchBtn_PH_340x100", box, font, "account.switch", Green, Bottom, new Vector2(-200, 50), new Vector2(340, 100), () => _ = SwitchAsync());
            NewButton("ConflictCancelBtn_PH_340x100", box, font, "common.cancel", Grey, Bottom, new Vector2(200, 50), new Vector2(340, 100), Hide);
            return box.gameObject;
        }

        private static (Button, RectTransform) NewButton(string name, RectTransform parent, Font font, string labelKey, Color color,
            Vector2 anchor, Vector2 position, Vector2 size, Action onClick)
        {
            var rect = NewRect(name, parent, anchor, anchor, anchor == Bottom ? position + new Vector2(0, size.y / 2) : position - new Vector2(0, size.y / 2), size);
            var image = AddImage(rect, color, raycast: true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            var label = NewText("Label", rect, font, 36, Color.white, Center, Vector2.zero, size);
            if (labelKey != null) Labels.Add((label, labelKey));
            return (button, rect);
        }

        private static InputField NewInput(string name, RectTransform parent, Font font, string placeholderKey, Vector2 anchor, Vector2 position, bool password)
        {
            var rect = NewRect(name, parent, anchor, anchor, position - new Vector2(0, 40), new Vector2(700, 80));
            var image = AddImage(rect, Field, raycast: true);
            var text = NewText("Text", rect, font, 36, Color.white, Center, Vector2.zero, new Vector2(660, 80));
            text.alignment = TextAnchor.MiddleLeft;
            var placeholder = NewText("Placeholder", rect, font, 36, new Color(1, 1, 1, 0.4f), Center, Vector2.zero, new Vector2(660, 80));
            placeholder.alignment = TextAnchor.MiddleLeft;
            Labels.Add((placeholder, placeholderKey));
            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            input.characterLimit = 64;
            return input;
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image AddImage(RectTransform rect, Color color, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static Text NewText(string name, Transform parent, Font font, int size, Color color, Vector2 anchor, Vector2 position, Vector2 box)
        {
            var text = NewRect(name, parent, anchor, anchor, position, box).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }
    }
}
