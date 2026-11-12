using System;

namespace BunDauPeo.Core
{
    // Cầu nối để UI đọc/ghi tên và avatar mà không phụ thuộc Boot (như RestaurantBarPref).
    public sealed class PlayerProfile
    {
        private readonly Func<string> _getName;
        private readonly Func<string> _getAvatar;
        private readonly Func<string, bool> _setName;
        private readonly Func<string, bool> _setAvatar;

        // Phát sau mỗi lần set, kể cả khi lưu lỗi (giá trị mới vẫn nằm trong bộ nhớ).
        public event Action Changed;

        public PlayerProfile(Func<string> getName, Func<string> getAvatar, Func<string, bool> setName, Func<string, bool> setAvatar)
        {
            _getName = getName ?? throw new ArgumentNullException(nameof(getName));
            _getAvatar = getAvatar ?? throw new ArgumentNullException(nameof(getAvatar));
            _setName = setName ?? throw new ArgumentNullException(nameof(setName));
            _setAvatar = setAvatar ?? throw new ArgumentNullException(nameof(setAvatar));
        }

        public string Name => _getName() ?? "";

        public string AvatarId => _getAvatar() ?? "";

        // true khi đã lưu; false khi lưu thất bại.
        public bool SetName(string name) => Raise(_setName(name));

        public bool SetAvatar(string id) => Raise(_setAvatar(id));

        private bool Raise(bool saved)
        {
            Changed.SafeInvoke();
            return saved;
        }
    }
}
