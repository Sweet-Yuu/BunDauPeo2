using System;

namespace BunDauPeo.Core
{
    // Pulls là số lượt kể từ SSR gần nhất của banner; banner chưa có dòng thì coi là 0.
    [Serializable]
    public struct PityEntry
    {
        public string BannerId;
        public int Pulls;
    }
}
