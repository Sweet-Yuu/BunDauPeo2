using System;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Thanh tài nguyên chung (spec UI hub B 5.3): một thanh cho mọi màn, ô hiện theo ResourceBarRule. Lobby neo trái cạnh avatar,
    // màn khác neo trên-phải. View nằm ngoài thanh để còn chạy khi thanh tắt; chỉ ghi Text khi số đổi.
    public class ResourceBarView : MonoBehaviour
    {
        private const float SlotWidth = 280f, BarHeight = 76f, Margin = 20f, LobbyLeft = 180f;
        // Ngoài Lobby thanh dời trái nhường nút Thông báo: 20 lề + 110 nút + 10 cách (spec UI hub C 5.3).
        public const float RightMargin = 140f;

        [SerializeField] private HubNav _hub;
        [SerializeField] private RectTransform _bar;
        // Thứ tự: Thể lực, Coin, Material, Gem (khớp bit của ResourceSlots).
        [SerializeField] private GameObject[] _slots;
        [SerializeField] private Text[] _slotTexts;
        [SerializeField] private Text _staminaNextText;
        // Thể lực, Coin, Gem (Material không có nút +).
        [SerializeField] private Button[] _plusButtons;

        private static readonly ResourceSlots[] Order = { ResourceSlots.Stamina, ResourceSlots.Coin, ResourceSlots.Material, ResourceSlots.Gem };

        private Wallet _wallet;
        private StaminaService _stamina;
        private bool _ready;
        private ResourceSlots _shownSlots = ResourceSlots.None;
        private bool _shownLobby;
        private bool _layoutDone;
        private int _shownCurrent = -1, _shownMax = -1;
        private long _shownNextKey = long.MinValue;
        private readonly long[] _shownAmounts = { -1, -1, -1, -1 };

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _wallet = ServiceLocator.Get<Wallet>();
                _stamina = ServiceLocator.Get<CombatService>().Stamina;
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(ResourceBarView)}: {e.Message}", this);
                return;
            }

            foreach (var plus in _plusButtons) plus.onClick.AddListener(_hub.ShowComingSoon);
            _layoutDone = false;
            _shownCurrent = -1;
            _shownMax = -1;
            _shownNextKey = long.MinValue;
            for (var i = 0; i < _shownAmounts.Length; i++) _shownAmounts[i] = -1;
            _ready = true;
        }

        private void OnDisable()
        {
            if (!_ready) return;

            _ready = false;
            foreach (var plus in _plusButtons)
                if (plus != null && _hub != null) plus.onClick.RemoveListener(_hub.ShowComingSoon);
        }

        private bool HasAllReferences()
        {
            var ok = _hub != null && _bar != null && _slots != null && _slots.Length == Order.Length && Array.TrueForAll(_slots, s => s != null)
                     && _slotTexts != null && _slotTexts.Length == Order.Length && Array.TrueForAll(_slotTexts, t => t != null)
                     && _staminaNextText != null && _plusButtons != null && _plusButtons.Length == 3 && Array.TrueForAll(_plusButtons, b => b != null);
            if (!ok) Debug.LogError($"{nameof(ResourceBarView)}: a serialized reference is not assigned.", this);
            return ok;
        }

        private void Update()
        {
            if (!_ready) return;

            var slots = ResourceBarRule.For(_hub.State.Current, _hub.State.MenuOpen, _hub.OpenPages);
            var visible = slots != ResourceSlots.None && !_hub.BarHidden;
            if (_bar.gameObject.activeSelf != visible) _bar.gameObject.SetActive(visible);
            if (!visible) return;

            var lobby = ResourceBarRule.IsLobbyLayout(_hub.State.Current, _hub.State.MenuOpen, _hub.AnyPageOpen);
            if (!_layoutDone || slots != _shownSlots || lobby != _shownLobby) ApplyLayout(slots, lobby);

            if (slots.HasFlag(ResourceSlots.Stamina)) RedrawStamina();
            RedrawAmount(1, slots, ResourceSlots.Coin, "Coin", CurrencyType.Coin);
            RedrawAmount(2, slots, ResourceSlots.Material, "Material", CurrencyType.Material);
            RedrawAmount(3, slots, ResourceSlots.Gem, "Gem", CurrencyType.Gem);
        }

        // Lobby: bốn ô liền nhau từ (180, 20). Màn khác: chỉ ô cần, dồn sát góc trên-phải.
        private void ApplyLayout(ResourceSlots slots, bool lobby)
        {
            _layoutDone = true;
            _shownSlots = slots;
            _shownLobby = lobby;

            var count = 0;
            for (var i = 0; i < Order.Length; i++)
            {
                var on = slots.HasFlag(Order[i]);
                _slots[i].SetActive(on);
                if (!on) continue;

                ((RectTransform)_slots[i].transform).anchoredPosition = new Vector2(count * SlotWidth, 0f);
                count++;
            }

            var anchor = lobby ? new Vector2(0f, 1f) : Vector2.one;
            _bar.anchorMin = anchor;
            _bar.anchorMax = anchor;
            _bar.pivot = anchor;
            _bar.anchoredPosition = lobby ? new Vector2(LobbyLeft, -Margin) : new Vector2(-RightMargin, -Margin);
            _bar.sizeDelta = new Vector2(count * SlotWidth, BarHeight);
        }

        private void RedrawStamina()
        {
            var current = _stamina.Current;
            var max = _stamina.Max;
            if (current != _shownCurrent || max != _shownMax)
            {
                _shownCurrent = current;
                _shownMax = max;
                _slotTexts[0].text = UiText.Format("combat.stamina_full", current, max);
                _shownNextKey = long.MinValue;
            }

            // Dòng đếm ngược đổi mỗi giây (làm tròn lên); đầy thì rỗng.
            var key = current >= max ? -1 : (long)Math.Ceiling(Math.Max(0d, _stamina.SecondsToNext));
            if (key == _shownNextKey) return;
            _shownNextKey = key;
            _staminaNextText.text = ResourceBarRule.StaminaNext(current, max, _stamina.SecondsToNext);
        }

        private void RedrawAmount(int index, ResourceSlots slots, ResourceSlots flag, string name, CurrencyType type)
        {
            if (!slots.HasFlag(flag)) return;

            var amount = _wallet.Get(type);
            if (amount == _shownAmounts[index]) return;
            _shownAmounts[index] = amount;
            _slotTexts[index].text = $"{name} {amount}";
        }
    }
}
