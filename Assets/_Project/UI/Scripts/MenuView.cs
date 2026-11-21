using System;
using System.Threading.Tasks;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Panel Menu của hub (spec UI hub 5.6, Online 5.8): Ngôn ngữ, Reset dữ liệu (luôn hỏi, chỉ chạy sau Xoá hết, qua GameResetter, D-005),
    // Xoá tài khoản (hộp xác nhận riêng, qua AccountDeleter), DEV, Đóng.
    // Component nằm trên MenuPanel nên OnEnable chạy mỗi lần mở Menu.
    public class MenuView : MonoBehaviour
    {
        [SerializeField] private HubNav _hub;
        [SerializeField] private Button _languageButton;
        [SerializeField] private Text _languageLabel;
        [SerializeField] private Button _resetButton;
        // Khai báo ngoài #if: không [SerializeField] trong #if (lệch bố cục serialize).
        [SerializeField] private Button _devButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _statusText;
        [SerializeField] private GameObject _resetConfirmRoot;
        [SerializeField] private Button _resetYesButton;
        [SerializeField] private Button _resetNoButton;
        [SerializeField] private Button _deleteAccountButton;
        [SerializeField] private GameObject _deleteConfirmRoot;
        [SerializeField] private Button _deleteYesButton;
        [SerializeField] private Button _deleteNoButton;

        private LanguageSwitcher _language;
        private GameResetter _resetter;
        private AccountDeleter _deleter;
        // Đang chờ server: không chạy lệnh thứ hai (bấm lại, hay Thử lại khi lượt trước chưa xong).
        private bool _busy;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            _statusText.text = "";
            _resetConfirmRoot.SetActive(false);
            _deleteConfirmRoot.SetActive(false);
            _languageLabel.text = MenuViewText.LanguageLabel();
            _closeButton.onClick.AddListener(_hub.CloseMenu);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _devButton.onClick.AddListener(OpenDev);
#else
            _devButton.gameObject.SetActive(false);
#endif
            try
            {
                _language = ServiceLocator.Get<LanguageSwitcher>();
                _resetter = ServiceLocator.Get<GameResetter>();
                _deleter = ServiceLocator.Get<AccountDeleter>();
            }
            catch (InvalidOperationException e)
            {
                // Mở không qua Boot: Đóng và DEV vẫn chạy, Ngôn ngữ và Reset không nối.
                Debug.LogError($"{nameof(MenuView)}: {e.Message}", this);
                return;
            }

            _languageButton.onClick.AddListener(SwitchLanguage);
            _resetButton.onClick.AddListener(AskReset);
            _resetYesButton.onClick.AddListener(ConfirmReset);
            _resetNoButton.onClick.AddListener(CancelReset);
            _deleteAccountButton.onClick.AddListener(AskDelete);
            _deleteYesButton.onClick.AddListener(ConfirmDelete);
            _deleteNoButton.onClick.AddListener(CancelDelete);
        }

        private void OnDisable()
        {
            if (_closeButton != null && _hub != null) _closeButton.onClick.RemoveListener(_hub.CloseMenu);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_devButton != null) _devButton.onClick.RemoveListener(OpenDev);
#endif
            if (_languageButton != null) _languageButton.onClick.RemoveListener(SwitchLanguage);
            if (_resetButton != null) _resetButton.onClick.RemoveListener(AskReset);
            if (_resetYesButton != null) _resetYesButton.onClick.RemoveListener(ConfirmReset);
            if (_resetNoButton != null) _resetNoButton.onClick.RemoveListener(CancelReset);
            if (_deleteAccountButton != null) _deleteAccountButton.onClick.RemoveListener(AskDelete);
            if (_deleteYesButton != null) _deleteYesButton.onClick.RemoveListener(ConfirmDelete);
            if (_deleteNoButton != null) _deleteNoButton.onClick.RemoveListener(CancelDelete);
            _language = null;
            _resetter = null;
            _deleter = null;
        }

        // Đổi sang ngôn ngữ kia rồi nạp lại Restaurant: người chơi ở đúng ô với Menu mở, chữ do builder dựng vẽ lại qua LocalizedText.
        private void SwitchLanguage()
        {
            // Trang đang mở (Chọn đội...) đóng sạch trước, rồi mới lưu: Set ghi cả save, nên đội chọn dở phải được trả về
            // trước đó, nếu không file save giữ đội nửa chừng (spec Review Focus 2).
            _hub.CloseOpenPages();
            var next = GameLanguage.Current == Language.Vi ? Language.En : Language.Vi;
            if (!_language.Set(next))
            {
                _statusText.text = MenuViewText.LanguageNotSaved();
                return;
            }

            HubNav.OpenOnNextLoad(_hub.State.Current, menu: true);
            SceneManager.LoadScene(BattleView.RestaurantScene);
        }

        private void AskReset() => _resetConfirmRoot.SetActive(true);

        private void CancelReset() => _resetConfirmRoot.SetActive(false);

        // Chỉ chạy sau khi bấm Xoá hết (D-005). Thành công: về Lobby như game mới, không ô chờ.
        private void ConfirmReset()
        {
            _resetConfirmRoot.SetActive(false);
            // Trang đang mở (Chọn đội...) giữ ảnh chụp đội cũ: đóng trước, kẻo Huỷ sau Reset lỗi ghi đội cũ vào save mới (D-005, L-035).
            _hub.CloseOpenPages();
            _ = ResetAsync(_resetter);
        }

        private async Task ResetAsync(GameResetter resetter)
        {
            if (_busy || resetter == null) return;
            _busy = true;
            var status = await resetter.ResetAsync();
            _busy = false;
            if (status == OnlineStatus.Ok)
            {
                SceneManager.LoadScene(BattleView.RestaurantScene);
                return;
            }

            // Máy dưới MinProtocolVersion: hộp Cần cập nhật, không phải lỗi chung (như lệnh qua CommandGate).
            if (status == OnlineStatus.ClientTooOld)
            {
                OnlineOverlay.ShowTooOld();
                return;
            }

            // Server chưa xoá gì (spec 5.8): hộp Mất kết nối, Thử lại chạy lại Reset một lần.
            if (status == OnlineStatus.NetworkError)
            {
                OnlineOverlay.ShowLost(() =>
                {
                    OnlineOverlay.Hide();
                    _ = ResetAsync(resetter);
                });
                return;
            }

            // Không nói "dữ liệu giữ nguyên": ResetToDefaults cũng báo lỗi khi đã ghi đè bộ nhớ mà lưu lỗi.
            if (this != null) _statusText.text = MenuViewText.ResetFailed();
            Debug.LogError($"MenuView: Reset Save Data FAILED ({status}).");
        }

        private void AskDelete() => _deleteConfirmRoot.SetActive(true);

        private void CancelDelete() => _deleteConfirmRoot.SetActive(false);

        // Chỉ chạy sau khi bấm Xoá trong hộp xác nhận. Thành công: về Tiêu đề, chạm thì đăng nhập thành người chơi mới.
        private void ConfirmDelete()
        {
            _deleteConfirmRoot.SetActive(false);
            _hub.CloseOpenPages();
            _ = DeleteAsync(_deleter);
        }

        private async Task DeleteAsync(AccountDeleter deleter)
        {
            if (_busy || deleter == null) return;
            _busy = true;
            var status = await deleter.DeleteAsync();
            _busy = false;
            if (status == OnlineStatus.Ok)
            {
                SceneManager.LoadScene(TitleView.TitleScene);
                return;
            }

            // Tài khoản còn và bản đệm chưa xoá; dữ liệu server có thể đã xoá giữa chừng, nên Boot đã đẩy lại save trong bộ nhớ.
            Debug.LogWarning($"MenuView: delete account failed ({status}).");
            // Lỗi server (module hỏng) thì Thử lại ngay cũng lỗi: báo ở dòng trạng thái, không mở hộp chỉ có Thử lại kẻo kẹt vòng.
            if (status != OnlineStatus.NetworkError)
            {
                if (this != null) _statusText.text = MenuViewText.DeleteFailed();
                return;
            }

            OnlineOverlay.ShowLost(() =>
            {
                OnlineOverlay.Hide();
                _ = DeleteAsync(deleter);
            });
        }

        private bool HasAllReferences()
        {
            var ok = _hub != null && _languageButton != null && _languageLabel != null && _resetButton != null && _devButton != null
                     && _closeButton != null && _statusText != null && _resetConfirmRoot != null && _resetYesButton != null && _resetNoButton != null
                     && _deleteAccountButton != null && _deleteConfirmRoot != null && _deleteYesButton != null && _deleteNoButton != null;
            if (!ok) Debug.LogError($"{nameof(MenuView)}: references are not assigned.", this);
            return ok;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void OpenDev() => DevOverlay.OpenSettings();
#endif
    }
}
