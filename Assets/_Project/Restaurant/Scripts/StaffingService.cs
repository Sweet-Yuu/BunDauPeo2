using System;
using BunDauPeo.Characters;
using BunDauPeo.Core;

namespace BunDauPeo.Restaurant
{
    public enum StaffAssignResult { Success, NotOwned, RoleFull }

    // Số ô lấy từ hồ sơ hiện tại (không từ UpgradeService), nên mua ô mới có hiệu lực ngay ở lần gán kế tiếp.
    public sealed class StaffingService
    {
        private readonly Roster _roster;
        private readonly Func<RestaurantProfile> _currentProfile;

        public StaffingService(Roster roster, Func<RestaurantProfile> currentProfile)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _currentProfile = currentProfile ?? throw new ArgumentNullException(nameof(currentProfile));
        }

        public StaffAssignResult TryAssign(string characterId, StaffRole role)
        {
            if (!IsOwned(characterId)) return StaffAssignResult.NotOwned;
            if (IsFull(role, characterId, null)) return StaffAssignResult.RoleFull;
            _roster.AssignStaff(characterId, role);
            return StaffAssignResult.Success;
        }

        public StaffAssignResult TryReplace(string oldId, string newId, StaffRole role)
        {
            // Kiểm hết trước khi gỡ người cũ, để bị từ chối thì vai không bị bỏ trống.
            if (!IsOwned(newId)) return StaffAssignResult.NotOwned;
            if (IsFull(role, newId, oldId)) return StaffAssignResult.RoleFull;
            _roster.RemoveStaff(oldId);
            _roster.AssignStaff(newId, role);
            return StaffAssignResult.Success;
        }

        public bool Remove(string characterId) => _roster.RemoveStaff(characterId);

        // Roster.IsOwned chỉ xét save; GetOwned bỏ qua Id không có trong catalog, nên AssignStaff không thể từ chối vì lý do đó.
        private bool IsOwned(string characterId)
        {
            foreach (var character in _roster.GetOwned())
                if (character.Id == characterId) return true;
            return false;
        }

        // Không đếm chính người được gán (và người bị thay): họ không chiếm thêm ô.
        private bool IsFull(StaffRole role, string incomingId, string leavingId)
        {
            var profile = _currentProfile();
            var others = 0;
            foreach (var worker in profile.Workers(role))
                if (worker.CharacterId != incomingId && worker.CharacterId != leavingId) others++;
            return others >= profile.Slots(role);
        }
    }
}
