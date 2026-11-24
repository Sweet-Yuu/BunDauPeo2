using System;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Màn Nhân vật: danh sách thẻ; bấm thẻ mở panel chi tiết (tab Cấp: nâng cấp bằng thuốc EXP + Coin; tab Sao: nâng sao).
    // Vẽ lại mỗi khi Roster, ví hay cấp đổi (Gacha, Reset ở panel DEV, thuốc từ quán).
    public class CharactersView : MonoBehaviour
    {
        private static readonly Color TabActive = new(0.30f, 0.50f, 0.90f);
        private static readonly Color TabIdle = new(0.30f, 0.34f, 0.46f);

        [SerializeField] private CharacterCardView[] _cards;
        [SerializeField] private GameObject _detailRoot;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _levelTabButton;
        [SerializeField] private Button _starTabButton;
        [SerializeField] private GameObject _levelGroup;
        [SerializeField] private GameObject _starGroup;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _rarityText;
        [SerializeField] private Text _starsText;
        [SerializeField] private Text _levelLineText;
        [SerializeField] private Text _hpText;
        [SerializeField] private Text _attackText;
        [SerializeField] private Text _defenseText;
        [SerializeField] private Text _speedText;
        // Tab Cấp.
        [SerializeField] private Text _levelChangeText;
        [SerializeField] private Text _levelPreviewText;
        [SerializeField] private Text _levelCapText;
        [SerializeField] private Text _potionText;
        [SerializeField] private Text _coinText;
        [SerializeField] private Button _levelUpButton;
        [SerializeField] private Text _levelUpLabel;
        // Tab Sao.
        [SerializeField] private Text _starPreviewText;
        [SerializeField] private Text _fragmentsText;
        [SerializeField] private Button _starUpButton;
        [SerializeField] private Text _starUpLabel;
        [SerializeField] private Text _statusText;

        private Roster _roster;
        private Wallet _wallet;
        private StarService _stars;
        private StarConfig _starConfig;
        private LevelConfig _levelConfig;
        private CharacterLevelService _levels;
        private PlayerLevelService _player;
        // null = đang ở danh sách; khác null = panel chi tiết của nhân vật đó.
        private string _selectedId;
        private bool _starTab;
        private bool _subscribed;

        private void OnEnable()
        {
            if (_cards == null || _cards.Length == 0 || _starUpButton == null || _levelUpButton == null || _detailRoot == null) return;

            try
            {
                _roster = ServiceLocator.Get<Roster>();
                _wallet = ServiceLocator.Get<Wallet>();
                _stars = ServiceLocator.Get<StarService>();
                _starConfig = ServiceLocator.Get<StarConfig>();
                _levelConfig = ServiceLocator.Get<LevelConfig>();
                _levels = ServiceLocator.Get<CharacterLevelService>();
                _player = ServiceLocator.Get<PlayerLevelService>();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(CharactersView)}: {e.Message}", this);
                return;
            }

            _roster.Changed += OnStateChanged;
            _wallet.Changed += OnStateChanged;
            _levels.Changed += OnStateChanged;
            _player.Changed += OnStateChanged;
            _starUpButton.onClick.AddListener(StarUp);
            _levelUpButton.onClick.AddListener(LevelUp);
            _backButton.onClick.AddListener(Back);
            _prevButton.onClick.AddListener(() => Step(-1));
            _nextButton.onClick.AddListener(() => Step(1));
            _levelTabButton.onClick.AddListener(() => SetTab(false));
            _starTabButton.onClick.AddListener(() => SetTab(true));
            for (var i = 0; i < _cards.Length; i++)
            {
                var index = i;
                _cards[i].Button.onClick.AddListener(() => Select(index));
            }

            _subscribed = true;
            _selectedId = null;
            _starTab = false;
            _statusText.text = "";
            Redraw();
        }

        private void OnDisable()
        {
            if (!_subscribed) return;
            _subscribed = false;
            _roster.Changed -= OnStateChanged;
            _wallet.Changed -= OnStateChanged;
            _levels.Changed -= OnStateChanged;
            _player.Changed -= OnStateChanged;
            _starUpButton.onClick.RemoveListener(StarUp);
            _levelUpButton.onClick.RemoveListener(LevelUp);
            _backButton.onClick.RemoveListener(Back);
            _prevButton.onClick.RemoveAllListeners();
            _nextButton.onClick.RemoveAllListeners();
            _levelTabButton.onClick.RemoveAllListeners();
            _starTabButton.onClick.RemoveAllListeners();
            foreach (var card in _cards) card.Button.onClick.RemoveAllListeners();
        }

        // Đổi từ nơi khác (Reset, Gacha, thuốc): dòng trạng thái cũ không còn đúng.
        private void OnStateChanged()
        {
            _statusText.text = "";
            Redraw();
        }

        // Mở chi tiết theo id ở tab Sao (nút Thông báo "đủ mảnh"); id không có trong danh sách thì không làm gì.
        public void SelectById(string id)
        {
            if (!_subscribed || id == null) return;

            if (!Rows().Any(r => r.Character.Id == id)) return;
            _selectedId = id;
            _starTab = true;
            _statusText.text = "";
            Redraw();
        }

        private System.Collections.Generic.IReadOnlyList<CharacterListRow> Rows() =>
            CharacterListRows.Build(_roster, _stars, _levels, _levelConfig, _starConfig);

        private void Select(int index)
        {
            var rows = Rows();
            if (index >= rows.Count) return;
            _selectedId = rows[index].Character.Id;
            _starTab = false;
            _statusText.text = "";
            Redraw();
        }

        private void Back()
        {
            _selectedId = null;
            _statusText.text = "";
            Redraw();
        }

        private void Step(int step)
        {
            _selectedId = CharacterDetailNav.Neighbor(Rows().Select(r => r.Character.Id).ToList(), _selectedId, step);
            _statusText.text = "";
            Redraw();
        }

        private void SetTab(bool star)
        {
            _starTab = star;
            _statusText.text = "";
            Redraw();
        }

        private void LevelUp()
        {
            if (_selectedId == null) return;
            var result = _levels.TryLevelUp(_selectedId);
            // Thành công đã bắn Changed (vẽ lại); đặt chữ sau cùng để Redraw không xoá nó.
            Redraw();
            _statusText.text = CharactersViewText.LevelStatusText(result);
        }

        private async void StarUp()
        {
            if (_selectedId == null) return;
            StarUpResult result;
            try
            {
                result = await _stars.TryStarUpAsync(_selectedId);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
                return;
            }

            // Panel bị tắt trong lúc chờ: state đã áp, không còn gì để vẽ.
            if (this == null || !isActiveAndEnabled) return;
            // Áp state đã bắn Roster.Changed (vẽ lại) khi thành công; đặt chữ sau cùng để Redraw không xoá nó.
            Redraw();
            _statusText.text = CharactersViewText.StatusText(result);
        }

        // shortcut: lưới cố định, không phân trang; thừa thẻ so với số nhân vật đã sở hữu thì ẩn. Hết chỗ thì thêm cuộn.
        private void Redraw()
        {
            var rows = Rows();
            var selected = -1;
            for (var i = 0; i < rows.Count; i++)
                if (rows[i].Character.Id == _selectedId) selected = i;
            // Nhân vật đang xem không còn (Reset): về danh sách.
            if (selected < 0) _selectedId = null;

            for (var i = 0; i < _cards.Length; i++)
            {
                if (i < rows.Count) _cards[i].Bind(rows[i], i == selected);
                else _cards[i].Hide();
            }

            _detailRoot.SetActive(selected >= 0);
            if (selected < 0) return;

            _levelTabButton.targetGraphic.color = _starTab ? TabIdle : TabActive;
            _starTabButton.targetGraphic.color = _starTab ? TabActive : TabIdle;
            _levelGroup.SetActive(!_starTab);
            _starGroup.SetActive(_starTab);
            var row = rows[selected];
            var character = row.Character;
            var cap = _levels.MaxLevel;
            _nameText.text = string.IsNullOrEmpty(character.DisplayName) ? character.Id : character.DisplayName;
            _rarityText.text = character.Rarity.ToString();
            _starsText.text = CharactersViewText.StarsLine(row.Stars);
            _levelLineText.text = CharactersViewText.LevelLine(row.Level, cap);
            _hpText.text = CharactersViewText.StatLine("hp", row.Now.HP, row.Now.HP, true);
            _attackText.text = CharactersViewText.StatLine("attack", row.Now.Attack, row.Now.Attack, true);
            _defenseText.text = CharactersViewText.StatLine("defense", row.Now.Defense, row.Now.Defense, true);
            _speedText.text = CharactersViewText.StatLine("speed", row.Now.Speed, row.Now.Speed, true);

            // Tab Sao: hành vi cũ.
            _starPreviewText.text = string.Join("   ", CharactersViewText.StatLine("hp", row.Now.HP, row.Next.HP, row.IsMax),
                CharactersViewText.StatLine("attack", row.Now.Attack, row.Next.Attack, row.IsMax),
                CharactersViewText.StatLine("defense", row.Now.Defense, row.Next.Defense, row.IsMax));
            _fragmentsText.text = CharactersViewText.FragmentsLine(row);
            _starUpLabel.text = CharactersViewText.ButtonLabel(row);
            _starUpButton.interactable = row.CanStarUp;

            // Tab Cấp.
            var preview = CharacterUpgradePreview.Build(character, _stars.GetStarUps(character.Id), row.Level, cap, _levelConfig, _starConfig.StatPercentPerStar);
            var (potionCost, coinCost) = _levels.GetNextCost(character.Id);
            var potionHave = _wallet.Get(CurrencyType.ExpPotion);
            var coinHave = _wallet.Get(CurrencyType.Coin);
            _levelChangeText.text = preview.HasNext
                ? UiText.Format("characters.level_now", row.Level, row.Level + 1)
                : UiText.Format("characters.level_max", row.Level);
            _levelPreviewText.text = string.Join("   ", CharactersViewText.StatLine("hp", preview.Before.HP, preview.After.HP, !preview.HasNext),
                CharactersViewText.StatLine("attack", preview.Before.Attack, preview.After.Attack, !preview.HasNext),
                CharactersViewText.StatLine("defense", preview.Before.Defense, preview.After.Defense, !preview.HasNext));
            _levelCapText.text = UiText.Format("characters.level_cap", cap);
            _potionText.text = UiText.Format("characters.need_potion", potionCost, potionHave);
            _coinText.text = UiText.Format("characters.need_coin", coinCost, coinHave);
            _levelUpLabel.text = CharactersViewText.UpgradeButtonLabel(potionCost, coinCost);
            var gate = !preview.HasNext ? LevelUpStatus.AtPlayerLevel
                : potionHave < potionCost ? LevelUpStatus.NotEnoughPotion
                : coinHave < coinCost ? LevelUpStatus.NotEnoughCoin
                : LevelUpStatus.Success;
            _levelUpButton.interactable = gate == LevelUpStatus.Success;
            // Lý do nút mờ nằm ở dòng trạng thái; kết quả của lần bấm ghi đè sau Redraw.
            _statusText.text = _starTab ? "" : CharactersViewText.UpgradeDisabledReason(gate);
        }
    }
}
