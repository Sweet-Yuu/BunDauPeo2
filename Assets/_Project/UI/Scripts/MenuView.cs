using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Panel Menu của hub (spec UI hub 5.6). Vỏ của đợt A1: Đóng và DEV chạy; Ngôn ngữ và Reset dữ liệu nối ở Task 4
    // (Reset chỉ chạy sau khi bấm Xoá hết, qua GameResetter, D-005). Component nằm trên MenuPanel nên OnEnable chạy mỗi lần mở Menu.
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
        }

        private void OnDisable()
        {
            if (_closeButton != null && _hub != null) _closeButton.onClick.RemoveListener(_hub.CloseMenu);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_devButton != null) _devButton.onClick.RemoveListener(OpenDev);
#endif
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
