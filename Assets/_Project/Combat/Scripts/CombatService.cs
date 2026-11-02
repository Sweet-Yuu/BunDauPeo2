using System;
using System.Linq;
using BunDauPeo.Characters;
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
        Entered, InvalidConfig, BattleInProgress, Unknown, Invalid, WrongKind, Locked, EmptyTeam, NotEnoughStamina, InvalidCharacter, SaveFailed
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
        private readonly System.Random _random;
        private readonly Func<bool> _persist;

        public CombatService(CombatConfig config, Roster roster, StaminaService stamina, StageProgressService progress,
            System.Random random, Func<bool> persist)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
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

            Current = new BattleSession(team, stage, Config, _random);
            return new StartBattleResult(StartBattleStatus.Success, Current);
        }

        // Mọi bước từ chối đứng trước bước trừ thể lực (spec 4.7): thất bại thì thể lực, ví, save và Current đều nguyên.
        public EnterStageResult EnterStage(string stageId)
        {
            if (!IsRunning) return new EnterStageResult(EnterStageStatus.InvalidConfig);
            // Trận đã Won/Lost không chặn (Thử lại); đang đánh dở thì bấm đôi sẽ không trừ hai lần.
            if (Current != null && (Current.State == BattleState.PlayerTurn || Current.State == BattleState.EnemyTurn))
                return new EnterStageResult(EnterStageStatus.BattleInProgress);

            var team = _roster.GetBattleTeam();
            switch (Progress.CanEnter(stageId, team.Count))
            {
                case StageEnterStatus.Unknown: return new EnterStageResult(EnterStageStatus.Unknown);
                case StageEnterStatus.Invalid: return new EnterStageResult(EnterStageStatus.Invalid);
                case StageEnterStatus.WrongKind: return new EnterStageResult(EnterStageStatus.WrongKind);
                case StageEnterStatus.Locked: return new EnterStageResult(EnterStageStatus.Locked);
                case StageEnterStatus.EmptyTeam: return new EnterStageResult(EnterStageStatus.EmptyTeam);
                case StageEnterStatus.NotEnoughStamina: return new EnterStageResult(EnterStageStatus.NotEnoughStamina);
            }

            if (team.Any(c => !IsBattleReady(c))) return new EnterStageResult(EnterStageStatus.InvalidCharacter);

            Config.Stages.TryGet(stageId, out var stage);
            var session = new BattleSession(team, stage, Config, _random);
            switch (Stamina.TrySpend(stage.StaminaCost, _persist))
            {
                case StaminaSpendStatus.SaveFailed: return new EnterStageResult(EnterStageStatus.SaveFailed);
                case StaminaSpendStatus.Rejected: return new EnterStageResult(EnterStageStatus.NotEnoughStamina);
            }

            session.EntryPaid = true;
            Current = session;
            return new EnterStageResult(EnterStageStatus.Entered, session, stage.StaminaCost, true);
        }

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
