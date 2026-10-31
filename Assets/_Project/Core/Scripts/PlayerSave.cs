using System;
using System.Collections.Generic;

namespace BunDauPeo.Core
{
    [Serializable]
    public struct StaffAssignment
    {
        public string CharacterId;
        public StaffRole Role;
    }

    // JsonUtility không đọc được Dictionary nên mọi tập hợp là List; field mới phải có giá trị khởi tạo để save cũ vẫn đọc được.
    [Serializable]
    public class PlayerSave
    {
        public int Version = 1;
        public List<CurrencyAmount> Balances = new();
        public List<string> OwnedCharacterIds = new();
        public List<string> BattleTeam = new();
        public List<StaffAssignment> Staff = new();
    }
}
