using System;
using System.Collections.Generic;
using BunDauPeo.Rules;
using UnityEngine;

namespace BunDauPeo.Core
{
    // Cầu nối PlayerSave (máy) ↔ ServerState (server giữ, spec con 4.3): server là bản chính của 8 field này.
    public static class ServerStateApplier
    {
        // Chép 8 field server giữ vào save; Gem nằm trong Balances nên thay mục cũ.
        public static void Apply(PlayerSave save, ServerState state)
        {
            SetGem(save, state.Gem);
            save.OwnedCharacterIds = new List<string>(state.OwnedCharacterIds);
            save.Fragments = new List<FragmentCount>(state.Fragments);
            save.StarUps = new List<StarUpEntry>(state.StarUps);
            save.GachaPity = new List<PityEntry>(state.GachaPity);
            save.GachaHistory = new List<GachaHistoryEntry>(state.Clone().GachaHistory);
            save.Stamina = new StaminaState { Current = state.Stamina.Current, AnchorUtcTicks = state.Stamina.AnchorUtcTicks };
            save.ClearedStages = new List<string>(state.ClearedStages);
            save.Missions = (state.Missions ?? new MissionState()).Clone();
        }

        // Ngược lại của Apply, cho LocalGameServer.Seed: bản đệm đầy đủ thành state (Revision 0, chưa có phiên).
        public static ServerState FromSave(PlayerSave save)
        {
            var gem = 0L;
            foreach (var b in save.Balances)
                if (b.Type == CurrencyType.Gem) gem += b.Amount;
            var state = new ServerState
            {
                Gem = gem,
                OwnedCharacterIds = new List<string>(save.OwnedCharacterIds),
                Fragments = new List<FragmentCount>(save.Fragments),
                StarUps = new List<StarUpEntry>(save.StarUps),
                GachaPity = new List<PityEntry>(save.GachaPity),
                GachaHistory = new List<GachaHistoryEntry>(save.GachaHistory.ConvertAll(HistoryToTicks)),
                Stamina = new StaminaState { Current = save.Stamina.Current, AnchorUtcTicks = save.Stamina.AnchorUtcTicks },
                ClearedStages = new List<string>(save.ClearedStages),
                Missions = (save.Missions ?? new MissionState()).Clone(),
            };
            return state.Clone();
        }

        // Đợt 1 ghi giây Unix (~1,7 tỉ); ticks của mọi ngày sau năm 0001 đều lớn hơn 1e11 nhiều. Dưới ngưỡng là giây.
        private const long SecondsBelow = 100_000_000_000L;

        private static GachaHistoryEntry HistoryToTicks(GachaHistoryEntry entry)
        {
            if (entry.Utc > 0 && entry.Utc < SecondsBelow) entry.Utc = DateTimeOffset.FromUnixTimeSeconds(entry.Utc).UtcTicks;
            return entry;
        }

        // Bản tải lên khoá save: không còn field server giữ, nên máy khác hay bản sửa tay không đổi được chúng.
        public static string ToUploadJson(PlayerSave save)
        {
            var copy = JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(save));
            copy.Balances.RemoveAll(b => b.Type == CurrencyType.Gem);
            copy.OwnedCharacterIds = new List<string>();
            copy.Fragments = new List<FragmentCount>();
            copy.StarUps = new List<StarUpEntry>();
            copy.GachaPity = new List<PityEntry>();
            copy.GachaHistory = new List<GachaHistoryEntry>();
            copy.Stamina = new StaminaState();
            copy.ClearedStages = new List<string>();
            copy.Missions = new MissionState();
            return JsonUtility.ToJson(copy);
        }

        // Bỏ khỏi đội, nhân viên, avatar mọi id không còn sở hữu (save đợt 1 có nhân vật gacha mà server không biết). Trả có đổi gì không.
        public static bool PruneUnowned(PlayerSave save)
        {
            var owned = new HashSet<string>(save.OwnedCharacterIds);
            var changed = save.BattleTeam.RemoveAll(id => !owned.Contains(id)) > 0;
            changed |= save.Staff.RemoveAll(s => !owned.Contains(s.CharacterId)) > 0;
            if (!string.IsNullOrEmpty(save.AvatarCharacterId) && !owned.Contains(save.AvatarCharacterId))
            {
                save.AvatarCharacterId = "";
                changed = true;
            }
            return changed;
        }

        private static void SetGem(PlayerSave save, long gem)
        {
            save.Balances.RemoveAll(b => b.Type == CurrencyType.Gem);
            if (gem > 0) save.Balances.Add(new CurrencyAmount(CurrencyType.Gem, gem));
        }
    }
}
