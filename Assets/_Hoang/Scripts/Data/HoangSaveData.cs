using System;
using BunDauPeo.Core;

namespace Hoang.Remake
{
    // Giữ các field gốc để Wallet/Roster và dịch vụ hiện có cùng đọc một object.
    [Serializable]
    public sealed class HoangSaveData : PlayerSave
    {
        public int RestaurantSchema = 1;
        public SkillTreeSaveState Skills = new();
        public FacilitySaveState Facilities = new();
        public ShopDailySaveState ShopDaily = new();
    }
}
