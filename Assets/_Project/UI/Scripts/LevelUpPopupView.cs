using System;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Popup lên cấp người chơi. Cấp đã báo nằm ở static để lần lên cấp xảy ra lúc vào trận (đổi sang scene Battle) vẫn được báo khi về Lobby;
    // gắn với instance service nên Play/Reset mới bắt đầu lại từ cấp hiện có.
    public class LevelUpPopupView : MonoBehaviour
    {
        private static PlayerLevelService _owner;
        private static int _shownLevel;

        [SerializeField] private GameObject _root;
        [SerializeField] private Text _bodyText;
        [SerializeField] private Button _okButton;

        private PlayerLevelService _service;
        private Wallet _wallet;
        private LevelConfig _config;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void OnEnable()
        {
            if (_root == null || _bodyText == null || _okButton == null) return;
            try
            {
                _service = ServiceLocator.Get<PlayerLevelService>();
                _config = ServiceLocator.Get<LevelConfig>();
                _wallet = ServiceLocator.Get<Wallet>();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(LevelUpPopupView)}: {e.Message}", this);
                _service = null;
                return;
            }

            _root.SetActive(false);
            _okButton.onClick.AddListener(Close);
            _service.Changed += Check;
            _wallet.Changed += Check; // Reset hạ cấp: báo qua ví
            if (_owner != _service)
            {
                _owner = _service;
                _shownLevel = _service.Level;
            }
            else
            {
                Check();
            }
        }

        private void OnDisable()
        {
            if (_service == null) return;
            _service.Changed -= Check;
            _wallet.Changed -= Check;
            _okButton.onClick.RemoveListener(Close);
            _service = null;
        }

        private void Check()
        {
            var level = _service.Level;
            if (level <= _shownLevel)
            {
                _shownLevel = level;
                return;
            }

            var (coin, potion) = Reward(_config, _shownLevel, level);
            _shownLevel = level;
            _bodyText.text = UiText.Format("level.up_body", level, coin, potion);
            _root.SetActive(true);
        }

        // Thưởng cộng dồn cho các cấp (from, to].
        public static (long Coin, long Potion) Reward(LevelConfig config, int from, int to)
        {
            long coin = 0, potion = 0;
            for (var level = from + 1; level <= to; level++)
            {
                coin += (long)config.LevelUpRewardCoinPerLevel * level;
                potion += config.LevelUpRewardPotion;
            }

            return (coin, potion);
        }

        public void Close() => _root.SetActive(false);
    }
}
