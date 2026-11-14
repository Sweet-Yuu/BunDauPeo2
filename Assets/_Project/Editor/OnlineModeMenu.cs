using BunDauPeo.Boot;
using UnityEditor;

namespace BunDauPeo.EditorTools
{
    // Chọn server khi Play trong Editor (spec 5.12). Thiết lập của Editor trong EditorPrefs, không phải dữ liệu người chơi.
    [InitializeOnLoad]
    public static class OnlineModeMenu
    {
        private const string PrefKey = "BunDauPeo.OnlineMode.UseUgs";
        private const string LocalItem = "Tools/BunDauPeo/Online Mode/Local";
        private const string UgsItem = "Tools/BunDauPeo/Online Mode/UGS (development)";

        // Chạy lại sau mỗi lần nạp domain, nên giá trị còn qua các lần Play.
        static OnlineModeMenu() => GameBootstrap.UseUgsInEditor = EditorPrefs.GetBool(PrefKey, false);

        [MenuItem(LocalItem)]
        private static void UseLocal() => Set(false);

        [MenuItem(UgsItem)]
        private static void UseUgs() => Set(true);

        [MenuItem(LocalItem, true)]
        private static bool LocalChecked()
        {
            Menu.SetChecked(LocalItem, !GameBootstrap.UseUgsInEditor);
            return true;
        }

        [MenuItem(UgsItem, true)]
        private static bool UgsChecked()
        {
            Menu.SetChecked(UgsItem, GameBootstrap.UseUgsInEditor);
            return true;
        }

        private static void Set(bool useUgs)
        {
            EditorPrefs.SetBool(PrefKey, useUgs);
            GameBootstrap.UseUgsInEditor = useUgs;
        }
    }
}
