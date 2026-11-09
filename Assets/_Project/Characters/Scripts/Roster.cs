using System;
using System.Collections.Generic;
using BunDauPeo.Core;
using UnityEngine;

namespace BunDauPeo.Characters
{
    // Nơi duy nhất sửa danh sách nhân vật, đội và nhân viên; thao tác thẳng trên PlayerSave như Wallet.
    public sealed class Roster
    {
        public const int MaxBattleTeamSize = 5;

        private readonly PlayerSave _save;
        private readonly CharacterCatalog _catalog;

        public event Action Changed;

        public Roster(PlayerSave save, CharacterCatalog catalog)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

            // Id lạ được giữ nguyên trong save để khôi phục khi SO quay lại, nên chỉ cảnh báo, không xoá.
            var warned = new HashSet<string>();
            foreach (var id in _save.OwnedCharacterIds) WarnIfUnknown(id, warned);
            foreach (var id in _save.BattleTeam) WarnIfUnknown(id, warned);
            foreach (var staff in _save.Staff) WarnIfUnknown(staff.CharacterId, warned);
        }

        // Gọi khi save bị thay từ bên ngoài (reset), để view nghe Changed vẽ lại.
        public void NotifyChanged() => Changed.SafeInvoke();

        public bool IsOwned(string id) => id != null && _save.OwnedCharacterIds.Contains(id);

        public bool Add(string id)
        {
            if (!_catalog.TryGet(id, out _) || IsOwned(id)) return false;
            _save.OwnedCharacterIds.Add(id);
            Changed.SafeInvoke();
            return true;
        }

        public bool AddFragments(string id, long count)
        {
            if (count <= 0 || !IsOwned(id) || !_catalog.TryGet(id, out _)) return false;

            var index = _save.Fragments.FindIndex(f => f.CharacterId == id);
            if (index < 0)
            {
                _save.Fragments.Add(new FragmentCount { CharacterId = id, Count = count });
            }
            else
            {
                var current = _save.Fragments[index].Count;
                var updated = current > long.MaxValue - count ? long.MaxValue : current + count;
                _save.Fragments[index] = new FragmentCount { CharacterId = id, Count = updated };
            }

            Changed.SafeInvoke();
            return true;
        }

        // Public như mọi hàm sửa khác của Roster; ngoài StarService thì không ai nên gọi.
        public bool SpendFragments(string id, long count)
        {
            if (count <= 0 || id == null) return false;

            var index = _save.Fragments.FindIndex(f => f.CharacterId == id);
            if (index < 0 || _save.Fragments[index].Count < count) return false;

            _save.Fragments[index] = new FragmentCount { CharacterId = id, Count = _save.Fragments[index].Count - count };
            Changed.SafeInvoke();
            return true;
        }

        public long GetFragments(string id)
        {
            if (id == null) return 0;
            var index = _save.Fragments.FindIndex(f => f.CharacterId == id);
            return index < 0 ? 0 : _save.Fragments[index].Count;
        }

        public bool AddToTeam(string id)
        {
            // Kiểm hết điều kiện trước khi gỡ khỏi Staff, để bị từ chối thì nhân vật vẫn ở quán.
            if (!IsOwned(id) || !_catalog.TryGet(id, out _)) return false;
            if (_save.BattleTeam.Contains(id) || _save.BattleTeam.Count >= MaxBattleTeamSize) return false;

            _save.Staff.RemoveAll(s => s.CharacterId == id);
            _save.BattleTeam.Add(id);
            Changed.SafeInvoke();
            return true;
        }

        public bool RemoveFromTeam(string id)
        {
            if (_save.BattleTeam.RemoveAll(x => x == id) == 0) return false;
            Changed.SafeInvoke();
            return true;
        }

        // Chọn đội dở rồi Hủy: trả đội và vai ở quán về như lúc mở. Cả hai danh sách, vì AddToTeam gỡ người khỏi quán.
        public TeamSnapshot SnapshotTeam() => new(_save.BattleTeam.ToArray(), _save.Staff.ToArray());

        public void RestoreTeam(TeamSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            _save.BattleTeam.Clear();
            _save.BattleTeam.AddRange(snapshot.Team);
            _save.Staff.Clear();
            _save.Staff.AddRange(snapshot.Staff);
            Changed.SafeInvoke();
        }

        public sealed class TeamSnapshot
        {
            internal TeamSnapshot(string[] team, StaffAssignment[] staff)
            {
                Team = team;
                Staff = staff;
            }

            internal string[] Team { get; }
            internal StaffAssignment[] Staff { get; }
        }

        public bool AssignStaff(string id, StaffRole role)
        {
            if (!IsOwned(id) || !_catalog.TryGet(id, out _)) return false;
            if (_save.Staff.Exists(s => s.CharacterId == id && s.Role == role)) return false;

            _save.BattleTeam.RemoveAll(x => x == id);
            _save.Staff.RemoveAll(s => s.CharacterId == id);
            _save.Staff.Add(new StaffAssignment { CharacterId = id, Role = role });
            Changed.SafeInvoke();
            return true;
        }

        public bool RemoveStaff(string id)
        {
            if (_save.Staff.RemoveAll(s => s.CharacterId == id) == 0) return false;
            Changed.SafeInvoke();
            return true;
        }

        public IReadOnlyList<CharacterDefinition> GetOwned() => Resolve(_save.OwnedCharacterIds);

        public IReadOnlyList<CharacterDefinition> GetBattleTeam() => Resolve(_save.BattleTeam);

        public IReadOnlyList<CharacterDefinition> GetStaff(StaffRole role)
        {
            var ids = new List<string>();
            foreach (var staff in _save.Staff)
                if (staff.Role == role) ids.Add(staff.CharacterId);
            return Resolve(ids);
        }

        private List<CharacterDefinition> Resolve(List<string> ids)
        {
            var result = new List<CharacterDefinition>(ids.Count);
            foreach (var id in ids)
                if (_catalog.TryGet(id, out var definition)) result.Add(definition);
            return result;
        }

        private void WarnIfUnknown(string id, HashSet<string> warned)
        {
            if (!_catalog.TryGet(id, out _) && warned.Add(id ?? string.Empty))
                Debug.LogWarning($"Roster: id '{id}' is not in the catalog; kept in save but ignored.");
        }
    }
}
