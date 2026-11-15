using BunDauPeo.Core;
using BunDauPeo.Online;

namespace BunDauPeo.Boot
{
    // Bản build luôn dùng UGS; Editor mặc định Local, bật UGS qua Tools/BunDauPeo/Online Mode (spec 5.12).
    public static class ServerChoice
    {
        public const string Development = "development";
        public const string Production = "production";

        // null = LocalGameServer (Boot tự dựng, nạp bản đệm).
        public static IGameServer Create(bool isEditor, bool useUgsInEditor, bool isDevelopmentBuild)
        {
            if (isEditor) return useUgsInEditor ? new UgsGameServer(Development) : null;
            return new UgsGameServer(isDevelopmentBuild ? Development : Production);
        }
    }
}
