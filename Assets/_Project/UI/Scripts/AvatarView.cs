using System;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Hình tròn của một nhân vật (spec UI hub C 5.4): màu theo độ hiếm, chữ cái đầu của tên. Dùng cho avatar Lobby, avatar lớn và mỗi ô của Hồ sơ.
    public class AvatarView : MonoBehaviour
    {
        public static readonly Color NoneColor = new(0.55f, 0.57f, 0.62f);

        [SerializeField] private Image _circle;
        [SerializeField] private Text _initial;
        // Avatar Lobby tự theo hồ sơ đã lưu; hai chỗ còn lại do ProfilePopup gọi Show.
        [SerializeField] private bool _followProfile;

        private PlayerProfile _profile;

        // null: xám, không chữ.
        public void Show(CharacterDefinition character)
        {
            if (_circle == null || _initial == null)
            {
                Debug.LogError($"{nameof(AvatarView)}: references are not assigned.", this);
                return;
            }

            _circle.color = character != null ? GachaViewText.RarityColor(character.Rarity) : NoneColor;
            _initial.text = AvatarRule.Initial(character);
        }

        // Nhân vật đang chọn. Id rỗng, hỏng, không có trong catalog hay không còn sở hữu → null (save cũ hỏng nhẹ, Review Focus 4).
        public static CharacterDefinition Current()
        {
            try
            {
                var roster = ServiceLocator.Get<Roster>();
                var owned = roster.GetOwned();
                return AvatarRule.Resolve(ServiceLocator.Get<PlayerProfile>().AvatarId, roster.IsOwned, id => owned.FirstOrDefault(c => c.Id == id));
            }
            catch (InvalidOperationException)
            {
                // Mở không qua Boot: avatar xám.
                return null;
            }
        }

        private void OnEnable()
        {
            if (!_followProfile) return;

            try
            {
                _profile = ServiceLocator.Get<PlayerProfile>();
                _profile.Changed += Refresh;
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(AvatarView)}: {e.Message}", this);
                _profile = null;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_profile != null) _profile.Changed -= Refresh;
            _profile = null;
        }

        private void Refresh() => Show(Current());
    }
}
