#nullable disable
using System;
using System.Collections.Generic;
using BunDauPeo.Core;

namespace BunDauPeo.Rules
{
    // Khoá "state" (Protected) của người chơi: mọi thứ server giữ (spec con 4.1). Thứ tự field cố định để test so JSON hai phía.
    [Serializable]
    public class ServerState
    {
        public int Version = 1;
        public long Revision;
        public long Gem;
        public List<string> OwnedCharacterIds = new();
        public List<FragmentCount> Fragments = new();
        public List<StarUpEntry> StarUps = new();
        public List<PityEntry> GachaPity = new();
        public List<GachaHistoryEntry> GachaHistory = new();
        public StaminaState Stamina = new();
        public List<string> ClearedStages = new();
        public string PendingTicket = "";
        public string PendingStageId = "";
        public string ActiveSessionId = "";
        public string LastRequestId = "";
        public string LastResultJson = "";

        // Sao chép sâu: thêm field List mới mà quên chép thì ServerStateTests.Clone_IsDeep_ForEveryListField đỏ.
        public ServerState Clone()
        {
            var c = (ServerState)MemberwiseClone();
            c.OwnedCharacterIds = new List<string>(OwnedCharacterIds);
            c.Fragments = new List<FragmentCount>(Fragments);
            c.StarUps = new List<StarUpEntry>(StarUps);
            c.GachaPity = new List<PityEntry>(GachaPity);
            c.GachaHistory = new List<GachaHistoryEntry>(GachaHistory.Count);
            foreach (var h in GachaHistory)
            {
                var copy = h;
                if (h.CharacterIds != null) copy.CharacterIds = (string[])h.CharacterIds.Clone();
                c.GachaHistory.Add(copy);
            }
            c.Stamina = new StaminaState { Current = Stamina.Current, AnchorUtcTicks = Stamina.AnchorUtcTicks };
            c.ClearedStages = new List<string>(ClearedStages);
            return c;
        }

        public static ServerState NewGame(GameData data)
        {
            return new ServerState
            {
                Gem = data.NewGame.StartingGem,
                OwnedCharacterIds = new List<string>(data.NewGame.StarterCharacterIds),
                Stamina = new StaminaState { Current = data.Stamina.Max, AnchorUtcTicks = 0 },
            };
        }
    }
}
