using BunDauPeo.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Menu tạm dừng: tên màn, thẻ cả đội (HP, năng lượng; chỉ xem), Tiếp tục, Bỏ cuộc (hỏi lại một lần). Chỉ BattleView mở; nó dừng hoạt hình khi IsOpen.
    public class BattlePausePopup : MonoBehaviour
    {
        // _root chặn chạm xuống trận; component nằm ở object luôn bật để còn nhận Open.
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _stageName;
        [SerializeField] private BattleCardView[] _cards;
        [SerializeField] private Button _resumeButton;
        // Cài đặt DEV nằm trong Menu thay cho nút góc màn (chỉ Editor và Development Build; bản phát hành ẩn nút).
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _surrenderButton;
        [SerializeField] private GameObject _confirmRoot;
        [SerializeField] private Text _confirmText;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        private BattleSession _session;

        public bool IsOpen => _root != null && _root.activeSelf;

        // maxEnergy: cùng giá trị BattleView đưa cho thẻ dưới trận (BattleSession.Config không public).
        // shownHp: máu đang hiển thị trên sân (BattleUnitView.ShownHp), để thẻ khớp hình khi mở Menu giữa lúc hiệu ứng còn chạy.
        public void Open(BattleSession session, int maxEnergy, System.Func<BattleUnit, int> shownHp)
        {
            if (session == null || !HasAllReferences()) return;

            _session = session;
            _stageName.text = session.Stage.DisplayName;
            for (var i = 0; i < _cards.Length; i++)
            {
                if (i >= session.Allies.Count)
                {
                    _cards[i].Hide();
                    continue;
                }

                var unit = session.Allies[i];
                _cards[i].Bind(unit);
                _cards[i].Button.interactable = false;
                _cards[i].Refresh(shownHp(unit), maxEnergy, false);
            }

#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            _settingsButton.gameObject.SetActive(false);
#endif
            _confirmText.text = CombatViewText.SurrenderConfirm();
            _confirmRoot.SetActive(false);
            _root.SetActive(true);
        }

        public void Close()
        {
            _session = null;
            if (_confirmRoot != null) _confirmRoot.SetActive(false);
            if (_root != null) _root.SetActive(false);
        }

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            _resumeButton.onClick.AddListener(Close);
            _settingsButton.onClick.AddListener(OpenSettings);
            _surrenderButton.onClick.AddListener(AskConfirm);
            _confirmNoButton.onClick.AddListener(HideConfirm);
            _confirmYesButton.onClick.AddListener(Surrender);
        }

        private void OnDisable()
        {
            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(Close);
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(OpenSettings);
            if (_surrenderButton != null) _surrenderButton.onClick.RemoveListener(AskConfirm);
            if (_confirmNoButton != null) _confirmNoButton.onClick.RemoveListener(HideConfirm);
            if (_confirmYesButton != null) _confirmYesButton.onClick.RemoveListener(Surrender);
        }

        private bool HasAllReferences()
        {
            var ok = _root != null && _stageName != null && _cards != null && _cards.Length == 5 && System.Array.TrueForAll(_cards, c => c != null)
                     && _resumeButton != null && _settingsButton != null && _surrenderButton != null && _confirmRoot != null && _confirmText != null
                     && _confirmYesButton != null && _confirmNoButton != null;
            if (!ok) Debug.LogError($"{nameof(BattlePausePopup)}: references are not assigned.", this);
            return ok;
        }

        private static void OpenSettings()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevOverlay.OpenSettings();
#endif
        }

        private void AskConfirm() => _confirmRoot.SetActive(true);

        private void HideConfirm() => _confirmRoot.SetActive(false);

        private void Surrender()
        {
            // Surrender chỉ làm trận thua; hoạt hình đang dở phát nốt rồi banner hiện.
            _session?.Surrender();
            Close();
        }
    }
}
