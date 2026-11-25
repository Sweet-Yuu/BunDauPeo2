using System;
using System.Linq;
using System.Threading.Tasks;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Rules;
using UnityEngine;

namespace BunDauPeo.Combat
{
    public enum StartBattleStatus { Success, UnknownStage, NotBattle, EmptyTeam, InvalidCharacter, InvalidConfig }

    public readonly struct StartBattleResult
    {
        public StartBattleResult(StartBattleStatus status, BattleSession session = null)
        {
            Status = status;
            Session = session;
        }

        public StartBattleStatus Status { get; }
        public BattleSession Session { get; }
    }

    public enum EnterStageStatus
    {
        Entered, InvalidConfig, BattleInProgress, Unknown, Invalid, WrongKind, Locked, EmptyTeam, NotEnoughStamina, InvalidCharacter, SaveFailed, Offline
    }

    public readonly struct EnterStageResult
    {
        public EnterStageResult(EnterStageStatus status, BattleSession session = null, int staminaSpent = 0, bool saved = false)
        {
            Status = status;
            Session = session;
            StaminaSpent = staminaSpent;
            Saved = saved;
        }

        public EnterStageStatus Status { get; }
        public BattleSession Session { get; }
        public int StaminaSpent { get; }
        public bool Saved { get; }
    }

    // Giữ trận đang đánh và dựng trận mới từ đội của Roster. EnterStage là đường vào trận thật (kiểm màn, trừ thể lực, lưu);
    // StartBattle chỉ cho dải DEV, không kiểm mở màn hay thể lực.
    public sealed class CombatService
    {
        private readonly Roster _roster;
        private readonly CharacterLevelService _levels;
        private readonly PlayerLevelService _playerLevel;
        private readonly LevelConfig _levelConfig;
        private readonly StarConfig _starConfig;
        private readonly System.Random _random;
        private readonly Func<bool> _persist;
        private readonly IGameServer _server;
        private readonly CommandGate _gate;
        private readonly Func<ServerState, bool> _apply;

        // apply chép state server vào PlayerSave và báo view; trả false khi lưu bản đệm thất bại.
        public CombatService(CombatConfig config, Roster roster, StarService stars, CharacterLevelService levels,
            PlayerLevelService playerLevel, LevelConfig levelConfig, StarConfig starConfig, StaminaService stamina,
            StageProgressService progress, System.Random random, Func<bool> persist,
            IGameServer server, CommandGate gate, Func<ServerState, bool> apply)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            Stars = stars ?? throw new ArgumentNullException(nameof(stars));
            _levels = levels ?? throw new ArgumentNullException(nameof(levels));
            _playerLevel = playerLevel ?? throw new ArgumentNullException(nameof(playerLevel));
            _levelConfig = levelConfig ?? throw new ArgumentNullException(nameof(levelConfig));
            _starConfig = starConfig ?? throw new ArgumentNullException(nameof(starConfig));
            Stamina = stamina ?? throw new ArgumentNullException(nameof(stamina));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));

            // Config lỗi thì không dùng gì từ nó nữa: Boot vẫn chạy Gacha và quán.
            var errors = config.Validate();
            IsRunning = errors.Count == 0;
            if (!IsRunning) Debug.LogError($"{nameof(CombatService)}: invalid config, combat not running:\n- {string.Join("\n- ", errors)}");
        }

        public CombatConfig Config { get; }
        public StarService Stars { get; }
        public StaminaService Stamina { get; }
        public StageProgressService Progress { get; }
        public bool IsRunning { get; }
        public BattleSession Current { get; private set; }
        // Nhớ giữa các trận trong phiên chơi, không lưu.
        public bool AutoEnabled { get; set; }
        public bool FastEnabled { get; set; }

        // Thất bại thì Current giữ nguyên.
        public StartBattleResult StartBattle(string stageId)
        {
            if (!IsRunning) return new StartBattleResult(StartBattleStatus.InvalidConfig);
            if (!Config.Stages.TryGet(stageId, out var stage)) return new StartBattleResult(StartBattleStatus.UnknownStage);
            if (stage.Kind != StageKind.Battle) return new StartBattleResult(StartBattleStatus.NotBattle);

            var team = _roster.GetBattleTeam();
            if (team.Count == 0) return new StartBattleResult(StartBattleStatus.EmptyTeam);
            if (team.Any(c => !IsBattleReady(c))) return new StartBattleResult(StartBattleStatus.InvalidCharacter);

            Current = new BattleSession(team, stage, Config, _random, StatsOf);
            return new StartBattleResult(StartBattleStatus.Success, Current);
        }

        // Máy kiểm trước để không gửi lệnh thừa; server tính lại, trừ thể lực và cấp vé (spec con 6.2). Thất bại thì
        // thể lực, ví, save và Current đều nguyên.
        public async Task<EnterStageResult> EnterStageAsync(string stageId)
        {
            if (!IsRunning) return new EnterStageResult(EnterStageStatus.InvalidConfig);
            // Trận đã Won/Lost không chặn (Thử lại); đang đánh dở thì bấm đôi sẽ không trừ hai lần.
            if (Current != null && (Current.State == BattleState.PlayerTurn || Current.State == BattleState.EnemyTurn))
                return new EnterStageResult(EnterStageStatus.BattleInProgress);

            var team = _roster.GetBattleTeam();
            var can = Progress.CanEnter(stageId, team.Count);
            if (can != StageEnterStatus.Ready) return new EnterStageResult(RejectionOf(can));

            if (team.Any(c => !IsBattleReady(c))) return new EnterStageResult(EnterStageStatus.InvalidCharacter);

            // CanEnter vừa tra được màn này; nếu hai danh mục lệch nhau thì dừng trước khi gửi.
            if (!Config.Stages.TryGet(stageId, out var stage)) return new EnterStageResult(EnterStageStatus.Unknown);
            // Dựng trận trước lệnh: dựng ném lỗi thì server chưa trừ gì.
            var session = new BattleSession(team, stage, Config, _random, StatsOf);
            var ids = team.Select(c => c.Id).ToList();

            var result = await _gate.RunAsync(id => _server.EnterStageAsync(id, stageId, ids));
            // Lỗi mạng cuối cùng (hộp mạng đã hiện) hoặc phiên hỏng: không đổi gì.
            if (!result.Ok) return new EnterStageResult(EnterStageStatus.Offline);

            var response = result.Value;
            if ((CommandCode)response.Code != CommandCode.Ok) return new EnterStageResult(RejectionOf((CommandCode)response.Code));

            EnterStagePayload payload;
            try
            {
                payload = JsonUtility.FromJson<EnterStagePayload>(response.ResultJson);
            }
            catch (ArgumentException e)
            {
                Debug.LogException(e);
                return new EnterStageResult(EnterStageStatus.Offline);
            }

            if (payload == null || response.State == null || string.IsNullOrEmpty(payload.Ticket))
                return new EnterStageResult(EnterStageStatus.Offline);

            session.Ticket = payload.Ticket;
            session.EntryPaid = true;
            // Lưu trước, rồi mới có trận (như cũ): lưu ném lỗi thì không có trận nửa vời.
            // Vào trận thành công: mỗi điểm thể lực tiêu là 1 EXP người chơi (lưu cùng lần _apply bên dưới).
            _playerLevel.AddExp(payload.StaminaCost, persist: false);
            var saved = _apply(response.State);
            Current = session;
            return new EnterStageResult(EnterStageStatus.Entered, session, stage.StaminaCost, saved);
        }

        // Chỉ số trong trận = gốc + % sao + % cấp; config cấp hoặc sao lỗi thì dùng chỉ số theo sao như trước.
        private CombatStats StatsOf(CharacterDefinition c) =>
            _levels.IsConfigValid && Stars.IsConfigValid
                ? GrowthStats.Get(c, Stars.GetStarUps(c.Id), _levels.GetLevel(c.Id), _starConfig.StatPercentPerStar, _levelConfig.StatPercentPerLevel)
                : Stars.GetCombatStats(c);

        private static EnterStageStatus RejectionOf(CommandCode code) => code switch
        {
            CommandCode.Invalid => EnterStageStatus.Invalid,
            CommandCode.WrongKind => EnterStageStatus.WrongKind,
            CommandCode.Locked => EnterStageStatus.Locked,
            CommandCode.EmptyTeam => EnterStageStatus.EmptyTeam,
            CommandCode.NotEnoughStamina => EnterStageStatus.NotEnoughStamina,
            CommandCode.NotOwned => EnterStageStatus.InvalidCharacter,
            _ => EnterStageStatus.Unknown,
        };

        // Trạng thái nào CanEnter thêm về sau mà chưa ánh xạ thì bị từ chối (Unknown), không rơi xuống bước trừ thể lực.
        private static EnterStageStatus RejectionOf(StageEnterStatus status) => status switch
        {
            StageEnterStatus.Invalid => EnterStageStatus.Invalid,
            StageEnterStatus.WrongKind => EnterStageStatus.WrongKind,
            StageEnterStatus.Locked => EnterStageStatus.Locked,
            StageEnterStatus.EmptyTeam => EnterStageStatus.EmptyTeam,
            StageEnterStatus.NotEnoughStamina => EnterStageStatus.NotEnoughStamina,
            _ => EnterStageStatus.Unknown,
        };

        public void EndBattle() => Current = null;

        public bool Persist() => _persist();

        // Dữ liệu trong PlayerSave đã bị ghi đè trước khi gọi; ở đây chỉ xoá trạng thái chạy (D-005).
        public void ResetRuntime()
        {
            Current = null;
            AutoEnabled = false;
            FastEnabled = false;
        }

        private static bool IsBattleReady(CharacterDefinition c) =>
            c.Validate().Count == 0
            && CombatSkillRules.Validate(c.BasicAttack, SkillKind.Basic).Count == 0
            && CombatSkillRules.Validate(c.Skill, SkillKind.Skill).Count == 0
            && CombatSkillRules.Validate(c.Ultimate, SkillKind.Ultimate).Count == 0;
    }
}
