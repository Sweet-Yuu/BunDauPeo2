using System;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Màn Nhân vật: chọn thẻ, xem chỉ số trước/sau, bấm nâng sao. Vẽ lại mỗi khi Roster đổi (Gacha, Reset ở panel DEV).
    public class CharactersView : MonoBehaviour
    {
        [SerializeField] private CharacterCardView[] _cards;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _rarityText;
        [SerializeField] private Text _starsText;
        [SerializeField] private Text _hpText;
        [SerializeField] private Text _attackText;
        [SerializeField] private Text _defenseText;
        [SerializeField] private Text _speedText;
        [SerializeField] private Text _fragmentsText;
        [SerializeField] private Button _starUpButton;
        [SerializeField] private Text _starUpLabel;
        [SerializeField] private Text _statusText;

        private Roster _roster;
        private StarService _stars;
        private string _selectedId;
        private bool _subscribed;

        private void OnEnable()
        {
            if (_cards == null || _cards.Length == 0 || _starUpButton == null) return;

            try
            {
                _roster = ServiceLocator.Get<Roster>();
                _stars = ServiceLocator.Get<StarService>();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(CharactersView)}: {e.Message}", this);
                return;
            }

            _roster.Changed += OnRosterChanged;
            _starUpButton.onClick.AddListener(StarUp);
            for (var i = 0; i < _cards.Length; i++)
            {
                var index = i;
                _cards[i].Button.onClick.AddListener(() => Select(index));
            }

            _subscribed = true;
            _selectedId = null;
            _statusText.text = "";
            Redraw();
        }

        private void OnDisable()
        {
            if (!_subscribed) return;
            _subscribed = false;
            _roster.Changed -= OnRosterChanged;
            _starUpButton.onClick.RemoveListener(StarUp);
            foreach (var card in _cards) card.Button.onClick.RemoveAllListeners();
        }

        // Đổi từ nơi khác (Reset, Gacha): dòng trạng thái cũ không còn đúng.
        private void OnRosterChanged()
        {
            _statusText.text = "";
            Redraw();
        }

        // Chọn theo id (nút Thông báo "đủ mảnh"); id không có trong danh sách thì không làm gì.
        public void SelectById(string id)
        {
            if (!_subscribed || id == null) return;

            if (!CharacterListRows.Build(_roster, _stars).Any(r => r.Character.Id == id)) return;
            _selectedId = id;
            _statusText.text = "";
            Redraw();
        }

        private void Select(int index)
        {
            var rows = CharacterListRows.Build(_roster, _stars);
            if (index >= rows.Count) return;
            _selectedId = rows[index].Character.Id;
            _statusText.text = "";
            Redraw();
        }

        private void StarUp()
        {
            if (_selectedId == null) return;
            var result = _stars.TryStarUp(_selectedId);
            // TryStarUp đã bắn Roster.Changed (vẽ lại) khi thành công; đặt chữ sau cùng để Redraw không xoá nó.
            Redraw();
            _statusText.text = CharactersViewText.StatusText(result);
        }

        // shortcut: lưới cố định, không phân trang; thừa thẻ so với số nhân vật đã sở hữu thì ẩn. Hết chỗ thì thêm cuộn.
        private void Redraw()
        {
            var rows = CharacterListRows.Build(_roster, _stars);
            var selected = -1;
            for (var i = 0; i < rows.Count; i++)
                if (rows[i].Character.Id == _selectedId) selected = i;
            if (selected < 0 && rows.Count > 0) selected = 0;
            _selectedId = selected >= 0 ? rows[selected].Character.Id : null;

            for (var i = 0; i < _cards.Length; i++)
            {
                if (i < rows.Count) _cards[i].Bind(rows[i], i == selected);
                else _cards[i].Hide();
            }

            if (selected < 0)
            {
                _nameText.text = _rarityText.text = _starsText.text = _hpText.text = _attackText.text = "";
                _defenseText.text = _speedText.text = _fragmentsText.text = _starUpLabel.text = "";
                _starUpButton.interactable = false;
                return;
            }

            var row = rows[selected];
            var character = row.Character;
            _nameText.text = string.IsNullOrEmpty(character.DisplayName) ? character.Id : character.DisplayName;
            _rarityText.text = character.Rarity.ToString();
            _starsText.text = CharactersViewText.StarsLine(row.Stars);
            _hpText.text = CharactersViewText.StatLine("hp", row.Now.HP, row.Next.HP, row.IsMax);
            _attackText.text = CharactersViewText.StatLine("attack", row.Now.Attack, row.Next.Attack, row.IsMax);
            _defenseText.text = CharactersViewText.StatLine("defense", row.Now.Defense, row.Next.Defense, row.IsMax);
            _speedText.text = CharactersViewText.StatLine("speed", row.Now.Speed, row.Now.Speed, true);
            _fragmentsText.text = CharactersViewText.FragmentsLine(row);
            _starUpLabel.text = CharactersViewText.ButtonLabel(row);
            _starUpButton.interactable = row.CanStarUp;
        }
    }
}
