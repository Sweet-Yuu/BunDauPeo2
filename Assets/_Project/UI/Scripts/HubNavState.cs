using UnityEngine;

namespace BunDauPeo.UI
{
    public enum HubNavResult { Changed, ComingSoon, MenuOpened }

    // Ô đang đứng của thanh dưới; thuần C#, không biết panel nào.
    public sealed class HubNavState
    {
        public HubTab Current { get; private set; } = HubTab.Restaurant;
        public bool MenuOpen { get; private set; }
        public HubTab Highlighted => MenuOpen ? HubTab.Menu : Current;

        public HubNavResult Select(HubTab tab)
        {
            switch (tab)
            {
                case HubTab.Story:
                    return HubNavResult.ComingSoon;
                case HubTab.Menu:
                    MenuOpen = true;
                    return HubNavResult.MenuOpened;
                default:
                    Current = tab;
                    MenuOpen = false;
                    return HubNavResult.Changed;
            }
        }

        public void CloseMenu() => MenuOpen = false;

        // Ô lạ (Story, Menu, ngoài enum) làm ô chờ thì về Restaurant; cờ Menu vẫn giữ.
        public static HubNavState FromPending(HubTab tab, bool menu)
        {
            var valid = tab == HubTab.Restaurant || tab == HubTab.Characters || tab == HubTab.Stages || tab == HubTab.Gacha;
            return new HubNavState { Current = valid ? tab : HubTab.Restaurant, MenuOpen = menu };
        }
    }

    // Ô chờ mở sau khi nạp lại Restaurant (đổi ngôn ngữ, về từ trận). Lấy một lần rồi xoá.
    public static class HubPending
    {
        private static bool _has;
        private static HubTab _tab;
        private static bool _menu;

        public static void Set(HubTab tab, bool menu)
        {
            _has = true;
            _tab = tab;
            _menu = menu;
        }

        public static bool TryTake(out HubTab tab, out bool menu)
        {
            tab = _tab;
            menu = _menu;
            var had = _has;
            _has = false;
            _tab = HubTab.Restaurant;
            _menu = false;
            return had;
        }

        // Tắt domain reload thì static sống qua các lần Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => TryTake(out _, out _);
    }
}
