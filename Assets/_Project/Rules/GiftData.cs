#nullable disable
using System;
using System.Collections.Generic;

namespace BunDauPeo.Rules
{
    // Quà từ Dashboard (spec đợt 4 mục 4.1). Field công khai, kiểu cơ bản: JsonUtility và Newtonsoft cho cùng một chuỗi.
    [Serializable]
    public class GiftReward
    {
        public string Type = "";
        public long Amount;
    }

    [Serializable]
    public class GiftData
    {
        public string Id = "";
        public string TitleVi = "";
        public string TitleEn = "";
        public List<GiftReward> Rewards = new();
        public long StartUtcTicks;
        public long EndUtcTicks;
    }

    // Một dòng ListGifts: chỉ quà đang trong hạn.
    [Serializable]
    public class GiftRow
    {
        public string Id = "";
        public string Title = "";
        public List<GiftReward> Rewards = new();
        public long EndUtcTicks;
        public bool Claimed;
    }

    [Serializable]
    public class GiftListPayload
    {
        public List<GiftRow> Gifts = new();
    }

    // Coin và Material máy cộng vào Wallet; Gem server đã cộng vào state.
    [Serializable]
    public class GiftClaimPayload
    {
        public long Gem;
        public long Coin;
        public long Material;
    }
}
