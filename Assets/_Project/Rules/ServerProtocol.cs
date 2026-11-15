using System;

namespace BunDauPeo.Rules
{
    // Dùng chung giữa game và module Cloud Code (ServerModule/): không được dùng UnityEngine.
    public static class ServerProtocol
    {
        public const string Module = "BunDauPeoServer";
        public const string Bootstrap = "Bootstrap";
        public const string ResetPlayer = "ResetPlayer";
        public const string SaveKey = "save";
    }

    // Field công khai, kiểu cơ bản: JsonUtility (game) và Newtonsoft.Json (Cloud Code) cho cùng một chuỗi.
    [Serializable]
    public class BootstrapResponse
    {
        public long ServerUtcTicks;
    }
}
