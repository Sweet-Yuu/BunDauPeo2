using System;
using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Hộp Hồ sơ giữa màn (spec UI hub C 5.4): tên sửa được (tối đa PlayerNameRule.MaxLength), avatar chọn trong lưới nhân vật đã sở hữu;
    // dòng ID người chơi + Sao chép (Online 5.9).
    // Component nằm trên root của hộp nên IsOpen = root bật. Là hộp nổi (IHubOverlay): không đổi bố cục Lobby.
    public class ProfilePopup : MonoBehaviour, IHubOverlay
    {
        private const string RingName = "AvatarSelected_PH_136x136";
        private const int Columns = 5;
        private const float Pitch = 130f;
        private const float CellSize = 120f;
        // Viền vàng của ô thò ra 8 px; khung cuộn nới đúng 8 px mỗi cạnh nên viền không bị cắt.
        private const float Pad = 8f;

        [SerializeField] private InputField _nameInput;
        [SerializeField] private Text _status;
        [SerializeField] private AvatarView _bigAvatar;
        [SerializeField] private RectTransform _gridContent;
        [SerializeField] private Button _cellTemplate;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _idText;
        [SerializeField] private Button _copyButton;
        [SerializeField] private GameObject _copiedText;
        // Dòng Tài khoản (Online đợt 3, spec 8.3); không bắt buộc để scene dựng trước đợt 3 vẫn mở được.
        [SerializeField] private Text _accountText;
        [SerializeField] private Button _accountButton;

        private const float CopiedSeconds = 2f;

        private sealed class Cell
        {
            public Button Button;
            public AvatarView View;
            public GameObject Ring;
            public string Id = "";
        }

        private readonly List<Cell> _cells = new();
        private PlayerProfile _profile;
        private Roster _roster;
        private RestaurantService _service;
        private IGameServer _server;
        private IAccountService _account;
        private float _copiedUntil;
        private bool _wired;
        // Chỉ nhận cú kết thúc gõ khi hộp mở: InputField còn focus bắn onEndEdit lúc bị tắt (Close, nạp lại scene).
        private bool _open;

        public bool IsOpen => gameObject.activeSelf;

        public void Open()
        {
            if (!HasAllReferences()) return;

            Wire();
            gameObject.SetActive(true);
            if (_profile == null || _roster == null) return;

            _open = true;
            ((Text)_nameInput.placeholder).text = PlayerNameRule.Display("");
            _nameInput.SetTextWithoutNotify(_profile.Name);
            _status.text = "";
            // Đọc lại mỗi lần mở: Xoá tài khoản rồi vào lại là người chơi mới.
            _idText.text = UiText.Format("profile.player_id", PlayerId());
            _copiedText.SetActive(false);
            RefreshAccount();
            RebuildGrid();
            _bigAvatar.Show(AvatarView.Current());
        }

        // Kết thúc gõ dở trước khi tắt, để chữ không mất. Reset/đổi ngôn ngữ cũng đi đường này (HubNav.CloseOpenPages, L-035):
        // chữ dở nằm vào save cũ rồi Reset ghi đè, không bao giờ vào save mới.
        public void Close()
        {
            if (!IsOpen) return;

            if (_open && _profile != null)
            {
                var name = PlayerNameRule.Normalize(_nameInput.text);
                if (name != _profile.Name) Report(_profile.SetName(name));
            }

            _open = false;
            // Tắt focus khi _open đã false: onEndEdit đến muộn bị bỏ qua.
            _nameInput.DeactivateInputField();
            gameObject.SetActive(false);
        }

        // Panel che quán nên không sinh sự cố mới trong lúc hộp mở (như NotifyPopup; cờ nhiều chủ, L-015).
        private void OnEnable()
        {
            try
            {
                _profile = ServiceLocator.Get<PlayerProfile>();
                _roster = ServiceLocator.Get<Roster>();
                _server = ServiceLocator.Get<IGameServer>();
                _service = ServiceLocator.Get<RestaurantService>();
                _service.Incidents.SetSpawnBlocked(this, true);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(ProfilePopup)}: {e.Message}", this);
                _service = null;
            }

            // Không bắt buộc: test dựng Hồ sơ không đăng ký tài khoản thì dòng Tài khoản ẩn.
            try
            {
                _account = ServiceLocator.Get<IAccountService>();
                _account.Changed += RefreshAccount;
            }
            catch (InvalidOperationException)
            {
                _account = null;
            }
        }

        private void OnDisable()
        {
            _open = false;
            _service?.Incidents.SetSpawnBlocked(this, false);
            if (_account != null) _account.Changed -= RefreshAccount;
            _account = null;
            _service = null;
            _profile = null;
            _roster = null;
            _server = null;
        }

        private void Update()
        {
            if (_copiedText != null && _copiedText.activeSelf && Time.unscaledTime >= _copiedUntil) _copiedText.SetActive(false);
        }

        private string PlayerId() => _server?.PlayerId ?? "";

        // shortcut: WebGL có thể không chép ra clipboard của trình duyệt qua systemCopyBuffer; nâng khi playtest WebGL thấy lỗi (kế hoạch Task 7).
        private void CopyId()
        {
            GUIUtility.systemCopyBuffer = PlayerId();
            _copiedText.SetActive(true);
            _copiedUntil = Time.unscaledTime + CopiedSeconds;
        }

        private bool HasAllReferences()
        {
            var ok = _nameInput != null && _status != null && _bigAvatar != null && _gridContent != null && _cellTemplate != null && _closeButton != null
                     && _idText != null && _copyButton != null && _copiedText != null;
            if (!ok) Debug.LogError($"{nameof(ProfilePopup)}: references are not assigned.", this);
            return ok;
        }

        // Một lần, lúc mở lần đầu (root còn tắt tới lúc đó).
        private void Wire()
        {
            if (_wired) return;

            _wired = true;
            _nameInput.onEndEdit.AddListener(OnNameEnd);
            _closeButton.onClick.AddListener(Close);
            _copyButton.onClick.AddListener(CopyId);
            if (_accountButton != null) _accountButton.onClick.AddListener(OpenLink);
        }

        private void RefreshAccount()
        {
            if (_accountText == null || _accountButton == null) return;
            _accountText.gameObject.SetActive(_account != null);
            _accountButton.gameObject.SetActive(_account != null && !_account.IsLinked);
            if (_account == null) return;
            var state = _account.IsLinked ? UiText.Format("account.linked", string.Join(", ", _account.LinkedNames)) : UiText.Get("account.not_linked");
            // Đã liên kết thì không có nút: chữ dùng cả bề ngang cột trái.
            var rect = _accountText.rectTransform;
            rect.sizeDelta = new Vector2(_account.IsLinked ? 410 : 230, rect.sizeDelta.y);
            _accountText.text = UiText.Get("account.row") + ": " + state;
        }

        // Liên kết: Conflict → hộp chuyển tài khoản; Chuyển → session bỏ, về Tiêu đề (không còn gì ở đây để cập nhật).
        private static void OpenLink() =>
            AccountBoxes.ShowLink(AccountBoxes.NewFlow(() => SceneManager.LoadScene(TitleView.TitleScene)));

        private void OnNameEnd(string text)
        {
            if (!_open || _profile == null) return;

            // Chặn ở đây: GameSession.SetPlayerName không tự cắt.
            var name = PlayerNameRule.Normalize(text);
            _nameInput.SetTextWithoutNotify(name);
            Report(_profile.SetName(name));
        }

        private void OnCellClicked(Cell cell)
        {
            if (_profile == null) return;

            Report(_profile.SetAvatar(cell.Id));
            Highlight(cell.Id);
            _bigAvatar.Show(AvatarView.Current());
        }

        // Lưu lỗi: giá trị mới vẫn nằm trong bộ nhớ, dòng báo cho người chơi biết (như cờ thanh quán).
        private void Report(bool saved) => _status.text = saved ? "" : UiText.Get("profile.not_saved");

        private void RebuildGrid()
        {
            var owned = _roster.GetOwned();
            for (var i = 0; i < owned.Count; i++)
            {
                if (i == _cells.Count) _cells.Add(NewCell());
                var cell = _cells[i];
                cell.Id = owned[i].Id;
                cell.View.Show(owned[i]);
                var rect = (RectTransform)cell.Button.transform;
                rect.anchoredPosition = new Vector2(Pad + i % Columns * Pitch, -(Pad + i / Columns * Pitch));
                cell.Button.gameObject.SetActive(true);
            }

            for (var i = owned.Count; i < _cells.Count; i++) _cells[i].Button.gameObject.SetActive(false);
            var rows = (owned.Count + Columns - 1) / Columns;
            _gridContent.sizeDelta = new Vector2(_gridContent.sizeDelta.x, rows == 0 ? 0f : Pad * 2 + (rows - 1) * Pitch + CellSize);
            Highlight(AvatarView.Current()?.Id);
        }

        private Cell NewCell()
        {
            var button = Instantiate(_cellTemplate, _gridContent);
            button.name = _cellTemplate.name;
            var cell = new Cell { Button = button, View = button.GetComponent<AvatarView>(), Ring = button.transform.Find(RingName).gameObject };
            button.onClick.AddListener(() => OnCellClicked(cell));
            return cell;
        }

        private void Highlight(string selectedId)
        {
            foreach (var cell in _cells) cell.Ring.SetActive(!string.IsNullOrEmpty(selectedId) && cell.Id == selectedId);
        }
    }
}
