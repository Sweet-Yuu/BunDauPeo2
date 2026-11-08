using System;

namespace BunDauPeo.Core
{
    // Count là số lần đã nâng sao, không phải số sao tuyệt đối.
    [Serializable]
    public struct StarUpEntry
    {
        public string CharacterId;
        public int Count;
    }
}
