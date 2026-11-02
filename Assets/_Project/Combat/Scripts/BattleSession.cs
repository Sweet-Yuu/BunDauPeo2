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
        public IReadOnlyList<BattleUnit> Allies => _allies;
        // Đợt hiện tại; SlotIndex là chỉ số trong mảng đợt.
        public IReadOnlyList<BattleUnit> Enemies => _enemies;
        // Chỉ StageProgressService đặt.
        public bool RewardsClaimed { get; internal set; }

        private bool IsEnded => State == BattleState.Won || State == BattleState.Lost;

        // Người sắp hành động; null khi trận đã hết.
        public BattleUnit Current => _queue.Count > 0 ? _queue[0] : null;

        // Còn lại trong vòng này, để vẽ thanh lượt. [0] là Current.
        public IReadOnlyList<BattleUnit> RemainingOrder => _queue.Where(u => u.IsAlive).ToList();

        public ActionAvailability GetAvailability(BattleActionKind kind)
        {
            if (IsEnded) return ActionAvailability.Ended;
            if (State != BattleState.PlayerTurn) return ActionAvailability.NotYourTurn;
            return kind == BattleActionKind.Basic ? ActionAvailability.Available : ActionAvailability.NoSuchSkill;
        }

        // null nếu kỹ năng không cần mục tiêu (hoặc trận đã hết).
        public BattleUnit DefaultTarget(BattleActionKind kind) =>
            !IsEnded && kind == BattleActionKind.Basic ? _enemies.FirstOrDefault(e => e.IsAlive) : null;

        public BattleActionResult PlayerAct(BattleActionKind kind, BattleUnit target = null)
        {
            if (IsEnded) return Rejected(BattleActionStatus.Ended);
            if (State != BattleState.PlayerTurn) return Rejected(BattleActionStatus.NotYourTurn);
            if (kind != BattleActionKind.Basic) return Rejected(BattleActionStatus.Unavailable);

            var chosen = target ?? DefaultTarget(kind);
            if (chosen == null || !chosen.IsAlive || !_enemies.Contains(chosen)) return Rejected(BattleActionStatus.InvalidTarget);

            var actor = Current;
            return Finish(actor, actor.BasicAttack, Strike(actor, actor.BasicAttack, chosen));
        }

        public BattleActionResult EnemyAct()
        {
            if (IsEnded) return Rejected(BattleActionStatus.Ended);
            if (State != BattleState.EnemyTurn) return Rejected(BattleActionStatus.NotYourTurn);

            var actor = Current;
            return Finish(actor, actor.BasicAttack, Strike(actor, actor.BasicAttack, PickAllyTarget()));
        }

        public BattleActionResult Surrender()
        {
            if (IsEnded) return Rejected(BattleActionStatus.Ended);
            State = BattleState.Lost;
            _queue.Clear();
            return new BattleActionResult(BattleActionStatus.Success, State);
        }

        private BattleActionResult Rejected(BattleActionStatus status) => new BattleActionResult(status, State);

        private static BattleHit Strike(BattleUnit actor, SkillDefinition skill, BattleUnit target)
        {
            var amount = DamageCalculator.Damage(actor.Attack, skill.Power, target.Defense);
            target.TakeDamage(amount);
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

        private BattleActionResult Finish(BattleUnit actor, SkillDefinition skill, BattleHit hit)
        {
            var waveCleared = false;
            var nextWave = false;
            if (!_enemies.Any(e => e.IsAlive))
            {
                waveCleared = true;
                if (WaveIndex + 1 < WaveCount)
                {
                    // Vòng mới: người chưa kịp đánh ở vòng cũ không được đánh bù.
                    WaveIndex++;
                    Round++;
                    SpawnWave();
                    BuildQueue();
                    nextWave = true;
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

            return new BattleActionResult(BattleActionStatus.Success, State, actor, skill, new[] { hit }, waveCleared, nextWave);
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
            else
            {
                Round++;
                BuildQueue();
            }
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

        private void UpdateTurnState() => State = Current.IsAlly ? BattleState.PlayerTurn : BattleState.EnemyTurn;
    }
}
