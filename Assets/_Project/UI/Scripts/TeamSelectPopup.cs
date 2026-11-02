using System;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Chọn đội trước khi vào trận (spec Combat 5.3). Đổi đội ghi bộ nhớ (Roster); EnterStage kiểm lại mọi thứ, trừ thể lực và lưu
    // cả đội một lần. Thể lực chỉ bị trừ khi bấm Vào trận (hoặc Có ở hộp xác nhận), không phải lúc mở popup hay hộp.
    public class TeamSelectPopup : MonoBehaviour
    {
        public const int CardsPerPage = 12;
        private static readonly Color OnChip = new(0.20f, 0.23f, 0.32f);

        // _root chặn chạm; component nằm ở object luôn bật để còn nhận Open.
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _title;
        [SerializeField] private TeamCardView[] _cards;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Text _pageText;
        [SerializeField] private Text _teamHeader;
        [SerializeField] private Text[] _positionLabels;
        [SerializeField] private Image[] _chips;
        [SerializeField] private Text[] _chipTexts;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _startButton;
        [SerializeField] private GameObject _smallConfirmRoot;
        [SerializeField] private Text _smallConfirmText;
        [SerializeField] private Button _smallYesButton;
        [SerializeField] private Button _smallNoButton;
        [SerializeField] private GameObject _staffConfirmRoot;
        [SerializeField] private Text _staffConfirmText;
        [SerializeField] private Button _staffYesButton;
        [SerializeField] private Button _staffNoButton;

        private CombatService _service;
        private Roster _roster;
        private StageDefinition _stage;
        private int _page;
        private string _pendingStaffId;
        // Đã bấm Vào trận và chưa có kết quả: giữ nút tắt (chống bấm đôi).
        private bool _busy;

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Open(CombatService service, Roster roster, StageDefinition stage)
        {
            if (service == null || roster == null || stage == null || !HasAllReferences()) return;

            _service = service;
            _roster = roster;
            _stage = stage;
            _page = 0;
            _busy = false;
            _pendingStaffId = null;
            _title.text = $"Chọn đội · {stage.DisplayName}";
            _messageText.text = "";
            _smallConfirmRoot.SetActive(false);
            _staffConfirmRoot.SetActive(false);
            for (var i = 0; i < _positionLabels.Length; i++) _positionLabels[i].text = CombatViewText.PositionName((BattlePosition)i);
            _root.SetActive(true);
            Refresh();
        }

        // Hủy: lưu đội đã đổi (Roster chỉ ghi bộ nhớ) rồi đóng.
        public void Cancel()
        {
            _service?.Persist();
            Close();
        }

        public void Close()
        {
            _stage = null;
            _pendingStaffId = null;
            if (_smallConfirmRoot != null) _smallConfirmRoot.SetActive(false);
            if (_staffConfirmRoot != null) _staffConfirmRoot.SetActive(false);
            if (_root != null) _root.SetActive(false);
        }

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            _cancelButton.onClick.AddListener(Cancel);
            _startButton.onClick.AddListener(OnStartClicked);
            _prevButton.onClick.AddListener(PrevPage);
            _nextButton.onClick.AddListener(NextPage);
            _smallYesButton.onClick.AddListener(OnSmallYes);
            _smallNoButton.onClick.AddListener(HideConfirms);
            _staffYesButton.onClick.AddListener(OnStaffYes);
            _staffNoButton.onClick.AddListener(HideConfirms);
            for (var i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                card.Button.onClick.AddListener(() => OnCardClicked(card));
            }
        }

        private void OnDisable()
        {
            if (_cancelButton != null) _cancelButton.onClick.RemoveListener(Cancel);
            if (_startButton != null) _startButton.onClick.RemoveListener(OnStartClicked);
            if (_prevButton != null) _prevButton.onClick.RemoveListener(PrevPage);
            if (_nextButton != null) _nextButton.onClick.RemoveListener(NextPage);
            if (_smallYesButton != null) _smallYesButton.onClick.RemoveListener(OnSmallYes);
            if (_smallNoButton != null) _smallNoButton.onClick.RemoveListener(HideConfirms);
            if (_staffYesButton != null) _staffYesButton.onClick.RemoveListener(OnStaffYes);
            if (_staffNoButton != null) _staffNoButton.onClick.RemoveListener(HideConfirms);
            if (_cards != null)
                foreach (var card in _cards)
                    if (card != null) card.Button.onClick.RemoveAllListeners();
        }

        private bool HasAllReferences()
        {
            var ok = _root != null && _title != null && _prevButton != null && _nextButton != null && _pageText != null
                     && _teamHeader != null && _messageText != null && _cancelButton != null && _startButton != null
                     && _smallConfirmRoot != null && _smallConfirmText != null && _smallYesButton != null && _smallNoButton != null
                     && _staffConfirmRoot != null && _staffConfirmText != null && _staffYesButton != null && _staffNoButton != null
                     && Complete(_cards, CardsPerPage) && Complete(_positionLabels, 3)
                     && Complete(_chips, Roster.MaxBattleTeamSize) && Complete(_chipTexts, Roster.MaxBattleTeamSize);
            if (!ok) Debug.LogError($"{nameof(TeamSelectPopup)}: references are not assigned.", this);
            return ok;
        }

        private static bool Complete<T>(T[] items, int count) where T : UnityEngine.Object =>
            items != null && items.Length == count && Array.TrueForAll(items, i => i != null);

        private void PrevPage()
        {
            _page--;
            Refresh();
        }

        private void NextPage()
        {
            _page++;
            Refresh();
        }

        // shortcut: lưới phân trang 12 thẻ mỗi trang, đủ cho vài chục nhân vật; nhiều hơn nữa thì đổi sang cuộn.
        private void Refresh()
        {
            if (_roster == null || _stage == null) return;

            var rows = TeamSelectRows.Build(_roster);
            var pageCount = Math.Max(1, (rows.Count + CardsPerPage - 1) / CardsPerPage);
            _page = Mathf.Clamp(_page, 0, pageCount - 1);
            for (var i = 0; i < _cards.Length; i++)
            {
                var index = _page * CardsPerPage + i;
                if (index < rows.Count) _cards[i].Bind(rows[index]);
                else _cards[i].Hide();
            }

            _prevButton.interactable = _page > 0;
            _nextButton.interactable = _page < pageCount - 1;
            _pageText.text = $"{_page + 1}/{pageCount}";

            var team = _roster.GetBattleTeam();
            _teamHeader.text = $"Thành viên hiện tại ({Math.Min(team.Count, Roster.MaxBattleTeamSize)}/{Roster.MaxBattleTeamSize})";
            for (var i = 0; i < _chips.Length; i++) _chipTexts[i].text = "-";
            foreach (var slot in BattleFormation.Assign(team))
            {
                _chipTexts[slot.SlotIndex].text = $"{(string.IsNullOrEmpty(slot.Character.DisplayName) ? slot.Character.Id : slot.Character.DisplayName)}\n{CombatViewText.ClassName(slot.Character.Class)}";
                _chips[slot.SlotIndex].color = BattleUnitView.ClassColor(slot.Character.Class);
            }

            for (var i = 0; i < _chips.Length; i++)
                if (_chipTexts[i].text == "-") _chips[i].color = OnChip;

            _startButton.interactable = !_busy && BattleFormation.GetEntryStatus(team.Count) != TeamEntryStatus.Empty;
        }

        private void OnCardClicked(TeamCardView card)
        {
            if (_busy || !card.HasRow) return;

            _messageText.text = "";
            var row = card.Row;
            if (row.InTeam) _roster.RemoveFromTeam(row.Character.Id);
            else if (row.StaffRole.HasValue)
            {
                // Đang làm quán: hỏi trước, vì vào đội là rời quán.
                _pendingStaffId = row.Character.Id;
                _staffConfirmText.text = CombatViewText.ConfirmFromStaff(
                    string.IsNullOrEmpty(row.Character.DisplayName) ? row.Character.Id : row.Character.DisplayName, row.StaffRole.Value);
                _staffConfirmRoot.SetActive(true);
                return;
            }
            else if (!_roster.AddToTeam(row.Character.Id)) _messageText.text = CombatViewText.TeamFull();

            Refresh();
        }

        private void OnStaffYes()
        {
            _staffConfirmRoot.SetActive(false);
            if (_pendingStaffId != null && !_roster.AddToTeam(_pendingStaffId)) _messageText.text = CombatViewText.TeamFull();
            _pendingStaffId = null;
            Refresh();
        }

        private void HideConfirms()
        {
            _smallConfirmRoot.SetActive(false);
            _staffConfirmRoot.SetActive(false);
            _pendingStaffId = null;
        }

        private void OnStartClicked()
        {
            if (_busy || _stage == null) return;

            var count = _roster.GetBattleTeam().Count;
            switch (BattleFormation.GetEntryStatus(count))
            {
                case TeamEntryStatus.Empty:
                    return;
                case TeamEntryStatus.NeedsConfirm:
                    _smallConfirmText.text = CombatViewText.ConfirmSmallTeam(count);
                    _smallConfirmRoot.SetActive(true);
                    return;
                default:
                    Enter();
                    return;
            }
        }

        private void OnSmallYes()
        {
            _smallConfirmRoot.SetActive(false);
            Enter();
        }

        // EnterStage một lần mỗi lần bấm: nó kiểm lại, trừ thể lực và lưu; chỉ Entered mới nạp Battle.
        private void Enter()
        {
            if (_busy || _stage == null) return;

            _busy = true;
            Refresh();
            var result = _service.EnterStage(_stage.Id);
            if (result.Status == EnterStageStatus.Entered)
            {
                SceneManager.LoadScene(BattleView.BattleScene);
                return;
            }

            _busy = false;
            _messageText.text = CombatViewText.EnterFailedLine(result.Status);
            Refresh();
        }
    }
}
