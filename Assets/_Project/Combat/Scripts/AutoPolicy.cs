using System;
using System.Linq;
using BunDauPeo.Characters;

namespace BunDauPeo.Combat
{
    public readonly struct AutoChoice
    {
        public AutoChoice(BattleActionKind kind, BattleUnit target)
        {
            Kind = kind;
            Target = target;
        }

        public BattleActionKind Kind { get; }
        public BattleUnit Target { get; }
    }

    // Chọn hành động cho nút AUTO. Thuần: chỉ đọc trận, không đổi gì.
    public static class AutoPolicy
    {
        public static AutoChoice Choose(BattleSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (session.State != BattleState.PlayerTurn)
                throw new InvalidOperationException($"AutoPolicy needs a player turn, state is {session.State}.");

            foreach (var kind in new[] { BattleActionKind.Ultimate, BattleActionKind.Skill })
                if (session.GetAvailability(kind) == ActionAvailability.Available && IsWorthUsing(session, kind))
                    return new AutoChoice(kind, session.DefaultTarget(kind));

            return new AutoChoice(BattleActionKind.Basic, session.DefaultTarget(BattleActionKind.Basic));
        }

        // Chiêu hồi máu chỉ đáng dùng khi có người dưới ngưỡng; đúng ngưỡng thì chưa.
        private static bool IsWorthUsing(BattleSession session, BattleActionKind kind)
        {
            var skill = kind == BattleActionKind.Ultimate ? session.Current.Ultimate : session.Current.Skill;
            if (skill.Effect != SkillEffect.Heal) return true;

            // Chuẩn hoá float như DamageCalculator: 0.7f thật là 0,699999988.
            var below = Math.Round((double)session.Config.AutoHealBelow, 4);
            return session.Allies.Any(a => a.IsAlive && a.Hp < below * a.MaxHp);
        }
    }
}
