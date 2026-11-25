using System;
using System.Threading.Tasks;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Màn Tiêu đề (spec UI hub C 5.1, Online 5.4): chạm bất kỳ đâu → "Đang kết nối…" → dựng session từ server → Lobby.
    // Lỗi ở bước nào thì hộp Không kết nối được; Thử lại chạy tiếp từ bước đó (IGameStarter giữ bước đã xong).
    public class TitleView : MonoBehaviour
    {
        public const string TitleScene = "Title";

        [SerializeField] private Button _tapArea;
        [SerializeField] private Text _tapHint;
        [SerializeField] private Text _versionText;
        // Nút Đăng nhập bằng tài khoản (Online đợt 3, spec 8.3); không bắt buộc để scene dựng trước đợt 3 vẫn chạy.
        [SerializeField] private Button _accountButton;

        private bool _ready;
        // Chạm thêm khi đang kết nối, hay chạm lần hai cùng khung, không được dựng session hay nạp Restaurant lần nữa: sẽ có hai HubNav.
        private bool _connecting;
        private bool _entering;

        private void OnEnable()
        {
            _ready = _tapArea != null && _tapHint != null && _versionText != null;
            if (!_ready)
            {
                Debug.LogError($"{nameof(TitleView)}: references are not assigned.", this);
                return;
            }

            _tapHint.text = UiText.Get("title.tap_to_start");
            _versionText.text = UiText.Format("title.version", Application.version);
            _tapArea.onClick.AddListener(Enter);
            if (_accountButton != null) _accountButton.onClick.AddListener(OpenSignIn);
        }

        private void OnDisable()
        {
            if (_tapArea != null) _tapArea.onClick.RemoveListener(Enter);
            if (_accountButton != null) _accountButton.onClick.RemoveListener(OpenSignIn);
        }

        private void Update()
        {
            if (!_ready) return;

            var color = _tapHint.color;
            // Đang kết nối thì chữ đứng yên, không nhấp nháy.
            color.a = _connecting || _entering ? 1f : TitlePulse.Alpha(Time.unscaledTime);
            _tapHint.color = color;
            // Ẩn lúc "Đang kết nối…" và khi đã vào game: không đổi người chơi giữa chừng một lượt kết nối.
            if (_accountButton != null) _accountButton.gameObject.SetActive(!_connecting && !_entering);
        }

        // Đăng nhập xong (session cũ đã bỏ) thì vào game như chạm; chạm lúc đó an toàn vì Enter tự chặn chạm lặp.
        private void OpenSignIn()
        {
            if (_connecting || _entering) return;
            try
            {
                AccountBoxes.ShowSignIn(AccountBoxes.NewFlow(Enter));
            }
            catch (InvalidOperationException e)
            {
                // Mở Tiêu đề không qua Boot: không có dịch vụ tài khoản.
                Debug.LogError($"{nameof(TitleView)}: {e.Message}", this);
            }
        }

        public void Enter()
        {
            if (_connecting || _entering) return;
            _ = ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            _connecting = true;
            _tapHint.text = UiText.Get("online.connecting");
            IGameStarter starter;
            try
            {
                starter = ServiceLocator.Get<IGameStarter>();
            }
            catch (InvalidOperationException e)
            {
                // Mở Tiêu đề không qua Boot: không có server để đăng nhập.
                Debug.LogError($"{nameof(TitleView)}: {e.Message}", this);
                _connecting = false;
                return;
            }

            OnlineOverlay.Watch(starter);
            // Cổng lệnh có từ lúc dựng starter (qua RegisterBase), nên hộp và màn chờ nghe được cả lệnh đầu tiên của game.
            var gate = ServiceLocator.Get<CommandGate>();
            OnlineOverlay.Watch(gate);
            WaitOverlay.Watch(gate);
            var status = await starter.StartAsync();
            // Scene đã đổi trong lúc chờ (vd. test nạp lại Tiêu đề): view cũ không làm gì nữa.
            if (this == null) return;
            _connecting = false;

            if (status != OnlineStatus.Ok)
            {
                if (status == OnlineStatus.ClientTooOld) OnlineOverlay.ShowTooOld();
                else OnlineOverlay.ShowConnectFailed(Retry);
                return;
            }

            _entering = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Panel DEV cần service game, nên chỉ dựng được sau khi có session.
            DevOverlay.Build();
#endif
            SceneManager.LoadScene(BattleView.RestaurantScene);
        }

        private void Retry()
        {
            OnlineOverlay.Hide();
            Enter();
        }
    }
}
