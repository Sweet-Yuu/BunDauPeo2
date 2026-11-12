using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Màn Tiêu đề (spec UI hub C 5.1): chạm bất kỳ đâu vào Lobby. Chữ "Chạm để bắt đầu" nhấp nháy chậm; số phiên bản do view ghi.
    public class TitleView : MonoBehaviour
    {
        public const string TitleScene = "Title";

        [SerializeField] private Button _tapArea;
        [SerializeField] private Text _tapHint;
        [SerializeField] private Text _versionText;

        private bool _ready;
        // Chạm lần hai (cùng khung hay khung kế) không được nạp Restaurant lần nữa: sẽ có hai HubNav.
        private bool _entering;

        private void OnEnable()
        {
            _ready = _tapArea != null && _tapHint != null && _versionText != null;
            if (!_ready)
            {
                Debug.LogError($"{nameof(TitleView)}: references are not assigned.", this);
                return;
            }

            _versionText.text = UiText.Format("title.version", Application.version);
            _tapArea.onClick.AddListener(Enter);
        }

        private void OnDisable()
        {
            if (_tapArea != null) _tapArea.onClick.RemoveListener(Enter);
        }

        private void Update()
        {
            if (!_ready) return;

            var color = _tapHint.color;
            color.a = TitlePulse.Alpha(Time.unscaledTime);
            _tapHint.color = color;
        }

        public void Enter()
        {
            if (_entering) return;

            _entering = true;
            SceneManager.LoadScene(BattleView.RestaurantScene);
        }
    }
}
