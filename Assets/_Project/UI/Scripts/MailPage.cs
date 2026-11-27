using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Missions;
using BunDauPeo.Restaurant;
using BunDauPeo.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Hộp thư kín màn, mở từ nút tròn ở Lobby (spec đợt 3 8.2): danh sách trái, nội dung phải. Nguồn thư và trạng thái đọc ở MailService.
    public class MailPage : MonoBehaviour, IHubPage
    {
        private static readonly Color RowOn = new(0.36f, 0.42f, 0.60f);
        private static readonly Color RowOff = new(0.27f, 0.30f, 0.40f);

        [SerializeField] private GameObject _root;
        [SerializeField] private Button _openButton;
        [SerializeField] private GameObject _openDot;
        [SerializeField] private Button _closeButton;
        [SerializeField] private RectTransform _listContent;
        [SerializeField] private Button _itemTemplate;
        [SerializeField] private GameObject _detail;
        [SerializeField] private Text _detailTitle;
        [SerializeField] private Text _detailSender;
        [SerializeField] private Text _detailDate;
        [SerializeField] private Text _detailBody;
        [SerializeField] private GameObject _attachBox;
        [SerializeField] private Button _actionButton;
        [SerializeField] private GameObject _emptyBox;
        [SerializeField] private Text _countText;
        [SerializeField] private Button _claimAllButton;
        [SerializeField] private Button _deleteReadButton;
        [SerializeField] private RectTransform _attachChips;
        [SerializeField] private GameObject _chipTemplate;
        [SerializeField] private Text _expiryText;
        [SerializeField] private GameObject _toast;
        [SerializeField] private Text _toastText;

        private sealed class Row
        {
            public Button Button;
            public Text Title;
            public Text Sender;
            public GameObject Star;
            public GameObject Dot;
            public MailItem Item;
        }

        private readonly List<Row> _rows = new();
        private MailService _service;
        private RestaurantService _restaurant;
        private IReadOnlyList<MailItem> _items = Array.Empty<MailItem>();
        private string _selectedId = "";
        private GiftService _gifts;
        private readonly List<GameObject> _chips = new();
        private float _toastLeft;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<MailService>();
                _restaurant = ServiceLocator.Get<RestaurantService>();
                _gifts = ServiceLocator.Get<GiftService>();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(MailPage)}: {e.Message}", this);
                _service = null;
                return;
            }

            _openButton.onClick.AddListener(Open);
            _closeButton.onClick.AddListener(Close);
            _actionButton.onClick.AddListener(OnAction);
            _deleteReadButton.onClick.AddListener(OnDeleteRead);
            _claimAllButton.onClick.AddListener(OnClaimAll);
            _service.Changed += Refresh;
            _root.SetActive(false);
            RefreshDot();
        }

        private void OnDisable()
        {
            if (_service == null) return;
            _service.Changed -= Refresh;
            _restaurant?.Incidents.SetSpawnBlocked(this, false);
            _openButton.onClick.RemoveListener(Open);
            _closeButton.onClick.RemoveListener(Close);
            _actionButton.onClick.RemoveListener(OnAction);
            _deleteReadButton.onClick.RemoveListener(OnDeleteRead);
            _claimAllButton.onClick.RemoveListener(OnClaimAll);
            // Dòng là con của trang, gắn một lần lúc tạo: không gỡ ở đây (OnEnable không gắn lại).
            _service = null;
            _restaurant = null;
            _gifts = null;
        }

        private void Update()
        {
            if (_toastLeft <= 0f) return;
            _toastLeft -= Time.unscaledDeltaTime;
            if (_toastLeft <= 0f) _toast.SetActive(false);
        }

        private bool HasAllReferences()
        {
            var ok = _root != null && _openButton != null && _openDot != null && _closeButton != null && _listContent != null && _itemTemplate != null
                     && _detail != null && _detailTitle != null && _detailSender != null && _detailDate != null && _detailBody != null
                     && _attachBox != null && _actionButton != null && _emptyBox != null && _countText != null && _claimAllButton != null
                     && _deleteReadButton != null && _attachChips != null && _chipTemplate != null && _expiryText != null
                     && _toast != null && _toastText != null;
            if (!ok) Debug.LogError($"{nameof(MailPage)}: a serialized reference is not assigned.", this);
            return ok;
        }

        public void Open() => Open(false);

        // firstGift: từ dòng Thông báo "Có quà chưa nhận" thì chọn thư quà đầu tiên chưa nhận (không có thì thư đầu).
        public void Open(bool firstGift)
        {
            if (_service == null) return;
            _restaurant.Incidents.SetSpawnBlocked(this, true);
            _root.SetActive(true);
            _items = _service.Items();
            // Mở trang = mở thư đầu (thư ghim lên đầu): đã đọc, chấm đỏ tắt.
            var pick = _items.Count > 0 ? _items[0] : null;
            if (firstGift)
                foreach (var item in _items)
                    if (item.Action == MailAction.ClaimGift && !item.Claimed)
                    {
                        pick = item;
                        break;
                    }
            _selectedId = pick?.Id ?? "";
            if (pick != null) _service.MarkRead(pick);
            Refresh();
            // Làm mới quà mỗi lần mở (spec đợt 4 5.1); lỗi thì giữ danh sách cũ, không chặn trang.
            _ = _gifts.RefreshAsync();
        }

        public void Close()
        {
            if (!IsOpen) return;
            _restaurant?.Incidents.SetSpawnBlocked(this, false);
            _root.SetActive(false);
        }

        private void Select(MailItem item)
        {
            _selectedId = item.Id;
            _service.MarkRead(item);
            Refresh();
        }

        private void Refresh()
        {
            if (_service == null) return;
            RefreshDot();
            if (!IsOpen) return;

            _items = _service.Items();
            while (_rows.Count < _items.Count) _rows.Add(NewRow());
            for (var i = 0; i < _rows.Count; i++)
            {
                var show = i < _items.Count;
                _rows[i].Button.gameObject.SetActive(show);
                if (!show) continue;
                var item = _items[i];
                var row = _rows[i];
                row.Item = item;
                row.Title.text = TitleOf(item);
                row.Sender.text = SenderOf(item);
                Dim(row.Title, item.Claimed);
                Dim(row.Sender, item.Claimed);
                row.Star.SetActive(item.Pinned);
                row.Dot.SetActive(_service.Unread(item));
                row.Button.targetGraphic.color = item.Id == _selectedId ? RowOn : RowOff;
            }

            MailItem selected = null;
            foreach (var item in _items)
                if (item.Id == _selectedId) selected = item;
            if (selected == null && _items.Count > 0) selected = _items[0];
            _selectedId = selected?.Id ?? "";

            _emptyBox.SetActive(_items.Count == 0);
            _detail.SetActive(selected != null);
            if (selected != null)
            {
                _detailTitle.text = TitleOf(selected);
                _detailSender.text = UiText.Get("mail.sender");
                _detailDate.text = Today();
                _detailBody.text = UiText.Get(selected.BodyKey);
                _attachBox.SetActive(selected.HasAttachments);
                ShowChips(selected);
                var isGift = selected.Action == MailAction.ClaimGift;
                _expiryText.text = isGift ? UiText.Format("mail.gift_expiry", GiftFormat.Long(selected.ExpiresUtcTicks)) : "";
                // Nút của thư: Liên kết (đợt 3) hoặc Nhận (đợt 4; mờ "Đã nhận" khi đã nhận).
                _actionButton.gameObject.SetActive(selected.Action == MailAction.LinkAccount || isGift);
                _actionButton.GetComponentInChildren<Text>().text =
                    UiText.Get(isGift ? (selected.Claimed ? "mail.gift_claimed" : "mail.gift_claim") : "mail.link_btn");
                _actionButton.interactable = !(isGift && selected.Claimed);
                SetAlpha(_actionButton.targetGraphic, _actionButton.interactable ? 1f : 0.4f);
            }

            _countText.text = UiText.Format("mail.count", _items.Count, MailRule.Capacity);
            _claimAllButton.interactable = _gifts.ClaimableCount > 0;
            _deleteReadButton.interactable = _service.CanDeleteRead();
            // Mờ khi không bấm được (nút dựng bằng Image màu, không có tint tắt).
            foreach (var button in new[] { _claimAllButton, _deleteReadButton })
            {
                var color = button.targetGraphic.color;
                color.a = button.interactable ? 1f : 0.4f;
                button.targetGraphic.color = color;
            }
        }

        private void RefreshDot() => _openDot.SetActive(_service != null && _service.HasRedDot());

        // Tên thư quà lấy từ Dashboard theo ngôn ngữ (không qua UiText); thư khác theo khoá.
        private static string TitleOf(MailItem item) => string.IsNullOrEmpty(item.Title) ? UiText.Get(item.TitleKey) : item.Title;

        private static string SenderOf(MailItem item)
        {
            if (item.Action != MailAction.ClaimGift) return UiText.Get("mail.sender");
            return item.Claimed ? UiText.Get("mail.gift_claimed") : UiText.Format("mail.gift_row_expiry", GiftFormat.Short(item.ExpiresUtcTicks));
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private static void Dim(Text text, bool dim) => SetAlpha(text, dim ? 0.5f : 1f);

        // Chip đính kèm gộp theo loại (Gem, Coin, Material): mỗi loại một chip.
        private void ShowChips(MailItem item)
        {
            var lines = item.Action == MailAction.ClaimGift ? GiftFormat.Lines(item.Rewards) : new List<string>();
            while (_chips.Count < lines.Count)
            {
                var chip = Instantiate(_chipTemplate, _attachChips);
                chip.name = _chipTemplate.name;
                _chips.Add(chip);
            }

            for (var i = 0; i < _chips.Count; i++)
            {
                _chips[i].SetActive(i < lines.Count);
                if (i < lines.Count) _chips[i].GetComponentInChildren<Text>().text = lines[i];
            }
        }

        private MailItem Selected()
        {
            foreach (var item in _items)
                if (item.Id == _selectedId) return item;
            return null;
        }

        private void ShowToast(string text)
        {
            _toastText.text = text;
            _toast.SetActive(true);
            _toastLeft = 2.5f;
        }

        private void ShowResult(GiftClaimResult result)
        {
            switch (result.Status)
            {
                case GiftClaimStatus.Ok:
                    ShowToast(UiText.Format("mail.gift_got", string.Join(", ", GiftFormat.Lines(result.Gem, result.Coin, result.Material))));
                    break;
                case GiftClaimStatus.NotInWindow: ShowToast(UiText.Get("mail.gift_expired")); break;
                case GiftClaimStatus.AlreadyClaimed: ShowToast(UiText.Get("mail.gift_claimed")); break;
                case GiftClaimStatus.NothingToClaim: ShowToast(UiText.Get("mail.gift_none")); break;
                // Offline: hộp Mất kết nối của cổng đã hiện; Invalid: danh sách đã được làm mới trong GiftService, trang tự vẽ lại qua Changed.
            }
        }

        private Row NewRow()
        {
            var button = Instantiate(_itemTemplate, _listContent);
            button.name = _itemTemplate.name;
            var row = new Row
            {
                Button = button,
                Title = button.transform.Find("Title").GetComponent<Text>(),
                Sender = button.transform.Find("Sender").GetComponent<Text>(),
                Star = button.transform.Find("Star").gameObject,
                Dot = button.transform.Find("Dot").gameObject,
            };
            button.onClick.AddListener(() => Select(row.Item));
            return row;
        }

        // Nút của thư: Liên kết (hộp chọn cách của Task 9; chuyển tài khoản = bỏ session, về Tiêu đề) hoặc Nhận quà.
        private async void OnAction()
        {
            var item = Selected();
            if (item == null) return;
            if (item.Action == MailAction.LinkAccount)
            {
                AccountBoxes.ShowLink(AccountBoxes.NewFlow(() => SceneManager.LoadScene(TitleView.TitleScene)));
                return;
            }

            if (item.Action != MailAction.ClaimGift || item.Claimed) return;
            var result = await _gifts.ClaimAsync(item.Id.Substring(GiftMailSource.Prefix.Length));
            if (_service != null) ShowResult(result);
        }

        private async void OnClaimAll()
        {
            var result = await _gifts.ClaimAllAsync();
            if (_service != null) ShowResult(result);
        }

        private void OnDeleteRead() => _service?.DeleteRead();

        private static string Today()
        {
            try
            {
                return ServiceLocator.Get<ServerTime>().UtcNow.ToString("yyyy-MM-dd");
            }
            catch (InvalidOperationException)
            {
                return "";
            }
        }
    }
}
