using System.Collections.Generic;
using BunDauPeo.Characters;

namespace BunDauPeo.Combat
{
    // Ở Combat chứ không ở SkillDefinition.Validate() để không làm đỏ test và asset sẵn có của Characters.
    public static class CombatSkillRules
    {
        // Mỗi lỗi bắt đầu bằng tên field ("Power: ...").
        public static List<string> Validate(SkillDefinition skill, SkillKind kind)
        {
            var errors = new List<string>();
            if (skill == null)
            {
                errors.Add("Skill: missing");
                return errors;
            }

            if (skill.Kind != kind) errors.Add($"Kind: must be {kind}, found {skill.Kind}");
            // Viết dạng !(x > 0) để NaN cũng bị bắt.
            if (!(skill.Power > 0f)) errors.Add("Power: must be > 0");

            if (skill.Effect == SkillEffect.Damage)
            {
                if (skill.Target != SkillTarget.SingleEnemy && skill.Target != SkillTarget.AllEnemies)
                    errors.Add($"Target: Damage needs SingleEnemy or AllEnemies, found {skill.Target}");
            }
            else if (skill.Target != SkillTarget.SingleAlly && skill.Target != SkillTarget.AllAllies && skill.Target != SkillTarget.Self)
            {
                errors.Add($"Target: Heal needs SingleAlly, AllAllies or Self, found {skill.Target}");
            }

            if (kind == SkillKind.Skill && skill.Cooldown < 1) errors.Add("Cooldown: a Skill needs >= 1");
            if (kind == SkillKind.Ultimate && skill.EnergyCost < 1) errors.Add("EnergyCost: an Ultimate needs >= 1");
            return errors;
        }
    }
}
