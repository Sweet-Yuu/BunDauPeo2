using System;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Thanh Lv + EXP người chơi; vẽ lại mỗi khi PlayerLevelService đổi (nhận EXP, Reset).
    public class PlayerLevelBarView : MonoBehaviour
    {
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _expText;
        // Lớp màu kéo từ trái; đổi anchorMax.x thay vì Image.Filled để không cần sprite riêng.
        [SerializeField] private RectTransform _fill;

        private PlayerLevelService _service;
        private Wallet _wallet;

        private void OnEnable()
        {
            if (_levelText == null || _expText == null || _fill == null) return;
            try
            {
                _service = ServiceLocator.Get<PlayerLevelService>();
                _wallet = ServiceLocator.Get<Wallet>();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(PlayerLevelBarView)}: {e.Message}", this);
                _service = null;
                return;
            }

            _service.Changed += Redraw;
            _wallet.Changed += Redraw; // Reset báo qua ví
            Redraw();
        }

        private void OnDisable()
        {
            if (_service == null) return;
            _service.Changed -= Redraw;
            _wallet.Changed -= Redraw;
            _service = null;
        }

        private void Redraw()
        {
            if (_service == null) return;
            _levelText.text = UiText.Format("level.player", _service.Level);
            _expText.text = _service.IsMaxLevel ? UiText.Get("level.player_max") : UiText.Format("level.player_exp", _service.Exp, _service.ExpToNext);
            var anchor = _fill.anchorMax;
            anchor.x = PlayerLevelBarRule.Fill(_service.Exp, _service.ExpToNext, _service.IsMaxLevel);
            _fill.anchorMax = anchor;
        }
    }
}
