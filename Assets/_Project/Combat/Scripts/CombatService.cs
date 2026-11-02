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

    // Giữ trận đang đánh và dựng trận mới từ đội của Roster. Không kiểm mở màn hay thể lực (để dải DEV vào thẳng).
    public sealed class CombatService
    {
        private readonly Roster _roster;
        private readonly System.Random _random;
        private readonly Func<bool> _persist;

        public CombatService(CombatConfig config, Roster roster, System.Random random, Func<bool> persist)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));

            // Config lỗi thì không dùng gì từ nó nữa: Boot vẫn chạy Gacha và quán.
            var errors = config.Validate();
            IsRunning = errors.Count == 0;
            if (!IsRunning) Debug.LogError($"{nameof(CombatService)}: invalid config, combat not running:\n- {string.Join("\n- ", errors)}");
        }

        public CombatConfig Config { get; }
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
