using BunDauPeo.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Màn cốt truyện giữ chỗ: chưa có nội dung. Khi có spec cốt truyện chỉ thay popup này bằng hội thoại; catalog và save không đổi.
    public class StoryPlaceholderPopup : MonoBehaviour
    {
        // _root chặn chạm; component nằm ở object luôn bật để còn nhận Open.
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _title;
        [SerializeField] private Text _body;
        [SerializeField] private Text _message;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _closeButton;

        private CombatService _service;
        private StageDefinition _stage;

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Open(CombatService service, StageDefinition stage)
        {
            if (service == null || stage == null || !HasAllReferences()) return;

            _service = service;
            _stage = stage;
            _title.text = stage.DisplayName;
            _body.text = CombatViewText.StoryEmpty();
            _message.text = "";
            _continueButton.interactable = true;
            _root.SetActive(true);
        }

        public void Close()
        {
            _stage = null;
            if (_root != null) _root.SetActive(false);
        }

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            _continueButton.onClick.AddListener(Continue);
            _closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            if (_continueButton != null) _continueButton.onClick.RemoveListener(Continue);
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
        }

        private bool HasAllReferences()
        {
            var ok = _root != null && _title != null && _body != null && _message != null && _continueButton != null && _closeButton != null;
            if (!ok) Debug.LogError($"{nameof(StoryPlaceholderPopup)}: references are not assigned.", this);
            return ok;
        }

        private void Continue()
        {
            if (_service == null || _stage == null) return;

            _continueButton.interactable = false;
            var result = _service.Progress.CompleteStory(_stage.Id);
            if (result.Status == StageCompleteStatus.AlreadyCleared || (result.Status == StageCompleteStatus.Success && result.Saved))
            {
                Close();
                return;
            }

            // Đã mở nhưng chưa lưu được: giữ popup để người chơi đọc cảnh báo (bộ nhớ đã đổi, lần lưu sau sẽ ghi).
            _message.text = result.Status == StageCompleteStatus.Success
                ? RestaurantViewText.SaveWarning(false)
                : CombatViewText.EnterFailedLine(EnterStageStatus.Unknown);
        }
    }
}
