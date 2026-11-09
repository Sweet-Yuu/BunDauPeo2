using System;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Panel Menu của hub (spec UI hub 5.6): Ngôn ngữ, Reset dữ liệu (luôn hỏi, chỉ chạy sau Xoá hết, qua GameResetter, D-005), DEV, Đóng.
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

        private LanguageSwitcher _language;
        private GameResetter _resetter;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            _statusText.text = "";
            _resetConfirmRoot.SetActive(false);
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
            _language = null;
            _resetter = null;
        }

        // Đổi sang ngôn ngữ kia rồi nạp lại Restaurant: người chơi ở đúng ô với Menu mở, chữ do builder dựng vẽ lại qua LocalizedText.
        private void SwitchLanguage()
        {
            var next = GameLanguage.Current == Language.Vi ? Language.En : Language.Vi;
            if (!_language.Set(next))
            {
                _statusText.text = MenuViewText.LanguageNotSaved();
                return;
            }

            // Trang đang mở (Chọn đội...) đóng sạch trước khi scene bị huỷ (spec Review Focus 2).
            _hub.CloseOpenPages();
            HubNav.OpenOnNextLoad(_hub.State.Current, menu: true);
            SceneManager.LoadScene(BattleView.RestaurantScene);
        }

        private void AskReset() => _resetConfirmRoot.SetActive(true);

        private void CancelReset() => _resetConfirmRoot.SetActive(false);

        // Chỉ chạy sau khi bấm Xoá hết (D-005). Thành công: về Lobby như game mới, không ô chờ.
        private void ConfirmReset()
        {
            _resetConfirmRoot.SetActive(false);
            if (_resetter.Reset())
            {
                SceneManager.LoadScene(BattleView.RestaurantScene);
                return;
            }

            // Không nói "dữ liệu giữ nguyên": ResetToDefaults cũng trả false khi đã ghi đè bộ nhớ mà lưu lỗi.
            _statusText.text = MenuViewText.ResetFailed();
            Debug.LogError("MenuView: Reset Save Data FAILED.", this);
        }

        private bool HasAllReferences()
        {
            var ok = _hub != null && _languageButton != null && _languageLabel != null && _resetButton != null && _devButton != null
                     && _closeButton != null && _statusText != null && _resetConfirmRoot != null && _resetYesButton != null && _resetNoButton != null;
            if (!ok) Debug.LogError($"{nameof(MenuView)}: references are not assigned.", this);
            return ok;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void OpenDev() => DevOverlay.OpenSettings();
#endif
    }
}
