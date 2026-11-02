using System.Collections.Generic;
using BunDauPeo.Characters;

namespace BunDauPeo.Combat
{
    public enum BattleState { PlayerTurn, EnemyTurn, Won, Lost }

    public enum BattleActionKind { Basic, Skill, Ultimate }

    public enum ActionAvailability { Available, OnCooldown, NotEnoughEnergy, NoSuchSkill, NotYourTurn, Ended }

    public enum BattleActionStatus { Success, NotYourTurn, Unavailable, InvalidTarget, Ended }

    public readonly struct BattleHit
    {
        public BattleHit(BattleUnit target, int amount, bool isHeal, bool killed)
        {
            Target = target;
            Amount = amount;
            IsHeal = isHeal;
            Killed = killed;
        }

        public BattleUnit Target { get; }
        // Số tính từ công thức, không kẹp theo máu còn lại của đích.
        public int Amount { get; }
        public bool IsHeal { get; }
        public bool Killed { get; }
    }

    // Kết quả một hành động để view phát hoạt hình. Hành động thất bại không đổi gì: Hits rỗng, StateAfter bằng State lúc gọi.
    public sealed class BattleActionResult
    {
        private static readonly IReadOnlyList<BattleHit> NoHits = new BattleHit[0];

        internal BattleActionResult(BattleActionStatus status, BattleState stateAfter, BattleUnit actor = null,
            SkillDefinition skill = null, IReadOnlyList<BattleHit> hits = null, bool waveCleared = false, bool nextWaveStarted = false)
        {
            Status = status;
            StateAfter = stateAfter;
            Actor = actor;
            Skill = skill;
            Hits = hits ?? NoHits;
            WaveCleared = waveCleared;
            NextWaveStarted = nextWaveStarted;
        }

        public BattleActionStatus Status { get; }
        public BattleUnit Actor { get; }
        public SkillDefinition Skill { get; }
        public IReadOnlyList<BattleHit> Hits { get; }
        public bool WaveCleared { get; }
        public bool NextWaveStarted { get; }
        public BattleState StateAfter { get; }
    }
}
