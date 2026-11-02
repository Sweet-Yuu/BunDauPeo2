using System;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Màn kết quả (spec Combat 5.5). BattleView gọi Show mỗi khung khi trận đã hết và hoạt hình đã xong; Show chỉ làm việc
    // ở lần đầu của mỗi trận, nên CompleteBattle đúng một lần. Thắng: thưởng; thua: gợi ý, thể lực đã trừ, Thử lại (EnterStage, trừ lại).
    public class BattleResultView : MonoBehaviour
    {
        // _root chặn chạm xuống trận; component nằm ở object luôn bật để còn nhận Show.
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _banner;
        [SerializeField] private Text _rewardText;
        [SerializeField] private Text _staminaText;
        [SerializeField] private Text _warningText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _backButton;

        private CombatService _service;
        private Roster _roster;
        private BattleSession _shown;
        private bool _busy;
        // Lý do lần Thử lại vừa thất bại; RefreshRetry giữ nó hiện khi CanEnter vẫn báo Ready (vd. chưa lưu được).
        private string _retryError = "";

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Show(CombatService service, BattleSession session)
        {
            if (service == null || session == null || !HasAllReferences()) return;

            _service = service;
            if (ReferenceEquals(_shown, session))
            {
                if (session.State == BattleState.Lost) RefreshRetry(session);
                return;
            }

            _shown = session;
            _busy = false;
            _retryError = "";
            _banner.text = CombatViewText.EndBanner(session.State);
            _messageText.text = "";
            _warningText.text = "";
            _root.SetActive(true);

            var won = session.State == BattleState.Won;
            _confirmButton.gameObject.SetActive(won);
            _retryButton.gameObject.SetActive(!won);
            _backButton.gameObject.SetActive(!won);
            if (won)
            {
                // Đúng một lần cho trận này (Show không chạy lại): kết quả giữ trong chữ trên màn.
                var result = service.Progress.CompleteBattle(session);
                if (result.Status == StageCompleteStatus.Success)
                {
                    _rewardText.text = CombatViewText.ResultRewards(result);
                    _staminaText.text = CombatViewText.StaminaUsed(result.StaminaSpent);
                    _warningText.text = RestaurantViewText.SaveWarning(result.Saved);
                }
                else
                {
                    // Chỉ gặp ở trận vào thẳng từ dải DEV (NotPaid): vẫn CLEAR nhưng không thưởng.
                    _rewardText.text = CombatViewText.TrialLine();
                    _staminaText.text = "";
                }
            }
            else
            {
                _rewardText.text = CombatViewText.FailHint();
                _staminaText.text = session.EntryPaid ? CombatViewText.FailStamina(session.Stage.StaminaCost) : "";
                RefreshRetry(session);
            }
        }

        // Trận mới đã thay trận cũ (Thử lại, hoặc Reset): đóng.
        public void Hide()
        {
            _shown = null;
            if (_root != null) _root.SetActive(false);
        }

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _roster = ServiceLocator.Get<Roster>();
            }
            catch (InvalidOperationException)
            {
                _roster = null;
            }

            _confirmButton.onClick.AddListener(ToMap);
            _backButton.onClick.AddListener(ToMap);
            _retryButton.onClick.AddListener(Retry);
        }

        private void OnDisable()
        {
            if (_confirmButton != null) _confirmButton.onClick.RemoveListener(ToMap);
            if (_backButton != null) _backButton.onClick.RemoveListener(ToMap);
            if (_retryButton != null) _retryButton.onClick.RemoveListener(Retry);
        }

        private bool HasAllReferences()
        {
            var ok = _root != null && _banner != null && _rewardText != null && _staminaText != null && _warningText != null
                     && _messageText != null && _confirmButton != null && _retryButton != null && _backButton != null;
            if (!ok) Debug.LogError($"{nameof(BattleResultView)}: references are not assigned.", this);
            return ok;
        }

        private void ToMap()
        {
            _service?.EndBattle();
            SceneManager.LoadScene(BattleView.StageSelectScene);
        }

        // Nút mờ khi không vào lại được, kèm lý do (thiếu thể lực thì kèm thời gian chờ). Trận DEV (chưa trả thể lực) luôn thử lại được.
        private void RefreshRetry(BattleSession session)
        {
            if (_busy) return;

            if (!session.EntryPaid)
            {
                _retryButton.interactable = true;
                return;
            }

            var teamSize = _roster == null ? 0 : _roster.GetBattleTeam().Count;
            var status = _service.Progress.CanEnter(session.Stage.Id, teamSize);
            _retryButton.interactable = status == StageEnterStatus.Ready;
            if (status == StageEnterStatus.Ready)
            {
                _messageText.text = _retryError;
                return;
            }

            if (status == StageEnterStatus.NotEnoughStamina)
            {
                var stamina = _service.Stamina;
                _messageText.text = CombatViewText.WaitLine(CombatViewText.SecondsUntilAffordable(
                    stamina.Current, session.Stage.StaminaCost, stamina.SecondsToNext, _service.Config.StaminaRegenSeconds));
            }
            else
            {
                // Tên các lý do từ chối của CanEnter trùng tên ở EnterStageStatus.
                _messageText.text = CombatViewText.EnterFailedLine((EnterStageStatus)Enum.Parse(typeof(EnterStageStatus), status.ToString()));
            }
        }

        // Một lần mỗi lần bấm: tắt nút ngay; vào được thì BattleView thấy trận mới và vẽ lại (Hide), không thì bật lại kèm lý do.
        private void Retry()
        {
            if (_busy || _shown == null || _service == null) return;

            _busy = true;
            _retryButton.interactable = false;
            var stageId = _shown.Stage.Id;
            if (_shown.EntryPaid)
            {
                var result = _service.EnterStage(stageId);
                if (result.Status == EnterStageStatus.Entered) return;

                _busy = false;
                _retryError = CombatViewText.EnterFailedLine(result.Status);
                _messageText.text = _retryError;
            }
            else
            {
                var result = _service.StartBattle(stageId);
                if (result.Status == StartBattleStatus.Success) return;

                _busy = false;
                _retryButton.interactable = true;
                _retryError = CombatViewText.EnterFailedLine(EnterStageStatus.Unknown);
                _messageText.text = _retryError;
            }
        }
    }
}
