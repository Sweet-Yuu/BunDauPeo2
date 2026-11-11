using System.Collections.Generic;

namespace BunDauPeo.UI
{
    // Luật thuần: màn/trang nào hiện ô tài nguyên nào (spec 5.3).
    public static class ResourceBarRule
    {
        // menuOpen -> None. Có trang mở -> hợp Slots của các trang đang mở (trang không cài IResourcePage tính None).
        public static ResourceSlots For(HubTab current, bool menuOpen, IEnumerable<IHubPage> openPages)
        {
            if (menuOpen)
            {
                return ResourceSlots.None;
            }

            var anyOpen = false;
            var union = ResourceSlots.None;
            if (openPages != null)
            {
                foreach (var page in openPages)
                {
                    if (page == null || !page.IsOpen)
                    {
                        continue;
                    }

                    anyOpen = true;
                    if (page is IResourcePage resourcePage)
                    {
                        union |= resourcePage.Slots;
                    }
                }
            }

            return anyOpen ? union : ForTab(current);
        }

        // Lobby (ô Quán, không trang, không Menu) dùng bố cục trái cạnh avatar; còn lại neo trên-phải.
        public static bool IsLobbyLayout(HubTab current, bool menuOpen, bool anyPageOpen) =>
            current == HubTab.Restaurant && !menuOpen && !anyPageOpen;

        // Đầy thì không có dòng đếm ngược; giây làm tròn lên như CombatViewText.StaminaCard.
        public static string StaminaNext(int current, int max, double secondsToNext) =>
            current >= max ? "" : UiText.Format("hub.res_stamina_next", CombatViewText.Clock(secondsToNext));

        private static ResourceSlots ForTab(HubTab tab) => tab switch
        {
            HubTab.Restaurant => ResourceSlots.All,
            HubTab.Characters => ResourceSlots.Coin,
            HubTab.Stages => ResourceSlots.Stamina,
            HubTab.Gacha => ResourceSlots.Gem,
            _ => ResourceSlots.None,
        };
    }
}
