using BunDauPeo.Core;

namespace BunDauPeo.UI
{
    public enum HubGoto { None, Stages, Gacha, Characters, Afk, Staff, Upgrade, Profile }

    // Bảng "Đi tới" của spec đợt 3 8.1.
    public static class MissionGoto
    {
        public static HubGoto Destination(string missionId)
        {
            switch (missionId)
            {
                case "daily_win_5":
                case "new_first_win":
                case "new_win_10": return HubGoto.Stages;
                case "daily_pull_1":
                case "new_first_pull": return HubGoto.Gacha;
                case "new_star_up": return HubGoto.Characters;
                case "new_afk": return HubGoto.Afk;
                case "new_staff": return HubGoto.Staff;
                case "new_upgrade": return HubGoto.Upgrade;
                case "new_name":
                case "new_link": return HubGoto.Profile;
                default: return HubGoto.None;
            }
        }
    }
}
