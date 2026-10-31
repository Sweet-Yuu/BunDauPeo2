using System.Collections.Generic;
using BunDauPeo.Characters;

namespace BunDauPeo.Gacha
{
    public enum GachaPullStatus { Success, UnknownBanner, InvalidBanner, NotEnoughCurrency }

    public readonly struct GachaPullEntry
    {
        public GachaPullEntry(CharacterDefinition character, bool isNew, long fragmentsGained)
        {
            Character = character;
            IsNew = isNew;
            FragmentsGained = fragmentsGained;
        }

        public CharacterDefinition Character { get; }
        public bool IsNew { get; }
        public long FragmentsGained { get; }
    }

    public sealed class GachaPullResult
    {
        public GachaPullStatus Status;
        // false khi persist() thất bại: bộ nhớ vẫn đúng, chỉ chưa xuống đĩa.
        public bool Saved;
        // Rỗng (không null) khi Status != Success.
        public IReadOnlyList<GachaPullEntry> Entries;
    }
}
