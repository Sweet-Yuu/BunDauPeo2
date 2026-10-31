using System;

namespace BunDauPeo.Core
{
    // Một mục = một lần bấm Quay 1 hoặc Quay 10. CharacterIds là mảng vì C# 9 không cho struct có field initializer.
    [Serializable]
    public struct GachaHistoryEntry
    {
        public long Utc;
        public string BannerId;
        public int Count;
        public string[] CharacterIds;
    }
}
