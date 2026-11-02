using System;
using System.Collections.Generic;
using BunDauPeo.Core;

namespace BunDauPeo.Combat
{
    public enum StageStatus { Unknown, Locked, Available, Cleared }

    public enum StageEnterStatus { Ready, Unknown, Invalid, WrongKind, Locked, EmptyTeam, NotEnoughStamina }

    public enum StageCompleteStatus { Success, NotWon, AlreadyClaimed, AlreadyCleared, Unknown, Invalid, WrongKind, Locked, NotPaid }

    public sealed class StageCompleteResult
    {
        private static readonly IReadOnlyList<CurrencyAmount> NoRewards = Array.Empty<CurrencyAmount>();

        internal StageCompleteResult(StageCompleteStatus status, bool firstClear = false, IReadOnlyList<CurrencyAmount> rewards = null,
            int staminaSpent = 0, bool saved = false)
        {
            Status = status;
            FirstClear = firstClear;
            Rewards = rewards ?? NoRewards;
            StaminaSpent = staminaSpent;
            Saved = saved;
        }

        public StageCompleteStatus Status { get; }
        public bool FirstClear { get; }
        public IReadOnlyList<CurrencyAmount> Rewards { get; }
        // Thể lực đã trả lúc vào trận, chỉ để hiện; CompleteBattle không đụng thể lực.
        public int StaminaSpent { get; }
        public bool Saved { get; }
    }

    // Tiến độ màn và thưởng (spec Combat 4.6). Mọi bước có thể thất bại đứng trước bước đổi đầu tiên; đọc thẳng PlayerSave
    // mỗi lần nên Reset (ghi đè tại chỗ) không để lại gì cũ. Catalog null (config lỗi) thì mọi id là Unknown.
    public sealed class StageProgressService
    {
        private readonly PlayerSave _save;
        private readonly Wallet _wallet;
        private readonly StageCatalog _catalog;
        private readonly StaminaService _stamina;
        private readonly Func<bool> _persist;

        public StageProgressService(PlayerSave save, Wallet wallet, StageCatalog catalog, StaminaService stamina, Func<bool> persist)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _catalog = catalog;
            _stamina = stamina ?? throw new ArgumentNullException(nameof(stamina));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
        }

        public event Action Changed;

        // Màn thứ i mở khi i = 0, màn i-1 đã clear, hoặc chính nó đã clear (chèn màn vào giữa không khoá lại người đã đi qua).
        public StageStatus GetStatus(string id)
        {
            var index = IndexOf(id);
            if (index < 0) return StageStatus.Unknown;
            if (_save.ClearedStages.Contains(id)) return StageStatus.Cleared;
            if (index == 0) return StageStatus.Available;
            var previous = _catalog.Items[index - 1];
            return previous != null && _save.ClearedStages.Contains(previous.Id) ? StageStatus.Available : StageStatus.Locked;
        }

        // Chỉ hỏi, không đổi gì. Thứ tự kiểm là hợp đồng.
        public StageEnterStatus CanEnter(string id, int teamSize)
        {
            if (!TryGetStage(id, out var stage)) return StageEnterStatus.Unknown;
            if (stage.Validate().Count > 0) return StageEnterStatus.Invalid;
            if (stage.Kind != StageKind.Battle) return StageEnterStatus.WrongKind;
            if (GetStatus(id) == StageStatus.Locked) return StageEnterStatus.Locked;
            if (teamSize < 1) return StageEnterStatus.EmptyTeam;
            return _stamina.CanAfford(stage.StaminaCost) ? StageEnterStatus.Ready : StageEnterStatus.NotEnoughStamina;
        }

        public StageCompleteResult CompleteBattle(BattleSession won)
        {
            if (won == null) throw new ArgumentNullException(nameof(won));
            if (won.State != BattleState.Won) return new StageCompleteResult(StageCompleteStatus.NotWon);
            if (won.RewardsClaimed) return new StageCompleteResult(StageCompleteStatus.AlreadyClaimed);
            // Trận từ StartBattle của dải DEV chưa trả thể lực: thắng cũng không có thưởng.
            if (!won.EntryPaid) return new StageCompleteResult(StageCompleteStatus.NotPaid);

            var id = won.Stage.Id;
            if (!TryGetStage(id, out var stage)) return new StageCompleteResult(StageCompleteStatus.Unknown);
            if (stage.Validate().Count > 0) return new StageCompleteResult(StageCompleteStatus.Invalid);
            if (stage.Kind != StageKind.Battle) return new StageCompleteResult(StageCompleteStatus.WrongKind);
            if (GetStatus(id) == StageStatus.Locked) return new StageCompleteResult(StageCompleteStatus.Locked);

            var firstClear = !_save.ClearedStages.Contains(id);
            var rewards = firstClear ? stage.FirstClearReward : stage.RepeatReward;
            foreach (var line in rewards) _wallet.Add(line.Type, line.Amount);
            if (firstClear) _save.ClearedStages.Add(id);
            won.RewardsClaimed = true;
            Changed.SafeInvoke();
            return new StageCompleteResult(StageCompleteStatus.Success, firstClear, rewards, stage.StaminaCost, _persist());
        }

        public StageCompleteResult CompleteStory(string id)
        {
            if (!TryGetStage(id, out var stage)) return new StageCompleteResult(StageCompleteStatus.Unknown);
            if (stage.Validate().Count > 0) return new StageCompleteResult(StageCompleteStatus.Invalid);
            if (stage.Kind != StageKind.Story) return new StageCompleteResult(StageCompleteStatus.WrongKind);
            var status = GetStatus(id);
            if (status == StageStatus.Cleared) return new StageCompleteResult(StageCompleteStatus.AlreadyCleared);
            if (status == StageStatus.Locked) return new StageCompleteResult(StageCompleteStatus.Locked);

            _save.ClearedStages.Add(id);
            Changed.SafeInvoke();
            return new StageCompleteResult(StageCompleteStatus.Success, saved: _persist());
        }

        private int IndexOf(string id)
        {
            if (_catalog == null || string.IsNullOrEmpty(id)) return -1;
            var items = _catalog.Items;
            for (var i = 0; i < items.Count; i++)
                if (items[i] != null && items[i].Id == id) return i;
            return -1;
        }

        private bool TryGetStage(string id, out StageDefinition stage)
        {
            stage = null;
            return _catalog != null && _catalog.TryGet(id, out stage);
        }
    }
}
