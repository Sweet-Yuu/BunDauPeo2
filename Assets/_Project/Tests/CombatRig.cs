using System;
using System.Collections.Generic;
using System.Linq;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using UnityEditor;

namespace BunDauPeo.Tests
{
    // Ảnh chụp bộ nhớ ngay lúc persist chạy: để test thấy "đã đổi xong chưa" và "đã có trận mới chưa".
    internal readonly struct PersistSnapshot
    {
        public PersistSnapshot(long material, long gem, string cleared, int staminaCurrent, long staminaAnchor, bool hasBattle, bool? watchedClaimed)
        {
            Material = material;
            Gem = gem;
            Cleared = cleared;
            StaminaCurrent = staminaCurrent;
            StaminaAnchor = staminaAnchor;
            HasBattle = hasBattle;
            WatchedClaimed = watchedClaimed;
        }

        public long Material { get; }
        public long Gem { get; }
        public string Cleared { get; }
        public int StaminaCurrent { get; }
        public long StaminaAnchor { get; }
        public bool HasBattle { get; }
        public bool? WatchedClaimed { get; }
    }

    // Một save, một ví, một đồng hồ giả và mọi dịch vụ Combat nối đúng như GameBootstrap; persist là stub đếm và chụp.
    internal sealed class CombatRig
    {
        public static readonly DateTime T0 = new DateTime(2026, 11, 3, 8, 0, 0, DateTimeKind.Utc);

        private readonly List<PersistSnapshot> _snapshots = new List<PersistSnapshot>();

        public CombatRig(StageDefinition[] stages, CharacterDefinition[] team = null, Action<SerializedObject> configEdit = null)
        {
            Now = T0;
            Save = new PlayerSave();
            Wallet = new Wallet(Save);
            team ??= new[] { CombatTestData.Hero("a") };
            Roster = new Roster(Save, TestData.Catalog(team));
            foreach (var hero in team)
            {
                Roster.Add(hero.Id);
                Roster.AddToTeam(hero.Id);
            }

            Config = CombatTestData.Config(so =>
            {
                so.FindProperty("_stages").objectReferenceValue = CombatTestData.Catalog(stages);
                configEdit?.Invoke(so);
            });
            Stamina = new StaminaService(Save, Config, () => Now);
            Progress = new StageProgressService(Save, Wallet, Config.Stages, Stamina, Persist);
            Service = CombatTestData.Service(Config, Roster, Persist, Stamina, Progress);
        }

        public DateTime Now { get; set; }
        public PlayerSave Save { get; }
        public Wallet Wallet { get; }
        public Roster Roster { get; }
        public CombatConfig Config { get; }
        public StaminaService Stamina { get; }
        public StageProgressService Progress { get; }
        public CombatService Service { get; }
        public bool PersistResult { get; set; } = true;
        public int PersistCalls { get; private set; }
        // Trận mà test muốn biết RewardsClaimed của nó lúc persist chạy.
        public BattleSession Watched { get; set; }
        public IReadOnlyList<PersistSnapshot> Snapshots => _snapshots;

        public bool Persist()
        {
            PersistCalls++;
            _snapshots.Add(new PersistSnapshot(Wallet.Get(CurrencyType.Material), Wallet.Get(CurrencyType.Gem),
                string.Join(",", Save.ClearedStages), Save.Stamina.Current, Save.Stamina.AnchorUtcTicks,
                Service?.Current != null, Watched?.RewardsClaimed));
            return PersistResult;
        }

        // Đặt thể lực thô đang hồi dở: Current điểm, mốc cách bây giờ `secondsAgo` giây.
        public void SetStamina(int current, double secondsAgo = 0)
        {
            Save.Stamina.Current = current;
            Save.Stamina.AnchorUtcTicks = Now.Ticks - (long)(secondsAgo * TimeSpan.TicksPerSecond);
        }

        // Mọi thứ một lần thử có thể làm đổi (trừ trận hiện tại và số lần persist): so hai chuỗi này để chứng minh "không đổi gì".
        public string State()
        {
            _ = Stamina.Current; // chốt phần hồi trước khi đọc số thô, vì đọc là chốt
            return string.Join("|", Wallet.Get(CurrencyType.Material), Wallet.Get(CurrencyType.Gem),
                string.Join(",", Save.ClearedStages), Save.Stamina.Current, Save.Stamina.AnchorUtcTicks, PersistCalls);
        }

        public void Clear(params string[] ids) => Save.ClearedStages.AddRange(ids.Where(id => !Save.ClearedStages.Contains(id)));
    }
}
