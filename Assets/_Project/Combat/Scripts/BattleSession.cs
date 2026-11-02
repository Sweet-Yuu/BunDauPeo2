using System;
using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Characters;

namespace BunDauPeo.Combat
{
    // Một trận chạy đồng bộ và tức thì; view phát hoạt hình từ BattleActionResult. Ngoài bộ nhớ của trận, nó không đổi dữ liệu nào
    // (thưởng và lưu do StageProgressService).
    public sealed class BattleSession
    {
        private readonly StageDefinition _stage;
        private readonly CombatConfig _config;
        private readonly Random _random;
        private readonly List<BattleUnit> _allies;
        private readonly List<BattleUnit> _queue = new List<BattleUnit>();
        private List<BattleUnit> _enemies;

        public BattleSession(IReadOnlyList<CharacterDefinition> team, StageDefinition stage, CombatConfig config, Random random)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            if (team == null || team.Count == 0) throw new ArgumentException("Team must have at least one character.", nameof(team));
            if (stage == null || stage.Kind != StageKind.Battle || stage.Waves.Count == 0
                || stage.Waves.Any(w => w == null || w.Enemies.Count == 0 || w.Enemies.Any(e => e == null)))
                throw new ArgumentException("Stage must be a Battle with at least one enemy in every wave.", nameof(stage));

            _stage = stage;
            _allies = BattleFormation.Assign(team).Select(BattleUnit.ForAlly).OrderBy(u => u.SlotIndex).ToList();
            Round = 1;
            SpawnWave();
            BuildQueue();
        }

        public StageDefinition Stage => _stage;
        public BattleState State { get; private set; }
        public int WaveIndex { get; private set; }
        public int WaveCount => _stage.Waves.Count;
        public int Round { get; private set; }
        internal CombatConfig Config => _config;
        public IReadOnlyList<BattleUnit> Allies => _allies;
        // Đợt hiện tại; SlotIndex là chỉ số trong mảng đợt.
        public IReadOnlyList<BattleUnit> Enemies => _enemies;
        // Chỉ StageProgressService đặt.
        public bool RewardsClaimed { get; internal set; }
        // Chỉ CombatService.EnterStage đặt, sau khi thể lực đã trừ và lưu; trận từ StartBattle (dải DEV) luôn false.
        public bool EntryPaid { get; internal set; }

        private bool IsEnded => State == BattleState.Won || State == BattleState.Lost;

        // Người sắp hành động; null khi trận đã hết.
        public BattleUnit Current => _queue.Count > 0 ? _queue[0] : null;

        // Còn lại trong vòng này, để vẽ thanh lượt. [0] là Current.
        public IReadOnlyList<BattleUnit> RemainingOrder => _queue.Where(u => u.IsAlive).ToList();

        public ActionAvailability GetAvailability(BattleActionKind kind)
        {
            if (IsEnded) return ActionAvailability.Ended;
            if (State != BattleState.PlayerTurn) return ActionAvailability.NotYourTurn;
            var skill = SkillOf(Current, kind);
            if (skill == null) return ActionAvailability.NoSuchSkill;
            if (kind == BattleActionKind.Skill && Current.SkillCooldown > 0) return ActionAvailability.OnCooldown;
            if (kind == BattleActionKind.Ultimate && Current.Energy < UltimateCost(skill)) return ActionAvailability.NotEnoughEnergy;
            return ActionAvailability.Available;
        }

        // null nếu kỹ năng không cần mục tiêu, không có kỹ năng đó, hoặc không phải lượt người chơi.
        public BattleUnit DefaultTarget(BattleActionKind kind)
        {
            if (State != BattleState.PlayerTurn) return null;
            var skill = SkillOf(Current, kind);
            return skill == null ? null : DefaultTarget(skill);
        }

        public BattleActionResult PlayerAct(BattleActionKind kind, BattleUnit target = null)
        {
            if (IsEnded) return Rejected(BattleActionStatus.Ended);
            if (State != BattleState.PlayerTurn) return Rejected(BattleActionStatus.NotYourTurn);
            if (GetAvailability(kind) != ActionAvailability.Available) return Rejected(BattleActionStatus.Unavailable);

            var actor = Current;
            var skill = SkillOf(actor, kind);
            var targets = PlayerTargets(actor, skill, target);
            if (targets == null) return Rejected(BattleActionStatus.InvalidTarget);

            if (kind == BattleActionKind.Ultimate) actor.SpendEnergy(UltimateCost(skill));
            else if (kind == BattleActionKind.Skill)
            {
                actor.StartCooldown(skill.Cooldown);
                actor.AddEnergy(_config.EnergyOnSkill, _config.MaxEnergy);
            }
            else
            {
                actor.AddEnergy(_config.EnergyOnBasic, _config.MaxEnergy);
            }

            return Finish(actor, skill, Apply(actor, skill, targets));
        }

        public BattleActionResult EnemyAct()
        {
            if (IsEnded) return Rejected(BattleActionStatus.Ended);
            if (State != BattleState.EnemyTurn) return Rejected(BattleActionStatus.NotYourTurn);

            var actor = Current;
            var useSkill = actor.Skill != null && actor.SkillCooldown == 0;
            var skill = useSkill ? actor.Skill : actor.BasicAttack;
            if (useSkill) actor.StartCooldown(skill.Cooldown);

            // Đánh cả phe ta thì không gọi RNG; đánh một người thì đúng một lần (PickAllyTarget).
            var targets = skill.Target == SkillTarget.AllEnemies ? AliveOf(_allies) : new List<BattleUnit> { PickAllyTarget() };
            return Finish(actor, skill, Apply(actor, skill, targets));
        }

        public BattleActionResult Surrender()
        {
            if (IsEnded) return Rejected(BattleActionStatus.Ended);
            State = BattleState.Lost;
            _queue.Clear();
            return new BattleActionResult(BattleActionStatus.Success, State);
        }

        private BattleActionResult Rejected(BattleActionStatus status) => new BattleActionResult(status, State);

        private static SkillDefinition SkillOf(BattleUnit unit, BattleActionKind kind) => kind switch
        {
            BattleActionKind.Basic => unit.BasicAttack,
            BattleActionKind.Skill => unit.Skill,
            _ => unit.Ultimate,
        };

        // Giá vượt năng lượng tối đa thì chỉ cần đầy thanh.
        private int UltimateCost(SkillDefinition ultimate) => Math.Min(ultimate.EnergyCost, _config.MaxEnergy);

        private static List<BattleUnit> AliveOf(IEnumerable<BattleUnit> units) => units.Where(u => u.IsAlive).ToList();

        private BattleUnit DefaultTarget(SkillDefinition skill) => skill.Target switch
        {
            SkillTarget.SingleEnemy => _enemies.FirstOrDefault(e => e.IsAlive),
            // _allies đã theo SlotIndex và OrderBy ổn định, nên hoà tỉ lệ thì ô thấp trước.
            SkillTarget.SingleAlly => _allies.Where(a => a.IsAlive).OrderBy(a => a.HpRatio).FirstOrDefault(),
            _ => null,
        };

        // null nếu mục tiêu người chơi truyền vào sai (chết, sai phe). Chiêu không cần chọn thì bỏ qua `chosen`.
        private List<BattleUnit> PlayerTargets(BattleUnit actor, SkillDefinition skill, BattleUnit chosen)
        {
            switch (skill.Target)
            {
                case SkillTarget.AllEnemies: return AliveOf(_enemies);
                case SkillTarget.AllAllies: return AliveOf(_allies);
                case SkillTarget.Self: return new List<BattleUnit> { actor };
            }

            var side = skill.Target == SkillTarget.SingleEnemy ? _enemies : (IReadOnlyList<BattleUnit>)_allies;
            var target = chosen ?? DefaultTarget(skill);
            return target != null && target.IsAlive && side.Contains(target) ? new List<BattleUnit> { target } : null;
        }

        private List<BattleHit> Apply(BattleUnit actor, SkillDefinition skill, IEnumerable<BattleUnit> targets) =>
            targets.Select(t => ApplyTo(actor, skill, t)).ToList();

        private BattleHit ApplyTo(BattleUnit actor, SkillDefinition skill, BattleUnit target)
        {
            if (skill.Effect == SkillEffect.Heal)
            {
                var healed = DamageCalculator.Heal(actor.Attack, skill.Power, target.Hp, target.MaxHp);
                target.Heal(healed);
                return new BattleHit(target, healed, true, false);
            }

            var amount = DamageCalculator.Damage(actor.Attack, skill.Power, target.Defense);
            target.TakeDamage(amount);
            // Sát thương luôn >= 1, nên mỗi lần trúng đều cộng.
            if (target.IsAlly) target.AddEnergy(_config.EnergyOnHit, _config.MaxEnergy);
            return new BattleHit(target, amount, false, !target.IsAlive);
        }

        // Đúng một NextDouble mỗi lần chọn, kể cả khi chỉ còn một người: số lần gọi RNG là hợp đồng test.
        private BattleUnit PickAllyTarget()
        {
            var alive = _allies.Where(a => a.IsAlive).ToList();
            var total = alive.Sum(a => (double)_config.Weight(a.Position));
            var roll = _random.NextDouble() * total;
            var cumulative = 0.0;
            foreach (var ally in alive)
            {
                cumulative += _config.Weight(ally.Position);
                if (roll < cumulative) return ally;
            }

            return alive[alive.Count - 1];
        }

        private BattleActionResult Finish(BattleUnit actor, SkillDefinition skill, IReadOnlyList<BattleHit> hits)
        {
            var waveCleared = false;
            var nextWave = false;
            if (!_enemies.Any(e => e.IsAlive))
            {
                waveCleared = true;
                if (WaveIndex + 1 < WaveCount)
                {
                    // Vòng mới: người chưa kịp đánh ở vòng cũ không được đánh bù.
                    if (++Round > _config.MaxRounds) End(BattleState.Lost);
                    else
                    {
                        WaveIndex++;
                        SpawnWave();
                        BuildQueue();
                        nextWave = true;
                    }
                }
                else
                {
                    End(BattleState.Won);
                }
            }
            else if (!_allies.Any(a => a.IsAlive))
            {
                End(BattleState.Lost);
            }
            else
            {
                AdvanceQueue();
            }

            return new BattleActionResult(BattleActionStatus.Success, State, actor, skill, hits, waveCleared, nextWave);
        }

        private void End(BattleState outcome)
        {
            State = outcome;
            _queue.Clear();
        }

        private void SpawnWave()
        {
            var wave = _stage.Waves[WaveIndex].Enemies;
            _enemies = wave.Select(BattleUnit.ForEnemy).ToList();
        }

        private void AdvanceQueue()
        {
            _queue.RemoveAt(0);
            while (_queue.Count > 0 && !_queue[0].IsAlive) _queue.RemoveAt(0);
            if (_queue.Count > 0) UpdateTurnState();
            else if (++Round > _config.MaxRounds) End(BattleState.Lost);
            else BuildQueue();
        }

        // Speed giảm; hoà thì phe ta trước, rồi ô thấp trước. Thứ tự này đầy đủ nên không phụ thuộc thuật toán sắp.
        private void BuildQueue()
        {
            _queue.Clear();
            _queue.AddRange(_allies.Concat(_enemies).Where(u => u.IsAlive));
            _queue.Sort((a, b) =>
            {
                if (a.Speed != b.Speed) return b.Speed.CompareTo(a.Speed);
                if (a.IsAlly != b.IsAlly) return a.IsAlly ? -1 : 1;
                return a.SlotIndex.CompareTo(b.SlotIndex);
            });
            UpdateTurnState();
        }

        // Mỗi lần có người thành Current đúng một lần, nên đây là chỗ hồi chiêu giảm (spec 4.3 bước 2).
        private void UpdateTurnState()
        {
            State = Current.IsAlly ? BattleState.PlayerTurn : BattleState.EnemyTurn;
            Current.TickCooldown();
        }
    }
}
