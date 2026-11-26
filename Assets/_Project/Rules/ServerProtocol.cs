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
        public const string StateKey = "state";

        // Tên lệnh = tên hằng (spec con 6.2).
        public const string GachaPull = "GachaPull";
        public const string EnterStage = "EnterStage";
        public const string CompleteStage = "CompleteStage";
        public const string CompleteStory = "CompleteStory";
        public const string StarUp = "StarUp";
        public const string DevAddGem = "DevAddGem";
        public const string DevRefillStamina = "DevRefillStamina";
        public const string ClaimMission = "ClaimMission";
        public const string ClaimAllMissions = "ClaimAllMissions";
        public const string GachaFreePull = "GachaFreePull";
        public const string DevResetMissions = "DevResetMissions";
        public const string DevAddStamina = "DevAddStamina";
        public const string ListGifts = "ListGifts";
        public const string ClaimGift = "ClaimGift";
        public const string ClaimAllGifts = "ClaimAllGifts";

        // Máy gửi Version; server từ chối bản dưới MinProtocolVersion (hàng rào bản cũ, spec con 3.3).
        public const int Version = 3;
        public const int MinProtocolVersion = 3;

        // Message của ClientTooOldException bắt đầu bằng chuỗi này để máy nhận ra qua lỗi Cloud Code.
        public const string ClientTooOldTag = "ClientTooOld";
    }

    // Field công khai, kiểu cơ bản: JsonUtility (game) và Newtonsoft.Json (Cloud Code) cho cùng một chuỗi.
    [Serializable]
    public class BootstrapResponse
    {
        public long ServerUtcTicks;
        public string SessionId = "";
        public string DataHash = "";
        public ServerState State;
    }
}
