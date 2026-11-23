using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Hộp xác nhận thể lực sắp vượt trần (spec đợt 3 8.5). Nằm trên một object riêng; _root là nền mờ + hộp, tắt lúc đầu.
    public class StaminaCapConfirm : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _body;
        [SerializeField] private Button _okButton;
        [SerializeField] private Button _cancelButton;

        private TaskCompletionSource<bool> _pending;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void OnEnable()
        {
            if (_okButton == null || _cancelButton == null || _root == null || _body == null)
            {
                Debug.LogError($"{nameof(StaminaCapConfirm)}: references are not assigned.", this);
                return;
            }

            _okButton.onClick.AddListener(Accept);
            _cancelButton.onClick.AddListener(Cancel);
        }

        // Thứ chờ người bấm thì mọi đường bỏ phiên phải huỷ nó (L-043).
        private void OnDisable()
        {
            if (_okButton != null) _okButton.onClick.RemoveListener(Accept);
            if (_cancelButton != null) _cancelButton.onClick.RemoveListener(Cancel);
            Resolve(false);
        }

        public Task<bool> AskAsync(int have, int granted, int cap, int wasted)
        {
            Resolve(false);
            _body.text = UiText.Format("stamina.cap_body", have, granted, cap, wasted);
            _pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _root.SetActive(true);
            return _pending.Task;
        }

        public void Accept() => Resolve(true);

        public void Cancel() => Resolve(false);

        private void Resolve(bool answer)
        {
            if (_root != null) _root.SetActive(false);
            var pending = _pending;
            _pending = null;
            pending?.TrySetResult(answer);
        }
    }
}
