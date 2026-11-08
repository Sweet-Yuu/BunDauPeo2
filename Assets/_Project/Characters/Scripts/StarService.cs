using System;
using BunDauPeo.Core;

namespace BunDauPeo.Characters
{
    // Giá trị ghim vì có thể đi vào test và log; chỉ thêm ở cuối.
    public enum StarUpStatus { Success, InvalidConfig, Unknown, NotOwned, MaxStars, NotEnoughFragments }

    public struct StarUpResult
    {
        public StarUpStatus Status;
        public int StarsAfter;
        public bool Saved;
    }

    // Giữ luật nâng sao; không giữ trạng thái chạy (chỉ đọc PlayerSave) nên Reset không cần báo riêng.
    public sealed class StarService
    {
        private readonly PlayerSave _save;
        private readonly Roster _roster;
        private readonly CharacterCatalog _catalog;
        private readonly StarConfig _config;
        private readonly Func<bool> _persist;

        public bool IsConfigValid { get; }

        public StarService(PlayerSave save, Roster roster, CharacterCatalog catalog, StarConfig config, Func<bool> persist)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
            IsConfigValid = _config.Validate().Count == 0;
        }

        // Số lần nâng có hiệu lực: bị chặn để sao không vượt 5 dù SO đổi sao khởi điểm sau khi người chơi đã nâng.
        public int GetStarUps(string id)
        {
            if (id == null || !_catalog.TryGet(id, out var character)) return 0;
            var start = character.Stars;
            if (start < 1 || start > StarConfig.MaxStars) return 0;

            var index = FindEntry(id);
            if (index < 0) return 0;
            return Math.Clamp(_save.StarUps[index].Count, 0, StarConfig.MaxStars - start);
        }

        public int GetStars(string id)
        {
            if (id == null || !_catalog.TryGet(id, out var character)) return 0;
            return character.Stars + GetStarUps(id);
        }

        // 0 khi đã 5★, id lạ hoặc config lỗi.
        public int GetNextCost(string id)
        {
            if (!IsConfigValid) return 0;
            var stars = GetStars(id);
            return stars < 1 || stars >= StarConfig.MaxStars ? 0 : _config.CostToReach(stars + 1);
        }

        public CombatStats GetCombatStats(CharacterDefinition character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            return GetCombatStats(character, GetStarUps(character.Id));
        }

        public CombatStats GetCombatStats(CharacterDefinition character, int starUps)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            var stats = character.CombatStats;
            if (!IsConfigValid) return stats;

            var percent = 100L + (long)_config.StatPercentPerStar * Math.Max(starUps, 0);
            stats.HP = Scale(stats.HP, percent);
            stats.Attack = Scale(stats.Attack, percent);
            stats.Defense = Scale(stats.Defense, percent);
            return stats;
        }

        // Mọi bước từ chối đứng trước bước đổi.
        public StarUpResult TryStarUp(string id)
        {
            if (!IsConfigValid) return Rejected(StarUpStatus.InvalidConfig, id);
            if (id == null || !_catalog.TryGet(id, out _)) return Rejected(StarUpStatus.Unknown, id);
            if (!_roster.IsOwned(id)) return Rejected(StarUpStatus.NotOwned, id);
            if (GetStars(id) >= StarConfig.MaxStars) return Rejected(StarUpStatus.MaxStars, id);

            var cost = GetNextCost(id);
            // Sao khởi điểm < 1 (SO chưa qua Setup Project) cho giá 0: không được nâng miễn phí.
            if (cost <= 0) return Rejected(StarUpStatus.InvalidConfig, id);
            if (_roster.GetFragments(id) < cost) return Rejected(StarUpStatus.NotEnoughFragments, id);

            // Đổi sao trước, rồi SpendFragments phát Changed một lần khi cả sao lẫn mảnh đã đổi.
            var index = FindEntry(id);
            if (index < 0) _save.StarUps.Add(new StarUpEntry { CharacterId = id, Count = 1 });
            else _save.StarUps[index] = new StarUpEntry { CharacterId = id, Count = Math.Max(_save.StarUps[index].Count, 0) + 1 };
            _roster.SpendFragments(id, cost);

            return new StarUpResult { Status = StarUpStatus.Success, StarsAfter = GetStars(id), Saved = _persist() };
        }

        private StarUpResult Rejected(StarUpStatus status, string id) =>
            new StarUpResult { Status = status, StarsAfter = GetStars(id) };

        private int FindEntry(string id) => _save.StarUps.FindIndex(e => e.CharacterId == id);

        // Làm tròn nửa lên bằng số nguyên long để không tràn.
        private static int Scale(int baseValue, long percent) => (int)((baseValue * percent + 50) / 100);
    }
}
