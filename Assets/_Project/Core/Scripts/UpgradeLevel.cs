using System;

namespace BunDauPeo.Core
{
    // Level 0 hoặc không có dòng nghĩa là chưa mua.
    [Serializable]
    public struct UpgradeLevel
    {
        public string UpgradeId;
        public int Level;
    }
}
