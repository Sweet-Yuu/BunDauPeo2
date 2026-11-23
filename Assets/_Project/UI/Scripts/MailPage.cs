using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using BunDauPeo.Missions;
using BunDauPeo.Restaurant;
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

        public bool IsOpen => _root != null && _root.activeSelf;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<MailService>();
                _restaurant = ServiceLocator.Get<RestaurantService>();
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
            foreach (var row in _rows) row.Button.onClick.RemoveAllListeners();
            _service = null;
            _restaurant = null;
        }

        private bool HasAllReferences()
        {
            var ok = _root != null && _openButton != null && _openDot != null && _closeButton != null && _listContent != null && _itemTemplate != null
                     && _detail != null && _detailTitle != null && _detailSender != null && _detailDate != null && _detailBody != null
                     && _attachBox != null && _actionButton != null && _emptyBox != null && _countText != null && _claimAllButton != null
                     && _deleteReadButton != null;
            if (!ok) Debug.LogError($"{nameof(MailPage)}: a serialized reference is not assigned.", this);
            return ok;
        }

        public void Open()
        {
            if (_service == null) return;
            _restaurant.Incidents.SetSpawnBlocked(this, true);
            _root.SetActive(true);
            _items = _service.Items();
            // Mở trang = mở thư đầu (thư ghim lên đầu): đã đọc, chấm đỏ tắt.
            _selectedId = _items.Count > 0 ? _items[0].Id : "";
            if (_items.Count > 0) _service.MarkRead(_items[0]);
            Refresh();
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
                row.Title.text = UiText.Get(item.TitleKey);
                row.Sender.text = UiText.Get("mail.sender");
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
                _detailTitle.text = UiText.Get(selected.TitleKey);
                _detailSender.text = UiText.Get("mail.sender");
                _detailDate.text = Today();
                _detailBody.text = UiText.Get(selected.BodyKey);
                _attachBox.SetActive(selected.HasAttachments);
                // Đợt 3 chỉ có thư liên kết: nút của thư là hành động của nó, không có phần đính kèm.
                _actionButton.gameObject.SetActive(selected.Action == MailAction.LinkAccount);
            }

            _countText.text = UiText.Format("mail.count", _items.Count, MailRule.Capacity);
            // Đợt 3 chưa có thư quà: Nhận hết luôn mờ (có trong khung để đợt 4 chỉ thêm nguồn thư).
            _claimAllButton.interactable = false;
            _deleteReadButton.interactable = _service.CanDeleteRead();
            // Mờ khi không bấm được (nút dựng bằng Image màu, không có tint tắt).
            foreach (var button in new[] { _claimAllButton, _deleteReadButton })
            {
                var color = button.targetGraphic.color;
                color.a = button.interactable ? 1f : 0.4f;
                button.targetGraphic.color = color;
            }
        }

        private void RefreshDot() => _openDot.SetActive(_service != null && _service.UnreadCount() > 0);

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

        // Nút Liên kết của thư: hộp chọn cách của Task 9. Chuyển tài khoản = bỏ session, về Tiêu đề.
        private void OnAction() =>
            AccountBoxes.ShowLink(AccountBoxes.NewFlow(() => SceneManager.LoadScene(TitleView.TitleScene)));

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
