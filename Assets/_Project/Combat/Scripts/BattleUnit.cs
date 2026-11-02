using System;
using BunDauPeo.Characters;

namespace BunDauPeo.Combat
{
    // Trạng thái của một người trong trận. Từ ngoài chỉ đọc; BattleSession là chỗ duy nhất đổi nó (qua hàm internal).
    public sealed class BattleUnit
    {
        private BattleUnit() { }

        public static BattleUnit ForAlly(FormationSlot slot)
        {
            var character = slot.Character;
            var stats = character.CombatStats;
            return new BattleUnit
            {
                DisplayName = character.DisplayName,
                IsAlly = true,
                SlotIndex = slot.SlotIndex,
                Position = slot.Position,
                Character = character,
                MaxHp = stats.HP,
                Hp = stats.HP,
                Attack = stats.Attack,
                Defense = stats.Defense,
                Speed = stats.Speed,
                BasicAttack = character.BasicAttack,
                Skill = character.Skill,
                Ultimate = character.Ultimate,
            };
        }

        public static BattleUnit ForEnemy(EnemyDefinition enemy, int slotIndex)
        {
            var stats = enemy.Stats;
            return new BattleUnit
            {
                DisplayName = enemy.DisplayName,
                IsBoss = enemy.IsBoss,
                SlotIndex = slotIndex,
                Enemy = enemy,
                MaxHp = stats.HP,
                Hp = stats.HP,
                Attack = stats.Attack,
                Defense = stats.Defense,
                Speed = stats.Speed,
                BasicAttack = enemy.BasicAttack,
                Skill = enemy.Skill,
            };
        }

        public string DisplayName { get; private set; }
        public bool IsAlly { get; private set; }
        public bool IsBoss { get; private set; }
        public int SlotIndex { get; private set; }
        // Chỉ có nghĩa với phe ta.
        public BattlePosition Position { get; private set; }
        // Cái còn lại là null.
        public CharacterDefinition Character { get; private set; }
        public EnemyDefinition Enemy { get; private set; }

        public int MaxHp { get; private set; }
        public int Hp { get; private set; }
        public int Attack { get; private set; }
        public int Defense { get; private set; }
        public int Speed { get; private set; }
        public int Energy { get; private set; }
        public int SkillCooldown { get; private set; }

        public SkillDefinition BasicAttack { get; private set; }
        public SkillDefinition Skill { get; private set; }
        // Quái không có ultimate.
        public SkillDefinition Ultimate { get; private set; }

        // Máu kẹp ở 0. Chỉ BattleSession gọi.
        internal void TakeDamage(int amount) => Hp = Math.Max(0, Hp - amount);

        // amount là số máu thực sự hồi (DamageCalculator.Heal đã kẹp theo máu thiếu).
        internal void Heal(int amount) => Hp = Math.Min(MaxHp, Hp + amount);

        internal void AddEnergy(int amount, int max) => Energy = Math.Min(max, Energy + amount);

        internal void SpendEnergy(int amount) => Energy -= amount;

        internal void StartCooldown(int turns) => SkillCooldown = turns;

        // Gọi khi tới lượt của chính nó, trước khi chọn chiêu.
        internal void TickCooldown()
        {
            if (SkillCooldown > 0) SkillCooldown--;
        }

        public bool IsAlive => Hp > 0;
        public float HpRatio => MaxHp > 0 ? (float)Hp / MaxHp : 0f;
    }
}
