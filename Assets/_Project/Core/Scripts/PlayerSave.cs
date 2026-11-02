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
        public List<FragmentCount> Fragments = new();
        public List<PityEntry> GachaPity = new();
        public List<GachaHistoryEntry> GachaHistory = new();
        public List<UpgradeLevel> Upgrades = new();
        // Tối đa 8 mục: 7 ngày trọn vẹn và hôm nay.
        public List<DailyPresenceEntry> DailyPresence = new();
        // Tối đa 10 mục, giá trị 0 bỏ lỡ, 1 trễ, 2 kịp.
        public List<int> IncidentScores = new();
        public AfkState Afk = new();
        // Id các màn đã thắng lần đầu; id lạ (màn đã bị xoá khỏi catalog) được giữ nguyên.
        public List<string> ClearedStages = new();
        public StaminaState Stamina = new();
    }
}
