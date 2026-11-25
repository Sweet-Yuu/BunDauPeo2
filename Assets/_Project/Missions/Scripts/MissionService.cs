using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using UnityEngine;

namespace BunDauPeo.Missions
{
    public enum MissionClaimStatus { Ok, NotReady, AlreadyClaimed, NotInWindow, Locked, NothingToClaim, Invalid, Offline }

    public struct MissionClaimResult
    {
        public MissionClaimStatus Status;
        public long Gem;
        public int Stamina;
        public int StaminaWasted;
    }

    public struct StaminaPreview
    {
        public int Have;
        public int Reward;
        public int Wasted;
        public int Cap;
        public int Granted => Reward - Wasted;
    }

    // Server trao thưởng (spec đợt 3 mục 6.1); máy chỉ kiểm sơ bộ để không gửi lệnh thừa rồi áp state server trả về.
    public sealed class MissionService
    {
        public const int TabDaily = 0, TabNewbie = 1;

        private readonly PlayerSave _save;
        private readonly GameData _data;
        private readonly IGameServer _server;
        private readonly CommandGate _gate;
        private readonly Func<ServerState, bool> _apply;
        private readonly Func<DateTime> _nowUtc;
        private readonly Func<IReadOnlyCollection<string>> _clientDone;

        // apply chép state vào PlayerSave và báo view (như GachaService); Changed báo trang Nhiệm vụ vẽ lại.
        public MissionService(PlayerSave save, GameData data, IGameServer server, CommandGate gate,
            Func<ServerState, bool> apply, Func<DateTime> nowUtc, Func<IReadOnlyCollection<string>> clientDone)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            _nowUtc = nowUtc ?? throw new ArgumentNullException(nameof(nowUtc));
            _clientDone = clientDone ?? throw new ArgumentNullException(nameof(clientDone));
        }

        public event Action Changed;

        // Việc tân thủ do máy báo (spec 3.15), dựng từ save. new_link: IAccountService.IsLinked (Task 9), chưa có thì false.
        public static List<string> ClientDoneFromSave(PlayerSave save, bool linked = false)
        {
            var done = new List<string>(5);
            if (!string.IsNullOrEmpty(save.PlayerName)) done.Add("new_name");
            if (save.Staff.Count > 0) done.Add("new_staff");
            if (save.Upgrades.Exists(u => u.Level > 0)) done.Add("new_upgrade");
            if (save.AfkCollected) done.Add("new_afk");
            if (linked) done.Add("new_link");
            return done;
        }

        public IReadOnlyList<MissionRow> Rows(int tab) => RowList(tab);

        private List<MissionRow> RowList(int tab)
        {
            var all = MissionRules.View(StateForView(), _data, _nowUtc(), _clientDone());
            var rows = new List<MissionRow>(all.Count);
            foreach (var row in all)
                if (TabOf(row.Id) == tab) rows.Add(row);
            return rows;
        }

        public MissionData Definition(string id) => _data.Missions.Find(d => d.Id == id);

        public int ClaimableCount(int tab)
        {
            var n = 0;
            foreach (var row in Rows(tab)) if (row.Claimable) n++;
            return n;
        }

        public TimeSpan UntilNextDay() => MissionRules.UntilNextDay(_nowUtc());

        // Thể lực sẽ mất nếu nhận ngay (cho hộp xác nhận); idOrNull = null là Nhận hết cả tab.
        public int PreviewStaminaWaste(int tab, string idOrNull) => PreviewStamina(tab, idOrNull).Wasted;

        public StaminaPreview PreviewStamina(int tab, string idOrNull)
        {
            var stamina = new StaminaState { Current = _save.Stamina.Current, AnchorUtcTicks = _save.Stamina.AnchorUtcTicks };
            var regen = _data.Stamina.RegenSeconds <= 0 ? 0 : _data.Stamina.RegenSeconds * TimeSpan.TicksPerSecond;
            var now = _nowUtc().Ticks;
            StaminaRules.Settle(stamina, now, _data.Stamina.Max, regen);
            var preview = new StaminaPreview { Have = stamina.Current, Cap = _data.Stamina.RewardCap };
            foreach (var row in RowList(tab))
            {
                if (idOrNull != null ? row.Id != idOrNull : !row.Claimable) continue;
                var def = _data.Missions.Find(d => d.Id == row.Id);
                if (def.RewardStamina <= 0) continue;
                var granted = StaminaRules.Add(stamina, def.RewardStamina, now, _data.Stamina.Max, regen, _data.Stamina.RewardCap);
                preview.Reward += def.RewardStamina;
                preview.Wasted += def.RewardStamina - granted;
            }
            return preview;
        }

        public async Task<MissionClaimResult> ClaimAsync(string id)
        {
            var def = string.IsNullOrEmpty(id) ? null : _data.Missions.Find(d => d.Id == id);
            if (def == null) return Fail(MissionClaimStatus.Invalid);
            var row = RowList(def.Tab).Find(r => r.Id == id);
            if (row.Locked) return Fail(MissionClaimStatus.Locked);
            if (row.Claimed) return Fail(MissionClaimStatus.AlreadyClaimed);
            if (row.Progress < row.Target) return Fail(MissionClaimStatus.NotReady);
            if (row.WindowState == 2 || row.WindowState == 3) return Fail(MissionClaimStatus.NotInWindow);

            var done = new List<string>(_clientDone());
            return await SendAsync(requestId => _server.ClaimMissionAsync(requestId, id, done));
        }

        public async Task<MissionClaimResult> ClaimAllAsync(int tab)
        {
            if (ClaimableCount(tab) == 0) return Fail(MissionClaimStatus.NothingToClaim);
            var done = new List<string>(_clientDone());
            return await SendAsync(requestId => _server.ClaimAllMissionsAsync(requestId, tab, done));
        }

        private async Task<MissionClaimResult> SendAsync(Func<string, Task<OnlineResult<CommandResponse>>> call)
        {
            var result = await _gate.RunAsync(call);
            if (!result.Ok) return Fail(MissionClaimStatus.Offline);

            var response = result.Value;
            var status = Map((CommandCode)response.Code);
            if (status != MissionClaimStatus.Ok) return Fail(status);

            MissionClaimPayload payload;
            try
            {
                payload = JsonUtility.FromJson<MissionClaimPayload>(response.ResultJson);
            }
            catch (ArgumentException e)
            {
                Debug.LogException(e);
                return Fail(MissionClaimStatus.Offline);
            }

            if (response.State == null || payload == null) return Fail(MissionClaimStatus.Offline);

            _apply(response.State);
            Changed.SafeInvoke();
            return new MissionClaimResult { Status = MissionClaimStatus.Ok, Gem = payload.Gem, Stamina = payload.Stamina, StaminaWasted = payload.StaminaWasted };
        }

        // View chỉ đọc Missions, ClearedStages, GachaHistory, StarUps của state: bọc lại từ save để dùng chung MissionRules.View với server.
        private ServerState StateForView() => new ServerState
        {
            Missions = _save.Missions,
            ClearedStages = _save.ClearedStages,
            GachaHistory = _save.GachaHistory,
            StarUps = _save.StarUps,
        };

        private int TabOf(string id) => _data.Missions.Find(d => d.Id == id).Tab;

        private static MissionClaimStatus Map(CommandCode code)
        {
            switch (code)
            {
                case CommandCode.Ok: return MissionClaimStatus.Ok;
                case CommandCode.NotReady: return MissionClaimStatus.NotReady;
                case CommandCode.AlreadyClaimed: return MissionClaimStatus.AlreadyClaimed;
                case CommandCode.NotInWindow: return MissionClaimStatus.NotInWindow;
                case CommandCode.Locked: return MissionClaimStatus.Locked;
                case CommandCode.NothingToClaim: return MissionClaimStatus.NothingToClaim;
                default: return MissionClaimStatus.Invalid;
            }
        }

        private static MissionClaimResult Fail(MissionClaimStatus status) => new MissionClaimResult { Status = status };
    }
}
