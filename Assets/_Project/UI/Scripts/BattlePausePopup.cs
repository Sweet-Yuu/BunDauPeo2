using BunDauPeo.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Menu tạm dừng: HP cả đội, Tiếp tục, Bỏ cuộc (hỏi lại một lần). Chỉ BattleView mở; nó dừng hoạt hình khi IsOpen.
    public class BattlePausePopup : MonoBehaviour
    {
        // _root chặn chạm xuống trận; component nằm ở object luôn bật để còn nhận Open.
        [SerializeField] private GameObject _root;
        [SerializeField] private Text[] _hpLines;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _surrenderButton;
        [SerializeField] private GameObject _confirmRoot;
        [SerializeField] private Text _confirmText;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        private BattleSession _session;

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Open(BattleSession session)
        {
            if (session == null || !HasAllReferences()) return;

            _session = session;
            for (var i = 0; i < _hpLines.Length; i++)
            {
                var show = i < session.Allies.Count;
                _hpLines[i].gameObject.SetActive(show);
                if (show) _hpLines[i].text = CombatViewText.PauseHpLine(session.Allies[i]);
            }

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
            _surrenderButton.onClick.AddListener(AskConfirm);
            _confirmNoButton.onClick.AddListener(HideConfirm);
            _confirmYesButton.onClick.AddListener(Surrender);
        }

        private void OnDisable()
        {
            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(Close);
            if (_surrenderButton != null) _surrenderButton.onClick.RemoveListener(AskConfirm);
            if (_confirmNoButton != null) _confirmNoButton.onClick.RemoveListener(HideConfirm);
            if (_confirmYesButton != null) _confirmYesButton.onClick.RemoveListener(Surrender);
        }

        private bool HasAllReferences()
        {
            var ok = _root != null && _hpLines != null && _hpLines.Length == 5 && System.Array.TrueForAll(_hpLines, t => t != null)
                     && _resumeButton != null && _surrenderButton != null && _confirmRoot != null && _confirmText != null
                     && _confirmYesButton != null && _confirmNoButton != null;
            if (!ok) Debug.LogError($"{nameof(BattlePausePopup)}: references are not assigned.", this);
            return ok;
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
