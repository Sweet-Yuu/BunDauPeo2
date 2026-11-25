using System;

namespace BunDauPeo.Core
{
    // Level là cấp đã lưu (có thể cao hơn cấp hiệu lực); không phải số lần nâng.
    [Serializable]
    public struct CharacterLevelEntry
    {
        public string CharacterId;
        public int Level;
    }
}
