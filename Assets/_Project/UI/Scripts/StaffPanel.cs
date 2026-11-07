using System;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Ba cột (đầu bếp, phục vụ, thu ngân), mỗi cột tối đa ba ô. Bấm ô mở danh sách nhân vật đã có; mọi tính toán nằm ở StaffPanelRows.
    public class StaffPanel : MonoBehaviour
    {
        private const int SlotsPerRole = 3;
        private static readonly StaffRole[] Roles = { StaffRole.Cook, StaffRole.Server, StaffRole.Cashier };

        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _messageText;
        [SerializeField] private Text[] _columnTitles;
        // Chỉ số = vai × 3 + ô.
        [SerializeField] private Button[] _slotButtons;
        [SerializeField] private Text[] _slotTexts;
        [SerializeField] private Button[] _removeButtons;
        [SerializeField] private GameObject _listRoot;
        [SerializeField] private Text _listTitle;
        [SerializeField] private RectTransform _listContent;
        [SerializeField] private Button _rowTemplate;
        [SerializeField] private Button _listCloseButton;
        [SerializeField] private GameObject _confirmRoot;
        [SerializeField] private Text _confirmText;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        private RestaurantService _service;
        private Roster _roster;
        private StaffRole _listRole;
        private string _replacingId;
        private CandidateRow _pending;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<RestaurantService>();
                _roster = ServiceLocator.Get<Roster>();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(StaffPanel)}: {e.Message}", this);
                _service = null;
                return;
            }

            _openButton.onClick.AddListener(Open);
            _closeButton.onClick.AddListener(Close);
            _listCloseButton.onClick.AddListener(CloseList);
            _confirmYesButton.onClick.AddListener(ConfirmYes);
            _confirmNoButton.onClick.AddListener(CloseConfirm);
            for (var r = 0; r < Roles.Length; r++)
            {
                for (var s = 0; s < SlotsPerRole; s++)
                {
                    var role = Roles[r];
                    var slot = s;
                    var index = r * SlotsPerRole + s;
                    _slotButtons[index].onClick.AddListener(() => OpenList(role, slot));
                    _removeButtons[index].onClick.AddListener(() => RemoveFromSlot(role, slot));
                }
            }

            _service.ProfileChanged += Redraw;
            _root.SetActive(false);
        }

        private void OnDisable()
        {
            if (_service == null) return;
            _service.Incidents.SetSpawnBlocked(this, false);
            _service.ProfileChanged -= Redraw;
            _openButton.onClick.RemoveListener(Open);
            _closeButton.onClick.RemoveListener(Close);
            _listCloseButton.onClick.RemoveListener(CloseList);
            _confirmYesButton.onClick.RemoveListener(ConfirmYes);
            _confirmNoButton.onClick.RemoveListener(CloseConfirm);
            foreach (var button in _slotButtons) button.onClick.RemoveAllListeners();
            foreach (var button in _removeButtons) button.onClick.RemoveAllListeners();
            _service = null;
        }

        private bool HasAllReferences()
        {
            var ok = _openButton != null && _closeButton != null && _root != null && _messageText != null
                     && Has(_columnTitles, Roles.Length) && Has(_slotButtons, Roles.Length * SlotsPerRole)
                     && Has(_slotTexts, Roles.Length * SlotsPerRole) && Has(_removeButtons, Roles.Length * SlotsPerRole)
                     && _listRoot != null && _listTitle != null && _listContent != null && _rowTemplate != null
                     && _listCloseButton != null && _confirmRoot != null && _confirmText != null
                     && _confirmYesButton != null && _confirmNoButton != null;
            if (!ok) Debug.LogError($"{nameof(StaffPanel)}: a serialized reference is not assigned.", this);
            return ok;
        }

        private static bool Has<T>(T[] items, int count) where T : UnityEngine.Object
        {
            if (items == null || items.Length != count) return false;
            foreach (var item in items)
                if (item == null) return false;
            return true;
        }

        private void Open()
        {
            // Panel che "!" nên không sinh sự cố mới; sự cố đang chạy vẫn đếm để xếp người xử lý được.
            _service.Incidents.SetSpawnBlocked(this, true);
            _root.SetActive(true);
            _listRoot.SetActive(false);
            _confirmRoot.SetActive(false);
            _messageText.text = "";
            Redraw();
        }

        private void Close()
        {
            _service.Incidents.SetSpawnBlocked(this, false);
            _root.SetActive(false);
            _listRoot.SetActive(false);
            _confirmRoot.SetActive(false);
        }

        private void Redraw()
        {
            if (!_root.activeSelf) return;

            var profile = _service.Profile;
            for (var r = 0; r < Roles.Length; r++)
            {
                var role = Roles[r];
                _columnTitles[r].text = StaffPanelRows.RoleName(role);
                var workers = profile.Workers(role);
                for (var s = 0; s < SlotsPerRole; s++)
                {
                    var index = r * SlotsPerRole + s;
                    var unlocked = s < profile.Slots(role);
                    var occupied = unlocked && s < workers.Count;
                    _slotButtons[index].interactable = unlocked;
                    _removeButtons[index].gameObject.SetActive(occupied);
                    _slotTexts[index].text = !unlocked ? StaffPanelRows.LockedHint(_service.Config, role)
                        : occupied ? $"{NameOf(workers[s].CharacterId)} · {workers[s].Stat}"
                        : UiText.Get("restaurant.slot_empty");
                }
            }
        }

        private void OpenList(StaffRole role, int slot)
        {
            if (!_service.IsRunning)
            {
                _messageText.text = RestaurantViewText.InvalidConfigLine();
                return;
            }

            var workers = _service.Profile.Workers(role);
            _listRole = role;
            _replacingId = slot < workers.Count ? workers[slot].CharacterId : null;
            _messageText.text = "";
            _listTitle.text = UiText.Format("restaurant.pick_title", StaffPanelRows.RoleName(role));

            // Dọn hàng cũ trước khi dựng lại, nếu không mỗi lần mở lại cộng dồn thêm hàng.
            for (var i = _listContent.childCount - 1; i >= 0; i--)
            {
                var old = _listContent.GetChild(i);
                old.SetParent(null, false);
                Destroy(old.gameObject);
            }

            foreach (var row in StaffPanelRows.Candidates(role, _service.Config, _service.Profile, _roster, _replacingId))
            {
                var button = Instantiate(_rowTemplate, _listContent, false);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<Text>().text = StaffPanelRows.RowLabel(row, role);
                // Người đã ở đúng vai này rồi thì không có gì để đổi.
                button.interactable = row.CurrentRole != role;
                var captured = row;
                button.onClick.AddListener(() => Pick(captured));
            }

            _listRoot.SetActive(true);
        }

        private void CloseList()
        {
            _listRoot.SetActive(false);
            _confirmRoot.SetActive(false);
        }

        private void CloseConfirm() => _confirmRoot.SetActive(false);

        private void Pick(CandidateRow row)
        {
            if (row.Character.Id == _replacingId)
            {
                CloseList();
                return;
            }

            if (!row.NeedsConfirm)
            {
                Apply(row);
                return;
            }

            _pending = row;
            var name = string.IsNullOrEmpty(row.Character.DisplayName) ? row.Character.Id : row.Character.DisplayName;
            _confirmText.text = UiText.Format("restaurant.confirm_move", name);
            _confirmRoot.SetActive(true);
        }

        private void ConfirmYes()
        {
            _confirmRoot.SetActive(false);
            Apply(_pending);
        }

        private void Apply(CandidateRow row)
        {
            var id = row.Character.Id;
            var result = _replacingId != null
                ? _service.Staffing.TryReplace(_replacingId, id, _listRole)
                : _service.Staffing.TryAssign(id, _listRole);
            if (result == StaffAssignResult.Success)
            {
                _messageText.text = "";
                CloseList();
                Redraw();
                return;
            }

            _messageText.text = result == StaffAssignResult.RoleFull ? UiText.Get("restaurant.role_full") : UiText.Get("restaurant.pick_failed");
        }

        private void RemoveFromSlot(StaffRole role, int slot)
        {
            var workers = _service.Profile.Workers(role);
            if (slot < workers.Count) _service.Staffing.Remove(workers[slot].CharacterId);
        }

        private string NameOf(string characterId)
        {
            foreach (var character in _roster.GetOwned())
                if (character.Id == characterId) return string.IsNullOrEmpty(character.DisplayName) ? character.Id : character.DisplayName;
            return characterId;
        }
    }
}
